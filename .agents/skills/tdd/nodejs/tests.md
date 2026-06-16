# Good and Bad Tests

Use **Jest** (most common) or **Vitest** (modern, faster). For HTTP endpoint tests use **supertest**.

## Good Tests

**Integration-style**: Test through real interfaces, not mocks of internal parts.

```ts
// GOOD: Tests observable behavior
it('user can checkout with valid cart', async () => {
  const cart = CartBuilder.withProduct(product);
  const result = await checkoutService.checkout(cart, validPaymentMethod);
  expect(result.status).toBe('confirmed');
});
```

Characteristics:

- Tests behavior callers care about
- Uses public API only
- Survives internal refactors
- Describes WHAT, not HOW
- One logical assertion per test

## Bad Tests

**Implementation-detail tests**: Coupled to internal structure.

```ts
// BAD: Tests implementation details
it('checkout calls payment service', async () => {
  const mockPayment = { process: jest.fn() };
  await checkoutService.checkout(cart, mockPayment);
  expect(mockPayment.process).toHaveBeenCalledWith(cart.total); // verifying internal wiring
});
```

Red flags:

- Mocking internal collaborators
- Asserting on call counts/order of internal calls
- Test breaks when refactoring without behavior change
- Test name describes HOW not WHAT

```ts
// BAD: Bypasses interface to verify
it('createUser saves to database', async () => {
  await userService.create({ name: 'Alice' });
  const row = await db.query('SELECT * FROM users WHERE name = $1', ['Alice']);
  expect(row).not.toBeNull();
});

// GOOD: Verifies through the public interface
it('createUser makes user retrievable', async () => {
  const created = await userService.create({ name: 'Alice' });
  const retrieved = await userService.get(created.id);
  expect(retrieved.name).toBe('Alice');
});
```

## HTTP endpoint tests

Use **supertest** to test the full HTTP stack without mocking internals:

```ts
import request from 'supertest';
import { app } from '../app';

describe('POST /orders', () => {
  it('returns 201 with order id', async () => {
    const res = await request(app)
      .post('/orders')
      .send({ productId: 1, quantity: 2 });

    expect(res.status).toBe(201);
    expect(res.body.orderId).toBeDefined();
  });
});
```
