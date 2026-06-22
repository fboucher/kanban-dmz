using KanbanDmz.Web.Components;
using KanbanDmz.Domain;
using KanbanDmz.Domain.DTOs;
using KanbanDmz.Web.Services;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddFluentUIComponents();
builder.Services.AddHttpContextAccessor();

// Register token provider and authorization handler
builder.Services.AddScoped<UserTokenProvider>();
builder.Services.AddTransient<AuthorizedHandler>();

builder.Services.AddHttpClient<KanbanService>(client =>
{
    client.BaseAddress = new Uri("https+http://dab/api/");
});

builder.Services.AddCascadingAuthenticationState();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    var authority = builder.Configuration["Keycloak:Authority"] ?? "http://localhost:8080";
    options.Authority = $"{authority}/auth/realms/kanban-dmz";
    options.ClientId = builder.Configuration["Keycloak:ClientId"] ?? "kanban-web";
    options.ClientSecret = builder.Configuration["Keycloak:ClientSecret"];
    options.ResponseType = "code";
    options.SaveTokens = true;
    options.RequireHttpsMetadata = false;
    options.MetadataAddress = builder.Configuration["Keycloak:MetadataAddress"] ?? $"{authority}/auth/realms/kanban-dmz/.well-known/openid-configuration";

    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        NameClaimType = "preferred_username",
        RoleClaimType = "roles"
    };
});

var app = builder.Build();

var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost,
    ForwardLimit = null
};
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        var statusCodePagesFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IStatusCodePagesFeature>();
        if (statusCodePagesFeature != null)
        {
            statusCodePagesFeature.Enabled = false;
        }
    }
    await next(context);
});
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapGet("/login", async (HttpContext context, string? redirectUri) =>
{
    await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties
    {
        RedirectUri = string.IsNullOrEmpty(redirectUri) ? "/" : redirectUri
    });
});

app.MapGet("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties
    {
        RedirectUri = "/"
    });
});

app.MapPost("/api/cards/{id}/visibility", async (Guid id, HttpContext httpContext, KanbanService kanbanService, UserTokenProvider tokenProvider) =>
{
    if (httpContext.User.Identity?.IsAuthenticated != true)
    {
        return Results.Unauthorized();
    }

    tokenProvider.AccessToken = await httpContext.GetTokenAsync("access_token");

    var success = await kanbanService.ToggleCardVisibilityAsync(id);
    return success ? Results.Ok() : Results.NotFound();
}).DisableAntiforgery();

app.MapPost("/api/boards", async (BoardCreateDto dto, HttpContext httpContext, KanbanService kanbanService, UserTokenProvider tokenProvider) =>
{
    if (httpContext.User.Identity?.IsAuthenticated != true)
    {
        return Results.Unauthorized();
    }

    tokenProvider.AccessToken = await httpContext.GetTokenAsync("access_token");

    var board = new Board
    {
        Name = dto.Name,
        IsPublic = dto.IsPublic
    };

    var createdBoard = await kanbanService.CreateBoardAsync(board);
    if (createdBoard == null)
    {
        return Results.BadRequest("Failed to create board.");
    }

    var defaultColumns = new[] { "Backlog", "To Do", "In Progress", "Pending", "Done" };
    for (int i = 0; i < defaultColumns.Length; i++)
    {
        var col = new Column
        {
            BoardId = createdBoard.Id,
            Name = defaultColumns[i],
            SortOrder = i
        };
        await kanbanService.CreateColumnAsync(col);
    }

    return Results.Created($"/api/boards/{createdBoard.Id}", createdBoard);
}).DisableAntiforgery();

app.Run();

public partial class Program { }




