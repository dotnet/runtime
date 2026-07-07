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

All new public API surface is marked with `[Experimental("SYSLIB5007")]` for now.

```csharp
namespace System.Net.Security;

public enum TlsOperationStatus
{
    /// <summary>The call made forward progress. Check <c>bytesConsumed</c>/<c>bytesWritten</c>.</summary>
    Complete = 0,

    /// <summary>The session needs more ciphertext from the peer to make progress.</summary>
    WantRead = 1,

    /// <summary>
    /// The session has ciphertext to send. Call <see cref="TlsSession.DrainPendingOutput"/>
    /// before retrying, or hand the output span from the current call to the transport.
    /// </summary>
    WantWrite = 2,

    /// <summary>The transport is gone or <c>close_notify</c> was received. Dispose the session.</summary>
    Closed = 3,

    /// <summary>
    /// Client-side only. The server sent a CertificateRequest and the session has no
    /// certificate context. Fetch a certificate (possibly out-of-band), call
    /// <see cref="TlsSession.SetClientCertificateContext"/>, then retry.
    /// Use <see cref="TlsSession.GetAcceptableIssuers"/> to inspect the server's hints.
    /// </summary>
    WantCredentials = 4,

    /// <summary>
    /// The peer presented a certificate awaiting the caller's verdict. Inspect
    /// <see cref="TlsSession.GetRemoteCertificate"/> / <see cref="TlsSession.GetRemoteCertificates"/>,
    /// then either call <see cref="TlsSession.AcceptWithDefaultValidation"/> for the
    /// same policy <see cref="SslStream"/> uses by default, or perform custom checks and
    /// call <see cref="TlsSession.SetRemoteCertificateValidationResult"/> with the verdict.
    /// </summary>
    NeedsCertificateValidation = 5,

    /// <summary>
    /// Server-side only. The peer's ClientHello has been received but server options
    /// have not been supplied. Inspect <see cref="TlsSession.ClientHelloInfo"/>,
    /// call <see cref="TlsSession.SetServerContext"/>, then retry.
    /// </summary>
    NeedsServerOptions = 6,
}

/// <summary>
/// Long-lived TLS configuration. Thread-safe; create once per logical endpoint
/// (or per virtual host) and share across many <see cref="TlsSession"/> instances.
/// The context owns the underlying SSL_CTX (or its per-platform equivalent) and its
/// session-ticket cache.
/// </summary>
public sealed class TlsContext : IDisposable
{
    /// <summary>
    /// Creates a server context. Pass <see langword="null"/> to defer options until
    /// the ClientHello arrives - the caller then inspects the SNI on
    /// <see cref="TlsSession.ClientHelloInfo"/> and steers the session onto a
    /// per-tenant context via <see cref="TlsSession.SetServerContext"/>.
    /// </summary>
    public static TlsContext Create(SslServerAuthenticationOptions? options);

    public static TlsContext Create(SslClientAuthenticationOptions options);

    public bool IsServer { get; }

    public void Dispose();
}

/// <summary>
/// A per-connection TLS session driving a non-blocking TLS state machine.
/// In detached mode the caller performs all socket I/O via <see cref="ProcessHandshake"/> /
/// <see cref="Encrypt"/> / <see cref="Decrypt"/>. In socket-bound mode
/// (<see cref="Create(TlsContext, SafeSocketHandle)"/>) the session reads and writes on
/// the bound socket via <see cref="Handshake"/> / <see cref="Read"/> / <see cref="Write"/>;
/// on Linux this uses OpenSSL's fd-binding fast path with no managed-side
/// ciphertext copies.
/// </summary>
public sealed class TlsSession : IDisposable
{
    // ── Construction ──────────────────────────────────────────────────────

    /// <summary>Creates a detached session. The caller drives all I/O.</summary>
    public static TlsSession Create(TlsContext context);

    /// <summary>
    /// Creates a socket-bound session. The session reads and writes on
    /// <paramref name="socket"/>. The socket must be non-blocking. The session takes
    /// ownership of the handle for the lifetime of the session.
    /// </summary>
    public static TlsSession Create(TlsContext context, SafeSocketHandle socket);

    // ── Identity / state ──────────────────────────────────────────────────

    /// <summary>The bound socket handle, or <see langword="null"/> for detached sessions.</summary>
    public SafeSocketHandle? Socket { get; }

    public bool IsHandshakeComplete { get; }

    /// <summary>
    /// SNI / target hostname. On client sessions the caller sets this before the first
    /// handshake call (or supplies it via <c>SslClientAuthenticationOptions.TargetHost</c>).
    /// On server sessions it is automatically populated from the parsed ClientHello SNI
    /// extension once the ClientHello has been received (subject to the
    /// <c>System.Net.Security.CaptureClientHello</c> switch — see <see cref="GetClientHelloBytes"/>).
    /// </summary>
    public string? TargetHostName { get; set; }

    // ── Negotiated info (valid after IsHandshakeComplete) ─────────────────

    public SslProtocols NegotiatedProtocol { get; }
    public TlsCipherSuite NegotiatedCipherSuite { get; }
    public SslApplicationProtocol NegotiatedApplicationProtocol { get; }

    /// <summary>
    /// The certificate the session presented to the peer (server cert on server sessions,
    /// client cert on client sessions with mTLS). <see langword="null"/> when the local
    /// side did not present a certificate.
    /// </summary>
    public X509Certificate2? LocalCertificate { get; }

    /// <summary>
    /// The certificate the peer presented (raw, unvalidated). May be non-null before
    /// the handshake completes and before the caller has accepted it.
    /// </summary>
    public X509Certificate2? GetRemoteCertificate();

    /// <summary>
    /// The full certificate collection the peer sent (leaf + intermediates), or
    /// <see langword="null"/> when the peer did not present a certificate.
    /// </summary>
    public X509Certificate2Collection? GetRemoteCertificates();

    /// <summary>RFC 5929 channel binding token.</summary>
    public ChannelBinding? GetChannelBinding(ChannelBindingKind kind);

    // ── ClientHello data (server-side, populated once the ClientHello is received) ─

    /// <summary>
    /// Parsed ClientHello (SNI + supported versions). Populated on every server session
    /// once the ClientHello has been received and remains populated until
    /// <see cref="Dispose"/>. <see langword="null"/> before the ClientHello arrives, on
    /// client sessions, and on server sessions where capture is disabled (see below).
    /// </summary>
    public SslClientHelloInfo? ClientHelloInfo { get; }

    /// <summary>
    /// Returns the raw ClientHello record bytes (5-byte TLS record header plus the
    /// ClientHello handshake message). The span is valid until <see cref="Dispose"/>;
    /// callers who need to persist the bytes should <c>.ToArray()</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown on client sessions, before the ClientHello has been received, or on
    /// server sessions where the <c>System.Net.Security.CaptureClientHello</c> AppContext
    /// switch is disabled AND options were supplied at <see cref="TlsContext"/> creation.
    /// </exception>
    public ReadOnlySpan<byte> GetClientHelloBytes();

    // ── Suspension resolvers ──────────────────────────────────────────────

    /// <summary>
    /// Server-side only. Resolves a session suspended on
    /// <see cref="TlsOperationStatus.NeedsServerOptions"/> by adopting the supplied
    /// <see cref="TlsContext"/>. The context's SSL_CTX (with its ticket cache and
    /// per-tenant options) backs the session for the rest of the handshake and its
    /// steady state. Callers that maintain a pool of virtual-host contexts (keyed by
    /// SNI + options fingerprint) use this to steer the session onto the pre-warmed
    /// context matching the ClientHello.
    /// </summary>
    public void SetServerContext(TlsContext serverContext);

    /// <summary>
    /// Client-side only. Supplies the certificate the session should send in response
    /// to the server's CertificateRequest. Resolves a session suspended on
    /// <see cref="TlsOperationStatus.WantCredentials"/>. May also be called before
    /// the first handshake call to seed the client credential when the
    /// <see cref="TlsContext"/> was created without one.
    /// </summary>
    public void SetClientCertificateContext(SslStreamCertificateContext context);

    /// <summary>
    /// Client-side only. Returns the acceptable issuer hints the server sent in its
    /// CertificateRequest, or <see langword="null"/> if none were sent. Meaningful while
    /// suspended on <see cref="TlsOperationStatus.WantCredentials"/>.
    /// </summary>
    public IReadOnlyList<string>? GetAcceptableIssuers();

    /// <summary>
    /// Runs the same chain build + hostname match + policy as <see cref="SslStream"/>'s
    /// default validation over the peer certificate and returns the resulting
    /// <see cref="SslPolicyErrors"/>. Also stamps the verdict onto the session, so
    /// the next call resumes the suspended handshake (Finished on accept, fatal alert
    /// on any error flag set).
    /// </summary>
    public SslPolicyErrors AcceptWithDefaultValidation();

    /// <summary>
    /// Supplies the verdict for the peer certificate from a custom validation. Pass
    /// <see cref="SslPolicyErrors.None"/> to accept, or any error flags to reject with
    /// a fatal <c>bad_certificate</c> alert. Throws if the session is not currently
    /// suspended on <see cref="TlsOperationStatus.NeedsCertificateValidation"/>.
    /// </summary>
    public void SetRemoteCertificateValidationResult(SslPolicyErrors errors);

    // ── State machine (detached mode) ─────────────────────────────────────

    /// <summary>Advances the handshake by one step.</summary>
    public TlsOperationStatus ProcessHandshake(
        ReadOnlySpan<byte> input, Span<byte> output,
        out int bytesConsumed, out int bytesWritten);

    /// <summary>
    /// Decrypts one record's worth of ciphertext. Throws
    /// <see cref="InvalidOperationException"/> before the handshake is complete.
    /// </summary>
    public TlsOperationStatus Decrypt(
        ReadOnlySpan<byte> ciphertext, Span<byte> plaintext,
        out int bytesConsumed, out int bytesWritten);

    /// <summary>
    /// Encrypts plaintext into ciphertext. Throws
    /// <see cref="InvalidOperationException"/> before the handshake is complete.
    /// </summary>
    public TlsOperationStatus Encrypt(
        ReadOnlySpan<byte> plaintext, Span<byte> ciphertext,
        out int bytesConsumed, out int bytesWritten);

    /// <summary>
    /// True when the session has ciphertext for the caller to send to the peer
    /// (handshake messages, alerts, KeyUpdate responses, <c>close_notify</c>).
    /// </summary>
    public bool HasPendingOutput { get; }

    /// <summary>
    /// Copies pending ciphertext into <paramref name="ciphertext"/>. If the span was
    /// too small to drain everything, returns <see cref="TlsOperationStatus.WantWrite"/>;
    /// call again with a fresh buffer.
    /// </summary>
    public TlsOperationStatus DrainPendingOutput(Span<byte> ciphertext, out int bytesWritten);

    // ── Socket-bound convenience ──────────────────────────────────────────
    // Throw InvalidOperationException on detached sessions.

    public TlsOperationStatus Handshake();
    public TlsOperationStatus Read(Span<byte> buffer, out int bytesRead);
    public TlsOperationStatus Write(ReadOnlySpan<byte> buffer, out int bytesWritten);

    // ── Shutdown / advanced ───────────────────────────────────────────────

    /// <summary>
    /// Emits a TLS <c>close_notify</c> alert into <paramref name="ciphertext"/>.
    /// The caller sends the produced bytes to the peer. Returns <see cref="TlsOperationStatus.WantWrite"/>
    /// if the buffer was too small for the alert record; call again with a larger buffer.
    /// </summary>
    public TlsOperationStatus Shutdown(Span<byte> ciphertext, out int bytesWritten);

    /// <summary>
    /// Server-side only. Emits a TLS 1.3 post-handshake CertificateRequest into
    /// <paramref name="ciphertext"/>. Subsequent <see cref="Decrypt"/> calls drive
    /// the auth flow and may surface <see cref="TlsOperationStatus.NeedsCertificateValidation"/>
    /// when the client's Certificate message arrives.
    /// </summary>
    public TlsOperationStatus RequestClientCertificate(Span<byte> ciphertext, out int bytesWritten);

    public void Dispose();
}
```

