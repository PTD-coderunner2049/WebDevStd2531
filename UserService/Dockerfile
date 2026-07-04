FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY UserService/UserService.csproj UserService/
RUN dotnet restore UserService/UserService.csproj

COPY . .
WORKDIR /src/UserService
RUN dotnet publish UserService.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Docker

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "UserService.dll"]
