---
applyTo: "src/libraries/System.IO.Compression*/**,src/libraries/System.IO.Packaging/**,src/native/libs/System.IO.Compression.Native/**,src/native/external/zlib-ng/**,src/native/external/zstd/**,src/native/external/brotli/**,src/native/external/zlib-ng-version.txt,src/native/external/zstd-version.txt,src/native/external/brotli-version.txt"
---

# System.IO.Compression — Folder-Specific Guidance

## Format Specification Correctness (D12)

- ZIP64 extensions must be used for files over 4GB — extra field sizes, offsets, and header values must use 64-bit fields when the 32-bit range is exceeded
- Compression levels must align with native library semantics — verify enum-to-native mapping is correct
- New compression format support (e.g., zstd in ZIP) must include a feature switch for trimming/AOT and an explicit opt-in mechanism
- Decompression must handle concatenated payloads and partial reads — rewind `Position` on lookahead probes when `CanSeek` is true and throw `InvalidDataException` on truncated headers under strict validation when `!CanSeek`
- Span-based `OperationStatus` codecs must treat multi-byte headers/trailers atomically: return `OperationStatus.NeedMoreData` with `bytesConsumed = 0` when fewer bytes than the header size are supplied so partial header slices never desynchronize decoder state
- In `ZipArchiveMode.Update`, mark existing entries dirty lazily on first actual mutation (not in `entry.Open()`), keep central-directory metadata readable while untouched, and run cross-property save-time consistency checks before writing output so invalid state throws without corrupting the archive stream
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

## Vendored Native Libraries (Upstream First)

- Deflate/ZLib/GZip use [zlib-ng](https://github.com/zlib-ng/zlib-ng) (default branch `develop`), Zstandard uses [zstd](https://github.com/meta/zstd) (`facebook/zstd` redirects there), and Brotli uses [brotli](https://github.com/google/brotli). Each is vendored under `src/native/external/<lib>/`; `<lib>-version.txt` records the version, commit, and local patches.
- For a compression behavior bug, search upstream before writing a local fix: issues and PRs by symptom, and release notes or changelogs after the vendored version, not just commits to the file you suspect. With `gh`, put the repository in `--repo` (not inside the quoted query) and project fields instead of reading whole conversations, e.g. `gh search issues "<symptom>" --repo <owner>/<repo> --json number,title,url`; trim bodies or drop bot comments with `--jq`.
- Prefer an exact upstream backport, noted in `<lib>-version.txt` with the upstream PR, the commit that landed on the upstream default branch (not the PR head), and the first release containing it. A version update requires the security consultation in step 1 of [`src/native/external/README.md`](../../src/native/external/README.md), so recommend it rather than doing it unasked.
- Builds using the system library (`CLR_CMAKE_USE_SYSTEM_ZLIB`/`BROTLI`/`ZSTD`) don't get local patches, so report fixes that aren't upstream to upstream; publishing still follows the root authorization rules.
- Vendored headers are the API contract for the shipped version; find their actual names in the vendored tree (some, like zlib-ng's, are generated from `*.h.in`) rather than relying on other versions' manuals.
- Before designing black-box behavioral or compression-ratio tests for codec options (such as hash/chain log bounds or window sizes), read the vendored C implementation's parameter clamping, strategy selection, and small-input fast paths so test inputs actually exercise the native state under test.
- Stateful reuse tests (reset, flush, partial input): verify native strategy/compressor selection across quality levels (including level 0 stored and level 1 fast paths), cover finished and unfinished prior operations with enough prior input to reach the state under test, and assert byte equality with a fresh instance plus a correct round-trip decode.

## Native Interop

- Use `LibraryImport` (source-generated) for new P/Invoke declarations
- SafeHandle-derived types must be used for native compression handles — never store raw IntPtr
- Native error codes must be mapped to appropriate .NET exceptions with the native error code preserved

## Error Handling (D9)

- Exceptions must be the most specific applicable type — `InvalidDataException` for corrupt archives, `IOException` for I/O failures, with actionable context (entry name, expected vs actual values)
- Operations on streams that may not support Length/Seek must be guarded appropriately

## Interoperability Testing (D10)

- Construct archive headers, records, and malformed-frame test inputs programmatically in C# (using explicit field/byte builders or sparse/virtual streams for 4 GiB+ offsets); never run external CLI tools to embed binary files or opaque `Convert.FromBase64String(...)` archive blobs in test `.cs` source
- When testing interoperability against real external-tool archives, reuse existing fixtures from the area's `*TestData` package (`dotnet/runtime-assets`) rather than committing new binary or Base64 blobs
- Cover edge cases: empty archives, seekable vs non-seekable streams, many small entries, entries at size boundaries (4GB, `uint.MaxValue`), and `ZipArchiveMode.Update` untouched vs modified entry sequences
- Dispose behavior must be tested — verify resources are released and post-disposal operations throw
