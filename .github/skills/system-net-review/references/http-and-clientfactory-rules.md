# HTTP & HttpClientFactory Rules

Checks for `System.Net.Http*`, `Microsoft.Extensions.Http*`, `HttpListener`, `System.Net.Requests`, `System.Net.ServerSentEvents`, and `System.Net.WebSockets.Client` HTTP plumbing.

---

## Headers & Wire Safety

| Check | What to look for |
|-------|-----------------|
| **Validate header values at every entry point** | New header paths (`TryAddWithoutValidation`, `NonValidated`, platform handlers, `Mail`/`Uri` builders) that skip CR/LF/NUL rejection, replace typed collection validation (`HttpRequestHeaders.GetHeaderDescriptor` / `Add`) with manual newline checks + `TryAddWithoutValidation` (silently accepting content-only headers such as `Content-Length` onto request headers), use `string.IsNullOrWhiteSpace` where OWS rules differ, parse or store the same value twice, or index sparse enums/known-header tables as dense arrays. Unvalidated newlines are request/response splitting and bypassed header-category checks violate the typed collection contract. Reuse `HttpRuleParser`/`HeaderDescriptor` checks per header value, keep validated and raw storage consistent, and look up `KnownHeaders` instead of re-parsing. (`#116335`, `#67833`, `#116634`) |
| **Invalidate content headers when the body changes** | Wrapping content in compression/decompression or JSON serialization while copying `Content-Length`, `Content-MD5`, or `Content-Encoding` from the original; sharing a mutable options object (compression level, serializer options) between `HttpContent` instances; casting header value collections to incompatible types. Recompute or remove length/hash headers, clone mutable options, and avoid wrapper allocations when defaults apply. (`#79386`, `#79281`, `#71757`) |

---

## Connection Pools & Protocol State

| Check | What to look for |
|-------|-----------------|
| **Pool accounting survives races and failures** | `HttpConnectionPool` code that increments/decrements stream or connection counts on only some paths, lets `TryReserveStream` counts drift, returns a connection to the pool after a failed or partially read response, or disposes the pool while waiters exist. Pair every reserve with a release in `finally`, re-check pool state after acquiring the lock, and track settings like `SETTINGS_MAX_HEADER_LIST_SIZE` as the minimum seen. (`#114083`, `#66416`, `#101531`) |
| **HTTP/2 GOAWAY, resets, and flow control** | Retrying a request after `GOAWAY`/`RST_STREAM` when the body already started streaming, window updates that can starve or double-credit a stream, or unbounded queues of `SETTINGS`/`PING` ACKs. Retry only when the server provably did not process the request (and the body is replayable), keep window arithmetic in one place, and bound control-frame responses. (`#123827`, `#70995`, `#97881`) |
| **HTTP/3 errors and QPACK/HPACK tables** | `Http3RequestStream` mapping QUIC aborts to the wrong `HttpRequestError`/`HttpProtocolException`, treating retryable stream refusals as user-visible failures, differing error handling between the first and later reads, or HPACK/QPACK dynamic table sizing bugs that only appear once the table fills. Map transport errors to `ConnectionError`, preserve the QUIC error code in the exception chain, and test table eviction boundaries. (`#78862`, `#116851`, `#88974`) |
| **HTTP/1.1 framing is exact** | Chunked parsing that mishandles CRLF split across reads, chunk extensions, or trailers; `Content-Length` mismatches; status-line parsing with unchecked indexes; or reusing a connection whose request body threw or whose response was not fully drained. Treat framing errors as fatal for the connection, use unsigned index checks on spans, and close rather than pool a connection in an unknown state. (`#77030`, `#105647`, `#70474`) |
| **Non-pooled active connections, monotonic pool limits, and dual-layer metrics** | Skipping active-connection tracking when `PooledConnectionIdleTimeout == TimeSpan.Zero` (breaking HTTP/2 keep-alive ping timers, connection metrics, or graceful shutdown while the request is still open), evaluating shutdown disposal before `MarkConnectionAsIdle`, overwriting a pool's memorized lower HTTP/2 stream concurrency bound (`_lastSeenHttp2MaxConcurrentStreams`) back up to the default when a later connection advertises `>= initialCap` (raise only that connection's active limit; update the shared pool bound only when `< initialCap`), missing `[UnsupportedOSPlatform]` attributes on compression wrappers (`BrotliStream`/`ZstandardStream`), or missing sibling instruments across telemetry layers (`SocketsHttpHandlerMetrics` AND `MetricsHandler` counters), stripping standard OpenTelemetry tags (such as `network.peer.address`) when switching to `Observable*`, or emitting unaggregated duplicate tag tuples across pools. Keep active connections tracked until disposal, preserve pool-bound monotonicity, verify all-TFM `src` builds without `-f`, audit every sibling `Meter` instrument, and aggregate observable measurements by distinct tag tuple. |

---

## Proxies, Redirects & Platform Handlers

| Check | What to look for |
|-------|-----------------|
| **Do not leak credentials across proxies or redirects** | `Proxy-Authorization`/`Authorization` headers or `NetworkCredential`s forwarded when failing over to the next proxy, following a cross-origin redirect, or sending through a tunnel; proxy URIs built without the default-port rule; auth handling added for Basic/Negotiate but not Digest. Strip auth headers when the target or proxy changes, apply fixes to every auth scheme, and surface CONNECT/SOCKS failures with a clear error. (`#117540`, `#123328`, `#113728`) |
| **Platform handlers keep SocketsHttpHandler semantics** | `BrowserHttpHandler`, `WasiHttpHandler`, `WinHttpHandler`, or Android/Apple native handlers that forget `response.RequestMessage`, do not propagate cancellation into JS promises/native calls, diverge on header encoding, or special-case one platform without reason. Match the managed handler's observable behavior, wrap native/JS operations so cancellation and disposal propagate, and document intentional differences. (`#107162`, `#91699`, `#116707`) |
| **IHttpClientFactory handler lifetime and logging** | `DefaultHttpClientFactory` changes where active/expired handler entries are not disposable, cleanup timers race with handler creation, `Lazy` initialization captures stale configuration, or logging handlers allocate (`Uri.ToString`, redacted query strings) or log twice on exceptions. Make tracking entries `IDisposable` and dispose them on factory shutdown, guard timer callbacks against disposal, and keep logging allocation-free unless the level is enabled. (`#115826`, `#103769`, `#88706`) |

---

## Server-Sent Events

| Check | What to look for |
|-------|-----------------|
| **SSE parsing is bounded and line-exact** | `SseParser` buffers that grow without a maximum line or event size, recognize only `\n` instead of `\r`, `\n`, and `\r\n`, mis-size the line buffer for the field value, or decode UTF-8 across buffer boundaries incorrectly. Enforce documented limits, handle all three line terminators, and size buffers from the actual field length. (`#109832`, `#132930`, `#128558`) |
