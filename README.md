# SellerSync - Temporal Workflow Demo

A demonstration of how Temporal Technologies can replace synchronous webhook processing with durable, fault-tolerant workflows.

## Overview

This project replaces the synchronous `HubSpotService` with a Temporal-based workflow that:
- Creates a company in HubSpot CRM
- Updates the seller in Marketplacer with the HubSpot ID
- Automatically retries failed operations with exponential backoff
- Survives process crashes and resumes from where it left off

## Prerequisites

- .NET 10.0 SDK
- Temporal CLI (`brew install temporal` or [download](https://github.com/temporalio/cli/releases))
- HubSpot API token (for real HubSpot integration)

## Project Structure

```
SellerSync.Workflows/    # Shared: Workflow & Activity definitions + DTOs
SellerSync.Client/       # ASP.NET Web API (port 5200) - receives webhooks, starts workflows
SellerSync.Worker/       # Temporal worker (no HTTP) - executes workflows and activities
```

## Quick Start

### 1. Start Temporal Server

```bash
temporal server start-dev
```

This starts:
- Temporal Server on `localhost:7233`
- Temporal Web UI on `localhost:8233`

### 2. Configure HubSpot Token (Optional)

```bash
cd SellerSync.Worker
dotnet user-secrets init
dotnet user-secrets set "HubSpotToken" "your-hubspot-api-token"
```

### 3. Run the Worker

```bash
dotnet run --project SellerSync.Worker
```

The worker connects to Temporal and polls for work (no HTTP port).

### 4. Run the Client (API)

```bash
dotnet run --project SellerSync.Client
```

The client starts on `http://localhost:5200`

### 5. Run Marketplacer (for end-to-end testing)

Update `marketplacer/appsettings.Development.json`:
```json
"HubSpotServiceWebhookEndpoint": "http://localhost:5200/api/webhooks"
```

Then:
```bash
dotnet run --project marketplacer
```

### 6. Create a Seller

```bash
curl -X POST http://localhost:5027/api/sellers \
  -H "Content-Type: application/json" \
  -d '{"sellerName":"Acme Corp","sellerDomain":"acme.com","sellerIndustry":"TECH","sellerPhone":"555-1234"}'
```

### 7. Watch the Workflow

Open the Temporal UI at http://localhost:8233 to see the workflow execute.

## API Endpoints (SellerSync.Client - port 5200)

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/` | GET | Health check / info |
| `/api/ping` | GET | Test endpoint - triggers a simple ping workflow |
| `/api/webhooks` | POST | Receives webhooks from Marketplacer, starts SellerCreatedWorkflow |
| `/api/workflows/{id}` | GET | Check workflow status |

**Note:** The Worker project has no HTTP endpoints - it only polls Temporal for work.

## Chaos Testing Demo

The workflow includes a 10-second delay to allow time for chaos testing:

1. Create a seller via Marketplacer
2. Immediately stop Marketplacer (Ctrl+C)
3. Watch the Temporal UI - the workflow will show retrying the Marketplacer callback
4. Restart Marketplacer
5. The workflow completes automatically!

**Key point:** No data was lost. No manual intervention needed.

## Architecture

```
┌─────────────┐     webhook     ┌─────────────────┐
│ Marketplacer │ ──────────────▶│ SellerSync.Client│
│  (port 5027) │                │   (port 5200)    │
└─────────────┘                └────────┬─────────┘
       ▲                                │
       │                                │ starts workflow
       │                                ▼
       │                       ┌─────────────────┐
       │                       │  Temporal Server │
       │                       │   (port 7233)    │
       │                       └────────┬─────────┘
       │                                │
       │                                │ dispatches to
       │                                ▼
       │                       ┌─────────────────┐
       │                       │SellerSync.Worker │
       │                       │  (no HTTP port)  │
       │                       └────────┬─────────┘
       │                                │
       │                                │ executes
       │                                ▼
       │                       ┌─────────────────┐
       │    PUT /sellers/{id}  │SellerCreated    │
       │◀──────────────────────│    Workflow     │
       │                       │                 │
       │                       │ 1. HubSpot API  │──────▶ HubSpot
       │                       │ 2. Marketplacer │
       │                       └─────────────────┘
```

## Key Temporal Concepts Demonstrated

| Concept | Where | Description |
|---------|-------|-------------|
| **Workflow** | `SellerCreatedWorkflow.cs` | Orchestrates the multi-step process |
| **Activity** | `HubSpotActivities.cs` | Performs the actual API calls |
| **RetryPolicy** | Workflow | Automatic retry with exponential backoff |
| **Durable Timer** | `Workflow.DelayAsync()` | Demo delay that survives restarts |
| **Idempotency** | Workflow ID = IdempotencyKey | Prevents duplicate processing |

## Comparison: Before vs After

| Aspect | HubSpotService (Before) | SellerSync (After) |
|--------|------------------------|-------------------|
| Error handling | None - crashes on failure | Automatic retry with backoff |
| Process crash | Data lost mid-request | Resumes from where it left off |
| Visibility | Console.WriteLine only | Full execution history in UI |
| Idempotency | Key exists but not enforced | Enforced via workflow ID |
| Timeout handling | None | Configurable per activity |
