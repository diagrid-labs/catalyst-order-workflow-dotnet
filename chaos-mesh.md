# Chaos Mesh (Chaos Engineering)

[Chaos Mesh](https://chaos-mesh.org/) can be deployed to the cluster to inject faults and test resilience.

## Install Chaos Mesh

```bash
helm repo add chaos-mesh https://charts.chaos-mesh.org
helm repo update
helm install chaos-mesh chaos-mesh/chaos-mesh \
  -n=chaos-mesh --create-namespace \
  --set dashboard.service.type=LoadBalancer
```

## Access the Chaos Mesh Dashboard

Get the external IP of the dashboard:

```bash
kubectl get svc -n chaos-mesh chaos-dashboard
```

Then open `http://<EXTERNAL-IP>:2333` in your browser.

## Dashboard Login (RBAC Token)

Create a service account and generate a token to log in:

```bash
kubectl create serviceaccount chaos-mesh-account -n chaos-mesh
kubectl create clusterrolebinding chaos-mesh-account-binding \
  --clusterrole=cluster-admin \
  --serviceaccount=chaos-mesh:chaos-mesh-account
kubectl create token chaos-mesh-account -n chaos-mesh
```

Copy the output token and paste it into the dashboard login screen.

You can create multiple service accounts with different permissions. For example, a read-only account:

```bash
kubectl create serviceaccount chaos-viewer -n chaos-mesh
kubectl create clusterrolebinding chaos-viewer-binding \
  --clusterrole=viewer \
  --serviceaccount=chaos-mesh:chaos-viewer
kubectl create token chaos-viewer -n chaos-mesh
```

Tokens created with `kubectl create token` are short-lived by default (1 hour). To create a non-expiring token, use a Secret-based approach:

```bash
kubectl apply -f - <<EOF
apiVersion: v1
kind: Secret
metadata:
  name: chaos-mesh-account-token
  namespace: chaos-mesh
  annotations:
    kubernetes.io/service-account.name: chaos-mesh-account
type: kubernetes.io/service-account-token
EOF
```

Retrieve the token:

```bash
kubectl get secret chaos-mesh-account-token -n chaos-mesh -o jsonpath='{.data.token}' | base64 -d
```

## Experiments

### Pod Kill (one by one)

Kills one random pod in the `catalyst-order-workflow-demo` namespace. Deployed paused — trigger it manually from the dashboard or CLI.

```yaml
apiVersion: chaos-mesh.org/v1alpha1
kind: PodChaos
metadata:
  name: pod-kill-one-by-one
  namespace: catalyst-order-workflow-demo
  annotations:
    experiment.chaos-mesh.org/pause: "true"
spec:
  action: pod-kill
  mode: one
  selector:
    namespaces:
      - catalyst-order-workflow-demo
```

Apply it:

```bash
kubectl apply -f - <<EOF
apiVersion: chaos-mesh.org/v1alpha1
kind: PodChaos
metadata:
  name: pod-kill-one-by-one
  namespace: catalyst-order-workflow-demo
  annotations:
    experiment.chaos-mesh.org/pause: "true"
spec:
  action: pod-kill
  mode: one
  selector:
    namespaces:
      - catalyst-order-workflow-demo
EOF
```

Unpause when ready:

```bash
kubectl annotate podchaos pod-kill-one-by-one -n catalyst-order-workflow-demo experiment.chaos-mesh.org/pause=false --overwrite
```
