# Build em duas etapas: o SDK compila, a imagem final leva só o runtime.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copia só os .csproj primeiro para o restore ficar em cache entre builds.
COPY global.json ./
COPY src/PluviAlerta.Domain/PluviAlerta.Domain.csproj src/PluviAlerta.Domain/
COPY src/PluviAlerta.Infrastructure/PluviAlerta.Infrastructure.csproj src/PluviAlerta.Infrastructure/
COPY src/PluviAlerta.Api/PluviAlerta.Api.csproj src/PluviAlerta.Api/
RUN dotnet restore src/PluviAlerta.Api/PluviAlerta.Api.csproj

COPY src/ src/
RUN dotnet publish src/PluviAlerta.Api/PluviAlerta.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "PluviAlerta.Api.dll"]
