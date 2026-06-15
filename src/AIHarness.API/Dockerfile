FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Layer-cache optimization: copy project files first for faster rebuilds
COPY AIHarness.sln .
COPY src/AIHarness.Core/AIHarness.Core.csproj                             src/AIHarness.Core/
COPY src/AIHarness.Infrastructure/AIHarness.Infrastructure.csproj         src/AIHarness.Infrastructure/
COPY src/AIHarness.Agents/AIHarness.Agents.csproj                         src/AIHarness.Agents/
COPY src/AIHarness.Orchestrator/AIHarness.Orchestrator.csproj             src/AIHarness.Orchestrator/
COPY src/AIHarness.API/AIHarness.API.csproj                               src/AIHarness.API/
COPY src/AIHarness.Worker/AIHarness.Worker.csproj                         src/AIHarness.Worker/

RUN dotnet restore src/AIHarness.API/AIHarness.API.csproj

COPY . .

RUN dotnet publish src/AIHarness.API/AIHarness.API.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080

RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AIHarness.API.dll"]
