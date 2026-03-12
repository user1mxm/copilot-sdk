# Debugging Guide

This guide covers common issues and debugging techniques for the Copilot SDK across all supported languages.

## Table of Contents

- [Enable Debug Logging](#enable-debug-logging)
- [Common Issues](#common-issues)
  - [Silent Model Fallback / Wrong Model Being Used](#silent-model-fallback--wrong-model-being-used)
- [MCP Server Debugging](#mcp-server-debugging)
- [Connection Issues](#connection-issues)
- [Tool Execution Issues](#tool-execution-issues)
- [Platform-Specific Issues](#platform-specific-issues)

---

## Enable Debug Logging

The first step in debugging is enabling verbose logging to see what's happening under the hood.

<details open>
<summary><strong>Node.js / TypeScript</strong></summary>

```typescript
import { CopilotClient } from "@github/copilot-sdk";

const client = new CopilotClient({
  logLevel: "debug",  // Options: "none", "error", "warning", "info", "debug", "all"
});
```

</details>

<details>
<summary><strong>Python</strong></summary>

```python
from copilot import CopilotClient

client = CopilotClient({"log_level": "debug"})
```

</details>

<details>
<summary><strong>Go</strong></summary>

<!-- docs-validate: hidden -->
```go
package main

import copilot "github.com/github/copilot-sdk/go"

func main() {
	client := copilot.NewClient(&copilot.ClientOptions{
		LogLevel: "debug",
	})
	_ = client
}
```
<!-- /docs-validate: hidden -->

```go
import copilot "github.com/github/copilot-sdk/go"

client := copilot.NewClient(&copilot.ClientOptions{
    LogLevel: "debug",
})
```

</details>

<details>
<summary><strong>.NET</strong></summary>

<!-- docs-validate: skip -->

```csharp
using GitHub.Copilot.SDK;
using Microsoft.Extensions.Logging;

// Using ILogger
var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.SetMinimumLevel(LogLevel.Debug);
    builder.AddConsole();
});

var client = new CopilotClient(new CopilotClientOptions
{
    LogLevel = "debug",
    Logger = loggerFactory.CreateLogger<CopilotClient>()
});
```

</details>

### Log Directory

The CLI writes logs to a directory. You can specify a custom location:

<details open>
<summary><strong>Node.js / TypeScript</strong></summary>

```typescript
const client = new CopilotClient({
  cliArgs: ["--log-dir", "/path/to/logs"],
});
```

</details>

<details>
<summary><strong>Python</strong></summary>

```python
# The Python SDK does not currently support passing extra CLI arguments.
# Logs are written to the default location or can be configured via
# the CLI when running in server mode.
```

> **Note:** Python SDK logging configuration is limited. For advanced logging, run the CLI manually with `--log-dir` and connect via `cli_url`.

</details>

<details>
<summary><strong>Go</strong></summary>

<!-- docs-validate: hidden -->
```go
package main

func main() {
	// The Go SDK does not currently support passing extra CLI arguments.
	// For custom log directories, run the CLI manually with --log-dir
	// and connect via CLIUrl option.
}
```
<!-- /docs-validate: hidden -->

```go
// The Go SDK does not currently support passing extra CLI arguments.
// For custom log directories, run the CLI manually with --log-dir
// and connect via CLIUrl option.
```

</details>

<details>
<summary><strong>.NET</strong></summary>

```csharp
var client = new CopilotClient(new CopilotClientOptions
{
    CliArgs = new[] { "--log-dir", "/path/to/logs" }
});
```

</details>

---

## Common Issues

### "CLI not found" / "copilot: command not found"

**Cause:** The Copilot CLI is not installed or not in PATH.

**Solution:**

1. Install the CLI: [Installation guide](https://docs.github.com/en/copilot/how-tos/set-up/install-copilot-cli)

2. Verify installation:
   ```bash
   copilot --version
   ```

3. Or specify the full path:

   <details open>
   <summary><strong>Node.js</strong></summary>

   ```typescript
   const client = new CopilotClient({
     cliPath: "/usr/local/bin/copilot",
   });
   ```
   </details>

   <details>
   <summary><strong>Python</strong></summary>

   ```python
   client = CopilotClient({"cli_path": "/usr/local/bin/copilot"})
   ```
   </details>

   <details>
   <summary><strong>Go</strong></summary>

   ```go
   client := copilot.NewClient(&copilot.ClientOptions{
       CLIPath: "/usr/local/bin/copilot",
   })
   ```
   </details>

   <details>
   <summary><strong>.NET</strong></summary>

   ```csharp
   var client = new CopilotClient(new CopilotClientOptions
   {
       CliPath = "/usr/local/bin/copilot"
   });
   ```
   </details>

### "Not authenticated"

**Cause:** The CLI is not authenticated with GitHub.

**Solution:**

1. Authenticate the CLI:
   ```bash
   copilot auth login
   ```

2. Or provide a token programmatically:

   <details open>
   <summary><strong>Node.js</strong></summary>

   ```typescript
   const client = new CopilotClient({
     githubToken: process.env.GITHUB_TOKEN,
   });
   ```
   </details>

   <details>
   <summary><strong>Python</strong></summary>

   ```python
   import os
   client = CopilotClient({"github_token": os.environ.get("GITHUB_TOKEN")})
   ```
   </details>

   <details>
   <summary><strong>Go</strong></summary>

   ```go
   client := copilot.NewClient(&copilot.ClientOptions{
       GithubToken: os.Getenv("GITHUB_TOKEN"),
   })
   ```
   </details>

   <details>
   <summary><strong>.NET</strong></summary>

   ```csharp
   var client = new CopilotClient(new CopilotClientOptions
   {
       GithubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN")
   });
   ```
   </details>

### "Session not found"

**Cause:** Attempting to use a session that was destroyed or doesn't exist.

**Solution:**

1. Ensure you're not calling methods after `disconnect()`:
   ```typescript
   await session.disconnect();
   // Don't use session after this!
   ```

2. For resuming sessions, verify the session ID exists:
   ```typescript
   const sessions = await client.listSessions();
   console.log("Available sessions:", sessions);
   ```

### "Connection refused" / "ECONNREFUSED"

**Cause:** The CLI server process crashed or failed to start.

**Solution:**

1. Check if the CLI runs correctly standalone:
   ```bash
   copilot --server --stdio
   ```

2. Enable auto-restart (enabled by default):
   ```typescript
   const client = new CopilotClient({
     autoRestart: true,
   });
   ```

3. Check for port conflicts if using TCP mode:
   ```typescript
   const client = new CopilotClient({
     useStdio: false,
     port: 0,  // Use random available port
   });
   ```

### Silent Model Fallback / Wrong Model Being Used

> See [GitHub Issue #250](https://github.com/github/copilot-sdk/issues/250)

**Symptoms:** You specify a model in your session configuration, but the session uses a
different model (typically `claude-sonnet-4.6`) without raising an error. This happens when the
requested model ID is invalid, misspelled, or not available for your account.

**Cause:** The SDK does not validate model IDs client-side — the name is forwarded directly
to the Copilot CLI server. When the server determines that the requested model is unavailable,
it silently falls back to the first available default model rather than returning an error.
The fallback order is `claude-sonnet-4.6` → `claude-sonnet-4.5` → `claude-haiku-4.5` → etc.
Warnings are logged server-side but are not surfaced to the SDK as errors.

> **Note:** There is currently no SDK-level option to disable automatic model fallback. This
> is tracked in [#250](https://github.com/github/copilot-sdk/issues/250).

**Solution:**

1. **Validate the model before creating a session** by calling `listModels()` to check
   available models:

   <details open>
   <summary><strong>Node.js / TypeScript</strong></summary>

   ```typescript
   const models = await client.listModels();
   const desiredModel = "gpt-5.4";

   const isAvailable = models.some((m) => m.id === desiredModel);
   if (!isAvailable) {
     throw new Error(
       `Model "${desiredModel}" is not available. Available: ${models.map((m) => m.id).join(", ")}`,
     );
   }

   const session = await client.createSession({
     model: desiredModel,
     // ...
   });
   ```
   </details>

   <details>
   <summary><strong>Python</strong></summary>

   ```python
   models = await client.list_models()
   desired_model = "gpt-5.4"

   available_ids = [m.id for m in models]
   if desired_model not in available_ids:
       raise ValueError(
           f'Model "{desired_model}" is not available. '
           f'Available: {", ".join(available_ids)}'
       )

   session = await client.create_session({"model": desired_model})
   ```
   </details>

   <details>
   <summary><strong>Go</strong></summary>

   <!-- docs-validate: hidden -->
   ```go
   package main

   import (
   	"context"
   	"fmt"
   	"strings"

   	copilot "github.com/github/copilot-sdk/go"
   )

   func main() {
   	client := copilot.NewClient(nil)
   	ctx := context.Background()
   	_ = client.Start(ctx)

   	models, _ := client.ListModels(ctx)
   	desiredModel := "gpt-5.4"

   	found := false
   	var ids []string
   	for _, m := range models {
   		ids = append(ids, m.ID)
   		if m.ID == desiredModel {
   			found = true
   		}
   	}
   	if !found {
   		panic(fmt.Sprintf("Model %q is not available. Available: %s",
   			desiredModel, strings.Join(ids, ", ")))
   	}
   }
   ```
   <!-- /docs-validate: hidden -->

   ```go
   models, err := client.ListModels(ctx)
   desiredModel := "gpt-5.4"

   found := false
   var ids []string
   for _, m := range models {
       ids = append(ids, m.ID)
       if m.ID == desiredModel {
           found = true
       }
   }
   if !found {
       panic(fmt.Sprintf("Model %q is not available. Available: %s",
           desiredModel, strings.Join(ids, ", ")))
   }
   ```
   </details>

   <details>
   <summary><strong>.NET</strong></summary>

   <!-- docs-validate: skip -->

   ```csharp
   var models = await client.ListModelsAsync();
   var desiredModel = "gpt-5.4";

   if (!models.Any(m => m.Id == desiredModel))
   {
       var available = string.Join(", ", models.Select(m => m.Id));
       throw new InvalidOperationException(
           $"Model \"{desiredModel}\" is not available. Available: {available}");
   }

   var session = await client.CreateSessionAsync(new SessionConfig
   {
       Model = desiredModel,
   });
   ```
   </details>

2. **Detect fallback at runtime** by comparing the model reported in `assistant.usage` events
   against the model you requested:

   <details open>
   <summary><strong>Node.js / TypeScript</strong></summary>

   ```typescript
   const requestedModel = "gpt-5.4";
   const session = await client.createSession({ model: requestedModel });

   session.on("assistant.usage", (event) => {
     if (event.data.model !== requestedModel) {
       console.warn(
         `Model fallback detected: requested "${requestedModel}", ` +
           `got "${event.data.model}"`,
       );
     }
   });
   ```
   </details>

   <details>
   <summary><strong>Python</strong></summary>

   ```python
   requested_model = "gpt-5.4"
   session = await client.create_session({"model": requested_model})

   @session.on("assistant.usage")
   def on_usage(event):
       if event.data.get("model") != requested_model:
           print(
               f'Model fallback detected: requested "{requested_model}", '
               f'got "{event.data["model"]}"'
           )
   ```
   </details>

3. **Query the active model** after session creation to confirm what model is in use:

   <details open>
   <summary><strong>Node.js / TypeScript</strong></summary>

   ```typescript
   const result = await session.rpc.model.getCurrent();
   console.log("Active model:", result.modelId);
   ```
   </details>

   <details>
   <summary><strong>Python</strong></summary>

   ```python
   result = await session.rpc.model.get_current()
   print("Active model:", result.get("modelId"))
   ```
   </details>

   <details>
   <summary><strong>Go</strong></summary>

   ```go
   result, err := session.Model.GetCurrent(ctx)
   fmt.Println("Active model:", result.ModelID)
   ```
   </details>

   <details>
   <summary><strong>.NET</strong></summary>

   <!-- docs-validate: skip -->

   ```csharp
   var result = await session.GetModelAsync();
   Console.WriteLine($"Active model: {result.ModelId}");
   ```
   </details>

4. **Enable debug logging** (see [above](#enable-debug-logging)) to see server-side
   fallback warnings in the CLI logs.

---

## MCP Server Debugging

MCP (Model Context Protocol) servers can be tricky to debug. For comprehensive MCP debugging guidance, see the dedicated **[MCP Debugging Guide](./mcp-debugging.md)**.

### Quick MCP Checklist

- [ ] MCP server executable exists and runs independently
- [ ] Command path is correct (use absolute paths)
- [ ] Tools are enabled: `tools: ["*"]`
- [ ] Server responds to `initialize` request correctly
- [ ] Working directory (`cwd`) is set if needed

### Test Your MCP Server

Before integrating with the SDK, verify your MCP server works:

```bash
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}' | /path/to/your/mcp-server
```

See [MCP Debugging Guide](./mcp-debugging.md) for detailed troubleshooting.

---

## Connection Issues

### Stdio vs TCP Mode

The SDK supports two transport modes:

| Mode | Description | Use Case |
|------|-------------|----------|
| **Stdio** (default) | CLI runs as subprocess, communicates via pipes | Local development, single process |
| **TCP** | CLI runs separately, communicates via TCP socket | Multiple clients, remote CLI |

**Stdio mode (default):**
```typescript
const client = new CopilotClient({
  useStdio: true,  // This is the default
});
```

**TCP mode:**
```typescript
const client = new CopilotClient({
  useStdio: false,
  port: 8080,  // Or 0 for random port
});
```

**Connect to existing server:**
```typescript
const client = new CopilotClient({
  cliUrl: "localhost:8080",  // Connect to running server
});
```

### Diagnosing Connection Failures

1. **Check client state:**
   ```typescript
   console.log("Connection state:", client.getState());
   // Should be "connected" after start()
   ```

2. **Listen for state changes:**
   ```typescript
   client.on("stateChange", (state) => {
     console.log("State changed to:", state);
   });
   ```

3. **Verify CLI process is running:**
   ```bash
   # Check for copilot processes
   ps aux | grep copilot
   ```

---

## Tool Execution Issues

### Custom Tool Not Being Called

1. **Verify tool registration:**
   ```typescript
   const session = await client.createSession({
     tools: [myTool],
   });
   
   // Check registered tools
   console.log("Registered tools:", session.getTools?.());
   ```

2. **Check tool schema is valid JSON Schema:**
   ```typescript
   const myTool = {
     name: "get_weather",
     description: "Get weather for a location",
     parameters: {
       type: "object",
       properties: {
         location: { type: "string", description: "City name" },
       },
       required: ["location"],
     },
     handler: async (args) => {
       return { temperature: 72 };
     },
   };
   ```

3. **Ensure handler returns valid result:**
   ```typescript
   handler: async (args) => {
     // Must return something JSON-serializable
     return { success: true, data: "result" };
     
     // Don't return undefined or non-serializable objects
   }
   ```

### Tool Errors Not Surfacing

Subscribe to error events:

```typescript
session.on("tool.execution_error", (event) => {
  console.error("Tool error:", event.data);
});

session.on("error", (event) => {
  console.error("Session error:", event.data);
});
```

---

## Platform-Specific Issues

### Windows

1. **Path separators:** Use raw strings or forward slashes:
   ```csharp
   CliPath = @"C:\Program Files\GitHub\copilot.exe"
   // or
   CliPath = "C:/Program Files/GitHub/copilot.exe"
   ```

2. **PATHEXT resolution:** The SDK handles this automatically, but if issues persist:
   ```csharp
   // Explicitly specify .exe
   Command = "myserver.exe"  // Not just "myserver"
   ```

3. **Console encoding:** Ensure UTF-8 for proper JSON handling:
   ```csharp
   Console.OutputEncoding = System.Text.Encoding.UTF8;
   ```

### macOS

1. **Gatekeeper issues:** If CLI is blocked:
   ```bash
   xattr -d com.apple.quarantine /path/to/copilot
   ```

2. **PATH issues in GUI apps:** GUI applications may not inherit shell PATH:
   ```typescript
   const client = new CopilotClient({
     cliPath: "/opt/homebrew/bin/copilot",  // Full path
   });
   ```

### Linux

1. **Permission issues:**
   ```bash
   chmod +x /path/to/copilot
   ```

2. **Missing libraries:** Check for required shared libraries:
   ```bash
   ldd /path/to/copilot
   ```

---

## Getting Help

If you're still stuck:

1. **Collect debug information:**
   - SDK version
   - CLI version (`copilot --version`)
   - Operating system
   - Debug logs
   - Minimal reproduction code

2. **Search existing issues:** [GitHub Issues](https://github.com/github/copilot-sdk/issues)

3. **Open a new issue** with the collected information

## See Also

- [Getting Started Guide](../getting-started.md)
- [MCP Overview](../features/mcp.md) - MCP configuration and setup
- [MCP Debugging Guide](./mcp-debugging.md) - Detailed MCP troubleshooting
- [API Reference](https://github.com/github/copilot-sdk)
