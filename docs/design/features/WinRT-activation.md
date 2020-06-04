# Managed WinRT Activation of .NET Core components

As part of supporting a complete story for XAML Islands on .NET Core, we should provide a mechanism of activating .NET Core WinRT components. To do so, we will follow the path of the COM and IJW activations and provide a new host for activating these components in a manner similar to native WinRT components, which we will call the `winrthost`. We are creating a separate host instead of combining with the COM host because the COM and WinRT hosts (although generally similar in design in Windows) have very different activation and resolution paths. As a result, we would not be able to reuse much code at all. Additionally, the WinRT host does not require a `clsidmap` or similar functionality to function. If we were to combine the two hosts, we would have to emit an empty clsidmap when creating the host and modify it even though the WinRT portion has no dependencies on any embedded resources.

## Requirements

* Discover all installed versions of .NET Core.
* Load the appropriate version of .NET Core for the class if a .NET Core instance is not running, or validate the currently existing .NET Core instance can satisfy the class requirement.
* Return an [`IActivationFactory`](https://docs.microsoft.com/windows/desktop/api/activation/nn-activation-iactivationfactory) implementation that will construct an instance of the .NET WinRT class.

## Native WinRT Activation

In the native (C++/CX, C++/WRL, or C++/WinRT) world, building a WinRT Component named `MyComponent` produces files named `MyComponent.dll`, `MyComponent.winmd`. In this world, the `winmd` contains the metadata describing the types provided by the component, and the code implementing said types is compiled into the `dll`. When an application wants to activate a component, it will call [`RoGetActivationFactory`](https://docs.microsoft.com/windows/desktop/api/roapi/nf-roapi-rogetactivationfactory). The operating system will then search through various areas in the filesystem to find the correct component for the class name. Starting in Windows 10 19H1, there is now support for declaring that specific classes come from components that live side-by-side with the application, similar to Reg-Free COM. After finding the correct component, the OS will load the component via `CoLoadLibrary` and then call the `DllGetActivationFactory` entrypoint, which gets an activation factory for the class by name.

## Proposed Managed WinRT Activation

In the managed (.NET) world, we put all of the code that implements the component into the `winmd`. So, when running in an AppContainer/`.appx`, a .NET component only needs one file. However, when outside of an AppContainer, there needs to be some sort of host to activate runtime. We will supply a host that implements the [`DllGetActivationFactory`](https://docs.microsoft.com/previous-versions//br205771(v=vs.85)) entrypoint. When `DllGetActivationFactory` is called, the following will occur:

1) If a [`.runtimeconfig.json`](https://github.com/dotnet/cli/blob/master/Documentation/specs/runtime-configuration-file.md) file exists adjacent to the shim assembly (`<shim_name>.runtimeconfig.json`), that file will be used to describe CLR configuration details. The documentation for the `.runtimeconfig.json` format defines under what circumstances this file may be optional.
2) Using the existing `hostfxr` library, attempt to discover the desired CLR and target [framework](https://docs.microsoft.com/en-us/dotnet/core/packages#frameworks).
   * If a CLR is active with the process, the requested CLR version will be validated against that CLR. If version satisfiability fails, activation will fail.
   * If a CLR is **not** active with the process, an attempt will be made to create a satisfying CLR instance.
   * Failure to create an instance will result in activation failure.
3) A request to the CLR will be made to load the corresponding WinRT type for the given type name into the runtime.
   * This request will use the runtime's current support for resolving WinRT types to correctly resolve them.
   * The ability to load an assembly from memory will require exposing a new function that can be called from `hostfxr`, as well as a new API in `System.Private.CoreLib` on a new class in `Internal.Runtime.InteropServices`:

```csharp
namespace Internal.Runtime.InteropServices.WindowsRuntime
{
    public static class ActivationFactoryLoader
    {
        public unsafe static int GetActivationFactory(
            char* componentPath,
            [MarshalAs(UnmanagedType.HString)] string typeName,
            [MarshalAs(UnmanagedType.Interface)] out IActivationFactory activationFactory);
    }
}
```

Note this API would not be exposed outside of `System.Private.CoreLib` unless we decide to do so. The runtime loads WinRT components via a different binder than .NET assemblies, so the WinRT component assemblies themselves will be loaded by the WinRT binder, but all assemblies that the WinRT component depends on will be loaded into an isolated `AssemblyLoadContext`.

To match the user-visible architecture of native WinRT Activation, when building a Windows Metadata component (`winmdobj`), we will copy the `winrthost` to the user's output directory and rename it to be the same name as the `.winmd` but with a `.dll` extension. For example, if the user creates a project `MyComponent` and is building a Windows Metadata component, we will copy the `winrthost` to the output directory for `MyComponent` and rename it to be `MyComponent.dll`. From a user's perspective, they will activate WinRT objects via `MyComponent.dll`, the same as if the component was a native WinRT component.

## Error reporting

If the runtime activation fails, we want to ensure that the user can diagnose the failure. However, we don't want to write directly to the `stderr` of a process that we don't own. So, we will redirect the trace stream to point to a local stream. In the case of failure to activate the runtime, we will report the error code via [`RoOriginateErrorW`](https://docs.microsoft.com/windows/desktop/api/roerrorapi/nf-roerrorapi-rooriginateerrorw), which will present the user with the error according to the error reporting flags they have set via [`RoSetErrorReportingFlags`](https://docs.microsoft.com/windows/desktop/api/roerrorapi/nf-roerrorapi-roseterrorreportingflags).

## Open issues

* The tool that converts the `.winmdobj` to a `.winmd`, WinMDExp, is only available in Desktop MSBuild and is not available in the .NET Core MSBuild distribution. Additionally, WinMDExp only supports full PDBs and will fail if given portable PDBs.
* To build a `.winmdobj`, the user will need to reference a contract assembly that includes the `Windows.Foundation` namespace. There are new packages that expose these contract assemblies, but we should ensure that these contract assemblies will be released by the time that we finalize our Managed WinRT Component support.
* Writing unit tests for the WinRT host without modifying system-global state requires the new Reg-Free WinRT support, which is Windows 10 19H1 only.
