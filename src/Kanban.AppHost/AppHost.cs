var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithBindMount(Path.GetFullPath(Path.Combine("..", "..", "db")), "/docker-entrypoint-initdb.d")
    .WithEnvironment("POSTGRES_DB", "kanban");

var postgresDb = postgres.AddDatabase("kanban");

var dabConfigFile = Path.GetFullPath(Path.Combine("..", "..", "dab", "dab-config.json"));

var dab = builder.AddDataAPIBuilder("dab", dabConfigFile)
    .WithReference(postgresDb);

var keycloakConfigPath = Path.GetFullPath(Path.Combine("..", "..", "keycloak"));

var keycloakAdmin = builder.AddParameter("keycloak-admin");
var keycloakAdminPassword = builder.AddParameter("keycloak-admin-password", secret: true);

var keycloak = builder.AddContainer("keycloak", "quay.io/keycloak/keycloak:latest")
    .WithArgs(["start-dev", "--import-realm"])
    .WithEnvironment("KEYCLOAK_ADMIN", keycloakAdmin)
    .WithEnvironment("KEYCLOAK_ADMIN_PASSWORD", keycloakAdminPassword)
    .WithEnvironment("KC_HTTP_RELATIVE_PATH", "/auth")
    .WithHttpEndpoint(targetPort: 8080, name: "http")
    .WithBindMount(keycloakConfigPath, "/opt/keycloak/data/import");

var web = builder.AddProject<Projects.Kanban_Web>("web")
    .WithEnvironment("Api__BaseUrl", dab.GetEndpoint("http"))
    .WithEnvironment("Keycloak__Authority", keycloak.GetEndpoint("http"))
    .WithExternalHttpEndpoints();

builder.Build().Run();
