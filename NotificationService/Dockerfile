FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8083

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["NotificationService/NotificationService.csproj", "NotificationService/"]
COPY ["Domain/Domain.csproj", "Domain/"]
COPY ["ServiceDefaults/ServiceDefaults.csproj", "ServiceDefaults/"]
RUN dotnet restore "NotificationService/NotificationService.csproj"
COPY NotificationService/ NotificationService/
COPY Domain/ Domain/
COPY ServiceDefaults/ ServiceDefaults/
RUN dotnet build "NotificationService/NotificationService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NotificationService/NotificationService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Diagrid.Labs.Catalyst.OrderWorkflow.NotificationService.dll"]
