# Orquestração com Kubernetes

Estrutura em **Kustomize**: uma `base` comum e um _overlay_ por ambiente.

```
k8s/
├── base/                      # manifests comuns aos dois ambientes
│   ├── api-configmap.yaml     # configuração não sensível
│   ├── api-secret.yaml        # JWT e senha do banco (exemplo)
│   ├── api-deployment.yaml    # Deployment da API + probes + limites
│   ├── api-service.yaml       # Service ClusterIP
│   ├── db-statefulset.yaml    # SQL Server 2022 + PVC (volume persistente)
│   ├── db-service.yaml        # Service headless do banco
│   └── ingress.yaml           # Ingress nginx
└── overlays/
    ├── staging/               # namespace govambiental-staging, 1 réplica
    └── production/            # namespace govambiental-prod, 2 réplicas + HPA + PDB
```

## Subir um cluster local e aplicar

```bash
# 1) cluster local
kind create cluster --name govambiental

# 2) conferir o que será aplicado (sem aplicar)
kubectl kustomize k8s/overlays/staging

# 3) staging
kubectl apply -k k8s/overlays/staging
kubectl -n govambiental-staging rollout status deploy/govambiental-api

# 4) produção
kubectl apply -k k8s/overlays/production
kubectl -n govambiental-prod rollout status deploy/govambiental-api

# 5) acessar a API sem Ingress
kubectl -n govambiental-staging port-forward svc/govambiental-api 8081:80
curl http://localhost:8081/health
```

## Apontar para outra versão da imagem

A imagem já aponta para `ghcr.io/gusttores/govambiental-api` nos dois `kustomization.yaml`.
Para fixar uma versão específica, use o próprio kustomize:

```bash
cd k8s/overlays/production
kustomize edit set image ghcr.io/gusttores/govambiental-api=ghcr.io/gusttores/govambiental-api:v1.2.3
```

## Recursos usados por requisito

| Requisito | Onde está |
|---|---|
| Volumes | `volumeClaimTemplates` (PVC de 5Gi) no `db-statefulset.yaml` |
| Variáveis de ambiente | `ConfigMap` + `envFrom` + `env` no `api-deployment.yaml` |
| Segredos | `Secret` `govambiental-api-secret` referenciado por `secretKeyRef` |
| Rede | `Service` ClusterIP + `Service` headless + `Ingress` nginx |
| Dois ambientes | overlays `staging` e `production`, em namespaces separados |
