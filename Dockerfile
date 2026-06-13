# ─────────────────────────────────────────────────────────────
# Dockerfile para NetGuard GT API (.NET 8) — listo para Render.com
# Build multi-etapa: se compila con el SDK y se ejecuta con el runtime (imagen final ligera).
# ─────────────────────────────────────────────────────────────

# Etapa 1: compilación y publicación
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiamos primero los manifiestos para aprovechar la caché de capas en el restore.
COPY global.json ./
COPY src/NetGuardGT.Api/NetGuardGT.Api.csproj src/NetGuardGT.Api/
RUN dotnet restore src/NetGuardGT.Api/NetGuardGT.Api.csproj

# Copiamos el resto del código fuente y publicamos en modo Release.
COPY src/ src/
RUN dotnet publish src/NetGuardGT.Api/NetGuardGT.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Etapa 2: imagen de ejecución (solo runtime de ASP.NET Core)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Render inyecta la variable de entorno PORT; la aplicación la lee en Program.cs.
# EXPOSE es informativo; Render mapea el puerto automáticamente.
EXPOSE 8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "NetGuardGT.Api.dll"]
