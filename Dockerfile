FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY MyMiniLMS/MyMiniLMS.csproj MyMiniLMS/
RUN dotnet restore MyMiniLMS/MyMiniLMS.csproj

COPY . .
RUN dotnet publish MyMiniLMS/MyMiniLMS.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV PORT=8080
EXPOSE 8080

ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080} dotnet MyMiniLMS.dll"]
