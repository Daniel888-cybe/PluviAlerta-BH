import type { HistoricoRegiao, RegiaoAtual } from './api'
import { classeNivel, corNivel, descricaoNivel, fatoresDoScore, formatarHora } from './risco'

type Props = {
  regiao: RegiaoAtual
  historico: HistoricoRegiao | null
}

// "Por que esse score?": mostra quanto cada fator pesou. É o diferencial do projeto (score explicável).
export function PainelRegional({ regiao, historico }: Props) {
  const fatores = fatoresDoScore(regiao)

  return (
    <aside className={`cartao painel ${classeNivel[regiao.nivel]}`}>
      <div className="painel-topo">
        <div>
          <p className="painel-kicker">Regional selecionada</p>
          <h2>{regiao.nome}</h2>
        </div>
        <div className="painel-score">
          <strong>{regiao.score}</strong>
          <span className="risco">{regiao.nivel}</span>
        </div>
      </div>
      <p className="painel-desc">
        {descricaoNivel[regiao.nivel]} · atualizado às {formatarHora(regiao.atualizadoEm)}
        {regiao.fonte === 'simulacao' && <span className="selo-simulacao">simulação</span>}
      </p>

      <h3 className="painel-subtitulo">Por que esse score?</h3>
      <ul className="fatores">
        {fatores.map((f) => (
          <li key={f.nome}>
            <div className="fator-linha">
              <span>{f.nome}</span>
              <strong>
                {f.pontos.toLocaleString('pt-BR', { maximumFractionDigits: 1 })}
                <small> / {f.maximo}</small>
              </strong>
            </div>
            <div className="fator-barra">
              <span style={{ width: `${(f.pontos / f.maximo) * 100}%` }} />
            </div>
            <small className="fator-detalhe">{f.detalhe}</small>
          </li>
        ))}
      </ul>

      <h3 className="painel-subtitulo">Últimas 24 h</h3>
      <GraficoScore historico={historico} />
    </aside>
  )
}

// Gráfico de linha simples em SVG (sem biblioteca): score ao longo do tempo, com as faixas de nível ao fundo.
function GraficoScore({ historico }: { historico: HistoricoRegiao | null }) {
  const pontos = historico?.pontos ?? []
  if (pontos.length < 2) {
    return <p className="grafico-vazio">O gráfico aparece depois de algumas coletas (uma a cada 15 min).</p>
  }

  const L = 320
  const A = 110
  const x = (i: number) => (i / (pontos.length - 1)) * L
  const y = (score: number) => A - (score / 100) * A
  const linha = pontos.map((p, i) => `${x(i).toFixed(1)},${y(p.score).toFixed(1)}`).join(' ')
  const ultimo = pontos[pontos.length - 1]

  // Faixas de fundo nos cortes do MotorDeRisco: 30, 55, 75.
  const faixas = [
    { de: 0, ate: 30, cor: corNivel.Verde },
    { de: 30, ate: 55, cor: corNivel.Amarelo },
    { de: 55, ate: 75, cor: corNivel.Laranja },
    { de: 75, ate: 100, cor: corNivel.Vermelho },
  ]

  return (
    <figure className="grafico">
      <svg viewBox={`0 0 ${L} ${A}`} preserveAspectRatio="none" role="img"
        aria-label={`Score de ${pontos[0].score} a ${ultimo.score} nas últimas 24 horas`}>
        {faixas.map((f) => (
          <rect key={f.de} x={0} width={L} y={y(f.ate)} height={y(f.de) - y(f.ate)} fill={f.cor} opacity={0.08} />
        ))}
        <polyline points={linha} fill="none" stroke="#066dcc" strokeWidth={2.5}
          strokeLinejoin="round" vectorEffect="non-scaling-stroke" />
      </svg>
      <figcaption>
        <span>{formatarHora(pontos[0].momento)}</span>
        <span>agora</span>
      </figcaption>
    </figure>
  )
}