---

## Contract

### Status semantics

| Status | Meaning | Caller action |
|---|---|---|
| `Complete` | Call made progress | Check `bytesWritten` / `bytesConsumed`; continue if more work expected |
| `WantRead` | Need more ciphertext from peer | `recv` from transport; append; retry |
| `WantWrite` | Pending output must be flushed | `DrainPendingOutput`; send to peer; retry |
| `Closed` | Transport gone or `close_notify` received | Dispose |
| `WantCredentials` | Client cert requested, none supplied | Fetch cert (`GetAcceptableIssuers` for hints); call `SetClientCertificateContext`; retry |
| `NeedsCertificateValidation` | Peer cert presented, awaiting verdict | Inspect cert; call `AcceptWithDefaultValidation` OR custom check + `SetRemoteCertificateValidationResult`; retry |
| `NeedsServerOptions` | ClientHello received, options not supplied | Inspect `ClientHelloInfo`; call `SetServerContext`; retry |

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
Finished in the mTLS case). The caller supplies the verdict via
`AcceptWithDefaultValidation()` (which runs the same chain build, hostname
match, and revocation policy as `SslStream` uses by default) or
`SetRemoteCertificateValidationResult(SslPolicyErrors)` for a custom check.

If the verdict is reject (any error flag set), the session emits a fatal
alert at exactly the right protocol state and the peer never observes a
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
| `SetServerContext` while not suspended on `NeedsServerOptions` | `InvalidOperationException` |
| `SetRemoteCertificateValidationResult` while not suspended on `NeedsCertificateValidation` | `InvalidOperationException` |
| `SetClientCertificateContext` on a server session | `InvalidOperationException` |
| `GetClientHelloBytes` when unavailable (client session, before ClientHello, or capture disabled) | `InvalidOperationException` |
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

