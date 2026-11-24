# C# Streams and Memory Rules

Checks for `.cs` changes in System.IO.Compression, System.Formats.Tar, and System.IO.Packaging covering stream contracts, lifetime, allocation, and untrusted-input arithmetic.

---

## Stream Contract

| Check | What to look for |
|-------|-----------------|
| **Honor partial reads and EOF** | `Stream.Read` may return fewer bytes than requested; only `0` means end of stream. Code that assumes one `Read` fills a header buffer, treats a short read as EOF, or loops by hand around a fixed `BufferSize` corrupts parsing on network or wrapped streams. Use `ReadExactly`/`ReadAtLeast(throwOnEndOfStream: false)`, handle `0`-byte reads, and pass zero-length buffers through without misreporting EOF (`#70574`, `#67883`, `#122093`). |
| **Respect CanSeek and substream positions** | Calling `Position`/`Length`/`Seek` on an unseekable archive throws, assuming the archive starts at offset 0 breaks embedded archives, and a substream that clamps or ignores out-of-range `Seek`/`Position` hides misuse. Branch on `CanSeek` (forward-only readers must never seek back), record the starting offset, honor `Seek` return values, throw on seeks before the start or past the end, and use `CopyTo` instead of ad-hoc intermediate buffers (`#126646`, `#105007`, `#128750`). |
| **Dispose what you own and honor leaveOpen** | Overwriting a stream field without disposing, wrappers that do not dispose their inner substream, finalizers calling async dispose, and `GC.SuppressFinalize` on sealed types without finalizers leak handles or double-free. Use `using`/`await using` (test `IAsyncDisposable` in async tests), dispose inner streams exactly once respecting `leaveOpen`, and keep `SafeHandle.ReleaseHandle` free of managed-object calls (`#125283`, `#70574`, `#119575`). |
| **Keep sync and async paths identical** | Sync and async copies drift: one validates or writes a checksum the other does not, async methods call sync `Read`, or async entry points omit `FileOptions.Asynchronous`. Share one core with a `bool isAsync` parameter or a single helper, mirror every validation and exception, and add missing async overloads with the sync API (`#129047`, `#122093`, `#123991`). |

---

## Allocation and Encoding

| Check | What to look for |
|-------|-----------------|
| **Avoid needless allocations** | `ToArray()`, eager `string` decode-then-re-encode, `bytes[0..n]` range copies, empty collections allocated up front, `Memory.Span[i]` in loops, and `new byte[]` per call add GC pressure in per-entry hot paths. Use `Span` slicing, `stackalloc` for small fixed sizes (e.g. a 512-byte record), `ArrayPool` for larger or async buffers with `try/finally` return, lazily allocated collections, and existing BCL span APIs (`#70574`, `#114421`, `#67883`). |
| **Use exact encoding and ordinal comparisons** | `Encoding.Equals` depends on ctor arguments (BOM, throw-on-invalid), culture-sensitive comparisons vary by locale, and intermediate strings created just to parse numbers waste allocations. Compare `CodePage` or exact instances, use `StringComparison.Ordinal`/`OrdinalIgnoreCase` for names and extensions per spec, and parse ASCII/UTF-8 bytes directly (`#59442`, `#72078`, `#71038`). |

---

## Untrusted Input Arithmetic and Safety

| Check | What to look for |
|-------|-----------------|
| **Guard integer overflow and >2 GiB truncation** | `offset + length` from headers can overflow and pass a bounds check; `long` sizes cast to `int`, `uint` on 32-bit, or values above `Array.MaxLength` truncate silently and corrupt or crash. Use `checked` arithmetic or subtraction-form checks (`x > max - y`), keep sizes as `long` when the format allows large inputs, and throw explicitly before narrowing (`#125283`, `#70574`, `#119575`). |
| **Use the unsigned bounds idiom correctly** | Separate `0 <= i && i < len` checks are slower, and wrong comparisons (`<` vs. `<=` for a completely filled buffer, unchecked negative `maxBytes`) cause `IndexOutOfRangeException` or reject valid data. Use `(uint)i < (uint)len`, and verify off-by-one boundaries against the buffer contract (`#67883`, `#119457`, `#69107`). |
| **Copy caller data before validate-then-use** | Validating caller-supplied arrays or spans and then reading them again lets a concurrent caller change lengths or contents between check and use (TOCTOU), causing native access violations. Defensively copy caller arrays/spans first, validate the copy (including negative values), and use only the copy; never make uncleared copies of secrets such as passwords (`#119575`, `#122093`). |
| **Cap work on untrusted archives** | Malformed archives can declare huge sizes, loop forever over cyclic offsets, or force O(n²) scans and buffers sized to the declared file length. Bound buffer sizes, iteration counts, and decompressed output, guarantee forward progress when parsing, avoid per-entry rescans or repeated lookups (cache them), and expose maximum-size limits for decoders (`#114023`, `#135357`, `#102704`). |
