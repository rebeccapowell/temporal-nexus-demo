# Temporal .NET Nexus Shopping Basket Demo - Design Spec

**Status:** Draft for review  
**Focus:** Minimal runnable demo with a complete purchase journey  
**Runtime:** .NET 10, local-only Aspire app host  
**Temporal:** .NET SDK with Nexus preview APIs

## 1. Purpose

Build a compact e-commerce demo that shows how Temporal Nexus fits into a real purchase journey in .NET.

The demo should prove four things:

1. A customer can start anonymously and keep continuity through a cookie.
2. Email verification can happen through MailPit without becoming an auth system.
3. Checkout can wait on business conditions instead of forcing a rigid wizard.
4. Checkout can cross Temporal namespace boundaries through Nexus to inventory, payment, and fulfillment.

The demo is local-only and blog-friendly. It should be easy to run, easy to inspect, and small enough to explain in one sitting.

## 2. Scope

### In scope

- Anonymous purchase intent tracking through a browser cookie.
- Product browsing and basket updates.
- Email capture, verification, and recovery email flow through MailPit.
- Condition-gated checkout.
- Nexus calls from storefront checkout into inventory, payment, and fulfillment namespaces.
- Aspire-based local orchestration and observability.
- Temporal UI inspection of workflows and Nexus activity.

### Out of scope

- Production deployment.
- Authentication or user accounts.
- Application database persistence.
- Custom workflow cockpit.
- Real payment, inventory, fulfillment, or email providers.
- Automated test suite for V1.

## 3. Product story

1. The user opens the shop.
2. The API creates or resumes a purchase intent using a cookie.
3. The user adds items to the basket.
4. The user enters an email address.
5. The system sends a verification email through MailPit.
6. The user may leave and return later.
7. If the email is verified, the system can send a recovery email later.
8. The user starts checkout.
9. Checkout waits until the required conditions are met:
   - basket has items
   - email is verified
   - shipping address is provided
   - payment method is provided
10. Once the gate passes, checkout calls Nexus services for inventory, payment, and fulfillment.
11. The user inspects the result in the UI, Aspire, MailPit, and Temporal UI.

## 4. Architecture

### Local runtime

The repository should run as an Aspire application, not as a standalone app host file.

The runtime should include:

- a proper `AppHost` project
- a separate `StandardDefaults` project for shared Aspire service defaults
- `Storefront.Api`
- `Storefront.Ui`
- `PurchaseIntent.Worker`
- `Checkout.Worker`
- `Inventory.Worker`
- `Payment.Worker`
- `Fulfillment.Worker`
- Temporal dev service
- MailPit

### Project shape

The implementation should follow a normal solution structure, not a single-file entry point:

```text
src/
  ShoppingBasket.AppHost/
  ShoppingBasket.StandardDefaults/
  Storefront.Api/
  Storefront.Ui/
  PurchaseIntent.Worker/
  Checkout.Worker/
  Inventory.Worker/
  Payment.Worker/
  Fulfillment.Worker/
```

The AppHost orchestrates the local demo runtime. The StandardDefaults project owns shared Aspire defaults and OpenTelemetry wiring.

## 5. Temporal and Nexus rules

### Nexus preview guidance

Nexus is still a public preview feature in the .NET SDK. The implementation and README should say that the API surface may change before stable release.

The implementation should use the current Nexus preview pattern:

- define Nexus service contracts as interfaces at the Nexus boundary only
- register Nexus handlers in the worker
- call Nexus from workflows through `Workflow.CreateNexusWorkflowClient<T>()`
- use explicit operation timeouts and cancellation options

### Namespace layout

Use separate namespaces so the demo clearly shows a cross-boundary call:

```text
storefront
inventory
payment
fulfillment
```

### Workflow rules

Temporal workflows in .NET should follow the SDK's deterministic execution model:

