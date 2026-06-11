using System.Net.Http.Json;
using KanbanDmz.Domain;
using KanbanDmz.Domain.DTOs;

namespace KanbanDmz.Web.Services;

public class KanbanService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<KanbanService> _logger;

    public KanbanService(HttpClient httpClient, ILogger<KanbanService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<BoardDetailDto?> GetBoardDetailAsync(Guid boardId)
    {
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
