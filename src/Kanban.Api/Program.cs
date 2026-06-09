var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHttpClient("Dab", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Dab:BaseUrl"] ?? "http://dab:5000");
});

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { Status = "Healthy" }));

app.Run();
