import 'leaflet/dist/leaflet.css'
import { CircleMarker, MapContainer, TileLayer, Tooltip, useMapEvents } from 'react-leaflet'
import type { RegiaoAtual, Relato } from './api'
import { corNivel } from './risco'

type Props = {
  regioes: RegiaoAtual[]
  relatos: Relato[]
  selecionadaId: number | null
  aoSelecionar: (id: number) => void
  // Modo "relatar um problema": o clique no mapa marca o ponto do relato.
  modoRelato: boolean
  pontoRelato: [number, number] | null
  aoMarcarPonto: (latitude: number, longitude: number) => void
}

const CENTRO_BH: [number, number] = [-19.895, -43.96]

// Círculo no ponto central de cada regional (ainda não temos os polígonos oficiais).
export function MapaRegionais(props: Props) {
  const { regioes, relatos, selecionadaId, aoSelecionar, modoRelato, pontoRelato, aoMarcarPonto } = props

  return (
    <MapContainer
      center={CENTRO_BH}
      zoom={11}
      scrollWheelZoom={false}
      className={modoRelato ? 'mapa modo-relato' : 'mapa'}
    >
      <TileLayer
        attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
        url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
      />
      <CliqueNoMapa ativo={modoRelato} aoClicar={aoMarcarPonto} />

      {regioes.map((r) => {
        const cor = corNivel[r.nivel]
        const selecionada = r.id === selecionadaId
        return (
          <CircleMarker
            // "interactive" só vale na criação do círculo: a chave muda com o modo para recriá-lo.
            key={`${r.id}-${modoRelato}`}
            center={[r.latitude, r.longitude]}
            radius={selecionada ? 26 : 20}
            // No modo relato o círculo não "rouba" o clique: o ponto pode cair em cima dele.
            interactive={!modoRelato}
            pathOptions={{
              color: selecionada ? '#012150' : cor,
              weight: selecionada ? 3 : 2,
              fillColor: cor,
              fillOpacity: 0.55,
            }}
            eventHandlers={{ click: () => aoSelecionar(r.id) }}
          >
            <Tooltip permanent direction="center" className="mapa-rotulo">
              <strong>{r.score}</strong>
              <span>{r.nome}</span>
            </Tooltip>
          </CircleMarker>
        )
      })}

      {relatos.map((rel) => (
        <CircleMarker
          key={`relato-${rel.id}`}
          center={[rel.latitude, rel.longitude]}
          radius={6}
          pathOptions={{ color: '#fff', weight: 2, fillColor: '#066dcc', fillOpacity: 1 }}
        >
          <Tooltip>
            Relato: {rel.tipo}
            {rel.descricao ? ` — ${rel.descricao}` : ''}
          </Tooltip>
        </CircleMarker>
      ))}

      {modoRelato && pontoRelato && (
        <CircleMarker
          center={pontoRelato}
          radius={10}
          interactive={false}
          pathOptions={{ color: '#012150', weight: 3, dashArray: '4 3', fillColor: '#43aff0', fillOpacity: 0.9 }}
        />
      )}
    </MapContainer>
  )
}

function CliqueNoMapa({ ativo, aoClicar }: { ativo: boolean; aoClicar: (lat: number, lng: number) => void }) {
  useMapEvents({
    click(e) {
      if (ativo) aoClicar(e.latlng.lat, e.latlng.lng)
    },
  })
  return null
}
