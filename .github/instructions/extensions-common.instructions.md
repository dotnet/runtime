---
applyTo: "src/libraries/Microsoft.Extensions.Http*/**,src/libraries/Microsoft.Extensions.FileProviders*/**,src/libraries/Microsoft.Extensions.Primitives*/**"
---

# Microsoft.Extensions Common Libraries — Folder-Specific Guidance

Covers `Microsoft.Extensions.Http`, `Microsoft.Extensions.FileProviders`, and `Microsoft.Extensions.Primitives`.

## Extensions.Http (D17, D7)

- `HttpClientFactory` registration helpers must use `TryAdd` patterns to avoid overriding user-configured handlers
- `DelegatingHandler` chains must correctly propagate disposal — document ownership of inner handlers
- Connection pooling and lifetime settings (`PooledConnectionLifetime`) must be documented when composing `HttpClient` configurations
- Ensure thread-safe access for `HttpClient` instances shared via factory — use `CompareExchange` for lazy initialization
- Avoid per-operation allocations in handler pipelines — cache delegates and reuse handler instances where possible
- All HttpClientFactory types must be trim-safe and NativeAOT-compatible

## Extensions.FileProviders (D10, D4)

- `PhysicalFileProvider` must handle concurrent file operations — use `FileShare.Delete` when reading files that may be rotated
- File provider ownership is not transferred to `IConfigurationRoot` or `IConfigurationProvider` — document the ownership boundary
- `IChangeToken` implementations must correctly handle callback registration and disposal
- Polling-based file watchers must have configurable intervals and must clean up on disposal
- Avoid unnecessary `ToArray()`/`ToList()` calls — prefer lazy enumeration with yield return when callers do not need materialized collections
- Directory enumeration order changes are breaking changes — test ordering expectations

## Extensions.Primitives (D1, D5)

- `StringSegment` must provide string-equivalent operations — missing methods should maintain API parity with `string`
- `ChangeToken.OnChange` registrations must be disposed when the owning component is disposed to prevent leaks
- Implicit operators (e.g., string to `StringSegment`) must be documented and must not allocate on the conversion path
- `GetHashCode` implementations must not allocate — use span overloads rather than allocating substrings
- Avoid `Unsafe.As` casts when a standard cast is equally cheap (e.g., object to string)
- Performance-sensitive code paths must avoid closures, string allocations, and boxing

## Cross-Cutting

- All types in these libraries must be trim-safe and NativeAOT-compatible
- Public API changes must be evaluated for backward compatibility — these are foundational types with many downstream consumers
- Abstractions (interfaces, base classes) belong in `*.Abstractions` packages; implementations in concrete packages
