# Testing

_Rules for regression tests, test attributes, determinism, and test hygiene. Part of the [code-review skill](../SKILL.md)._

- **Always add regression tests for bug fixes and behavior changes.** Prefer adding `[InlineData]` test cases to existing test files rather than creating new ones. Ensure new test files are included in the csproj.
- **Use platform-specific test attributes correctly.** Use `[PlatformSpecific]`, `[ConditionalFact]`, or `[ActiveIssue]` for skip logic rather than runtime if-checks. `ConditionalFact` is required for `SkipTestException` to work.
- **Test edge cases, error paths, and all affected types.** Include empty strings, negative values, boundary conditions, Turkish 'i', surrogate pairs. Test both true and false for boolean options. Choose inputs that can't accidentally pass if output wasn't touched.
- **Test assertions must be specific.** Assert exact expected values (exact `OperationStatus`, exact byte counts), not broad conditions. Ensure tests actually fail when the fix is reverted.
- **Delete flaky and low-value tests rather than patching them.** Do not add tests known to be flaky. If a test relies on fragile runtime details and cannot be made reliable, prefer deletion.
- **Make test data deterministic and culture-independent.** Create `CultureInfo` with explicit format settings. Use `[Theory]` with `[InlineData]` over individual `[Fact]` methods.
- **Use `PLACEHOLDER` for test passwords.** Avoids false positives from credential scanning tools.
- **Use checked builds for CI, lower priority for regression tests.** Use checked (not debug) CoreCLR builds for CI. New JIT regression tests should typically be `CLRTestPriority 1`.
- **Use `RemoteExecutor` for tests with process-wide shared state.** Tests that modify shared state should use `RemoteExecutor` for isolation. Avoid hardcoded paths; use temp files. Do not add heavy dependencies like `Microsoft.CodeAnalysis.CSharp` to test assemblies.
- **Catch only expected exceptions in fuzz tests.** Catching all exceptions masks bugs like undocumented exceptions escaping the API.
- **Use modern xUnit patterns for xUnit-based tests.** In xUnit test projects (for example, most libraries tests), use `Assert.*` instead of the legacy `return 100 == success` pattern, use `[Fact]`/`[Theory]`, prefer `ThrowsAnyAsync<OperationCanceledException>` for cancellation, and name regression test classes after the issue number (e.g., `Runtime_117605`). Legacy non-xUnit tests under `src/tests` may continue to use the existing `return 100` convention.
- **Reduce test output volume.** Avoid megabytes of console output. Use `Thread.Sleep` with fewer iterations instead of busy loops.
- **Follow naming conventions for regression test directories.** In `src/tests/Regressions/coreclr/`, use `GitHub_<issue_number>` for the directory and `test<issue_number>` for the test name.
