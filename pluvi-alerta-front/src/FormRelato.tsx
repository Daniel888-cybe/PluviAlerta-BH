import { useState } from 'react'
import { criarRelato, type Relato, type TipoRelato } from './api'

type Props = {
  ponto: [number, number] | null
  aoEnviar: (relato: Relato) => void
  aoCancelar: () => void
}

const TIPOS: { valor: TipoRelato; rotulo: string }[] = [
  { valor: 'Alagamento', rotulo: '🌊 Alagamento' },
  { valor: 'Deslizamento', rotulo: '⛰️ Deslizamento' },
  { valor: 'Outro', rotulo: 'Outro' },
]

// Formulário que aparece em cima do mapa no modo "relatar um problema".
export function FormRelato({ ponto, aoEnviar, aoCancelar }: Props) {
  const [tipo, setTipo] = useState<TipoRelato>('Alagamento')
  const [descricao, setDescricao] = useState('')
  const [enviando, setEnviando] = useState(false)
  const [erro, setErro] = useState('')

  async function enviar() {
    if (!ponto) return
    setEnviando(true)
    setErro('')
    try {
      const relato = await criarRelato({
        latitude: ponto[0],
        longitude: ponto[1],
        tipo,
        descricao: descricao.trim() || undefined,
      })
      aoEnviar(relato)
    } catch (e) {
      setErro(e instanceof Error ? e.message : 'Não foi possível enviar o relato.')
    } finally {
      setEnviando(false)
    }
  }

  return (
    <div className="form-relato">
      {!ponto ? (
        <p className="form-relato-dica">
          <strong>1.</strong> Clique no mapa onde está o problema.
        </p>
      ) : (
        <>
          <p className="form-relato-dica">
            <strong>2.</strong> O que está acontecendo nesse ponto?
          </p>
          <div className="form-relato-tipos" role="radiogroup" aria-label="Tipo do relato">
            {TIPOS.map((t) => (
              <button
                key={t.valor}
                type="button"
                role="radio"
                aria-checked={tipo === t.valor}
                className={tipo === t.valor ? 'tipo ativo' : 'tipo'}
                onClick={() => setTipo(t.valor)}
              >
                {t.rotulo}
              </button>
            ))}
          </div>
          <textarea
            value={descricao}
            onChange={(e) => setDescricao(e.target.value)}
            maxLength={280}
            rows={2}
            placeholder="Descrição (opcional): ex. rua alagada na altura do nº 200"
          />
        </>
      )}

      {erro && <p className="form-relato-erro">{erro}</p>}

      <div className="form-relato-acoes">
        <button type="button" className="btn-secundario" onClick={aoCancelar}>
          Cancelar
        </button>
        <button type="button" className="btn-enviar" disabled={!ponto || enviando} onClick={enviar}>
          {enviando ? 'Enviando…' : 'Enviar relato'}
        </button>
      </div>
      <small className="form-relato-nota">O relato aparece no mapa e conta no risco da regional por 3 h.</small>
    </div>
  )
}
