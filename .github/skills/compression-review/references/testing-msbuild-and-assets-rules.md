# Testing, MSBuild, and Test Asset Rules

Checks for changes under `**/tests/**`, `.csproj`/`.props`/`.targets` files, and `ref/**` in System.IO.Compression, System.Formats.Tar, and System.IO.Packaging.

---

## Test Inputs and Oracles

| Check | What to look for |
|-------|-----------------|
| **Build test inputs in code first** | Prefer inputs in this order: (1) generate them in code with `TarWriter`/`ZipArchive`/small builders, `new string('a', n)`, and virtual, sparse, or length-faking streams for >4 GiB offsets and Zip64 instead of real multi-GB files; (2) a tiny, readable inline byte slice with a field-by-field comment; (3) a shared asset package. Never commit opaque binary or Base64 archives to the repo; they are unreviewable and hide what is being tested. Clean up temp files and links (`#87534`, `#70869`, `#103152`). |
| **Reuse runtime-assets TestData packages** | Real-world or tool-produced archives belong in the `System.Formats.Tar.TestData`, `System.IO.Compression.TestData`, or `System.IO.Packaging.TestData` packages from dotnet/runtime-assets, versioned in `eng/Version.Details.props`. Check whether an existing asset already covers the case before adding one; if a new archive is truly needed, flag that a runtime-assets PR plus a version bump is required and document the asset's contents in the test (`#105007`, `#125283`, `#67332`). |
| **Use independent spec-derived oracles** | Expected bytes produced by the same helper, writer, or algorithm under test only prove the implementation agrees with itself, and a test that skips the buggy validation path passes with or without the fix. Derive expected wire bytes, offsets, and checksums from the format spec or a frozen artifact produced by another tool, and confirm the test fails without the fix (`#59442`, `#107815`, `#60973`). |
| **Never weaken existing regression assertions** | Turning `Assert.Equal` into `>=`, normalizing data so a comparison passes, deleting cases, or rewriting old tests in a "mostly compatible" change hides regressions. Keep existing assertions intact, add new cases alongside, and justify any changed expectation explicitly (`#71038`, `#119459`, `#115211`). |

---

## Coverage and Assertions

| Check | What to look for |
|-------|-----------------|
| **Cover boundaries, malformed input, and stream shapes** | Fixes tested only on one happy path or single direction miss empty/zero/max-length values, `Seek(>EOF)` + 0-byte `Write`/`Flush`, invalid enum or format inputs, holes in sparse data, unseekable and partial-read streams, compressed vs. uncompressed archives, each entry format, positive parsed-output checks on valid inputs, and sync vs. async. Add an exact repro of the bug and a matrix over format × seekability × state transitions × async, plus checks that async paths never call sync APIs (`#123407`, `#97898`, `#77151`). |
| **Assert the full payload, not localized text** | Tests that only check "no exception", check one of N entries, compare unsliced rented buffers, or match localized message text are weak or flaky. Assert returned values, data streams, timestamps, and every entry; use `Assert.InRange`/`Assert.Throws<T>` with `paramName`; never assert on message strings; catch only expected exceptions in fuzzers; name tests for the scenario they cover (`#88231`, `#70325`, `#121917`). |
| **Use Theory and shared helpers over copy-paste** | Many near-identical `[Fact]`s, or one monolithic test looping over cases, make failures hard to diagnose and drift apart. Use `[Theory]` with `MemberData` (including existing shared data such as boolean sources), split seekable/unseekable into separate cases, use `[Fact]` when there is only one input, and move duplicated setup into the existing test base (`#76707`, `#103153`, `#70325`). |
| **Gate tests precisely** | Blanket `PlatformSpecific`/`ActiveIssue` skips, `OuterLoop` used as a privilege check, early `return` instead of a skip, and process-wide `AppContext` switches set in-process hide coverage or poison other tests. Use `[ConditionalFact]`/`[ConditionalTheory]` with capability checks (symlink creation, elevation), `RemoteExecutor` for process-wide state, and link a tracking issue on every `ActiveIssue` (`#89102`, `#75671`, `#130342`). |

---

## MSBuild, Ref, and Packaging

| Check | What to look for |
|-------|-----------------|
| **Keep project files and ref source in sync** | New test files not added to the `.csproj` never run; unused `Compile` items, stale resx entries, wrong OS/TFM conditions (including placing `.NETCoreApp`-only APIs in shared test files instead of `*.Net.cs` in multi-targeted `$(NetFrameworkCurrent)` / `netstandard*` test projects), or `#if NET` vs. multi-targeting mistakes break other builds. Build multi-TFM `src/` and `tests/` projects without `-f` before finishing; every ref change must match the approved API exactly (types, nullability, enum underlying type) and add no unapproved surface (`#123407`, `#122045`, `#120041`). |
| **Respect native, trim, and shared-framework packaging** | Changes to `UseSystemZlib`/brotli/zstd switches, `liveBuilds.targets`, NativeAOT targets, or `NetCoreAppLibrary.props` affect every runtime flavor, distro source-build, and WASM/mobile. Keep NativeAOT and mobile defaults consistent, do not ship dev-time static libs in the shared framework, and run the packaging build locally since PR validation may not cover it (`#136257`, `#104454`, `#68337`). |
