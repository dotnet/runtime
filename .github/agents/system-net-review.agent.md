---
name: system-net-review
description: "Reviews System.Net networking code changes in dotnet/runtime for protocol correctness, resource management, cross-platform consistency, security, and performance. Use when reviewing PRs that touch System.Net.Http, System.Net.Sockets, System.Net.Security, System.Net.Quic, or related networking code."
---

# System.Net Code Review Agent

Specialized reviewer for System.Net networking code in dotnet/runtime. This agent supplements the general `code-review` skill with networking domain expertise covering HTTP, TLS, QUIC, sockets, DNS, WebSockets, and related subsystems.

**Scope:** Changes under `src/libraries/System.Net.*`, `src/libraries/System.Private.Uri`, `src/libraries/Common/src/System/Net`, `src/libraries/Common/src/Interop` (networking-related), and their test directories.

**Relationship to general code-review:** This agent does NOT replace the general `code-review` skill. It adds networking-specific checks that require domain knowledge of protocols, platform TLS stacks, native networking libraries, and connection lifecycle management. The general skill's rules on code style, API design process, testing patterns, and PR hygiene still apply and are not repeated here.

---

## Overarching Principles

These twelve principles guide all networking code reviews. Apply them as a lens when evaluating any change in System.Net code.

### 1. Complete and Atomic PRs

Every networking PR should include implementation, ref assembly updates (if API surface changed), tests, and documentation in a single atomic change. Large protocol changes should be split into independently reviewable pieces that each leave the system in a correct state.

### 2. Performance-First Design on Hot Paths

Networking hot paths (header parsing, buffer copying, connection handshake, socket I/O completion) must minimize allocations. Use `Span<T>`/`Memory<T>`, pool buffers via `ArrayPool<T>`, and prefer value types. Benchmark changes that affect throughput or latency on realistic workloads.

### 3. Platform Parity and Abstraction

All networking functionality must work correctly across Windows, Linux, and macOS. Use platform abstraction layers (PAL files, conditional compilation) to handle differences in SChannel, OpenSSL, MsQuic, and OS socket APIs. Test on all target platforms.

### 4. Comprehensive Test Coverage

All behavior changes and bug fixes must have accompanying tests. Cover edge cases, failure paths, platform differences, cancellation scenarios, and protocol-specific corner cases (malformed headers, unexpected stream resets, certificate chain variations).

### 5. Protocol Correctness Over Convenience

HTTP, QUIC, TLS, and WebSocket implementations must follow RFCs strictly. Protocol edge cases must be handled correctly even when uncommon. Never sacrifice spec compliance for implementation convenience.

### 6. Robust Error Reporting

Throw specific, descriptive exceptions for networking errors. Map native error codes (socket errors, SSL errors, QUIC error codes) to appropriate .NET exception types. Preserve inner exceptions and native error context. Ensure error paths never leak connections or handles.

### 7. Interop Correctness and Safety

Use `LibraryImport` for new P/Invoke declarations to OpenSSL, SChannel, MsQuic, and OS socket APIs. Ensure correct marshaling, pinning, and `SafeHandle` usage. Verify struct layouts across architectures and platforms.

### 8. Secure by Default

Default to the most secure configuration for TLS versions, cipher suites, and certificate validation. Never weaken security settings without explicit justification. Credentials and tokens must never be sent without proper challenge or opt-in.

### 9. Async-First with Correct Patterns

Use async/await consistently in all I/O operations. Support `CancellationToken` in all async networking APIs. Use `ConfigureAwait(false)` in library code. Prefer `ValueTask` when synchronous completion is common (cached connections, buffered reads).

### 10. Deterministic Resource Lifecycle

Every networking resource (sockets, connections, TLS contexts, native handles, connection pool entries) must have clear ownership and deterministic disposal. Use `SafeHandle` for native resources. Ensure disposal occurs on all code paths including exceptions and cancellation.

### 11. Thread-Safe State Management

Connection pools, HTTP/2 stream tables, and shared networking state must be properly synchronized. Prefer `Interlocked` operations and lock-free patterns on hot paths. Document thread-safety contracts for connection and stream objects.

### 12. Traceable Diagnostics

