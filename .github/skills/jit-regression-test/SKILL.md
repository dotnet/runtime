---
name: jit-regression-test
description: Extract a standalone JIT regression test case from a given GitHub issue and save it under the JitBlue folder. Use this when asked to create or extract a JIT regression test from an issue.
---

# JIT Regression Test Extraction

When you need to extract a JIT regression test case from a GitHub issue, follow this process to create a properly structured test under `src/tests/JIT/Regression/JitBlue/`.

## Step 1: Gather Information from the GitHub Issue

From the GitHub issue, extract:
1. **Issue number** - Used to name the test folder and files (e.g., issue #99391 → `Runtime_99391`)
2. **Reproduction code** - The C# code that demonstrates the bug
3. **Environment variables** - Any DOTNET_* environment variables required to reproduce the bug
4. **Expected behavior** - What the correct output/behavior should be

## Step 2: Create the Test Directory

Create a new folder under `src/tests/JIT/Regression/JitBlue/`:

```
src/tests/JIT/Regression/JitBlue/Runtime_<issue_number>/
```

## Step 3: Create the Test File

Create a `Runtime_<issue_number>.cs` file following these conventions:

### Basic Structure

```csharp
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


namespace Runtime_<issue_number>;

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
- **Namespace**: Use `Runtime_<issue_number>;` with file-scoped namespace
- **Using directives**: Place `using` statements after the namespace declaration
- **Class name**: Match the file name exactly (`Runtime_<issue_number>`)
- **Test method**: Use `[Fact]` attribute and name the method `TestEntryPoint()` or `Test()`
- **Return type**: Two options:
  - `void` with Assert methods (preferred for most cases)
  - `int` returning 100 for success, any other value for failure (legacy pattern)
- **Assertions**: Prefer `Assert.Equal`, `Assert.True`, `Assert.False` from Xunit

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

### Example: Test with Return Code (from Runtime_97625)

```csharp
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


namespace Runtime_97625;

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

public static class Runtime_97625
{
    public class CustomModel
    {
        public decimal Cost { get; set; }
    }

    [Fact]
    public static int Test()
    {
        List<CustomModel> models = new List<CustomModel>();
        models.Add(new CustomModel { Cost = 1 });
        return models.Average(x => x.Cost) == 1 ? 100 : -1;
    }
}
```

## Step 4: Create a .csproj File (Only When Needed)

A custom `.csproj` file is **only required** when:
- Environment variables are needed to reproduce the bug
- Unsafe code is used (`AllowUnsafeBlocks`)
- Special compilation settings are required

### When Environment Variables Are Required

If the issue mentions environment variables like `DOTNET_TieredCompilation=0`, `DOTNET_JitStressModeNames`, etc., create a `Runtime_<issue_number>.csproj` file:

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

### Common Environment Variable Patterns

| Scenario | Environment Variables |
|----------|----------------------|
| Disable tiered compilation | `DOTNET_TieredCompilation=0` |
| Enable tiered PGO | `DOTNET_TieredCompilation=1`, `DOTNET_TieredPGO=1` |
| Disable hardware intrinsics | `DOTNET_EnableHWIntrinsic=0` |
| JIT stress mode | `DOTNET_JitStressModeNames=STRESS_*` |

### When Unsafe Code Is Used

If the test uses `unsafe` blocks or pointers:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <Optimize>True</Optimize>
    <DebugType>None</DebugType>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="$(MSBuildProjectName).cs" />
  </ItemGroup>
</Project>
```

### Example: Full .csproj with Environment Variables (from Runtime_95315)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <Optimize>True</Optimize>
    <DebugType>None</DebugType>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <!-- Needed for CLRTestEnvironmentVariable -->
    <RequiresProcessIsolation>true</RequiresProcessIsolation>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="$(MSBuildProjectName).cs" />

    <CLRTestEnvironmentVariable Include="DOTNET_TieredCompilation" Value="1" />
    <CLRTestEnvironmentVariable Include="DOTNET_TieredPGO" Value="1" />
  </ItemGroup>
</Project>
```

## Step 5: Verify the Test

After creating the test files, verify they compile and run:

1. Navigate to the test directory
2. Build the test: `dotnet build`
3. Run the test: `dotnet build /t:test`

## Important Notes

- **No .csproj needed for simple tests**: Most tests only need the `.cs` file. The test infrastructure uses default settings that work for most cases.
- **Look at recent tests**: When in doubt, examine recent tests under `src/tests/JIT/Regression/JitBlue/Runtime_*` for the latest conventions.
- **Use `[MethodImpl(MethodImplOptions.NoInlining)]`**: When you need to prevent inlining to reproduce a JIT bug.
- **Minimize the reproduction**: Strip down the test code to the minimal case that reproduces the issue.

## Test Location

All tests should be placed in:
```
src/tests/JIT/Regression/JitBlue/Runtime_<issue_number>/
```

The final structure should be:
```
Runtime_<issue_number>/
├── Runtime_<issue_number>.cs
└── Runtime_<issue_number>.csproj  (optional, only if needed)
```
