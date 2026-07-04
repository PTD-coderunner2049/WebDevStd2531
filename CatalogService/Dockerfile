FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY CatalogService/CatalogService.csproj CatalogService/
RUN dotnet restore CatalogService/CatalogService.csproj

COPY . .
WORKDIR /src/CatalogService
RUN dotnet publish CatalogService.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Docker

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "CatalogService.dll"]
