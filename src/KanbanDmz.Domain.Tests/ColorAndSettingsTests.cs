using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using KanbanDmz.Domain;
using KanbanDmz.Domain.DTOs;
using KanbanDmz.Web.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace KanbanDmz.Domain.Tests;

public class ColorAndSettingsTests
{
    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];
        public Func<HttpRequestMessage, HttpResponseMessage>? ResponseFunc { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            if (ResponseFunc != null)
            {
                return Task.FromResult(ResponseFunc(request));
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }

    [Fact]
    public async Task UpdateBoardAsync_SendsCorrectPayload_ReturnsTrue()
    {
        // Arrange
        var fakeHandler = new FakeHttpMessageHandler();
        var httpClient = new HttpClient(fakeHandler) { BaseAddress = new Uri("http://fake-api/") };
        var service = new KanbanService(httpClient, NullLogger<KanbanService>.Instance);

        var board = new Board
        {
            Id = Guid.NewGuid(),
            Name = "Colored Board",
            IsPublic = false,
            BackgroundColor = "#f3f4f6"
        };

        fakeHandler.ResponseFunc = (req) =>
        {
            if (req.Method == HttpMethod.Patch && req.RequestUri!.AbsolutePath == $"/Board/id/{board.Id}")
            {
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        };

        // Act
        var result = await service.UpdateBoardAsync(board);

        // Assert
        Assert.True(result);
        Assert.Single(fakeHandler.Requests);
        var request = fakeHandler.Requests[0];
        Assert.Equal(HttpMethod.Patch, request.Method);
        Assert.Equal($"/Board/id/{board.Id}", request.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GetTagsAsync_ReturnsList()
    {
        // Arrange
        var fakeHandler = new FakeHttpMessageHandler();
        var httpClient = new HttpClient(fakeHandler) { BaseAddress = new Uri("http://fake-api/") };
        var service = new KanbanService(httpClient, NullLogger<KanbanService>.Instance);

        var expectedTags = new List<Tag>
        {
            new() { Name = "test-tag", Color = "#ff0000" }
        };

        fakeHandler.ResponseFunc = (req) =>
        {
            if (req.Method == HttpMethod.Get && req.RequestUri!.AbsolutePath == "/Tag")
            {
                var responseObj = new DabResponse<Tag> { Value = expectedTags };
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(responseObj), System.Text.Encoding.UTF8, "application/json")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        };

        // Act
        var result = await service.GetTagsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("test-tag", result[0].Name);
        Assert.Equal("#ff0000", result[0].Color);
    }

    [Fact]
    public async Task UpdateTagColorAsync_SendsPatchRequest_ReturnsTrue()
    {
        // Arrange
        var fakeHandler = new FakeHttpMessageHandler();
        var httpClient = new HttpClient(fakeHandler) { BaseAddress = new Uri("http://fake-api/") };
        var service = new KanbanService(httpClient, NullLogger<KanbanService>.Instance);

        fakeHandler.ResponseFunc = (req) =>
        {
            if (req.Method == HttpMethod.Patch && req.RequestUri!.AbsolutePath == "/Tag/name/my-tag")
            {
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        };

        // Act
        var result = await service.UpdateTagColorAsync("my-tag", "#00ff00");

        // Assert
        Assert.True(result);
        Assert.Single(fakeHandler.Requests);
        var request = fakeHandler.Requests[0];
        Assert.Equal(HttpMethod.Patch, request.Method);
        Assert.Equal("/Tag/name/my-tag", request.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task CreateTagAsync_SendsPostRequest_ReturnsCreatedTag()
    {
        // Arrange
        var fakeHandler = new FakeHttpMessageHandler();
        var httpClient = new HttpClient(fakeHandler) { BaseAddress = new Uri("http://fake-api/") };
        var service = new KanbanService(httpClient, NullLogger<KanbanService>.Instance);

        var createdTag = new Tag { Name = "brand-new", Color = "#0000ff" };

        fakeHandler.ResponseFunc = (req) =>
        {
            if (req.Method == HttpMethod.Post && req.RequestUri!.AbsolutePath == "/Tag")
            {
                var responseObj = new DabResponse<Tag> { Value = [createdTag] };
                return new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent(JsonSerializer.Serialize(responseObj), System.Text.Encoding.UTF8, "application/json")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        };

        // Act
        var result = await service.CreateTagAsync("brand-new", "#0000ff");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("brand-new", result.Name);
        Assert.Equal("#0000ff", result.Color);
    }

    [Fact]
    public async Task DeleteTagAsync_SendsDeleteRequest_ReturnsTrue()
    {
        // Arrange
        var fakeHandler = new FakeHttpMessageHandler();
        var httpClient = new HttpClient(fakeHandler) { BaseAddress = new Uri("http://fake-api/") };
        var service = new KanbanService(httpClient, NullLogger<KanbanService>.Instance);

        fakeHandler.ResponseFunc = (req) =>
        {
            if (req.Method == HttpMethod.Delete && req.RequestUri!.AbsolutePath == "/Tag/name/doomed-tag")
            {
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        };

        // Act
        var result = await service.DeleteTagAsync("doomed-tag");

        // Assert
        Assert.True(result);
        Assert.Single(fakeHandler.Requests);
        var request = fakeHandler.Requests[0];
        Assert.Equal(HttpMethod.Delete, request.Method);
    }

    [Fact]
    public async Task GetBoardsAsync_SendsGetRequest_ReturnsBoardsList()
    {
        // Arrange
        var fakeHandler = new FakeHttpMessageHandler();
        var httpClient = new HttpClient(fakeHandler) { BaseAddress = new Uri("http://fake-api/") };
        var service = new KanbanService(httpClient, NullLogger<KanbanService>.Instance);

        var expectedBoards = new List<Board>
        {
            new() { Id = Guid.NewGuid(), Name = "Board A" }
        };

        fakeHandler.ResponseFunc = (req) =>
        {
            if (req.Method == HttpMethod.Get && req.RequestUri!.AbsolutePath == "/Board")
            {
                var responseObj = new DabResponse<Board> { Value = expectedBoards };
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(responseObj), System.Text.Encoding.UTF8, "application/json")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        };

        // Act
        var result = await service.GetBoardsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Board A", result[0].Name);
    }

    [Fact]
    public async Task DeleteBoardAsync_SendsDeleteRequest_ReturnsTrue()
    {
        // Arrange
        var fakeHandler = new FakeHttpMessageHandler();
        var httpClient = new HttpClient(fakeHandler) { BaseAddress = new Uri("http://fake-api/") };
        var service = new KanbanService(httpClient, NullLogger<KanbanService>.Instance);

        var boardId = Guid.NewGuid();

        fakeHandler.ResponseFunc = (req) =>
        {
            if (req.Method == HttpMethod.Delete && req.RequestUri!.AbsolutePath == $"/Board/id/{boardId}")
            {
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        };

        // Act
        var result = await service.DeleteBoardAsync(boardId);

        // Assert
        Assert.True(result);
    }
}
