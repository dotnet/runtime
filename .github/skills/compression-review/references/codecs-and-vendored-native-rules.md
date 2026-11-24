# Codecs and Vendored Native Review Rules

Rules for Deflate/ZLib/GZip/Brotli/Zstandard streams and encoders, `System.IO.Compression.Native`, its interop, and the vendored libraries under `src/native/external/` (zlib-ng, brotli, zstd).

---

## Managed Codec State Machines

| Check | What to look for |
|-------|-----------------|
| **Inflater and Deflater NeedsInput loops** | `Read` must loop until it produces output, reaches end of stream, or the base stream returns 0; an empty caller buffer returns 0 without touching the base stream, and `NeedsInput()` must not trigger a read from a depleted stream while output is still pending. Distinguish "finished" from "truncated" (EOF before the final block) and reject truncated input consistently across sync/async and all codecs. (`#61768`) |
| **Seekable trailer lookahead and atomic `OperationStatus` headers** | Probing for concatenated stream headers (such as a lone `0x1F` byte after a valid GZip member) without rewinding `Position` when `CanSeek` is true or throwing truncated-header `InvalidDataException` when `!CanSeek`, or consuming bytes in span-based `OperationStatus` decoders before a complete multi-byte header/trailer is present (`NeedMoreData` must report `bytesConsumed = 0` on partial headers). |
| **Native stream pointers cleared in finally** | Every assignment of `next_in`/`avail_in` (or `next_out`) to a pinned managed buffer is paired with a reset to null/0 in a `finally` in the same method, sync and async alike, in both `Inflater` and `Deflater`. A dangling pointer to an unpinned buffer is a use-after-free on the next call or on concurrent dispose; deallocate input buffer handles under the same lock that guards the native call. (`#132407`) |
| **Reset and reuse across levels and states** | `Reset()`/reuse must produce output identical to a fresh instance after both a finished and an unfinished prior operation, at every quality level: in zlib-ng, level 1 uses a different compressor (`deflate_quick`) with its own state, so test levels 1, default and max. Map `WindowLog` to zlib `windowBits` (raw/zlib/gzip offsets) in one helper, accept every native level not just `CompressionLevel` values, and make option defaults explicit instead of a silent 0. (`#123145`, `#105430`) |
| **Brotli and Zstandard streaming loops and bounds** | Drive the `OperationStatus` loop until `Done`, keep buffered input/output across calls, and don't use `bytesWritten == 0` as end-of-stream; repeated `Read` after completion must not rewind or re-decode. Bound `WindowLog`/quality to the native min/max (Zstd window max differs on 32-bit), batch context parameters, reset the native context instead of probing magic bytes in managed code, and don't do CPU-bound compression on the sync part of an async call unexpectedly. (`#129047`, `#121111`, `#119575`) |
| **Concurrent misuse must not corrupt native memory** | Streams aren't thread-safe, but concurrent `Read`/`Write`/`Dispose` on one instance may only produce wrong data or an exception, never a native crash or heap corruption: keep the active-operation flag checked (no-op, not throw, in `Dispose`), hold `SafeHandle` references across native calls, and apply the fix to every codec sharing the pattern. Custom allocators need perf evidence per platform. (`#128752`, `#119575`, `#117949`) |

---

## Vendored Libraries and Build

| Check | What to look for |
|-------|-----------------|
| **Upstream first for vendored libraries** | Before patching zlib-ng/zstd/brotli, search upstream issues, PRs and releases; prefer taking a release or an exact upstream commit. Any local delta is an exact backport recorded in `<lib>-version.txt` with the upstream PR, default-branch commit and release, keeps custom code out of the vendored directory, and updates `cgmanifest.json`. `CLR_CMAKE_USE_SYSTEM_ZLIB/BROTLI/ZSTD` (`UseSystem*`) builds link distro libraries and get no local patches, so fixes can't depend on them. (`#105433`, `#106994`, `#107166`) |
| **CMake targets for native compression** | Keep library probes and link lists in `extra_libs.cmake`/`System.IO.Compression.Native` and reuse them from NativeAOT, Mono and browser hosts; don't link static archives into targets that don't use them or add redundant `add_dependencies`. Keep SDL warnings enabled for vendored sources (patch upstream instead), list sources instead of globbing, and keep `build-native.sh` working without MSBuild. (`#102403`, `#113176`, `#104904`) |
| **Third-party licenses and notices** | Vendoring a new library (or more of one) requires `THIRD-PARTY-NOTICES.TXT` updates and preserved license headers; review non-standard licenses and avoid importing GPL-licensed scripts or fixtures into the MIT repo. Copy only the subset of upstream needed and document it in the version file. (`#124003`, `#61883`) |

---

## Interop and C PAL

| Check | What to look for |
|-------|-----------------|
| **C PAL feature probes, out params and errno** | Gate platform code on `HAVE_*` from `check_symbol_exists` (not hand-rolled compile snippets or `TARGET_*` OS lists); WASI is not Emscripten. Initialize every out parameter on all paths, use real NULL checks not `assert`, check syscall/`getpwuid_r` return codes (they return the error, so don't read or zero `errno`), and don't set `errno` for functions like `makedev`. Use `static inline` in C headers, `#undef` local macros, and don't `FUNCTIONEXPORT` internal allocator helpers. (`#69469`, `#123699`, `#69107`) |
| **LibraryImport signatures match native types** | `[LibraryImport]` parameters must match the native widths exactly (`unsigned long long` is `ulong`, not `nuint`), prefer pointers over `out` for blittable results, drop `StringMarshalling` when no strings are marshalled, and free native-returned strings explicitly. `[UnmanagedCallersOnly]` callbacks must not let exceptions escape, and `unsafe` belongs on the narrowest member that needs it. (`#67883`, `#119575`, `#136657`) |
