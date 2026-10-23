import { useCallback, useEffect, useState } from 'react'
import './App.css'
import './integracao.css'
import logoEscudo from './assets/logo-escudo.png'
import qrTelegram from './assets/qr-telegram.svg' // gerado uma vez: npx qrcode https://t.me/PluviAlerta_bot
import {
  buscarHistorico,
  buscarRegioes,
  buscarRelatosAtivos,
  LINK_TELEGRAM,
  VALIDADE_RELATO_HORAS,
  type HistoricoRegiao,
  type RegiaoAtual,
  type Relato,
} from './api'
import { FormRelato } from './FormRelato'
import { MapaRegionais } from './MapaRegionais'
import { PainelDemo } from './PainelDemo'
import { PainelRegional } from './PainelRegional'
import { classeNivel, descricaoNivel, emAlerta, formatarHora, formatarMm } from './risco'

// A API recalcula a cada 15 min; o painel confere a cada 1 min para pegar a simulação da demo rápido.
const ATUALIZAR_A_CADA_MS = 60_000

function App() {
  const [regioes, setRegioes] = useState<RegiaoAtual[]>([])
  const [relatos, setRelatos] = useState<Relato[]>([])
  const [selecionadaId, setSelecionadaId] = useState<number | null>(null)
  const [historico, setHistorico] = useState<HistoricoRegiao | null>(null)
  const [carregando, setCarregando] = useState(true)
  const [erro, setErro] = useState('')
  const [modoRelato, setModoRelato] = useState(false)
  const [pontoRelato, setPontoRelato] = useState<[number, number] | null>(null)
  const [avisoRelato, setAvisoRelato] = useState('')
  // Área de demonstração: abre pelo link do rodapé ou pelo endereço .../#demo
  const [mostrarDemo, setMostrarDemo] = useState(() => window.location.hash === '#demo')

  useEffect(() => {
    const aoMudarHash = () => setMostrarDemo(window.location.hash === '#demo')
    window.addEventListener('hashchange', aoMudarHash)
    return () => window.removeEventListener('hashchange', aoMudarHash)
  }, [])

  const atualizar = useCallback(async () => {
    try {
      const [novasRegioes, novosRelatos] = await Promise.all([buscarRegioes(), buscarRelatosAtivos()])
      setRegioes(novasRegioes)
      setRelatos(novosRelatos)
      setErro('')
    } catch {
      setErro('Não foi possível carregar os dados. Verifique se a API está funcionando.')
    } finally {
      setCarregando(false)
    }
  }, [])

  useEffect(() => {
    atualizar()
    const timer = setInterval(atualizar, ATUALIZAR_A_CADA_MS)
    return () => clearInterval(timer)
  }, [atualizar])

  // Começa mostrando a regional de maior score.
  const maior = [...regioes].sort((a, b) => b.score - a.score)[0]
  const idPainel = selecionadaId ?? maior?.id ?? null
  const regiaoPainel = regioes.find((r) => r.id === idPainel) ?? null
  const atualizadoEmPainel = regiaoPainel?.atualizadoEm

  useEffect(() => {
    if (idPainel === null) return
    buscarHistorico(idPainel).then(setHistorico).catch(() => setHistorico(null))
  }, [idPainel, atualizadoEmPainel])

  const emAlertaAgora = regioes.filter(emAlerta).sort((a, b) => b.score - a.score)
  const maisChuva = [...regioes].sort((a, b) => b.chuvaAcum6hMm - a.chuvaAcum6hMm)[0]
  const ultimaAtualizacao = regioes
    .map((r) => r.atualizadoEm)
    .filter((d): d is string => d !== null)
    .sort()
    .at(-1) ?? null
  const temSimulacao = regioes.some((r) => r.fonte === 'simulacao')
  const ordenadas = [...regioes].sort((a, b) => b.score - a.score)

  function fecharDemo() {
    // Tira o #demo do endereço sem recarregar a página.
    history.replaceState(null, '', window.location.pathname + window.location.search)
    setMostrarDemo(false)
  }

  async function demoMudou(regiaoId?: number) {
    if (regiaoId !== undefined) setSelecionadaId(regiaoId)
    await atualizar()
  }

  function fecharRelato() {
    setModoRelato(false)
    setPontoRelato(null)
  }

  async function relatoEnviado(relato: Relato) {
    fecharRelato()
    const nome = regioes.find((r) => r.id === relato.regiaoId)?.nome ?? 'sua regional'
    setAvisoRelato(`Relato enviado para a regional ${nome}. Obrigado! Ele conta no risco por ${VALIDADE_RELATO_HORAS} h.`)
    setTimeout(() => setAvisoRelato(''), 8000)
    setSelecionadaId(relato.regiaoId)
    await atualizar() // a API já recalculou o score: busca o número novo na hora
  }

  return (
    <main className="dashboard">
      <header className="cabecalho">
        <div className="marca">
          <img className="marca-logo" src={logoEscudo} alt="" />
          <div>
            <h1>
              Pluvi<span>Alerta</span>-BH
            </h1>
            <p>Monitoramento de chuvas em Belo Horizonte</p>
          </div>
        </div>

        <span className={erro ? 'status status-off' : 'status'}>
          {erro ? 'Sem conexão' : `Ao vivo · ${formatarHora(ultimaAtualizacao)}`}
        </span>
      </header>

      <div className="faixa-slogan">
        <strong>Informação hoje. Mais segurança amanhã.</strong>
        <span>Belo Horizonte</span>
      </div>

      {temSimulacao && (
        <p className="aviso-simulacao" role="status">
          <strong>Modo simulação ativo.</strong> Uma tempestade fictícia foi injetada para demonstração.
          Os dados marcados como “simulação” não são reais.
        </p>
      )}

      <section className="boas-vindas com-acao">
        <div>
          <p className="sobretitulo">Central de monitoramento</p>
          <h2>Acompanhe o risco de chuva em cada regional</h2>
          <p>Score de 0 a 100 calculado com chuva, previsão, vulnerabilidade e relatos de moradores.</p>
        </div>
        <a className="botao-telegram" href={LINK_TELEGRAM} target="_blank" rel="noreferrer">
          <IconeTelegram />
          Receber alertas no Telegram
        </a>
      </section>

      <section className="indicadores">
        <article className="cartao indicador">
          <span>Regionais em alerta</span>
          <strong>
            {carregando ? '--' : emAlertaAgora.length}
            <small> de {regioes.length || 9}</small>
          </strong>
          <small>Nível Laranja ou Vermelho</small>
        </article>

        <article className="cartao indicador">
          <span>Maior chuva em 6 h</span>
          <strong>
            {maisChuva ? formatarMm(maisChuva.chuvaAcum6hMm) : '--'} <small>mm</small>
          </strong>
          <small>{maisChuva ? maisChuva.nome : 'Aguardando dados'}</small>
        </article>

        <article className="cartao indicador">
          <span>Relatos de moradores</span>
          <strong>{carregando ? '--' : relatos.length}</strong>
          <small>Ativos na última {VALIDADE_RELATO_HORAS === 1 ? 'hora' : `${VALIDADE_RELATO_HORAS} h`}</small>
        </article>
      </section>

      {erro && (
        <p className="mensagem-erro" role="alert">
          {erro}
          <button type="button" onClick={atualizar}>
            Tentar de novo
          </button>
        </p>
      )}

      {mostrarDemo && <PainelDemo regioes={regioes} aoMudar={demoMudou} aoFechar={fecharDemo} />}

      <section className="secao-mapa">
        <div className="cartao mapa-cartao">
          <div className="mapa-barra">
            <div>
              <strong>Mapa de risco</strong>
              <span>Clique numa regional para ver o detalhe</span>
            </div>
            {!modoRelato && (
              <button type="button" className="btn-relatar" onClick={() => setModoRelato(true)}>
                📍 Relatar um problema
              </button>
            )}
          </div>

          {modoRelato && (
            <FormRelato ponto={pontoRelato} aoEnviar={relatoEnviado} aoCancelar={fecharRelato} />
          )}
          {avisoRelato && (
            <p className="aviso-relato" role="status">
              ✅ {avisoRelato}
            </p>
          )}

          {carregando ? (
            <div className="esqueleto mapa" />
          ) : (
            <MapaRegionais
              regioes={regioes}
              relatos={relatos}
              selecionadaId={idPainel}
              aoSelecionar={setSelecionadaId}
              modoRelato={modoRelato}
              pontoRelato={pontoRelato}
              aoMarcarPonto={(lat, lng) => setPontoRelato([lat, lng])}
            />
          )}
          <Legenda />
        </div>
        {regiaoPainel ? (
          <PainelRegional regiao={regiaoPainel} historico={historico} />
        ) : (
          <div className="cartao painel esqueleto" />
        )}
      </section>

      <section className="secao-regionais">
        <div className="titulo-secao">
          <div>
            <h2>Regionais de Belo Horizonte</h2>
            <p>Da maior para a menor pontuação. Clique para ver o detalhe.</p>
          </div>
          <span className="contador">{regioes.length} regiões</span>
        </div>

        {carregando && (
          <div className="grade-regionais" aria-busy="true" aria-label="Carregando regionais">
            {[1, 2, 3].map((n) => (
              <div className="esqueleto" key={n} />
            ))}
          </div>
        )}

        {!carregando && !erro && regioes.length === 0 && (
          <p className="vazio">Nenhuma regional foi retornada pela API.</p>
        )}

        {!carregando && regioes.length > 0 && (
          <div className="grade-regionais">
            {ordenadas.map((r) => (
              <button
                type="button"
                className={`cartao regional ${classeNivel[r.nivel]} ${r.id === idPainel ? 'selecionada' : ''}`}
                key={r.id}
                onClick={() => setSelecionadaId(r.id)}
              >
                <div className="icone-regional">{r.score}</div>
                <div>
                  <h3>{r.nome}</h3>
                  <p>
                    {formatarMm(r.chuvaAcum6hMm)} mm em 6 h · {formatarMm(r.previsao3hMm)} mm previstos
                  </p>
                </div>
                <span className="risco">
                  {r.nivel} · {descricaoNivel[r.nivel]}
                </span>
                <div className="medidor">
                  <span style={{ width: `${r.score}%` }} />
                </div>
              </button>
            ))}
          </div>
        )}
      </section>

      <section className="cartao area-alertas">
        <div className="icone-alerta">!</div>
        <div className="alertas-conteudo">
          <h2>Central de alertas</h2>
          {emAlertaAgora.length === 0 ? (
            <p>Nenhuma regional em Laranja ou Vermelho agora. Inscreva-se no Telegram para ser avisado quando o risco subir.</p>
          ) : (
            <ul className="lista-alertas">
              {emAlertaAgora.map((r) => (
                <li key={r.id} className={classeNivel[r.nivel]}>
                  <strong>
                    {r.nome}: {r.nivel} ({r.score})
                  </strong>
                  <span>
                    {formatarMm(r.chuvaAcum6hMm)} mm em 6 h, {formatarMm(r.previsao3hMm)} mm previstos,{' '}
                    {r.relatosAtivos} relato(s)
                    {r.fonte === 'simulacao' && ' · simulação'}
                  </span>
                </li>
              ))}
            </ul>
          )}
        </div>
        <a className="qr-telegram" href={LINK_TELEGRAM} target="_blank" rel="noreferrer">
          <img src={qrTelegram} alt="QR code para abrir o bot @PluviAlerta_bot no Telegram" width={112} height={112} />
          <span>
            <IconeTelegram />
            Aponte a câmera e receba os alertas
            <strong>@PluviAlerta_bot</strong>
          </span>
        </a>
      </section>

      <footer>
        <img className="rodape-logo" src={logoEscudo} alt="" />
        <p className="rodape-aviso">
          Estimativa de um projeto acadêmico, não é alerta oficial. Em emergência, ligue <strong>199</strong> (Defesa Civil).
        </p>
        <p>
          Dados meteorológicos:{' '}
          <a href="https://open-meteo.com/" target="_blank" rel="noreferrer">Open-Meteo</a> (CC BY 4.0), dado de modelo,
          não de pluviômetro · Mapa ©{' '}
          <a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noreferrer">OpenStreetMap</a>
        </p>
        <p>
          PluviAlerta-BH · Sistemas de Informação · UniBH · ODS 11 e 13 ·{' '}
          <a href="#demo">Área de demonstração</a>
        </p>
      </footer>
    </main>
  )
}

function Legenda() {
  return (
    <ul className="legenda">
      <li className="risco-verde">Verde 0–29</li>
      <li className="risco-amarelo">Amarelo 30–54</li>
      <li className="risco-laranja">Laranja 55–74</li>
      <li className="risco-vermelho">Vermelho 75–100</li>
      <li className="legenda-relato">Relato de morador</li>
    </ul>
  )
}

function IconeTelegram() {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true" width="18" height="18" fill="currentColor">
      <path d="M21.9 4.3 18.7 19.4c-.2 1-.9 1.3-1.7.8l-4.8-3.5-2.3 2.2c-.3.3-.5.5-1 .5l.3-4.9 8.9-8c.4-.3-.1-.5-.6-.2l-11 6.9L1.8 11.8c-1-.3-1-1 .2-1.5L20.6 3.2c.9-.3 1.6.2 1.3 1.1Z" />
    </svg>
  )
}

export default App
