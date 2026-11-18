FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY Partilhae_Net/Partilhae_Net.csproj Partilhae_Net/

RUN dotnet restore Partilhae_Net/Partilhae_Net.csproj

COPY Partilhae_Net/ Partilhae_Net/

RUN dotnet publish Partilhae_Net/Partilhae_Net.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=10000

EXPOSE 10000

ENTRYPOINT ["dotnet", "Partilhae_Net.dll"]