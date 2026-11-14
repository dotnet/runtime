# DBI value tests

The opt-in `dbi_value_tests` target (Windows only) links the production DBI
libraries against a mock DAC and data target. Each case runs with both 4-byte
and 8-byte target pointers, and guard pages check that target-sized buffers are
not overread or overwritten. These are unit tests, not live debugger integration
tests.

Return-value eligibility cases cover integer, mixed integer/floating-point, and
all-floating-point register pairs. The x86 host rejects floating-point pairs for
64-bit targets because its register-pair decoder does not support them.

After building CoreCLR, run the following from a Visual Studio developer
PowerShell. Use both the x64 and x86 build directories to cover both host
pointer sizes.

```powershell
$nativeBuildDir = 'artifacts\obj\coreclr\windows.x64.Debug'
cmake --build $nativeBuildDir --target dbi_value_tests
ctest --test-dir "$nativeBuildDir\dlls\mscordbi\tests" --output-on-failure
```

Use CTest's `-R` option to select cases, for example
`-R 'dbi_value_ReferenceAssignment_4$'` for reference assignment with 32-bit
target pointers.
