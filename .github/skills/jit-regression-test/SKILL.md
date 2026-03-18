---
name: jit-regression-test
description: >
  Extract a standalone JIT regression test case from a given GitHub issue and
  save it under the JitBlue folder. USE FOR: creating JIT regression tests,
  extracting repro code from dotnet/runtime issues, "write a test for this JIT
  bug", "create a regression test for issue #NNNNN", converting issue repro to
  xunit test. DO NOT USE FOR: non-JIT tests (use standard test patterns),
  debugging JIT issues without a known repro, performance benchmarks (use
  performance-benchmark skill).
---

# JIT Regression Test Extraction

> 🚨 **Do NOT create a test when**: the issue has no reproducible code and you cannot compose a minimal repro, the issue is a duplicate of an existing test under `JitBlue/`, or the bug is in libraries/runtime rather than the JIT compiler itself.

Extract a JIT regression test case from a GitHub issue into a properly structured test under `src/tests/JIT/Regression/JitBlue/`.

## Step 1: Gather Information from the GitHub Issue

From the GitHub issue, extract:
1. **Issue number** → folder/file name (e.g., #99391 → `Runtime_99391`)
2. **Reproduction code** — if none provided, compose a minimal repro yourself
3. **Environment variables** — any `DOTNET_*` vars needed to reproduce
4. **Expected behavior** — correct output/behavior

## Step 2: Create the Test Directory

Create a new folder under `src/tests/JIT/Regression/JitBlue/`:

```
src/tests/JIT/Regression/JitBlue/Runtime_<issue_number>/
```

## Step 3: Create the Test File

Create a `Runtime_<issue_number>.cs` file following these conventions:

Example:

```csharp
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_<issue_number>
{
    [Fact]
    public static void TestEntryPoint()
    {
        // Test code that exercises the bug
        // Use Assert.Equal, Assert.True, etc. for validation
    }
}
```

### Key Conventions

- **License header**: Always include the standard .NET Foundation license header
- **Class name**: Match the file name exactly (`Runtime_<issue_number>`)
- **Test method**: `[Fact]` attribute, named `TestEntryPoint()`
- **Minimize the reproduction**: Strip to the minimal case that triggers the bug
- **Use `[MethodImpl(MethodImplOptions.NoInlining)]`** when preventing inlining is needed to reproduce

### Example: Simple Test (from Runtime_99391)

```csharp
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


namespace Runtime_99391;

using System;
using System.Runtime.CompilerServices;
using System.Numerics;
using Xunit;

public class Runtime_99391
{
    [Fact]
    public static void TestEntryPoint()
    {
        Vector2 result2a = Vector2.Normalize(Value2);
        Assert.Equal(new Vector2(0, 1), result2a);
    }

    private static Vector2 Value2
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        get => new Vector2(0, 2);
    }
}
```

## Step 4: Create a .csproj File or add to the existing Regression_*.csproj

A custom `.csproj` file is **only required** when:
- Environment variables are needed to reproduce the bug (such as `DOTNET_JitStressModeNames`)
- Special compilation settings are required

Otherwise, register the test file in the existing `src/tests/JIT/Regression/Regression_*.csproj` (`Regression_ro_2.csproj` is a good default) file and skip creating a new .csproj.

If a custom .csproj file is needed, it should be located next to the test source file with the following name: `Runtime_<issue_number>.csproj`. Example:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <Optimize>True</Optimize>
    <DebugType>None</DebugType>
    <!-- Needed for CLRTestEnvironmentVariable -->
    <RequiresProcessIsolation>true</RequiresProcessIsolation>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="$(MSBuildProjectName).cs" />

    <CLRTestEnvironmentVariable Include="DOTNET_TieredCompilation" Value="0" />
  </ItemGroup>
</Project>
```

## Step 5: ⚠️ Verify Test Correctness (Mandatory)

A regression test is only valid if it **fails without the fix** and **passes with the fix**. A test that passes in both cases is worthless — it does not actually test the bug.

1. **Verify the test FAILS without the fix:**
   - If you have a baseline build from `main` (before the fix), use those artifacts to run the test.
   - The test should fail (non-zero exit code, assertion failure, or incorrect output).
   - If the test passes without the fix, the test is wrong — revisit your assertions.

2. **Verify the test PASSES with the fix:**
   - Build and run with the fix applied.
   - The test should pass (exit code 100 for CoreCLR tests, or all assertions green for xUnit tests).

Do not consider the test complete until both conditions are confirmed. A first green run is not evidence the test is valid — it may be a false positive caused by incorrect assertions or test inputs that don't exercise the bug.

## Tips

- **No .csproj needed for simple tests** — register the `.cs` file in `Regression_ro_2.csproj` instead.
- **Look at recent tests** under `src/tests/JIT/Regression/JitBlue/Runtime_*` when in doubt about current conventions.
