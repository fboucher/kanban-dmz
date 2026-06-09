var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var postgresDb = postgres.AddDatabase("kanban");

var dabConfigPath = Path.GetFullPath(Path.Combine("..", "..", "dab"));

var dab = builder.AddContainer("dab", "mcr.microsoft.com/azure-databases/data-api-builder:latest")
    .WithBindMount(dabConfigPath, "/App/configs")
    .WithEnvironment("DAB_CONFIG", "/App/configs/dab-config.json")
    .WithEnvironment("ASPNETCORE_URLS", "http://0.0.0.0:5000")
    .WithHttpEndpoint(targetPort: 5000, name: "http")
    .WithReference(postgresDb);

var keycloakConfigPath = Path.GetFullPath(Path.Combine("..", "..", "keycloak"));

var keycloak = builder.AddContainer("keycloak", "quay.io/keycloak/keycloak:latest")
    .WithArgs(["start-dev", "--import-realm"])
    .WithEnvironment("KEYCLOAK_ADMIN", "admin")
    .WithEnvironment("KEYCLOAK_ADMIN_PASSWORD", "admin")
    .WithEnvironment("KC_HTTP_RELATIVE_PATH", "/auth")
    .WithHttpEndpoint(targetPort: 8080, name: "http")
    .WithBindMount(keycloakConfigPath, "/opt/keycloak/data/import");

var api = builder.AddProject<Projects.Kanban_Api>("api")
    .WithEnvironment("Dab__BaseUrl", dab.GetEndpoint("http"))
    .WithReference(postgresDb);

var web = builder.AddProject<Projects.Kanban_Web>("web")
    .WithEnvironment("Api__BaseUrl", api.GetEndpoint("http"))
    .WithEnvironment("Keycloak__Authority", keycloak.GetEndpoint("http"))
    .WithExternalHttpEndpoints();

builder.Build().Run();