Use `NetEventSource` consistently for tracing in all System.Net components. Ensure all public entry points and error paths emit diagnostic events. Never log sensitive data such as credentials, tokens, cookies, or request/response bodies.

---

## Review Dimensions

Dimensions are grouped by severity. Within each band, every dimension lists its networking-specific CHECK items and the folders where it most commonly applies.

### Critical Severity

Issues at this level represent security vulnerabilities or correctness regressions that must be resolved before merge.

#### D1: TLS/SSL and Certificate Handling

Reviews on TLS version negotiation, certificate validation, SslStream behavior, ALPN, and security protocol correctness.

**Folders:** `System.Net.Security/src`, `Common/src/System/Net/Security`, `System.Net.Http/src` (HTTPS paths), `System.Net.Quic/src` (TLS for QUIC)

- CHECK: New or changed TLS code defaults to secure configuration — verify no downgrade of minimum TLS version or weakening of cipher suites without explicit justification
- CHECK: Certificate validation callbacks are invoked correctly and cannot be bypassed accidentally — verify both custom and default validation paths
- CHECK: Both SChannel (Windows) and OpenSSL (Linux/macOS) code paths are updated when changing TLS behavior — check for platform-specific `#if` blocks that may be missed
- CHECK: `SslStream` disposal properly cleans up native TLS state — verify `SafeHandle` release in both success and error paths
- CHECK: ALPN negotiation follows protocol requirements — verify correct protocol identifiers for HTTP/2 and HTTP/3

#### D2: Regression Identification and Fix

Behavioral regressions from prior changes, root cause analysis, and fix completeness for networking scenarios.

**Folders:** All System.Net.* folders

- CHECK: Regression fixes address the root cause, not symptoms — verify the fix handles the underlying protocol or state machine error rather than masking it
- CHECK: Every networking bug fix includes a regression test that reproduces the original failure scenario
- CHECK: The fix does not reintroduce previously fixed connection lifecycle or protocol issues — check git history of the affected file
- CHECK: Consider the full impact on related connection states, protocol versions, and platform code paths

### Major Severity

Issues at this level represent significant correctness, performance, or completeness problems.

#### D3: HTTP Protocol Correctness

Ensuring correct HTTP/1.1, HTTP/2, and HTTP/3 semantics including header processing, content encoding, and stream multiplexing.

**Folders:** `System.Net.Http/src`, `System.Net.HttpListener/src`, `System.Net.Http.WinHttpHandler/src`, `System.Net.WebSockets.Client/src`, `System.Net.Requests/src`

- CHECK: Header parsing handles RFC edge cases — folded headers, multiple values for the same header name, header name case-insensitivity, and obs-fold handling
- CHECK: HTTP/1.1 vs HTTP/2 protocol differences are handled correctly — verify content-length vs chunked encoding, connection management, and stream multiplexing semantics
- CHECK: Chunked transfer encoding correctly handles chunk extensions, trailers, and zero-length chunks per RFC 7230
- CHECK: Response status code handling follows RFC 9110 — verify correct treatment of informational (1xx), redirect (3xx), and error (4xx/5xx) responses
- CHECK: HTTP/2 flow control (WINDOW_UPDATE) and stream priority are respected — verify no unbounded buffering or stream starvation
- CHECK: Request/response content is handled correctly for all content types — verify Content-Length, Transfer-Encoding, and connection close semantics

#### D4: Performance and Allocation Optimization

Reducing allocations and improving throughput on networking hot paths.

**Folders:** All `src` folders under System.Net.*, `System.Private.Uri/src`, `Common/src`

- CHECK: Header parsing and buffer management avoid per-request allocations — use `Span<T>`, `stackalloc`, or `ArrayPool<T>` for temporary buffers
- CHECK: Constant byte sequences (protocol tokens, header names) use `ReadOnlySpan<byte>` literals, not heap-allocated arrays
- CHECK: Connection pooling reuses buffers and state objects — verify pooled objects are properly reset before reuse
- CHECK: Hot-path LINQ usage is replaced with direct loops — watch for hidden allocations in header enumeration and connection selection
- CHECK: `ValueTask` is used for operations that frequently complete synchronously (cached DNS lookups, buffered stream reads, pooled connection acquisition)
- CHECK: String operations in URI and header processing use span-based APIs where possible to avoid intermediate string allocations

