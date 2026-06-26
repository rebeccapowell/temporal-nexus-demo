# Temporal .NET Nexus Shopping Basket Demo — Specification of Work

**Repository working name:** `temporal-dotnet-nexus-shopping-basket`  
**Solution format:** `.slnx`  
**Runtime:** .NET 10  
**Frontend:** React + Vite + shadcn/ui + Tailwind + pnpm  
**Hosting:** .NET Aspire, local only  
**Temporal hosting:** `InfinityFlow.Aspire.Temporal`  
**Email:** MailPit via Aspire Community Toolkit  
**Persistence:** No application database  
**Deployment:** Explicitly out of scope

---

## Rubber-ducked plan

Before writing this specification, the design was deliberately simplified.

The original idea included a custom workflow cockpit using a workflow map, aggregated run overlays, and a selected-instance path explorer. That is an interesting future direction, but it would distract from the actual demo. Temporal UI, Aspire logs/traces, and MailPit are sufficient for this repository.

The final demo should focus on one clear idea:

> A shopping journey is not just checkout. A customer can begin anonymously, build a purchase intent, verify their email, abandon and recover, then start a condition-gated checkout that crosses Nexus boundaries into inventory, payment, and fulfillment.

The main design decisions are:

1. **Use `PurchaseIntentWorkflow` instead of `BasketWorkflow`.**  
   A basket is only one part of the journey. The workflow actually owns a recoverable purchase intent: anonymous cookie correlation, basket items, verified email state, reminder behaviour, checkout reference, and final outcome.

2. **Use no database.**  
   Workflow state and Temporal history are enough for this local demo. The UI queries workflow state through the Minimal API.

3. **Use a cookie for anonymous continuity.**  
   No authentication is required. A local HTTP cookie links the browser to the current `PurchaseIntentWorkflow`.

4. **Email verification is a business condition, not a login system.**  
   Once the user verifies their email through MailPit, the workflow records that the required task has been fulfilled.

5. **Checkout is condition-gated, not a strict wizard.**  
   Checkout can start when the customer expresses intent to buy. It waits until required business conditions are fulfilled: basket has items, email verified, shipping address provided, and payment method provided.

6. **Use Nexus only for external durable service boundaries.**  
   Inventory, payment, and fulfillment are separate Temporal namespaces with separate workers and Nexus endpoints. Email sending is internal and goes through MailPit.

7. **Keep workers separate but lightweight.**  
   Separate worker projects make the architecture clearer in Aspire and Temporal UI, without adding databases or real integrations.

8. **Use Temporal UI and Aspire for observability.**  
   The demo should make it easy to inspect workflows, retries, logs, MailPit emails, and Nexus calls. No custom cockpit in V1.

---

# Part One — Human Architecture Specification

## 1. Purpose

Build a local-only demo repository that demonstrates Temporal Nexus for the .NET SDK in a realistic but compact e-commerce scenario.

The demo should show:

- Long-lived interactive workflows.
- Anonymous purchase intent tracking through a cookie.
- Email verification through MailPit.
- Business-rule-gated checkout.
- Separate Temporal namespaces.
- Nexus calls across namespace boundaries.
- Activity retries and controlled failure modes.
- Aspire-based local orchestration and observability.
- Temporal UI inspection of the full process.

The demo should be easy to run locally and useful as the basis for a blog post.

---

## 2. High-level customer journey

```text
1. User opens the shop.
2. API creates or resumes a purchase intent using a local cookie.
3. User adds products to the basket.
4. User may provide an email address.
5. Email verification is sent through MailPit.
6. User may abandon before checkout.
7. If email is verified, a recovery email can be sent later.
8. User clicks checkout.
9. CheckoutWorkflow starts and waits for required conditions:
   - basket has items
   - email is verified
   - shipping address is provided
   - payment method is provided
10. When all conditions are fulfilled, CheckoutWorkflow calls Nexus services:
   - Inventory
   - Payment
   - Fulfillment
11. Payment may approve, decline, fail once, or randomly fail based on per-attempt demo options.
12. Fulfillment may succeed or fail based on per-attempt demo options.
13. Checkout either completes or compensates.
14. User inspects the result in the UI, Temporal UI, Aspire logs, and MailPit.
```

---

## 3. Local architecture

```text
Aspire AppHost
  ├── Temporal dev server
  ├── MailPit
  ├── Storefront.Api
  ├── Storefront.Ui
  ├── PurchaseIntent.Worker
  ├── Checkout.Worker
  ├── Inventory.Worker
  ├── Payment.Worker
  └── Fulfillment.Worker
```

This application is not designed for deployment. The AppHost is the product runtime.

---

## 4. Temporal namespaces

Use separate namespaces from V1.

```text
storefront
inventory
payment
fulfillment
```

The separation is important because the demo is specifically about Nexus across durable service boundaries.

