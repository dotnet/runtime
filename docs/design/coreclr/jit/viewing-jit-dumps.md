# Viewing JIT Dumps

This document is intended for people interested in seeing the disassembly, GC info, or other details the JIT generates for a managed program.

To make sense of the results, it is recommended you also read the [Reading a JitDump](ryujit-overview.md#reading-a-jitdump) section of the RyuJIT Overview.

## Setting up our environment

The first thing to do is setup the .NET Core app we want to dump. Here are the steps to do this, if you don't have one ready:

* Cd into `src/coreclr`
* Perform a release build of the runtime by passing `release` to the build command. You don't need to build tests, so you can pass `skiptests` to the build command to make it faster. Note: the release build can be skipped, but in order to see optimized code of the core library it is needed.
* Perform a debug build of the runtime. Tests aren't needed as in the release build, so you can pass `skiptests` to the build command. Note: the debug build is necessary, so that the JIT recognizes the configuration knobs.
* Install the (latest) [.NET CLI](https://github.com/dotnet/runtime/blob/main/docs/project/dogfooding.md), which we'll use to compile/publish our app.
* `cd` to where you want your app to be placed, and run `dotnet new console`.
* Modify your `csproj` file so that it contains a RID (runtime ID) corresponding to the OS you're using in the `<RuntimeIdentifier>` tag. For example, for Windows 10 x64 machine, the project file is:

    ```xml
    <Project Sdk="Microsoft.NET.Sdk">

      <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net6.0</TargetFramework>
        <RuntimeIdentifier>win-x64</RuntimeIdentifier>
      </PropertyGroup>

    </Project>
    ```

   You can find a list of RIDs and their corresponding OSes [here](https://docs.microsoft.com/en-us/dotnet/articles/core/rid-catalog).

* Edit `Program.cs`, and call the method(s) you want to dump in there. Make sure they are, directly or indirectly, called from `Main`. In this example, we'll be looking at the disassembly of our custom function `InefficientJoin`:

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

* Set the configuration knobs you need (see below) and run your published app. The info you want should be dumped to stdout.

   Here's some sample output on my machine showing the disassembly for `InefficientJoin`:

   ```asm
   G_M2530_IG01:
          55                   push     rbp
          4883EC40             sub      rsp, 64
          488D6C2440           lea      rbp, [rsp+40H]
          33C0                 xor      rax, rax
          488945F8             mov      qword ptr [rbp-08H], rax
          488965E0             mov      qword ptr [rbp-20H], rsp

   G_M2530_IG02:
          49BB60306927E5010000 mov      r11, 0x1E527693060
          4D8B1B               mov      r11, gword ptr [r11]
          4C895DF8             mov      gword ptr [rbp-08H], r11
          49BB200058F7FD7F0000 mov      r11, 0x7FFDF7580020
          3909                 cmp      dword ptr [rcx], ecx
          41FF13               call     gword ptr [r11]System.Collections.Generic.IEnumerable`1[__Canon][System.__Canon]:GetEnumerator():ref:this
          488945F0             mov      gword ptr [rbp-10H], rax

   ; ...
   ```

## Setting configuration variables

The behavior of the JIT can be controlled via a number of configuration variables. These are declared in [inc/clrconfigvalues.h](/src/coreclr/inc/clrconfigvalues.h) and [jit/jitconfigvalues.h](/src/coreclr/jit/jitconfigvalues.h). When used as an environment variable, the string name generally has `COMPlus_` prepended. When used as a registry value name, the configuration name is used directly.

These can be set in one of three ways:

* Setting the environment variable `COMPlus_<flagname>`. For example, the following will set the `JitDump` flag so that the compilation of all methods named `Main` will be dumped:

   ```shell
   # Windows
   set COMPlus_JitDump=Main

   # Powershell
   $env:COMPlus_JitDump="Main"

   # Unix
   export COMPlus_JitDump=Main
   ```

* *Windows-only:* Setting the registry key `HKCU\Software\Microsoft\.NETFramework`, Value `<flagName>`, type `REG_SZ` or `REG_DWORD` (depending on the flag).
* *Windows-only:* Setting the registry key `HKLM\Software\Microsoft\.NETFramework`, Value `<flagName>`, type `REG_SZ` or `REG_DWORD` (depending on the flag).

## Specifying method names

Some environment variables such as `COMPlus_JitDump` take a set of patterns specifying method names. The matching works in the following way:
* A method set string is a space-separated list of patterns. Patterns can arbitrarily contain both '*' (match any characters) and '?' (match any 1 character).
* The string matched against depends on characters in the pattern:
  + If the pattern contains a ':' character, the string matched against is prefixed by the class name and a colon
  + If the pattern contains a '(' character, the string matched against is suffixed by the signature
  + If the class name (part before colon) contains a '[', the class contains its generic instantiation
  + If the method name (part between colon and '(') contains a '[', the method contains its generic instantiation

In particular, the matching is done against strings of the following format which coincides with how the JIT displays method signatures (so these can be copy pasted into the environment variable).
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

The full names of these instantiations are the following, as printed by `COMPlus_JitDisasmSummary`:

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

## Useful COMPlus variables

Below are some of the most useful `COMPlus` variables. Where {method-list} is specified in the list below, you can supply a space-separated list of either fully-qualified or simple method names (the former is useful when running something that has many methods of the same name), or you can specify `*` to mean all methods.

* `COMPlus_JitDump`={method-list} – dump lots of useful information about what the JIT is doing. See [Reading a JitDump](ryujit-overview.md#reading-a-jitdump) for more on how to analyze this data.
* `COMPlus_JitDumpASCII`={1 or 0} - Specifies whether the JIT dump should be ASCII only (Defaults to 1). Disabling this generates more readable expression trees.
* `COMPlus_JitDisasm`={method-list} – dump a disassembly listing of each method.
* `COMPlus_JitDiffableDasm` – set to 1 to tell the JIT to avoid printing things like pointer values that can change from one invocation to the next, so that the disassembly can be more easily compared.
* `COMPlus_JitGCDump`={method-list} – dump the GC information.
* `COMPlus_JitUnwindDump`={method-list} – dump the unwind tables.
* `COMPlus_JitEHDump`={method-list} – dump the exception handling tables.
* `COMPlus_JitTimeLogFile`={file name} – this specifies a log file to which timing information is written.
* `COMPlus_JitTimeLogCsv`={file name} – this specifies a log file to which summary timing information can be written, in CSV form.

## Dumping native images

If you followed the tutorial above and ran the sample app, you may be wondering why the disassembly for methods like `Substring` didn't show up in the output. This is because `Substring` lives in System.Private.CoreLib.dll, which (by default) is compiled ahead-of-time via crossgen2. Telling crossgen2 to dump the info works slightly differently in that it has to be specified on the command line instead. For more information, see the [debugging-aot-compilers](/docs/workflow/debugging/coreclr/debugging-aot-compilers.md) document.
