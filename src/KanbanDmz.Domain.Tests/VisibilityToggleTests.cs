using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using KanbanDmz.Domain;
using KanbanDmz.Domain.DTOs;
using KanbanDmz.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace KanbanDmz.Domain.Tests;

public class VisibilityToggleTests
{
    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, Task<HttpResponseMessage>>? HandlerFunc { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (HandlerFunc != null)
            {
                return await HandlerFunc(request);
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }
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
            var ticket = new AuthenticationTicket(principal, "Test");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    [Fact]
    public async Task PostVisibility_AnonymousUser_ReturnsUnauthorized()
    {
        // Arrange
        var cardId = Guid.NewGuid();
        var fakeHandler = new FakeHttpMessageHandler();
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Override authentication scheme
                    services.AddAuthentication(options =>
                    {
                        options.DefaultScheme = "Test";
                        options.DefaultAuthenticateScheme = "Test";
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });

                    // Swap KanbanService with a mock that uses our fake handler
                    var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(KanbanService));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }
                    var httpClient = new HttpClient(fakeHandler)
                    {
                        BaseAddress = new Uri("http://fake-dab/")
                    };
                    services.AddScoped(sp => new KanbanService(httpClient, sp.GetRequiredService<ILogger<KanbanService>>()));
                });
            });

        var client = factory.CreateClient();

        // Act
        // Make request WITHOUT X-Test-Auth header
        var response = await client.PostAsync($"/api/cards/{cardId}/visibility", null);

        // Assert
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Unauthorized, 
            $"Expected Unauthorized, but got {response.StatusCode}. Body: {responseContent}");
    }

    [Fact]
    public async Task PostVisibility_AuthenticatedUser_CardExists_TogglesAndReturnsOk()
    {
        // Arrange
        var cardId = Guid.NewGuid();
        var fakeHandler = new FakeHttpMessageHandler();
        
        // Setup mock response from DAB
        fakeHandler.HandlerFunc = async (request) =>
        {
            // Get card query
            if (request.Method == HttpMethod.Get && Uri.UnescapeDataString(request.RequestUri!.Query).Contains("id eq"))
            {
                var card = new Card
                {
                    Id = cardId,
                    Title = "Test Card",
                    IsPublic = true
                };
                var dabResponse = new DabResponse<Card> { Value = new List<Card> { card } };
                var json = JsonSerializer.Serialize(dabResponse);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                };
            }
            // Patch card visibility
            if (request.Method == HttpMethod.Patch && request.RequestUri!.AbsolutePath.Contains($"Card/id/{cardId}"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        };

        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddAuthentication(options =>
                    {
                        options.DefaultScheme = "Test";
                        options.DefaultAuthenticateScheme = "Test";
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });

                    var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(KanbanService));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }
                    var httpClient = new HttpClient(fakeHandler)
                    {
                        BaseAddress = new Uri("http://fake-dab/")
                    };
                    services.AddScoped(sp => new KanbanService(httpClient, sp.GetRequiredService<ILogger<KanbanService>>()));
                });
            });

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Auth", "true");

        // Act
        var response = await client.PostAsync($"/api/cards/{cardId}/visibility", null);

        // Assert
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, 
            $"Expected OK, but got {response.StatusCode}. Body: {responseContent}");
    }

    [Fact]
    public async Task PostVisibility_AuthenticatedUser_CardDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var cardId = Guid.NewGuid();
        var fakeHandler = new FakeHttpMessageHandler();
        
        // Setup mock response from DAB to return empty list (card not found)
        fakeHandler.HandlerFunc = async (request) =>
        {
            if (request.Method == HttpMethod.Get && Uri.UnescapeDataString(request.RequestUri!.Query).Contains("id eq"))
            {
                var dabResponse = new DabResponse<Card> { Value = new List<Card>() };
                var json = JsonSerializer.Serialize(dabResponse);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        };

        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddAuthentication(options =>
                    {
                        options.DefaultScheme = "Test";
                        options.DefaultAuthenticateScheme = "Test";
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });

                    var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(KanbanService));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }
                    var httpClient = new HttpClient(fakeHandler)
                    {
                        BaseAddress = new Uri("http://fake-dab/")
                    };
                    services.AddScoped(sp => new KanbanService(httpClient, sp.GetRequiredService<ILogger<KanbanService>>()));
                });
            });

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Auth", "true");

        // Act
        var response = await client.PostAsync($"/api/cards/{cardId}/visibility", null);

        // Assert
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.NotFound, 
            $"Expected NotFound, but got {response.StatusCode}. Body: {responseContent}");
    }
}
