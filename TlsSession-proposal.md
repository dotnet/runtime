# API Proposal: `TlsSession` — Non-Blocking TLS State Machine

## Summary

A new low-level, non-blocking, span-based TLS API in `System.Net.Security` that
exposes the TLS state machine directly. The caller drives I/O; the session
handles only encryption, decryption, and the handshake protocol. The same
session type optionally supports binding to a `SafeSocketHandle`, in which case
it performs socket I/O itself — on Linux via OpenSSL's `SSL_set_fd` fast path,
on other platforms via an internal pump over the same state machine.

The API is the synchronous primitive on which higher-level adapters
(`Stream`, `IDuplexPipe`, and ultimately `SslStream` itself) are layered.

This proposal is a refinement of [#127928] in response to feedback from
@bartonjs and @rzikm. Key shape changes from that proposal:

- Single `TlsSession` type (no `TlsDetachedSession` / `TlsSocketBoundSession` split).
- Cross-platform API surface. Linux is an implementation fast path, not a separate type.
- Role (client vs. server) bound to `TlsContext` via the options type; no `bool isServer`.
- Two explicit suspension states (`NeedsServerOptions`, `NeedsCertificateValidation`)
  replace async callbacks running on the I/O thread.
- No internal certificate validation. Caller is responsible for the trust decision;
  `AcceptWithDefaultValidation()` extension preserves `SslStream`'s default behavior.

[#127928]: https://github.com/dotnet/runtime/issues/127928

## Background and Motivation

`SslStream` is the only public TLS API in .NET today. It bundles three concerns:

1. The TLS state machine (handshake, encrypt/decrypt records, alerts, renegotiation).
2. An async I/O loop that reads ciphertext from an inner `Stream` and writes
   plaintext out.
3. A `Stream`-shaped public surface.

For high-throughput servers — most notably Kestrel — that bundling forces two
costs:

- **Buffer copies.** On Linux, ciphertext goes `kernel → managed buffer →
  BIO_write → OpenSSL → SSL_read out → managed buffer`. Two memcpys per read
  (and symmetrically per write) that have nothing to do with cryptography.
- **An adapter at the consumer boundary.** Kestrel's transport surface is
  `IDuplexPipe`. Wrapping a `Stream`-shaped TLS connection in a
  `StreamPipeReader` / `StreamPipeWriter` re-introduces buffer copies between
  Pipe segments and byte arrays — and means TLS connections take a
  fundamentally different code path through Kestrel than plaintext ones.

There is also no way today to:

- Drive the TLS state machine from a custom I/O loop (epoll, io_uring, custom
  thread-per-core, etc.) without a parallel re-implementation of the OpenSSL
  P/Invoke layer.
- Perform certificate validation asynchronously without blocking an I/O thread
  inside `RemoteCertificateValidationCallback`.
- Resolve SNI-based server cert selection without a callback that holds up the
  handshake.

The existing `SslStream` PAL is already span-in / span-out and synchronous —
`InitializeSecurityContext` / `AcceptSecurityContext` / `EncryptMessage` /
`DecryptMessage` on every backing provider (OpenSSL, Schannel, Apple/managed).
The async loop lives entirely above the PAL in `SslStream.IO.cs`. This proposal
exposes the PAL-level state machine as a public type, then re-hosts `SslStream`
on top of it. There is one TLS state machine implementation in the box, not two.

## Goals

- Public, non-blocking TLS state machine on top of the existing PAL.
- Cross-platform contract: same API on Windows, Linux, macOS.
- No internal I/O abstraction; caller chooses Stream, Pipe, raw socket, kTLS,
  io_uring, whatever.
- Optional socket-bound mode that exploits OpenSSL's `SSL_set_fd` on Linux
  without forcing other platforms onto a Linux-only API.
- Explicit suspension model for async cert validation and SNI-based server
  cert selection — no callbacks running on the I/O thread.
- Reuse existing `SslServerAuthenticationOptions` / `SslClientAuthenticationOptions`;
  no new configuration surface to learn.
- One implementation. `SslStream` is re-hosted on top of `TlsSession`.

## Non-Goals

- A built-in async I/O loop (that's what adapter types are for).
- A built-in certificate validation policy (that's an opt-in helper).
- A new exception hierarchy.
- An immediate Schannel/macOS equivalent of `SSL_set_fd` (impossible; not needed).

---

## API Surface

```csharp
namespace System.Net.Security;

public enum TlsOperationStatus
{
    /// <summary>The call made forward progress. Check <c>consumed</c>/<c>produced</c>.</summary>
    Complete = 0,

    /// <summary>The session needs more ciphertext from the peer to make progress.</summary>
    WantRead = 1,

    /// <summary>
    /// The session has ciphertext to send. Call <see cref="TlsSession.DrainPendingOutput"/>
    /// before retrying.
    /// </summary>
    WantWrite = 2,

    /// <summary>The transport is gone or <c>close_notify</c> was received. Dispose the session.</summary>
    Closed = 3,

    /// <summary>
    /// Server-side only. The peer's ClientHello has been received but server options
    /// have not been supplied. Inspect <see cref="TlsSession.PendingClientHello"/>,
    /// call <see cref="TlsSession.SetServerOptions"/>, then retry.
    /// </summary>
    NeedsServerOptions = 4,

    /// <summary>
    /// The peer presented a certificate that the session is not validating.
    /// Inspect <see cref="TlsSession.GetRemoteCertificate"/> /
    /// <see cref="TlsSession.GetRemoteCertificateChain"/>, call
    /// <see cref="TlsSession.CompleteCertificateValidation"/>, then retry.
    /// </summary>
    NeedsCertificateValidation = 5,
}

/// <summary>
/// Long-lived TLS configuration. Thread-safe; create once per logical endpoint
/// and share across many <see cref="TlsSession"/> instances.
/// </summary>
public sealed class TlsContext : IDisposable
{
    public static TlsContext Create(SslServerAuthenticationOptions options);
    public static TlsContext Create(SslClientAuthenticationOptions options);

    public bool IsServer { get; }

    public void Dispose();
}

/// <summary>
/// A per-connection TLS session driving a non-blocking TLS state machine.
/// In detached mode the caller performs all socket I/O. In socket-bound mode
/// the session reads and writes the bound socket directly; on Linux this uses
/// OpenSSL's fd-binding fast path with no managed-side ciphertext copies.
/// </summary>
public sealed class TlsSession : IDisposable
{
    // ── Construction ──────────────────────────────────────────────────────

    /// <summary>Creates a detached session. The caller drives all I/O.</summary>
    public static TlsSession Create(TlsContext context);

    /// <summary>
    /// Creates a socket-bound session. The session reads and writes on
    /// <paramref name="socket"/>. The socket must be non-blocking.
    /// </summary>
    public static TlsSession Create(TlsContext context, SafeSocketHandle socket);

    // ── Identity / state ──────────────────────────────────────────────────

    public bool IsServer { get; }
    public bool IsSocketBound { get; }
    public SafeSocketHandle? Socket { get; }
    public bool IsHandshakeComplete { get; }

    /// <summary>SNI / target hostname. Set before the first handshake call.</summary>
    public string? TargetHostName { get; set; }

    // ── Negotiated info (valid after IsHandshakeComplete) ─────────────────

    public SslProtocols NegotiatedProtocol { get; }
    public TlsCipherSuite NegotiatedCipherSuite { get; }
    public SslApplicationProtocol NegotiatedApplicationProtocol { get; }
    public bool SessionResumed { get; }

    public X509Certificate2? GetLocalCertificate();

    /// <summary>
    /// The certificate the peer presented (raw, unvalidated). May be non-null before
    /// the handshake completes and before the caller has accepted it.
    /// </summary>
    public X509Certificate2? GetRemoteCertificate();

    /// <summary>
    /// The certificates the peer sent in its Certificate message (excluding the leaf).
    /// Pass to <see cref="X509Chain.ChainPolicy"/>'s <c>ExtraStore</c> when building.
    /// </summary>
    public X509Certificate2Collection GetRemoteCertificateChain();

    /// <summary>RFC 5929 channel binding token.</summary>
    public ChannelBinding? GetChannelBinding(ChannelBindingKind kind);

    // ── Suspension data ───────────────────────────────────────────────────

    /// <summary>
    /// Valid only while the last status was <see cref="TlsOperationStatus.NeedsServerOptions"/>.
    /// </summary>
    public SslClientHelloInfo? PendingClientHello { get; }

    /// <summary>
    /// Supplies server options resolved from <see cref="PendingClientHello"/>.
    /// Throws if the session is not currently suspended on <c>NeedsServerOptions</c>.
    /// </summary>
    public void SetServerOptions(SslServerAuthenticationOptions options);

    /// <summary>
    /// Supplies the verdict for the peer certificate.
    /// On <see langword="true"/>, the session emits its Finished message on the next call.
    /// On <see langword="false"/>, the session emits a fatal <c>bad_certificate</c> alert.
    /// Throws if the session is not currently suspended on <c>NeedsCertificateValidation</c>.
    /// </summary>
    public void CompleteCertificateValidation(bool accept);

    // ── State machine (always available, both modes) ──────────────────────

    /// <summary>Advances the handshake by one step.</summary>
    public TlsOperationStatus ProcessHandshake(
        ReadOnlySpan<byte> input, Span<byte> output,
        out int consumed, out int produced);

    /// <summary>
    /// Decrypts one record's worth of ciphertext. Throws
    /// <see cref="InvalidOperationException"/> before the handshake is complete.
    /// </summary>
    public TlsOperationStatus Decrypt(
        ReadOnlySpan<byte> ciphertext, Span<byte> plaintext,
        out int consumed, out int produced);

    /// <summary>
    /// Encrypts plaintext into ciphertext. Throws
    /// <see cref="InvalidOperationException"/> before the handshake is complete.
    /// </summary>
    public TlsOperationStatus Encrypt(
        ReadOnlySpan<byte> plaintext, Span<byte> ciphertext,
        out int consumed, out int produced);

    /// <summary>
    /// True when the session has ciphertext for the caller to send to the peer
    /// (handshake messages, alerts, KeyUpdate responses, <c>close_notify</c>).
    /// </summary>
    public bool HasPendingOutput { get; }

    /// <summary>
    /// Copies pending ciphertext into <paramref name="ciphertext"/>. If the span was
    /// too small to drain everything, returns <c>WantWrite</c>; call again.
    /// </summary>
    public TlsOperationStatus DrainPendingOutput(Span<byte> ciphertext, out int produced);

    // ── Socket-bound convenience ──────────────────────────────────────────
    // Throw InvalidOperationException if !IsSocketBound.

    public TlsOperationStatus Handshake();
    public TlsOperationStatus Read(Span<byte> buffer, out int bytesRead);
    public TlsOperationStatus Write(ReadOnlySpan<byte> buffer, out int bytesWritten);

    // ── Shutdown / advanced ───────────────────────────────────────────────

    /// <summary>Initiates a TLS shutdown (sends <c>close_notify</c>).</summary>
    public TlsOperationStatus Shutdown();

    /// <summary>
    /// Initiates TLS 1.3 post-handshake authentication (server only).
    /// Subsequent <see cref="Decrypt"/> / <see cref="Encrypt"/> calls drive the
    /// auth flow and may surface <see cref="TlsOperationStatus.NeedsCertificateValidation"/>.
    /// </summary>
    public TlsOperationStatus RequestPostHandshakeAuthentication();

    public void Dispose();
}

/// <summary>Convenience helpers built on top of <see cref="TlsSession"/>.</summary>
public static class TlsSessionExtensions
{
    /// <summary>
    /// Validates the peer's certificate using the same default policy as
    /// <see cref="SslStream"/>, then accepts or rejects the suspended handshake.
    /// </summary>
    public static bool AcceptWithDefaultValidation(
        this TlsSession session,
        X509RevocationMode revocationMode = X509RevocationMode.NoCheck);

    /// <summary>
    /// Performs the default validation check without supplying a verdict to the
    /// session. The caller is responsible for calling
    /// <see cref="TlsSession.CompleteCertificateValidation"/>.
    /// </summary>
    public static bool PassesDefaultValidation(
        this TlsSession session,
        X509RevocationMode revocationMode = X509RevocationMode.NoCheck);
}
```

---

## Contract

### Status semantics

| Status | Meaning | Caller action |
|---|---|---|
| `Complete` | Call made progress | Check `produced` / `consumed`; continue if more work expected |
| `WantRead` | Need more ciphertext from peer | `recv` from transport; append; retry |
| `WantWrite` | Pending output must be flushed | `DrainPendingOutput`; send to peer; retry |
| `Closed` | Transport gone or `close_notify` received | Dispose |
| `NeedsServerOptions` | ClientHello received, options not supplied | Inspect `PendingClientHello`; call `SetServerOptions`; retry |
| `NeedsCertificateValidation` | Peer cert presented, awaiting verdict | Inspect cert; call `CompleteCertificateValidation`; retry |

### Loop invariants

1. **One step per call.** Any method returns at the first checkpoint. The caller
   loops; the session never blocks.
2. **`WantWrite` always takes priority.** The session does not consume new input
   while pending output is non-empty.
3. **`WantRead` / `WantWrite` describe transport direction**, not which API
   method to call next.
4. **Suspension is durable.** While suspended on a `Needs…` state, the session's
   internal state is preserved until the caller supplies the verdict (or disposes).

### Renegotiation, KeyUpdate, alerts

These surface as `WantWrite` (with the relevant ciphertext appearing in
`DrainPendingOutput`) and, if necessary, `WantRead` for the peer's response.
The caller's existing read/write loop handles them with no special-casing.

For TLS 1.2 renegotiation that re-presents the peer cert,
`NeedsCertificateValidation` may surface from `Decrypt` (not just
`ProcessHandshake`). Same protocol applies.

### Certificate validation timing

The handshake **suspends before** the client's Finished (or the server's
Finished in the mTLS case). If the verdict is reject, the session emits a
fatal alert at exactly the right protocol state and the peer never observes a
successful handshake. No application data is ever exchanged with a rejected
peer. This matches the recent `SslStream` fix that ensured the validation
callback's verdict actually gates the Finished.

### Error model

Reuses existing exception types:

| Condition | Exception |
|---|---|
| Handshake protocol error (bad cert, alert from peer, version mismatch) | `AuthenticationException` |
| Unrecoverable I/O error after handshake | `IOException` |
| `Read` / `Write` before `IsHandshakeComplete` | `InvalidOperationException` |
| `SetServerOptions` / `CompleteCertificateValidation` while not suspended on the matching state | `InvalidOperationException` |
| Socket-bound API called on detached session (and vice versa) | `InvalidOperationException` |
| Suspension feature unsupported on current platform (e.g. OpenSSL 1.1.1 cert-validation pause) | `PlatformNotSupportedException` |

---

## Platform Implementation

| Suspension | Linux (OpenSSL ≥ 3.0) | Linux (OpenSSL 1.1.1) | Windows (Schannel) | macOS (managed) |
|---|---|---|---|---|
| `NeedsServerOptions` | `SSL_CTX_set_client_hello_cb` → `SSL_CLIENT_HELLO_RETRY` | Same | Pre-parse ClientHello, defer first `AcceptSecurityContext` | Our state machine |
| `NeedsCertificateValidation` | `set_cert_verify_callback` returning `-1` (`SSL_ERROR_WANT_RETRY_VERIFY`) | `PlatformNotSupportedException` — caller validates post-handshake | `SCH_CRED_MANUAL_CRED_VALIDATION`; we buffer Finished output | Our state machine |

OpenSSL 1.1.1 is EOL September 2026; the `PlatformNotSupportedException` window is small.

For the socket-bound path:

| Aspect | Linux (OpenSSL) | Other |
|---|---|---|
| `SSL_set_fd` direct path | Yes | No (internal pump over `Decrypt` / `Encrypt`) |
| Zero managed ciphertext copy | Yes | No |
| Same public API | Yes | Yes |

The cross-platform contract is "the session does the socket I/O for you." Linux
additionally gets "and the TLS provider owns the syscall, with no managed-side
copy" as an implementation optimization. There are no `[SupportedOSPlatform]`
annotations on the public surface.

---

## Examples

### Detached client over an arbitrary `Socket`

```csharp
using System.Buffers;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;

static async Task<string> SendRequestAsync(string host, int port, string request, CancellationToken ct)
{
    using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    await socket.ConnectAsync(host, port, ct);

    using var ctx = TlsContext.Create(new SslClientAuthenticationOptions
    {
        TargetHost = host,
        EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
        ApplicationProtocols = [SslApplicationProtocol.Http11],
    });

    using var session = TlsSession.Create(ctx);
    session.TargetHostName = host;

    byte[] netIn  = ArrayPool<byte>.Shared.Rent(16 * 1024);
    byte[] netOut = ArrayPool<byte>.Shared.Rent(16 * 1024);
    byte[] plain  = ArrayPool<byte>.Shared.Rent(16 * 1024);
    int inUsed = 0;

    try
    {
        // Handshake
        while (!session.IsHandshakeComplete)
        {
            var status = session.ProcessHandshake(
                netIn.AsSpan(0, inUsed), netOut,
                out int consumed, out int produced);
            Consume(netIn, ref inUsed, consumed);

            if (produced > 0)
                await socket.SendAsync(netOut.AsMemory(0, produced), ct);

            switch (status)
            {
                case TlsOperationStatus.Complete:
                    continue;

                case TlsOperationStatus.WantWrite:
                    await DrainAsync(session, socket, netOut, ct);
                    continue;

                case TlsOperationStatus.WantRead:
                    int r = await socket.ReceiveAsync(netIn.AsMemory(inUsed), ct);
                    if (r == 0) throw new IOException("Connection closed during handshake.");
                    inUsed += r;
                    continue;

                case TlsOperationStatus.NeedsCertificateValidation:
                    // Run validation off the I/O thread.
                    bool ok = await Task.Run(() => session.PassesDefaultValidation(), ct);
                    session.CompleteCertificateValidation(ok);
                    if (!ok) await DrainAsync(session, socket, netOut, ct);  // flush alert
                    continue;

                case TlsOperationStatus.Closed:
                    throw new IOException("Peer closed the connection during handshake.");
            }
        }

        // Send the request, read the response (omitted for brevity — same loop shape
        // as the handshake, calling Encrypt / Decrypt instead of ProcessHandshake).
        return await ExchangeAsync(session, socket, Encoding.ASCII.GetBytes(request), netIn, netOut, plain, ct);
    }
    finally
    {
        ArrayPool<byte>.Shared.Return(netIn);
        ArrayPool<byte>.Shared.Return(netOut);
        ArrayPool<byte>.Shared.Return(plain);
    }
}

static async Task DrainAsync(TlsSession session, Socket socket, byte[] buffer, CancellationToken ct)
{
    while (session.HasPendingOutput)
    {
        var s = session.DrainPendingOutput(buffer, out int n);
        if (n > 0) await socket.SendAsync(buffer.AsMemory(0, n), ct);
        if (s != TlsOperationStatus.WantWrite) break;
    }
}

static void Consume(byte[] buf, ref int used, int n)
{
    if (n == 0) return;
    if (n < used) Buffer.BlockCopy(buf, n, buf, 0, used - n);
    used -= n;
}
```

### Socket-bound server with SNI-based cert selection

```csharp
// One-time, shared across the listener.
ConcurrentDictionary<string, TlsContext> contextsBySni = LoadContexts();
TlsContext fallback = TlsContext.Create(MinimalServerOptions());  // for ClientHello parsing only

async Task HandleAsync(Socket client, CancellationToken ct)
{
    client.Blocking = false;
    using var session = TlsSession.Create(fallback, client.SafeHandle);

    // Handshake — readiness pump.
    while (!session.IsHandshakeComplete)
    {
        var status = session.Handshake();
        switch (status)
        {
            case TlsOperationStatus.Complete:
                break;

            case TlsOperationStatus.WantRead:
                await PollReadableAsync(client, ct);
                break;

            case TlsOperationStatus.WantWrite:
                await PollWritableAsync(client, ct);
                break;

            case TlsOperationStatus.NeedsServerOptions:
                var hello = session.PendingClientHello!;
                var ctx = contextsBySni.GetValueOrDefault(hello.ServerName)
                          ?? contextsBySni["default"];
                session.SetServerOptions(BuildOptionsFor(ctx, hello));
                break;

            case TlsOperationStatus.Closed:
                return;
        }
    }

    // Steady state — session does recv/send itself.
    byte[] buf = ArrayPool<byte>.Shared.Rent(16 * 1024);
    try
    {
        while (true)
        {
            var rs = session.Read(buf, out int n);
            if (rs == TlsOperationStatus.WantRead)  { await PollReadableAsync(client, ct); continue; }
            if (rs == TlsOperationStatus.WantWrite) { await PollWritableAsync(client, ct); continue; }
            if (rs == TlsOperationStatus.Closed || n == 0) return;

            await HandleRequestAsync(session, buf.AsMemory(0, n), ct);
        }
    }
    finally { ArrayPool<byte>.Shared.Return(buf); }
}
```

### Migration from `SslStream` (one-line default validation)

```csharp
// What the new code looks like for the common case:
case TlsOperationStatus.NeedsCertificateValidation:
    if (!session.AcceptWithDefaultValidation())
    {
        // Optional: log the rejection.
    }
    continue;
```

`AcceptWithDefaultValidation` performs the same chain build, hostname match,
and revocation policy that `SslStream` uses by default. Anyone who didn't pass
a custom `RemoteCertificateValidationCallback` to `SslStream` gets equivalent
behavior with this single `case`.

---

## Adapter Types

`TlsSession` is the primitive. Two adapters ship alongside it in
`System.Net.Security` to give each ecosystem the shape it expects without
forcing the primitive to know about either.

### `TlsDuplexPipe` — `IDuplexPipe` wrapper

For Kestrel-style consumers. Wraps an `IDuplexPipe` transport and produces an
`IDuplexPipe` of plaintext. Internally owns a `TlsSession` and runs two
background pumps (inbound: transport → `Decrypt` → plaintext pipe; outbound:
plaintext pipe → `Encrypt` → transport). Exposes async-shaped callbacks for
the suspension states, because async is natural at the pipe layer.

```csharp
public sealed class TlsDuplexPipe : IDuplexPipe, IAsyncDisposable
{
    public static ValueTask<TlsDuplexPipe> CreateAsync(
        TlsContext context,
        IDuplexPipe transport,
        TlsDuplexPipeOptions? options = null,
        CancellationToken cancellationToken = default);

    public PipeReader Input { get; }
    public PipeWriter Output { get; }

    // Session metadata after handshake.
    public SslProtocols NegotiatedProtocol { get; }
    public TlsCipherSuite NegotiatedCipherSuite { get; }
    public SslApplicationProtocol NegotiatedApplicationProtocol { get; }
    public X509Certificate2? GetRemoteCertificate();
    // ...

    public ValueTask DisposeAsync();
}

public sealed class TlsDuplexPipeOptions
{
    public string? TargetHostName { get; set; }
    public Func<SslClientHelloInfo, ValueTask<SslServerAuthenticationOptions>>? ServerOptionsSelector { get; set; }
    public Func<TlsCertificateValidationContext, ValueTask<bool>>? CertificateValidator { get; set; }
    public PipeOptions? InputPipeOptions { get; set; }
    public PipeOptions? OutputPipeOptions { get; set; }
}
```

### `Stream` adapter

For `SslStream` migrants who want the new contract but still consume via
`Stream`. Either an extension method (`session.AsStream(Stream transport)`) or
a thin sealed `Stream` wrapper. Implementation is ~150 lines on top of the
public `TlsSession` API.

### `SslStream` re-hosting

Once the primitive lands, `SslStream`'s implementation is rewritten on top of
`TlsSession`:

- `ForceAuthenticationAsync` becomes a loop over `ProcessHandshake`.
- `ReadAsyncInternal` becomes a loop over `Decrypt` (+ `DrainPendingOutput` on
  `WantWrite`, await the inner stream on `WantRead`).
- `WriteAsyncInternal` becomes a loop over `Encrypt`.
- `RemoteCertificateValidationCallback` is invoked from the
  `NeedsCertificateValidation` case.
- `ServerOptionsSelectionCallback` is invoked from the
  `NeedsServerOptions` case.

The provider-specific PAL layer (`SslStreamPal.Unix.cs`,
`SslStreamPal.Windows.cs`, the managed PAL) is shared between `TlsSession` and
the rehosted `SslStream`. There is one TLS state machine implementation in the
box.

---

## Open Questions

1. **OpenSSL 1.1.1 fallback for `NeedsCertificateValidation`.** `PlatformNotSupportedException`
   (current proposal) vs. silently disable the suspension and require post-handshake
   validation. The exception is more honest but is a hard breakage for the minor
   slice of users still on 1.1.1. Recommendation: exception, given 1.1.1 EOL.

2. **`X509RevocationMode` default in `AcceptWithDefaultValidation`.** Match
   `SslStream` historical default (`NoCheck`) vs. modern best practice
   (`Online`). Current proposal: match `SslStream`. Easy to change later as a
   default-value adjustment.

3. **Where does `TlsDuplexPipe` live?** Same assembly as `TlsSession`
   (`System.Net.Security`) vs. a separate package. Current proposal: same
   assembly, since `System.IO.Pipelines` is already an inbox shared framework
   dependency on .NET.

4. **`Stream` adapter shape.** Extension method (`session.AsStream(transport)`)
   vs. dedicated public type. Recommendation: extension method returning a
   private sealed `Stream`.

5. **Async cancellation in socket-bound mode.** `Read` / `Write` /
   `Handshake` don't take `CancellationToken` (they're synchronous non-blocking).
   Cancellation is "dispose the session." Confirm this is acceptable.

6. **Telemetry.** Hook `TlsSession` into `NetSecurityTelemetry` the same way
   `SslStream` is today. Mostly mechanical, but worth confirming the event
   shape (e.g. do we want a `tls.session.kind = "detached" | "socket-bound"`
   tag).

7. **macOS Network.framework future.** A future macOS PAL on
   `nw_protocol_options_tls_*` could give us a Linux-style fast path
   (Network.framework owns the socket). The unified type accommodates this
   without an API change. Confirm we're comfortable not committing to it now.

---

## Functional Comparison with `SslStream`

### Covered with no surface gap

- Handshake (client / server), read, write, shutdown.
- Negotiated protocol / cipher / ALPN / session-resumed / peer cert.
- SNI (`TargetHostName`).
- Renegotiation, TLS 1.3 KeyUpdate, NewSessionTicket, alerts (handled implicitly).
- Channel binding (`GetChannelBinding`).
- All `SslServer/ClientAuthenticationOptions` configuration knobs.

### Covered with explicit suspension protocol

- `ServerOptionsSelectionCallback` → `NeedsServerOptions` + `SetServerOptions`.
- `RemoteCertificateValidationCallback` → `NeedsCertificateValidation` +
  `CompleteCertificateValidation`.
- Post-handshake authentication (TLS 1.3 PHA) → `RequestPostHandshakeAuthentication`.

### Layered above the primitive

- `Stream` shape → adapter.
- `IDuplexPipe` shape → `TlsDuplexPipe`.
- Default validation policy → `AcceptWithDefaultValidation` extension.

### Deliberate omissions

- Internal certificate validation (caller's responsibility).
- `SslPolicyErrors` enum on the API (no internal validation means no errors to report).
- Async configuration callbacks running on the I/O thread (replaced by suspension).
- `bool isServer` parameter (inferred from `TlsContext` options type).

---

## Implementation Sketch

Approximate scope, assuming the existing PAL stays:

1. **New status codes.** Add `WantRead` / `WantWrite` (distinct from today's
   collapsed `OK`) and the two `Needs…` codes to `SecurityStatusPalErrorCode`
   plumbing. Map in each PAL's `MapNativeErrorCode`. Small.

2. **Suspension wiring.**
   - Linux: register `client_hello_cb` and `cert_verify_callback` on
     `SSL_CTX`; thread suspension state through the OpenSSL `SSL*` app-data
     slot.
   - Windows: pre-parse SNI from the ClientHello (`TlsFrameHelper` already
     does this); set `SCH_CRED_MANUAL_CRED_VALIDATION`; gate handshake output
     drain on validation verdict.
   - macOS managed: add explicit states to the state machine.

3. **`TlsContext` / `TlsSession` types.** New ref-source entries, new
   implementation files in `System.Net.Security`. Roughly mirror
   `SafeDeleteSslContext` ownership patterns.

4. **`SslStream` re-hosting.** Rewrite `SslStream.IO.cs` on top of
   `TlsSession`. Mostly mechanical; the existing async loop becomes a thinner
   driver over the new public API.

5. **Adapter types.** `TlsDuplexPipe`, `Stream` adapter,
   `TlsSessionExtensions`. Pure managed code on top of `TlsSession`.

6. **Tests.** Re-purpose existing `SslStream` interop tests against
   `TlsSession` directly, plus new tests for the suspension protocol on each
   platform.

No new P/Invokes are required for the detached / cross-platform path. The
socket-bound Linux path adds one (`SSL_set_fd`); other platforms' socket-bound
mode is implemented entirely in managed code over `Decrypt` / `Encrypt`.

---

## Appendix A: Comparison with the Original `SafeTlsContextHandle` Proposal

[#127928] originally proposed two `SafeHandle` types
(`SafeTlsContextHandle`, `SafeTlsHandle`) and a separate split between
`TlsDetachedSession` / `TlsSocketBoundSession`. The shape evolved as follows:

| Original | Now | Rationale |
|---|---|---|
| `SafeTlsContextHandle` : `SafeHandle` with public instance methods | `TlsContext` : `IDisposable` | `SafeHandle` with public instance API is unusual; it's really a `TlsContext` object that happens to hold a handle (@bartonjs) |
| `SafeTlsHandle` : `SafeHandle` | `TlsSession` : `IDisposable` | Same reasoning |
| Two session types (`TlsDetachedSession` / `TlsSocketBoundSession`) | One `TlsSession` with two factories | The session contract is identical; socket binding is an implementation detail of *one* method group |
| `[SupportedOSPlatform("linux")]` on socket-bound type | No platform annotation; Linux is a fast path | Other platforms implement socket-bound mode via internal pump; same API on every platform (@rzikm) |
| `bool isServer` | Inferred from options type at `TlsContext` creation | Removes a class of mismatched-options bugs |
| Async config via callbacks running on the I/O thread | Explicit suspension states | Decouples validation work from the I/O thread; integrates cleanly with async/await; cancellation is "dispose" |
| Internal default validation | No validation; caller decides | True primitive; default policy is opt-in via extension method |

The shape that survived is the one that fits cleanly under both
`SslStream` (rehosted) and `TlsDuplexPipe` (new adapter) — i.e. it's the
extracted PAL contract, polished and made public.
