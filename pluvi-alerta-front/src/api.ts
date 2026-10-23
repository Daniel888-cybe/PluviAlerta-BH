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

// Mesmo valor de RegrasRelato.ValidadeHoras na API (quanto tempo um relato conta no risco).
export const VALIDADE_RELATO_HORAS = 1

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

// ---------- Área de demonstração (rotas de admin, com JWT) ----------

export class SessaoExpirada extends Error {}

export async function entrarComoAdmin(usuario: string, senha: string): Promise<string> {
  const resposta = await fetch('/api/auth/token', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ usuario, senha }),
  })
  if (resposta.status === 401) throw new Error('Usuário ou senha incorretos.')
  if (resposta.status === 429) throw new Error('Muitas tentativas. Espere 1 minuto.')
  if (!resposta.ok) throw new Error('Não foi possível entrar. Verifique se a API está no ar.')
  const dados = (await resposta.json()) as { token: string }
  return dados.token
}

export type ResultadoSimulacao = {
  regiaoId: number
  nome: string
  score: number
  nivel: Nivel
  simuladoEm: string
  validaAte: string
}

export type ParametrosSimulacao = {
  regiaoId: number
  chuvaAcum6hMm: number
  previsao3hMm: number
  relatos: number
}

async function chamarAdmin(token: string, metodo: 'POST' | 'DELETE', corpo?: unknown) {
  const resposta = await fetch('/api/simulacao', {
    method: metodo,
    headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
    body: corpo ? JSON.stringify(corpo) : undefined,
  })
  if (resposta.status === 401) throw new SessaoExpirada('A sessão expirou. Entre de novo.')
  if (!resposta.ok) throw new Error(`A API respondeu ${resposta.status}.`)
  return resposta.json()
}

export const simularTempestade = (token: string, p: ParametrosSimulacao) =>
  chamarAdmin(token, 'POST', p) as Promise<ResultadoSimulacao>

export const encerrarSimulacoes = (token: string) =>
  chamarAdmin(token, 'DELETE') as Promise<{ scoresSimuladosApagados: number }>

// Apaga todos os relatos (para apresentar com o mapa limpo depois dos ensaios).
export async function apagarRelatos(token: string): Promise<{ relatosApagados: number }> {
  const resposta = await fetch('/api/relatos', {
    method: 'DELETE',
    headers: { Authorization: `Bearer ${token}` },
  })
  if (resposta.status === 401) throw new SessaoExpirada('A sessão expirou. Entre de novo.')
  if (!resposta.ok) throw new Error(`A API respondeu ${resposta.status}.`)
  return resposta.json() as Promise<{ relatosApagados: number }>
}

export const LINK_TELEGRAM = 'https://t.me/PluviAlerta_bot'
