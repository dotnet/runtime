---
name: jit-regression-test
description: Extract a standalone JIT regression test case from a given GitHub issue and save it under the JitBlue folder. Use this when asked to create or extract a JIT regression test from an issue.
---

# JIT Regression Test Extraction

When you need to extract a JIT regression test case from a GitHub issue, follow this process to create a properly structured test under `src/tests/JIT/Regression/JitBlue/`.

## Step 1: Gather Information from the GitHub Issue

From the GitHub issue, extract:
1. **Issue number** - Used to name the test folder and files (e.g., issue #99391 → `Runtime_99391`)
2. **Reproduction code** - The C# code that demonstrates the bug. If no code provided, try to compose it yourself.
3. **Environment variables** - Any DOTNET_* environment variables required to reproduce the bug
4. **Expected behavior** - What the correct output/behavior should be

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
- **Test method**: Use `[Fact]` attribute and name the method `TestEntryPoint()` or `Test()`
- **Assertions**: Use Xunit's helpers or just throw plain exceptions.

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

## [Optional] Step 4: Create a .csproj File (Only When Needed)

A custom `.csproj` file is **only required** when:
- Environment variables are needed to reproduce the bug (such as `DOTNET_JitStressModeNames`)
- Special compilation settings are required

It should be located next to the test source file with the following name: `Runtime_<issue_number>.csproj`. Example:

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

## Important Notes

- **No .csproj needed for simple tests**: Most tests only need the `.cs` file. The test infrastructure uses default settings that work for most cases.
- **Look at recent tests**: When in doubt, examine recent tests under `src/tests/JIT/Regression/JitBlue/Runtime_*` for the latest conventions.
- **Use `[MethodImpl(MethodImplOptions.NoInlining)]`**: When you need to prevent inlining to reproduce a JIT bug.
- **Minimize the reproduction**: Strip down the test code to the minimal case that reproduces the issue.
