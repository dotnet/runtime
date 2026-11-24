# Sockets, DNS, WebSockets & Uri Rules

Checks for `System.Net.Sockets`, `System.Net.NameResolution`, `System.Net.Primitives`, `System.Net.NetworkInformation`, `System.Net.Ping`, `System.Net.WebSockets*`, `System.Net.Mail`, and `System.Private.Uri`.

---

## Sockets & Async Engine

| Check | What to look for |
|-------|-----------------|
| **Connect, send-file, and socket options preserve OS semantics** | Dual-mode/parallel connect paths that skip cleanup or telemetry another path performs, `SendFile`/`TransmitFile` fallbacks that drop pre/post buffers or do not send data in the `#else` branch, socket options applied to the wrong `SocketType`, or IPv6 endpoints built without `ScopeId`. Keep every connect path symmetric, coalesce buffers exactly as the native API does, check option validity per socket type, and carry `ScopeId` for link-local addresses. (`#99490`, `#106374`, `#111791`) |
| **Async engine readiness and pinned buffers** | `SocketAsyncContext` callbacks invoked on new paths, readiness/EOF state lost between `epoll`/`kqueue`/`io_uring` events, global and per-socket opt-outs that conflict, assuming a 0-byte `ReceiveAsync` waits for readability on every platform, or IOCP pinned buffer handles not released on every exception path. Keep one readiness state machine, document precedence of global vs local switches, and dispose pinning handles in `finally`. (`#64770`, `#124200`, `#130463`) |
| **Unix buffered `ConnectAsync` symmetry, blocking mode, and telemetry** | `SocketAsyncContext.Unix.cs` `ConnectOperation` paths where synchronous vs asynchronous connect completion diverges on sending buffered payloads (`TryCompleteSendTo` when `BytesTransferred > 0` or zero-length buffer) or forgets `_socket.SetBlocking()` on one completion branch, or `SocketsTelemetry` counting non-blocking `WouldBlock`/`InProgress` returns as failed connects. Keep sync and async connect-with-payload completion symmetric and restore blocking mode on every exit. |

---

## DNS, NetworkInformation & Legacy Protocols

| Check | What to look for |
|-------|-----------------|
| **DNS callbacks and record parsing are safe** | Windows `GetAddrInfoEx`/`DnsQueryEx` callbacks racing cancellation registration or disposal, cancellation registered inside a `try` whose `catch` frees state the callback still uses, malformed records silently skipped instead of failing the response, or `localhost`/`*.localhost` and hosts-file rules applied inconsistently. Register cancellation after the operation starts and outside cleanup handlers, keep callback state alive until the final callback, and treat malformed requested-type records as errors. (`#129845`, `#129846`, `#123076`) |
| **NetworkInformation, Ping, and Cookie follow the platform data** | `getifaddrs` loops that miss `AF_PACKET`/link-layer entries, BSD `sysctl` table parsing off by a struct, Ping reply fields read when the status is not success, `ping` vs `ping6` selection that changes behavior, or cookie domain matching that ignores leading-dot rules. Handle every address family the OS returns, read reply fields only for success, and follow RFC 6265 domain rules. (`#125022`, `#124424`, `#103838`) |

---

## WebSockets & Uri

| Check | What to look for |
|-------|-----------------|
| **WebSocket close and framing follow RFC 6455** | Close handshakes that can hang or race with an in-flight receive, a single `ReceiveAsync` assumed to return a whole message, UTF-8 validation that fails on code points split across fragments, keep-alive state touched from multiple threads, or HTTP/2 WebSocket downgrade logic that reuses a sent `HttpRequestMessage` or checks the requested instead of negotiated version. Loop until `EndOfMessage`, validate UTF-8 incrementally, serialize state transitions, and build a fresh request for a retry. (`#127183`, `#74579`, `#70895`) |
| **Uri formatting is canonical and allocation-aware** | Code that uses `Uri.ToString()`/`AbsoluteUri` where the original or escaped form is required, ignores `DangerousDisablePathAndQueryCanonicalization`, applies redaction to the user name but not the password, or sends Unicode hosts where punycode/IDN is required (and mishandles IDN length limits). Pick the `UriComponents`/format explicitly, convert hosts with `IdnMapping` for the wire, and redact every userinfo component. (`#103769`, `#123934`, `#74953`) |
| **`Uri.Flags` invariants and mutable client options** | Changing `Uri` scheme/authority flags without tracing coupled `Debug.Assert` invariants across `CreateThis`, `CheckCanonical`, and `GetComponentsHelper` (especially custom schemes and `MustHaveAuthority` `file:` URIs without `//` such as `file:C:/...` or `file:/C:/...`), or guarding mutable client options setters (`SmtpClient` TLS options) with `!ReferenceEquals` instead of invalidating pooled connections unconditionally and preserving `TargetHost = string.Empty` (`??=`). |
