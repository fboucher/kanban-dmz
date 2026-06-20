using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using KanbanDmz.Domain;
using KanbanDmz.Domain.DTOs;
using KanbanDmz.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Kanban.Api.IntegrationTests;

public class IntegrationTests : IClassFixture<DabTestFixture>
{
    private readonly DabTestFixture _fixture;

    public IntegrationTests(DabTestFixture fixture)
    {
        _fixture = fixture;
    }

    private class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger, UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey("X-Test-Auth"))
            {
                return Task.FromResult(AuthenticateResult.Fail("Not authenticated"));
            }

            var claims = new[] { 
                new Claim(ClaimTypes.Name, "TestUser"), 
                new Claim("preferred_username", "testuser") 
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            
            var authProperties = new AuthenticationProperties();
            authProperties.StoreTokens(new[]
            {
                new AuthenticationToken
                {
                    Name = "access_token",
                    Value = "header.eyJzdWIiOiJ0ZXN0dXNlci1pZCIsInByZWZlcnJlZF91c2VybmFtZSI6InRlc3R1c2VyIn0.signature"
                }
            });

            var ticket = new AuthenticationTicket(principal, authProperties, "Test");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    private WebApplicationFactory<Program> CreateFactory()
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Override authentication
                    services.AddAuthentication(options =>
                    {
                        options.DefaultScheme = "Test";
                        options.DefaultAuthenticateScheme = "Test";
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });

                    // Re-register KanbanService to point to our Testcontainers DAB
                    var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(KanbanService));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddScoped(sp =>
                    {
                        var client = new HttpClient { BaseAddress = new Uri(_fixture.DabBaseUrl) };
                        var tokenProvider = sp.GetService<UserTokenProvider>();
                        return new KanbanService(client, sp.GetRequiredService<ILogger<KanbanService>>(), tokenProvider);
                    });
                });
            });
    }

    private HttpClient CreateClient(WebApplicationFactory<Program> factory, bool authenticated)
    {
        var client = factory.CreateClient();
        if (authenticated)
        {
            client.DefaultRequestHeaders.Add("X-Test-Auth", "true");
        }
        return client;
    }

    private void AuthenticateDirectClient(HttpClient client)
    {
        var principalObj = new
        {
            identityProvider = "keycloak",
            userId = "testuser-id",
            userDetails = "testuser",
            userRoles = new[] { "anonymous", "authenticated" }
        };

        var principalJson = JsonSerializer.Serialize(principalObj);
        var principalBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(principalJson));
        client.DefaultRequestHeaders.Remove("X-MS-CLIENT-PRINCIPAL");
        client.DefaultRequestHeaders.Remove("X-MS-API-ROLE");
        client.DefaultRequestHeaders.Add("X-MS-CLIENT-PRINCIPAL", principalBase64);
        client.DefaultRequestHeaders.Add("X-MS-API-ROLE", "authenticated");
    }

    [Fact]
    public async Task CreateBoard_Unauthenticated_Returns401()
    {
        // Arrange
        using var factory = CreateFactory();
        using var client = CreateClient(factory, authenticated: false);
        var payload = new BoardCreateDto { Name = "New Board", IsPublic = true };

        // Act
        var response = await client.PostAsJsonAsync("/api/boards", payload);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateBoard_Authenticated_Returns201AndCreates5DefaultColumns()
    {
        // Arrange
        using var factory = CreateFactory();
        using var client = CreateClient(factory, authenticated: true);
        var payload = new BoardCreateDto { Name = "Integration Test Board", IsPublic = true };

        // Act
        var response = await client.PostAsJsonAsync("/api/boards", payload);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var createdBoard = await response.Content.ReadFromJsonAsync<Board>();
        Assert.NotNull(createdBoard);
        Assert.NotEqual(Guid.Empty, createdBoard.Id);
        Assert.Equal("Integration Test Board", createdBoard.Name);

        // Verify that 5 default columns were created in the database via the DAB container
        using var checkClient = new HttpClient { BaseAddress = new Uri(_fixture.DabBaseUrl) };
        AuthenticateDirectClient(checkClient);
        var columnsResponse = await checkClient.GetFromJsonAsync<DabResponse<Column>>($"Column?$filter=boardid eq {createdBoard.Id}");
        Assert.NotNull(columnsResponse);
        Assert.Equal(5, columnsResponse.Value.Count);
        
        var columnNames = columnsResponse.Value.OrderBy(c => c.SortOrder).Select(c => c.Name).ToList();
        var expectedNames = new[] { "Backlog", "To Do", "In Progress", "Pending", "Done" };
        Assert.Equal(expectedNames, columnNames);
    }

    [Fact]
    public async Task ToggleCardVisibility_Unauthenticated_Returns401()
    {
        // Arrange
        using var factory = CreateFactory();
        using var client = CreateClient(factory, authenticated: false);
        var cardId = Guid.NewGuid();

        // Act
        var response = await client.PostAsync($"/api/cards/{cardId}/visibility", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ToggleCardVisibility_Authenticated_ChangesIsPublicFlag()
    {
        // Arrange
        using var factory = CreateFactory();
        using var client = CreateClient(factory, authenticated: true);
        
        // Seed a board, column, and card directly into DAB first
        using var seedClient = new HttpClient { BaseAddress = new Uri(_fixture.DabBaseUrl) };
        AuthenticateDirectClient(seedClient);
        
        // Post a board
        var boardResponse = await seedClient.PostAsJsonAsync("Board", new { name = "Seed Board", ispublic = true });
        var board = (await boardResponse.Content.ReadFromJsonAsync<DabResponse<Board>>())!.Value[0];

        // Post a column
        var columnResponse = await seedClient.PostAsJsonAsync("Column", new { boardid = board.Id, name = "Seed Column", sortorder = 0 });
        var column = (await columnResponse.Content.ReadFromJsonAsync<DabResponse<Column>>())!.Value[0];

        // Post a card (initially public)
        var cardResponse = await seedClient.PostAsJsonAsync("Card", new {
            boardid = board.Id,
            columnid = column.Id,
            title = "Test Card",
            publicdescription = "Public desc",
            categoryid = 1,
            ispublic = true
        });
        var card = (await cardResponse.Content.ReadFromJsonAsync<DabResponse<Card>>())!.Value[0];

        // Act & Assert 1: Toggle public -> private
        var toggleResponse1 = await client.PostAsync($"/api/cards/{card.Id}/visibility", null);
        Assert.Equal(HttpStatusCode.OK, toggleResponse1.StatusCode);

        // Check if card is private now
        var checkCard1 = await seedClient.GetFromJsonAsync<DabResponse<Card>>($"Card?$filter=id eq {card.Id}");
        Assert.False(checkCard1!.Value[0].IsPublic);

        // Act & Assert 2: Toggle private -> public
        var toggleResponse2 = await client.PostAsync($"/api/cards/{card.Id}/visibility", null);
        Assert.Equal(HttpStatusCode.OK, toggleResponse2.StatusCode);

        // Check if card is public now
        var checkCard2 = await seedClient.GetFromJsonAsync<DabResponse<Card>>($"Card?$filter=id eq {card.Id}");
        Assert.True(checkCard2!.Value[0].IsPublic);
    }

    [Fact]
    public async Task Comments_PrivacyPolicies_Enforced()
    {
        // Arrange
        using var factory = CreateFactory();
        
        // We will seed a public card and a private card with comments
        using var seedClient = new HttpClient { BaseAddress = new Uri(_fixture.DabBaseUrl) };
        AuthenticateDirectClient(seedClient);

        // 1. Post a board
        var boardResponse = await seedClient.PostAsJsonAsync("Board", new { name = "Comments Test Board", ispublic = true });
        var board = (await boardResponse.Content.ReadFromJsonAsync<DabResponse<Board>>())!.Value[0];

        // 2. Post a column
        var columnResponse = await seedClient.PostAsJsonAsync("Column", new { boardid = board.Id, name = "Comments Col", sortorder = 0 });
        var column = (await columnResponse.Content.ReadFromJsonAsync<DabResponse<Column>>())!.Value[0];

        // 3. Post a public card and a private card
        var publicCardResponse = await seedClient.PostAsJsonAsync("Card", new {
            boardid = board.Id,
            columnid = column.Id,
            title = "Public Card",
            publicdescription = "Public card desc",
            categoryid = 1,
            ispublic = true
        });
        var publicCard = (await publicCardResponse.Content.ReadFromJsonAsync<DabResponse<Card>>())!.Value[0];

        var privateCardResponse = await seedClient.PostAsJsonAsync("Card", new {
            boardid = board.Id,
            columnid = column.Id,
            title = "Private Card",
            publicdescription = "Private card desc",
            categoryid = 1,
            ispublic = false
        });
        var privateCard = (await privateCardResponse.Content.ReadFromJsonAsync<DabResponse<Card>>())!.Value[0];

        // 4. Post comments to both cards
        await seedClient.PostAsJsonAsync("CardComment", new {
            cardid = publicCard.Id,
            content = "Comment on public card",
            createdby = "Alice"
        });

        await seedClient.PostAsJsonAsync("CardComment", new {
            cardid = privateCard.Id,
            content = "Comment on private card",
            createdby = "Bob"
        });

        // 5. Query comments as anonymous user (direct client without auth headers)
        using var anonymousClient = new HttpClient { BaseAddress = new Uri(_fixture.DabBaseUrl) };
        
        // Fetch comments for public card as anonymous
        var publicResponse = await anonymousClient.GetAsync($"CardComment?$filter=cardid eq {publicCard.Id}");
        var publicContent = await publicResponse.Content.ReadAsStringAsync();
        if (!publicResponse.IsSuccessStatusCode)
        {
            throw new Exception($"DAB failed with status {publicResponse.StatusCode}. Error: {publicContent}");
        }
        var publicCommentsResponse = JsonSerializer.Deserialize<DabResponse<CardComment>>(publicContent, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        Assert.NotNull(publicCommentsResponse);
        Assert.Single(publicCommentsResponse.Value);
        Assert.Equal("Comment on public card", publicCommentsResponse.Value[0].Content);

        // Fetch comments for private card as anonymous (should be filtered out by policy, yielding empty)
        var privateCommentsResponse = await anonymousClient.GetFromJsonAsync<DabResponse<CardComment>>($"CardComment?$filter=cardid eq {privateCard.Id}");
        Assert.NotNull(privateCommentsResponse);
        Assert.Empty(privateCommentsResponse.Value);
    }
}