---

## 5. Task queues

```text
storefront namespace
  purchase-intent-task-queue
  checkout-task-queue

inventory namespace
  inventory-task-queue

payment namespace
  payment-task-queue

fulfillment namespace
  fulfillment-task-queue
```

---

## 6. Workers

### 6.1 PurchaseIntent.Worker

Namespace:

```text
storefront
```

Task queue:

```text
purchase-intent-task-queue
```

Owns:

- `PurchaseIntentWorkflow`
- Purchase-intent local activities
- Recovery email activity
- Purchase-intent status logging

Responsibilities:

- Track anonymous purchase intent by cookie.
- Maintain basket items in workflow state.
- Track email verification status.
- Track last customer activity.
- Send recovery reminders through MailPit when possible.
- Start `CheckoutWorkflow`.
- Record checkout workflow ID/status.
- Keep the purchase intent open until converted, expired, or cancelled.

---

### 6.2 Checkout.Worker

Namespace:

```text
storefront
```

Task queue:

```text
checkout-task-queue
```

Owns:

- `CheckoutWorkflow`
- `EmailVerificationWorkflow`
- Checkout local activities
- Email verification activities
- Order confirmation/failure email activities

Responsibilities:

- Start checkout from a snapshot of purchase-intent state.
- Track checkout conditions.
- Coordinate email verification if needed.
- Wait until all required conditions are fulfilled.
- Call Nexus services only after the condition gate passes.
- Handle payment/fulfillment failures.
- Compensate when required.
- Signal `PurchaseIntentWorkflow` when important checkout state changes.

Why email verification lives here:

- Email verification is a checkout/business condition.
- It is internal to the storefront namespace.
- It does not deserve a Nexus boundary.
- It is small enough to live in the checkout worker.

---

### 6.3 Inventory.Worker

Namespace:

```text
inventory
```

Task queue:

```text
inventory-task-queue
```

Owns:

- Inventory Nexus service handler.
- Simple fake inventory workflows or activities.

Operations:

- `ReserveStock`
- `ReleaseStock`

---

### 6.4 Payment.Worker

Namespace:

```text
payment
```

Task queue:

```text
payment-task-queue
```

Owns:

- Payment Nexus service handler.
- Simple fake payment workflows or activities.

Operations:

- `AuthorizePayment`
- `CapturePayment`
- `VoidAuthorization`

---

### 6.5 Fulfillment.Worker

Namespace:

```text
fulfillment
```

Task queue:

```text
fulfillment-task-queue
```

Owns:

- Fulfillment Nexus service handler.
- Simple fake fulfillment workflows or activities.

Operations:

- `CreateShipment`
- `CancelShipment`

---

## 7. Nexus endpoints

Create three Nexus endpoints:

```text
inventory-service   → inventory namespace / inventory-task-queue
payment-service     → payment namespace / payment-task-queue
fulfillment-service → fulfillment namespace / fulfillment-task-queue
```

These endpoints are called by `CheckoutWorkflow` from the `storefront` namespace.

Nexus is in Public Preview in the .NET SDK, so the README and blog post must state that API names and configuration may change before stable release.

---

## 8. Workflow model

## 8.1 PurchaseIntentWorkflow

### Purpose

`PurchaseIntentWorkflow` is the long-lived anchor for the customer journey.

It should replace the earlier idea of `BasketWorkflow`.

### Workflow ID

Use a stable ID:

```text
purchase-intent-{purchaseIntentId}
```

The `purchaseIntentId` is stored in a browser cookie.

Suggested cookie name:

```text
demo_purchase_intent_id
```

For V1, no authentication or account identity exists.

### State

The workflow state should include:

```text
PurchaseIntentId
DemoSessionId
Status
Items
EmailAddress?
EmailVerified
EmailVerificationWorkflowId?
LastCustomerActivityAt
CreatedAt
ExpiresAt
RecoveryReminderDueAt?
RecoveryReminderSentAt?
CheckoutWorkflowId?
CheckoutStatus?
CheckoutOutcome?
```

Avoid logging raw email addresses. Logs should use masked email or a hash.

### Statuses

```text
ActiveAnonymous
ActiveWithUnverifiedEmail
ActiveWithVerifiedEmail
CheckoutStarted
CheckoutWaitingForConditions
CheckoutProcessing
Converted
Expired
Cancelled
CheckoutFailed
```

### Commands / Updates

Use workflow updates where the API expects the updated state back.

```text
AddItem
RemoveItem
UpdateQuantity
ProvideEmail
MarkEmailVerified
StartCheckout
RecordCheckoutStatus
CancelPurchaseIntent
```

### Queries

```text
GetPurchaseIntent
GetPurchaseIntentSummary
GetCurrentBasket
```

### Timers

The workflow should manage recovery and expiry timers.

Demo timings should be configurable. Use defaults that allow a human demo without rushing.

