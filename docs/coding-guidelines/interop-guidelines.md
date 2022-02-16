Interop Guidelines
==================

## Goals
We have the following goals related to interop code being used in dotnet/runtime:

- Minimize code duplication for interop.
  - We should only define a given interop signature in a single place. This stuff is tricky, and we shouldn't be copy-and-pasting it.
- Minimize unnecessary IL in assemblies.
  - Interop signatures should only be compiled into the assemblies that actually consume them. Having extra signatures bloats assemblies and makes it more difficult to do static analysis over assemblies to understand what they actually use. It also leads to problems when such static verification is used as a gate, e.g. if a store verifies that only certain APIs are used by apps in the store.
- Keep interop code isolated and consolidated.
  - This is both for good hygiene and to help keep platform-specific code separated from platform-neutral code, which is important for maximizing reusable code above PAL layers.
- Ensure maximal managed code reuse across different OS flavors which have  the same API but not the same ABI.
   - This is the case for UNIX and addressing it is a work-in-progress (see issue #2137 and section on "shims" below.)

## Submitting Changes

Interop code implicitly defines the native platform dependencies that .NET has. These dependencies are tracked and modeled according to the [Tracking Platform Dependencies design](https://github.com/dotnet/designs/blob/main/accepted/2021/platform-dependencies/platform-dependencies.md). Whenever a PR is submitted that changes interop code, it needs to be reviewed to determine whether a change to the platform dependencies model is required.

By default, any change to `src/libraries/Common/src/Interop` folder will add @dotnet/platform-deps-team as a reviewer. If necessary, update the corresponding `https://github.com/dotnet/core/blob/main/release-notes/<product-version>/runtime-deps.json` file to reflect the dependency change. The scope of dependencies is at the file/package level, not individual functions, so interop changes rarely require an update to the model.

## Approach

### Interop type
- All code related to interop signatures (DllImports, interop structs used in DllImports, constants that map to native values, etc.) should live in a partial, static, and internal “Interop” class in the root namespace, e.g.

```C#
internal static partial class Interop { ... }
```

- Declarations shouldn't be in Interop directly, but rather within a partial, static, internal nested type named for a given library or set of libraries, e.g.

```C#
internal static partial class Interop
{
    internal static partial class libc { ... }
}
...
internal static partial class Interop
{
    internal static partial class mincore { ... }
}
```
- With few exceptions, the only methods that should be defined in these interop types are DllImports.
  - Exceptions are limited to times when most or every consumer of a particular DllImport will need to wrap its invocation in a helper, e.g. to provide additional marshaling support, to hide thread-safety issues in the underlying OS implementation, to do any required manipulation of safe handles, etc. In such cases, the DllImport should be private whenever possible rather than internal, with the helper code exposed to consumers rather than having the DllImport exposed directly.

### File organization

- The Interop partial class definitions should live in Interop.*.cs files. These Interop.*.cs files should all live under Common rather than within a given assembly's folder.
 - The only exception to this should be when an assembly P/Invokes to its own native library that isn't available to or consumed by anyone else, e.g. System.IO.Compression P/Invoking to System.IO.Compression.Native.dll. In such cases, System.IO.Compression should have its own Interop folder which follows a similar scheme as outlined in this proposal, but just for these private P/Invokes.
- Under Common\src\Interop, we'll have a folder for each target platform, and within each platform, for each library from which functionality is being consumed. The Interop.*.cs files will live within those library folders, e.g.

```
\Common\src\Interop
    \Windows
        \mincore
            ... interop files
    \Unix
        \libc
            ... interop files
    \Linux
        \libc
            ... interop files
```

As shown above, platforms may be additive, in that an assembly may use functionality from multiple folders, e.g. System.IO.FileSystem's Linux build will use functionality both from Unix (common across all Unix systems) and from Linux (specific to Linux and not available across non-Linux Unix systems).

- Interop.*.cs files are created in a way such that every assembly consuming the file will need every DllImport it contains.
  - If multiple related DllImports will all be needed by every consumer, they may be declared in the same file, named for the functionality grouping, e.g. Interop.IOErrors.cs.
  - Otherwise, in the limit (and the expected case for most situations) each Interop.*.cs file will contain a single DllImport and associated interop types (e.g. the structs used with that signature) and helper wrappers, e.g. Interop.strerror.cs.

```
\Common\src\Interop
    \Unix
        \libc
            \Interop.strerror.cs
    \Windows
        \mincore
            \Interop.OutputDebugString.cs
```

- If structs/constants will be used on their own without an associated DllImport, or if they may be used with multiple DllImports not in the same file, they should be declared in a separate file.
- In the case of multiple overloads of the same DllImport (e.g. some overloads taking a SafeHandle and others taking an IntPtr, or overloads taking different kinds of SafeHandles), if they can't all be declared in the same file (because they won't all be consumed by all consumers), the file should be qualified with the key differentiator, e.g.

```
\Common\src\Interop
    \Windows
        \mincore
            \Interop.DuplicateHandle_SafeTokenHandle.cs
            \Interop.DuplicateHandle_IntPtr.cs
```

- The library names used per-platform are stored in internal constants in the Interop class in a private Libraries class in a per-platform file named Interop.Libraries.cs. These constants are then used for all DllImports to that library, rather than having the string duplicated each time, e.g.

```C#
internal static partial class Interop // contents of Common\src\Interop\Windows\Interop.Libraries.cs
{
    private static class Libraries
    {
        internal const string Kernel32 = "kernel32.dll";
        internal const string Localization = "api-ms-win-core-localization-l1-2-0.dll";
        internal const string Handle = "api-ms-win-core-handle-l1-1-0.dll";
        internal const string ProcessThreads = "api-ms-win-core-processthreads-l1-1-0.dll";
        internal const string File = "api-ms-win-core-file-l1-1-0.dll";
        internal const string NamedPipe = "api-ms-win-core-namedpipe-l1-1-0.dll";
        internal const string IO = "api-ms-win-core-io-l1-1-0.dll";
        ...
    }
}

```
(Note that this will likely result in some extra constants defined in each assembly that uses interop, which minimally violates one of the goals, but it's very minimal.)

- .csproj project files then include the interop code they need, e.g.
```XML
<ItemGroup Condition=" '$(TargetsUnix)' == 'true' ">
    <Compile Include="Interop\Unix\Interop.Libraries.cs" />
    <Compile Include="Interop\Unix\libc\Interop.strerror.cs" />
    <Compile Include="Interop\Unix\libc\Interop.getenv.cs" />
    <Compile Include="Interop\Unix\libc\Interop.getenv.cs" />
    <Compile Include="Interop\Unix\libc\Interop.open64.cs" />
    <Compile Include="Interop\Unix\libc\Interop.close.cs" />
    <Compile Include="Interop\Unix\libc\Interop.snprintf.cs" />
    ...
</ItemGroup>
```

### Build System
When building dotnet/runtime, we use the "TargetOS" property to control what target platform we are building for. The valid values for this property are windows (which is the default value from MSBuild when running on Windows), Linux and OSX.

#### Project Files
Whenever possible, a single .csproj should be used per assembly, spanning all target platforms, e.g. System.Console.csproj includes conditional entries for when targeting Windows vs when targeting Linux. A property can be passed to dotnet build to control which flavor is built, e.g. `dotnet build /p:TargetOS=OSX System.Console.csproj`.

### Constants
- Wherever possible, constants should be defined as "const". Only if the data type doesn't support this (e.g. IntPtr) should they instead be static readonly fields.

- Related constants should be grouped under a partial, static, internal type, e.g. for error codes they'd be grouped under an Errors type:

```C#
internal static partial class Interop
{
    internal static partial class libc
    {
        internal static partial class Errors
        {
            internal const int ENOENT = 2;
            internal const int EINTR = 4;
            internal const int EWOULDBLOCK = 11;
            internal const int EACCES = 13;
            internal const int EEXIST = 17;
            internal const int EXDEV = 18;
            internal const int EISDIR = 21;
            internal const int EINVAL = 22;
            internal const int EFBIG = 27;
            internal const int ENAMETOOLONG = 36;
            internal const int ECANCELED = 125;
            ...
        }
    }
}
```

Using enums instead of partial, static classes can lead to needing lots of casts at call sites and can cause problems if such a type needs to be split across multiple files (enums can't currently be partial). However, enums can be valuable in making it clear in a DllImport signature what values are permissible. Enums may be used in limited circumstances where these aren't concerns: the full set of values can be represented in the enum, and the interop signature can be defined to use the enum type rather than the underlying integral type.

## P/Invoke Definitions

When defining the P/Invoke signatures and structs, we follow the guidelines in the [interop best practices documentation](https://docs.microsoft.com/en-us/dotnet/standard/native-interop/best-practices).

The runtime repo makes use of [source-generated p/invokes](../design/features/source-generator-pinvokes.md) whenever possible (see [the compatibility doc](../design/libraries/DllImportGenerator/Compatibility.md) for unsupported scenarios). Methods should be marked `GeneratedDllImport` and be `static` and `partial`.

If implicit framework references are disabled (as is the case for most libraries projects), explicit references to the below are required for marshalling arrays:
  - `System.Memory`
  - `System.Runtime.CompilerServices.Unsafe`

## UNIX shims

Often, various UNIX flavors offer the same API from the point-of-view of compatibility with C/C++ source code, but they do not have the same ABI. e.g. Fields can be laid out differently, constants can have different numeric values, exports can be named differently, etc. There are not only differences between operating systems (Mac OS X vs. Ubuntu vs. FreeBSD), but also differences related to the underlying processor architecture (x64 vs. x86 vs. ARM).

This leaves us with a situation where we can't write portable P/Invoke declarations that will work on all flavors, and writing separate declarations per flavor is quite fragile and won't scale.

To address this, we're moving to a model where all UNIX interop from dotnet/runtime starts with a P/Invoke to a C++ lib written specifically for dotnet/runtime. These libs -- System.*.Native.so (aka "shims") -- are intended to be very thin layers over underlying platform libraries. Generally, they are not there to add any significant abstraction, but to create a stable ABI such that the same IL assembly can work across UNIX flavors.

The System.Native shims are a private implementation detail of the Microsoft.NETCore.App shared framework and are intended only for use by code inside of the shared framework. Calling into the shims from external to Microsoft.NETCore.App has similar risks to using private reflection, with no guarantees from version to version or even patch to patch of stable exports. Assemblies that ship outside of the shared framework (e.g. Microsoft.Extensions.*) must not directly access the shims.

Guidelines for shim C++ API:

- Keep them as "thin"/1:1 as possible.
  - We want to write the majority of code in C#.
- Never skip the shim and P/Invoke directly to the underlying platform API. It's easy to assume something is safe/guaranteed when it isn't.
- Don't cheat and take advantage of coincidental agreement between one flavor's ABI and the shim's ABI.
- Use PascalCase in a style closer to Win32 than libc.
  - If an export point has a 1:1 correspondence to the platform API, then name it after the platform API in PascalCase (e.g. stat -> Stat, fstat -> FStat).
  - If an export is not 1:1, then spell things out as we typically would in dotnet/runtime code (i.e. don't use abbreviations unless they come from the underlying API.
  - At first, it seemed that we'd want to use 1:1 names throughout, but it turns out there are many cases where being strictly 1:1 isn't practical.
  - In order to reduce the chance of collisions when linking with CoreRT, all exports should have a prefix that corresponds to the Libraries' name, e.g. "SystemNative_" or "CryptoNative_" to make the method name more unique. See https://github.com/dotnet/runtime/issues/15854.
- Stick to data types which are guaranteed not to vary in size across flavors.
  - Use int32_t, int64_t, etc. from stdint.h and not int, long, etc.
  - Use char* for ASCII or UTF-8 strings and uint8_t* for byte buffers.
     - Note that sizeof(char) == 1 is guaranteed.
  - Do not use size_t in shim API. Always pick a fixed size. Often, it is most convenient to line up with the managed int as int32_t (e.g. scratch buffer size for read/write), but sometimes we need to handle huge sizes (e.g. memory mapped files) and therefore use uint64_t.
  - Use int64_t for native off_t values.
