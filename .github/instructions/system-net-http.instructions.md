---
applyTo: "src/libraries/System.Net.Http/**,src/libraries/System.Net.Http.Json/**,src/libraries/System.Net.Http.WinHttpHandler/**"
---

# System.Net.Http — Folder-Specific Guidance

## Connection Pooling and Lifecycle

- SocketsHttpHandler pools connections per host/port/scheme — respect pool limits and idle timeouts when modifying connection management
- Connection lifetime and idle timeout logic must handle races between pool cleanup and new request acquisition
- When modifying connection pooling, ensure HTTP/1.1 and HTTP/2 paths are both updated — they have distinct pool semantics
- Connection state transitions (idle → active → draining → disposed) must be atomic and thread-safe

## HTTP Protocol Handling

- HTTP/2 multiplexes streams over a single connection — changes to stream management must not starve other streams or deadlock flow control windows
- HTTP/2 flow control (connection-level and stream-level) windows must be updated correctly; failing to send WINDOW_UPDATE causes hangs
- Header parsing must handle folded headers, multiple values for the same header name, and case-insensitive comparison per RFC 9110
- Ensure chunked transfer encoding, content-length validation, and trailer handling follow HTTP/1.1 and HTTP/2 specs exactly
- Redirect handling must not follow redirects across security boundaries (HTTPS → HTTP) unless explicitly configured

## Request/Response Lifecycle

- HttpContent must be disposed after the response is consumed — leaking content streams holds connections open in the pool
- HttpResponseMessage owns the response stream; disposing it before reading content causes ObjectDisposedException
- Request headers vs content headers have distinct allowed sets — do not allow content headers on requests without a body
- Cancellation tokens must propagate through the entire send pipeline including DNS, connect, TLS handshake, and response read

## HttpContent and Serialization

- Avoid buffering entire request/response bodies in memory — prefer streaming for large payloads
- HttpContent.ReadAsStreamAsync should return the underlying stream without copying when possible
- System.Net.Http.Json extensions must handle JsonSerializer cancellation and disposal correctly
- Serialization errors must surface as HttpRequestException or JsonException, not raw IO exceptions

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
- Never log request/response bodies, Authorization headers, cookies, or credentials in trace output
