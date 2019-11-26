# Hosting layer error and exit codes
This document lists all the values returned as special exit codes when running `dotnet.exe` or `apphost` or returned by the hosting APIs (`hostfxr`, `hostpolicy` and `nethost`).
Note that the exit code returned by running an application via `dotnet.exe` or `apphost` can either be one of the below values or the exit code of the managed application itself.


### Success error/exit codes

* `Success` (`0`) - Operation was successful.

* `Success_HostAlreadyInitialized` (`0x00000001`) - Initialization was successful, but another host context is already initialized, so the returned context is "secondary". The requested context was otherwise fully compatible with the already initialized context.  
This is returned by `hostfxr_initialize_for_runtime_config` if it's called when the host is already initialized in the process. Comes from `corehost_initialize` in `hostpolicy`.

* `Success_DifferentRuntimeProperties` (`0x00000002`) - Initialization was successful, but another host context is already initialized and the requested context specified some runtime properties which are not the same (either in value or in presence) to the already initialized context.
This is returned by `hostfxr_initialize_for_runtime_config` if it's called when the host is already initialized in the process. Comes from `corehost_initialize` in `hostpolicy`.


### Failure error/exit codes

* `InvalidArgFailure` (`0x80008081`) - One of the specified arguments for the operation is invalid.

* `CoreHostLibLoadFailure` (`0x80008082`) - There was a failure loading a dependent library. If any of the hosting components calls `LoadLibrary`/`dlopen` on a dependent library and the call fails, this error code is returned. The most common case for this failure is if the dependent library is missing some of its dependencies (for example the necessary CRT is missing on the machine), likely corrupt or incomplete install.  
This error code is also returned from `corehost_resolve_component_dependencies` if it's called on a `hostpolicy` which has not been initialized via the hosting layer. This would typically happen if `coreclr` is loaded directly without the hosting layer and then `AssemblyDependencyResolver` is used (which is an unsupported scenario).

* `CoreHostLibMissingFailure` (`0x80008083`) - One of the dependent libraries is missing. Typically when the `hostfxr`, `hostpolicy` or `coreclr` dynamic libraries are not present in the expected locations. Probably means corrupted or incomplete installation.

* `CoreHostEntryPointFailure` (`0x80008084`) - One of the dependent libraries is missing a required entry point.

* `CoreHostCurHostFindFailure` (`0x80008085`) - If the hosting component is trying to use the path to the current module (the hosting component itself) and from it deduce the location of the installation. Either the location of the current module could not be determined (some weird OS call failure) or the location is not in the right place relative to other expected components.  
For example the `hostfxr` may look at its location and try to deduce the location of the `shared` folder with the framework from it. It assumes the typical install layout on disk. If this doesn't work, this error will be returned.

* `CoreClrResolveFailure` (`0x80008087`) - If the `coreclr` library could not be found. The hosting layer (`hostpolicy`) looks for `coreclr` library either next to the app itself (for self-contained) or in the root framework (for framework-dependent). This search can be done purely by looking at disk or more commonly by looking into the respective `.deps.json`. If the `coreclr` library is missing in `.deps.json` or it's there but doesn't exist on disk, this error is returned.

* `CoreClrBindFailure` (`0x80008088`) - The loaded `coreclr` library doesn't have one of the required entry points.

* `CoreClrInitFailure` (`0x80008089`) - The call to `coreclr_initialize` failed. The actual error returned by `coreclr` is reported in the error message.

* `CoreClrExeFailure` (`0x8000808a`) - The call to `coreclr_execute_assembly` failed. Note that this does not mean anything about the app's exit code, this failure occurs if `coreclr` failed to run the app itself.

* `ResolverInitFailure` (`0x8000808b`) - Initialization of the `hostpolicy` dependency resolver failed. This can be:
  * One of the frameworks or the app is missing a required `.deps.json` file.
  * One of the `.deps.json` files is invalid (invalid JSON, or missing required properties and so on).

