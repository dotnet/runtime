# Wasm.Build.Tests `native` Category — CoreCLR Test Run Report

- **Date**: 2026-03-28
- **Branch**: `maraf/WasmCoreCLRNativeBuild`
- **Runtime flavor**: CoreCLR
- **SDK**: `dotnet-none` (11.0.100-preview.2.26116.109, no workload)
- **Trait filter**: `-trait category=native -notrait category=workload`
- **Note**: `global.json` was temporarily patched from preview.3 → preview.2 to match the provisioned SDK.
- **Env var fix**: `RUNTIME_FLAVOR_FOR_TESTS=CoreCLR` (not `RUNTIME_FLAVOR`) is required for `AddCoreClrProjectProperties` to inject `UsingBrowserRuntimeWorkload=false` into test project csprojs.

## Summary

| Metric | Count |
|--------|-------|
| Total test cases | 311 |
| Passed | 16 |
| Failed | 295 |

## Failure Breakdown

| Failure Category | Count | Fixable? |
|-----------------|-------|----------|
| PASS | 16 | ✅ |
| XHARNESS_TOOL_MISSING | 89 | Yes — install xharness tool |
| TEMPLATE_MISSING | 38 | Yes — install workload or convert to CopyTestAsset |
| NETSDK1147_WORKLOAD_REQUIRED | 59 | Yes — propagate UsingBrowserRuntimeWorkload to referenced projects |
| PUBLISH_FAILED | 1 | Investigate — may be AOT/tooling gaps |
| BUILD_FAILED | 3 | Investigate |
| ASSERTION_FAILURE | 89 | Adapt tests for CoreCLR behavior |
| OTHER_FAILURE | 16 | Investigate |

## PASS (16 tests)

Tests that passed successfully with CoreCLR native relink.

| # | Test | Time |
|---|------|------|
| 1 | `Wasm.Build.Tests.Blazor.BuildPublishTests.DefaultTemplate_AOT_WithWorkload(config: Release, testUnicode: False)` | 13.2987496s |
| 2 | `Wasm.Build.Tests.Blazor.BuildPublishTests.DefaultTemplate_AOT_WithWorkload(config: Release, testUnicode: True)` | 13.3041154s |
| 3 | `Wasm.Build.Tests.Blazor.MiscTests.BugRegression_60479_WithRazorClassLib` | 13.4360998s |
| 4 | `Wasm.Build.Tests.DllImportTests.UnmanagedCallback_WithFunctionPointers_CompilesWithWarnings(config: Debug, aot: False)` | 9.4662701s |
| 5 | `Wasm.Build.Tests.DllImportTests.UnmanagedCallback_WithFunctionPointers_CompilesWithWarnings(config: Release, aot: False)` | 9.1669601s |
| 6 | `Wasm.Build.Tests.MemoryTests.AllocateLargeHeapThenRepeatedlyInterop` | 1.2761189s |
| 7 | `Wasm.Build.Tests.MemoryTests.AllocateLargeHeapThenRepeatedlyInterop_NoWorkload` | 1.1917093s |
| 8 | `Wasm.Build.Tests.NativeBuildTests.ZipArchiveInteropTest` | 6.8610029s |
| 9 | `Wasm.Build.Tests.PInvokeTableGeneratorTests.UnmanagedStructAndMethodIn_SameAssembly_WithDisableRuntimeMarshallingAttribute_ConsideredBlittable(config: Debug, aot: False)` | 6.6588891s |
| 10 | `Wasm.Build.Tests.PInvokeTableGeneratorTests.UnmanagedStructAndMethodIn_SameAssembly_WithDisableRuntimeMarshallingAttribute_ConsideredBlittable(config: Release, aot: False)` | 6.7334908s |
| 11 | `Wasm.Build.Tests.PInvokeTableGeneratorTests.UnmanagedStructAndMethodIn_SameAssembly_WithoutDisableRuntimeMarshallingAttribute_WithStructLayout_ConsideredBlittable(config: Debug, aot: False)` | 4.1707258s |
| 12 | `Wasm.Build.Tests.PInvokeTableGeneratorTests.UnmanagedStructAndMethodIn_SameAssembly_WithoutDisableRuntimeMarshallingAttribute_WithStructLayout_ConsideredBlittable(config: Release, aot: False)` | 4.3321497s |
| 13 | `Wasm.Build.Tests.PInvokeTableGeneratorTests.UnmanagedStructsAreConsideredBlittableFromDifferentAssembly(config: Debug, aot: False, libraryHasAttribute: False, appHasAttribute: True, expectSuccess: True)` | 6.8793959s |
| 14 | `Wasm.Build.Tests.PInvokeTableGeneratorTests.UnmanagedStructsAreConsideredBlittableFromDifferentAssembly(config: Debug, aot: False, libraryHasAttribute: True, appHasAttribute: True, expectSuccess: True)` | 6.938768s |
| 15 | `Wasm.Build.Tests.PInvokeTableGeneratorTests.UnmanagedStructsAreConsideredBlittableFromDifferentAssembly(config: Release, aot: False, libraryHasAttribute: False, appHasAttribute: True, expectSuccess: True)` | 6.9308685s |
| 16 | `Wasm.Build.Tests.PInvokeTableGeneratorTests.UnmanagedStructsAreConsideredBlittableFromDifferentAssembly(config: Release, aot: False, libraryHasAttribute: True, appHasAttribute: True, expectSuccess: True)` | 6.7890862s |

## XHARNESS_TOOL_MISSING (89 tests)

`dotnet xharness` tool is not installed in `dotnet-none`. These tests **built successfully** (native relink worked) but failed at the run step because the xharness webserver tool is missing. Installing xharness into the SDK should make most of these pass.

