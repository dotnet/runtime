# IJW Activation for .NET Core on Windows

To support any C++/CLI users that wish to use .NET Core, the runtime and hosting APIs must be updated to provide support activation of the managed portion of mixed-mode assemblies. Without this support, any users of C++/CLI cannot move to .NET Core without using the deprecated Visual C++ compiler `/clr:pure` switch.

## Requirements

* Discover all installed versions of .NET Core.
* Load the appropriate version of .NET Core for the assembly if a .NET Core instance is not running, or validate that the currently running .NET Core instance can satisfy the assemblies requirements.
* Load the (already-in-memory) assembly into the runtime.
* Patch the vtfixup table tokens to point to JIT stubs.

## Design

IJW activation has a variety of hard problems associated with it, mainly with loading in mixed mode assemblies that are not the application.

Specifically, since IJW assemblies can be loaded from native code while under the Windows loader lock, the library that does the activation must be passed to the linker. Additionally, the library that does the activation cannot call `LoadLibrary` or family under the loader lock. So, the host library cannot start the runtime when it is initially loaded. As a result, the host library will patch the vtfixup table with its own stubs that, when called, will load the runtime and patch the vtfixup table again with pointers to JIT stubs.

IJW applications are much easier in that they are not loaded under the loader-lock, so they only need to load the runtime, load the image in, and patch the vtfixup table once to place in JIT stubs.

### .NET Framework IJW Activation

When targeting .NET Framework, mixed mode assemblies are linked to the shim library `mscoree.dll` or `mscoreei.dll`. See the document on [COM Activation](COM-activation.md#NET-Framework-Class-COM-Activation) for more information on the history of `mscoree.dll` and `mscoreei.dll`. C++/CLI executables are wired up to call `mscoreei.dll`'s `_CorExeMain` method on start which starts the runtime, patching the vtfixup table and calling the managed entry point. If the C++/CLI executable has a native entry point, a managed P/Invoke signature pointing to that native function in the image is emitted into the assembly as the assembly entry point.

If the assembly is a library, the library's native initialization function (what calls `DllMain` in a fully native DLL load scenario), calls into `mscoreei.dll`'s `_CorDllMain`. This `_CorDllMain` function patches the vtfixup table to have pointers to stubs that will start the runtime when called. Additionally, the runtime will call back into `mscoree.dll` when patching the vtfixup table with JIT stubs to check if the value in the table is a token or a stub to ensure that it patches the correct JIT stub in place.

Additionally, .NET Framework has support for a legacy code-path to forward calls to `_CorDllMain` to a user-provided `DllMain`. These code-paths are prone to locking under the loader lock if they call into any managed code or if the user-provided `DllMain` is a managed method, so support for a managed `DllMain` implementation is deprecated. Additionally, the Visual C++ compiler no longer generates a `DllMain` implementation for library initialization. Instead, the compiler generates a static module constructor to initialize any global state.

### .NET Core IJW Activation

Like with COM activation, our intent is to avoid a system-wide shim for IJW activation, especially since the host DLL needs to be linked to all C++/CLI assemblies. This new library (henceforth called the 'shim') will export functions to fulfill the requirements that the Visual C++ compiler needs to compile C++/CLI assemblies. Since we do not need to support backward compatibility with previously compiled mixed-mode assemblies, we are free to rename the exported functions while finalizing the design.

Below are the entry-points that the Visual C++ team needs

* `std::int32_t _CorExeMain()`
  * Called from a `.exe` mixed-mode assembly on startup. Starts the runtime with the entry `.exe`.
* `BOOL _CorDllMain(HMODULE hModule, DWORD dwFlags, LPVOID lpReserved)`
  * Called from a `.dll` mixed-mode assembly on load and unload.
  * On load, inserts the delayed-activation thunks.
  * On unload, frees the memory allocated for the delayed-activation thunks.
  * Calls the user-provided native `DllMain`.

#### IJW Executables

When `_CorExeMain()` is called, the following will occur:

1) If a [`.runtimeconfig.json`](https://github.com/dotnet/cli/blob/master/Documentation/specs/runtime-configuration-file.md) file exists adjacent to the shim assembly (`<shim_name>.runtimeconfig.json`), that file will be used to describe CLR configuration details. The documentation for the `.runtimeconfig.json` format defines under what circumstances this file may be optional.
2) Using the existing `hostfxr` library, attempt to discover the desired CLR and target [framework](https://docs.microsoft.com/en-us/dotnet/core/packages#frameworks).
   * If a CLR is active with the process, the requested CLR version will be validated against that CLR. If version satisfiability fails, activation will fail.
   * If a CLR is **not** active with the process, an attempt will be made to create a satisfying CLR instance.
   * Failure to create an instance will result in activation failure.
3) A request to the CLR will be made to load the assembly from memory and get the entry-point.
   * The ability to load an assembly from memory will require exposing a new function that can be called from `hostfxr`, as well as a new API in `System.Private.CoreLib` on a new class in `Internal.Runtime.InteropServices`:

   ```csharp
   public static class InMemoryAssemblyLoader
   {
       public static int LoadAndExecuteInMemoryAssembly(IntPtr handle, int argc, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 1)] string[] argv); /* argc is required for marshalling to know how large to make the argv array */
   }
   ```

   Note this API would not be exposed outside of `System.Private.CoreLib` unless we decide to do so.
   * The loading of the assembly will take place in the default `AssemblyLoadContext`.