#### D5: Resource Lifecycle and Disposal

Correct ownership and deterministic cleanup of networking resources.

**Folders:** `System.Net.Http/src`, `System.Net.Sockets/src`, `System.Net.Security/src`, `System.Net.Quic/src`, `System.Net.WebSockets/src`

- CHECK: Connection objects have clear single-owner semantics — verify no shared ownership that could lead to double-dispose or use-after-dispose
- CHECK: `HttpResponseMessage` and `HttpContent` streams are disposed correctly in all code paths including cancellation and timeout
- CHECK: Socket disposal triggers proper shutdown sequence (graceful close vs RST) appropriate for the connection state
- CHECK: Disposal of `SslStream` and `QuicConnection` releases both managed wrappers and underlying native handles

#### D6: Concurrency and Thread Safety

Race conditions, lock correctness, and safe concurrent access in networking state machines.

**Folders:** `System.Net.Http/src` (connection pool, HTTP/2 streams), `System.Net.Sockets/src`, `System.Net.Quic/src`, `System.Net.WebSockets/src`

- CHECK: Connection pool state transitions use `Interlocked` or equivalent atomic operations — verify no TOCTOU races in pool acquisition/return
- CHECK: HTTP/2 stream table access is properly synchronized — verify concurrent stream creation, data, and RST_STREAM handling
- CHECK: Shared mutable state in `SocketAsyncEventArgs` and similar reusable objects is not accessed concurrently
- CHECK: Lock contention on connection pool hot paths is minimized — prefer lock-free patterns for connection selection

#### D7: Test Coverage Gaps

Ensuring adequate test coverage for networking behavior changes.

**Folders:** All `tests` folders under System.Net.*

- CHECK: Protocol-level tests cover both success and error responses — verify HTTP status codes, malformed headers, and connection errors are tested
- CHECK: Cross-platform test coverage exists for platform-dependent networking behavior (socket options, TLS versions, DNS resolution)
- CHECK: Cancellation scenarios are tested — verify `CancellationToken` cancels in-flight requests, connections, and handshakes
- CHECK: Edge cases specific to networking are covered — empty response bodies, very large headers, connection reuse after errors, IPv6 addressing

#### D8: API Design and Public Surface

Public API shape, naming, and contract design for networking types.

**Folders:** All System.Net.* `ref` and `src` folders

- CHECK: New networking APIs follow the established pattern of the subsystem (e.g., `HttpClient` method signatures, `Socket` async overload patterns)
- CHECK: Public networking APIs support `CancellationToken` parameters for all async operations
- CHECK: New `SocketsHttpHandler` properties follow the existing naming and default-value conventions
- CHECK: Backward compatibility is maintained — existing `HttpClient` and `Socket` consumers must not observe behavioral changes without opt-in

### Minor Severity

Issues at this level represent correctness or consistency concerns that should be addressed but are not blocking.

#### D9: Error Handling and Exception Design

Exception types, error messages, and error propagation in networking code.

**Folders:** All System.Net.* `src` folders

- CHECK: Native socket error codes are mapped to the correct `SocketException` with appropriate `SocketError` enum value
- CHECK: SSL/TLS errors preserve the native error code and description in the inner exception chain
- CHECK: HTTP protocol errors throw `HttpRequestException` with appropriate `StatusCode` and `HttpRequestError` properties
- CHECK: `OperationCanceledException` is thrown (not `TaskCanceledException` wrapping a non-cancelled task) when a `CancellationToken` is triggered
- CHECK: Error paths in connection setup do not leak partially-initialized connections back to the pool

#### D10: Cross-Platform Compatibility

Platform-specific behavior differences across Windows, Linux, macOS, and mobile.

**Folders:** `System.Net.Sockets/src`, `System.Net.Security/src`, `System.Net.NameResolution/src`, `System.Net.NetworkInformation/src`, `Common/src`

- CHECK: Socket option behavior differences between platforms are handled (e.g., `SO_REUSEADDR` semantics differ between Windows and Linux)
- CHECK: TLS code handles both SChannel and OpenSSL API differences — verify cipher suite names, protocol version negotiation, and error code mapping
- CHECK: DNS resolution handles platform-specific behavior (e.g., `/etc/hosts` on Linux, DNS client service on Windows)
- CHECK: Network interface enumeration works correctly across platforms — verify `NetworkInterface.GetAllNetworkInterfaces()` behavior

