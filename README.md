# PluviAlerta BH

Sistema de alerta de chuva para Belo Horizonte, desenvolvido como trabalho acadêmico de Sistemas de Informação (UniBH).

O PluviAlerta BH acompanha a chuva na cidade, calcula um **nível de risco** (🟢 Verde, 🟡 Amarelo, 🟠 Laranja, 🔴 Vermelho) para cada uma das **9 regionais** e avisa os moradores inscritos pelo **Telegram**. O score é **explicável**: o painel mostra quanto veio da chuva acumulada, da previsão, da vulnerabilidade da regional e dos relatos de moradores.

> **Estimativa de um projeto acadêmico, não é alerta oficial.** Ela complementa a Defesa Civil, não a substitui. Em emergência, ligue **199 (Defesa Civil)**.
> Os dados de chuva vêm de modelo meteorológico ([Open-Meteo](https://open-meteo.com/)), não de pluviômetro local.

## O que o sistema faz

- **Painel com mapa:** um círculo por regional, com score de 0 a 100, atualizado a cada 15 minutos.
- **Score explicável:** `Score = 0,40 × chuva + 0,30 × previsão + 0,20 × vulnerabilidade + 0,10 × relatos`.
- **Relatos de moradores:** qualquer pessoa marca um alagamento ou deslizamento pelo site ou pelo Telegram, sem precisar de conta. O relato expira em 1 h e entra no cálculo na hora.
- **Alertas no Telegram:** o bot [@PluviAlerta_bot](https://t.me/PluviAlerta_bot) avisa quando o nível sobe para Laranja ou Vermelho e quando o risco é reduzido.
- **Modo simulação:** uma tempestade fictícia, protegida por login de admin, para demonstrar o ciclo completo sem depender do tempo real.

## Estrutura do repositório

```
PluviAlerta-BH/
  PluviAlertaApi/       # backend em .NET 9 + MySQL (API, coleta, alertas e bot)
  pluvi-alerta-front/   # frontend em React (Vite)
```

| Pasta | O que tem | Documentação |
|---|---|---|
| [`PluviAlertaApi`](PluviAlertaApi) | API REST, worker de coleta do Open-Meteo, motor de risco, bot do Telegram | [README da API](PluviAlertaApi/README.md) |
| [`pluvi-alerta-front`](pluvi-alerta-front) | Painel com mapa, relatos e área de demonstração | [README do front](pluvi-alerta-front/README.md) |

## Tecnologias

- **Backend:** .NET 9, ASP.NET Core, EF Core 9 (Pomelo), MySQL 8.4, JWT, xUnit
- **Frontend:** React, Vite
- **Integrações:** Open-Meteo (dados meteorológicos) e Telegram Bot API (alertas)
- **Infraestrutura:** Docker e Docker Compose

## Como rodar

### Pré-requisitos

- [.NET SDK 9](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker](https://docs.docker.com/get-docker/) com Docker Compose
- [Node.js](https://nodejs.org/) e npm, para o frontend

### 1. Backend

```bash
cd PluviAlertaApi
cp .env.example .env      # preencha as senhas e segredos no .env
docker compose up --build
```

- API e Swagger: http://localhost:8080
- Health check: http://localhost:8080/health

Para rodar a API fora do Docker (com o MySQL no Docker), veja a seção *Como rodar sem Docker* no [README da API](PluviAlertaApi/README.md).

### 2. Frontend

```bash
cd pluvi-alerta-front
npm install
npm run dev
```

O painel abre em http://localhost:5173.

### 3. Testes

```bash
cd PluviAlertaApi
dotnet test
```

## Demonstração

O roteiro completo da apresentação (preparação, passo a passo, o que falar e o que fazer se algo der errado) está em [`ROTEIRO-DEMO.md`](ROTEIRO-DEMO.md).

Resumo do fio da história: **chove forte numa regional → o score sobe e mostra o porquê → o morador é avisado no Telegram → a chuva passa e chega o aviso de risco reduzido.**

Para entrar na área de demonstração, abra http://localhost:5173/#demo e use o `ADMIN_USUARIO` e a `ADMIN_SENHA` do `.env`.

## Limites conhecidos

- O dado vem de **modelo meteorológico**, com grade de cerca de 8 km. As 9 regionais caem em 6 células, então algumas recebem a mesma chuva.
- As coordenadas das regionais são aproximadas e a vulnerabilidade (50 para todas) é um valor provisório.
- Pesos e cortes do score são provisórios e serão calibrados com um dia real de chuva.
- O score é por regional, e não por bairro, para não passar uma precisão que o dado não tem.

Nenhum desses valores é dado oficial.

## Privacidade (LGPD)

- O bot guarda apenas o **ID do chat** e a **regional** escolhida. O comando `/sair` apaga o cadastro.
- O relato **não guarda quem o enviou**.
- O cidadão usa o sistema sem criar conta. O login existe só para proteger a simulação.

## Equipe

- Matheus Honorato Leite Teixeira
- Daniel Andrade Lopes
- Sabrina Custódio Brito
- Carlos Gabriel Pereira de Sousa
- Victor Guimarães Rodrigues
- Frederico Bastos
- Vitor Emmanoel Guedes Fonseca

Disciplina: Usabilidade, desenvolvimento web, mobile e jogos. Trabalho acadêmico de Sistemas de Informação, UniBH.

## Créditos

Dados meteorológicos: [Open-Meteo](https://open-meteo.com/), sob a licença [CC-BY 4.0](https://creativecommons.org/licenses/by/4.0/) e para uso não comercial.
