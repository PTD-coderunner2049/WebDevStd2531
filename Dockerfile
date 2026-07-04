FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY WebDevStd2531/WebDevStd2531.csproj WebDevStd2531/
RUN dotnet restore WebDevStd2531/WebDevStd2531.csproj

COPY . .
WORKDIR /src/WebDevStd2531
RUN dotnet publish WebDevStd2531.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Docker

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "WebDevStd2531.dll"]
