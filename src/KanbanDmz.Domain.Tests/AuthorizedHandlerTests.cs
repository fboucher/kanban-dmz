using System.Net;
using System.Text;
using System.Text.Json;
using KanbanDmz.Web.Services;
using Xunit;

namespace KanbanDmz.Domain.Tests;

public class AuthorizedHandlerTests
{
    private class SimpleHttpMessageHandler : DelegatingHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    [Fact]
    public async Task SendAsync_NoToken_DoesNotAddHeaders()
    {
        // Arrange
        var tokenProvider = new UserTokenProvider();
        var innerHandler = new SimpleHttpMessageHandler();
        var handler = new AuthorizedHandler(tokenProvider)
        {
            InnerHandler = innerHandler
        };
        var client = new HttpClient(handler);

        // Act
        await client.GetAsync("http://example.com/api/test");

        // Assert
        var request = innerHandler.LastRequest;
        Assert.NotNull(request);
        Assert.False(request.Headers.Contains("Authorization"));
        Assert.False(request.Headers.Contains("X-MS-CLIENT-PRINCIPAL"));
        Assert.False(request.Headers.Contains("X-MS-API-ROLE"));
    }

    [Fact]
    public async Task SendAsync_WithValidToken_AddsHeaders()
    {
        // Arrange
        var tokenProvider = new UserTokenProvider();
        // JWT: header.payload.signature
        // Payload: {"sub":"123-id","preferred_username":"frank"}
        var payload = "{\"sub\":\"123-id\",\"preferred_username\":\"frank\"}";
        var payloadBase64Url = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        tokenProvider.AccessToken = $"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.{payloadBase64Url}.sig";

        var innerHandler = new SimpleHttpMessageHandler();
        var handler = new AuthorizedHandler(tokenProvider)
        {
            InnerHandler = innerHandler
        };
        var client = new HttpClient(handler);

        // Act
        await client.GetAsync("http://example.com/api/test");

        // Assert
        var request = innerHandler.LastRequest;
        Assert.NotNull(request);
        
        // Assert Authorization Header
        Assert.True(request.Headers.Contains("Authorization"));
        Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
        Assert.Equal(tokenProvider.AccessToken, request.Headers.Authorization?.Parameter);

        // Assert X-MS-API-ROLE Header
        Assert.True(request.Headers.Contains("X-MS-API-ROLE"));
        Assert.Equal("authenticated", request.Headers.GetValues("X-MS-API-ROLE").First());

        // Assert X-MS-CLIENT-PRINCIPAL Header
        Assert.True(request.Headers.Contains("X-MS-CLIENT-PRINCIPAL"));
        var principalHeaderBase64 = request.Headers.GetValues("X-MS-CLIENT-PRINCIPAL").First();
        var principalJson = Encoding.UTF8.GetString(Convert.FromBase64String(principalHeaderBase64));
        
        using var doc = JsonDocument.Parse(principalJson);
        var root = doc.RootElement;
        
        Assert.Equal("keycloak", root.GetProperty("identityProvider").GetString());
        Assert.Equal("123-id", root.GetProperty("userId").GetString());
        Assert.Equal("frank", root.GetProperty("userDetails").GetString());
        
        var roles = root.GetProperty("userRoles").EnumerateArray().Select(r => r.GetString()).ToList();
        Assert.Contains("anonymous", roles);
        Assert.Contains("authenticated", roles);
    }
}
