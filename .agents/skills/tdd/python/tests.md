# Good and Bad Tests

Use **pytest**. For HTTP endpoint tests use **httpx** + `TestClient` (FastAPI/Starlette) or the **pytest-django** test client.

## Good Tests

**Integration-style**: Test through real interfaces, not mocks of internal parts.

```python
# GOOD: Tests observable behavior
def test_user_can_checkout_with_valid_cart(checkout_service, product):
    cart = CartBuilder.with_product(product)
    result = checkout_service.checkout(cart, valid_payment_method)
    assert result.status == "confirmed"
```

Characteristics:

- Tests behavior callers care about
- Uses public API only
- Survives internal refactors
- Describes WHAT, not HOW
- One logical assertion per test

## Bad Tests

**Implementation-detail tests**: Coupled to internal structure.

```python
# BAD: Tests implementation details
def test_checkout_calls_payment_service(checkout_service, mocker):
    mock_payment = mocker.patch.object(checkout_service, '_payment_client')
    checkout_service.checkout(cart, payment)
    mock_payment.process.assert_called_once_with(cart.total)  # verifying internal wiring
```

Red flags:

- Mocking internal collaborators
- Patching private methods/attributes
- Asserting on call counts/order of internal calls
- Test breaks when refactoring without behavior change
- Test name describes HOW not WHAT

```python
# BAD: Bypasses interface to verify
def test_create_user_saves_to_database(user_service, db_session):
    user_service.create(CreateUserRequest(name="Alice"))
    row = db_session.execute("SELECT * FROM users WHERE name = 'Alice'").first()
    assert row is not None

# GOOD: Verifies through the public interface
def test_create_user_makes_user_retrievable(user_service):
    created = user_service.create(CreateUserRequest(name="Alice"))
    retrieved = user_service.get(created.id)
    assert retrieved.name == "Alice"
```

## HTTP endpoint tests

Use `TestClient` to test the full HTTP stack without mocking internals:

```python
from fastapi.testclient import TestClient
from app.main import app

client = TestClient(app)

def test_post_order_returns_201_with_order_id():
    response = client.post("/orders", json={"product_id": 1, "quantity": 2})
    assert response.status_code == 201
    assert "order_id" in response.json()
```

For async apps use **httpx** with `AsyncClient`:

```python
import pytest
import httpx
from app.main import app

@pytest.mark.anyio
async def test_post_order_returns_201():
    async with httpx.AsyncClient(app=app, base_url="http://test") as client:
        response = await client.post("/orders", json={"product_id": 1, "quantity": 2})
    assert response.status_code == 201
```
