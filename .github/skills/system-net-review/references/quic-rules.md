# QUIC Rules

Checks for `System.Net.Quic`, MsQuic interop, and the QUIC transport used by HTTP/3.

---

## Callback State & Lifetime

| Check | What to look for |
|-------|-----------------|
| **Free callback state only at SHUTDOWN_COMPLETE** | Freeing the `GCHandle`, context object, TLS-secret buffer, or ALPN memory passed to MsQuic from `Dispose`, a finalizer, or an earlier event. MsQuic can still deliver events (`CONNECTED`, receive, shutdown) on its own threads until `SHUTDOWN_COMPLETE`, so early release is a use-after-free or a crash on a freed handle. Keep native state rooted until the `SHUTDOWN_COMPLETE` event (or `ReleaseHandle` after it), make `Dispose` wait for or hand off to that event, and use refcounted safe handles when callbacks and `Dispose` race. (`#83001`, `#93119`, `#99357`) |
| **Guard operations against racing MsQuic events** | `QuicStream`/`QuicConnection` code that checks state (read state, capacity, start completion) and then acts while a MsQuic callback on another thread changes it, or issues stream operations before start completes. Lost wakeups and deadlocks follow. Perform state transitions under one lock or `Interlocked` exchange, re-check after acquiring the lock, and fail operations that run before `StartComplete`. (`#67329`, `#72439`, `#101250`) |
| **Native buffers passed to MsQuic are valid** | `QUIC_BUFFER` arrays built from unpinned memory, pooled `_handles` arrays assumed to be zeroed, or a 0-byte send/receive passing a null data pointer. MsQuic validates pointers even when length is 0, and a dirty rented array makes cleanup free handles from a previous use. Pin or natively allocate buffer descriptors, pass a non-null pointer (e.g. to a static dummy byte) for empty buffers, and clear rented handle arrays before use and on return. (`#68288`, `#71579`, `#72746`) |

---

## Shutdown, Errors & Versioning

| Check | What to look for |
|-------|-----------------|
| **Stream and connection shutdown is symmetric and precise** | `Dispose` that aborts one direction but gracefully closes the other, sends sentinel wire error codes (`0xffffffff`) instead of the configured default, or throws `ConnectionAborted` when the transport shut down the connection. Peers see the wrong error and tests flake. Shut down read and write sides deliberately with the configured application error codes, surface the `QuicError`/transport status from the shutdown event, and report the actual bound `LocalEndPoint`. (`#72599`, `#71969`, `#69182`) |
| **Check MsQuic version and capabilities at runtime** | New MsQuic APIs, parameters, or flags used without querying `QUIC_PARAM_GLOBAL_LIBRARY_VERSION`, IPv6/OS support not folded into `IsSupported`, or library loading that probes the application directory without opt-in. Older system MsQuic packages fail at runtime instead of reporting unsupported. Check the minimum version (and feature-specific versions) once in `MsQuicApi`, include prerequisites in `IsQuicSupported`, and keep native library probing explicit. (`#68288`, `#87208`, `#103351`) |
| **TLS 1.3 support detection is per platform** | Assuming SChannel and OpenSSL MsQuic builds share configuration, reading TLS 1.3 enablement from the registry without handling conflicting `Enabled`/`DisabledByDefault` values, or caching a provider check that can differ across MsQuic versions. Detect support per TLS provider, handle client vs server registry keys separately, and log why QUIC was disabled. (`#66019`, `#70730`, `#72262`) |
