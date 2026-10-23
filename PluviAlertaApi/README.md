# PluviAlerta BH: API

Backend do **PluviAlerta BH**, trabalho acadêmico de Sistemas de Informação (UniBH). Ele acompanha a chuva em Belo Horizonte, calcula um nível de risco (Verde, Amarelo, Laranja, Vermelho) para cada uma das 9 regionais e avisa os moradores inscritos pelo Telegram.

> **Estimativa de um projeto acadêmico, não é alerta oficial.** Em emergência, ligue **199 (Defesa Civil)**.
> Os dados de chuva vêm de modelo meteorológico (Open-Meteo), não de pluviômetro local.

## Estrutura

```
PluviAlertaApi/
  src/
    PluviAlerta.Domain/          # entidades e MotorDeRisco (sem dependências externas)
    PluviAlerta.Infrastructure/  # EF Core, cliente Open-Meteo, Telegram
    PluviAlerta.Api/             # controllers, worker de coleta, Program.cs
  tests/
    PluviAlerta.Tests/           # xUnit
  docker-compose.yml             # api + MySQL
  .env.example                   # variáveis necessárias, sem valores reais
```

O front em React fica em [`../pluvi-alerta-front`](../pluvi-alerta-front).

## Pré-requisitos

- [.NET SDK 9](https://dotnet.microsoft.com/download/dotnet/9.0) (o `global.json` fixa a versão 9)
- [Docker](https://docs.docker.com/get-docker/) com Docker Compose

## Como subir com Docker

```bash
cp .env.example .env      # depois preencha as senhas no .env
docker compose up --build
```

- API e Swagger: http://localhost:8080
- Health check: http://localhost:8080/health

Ao subir, a API aplica as migrations sozinha. O banco nasce com as tabelas e as 9 regionais.

## Como rodar sem Docker

A API roda na sua máquina e o MySQL continua no Docker:

```bash
docker compose up -d db
dotnet user-secrets set "ConnectionStrings:PluviAlerta" "Server=localhost;Port=3306;Database=pluvialerta;User=pluvialerta;Password=<MYSQL_PASSWORD do .env>" --project src/PluviAlerta.Api
dotnet run --project src/PluviAlerta.Api
```

A API sobe em http://localhost:5261. O user-secrets guarda a senha fora do repositório e só precisa ser configurado uma vez por máquina.

## Banco de dados e migrations

MySQL 8.4 com EF Core 9 e o provedor Pomelo. O `dotnet-ef` está fixado na versão 9 como ferramenta local:

```bash
dotnet tool restore                      # uma vez por máquina
dotnet ef migrations add <Nome> --project src/PluviAlerta.Infrastructure --startup-project src/PluviAlerta.Api --output-dir Dados/Migrations
dotnet ef database update --project src/PluviAlerta.Infrastructure --startup-project src/PluviAlerta.Api
```

Para apagar o banco e recomeçar: `docker compose down -v`.

**Dados provisórios no seed** ([`SeedRegioes.cs`](src/PluviAlerta.Infrastructure/Dados/SeedRegioes.cs)):
- As coordenadas de cada regional são **aproximadas**, um ponto central estimado.
- A `Vulnerabilidade` é **50 para todas**, um valor provisório até o grupo definir os números.

Nenhum desses valores é dado oficial.

## Endpoints

Com a API rodando em modo Development, a raiz (`/`) abre o **Swagger** em `/swagger`.

| Rota | Situação |
|---|---|
| `GET /api/regioes` | ✅ Real: último score de cada regional, com as entradas do cálculo |
| `GET /api/regioes/{id}/historico` | ✅ Real: um ponto por coleta nas últimas 24 h, do mais antigo ao mais recente |
| `GET /api/relatos?ativos=true` | ✅ Real (banco). `ativos=false` traz todos das últimas 24 h |
| `POST /api/relatos` | ✅ Real (banco), com as regras abaixo |
| `POST /api/auth/token` | ✅ Login do admin, devolve um JWT válido por 2 h. Limite de 5 tentativas por minuto por IP |
| `POST /api/simulacao` | ✅ 🔒 Tempestade fictícia numa regional (veja abaixo) |
| `DELETE /api/simulacao` | ✅ 🔒 Apaga os dados simulados e volta ao dado real |
| `DELETE /api/relatos` | ✅ 🔒 Apaga todos os relatos (para apresentar com o mapa limpo) e recalcula as regionais |

**Regras dos relatos** ([`RegrasRelato.cs`](src/PluviAlerta.Domain/Relatos/RegrasRelato.cs)):
- O corpo é `{ "latitude", "longitude", "tipo", "descricao" }`. O `tipo` aceita `Alagamento`, `Deslizamento` ou `Outro`, uma lista provisória. A `descricao` é opcional, com até 280 caracteres.
- A coordenada precisa estar dentro de um retângulo **aproximado** em volta de BH. Esse retângulo não é o limite oficial do município.
- A regional é a do **centro mais próximo**, até termos os polígonos das regionais.
- O relato expira em **1 h** (`RegrasRelato.ValidadeHoras`, valor provisório).
- O limite é de **5 envios a cada 10 min por IP**. Acima disso, a API responde HTTP 429.
- **O score da regional é recalculado na hora**, com a mesma chuva da última leitura, então não precisa esperar a próxima coleta. Isso só acontece se essa leitura tiver até 30 min, para não gerar alerta com dado velho. Durante uma simulação, os relatos fictícios da demo são preservados.

**Três jeitos de enviar um relato**, todos passando pelo [`ServicoDeRelatos`](src/PluviAlerta.Infrastructure/Relatos/ServicoDeRelatos.cs):
1. **Pelo site:** botão "📍 Relatar um problema" e um clique no mapa.
2. **Pelo Telegram:** `/relatar`, depois "📍 Enviar minha localização" e a escolha do tipo. O limite é de 5 a cada 10 min por chat. O relato não guarda quem enviou (LGPD).
3. **Pela API:** `POST /api/relatos`, que é o caminho dos outros dois.

Exemplo de item do `GET /api/regioes`:

```json
{
  "id": 2, "nome": "Centro-Sul", "latitude": -19.93, "longitude": -43.94,
  "vulnerabilidade": 50, "score": 68, "nivel": "Laranja",
  "chuvaAcum6hMm": 45, "previsao3hMm": 30, "relatosAtivos": 2,
  "atualizadoEm": "2026-10-22T02:03:40Z", "fonte": "open-meteo"
}
```

O campo `fonte` pode valer:
- `open-meteo` para o dado real;
- `simulacao` para a demo;
- `sem-dados` quando a regional ainda não teve nenhuma coleta. Nesse caso o `atualizadoEm` vem `null`.

As datas estão sempre em UTC (com `Z`). As entradas do cálculo vêm junto, para o front explicar o score. O CORS libera o front do Vite em `http://localhost:5173`, e outras origens podem ser adicionadas em `Cors:Origens` no `appsettings.json`.

## Modo simulação (demo)

Funciona **sem internet**. A leitura fictícia passa pelo mesmo código do dado real: `AtualizadorDeRisco` e depois `MotorDeRisco`.

**O jeito mais fácil é pelo site.** Abra `http://localhost:5173/#demo`, ou use o link "Área de demonstração" no rodapé. Entre com o `ADMIN_USUARIO` e a `ADMIN_SENHA` do `.env`, escolha a regional e clique em **⛈️ Simular tempestade**. O botão **Encerrar simulações** volta ao dado real.

**Pelo Swagger:**

1. No Swagger, chame `POST /api/auth/token` com `{"usuario": "<ADMIN_USUARIO>", "senha": "<ADMIN_SENHA>"}`, usando os valores do `.env`. Copie o `token` da resposta.
2. Clique em **Authorize** e cole o token.
3. Chame `POST /api/simulacao` com `{"regiaoId": 5}`. O padrão é 60 mm em 6 h, 40 mm de previsão e 4 relatos. Com a vulnerabilidade provisória 50, o score fica em **90 (Vermelho)**.
4. Por **30 min**, a coleta do Open-Meteo não sobrescreve essa regional.
5. Para ensaiar de novo: `DELETE /api/simulacao`.

Os 4 relatos da simulação entram só na conta do score. Eles não criam pontos no mapa.

## Alertas

A cada score novo, vindo do dado real ou da simulação, o [`ServicoDeAlertas`](src/PluviAlerta.Infrastructure/Alertas/ServicoDeAlertas.cs) aplica a [`RegraAlerta`](src/PluviAlerta.Domain/Alertas/RegraAlerta.cs):
- **avisa** quando o nível **sobe** para Laranja ou Vermelho;
- **não repete** o mesmo nível na mesma regional em menos de **2 h**.

O alerta é gravado em `AlertasEnviados` e enviado aos inscritos da regional.

**Aviso de "risco reduzido":** quando a regional sai de Laranja ou Vermelho e volta para Verde ou Amarelo, os inscritos recebem 🟢 "Risco reduzido". Esse aviso também sai ao **encerrar uma simulação**, o que fecha o ciclo da demo: alerta, redução.

- **Sem `TELEGRAM_BOT_TOKEN`:** o alerta vai só para o log da API.
- **Com token:** vai pelo Telegram, no bot **[@PluviAlerta_bot](https://t.me/PluviAlerta_bot)**.

**Bot do Telegram:**
- `/start` mostra as 9 regionais em botões. Tocar num botão inscreve a pessoa naquela regional, e dá para escolher mais de uma.
- `/relatar` mostra o botão "📍 Enviar minha localização". O bot pergunta o tipo (Alagamento, Deslizamento ou Outro) e cria o relato.
- `/sair` apaga o cadastro.
- Pela LGPD, o banco guarda só o ID do chat e a regional.
- O bot recebe as mensagens por *long polling*, ou seja, a API pergunta ao Telegram de tempos em tempos se chegou mensagem. Por isso não precisa de endereço público.
- **Só uma instância da API pode ficar ligada com o token por vez.** Com duas, o Telegram recusa a segunda.

O `DELETE /api/simulacao` também apaga os alertas simulados. Assim dá para ensaiar a demo várias vezes sem cair na regra das 2 h.

## Coleta do Open-Meteo

O [`ColetaWorker`](src/PluviAlerta.Api/Coleta/ColetaWorker.cs) roda ao subir a API e depois a cada 15 minutos:
1. Faz **uma requisição** ao Open-Meteo com as 9 coordenadas (`hourly=precipitation`, `past_hours=6`, `forecast_hours=4`). São cerca de 96 requisições por dia. O limite gratuito é de menos de 10 mil chamadas por dia.
2. Soma a chuva das últimas 6 h e a previsão das próximas 3 h. No Open-Meteo, o valor da hora T é a chuva entre T-1h e T.
3. Grava a `LeituraChuva` e o `ScoreRisco`. O score guarda as entradas usadas no cálculo.

Se o Open-Meteo falhar, o erro vai para o log, o último dado continua valendo e a API não cai. Sem dado novo, nenhum alerta é gerado.

Configuração em `appsettings.json`:
- `Coleta:Habilitada` (`false` desliga a coleta, útil para a demo offline);
- `Coleta:IntervaloMinutos`;
- `OpenMeteo:BaseUrl`.

**Limites do dado:**
- É dado de **modelo meteorológico**, e não de pluviômetro.
- A grade do modelo tem células de cerca de 8 km, então as 9 regionais caem em **6 células**:
  - Centro-Sul, Noroeste e Oeste recebem a mesma chuva;
  - Norte e Venda Nova também recebem a mesma chuva.

  Nesses casos, a diferença de score entre elas vem da vulnerabilidade e dos relatos.

## Testes

```bash
dotnet test
```

Os testes cobrem o `MotorDeRisco`, as regras dos relatos e a soma da chuva do Open-Meteo.

## Como o risco é calculado

`Score = 0,40 × chuva + 0,30 × previsão + 0,20 × vulnerabilidade + 0,10 × relatos`. Cada fator vai de 0 a 100:

| Fator | Cálculo |
|---|---|
| chuva | chuva acumulada em 6 h ÷ 60 mm × 100 |
| previsão | previsão para 3 h ÷ 40 mm × 100 |
| vulnerabilidade | valor fixo da regional |
| relatos | relatos ativos × 25 |

O score é arredondado com 0,5 sempre subindo. Níveis: **Verde** 0-29, **Amarelo** 30-54, **Laranja** 55-74, **Vermelho** 75-100.

Pesos e cortes são **provisórios**. Eles serão ajustados depois de validar com um dia real de chuva e ficam como constantes em [`MotorDeRisco.cs`](src/PluviAlerta.Domain/Risco/MotorDeRisco.cs).

## Variáveis de ambiente

| Variável (`.env`) | Para que serve |
|---|---|
| `MYSQL_DATABASE`, `MYSQL_USER`, `MYSQL_PASSWORD`, `MYSQL_ROOT_PASSWORD` | Banco MySQL do compose. A API recebe a connection string montada a partir delas. |
| `JWT_CHAVE` | Chave do JWT, com 32 ou mais caracteres aleatórios (`openssl rand -base64 48`). Sem ela a API não sobe. |
| `ADMIN_USUARIO`, `ADMIN_SENHA` | Login do admin, usado na simulação. Sem eles a API não sobe. |
| `TELEGRAM_BOT_TOKEN` | Token do bot, gerado no @BotFather. É opcional: sem ele, os alertas vão só para o log. |

Os segredos ficam no `.env`, que está no `.gitignore`. Para rodar sem Docker, os mesmos valores vão no `dotnet user-secrets`:

```bash
dotnet user-secrets set "Jwt:Chave" "<JWT_CHAVE>" --project src/PluviAlerta.Api
dotnet user-secrets set "Admin:Usuario" "<ADMIN_USUARIO>" --project src/PluviAlerta.Api
dotnet user-secrets set "Admin:Senha" "<ADMIN_SENHA>" --project src/PluviAlerta.Api
```

## Andamento

- [x] 1. Estrutura da solução, Docker Compose e README
- [x] 2. `MotorDeRisco` com testes
- [x] 3. Entidades, `DbContext`, migration e seed das 9 regionais
- [x] 4. `GET /api/regioes` com dados mockados e Swagger
- [x] 5. Worker do Open-Meteo, relatos, simulação, alertas e bot do Telegram

## Créditos

Dados meteorológicos: [Open-Meteo](https://open-meteo.com/), sob a licença [CC-BY 4.0](https://creativecommons.org/licenses/by/4.0/) e para uso não comercial. O crédito precisa aparecer no rodapé do app.
