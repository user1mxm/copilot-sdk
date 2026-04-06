/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *--------------------------------------------------------------------------------------------*/

using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using StreamJsonRpc;
using Xunit;

namespace GitHub.Copilot.SDK.Test;

/// <summary>
/// Unit tests verifying forward compatibility: an unknown hook type received from the CLI
/// must not cause an RPC error that would shut down the session.
/// </summary>
public class HookForwardCompatibilityTests
{
    private const string TestSessionId = "test-hook-forward-compat-session";

    [Fact]
    public async Task Unknown_Hook_Type_With_Known_Hooks_Registered_Does_Not_Return_RpcError()
    {
        // Arrange: start a minimal in-process fake CLI server over TCP.
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        // sessionReady is signalled after CreateSessionAsync completes so that
        // hooks.invoke is only sent once the session is fully registered.
        var sessionReady = new TaskCompletionSource();
        var fakeCLITask = RunFakeCLIAsync(listener, sessionReady.Task, "postToolUseFailure");

        await using var client = new CopilotClient(new CopilotClientOptions
        {
            CliUrl = $"localhost:{port}",
        });

        // Register a known hook so the SDK sends Hooks=true in the request,
        // matching the real-world scenario where unknown hook types are a risk.
        _ = await client.CreateSessionAsync(new SessionConfig
        {
            SessionId = TestSessionId,
            OnPermissionRequest = PermissionHandler.ApproveAll,
            Hooks = new SessionHooks
            {
                OnPostToolUse = (_, _) => Task.FromResult<PostToolUseHookOutput?>(null),
            },
        });

        // Unblock the fake CLI so it can send hooks.invoke.
        sessionReady.SetResult();

        // Assert: the fake CLI must complete without throwing RemoteRpcException.
        // If the SDK had returned a JSON-RPC error for the unknown hook type,
        // rpc.InvokeAsync on the fake CLI side would throw RemoteRpcException here.
        await fakeCLITask;
    }

    [Fact]
    public async Task Unknown_Hook_Type_With_No_Hooks_Registered_Does_Not_Return_RpcError()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var sessionReady = new TaskCompletionSource();
        var fakeCLITask = RunFakeCLIAsync(listener, sessionReady.Task, "futureHookType");

        await using var client = new CopilotClient(new CopilotClientOptions
        {
            CliUrl = $"localhost:{port}",
        });

        // No hooks registered – the session's hook table is empty.
        _ = await client.CreateSessionAsync(new SessionConfig
        {
            SessionId = TestSessionId,
            OnPermissionRequest = PermissionHandler.ApproveAll,
        });

        sessionReady.SetResult();

        await fakeCLITask;
    }

    /// <summary>
    /// Runs a minimal fake CLI over <paramref name="listener"/> that:
    /// <list type="bullet">
    ///   <item>responds to <c>ping</c> with a valid protocol version,</item>
    ///   <item>responds to <c>session.create</c> with a minimal session object, and</item>
    ///   <item>once <paramref name="sessionReady"/> completes, invokes <c>hooks.invoke</c>
    ///   with <paramref name="unknownHookType"/> on the SDK.</item>
    /// </list>
    /// The returned task faults if the SDK returns a JSON-RPC error for the unknown hook type.
    /// </summary>
    private static async Task RunFakeCLIAsync(
        TcpListener listener,
        Task sessionReady,
        string unknownHookType)
    {
        using var tcpClient = await listener.AcceptTcpClientAsync();
        var stream = tcpClient.GetStream();

        var formatter = new SystemTextJsonFormatter
        {
            JsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                TypeInfoResolver = HookForwardCompatibilityJsonContext.Default,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            },
        };

        using var rpc = new JsonRpc(new HeaderDelimitedMessageHandler(stream, stream, formatter));

        rpc.AddLocalRpcMethod("ping", (JsonElement _) =>
            new FakePingResponse(string.Empty, 0, 3 /* protocol version */));

        rpc.AddLocalRpcMethod("session.create", (JsonElement _) =>
            new FakeCreateSessionResponse(TestSessionId, null));

        rpc.StartListening();

        // Wait until CreateSessionAsync has returned so that the session is
        // fully registered and its hooks table is populated before we invoke.
        await sessionReady;

        // Invoke hooks.invoke with an unknown hook type.
        // This must NOT cause a JSON-RPC error - the session should ignore it.
        // If the SDK returns an error, rpc.InvokeAsync throws RemoteRpcException.
        await rpc.InvokeAsync<JsonElement>(
            "hooks.invoke",
            TestSessionId,
            unknownHookType,
            JsonDocument.Parse("{}").RootElement);
    }

}

internal record FakePingResponse(string Message, long Timestamp, int? ProtocolVersion);
internal record FakeCreateSessionResponse(string SessionId, string? WorkspacePath);

[JsonSerializable(typeof(FakePingResponse))]
[JsonSerializable(typeof(FakeCreateSessionResponse))]
[JsonSerializable(typeof(JsonElement))]
internal partial class HookForwardCompatibilityJsonContext : JsonSerializerContext;