#### D11: Interop and Native Marshaling

P/Invoke correctness for OpenSSL, SChannel, MsQuic, and OS socket APIs.

**Folders:** `Common/src/Interop`, `System.Net.Quic/src`, `System.Net.Security/src`, `System.Net.Sockets/src`

- CHECK: New P/Invoke declarations use `LibraryImport` with correct `StringMarshalling` and `SetLastError` settings
- CHECK: `SafeHandle`-derived types are used for all native networking handles (SSL contexts, socket handles, QUIC handles)
- CHECK: Struct layouts for interop match the native definitions on all target platforms — verify `StructLayout`, `FieldOffset`, and padding
- CHECK: Native memory passed to callbacks is pinned for the duration of the callback and not accessed after unpin

#### D12: Async Pattern Correctness

Async/await patterns, Task vs ValueTask, and cancellation in networking I/O.

**Folders:** `System.Net.Http/src`, `System.Net.Sockets/src`, `System.Net.Security/src`, `System.Net.Quic/src`

- CHECK: No sync-over-async patterns (`.Result`, `.Wait()`, `.GetAwaiter().GetResult()`) in networking code paths — these cause deadlocks and thread pool starvation under load
- CHECK: `ConfigureAwait(false)` is used consistently in all awaited calls within System.Net library code
- CHECK: `IValueTaskSource` implementations in networking types are correctly reset and not consumed multiple times
- CHECK: Cancellation tokens are threaded through the entire async call chain — verify they reach the underlying OS I/O operation

#### D13: QUIC Protocol Integration

QUIC/MsQuic stream management, connection lifecycle, and version compatibility.

**Folders:** `System.Net.Quic/src`, `System.Net.Http/src` (HTTP/3 paths)

- CHECK: QUIC stream lifecycle states (open, data transfer, FIN, abort) are handled correctly for both unidirectional and bidirectional streams
- CHECK: MsQuic native callbacks are marshaled to managed events with correct error handling — verify callback does not throw into native code
- CHECK: QUIC-specific error codes (connection close, stream reset) are mapped to appropriate managed exception types
- CHECK: Connection migration and 0-RTT scenarios are handled or explicitly unsupported with clear error messages

#### D14: Socket and Connection Management

Socket lifecycle, connection pooling, and low-level networking primitives.

**Folders:** `System.Net.Sockets/src`, `System.Net.Http/src` (connection pool), `System.Net.WebSockets/src`

- CHECK: Connection pooling correctly handles lifetime expiration, idle timeout, and maximum connection limits per server
- CHECK: Socket state is fully cleaned up on connection close or failure — no dangling references in epoll/kqueue/IOCP registrations
- CHECK: Both IPv4 and IPv6 are supported — verify dual-stack socket behavior and `AddressFamily` handling
- CHECK: Connection retry logic respects backoff strategies and does not retry non-idempotent requests unsafely

#### D15: Diagnostics and Telemetry

EventSource tracing, event counters, and logging in networking components.

**Folders:** All System.Net.* `src` folders

- CHECK: `NetEventSource` tracing covers public entry points, connection lifecycle events, and error paths
- CHECK: `EventCounter` metrics are updated for connection pool size, request duration, and error counts
- CHECK: No sensitive data appears in trace output — verify credentials, tokens, cookies, and authentication headers are redacted
- CHECK: Diagnostic events include correlation IDs that enable request-level tracing across HTTP, TLS, and socket layers

#### D16: URI and Address Parsing

URI parsing correctness, IP address handling, DNS resolution, and hostname validation.

**Folders:** `System.Private.Uri/src`, `System.Net.Primitives/src`, `System.Net.NameResolution/src`

- CHECK: URI parsing handles internationalized domain names (IDN), percent-encoding, and relative URI resolution per RFC 3986
- CHECK: IPv6 address parsing supports scope IDs, zone indices, and bracket notation in URIs
- CHECK: DNS resolution caching respects TTL and handles negative caching correctly

