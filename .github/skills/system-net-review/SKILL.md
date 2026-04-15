---
name: system-net-review
description: "Guidance for writing and modifying System.Net networking code in dotnet/runtime. Covers resource lifecycle, connection pooling, cross-platform interop, protocol compliance, async patterns, and security posture. For full code review, delegates to the @system-net-review agent. Trigger words: system.net, networking, http client, sockets, ssl, tls, quic, http2, http3, socketshttphandler, sslstream, connection pool."
---

# Writing & Reviewing System.Net Code

This skill provides **implementation guidance** when writing or modifying code under `src/libraries/System.Net.*`, `src/libraries/System.Private.Uri`, and networking-related files in `src/libraries/Common/src`. It also serves as a **review trigger** for System.Net changes.

## Scope

Applies to all `System.Net.*` libraries, `System.Private.Uri`, and shared networking code in `Common/src`. For general dotnet/runtime conventions (style, builds, testing workflow), defer to the repo-level `copilot-instructions.md` and the `code-review` skill.

### Review Delegation

When **reviewing** a PR or code change that touches System.Net code, **invoke the `@system-net-review` agent as a sub-task**. The agent carries the full structured checklist with severity-weighted routing and per-folder hotspot coverage. This skill provides the decision frameworks and coding patterns; the agent provides the line-by-line review checklist.

When invoking from an existing reviewer agent,  tell the `@system-net-review` to only **collect** feedback items and return them, do not post them to GH. The parent reviewer is responsible for collection, deduplication, and posting.

> **Do not duplicate the agent's CHECK items here.** Use this skill for authoring guidance and the agent for review verdicts.

---

## Decision Frameworks

### 1. Resource Lifecycle

```
New resource introduced?
├─ Native handle (socket, TLS context, msquic obj) → SafeHandle subclass; Release in ReleaseHandle()
├─ Managed IDisposable (Stream, HttpConnection) → single owner; dispose in finally/using
├─ Pooled resource (HttpConnectionPool entry) → return-to-pool on success; dispose on fault
└─ Temporary buffer → ArrayPool<byte>.Shared; return in finally block
```

**Key rules:**
- Every resource must have exactly **one owner** responsible for disposal.
- `Dispose()` must be **idempotent** and **never throw**.
- Error paths must dispose; use `try/finally`, not just `using` on the happy path.

### 2. Connection Pooling

```
Modifying connection pool logic?
├─ Pool sizing / lifetime → respect HttpConnectionPoolManager limits; honor PooledConnectionIdleTimeout
├─ Connection acquisition → async wait with CancellationToken; fail fast on disposed pool
├─ Connection return → validate state before reuse; drain if protocol error
└─ Connection eviction → close gracefully; log via NetEventSource
```

**Key rules:**
- Never hold a lock while performing I/O or awaiting.
- Connections returned to pool must be in a **reusable state** (no partial reads, no pending writes).
- Pool disposal must drain all waiters and close all connections.
- Prefer `Interlocked` operations over `lock` for counters on hot paths.

### 3. Cross-Platform Interop

```
Adding or modifying P/Invoke?
├─ New declaration → use [LibraryImport], not [DllImport]
├─ Struct layout → verify with StructLayout on all target architectures (x86, x64, arm64)
├─ String marshaling → use StringMarshalling.Utf8 for Unix, Utf16 for Windows
├─ Return values → check errno/GetLastPInvokeError(); map to SocketException/Win32Exception
└─ Lifetime → SafeHandle for any native handle; never raw IntPtr across async boundaries
```

**Key rules:**
- Platform-specific code belongs in `.Windows.cs` / `.Unix.cs` / `.OSX.cs` partial files.
- All `Interop.` declarations live under `Common/src/Interop/<platform>/`.
- Test on all platforms via `ConditionalFact`/`ConditionalTheory` with `PlatformDetection`.

### 4. Protocol Compliance

```
Touching HTTP/QUIC framing or headers?
├─ Header parsing → case-insensitive comparison; handle folded headers; reject invalid chars
├─ HTTP/2 frames → respect flow control windows; never exceed MAX_FRAME_SIZE; send GOAWAY cleanly
├─ HTTP/3 / QUIC → use MsQuic API via QuicConnection/QuicStream; handle 0-RTT carefully
├─ Content-Length → validate against actual bytes; fail on mismatch
└─ Transfer-Encoding: chunked → handle final chunk + trailers; zero-length chunk = end
```

**Key rules:**
- Follow the relevant RFC strictly (RFC 9110/9112 for HTTP, RFC 9000/9001 for QUIC).
- Protocol violations from the peer must produce clear exceptions, not silent corruption.
- Version-specific behavior must be gated on `HttpRequestMessage.Version` or negotiated ALPN.

### 5. Async Patterns

```
Writing async networking code?
├─ Return type → ValueTask<T> if synchronous completion is common (reads from buffer)
├─ ConfigureAwait → always ConfigureAwait(false) in library code
├─ Cancellation → accept CancellationToken; link with internal timeout tokens
├─ Locking → never await inside lock{}; use SemaphoreSlim for async mutual exclusion
└─ Completion source → PooledValueTaskSource or ManualResetValueTaskSourceCore for reuse
```

**Key rules:**
- Cancellation must interrupt I/O promptly, not just check at the next loop iteration.
- `ValueTask` must not be awaited more than once or stored.
- See the agent's async CHECK items for the full list.

### 6. Security Posture

