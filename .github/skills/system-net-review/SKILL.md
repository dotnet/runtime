---
name: system-net-review
description: "Guidance for writing and reviewing System.Net networking code in dotnet/runtime: resource lifecycle, connection pooling, cross-platform interop, protocol compliance, async patterns, and security posture. For reviews, routes to the @system-net-review agent and its references/ checklists. Trigger words: system.net, networking, http client, sockets, ssl, tls, quic, http2, http3, socketshttphandler, sslstream, connection pool."
---

# Writing & Reviewing System.Net Code

> **Authoring guide only — not an independent review:** Loading this skill (`skill("system-net-review")`) puts authoring rules into your own context; it does **not** satisfy the [author review gate](../code-review/author-review.md). Before finishing a behavioral change, spawn the **`@system-net-review`** custom agent in a **fresh read-only sub-agent** (`task` / sub-agent tool) when sub-agents are available.

Use this skill when writing or reviewing code under `src/libraries/System.Net.*`,
`src/libraries/System.Private.Uri`, `src/libraries/Microsoft.Extensions.Http*`, shared networking
code in `src/libraries/Common/src/System/Net` and networking `Interop`, and the networking native
PAL (`src/native/libs/System.Native/pal_network*`, `src/native/libs/System.Net.Security.Native*`).
General conventions stay in `.github/instructions/` (including the `system-net-*` files with RFC
pointers), the `build-and-test` skill, and the `code-review` skill.

## Reviewing

The [`@system-net-review` agent](../../agents/system-net-review.agent.md) holds the review
workflow; the checks are in [`references/`](references/):

| Reference | Covers |
|-----------|--------|
| [`ai-and-repo-conventions.md`](references/ai-and-repo-conventions.md) | Diff hygiene, naming, comments, dead code, API docs, exceptions, breaking changes, AI-code pitfalls (always) |
| [`csharp-async-and-memory-rules.md`](references/csharp-async-and-memory-rules.md) | Async, cancellation, races, lifetime, streams, allocations, overflow, untrusted-input bounds |
| [`http-and-clientfactory-rules.md`](references/http-and-clientfactory-rules.md) | `SocketsHttpHandler`, HTTP/1.1/2/3 framing, headers, pools, proxies, platform handlers, `IHttpClientFactory` |
| [`tls-and-security-rules.md`](references/tls-and-security-rules.md) | `SslStream`, X.509, SChannel/OpenSSL/Apple/Android PALs, Negotiate, credentials |
| [`sockets-dns-ws-uri-rules.md`](references/sockets-dns-ws-uri-rules.md) | Sockets, DNS, WebSockets, `Uri`, NetworkInformation, Ping |
| [`quic-rules.md`](references/quic-rules.md) | `System.Net.Quic`, MsQuic interop, HTTP/3 transport |
| [`native-msbuild-and-testing-rules.md`](references/native-msbuild-and-testing-rules.md) | Interop, C PAL, CMake, MSBuild/ref, trimming/AOT/WASM, tests |

Invoke the agent as a sub-task when sub-agents are available; tell it to return findings, not
post them. In `code-review` reviewer-worker mode, read the agent file and the matching
references as a checklist instead of invoking it.

## Writing networking code

Before finishing, walk the references that match your change as a self-check. The essentials:

- **Ownership:** every resource has one owner. Native handles are `SafeHandle`s, never raw
  `IntPtr` across async boundaries; dispose on error paths with `try/finally`; `Dispose` is
  idempotent and doesn't throw; pooled buffers return in `finally`.
- **Pools:** never hold a lock across I/O or `await`; return only reusable connections (no
  partial reads or pending writes); pool disposal drains waiters and closes connections.
- **Platforms:** platform code lives in `*.Windows.cs`/`*.Unix.cs`/`*.OSX.cs` partials and
  `Common/src/Interop/<platform>/`; new P/Invokes use `[LibraryImport]`; map native errors to
  `SocketException`/`Win32Exception`/`AuthenticationException` and fix every sibling platform.
- **Protocols:** follow the RFCs in the `system-net-*` instruction files; peer violations raise
  clear exceptions instead of corrupting state; gate version-specific behavior on the negotiated
  version or ALPN.
- **Async:** accept and flow `CancellationToken`s so cancellation interrupts I/O promptly; use
  `ConfigureAwait(false)`; never `await` inside `lock`; await a `ValueTask` once.
- **Security:** secure by default, with explicit opt-outs; validate certificates and options
  before the handshake; never log or over-cache credentials.
- **Tracing:** guard `NetEventSource` calls with `IsEnabled()` and pass `this` for correlation.

## Testing

| Server | Use for |
|--------|---------|
| `LoopbackServer` | HTTP/1.1 with raw request/response control |
| `Http2LoopbackServer` | HTTP/2 frame-level tests |
| `Http3LoopbackServer` | HTTP/3 and QUIC tests |
| `LoopbackProxyServer` | Proxy behavior |

Gate platform- or feature-specific tests with `[ConditionalFact]`/`[ConditionalClass]` and
`PlatformDetection`, and cite an issue in `[ActiveIssue]`. Dispose every socket, stream, and
server; long-running loops belong in `tests/StressTests/`. Shared test helpers are under
[`Common/tests/System/Net`](/src/libraries/Common/tests/System/Net/). See
[testing libraries](/docs/workflow/testing/libraries/testing.md) for commands.
