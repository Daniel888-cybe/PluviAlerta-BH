import type { Nivel, RegiaoAtual } from './api'

// Cores e textos por nível (as classes CSS ficam em App.css).
export const classeNivel: Record<Nivel, string> = {
  Verde: 'risco-verde',
  Amarelo: 'risco-amarelo',
  Laranja: 'risco-laranja',
  Vermelho: 'risco-vermelho',
}

export const corNivel: Record<Nivel, string> = {
  Verde: '#059669',
  Amarelo: '#d4a20a',
  Laranja: '#ff560b',
  Vermelho: '#dc2626',
}

export const descricaoNivel: Record<Nivel, string> = {
  Verde: 'Risco baixo',
  Amarelo: 'Atenção',
  Laranja: 'Risco alto',
  Vermelho: 'Risco muito alto',
}

export const emAlerta = (r: RegiaoAtual) => r.nivel === 'Laranja' || r.nivel === 'Vermelho'

// Mesma fórmula do MotorDeRisco da API (PluviAlerta.Domain/Risco/MotorDeRisco.cs),
// só para MOSTRAR quanto cada fator pesou. Quem calcula o score oficial é a API.
const limitar = (v: number) => Math.min(100, Math.max(0, v))

export type Fator = { nome: string; detalhe: string; pontos: number; maximo: number }

export function fatoresDoScore(r: RegiaoAtual): Fator[] {
  return [
    {
      nome: 'Chuva nas últimas 6 h',
      detalhe: `${formatarMm(r.chuvaAcum6hMm)} mm de 60 mm`,
      pontos: 0.4 * limitar((r.chuvaAcum6hMm / 60) * 100),
      maximo: 40,
    },
    {
      nome: 'Previsão para 3 h',
      detalhe: `${formatarMm(r.previsao3hMm)} mm de 40 mm`,
      pontos: 0.3 * limitar((r.previsao3hMm / 40) * 100),
      maximo: 30,
    },
    {
      nome: 'Vulnerabilidade da regional',
      detalhe: `${r.vulnerabilidade} de 100`,
      pontos: 0.2 * limitar(r.vulnerabilidade),
      maximo: 20,
    },
    {
      nome: 'Relatos de moradores',
      detalhe: `${r.relatosAtivos} ativo(s), 25 pontos cada`,
      pontos: 0.1 * limitar(r.relatosAtivos * 25),
      maximo: 10,
    },
  ]
}

export const formatarMm = (mm: number) => mm.toLocaleString('pt-BR', { maximumFractionDigits: 1 })

export const formatarHora = (iso: string | null) =>
  iso ? new Date(iso).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' }) : '--:--'
