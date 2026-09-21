# syntax=docker/dockerfile:1

# =============================================================================
# GovAmbiental.API — imagem multi-stage
#
#   restore -> build -> test -> publish -> final
#
# Estratégias aplicadas:
#   * multi-stage: o SDK (~800 MB) fica fora da imagem final, que usa só o
#     runtime ASP.NET (~220 MB);
#   * cache de camadas: o .csproj é copiado antes do código, então o
#     `dotnet restore` só roda de novo quando as dependências mudam;
#   * stage `test`: permite rodar os testes dentro do Docker
#     (`docker build --target test .`), usado como fallback no pipeline;
#   * usuário não-root (`appuser`) na imagem final;
#   * HEALTHCHECK apontando para o endpoint /health da própria API.
# =============================================================================

# ---------- 1) Restore ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS restore
WORKDIR /src

COPY ["GovAmbiental.sln", "./"]
COPY ["src/GovAmbiental.API/GovAmbiental.API.csproj", "src/GovAmbiental.API/"]
COPY ["tests/GovAmbiental.Tests/GovAmbiental.Tests.csproj", "tests/GovAmbiental.Tests/"]
RUN dotnet restore "GovAmbiental.sln"

# ---------- 2) Build ----------
FROM restore AS build
COPY . .
RUN dotnet build "GovAmbiental.sln" -c Release --no-restore

# ---------- 3) Test ----------
FROM build AS test
RUN dotnet test "tests/GovAmbiental.Tests/GovAmbiental.Tests.csproj" \
        -c Release --no-build \
        --logger "trx;LogFileName=test-results.trx" \
        --results-directory /testresults

# ---------- 4) Publish ----------
FROM build AS publish
ARG APP_VERSION=local
RUN dotnet publish "src/GovAmbiental.API/GovAmbiental.API.csproj" \
        -c Release --no-build \
        -o /app/publish \
        /p:UseAppHost=false

# ---------- 5) Runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

# curl é usado apenas pelo HEALTHCHECK do container.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Usuário sem privilégios (princípio do menor privilégio).
RUN groupadd --system --gid 1001 appgroup \
    && useradd --system --uid 1001 --gid appgroup --create-home appuser

WORKDIR /app
COPY --from=publish --chown=appuser:appgroup /app/publish .

ARG APP_VERSION=local
ENV APP_VERSION=${APP_VERSION} \
    ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    TZ=America/Sao_Paulo

EXPOSE 8080

USER appuser

HEALTHCHECK --interval=15s --timeout=5s --start-period=40s --retries=5 \
    CMD curl --fail --silent http://localhost:8080/health || exit 1

LABEL org.opencontainers.image.title="GovAmbiental.API" \
      org.opencontainers.image.description="API de Governança e Compliance Ambiental (ESG) — .NET 8" \
      org.opencontainers.image.version="${APP_VERSION}" \
      org.opencontainers.image.source="https://github.com/gustavo-torres/govambiental-devops"

ENTRYPOINT ["dotnet", "GovAmbiental.API.dll"]
