# When to Mock

Mock at **system boundaries** only:

- External HTTP APIs (payment, email, etc.)
- Databases (sometimes — prefer a real test DB or in-memory SQLite)
- Time/randomness (`Date.now`, `Math.random`)
- File system (sometimes)

Don't mock:

- Your own classes/services
- Internal collaborators
- Anything you control

## Designing for Mockability

**1. Use dependency injection**

Accept dependencies as parameters or constructor arguments; never instantiate them internally:

```ts
// Easy to mock
class PaymentService {
  constructor(private readonly client: PaymentClient) {}

  async process(order: Order): Promise<PaymentResult> {
    return this.client.charge(order.total);
  }
}

// Hard to mock
class PaymentService {
  async process(order: Order): Promise<PaymentResult> {
    const client = new StripeClient(process.env.STRIPE_KEY!);
    return client.charge(order.total);
  }
}
```

**2. Prefer typed interfaces over generic wrappers**

```ts
// GOOD: Each method is independently mockable
interface OrderApiClient {
  getUser(id: string): Promise<User>;
  getOrders(userId: string): Promise<Order[]>;
  createOrder(req: CreateOrderRequest): Promise<Order>;
}

// BAD: Mocking requires conditional logic inside the fake
interface ApiClient {
  send(endpoint: string, method: string, body?: unknown): Promise<string>;
}
```

## Mocking strategies

**jest.fn() — standalone function stubs**

```ts
const charge = jest.fn().mockResolvedValue({ success: true });
```

**jest.mock() — module-level replacement**

```ts
jest.mock('../paymentClient');
import { PaymentClient } from '../paymentClient';
(PaymentClient as jest.Mock).mockImplementation(() => ({ charge: jest.fn() }));
```

**jest.spyOn() — spy on a real implementation**

```ts
const spy = jest.spyOn(emailService, 'send').mockResolvedValue(undefined);
expect(spy).toHaveBeenCalledWith(expect.objectContaining({ to: 'alice@example.com' }));
```

**MSW — mock HTTP at the network level (preferred for HTTP boundaries)**

Intercepts `fetch`/`axios` calls without touching application code:

```ts
import { setupServer } from 'msw/node';
import { http, HttpResponse } from 'msw';

const server = setupServer(
  http.post('https://api.stripe.com/charge', () =>
    HttpResponse.json({ success: true })
  )
);

beforeAll(() => server.listen());
afterEach(() => server.resetHandlers());
afterAll(() => server.close());
```
