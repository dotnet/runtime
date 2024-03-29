# Viewing JIT disassembly and dumps

This document is intended for people interested in seeing the disassembly, GC info, or other details the JIT generates for a managed program.

Some JIT output is available in the shipped product (aka the Release build), while some requires a "Debug" or "Checked" build of the runtime.

# Setting up our environment

The first thing to do is setup the .NET Core app we want to dump. Here are the steps to do this, if you don't have one ready:

* Install the [.NET SDK](https://dotnet.microsoft.com/en-us/).
* `cd` to where you want your app to be placed, and run `dotnet new console`.

* Edit `Program.cs`, and call the method(s) you want to dump in there. Make sure they are, directly or indirectly,
called from `Main`. In this example, we'll be looking at the disassembly of our custom function `InefficientJoin`:

    ```cs
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    namespace ConsoleApplication
    {
        public class Program
        {
            public static void Main(string[] args)
            {
                Console.WriteLine(InefficientJoin(args));
            }

            // Add NoInlining to prevent this from getting
            // mixed up with the rest of the code in Main
            [MethodImpl(MethodImplOptions.NoInlining)]
            private static string InefficientJoin(IEnumerable<string> args)
            {
                var result = string.Empty;
                foreach (var arg in args) result += (arg + ' ');
                return result.Substring(0, Math.Max(0, result.Length - 1));
            }
        }
    }
    ```

* Set the configuration variables you need (see below) and run your app. For example:

    ```
    C:\test>set DOTNET_JitDisasm=InefficientJoin
    C:\test>dotnet run -c Release
    ; Assembly listing for method ConsoleApplication.Program:InefficientJoin(System.Collections.Generic.IEnumerable`1[System.String]):System.String (Instrumented Tier0)
    ; Emitting BLENDED_CODE for X64 with AVX512 - Windows
    ; Instrumented Tier0 code
    ; rbp based frame
    ; fully interruptible

    G_M000_IG01:                ;; offset=0x0000
        push     rbp
        sub      rsp, 208
        lea      rbp, [rsp+0xD0]
        xor      eax, eax
        mov      qword ptr [rbp-0x98], rax
        vxorps   xmm4, xmm4, xmm4
        vmovdqa  xmmword ptr [rbp-0x90], xmm4
        vmovdqa  xmmword ptr [rbp-0x80], xmm4
        vmovdqa  xmmword ptr [rbp-0x70], xmm4
        vmovdqa  xmmword ptr [rbp-0x60], xmm4
        vmovdqa  xmmword ptr [rbp-0x50], xmm4
        mov      qword ptr [rbp-0x40], rax
        mov      qword ptr [rbp-0xB0], rsp
        mov      gword ptr [rbp+0x10], rcx

    G_M000_IG02:                ;; offset=0x0048
        mov      rcx, 0x22680000008
        mov      gword ptr [rbp-0x40], rcx
        mov      rcx, gword ptr [rbp+0x10]
        mov      gword ptr [rbp-0x58], rcx
        mov      rcx, gword ptr [rbp-0x58]
        mov      rdx, 0x7FF8E62CF8B0
        call     CORINFO_HELP_CLASSPROFILE32
        mov      rcx, gword ptr [rbp-0x58]
        mov      gword ptr [rbp-0x80], rcx
        mov      rcx, gword ptr [rbp-0x80]
        mov      r11, 0x7FF8E5FB0040
        call     [r11]System.Collections.Generic.IEnumerable`1[System.__Canon]:GetEnumerator():System.Collections.Generic.IEnumerator`1[System.__Canon]:this
        mov      gword ptr [rbp-0x48], rax
        mov      dword ptr [rbp-0x78], 0x3E8   
    ...
    ```

Note that `dotnet run` runs quite a lot of code, such as msbuild, nuget, the Roslyn compiler, etc. The environment variables
will apply to all of them. Thus, it might be preferable to first build the application using `dotnet build` or `dotnet publish`
and then set the configuration variables and run the program.

If you want to use a Debug or Checked build of the JIT, to get access to configuration variables only available
in those build flavors, you will need to build your own version of the runtime repo. See instructions
[here](https://github.com/dotnet/runtime/blob/main/docs/workflow/README.md).

After building the repo, you may also want to use the "Dogfooding daily builds of .NET" instructions
[here](https://github.com/dotnet/runtime/blob/main/docs/project/dogfooding.md). However, you may also
be able to run tests just using the built corerun.exe tool.

Ideally, install a dogfood build as described above. Then, build both Debug and Release versions of the
repo. You will use the Release version for everything except the JIT.

* Create a test program using `dotnet new console` as described above.
* Modify your `csproj` file so that it contains a RID (runtime ID) corresponding to the OS you're using in the `<RuntimeIdentifier>` tag.
For example, for Windows x64 machine, the project file is:

    ```xml
    <Project Sdk="Microsoft.NET.Sdk">
      <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net8.0</TargetFramework>
        <RuntimeIdentifier>win-x64</RuntimeIdentifier>
      </PropertyGroup>
    </Project>
    ```

   You can find a list of RIDs and their corresponding OSes [here](https://docs.microsoft.com/en-us/dotnet/articles/core/rid-catalog).

* After you've finished editing the code, run `dotnet restore` and `dotnet publish -c Release`. This should drop all of the binaries needed to run your app in `bin/Release/<tfm>/<rid>/publish`.
* Overwrite the CLR dlls with the ones you've built locally. If you're a fan of the command line, here are some shell commands for doing this:

    ```shell
    # Windows
    robocopy /e <runtime-repo path>\artifacts\bin\coreclr\windows.<arch>.Release <app root>\bin\Release\<tfm>\<rid>\publish > NUL
    copy /y <runtime-repo path>\artifacts\bin\coreclr\windows.<arch>.Debug\clrjit.dll <app root>\bin\Release\<tfm>\<rid>\publish > NUL

    # Unix
    cp -rT <runtime-repo path>/artifacts/bin/coreclr/<OS>.<arch>.Release <app root>/bin/Release/<tfm>/<rid>/publish
    cp <runtime-repo path>/artifacts/bin/coreclr/<OS>.<arch>.Debug/libclrjit.so <app root>/bin/Release/<tfm>/<rid>/publish
    ```

# Setting configuration variables

The behavior of the JIT can be controlled via a number of configuration variables.
These are declared in [jit/jitconfigvalues.h](/src/coreclr/jit/jitconfigvalues.h). However, some configuration variables
are read and processed by the VM instead of the JIT; these are specified in [inc/clrconfigvalues.h](/src/coreclr/inc/clrconfigvalues.h).
The configuration string name generally has `DOTNET_` prepended.

The configuration variables are generally set as environment variables, using the name `DOTNET_<name>`.
For example, the following will set the `JitDisasm` flag so that the disassembly of all methods named `Main` will be displayed:

   ```shell
   # Windows
   set DOTNET_JitDisasm=Main

   # Powershell
   $env:DOTNET_JitDisasm="Main"

   # Unix
   export DOTNET_JitDisasm=Main
   ```

Specifying a JIT configuration variable to crossgen2 (ReadyToRun) or ilc (NativeAOT) requires passing the JIT configuration
on the command-line using the `--codegenopt` switch; it cannot be specified using an environment variable.
For more information, see [debugging-aot-compilers](/docs/workflow/debugging/coreclr/debugging-aot-compilers.md).

Also, JIT developers using superpmi.exe pass JIT a configuration variable via the `-jitoption` / `-jit2option` switches,
and to superpmi.py using the `-jitoption` / `-base_jit_option` / `-diff_jit_option` switches. In each case, the variable
is passed without the `DOTNET_` prefix.

A configuration variable is either a string, an integer (often 0 meaning "false" or "off" and 1 meaning "true" or "on"), or a method (function) list.
Note that integers are interpreted as hexadecimal numbers. Specifying method lists is described in the next section.

If a variable is not specified, a default is used. Typically, that default is interpreted as "off/disabled" or "false", and for variables
that take a method list, the default is that no method is specified.

## Specifying method names

Some environment variables such as `DOTNET_JitDisasm` take a list of patterns specifying method names. The matching works in the following way:
* A method list string is a space-separated list of patterns.
  + The simplest method list is a single method name specified using just the method name (no class name), e.g. `Main`.
  + A list of simple method names can be used, e.g., `Main Test1 Test2`.
* The string matched against depends on characters in the pattern:
  + If the pattern contains a ':' character, the string matched against is prefixed by the class name and a colon.
    - Example: `TestClass:Main` - specifies a single method named `Main` in the class named `TestClass`.
  + If the pattern contains a '(' character, the string matched against is suffixed by the signature.
  + If the class name (part before colon) contains a '[', the class contains its generic instantiation.
  + If the method name (part between colon and '(') contains a '[', the method contains its generic instantiation.
* Patterns can contain arbitrary uses of two different wildcards: '*' (match any characters) and '?' (match any 1 character).
  + The simplest and most commonly used pattern is just `*`, which means "all methods", e.g., `DOTNET_JitDisasm=*`.
  + Example: `TestClass:*` - specifies all methods in class named `TestClass`.
  + Example: `*Main*` - specifies all methods with `Main` as a substring of their name (including a method named exactly `Main`).

In particular, the matching is done against strings of the following format which coincides with how the
JIT displays method signatures (so these can be copy pasted into the environment variable).
```
[ClassName[Instantiation]:]MethodName[Instantiation][(<types>)]
```

For example, consider the following:
```csharp
namespace MyNamespace
{
    public class C<T1, T2>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void M<T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
        }
    }
}

