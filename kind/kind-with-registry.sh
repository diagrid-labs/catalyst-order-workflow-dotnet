#!/bin/sh
set -o errexit

export HA_MODE=true
export DAPR_REGISTRY=localhost:5001/dapr
export DAPR_TAG=dev
export DAPR_TEST_NAMESPACE=dapr-tests
export DAPR_NAMESPACE=dapr-tests
export TARGET_OS=linux
export TARGET_ARCH=arm64
export GOOS=linux
export GOARCH=arm64
export LOG_LEVEL=debug

# 1. Create registry container unless it already exists
reg_name='kind-registry'
reg_port='5001'
if [ "$(docker inspect -f '{{.State.Running}}' "${reg_name}" 2>/dev/null || true)" != 'true' ]; then
  docker run \
    -d --restart=always -p "127.0.0.1:${reg_port}:5000" --network bridge --name "${reg_name}" \
    registry:2.7
fi

# 2. Create kind cluster

kind create cluster --name "${CLUSTER_NAME}" --config kind/kind-cluster-config.yaml

# 3. Add the registry config to the nodes
#
# This is necessary because localhost resolves to loopback addresses that are
# network-namespace local.
# In other words: localhost in the container is not localhost on the host.
#
# We want a consistent name that works from both ends, so we tell containerd to
# alias localhost:${reg_port} to the registry container when pulling images
REGISTRY_DIR="/etc/containerd/certs.d/localhost:${reg_port}"
for node in $(kind get nodes --name "${CLUSTER_NAME}"); do
  docker exec "${node}" mkdir -p "${REGISTRY_DIR}"
  cat <<EOF | docker exec -i "${node}" cp /dev/stdin "${REGISTRY_DIR}/hosts.toml"
[host."http://${reg_name}:5000"]
EOF
done

# 4. Connect the registry to the cluster network if not already connected
# This allows kind to bootstrap the network but ensures they're on the same network
if [ "$(docker inspect -f='{{json .NetworkSettings.Networks.kind}}' "${reg_name}")" = 'null' ]; then
  docker network connect "kind" "${reg_name}"
fi

# 5. Document the local registry
# https://github.com/kubernetes/enhancements/tree/master/keps/sig-cluster-lifecycle/generic/1755-communicating-a-local-registry
cat <<EOF | kubectl apply -f -
apiVersion: v1
kind: ConfigMap
metadata:
  name: local-registry-hosting
  namespace: kube-public
data:
  localRegistryHosting.v1: |
    host: "localhost:${reg_port}"
    help: "https://kind.sigs.k8s.io/docs/user/local-registry/"
EOF

kubectl create namespace dapr-tests

# 6. Install CloudNative PostgreSQL
# https://cloudnative-pg.io/docs/1.27/quickstart/
#kubectl apply --server-side -f \
#  https://raw.githubusercontent.com/cloudnative-pg/cloudnative-pg/release-1.27/releases/cnpg-1.27.0.yaml

#kubectl rollout status deployment \
#  -n cnpg-system cnpg-controller-manager

# 7. Install PostgreSQL Cluster
#kubectl apply -f ./postgres-cluster.yaml

# 8. Install chaos-mess
#helm install chaos-mesh chaos-mesh/chaos-mesh -n=chaos-mesh --create-namespace

# 9. Create manager role for chaos dahsboard
#kubectl apply -f manager-rbac.yaml

# 10. Create manager token for chaos dahsboard
#kubectl create token account-cluster-manager-thrnb

# 11. Install Cert Manager
kubectl apply -f https://github.com/cert-manager/cert-manager/releases/download/v1.19.1/cert-manager.yaml
#helm repo add jetstack https://charts.jetstack.io --force-update
#helm upgrade --install cert-manager jetstack/cert-manager --namespace cert-manager --create-namespace --set crds.enabled=true

# 12. Install OTEL operator
#kubectl apply -f https://github.com/open-telemetry/opentelemetry-operator/releases/latest/download/opentelemetry-operator.yaml

# 13. Install Jaeger in-memory
#kubectl create namespace observability
#kubectl apply -f jaeger-inmemory.yaml  -n observability

