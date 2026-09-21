#!/usr/bin/env bash
# Aguarda a API responder 200 em /health.
#   ./scripts/wait-for-health.sh http://localhost:8081 [timeout_segundos]
set -euo pipefail

BASE_URL="${1:-http://localhost:8080}"
TIMEOUT="${2:-180}"
INTERVALO=5
DECORRIDO=0

echo "==> Aguardando ${BASE_URL}/health (timeout: ${TIMEOUT}s)"

while [ "${DECORRIDO}" -lt "${TIMEOUT}" ]; do
  CODIGO="$(curl -s -o /dev/null -w '%{http_code}' --max-time 5 "${BASE_URL}/health" || echo 000)"

  if [ "${CODIGO}" = "200" ]; then
    echo "==> API saudável após ${DECORRIDO}s"
    curl -s "${BASE_URL}/health"
    echo
    exit 0
  fi

  echo "    ... ainda não disponível (HTTP ${CODIGO}) — ${DECORRIDO}s/${TIMEOUT}s"
  sleep "${INTERVALO}"
  DECORRIDO=$((DECORRIDO + INTERVALO))
done

echo "==> ERRO: a API não ficou saudável em ${TIMEOUT}s" >&2
exit 1
