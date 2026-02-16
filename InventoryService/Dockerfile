FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["InventoryService/InventoryService.csproj", "InventoryService/"]
COPY ["Domain/Domain.csproj", "Domain/"]
COPY ["ServiceDefaults/ServiceDefaults.csproj", "ServiceDefaults/"]
RUN dotnet restore "InventoryService/InventoryService.csproj"
COPY InventoryService/ InventoryService/
COPY Domain/ Domain/
COPY ServiceDefaults/ ServiceDefaults/
RUN dotnet build "InventoryService/InventoryService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "InventoryService/InventoryService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Diagrid.Labs.Catalyst.OrderWorkflow.InventoryService.dll"]