```
Changing TLS, auth, or credential handling?
├─ TLS defaults → SslProtocols.None (OS-negotiated); never hardcode TLS 1.0/1.1
├─ Certificate validation → default RemoteCertificateValidationCallback must reject invalid certs
├─ Credentials → never log; never cache beyond session; clear from memory when done
├─ SslStream options → default to RequireEncryption; client cert negotiation must be explicit
└─ QUIC TLS → ensure certificate chain is validated via SslServerAuthenticationOptions
```

**Key rules:**
- Security settings must be **secure by default**; opt-out must be explicit and documented.
- Validate all TLS configuration before handshake; fail early with clear `AuthenticationException`.
- See the agent's security and diagnostics CHECK items for the full list.

---

## Code Patterns

### NetEventSource Tracing

```csharp
// DO: guard with IsEnabled and pass 'this' for correlation
if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Connecting to {address}");

// DON'T: unconditional string formatting
NetEventSource.Info(this, $"Header: {header}");  // allocates even when tracing is off

// DON'T: log sensitive data
NetEventSource.Info(this, $"Auth token: {token}");  // NEVER
```

### Socket Async Operations

```csharp
// DO: reuse SocketAsyncEventArgs; set callback once
private readonly SocketAsyncEventArgs _recvArgs = new();

public MySocketHandler()
{
    _recvArgs.Completed += OnReceiveCompleted;
}
// DO: check synchronous completion
if (!_socket.ReceiveAsync(_recvArgs))
{
    OnReceiveCompleted(null, _recvArgs);
}

// DON'T: allocate new SocketAsyncEventArgs per operation
var args = new SocketAsyncEventArgs();  // hot-path allocation
```

### Connection Pool Return

```csharp
// DO: validate state before returning to pool
if (connection.IsUsable && !connection.HasUnreadData)
{
    pool.ReturnConnection(connection);
}
else
{
    connection.Dispose();
}

// DON'T: return without checking state
pool.ReturnConnection(connection);  // may reuse a broken connection
```

### SslStream Configuration

```csharp
// DO: let the OS negotiate the best protocol
var options = new SslClientAuthenticationOptions
{
    TargetHost = host,
    EnabledSslProtocols = SslProtocols.None, // OS default
};

// DON'T: pin to a specific version
EnabledSslProtocols = SslProtocols.Tls12,  // blocks future upgrades
```

### Buffer Handling

```csharp
// DO: rent from ArrayPool; return in finally
byte[] buffer = ArrayPool<byte>.Shared.Rent(4096);
try
{
    int bytesRead = await stream.ReadAsync(buffer.AsMemory(0, 4096), ct);
    ProcessData(buffer.AsSpan(0, bytesRead));
}
finally
{
    ArrayPool<byte>.Shared.Return(buffer);
}

// DON'T: allocate on the hot path
byte[] buffer = new byte[4096];  // GC pressure under load
```

### HTTP/2 Flow Control

```csharp
// DO: respect the peer's flow-control window before sending
int allowed = Math.Min(dataLength, _peerWindowAvailable);
await SendDataFrameAsync(streamId, data.Slice(0, allowed), ct);
_peerWindowAvailable -= allowed;

// DON'T: send data exceeding the window → protocol violation / FLOW_CONTROL_ERROR
```

### Cancellation Plumbing

```csharp
// DO: link user token with internal timeout
using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
    cancellationToken, _connectionTimeoutCts.Token);

// DO: register cancellation to abort underlying I/O
using var reg = linkedCts.Token.UnsafeRegister(
    static s => ((Socket)s!).Dispose(), _socket);

// DON'T: ignore the token
await stream.ReadAsync(buffer);  // not cancellable
```

---

## Testing Guidance

### LoopbackServer Pattern

System.Net functional tests use in-process loopback servers to avoid real network dependencies. Choose the right one:

| Server | Use For |
|--------|---------|
| `LoopbackServer` | HTTP/1.1 tests (raw request/response control) |
| `Http2LoopbackServer` | HTTP/2 frame-level tests |
| `Http3LoopbackServer` | HTTP/3 / QUIC tests |
| `LoopbackProxyServer` | Proxy behavior tests |

### Conditional Attributes

```csharp
// Platform-specific
[ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows))]

// Feature gating
[ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.SupportsAlpn))]

// Known failures — always cite the issue
[ActiveIssue("https://github.com/dotnet/runtime/issues/<issue-number>")]
```

### Test Conventions

- Tests must clean up all sockets, streams, and servers — wrap in `using`/`await using`.
- Stress tests live under `tests/StressTests/`; don't add long-running loops to functional tests.
- General test conventions (prefer existing files, `[Theory]` over `[Fact]`) are in `copilot-instructions.md`.

---

## Reference Links

- [System.Net.Http SocketsHttpHandler source](/src/libraries/System.Net.Http/src/System/Net/Http/SocketsHttpHandler/)
- [HTTP/2 implementation notes](/src/libraries/System.Net.Http/src/System/Net/Http/SocketsHttpHandler/Http2Connection.cs)
- [QUIC managed wrapper](/src/libraries/System.Net.Quic/src/System/Net/Quic/)
- [Common Interop layer](/src/libraries/Common/src/Interop/)
- [Test infrastructure](/src/libraries/Common/tests/System/Net/)
- [Build & test libraries](/docs/workflow/building/libraries/README.md)
- [Testing libraries](/docs/workflow/testing/libraries/testing.md)
