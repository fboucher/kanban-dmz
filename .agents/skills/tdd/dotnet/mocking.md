# When to Mock

Mock at **system boundaries** only:

- External APIs (payment, email, etc.)
- Databases (sometimes - prefer a real test DB or in-memory EF Core)
- Time/randomness (`TimeProvider`, `ISystemClock`)
- File system (sometimes)

Don't mock:

- Your own classes/services
- Internal collaborators
- Anything you control

## Designing for Mockability

At system boundaries, design interfaces that are easy to mock.
Use NSubstitute (preferred) or Moq.

**1. Use dependency injection**

Register dependencies via the constructor; never `new` them internally:

```csharp
// Easy to mock
public class PaymentService(IPaymentClient paymentClient)
{
    public Task<PaymentResult> ProcessAsync(Order order)
        => paymentClient.ChargeAsync(order.Total);
}

// Hard to mock
public class PaymentService
{
    public Task<PaymentResult> ProcessAsync(Order order)
    {
        var client = new StripeClient(Environment.GetEnvironmentVariable("STRIPE_KEY")!);
        return client.ChargeAsync(order.Total);
    }
}
```

**2. Prefer typed interfaces over generic wrappers**

Create specific interface methods for each external operation instead of one generic method with conditional logic:

```csharp
// GOOD: Each method is independently substitutable
public interface IOrderApiClient
{
    Task<User>   GetUserAsync(Guid id);
    Task<Order[]> GetOrdersAsync(Guid userId);
    Task<Order>  CreateOrderAsync(CreateOrderRequest request);
}

// BAD: Substituting requires conditional logic inside the fake
public interface IApiClient
{
    Task<string> SendAsync(string endpoint, HttpMethod method, object? body = null);
}
```

The typed interface approach means:
- Each substitute returns one specific type
- No conditional logic in test setup
- Easier to see which operations a test exercises
- Full compile-time type safety

**3. NSubstitute quick reference**

```csharp
var gateway = Substitute.For<IPaymentGateway>();

// Arrange
gateway.ChargeAsync(Arg.Any<decimal>())
       .Returns(Task.FromResult(new PaymentResult { Success = true }));

// Assert interaction (only when the call itself IS the behavior)
await gateway.Received(1).ChargeAsync(order.Total);
```