Suggested local defaults:

```text
Purchase intent expiry: 60 minutes
Email verification expiry: 10 minutes
Recovery reminder delay after verified inactivity: 2 minutes
Checkout waiting timeout: 15 minutes
```

Real-world equivalents can be documented separately:

```text
Anonymous basket expiry: 30 minutes to 24 hours
Email verification expiry: 2 hours
Abandoned basket reminder: 12 hours
Verified basket recoverability: 7 days
```

### Recovery behaviour

The purchase intent should remain the recovery anchor even after checkout starts.

If the user has a verified email and has not converted, the system can send a recovery email through MailPit.

There are two recovery scenarios:

```text
Pre-checkout recovery:
  User added items and verified email, but never clicked checkout.

Checkout recovery:
  User clicked checkout but left while checkout was waiting for conditions,
  payment retry, or another recoverable state.
```

For V1, recovery emails can be simple and logged through MailPit.

---

## 8.2 EmailVerificationWorkflow

### Purpose

`EmailVerificationWorkflow` owns the verification token, MailPit email, callback, and timeout.

It is not a Nexus service.

### Workflow ID

```text
email-verification-{purchaseIntentId}-{attemptNumber}
```

### State

```text
PurchaseIntentId
CheckoutWorkflowId?
EmailAddress
Token
Status
ExpiresAt
VerifiedAt?
```

### Statuses

```text
Pending
Verified
Expired
Cancelled
```

### Flow

```text
1. Generate verification token.
2. Send verification email through MailPit.
3. Wait for verification callback.
4. On successful verification:
   - mark itself verified
   - signal PurchaseIntentWorkflow
   - signal CheckoutWorkflow if checkout is waiting for email verification
5. On timeout:
   - mark itself expired
   - signal interested workflow(s) if needed
```

### Verification link

The email should contain a local link such as:

```text
http://localhost:{api-port}/api/email-verification/confirm?purchaseIntentId={id}&token={token}
```

The exact port should be injected/configured by Aspire or shown through the API endpoint returned to the UI.

The API endpoint validates the token by signalling/updating the `EmailVerificationWorkflow`.

---

## 8.3 CheckoutWorkflow

### Purpose

`CheckoutWorkflow` owns the purchase finalization process.

It is an internal workflow in the `storefront` namespace, not a Nexus service.

### Workflow ID

```text
checkout-{checkoutId}
```

### Input

The checkout starts with a snapshot of purchase-intent state:

```text
PurchaseIntentId
CheckoutId
Items
EmailAddress?
EmailVerified
DemoOptions
```

The checkout should not rely on mutable basket state after it starts. It receives the basket snapshot it needs.

### State

```text
CheckoutId
PurchaseIntentId
Status
Items
EmailVerified
ShippingAddress?
PaymentMethod?
DemoOptions
InventoryReservationId?
PaymentAuthorizationId?
PaymentCaptureId?
ShipmentId?
FailureReason?
CompensationStatus?
```

### Statuses

```text
Started
WaitingForRequiredConditions
WaitingForEmailVerification
WaitingForShippingAddress
WaitingForPaymentMethod
Processing
InventoryReserved
PaymentAuthorized
FulfillmentCreated
PaymentCaptured
Completed
PaymentDeclined
Compensating
Failed
Cancelled
TimedOut
```

### Required conditions

Checkout can only enter the external Nexus phase when:

```text
BasketHasItems = true
EmailVerified = true
ShippingAddressProvided = true
PaymentMethodProvided = true
```

The UI may guide the customer through these tasks, but the workflow is the authority.

### Commands / Updates

```text
ProvideShippingAddress
ProvidePaymentMethod
MarkEmailVerified
CancelCheckout
RetryPayment
```

### Queries

```text
GetCheckoutState
GetRequiredConditions
```

### Flow

```text
1. Start checkout.
2. Evaluate required conditions.
3. If email is missing or unverified:
   - coordinate/start EmailVerificationWorkflow.
   - wait for EmailVerified signal/update.
4. If shipping address is missing:
   - wait for ProvideShippingAddress update.
5. If payment method is missing:
   - wait for ProvidePaymentMethod update.
6. Once all conditions are fulfilled:
   - call Inventory Nexus service to reserve stock.
   - call Payment Nexus service to authorize payment.
   - call Fulfillment Nexus service to create shipment.
   - call Payment Nexus service to capture payment.
7. Send order confirmation email through MailPit.
8. Signal PurchaseIntentWorkflow that the purchase converted.
```

### Failure and compensation

If inventory says out of stock:

```text
Checkout fails before payment.
No compensation required.
```

If payment is declined:

```text
Release stock through Inventory Nexus service.
Return checkout to a recoverable payment state.
```

If fulfillment fails after payment authorization:

