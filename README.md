# temporal-nexus-demo

Local .NET 10 shopping basket demo showing Temporal Nexus across separate namespaces with Aspire orchestration, MailPit email flows, and a React/Vite storefront.

## What this demo shows

- Anonymous purchase intent tracking with a browser cookie
- Email verification and confirmation emails through MailPit
- A condition-gated checkout workflow in the `storefront` namespace
- Nexus calls from checkout into `inventory`, `payment`, and `fulfillment`
- Local observability through Aspire, Temporal UI, and worker logs

## Solution layout

```text
src/
  ShoppingBasket.AppHost/
  ShoppingBasket.StandardDefaults/
  ShoppingBasket.Contracts/
  ShoppingBasket.NexusContracts/
  Storefront.Api/
  Storefront.Ui/
  PurchaseIntent.Worker/
  Checkout.Worker/
  Inventory.Worker/
  Payment.Worker/
  Fulfillment.Worker/
```

## Prerequisites

- .NET SDK 10.0.301
- Docker Desktop or compatible container runtime for Aspire resources
- Node.js with pnpm

## Build

```powershell
dotnet build Temporal.Nexus.ShoppingBasket.slnx /nodeReuse:false

Set-Location .\src\Storefront.Ui
pnpm install
pnpm build
```

If Windows leaves an MSBuild/Roslyn process holding a build output, run:

```powershell
dotnet build-server shutdown
```

## Run the demo

```powershell
aspire run
```

Or start the AppHost directly:

```powershell
dotnet run --project .\src\ShoppingBasket.AppHost\ShoppingBasket.AppHost.csproj
```

Useful endpoints after startup:

- Aspire dashboard: launched by AppHost
- Storefront UI: `http://localhost:5173`
- MailPit: check the AppHost resource endpoint
- Temporal UI: check the `temporal` resource endpoint in Aspire

## Demo walkthrough

1. Open the storefront UI.
2. Add one or more demo products.
3. Enter an email address.
4. Open MailPit and click the verification link.
5. Start checkout.
6. Enter shipping and payment details.
7. Watch checkout progress in the UI and inspect workflows in Temporal UI.
8. Repeat checkout attempts to hit the simulated periodic payment decline path.

## Namespace model

- `storefront`: purchase intent, checkout, email verification
- `inventory`: inventory Nexus handler
- `payment`: payment Nexus handler
- `fulfillment`: fulfillment Nexus handler

## Notes

- Temporal Nexus support is still preview in the .NET SDK, so APIs may change.
- Nexus endpoints are created automatically by `Storefront.Api` at startup if they do not already exist.
- MailKit 4.13.0 currently restores with known moderate vulnerability warnings because this demo intentionally follows the requested package version.

## AI agent setup

- Root guidance: `AGENTS.MD`
- Minimal Copilot router: `.github/copilot-instructions.md`
- Scoped instructions: `.github/instructions/*.instructions.md`
- Modern skill location: `.agents/skills`
- Copilot cloud setup workflow: `.github/workflows/copilot-setup-steps.yml`
