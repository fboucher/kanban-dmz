# When to Mock

Mock at **system boundaries** only:

- External APIs (payment, email, etc.)
- Databases (sometimes — prefer a real test DB or SQLite in-memory)
- Time/randomness (`datetime.now`, `random`)
- File system (sometimes)

Don't mock:

- Your own classes/services
- Internal collaborators
- Anything you control

## Designing for Mockability

**1. Use dependency injection**

Accept dependencies as constructor or function parameters; never instantiate them internally:

```python
# Easy to mock
class PaymentService:
    def __init__(self, client: PaymentClient) -> None:
        self._client = client

    def process(self, order: Order) -> PaymentResult:
        return self._client.charge(order.total)

# Hard to mock
class PaymentService:
    def process(self, order: Order) -> PaymentResult:
        client = StripeClient(os.environ["STRIPE_KEY"])
        return client.charge(order.total)
```

**2. Prefer typed protocols over generic wrappers**

```python
# GOOD: Each method is independently mockable
class OrderApiClient(Protocol):
    def get_user(self, id: str) -> User: ...
    def get_orders(self, user_id: str) -> list[Order]: ...
    def create_order(self, req: CreateOrderRequest) -> Order: ...

# BAD: Mocking requires conditional logic inside the fake
class ApiClient(Protocol):
    def send(self, endpoint: str, method: str, body: object = None) -> str: ...
```

## Mocking strategies

**pytest-mock fixture (preferred)**

```python
def test_sends_confirmation_email(user_service, mocker):
    mock_email = mocker.patch("app.services.email_client.send")
    user_service.create(CreateUserRequest(name="Alice", email="alice@example.com"))
    mock_email.assert_called_once_with(to="alice@example.com")
```

**unittest.mock.patch as context manager**

```python
from unittest.mock import patch, MagicMock

def test_payment_gateway_called():
    with patch("app.services.stripe_client.charge") as mock_charge:
        mock_charge.return_value = PaymentResult(success=True)
        result = payment_service.process(order)
    assert result.success is True
```

**AsyncMock for async code**

```python
from unittest.mock import AsyncMock

async def test_async_checkout(mocker):
    mock_client = AsyncMock()
    mock_client.charge.return_value = PaymentResult(success=True)
    service = PaymentService(client=mock_client)
    result = await service.process(order)
    assert result.success is True
```

**respx — mock httpx calls at the network level (preferred for HTTP boundaries)**

Intercepts `httpx` requests without touching application code:

```python
import respx
import httpx

@respx.mock
def test_external_api_call():
    respx.post("https://api.stripe.com/charge").mock(
        return_value=httpx.Response(200, json={"success": True})
    )
    result = payment_service.process(order)
    assert result.success is True
```
