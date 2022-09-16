# Disassembly output verification checks
There are tests that the runtime executes that will be able to verify X64/ARM64 assembly output from the JIT.
The tools used to accomplish this are LLVM FileCheck, SuperFileCheck, and the JIT's ability to output disassembly using `DOTNET_JitDisasm`. LLVM FileCheck is built in https://www.github.com/dotnet/llvm-project and provides several packages for the various platforms. See more about LLVM FileCheck and its syntax here: https://llvm.org/docs/CommandGuide/FileCheck.html. SuperFileCheck is a custom tool located in https://www.github.com/dotnet/runtime. It wraps LLVM FileCheck and provides a simplified workflow for writing these tests in a C# file by leveraging Roslyn's syntax tree APIs.
# What is FileCheck?
From https://www.llvm.org/docs/CommandGuide/FileCheck.html:

> **FileCheck** reads two files (one from standard input, and one specified on the command line) and uses one
to verify the other. This behavior is particularly useful for the testsuite, which wants to verify that the
output of some tool (e.g. **llc**) contains the expected information (for example, a movsd from esp or
whatever is interesting). This is similar to using **grep**, but it is optimized for matching multiple
different inputs in one file in a specific order.
# Converting an existing test to use disassembly checking
We will use the existing test `JIT\Regression\JitBlue\Runtime_33972` as an example. The test's intent is to verify that on ARM64, the method `AdvSimd.CompareEqual` behaves correctly when a zero vector is passed as the second argument. Below are snippets of its use:
```csharp
    static Vector64<byte> AdvSimd_CompareEqual_Vector64_Byte_Zero(Vector64<byte> left)
    {
        return AdvSimd.CompareEqual(left, Vector64<byte>.Zero);
    }
...
...
        if (!ValidateResult_Vector64<byte>(AdvSimd_CompareEqual_Vector64_Byte_Zero(Vector64<byte>.Zero), Byte.MaxValue))
            result = -1;
```
Currently, the test only verifies that the behavior is correct. It does not verify that the optimal ARM64 instruction was actually used. So now we will add this verification.
First we need to modify the project file `Runtime_33972.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
  <PropertyGroup>
    <DebugType>None</DebugType>
    <Optimize>True</Optimize>
    <AllowUnsafeBlocks>True</AllowUnsafeBlocks>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="$(MSBuildProjectName).cs" />
  </ItemGroup>
</Project>
```
Looking at the `ItemGroup`:
```xml
  <ItemGroup>
    <Compile Include="$(MSBuildProjectName).cs" />
  </ItemGroup>
```
We want to add `<HasDisasmCheck>true</HasDisasmCheck>` as a child of the `Compile` tag:
```xml
  <ItemGroup>
    <Compile Include="$(MSBuildProjectName).cs">
      <HasDisasmCheck>true</HasDisasmCheck>
    </Compile>
  </ItemGroup>
```
Doing this lets the test builder and runner know that this test has assembly that needs to be verified. Finally, we need to write the assembly check and put the `[MethodImpl(MethodImplOptions.NoInlining)]` attribute on the method `AdvSimd_CompareEqual_Vector64_Byte_Zero`:
```csharp
    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<byte> AdvSimd_CompareEqual_Vector64_Byte_Zero(Vector64<byte> left)
    {
        // ARM64-FULL-LINE: cmeq v0.8b, v0.8b, #0
        return AdvSimd.CompareEqual(left, Vector64<byte>.Zero);
    }
```
And that is it. A few notes about the above example:
- `ARM64-FULL-LINE` checks to see if there is an exact line that matches the disassembly output of the method `AdvSimd_CompareEqual_Vector64_Byte_Zero` - leading and trailing spaces are ignored.
- Method bodies that have FileCheck syntax, e.g. `ARM64-FULL-LINE:`/`X64:`/etc, must have the attribute `[MethodImpl(MethodImplOptions.NoInlining)]`. If it does not, then an error is reported.
- FileCheck syntax outside of a method body will also report an error.
# Additional functionality
LLVM has a different setup where each test file is passed to `lit`, and `RUN:` lines inside the test specify
configuration details such as architectures to run, FileCheck prefixes to use, etc.  In our case, the build
files handle a lot of this with build conditionals and `.cmd`/`.sh` file generation.  Additionally, LLVM tests
rely on the order of the compiler output corresponding to the order of the input functions in the test file.
When running under the JIT, the compilation order is dependent on execution, not the source order.

Functionality that has been added or moved to MSBuild:
- Conditionals controlling test execution
- Automatic specificiation of `CHECK` and `<architecture>` as check prefixes

Functionality that has been added or moved to SuperFileCheck:
- Each function is run under a separate invocation of FileCheck. SuperFileCheck adds additional `CHECK` lines
  that search for the beginning and end of the output for each function. This ensures that output from
  different functions don't contaminate each other. The separate invocations remove any dependency on the
  order of the functions.
- `<check-prefix>-FULL-LINE:` - same as using FileCheck's `<check-prefix>:`, but checks that the line matches exactly; leading and trailing whitespace is ignored.
- `<check-prefix>-FULL-LINE-NEXT:` - same as using FileCheck's `<check-prefix>-NEXT:`, but checks that the line matches exactly; leading and trailing whitespace is ignored.