#### IJW DLLs and Delayed-Activation Thunks

When `_CorDllMain()` is called, the following will occur:

1) If `_CorDllMain` was called because the DLL is being attached to a process:
   1) Calculate how many thunks we need to create from the number of entries in each record in the vtfixup table of the calling DLL.
   2) Allocate executable memory for all of the thunks needed for this module.
   3) Mark this chunk of thunks as associated to the calling DLL.
   4) For each method in each record of the vtfixup table, initialize the thunk to call into the thunk stub (which then calls a helper to start up the runtime) and replace the stub with the original token for later patching by the runtime.
2) Call the native `DllMain` if the user provided one.
3) If `_CorDllMain` was called because the DLL is being unloaded from the process:
   1) Deallocate the thunks allocated for the calling DLL.

#### Loading the Assembly Into the Runtime

When a delayed-activation thunk is called, it will be outside of the loader lock. So, we can load the runtime. We can now follow steps 1 and 2 from the section on [IJW Executables](#IJW-Executables). Finally, we will need another new function on `hostfxr` and a new API in `System.Private.CoreLib` in `Internal.Runtime.InteropServices`:

    ```csharp
    public static class InMemoryAssemblyLoader
    {
        public static unsafe void LoadInMemoryAssembly(IntPtr handle, char* modulePath);
    }
    ```

  Note this API would not be exposed outside of `System.Private.CoreLib` unless we decide to do so.
  * The loading of this assembly will take place in an isolated `AssemblyLoadContext`.

The naming of these APIs is designed to be useful for non-IJW scenarios as well, such as possibly Single-Exe.

When the runtime loads the assembly, it needs to know if each element in the vtfixup table is a token or a stub. In .NET Framework, this check is implemented by the runtime querying `mscoree.dll` by looking up callbacks. When the runtime is traversing the vtfixup table and updating the entries to point to JIT stubs, it queries `mscoree.dll` if the module has stubs. If the module has stubs, it calls back into `mscoree.dll` to query the stub data structures for the metadata token. Otherwise, it grabs the token from the slot.

We will implement it similarly, by having CoreCLR call back into the IJW assembly's shim. We will discover this shim by traversing the IJW assembly's import table to find the `_CorDllMain` import, and from there resolve the shim's `HMODULE`. Since it is technically possible to craft a non-IJW assembly that exports functions via the vtfixup table, we will enable CoreCLR to resolve the tokens from the table in the simple case where no delayed-activation thunks are used.

#### Caveats

Since native images can only be loaded into memory once on Windows, there is only one instance of the vtfixup table. As a result, the native code in an IJW assembly will always call into managed code from the first managed load of the assembly. As a result, if an IJW assembly is loaded into two different ALCs, then a call to managed code in an IJW assembly that calls into native code and back into managed within the IJW assembly may change ALCs within the stack if the call into the IJW assembly is in a different ALC than the IJW assembly was initially loaded into. We have a test that reproduces this behavior.

## Incompatible with trimming
.NET Core IJW Activation support on managed side is disabled by default on trimmed apps. .NET Core IJW Activation and trimming are incompatible since the trimmer cannot analyze methods that are called by the native side or doesn't even know about the assembly being loaded (and thus cannot analyze its dependencies). Native hosting support for trimming can be managed through the [feature switch](https://github.com/dotnet/runtime/blob/main/docs/workflow/trimming/feature-switches.md) settings specific to each native host.
