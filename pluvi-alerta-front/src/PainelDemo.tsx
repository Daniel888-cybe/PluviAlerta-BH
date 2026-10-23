import { useEffect, useRef, useState } from 'react'
import {
  apagarRelatos,
  encerrarSimulacoes,
  entrarComoAdmin,
  SessaoExpirada,
  simularTempestade,
  type RegiaoAtual,
} from './api'

type Props = {
  regioes: RegiaoAtual[]
  aoMudar: (regiaoId?: number) => void // pede ao App para recarregar o painel
  aoFechar: () => void
}

const CHAVE_TOKEN = 'pluvialerta-token-admin'

// sessionStorage: o login vale até fechar a aba (e o token expira em 2 h de qualquer jeito).
function lerToken() {
  try {
    return sessionStorage.getItem(CHAVE_TOKEN)
  } catch {
    return null
  }
}

function gravarToken(token: string | null) {
  try {
    if (token) sessionStorage.setItem(CHAVE_TOKEN, token)
    else sessionStorage.removeItem(CHAVE_TOKEN)
  } catch {
    // navegador sem storage: segue só em memória
  }
}

// Área de demonstração: dispara a tempestade fictícia sem precisar do Swagger.
export function PainelDemo({ regioes, aoMudar, aoFechar }: Props) {
  const [token, setToken] = useState<string | null>(lerToken)
  const [usuario, setUsuario] = useState('admin')
  const [senha, setSenha] = useState('')
  const [regiaoId, setRegiaoId] = useState<number | null>(null)
  const [chuva, setChuva] = useState(60)
  const [previsao, setPrevisao] = useState(40)
  const [relatos, setRelatos] = useState(4)
  const [ocupado, setOcupado] = useState(false)
  const [mensagem, setMensagem] = useState<{ tipo: 'ok' | 'erro'; texto: string } | null>(null)
  const secao = useRef<HTMLElement>(null)

  // Aberto pelo link do rodapé: rola a página até o painel.
  useEffect(() => {
    secao.current?.scrollIntoView({ behavior: 'smooth', block: 'start' })
  }, [])

  function sair(texto?: string) {
    gravarToken(null)
    setToken(null)
    setMensagem(texto ? { tipo: 'erro', texto } : null)
  }

  async function executar(acao: () => Promise<void>) {
    setOcupado(true)
    setMensagem(null)
    try {
      await acao()
    } catch (e) {
      if (e instanceof SessaoExpirada) sair(e.message)
      else setMensagem({ tipo: 'erro', texto: e instanceof Error ? e.message : 'Algo deu errado.' })
    } finally {
      setOcupado(false)
    }
  }

  const entrar = () =>
    executar(async () => {
      const novo = await entrarComoAdmin(usuario, senha)
      gravarToken(novo)
      setToken(novo)
      setSenha('')
    })

  const simular = () =>
    executar(async () => {
      if (!token || regiaoId === null) return
      const r = await simularTempestade(token, {
        regiaoId, chuvaAcum6hMm: chuva, previsao3hMm: previsao, relatos,
      })
      setMensagem({
        tipo: 'ok',
        texto: `${r.nome}: score ${r.score} (${r.nivel}). Se o nível subiu para Laranja ou Vermelho, ` +
          'os inscritos dessa regional receberam o alerta no Telegram.',
      })
      aoMudar(r.regiaoId)
    })

  const encerrar = () =>
    executar(async () => {
      if (!token) return
      const r = await encerrarSimulacoes(token)
      setMensagem({
        tipo: 'ok',
        texto: r.scoresSimuladosApagados > 0
          ? 'Simulações encerradas. O painel voltou ao dado real.'
          : 'Não havia simulação ativa.',
      })
      aoMudar()
    })

  const limparRelatos = () =>
    executar(async () => {
      if (!token || !window.confirm('Apagar TODOS os relatos do mapa?')) return
      const r = await apagarRelatos(token)
      setMensagem({ tipo: 'ok', texto: `${r.relatosApagados} relato(s) apagado(s). O mapa está limpo.` })
      aoMudar()
    })

  return (
    <section ref={secao} className="cartao painel-demo" aria-label="Área de demonstração">
      <div className="demo-topo">
        <div>
          <p className="painel-kicker">Área de demonstração</p>
          <h2>Simular uma tempestade</h2>
          <p className="demo-sub">
            Injeta chuva fictícia numa regional para mostrar o score subindo e o alerta no Telegram.
            Funciona sem internet.
          </p>
        </div>
        <button type="button" className="btn-secundario" onClick={aoFechar}>
          Fechar
        </button>
      </div>

      {!token ? (
        <form
          className="demo-login"
          onSubmit={(e) => {
            e.preventDefault()
            entrar()
          }}
        >
          <label>
            Usuário
            <input value={usuario} onChange={(e) => setUsuario(e.target.value)} autoComplete="username" />
          </label>
          <label>
            Senha
            <input
              type="password"
              value={senha}
              onChange={(e) => setSenha(e.target.value)}
              autoComplete="current-password"
              placeholder="ADMIN_SENHA do .env"
            />
          </label>
          <button type="submit" className="btn-enviar" disabled={ocupado || !senha}>
            {ocupado ? 'Entrando…' : 'Entrar'}
          </button>
        </form>
      ) : (
        <>
          <p className="demo-passo">1. Escolha a regional</p>
          <div className="demo-regioes">
            {regioes.map((r) => (
              <button
                key={r.id}
                type="button"
                className={regiaoId === r.id ? 'tipo ativo' : 'tipo'}
                onClick={() => setRegiaoId(r.id)}
              >
                {r.nome}
              </button>
            ))}
          </div>

          <p className="demo-passo">2. Intensidade (padrão: tempestade forte)</p>
          <div className="demo-campos">
            <label>
              Chuva em 6 h (mm)
              <input type="number" min={0} max={500} value={chuva} onChange={(e) => setChuva(Number(e.target.value))} />
            </label>
            <label>
              Previsão 3 h (mm)
              <input type="number" min={0} max={500} value={previsao} onChange={(e) => setPrevisao(Number(e.target.value))} />
            </label>
            <label>
              Relatos fictícios
              <input type="number" min={0} max={20} value={relatos} onChange={(e) => setRelatos(Number(e.target.value))} />
            </label>
          </div>

          <div className="demo-acoes">
            <button type="button" className="btn-tempestade" disabled={ocupado || regiaoId === null} onClick={simular}>
              ⛈️ Simular tempestade
            </button>
            <button type="button" className="btn-secundario" disabled={ocupado} onClick={encerrar}>
              Encerrar simulações
            </button>
            <button type="button" className="btn-secundario" disabled={ocupado} onClick={limparRelatos}>
              🧹 Apagar relatos
            </button>
            <button type="button" className="btn-link" onClick={() => sair()}>
              Sair
            </button>
          </div>
        </>
      )}

      {mensagem && (
        <p className={mensagem.tipo === 'ok' ? 'aviso-relato' : 'form-relato-erro'} role="status">
          {mensagem.texto}
        </p>
      )}
    </section>
  )
}
