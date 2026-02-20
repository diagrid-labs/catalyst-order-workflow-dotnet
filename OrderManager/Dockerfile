FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["OrderManager/OrderManager.csproj", "OrderManager/"]
COPY ["Domain/Domain.csproj", "Domain/"]
COPY ["ServiceDefaults/ServiceDefaults.csproj", "ServiceDefaults/"]
RUN dotnet restore "OrderManager/OrderManager.csproj"
COPY . .
WORKDIR "/src/OrderManager"
RUN dotnet build "OrderManager.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OrderManager.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Diagrid.Labs.Catalyst.OrderWorkflow.OrderManager.dll"]