* `ResolverResolveFailure` (`0x8000808c`) - Resolution of dependencies in `hostpolicy` failed. This can mean many different things, but in general one of the processed `.deps.json` contains entry for a file which could not found, or its resolution failed for some other reason (conflict for example).

* `LibHostCurExeFindFailure` (`0x8000808d`) - Failure to determine the location of the current executable. The hosting layer uses the current executable path to deduce the install location in some cases. If this path can't be obtained (OS call fails, or the returned path doesn't exist), this error is returned.

* `LibHostInitFailure` (`0x8000808e`) - Initialization of the `hostpolicy` library failed. The `corehost_load` method takes a structure with lot of initialization parameters. If the version of this structure doesn't match the expected value, this error code is returned. This would in general mean incompatibility between the `hostfxr` and `hostpolicy`, which should really only happen if somehow a newer `hostpolicy` is used by older `hostfxr`. This typically means corrupted installation.

* `LibHostSdkFindFailure` (`0x80008091`) - Failure to find the requested SDK. This happens in the `hostfxr` when an SDK (also called CLI) command is used with `dotnet`. In this case the hosting layer tries to find an installed .NET Core SDK to run the command on. The search is based on deduced install location and on the requested version from potential `global.json` file. If either no matching SDK version can be found, or that version exists, but it's missing the `dotnet.dll` file, this error code is returned.

* `LibHostInvalidArgs` (`0x80008092`) - Arguments to `hostpolicy` are invalid. This is used in three unrelated places in the `hostpolicy`, but in all cases it means the component calling `hostpolicy` did something wrong:
  * Command line arguments for the app - the failure would typically mean that wrong argument was passed or such. For example if the application main assembly is not specified on the command line. On its own this should not happen as `hostfxr` should have parsed and validated all command line arguments.
  * `hostpolicy` context's `get_delegate` - if the requested delegate enum value is not recognized. Again this would mean `hostfxr` passed the wrong value.
  * `corehost_resolve_component_dependencies` - if something went wrong initializing `hostpolicy` internal structures. Would happen for example when the `component_main_assembly_path` argument is wrong.

* `InvalidConfigFile` (`0x80008093`) - The `.runtimeconfig.json` file is invalid. The reasons for this failure can be among these:
  * Failure to read from the file
  * Invalid JSON
  * Invalid value for a property (for example number for property which requires a string)
  * Missing required property
  * Other inconsistencies (for example `rollForward` and `applyPatches` are not allowed to be specified in the same config file)
  * Any of the above failures reading the `.runtimecofig.dev.json` file
  * Self-contained `.runtimeconfig.json` used in `hostfxr_initialize_for_runtime_config`
Note that missing `.runtimconfig.json` is not an error (means self-contained app).  
This error code is also used when there is a problem reading the CLSID map file in `comhost`.

* `AppArgNotRunnable` (`0x80008094`) - Used internally when the command line for `dotnet.exe` doesn't contain path to the application to run. In such case the command line is considered to be a CLI/SDK command. This error code should never be returned to external caller.

* `AppHostExeNotBoundFailure` (`0x80008095`) - `apphost` failed to determine which application to run. This can mean:
  * The `apphost` binary has not been imprinted with the path to the app to run (so freshly built `apphost.exe` from the branch will fail to run like this)
  * The `apphost` is a bundle (single-file exe) and it failed to extract correctly.

* `FrameworkMissingFailure` (`0x80008096`) - It was not possible to find a compatible framework version. This originates in `hostfxr` (`resolve_framework_reference`) and means that the app specified a reference to a framework in its `.runtimeconfig.json` which could not be resolved. The failure to resolve can mean that no such framework is available on the disk, or that the available frameworks don't match the minimum version specified or that the roll forward options specified excluded all available frameworks.  
Typically this would be used if a 3.0 app is trying to run on a machine which has no 3.0 installed.  
It would also be used for example if a 32bit 3.0 app is running on a machine which has 3.0 installed but only for 64bit.

* `HostApiFailed` (`0x80008097`) - Returned by `hostfxr_get_native_search_directories` if the `hostpolicy` could not calculate the `NATIVE_DLL_SEARCH_DIRECTORIES`.

