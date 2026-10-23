# PluviAlerta BH: front

Painel em React, TypeScript e Vite, com mapa (react-leaflet) do risco de chuva nas 9 regionais de Belo Horizonte. Ele consome a API em [`../PluviAlertaApi`](../PluviAlertaApi).

> Estimativa de um projeto acadêmico, não é alerta oficial. Em emergência, ligue **199** (Defesa Civil).

## Como rodar

Primeiro suba a API, seguindo o README dela. Depois:

```bash
npm install
npm run dev
```

Abra http://localhost:5173.

O front chama `/api/...`, e o Vite repassa essas chamadas para a API ([`vite.config.ts`](vite.config.ts)). Por padrão, ele procura a API em `http://localhost:5261`, que é a porta do `dotnet run`. Se a API estiver no Docker, crie um arquivo `.env.local` com:

```
VITE_API_URL=http://localhost:8080
```

## O que tem na tela

- **Mapa** com as 9 regionais coloridas pelo nível de risco e os relatos de moradores ativos.
- **"Por que esse score?":** quanto cada fator pesou (chuva, previsão, vulnerabilidade e relatos) e o gráfico das últimas 24 h.
- **Relatar um problema:** botão "📍 Relatar um problema", um clique no mapa, a escolha do tipo e o envio. O score da regional é recalculado na hora.
- **Cards das regionais**, ordenados pelo score.
- **Central de alertas**, com as regionais em Laranja ou Vermelho, e o link para o bot **@PluviAlerta_bot** no Telegram.
- **Aviso de modo simulação** quando a demo injeta uma tempestade fictícia.
- **Atualização automática** a cada 1 minuto.

## Arquivos

| Arquivo | Para que serve |
|---|---|
| `src/api.ts` | Tipos do contrato da API e funções de busca |
| `src/risco.ts` | Cores e textos por nível, e o detalhamento do score (mesma fórmula da API, só para exibição) |
| `src/MapaRegionais.tsx` | Mapa (Leaflet + OpenStreetMap) |
| `src/PainelRegional.tsx` | Painel "Por que esse score?" e o gráfico das 24 h |
| `src/FormRelato.tsx` | Formulário de relato (clique no mapa) |
| `src/App.css` | Design original do painel |
| `src/integracao.css` | Estilos do mapa, dos níveis de risco e dos alertas |

## Créditos

- Dados meteorológicos: [Open-Meteo](https://open-meteo.com/), sob a licença CC BY 4.0.
- Mapa: © [OpenStreetMap](https://www.openstreetmap.org/copyright).