new C<sbyte, string>().M<int, object>(default, default, default, default); // compilation 1
new C<int, int>().M<int, int>(default, default, default, default); // compilation 2
```

The full names of these instantiations are the following, as printed by `DOTNET_JitDisasmSummary`:

```
MyNamespace.C`2[byte,System.__Canon]:M[int,System.__Canon](byte,System.__Canon,int,System.__Canon)
MyNamespace.C`2[int,int]:M[int,int](int,int,int,int)
```
Note that ``C`2`` here is the name put into metadata by Roslyn; the suffix is not added by RyuJIT.
For Powershell users keep in mind that backtick is the escape character and itself has to be escaped via double backtick.

The following strings will match both compilations:
```
M
*C`2:M
*C`2[*]:M[*](*)
MyNamespace.C`2:M
```

The following match only the first compilation:
```
M[int,*Canon]
MyNamespace.C`2[byte,*]:M
M(*Canon)
```

# Disasmo

One easy way to view JIT generated disassembly and other output without using the command-line
is to use the Visual Studio [Disasmo](https://github.com/EgorBo/Disasmo) plugin.

# Disassembly output

The following set of JIT configuration variables are available in both the shipped product and in internal Debug/Checked builds.

* `DOTNET_JitDisasmSummary`={1 or 0} - set to 1 to print a list of all JIT compiled functions.
* `DOTNET_JitDisasm`={method-list} - output disassembly for the specified functions.
E.g., `DOTNET_JitDisasm=Main`, `DOTNET_JitDisasm=Main Test1 Test2`, `DOTNET_JitDisasm=*` (for all functions).
* `DOTNET_JitDisasmDiffable`={1 or 0} - set to 1 to make the generated code "diff-able", namely, replace pointer values in the output with
the same well-known, identical values, so they textually compare identically.
* `DOTNET_JitDisasmWithAlignmentBoundaries`={1 or 0} - set to 1 to display alignment boundaries in the generated code.
* `DOTNET_JitDisasmOnlyOptimized`={1 or 0} - set to 1 to hide disasm for unoptimized code
* `DOTNET_JitDisasmWithCodeBytes`={1 or 0} - set to 1 to display the actual code bytes in addition to textual disassembly. (Don't use
if `DOTNET_JitDisasmDiffable=1`.)
* `DOTNET_JitStdOutFile`={file name} - if not set, all JIT output goes to standard output. If set, it is the name of a file to which JIT output
will be written.

## Disassembly options only in Debug/Checked builds

The following configuration variables related to disassembly output are only available in Debug/Checked builds.

* `DOTNET_JitDisasmWithGC`={1 or 0} - Set to 1 to display GC information interleaved with the textual disassembly.
* `DOTNET_JitDisasmWithDebugInfo`={1 or 0} - Set to 1 to display debug information (variable live ranges)
interleaved with the textual disassembly.
* `DOTNET_JitDisasmAssemblies`={assembly list} - Specify a semicolon-separated list of assembly names. JitDisasm and other JIT output
will only apply to functions in these assemblies. E.g., `DOTNET_JitDisasmAssemblies=MyAssembly1;MyAssembly2`.

# Note about multi-threaded applications

JIT output is not synchronized when multiple threads are running. That means that if you set `DOTNET_JitDisasm=*`, and multiple functions are being
simultaneously compiled on multiple threads, the output will be interleaved (and most likely will be incomprehensible). To avoid this, run the test case
with a single thread, if possible; use the `DOTNET_JitStdOutFile` option to write output to a file instead of standard output (which might interleave JIT
output with program output); and restrict the method list specified to just one function.

# Miscellaneous always-available configuration options

* `DOTNET_JitTimeLogFile`={file name} – specify a log file to which timing information is written.
* `DOTNET_JitTimeLogCsv`={file name} – specify a log file to which summary timing information is written, in CSV form.

# The JIT late disassembler

The disassembly displayed by `DOTNET_JitDisasm` is printed based on a JIT internal representation. An additional disassembler, called
the "late disassembler", is available that disassembles the final code bytes. The late disassembler uses the coredistools package
to interpret the code bytes. In this way, it provides a way to verify that the JIT disassembly matches the disassembly produced
by a third-party disassembler for a particular set of code bytes.

The late disassembler currently is only available in Debug/Checked builds.

(Note: coredistools is curently version 1.4.0, based on LLVM 17.0.6. The source code is [here](https://github.com/dotnet/jitutils)).

To invoke the late disassembler, use:
* `DOTNET_JitLateDisasm`={method-list} - output late disassembly for the specified functions. E.g., `DOTNET_JitLateDisasm=Main`,
`DOTNET_JitLateDisasm=Main Test1 Test2`, `DOTNET_JitLateDisasm=*`.
* `DOTNET_JitLateDisasmTo`={file name} - (Optional) specify a file name to which late disassembly output is written.

Late disassembly output is sent to the first of these locations:
* file specified by `DOTNET_JitLateDisasmTo`, if set.
* file specified by `DOTNET_JitStdOutputFile`, if set.
* standard output.

## Using the late disassembler with the emitter unit tests

One use for the late disassembler is when adding new instructions to the JIT emitter, assuming coredistools already knows about those
new instructions. For example, when adding Arm64 SVE instructions, new unit tests for each new instruction are added to
`CodeGen::genEmitterUnitTests()` (or a function called by that). Then, create a "Hello World" program with a `Main` function and set:

* `DOTNET_JitDisasm=Main`
* `DOTNET_JitLateDisasm=Main`
* `DOTNET_JitLateDisasmTo=latedis.asm` - optionally send `JitLateDisasm` output to a separate file.
* `DOTNET_JitEmitUnitTests=Main`
* `DOTNET_JitEmitUnitTestsSections=sve`

With these environment variables set, run the program. The `Main` function will be populated with a lot of additional
"unit test" instructions, which will be output as disassembly by two methods: the built-in `JitDisasm` disassembly
printer, and the coredistools `JitLateDisasm` disassembler. These can be compared to see if they match.

In addition, you can set:

* `DOTNET_JitRawHexCode=Main`
* `DOTNET_JitRawHexCodeFile=rawhex.txt` - optionally specify an output file, otherwise the code will be output to the `DOTNET_JitStdOutFile`
location, or standard output.

Then, use an external disassembler that reads the written textual hex coded bytes (e.g., `F3E4C10F`, etc.) and disassembles them
to textual disassembly form, which can be compared against the `JitDisasm` and/or `JitLateDisasm` output, for example
[capstone](https://github.com/TIHan/capstone).

## Using emitter unit tests with SuperPMI

Since it doesn't really matter which function gets compiled when using the unit tests, one option is to choose (or create) a
SuperPMI MCH file, then just compile a single function with the unit tests enabled. For example:

```
superpmi.exe -c 1 -target arm64 -jitoption JitDisasm=* -jitoption JitLateDisasm=* -jitoption JitEmitUnitTests=* -jitoption JitEmitUnitTestsSections=sve benchmarks.run.windows.arm64.checked.mch
```

This is useful when using the cross-compilers. Using `DOTNET_AltJit`/`DOTNET_AltJitName` is another option for generating
disassembly using a cross-compiler.

# JIT dump

A JIT dump is a verbose display of JIT internal data structures and actions during compilation, used by JIT developers. It is only available
in Debug/Checked builds of the JIT. See [Reading a JitDump](ryujit-overview.md#reading-a-jitdump) for more on how to analyze this output.

Here are some variables that control JIT dump output:

* `DOTNET_JitDump`={method-list} – enable the JIT dump for the specified functions.
* `DOTNET_JitDumpASCII`={1 or 0} - specify whether the JIT dump should be ASCII only (defaults to 1). Disabling this generates more readable expression trees.
* `DOTNET_JitGCDump`={method-list} – dump the GC information.
* `DOTNET_JitUnwindDump`={method-list} – dump the unwind tables.
* `DOTNET_JitEHDump`={method-list} – dump the exception handling tables.

# Dumping native images

If you followed the tutorial above and ran the sample app, you may be wondering why the disassembly for methods like
`Substring` didn't show up in the output. This is because `Substring` lives in System.Private.CoreLib.dll,
which (by default) is compiled ahead-of-time via crossgen2. Telling crossgen2 to dump the info works slightly
differently in that it has to be specified on the command line instead. In particular, you need to use the
`--codegenopt` argument using the configuration name *without* the `DOTNET_` prefix.

For more information, see [debugging-aot-compilers](/docs/workflow/debugging/coreclr/debugging-aot-compilers.md).