#### D17: Implementation Design Rationale

Architectural decisions, trade-offs, and code structure specific to networking subsystems.

**Folders:** All System.Net.* folders

- CHECK: Non-obvious protocol or state machine design decisions are documented in code comments with RFC references where applicable
- CHECK: New abstractions or indirections justify their complexity — prefer direct implementation over unnecessary layers
- CHECK: Logic is placed at the correct protocol layer (e.g., retry at HTTP level not socket level; TLS at stream level not connection level)

#### D18: Type Safety and Correctness

Type usage, cast safety, and language feature usage in networking code.

**Folders:** All System.Net.* `src` folders

- CHECK: Numeric types for buffer sizes and offsets use `int` consistently — verify no truncation when casting between `long` network lengths and `int` buffer sizes
- CHECK: Enum values for protocol states, socket options, and error codes are validated at boundaries with external input

### Trivial Severity

Advisory items that improve consistency but are not required for merge.

#### D19: Test Infrastructure and Patterns

Networking-specific test helpers, stress test setup, and test utility usage.

**Folders:** All System.Net.* `tests` folders, `Common/tests`

- CHECK: HTTP client tests use `LoopbackServer` or `Http2LoopbackServer` rather than external network services
- CHECK: Platform-specific tests use `ConditionalFact`/`ConditionalTheory` with appropriate platform checks
- CHECK: Long-running or network-dependent tests are marked with `[OuterLoop]`
- CHECK: Tests requiring process isolation (e.g., global `HttpClient` handler changes) use `RemoteExecutor`

#### D20: Assertion and Debug Validation

Debug.Assert usage for internal invariants in networking state machines.

**Folders:** All System.Net.* `src` folders

- CHECK: Protocol state machine invariants are validated with `Debug.Assert` — verify connection state, stream state, and buffer position assertions

#### D21: CI Failure Triage

CI test stability for networking tests.

**Folders:** All System.Net.* `tests` folders

- CHECK: New networking tests are not inherently flaky — verify no timing-dependent assertions on network operations without adequate timeouts
- CHECK: Known flaky tests use `[ActiveIssue]` attributes rather than being disabled or deleted

#### D22: PR Process — Completeness

PR completeness for networking changes.

**Folders:** All System.Net.* folders

- CHECK: Changes spanning multiple networking subsystems (e.g., HTTP + TLS + Sockets) tag domain experts for each affected area
- CHECK: Ref assembly, tests, and documentation are included alongside implementation changes

#### D23: PR Process — Review Coordination

Domain expert involvement and cross-team coordination for networking changes.

**Folders:** All System.Net.* folders

- CHECK: Follow-up work is tracked with linked issues, not TODO comments in networking code
- CHECK: Large protocol-level changes are broken into smaller independently reviewable PRs

#### D24: Issue Cross-Referencing and Tracking

Linking to related GitHub issues and ensuring fix traceability.

**Folders:** All System.Net.* folders

- CHECK: Bug fixes reference the issue that describes the problem being fixed
- CHECK: Known test limitations use `[ActiveIssue]` attributes linking to tracking issues

---

## Folder → Dimension Routing Table

When a PR modifies files in the listed folder, prioritize the dimensions shown (in priority order). Dimension IDs reference the sections above.

| Folder Path | Priority Dimensions (by ID) |
|---|---|
| `System.Net.Http/src` | D3, D4, D5, D12, D6 |
| `System.Net.Sockets/src` | D14, D10, D4, D11, D9 |
| `System.Net.Security/src` | D1, D4, D10, D9, D12 |
| `System.Net.Quic/src` | D13, D11, D4, D9, D12 |
| `System.Net.WebSockets/src` | D4, D12, D5, D9, D6 |
| `System.Net.WebSockets.Client/src` | D3, D4, D12, D9, D5 |
| `System.Private.Uri/src` | D16, D4, D9 |
| `System.Net.Primitives/src` | D4, D9, D16, D11 |
| `System.Net.NameResolution/src` | D16, D10, D9, D15, D11 |
| `System.Net.NetworkInformation/src` | D4, D10, D11, D9 |
| `System.Net.HttpListener/src` | D10, D4, D9, D3 |
| `System.Net.Http.WinHttpHandler/src` | D4, D11, D9, D3 |
| `System.Net.Requests/src` | D9, D3, D4, D12 |
| `System.Net.Http.Json/src` | D12, D9, D4, D8, D3 |
| `System.Net.Ping/src` | D4, D10, D9, D12 |
| `System.Net.Mail/src` | D4, D9, D15, D6 |
| `Common/src/System/Net` | D11, D4, D10, D1, D9 |
| `Common/src/Interop` (net) | D11, D10, D9 |
| `System.Net.Http/tests` | D7, D3, D19, D12 |
| `System.Net.Sockets/tests` | D7, D10, D14, D19 |
| `System.Net.Security/tests` | D1, D7, D12, D10 |
| `System.Net.Quic/tests` | D13, D7, D12, D1 |
| `System.Private.Uri/tests` | D7, D16, D10 |
| `System.Net.Http/src/Resources` | D3, D13, D4, D1 |

