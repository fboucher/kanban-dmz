using System.Net.Http.Headers;
using System.Net.Http.Json;
using KanbanDmz.Domain;
using KanbanDmz.Domain.DTOs;

namespace KanbanDmz.Web.Services;

public class KanbanService
{
    private readonly HttpClient _httpClient;
    private readonly UserTokenProvider _tokenProvider;
    private readonly ILogger<KanbanService> _logger;

    public KanbanService(HttpClient httpClient, ILogger<KanbanService> logger, UserTokenProvider? tokenProvider = null)
    {
        _httpClient = httpClient;
        _tokenProvider = tokenProvider ?? new UserTokenProvider();
        _logger = logger;
    }

    private void EnsureAuthHeaders()
    {
        _httpClient.DefaultRequestHeaders.Remove("Authorization");
        _httpClient.DefaultRequestHeaders.Remove("X-MS-CLIENT-PRINCIPAL");
        _httpClient.DefaultRequestHeaders.Remove("X-MS-API-ROLE");

        if (!string.IsNullOrEmpty(_tokenProvider.AccessToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _tokenProvider.AccessToken);

            try
            {
                var parts = _tokenProvider.AccessToken.Split('.');
                if (parts.Length > 1)
                {
                    var payloadJson = Base64UrlDecode(parts[1]);
                    using var doc = System.Text.Json.JsonDocument.Parse(payloadJson);
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

                    var principalJson = System.Text.Json.JsonSerializer.Serialize(principalObj);
                    var principalBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(principalJson));
                    _httpClient.DefaultRequestHeaders.Add("X-MS-CLIENT-PRINCIPAL", principalBase64);
                    _httpClient.DefaultRequestHeaders.Add("X-MS-API-ROLE", "authenticated");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decode access token in KanbanService.");
            }
        }
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

    public async Task<BoardDetailDto?> GetBoardDetailAsync(Guid boardId)
    {
        EnsureAuthHeaders();
        try
        {
            var boardResponse = await _httpClient.GetFromJsonAsync<DabResponse<Board>>($"Board?$filter=id eq {boardId}");
            if (boardResponse == null || boardResponse.Value.Count == 0)
            {
                _logger.LogWarning("Board with ID {BoardId} not found in database via DAB.", boardId);
                return null;
            }
            var board = boardResponse.Value[0];

            var columnsResponse = await _httpClient.GetFromJsonAsync<DabResponse<Column>>($"Column?$filter=boardid eq {boardId}");
            var cardsResponse = await _httpClient.GetFromJsonAsync<DabResponse<Card>>($"Card?$filter=boardid eq {boardId}");
            var categoriesResponse = await _httpClient.GetFromJsonAsync<DabResponse<Category>>("Category");
            var tagsResponse = await _httpClient.GetFromJsonAsync<DabResponse<CardTag>>("CardTag");

            var boardDetail = BoardMapper.MapToDetail(
                board,
                columnsResponse?.Value,
                cardsResponse?.Value,
                categoriesResponse?.Value,
                tagsResponse?.Value
            );

            return boardDetail;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching board details from DAB for Board ID: {BoardId}", boardId);
            throw;
        }
    }

    public async Task<bool> ToggleCardVisibilityAsync(Guid cardId)
    {
        EnsureAuthHeaders();
        try
        {
            // First, fetch the card to get its current IsPublic state
            var cardResponse = await _httpClient.GetFromJsonAsync<DabResponse<Card>>($"Card?$filter=id eq {cardId}");
            if (cardResponse == null || cardResponse.Value.Count == 0)
            {
                _logger.LogWarning("Card with ID {CardId} not found in database via DAB.", cardId);
                return false;
            }
            var card = cardResponse.Value[0];
            var newIsPublic = !card.IsPublic;

            // Now, send a PATCH request to toggle it in DAB
            var response = await _httpClient.PatchAsJsonAsync($"Card/id/{cardId}", new { ispublic = newIsPublic });
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error patching card visibility via DAB. Status: {Status}, Error: {Error}", response.StatusCode, error);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling card visibility for Card ID: {CardId}", cardId);
            throw;
        }
    }
}
