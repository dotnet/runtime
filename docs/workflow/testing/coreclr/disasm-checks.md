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
# Test Run Limitations
1. Disasm checks will not run if these environment variables are set:
- `DOTNET_JitStress`
- `DOTNET_JitStressRegs`
- `DOTNET_TailcallStress`
- `DOTNET_TieredPGO`
2. Disasm checks will not run under GCStress test modes.
3. Disasm checks will not run under heap-verify test modes.
4. Disasm checks will not run under cross-gen2 test modes.
# Method Disassembly Limitations
There are a few limitations when using FileChecked methods that the user should be aware of:
1. Local functions are not supported.
2. Conditional defines are not recognized by the `.csproj`.
3. Overloaded methods are not supported. Snippet below will not work:
```csharp
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Test(int x)
    {
        // CHECK: ...
        return 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static float Test(float x, float y)
    {
        // CHECK: ...
        return 1;
    }
```
4. Using a FileChecked generic method with different type arguments may result in ambiguity when FileCheck looks at the disassembly output. The snippet below will work, but the test itself is brittle and may fail:
```csharp
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Test<T>()
    {
        // CHECK: ...
        (implementation)
    }

    static int Main(string args[])
    {
        // Disassembly output will have two specialized methods, each with different codegen that
        // is ambiguous with our CHECK:
        Test<float>();
        Test<int>();
        return 200;
    }
```
The reason for these limitations are that SuperFileCheck only relies on the C# syntax tree. In the future, it may be possible to get the semantic model that will allow getting an accurate method signature, complete with types. However, it is non-trivial to resolve all required assemblies from the `.csproj` and feed them into C#'s compilation - it also adds a performance cost when using a full compilation compared to just the syntax tree.
# Future Improvements
- SuperFileCheck supports writing FileChecked methods where the methods are in any order. It can do this by determining the *start* and *end* "anchors" of the JIT disassembly output. However, these anchors are not necessarily standardized and changes to its current output would break disasm check tests. We should improve this by being very explicit with the output of the anchors. Below is an example of what the current anchor output is today:
```
; Assembly listing for method Program:PerformMod_1(uint):uint     <-- start anchor
.......
; Total bytes of code 6, prolog size 0, PerfScore 2.10, instruction count 3, allocated bytes for code 6 (MethodHash=e2c7b489) for method Program:PerformMod_1(uint):uint     <-- end anchor
```
- SuperFileCheck does not use a command line library today due to it being so minimal and most of the heavy lifting is done passing the arguments to FileCheck itself. As SuperFileCheck continues to grow, we will need to use a command line library, such as System.CommandLine.
- Support various JIT test modes to allow testing codegen under specific scenarios. (Note: these can already be partially done by setting environment variables (like `DOTNET_JITMinOpts`) in the test itself.)
- JIT IR Testing - we may want to allow testing against certain phases of a method by looking at the IR. There are a lot of unknowns surrounding this, but it would be useful to have a prototype.
