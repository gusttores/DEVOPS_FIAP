# Projeto — API de Governança e Compliance Ambiental (ESG) · DevOps

> **Disciplina:** Navegando pelo mundo DevOps — FIAP
> **Integrante:** Gustavo Silva Torres — RM564909
> **Aplicação base:** Web API em **C# / ASP.NET Core 8** desenvolvida no desafio
> anterior, tema ESG *Governança e compliance ambiental*.

Esta entrega adapta a API para um ciclo de vida **completamente automatizado**:
build, testes, empacotamento em imagem Docker, publicação no registry e deploy
em **dois ambientes (staging e produção)** — tudo disparado por um `git push`.

---

## Sumário

1. [O que a aplicação faz](#1-o-que-a-aplicação-faz)
2. [Arquitetura da solução DevOps](#2-arquitetura-da-solução-devops)
3. [Como executar localmente com Docker](#3-como-executar-localmente-com-docker)
4. [Pipeline CI/CD](#4-pipeline-cicd)
5. [Containerização](#5-containerização)
6. [Orquestração (Compose e Kubernetes)](#6-orquestração-compose-e-kubernetes)
7. [Variáveis de ambiente](#7-variáveis-de-ambiente)
8. [Testes automatizados](#8-testes-automatizados)
9. [Prints do funcionamento](#9-prints-do-funcionamento)
10. [Endpoints da API](#10-endpoints-da-api)
11. [Tecnologias utilizadas](#11-tecnologias-utilizadas)
12. [Desafios encontrados](#12-desafios-encontrados)
13. [Checklist de entrega](#13-checklist-de-entrega)

---

## 1. O que a aplicação faz

API RESTful para **registro automático de conformidade com normas ambientais e
auditorias internas**. O núcleo é um **motor de avaliação**: ao submeter as
evidências de uma auditoria, a API calcula um **score ponderado de
conformidade**, classifica o resultado e **gera automaticamente as
não-conformidades com seus planos de ação**, com prazos definidos pela
criticidade de cada requisito.

| Recurso | Implementação |
|---|---|
| Endpoints REST | 9 endpoints em 4 controllers |
| Arquitetura | MVVM (`Models` · `ViewModels` · `Controllers`/`Services`) |
| Autenticação | JWT Bearer + `[Authorize(Roles="Auditor")]` |
| Validação | FluentValidation + filtro global (HTTP 400) |
| Erros | `ExceptionHandlingMiddleware` → ProblemDetails (404/422/500) |
| Persistência | EF Core 8 + SQL Server 2022 + migrations automáticas |
| Observabilidade | `/health` (liveness) e `/health/ready` (readiness com banco) |
| Testes | 9 testes de integração xUnit + `WebApplicationFactory` |

---

## 2. Arquitetura da solução DevOps

```
                    git push (main / develop / tag v*)
                                  |
                                  v
        +---------------------------------------------------+
        |              GitHub Actions — CI/CD               |
        |                                                   |
        |  [1] build-and-test                               |
        |      dotnet restore -> build -> test (xUnit)      |
        |      cobertura + relatório + artefatos            |
        |                        |                          |
        |  [2] docker-image      v                          |
        |      docker buildx (multi-stage) -> push GHCR     |
        |      tags: sha-<commit> | staging | latest | vX.Y |
        |                        |                          |
        |  [3] deploy-staging    v      (Environment: staging)
        |      docker compose -f docker-compose.staging.yml |
        |      wait-for-health + smoke tests  :8081         |
        |                        |                          |
        |  [4] deploy-production v   (Environment: production
        |      aprovação manual obrigatória)                |
        |      docker compose -f docker-compose.prod.yml    |
        |      smoke tests :8080 + rollback automático      |
        +---------------------------------------------------+
                                  |
                                  v
        +----------------------+      +----------------------+
        |   STAGING  :8081     |      |  PRODUÇÃO  :8080     |
        |  api + SQL Server    |      |  api + SQL Server    |
        |  rede/volume próprios|      |  rede/volume próprios|
        +----------------------+      +----------------------+

        Alternativa de orquestração: k8s/overlays/{staging,production}
        (namespaces separados, PVC, ConfigMap, Secret, Ingress, HPA)
```

---

## 3. Como executar localmente com Docker

**Pré-requisitos:** Docker Desktop (ou Docker Engine) + Docker Compose v2.
Não é necessário ter .NET nem SQL Server instalados.

### 3.1 Subir tudo com um comando

```bash
git clone <url-do-repositorio>
cd WEBSERVICE_ASP.ET

cp .env.example .env        # ajuste as senhas se quiser
docker compose up -d --build
```

O Compose sobe **dois serviços**:

| Serviço | Imagem | Porta | Papel |
|---|---|---|---|
| `db` | `mcr.microsoft.com/mssql/server:2022-latest` | 1433 | banco de dados |
| `api` | construída a partir do `Dockerfile` | 8080 | aplicação |

A API só inicia depois que o banco passa no `healthcheck`
(`depends_on: condition: service_healthy`), e as **migrations + seed** rodam
sozinhas no startup.

### 3.2 Conferir que subiu

```bash
docker compose ps                         # os dois containers "healthy"
curl http://localhost:8080/health         # {"status":"Healthy",...}
curl http://localhost:8080/health/ready   # inclui a checagem do banco
curl "http://localhost:8080/api/normas?pageNumber=1&pageSize=5"
```

Swagger: **http://localhost:8080/swagger**

Login para obter o token JWT:

```bash
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"auditor@govambiental.com","senha":"Auditor@123"}'
```

### 3.3 Validar o ambiente automaticamente

```bash
./scripts/smoke-test.sh http://localhost:8080
```

Saída esperada: `7/7 verificações passaram`.

### 3.4 Encerrar

```bash
docker compose down          # mantém os dados (volume nomeado)
docker compose down -v       # remove também o volume do banco
```

### 3.5 Subir staging e produção na própria máquina

```bash
./scripts/deploy-local.sh staging   # http://localhost:8081/swagger
./scripts/deploy-local.sh prod      # http://localhost:8080/swagger
```

O script constrói a imagem local, cria o `.env` do ambiente a partir do
`.env.example`, sobe a stack, espera o health check e roda os smoke tests.
Os dois ambientes usam **redes, volumes, bancos e portas diferentes**, então
podem rodar ao mesmo tempo.

> **Mac com Apple Silicon:** a imagem oficial do SQL Server existe apenas para
> `linux/amd64` e roda emulada. Isso já está tratado com
> `platform: ${DB_PLATFORM:-linux/amd64}` nos arquivos Compose — só é preciso
> manter a emulação (Rosetta) habilitada no Docker Desktop.

---

## 4. Pipeline CI/CD

**Ferramenta escolhida: GitHub Actions** — integrada ao repositório, sem
servidor para manter, com registry de imagens (GHCR) e *environments* com
aprovação manual já embutidos.

Arquivos: `.github/workflows/ci-cd.yml` e `.github/workflows/k8s-validate.yml`.

### 4.1 Gatilhos

| Evento | O que roda |
|---|---|
| `push` em `develop` | build + testes + imagem + deploy em **staging** |
| `push` em `main` | build + testes + imagem + **staging** + **produção** (com aprovação) |
| `push` de tag `v*` | idem `main`, com a imagem versionada (`v1.0.0`) |
| `pull_request` para `main` | apenas build + testes (não publica nem faz deploy) |
| `workflow_dispatch` | execução manual pelo botão *Run workflow* |

### 4.2 Etapas

**1 — `build-and-test` (integração contínua)**

- `actions/setup-dotnet` com .NET 8 e **cache dos pacotes NuGet**;
- `dotnet restore` → `dotnet build -c Release`;
- `dotnet test` dos 9 testes xUnit, gerando `.trx` e **cobertura de código**;
- relatório de testes publicado no resumo do job (`dorny/test-reporter`);
- `dotnet publish` e upload dos artefatos (`resultados-testes`, `app-publish`).
- **Se um teste falhar, o pipeline para aqui** — nada é publicado nem implantado.

**2 — `docker-image` (empacotamento)**

- `docker/setup-buildx-action` + **cache de camadas no GitHub Actions** (`type=gha`);
- login no **GHCR** com o `GITHUB_TOKEN` (nenhum segredo extra necessário);
- `docker/metadata-action` gera as tags automaticamente:
  `sha-<commit>`, nome da branch, `staging`, `latest` e `vX.Y.Z` em tags;
- push da imagem e publicação de digest/versão no resumo do job.

**3 — `deploy-staging` (entrega contínua)**

- usa o GitHub *Environment* **`staging`**;
- monta o `.env.staging` a partir de *secrets* (com valores padrão de estudo);
- `docker compose -f docker-compose.staging.yml up -d` **com a imagem recém-publicada**;
- `scripts/wait-for-health.sh` aguarda `/health` responder 200 (até 240s);
- `scripts/smoke-test.sh` valida 7 cenários reais (health, readiness, Swagger,
  listagem paginada, login JWT, credencial inválida → 401, endpoint protegido → 401);
- logs, `docker compose ps` e o JSON do health são salvos como **artefato de evidência**.

**4 — `deploy-production` (deploy contínuo com portão)**

- depende do sucesso de staging;
- usa o *Environment* **`production`**, configurado com **required reviewers** →
  o job fica em *Waiting* até a aprovação manual;
- sobe `docker-compose.prod.yml` na porta 8080 e repete os smoke tests;
- **rollback automático**: se os smoke tests falharem, o passo `if: failure()`
  recoloca a tag `latest` (última imagem estável) e sobe de novo;
- evidências salvas por 30 dias.

**Workflow auxiliar — `k8s-validate.yml`**

Renderiza os dois overlays com `kubectl kustomize` e valida os manifests
gerados contra os schemas oficiais do Kubernetes com `kubeconform -strict`.

### 4.3 Como ligar o pipeline no seu repositório

```bash
# 1) publicar o código
git init && git add . && git commit -m "feat: pipeline CI/CD, Docker e Kubernetes"
git branch -M main
git remote add origin https://github.com/<SEU-USUARIO>/govambiental-devops.git
git push -u origin main
```

2. No GitHub: **Settings → Environments → New environment** → crie `staging`
   e `production`. Em `production`, marque **Required reviewers** e adicione
   você mesmo (é isso que cria a aprovação manual).
3. Opcional — **Settings → Secrets and variables → Actions** → crie
   `SA_PASSWORD`, `JWT_KEY` e `AUTH_PASSWORD`. Sem eles o pipeline usa os
   valores padrão de estudo e roda igual.
4. **Settings → Actions → General → Workflow permissions** →
   *Read and write permissions* (necessário para publicar no GHCR).
5. Acompanhe em **Actions**; a imagem aparece em **Packages**.

---

## 5. Containerização

### 5.1 Dockerfile

```dockerfile
# syntax=docker/dockerfile:1

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
        -c Release --no-build -o /app/publish /p:UseAppHost=false

# ---------- 5) Runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
RUN groupadd --system --gid 1001 appgroup \
    && useradd --system --uid 1001 --gid appgroup --create-home appuser
WORKDIR /app
COPY --from=publish --chown=appuser:appgroup /app/publish .
ARG APP_VERSION=local
ENV APP_VERSION=${APP_VERSION} \
    ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    TZ=America/Sao_Paulo
EXPOSE 8080
USER appuser
HEALTHCHECK --interval=15s --timeout=5s --start-period=40s --retries=5 \
    CMD curl --fail --silent http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "GovAmbiental.API.dll"]
```

### 5.2 Estratégias adotadas

| Estratégia | Por quê |
|---|---|
| **Multi-stage build** | O SDK (~800 MB) fica fora da imagem final, que usa só o runtime ASP.NET. Resultado: imagem final ~15% do tamanho do build. |
| **Camada de restore separada** | O `.csproj` é copiado **antes** do código; enquanto as dependências não mudam, o `dotnet restore` vem do cache, cortando minutos de build. |
| **Stage `test` dedicado** | `docker build --target test .` roda os testes dentro do container — mesma execução em qualquer máquina. |
| **Usuário não-root (`appuser`)** | Menor privilégio: se a aplicação for comprometida, o processo não é root dentro do container. |
| **`HEALTHCHECK` nativo** | O Docker sabe se a API está saudável; o Compose usa isso em `depends_on: service_healthy` e o Kubernetes nas *probes*. |
| **Configuração por variável de ambiente** | Nenhum segredo dentro da imagem — a mesma imagem roda em dev, staging e produção, mudando só o ambiente (12-factor). |
| **`.dockerignore` enxuto** | `bin/`, `obj/`, `.git/`, `docs/`, `entrega/` ficam fora do contexto: build mais rápido e imagem sem lixo. |
| **Cache `type=gha` no CI** | As camadas são reaproveitadas entre execuções do pipeline. |
| **`ARG APP_VERSION` + LABELs OCI** | A versão publicada aparece no endpoint `/health` e nos metadados da imagem. |

### 5.3 Comandos úteis

```bash
docker build -t govambiental-api:local .              # build da imagem
docker build --target test .                          # rodar os testes no container
docker images govambiental-api                        # ver o tamanho
docker run --rm -p 8080:8080 govambiental-api:local   # rodar só a API
docker inspect --format '{{json .State.Health}}' govambiental-api-dev
docker compose logs -f api                            # acompanhar os logs
```

---

## 6. Orquestração (Compose e Kubernetes)

### 6.1 Docker Compose — três arquivos, três ambientes

| Arquivo | Ambiente | Porta | Imagem | Destaques |
|---|---|---|---|---|
| `docker-compose.yml` | desenvolvimento | 8080 | build local | banco exposto em 1433, hot rebuild |
| `docker-compose.staging.yml` | staging | 8081 | GHCR (`:staging`) | limites de recurso, rotação de logs |
| `docker-compose.prod.yml` | produção | 8080 | GHCR (`:latest`/`vX.Y.Z`) | `restart: always`, `no-new-privileges`, reservas de CPU/memória, senhas obrigatórias |

Os três requisitos de composição de ambiente estão cobertos:

- **Volumes** — volume nomeado por ambiente (`govambiental-db-data`,
  `govambiental-db-staging`, `govambiental-db-prod`) montado em
  `/var/opt/mssql`, então os dados sobrevivem ao `docker compose down`.
- **Variáveis de ambiente** — tudo que muda entre ambientes vem de `.env`
  (connection string, JWT, credenciais, portas, tag da imagem). O padrão
  `${VAR:?mensagem}` faz o Compose **falhar cedo** em staging/produção se uma
  senha não for informada.
- **Redes** — uma bridge dedicada por ambiente (`govambiental-net`,
  `govambiental-staging`, `govambiental-prod`). A API fala com o banco pelo
  **nome do serviço** (`Server=db,1433`) e, em staging/produção, o banco **não
  publica porta no host** — só é acessível de dentro da rede.

### 6.2 Kubernetes (alternativa completa)

```
k8s/
├── base/          ConfigMap · Secret · Deployment · Service · StatefulSet+PVC · Ingress
└── overlays/
    ├── staging/     namespace govambiental-staging · 1 réplica · logs Information
    └── production/  namespace govambiental-prod · 2 réplicas · HPA · PodDisruptionBudget
```

```bash
kind create cluster --name govambiental
kubectl apply -k k8s/overlays/staging
kubectl apply -k k8s/overlays/production
kubectl -n govambiental-prod rollout status deploy/govambiental-api
kubectl -n govambiental-staging port-forward svc/govambiental-api 8081:80
```

Detalhes em [`k8s/README.md`](k8s/README.md): *probes* (`startupProbe`,
`livenessProbe`, `readinessProbe`), PVC de 5 Gi para o banco, *rolling update*
com `maxUnavailable: 0` e autoscaling por CPU em produção.

---

## 7. Variáveis de ambiente

Modelo completo em [`.env.example`](.env.example). Nenhum `.env` real vai para
o Git (`.gitignore`); no pipeline os valores vêm de **GitHub Secrets** e, no
Kubernetes, de **ConfigMap** (não sensível) e **Secret** (sensível).

| Variável | Para que serve | Exemplo |
|---|---|---|
| `IMAGE_NAME` / `IMAGE_TAG` | imagem publicada pelo pipeline | `ghcr.io/usuario/govambiental-api` · `staging` |
| `SA_PASSWORD` | senha do SQL Server | `Gov@mbiental2026` |
| `DB_NAME` | banco por ambiente | `GovAmbientalDb_Staging` |
| `API_PORT` | porta publicada no host | `8081` (staging) · `8080` (produção) |
| `DB_PLATFORM` | arquitetura da imagem do banco | `linux/amd64` |
| `JWT_KEY` / `JWT_ISSUER` / `JWT_AUDIENCE` | emissão e validação do token | mínimo 32 bytes |
| `AUTH_EMAIL` / `AUTH_PASSWORD` | credencial do auditor | `auditor@govambiental.com` |
| `ASPNETCORE_ENVIRONMENT` | perfil de configuração | `Development` · `Staging` · `Production` |
| `ConnectionStrings__DefaultConnection` | conexão com o banco | montada pelo Compose |

> O ASP.NET Core mapeia `Jwt__Key` → seção `Jwt:Key`: o duplo *underline* é o
> separador de seções. É assim que a mesma imagem atende os três ambientes.

---

## 8. Testes automatizados

```bash
dotnet test                              # local
docker build --target test .             # dentro do container
```

9 testes de integração xUnit sobem a API inteira com `WebApplicationFactory`,
trocando o SQL Server por **EF Core InMemory** — por isso rodam no runner do
GitHub Actions sem precisar de banco. Cobrem: status 200 dos 4 controllers,
autenticação JWT (200/401), paginação e tradução das consultas.

No pipeline, os testes ficam **entre o build e o empacotamento**: uma falha
impede a publicação da imagem e, por consequência, qualquer deploy. Depois de
cada deploy, os **smoke tests** (`scripts/smoke-test.sh`) validam o ambiente
já no ar.

---

## 9. Prints do funcionamento

As evidências ficam em [`docs/evidencias/`](docs/evidencias/) e também na
documentação técnica em PDF (`docs/documentacao-tecnica.pdf`).

| # | Evidência | Onde capturar |
|---|---|---|
| 1 | Pipeline completo verde | aba **Actions** → execução do workflow |
| 2 | Job de build e testes (9/9 passando) | job *Build e testes automatizados* |
| 3 | Imagem publicada no GHCR | aba **Packages** do repositório |
| 4 | Job de deploy em staging + smoke tests | job *Deploy em STAGING* |
| 5 | Aprovação manual pendente em produção | job *Deploy em PRODUÇÃO* (*Review deployments*) |
| 6 | Job de deploy em produção concluído | job *Deploy em PRODUÇÃO* |
| 7 | `docker compose ps` com os containers *healthy* | terminal local |
| 8 | Swagger em staging (`:8081`) | navegador |
| 9 | Swagger em produção (`:8080`) | navegador |
| 10 | `/health` respondendo em ambos | navegador ou `curl` |

O pipeline também guarda evidências automaticamente como **artefatos**
(`evidencias-staging`, `evidencias-producao`, `resultados-testes`): logs dos
containers, `docker compose ps` e o JSON do health check.

---

## 10. Endpoints da API

| Método | Rota | Auth | Descrição |
|---|---|:--:|---|
| GET | `/health` | — | Liveness (sem tocar no banco) |
| GET | `/health/ready` | — | Readiness (checa conexão com o banco) |
| POST | `/api/auth/login` | — | Autentica e retorna o JWT |
| GET | `/api/normas` | — | Lista paginada de normas (filtros: categoria, órgão, ativa) |
| POST | `/api/normas` | 🔒 | Cadastra norma + requisitos |
| GET | `/api/auditorias` | — | Lista paginada de auditorias |
| GET | `/api/auditorias/{id}/relatorio` | — | Relatório consolidado (score por categoria) |
| POST | `/api/auditorias` | 🔒 | Planeja auditoria a partir de normas |
| POST | `/api/auditorias/{id}/avaliar` | 🔒 | Avaliação automática: score + não-conformidades |
| GET | `/api/conformidade/indicadores` | 🔒 | KPIs de conformidade |
| GET | `/api/conformidade/nao-conformidades` | — | Lista paginada com alertas de prazo |

**Credenciais de seed:** `auditor@govambiental.com` / `Auditor@123`
**Coleção Postman:** `postman/GovAmbiental.postman_collection.json` (o login já
salva o token em `{{token}}`; ajuste `{{baseUrl}}` para `:8080` ou `:8081`).

**Regras do motor de avaliação**

- Score = (Σ peso dos requisitos atendidos ÷ Σ peso total) × 100
- Classificação: ≥ 90% `Conforme` · 60–89% `ParcialmenteConforme` · < 60% `NaoConforme`
- Prazo da ação corretiva: Crítica 7d · Alta 15d · Média 30d · Baixa 60d

---

## 11. Tecnologias utilizadas

| Camada | Stack |
|---|---|
| **Aplicação** | C# 12 · ASP.NET Core 8 (Web API) · EF Core 8 · FluentValidation 11 · JWT Bearer · Swagger/OpenAPI (Swashbuckle) |
| **Banco de dados** | Microsoft SQL Server 2022 (container) · migrations EF Core · EF Core InMemory nos testes |
| **Testes** | xUnit 2.9 · `Microsoft.AspNetCore.Mvc.Testing` · Coverlet (cobertura) |
| **Containerização** | Docker (multi-stage, usuário não-root, HEALTHCHECK) · Docker Buildx · `.dockerignore` |
| **Orquestração** | Docker Compose v2 (dev/staging/produção) · Kubernetes + Kustomize (base + overlays, PVC, ConfigMap, Secret, Ingress, HPA, PDB) |
| **CI/CD** | GitHub Actions · GitHub Container Registry (GHCR) · Environments com aprovação · cache de NuGet e de camadas Docker · artefatos de evidência |
| **Qualidade / operação** | Health checks ASP.NET Core · smoke tests em Bash · kubeconform · rollback automático |
| **Ferramentas** | Git · Postman · Visual Studio / VS Code |

---

## 12. Desafios encontrados

| Desafio | Solução |
|---|---|
| **A API subia antes do SQL Server e quebrava nas migrations.** | `healthcheck` no serviço `db` + `depends_on: condition: service_healthy`, `EnableRetryOnFailure` no EF Core e um laço de retry com *backoff* de 5s (10 tentativas) ao aplicar as migrations no startup. |
| **O Docker não sabia se a aplicação estava realmente pronta.** | Endpoints `/health` (liveness) e `/health/ready` (readiness com `CanConnectAsync`), usados pelo `HEALTHCHECK`, pelo Compose e pelas probes do Kubernetes. |
| **"Passou no build" não significa "está no ar".** | Smoke tests pós-deploy (`scripts/smoke-test.sh`) validando 7 cenários reais em cada ambiente; o job falha se qualquer um falhar. |
| **Segredos não podem ir para a imagem nem para o Git.** | Configuração 100% por variável de ambiente (`Jwt__Key`, `ConnectionStrings__DefaultConnection`), `.env` no `.gitignore`, `.env.example` versionado, GitHub Secrets no pipeline e `Secret` no Kubernetes. |
| **Staging e produção colidiam na mesma máquina.** | Ambientes isolados: `name:` de projeto, rede, volume, banco, nome de container e porta próprios (8081 × 8080). |
| **Deploy em produção não podia ser automático demais.** | GitHub Environment `production` com *required reviewers*: o job aguarda aprovação manual; se os smoke tests falharem, o passo `if: failure()` faz rollback para a tag `latest`. |
| **Build da imagem lento a cada commit.** | Multi-stage com camada de restore separada + cache `type=gha` do Buildx + cache de pacotes NuGet no runner. |
| **Imagem do SQL Server não roda nativamente em Apple Silicon.** | `platform: ${DB_PLATFORM:-linux/amd64}` nos arquivos Compose (emulação no Mac, nativo no runner Linux). |
| **Erros de manifesto do Kubernetes só apareciam no `apply`.** | Workflow `k8s-validate.yml`: `kubectl kustomize` + `kubeconform -strict` contra os schemas oficiais, a cada push em `k8s/`. |

---

## 13. Checklist de entrega

| # | Item obrigatório | Status | Onde está |
|:--:|---|:--:|---|
| 1 | Projeto compactado em `.ZIP` com estrutura organizada | ☑ | `entrega/govambiental-devops.zip` |
| 2 | `Dockerfile` funcional | ☑ | [`Dockerfile`](Dockerfile) — multi-stage, não-root, HEALTHCHECK |
| 3 | `docker-compose.yml` ou arquivos Kubernetes | ☑ | `docker-compose.yml`, `docker-compose.staging.yml`, `docker-compose.prod.yml` **e** `k8s/` |
| 4 | Pipeline com etapas de build, teste e deploy | ☑ | [`.github/workflows/ci-cd.yml`](.github/workflows/ci-cd.yml) — 4 jobs encadeados |
| 5 | `README.md` com instruções e prints | ☑ | este arquivo (seções 3, 5 e 9) |
| 6 | Documentação técnica com evidências (PDF ou PPT) | ☑ | `docs/documentacao-tecnica.pdf` |
| 7 | Deploy realizado nos ambientes staging e produção | ☑ | jobs *Deploy em STAGING* (:8081) e *Deploy em PRODUÇÃO* (:8080) |

### Estrutura do projeto entregue

```
WEBSERVICE_ASP.ET/
├── .github/workflows/
│   ├── ci-cd.yml              # build · testes · imagem · staging · produção
│   └── k8s-validate.yml       # kustomize build + kubeconform
├── docs/
│   ├── documentacao-tecnica.pdf
│   └── evidencias/            # prints do pipeline e dos ambientes
├── k8s/
│   ├── base/                  # ConfigMap, Secret, Deployment, Service, StatefulSet, Ingress
│   └── overlays/{staging,production}/
├── scripts/
│   ├── deploy-local.sh        # sobe staging/produção na máquina local
│   ├── smoke-test.sh          # validação pós-deploy (7 cenários)
│   └── wait-for-health.sh     # espera o /health responder 200
├── src/GovAmbiental.API/      # código-fonte da aplicação
├── tests/GovAmbiental.Tests/  # 9 testes de integração xUnit
├── postman/                   # coleção Postman
├── Dockerfile
├── docker-compose.yml
├── docker-compose.staging.yml
├── docker-compose.prod.yml
├── .dockerignore
├── .env.example
└── README.md
```
