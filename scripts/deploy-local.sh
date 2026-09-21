#!/usr/bin/env bash
# Sobe localmente um dos ambientes usando a imagem construída na própria máquina.
#   ./scripts/deploy-local.sh staging
#   ./scripts/deploy-local.sh prod
set -euo pipefail

AMBIENTE="${1:-staging}"
cd "$(dirname "$0")/.."

case "${AMBIENTE}" in
  staging) COMPOSE=docker-compose.staging.yml; ENVFILE=.env.staging; PORTA=8081 ;;
  prod|producao|production) COMPOSE=docker-compose.prod.yml; ENVFILE=.env.prod; PORTA=8080 ;;
  *) echo "Uso: $0 [staging|prod]" >&2; exit 1 ;;
esac

if [ ! -f "${ENVFILE}" ]; then
  echo "==> ${ENVFILE} não encontrado: criando a partir de .env.example"
  cp .env.example "${ENVFILE}"
  sed -i.bak "s|^IMAGE_NAME=.*|IMAGE_NAME=govambiental-api|"   "${ENVFILE}"
  sed -i.bak "s|^IMAGE_TAG=.*|IMAGE_TAG=local|"                "${ENVFILE}"
  sed -i.bak "s|^PULL_POLICY=.*|PULL_POLICY=never|"            "${ENVFILE}"
  sed -i.bak "s|^API_PORT=.*|API_PORT=${PORTA}|"               "${ENVFILE}"
  rm -f "${ENVFILE}.bak"
fi

echo "==> Construindo a imagem local govambiental-api:local"
docker build -t govambiental-api:local --build-arg APP_VERSION=local .

echo "==> Subindo o ambiente ${AMBIENTE}"
docker compose --env-file "${ENVFILE}" -f "${COMPOSE}" up -d

./scripts/wait-for-health.sh "http://localhost:${PORTA}" 300
./scripts/smoke-test.sh "http://localhost:${PORTA}"

echo "==> Pronto: http://localhost:${PORTA}/swagger"
