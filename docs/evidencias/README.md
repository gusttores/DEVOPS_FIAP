# Como capturar as evidências (prints)

Salve cada print **nesta pasta**, com exatamente o nome indicado na tabela.
Depois, abra `docs/documentacao-tecnica.html` no navegador e use
**Arquivo → Imprimir → Salvar como PDF** (A4, margens padrão, "Imprimir
imagens de fundo" ligado): as figuras entram no lugar dos espaços reservados e
o PDF é regerado com as evidências embutidas.

| Arquivo | O que capturar | Onde |
|---|---|---|
| `01-pipeline-completo.png` | O workflow inteiro concluído, com os 4 jobs em verde | GitHub → aba **Actions** → execução "CI/CD - GovAmbiental API" |
| `02-build-testes.png` | Passo `dotnet test` expandido, mostrando os 9 testes aprovados | job *Build e testes automatizados* |
| `03-imagem-ghcr.png` | A imagem publicada e suas tags | aba **Packages** do repositório (ou o resumo do job *Build e push da imagem Docker*) |
| `04-deploy-staging.png` | Passo *Smoke tests* com "7/7 verificações passaram" | job *Deploy em STAGING* |
| `05-aprovacao-producao.png` | Tela **Review deployments** com o job aguardando aprovação | job *Deploy em PRODUÇÃO* (status *Waiting*) |
| `06-deploy-producao.png` | Job de produção concluído, smoke tests aprovados na 8080 | job *Deploy em PRODUÇÃO* |
| `07-containers-healthy.png` | `docker compose ps` com `api` e `db` em *healthy* | terminal local |
| `08-staging-swagger.png` | Swagger aberto em `http://localhost:8081/swagger` | navegador |
| `09-producao-swagger.png` | Swagger aberto em `http://localhost:8080/swagger` | navegador |
| `10-health-ambientes.png` | `curl` do `/health` nas portas 8081 e 8080, lado a lado | terminal local |

## Roteiro rápido para gerar os prints 7 a 10

```bash
# staging na 8081 e produção na 8080, ao mesmo tempo
./scripts/deploy-local.sh staging
./scripts/deploy-local.sh prod

docker compose --env-file .env.staging -f docker-compose.staging.yml ps
docker compose --env-file .env.prod    -f docker-compose.prod.yml    ps

curl -s http://localhost:8081/health | python3 -m json.tool
curl -s http://localhost:8080/health | python3 -m json.tool
```

Abra `http://localhost:8081/swagger` e `http://localhost:8080/swagger` no
navegador para os prints 8 e 9.

## Evidências que o próprio pipeline gera

Cada execução do workflow anexa artefatos que também servem como comprovação
(Actions → execução → seção **Artifacts**):

- `resultados-testes` — relatório `.trx` dos testes xUnit e cobertura de código;
- `evidencias-staging` — `docker compose ps`, logs completos e `/health` de staging;
- `evidencias-producao` — os mesmos arquivos do ambiente de produção;
- `app-publish` — binários publicados pelo `dotnet publish`.
