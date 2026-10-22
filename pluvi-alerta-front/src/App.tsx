import { useCallback, useEffect, useState } from 'react'
import './App.css'
import logoEscudo from './assets/logo-escudo.png'

type Regional = {
  id: number
  nome: string
}

const API_URL = 'http://localhost:5251/api/regioes'

function App() {
  const [regioes, setRegioes] = useState<Regional[]>([])
  const [carregando, setCarregando] = useState(true)
  const [erro, setErro] = useState('')

  const buscarRegioes = useCallback(async () => {
    setCarregando(true)
    setErro('')

    try {
      const resposta = await fetch(API_URL)

      if (!resposta.ok) {
        throw new Error('Não foi possível consultar as regionais.')
      }

      const dados: Regional[] = await resposta.json()
      setRegioes(dados)
    } catch {
      setErro('Não foi possível carregar as regionais. Verifique se a API está funcionando.')
    } finally {
      setCarregando(false)
    }
  }, [])

  useEffect(() => {
    buscarRegioes()
  }, [buscarRegioes])

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

        <span className="status">Painel meteorológico</span>
      </header>

      <div className="faixa-slogan">
        <strong>Informação hoje. Mais segurança amanhã.</strong>
        <span>Belo Horizonte</span>
      </div>

      <section className="boas-vindas">
        <p className="sobretitulo">Central de monitoramento</p>
        <h2>Acompanhe as condições da chuva</h2>
        <p>Consulte as regionais monitoradas pelo PluviAlerta-BH.</p>
      </section>

      <section className="indicadores">
        <article className="cartao indicador">
          <span>Regionais monitoradas</span>
          <strong>{regioes.length || '--'}</strong>
          <small>Em Belo Horizonte</small>
        </article>

        <article className="cartao indicador">
          <span>Chuva atual</span>
          <strong>
            -- <small>mm</small>
          </strong>
          <small>Aguardando dados meteorológicos</small>
        </article>

        <article className="cartao indicador">
          <span>Alertas ativos</span>
          <strong>--</strong>
          <small>Aguardando integração dos alertas</small>
        </article>
      </section>

      <section className="secao-regionais">
        <div className="titulo-secao">
          <div>
            <h2>Regionais de Belo Horizonte</h2>
            <p>Consulte as regiões retornadas pela API.</p>
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

        {erro && (
          <p className="mensagem-erro" role="alert">
            {erro}
            <button type="button" onClick={buscarRegioes}>
              Tentar de novo
            </button>
          </p>
        )}

        {!carregando && !erro && regioes.length === 0 && (
          <p className="vazio">Nenhuma regional foi retornada pela API.</p>
        )}

        {!carregando && !erro && regioes.length > 0 && (
          <div className="grade-regionais">
            {regioes.map((regional) => (
              <article className="cartao regional" key={regional.id}>
                <div className="icone-regional">⌖</div>
                <div>
                  <h3>{regional.nome}</h3>
                  <p>Monitoramento meteorológico</p>
                </div>
                <span className="risco">Sem classificação</span>
              </article>
            ))}
          </div>
        )}
      </section>

      <section className="cartao area-alertas">
        <div className="icone-alerta">!</div>
        <div>
          <h2>Central de alertas</h2>
          <p>
            Os alertas serão exibidos aqui quando a integração
            com os dados meteorológicos estiver pronta.
          </p>
        </div>
      </section>

      <footer>
        <img className="rodape-logo" src={logoEscudo} alt="" />
        PluviAlerta-BH · Sistema de monitoramento de chuvas
      </footer>
    </main>
  )
}

export default App