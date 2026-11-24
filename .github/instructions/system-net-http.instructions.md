---
applyTo: "src/libraries/System.Net.Http/**,src/libraries/System.Net.Http.Json/**,src/libraries/System.Net.Http.WinHttpHandler/**"
---

# System.Net.Http — Folder-Specific Guidance

## Connection Pooling and Lifecycle

- SocketsHttpHandler pools connections per host/port/scheme — respect pool limits and idle timeouts when modifying connection management
- Connection lifetime and idle timeout logic must handle races between pool cleanup and new request acquisition
- When modifying connection pooling, ensure HTTP/1.1, HTTP/2, and HTTP/3 paths are all evaluated — they have distinct pool and multiplex semantics
- Connection state transitions (idle → active → draining → disposed) must be atomic and thread-safe; transition connections (`MarkConnectionAsIdle`) before evaluating shutdown/disposal checks
- Non-pooled active connections (for example when `PooledConnectionIdleTimeout == TimeSpan.Zero` or a connection is excluded from idle pooling) must remain tracked in active connection state while open so HTTP/2 keep-alive ping timers, connection metrics, and graceful shutdown continue operating until request completion and disposal
- Separate per-connection dynamic limits from pool-level remembered bounds: when a pool caches a conservative peer cap across connections (such as HTTP/2 initial stream concurrency), a higher peer advertisement may raise that connection's active limit without erasing a stricter lower bound already memorized for new connections (update the shared pool bound only when below the configured initial cap).

## HTTP Protocol Handling

- [Verified](../skills/build-and-test/SKILL.md#reporting-validation) specs at `rfc-editor.org/info/rfcN`: RFC 9110 semantics, 9111 caching, 9112 HTTP/1.1, 9113 HTTP/2, 9114 HTTP/3 (QUIC transport: see System.Net.Quic guidance)
- HTTP/2 multiplexes streams over a single connection — changes to stream management must not starve other streams or deadlock flow control windows
- HTTP/2 flow control (connection-level and stream-level) windows must be updated correctly; failing to send WINDOW_UPDATE causes hangs
- Header parsing must handle folded headers, multiple values for the same header name, and case-insensitive comparison per RFC 9110
- Ensure chunked transfer encoding, content-length validation, and trailer handling follow HTTP/1.1 and HTTP/2 specs exactly
- Redirect handling must not follow redirects across security boundaries (HTTPS → HTTP) unless explicitly configured

## Request/Response Lifecycle

- HttpContent must be disposed after the response is consumed — leaking content streams holds connections open in the pool
- HttpResponseMessage owns the response stream; disposing it before reading content causes ObjectDisposedException
- Request headers vs content headers have distinct allowed sets — validate injected request headers through the typed `HttpRequestHeaders` descriptor/collection path rather than bypassing content-header rejection with manual checks plus `TryAddWithoutValidation`
- Cancellation tokens must propagate through the entire send pipeline including DNS, connect, TLS handshake, and response read

## HttpContent and Serialization

- Avoid buffering entire request/response bodies in memory — prefer streaming for large payloads
- HttpContent.ReadAsStreamAsync should return the underlying stream without copying when possible
- System.Net.Http.Json extensions must handle JsonSerializer cancellation and disposal correctly
- Serialization errors must surface as HttpRequestException or JsonException, not raw IO exceptions
- Content-encoding or compression wrappers (`HttpContent` decorators) must clear or recompute `Content-Length`/`Content-MD5` when transforming the body, set `Content-Encoding` without duplicating values, and mirror `[UnsupportedOSPlatform]` attributes from wrapped streams onto both `ref/` and `src/` — always verify with an all-TFM `src` build without `-f`

## WinHttpHandler

- WinHttpHandler delegates to the Windows WinHTTP API — changes must verify behavior on all supported Windows versions
- Error codes from WinHTTP must be mapped to appropriate HttpRequestException with inner Win32Exception
- WinHTTP callback state must be pinned for the duration of the async operation to prevent GC collection

## Performance

- Avoid allocating strings for well-known header names and values — use the static KnownHeaders table
- Header value parsing should use Span-based APIs to avoid substring allocations
- Pool HTTP/2 frame buffers and avoid per-frame allocations on the read/write loops

## Diagnostics

- Use NetEventSource for tracing request lifecycle events (send, redirect, retry, failure)
- Metrics span both transport connection instruments (`SocketsHttpHandlerMetrics` / `ConnectionMetrics`) and request pipeline instruments (`MetricsHandler` / `DiagnosticsHandler`, including active-request counters and duration histograms); audit every sibling instrument in the `Meter` together, preserve documented OpenTelemetry tags when switching counter kinds (such as `ObservableUpDownCounter` omitting closed zero series), and aggregate observable measurements by distinct tag tuple across pools
- Never log request/response bodies, Authorization headers, cookies, or credentials in trace output
- Filter `System.Net.Http.Functional.Tests` runs with `/p:XUnitClassName=...` (never run the full assembly unfiltered) and use `100–500ms` keep-alive/timeout intervals in new tests
