diagrid login

d orgs list
d region list

kind get clusters
d region deploy #TUI
d region list

diagrid region use alice-local-region 
# show the dapr.yaml
diagrid dev run --file dapr.yaml --project order-manager-workflow-local  

k ctx kind-catalyst-alice-local-region
k get po --all-namespaces

# talk through deployment
# show the ui and import the sub

