---
applyTo: "src/libraries/System.IO.Compression*/**"
---

# System.IO.Compression — Folder-Specific Guidance

## Format Specification Correctness (D12)

- ZIP64 extensions must be used for files over 4GB — extra field sizes, offsets, and header values must use 64-bit fields when the 32-bit range is exceeded
- Compression levels must align with native library semantics — verify enum-to-native mapping is correct
- New compression format support (e.g., zstd in ZIP) must include a feature switch for trimming/AOT and an explicit opt-in mechanism
- Decompression must handle concatenated payloads and partial reads — the decompressor must not assume a single contiguous compressed stream
- Breaking changes to format handling must be documented and include migration guidance

## Security

- Maximum decompressed size limits must be configurable to prevent zip-bomb attacks, following the existing deflate size limit pattern
- Archive extraction must validate entry paths to prevent path traversal attacks (entries with `../` segments)

## Performance & Allocation (D5)

- Use `ArrayPool<byte>` for variable-size compression/decompression buffers — return buffers in finally blocks
- Avoid allocating excessively large fixed buffers per operation (100KB+ per compression operation is expensive)
- Pin buffers for the duration of native I/O operations
- Hot paths must avoid per-operation allocations — prefer pooled buffers and cached delegates
- Closures that capture state on hot paths must be eliminated — use static lambdas with explicit state

## Async Operations

- Async compression/decompression must not perform the actual compression work synchronously before the first await
- Sync and async code paths must share non-trivial logic through common helpers to prevent divergence

## Cross-Platform Metadata (D19)

- Archive extraction must preserve or correctly translate platform-specific metadata — Unix execute permissions, symlinks, and hidden file attributes
- File path operations within archives must use forward slashes as the archive-internal separator per the ZIP specification
- Tests must verify metadata round-trip on both Windows and Unix platforms

## Native Interop

- Native library updates (brotli, zlib, zstd) must be tracked and the managed wrapper updated accordingly
- Use `LibraryImport` (source-generated) for new P/Invoke declarations
- SafeHandle-derived types must be used for native compression handles — never store raw IntPtr
- Native error codes must be mapped to appropriate .NET exceptions with the native error code preserved

## Error Handling (D9)

- Exceptions must be the most specific applicable type — `InvalidDataException` for corrupt archives, `IOException` for I/O failures, with actionable context (entry name, expected vs actual values)
- Operations on streams that may not support Length/Seek must be guarded appropriately

## Interoperability Testing (D10)

- Tests must use archive files created by external tools — not just round-trip tests with the same .NET implementation
- Test with archives from multiple platforms and compression libraries to verify cross-tool compatibility
- Cover edge cases: empty archives, many small entries, entries at size boundaries (4GB, uint.MaxValue)
- Dispose behavior must be tested — verify resources are released and post-disposal operations throw
