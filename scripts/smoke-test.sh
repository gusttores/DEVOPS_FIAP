#!/usr/bin/env bash
# Smoke tests pós-deploy: valida que o ambiente publicado está realmente de pé.
#   ./scripts/smoke-test.sh http://localhost:8081
#
# Verifica: liveness, readiness (banco), Swagger, login JWT e listagem paginada.
set -uo pipefail

BASE_URL="${1:-http://localhost:8080}"
EMAIL="${AUTH_EMAIL:-auditor@govambiental.com}"
SENHA="${AUTH_PASSWORD:-Auditor@123}"

FALHAS=0
TOTAL=0

verificar() {
  local nome="$1" esperado="$2" obtido="$3"
  TOTAL=$((TOTAL + 1))
  if [ "${obtido}" = "${esperado}" ]; then
    echo "  [OK]    ${nome} (HTTP ${obtido})"
  else
    echo "  [FALHA] ${nome} — esperado HTTP ${esperado}, obtido ${obtido}"
    FALHAS=$((FALHAS + 1))
  fi
}

echo "==> Smoke tests em ${BASE_URL}"

# 1) Liveness
verificar "GET /health" 200 \
  "$(curl -s -o /dev/null -w '%{http_code}' "${BASE_URL}/health")"

# 2) Readiness (inclui conexão com o banco)
verificar "GET /health/ready" 200 \
  "$(curl -s -o /dev/null -w '%{http_code}' "${BASE_URL}/health/ready")"

# 3) Documentação OpenAPI
verificar "GET /swagger/v1/swagger.json" 200 \
  "$(curl -s -o /dev/null -w '%{http_code}' "${BASE_URL}/swagger/v1/swagger.json")"

# 4) Listagem pública paginada
verificar "GET /api/normas?pageNumber=1&pageSize=5" 200 \
  "$(curl -s -o /dev/null -w '%{http_code}' "${BASE_URL}/api/normas?pageNumber=1&pageSize=5")"

# 5) Login válido — precisa devolver um token JWT
LOGIN_BODY="$(curl -s -X POST "${BASE_URL}/api/auth/login" \
  -H 'Content-Type: application/json' \
  -d "{\"email\":\"${EMAIL}\",\"senha\":\"${SENHA}\"}")"

TOKEN="$(printf '%s' "${LOGIN_BODY}" | sed -n 's/.*"token"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p')"

TOTAL=$((TOTAL + 1))
if [ -n "${TOKEN}" ]; then
  echo "  [OK]    POST /api/auth/login devolveu token JWT"
else
  echo "  [FALHA] POST /api/auth/login não devolveu token — resposta: ${LOGIN_BODY}"
  FALHAS=$((FALHAS + 1))
fi

# 6) Credencial inválida deve ser rejeitada
verificar "POST /api/auth/login (senha errada) => 401" 401 \
  "$(curl -s -o /dev/null -w '%{http_code}' -X POST "${BASE_URL}/api/auth/login" \
      -H 'Content-Type: application/json' \
      -d '{"email":"invalido@teste.com","senha":"errada"}')"

# 7) Endpoint protegido sem token deve barrar
verificar "POST /api/normas sem token => 401" 401 \
  "$(curl -s -o /dev/null -w '%{http_code}' -X POST "${BASE_URL}/api/normas" \
      -H 'Content-Type: application/json' -d '{}')"

echo "==> Resultado: $((TOTAL - FALHAS))/${TOTAL} verificações passaram"

if [ "${FALHAS}" -gt 0 ]; then
  echo "==> SMOKE TESTS FALHARAM (${FALHAS} falha(s))" >&2
  exit 1
fi

echo "==> Ambiente validado com sucesso"