* `HostApiBufferTooSmall` (`0x80008098`) - Returned when the buffer specified to an API is not big enough to fit the requested value. Can be returned from:
  * `hostfxr_get_runtime_properties`
  * `hostfxr_get_native_search_directories`
  * `get_hostfxr_path`

* `LibHostUnknownCommand` (`0x80008099`) - Returned by `hostpolicy` if the `corehost_main_with_output_buffer` is called with unsupported host command. This error code means there is incompatibility between the `hostfxr` and `hostpolicy`. In reality this should pretty much never happen.

* `LibHostAppRootFindFailure` (`0x8000809a`) - Returned by `apphost` if the imprinted application path doesn't exist. This would happen if the app is built with an executable (the `apphost`) and the main `app.dll` is missing.

* `SdkResolverResolveFailure` (`0x8000809b`) - Returned from `hostfxr_resolve_sdk2` when it fails to find matching SDK. Similar to `LibHostSdkFindFailure` but only used in the `hostfxr_resolve_sdk2`.

* `FrameworkCompatFailure` (`0x8000809c`) - During processing of `.runtimeconfig.json` there were two framework references to the same framework which were not compatible. This can happen if the app specified a framework reference to a lower-level framework which is also specified by a higher-level framework which is also used by the app.  
For example, this would happen if the app referenced `Microsoft.AspNet.App` version 2.0 and `Microsoft.NETCore.App` version 3.0. In such case the `Microsoft.AspNet.App` has `.runtimeconfig.json` which also references `Microsoft.NETCore.App` but it only allows versions 2.0 up to 2.9 (via roll forward options). So the version 3.0 requested by the app is incompatible.

* `FrameworkCompatRetry` (`0x8000809d`) - Error used internally if the processing of framework references from `.runtimeconfig.json` reached a point where it needs to reprocess another already processed framework reference. If this error is returned to the external caller, it would mean there's a bug in the framework resolution algorithm.

* `AppHostExeNotBundle` (`0x8000809e`) - Error reading the bundle footer metadata from a single-file `apphost`. This would mean a corrupted `apphost`.

* `BundleExtractionFailure` (`0x8000809f`) - Error extracting single-file `apphost` bundle. This is used in case of any error related to the bundle itself. Typically would mean a corrupted bundle.

* `BundleExtractionIOError` (`0x800080a0`) - Error reading or writing files during single-file `apphost` bundle extraction.

* `LibHostDuplicateProperty` (`0x800080a1`) - The `.runtimeconfig.json` specified by the app contains a runtime property which is also produced by the hosting layer. For example if the `.runtimeconfig.json` would specify a property `TRUSTED_PLATFORM_ROOTS`, this error code would be returned. It is not allowed to specify properties which are otherwise populated by the hosting layer (`hostpolicy`) as there is not good way to resolve such conflicts.

* `HostApiUnsupportedVersion` (`0x800080a2`) - Feature which requires certain version of the hosting layer binaries was used on a version which doesn't support it.  
For example if COM component specified to run on 2.0 `Microsoft.NETCore.App` - as that contains older version of `hostpolicy` which doesn't support the necessary features to provide COM services.

* `HostInvalidState` (`0x800080a3`) - Error code returned by the hosting APIs in `hostfxr` if the current state is incompatible with the requested operation. There are many such cases, please refer to the documentation of the hosting APIs for details.    
For example if `hostfxr_get_runtime_property_value` is called with the `host_context_handle` `nullptr` (meaning get property from the active runtime) but there's no active runtime in the process.

* `HostPropertyNotFound` (`0x800080a4`) - property requested by `hostfxr_get_runtime_property_value` doesn't exist.

* `CoreHostIncompatibleConfig` (`0x800080a5`) - Error returned by `hostfxr_initialize_for_runtime_config` if the component being initialized requires framework which is not available or incompatible with the frameworks loaded by the runtime already in the process. For example trying to load a component which requires 3.0 into a process which is already running a 2.0 runtime.
