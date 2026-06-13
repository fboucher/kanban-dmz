using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Logging;

namespace KanbanDmz.Web.Services;

public class AuthorizedHandler : DelegatingHandler
{
    private readonly UserTokenProvider _tokenProvider;
    private readonly ILogger<AuthorizedHandler> _logger;

    public AuthorizedHandler(UserTokenProvider tokenProvider, ILogger<AuthorizedHandler>? logger = null)
    {
        _tokenProvider = tokenProvider;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<AuthorizedHandler>.Instance;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("AuthorizedHandler: Sending request to {Uri}. AccessToken is null or empty? {IsNullOrEmpty}, length: {Length}", 
            request.RequestUri, string.IsNullOrEmpty(_tokenProvider.AccessToken), _tokenProvider.AccessToken?.Length ?? 0);

        if (!string.IsNullOrEmpty(_tokenProvider.AccessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenProvider.AccessToken);

            try
            {
                var parts = _tokenProvider.AccessToken.Split('.');
                if (parts.Length > 1)
                {
                    var payloadJson = Base64UrlDecode(parts[1]);
                    using var doc = JsonDocument.Parse(payloadJson);
                    var root = doc.RootElement;

                    var userId = root.TryGetProperty("sub", out var subEl) ? subEl.GetString() : "unknown";
                    var userDetails = root.TryGetProperty("preferred_username", out var nameEl) ? nameEl.GetString() : "unknown";

                    var principalObj = new
                    {
                        identityProvider = "keycloak",
                        userId = userId,
                        userDetails = userDetails,
                        userRoles = new[] { "anonymous", "authenticated" }
                    };

                    var principalJson = JsonSerializer.Serialize(principalObj);
                    var principalBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(principalJson));
                    request.Headers.Add("X-MS-CLIENT-PRINCIPAL", principalBase64);
                    request.Headers.Add("X-MS-API-ROLE", "authenticated");

                    _logger.LogInformation("AuthorizedHandler: Set X-MS-CLIENT-PRINCIPAL for user {Username} (ID: {UserId}) and role authenticated", userDetails, userId);
                }
                else
                {
                    _logger.LogWarning("AuthorizedHandler: AccessToken is not a valid JWT (less than 2 parts).");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AuthorizedHandler: Failed to decode JWT and set headers.");
                // Fallback or ignore decoding failure if token is malformed
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private static byte[] Base64UrlDecode(string input)
    {
        string base64 = input.Replace('-', '+').Replace('_', '/');
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
}
