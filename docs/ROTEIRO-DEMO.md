# Roteiro da demo: PluviAlerta BH

A demo dura de 2 a 3 minutos e cabe no pitch de 5. O fio da história é: **chove forte numa regional → o score sobe e mostra o porquê → o morador é avisado no Telegram → a chuva passa e chega o aviso de risco reduzido.**

## Antes de apresentar (30 min antes)

Abra 3 terminais na pasta `PluviAlerta-BH`.

| Terminal | Comando |
|---|---|
| 1. Banco | `cd PluviAlertaApi` e depois `docker compose up -d db` |
| 2. API e bot | `cd PluviAlertaApi` e depois `dotnet run --project src/PluviAlerta.Api` |
| 3. Site | `cd pluvi-alerta-front` e depois `npm run dev` |

Depois confira:
- [ ] http://localhost:5173 abre o painel e mostra "Ao vivo" no topo.
- [ ] No terminal da API aparecem `Bot do Telegram ativo: @PluviAlerta_bot` e `Coleta ok`.
- [ ] Em http://localhost:5173/#demo, você entra com `admin` e a `ADMIN_SENHA` do `.env`.
- [ ] **Encerrar simulações** e **🧹 Apagar relatos** foram usados, para começar com o mapa limpo.
- [ ] O seu celular está inscrito na regional da demo (`/start` no @PluviAlerta_bot, depois **Leste**).
- [ ] Há internet: o fundo do mapa e o Telegram precisam dela. Leve um 4G de reserva.
- [ ] Só **uma** API está rodando com o token do bot. Com duas, o Telegram recusa a segunda.

## A demo, passo a passo

| # | O que fazer | O que falar |
|---|---|---|
| 1 | Mostrar o painel com o mapa. | "Cada círculo é uma regional de BH. O score vai de 0 a 100 e é recalculado a cada 15 min com dados do Open-Meteo." |
| 2 | Clicar numa regional e mostrar o "Por que esse score?". | "O diferencial é que o score é explicável: chuva das últimas 6 h, previsão das próximas 3 h, vulnerabilidade da regional e relatos de moradores." |
| 3 | Mostrar o QR code (slide ou Central de alertas). | "Quem quiser pode se inscrever agora: aponte a câmera, toque em Iniciar e escolha a **Leste**." |
| 4 | Clicar em **📍 Relatar um problema**, depois num ponto da Leste, e enviar. | "O morador também participa: marca um alagamento no mapa e o relato entra no cálculo na hora." |
| 5 | Abrir **Área de demonstração**, escolher **Leste** e clicar em **⛈️ Simular tempestade**. | "Agora simulo uma tempestade forte: 60 mm em 6 h e 40 mm previstos." |
| 6 | Mostrar o mapa: a Leste fica **vermelha, com score 90**. Mostrar o celular com o alerta. | "O risco subiu para Vermelho e quem está inscrito na Leste recebeu o alerta no Telegram na mesma hora." |
| 7 | Clicar em **Encerrar simulações**. Mostrar o celular com 🟢 "Risco reduzido". | "Quando a chuva passa, o sistema avisa que o risco baixou. O ciclo fica completo." |
| 8 | Apontar o rodapé. | "É uma estimativa acadêmica e complementa a Defesa Civil, não a substitui. Em emergência, ligue 199." |

## Se algo der errado

| Problema | Saída |
|---|---|
| O alerta não chegou no celular | Confira se você está inscrito na **mesma** regional da simulação (`/start`). O alerta só sai quando o nível **sobe**. Se a regional já estava Vermelha, clique em **Encerrar simulações** e simule de novo. |
| O mapa ficou cinza | Falta internet para o fundo do OpenStreetMap. Os círculos, o score e a simulação continuam funcionando. |
| "Sessão expirada" na área de demonstração | O login vale 2 h. Entre de novo. |
| O painel mostra "Sem conexão" | A API caiu. Rode `dotnet run` de novo no terminal 2. |
| O bot não responde | Veja se há outra API ligada com o mesmo token, numa outra máquina ou terminal, e deixe só uma. |

## Perguntas prováveis da banca

| Pergunta | Resposta curta |
|---|---|
| O dado é preciso? | É dado de **modelo meteorológico**, com grade de cerca de 8 km, e não de pluviômetro. Por isso o app diz que é uma estimativa. |
| De onde vêm os pesos? | São provisórios e ficam em constantes no `MotorDeRisco`, para calibrar com um dia real de chuva. |
| E se alguém mandar relato falso? | O relato pesa no máximo 10% do score, expira em 1 h e tem limite de 5 a cada 10 min por pessoa. |
| Por que regional e não bairro? | Com uma grade de 8 km, um score por bairro teria precisão aparente, não real. Fica como evolução, junto com pluviômetros locais. |
| Por que não precisa de cadastro? | O cidadão usa sem conta, de propósito, para não ter barreira. O login existe só para proteger a simulação. |
| E a LGPD? | O bot guarda só o ID do chat e a regional. O relato não guarda quem enviou. `/sair` apaga o cadastro. |
| Tem testes? | São 57 testes automatizados, cobrindo o motor de risco, a regra de alerta, os relatos e a soma da chuva. |

O QR code para o slide está em [`qr-pluvialerta-bot.png`](qr-pluvialerta-bot.png).
