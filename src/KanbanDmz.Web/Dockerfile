# Build stage — needs all projects since KanbanDmz.Web references Domain and ServiceDefaults
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /build

# Copy project files first for layer-cached restore
COPY src/KanbanDmz.Domain/KanbanDmz.Domain.csproj           src/KanbanDmz.Domain/
COPY src/KanbanDmz.ServiceDefaults/KanbanDmz.ServiceDefaults.csproj src/KanbanDmz.ServiceDefaults/
COPY src/KanbanDmz.Web/KanbanDmz.Web.csproj                 src/KanbanDmz.Web/
RUN dotnet restore src/KanbanDmz.Web/KanbanDmz.Web.csproj

# Copy all source and publish
COPY src/ src/
RUN dotnet publish src/KanbanDmz.Web/KanbanDmz.Web.csproj \
    -c Release \
    --no-restore \
    -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "KanbanDmz.Web.dll"]