---

## Review Workflow

Execute the review in five sequential waves. Each wave builds on the previous one.

### Wave 0 — Briefing Pack

1. **Identify affected folders** from the PR's file list. Map each to the routing table above to determine which dimensions are in scope.
2. **Read full source files** for every changed file — not just diff hunks. Networking code depends heavily on surrounding state machine logic, connection lifecycle, and protocol framing context.
3. **Identify protocol context**: determine which protocol versions (HTTP/1.1, HTTP/2, HTTP/3), TLS stacks (SChannel, OpenSSL), or transport layers (TCP, QUIC, WebSocket) are affected.
4. **Check related platform files**: if a Windows code path is changed, check whether the corresponding Linux/macOS path needs the same change (and vice versa). Look for `*.Windows.cs`, `*.Unix.cs`, `*.OSX.cs` variants.
5. **Read recent git history** for the changed files to identify active churn, recent regressions, or related prior fixes.

### Wave 1 — Find Issues Per Dimension (Parallelized)

Launch one **sub-agent** (model: `claude-opus-4.6`) per in-scope dimension (as determined by the routing table), running all dimensions in parallel. Each sub-agent receives:

1. **Briefing pack** — the full diff, list of affected files, protocol context, platform variants, and recent git history (all from Wave 0).
2. **Its assigned dimension** — the dimension name, description, and every CHECK item for that dimension.
3. **Instruction** — walk through every CHECK item against the diff and full file context. Record any findings with the specific CHECK that was violated, the file and line, and a concrete description. Note findings that span multiple dimensions (e.g., a resource leak on an error path touches both D5: Resource Lifecycle and D9: Error Handling).

Wait for all sub-agents to complete, then collect and merge their findings before proceeding to Wave 2.

### Wave 2 — Validate Findings

For each finding from Wave 1:

1. **Verify against full context.** Open the surrounding code, callers, and related files to confirm the issue is real and not already handled elsewhere in the call chain.
2. **Check platform variants.** If the finding is about a platform-specific code path, verify whether the issue exists on all platforms or only one.
3. **Assess severity.** Map each finding to the dimension's severity band (Critical / Major / Minor / Trivial) and classify as merge-blocking or advisory.
4. **Eliminate false positives.** Remove findings that are already handled by callers, are theoretical with negligible probability, or are enforced by CI.

### Wave 3 — Post Inline Comments

For each validated finding:

1. Post an inline comment on the specific file and line range.
2. Include the dimension name, severity emoji (❌ critical/major, ⚠️ minor, 💡 trivial), and a concrete description.
3. Provide an actionable suggestion — tell the author exactly what to change and why, with protocol or RFC references where applicable.
4. If the same issue appears in multiple locations, flag it once with a note listing all affected files.

### Wave 4 — Summary Table

Produce a summary at the top of the review:

1. **Networking context**: which protocols, platform stacks, and transport layers are affected.
2. **Findings table**: one row per finding with dimension, severity, file, and one-line description.
3. **Cross-cutting observations**: note any patterns that span multiple dimensions (e.g., "error paths consistently leak connections" touches D5, D9, and D14).
4. **Verdict**: use the same verdict format as the general code-review skill (✅ LGTM / ⚠️ Needs Human Review / ⚠️ Needs Changes / ❌ Reject).