```text
Void payment authorization through Payment Nexus service.
Release stock through Inventory Nexus service.
Send checkout failure email through MailPit.
Signal PurchaseIntentWorkflow.
```

If capture fails after fulfillment:

```text
Cancel shipment if supported.
Void or mark payment failed, depending on fake payment result.
Release stock if still meaningful.
Mark checkout failed.
```

For V1, keep capture failure simple and clearly documented.

---

## 9. Demo options

Failure modes should be **per checkout attempt**, not global.

Each checkout request carries demo options:

```text
inventoryMode:
  InStock
  OutOfStock
  FailOnceThenSucceed

paymentMode:
  AlwaysApprove
  AlwaysDecline
  Random
  FailOnceThenSucceed

fulfillmentMode:
  AlwaysSucceed
  FailAfterPaymentAuthorization
  FailOnceThenSucceed
```

### Business failure vs technical failure

Model these differently.

Business failures should return normal domain results:

```text
Out of stock
Payment declined
Invalid shipping address
```

Technical/transient failures should throw exceptions in activities:

```text
SMTP unavailable
Temporary payment provider error
Temporary inventory timeout
Temporary fulfillment timeout
```

Randomness must only happen inside activities or handlers, not inside deterministic workflow code.

---

## 10. No database design

V1 must not use an application database.

The state model is:

```text
Temporal workflow state:
  Durable source of truth.

Temporal history:
  Durable execution history.

Workflow queries:
  API access to current workflow state.

Search attributes:
  Optional lightweight filtering in Temporal UI.

Structured logs:
  Observability in Aspire, not the application state store.
```

No Postgres, SQLite, Redis, event store, or projection database should be added.

---

## 11. UI design

The UI is a single React/Vite SPA using shadcn/ui and Tailwind.

### Views

Use two simple switchable views or sections inside the SPA:

```text
Shop
Demo controls / status
```

Do not build a custom workflow cockpit in V1.

### Shop view

Must include:

- Product grid.
- Basket drawer or basket panel.
- Quantity controls.
- Email capture.
- Link/help text telling the user to open MailPit to verify email.
- Shipping address form.
- Payment method form.
- Checkout button.
- Checkout status display.

### Demo controls

Must include per-checkout failure controls:

```text
Inventory mode
Payment mode
Fulfillment mode
```

Must include customer/session controls:

```text
New clean customer
Clear current cookie
Resume current customer
```

A “new clean customer” should create a new purchase intent cookie without deleting previous workflows. This preserves previous workflow runs for Temporal UI inspection.

A “clear cookie” should remove the cookie and reload the UI as an anonymous user.

### UI theme

Use shadcn/ui with Tailwind and support light/dark mode.

The UI should be clean but not overbuilt. The point is to drive the Temporal demo.

---

## 12. API design

The API is a .NET 10 ASP.NET Core Minimal API.

### Cookie/session endpoints

```http
GET  /api/session
POST /api/session/new-clean-customer
POST /api/session/clear-cookie
```

### Product endpoints

```http
GET /api/products
```

Products can be static in memory.

Suggested products:

```text
Temporal Coffee Mug
Durable Execution Hoodie
Retry Policy Socks
Nexus Notebook
Workflow Timer Desk Toy
Saga Sticker Pack
Idempotency Water Bottle
Worker Queue Tote Bag
```

### Purchase intent endpoints

```http
GET    /api/purchase-intent/current
POST   /api/purchase-intent/items
PUT    /api/purchase-intent/items/{sku}
DELETE /api/purchase-intent/items/{sku}
POST   /api/purchase-intent/email
POST   /api/purchase-intent/checkout
```

### Email verification endpoint

```http
GET  /api/email-verification/confirm
POST /api/email-verification/confirm
```

GET is useful for clicking links from MailPit. POST can be used by the UI if needed.

### Checkout endpoints

```http
GET  /api/checkouts/{checkoutId}
POST /api/checkouts/{checkoutId}/shipping-address
POST /api/checkouts/{checkoutId}/payment-method
POST /api/checkouts/{checkoutId}/retry-payment
POST /api/checkouts/{checkoutId}/cancel
```

---

## 13. Aspire AppHost requirements

Use:

```text
InfinityFlow.Aspire.Temporal
CommunityToolkit.Aspire.Hosting.MailPit
Aspire JavaScript/Vite hosting
```

### Temporal

Use the container resource from `InfinityFlow.Aspire.Temporal`.

The AppHost should create the namespaces:

```text
storefront
inventory
payment
fulfillment
```

Enable workflow updates where needed.

Conceptual shape:

```csharp
using InfinityFlow.Aspire.Temporal;

var temporal = builder.AddTemporalServerContainer("temporal")
    .WithLogFormat(LogFormat.Json)
    .WithLogLevel(LogLevel.Info)
    .WithNamespace("storefront", "inventory", "payment", "fulfillment")
    .WithDynamicConfigValue("frontend.enableUpdateWorkflowExecution", true);
```

