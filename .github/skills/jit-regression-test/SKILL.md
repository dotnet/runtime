---
name: jit-regression-test
description: >
  Extract a standalone JIT regression test case from a given GitHub issue and
  save it under a JIT regression runner folder. USE FOR: creating JIT regression tests,
  extracting repro code from dotnet/runtime issues, "write a test for this JIT
  bug", "create a regression test for issue #NNNNN", converting issue repro to
  xunit test. DO NOT USE FOR: non-JIT tests (use standard test patterns),
  debugging JIT issues without a known repro, performance benchmarks (use
  performance-benchmark skill).
---

# JIT Regression Test Extraction

> 🚨 **Do NOT create a test when**: the issue has no reproducible code and you cannot compose a minimal repro, the issue is a duplicate of an existing JIT regression test, or the bug is in libraries/runtime rather than the JIT compiler itself.

Extract a JIT regression test case from a GitHub issue into a properly structured test under `src/tests/JIT/Regression_*/`.

## Step 1: Gather Information from the GitHub Issue

From the GitHub issue, extract:
1. **Issue number** → folder/file name (e.g., #99391 → `Runtime_99391`)
2. **Reproduction code** — if none provided, compose a minimal repro yourself
3. **Environment variables** — any `DOTNET_*` vars needed to reproduce
4. **Expected behavior** — correct output/behavior

## Step 2: Choose the Test Location

For a simple test using optimized compilation without debug information, add the source file directly to:

```
src/tests/JIT/Regression_ro_2/Runtime_<issue_number>.cs
```

`Regression_ro_2/Regression_ro_2.csproj` recursively includes sources in its directory. Do not edit its source list or create a project for the test. Use a `Runtime_<issue_number>/` subdirectory only when keeping multiple related files together.

If the test needs its own project (see Step 4), use a separate directory outside the globbed source directories:

```
src/tests/JIT/Regression_2/Runtime_<issue_number>/
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

## Step 4: Create a .csproj File Only When Required

A custom `.csproj` file is **only required** when:
- Environment variables are needed to reproduce the bug (such as `DOTNET_JitStressModeNames`)
- Special compilation settings are required
- Separate assemblies, native dependencies, or process isolation are required

Otherwise, the source glob in `Regression_ro_2.csproj` includes the test automatically. Keep custom projects and their sources under `src/tests/JIT/Regression_2/Runtime_<issue_number>/`. `Regression_2/Regression_2.csproj` discovers those projects recursively without compiling their sources directly. Do not place them in source-glob directories such as `src/tests/JIT/Regression_ro_2/`, where they would also be compiled with the runner's settings.

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

## Tips

- **No .csproj edits needed for simple tests** — add the `.cs` file under `src/tests/JIT/Regression_ro_2/`.
- **Look at recent tests** under `src/tests/JIT/Regression_ro_2/` for simple tests, or `src/tests/JIT/Regression_2/Runtime_*` for tests with custom projects.