### ClientHello capture

On server sessions the ClientHello record is peeked managed-side (via a
socket-replay BIO on Linux, natively on other PALs) so that `ClientHelloInfo`,
`TargetHostName`, and `GetClientHelloBytes()` are populated the same way
regardless of whether the caller uses the SNI-callback flow
(`TlsContext.Create((SslServerAuthenticationOptions?)null)` +
`SetServerContext`) or supplies options up front. The retained peek buffer is
handed to the SSL* as its read BIO after `SetServerContext`, so the handshake
then runs unchanged from OpenSSL's point of view.

The measured overhead of peek + parse + BIO-handoff vs. the pre-capture
`SSL_set_fd` fast path is ~10-30 µs per handshake and ~500 B (loopback bench,
long-run pinned, TLS 1.2 and TLS 1.3 both full and resumed). That's inside the
noise floor of a real-network handshake, so capture is always on by default.

**Opt-out**: `System.Net.Security.CaptureClientHello` (`AppContext` switch, also
readable from `DOTNET_SYSTEM_NET_SECURITY_CAPTURECLIENTHELLO=0`), cached once at
process init. Only meaningful on server sessions with options supplied up front —
the deferred / SNI-callback and buffered / managed-loop paths always capture
because they parse the ClientHello anyway. When the switch is off,
`GetClientHelloBytes()` throws `InvalidOperationException` on server sessions that
took the pure-fd fast path; `ClientHelloInfo` and `TargetHostName` behave as they
did pre-capture on that path.

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
                    // Run validation off the I/O thread; AcceptWithDefaultValidation both
                    // performs the check and stamps the verdict onto the session.
                    var errors = await Task.Run(session.AcceptWithDefaultValidation, ct);
                    if (errors != SslPolicyErrors.None)
                    {
                        await DrainAsync(session, socket, netOut, ct);  // flush fatal alert
                        throw new AuthenticationException($"Server cert rejected: {errors}");
                    }
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
// Bootstrap context (no cert). The session is created against it so the ClientHello can
// be peeked managed-side; SetServerContext then adopts the per-tenant context.
TlsContext bootstrap = TlsContext.Create((SslServerAuthenticationOptions?)null);

