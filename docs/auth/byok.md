# BYOK (Bring Your Own Key)

BYOK allows you to use the Copilot SDK with your own API keys from model providers, bypassing GitHub Copilot authentication. This is useful for enterprise deployments, custom model hosting, or when you want direct billing with your model provider.

## Supported Providers

| Provider | Type Value | Notes |
|----------|------------|-------|
| OpenAI | `"openai"` | OpenAI API and OpenAI-compatible endpoints |
| Azure OpenAI / Azure AI Foundry | `"azure"` | Azure-hosted models |
| Anthropic | `"anthropic"` | Claude models |
| Ollama | `"openai"` | Local models via OpenAI-compatible API |
| Microsoft Foundry Local | `"openai"` | Run AI models locally on your device via OpenAI-compatible API |
| Other OpenAI-compatible | `"openai"` | vLLM, LiteLLM, etc. |

## Quick Start: Azure AI Foundry

Azure AI Foundry (formerly Azure OpenAI) is a common BYOK deployment target for enterprises. Here's a complete example:

<details open>
<summary><strong>Python</strong></summary>

```python
import asyncio
import os
from copilot import CopilotClient
from copilot.session import PermissionHandler

FOUNDRY_MODEL_URL = "https://your-resource.openai.azure.com/openai/v1/"
# Set FOUNDRY_API_KEY environment variable

async def main():
    client = CopilotClient()
    await client.start()

    session = await client.create_session(on_permission_request=PermissionHandler.approve_all, model="gpt-5.2-codex", provider={
        "type": "openai",
        "base_url": FOUNDRY_MODEL_URL,
        "wire_api": "responses",  # Use "completions" for older models
        "api_key": os.environ["FOUNDRY_API_KEY"],
    })

    done = asyncio.Event()

    def on_event(event):
        if event.type.value == "assistant.message":
            print(event.data.content)
        elif event.type.value == "session.idle":
            done.set()

    session.on(on_event)
    await session.send({"prompt": "What is 2+2?"})
    await done.wait()

    await session.disconnect()
    await client.stop()

asyncio.run(main())
```

</details>

<details>
<summary><strong>Node.js / TypeScript</strong></summary>

```typescript
import { CopilotClient } from "@github/copilot-sdk";

const FOUNDRY_MODEL_URL = "https://your-resource.openai.azure.com/openai/v1/";

const client = new CopilotClient();
const session = await client.createSession({
    model: "gpt-5.2-codex",  // Your deployment name
    provider: {
        type: "openai",
        baseUrl: FOUNDRY_MODEL_URL,
        wireApi: "responses",  // Use "completions" for older models
        apiKey: process.env.FOUNDRY_API_KEY,
    },
});

session.on("assistant.message", (event) => {
    console.log(event.data.content);
});

await session.sendAndWait({ prompt: "What is 2+2?" });
await client.stop();
```

</details>

<details>
<summary><strong>Go</strong></summary>

```go
package main

import (
    "context"
    "fmt"
    "os"
    copilot "github.com/github/copilot-sdk/go"
)

func main() {
    ctx := context.Background()
    client := copilot.NewClient(nil)
    if err := client.Start(ctx); err != nil {
        panic(err)
    }
    defer client.Stop()

    session, err := client.CreateSession(ctx, &copilot.SessionConfig{
        Model: "gpt-5.2-codex",  // Your deployment name
        Provider: &copilot.ProviderConfig{
            Type:    "openai",
            BaseURL: "https://your-resource.openai.azure.com/openai/v1/",
            WireApi: "responses",  // Use "completions" for older models
            APIKey:  os.Getenv("FOUNDRY_API_KEY"),
        },
    })
    if err != nil {
        panic(err)
    }

    response, err := session.SendAndWait(ctx, copilot.MessageOptions{
        Prompt: "What is 2+2?",
    })
    if err != nil {
        panic(err)
    }

    fmt.Println(*response.Data.Content)
}
```

</details>

<details>
<summary><strong>.NET</strong></summary>

