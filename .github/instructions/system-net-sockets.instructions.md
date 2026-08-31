---
applyTo: "src/libraries/System.Net.Sockets/**"
---

# System.Net.Sockets — Folder-Specific Guidance

## Async Socket Operations

- Prefer SocketAsyncEventArgs-based operations over Task-based wrappers on hot paths to avoid allocations
- SocketAsyncEventArgs instances must be reused — allocating a new one per operation defeats the purpose
- When an async socket operation completes synchronously (returns false), invoke the continuation inline without posting to the thread pool
- Cancellation must close the socket to unblock pending operations — there is no other reliable mechanism to cancel in-flight I/O

## Buffer Management

- Pin buffers for the duration of the native I/O operation — use GCHandle or fixed statements, not just Memory<T>
- Prefer ArrayPool<byte> for receive and send buffers; return buffers in finally blocks even on exception paths
- Multi-buffer (scatter/gather) send and receive must handle partial completions — the OS may transfer fewer bytes than requested

## Dual-Stack and Address Families

- Support dual-stack sockets (IPv6 with IPv4-mapped addresses) as the default where the OS supports it
- When creating sockets, handle AddressFamily.InterNetworkV6 with DualMode=true rather than creating separate IPv4/IPv6 sockets
- Test with both IPv4-only and IPv6-only endpoints — do not assume dual-stack availability

## Platform-Specific Behavior

- Socket option names and behaviors differ between Windows (Winsock) and Unix (POSIX) — guard platform-specific options with runtime checks
- Linux uses epoll, macOS uses kqueue, and Windows uses IO completion ports — async completion models differ per platform
- Connection reset behavior varies: Windows returns WSAECONNRESET immediately, Linux may return it on the next read
- SO_REUSEADDR semantics differ between platforms — document expected behavior clearly

## Connection and State Management

- Socket disposal must cancel all pending operations and release the native handle
- Track socket state (created → bound → listening/connected → shutdown → disposed) explicitly to prevent invalid operations
- Do not issue concurrent reads or concurrent writes on the same socket unless the protocol explicitly requires it
- LingerOption and shutdown sequences must be tested on all platforms — graceful close behavior varies

## Error Handling

- Map platform-specific error codes (WSA* on Windows, errno on Unix) to SocketException with the correct SocketError
- Distinguish between transient errors (EWOULDBLOCK, EINTR) and fatal errors (ECONNREFUSED, ENETUNREACH) in retry logic
- SocketException.SocketErrorCode must be set correctly — callers depend on it for error classification
