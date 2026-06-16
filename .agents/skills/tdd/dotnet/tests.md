# Good and Bad Tests

Use **xUnit** + **FluentAssertions**. For ASP.NET Core endpoint tests use `WebApplicationFactory`.

## Good Tests

**Integration-style**: Test through real interfaces, not mocks of internal parts.

```csharp
// GOOD: Tests observable behavior
[Fact]
public async Task UserCanCheckoutWithValidCart()
{
    var cart = CartBuilder.WithProduct(product);
    var result = await _checkoutService.CheckoutAsync(cart, validPaymentMethod);
    result.Status.Should().Be(OrderStatus.Confirmed);
}
```

Characteristics:

- Tests behavior callers care about
- Uses public API only
- Survives internal refactors
- Describes WHAT, not HOW
- One logical assertion per test

## Bad Tests

**Implementation-detail tests**: Coupled to internal structure.

```csharp
// BAD: Tests implementation details
[Fact]
public async Task Checkout_CallsPaymentServiceProcess()
{
    var mockPayment = Substitute.For<IPaymentService>();
    await _checkoutService.CheckoutAsync(cart, payment);
    await mockPayment.Received().ProcessAsync(cart.Total); // verifying internal wiring
}
```

Red flags:

- Mocking internal collaborators
- Testing private methods
- Asserting on call counts/order of internal calls
- Test breaks when refactoring without behavior change
- Test name describes HOW not WHAT
- Verifying through external means instead of interface

```csharp
// BAD: Bypasses interface to verify
[Fact]
public async Task CreateUser_SavestoDatabase()
{
    await _userService.CreateAsync(new CreateUserRequest { Name = "Alice" });
    var row = await _dbContext.Users.FirstOrDefaultAsync(u => u.Name == "Alice");
    row.Should().NotBeNull();
}

// GOOD: Verifies through the public interface
[Fact]
public async Task CreateUser_MakesUserRetrievable()
{
    var created = await _userService.CreateAsync(new CreateUserRequest { Name = "Alice" });
    var retrieved = await _userService.GetAsync(created.Id);
    retrieved.Name.Should().Be("Alice");
}
```

## ASP.NET Core endpoint tests

Use `WebApplicationFactory` to test the full HTTP stack without mocking internals:

```csharp
public class OrdersEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task PostOrder_Returns201WithOrderId()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/orders", new { ProductId = 1, Quantity = 2 });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<OrderResponse>();
        body!.OrderId.Should().NotBeEmpty();
    }
}
```