The exact API should be checked against the current package version during implementation.

### MailPit

Add MailPit as a first-class Aspire resource:

```csharp
var mailpit = builder.AddMailPit("mailpit");
```

Reference MailPit from the workers that send email:

```text
PurchaseIntent.Worker
Checkout.Worker
```

The MailPit web UI should be reachable through the Aspire dashboard.

### API

```csharp
builder.AddProject<Projects.Storefront_Api>("storefront-api")
    .WithReference(temporal)
    .WithReference(mailpit);
```

### Workers

```csharp
builder.AddProject<Projects.PurchaseIntent_Worker>("purchase-intent-worker")
    .WithReference(temporal)
    .WithReference(mailpit);

builder.AddProject<Projects.Checkout_Worker>("checkout-worker")
    .WithReference(temporal)
    .WithReference(mailpit);

builder.AddProject<Projects.Inventory_Worker>("inventory-worker")
    .WithReference(temporal);

builder.AddProject<Projects.Payment_Worker>("payment-worker")
    .WithReference(temporal);

builder.AddProject<Projects.Fulfillment_Worker>("fulfillment-worker")
    .WithReference(temporal);
```

### React/Vite UI

Use Aspire’s JavaScript/Vite integration.

Use pnpm.

Conceptual shape:

```csharp
var ui = builder.AddViteApp("storefront-ui", "../Storefront.Ui")
    .WithReference(api);
```

The exact pnpm configuration should follow the current Aspire JavaScript integration API. If a direct package-manager argument is unavailable in the installed version, use the current supported pnpm extension/package and document the reason.

---

## 14. OpenTelemetry and logging

Use Aspire service defaults.

Each .NET project should use standard logging.

The Temporal client/worker integration should include Temporal OpenTelemetry integration through the `InfinityFlow.Aspire.Temporal.Client` package and service defaults where applicable.

Log important events:

```text
Purchase intent created
Item added
Email verification requested
Verification email sent
Email verified
Recovery email sent
Checkout started
Checkout waiting for conditions
Shipping address provided
Payment method provided
Inventory reservation requested
Inventory reserved
Payment authorization requested
Payment authorized
Payment declined
Fulfillment requested
Shipment created
Payment captured
Compensation started
Payment voided
Stock released
Checkout completed
Checkout failed
```

Logs should include useful properties:

```text
purchaseIntentId
checkoutId
workflowId
namespace
taskQueue
sku
quantity
demoMode
nexusEndpoint
nexusOperation
status
failureReason
```

Do not log raw payment details. Payment method can be a fake token or enum.

Avoid raw email in logs. Use masked email or hash.

---

## 15. Manual verification scenarios

Tests are not required for V1.

Manual demo scenarios should be documented in the README.

### Scenario 1: Happy path

```text
1. New clean customer.
2. Add item.
3. Provide email.
4. Open MailPit and click verification link.
5. Start checkout.
6. Provide address.
7. Provide payment method.
8. Use payment mode AlwaysApprove.
9. Use fulfillment mode AlwaysSucceed.
10. Observe checkout completion in UI and Temporal UI.
```

### Scenario 2: Checkout started before email verification

```text
1. New clean customer.
2. Add item.
3. Start checkout before verifying email.
4. Observe checkout waiting for email verification.
5. Verify email via MailPit.
6. Observe checkout continuing once other conditions are fulfilled.
```

### Scenario 3: Payment declined

```text
1. Use payment mode AlwaysDecline.
2. Complete required conditions.
3. Observe payment declined.
4. Observe stock release compensation.
5. Retry payment with AlwaysApprove.
```

### Scenario 4: Fulfillment failure after payment authorization

```text
1. Use fulfillment mode FailAfterPaymentAuthorization.
2. Complete required conditions.
3. Observe inventory reservation and payment authorization.
4. Observe fulfillment failure.
5. Observe payment void and stock release compensation.
```

### Scenario 5: Activity retry

```text
1. Use inventory/payment/fulfillment FailOnceThenSucceed.
2. Observe activity failure and retry in Temporal UI.
3. Observe eventual success.
```

### Scenario 6: Recovery email

```text
1. New clean customer.
2. Add item.
3. Verify email.
4. Wait for recovery timer.
5. Open MailPit and inspect recovery email.
6. Click recovery link and resume the purchase intent.
```

---

## 16. Non-goals for V1

Do not implement:

```text
Production deployment
Authentication
Database persistence
Custom workflow cockpit
SignalR
AI shopping assistant
Real payment provider
Real inventory provider
Real fulfillment provider
Real email provider
Automated test suite
Event sourcing
WorkflowAPI standard library
DataDog export
Kubernetes manifests
Helm charts
```

---

# Part Two — Coding Agent Implementation Brief

