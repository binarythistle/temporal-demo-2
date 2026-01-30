# SellerSync - Temporal Workflow Demo

A demonstration of how Temporal Technologies can replace synchronous webhook processing with durable, fault-tolerant workflows.

## Overview

This project replaces the synchronous `HubSpotService` (the _before code_) with a Temporal-based workflow that:
- Creates a company in HubSpot CRM
- Updates the seller in [Marketplacer](https://marketplacer.com/) with the HubSpot ID
- Automatically retries failed operations with exponential backoff
- Survives process crashes and resumes from where it left off

### Business Context

Marketplacer is a SaaS platform that hosts Sellers and the Sellers Products. Products can be surfaced by an _Operator_ for sale on their marketplace, and any associated orders for those products flow back through Marketplacer to be actioned by each seller.

The _seller onboarding_ process is critical to the successful operation of the marketplace. In the workflow we look at today, Sellers are created in Marketplacer but must also be created in the Operaors CRM, (in this case HubSpot). Moreover, an association between the objects created in both systems must exist.

<img width="507" height="163" alt="2026-01-30_12-29-05" src="https://github.com/user-attachments/assets/7be294ef-0170-4a69-acb0-12aee098c6e3" />

> [!NOTE]
> In order for the Seller to be considered successfully created in Marketplacer, it must have an associate HubSpot Id. This success condition is therefore depended on a distributed workflow.



## Prerequisites

- .NET 10.0 SDK
- Temporal CLI (`brew install temporal` or [download](https://github.com/temporalio/cli/releases))
- HubSpot API token (for real HubSpot integration)
- Clone this repository to your machine

> [!TIP]
> Set up instructions on how to set up a _Private Legacy App_ in HubSpot can be found [here](https://developers.hubspot.com/docs/apps/legacy-apps/private-apps/overview). When selecting the _scopes_ that you need, select: `crm.objects.companies.read` and `crm.objects.companies.write`. The _Access Token_ is what you need to use to follow along.

## Project Structure

```
HubSpotService           # The legacy or before code, replaced by the 3 Seller.* projects
SellerSync.Workflows/    # Shared: Workflow & Activity definitions + DTOs
SellerSync.Client/       # ASP.NET Web API (port 5200) - receives webhooks, starts workflows
SellerSync.Worker/       # Temporal worker (no HTTP) - executes workflows and activities
marketplacer             # Mock service (create sellers here and auto generate webhooks)
```

## Starting Marketplacer

The Marketplacer Mock service is recommended for creating sellers and generating webhooks irrespective of whether you are runing the legacy or Temporal solution. It also acts as the API endpoint that allows for the updating of the Marketplacer Seller object with the HubSpot Id.

### 1. Run database migrations



To set up, navigate to the `marketplacer` project folder and run the database migrations to create the Sqlite DB:

> [!TIP]
> You'll need **EF Core** tools for this bit, to install them: `dotnet tool install --global dotnet-ef`, suggest you also to build the `marketplacer` project prior to running migrations: `dotnet build`: 

```bash
dotnet ef database update
```

### 2. Check webhook endpoint configuration

Check `appsettings.Development.json` has the following entry:

```json
"HubSpotServiceWebhookEndpoint" : "http://localhost:5200/api/webhooks",
```

This is the webhook destination endpoint.

> [!WARNING]
> **Both** the **HubSpotService** and the **SellerSync.Client** start on port `5200` to allow for easy movement between them during the demo. Ensure only 1 is running at a time.

### 3. Run

You can now run up the project:

```bash
dotnet run
```

Navigate to: `http://localhost:5027/` to see the UI.

## HubSpotService Quick Start

> [!NOTE]
> If you're not interested in running the legacy code, jump to the **Temporal Quick Start** 

### 1. Build the HubSpotSerivce project

Navigate to the `HubSpotService` project and build it:

```bash
dotnet build
```

### 2. Run database migrations

Execute the database migrations to set up the Sqlite Db.

> [!TIP]
> Mentioning again, you'll need **EF Core** tools, to install them: `dotnet tool install --global dotnet-ef`

```bash
dotnet ef database update
```

### 3. Set up user secrets

Next you'll need to add the HubSpot Access Token as a user secret:

```bash
dotnet user-secrets init
dotnet user-secrets set "HubSpotToken" "<your-hubspot-api-token>"
```

### 4. Run

You can now run the project:

```bash
dotnet run
```

Navigate to: `http://localhost:5200/` to see the UI.

### 5. Create Sellers

You can create Sellers from the Marketplacer service here: http://localhost:5027/.

> [!IMPORTANT]
> There is an intentional 10s in the HubSpotService service before it calls back to Marketplacer (giving the user time to kill Marketplacer for demo purposes). As this is a synchronous operation, there is a delay in the Marketplacer service UI updating.

## Temporal Quick Start

> [!WARNING]
> If you've run the HubSpotLegacy service, be sure to kill it before continuing as it uses the same port (`5200`) as the Temporal Client.

### 1. Start Temporal Server

```bash
temporal server start-dev
```

This starts:
- Temporal Server on `localhost:7233`
- Temporal Web UI on `localhost:8233`

### 2. Configure HubSpot Token

```bash
cd SellerSync.Worker
dotnet user-secrets init
dotnet user-secrets set "HubSpotToken" "<your-hubspot-api-token>"
```

### 3. Run the Worker

```bash
dotnet run
```

The worker connects to Temporal and polls for work (no HTTP port).

### 4. Run the Client (API)

```bash
cd SellerSync.Client
dotnet run
```

The client starts on `http://localhost:5200`


### 5. Create a Seller

Use the **Marketplacer** service (http://localhost:5027/.) to create a Seller.


### 6. Watch the Workflow

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

### Test 1: Kill Marketplacer

The Temporal workflow includes a 10-second delay to allow time for chaos testing:

1. Create a seller via Marketplacer
2. Immediately stop Marketplacer (Ctrl+C)
3. Watch the Temporal UI - the workflow will show retrying the Marketplacer callback
4. Restart Marketplacer
5. The workflow completes automatically

**Key point:** No data was lost. No manual intervention needed.

## Architecture

<img width="646" height="548" alt="2026-01-30_13-23-25" src="https://github.com/user-attachments/assets/8c56226c-6934-4635-8495-1727d936a548" />


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
