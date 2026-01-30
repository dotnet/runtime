# Test: System.Formats.Nrbf.Tests.EdgeCaseTests - BinaryFormatter Tests

## Test Suite
System.Formats.Nrbf.Tests

## Failure Type
exception - test discovery failure

## Exception Type
System.Reflection.CustomAttributeFormatException

## Stack Trace
```
System.InvalidOperationException : Exception during discovery:
System.Reflection.CustomAttributeFormatException: Could not load file or assembly 
'System.Runtime.Serialization.Formatters, Version=11.0.0.0, Culture=neutral, 
PublicKeyToken=b03f5f7f11d50a3a'. The system cannot find the file specified.
 ---> System.IO.FileNotFoundException: Could not load file or assembly 
'System.Runtime.Serialization.Formatters, ...
at System.Reflection.RuntimeAssembly.InternalLoad(...)
at System.Reflection.TypeNameResolver.ResolveAssembly(...)
```

## Affected Tests
- EdgeCaseTests.ArraysOfStringsCanContainMemberReferences
- EdgeCaseTests.FormatterTypeStyleOtherThanTypesAlwaysAreNotSupportedByDesign

## Notes
- Platform: Browser/WASM + CoreCLR
- Category: platform-unsupported

These tests use `FormatterTypeStyle` enum from `System.Runtime.Serialization.Formatters` assembly 
in their `[InlineData]` attributes. On Browser platform, this assembly isn't available, and xunit 
fails during test discovery when trying to parse the attribute metadata.

Runtime skip attributes (ActiveIssue, SkipOnPlatform) don't help because the failure occurs during
reflection-based attribute parsing, before any skip logic is evaluated.

**Required Fix:** These tests need to be wrapped with `#if !BROWSER` compile-time conditional or 
similar mechanism to exclude them from Browser builds entirely. This is the same approach used by 
Mono+Browser (which only runs 4 tests from this suite).

**Mono Baseline:** Tests run: 4, Passed: 4
**CoreCLR:** Tests run: 6, Passed: 4, Failed: 2

The failures are not CoreCLR-specific - they would fail on Mono+Browser too if the tests were 
included. The difference is in how the test project excludes these tests at build time for Mono.
