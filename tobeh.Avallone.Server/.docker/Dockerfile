FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app

# signalr
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["tobeh.Avallone.Server.csproj", "tobeh.Avallone.Server/"]
RUN dotnet restore "tobeh.Avallone.Server/tobeh.Avallone.Server.csproj"
COPY . ./tobeh.Avallone.Server
WORKDIR "/src/tobeh.Avallone.Server"
RUN dotnet build "tobeh.Avallone.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "tobeh.Avallone.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

CMD ["sh", "-c", "dotnet tobeh.Avallone.Server.dll"]