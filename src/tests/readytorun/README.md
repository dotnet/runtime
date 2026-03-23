# ReadyToRun Tests

These tests validate ReadyToRun (R2R) behavior in the CoreCLR test suite, especially crossgen2 compilation correctness, cross-module compilation behavior, platform-specific code generation, and determinism. The directory contains both harness-driven tests and tests that invoke crossgen2 manually when they need more control over inputs or emitted artifacts.

## Test Patterns

### Pattern 1: Built-in Harness (preferred for simple tests)

Use the built-in harness when a test only needs the standard ReadyToRun pipeline for a single assembly.

- Enable it with `<AlwaysUseCrossGen2>true</AlwaysUseCrossGen2>`.
- Add extra crossgen2 switches when needed with `<CrossGen2TestExtraArguments>`.
- Examples:
  - `crossgen2/crossgen2smoke.csproj` for the basic harness-driven pattern
  - `HardwareIntrinsics/` for harness-driven tests with extra instruction-set flags
  - `GenericCycleDetection/` for harness-driven tests with extra cycle-detection flags
- When to use: single-assembly compilation, standard validation, or tests that only need a few extra crossgen2 switches.

This pattern is the simplest to maintain because the normal test infrastructure handles the crossgen2 invocation and test execution. For example, `GenericCycleDetection/*.csproj` enables `AlwaysUseCrossGen2` and adds cycle-detection options through `CrossGen2TestExtraArguments`, while `HardwareIntrinsics/X86/*.csproj` appends instruction-set flags the same way.

### Pattern 2: Manual crossgen2 Invocation

Use manual crossgen2 invocation when the built-in harness is not expressive enough.

- Disable automatic crossgen with `<CrossGenTest>false</CrossGenTest>`.
- Run custom compilation steps with `CLRTestBashPreCommands` (and usually matching `CLRTestBatchPreCommands` for Windows).
- When to use: need `--map`, `--inputbubble`, `--opt-cross-module`, multi-step compilation, or platform-specific scripting.
- Examples:
  - `tests/mainv1.csproj`
  - `determinism/crossgen2determinism.csproj`
  - `ObjCPInvokeR2R/ObjCPInvokeR2R.csproj`

This pattern is common when a test must stage input assemblies, compile multiple outputs in a specific order, compare multiple generated binaries, or run platform-specific shell logic before execution.

## Map File Validation

When using `--map` to validate R2R compilation:

- **`MethodWithGCInfo`** entries represent actual compiled native code in the ReadyToRun image. This is the signal to use when asserting that a method was precompiled.
- **`MethodFixupSignature`** entries are metadata or signature references. They do not prove that the method body was compiled.
- **`DelayLoadHelperImport`** entries are call-site fixups. They are also metadata-related and are not proof of compilation.

Always check for `MethodWithGCInfo` when asserting that a method was precompiled into the R2R image. `ObjCPInvokeR2R/ObjCPInvokeR2R.cs` and `tests/test.cs` both use this rule when validating map output.

## Version Bubble and `--inputbubble`

R2R compilation outside CoreLib has cross-module reference limitations. The main background is documented in [R2R P/Invoke Design](../../../docs/design/features/readytorun-pinvoke.md).

- P/Invoke stubs that call CoreLib helpers, such as Objective-C pending-exception helpers or `SetLastError` support, require `--inputbubble` so crossgen2 can create fixups for CoreLib methods.
- Without `--inputbubble`, crossgen2 can only reference members that the input assembly already references through existing `MemberRef` tokens in IL metadata.

In practice, this is why tests like `ObjCPInvokeR2R` use manual crossgen2 invocation with `--inputbubble`.

## Platform Gating

Use MSBuild conditions to keep ReadyToRun tests targeted to the environments they actually validate.

- Use `<CLRTestTargetUnsupported Condition="...">true</CLRTestTargetUnsupported>` for platform-specific tests.
- Example: `ObjCPInvokeR2R.csproj` uses `Condition="'$(TargetsOSX)' != 'true'"` because `objc_msgSend` behavior is only relevant on Apple platforms.
- When sanitizers are enabled, crossgen2 tests may need `<CLRTestTargetUnsupported Condition="'$(EnableNativeSanitizers)' != ''">true</CLRTestTargetUnsupported>` or `DisableProjectBuild` to avoid unsupported infrastructure combinations.

Other tests in this directory also gate on `$(RuntimeFlavor)`, target architecture, or 32-bit limitations.

## P/Invoke Detection Tests

When testing platform-specific P/Invoke behavior, validate the exact detection logic used by the type system.

- Check the exact library path and entrypoint constants in `src/coreclr/tools/Common/TypeSystem/Interop/IL/MarshalHelpers.cs`.
- Example: Objective-C detection matches `"/usr/lib/libobjc.dylib"` exactly.
- Using only `"libobjc.dylib"` in a `DllImport` will not trigger the Objective-C-specific code path.

For ObjC-related tests, also verify the expected entrypoint name such as `objc_msgSend`.

## Building and Running

Tests are built and run as part of the CoreCLR test suite:

```bash
# Build all R2R tests
src/tests/build.sh checked -tree:src/tests/readytorun

# Generate Core_Root layout (required for manual runs)
src/tests/build.sh -GenerateLayoutOnly x64 Release

# Run a single test manually
export CORE_ROOT=$(pwd)/artifacts/tests/coreclr/<os>.<arch>.Release/Tests/Core_Root
cd artifacts/tests/coreclr/<os>.<arch>.Debug/readytorun/<TestName>/
$CORE_ROOT/corerun <TestName>.dll
# Exit code 100 = pass
```

Manual-invocation tests often expect the generated `.map` files and any staged IL assemblies to live beside the test output, so run them from the built test directory, not from the source tree.

## Related Documentation

- [R2R P/Invoke Design](../../../docs/design/features/readytorun-pinvoke.md) - version bubble constraints and marshalling pregeneration
- [R2R Composite Format](../../../docs/design/features/readytorun-composite-format-design.md) - composite image design