```csharp
using GitHub.Copilot.SDK;

await using var client = new CopilotClient();
await using var session = await client.CreateSessionAsync(new SessionConfig
{
    Model = "gpt-5.2-codex",  // Your deployment name
    Provider = new ProviderConfig
    {
        Type = "openai",
        BaseUrl = "https://your-resource.openai.azure.com/openai/v1/",
        WireApi = "responses",  // Use "completions" for older models
        ApiKey = Environment.GetEnvironmentVariable("FOUNDRY_API_KEY"),
    },
});

var response = await session.SendAndWaitAsync(new MessageOptions
{
    Prompt = "What is 2+2?",
});
Console.WriteLine(response?.Data.Content);
```

</details>

## Provider Configuration Reference

### ProviderConfig Fields

| Field | Type | Description |
|-------|------|-------------|
| `type` | `"openai"` \| `"azure"` \| `"anthropic"` | Provider type (default: `"openai"`) |
| `baseUrl` / `base_url` | string | **Required.** API endpoint URL |
| `apiKey` / `api_key` | string | API key (optional for local providers like Ollama) |
| `bearerToken` / `bearer_token` | string | Bearer token auth (takes precedence over apiKey) |
| `wireApi` / `wire_api` | `"completions"` \| `"responses"` | API format (default: `"completions"`) |
| `headers` | `Record<string, string>` | Custom HTTP headers for all outbound requests ([details](#custom-headers)) |
| `azure.apiVersion` / `azure.api_version` | string | Azure API version (default: `"2024-10-21"`) |

### Wire API Format

The `wireApi` setting determines which OpenAI API format to use:

- **`"completions"`** (default) - Chat Completions API (`/chat/completions`). Use for most models.
- **`"responses"`** - Responses API. Use for GPT-5 series models that support the newer responses format.

### Type-Specific Notes

**OpenAI (`type: "openai"`)**
- Works with OpenAI API and any OpenAI-compatible endpoint
- `baseUrl` should include the full path (e.g., `https://api.openai.com/v1`)

**Azure (`type: "azure"`)**
- Use for native Azure OpenAI endpoints
- `baseUrl` should be just the host (e.g., `https://my-resource.openai.azure.com`)
- Do NOT include `/openai/v1` in the URL—the SDK handles path construction

**Anthropic (`type: "anthropic"`)**
- For direct Anthropic API access
- Uses Claude-specific API format

## Example Configurations

### OpenAI Direct

```typescript
provider: {
    type: "openai",
    baseUrl: "https://api.openai.com/v1",
    apiKey: process.env.OPENAI_API_KEY,
}
```

### Azure OpenAI (Native Azure Endpoint)

Use `type: "azure"` for endpoints at `*.openai.azure.com`:

```typescript
provider: {
    type: "azure",
    baseUrl: "https://my-resource.openai.azure.com",  // Just the host
    apiKey: process.env.AZURE_OPENAI_KEY,
    azure: {
        apiVersion: "2024-10-21",
    },
}
```

### Azure AI Foundry (OpenAI-Compatible Endpoint)

For Azure AI Foundry deployments with `/openai/v1/` endpoints, use `type: "openai"`:

```typescript
provider: {
    type: "openai",
    baseUrl: "https://your-resource.openai.azure.com/openai/v1/",
    apiKey: process.env.FOUNDRY_API_KEY,
    wireApi: "responses",  // For GPT-5 series models
}
```

### Ollama (Local)

```typescript
provider: {
    type: "openai",
    baseUrl: "http://localhost:11434/v1",
    // No apiKey needed for local Ollama
}
```

### Microsoft Foundry Local

[Microsoft Foundry Local](https://foundrylocal.ai) lets you run AI models locally on your own device with an OpenAI-compatible API. Install it via the Foundry Local CLI, then point the SDK at your local endpoint:

```typescript
provider: {
    type: "openai",
    baseUrl: "http://localhost:<PORT>/v1",
    // No apiKey needed for local Foundry Local
}
```

> **Note:** Foundry Local starts on a **dynamic port** — the port is not fixed. Use `foundry service status` to confirm the port the service is currently listening on, then use that port in your `baseUrl`.

To get started with Foundry Local:

```bash
# Windows: Install Foundry Local CLI (requires winget)
winget install Microsoft.FoundryLocal

# macOS / Linux: see https://foundrylocal.ai for installation instructions
# List available models
foundry model list

# Run a model (starts the local server automatically)
foundry model run phi-4-mini

# Check the port the service is running on
foundry service status
```

### Anthropic

```typescript
provider: {
    type: "anthropic",
    baseUrl: "https://api.anthropic.com",
    apiKey: process.env.ANTHROPIC_API_KEY,
}
```

### Bearer Token Authentication

Some providers require bearer token authentication instead of API keys:

```typescript
provider: {
    type: "openai",
    baseUrl: "https://my-custom-endpoint.example.com/v1",
    bearerToken: process.env.MY_BEARER_TOKEN,  // Sets Authorization header
}
```

> **Note:** The `bearerToken` option accepts a **static token string** only. The SDK does not refresh this token automatically. If your token expires, requests will fail and you'll need to create a new session with a fresh token.

## Custom Headers

Custom headers let you attach additional HTTP headers to every outbound model request. This is useful when your provider endpoint sits behind an API gateway or proxy that requires extra authentication or routing headers.

### Use Cases

| Scenario | Example Header |
|----------|---------------|
| Azure API Management / AI Gateway | `Ocp-Apim-Subscription-Key` |
| Cloudflare Tunnel authentication | `CF-Access-Client-Id`, `CF-Access-Client-Secret` |
| Custom API gateways with proprietary auth | `X-Gateway-Auth`, `X-Tenant-Id` |
| BYOK routing through enterprise proxies | `X-Proxy-Authorization`, `X-Route-Target` |

### Session-Level Headers

Set `headers` on `ProviderConfig` when creating a session. These headers are included in **every** outbound request for the lifetime of the session.

<details open>
<summary><strong>Node.js / TypeScript</strong></summary>

```typescript
import { CopilotClient } from "@github/copilot-sdk";

const client = new CopilotClient();
const session = await client.createSession({
    model: "gpt-4.1",
    provider: {
        type: "openai",
        baseUrl: "https://my-gateway.example.com/v1",
        apiKey: process.env.OPENAI_API_KEY,
        headers: {
            "Ocp-Apim-Subscription-Key": process.env.APIM_KEY!,
            "X-Tenant-Id": "my-team",
        },
    },
});
```

</details>

<details>
<summary><strong>Python</strong></summary>

```python
import os
from copilot import CopilotClient

client = CopilotClient()
await client.start()

session = await client.create_session(
    model="gpt-4.1",
    provider={
        "type": "openai",
        "base_url": "https://my-gateway.example.com/v1",
        "api_key": os.environ["OPENAI_API_KEY"],
        "headers": {
            "Ocp-Apim-Subscription-Key": os.environ["APIM_KEY"],
            "X-Tenant-Id": "my-team",
        },
    },
)
```

</details>

<details>
<summary><strong>Go</strong></summary>

```go
session, err := client.CreateSession(ctx, &copilot.SessionConfig{
    Model: "gpt-4.1",
    Provider: &copilot.ProviderConfig{
        Type:    "openai",
        BaseURL: "https://my-gateway.example.com/v1",
        APIKey:  os.Getenv("OPENAI_API_KEY"),
        Headers: map[string]string{
            "Ocp-Apim-Subscription-Key": os.Getenv("APIM_KEY"),
            "X-Tenant-Id":              "my-team",
        },
    },
})
```

</details>

<details>
<summary><strong>.NET</strong></summary>

```csharp
var session = await client.CreateSessionAsync(new SessionConfig
{
    Model = "gpt-4.1",
    Provider = new ProviderConfig
    {
        Type = "openai",
        BaseUrl = "https://my-gateway.example.com/v1",
        ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY"),
        Headers = new Dictionary<string, string>
        {
            ["Ocp-Apim-Subscription-Key"] = Environment.GetEnvironmentVariable("APIM_KEY")!,
            ["X-Tenant-Id"] = "my-team",
        },
    },
});
```

</details>

### Per-Turn Headers

Pass `requestHeaders` on `send()` to include headers for a **single turn** only. This is useful when headers change between requests (e.g., per-request trace IDs or rotating tokens).

<details open>
<summary><strong>Node.js / TypeScript</strong></summary>

```typescript
await session.send({
    prompt: "Summarize this document",
    requestHeaders: {
        "X-Request-Id": crypto.randomUUID(),
    },
});
```

</details>

<details>
<summary><strong>Python</strong></summary>

```python
import uuid

await session.send(
    "Summarize this document",
    request_headers={
        "X-Request-Id": str(uuid.uuid4()),
    },
)
```

</details>

<details>
<summary><strong>Go</strong></summary>

```go
_, err := session.Send(ctx, copilot.MessageOptions{
    Prompt: "Summarize this document",
    RequestHeaders: map[string]string{
        "X-Request-Id": uuid.NewString(),
    },
})
```

</details>

<details>
<summary><strong>.NET</strong></summary>

```csharp
await session.SendAsync(new MessageOptions
{
    Prompt = "Summarize this document",
    RequestHeaders = new Dictionary<string, string>
    {
        ["X-Request-Id"] = Guid.NewGuid().ToString(),
    },
});
```

</details>

### Header Merge Strategy

When you provide both session-level `headers` and per-turn `requestHeaders`, the `headerMergeStrategy` controls how they combine.

| Strategy | Behavior |
|----------|----------|
| `"override"` (default) | Per-turn headers **completely replace** session-level headers. No session headers are sent for that turn. This is the safest default — no unexpected header leakage. |
| `"merge"` | Per-turn headers are **merged** with session-level headers. Per-turn values win on key conflicts. |

#### Override (Default)

```typescript
// Session created with headers: { "X-Team": "alpha", "X-Env": "prod" }

await session.send({
    prompt: "Hello",
    requestHeaders: { "X-Request-Id": "abc-123" },
    // headerMergeStrategy defaults to "override"
});
// Only "X-Request-Id" is sent — session headers are NOT included
```

#### Merge

```typescript
// Session created with headers: { "X-Team": "alpha", "X-Env": "prod" }

await session.send({
    prompt: "Hello",
    requestHeaders: { "X-Env": "staging", "X-Request-Id": "abc-123" },
    headerMergeStrategy: "merge",
});
// Sent headers: { "X-Team": "alpha", "X-Env": "staging", "X-Request-Id": "abc-123" }
// "X-Env" from per-turn wins over session-level value
```

The merge strategy setting is available in all languages:

| Language | Field |
|----------|-------|
| TypeScript | `headerMergeStrategy: "override" \| "merge"` |
| Python | `header_merge_strategy: Literal["override", "merge"]` |
| Go | `HeaderMergeStrategy: copilot.HeaderMergeStrategyOverride \| copilot.HeaderMergeStrategyMerge` |
| C# | `HeaderMergeStrategy = HeaderMergeStrategy.Override \| HeaderMergeStrategy.Merge` |

### Updating Provider Configuration Mid-Session

Use `updateProvider()` to change provider configuration — including headers — between turns without recreating the session. This is useful for rotating API keys, switching tenants, or adjusting gateway headers on the fly.

<details open>
<summary><strong>Node.js / TypeScript</strong></summary>

```typescript
// Rotate the subscription key between turns
await session.updateProvider({
    headers: {
        "Ocp-Apim-Subscription-Key": newSubscriptionKey,
        "X-Tenant-Id": "new-team",
    },
});

// Subsequent sends use the updated headers
await session.send({ prompt: "Continue" });
```

</details>

<details>
<summary><strong>Python</strong></summary>

```python
await session.update_provider({
    "headers": {
        "Ocp-Apim-Subscription-Key": new_subscription_key,
        "X-Tenant-Id": "new-team",
    },
})

await session.send("Continue")
```

</details>

<details>
<summary><strong>Go</strong></summary>

```go
err := session.UpdateProvider(ctx, copilot.ProviderConfig{
    Headers: map[string]string{
        "Ocp-Apim-Subscription-Key": newSubscriptionKey,
        "X-Tenant-Id":              "new-team",
    },
})

_, err = session.Send(ctx, copilot.MessageOptions{Prompt: "Continue"})
```

</details>

<details>
<summary><strong>.NET</strong></summary>

```csharp
await session.UpdateProviderAsync(new ProviderConfig
{
    Headers = new Dictionary<string, string>
    {
        ["Ocp-Apim-Subscription-Key"] = newSubscriptionKey,
        ["X-Tenant-Id"] = "new-team",
    },
});

await session.SendAsync(new MessageOptions { Prompt = "Continue" });
```

</details>

### Environment Variable Expansion

Header values support environment variable expansion at the runtime level. This lets you reference secrets without hardcoding them in your application code.

| Syntax | Behavior |
|--------|----------|
| `${VAR}` | Replaced with the value of `VAR`. Fails if `VAR` is not set. |
| `$VAR` | Same as `${VAR}`. |
| `${VAR:-default}` | Replaced with the value of `VAR`, or `default` if `VAR` is not set. |

```typescript
provider: {
    type: "openai",
    baseUrl: "https://my-gateway.example.com/v1",
    headers: {
        // Expanded at runtime from the APIM_KEY environment variable
        "Ocp-Apim-Subscription-Key": "${APIM_KEY}",
        // Falls back to "default-tenant" if X_TENANT is not set
        "X-Tenant-Id": "${X_TENANT:-default-tenant}",
    },
}
```

> **Note:** Expansion is performed by the CLI server, not the SDK client. The SDK passes header values as-is to the server, which resolves environment variables before sending requests to your provider.

### Security Considerations

- **Scoped to your endpoint** — Custom headers are sent only to the configured `baseUrl`. They are never sent to GitHub Copilot servers or other endpoints.
- **Prefer env var expansion** — Use `${VAR}` syntax for sensitive values like API keys and tokens rather than hardcoding them. This avoids secrets in source code and logs.
- **Override is the safe default** — The default `headerMergeStrategy` of `"override"` ensures per-turn headers completely replace session-level headers, preventing accidental leakage of session headers into turns that specify their own.

## Custom Model Listing

When using BYOK, the CLI server may not know which models your provider supports. You can supply a custom `onListModels` handler at the client level so that `client.listModels()` returns your provider's models in the standard `ModelInfo` format. This lets downstream consumers discover available models without querying the CLI.

<details open>
<summary><strong>Node.js / TypeScript</strong></summary>

```typescript
import { CopilotClient } from "@github/copilot-sdk";
import type { ModelInfo } from "@github/copilot-sdk";

const client = new CopilotClient({
    onListModels: () => [
        {
            id: "my-custom-model",
            name: "My Custom Model",
            capabilities: {
                supports: { vision: false, reasoningEffort: false },
                limits: { max_context_window_tokens: 128000 },
            },
        },
    ],
});
```

</details>

<details>
<summary><strong>Python</strong></summary>

```python
from copilot import CopilotClient
from copilot.client import ModelInfo, ModelCapabilities, ModelSupports, ModelLimits

client = CopilotClient({
    "on_list_models": lambda: [
        ModelInfo(
            id="my-custom-model",
            name="My Custom Model",
            capabilities=ModelCapabilities(
                supports=ModelSupports(vision=False, reasoning_effort=False),
                limits=ModelLimits(max_context_window_tokens=128000),
            ),
        )
    ],
})
```

</details>

<details>
<summary><strong>Go</strong></summary>

```go
package main

import (
    "context"
    copilot "github.com/github/copilot-sdk/go"
)

func main() {
    client := copilot.NewClient(&copilot.ClientOptions{
        OnListModels: func(ctx context.Context) ([]copilot.ModelInfo, error) {
            return []copilot.ModelInfo{
                {
                    ID:   "my-custom-model",
                    Name: "My Custom Model",
                    Capabilities: copilot.ModelCapabilities{
                        Supports: copilot.ModelSupports{Vision: false, ReasoningEffort: false},
                        Limits:   copilot.ModelLimits{MaxContextWindowTokens: 128000},
                    },
                },
            }, nil
        },
    })
    _ = client
}
```

</details>

<details>
<summary><strong>.NET</strong></summary>

```csharp
using GitHub.Copilot.SDK;

var client = new CopilotClient(new CopilotClientOptions
{
    OnListModels = (ct) => Task.FromResult(new List<ModelInfo>
    {
        new()
        {
            Id = "my-custom-model",
            Name = "My Custom Model",
            Capabilities = new ModelCapabilities
            {
                Supports = new ModelSupports { Vision = false, ReasoningEffort = false },
                Limits = new ModelLimits { MaxContextWindowTokens = 128000 }
            }
        }
    })
});
```

</details>

Results are cached after the first call, just like the default behavior. The handler completely replaces the CLI's `models.list` RPC — no fallback to the server occurs.

## Limitations

When using BYOK, be aware of these limitations:

### Identity Limitations

BYOK authentication uses **static credentials only**. The following identity providers are NOT supported:

- ❌ **Microsoft Entra ID (Azure AD)** - No support for Entra managed identities or service principals
- ❌ **Third-party identity providers** - No OIDC, SAML, or other federated identity
- ❌ **Managed identities** - Azure Managed Identity is not supported

You must use an API key or static bearer token that you manage yourself.

**Why not Entra ID?** While Entra ID does issue bearer tokens, these tokens are short-lived (typically 1 hour) and require automatic refresh via the Azure Identity SDK. The `bearerToken` option only accepts a static string—there is no callback mechanism for the SDK to request fresh tokens. For long-running workloads requiring Entra authentication, you would need to implement your own token refresh logic and create new sessions with updated tokens.

### Feature Limitations

Some Copilot features may behave differently with BYOK:

- **Model availability** - Only models supported by your provider are available
- **Rate limiting** - Subject to your provider's rate limits, not Copilot's
- **Usage tracking** - Usage is tracked by your provider, not GitHub Copilot
- **Premium requests** - Do not count against Copilot premium request quotas

### Provider-Specific Limitations

| Provider | Limitations |
|----------|-------------|
| Azure AI Foundry | No Entra ID auth; must use API keys |
| Ollama | No API key; local only; model support varies |
| [Microsoft Foundry Local](https://foundrylocal.ai) | Local only; model availability depends on device hardware; no API key required |
| OpenAI | Subject to OpenAI rate limits and quotas |

## Troubleshooting

### "Model not specified" Error

When using BYOK, the `model` parameter is **required**:

```typescript
// ❌ Error: Model required with custom provider
const session = await client.createSession({
    provider: { type: "openai", baseUrl: "..." },
});

// ✅ Correct: Model specified
const session = await client.createSession({
    model: "gpt-4",  // Required!
    provider: { type: "openai", baseUrl: "..." },
});
```

### Azure Endpoint Type Confusion

For Azure OpenAI endpoints (`*.openai.azure.com`), use the correct type:

<!-- docs-validate: hidden -->
```typescript
import { CopilotClient } from "@github/copilot-sdk";

const client = new CopilotClient();
const session = await client.createSession({
    model: "gpt-4.1",
    provider: {
        type: "azure",
        baseUrl: "https://my-resource.openai.azure.com",
    },
});
```
<!-- /docs-validate: hidden -->

```typescript
// ❌ Wrong: Using "openai" type with native Azure endpoint
provider: {
    type: "openai",  // This won't work correctly
    baseUrl: "https://my-resource.openai.azure.com",
}

// ✅ Correct: Using "azure" type
provider: {
    type: "azure",
    baseUrl: "https://my-resource.openai.azure.com",
}
```

However, if your Azure AI Foundry deployment provides an OpenAI-compatible endpoint path (e.g., `/openai/v1/`), use `type: "openai"`:

<!-- docs-validate: hidden -->
```typescript
import { CopilotClient } from "@github/copilot-sdk";

const client = new CopilotClient();
const session = await client.createSession({
    model: "gpt-4.1",
    provider: {
        type: "openai",
        baseUrl: "https://your-resource.openai.azure.com/openai/v1/",
    },
});
```
<!-- /docs-validate: hidden -->

```typescript
// ✅ Correct: OpenAI-compatible Azure AI Foundry endpoint
provider: {
    type: "openai",
    baseUrl: "https://your-resource.openai.azure.com/openai/v1/",
}
```

### Connection Refused (Ollama)

Ensure Ollama is running and accessible:

```bash
# Check Ollama is running
curl http://localhost:11434/v1/models

# Start Ollama if not running
ollama serve
```

### Connection Refused (Foundry Local)

Foundry Local uses a dynamic port that may change between restarts. Confirm the active port:

```bash
# Check the service status and port
foundry service status
```

Update your `baseUrl` to match the port shown in the output. If the service is not running, start a model to launch it:

```bash
foundry model run phi-4-mini
```

### Authentication Failed

1. Verify your API key is correct and not expired
2. Check the `baseUrl` matches your provider's expected format
3. For bearer tokens, ensure the full token is provided (not just a prefix)

## Next Steps

- [Authentication Overview](./index.md) - Learn about all authentication methods
- [Getting Started Guide](../getting-started.md) - Build your first Copilot-powered app
