---
applyTo: "src/tests/**,**/tests/**"
---

# Tests

Conventions for test changes. Also apply `conventions` plus the language file for the code under
test (`csharp` or `native`). Note that test conventions differ across areas: libraries tests
(`src/libraries/**/tests/`) typically use xUnit with `[Fact]`/`[Theory]` and `Assert.*`;
JIT/runtime tests (`src/tests/`) often use a `return 100` success convention and have different
naming/priority requirements. Apply area-specific rules when they conflict with general guidance
below.

## Testing

- **Always add regression tests for bug fixes and behavior changes.** Prefer adding `[InlineData]` test cases to existing test files rather than creating new ones, and ensure any new test file is included in the `.csproj`. Before editing, inspect `<TargetFrameworks>` and conditional `<Compile>` groups (`*.Net.cs`, `*.Unix.cs`, `*.Windows.cs`) in the test `.csproj` so TFM- or OS-specific APIs stay inside the matching file or `#if` guard; verify multi-TFM test projects build cleanly without `-f` on all configured targets.
- **Use platform-specific test attributes correctly.** Use `[PlatformSpecific]`, `[ConditionalFact]`, or `[ActiveIssue]` for skip logic rather than runtime if-checks. `ConditionalFact` is required for `SkipTestException` to work.
- **Cover a compact 3-part behavioral matrix (repro, transitions/boundaries, positive outcomes).** Beyond the exact failure repro, test bidirectional state transitions and shared-state sequences (`true -> false` and `false -> true`; `low -> high -> subsequent instance` for pooled/remembered limits), empty/negative/boundary values (including Turkish `'i'`, surrogate pairs, zero-length/seek-past-EOF edges), and sync + async twins. Choose inputs that can't accidentally pass if output wasn't touched.
- **Test assertions must be specific and verify positive outputs.** Assert exact expected values (exact `OperationStatus`, exact byte counts, parsed property values on valid inputs — never just "did not throw" on parser/validator crash fixes). Ensure tests actually fail when the fix is reverted. Compute expected wire/header bytes from independent spec constants or formulas, not from the output under test or only from a round trip that reads a different structure. Don't weaken or rewrite existing regression assertions (such as write-count or layout contracts) to make a change pass unless the old contract is proven obsolete.
- **Build test inputs in code.** Construct them programmatically first (explicit header/record builders; lightweight virtual or sparse streams for large logical offsets instead of multi-GiB buffers). Small inline byte/ASCII slices are fine for tiny framing or malformed-header cases where each field is readable in source. Don't embed opaque binary or Base64-encoded files, such as external-tool outputs, in test source: reuse a fixture from the area's `*TestData` package from [dotnet/runtime-assets](https://github.com/dotnet/runtime-assets) (versions in `eng/Version.Details.props`). If interop genuinely needs a new external artifact, keep any generator uncommitted and note that a runtime-assets PR and `*TestData` package update are required before merge.
- **Delete flaky and low-value tests rather than patching them.** Do not add tests known to be flaky. If a test relies on fragile runtime details and cannot be made reliable, prefer deletion.
- **Make test data deterministic and culture-independent.** Create `CultureInfo` with explicit format settings. Use `[Theory]` with `[InlineData]` over individual `[Fact]` methods.
- **Use `PLACEHOLDER` for test passwords.** Avoids false positives from credential scanning tools.
- **Use checked builds for CI, lower priority for regression tests.** Use checked (not debug) CoreCLR builds for CI. New JIT regression tests should typically be `CLRTestPriority 1`.
- **Use `RemoteExecutor` for tests with process-wide shared state.** Tests that modify shared state should use `RemoteExecutor` for isolation. Avoid hardcoded paths; use temp files. Do not add heavy dependencies like `Microsoft.CodeAnalysis.CSharp` to test assemblies.
- **Catch only expected exceptions in fuzz tests.** Catching all exceptions masks bugs like undocumented exceptions escaping the API.
- **Use modern xUnit patterns for xUnit-based tests.** In xUnit test projects (for example, most libraries tests), use `Assert.*` instead of the legacy `return 100 == success` pattern, use `[Fact]`/`[Theory]`, prefer `ThrowsAnyAsync<OperationCanceledException>` for cancellation, and name regression test classes after the issue number (e.g., `Runtime_117605`). Legacy non-xUnit tests under `src/tests` may continue to use the existing `return 100` convention.
- **Reduce test output volume and keep test timers short.** Avoid megabytes of console output. Use `Thread.Sleep` with fewer iterations instead of busy loops. In loopback, keep-alive, retry, or timeout tests, configure intervals to `100–500ms` and bound `Task` awaits with `.WaitAsync(TimeSpan.FromSeconds(5))` so failing reproductions fail fast instead of hanging `100–180s` on default client or harness timeouts. When running `src/libraries/` tests, rebuild `src/<Lib>.csproj` before `/t:Test` so `testhost` picks up product changes.
- **Follow naming conventions for regression test directories.** In `src/tests/Regressions/coreclr/`, use `GitHub_<issue_number>` for the directory and `test<issue_number>` for the test name.
