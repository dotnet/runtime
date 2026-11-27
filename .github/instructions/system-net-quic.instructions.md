---
applyTo: "src/libraries/System.Net.Quic/**"
---

# System.Net.Quic — Folder-Specific Guidance

## MsQuic Interop

- All MsQuic API calls go through the MsQuicApi function table — never call native exports directly
- MsQuic callbacks execute on MsQuic worker threads — minimize work in callbacks and avoid blocking
- Native callback delegates must be prevented from being garbage collected (pin or store a reference) for the lifetime of the registration
- MsQuic configuration handles are reference-counted — pair every open with a close and verify no leaks
- Check MsQuic version compatibility at startup — API surface differs between MsQuic releases

## QuicConnection Lifecycle

- Connection state transitions (connecting → connected → shutting down → closed) must be guarded with proper synchronization
- Connection close must first shut down all streams, then close the connection handle — skipping stream cleanup causes native assertions
- Handle connection migration events correctly — the remote address may change mid-connection
- Graceful shutdown requires sending CONNECTION_CLOSE frames — abortive close should be the fallback, not the default

## QuicStream Lifecycle

- Streams are unidirectional or bidirectional — enforce directionality at the API level (no reads on send-only, no writes on receive-only)
- Stream disposal must abort the stream if it has not been gracefully closed — leaking open streams blocks connection shutdown
- Handle QUIC flow control (MAX_STREAM_DATA, MAX_STREAMS) correctly — exceeding limits is a protocol violation
- Reading from a stream may return 0 bytes when the peer has sent FIN — distinguish FIN from flow control blocking

## Error Handling

- Map MsQuic status codes (QUIC_STATUS_*) to appropriate .NET exceptions (QuicException with transport error codes)
- Connection errors must propagate to all open streams — a single stream should not silently orphan after connection failure
- Application error codes (set via stream abort) must round-trip correctly between peers

## Async Patterns

- Use ValueTask for stream read/write operations — they frequently complete synchronously when data is already buffered
- Completion sources for MsQuic callbacks must use RunContinuationsAsynchronously to avoid executing user code on the MsQuic thread
- Cancellation must trigger QUIC stream or connection abort with an appropriate error code — not just abandon the Task

## Performance

- Avoid per-operation allocations on the stream read/write path — pool buffers and reuse completion sources
- QuicStream send and receive buffers should be sized to match QUIC packet sizes to minimize fragmentation
- Minimize managed-to-native transitions in the data transfer hot path by batching operations where possible

## Testing

- QUIC tests require MsQuic to be available — use ConditionalFact/ConditionalTheory with appropriate platform checks
- Test stream multiplexing under concurrency — verify that multiple streams on a single connection do not interfere
- Verify behavior when the peer resets a stream or closes the connection unexpectedly
