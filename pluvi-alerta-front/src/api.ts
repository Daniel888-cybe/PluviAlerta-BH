// Tipos espelhando o contrato da API (PluviAlertaApi/README.md e Swagger).
// Se a API mudar um campo, o TypeScript acusa aqui.

export type Nivel = 'Verde' | 'Amarelo' | 'Laranja' | 'Vermelho'
export type Fonte = 'open-meteo' | 'simulacao' | 'sem-dados'

export type RegiaoAtual = {
  id: number
  nome: string
  latitude: number
  longitude: number
  vulnerabilidade: number
  score: number
  nivel: Nivel
  chuvaAcum6hMm: number
  previsao3hMm: number
  relatosAtivos: number
  atualizadoEm: string | null // UTC
  fonte: Fonte
}

export type PontoHistorico = {
  momento: string // UTC
  score: number
  nivel: Nivel
  chuvaAcum6hMm: number
  previsao3hMm: number
}

export type HistoricoRegiao = {
  regiaoId: number
  nome: string
  fonte: Fonte
  pontos: PontoHistorico[]
}

export type Relato = {
  id: number
  latitude: number
  longitude: number
  regiaoId: number
  tipo: string
  descricao: string | null
  criadoEm: string
  expiraEm: string
}

async function getJson<T>(url: string): Promise<T> {
  const resposta = await fetch(url)
  if (!resposta.ok) throw new Error(`${url} respondeu ${resposta.status}`)
  return resposta.json() as Promise<T>
}

// Caminhos relativos: o proxy do Vite (vite.config.ts) leva até a API.
export const buscarRegioes = () => getJson<RegiaoAtual[]>('/api/regioes')
export const buscarHistorico = (id: number) => getJson<HistoricoRegiao>(`/api/regioes/${id}/historico`)
export const buscarRelatosAtivos = () => getJson<Relato[]>('/api/relatos?ativos=true')

export type TipoRelato = 'Alagamento' | 'Deslizamento' | 'Outro'

export type NovoRelato = {
  latitude: number
  longitude: number
  tipo: TipoRelato
  descricao?: string
}

// POST /api/relatos. Traduz os erros da API (fora de BH, limite por IP) em mensagem para a tela.
export async function criarRelato(novo: NovoRelato): Promise<Relato> {
  const resposta = await fetch('/api/relatos', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(novo),
  })

  if (resposta.status === 429) throw new Error('Muitos relatos seguidos. Tente de novo em alguns minutos.')
  if (resposta.status === 400) {
    const problema = (await resposta.json().catch(() => null)) as { errors?: Record<string, string[]> } | null
    const primeiro = problema?.errors ? Object.values(problema.errors).flat()[0] : undefined
    throw new Error(primeiro ?? 'Dados do relato inválidos.')
  }
  if (!resposta.ok) throw new Error('Não foi possível enviar o relato. Tente de novo.')
  return resposta.json() as Promise<Relato>
}

export const LINK_TELEGRAM = 'https://t.me/PluviAlerta_bot'
