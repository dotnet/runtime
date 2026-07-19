---
applyTo: "src/libraries/Microsoft.Extensions.Caching*/**"
---

# Microsoft.Extensions.Caching — Folder-Specific Guidance

## Cache Key Correctness (D20)

- Cache keys must incorporate all inputs that affect the cached result — including format versions, serialization options, and any parameters that change the output
- Key generation must be deterministic and stable across process restarts for distributed caches
- Verify that key composition does not create collisions for distinct inputs
- Cache miss must be distinguishable from a cached null value

## Eviction Policies (D20)

- Cache eviction must consider TTL, priority, estimated object size, and memory pressure — not just LRU
- Expose eviction callbacks so consumers can observe and react to entry removal
- Memory pressure-based eviction should clear stale references to avoid retaining unused objects

## Stampede Prevention (D20)

- When a cache entry expires, only one caller should recompute the value while others wait (thundering herd mitigation)
- HybridCache (available in .NET 9+) and similar patterns should use a single-flight mechanism for concurrent requests to the same key

## Performance & Allocation (D5)

- Closures for cache factory methods must not allocate on every cache access — cache the delegate or use a static lambda with explicit state
- Distributed cache serialization must handle large objects efficiently — consider compression and streaming for values over 100KB
- In-memory cache operations must avoid holding locks during expensive value computation
- Hot paths must avoid per-operation allocations — prefer pooled buffers and cached delegates

## Thread Safety (D6)

- `MemoryCache` and `HybridCache` (available in .NET 9+) are expected to be used concurrently — all access patterns must be thread-safe
- Entry creation and eviction callbacks may execute on different threads — do not assume single-threaded access

## Distributed vs In-Memory

- `IDistributedCache` implementations must handle serialization/deserialization correctly and document size limits
- Test with both in-memory and distributed backing stores — behavior differences must be accounted for
- Distributed cache key expiration semantics may differ from in-memory — document and test accordingly
