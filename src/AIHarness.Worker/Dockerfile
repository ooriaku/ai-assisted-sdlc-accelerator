FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY AIHarness.sln .
COPY src/AIHarness.Core/AIHarness.Core.csproj                             src/AIHarness.Core/
COPY src/AIHarness.Infrastructure/AIHarness.Infrastructure.csproj         src/AIHarness.Infrastructure/
COPY src/AIHarness.Worker/AIHarness.Worker.csproj                         src/AIHarness.Worker/

RUN dotnet restore src/AIHarness.Worker/AIHarness.Worker.csproj

COPY . .

RUN dotnet publish src/AIHarness.Worker/AIHarness.Worker.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AIHarness.Worker.dll"]