- workflows are concrete classes marked with `[Workflow]`
- a single async run method uses `[WorkflowRun]`
- workflow inputs should be serializable DTOs or records
- workflow logic must not perform IO, use random behavior, or depend on wall-clock time
- avoid `Task.Run`, `Task.Delay`, `Task.WhenAny`, `Task.WhenAll`, and other APIs that can drift away from the deterministic scheduler
- use `Workflow.DelayAsync`, `Workflow.WaitConditionAsync`, `Workflow.WhenAnyAsync`, and `Workflow.WhenAllAsync` instead
- use workflow-friendly coordination primitives when needed

Workflow files should use a `.workflow.cs` suffix so workflow-specific analyzer rules can be scoped cleanly.

### Analyzer guidance

Add workflow-specific `.editorconfig` rules to avoid false friction from standard C# analyzers. The workflow code should allow the Temporal style that looks unusual in normal C#:

- queries may look like getters
- workflow methods are instance methods even when they could be static
- `ConfigureAwait(true)` is the only relevant form if it is used at all
- deterministic random and scheduler behavior is intentional

## 6. Workflow model

### PurchaseIntentWorkflow

`PurchaseIntentWorkflow` is the long-lived anchor for the customer journey.

Responsibilities:

- track the anonymous purchase intent by cookie
- hold basket items in workflow state
- remember email state and verification state
- track recovery timing
- start checkout
- remember checkout status and result

Suggested workflow ID:

```text
purchase-intent-{purchaseIntentId}
```

Suggested cookie name:

```text
demo_purchase_intent_id
```

### CheckoutWorkflow

`CheckoutWorkflow` owns the purchase finalization process.

Responsibilities:

- take a snapshot of the purchase intent
- wait for required business conditions
- coordinate email verification if needed
- call inventory, payment, and fulfillment Nexus services
- send order confirmation email
- signal the purchase intent when checkout completes

The checkout workflow should not depend on mutable basket state after it starts.

### EmailVerificationWorkflow

`EmailVerificationWorkflow` owns the verification token, MailPit email, callback, and timeout.

It is not a Nexus service. It is an internal storefront workflow.

## 7. API and UI

### API

The API should be a thin Minimal API layer over workflow state and commands.

It should:

- resolve the current cookie/session
- return current purchase intent state
- accept basket updates
- start checkout
- expose checkout status
- accept email verification callbacks

### UI

The UI should be a simple React/Vite app using shadcn/ui and Tailwind.

It should show:

- product grid
- basket panel
- email capture
- shipping address form
- payment method form
- checkout button
- checkout status
- a link or hint to open MailPit

The UI should stay clean and direct. It is not a workflow cockpit.

## 8. Error handling

Business failures and technical failures should be treated differently.

- Business failures should return normal domain results.
- Technical failures should throw and retry through Temporal.
- Random behavior belongs inside activities or handlers, never inside workflow code.

The spec does not require a large failure matrix for V1. The demo only needs enough error handling to keep the journey understandable and recoverable.

## 9. Observability

Use Aspire and Temporal UI as the main observability surfaces.

Log the important milestones:

- purchase intent created
- item added
- email verification requested
- verification email sent
- email verified
- recovery email sent
- checkout started
- checkout waiting for conditions
- inventory reservation requested
- payment authorization requested
- fulfillment requested
- checkout completed
- checkout failed

Avoid logging raw email addresses or payment details.

## 10. Manual verification

V1 does not require an automated test suite.

Manual demo scenarios should be documented in the README:

1. Happy path from basket to successful checkout.
2. Checkout started before email verification.
3. Recovery email after verified inactivity.
4. Temporal UI inspection of workflow history and Nexus calls.

## 11. Acceptance criteria

The feature is done when:

- the repo runs locally through an Aspire AppHost project
- the solution includes a separate StandardDefaults project
- the demo uses Temporal .NET Nexus preview APIs correctly
- workflow code follows Temporal's determinism rules
- checkout crosses namespace boundaries through Nexus
- MailPit handles verification and recovery email
- the README explains how to run and inspect the demo locally

## 12. Implementation brief

When implementation starts, keep the smallest possible surface area:

- keep the AppHost project-based
- replace the current standalone `apphost.cs` entry point with the AppHost project
- keep the demo local-only
- keep the checkout journey focused on the core purchase flow
- keep workflow code deterministic and isolated
- keep the spec aligned with the current Temporal Nexus preview docs