| # | Test | Error (truncated) |
|---|------|--------------------|
| 1 | `Wasm.Build.Tests.Blazor.BuildPublishTests.Test_WasmStripILAfterAOT(stripILAfterAOT: \"\", expectILStripping: True)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 2 | `Wasm.Build.Tests.Blazor.BuildPublishTests.Test_WasmStripILAfterAOT(stripILAfterAOT: \"false\", expectILStripping: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 3 | `Wasm.Build.Tests.Blazor.SimpleRunTests.BlazorPublishRunTest(config: Debug, aot: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 4 | `Wasm.Build.Tests.Blazor.SimpleRunTests.BlazorPublishRunTest(config: Release, aot: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 5 | `Wasm.Build.Tests.Blazor.SimpleRunTests.BlazorPublishRunTest(config: Release, aot: True)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 6 | `Wasm.Build.Tests.DllImportTests.DllImportWithFunctionPointersCompilesWithoutWarning(config: Debug, aot: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 7 | `Wasm.Build.Tests.DllImportTests.DllImportWithFunctionPointersCompilesWithoutWarning(config: Release, aot: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 8 | `Wasm.Build.Tests.DllImportTests.DllImportWithFunctionPointers_ForVariadicFunction_CompilesWithWarning(config: Debug, aot: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 9 | `Wasm.Build.Tests.DllImportTests.DllImportWithFunctionPointers_ForVariadicFunction_CompilesWithWarning(config: Release, aot: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 10 | `Wasm.Build.Tests.DllImportTests.DllImportWithFunctionPointers_WarningsAsMessages(config: Debug, aot: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 11 | `Wasm.Build.Tests.DllImportTests.DllImportWithFunctionPointers_WarningsAsMessages(config: Release, aot: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 12 | `Wasm.Build.Tests.InvariantGlobalizationTests.AOT_InvariantGlobalization(config: Debug, aot: False, invariantGlobalization: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 13 | `Wasm.Build.Tests.InvariantGlobalizationTests.AOT_InvariantGlobalization(config: Debug, aot: False, invariantGlobalization: True)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 14 | `Wasm.Build.Tests.InvariantGlobalizationTests.AOT_InvariantGlobalization(config: Debug, aot: False, invariantGlobalization: null)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 15 | `Wasm.Build.Tests.InvariantGlobalizationTests.AOT_InvariantGlobalization(config: Release, aot: False, invariantGlobalization: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 16 | `Wasm.Build.Tests.InvariantGlobalizationTests.AOT_InvariantGlobalization(config: Release, aot: False, invariantGlobalization: True)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 17 | `Wasm.Build.Tests.InvariantGlobalizationTests.AOT_InvariantGlobalization(config: Release, aot: False, invariantGlobalization: null)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 18 | `Wasm.Build.Tests.InvariantGlobalizationTests.RelinkingWithoutAOT(config: Debug, aot: False, invariantGlobalization: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 19 | `Wasm.Build.Tests.InvariantGlobalizationTests.RelinkingWithoutAOT(config: Debug, aot: False, invariantGlobalization: True)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 20 | `Wasm.Build.Tests.InvariantGlobalizationTests.RelinkingWithoutAOT(config: Debug, aot: False, invariantGlobalization: null)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 21 | `Wasm.Build.Tests.InvariantGlobalizationTests.RelinkingWithoutAOT(config: Release, aot: False, invariantGlobalization: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 22 | `Wasm.Build.Tests.InvariantGlobalizationTests.RelinkingWithoutAOT(config: Release, aot: False, invariantGlobalization: True)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 23 | `Wasm.Build.Tests.InvariantGlobalizationTests.RelinkingWithoutAOT(config: Release, aot: False, invariantGlobalization: null)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 24 | `Wasm.Build.Tests.InvariantTimezoneTests.AOT_InvariantTimezone(config: Debug, aot: False, invariantTimezone: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 25 | `Wasm.Build.Tests.InvariantTimezoneTests.AOT_InvariantTimezone(config: Debug, aot: False, invariantTimezone: True)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 26 | `Wasm.Build.Tests.InvariantTimezoneTests.AOT_InvariantTimezone(config: Debug, aot: False, invariantTimezone: null)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 27 | `Wasm.Build.Tests.InvariantTimezoneTests.AOT_InvariantTimezone(config: Debug, aot: True, invariantTimezone: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 28 | `Wasm.Build.Tests.InvariantTimezoneTests.AOT_InvariantTimezone(config: Debug, aot: True, invariantTimezone: True)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 29 | `Wasm.Build.Tests.InvariantTimezoneTests.AOT_InvariantTimezone(config: Debug, aot: True, invariantTimezone: null)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 30 | `Wasm.Build.Tests.InvariantTimezoneTests.AOT_InvariantTimezone(config: Release, aot: False, invariantTimezone: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 31 | `Wasm.Build.Tests.InvariantTimezoneTests.AOT_InvariantTimezone(config: Release, aot: False, invariantTimezone: True)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 32 | `Wasm.Build.Tests.InvariantTimezoneTests.AOT_InvariantTimezone(config: Release, aot: False, invariantTimezone: null)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 33 | `Wasm.Build.Tests.InvariantTimezoneTests.AOT_InvariantTimezone(config: Release, aot: True, invariantTimezone: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 34 | `Wasm.Build.Tests.InvariantTimezoneTests.AOT_InvariantTimezone(config: Release, aot: True, invariantTimezone: True)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 35 | `Wasm.Build.Tests.InvariantTimezoneTests.AOT_InvariantTimezone(config: Release, aot: True, invariantTimezone: null)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 36 | `Wasm.Build.Tests.InvariantTimezoneTests.RelinkingWithoutAOT(config: Debug, aot: False, invariantTimezone: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 37 | `Wasm.Build.Tests.InvariantTimezoneTests.RelinkingWithoutAOT(config: Debug, aot: False, invariantTimezone: True)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 38 | `Wasm.Build.Tests.InvariantTimezoneTests.RelinkingWithoutAOT(config: Debug, aot: False, invariantTimezone: null)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 39 | `Wasm.Build.Tests.InvariantTimezoneTests.RelinkingWithoutAOT(config: Release, aot: False, invariantTimezone: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 40 | `Wasm.Build.Tests.InvariantTimezoneTests.RelinkingWithoutAOT(config: Release, aot: False, invariantTimezone: True)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 41 | `Wasm.Build.Tests.InvariantTimezoneTests.RelinkingWithoutAOT(config: Release, aot: False, invariantTimezone: null)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 42 | `Wasm.Build.Tests.MainWithArgsTests.AsyncMainWithArgs(config: Debug, aot: False, args: [\"abc\", \"foobar\"])` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 43 | `Wasm.Build.Tests.MainWithArgsTests.AsyncMainWithArgs(config: Debug, aot: False, args: [])` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 44 | `Wasm.Build.Tests.MainWithArgsTests.AsyncMainWithArgs(config: Release, aot: False, args: [\"abc\", \"foobar\"])` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 45 | `Wasm.Build.Tests.MainWithArgsTests.AsyncMainWithArgs(config: Release, aot: False, args: [])` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 46 | `Wasm.Build.Tests.MainWithArgsTests.NonAsyncMainWithArgs(config: Debug, aot: False, args: [\"abc\", \"foobar\"])` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 47 | `Wasm.Build.Tests.MainWithArgsTests.NonAsyncMainWithArgs(config: Debug, aot: False, args: [])` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 48 | `Wasm.Build.Tests.MainWithArgsTests.NonAsyncMainWithArgs(config: Release, aot: False, args: [\"abc\", \"foobar\"])` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 49 | `Wasm.Build.Tests.MainWithArgsTests.NonAsyncMainWithArgs(config: Release, aot: False, args: [])` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 50 | `Wasm.Build.Tests.NativeLibraryTests.ProjectUsingBrowserNativeCrypto(config: Debug, aot: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 51 | `Wasm.Build.Tests.NativeLibraryTests.ProjectUsingBrowserNativeCrypto(config: Release, aot: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 52 | `Wasm.Build.Tests.NativeLibraryTests.ProjectWithNativeLibrary(config: Debug, aot: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 53 | `Wasm.Build.Tests.NativeLibraryTests.ProjectWithNativeLibrary(config: Release, aot: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 54 | `Wasm.Build.Tests.NativeLibraryTests.ProjectWithNativeReference(config: Debug, aot: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 55 | `Wasm.Build.Tests.NativeLibraryTests.ProjectWithNativeReference(config: Release, aot: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 56 | `Wasm.Build.Tests.PInvokeTableGeneratorTests.BuildNativeInNonEnglishCulture(config: Debug, aot: False, culture: \"tr_TR.UTF-8\")` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 57 | `Wasm.Build.Tests.PInvokeTableGeneratorTests.BuildNativeInNonEnglishCulture(config: Release, aot: False, culture: \"tr_TR.UTF-8\")` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 58 | `Wasm.Build.Tests.PInvokeTableGeneratorTests.UCOWithSpecialCharacters(config: Debug, aot: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 59 | `Wasm.Build.Tests.PInvokeTableGeneratorTests.UCOWithSpecialCharacters(config: Release, aot: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 60 | `Wasm.Build.Tests.PInvokeTableGeneratorTests.UnmanagedCallback_InFileType(config: Debug, aot: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 61 | `Wasm.Build.Tests.PInvokeTableGeneratorTests.UnmanagedCallback_InFileType(config: Release, aot: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 62 | `Wasm.Build.Tests.PInvokeTableGeneratorTests.UnmanagedCallersOnly_Namespaced(config: Debug, aot: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 63 | `Wasm.Build.Tests.PInvokeTableGeneratorTests.UnmanagedCallersOnly_Namespaced(config: Release, aot: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 64 | `Wasm.Build.Tests.SatelliteAssembliesTests.ResourcesFromMainAssembly(config: Debug, aot: False, nativeRelink: False, argCulture: \"es-ES\")` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files - |
| 65 | `Wasm.Build.Tests.SatelliteAssembliesTests.ResourcesFromMainAssembly(config: Debug, aot: False, nativeRelink: False, argCulture: \"ja-JP\")` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files - |
| 66 | `Wasm.Build.Tests.SatelliteAssembliesTests.ResourcesFromMainAssembly(config: Debug, aot: False, nativeRelink: False, argCulture: null)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files - |
| 67 | `Wasm.Build.Tests.SatelliteAssembliesTests.ResourcesFromMainAssembly(config: Debug, aot: False, nativeRelink: True, argCulture: \"es-ES\")` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files - |
| 68 | `Wasm.Build.Tests.SatelliteAssembliesTests.ResourcesFromMainAssembly(config: Debug, aot: False, nativeRelink: True, argCulture: \"ja-JP\")` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files - |
| 69 | `Wasm.Build.Tests.SatelliteAssembliesTests.ResourcesFromMainAssembly(config: Debug, aot: False, nativeRelink: True, argCulture: null)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files - |
| 70 | `Wasm.Build.Tests.SatelliteAssembliesTests.ResourcesFromMainAssembly(config: Release, aot: False, nativeRelink: False, argCulture: \"es-ES\")` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files - |
| 71 | `Wasm.Build.Tests.SatelliteAssembliesTests.ResourcesFromMainAssembly(config: Release, aot: False, nativeRelink: False, argCulture: \"ja-JP\")` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files - |
| 72 | `Wasm.Build.Tests.SatelliteAssembliesTests.ResourcesFromMainAssembly(config: Release, aot: False, nativeRelink: False, argCulture: null)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files - |
| 73 | `Wasm.Build.Tests.SatelliteAssembliesTests.ResourcesFromMainAssembly(config: Release, aot: False, nativeRelink: True, argCulture: \"es-ES\")` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files - |
| 74 | `Wasm.Build.Tests.SatelliteAssembliesTests.ResourcesFromMainAssembly(config: Release, aot: False, nativeRelink: True, argCulture: \"ja-JP\")` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files - |
| 75 | `Wasm.Build.Tests.SatelliteAssembliesTests.ResourcesFromMainAssembly(config: Release, aot: False, nativeRelink: True, argCulture: null)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files - |
| 76 | `Wasm.Build.Tests.SatelliteAssembliesTests.ResourcesFromProjectReference(config: Debug, aot: False, nativeRelink: False, argCulture: \"es-ES\")` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 77 | `Wasm.Build.Tests.SatelliteAssembliesTests.ResourcesFromProjectReference(config: Debug, aot: False, nativeRelink: False, argCulture: \"ja-JP\")` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 78 | `Wasm.Build.Tests.SatelliteAssembliesTests.ResourcesFromProjectReference(config: Debug, aot: False, nativeRelink: False, argCulture: null)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 79 | `Wasm.Build.Tests.SatelliteAssembliesTests.ResourcesFromProjectReference(config: Debug, aot: False, nativeRelink: True, argCulture: \"es-ES\")` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 80 | `Wasm.Build.Tests.SatelliteAssembliesTests.ResourcesFromProjectReference(config: Debug, aot: False, nativeRelink: True, argCulture: \"ja-JP\")` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 81 | `Wasm.Build.Tests.SatelliteAssembliesTests.ResourcesFromProjectReference(config: Debug, aot: False, nativeRelink: True, argCulture: null)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 82 | `Wasm.Build.Tests.SatelliteAssembliesTests.ResourcesFromProjectReference(config: Release, aot: False, nativeRelink: False, argCulture: \"es-ES\")` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 83 | `Wasm.Build.Tests.SatelliteAssembliesTests.ResourcesFromProjectReference(config: Release, aot: False, nativeRelink: False, argCulture: \"ja-JP\")` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 84 | `Wasm.Build.Tests.SatelliteAssembliesTests.ResourcesFromProjectReference(config: Release, aot: False, nativeRelink: False, argCulture: null)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 85 | `Wasm.Build.Tests.SatelliteAssembliesTests.ResourcesFromProjectReference(config: Release, aot: False, nativeRelink: True, argCulture: \"es-ES\")` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 86 | `Wasm.Build.Tests.SatelliteAssembliesTests.ResourcesFromProjectReference(config: Release, aot: False, nativeRelink: True, argCulture: \"ja-JP\")` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 87 | `Wasm.Build.Tests.SatelliteAssembliesTests.ResourcesFromProjectReference(config: Release, aot: False, nativeRelink: True, argCulture: null)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 88 | `Wasm.Build.Tests.WasmSIMDTests.PublishSIMD_AOT(config: Debug, aot: False, simd: True)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |
| 89 | `Wasm.Build.Tests.WasmSIMDTests.PublishSIMD_AOT(config: Release, aot: False, simd: True)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet xharness wasm webserver --app=. --web-server-use-default-files\n |

## TEMPLATE_MISSING (38 tests)

`dotnet new wasmbrowser` template is not available without the `wasm-tools` workload. These tests use `CreateWasmTemplateProject` and cannot work with `dotnet-none`. They would need either workload installation or conversion to `CopyTestAsset`.

| # | Test | Error (truncated) |
|---|------|--------------------|
| 1 | `Wasm.Build.Templates.Tests.NativeBuildTests.BuildWithUndefinedNativeSymbol(allowUndefined: False)` |  Expected 0 exit code but got 103: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet new wasmbrowser \nStandard Output:\n[] No templates or subcomm |
| 2 | `Wasm.Build.Templates.Tests.NativeBuildTests.BuildWithUndefinedNativeSymbol(allowUndefined: True)` |  Expected 0 exit code but got 103: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet new wasmbrowser \nStandard Output:\n[] No templates or subcomm |
| 3 | `Wasm.Build.Templates.Tests.NativeBuildTests.ProjectWithDllImportsRequiringMarshalIlGen_ArrayTypeParameter(config: Debug)` |  Expected 0 exit code but got 103: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet new wasmbrowser \nStandard Output:\n[] No templates or subcomm |
| 4 | `Wasm.Build.Templates.Tests.NativeBuildTests.ProjectWithDllImportsRequiringMarshalIlGen_ArrayTypeParameter(config: Release)` |  Expected 0 exit code but got 103: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet new wasmbrowser \nStandard Output:\n[] No templates or subcomm |
| 5 | `Wasm.Build.Tests.IcuShardingTests.AutomaticShardSelectionDependingOnEnvLocale(config: Release, aot: False, environmentLocale: \"fr-FR\", testedLocales: \"new Locale[] {\\n        new Locale(\\\"en-US\\\", \\\"Su\"···)` |  Expected 0 exit code but got 103: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet new wasmbrowser \nStandard Output:\n[] No templates or subcomm |
| 6 | `Wasm.Build.Tests.IcuShardingTests.AutomaticShardSelectionDependingOnEnvLocale(config: Release, aot: False, environmentLocale: \"ja-JP\", testedLocales: \"new Locale[] {\\n        new Locale(\\\"en-GB\\\", \\\"Su\"···)` |  Expected 0 exit code but got 103: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet new wasmbrowser \nStandard Output:\n[] No templates or subcomm |
| 7 | `Wasm.Build.Tests.IcuShardingTests.AutomaticShardSelectionDependingOnEnvLocale(config: Release, aot: False, environmentLocale: \"sk-SK\", testedLocales: \"new Locale[] {\\n        new Locale(\\\"en-AU\\\", \\\"Su\"···)` |  Expected 0 exit code but got 103: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet new wasmbrowser \nStandard Output:\n[] No templates or subcomm |
| 8 | `Wasm.Build.Tests.IcuShardingTests.AutomaticShardSelectionDependingOnEnvLocale(config: Release, aot: True, environmentLocale: \"fr-FR\", testedLocales: \"new Locale[] {\\n        new Locale(\\\"en-US\\\", \\\"Su\"···)` |  Expected 0 exit code but got 103: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet new wasmbrowser \nStandard Output:\n[] No templates or subcomm |
| 9 | `Wasm.Build.Tests.IcuShardingTests.AutomaticShardSelectionDependingOnEnvLocale(config: Release, aot: True, environmentLocale: \"ja-JP\", testedLocales: \"new Locale[] {\\n        new Locale(\\\"en-GB\\\", \\\"Su\"···)` |  Expected 0 exit code but got 103: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet new wasmbrowser \nStandard Output:\n[] No templates or subcomm |
| 10 | `Wasm.Build.Tests.IcuShardingTests.AutomaticShardSelectionDependingOnEnvLocale(config: Release, aot: True, environmentLocale: \"sk-SK\", testedLocales: \"new Locale[] {\\n        new Locale(\\\"en-AU\\\", \\\"Su\"···)` |  Expected 0 exit code but got 103: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet new wasmbrowser \nStandard Output:\n[] No templates or subcomm |
| 11 | `Wasm.Build.Tests.IcuShardingTests.CustomIcuShard(config: Release, aot: False, customIcuPath: \"/workspaces/runtime/artifacts/bin/Wasm.Build.Tests\"···, customLocales: \"new Locale[] {\\n        new Locale(\\\"cy-GB\\\",  \\\"D\"···, onlyPredefinedCultures: False)` |  Expected 0 exit code but got 103: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet new wasmbrowser \nStandard Output:\n[] No templates or subcomm |
| 12 | `Wasm.Build.Tests.IcuShardingTests.CustomIcuShard(config: Release, aot: True, customIcuPath: \"/workspaces/runtime/artifacts/bin/Wasm.Build.Tests\"···, customLocales: \"new Locale[] {\\n        new Locale(\\\"cy-GB\\\",  \\\"D\"···, onlyPredefinedCultures: False)` |  Expected 0 exit code but got 103: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet new wasmbrowser \nStandard Output:\n[] No templates or subcomm |
| 13 | `Wasm.Build.Tests.IcuShardingTests2.DefaultAvailableIcuShardsFromRuntimePack(config: Release, aot: False, shardName: \"icudt.dat\", testedLocales: \"new Locale[] {\\n                                  \"···)` |  Expected 0 exit code but got 103: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet new wasmbrowser \nStandard Output:\n[] No templates or subcomm |
| 14 | `Wasm.Build.Tests.IcuShardingTests2.DefaultAvailableIcuShardsFromRuntimePack(config: Release, aot: False, shardName: \"icudt_CJK.dat\", testedLocales: \"new Locale[] {\\n        new Locale(\\\"en-GB\\\", \\\"Su\"···)` |  Expected 0 exit code but got 103: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet new wasmbrowser \nStandard Output:\n[] No templates or subcomm |
| 15 | `Wasm.Build.Tests.IcuShardingTests2.DefaultAvailableIcuShardsFromRuntimePack(config: Release, aot: False, shardName: \"icudt_EFIGS.dat\", testedLocales: \"new Locale[] {\\n        new Locale(\\\"en-US\\\", \\\"Su\"···)` |  Expected 0 exit code but got 103: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet new wasmbrowser \nStandard Output:\n[] No templates or subcomm |
| 16 | `Wasm.Build.Tests.IcuShardingTests2.DefaultAvailableIcuShardsFromRuntimePack(config: Release, aot: False, shardName: \"icudt_no_CJK.dat\", testedLocales: \"new Locale[] {\\n        new Locale(\\\"en-AU\\\", \\\"Su\"···)` |  Expected 0 exit code but got 103: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet new wasmbrowser \nStandard Output:\n[] No templates or subcomm |
| 17 | `Wasm.Build.Tests.IcuShardingTests2.DefaultAvailableIcuShardsFromRuntimePack(config: Release, aot: True, shardName: \"icudt.dat\", testedLocales: \"new Locale[] {\\n                                  \"···)` |  Expected 0 exit code but got 103: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet new wasmbrowser \nStandard Output:\n[] No templates or subcomm |
| 18 | `Wasm.Build.Tests.IcuShardingTests2.DefaultAvailableIcuShardsFromRuntimePack(config: Release, aot: True, shardName: \"icudt_CJK.dat\", testedLocales: \"new Locale[] {\\n        new Locale(\\\"en-GB\\\", \\\"Su\"···)` |  Expected 0 exit code but got 103: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet new wasmbrowser \nStandard Output:\n[] No templates or subcomm |
| 19 | `Wasm.Build.Tests.IcuShardingTests2.DefaultAvailableIcuShardsFromRuntimePack(config: Release, aot: True, shardName: \"icudt_EFIGS.dat\", testedLocales: \"new Locale[] {\\n        new Locale(\\\"en-US\\\", \\\"Su\"···)` |  Expected 0 exit code but got 103: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet new wasmbrowser \nStandard Output:\n[] No templates or subcomm |
| 20 | `Wasm.Build.Tests.IcuShardingTests2.DefaultAvailableIcuShardsFromRuntimePack(config: Release, aot: True, shardName: \"icudt_no_CJK.dat\", testedLocales: \"new Locale[] {\\n        new Locale(\\\"en-AU\\\", \\\"Su\"···)` |  Expected 0 exit code but got 103: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet new wasmbrowser \nStandard Output:\n[] No templates or subcomm |
| 21 | `Wasm.Build.Tests.IcuTests.FullIcuFromRuntimePackWithCustomIcu(config: Release, aot: False, fullIcu: False)` |  Expected 0 exit code but got 103: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet new wasmbrowser \nStandard Output:\n[] No templates or subcomm |
| 22 | `Wasm.Build.Tests.IcuTests.FullIcuFromRuntimePackWithCustomIcu(config: Release, aot: False, fullIcu: True)` |  Expected 0 exit code but got 103: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet new wasmbrowser \nStandard Output:\n[] No templates or subcomm |
| 23 | `Wasm.Build.Tests.IcuTests.FullIcuFromRuntimePackWithCustomIcu(config: Release, aot: True, fullIcu: False)` |  Expected 0 exit code but got 103: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet new wasmbrowser \nStandard Output:\n[] No templates or subcomm |
| 24 | `Wasm.Build.Tests.IcuTests.FullIcuFromRuntimePackWithCustomIcu(config: Release, aot: True, fullIcu: True)` |  Expected 0 exit code but got 103: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet new wasmbrowser \nStandard Output:\n[] No templates or subcomm |
| 25 | `Wasm.Build.Tests.IcuTests.FullIcuFromRuntimePackWithInvariant(config: Release, aot: False, invariant: False, fullIcu: False, testedLocales: \"new Locale[] {\\n        new Locale(\\\"en-US\\\", \\\"Su\"···)` |  Expected 0 exit code but got 103: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet new wasmbrowser \nStandard Output:\n[] No templates or subcomm |
| 26 | `Wasm.Build.Tests.IcuTests.FullIcuFromRuntimePackWithInvariant(config: Release, aot: False, invariant: False, fullIcu: True, testedLocales: \"new Locale[] {\\n        new Locale(\\\"en-GB\\\", \\\"Su\"···)` |  Expected 0 exit code but got 103: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet new wasmbrowser \nStandard Output:\n[] No templates or subcomm |
| 27 | `Wasm.Build.Tests.IcuTests.FullIcuFromRuntimePackWithInvariant(config: Release, aot: False, invariant: True, fullIcu: False, testedLocales: \"Array.Empty<Locale>()\")` |  Expected 0 exit code but got 103: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet new wasmbrowser \nStandard Output:\n[] No templates or subcomm |
| 28 | `Wasm.Build.Tests.IcuTests.FullIcuFromRuntimePackWithInvariant(config: Release, aot: False, invariant: True, fullIcu: True, testedLocales: \"Array.Empty<Locale>()\")` |  Expected 0 exit code but got 103: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet new wasmbrowser \nStandard Output:\n[] No templates or subcomm |
| 29 | `Wasm.Build.Tests.IcuTests.FullIcuFromRuntimePackWithInvariant(config: Release, aot: True, invariant: False, fullIcu: False, testedLocales: \"new Locale[] {\\n        new Locale(\\\"en-US\\\", \\\"Su\"···)` |  Expected 0 exit code but got 103: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet new wasmbrowser \nStandard Output:\n[] No templates or subcomm |
| 30 | `Wasm.Build.Tests.IcuTests.FullIcuFromRuntimePackWithInvariant(config: Release, aot: True, invariant: False, fullIcu: True, testedLocales: \"new Locale[] {\\n        new Locale(\\\"en-GB\\\", \\\"Su\"···)` |  Expected 0 exit code but got 103: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet new wasmbrowser \nStandard Output:\n[] No templates or subcomm |
| 31 | `Wasm.Build.Tests.IcuTests.FullIcuFromRuntimePackWithInvariant(config: Release, aot: True, invariant: True, fullIcu: False, testedLocales: \"Array.Empty<Locale>()\")` |  Expected 0 exit code but got 103: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet new wasmbrowser \nStandard Output:\n[] No templates or subcomm |
| 32 | `Wasm.Build.Tests.IcuTests.FullIcuFromRuntimePackWithInvariant(config: Release, aot: True, invariant: True, fullIcu: True, testedLocales: \"Array.Empty<Locale>()\")` |  Expected 0 exit code but got 103: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet new wasmbrowser \nStandard Output:\n[] No templates or subcomm |
| 33 | `Wasm.Build.Tests.NativeBuildTests.AOTNotSupportedWithNoTrimming(config: Debug, aot: True)` |  Expected 0 exit code but got 103: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet new wasmbrowser \nStandard Output:\n[] No templates or subcomm |
| 34 | `Wasm.Build.Tests.NativeBuildTests.AOTNotSupportedWithNoTrimming(config: Release, aot: True)` |  Expected 0 exit code but got 103: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet new wasmbrowser \nStandard Output:\n[] No templates or subcomm |
| 35 | `Wasm.Build.Tests.NativeBuildTests.IntermediateBitcodeToObjectFilesAreNotLLVMIR(config: Release, aot: True)` |  Expected 0 exit code but got 103: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet new wasmbrowser \nStandard Output:\n[] No templates or subcomm |
| 36 | `Wasm.Build.Tests.NativeBuildTests.NativeBuildIsRequired(config: Release, aot: True)` |  Expected 0 exit code but got 103: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet new wasmbrowser \nStandard Output:\n[] No templates or subcomm |
| 37 | `Wasm.Build.Tests.NativeBuildTests.SimpleNativeBuild(config: Debug, aot: False)` |  Expected 0 exit code but got 103: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet new wasmbrowser \nStandard Output:\n[] No templates or subcomm |
| 38 | `Wasm.Build.Tests.NativeBuildTests.SimpleNativeBuild(config: Release, aot: False)` |  Expected 0 exit code but got 103: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet new wasmbrowser \nStandard Output:\n[] No templates or subcomm |

## NETSDK1147_WORKLOAD_REQUIRED (59 tests)

Build fails with NETSDK1147 "wasm-tools workload must be installed". This happens when referenced projects (e.g., LazyLibrary.csproj) use `Microsoft.NET.Sdk.WebAssembly` and the `UsingBrowserRuntimeWorkload=false` property is not propagated to them.

| # | Test | Error (truncated) |
|---|------|--------------------|
| 1 | `Wasm.Build.NativeRebuild.Tests.FlagsChangeRebuildTests.ExtraEmccFlagsSetButNoRealChange(config: Release, aot: False, extraCFlags: \"/p:EmccExtraCFlags=-g\", extraLDFlags: \"/p:EmccExtraLDFlags=-g\")` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 2 | `Wasm.Build.NativeRebuild.Tests.FlagsChangeRebuildTests.ExtraEmccFlagsSetButNoRealChange(config: Release, aot: False, extraCFlags: \"/p:EmccExtraCFlags=-g\", extraLDFlags: \"\")` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 3 | `Wasm.Build.NativeRebuild.Tests.FlagsChangeRebuildTests.ExtraEmccFlagsSetButNoRealChange(config: Release, aot: False, extraCFlags: \"\", extraLDFlags: \"/p:EmccExtraLDFlags=-g\")` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 4 | `Wasm.Build.NativeRebuild.Tests.FlagsChangeRebuildTests.ExtraEmccFlagsSetButNoRealChange(config: Release, aot: True, extraCFlags: \"/p:EmccExtraCFlags=-g\", extraLDFlags: \"/p:EmccExtraLDFlags=-g\")` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 5 | `Wasm.Build.NativeRebuild.Tests.FlagsChangeRebuildTests.ExtraEmccFlagsSetButNoRealChange(config: Release, aot: True, extraCFlags: \"/p:EmccExtraCFlags=-g\", extraLDFlags: \"\")` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 6 | `Wasm.Build.NativeRebuild.Tests.FlagsChangeRebuildTests.ExtraEmccFlagsSetButNoRealChange(config: Release, aot: True, extraCFlags: \"\", extraLDFlags: \"/p:EmccExtraLDFlags=-g\")` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 7 | `Wasm.Build.NativeRebuild.Tests.NoopNativeRebuildTest.NativeRelinkFailsWithInvariant` | Assert.Contains() Failure: Sub-string not found\nString:    \"\\n** -------- publish -------- **\\n\\nBinlog \"···\nNot found: \"WasmBuildNative is re |
| 8 | `Wasm.Build.NativeRebuild.Tests.NoopNativeRebuildTest.NoOpRebuildForNativeBuilds(config: Debug, aot: False, nativeRelink: True, invariant: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 9 | `Wasm.Build.NativeRebuild.Tests.NoopNativeRebuildTest.NoOpRebuildForNativeBuilds(config: Debug, aot: False, nativeRelink: True, invariant: True)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 10 | `Wasm.Build.NativeRebuild.Tests.NoopNativeRebuildTest.NoOpRebuildForNativeBuilds(config: Release, aot: False, nativeRelink: True, invariant: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 11 | `Wasm.Build.NativeRebuild.Tests.NoopNativeRebuildTest.NoOpRebuildForNativeBuilds(config: Release, aot: False, nativeRelink: True, invariant: True)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 12 | `Wasm.Build.NativeRebuild.Tests.NoopNativeRebuildTest.NoOpRebuildForNativeBuilds(config: Release, aot: True, nativeRelink: False, invariant: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 13 | `Wasm.Build.NativeRebuild.Tests.OptimizationFlagChangeTests.OptimizationFlagChange(config: Release, aot: False, cflags: \"/p:EmccCompileOptimizationFlag=-O1\", ldflags: \"\")` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 14 | `Wasm.Build.NativeRebuild.Tests.OptimizationFlagChangeTests.OptimizationFlagChange(config: Release, aot: False, cflags: \"\", ldflags: \"/p:EmccLinkOptimizationFlag=-O1\")` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 15 | `Wasm.Build.NativeRebuild.Tests.OptimizationFlagChangeTests.OptimizationFlagChange(config: Release, aot: True, cflags: \"/p:EmccCompileOptimizationFlag=-O1\", ldflags: \"\")` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 16 | `Wasm.Build.NativeRebuild.Tests.OptimizationFlagChangeTests.OptimizationFlagChange(config: Release, aot: True, cflags: \"\", ldflags: \"/p:EmccLinkOptimizationFlag=-O1\")` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 17 | `Wasm.Build.NativeRebuild.Tests.ReferenceNewAssemblyRebuildTest.ReferenceNewAssembly(config: Debug, aot: False, nativeRelink: True, invariant: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 18 | `Wasm.Build.NativeRebuild.Tests.ReferenceNewAssemblyRebuildTest.ReferenceNewAssembly(config: Debug, aot: False, nativeRelink: True, invariant: True)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 19 | `Wasm.Build.NativeRebuild.Tests.ReferenceNewAssemblyRebuildTest.ReferenceNewAssembly(config: Release, aot: False, nativeRelink: True, invariant: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 20 | `Wasm.Build.NativeRebuild.Tests.ReferenceNewAssemblyRebuildTest.ReferenceNewAssembly(config: Release, aot: False, nativeRelink: True, invariant: True)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 21 | `Wasm.Build.NativeRebuild.Tests.ReferenceNewAssemblyRebuildTest.ReferenceNewAssembly(config: Release, aot: True, nativeRelink: False, invariant: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 22 | `Wasm.Build.NativeRebuild.Tests.SimpleSourceChangeRebuildTest.SimpleStringChangeInSource(config: Debug, aot: False, nativeRelink: True, invariant: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 23 | `Wasm.Build.NativeRebuild.Tests.SimpleSourceChangeRebuildTest.SimpleStringChangeInSource(config: Debug, aot: False, nativeRelink: True, invariant: True)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 24 | `Wasm.Build.NativeRebuild.Tests.SimpleSourceChangeRebuildTest.SimpleStringChangeInSource(config: Release, aot: False, nativeRelink: True, invariant: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 25 | `Wasm.Build.NativeRebuild.Tests.SimpleSourceChangeRebuildTest.SimpleStringChangeInSource(config: Release, aot: False, nativeRelink: True, invariant: True)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 26 | `Wasm.Build.NativeRebuild.Tests.SimpleSourceChangeRebuildTest.SimpleStringChangeInSource(config: Release, aot: True, nativeRelink: False, invariant: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 27 | `Wasm.Build.Tests.BuildPublishTests.BuildThenPublishWithAOT(config: Release, aot: True)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet build -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/Deb |
| 28 | `Wasm.Build.Tests.BuildPublishTests.Wasm_CannotAOT_InDebug(config: Debug, aot: True)` | Assert.Contains() Failure: Sub-string not found\nString:    \"Property reassignment: $(MSBuildProjectEx\"···\nNot found: \"AOT is not supported in deb |
| 29 | `Wasm.Build.Tests.InvariantGlobalizationTests.AOT_InvariantGlobalization(config: Release, aot: True, invariantGlobalization: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 30 | `Wasm.Build.Tests.InvariantGlobalizationTests.AOT_InvariantGlobalization(config: Release, aot: True, invariantGlobalization: True)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 31 | `Wasm.Build.Tests.InvariantGlobalizationTests.AOT_InvariantGlobalization(config: Release, aot: True, invariantGlobalization: null)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 32 | `Wasm.Build.Tests.MainWithArgsTests.AsyncMainWithArgs(config: Release, aot: True, args: [\"abc\", \"foobar\"])` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 33 | `Wasm.Build.Tests.MainWithArgsTests.AsyncMainWithArgs(config: Release, aot: True, args: [])` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 34 | `Wasm.Build.Tests.MainWithArgsTests.NonAsyncMainWithArgs(config: Release, aot: True, args: [\"abc\", \"foobar\"])` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 35 | `Wasm.Build.Tests.MainWithArgsTests.NonAsyncMainWithArgs(config: Release, aot: True, args: [])` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 36 | `Wasm.Build.Tests.NativeLibraryTests.ProjectUsingBrowserNativeCrypto(config: Release, aot: True)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 37 | `Wasm.Build.Tests.NativeLibraryTests.ProjectWithNativeLibrary(config: Release, aot: True)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 38 | `Wasm.Build.Tests.NativeLibraryTests.ProjectWithNativeReference(config: Release, aot: True)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 39 | `Wasm.Build.Tests.PInvokeTableGeneratorTests.EnsureComInteropCompilesInAOT(config: Release, aot: True)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 40 | `Wasm.Build.Tests.PInvokeTableGeneratorTests.EnsureWasmAbiRulesAreFollowedInAOT(config: Release, aot: True)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 41 | `Wasm.Build.Tests.SatelliteAssembliesTests.CheckThatSatelliteAssembliesAreNotAOTed(config: Release, aot: True)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 42 | `Wasm.Build.Tests.SatelliteAssembliesTests.ResourcesFromMainAssembly(config: Release, aot: True, nativeRelink: False, argCulture: \"es-ES\")` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 43 | `Wasm.Build.Tests.SatelliteAssembliesTests.ResourcesFromMainAssembly(config: Release, aot: True, nativeRelink: False, argCulture: \"ja-JP\")` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 44 | `Wasm.Build.Tests.SatelliteAssembliesTests.ResourcesFromMainAssembly(config: Release, aot: True, nativeRelink: False, argCulture: null)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 45 | `Wasm.Build.Tests.SatelliteAssembliesTests.ResourcesFromProjectReference(config: Release, aot: True, nativeRelink: True, argCulture: \"es-ES\")` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 46 | `Wasm.Build.Tests.SatelliteAssembliesTests.ResourcesFromProjectReference(config: Release, aot: True, nativeRelink: True, argCulture: \"ja-JP\")` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 47 | `Wasm.Build.Tests.SatelliteAssembliesTests.ResourcesFromProjectReference(config: Release, aot: True, nativeRelink: True, argCulture: null)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 48 | `Wasm.Build.Tests.WasmBuildAppTest.AsyncMain_AOT(config: Release, aot: True)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 49 | `Wasm.Build.Tests.WasmBuildAppTest.Bug49588_RegressionTest_AOT(config: Release, aot: True)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 50 | `Wasm.Build.Tests.WasmBuildAppTest.Bug49588_RegressionTest_NativeRelinking(config: Debug, aot: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 51 | `Wasm.Build.Tests.WasmBuildAppTest.Bug49588_RegressionTest_NativeRelinking(config: Release, aot: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 52 | `Wasm.Build.Tests.WasmBuildAppTest.NonAsyncMain_AOT(config: Release, aot: True)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 53 | `Wasm.Build.Tests.WasmBuildAppTest.TopLevelMain_AOT(config: Release, aot: True)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 54 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithPublish(config: Debug, extraProperties: \"<PublishTrimmed>false</PublishTrimmed>\", aot: True, expectWasmBuildNativeForBuild: False, expectWasmBuildNativeForPublish: True)` | Assert.Contains() Failure: Sub-string not found\nString:    \"Assembly loaded during LoggerInitializati\"···\nNot found: \"Stopping the build\" |
| 55 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithPublish(config: Debug, extraProperties: \"\", aot: True, expectWasmBuildNativeForBuild: False, expectWasmBuildNativeForPublish: True)` | Assert.Contains() Failure: Sub-string not found\nString:    \"Assembly loaded during LoggerInitializati\"···\nNot found: \"Stopping the build\" |
| 56 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithPublish(config: Release, extraProperties: \"<PublishTrimmed>false</PublishTrimmed>\", aot: True, expectWasmBuildNativeForBuild: False, expectWasmBuildNativeForPublish: True)` | Assert.Contains() Failure: Sub-string not found\nString:    \"Property reassignment: $(MSBuildProjectEx\"···\nNot found: \"Stopping the build\" |
| 57 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithPublish(config: Release, extraProperties: \"\", aot: True, expectWasmBuildNativeForBuild: False, expectWasmBuildNativeForPublish: True)` | Assert.Contains() Failure: Sub-string not found\nString:    \"Property reassignment: $(MSBuildProjectEx\"···\nNot found: \"Stopping the build\" |
| 58 | `Wasm.Build.Tests.WasmSIMDTests.PublishSIMD_AOT(config: Release, aot: True, simd: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |
| 59 | `Wasm.Build.Tests.WasmSIMDTests.PublishSIMD_AOT(config: Release, aot: True, simd: True)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |

## PUBLISH_FAILED (1 tests)

`dotnet publish` command failed. These may be AOT-related failures (AOT not yet supported on CoreCLR WASM), NETSDK1147 on transitive project references, or other publish-time issues.

| # | Test | Error (truncated) |
|---|------|--------------------|
| 1 | `Wasm.Build.Tests.Blazor.DllImportTests.WithDllImportInMainAssembly(config: Release, build: False, publish: True)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/D |

## BUILD_FAILED (3 tests)

`dotnet build` command failed during the build step.

| # | Test | Error (truncated) |
|---|------|--------------------|
| 1 | `Wasm.Build.Tests.Blazor.DllImportTests.WithDllImportInMainAssembly(config: Debug, build: True, publish: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet build -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/Deb |
| 2 | `Wasm.Build.Tests.Blazor.DllImportTests.WithDllImportInMainAssembly(config: Release, build: True, publish: False)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet build -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/Deb |
| 3 | `Wasm.Build.Tests.Blazor.DllImportTests.WithDllImportInMainAssembly(config: Release, build: True, publish: True)` |  Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet build -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/Deb |

## ASSERTION_FAILURE (89 tests)

The test built and ran but the assertions did not match expected behavior. These indicate behavioral differences between CoreCLR and Mono WASM runtimes that may need test adaptation.

| # | Test | Error (truncated) |
|---|------|--------------------|
| 1 | `Wasm.Build.Tests.Blazor.BuildPublishTests.BlazorWasm_CannotAOT_InDebug(config: Debug)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 2 | `Wasm.Build.Tests.Blazor.MiscTests.DefaultTemplate_AOT_InProjectFile(config: Release)` | Assert.Contains() Failure: Sub-string not found\nString:    \"Property reassignment: $(MSBuildProjectEx\"···\nNot found: \"Microsoft.JSInterop.dll ->  |
| 3 | `Wasm.Build.Tests.Blazor.NativeTests.BlazorWasm_CannotAOT_WithNoTrimming(config: Release)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 4 | `Wasm.Build.Tests.DllImportTests.NativeLibraryWithVariadicFunctions(config: Debug, aot: False)` | Assert.Matches() Failure: Pattern not found in value\nRegex: \"warning.*native function.*sum.*varargs\"\nValue: \"Assembly loaded during LoggerInitial |
| 5 | `Wasm.Build.Tests.DllImportTests.NativeLibraryWithVariadicFunctions(config: Release, aot: False)` | Assert.Matches() Failure: Pattern not found in value\nRegex: \"warning.*native function.*sum.*varargs\"\nValue: \"Property reassignment: $(MSBuildProj |
| 6 | `Wasm.Build.Tests.ModuleConfigTests.SymbolMapFileEmitted(isPublish: False)` | Assert.Equal() Failure: Values differ\nExpected: True\nActual:   False |
| 7 | `Wasm.Build.Tests.ModuleConfigTests.SymbolMapFileEmitted(isPublish: True)` | Assert.Equal() Failure: Values differ\nExpected: True\nActual:   False |
| 8 | `Wasm.Build.Tests.PInvokeTableGeneratorTests.UnmanagedStructAndMethodIn_SameAssembly_WithoutDisableRuntimeMarshallingAttribute_NotConsideredBlittable(config: Debug, aot: False)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 9 | `Wasm.Build.Tests.PInvokeTableGeneratorTests.UnmanagedStructAndMethodIn_SameAssembly_WithoutDisableRuntimeMarshallingAttribute_NotConsideredBlittable(config: Release, aot: False)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 10 | `Wasm.Build.Tests.PInvokeTableGeneratorTests.UnmanagedStructsAreConsideredBlittableFromDifferentAssembly(config: Debug, aot: False, libraryHasAttribute: False, appHasAttribute: False, expectSuccess: False)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 11 | `Wasm.Build.Tests.PInvokeTableGeneratorTests.UnmanagedStructsAreConsideredBlittableFromDifferentAssembly(config: Debug, aot: False, libraryHasAttribute: True, appHasAttribute: False, expectSuccess: False)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 12 | `Wasm.Build.Tests.PInvokeTableGeneratorTests.UnmanagedStructsAreConsideredBlittableFromDifferentAssembly(config: Release, aot: False, libraryHasAttribute: False, appHasAttribute: False, expectSuccess: False)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 13 | `Wasm.Build.Tests.PInvokeTableGeneratorTests.UnmanagedStructsAreConsideredBlittableFromDifferentAssembly(config: Release, aot: False, libraryHasAttribute: True, appHasAttribute: False, expectSuccess: False)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 14 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithBuild(config: Debug, extraProperties: \"<InvariantGlobalization>false</InvariantGlobalizat\"···, aot: False, expectWasmBuildNativeForBuild: False, expectWasmBuildNativeForPublish: False)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 15 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithBuild(config: Debug, extraProperties: \"<InvariantGlobalization>true</InvariantGlobalizati\"···, aot: False, expectWasmBuildNativeForBuild: True, expectWasmBuildNativeForPublish: True)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 16 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithBuild(config: Debug, extraProperties: \"<InvariantTimezone>false</InvariantTimezone>\", aot: False, expectWasmBuildNativeForBuild: False, expectWasmBuildNativeForPublish: False)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 17 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithBuild(config: Debug, extraProperties: \"<InvariantTimezone>true</InvariantTimezone>\", aot: False, expectWasmBuildNativeForBuild: True, expectWasmBuildNativeForPublish: True)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 18 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithBuild(config: Debug, extraProperties: \"<WasmEnableExceptionHandling>false</WasmEnableExce\"···, aot: False, expectWasmBuildNativeForBuild: True, expectWasmBuildNativeForPublish: True)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 19 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithBuild(config: Debug, extraProperties: \"<WasmEnableExceptionHandling>true</WasmEnableExcep\"···, aot: False, expectWasmBuildNativeForBuild: False, expectWasmBuildNativeForPublish: False)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 20 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithBuild(config: Debug, extraProperties: \"<WasmEnableSIMD>false</WasmEnableSIMD>\", aot: False, expectWasmBuildNativeForBuild: True, expectWasmBuildNativeForPublish: True)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 21 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithBuild(config: Debug, extraProperties: \"<WasmEnableSIMD>true</WasmEnableSIMD>\", aot: False, expectWasmBuildNativeForBuild: False, expectWasmBuildNativeForPublish: False)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 22 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithBuild(config: Debug, extraProperties: \"\", aot: False, expectWasmBuildNativeForBuild: False, expectWasmBuildNativeForPublish: False)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 23 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithBuild(config: Debug, extraProperties: \"\", aot: False, expectWasmBuildNativeForBuild: False, expectWasmBuildNativeForPublish: False)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 24 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithBuild(config: Debug, extraProperties: \"\", aot: False, expectWasmBuildNativeForBuild: False, expectWasmBuildNativeForPublish: False)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 25 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithBuild(config: Debug, extraProperties: \"\", aot: False, expectWasmBuildNativeForBuild: False, expectWasmBuildNativeForPublish: False)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 26 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithBuild(config: Debug, extraProperties: \"\", aot: False, expectWasmBuildNativeForBuild: False, expectWasmBuildNativeForPublish: True)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 27 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithBuild(config: Release, extraProperties: \"<InvariantGlobalization>false</InvariantGlobalizat\"···, aot: False, expectWasmBuildNativeForBuild: False, expectWasmBuildNativeForPublish: False)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 28 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithBuild(config: Release, extraProperties: \"<InvariantGlobalization>true</InvariantGlobalizati\"···, aot: False, expectWasmBuildNativeForBuild: True, expectWasmBuildNativeForPublish: True)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 29 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithBuild(config: Release, extraProperties: \"<InvariantTimezone>false</InvariantTimezone>\", aot: False, expectWasmBuildNativeForBuild: False, expectWasmBuildNativeForPublish: False)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 30 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithBuild(config: Release, extraProperties: \"<InvariantTimezone>true</InvariantTimezone>\", aot: False, expectWasmBuildNativeForBuild: True, expectWasmBuildNativeForPublish: True)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 31 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithBuild(config: Release, extraProperties: \"<PublishTrimmed>false</PublishTrimmed>\", aot: False, expectWasmBuildNativeForBuild: False, expectWasmBuildNativeForPublish: False)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 32 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithBuild(config: Release, extraProperties: \"<WasmEnableExceptionHandling>false</WasmEnableExce\"···, aot: False, expectWasmBuildNativeForBuild: True, expectWasmBuildNativeForPublish: True)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 33 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithBuild(config: Release, extraProperties: \"<WasmEnableExceptionHandling>true</WasmEnableExcep\"···, aot: False, expectWasmBuildNativeForBuild: False, expectWasmBuildNativeForPublish: False)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 34 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithBuild(config: Release, extraProperties: \"<WasmEnableSIMD>false</WasmEnableSIMD>\", aot: False, expectWasmBuildNativeForBuild: True, expectWasmBuildNativeForPublish: True)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 35 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithBuild(config: Release, extraProperties: \"<WasmEnableSIMD>true</WasmEnableSIMD>\", aot: False, expectWasmBuildNativeForBuild: False, expectWasmBuildNativeForPublish: False)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 36 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithBuild(config: Release, extraProperties: \"\", aot: False, expectWasmBuildNativeForBuild: False, expectWasmBuildNativeForPublish: False)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 37 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithBuild(config: Release, extraProperties: \"\", aot: False, expectWasmBuildNativeForBuild: False, expectWasmBuildNativeForPublish: False)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 38 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithBuild(config: Release, extraProperties: \"\", aot: False, expectWasmBuildNativeForBuild: False, expectWasmBuildNativeForPublish: False)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 39 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithBuild(config: Release, extraProperties: \"\", aot: False, expectWasmBuildNativeForBuild: False, expectWasmBuildNativeForPublish: False)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 40 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithBuild(config: Release, extraProperties: \"\", aot: False, expectWasmBuildNativeForBuild: False, expectWasmBuildNativeForPublish: True)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 41 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithPublish(config: Debug, extraProperties: \"<InvariantGlobalization>false</InvariantGlobalizat\"···, aot: False, expectWasmBuildNativeForBuild: False, expectWasmBuildNativeForPublish: False)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 42 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithPublish(config: Debug, extraProperties: \"<InvariantGlobalization>true</InvariantGlobalizati\"···, aot: False, expectWasmBuildNativeForBuild: True, expectWasmBuildNativeForPublish: True)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 43 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithPublish(config: Debug, extraProperties: \"<InvariantTimezone>false</InvariantTimezone>\", aot: False, expectWasmBuildNativeForBuild: False, expectWasmBuildNativeForPublish: False)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 44 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithPublish(config: Debug, extraProperties: \"<InvariantTimezone>true</InvariantTimezone>\", aot: False, expectWasmBuildNativeForBuild: True, expectWasmBuildNativeForPublish: True)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 45 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithPublish(config: Debug, extraProperties: \"<WasmEnableExceptionHandling>false</WasmEnableExce\"···, aot: False, expectWasmBuildNativeForBuild: True, expectWasmBuildNativeForPublish: True)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 46 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithPublish(config: Debug, extraProperties: \"<WasmEnableExceptionHandling>true</WasmEnableExcep\"···, aot: False, expectWasmBuildNativeForBuild: False, expectWasmBuildNativeForPublish: False)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 47 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithPublish(config: Debug, extraProperties: \"<WasmEnableSIMD>false</WasmEnableSIMD>\", aot: False, expectWasmBuildNativeForBuild: True, expectWasmBuildNativeForPublish: True)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 48 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithPublish(config: Debug, extraProperties: \"<WasmEnableSIMD>true</WasmEnableSIMD>\", aot: False, expectWasmBuildNativeForBuild: False, expectWasmBuildNativeForPublish: False)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 49 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithPublish(config: Debug, extraProperties: \"\", aot: False, expectWasmBuildNativeForBuild: False, expectWasmBuildNativeForPublish: False)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 50 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithPublish(config: Debug, extraProperties: \"\", aot: False, expectWasmBuildNativeForBuild: False, expectWasmBuildNativeForPublish: False)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 51 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithPublish(config: Debug, extraProperties: \"\", aot: False, expectWasmBuildNativeForBuild: False, expectWasmBuildNativeForPublish: False)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 52 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithPublish(config: Debug, extraProperties: \"\", aot: False, expectWasmBuildNativeForBuild: False, expectWasmBuildNativeForPublish: False)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 53 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithPublish(config: Debug, extraProperties: \"\", aot: False, expectWasmBuildNativeForBuild: False, expectWasmBuildNativeForPublish: False)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 54 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithPublish(config: Release, extraProperties: \"<InvariantGlobalization>false</InvariantGlobalizat\"···, aot: False, expectWasmBuildNativeForBuild: False, expectWasmBuildNativeForPublish: True)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 55 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithPublish(config: Release, extraProperties: \"<InvariantGlobalization>true</InvariantGlobalizati\"···, aot: False, expectWasmBuildNativeForBuild: True, expectWasmBuildNativeForPublish: True)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 56 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithPublish(config: Release, extraProperties: \"<InvariantTimezone>false</InvariantTimezone>\", aot: False, expectWasmBuildNativeForBuild: False, expectWasmBuildNativeForPublish: True)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 57 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithPublish(config: Release, extraProperties: \"<InvariantTimezone>true</InvariantTimezone>\", aot: False, expectWasmBuildNativeForBuild: True, expectWasmBuildNativeForPublish: True)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 58 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithPublish(config: Release, extraProperties: \"<PublishTrimmed>false</PublishTrimmed>\", aot: False, expectWasmBuildNativeForBuild: False, expectWasmBuildNativeForPublish: False)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 59 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithPublish(config: Release, extraProperties: \"<WasmEnableExceptionHandling>false</WasmEnableExce\"···, aot: False, expectWasmBuildNativeForBuild: True, expectWasmBuildNativeForPublish: True)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 60 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithPublish(config: Release, extraProperties: \"<WasmEnableExceptionHandling>true</WasmEnableExcep\"···, aot: False, expectWasmBuildNativeForBuild: False, expectWasmBuildNativeForPublish: True)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 61 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithPublish(config: Release, extraProperties: \"<WasmEnableSIMD>false</WasmEnableSIMD>\", aot: False, expectWasmBuildNativeForBuild: True, expectWasmBuildNativeForPublish: True)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 62 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithPublish(config: Release, extraProperties: \"<WasmEnableSIMD>true</WasmEnableSIMD>\", aot: False, expectWasmBuildNativeForBuild: False, expectWasmBuildNativeForPublish: True)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 63 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithPublish(config: Release, extraProperties: \"\", aot: False, expectWasmBuildNativeForBuild: False, expectWasmBuildNativeForPublish: True)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 64 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithPublish(config: Release, extraProperties: \"\", aot: False, expectWasmBuildNativeForBuild: False, expectWasmBuildNativeForPublish: True)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 65 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithPublish(config: Release, extraProperties: \"\", aot: False, expectWasmBuildNativeForBuild: False, expectWasmBuildNativeForPublish: True)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 66 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithPublish(config: Release, extraProperties: \"\", aot: False, expectWasmBuildNativeForBuild: False, expectWasmBuildNativeForPublish: True)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 67 | `Wasm.Build.Tests.WasmNativeDefaultsTests.DefaultsWithPublish(config: Release, extraProperties: \"\", aot: False, expectWasmBuildNativeForBuild: False, expectWasmBuildNativeForPublish: True)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 68 | `Wasm.Build.Tests.WasmNativeDefaultsTests.WasmNativeStripDefaultWithBuild(config: Debug, extraProperties: \"<WasmNativeStrip>false</WasmNativeStrip><Invariant\"···, expectedWasmBuildNativeValue: True, expectedWasmNativeStripValue: False)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 69 | `Wasm.Build.Tests.WasmNativeDefaultsTests.WasmNativeStripDefaultWithBuild(config: Debug, extraProperties: \"<WasmNativeStrip>false</WasmNativeStrip>\", expectedWasmBuildNativeValue: True, expectedWasmNativeStripValue: False)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 70 | `Wasm.Build.Tests.WasmNativeDefaultsTests.WasmNativeStripDefaultWithBuild(config: Debug, extraProperties: \"<WasmNativeStrip>true</WasmNativeStrip><InvariantT\"···, expectedWasmBuildNativeValue: True, expectedWasmNativeStripValue: True)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 71 | `Wasm.Build.Tests.WasmNativeDefaultsTests.WasmNativeStripDefaultWithBuild(config: Debug, extraProperties: \"<WasmNativeStrip>true</WasmNativeStrip>\", expectedWasmBuildNativeValue: False, expectedWasmNativeStripValue: True)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 72 | `Wasm.Build.Tests.WasmNativeDefaultsTests.WasmNativeStripDefaultWithBuild(config: Release, extraProperties: \"<WasmNativeStrip>false</WasmNativeStrip><Invariant\"···, expectedWasmBuildNativeValue: True, expectedWasmNativeStripValue: False)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 73 | `Wasm.Build.Tests.WasmNativeDefaultsTests.WasmNativeStripDefaultWithBuild(config: Release, extraProperties: \"<WasmNativeStrip>false</WasmNativeStrip>\", expectedWasmBuildNativeValue: True, expectedWasmNativeStripValue: False)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 74 | `Wasm.Build.Tests.WasmNativeDefaultsTests.WasmNativeStripDefaultWithBuild(config: Release, extraProperties: \"<WasmNativeStrip>true</WasmNativeStrip><InvariantT\"···, expectedWasmBuildNativeValue: True, expectedWasmNativeStripValue: True)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 75 | `Wasm.Build.Tests.WasmNativeDefaultsTests.WasmNativeStripDefaultWithBuild(config: Release, extraProperties: \"<WasmNativeStrip>true</WasmNativeStrip>\", expectedWasmBuildNativeValue: False, expectedWasmNativeStripValue: True)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 76 | `Wasm.Build.Tests.WasmNativeDefaultsTests.WasmNativeStripDefaultWithPublish(config: Debug, extraProperties: \"<WasmNativeStrip>false</WasmNativeStrip><Invariant\"···, expectedWasmBuildNativeValue: True, expectedWasmNativeStripValue: False)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 77 | `Wasm.Build.Tests.WasmNativeDefaultsTests.WasmNativeStripDefaultWithPublish(config: Debug, extraProperties: \"<WasmNativeStrip>false</WasmNativeStrip>\", expectedWasmBuildNativeValue: True, expectedWasmNativeStripValue: False)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 78 | `Wasm.Build.Tests.WasmNativeDefaultsTests.WasmNativeStripDefaultWithPublish(config: Debug, extraProperties: \"<WasmNativeStrip>true</WasmNativeStrip><InvariantT\"···, expectedWasmBuildNativeValue: True, expectedWasmNativeStripValue: True)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 79 | `Wasm.Build.Tests.WasmNativeDefaultsTests.WasmNativeStripDefaultWithPublish(config: Debug, extraProperties: \"<WasmNativeStrip>true</WasmNativeStrip>\", expectedWasmBuildNativeValue: False, expectedWasmNativeStripValue: True)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 80 | `Wasm.Build.Tests.WasmNativeDefaultsTests.WasmNativeStripDefaultWithPublish(config: Release, extraProperties: \"<WasmNativeStrip>false</WasmNativeStrip><Invariant\"···, expectedWasmBuildNativeValue: True, expectedWasmNativeStripValue: False)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 81 | `Wasm.Build.Tests.WasmNativeDefaultsTests.WasmNativeStripDefaultWithPublish(config: Release, extraProperties: \"<WasmNativeStrip>false</WasmNativeStrip>\", expectedWasmBuildNativeValue: True, expectedWasmNativeStripValue: False)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 82 | `Wasm.Build.Tests.WasmNativeDefaultsTests.WasmNativeStripDefaultWithPublish(config: Release, extraProperties: \"<WasmNativeStrip>true</WasmNativeStrip><InvariantT\"···, expectedWasmBuildNativeValue: True, expectedWasmNativeStripValue: True)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 83 | `Wasm.Build.Tests.WasmNativeDefaultsTests.WasmNativeStripDefaultWithPublish(config: Release, extraProperties: \"<WasmNativeStrip>true</WasmNativeStrip>\", expectedWasmBuildNativeValue: True, expectedWasmNativeStripValue: True)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 84 | `Wasm.Build.Tests.WasmNativeDefaultsTests.WithNativeReference(config: Debug, extraProperties: \"\", publish: False)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 85 | `Wasm.Build.Tests.WasmNativeDefaultsTests.WithNativeReference(config: Debug, extraProperties: \"\", publish: True)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 86 | `Wasm.Build.Tests.WasmNativeDefaultsTests.WithNativeReference(config: Release, extraProperties: \"<PublishTrimmed>false</PublishTrimmed>\", publish: True)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 87 | `Wasm.Build.Tests.WasmNativeDefaultsTests.WithNativeReference(config: Release, extraProperties: \"\", publish: False)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 88 | `Wasm.Build.Tests.WasmNativeDefaultsTests.WithNativeReference(config: Release, extraProperties: \"\", publish: True)` | Build should have failed, but it didn't. Process exited with exitCode : 0 |
| 89 | `Wasm.Build.Tests.WorkloadTests.FilesInUnixFilesPermissionsXmlExist` | Assert.Contains() Failure: Filter not matched in collection\nCollection: [] |

## OTHER_FAILURE (16 tests)

Other failures not fitting the above categories.

| # | Test | Error (truncated) |
|---|------|--------------------|
| 1 | `Wasm.Build.Tests.Blazor.CleanTests.Blazor_BuildNative_ThenBuildNonNative_ThenClean(config: Debug)` | Could not find expected relink dir: /workspaces/runtime/artifacts/bin/Wasm.Build.Tests/Debug/net11.0/linux-x64/wbt artifacts/clean_native_Debug_True_g |
| 2 | `Wasm.Build.Tests.Blazor.CleanTests.Blazor_BuildNative_ThenBuildNonNative_ThenClean(config: Release)` | Could not find expected relink dir: /workspaces/runtime/artifacts/bin/Wasm.Build.Tests/Debug/net11.0/linux-x64/wbt artifacts/clean_native_Release_True |
| 3 | `Wasm.Build.Tests.Blazor.CleanTests.Blazor_BuildNoNative_ThenBuildNative_ThenClean(config: Debug)` | Could not find expected relink dir: /workspaces/runtime/artifacts/bin/Wasm.Build.Tests/Debug/net11.0/linux-x64/wbt artifacts/clean_native_Debug_True_f |
| 4 | `Wasm.Build.Tests.Blazor.CleanTests.Blazor_BuildNoNative_ThenBuildNative_ThenClean(config: Release)` | Could not find expected relink dir: /workspaces/runtime/artifacts/bin/Wasm.Build.Tests/Debug/net11.0/linux-x64/wbt artifacts/clean_native_Release_True |
| 5 | `Wasm.Build.Tests.Blazor.CleanTests.Blazor_BuildThenClean_NativeRelinking(config: Debug)` | Could not find expected relink dir: /workspaces/runtime/artifacts/bin/Wasm.Build.Tests/Debug/net11.0/linux-x64/wbt artifacts/clean_Debug_True_2f5mjowm |
| 6 | `Wasm.Build.Tests.Blazor.CleanTests.Blazor_BuildThenClean_NativeRelinking(config: Release)` | Could not find expected relink dir: /workspaces/runtime/artifacts/bin/Wasm.Build.Tests/Debug/net11.0/linux-x64/wbt artifacts/clean_Release_True_a2z5vh |
| 7 | `Wasm.Build.Tests.Blazor.NoopNativeRebuildTest.BlazorNoopRebuild(config: Debug)` | System.IO.DirectoryNotFoundException : Could not find a part of the path '/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/Debug/net11.0/linux-x64/w |
| 8 | `Wasm.Build.Tests.Blazor.NoopNativeRebuildTest.BlazorNoopRebuild(config: Release)` | System.IO.DirectoryNotFoundException : Could not find a part of the path '/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/Debug/net11.0/linux-x64/w |
| 9 | `Wasm.Build.Tests.Blazor.NoopNativeRebuildTest.BlazorOnlyLinkRebuild(config: Debug)` | System.IO.DirectoryNotFoundException : Could not find a part of the path '/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/Debug/net11.0/linux-x64/w |
| 10 | `Wasm.Build.Tests.Blazor.NoopNativeRebuildTest.BlazorOnlyLinkRebuild(config: Release)` | System.IO.DirectoryNotFoundException : Could not find a part of the path '/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/Debug/net11.0/linux-x64/w |
| 11 | `Wasm.Build.Tests.DllImportTests.CallIntoLibrariesWithNonAlphanumericCharactersInTheirNames(config: Debug, aot: False, libraryNames: [\"with-hyphen\", \"with#hash-and-hyphen\", \"with.per.iod\", \"with🚀unicode#\"])` | System.Exception : Expected exit code 42 but got 1.\nconsoleOutput=Unhandled exception. System.DllNotFoundException: Unable to load shared library 'wi |
| 12 | `Wasm.Build.Tests.DllImportTests.CallIntoLibrariesWithNonAlphanumericCharactersInTheirNames(config: Release, aot: False, libraryNames: [\"with-hyphen\", \"with#hash-and-hyphen\", \"with.per.iod\", \"with🚀unicode#\"])` | System.Exception : Expected exit code 42 but got 1.\nconsoleOutput=Unhandled exception. System.DllNotFoundException: Unable to load shared library 'wi |
| 13 | `Wasm.Build.Tests.PInvokeTableGeneratorTests.EnsureWasmAbiRulesAreFollowedInInterpreter(config: Debug, aot: False)` | System.IO.DirectoryNotFoundException : Could not find a part of the path '/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/Debug/net11.0/linux-x64/w |
| 14 | `Wasm.Build.Tests.PInvokeTableGeneratorTests.EnsureWasmAbiRulesAreFollowedInInterpreter(config: Release, aot: False)` | System.IO.DirectoryNotFoundException : Could not find a part of the path '/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/Debug/net11.0/linux-x64/w |
| 15 | `Wasm.Build.Tests.PInvokeTableGeneratorTests.IcallWithOverloadedParametersAndEnum(config: Debug, aot: False)` | System.IO.DirectoryNotFoundException : Could not find tasks base directory /workspaces/runtime/artifacts/bin/dotnet-none/packs/Microsoft.NET.Runtime.W |
| 16 | `Wasm.Build.Tests.PInvokeTableGeneratorTests.IcallWithOverloadedParametersAndEnum(config: Release, aot: False)` | System.IO.DirectoryNotFoundException : Could not find tasks base directory /workspaces/runtime/artifacts/bin/dotnet-none/packs/Microsoft.NET.Runtime.W |

## Appendix: Full Error Messages for Key Failures

### `Wasm.Build.Tests.Blazor.BuildPublishTests.BlazorWasm_CannotAOT_InDebug(config: Debug)`

**Category**: ASSERTION_FAILURE

**Message**:
```
Build should have failed, but it didn't. Process exited with exitCode : 0
```

**Stack trace** (truncated):
```
   at Wasm.Build.Tests.BuildTestBase.BuildProjectWithoutAssert(Configuration configuration, String projectName, MSBuildOptions buildOptions) in /workspaces/runtime/src/mono/wasm/Wasm.Build.Tests/BuildTestBase.cs:line 178
   at Wasm.Build.Tests.WasmTemplateTestsBase.BuildProjectCore(ProjectInfo info, Configuration configuration, MSBuildOptions buildOptions, Nullable`1 isNativeBuild, Nullable`1 wasmFingerprintDotnetJs) in /workspaces/runtime/src/mono/wasm/Wasm.Build.Tests/Templates/WasmTemplateTestsBase.cs:line 242
   at Wasm.Build.Tests.WasmTemplateTestsBase.PublishProject(ProjectInfo info, Configuration configuration, PublishOptions publishOptions, Nullable`1 isNativeBuild, Nullable`1 wasmFingerprintDotnetJs) in /workspaces/runtime/src/mono/wasm/Wasm.Build.Tests/Templates/WasmTemplateTests
```

### `Wasm.Build.Tests.Blazor.CleanTests.Blazor_BuildNative_ThenBuildNonNative_ThenClean(config: Debug)`

**Category**: OTHER_FAILURE

**Message**:
```
Could not find expected relink dir: /workspaces/runtime/artifacts/bin/Wasm.Build.Tests/Debug/net11.0/linux-x64/wbt artifacts/clean_native_Debug_True_gwykxyob_1vm/App/obj/Debug/net11.0/wasm/for-build
```

**Stack trace** (truncated):
```
   at Wasm.Build.Tests.Blazor.CleanTests.Blazor_BuildNativeNonNative_ThenCleanTest(Configuration config, Boolean firstBuildNative) in /workspaces/runtime/src/mono/wasm/Wasm.Build.Tests/Blazor/CleanTests.cs:line 71
   at Wasm.Build.Tests.Blazor.CleanTests.Blazor_BuildNative_ThenBuildNonNative_ThenClean(Configuration config) in /workspaces/runtime/src/mono/wasm/Wasm.Build.Tests/Blazor/CleanTests.cs:line 56
   at System.RuntimeMethodHandle.InvokeMethod(ObjectHandleOnStack target, Void** arguments, ObjectHandleOnStack sig, BOOL isConstructor, ObjectHandleOnStack result)
   at System.RuntimeMethodHandle.InvokeMethod(ObjectHandleOnStack target, Void** arguments, ObjectHandleOnStack sig, BOOL isConstructor, ObjectHandleOnStack result)
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Me
```

### `Wasm.Build.Tests.Blazor.DllImportTests.WithDllImportInMainAssembly(config: Debug, build: True, publish: False)`

**Category**: BUILD_FAILED

**Message**:
```
 Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet build -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/Debug/net11.0/linux-x64/blz_dllimp_Debug_鿀蜒枛遫䡫煉build_Debug_False_yfy4iz14_uc0/BlazorBasicTestApp-build.binlog -p:Configuration=Debug -nr:false -p:WasmEnableHotReload=false /warnaserror \nStandard Output:\n[] \n[]   Determining projects to restore...\n[]   Restored /workspaces/runtime/artifacts/bin/Wasm.Build.Tests/Debug/net11.0/linux-x64/wbt artifacts/blz_dllimp_Debug_鿀蜒枛遫䡫煉build_Debug_False_yfy4iz14_uc0/App/BlazorBasicTestApp.csproj (in 631 ms).\n[] /workspaces/runtime/artifacts/bin/dotnet-none/sdk/11.0.100-preview.2.26116.109/Sdks/Microsoft.NET.Sdk/targets/Microsoft.NET.RuntimeIdentifierInference.targets(383,5): message NETSDK1057: You are using a preview version of .NET. See: https://aka.ms/dotnet-support-policy [/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/Debug/net11.0/linux-x64/wbt artifacts/blz_dllimp_Debug_鿀蜒枛遫䡫煉build_Debug_False_yfy4iz14_uc0/App/BlazorBasicTestApp.csproj]\n[]   BlazorBasicTestApp -> /workspaces/runtime/artifacts/bin/Wasm.Build.Tests/Debug/net11.0/linux-x64/wbt artifacts/blz_dllimp_Debug_鿀蜒枛遫䡫煉build_Debug_False_yfy4iz14_uc0/App/bin/Debug/net11.0/BlazorBasicTestApp.dll\n[]   BlazorBasicTestApp (Blazor output) -> /workspaces/runtime/artifacts/bin/Wasm.Build.Tests/Debug/net11.0/linux-x64/wbt artifacts/blz_dllimp_Debug_鿀蜒枛遫䡫煉build_Debug_False_yfy4iz14_uc0/App/bin/Debug/net11.0/wwwroot\n[] /workspaces/runt
```

**Stack trace** (truncated):
```
   at Wasm.Build.Tests.CommandResult.EnsureExitCode(Int32 expectedExitCode, String messagePrefix, Boolean suppressOutput) in /workspaces/runtime/src/mono/wasm/Wasm.Build.Tests/Common/CommandResult.cs:line 51
   at Wasm.Build.Tests.CommandResult.EnsureSuccessful(String messagePrefix, Boolean suppressOutput) in /workspaces/runtime/src/mono/wasm/Wasm.Build.Tests/Common/CommandResult.cs:line 28
   at Wasm.Build.Tests.BuildTestBase.BuildProjectWithoutAssert(Configuration configuration, String projectName, MSBuildOptions buildOptions) in /workspaces/runtime/src/mono/wasm/Wasm.Build.Tests/BuildTestBase.cs:line 176
   at Wasm.Build.Tests.WasmTemplateTestsBase.BuildProjectCore(ProjectInfo info, Configuration configuration, MSBuildOptions buildOptions, Nullable`1 isNativeBuild, Nullable`1 wasmFinger
```

### `Wasm.Build.Tests.Blazor.DllImportTests.WithDllImportInMainAssembly(config: Release, build: False, publish: True)`

**Category**: PUBLISH_FAILED

**Message**:
```
 Expected 0 exit code but got 1: /workspaces/runtime/artifacts/bin/dotnet-none/dotnet publish -bl:/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/Debug/net11.0/linux-x64/blz_dllimp_Release_鿀蜒枛遫䡫煉publish_Release_False_ra45311g_kr2/BlazorBasicTestApp-publish.binlog -p:Configuration=Release -nr:false -p:CompressionEnabled=false -p:WasmEnableHotReload=false /warnaserror  \nStandard Output:\n[] \n[]   Determining projects to restore...\n[]   Restored /workspaces/runtime/artifacts/bin/Wasm.Build.Tests/Debug/net11.0/linux-x64/wbt artifacts/blz_dllimp_Release_鿀蜒枛遫䡫煉publish_Release_False_ra45311g_kr2/App/BlazorBasicTestApp.csproj (in 690 ms).\n[] /workspaces/runtime/artifacts/bin/dotnet-none/sdk/11.0.100-preview.2.26116.109/Sdks/Microsoft.NET.Sdk/targets/Microsoft.NET.RuntimeIdentifierInference.targets(383,5): message NETSDK1057: You are using a preview version of .NET. See: https://aka.ms/dotnet-support-policy [/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/Debug/net11.0/linux-x64/wbt artifacts/blz_dllimp_Release_鿀蜒枛遫䡫煉publish_Release_False_ra45311g_kr2/App/BlazorBasicTestApp.csproj]\n[]   BlazorBasicTestApp -> /workspaces/runtime/artifacts/bin/Wasm.Build.Tests/Debug/net11.0/linux-x64/wbt artifacts/blz_dllimp_Release_鿀蜒枛遫䡫煉publish_Release_False_ra45311g_kr2/App/bin/Release/net11.0/BlazorBasicTestApp.dll\n[]   BlazorBasicTestApp (Blazor output) -> /workspaces/runtime/artifacts/bin/Wasm.Build.Tests/Debug/net11.0/linux-x64/wbt artifacts/blz_dllimp_Release_鿀蜒枛遫䡫煉publish_Release_Fa
```

**Stack trace** (truncated):
```
   at Wasm.Build.Tests.CommandResult.EnsureExitCode(Int32 expectedExitCode, String messagePrefix, Boolean suppressOutput) in /workspaces/runtime/src/mono/wasm/Wasm.Build.Tests/Common/CommandResult.cs:line 51
   at Wasm.Build.Tests.CommandResult.EnsureSuccessful(String messagePrefix, Boolean suppressOutput) in /workspaces/runtime/src/mono/wasm/Wasm.Build.Tests/Common/CommandResult.cs:line 28
   at Wasm.Build.Tests.BuildTestBase.BuildProjectWithoutAssert(Configuration configuration, String projectName, MSBuildOptions buildOptions) in /workspaces/runtime/src/mono/wasm/Wasm.Build.Tests/BuildTestBase.cs:line 176
   at Wasm.Build.Tests.WasmTemplateTestsBase.BuildProjectCore(ProjectInfo info, Configuration configuration, MSBuildOptions buildOptions, Nullable`1 isNativeBuild, Nullable`1 wasmFinger
```

### `Wasm.Build.Tests.Blazor.MiscTests.DefaultTemplate_AOT_InProjectFile(config: Release)`

**Category**: ASSERTION_FAILURE

**Message**:
```
Assert.Contains() Failure: Sub-string not found\nString:    \"Property reassignment: $(MSBuildProjectEx\"···\nNot found: \"Microsoft.JSInterop.dll -> Microsoft.JSIn\"···
```

**Stack trace** (truncated):
```
   at Wasm.Build.Tests.BlazorWasmTestBase.AssertBundle(Configuration config, String buildOutput, MSBuildOptions buildOptions, Nullable`1 isNativeBuild) in /workspaces/runtime/src/mono/wasm/Wasm.Build.Tests/Blazor/BlazorWasmTestBase.cs:line 171
   at Wasm.Build.Tests.BlazorWasmTestBase.BlazorPublish(ProjectInfo info, Configuration config, PublishOptions publishOptions, Nullable`1 isNativeBuild) in /workspaces/runtime/src/mono/wasm/Wasm.Build.Tests/Blazor/BlazorWasmTestBase.cs:line 148
   at Wasm.Build.Tests.Blazor.MiscTests.DefaultTemplate_AOT_InProjectFile(Configuration config) in /workspaces/runtime/src/mono/wasm/Wasm.Build.Tests/Blazor/MiscTests.cs:line 75
   at System.RuntimeMethodHandle.InvokeMethod(ObjectHandleOnStack target, Void** arguments, ObjectHandleOnStack sig, BOOL isConstruct
```

### `Wasm.Build.Tests.Blazor.NoopNativeRebuildTest.BlazorNoopRebuild(config: Debug)`

**Category**: OTHER_FAILURE

**Message**:
```
System.IO.DirectoryNotFoundException : Could not find a part of the path '/workspaces/runtime/artifacts/bin/Wasm.Build.Tests/Debug/net11.0/linux-x64/wbt artifacts/blz_rebuild_Debug_False_egtjyytj_qg4/App/obj/Debug/net11.0/wasm'.
```

**Stack trace** (truncated):
```
   at System.IO.Enumeration.FileSystemEnumerator`1.Init()
   at System.IO.Directory.InternalEnumeratePaths(String path, String searchPattern, SearchTarget searchTarget, EnumerationOptions enumerationOptions)
   at Wasm.Build.Tests.ProjectProviderBase.GetFilesTable(Boolean unchanged, String[] baseDirs) in /workspaces/runtime/src/mono/wasm/Wasm.Build.Tests/ProjectProviderBase.cs:line 334
   at Wasm.Build.Tests.Blazor.NoopNativeRebuildTest.BlazorNoopRebuild(Configuration config) in /workspaces/runtime/src/mono/wasm/Wasm.Build.Tests/Blazor/NoopNativeRebuildTest.cs:line 35
   at System.RuntimeMethodHandle.InvokeMethod(ObjectHandleOnStack target, Void** arguments, ObjectHandleOnStack sig, BOOL isConstructor, ObjectHandleOnStack result)
   at System.RuntimeMethodHandle.InvokeMethod(ObjectHandleOn
```

### `Wasm.Build.Tests.DllImportTests.CallIntoLibrariesWithNonAlphanumericCharactersInTheirNames(config: Debug, aot: False, libraryNames: [\"with-hyphen\", \"with#hash-and-hyphen\", \"with.per.iod\", \"with🚀unicode#\"])`

**Category**: OTHER_FAILURE

**Message**:
```
System.Exception : Expected exit code 42 but got 1.\nconsoleOutput=Unhandled exception. System.DllNotFoundException: Unable to load shared library 'with-hyphen' or one of its dependencies. In order to help diagnose loading problems, consider using a tool like strace. If you're using glibc, consider setting the LD_DEBUG environment variable: \ndynamic linking not enabled\n\n   at Program.<Main>$(String[] args) in /workspaces/runtime/artifacts/bin/Wasm.Build.Tests/Debug/net11.0/linux-x64/wbt artifacts/abi_Debug_False_xg1yxqb3_zeu/App/Common/Program.cs:line 4
```

**Stack trace** (truncated):
```
   at Wasm.Build.Tests.WasmTemplateTestsBase.BrowserRun(ToolCommand cmd, String runArgs, RunOptions runOptions) in /workspaces/runtime/src/mono/wasm/Wasm.Build.Tests/Templates/WasmTemplateTestsBase.cs:line 454
--- End of stack trace from previous location ---
   at Wasm.Build.Tests.WasmTemplateTestsBase.BrowserRun(ToolCommand cmd, String runArgs, RunOptions runOptions) in /workspaces/runtime/src/mono/wasm/Wasm.Build.Tests/Templates/WasmTemplateTestsBase.cs:line 456
   at Wasm.Build.Tests.WasmTemplateTestsBase.BrowserRunTest(String runArgs, String workingDirectory, RunOptions runOptions) in /workspaces/runtime/src/mono/wasm/Wasm.Build.Tests/Templates/WasmTemplateTestsBase.cs:line 397
   at Wasm.Build.Tests.WasmTemplateTestsBase.BrowserRun(RunOptions runOptions) in /workspaces/runtime/src/mo
```

### `Wasm.Build.Tests.DllImportTests.NativeLibraryWithVariadicFunctions(config: Debug, aot: False)`

**Category**: ASSERTION_FAILURE

**Message**:
```
Assert.Matches() Failure: Pattern not found in value\nRegex: \"warning.*native function.*sum.*varargs\"\nValue: \"Assembly loaded during LoggerInitialization (Micro\"···
```

**Stack trace** (truncated):
```
   at Wasm.Build.Tests.DllImportTests.NativeLibraryWithVariadicFunctions(Configuration config, Boolean aot) in /workspaces/runtime/src/mono/wasm/Wasm.Build.Tests/DllImportTests.cs:line 32
--- End of stack trace from previous location ---
--- End of stack trace from previous location ---
```

### `Wasm.Build.Tests.ModuleConfigTests.SymbolMapFileEmitted(isPublish: False)`

**Category**: ASSERTION_FAILURE

**Message**:
```
Assert.Equal() Failure: Values differ\nExpected: True\nActual:   False
```

**Stack trace** (truncated):
```
   at Wasm.Build.Tests.ModuleConfigTests.SymbolMapFileEmittedCore(Boolean emitSymbolMap, Boolean isPublish) in /workspaces/runtime/src/mono/wasm/Wasm.Build.Tests/ModuleConfigTests.cs:line 156
   at Wasm.Build.Tests.ModuleConfigTests.SymbolMapFileEmitted(Boolean isPublish) in /workspaces/runtime/src/mono/wasm/Wasm.Build.Tests/ModuleConfigTests.cs:line 131
   at System.RuntimeMethodHandle.InvokeMethod(ObjectHandleOnStack target, Void** arguments, ObjectHandleOnStack sig, BOOL isConstructor, ObjectHandleOnStack result)
   at System.RuntimeMethodHandle.InvokeMethod(ObjectHandleOnStack target, Void** arguments, ObjectHandleOnStack sig, BOOL isConstructor, ObjectHandleOnStack result)
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
   at System.Reflect
```

### `Wasm.Build.Tests.PInvokeTableGeneratorTests.IcallWithOverloadedParametersAndEnum(config: Debug, aot: False)`

**Category**: OTHER_FAILURE

**Message**:
```
System.IO.DirectoryNotFoundException : Could not find tasks base directory /workspaces/runtime/artifacts/bin/dotnet-none/packs/Microsoft.NET.Runtime.WebAssembly.Sdk/11.0.0-dev/tasks
```

**Stack trace** (truncated):
```
   at Wasm.Build.Tests.PInvokeTableGeneratorTests.IcallWithOverloadedParametersAndEnum(Configuration config, Boolean aot) in /workspaces/runtime/src/mono/wasm/Wasm.Build.Tests/PInvokeTableGeneratorTests.cs:line 240
   at System.RuntimeMethodHandle.InvokeMethod(ObjectHandleOnStack target, Void** arguments, ObjectHandleOnStack sig, BOOL isConstructor, ObjectHandleOnStack result)
   at System.RuntimeMethodHandle.InvokeMethod(ObjectHandleOnStack target, Void** arguments, ObjectHandleOnStack sig, BOOL isConstructor, ObjectHandleOnStack result)
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
   at System.Reflection.MethodBaseInvoker.InvokeDirectByRefWithFewArgs(Object obj, Span`1 copyOfArgs, BindingFlags invokeAttr)
```

### `Wasm.Build.Tests.WorkloadTests.FilesInUnixFilesPermissionsXmlExist`

**Category**: ASSERTION_FAILURE

**Message**:
```
Assert.Contains() Failure: Filter not matched in collection\nCollection: []
```

**Stack trace** (truncated):
```
   at Wasm.Build.Tests.WorkloadTests.FilesInUnixFilesPermissionsXmlExist() in /workspaces/runtime/src/mono/wasm/Wasm.Build.Tests/WorkloadTests.cs:line 68
   at System.RuntimeMethodHandle.InvokeMethod(ObjectHandleOnStack target, Void** arguments, ObjectHandleOnStack sig, BOOL isConstructor, ObjectHandleOnStack result)
   at System.RuntimeMethodHandle.InvokeMethod(ObjectHandleOnStack target, Void** arguments, ObjectHandleOnStack sig, BOOL isConstructor, ObjectHandleOnStack result)
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)
```

