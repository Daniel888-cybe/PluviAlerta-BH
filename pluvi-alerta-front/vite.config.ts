import react from '@vitejs/plugin-react'
import { defineConfig, loadEnv } from 'vite'

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')

  return {
    plugins: [react()],
    server: {
      // O front chama "/api/..." e o Vite repassa para a API em C#.
      // Assim não tem porta fixa no código nem problema de CORS no desenvolvimento.
      // dotnet run = 5261 (padrão) | Docker = 8080 → crie um .env.local com VITE_API_URL=http://localhost:8080
      proxy: {
        '/api': {
          target: env.VITE_API_URL || 'http://localhost:5261',
          changeOrigin: true,
        },
      },
    },
  }
})