## 17. Prime directive

Build the smallest local demo that clearly demonstrates:

```text
PurchaseIntentWorkflow
  → condition-gated CheckoutWorkflow
  → Nexus calls into Inventory, Payment, and Fulfillment
  → MailPit email verification and recovery
  → Aspire + Temporal UI observability
```

Do not add production infrastructure.

Do not add a database.

Do not build a custom cockpit.

Do not add authentication.

---

## 18. Required technology choices

Use:

```text
.NET 10
.slnx solution
ASP.NET Core Minimal API
React
Vite
pnpm
shadcn/ui
Tailwind
.NET Aspire
InfinityFlow.Aspire.Temporal
CommunityToolkit.Aspire.Hosting.MailPit
Temporal .NET SDK
Temporal Nexus preview APIs
```

The .NET 10 SDK creates `.slnx` by default when using `dotnet new sln`.

---

## 19. Suggested project layout

```text
temporal-dotnet-nexus-shopping-basket/
  Temporal.Nexus.ShoppingBasket.slnx

  src/
    ShoppingBasket.AppHost/
    ShoppingBasket.ServiceDefaults/

    ShoppingBasket.Api/
    ShoppingBasket.Ui/

    ShoppingBasket.Contracts/
    ShoppingBasket.NexusContracts/

    ShoppingBasket.PurchaseIntent.Worker/
    ShoppingBasket.Checkout.Worker/
    ShoppingBasket.Inventory.Worker/
    ShoppingBasket.Payment.Worker/
    ShoppingBasket.Fulfillment.Worker/

    ShoppingBasket.Storefront.Workflows/
    ShoppingBasket.Storefront.Activities/
    ShoppingBasket.Inventory/
    ShoppingBasket.Payment/
    ShoppingBasket.Fulfillment/

  docs/
    workflow-design.md
    demo-scenarios.md
```

A slightly different folder structure is acceptable if it preserves the same boundaries.

---

## 20. Solution creation

Use .NET 10.

```bash
dotnet new sln --name Temporal.Nexus.ShoppingBasket
```

This should create a `.slnx` file with .NET 10.

Create the projects and add them to the solution.

Use project references intentionally:

```text
Api
  references Contracts

PurchaseIntent.Worker
  references Contracts, Storefront.Workflows, Storefront.Activities

Checkout.Worker
  references Contracts, NexusContracts, Storefront.Workflows, Storefront.Activities

Inventory.Worker
  references NexusContracts, Inventory

Payment.Worker
  references NexusContracts, Payment

Fulfillment.Worker
  references NexusContracts, Fulfillment
```

Avoid circular references.

---

## 21. AppHost tasks

Install required packages:

```bash
dotnet add src/ShoppingBasket.AppHost package InfinityFlow.Aspire.Temporal
dotnet add src/ShoppingBasket.AppHost package CommunityToolkit.Aspire.Hosting.MailPit
```

Add Temporal dev server with namespaces:

```text
storefront
inventory
payment
fulfillment
```

Add MailPit.

Add API, UI, and all workers.

Ensure the Temporal and MailPit resources are visible in Aspire dashboard.

Ensure Temporal UI is reachable through the Aspire dashboard.

---

## 22. Temporal client and worker setup

Use the `InfinityFlow.Aspire.Temporal.Client` package in API and worker projects where appropriate.

Register clients and workers with explicit namespace and task queue.

Example conceptual worker registration:

```csharp
builder.AddTemporalWorker("temporal", "purchase-intent-task-queue", options =>
{
    options.Namespace = "storefront";
})
.AddWorkflow<PurchaseIntentWorkflow>()
.AddScopedActivities<PurchaseIntentActivities>();
```

The exact fluent syntax should be verified against the package version.

---

## 23. Namespace and Nexus setup

The implementation must create the required Nexus endpoints for the local Temporal dev server.

Options:

1. Configure through AppHost helper/lifecycle if the hosting extension supports it.
2. Add a setup script under `scripts/`.
3. Add a small setup project/resource that runs after Temporal starts.

Required endpoints:

```bash
temporal operator nexus endpoint create \
  --name inventory-service \
  --target-namespace inventory \
  --target-task-queue inventory-task-queue

temporal operator nexus endpoint create \
  --name payment-service \
  --target-namespace payment \
  --target-task-queue payment-task-queue

temporal operator nexus endpoint create \
  --name fulfillment-service \
  --target-namespace fulfillment \
  --target-task-queue fulfillment-task-queue
```

The setup must be idempotent or easy to rerun.

If the endpoint already exists, the setup should not fail the full demo.

---

## 24. Workflow contracts

Create strongly typed records for:

```text
Product
BasketItem
PurchaseIntentState
PurchaseIntentView
CheckoutState
CheckoutView
ShippingAddress
PaymentMethod
DemoOptions
```

