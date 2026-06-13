using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using KanbanDmz.Domain;
using KanbanDmz.Domain.DTOs;
using KanbanDmz.Web.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace KanbanDmz.Domain.Tests;

public class CategoryCrudTests
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
    public async Task GetCategoriesAsync_ReturnsCategories()
    {
        // Arrange
        var fakeHandler = new FakeHttpMessageHandler();
        var httpClient = new HttpClient(fakeHandler) { BaseAddress = new Uri("http://fake-api/") };
        var service = new KanbanService(httpClient, NullLogger<KanbanService>.Instance);

        var expectedCategories = new List<Category>
        {
            new() { Id = 1, Name = "Bug" },
            new() { Id = 2, Name = "Feature" }
        };

        fakeHandler.ResponseFunc = (req) =>
        {
            if (req.Method == HttpMethod.Get && req.RequestUri!.AbsolutePath == "/Category")
            {
                var responseObj = new DabResponse<Category> { Value = expectedCategories };
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(responseObj), System.Text.Encoding.UTF8, "application/json")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        };

        // Act
        var result = await service.GetCategoriesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Bug", result[0].Name);
        Assert.Equal("Feature", result[1].Name);
    }

    [Fact]
    public async Task CreateCategoryAsync_SendsCorrectPayload_ReturnsCreatedCategory()
    {
        // Arrange
        var fakeHandler = new FakeHttpMessageHandler();
        var httpClient = new HttpClient(fakeHandler) { BaseAddress = new Uri("http://fake-api/") };
        var service = new KanbanService(httpClient, NullLogger<KanbanService>.Instance);

        var expectedCategory = new Category { Id = 4, Name = "Chore" };

        fakeHandler.ResponseFunc = (req) =>
        {
            if (req.Method == HttpMethod.Post && req.RequestUri!.AbsolutePath == "/Category")
            {
                var responseObj = new DabResponse<Category> { Value = [expectedCategory] };
                return new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent(JsonSerializer.Serialize(responseObj), System.Text.Encoding.UTF8, "application/json")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        };

        // Act
        var result = await service.CreateCategoryAsync("Chore");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Id);
        Assert.Equal("Chore", result.Name);
        
        var request = fakeHandler.Requests[0];
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/Category", request.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task UpdateCategoryAsync_SendsPatchRequest_ReturnsTrue()
    {
        // Arrange
        var fakeHandler = new FakeHttpMessageHandler();
        var httpClient = new HttpClient(fakeHandler) { BaseAddress = new Uri("http://fake-api/") };
        var service = new KanbanService(httpClient, NullLogger<KanbanService>.Instance);

        fakeHandler.ResponseFunc = (req) =>
        {
            if (req.Method == HttpMethod.Patch && req.RequestUri!.AbsolutePath == "/Category/id/1")
            {
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        };

        // Act
        var result = await service.UpdateCategoryAsync(new Category { Id = 1, Name = "Refactored Bug" });

        // Assert
        Assert.True(result);
        var request = fakeHandler.Requests[0];
        Assert.Equal(HttpMethod.Patch, request.Method);
        Assert.Equal("/Category/id/1", request.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task DeleteCategoryAsync_SendsDeleteRequest_ReturnsTrue()
    {
        // Arrange
        var fakeHandler = new FakeHttpMessageHandler();
        var httpClient = new HttpClient(fakeHandler) { BaseAddress = new Uri("http://fake-api/") };
        var service = new KanbanService(httpClient, NullLogger<KanbanService>.Instance);

        fakeHandler.ResponseFunc = (req) =>
        {
            if (req.Method == HttpMethod.Delete && req.RequestUri!.AbsolutePath == "/Category/id/3")
            {
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        };

        // Act
        var result = await service.DeleteCategoryAsync(3);

        // Assert
        Assert.True(result);
        var request = fakeHandler.Requests[0];
        Assert.Equal(HttpMethod.Delete, request.Method);
        Assert.Equal("/Category/id/3", request.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GetCardsByCategoryIdAsync_SendsFilterRequest_ReturnsCards()
    {
        // Arrange
        var fakeHandler = new FakeHttpMessageHandler();
        var httpClient = new HttpClient(fakeHandler) { BaseAddress = new Uri("http://fake-api/") };
        var service = new KanbanService(httpClient, NullLogger<KanbanService>.Instance);

        var expectedCards = new List<Card>
        {
            new() { Id = Guid.NewGuid(), Title = "Card 1", CategoryId = 2 },
            new() { Id = Guid.NewGuid(), Title = "Card 2", CategoryId = 2 }
        };

        fakeHandler.ResponseFunc = (req) =>
        {
            var unescapedQuery = Uri.UnescapeDataString(req.RequestUri!.Query);
            if (req.Method == HttpMethod.Get && req.RequestUri!.AbsolutePath == "/Card" && unescapedQuery.Contains("$filter=categoryid eq 2"))
            {
                var responseObj = new DabResponse<Card> { Value = expectedCards };
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(responseObj), System.Text.Encoding.UTF8, "application/json")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        };

        // Act
        var result = await service.GetCardsByCategoryIdAsync(2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Card 1", result[0].Title);
        Assert.Equal("Card 2", result[1].Title);
        
        var request = fakeHandler.Requests[0];
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Contains("$filter=categoryid eq 2", Uri.UnescapeDataString(request.RequestUri!.Query));
    }
}