async Task HandleAsync(Socket client, CancellationToken ct)
{
    client.Blocking = false;
    using var session = TlsSession.Create(bootstrap, client.SafeHandle);

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
                var hello = session.ClientHelloInfo!.Value;
                var ctx = contextsBySni.GetValueOrDefault(hello.ServerName)
                          ?? contextsBySni["default"];
                session.SetServerContext(ctx);
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
    if (session.AcceptWithDefaultValidation() != SslPolicyErrors.None)
    {
        // Optional: log the rejection. The session has already stamped the
        // reject verdict; the next call flushes the fatal alert.
    }
    continue;
```

`AcceptWithDefaultValidation` performs the same chain build, hostname match,
and revocation policy that `SslStream` uses by default and returns the
resulting `SslPolicyErrors`. `SslPolicyErrors.None` accepts, anything else
rejects. Anyone who didn't pass a custom `RemoteCertificateValidationCallback`
to `SslStream` gets equivalent behavior with this single `case`.

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
- SNI (`TargetHostName`, populated server-side from the parsed ClientHello).
- Raw ClientHello bytes via `GetClientHelloBytes()` (server-side; JA3 fingerprinting, tenant routing, audit logging).
- Renegotiation, TLS 1.3 KeyUpdate, NewSessionTicket, alerts (handled implicitly).
- Channel binding (`GetChannelBinding`).
- All `SslServer/ClientAuthenticationOptions` configuration knobs.

### Covered with explicit suspension protocol

- `ServerOptionsSelectionCallback` → `NeedsServerOptions` + `SetServerContext`.
- `RemoteCertificateValidationCallback` → `NeedsCertificateValidation` +
  `AcceptWithDefaultValidation` / `SetRemoteCertificateValidationResult`.
- `LocalCertificateSelectionCallback` → `WantCredentials` + `SetClientCertificateContext`
  (with `GetAcceptableIssuers` for the server's CertificateRequest hints).
- Post-handshake authentication (TLS 1.3 PHA) → `RequestClientCertificate`.

### Layered above the primitive

- `Stream` shape → adapter.
- `IDuplexPipe` shape → `TlsDuplexPipe`.
- Default validation policy is a member method (`AcceptWithDefaultValidation`),
  not an extension, because it needs to stamp the verdict onto the session
  state to resume the suspended handshake.

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

5. **Adapter types.** `TlsDuplexPipe` and the `Stream` adapter. Pure managed
   code on top of `TlsSession`.

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