Use simple immutable records where possible.

Example demo options:

```csharp
public sealed record CheckoutDemoOptions(
    InventoryMode InventoryMode,
    PaymentMode PaymentMode,
    FulfillmentMode FulfillmentMode);
```

Enums:

```csharp
public enum InventoryMode
{
    InStock,
    OutOfStock,
    FailOnceThenSucceed
}

public enum PaymentMode
{
    AlwaysApprove,
    AlwaysDecline,
    Random,
    FailOnceThenSucceed
}

public enum FulfillmentMode
{
    AlwaysSucceed,
    FailAfterPaymentAuthorization,
    FailOnceThenSucceed
}
```

---

## 25. Nexus contracts

Create `ShoppingBasket.NexusContracts`.

Define:

```csharp
[NexusService]
public interface IInventoryNexusService
{
    public const string EndpointName = "inventory-service";

    [NexusOperation]
    Task<ReserveStockResult> ReserveStockAsync(ReserveStockRequest request);

    [NexusOperation]
    Task<ReleaseStockResult> ReleaseStockAsync(ReleaseStockRequest request);
}
```

```csharp
[NexusService]
public interface IPaymentNexusService
{
    public const string EndpointName = "payment-service";

    [NexusOperation]
    Task<AuthorizePaymentResult> AuthorizePaymentAsync(AuthorizePaymentRequest request);

    [NexusOperation]
    Task<CapturePaymentResult> CapturePaymentAsync(CapturePaymentRequest request);

    [NexusOperation]
    Task<VoidAuthorizationResult> VoidAuthorizationAsync(VoidAuthorizationRequest request);
}
```

```csharp
[NexusService]
public interface IFulfillmentNexusService
{
    public const string EndpointName = "fulfillment-service";

    [NexusOperation]
    Task<CreateShipmentResult> CreateShipmentAsync(CreateShipmentRequest request);

    [NexusOperation]
    Task<CancelShipmentResult> CancelShipmentAsync(CancelShipmentRequest request);
}
```

Use the current .NET Nexus preview API names and adjust if necessary.

---

## 26. PurchaseIntentWorkflow implementation tasks

Implement:

```text
Start with purchaseIntentId and demoSessionId.
Add/update/remove items.
Provide email.
Start or reference EmailVerificationWorkflow.
Track email verified.
Start CheckoutWorkflow.
Record checkout status updates.
Send recovery email when eligible.
Expire when inactive.
Expose query view.
```

Important constraints:

- No HTTP calls from workflow code.
- No random values from workflow code.
- No direct MailPit access from workflow code.
- Use activities for IO.
- Use workflow time APIs for timers.

---

## 27. EmailVerificationWorkflow implementation tasks

Implement:

```text
Generate token through activity or from deterministic input.
Send verification email through MailPit activity.
Wait for verification confirmation.
Expire after configured timeout.
Signal PurchaseIntentWorkflow on verified.
Signal CheckoutWorkflow if checkout is waiting.
```

The verification email must contain a clickable link to the API endpoint.

The MailPit UI should show the email.

---

## 28. CheckoutWorkflow implementation tasks

Implement:

```text
Start from purchase intent snapshot.
Evaluate required conditions.
Wait for missing conditions.
Accept updates for address and payment method.
Accept signal/update for email verified.
Only after conditions are met:
  call Inventory Nexus service.
  call Payment Nexus service.
  call Fulfillment Nexus service.
  capture payment.
Handle compensation.
Signal PurchaseIntentWorkflow on status changes.
Expose query view.
```

The checkout workflow should be readable in Temporal UI.

Name workflow steps and log events clearly.

---

## 29. Inventory implementation tasks

Implement the Nexus handler and fake behaviour.

Rules:

```text
InStock:
  Reserve succeeds.

OutOfStock:
  Reserve returns business failure.

FailOnceThenSucceed:
  First attempt throws transient exception from activity.
  Retry succeeds.
```

Release stock always succeeds for V1.

---

## 30. Payment implementation tasks

Implement the Nexus handler and fake behaviour.

Rules:

```text
AlwaysApprove:
  Authorize succeeds.

AlwaysDecline:
  Authorize returns business failure.

Random:
  Authorize randomly approves or declines inside an activity.

FailOnceThenSucceed:
  First attempt throws transient exception from activity.
  Retry succeeds.
```

Capture payment succeeds unless explicitly extended later.

Void authorization always succeeds for V1.

---

## 31. Fulfillment implementation tasks

Implement the Nexus handler and fake behaviour.

Rules:

```text
AlwaysSucceed:
  CreateShipment succeeds.

FailAfterPaymentAuthorization:
  CreateShipment returns business failure after payment authorization has happened.

FailOnceThenSucceed:
  First attempt throws transient exception from activity.
  Retry succeeds.
```

Cancel shipment always succeeds for V1.

---

