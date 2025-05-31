---
mode: 'agent'
tools: ['fetch', 'codebase', 'terminalLastCommand']
description: 'Add a new JIT regression test from dotnet/runtime github issue'
---

#### 1 — Goal

Implement **one** new JIT regression test based on a provided GitHub issue.

#### 2 — Prerequisites for the model

* You have full repo access
* Ask **clarifying questions** if anything is unclear.

#### 3 — Required user inputs

Ask the user for github issue URL or number if it's not provided.
Examples: `https://github.com/dotnet/runtime/issues/116159` or `116159`.or `#116159`.

#### 4 — Implementation steps (must be completed in order)

1. Fetch the issue details from the provided GitHub URL or number (it is always in the `dotnet/runtime` repository).
Extract the repro code snippet (if available, otherise try to construct it from the issue description) and any 
relevant information about the test, such as environment variables.
2. Create the directory structure for the new test:
   * `<repo_root>/src/tests/JIT/Regression/JitBlue/Runtime_<issue_number>/`
3. Add a new C# project file named `Runtime_<issue_number>.csproj` in that directory with the following content:

  ```xml
  <Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
      <Optimize>True</Optimize>
    </PropertyGroup>
    <ItemGroup>
      <Compile Include="$(MSBuildProjectName).cs" />
    </ItemGroup>
  </Project>
  ```

  If the test mentions environment variables (typically prefixed with `DOTNET_`), add the following ItemGroup csproj file:

  ```xml
    <ItemGroup>
      <CLRTestEnvironmentVariable Include="DOTNET_VAR1" Value="value1" />
      <CLRTestEnvironmentVariable Include="DOTNET_VAR2" Value="value2" />
    </ItemGroup>
  ```

  environment variablies require the following property:

  ```xml
    <PropertyGroup>
      <!-- Needed for CLRTestEnvironmentVariable -->
      <RequiresProcessIsolation>true</RequiresProcessIsolation>
    </PropertyGroup>
  ```
  Target framework should never be specified, it will be set automatically.

4. Add a new C# file named `Runtime_<issue_number>.cs` in that directory with the repo code snippet extracted from the issue in the step 1. The file should include the necessary namespaces and a test method. Use the following template:

```csharp
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class Runtime_<issue_number>
{
    [Fact]`
    public static void Problem()
    {
    }
}
```

The test should include the license header, the Fuzzlyn info (if available), and at least one public static test method with an xunit attribute (e.g., `[Fact]`).
Do not attempt to build the test.

#### 5 — Definition of Done (self-check list)

* [ ] Each source file changed exactly once; no unrelated edits. The following files must be added:
   * `<repo_root>/src/tests/JIT/Regression/JitBlue/Runtime_<issue_number>/Runtime_<issue_number>.cs`
   * `<repo_root>/src/tests/JIT/Regression/JitBlue/Runtime_<issue_number>/Runtime_<issue_number>.csproj`
* [ ] No extra files such as solution files, readme files, or other project files are added.
* [ ] The test must be a valid C# file with at least one test method.