## 32. MailPit email implementation tasks

Use MailPit for:

```text
Email verification email
Recovery email
Order confirmation email
Checkout failure email
```

Use SMTP connection properties injected by Aspire.

No real email provider.

Email templates can be plain HTML strings.

Each email should include:

```text
Subject
Body
Purchase intent ID
Checkout ID where relevant
Local link where relevant
```

---

## 33. API implementation tasks

Implement cookie handling:

```text
If cookie exists:
  resume current PurchaseIntentWorkflow.

If cookie missing:
  create new PurchaseIntentWorkflow and set cookie.

New clean customer:
  create new purchaseIntentId and overwrite cookie.
  do not delete old workflows.

Clear cookie:
  remove cookie only.
```

Implement all endpoints from section 12.

The API should translate frontend actions into Temporal workflow updates/signals/queries.

The API should not store state in memory except static product catalogue/config.

---

## 34. UI implementation tasks

Create React/Vite app with pnpm.

Use shadcn/ui and Tailwind.

Implement:

```text
Product grid
Basket panel
Email verification panel
Checkout form
Demo failure controls
Session controls
Status display
```

The UI should show enough state to guide the user:

```text
Current purchase intent ID
Email verification status
Checkout ID
Checkout status
Required conditions
Failure mode selections
```

Do not build a custom workflow cockpit.

Add simple links/instructions:

```text
Open MailPit from Aspire dashboard to verify email.
Open Temporal UI from Aspire dashboard to inspect workflows.
```

---

## 35. README requirements

The README must include:

```text
Purpose
Architecture diagram
How to run locally
Prerequisites
How to inspect Temporal UI
How to inspect MailPit
Demo scenarios
Known limitations
Public Preview warning for Temporal Nexus .NET SDK
No deployment statement
No database statement
```

Include a short explanation:

```text
Activities are local IO.
Child/internal workflows are storefront-owned durable sub-processes.
Nexus services are external durable service boundaries.
```

---

## 36. Guardrails for coding agents

Do not:

```text
Add a database.
Add auth.
Add Docker Compose manually unless Aspire requires it.
Add Kubernetes/deployment assets.
Add a custom cockpit.
Add SignalR.
Add AI chat.
Add test suite unless explicitly requested later.
Make payment/inventory/fulfillment realistic.
Hide Nexus behind normal HTTP calls.
Put randomness inside workflow code.
Log raw email or payment details.
```

Do:

```text
Keep workflows readable.
Keep failure modes deterministic where possible.
Use Temporal UI-friendly workflow IDs.
Use structured logs.
Use Aspire service defaults.
Use MailPit for all email.
Use separate namespaces.
Use separate workers.
Keep local setup simple.
Document everything needed for a blog reader.
```

---

## 37. Acceptance criteria

The work is complete when:

```text
1. `dotnet run --project src/ShoppingBasket.AppHost` starts the full local demo.
2. Aspire dashboard shows:
   - Temporal
   - MailPit
   - Storefront.Api
   - Storefront.Ui
   - PurchaseIntent.Worker
   - Checkout.Worker
   - Inventory.Worker
   - Payment.Worker
   - Fulfillment.Worker
3. Temporal has namespaces:
   - storefront
   - inventory
   - payment
   - fulfillment
4. Nexus endpoints exist:
   - inventory-service
   - payment-service
   - fulfillment-service
5. UI can create a clean customer.
6. UI can add products to a purchase intent.
7. UI can send email verification to MailPit.
8. Clicking the MailPit verification link updates workflow state.
9. Checkout can start before all conditions are fulfilled.
10. Checkout waits for missing email/address/payment conditions.
11. Checkout calls Nexus services after conditions are fulfilled.
12. Happy path completes.
13. Payment declined path is visible.
14. Fulfillment failure compensation path is visible.
15. Fail-once-then-succeed retry path is visible in Temporal UI.
16. Recovery email can be sent to MailPit.
17. No application database is required.
18. README explains how to inspect everything.
```

---

## 38. Reference links

- Temporal .NET Nexus quickstart: https://docs.temporal.io/develop/dotnet/nexus/quickstart
- Temporal .NET Nexus feature guide: https://docs.temporal.io/develop/dotnet/nexus/feature-guide
- InfinityFlow.Aspire.Temporal: https://github.com/InfinityFlowApp/aspire-temporal
- Aspire MailPit integration: https://aspire.dev/integrations/devtools/mailpit/mailpit-get-started/
- Aspire MailPit AppHost setup: https://aspire.dev/integrations/devtools/mailpit/mailpit-host/
- Aspire JavaScript/Vite integration: https://aspire.dev/integrations/frameworks/javascript/
- .NET 10 `.slnx` default: https://learn.microsoft.com/en-us/dotnet/core/compatibility/sdk/10.0/dotnet-new-sln-slnx-default
