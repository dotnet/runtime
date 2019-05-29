# Native hosting

Native hosting is the ability to host the .NET Core runtime in an arbitrary process, one which didn't start from .NET Core produced binaries.

#### Terminology
* "native host" - the code which uses the proposed APIs. Can be any non .NET Core application (.NET Core applications have easier ways to perform these scenarios).
* "hosting components" - shorthand for .NET Core hosting components. Typically refers to `hostfxr` and `hostpolicy`. Sometimes also referred to simply as "host".
* "host context" - state which `hostfxr` creates and maintains and represents a logical operation on the hosting components.

## Scenarios
* **Hosting managed components**  
Native host which wants to load managed assembly and call into it for some functionality. Must support loading multiple such components side by side.
* **Hosting managed apps**  
Native host which wants to run a managed app in-proc. Basically a different implementation of the existing .NET Core hosts (`dotnet.exe` or `apphost`). The intent is the ability to modify how the runtime starts and how the managed app is executed (and where it starts from).
* **App using other .NET Core hosting services**  
App (native or .NET Core both) which needs to use some of the other services provided by the .NET Core hosting layer. For example the ability to locate available SDKs and so on.


## Existing support
* **C-style ABI in `coreclr`**  
`coreclr` exposes ABI to host the .NET Core runtime and run managed code already using C-style APIs. See this [header file](https://github.com/dotnet/coreclr/blob/master/src/coreclr/hosts/inc/coreclrhost.h) for the exposed functions.
This API requires the native host to locate the runtime and to fully specify all startup parameters for the runtime. There's no inherent interoperability between these APIs and the .NET Core SDK.
* **COM-style ABI in `coreclr`**  
`coreclr` exposes COM-style ABI to host the .NET Core runtime and perform a wide range of operations on it. See this [header file](https://github.com/dotnet/coreclr/blob/master/src/pal/prebuilt/inc/mscoree.h) for more details.
Similarly to the C-style ABI the COM-style ABI also requires the native host to locate the runtime and to fully specify all startup parameters.
There's no inherent interoperability between these APIs and the .NET Core SDK.  
The COM-style ABI is deprecated and should not be used going forward.
* **`hostfxr` and `hostpolicy` APIs**  
The hosting layer of .NET Core already exposes some functionality as C-style ABI on either the `hostfxr` or `hostpolicy` libraries. These can execute application, determine available SDKs, determine native dependency locations, resolve component dependencies and so on.
Unlike the above `coreclr` based APIs these don't require the caller to fully specify all startup parameters, instead these APIs understand artifacts produced by .NET Core SDK making it much easier to consume SDK produced apps/libraries.
The native host is still required to locate the `hostfxr` or `hostpolicy` libraries. These APIs are also designed for specific narrow scenarios, any usage outside of these bounds is typically not possible.


## Scope
This document focuses on easy-to-use hosting which cooperates with the .NET Core SDK and consumes the artifacts produced by building the managed app/libraries directly. It completely ignores the COM-style ABI as it's hard to use from some programming languages.

As such the document explicitly excludes any hosting based on directly loading `coreclr`. The document focuses on using the existing .NET Core hosting layer in new ways. For details on the .NET Core hosting components see [this document](https://github.com/dotnet/core-setup/blob/master/Documentation/design-docs/host-components.md).


## High-level proposal
In .NET Core 3.0 the hosting layer (see [here](https://github.com/dotnet/core-setup/blob/master/Documentation/design-docs/host-components.md)) ships with several hosts. These are binaries which act as the entry points to the .NET Core hosting/runtime:
* The "muxer" (`dotnet.exe`)
* The `apphost` (`.exe` which is part of the app)
* The `comhost` (`.dll` which is part of the app and acts as COM server)
* The `ijwhost` (`.dll` consumed via `.lib` used by IJW assemblies)

Every one of these hosts serve different scenario and expose different APIs. The one thing they have in common is that their main purpose is to find the right `hostfxr`, load it and call into it to execute the desired scenario. For the most part all these hosts are basically just wrappers around functionality provided by `hostfxr`.

The proposal is to add a new host library `nethost` which can be used by native host to easily host managed components and to easily locate `hostfxr` for more advanced scenarios.

At the same time add the ability to pass additional runtime properties when starting the runtime through the hosting entry points (starting app, loading component). This can be used by the native host to:
* Register startup hook without modifying environment variables (which are inherited by child processes)
* Introduce new runtime knobs which are only available for native hosts without the need to update the hosting APIs every time.


*Technical note: All strings in the proposed APIs are using the `char_t` in this document for simplicity. In real implementation they are of the type `pal::char_t`. In particular:*
* *On Windows - they are `WCHAR *` using `UTF16` encoding*
* *On Linux/macOS - they are `char *` using `UTF8` encoding*


## New host binary for finding `hostfxr`
Add new library `nethost` which will provide a way to locate the right `hostfxr`.
The library would be a dynamically loaded library (`.dll`, `.so`, `.dylib`). For ease of use there would be a header file for C/C++ apps as well as `.lib`/`.a` for easy linking.
Native host would ship this library as part of the app. Unlike the `apphost`, `comhost` and `ijwhost`, the `nethost` will not be directly supported by the .NET Core SDK since it's target usage is not from .NET Core apps.

The exact delivery mechanism is TBD (pending investigation):
* `.zip` which would contain the `.dll`, `.h` and `.lib` on Windows, `.so` and `.h` on Linux and `.dylib` and `.h` on macOS.
* Possibly a NuGet package for easy consumption from VS C++ projects
* Possibly include it in some form in .NET Core SDK as well (similar to `ijwhost`)

The binary itself should be signed by Microsoft as there will be no support for modifying the binary as part of custom application build (unlike `apphost`).


### Locate `hostfxr`
``` C++
int get_hostfxr_path(
    char_t * result_buffer,
    size_t * buffer_size,
    const char_t * assembly_path);
```

This API locates the `hostfxr` and returns its path by calling the `result` function.

* `result_buffer` - Buffer that will be populated with the hostfxr path, including a null terminator.
* `buffer_size` - On input this points to the size of the `result_buffer` in `char_t` units. On output this points to the number of `char_t` units used from the `result_buffer` (including the null terminator). If `result_buffer` is `nullptr` the input value is ignored and only the minimum required size in `char_t` units is set on output.
* `assembly_path` - Optional. Path to the component's assembly. Whether or not this is specified determines the behavior for locating the hostfxr library.
  * If `nullptr`, `hostfxr` is located using the environment variable or global registration
  * If specified, `hostfxr` is located as if the `assembly_path` is an application with `apphost`

`nethost` library uses the `__stdcall` calling convention.


## Improve API to run application and load components

### Goals
* All hosts should be able to use the new API (whether they will is a separate question as the old API has to be kept for backward compat reasons)
* Hide implementation details as much as possible
  * Make the API generally easier to use/understand
  * Give the implementation more freedom
  * Allow future improvements without breaking the API
  * Consider explicitly documenting types of behaviors which nobody should take dependency on (specifically failure scenarios)
* Extensible
  * It should allow additional parameters to some of the operations without a need to add new exported APIs
  * It should allow additional interactions with the host - for example modifying how the runtime is initialized via some new options, without a need for a completely new set of APIs

### New scenarios
The API should allow these scenarios:
* Runtime properties
  * Specify additional runtime properties from the native host
  * Implement conflict resolution for runtime properties
  * Inspect calculated runtime properties (the ones calculated by `hostfxr`/`hostpolicy`)

It should be possible to ship with only some of these supported, then enable more scenarios later on.

All the proposed APIs will be exports of the `hostfxr` library and will use the same calling convention and name mangling as existing `hostfxr` exports.

### Initialize host context

All the "initialize" functions will
* Process the `.runtimeconfig.json`
* Resolve framework references and find actual frameworks
* Find the root framework (`Microsoft.NETCore.App`) and load the `hostpolicy` from it
* The `hostpolicy` will then process all relevant `.deps.json` files and produce the list of assemblies, native search paths and other artifacts needed to initialize the runtime.

The functions will NOT load the CoreCLR runtime. They just prepare everything to the point where it can be loaded.

The functions return a handle to a new host context:
* The handle must be closed via `hostfxr_close`.
* The handle is not thread safe - the consumer should only call functions on it from one thread at a time.

The `hostfxr` will also track active runtime in the process. Due to limitations (and to simplify implementation) this tracking will actually not look at the actual `coreclr` module (or try to communicate with the runtime in any way). Instead `hostfxr` itself will track the host context initialization. The first host context initialization in the process will represent the "loaded runtime". It is only possible to have one "loaded runtime" in the process. Any subsequent host context initialization will just "attach" to the "loaded runtime" instead of creating a new one.

``` C
#define hostfxr_handle = void *;

struct hostfxr_initialize_parameters
{
    size_t size;
    const char_t * host_path;
    const char_t * dotnet_root;
};
```

The `hostfxr_initialize_parameters` structure stores parameters which are common to all forms of initialization.
* `size` - the size of the structure. This is used for versioning. Should be set to `sizeof(hostfxr_initialize_parameters)`.
* `host_path` - path to the native host (typically the `.exe`). This value is not used for anything by the hosting components. It's just passed to the CoreCLR as the path to the executable. It can point to a file which is not executable itself, if such file doesn't exist (for example in COM activation scenarios this points to the `comhost.dll`). This is used by PAL to initialize internal command line structures, process name and so on.
* `dotnet_root` - path to the root of the .NET Core installation in use. This typically points to the install location from which the `hostfxr` has been loaded. For example on Windows this would typically point to `C:\Program Files\dotnet`. The path is used to search for shared frameworks and potentially SDKs.


``` C
int hostfxr_initialize_for_dotnet_command_line(
    int argc,
    const char_t * argv[],
    const hostfxr_initialize_parameters * parameters,
    hostfxr_handle * host_context_handle
);
```

Initializes the hosting components for running a managed application.
The command line is parsed to determine the app path. The app path will be used to locate the `.runtimeconfig.json` and the `.deps.json` which will be used to load the application and its dependent frameworks.
* `argc` and `argv` - the command line for running a managed application. These represent the arguments which would have been passed to the muxer if the app was being run from the command line.
* `parameters` - additional parameters - see `hostfxr_initialize_parameters` for details. (Could be made optional potentially)
* `host_context_handle` - output parameter. On success receives an opaque value which identifies the initialized host context. The handle should be closed by calling `hostfxr_close`.

This function only supports arguments for running an application as through the muxer. It does not support SDK commands.

This function can only be called once per-process. It's not supported to run multiple apps in one process (even sequentially).

This function will fail if there already is a CoreCLR running in the process as it's not possible to run two apps in a single process.

*Note: This is effectively a replacement for `hostfxr_main_startupinfo` and `hostfxr_main`. Currently it is not a goal to fully replace these APIs because they also support SDK commands which are special in lot of ways and don't fit well with the rest of the native hosting. There's no scenario right now which would require the ability to issue SDK commands from a native host. That said nothing in this proposal should block enabling even SDK commands through these APIs.*


``` C
int hostfxr_initialize_for_runtime_config(
    const char_t * runtime_config_path,
    const hostfxr_initialize_parameters * parameters,
    hostfxr_handle * host_context_handle
);
```

This function would load the specified `.runtimeconfig.json`, resolve all frameworks, resolve all the assets from those frameworks and then prepare runtime initialization where the TPA contains only frameworks. Note that this case does NOT consume any `.deps.json` from the app/component (only processes the framework's `.deps.json`). This entry point is intended for `comhost`/`ijwhost`/`nethost` and similar scenarios.
* `runtime_config_path` - path to the `.runtimeconfig.json` file to process. Unlike with `hostfxr_initialize_for_dotnet_command_line`, any `.deps.json` from the app/component will not be processed by the hosting layers.
* `parameters` - additional parameters - see `hostfxr_initialize_parameters` for details. (Could be made optional potentially)
* `host_context_handle` - output parameter. On success receives an opaque value which identifies the initialized host context. The handle should be closed by calling `hostfxr_close`.

This function can be called multiple times in a process.
* If it's called when no runtime is present, it will run through the steps to "initialize" the runtime (resolving frameworks and so on).
* If it's called when there already is CoreCLR in the process (loaded through the `hostfxr`, direct usage of `coreclr` is not supported), then the function determines if the specified runtime configuration is compatible with the existing runtime and frameworks. If it is, it returns a valid handle, otherwise it fails.

It needs to be possible to call this function simultaneously from multiple threads at the same time.
It also needs to be possible to call this function while there is an active host context created by `hostfxr_initialize_for_dotnet_command_line` and running inside the `hostfxr_run_app`.

The function returns specific return code for the first initialized host context, and a different one for any subsequent one. Both return codes are considered "success". If there already was initialized host context in the process then the returned host context has these limitations:
* It won't allow setting runtime properties.
* The initialization will compare the runtime properties from the `.runtimeconfig.json` specified in the `runtime_config_path` with those already set to the runtime in the process
  * If all properties from the new runtime config are already set and have the exact same values (case sensitive string comparison), the initialization succeeds with no additional consequences. (Note that this is the most typical case where the runtime config have no properties in it.)
  * If there are either new properties which are not set in the runtime or ones which have different values, the initialization will return a special return code - a "warning". It's not a full on failure as initialized context will be returned.
  * In both cases only the properties specified by the new runtime config will be reported on the host context. This is to allow the native host to decide in the "warning" case if it's OK to let the component run or not.
  * In both cases the returned host context can still be used to get a runtime delegate, the properties from the new runtime config will be ignored (as there's no way to modify those in the runtime).


### Inspect and modify host context

#### Runtime properties
These functions allow the native host to inspect and modify runtime properties.
* If the `host_context_handle` represents the first initialized context in the process, these functions expose all properties from runtime configurations as well as those computed by the hosting layer components. These functions will allow modification of the properties via `hostfxr_set_runtime_property_value`. 
* If the `host_context_handle` represents any other context (so not the first one), these functions expose only properties from runtime configuration. These functions won't allow modification of the properties.

It is possible to access runtime properties of the first initialized context in the process at any time (for reading only), by specifying `nullptr` as the `host_context_handle`.

``` C
int hostfxr_get_runtime_property_value(
    const hostfxr_handle host_context_handle,
    const char_t * name,
    const char_t ** value);
```

Returns the value of a runtime property specified by its name.
* `host_context_handle` - the initialized host context. If set to `nullptr` the function will operate on runtime properties of the first host context in the process.
* `name` - the name of the runtime property to get. Must not be `nullptr`.
* `value` - returns a pointer to a buffer with the property value. The buffer is owned by the host context. The caller should make a copy of it if it needs to store it for anything longer than immediate consumption. The lifetime is only guaranteed until any of the below happens:
  * one of the "run" methods is called on the host context
  * the host context is closed via `hostfxr_close`
  * the value of the property is changed via `hostfxr_set_runtime_property_value`

Trying to get a property which doesn't exist is an error and will return an appropriate error code.

We're proposing a fix in `hostpolicy` which will make sure that there are no duplicates possible after initialization (see [dotnet/core-setup#5529](https://github.com/dotnet/core-setup/issues/5529)). With that `hostfxr_get_runtime_property_value` will work always (as there can only be one value).


``` C
int hostfxr_set_runtime_property_value(
    const hostfxr_handle host_context_handle,
    const char_t * name,
    const char_t * value);
```

Sets the value of a property.
* `host_context_handle` - the initialized host context. (Must not be `nullptr`)
* `name` - the name of the runtime property to set. Must not be `nullptr`.
* `value` - the value of the property to set. If the property already has a value in the host context, this function will overwrite it. When set to `nullptr` and if the property already has a value then the property is "unset" - removed from the runtime property collection.

Setting properties is only supported on the first host context in the process. This is really a limitation of the runtime for which the runtime properties are immutable. Once the first host context is initialized and starts a runtime there's no way to change these properties. For now we will not consider the scenario where the host context is initialized but the runtime hasn't started yet, mainly for simplicity of implementation and lack of requirements.


``` C
int hostfxr_get_runtime_properties(
    const hostfxr_handle host_context_handle,
    size_t * count,
    const char_t **keys,
    const char_t **values);
```

Returns the full set of all runtime properties for the specified host context.
* `host_context_handle` - the initialized host context. If set to `nullptr` the function will operate on runtime properties of the first host context in the process.
* `count` - in/out parameter which must not be `nullptr`. On input it specifies the size of the the `keys` and `values` buffers. On output it contains the number of entries used from `keys` and `values` buffers - the number of properties returned. If the size of the buffers is too small, the function returns a specific error code and fill the `count` with the number of available properties. If `keys` or `values` is `nullptr` the function ignores the input value of `count` and just returns the number of properties.
* `keys` - buffer which acts as an array of pointers to buffers with keys for the runtime properties.
* `values` - buffer which acts as an array of pointer to buffers with values for the runtime properties.

`keys` and `values` store pointers to buffers which are owned by the host context. The caller should make a copy of it if it needs to store it for anything longer than immediate consumption. The lifetime is only guaranteed until any of the below happens:
  * one of the "run" methods is called on the host context
  * the host context is closed via `hostfxr_close`
  * the value or existence of any property is changed via `hostfxr_set_runtime_property_value`

Note that `hostfxr_set_runtime_property_value` can remove or add new properties, so the number of properties returned is only valid as long as no properties were added/removed.


### Start the runtime

#### Running an application
``` C
int hostfxr_run_app(const hostfxr_handle host_context_handle);
```
Runs the application specified by the `hostfxr_initialize_for_dotnet_command_line`. It is illegal to try to use this function when the host context was initialized through any other way.
* `host_context_handle` - handle to the initialized host context.

The function will return only once the managed application exits.

`hostfxr_run_app` cannot be used in combination with any other "run" function. It can also only be called once.


#### Getting a delegate for runtime functionality
``` C
int hostfxr_get_runtime_delegate(const hostfxr_handle host_context_handle, hostfxr_delegate_type type, void ** delegate);
```
Starts the runtime and returns a function pointer to specified functionality of the runtime.
* `host_context_handle` - handle to the initialized host context.
* `type` - the type of runtime functionality requested
  * `load_assembly_and_get_function_pointer` - entry point which loads an assembly (with dependencies) and returns function pointer for a specified static method. This is the new way to load managed components by the native host. The intent is that the native app will call `hostfxr_get_runtime_delegate(handle, load_assembly_and_get_function_pointer, &helper)` and then use the `helper` to load assembly and get a function pointer to a method. So something like `helper(assembly_path, type_name, method_name, &function_ptr)`. The exact signature and behavior of this runtime helper is TBD.
  * `com_activation` - COM activation entry-point - see [COM activation](https://github.com/dotnet/core-setup/blob/master/Documentation/design-docs/COM-activation.md) for more details.
  * `load_in_memory_assembly` - IJW entry-point - see [IJW activation](https://github.com/dotnet/core-setup/blob/master/Documentation/design-docs/IJW-activation.md) for more details.
  * `winrt_activation` - WinRT activation entry-point - see [WinRT activation](https://github.com/dotnet/core-setup/blob/master/Documentation/design-docs/WinRT-activation.md) for more details.
* `delegate` - when successful, the native function pointer to the requested runtime functionality.

Initially the function will only work if `hostfxr_initialize_for_runtime_config` was used to initialize the host context. Later on this could be relaxed to allow being used in combination with `hostfxr_initialize_for_dotnet_command_line`.  

Initially there might be a limitation of calling this function only once on a given host context to simplify the implementation. Currently we don't have a scenario where it would be absolutely required to support multiple calls.


### Cleanup
``` C
int hostfxr_close(const hostfxr_handle host_context_handle);
```
Closes a host context.
* `host_context_handle` - handle to the initialized host context to close.


### Multiple host contexts interactions

It is important to correctly synchronize some of these operations to achieve the desired API behavior as well as thread safety requirements. The following behaviors will be used to achieve this.

#### Terminology
* `first host context` is the one which is used to load and initialize the CoreCLR runtime in the process. At any given time there can only be one `first host context`.
* `secondary host context` is any other initialized host context when `first host context` already exists in the process.

#### Synchronization
* If there's no `first host context` in the process the first call to `hostfxr_initialize_...` will create a new `first host context`. There can only be one `first host context` in existence at any point in time.
* Calling `hostfxr_initialize...` when `first host context` already exists will always return a `secondary host context`.
* The `first host context` blocks creation of any other host context until it is used to load and initialize the CoreCLR runtime. This means that `hostfxr_initialize...` and subsequently one of the "run" methods must be called on the `first host context` to unblock creation of `secondary host contexts`.
* Calling `hostfxr_initialize...` will block until the `first host context` is initialized, a "run" method is called on it and the CoreCLR is loaded and initialized. The `hostfxr_initialize...` will block potentially indefinitely. The method will block very early on. All of the operations done by the initialize will only happen once it's unblocked.
* `first host context` can fail to initialize the runtime (or anywhere up to that point). If this happens, it's marked as failed and is not considered a `first host context` anymore. This unblocks the potentially waiting `hostfxr_initialize...` calls. In this case the first `hostfxr_initialize...` after the failure will create a new `first host context`.
* `first host context` can be closed using `hostfxr_close` before it is used to initialize the CoreCLR runtime. This is similar to the failure above, the host context is marked as "closed/failed" and is not considered `first host context` anymore. This unblocks any waiting `hostfxr_initialize...` calls.
* Once the `first host context` successfully initialized the CoreCLR runtime it is permanently marked as "successful" and will remain the `first host context` for the lifetime of the process. Such host context should still be closed once not needed via `hostfxr_close`.

#### Invalid usage
* It is invalid to initialize a host context via `hostfxr_initialize...` and then never call `hostfxr_close` on it. An initialized but not closed host context is considered abandoned. Abandoned `first host context` will cause infinite blocking of any future `hostfxr_initialize...` calls.

#### Important scenarios
The above behaviors should make sure that some important scenarios are possible and work reliably.

One such scenario is a COM host on multiple threads. The app is not running any .NET Core yet (no CoreCLR loaded). On two threads in parallel COM activation is invoked which leads to two invocations into the `comhost` to active .NET Core objects. The `comhost` will use the `hostfxr_initialize...` and `hostfxr_get_runtime_delegate` APIs on two threads in parallel then. Only one of them can load and initialize the runtime (and also perform full framework resolution and determine the framework versions and assemblies to load). The other has to become a `secondary host context` and try to conform to the first one. The above behavior of `hostfxr_initialize...` blocking until the `first host context` is done initializing the runtime will make sure of the correct behavior in this case.

At the same time it gives the native app (`comhost` in this case) the ability to query and modify runtime properties in between the `hostfxr_initialize...` and `hostfxr_get_runtime_delegate` calls on the `first host context`.


### Support for older versions

Since `hostfxr` and the other components of hosting layers are versioned independently there are several interesting cases of version mismatches:

#### muxer/`apphost` versus `hostfxr`
For muxer it should almost always match, but `apphost` can be different. That is, it's perfectly valid to use older 2.* `apphost` with a new 3.0 `hostfxr`. The opposite should be rare, but in theory can happen as well. To keep the code simple both muxer and `apphost` will keep using the existing 2.* APIs on `hostfxr` even in situation where both are 3.0 and thus could start using the new APIs.

`hostfxr` must be backward compatible and support 2.* APIs.

Potentially we could switch just `apphost` to use the new APIs (since it doesn't have the same compatibility burden as the muxer), but it's probably safer to not do that.

#### `hostfxr` versus `hostpolicy`
It should really only happen that `hostfxr` is equal or newer than `hostpolicy`. The opposite should be very rare. In any case `hostpolicy` should support existing 2.* APIs and thus the rare case will keep working anyway.

The interesting case is 3.0 `hostfxr` using 2.* `hostpolicy`. This will be very common, basically any 2.* app running on a machine with 3.0 installed will be in that situation. This case has two sub-cases:
* `hostfxr` is invoked using one of the 2.* APIs. In this case the simple solution is to keep using the 2.* `hostpolicy` APIs always.
* `hostfxr` is invoked using one of the new 3.0 APIs (like `hostfxr_initialize...`). In this case it's not possible to completely support the new APIs, since they require new functionality from `hostpolicy`. For now the `hostfxr` should simply fail.  
It is in theory possible to support some kind of emulation mode where for some scenarios the new APIs would work even with old `hostpolicy`, but for simplicity it's better to start with just failing.

#### Implementation of existing 2.* APIs in `hostfxr`
The existing 2.* APIs in `hostfxr` could switch to internally use the new functionality and in turn use the new 3.0 `hostpolicy` APIs. The tricky bit here is scenario like this:
* 3.0 App is started via `apphost` or muxer which as mentioned above will keep on using the 2.* `hostfxr` APIs. This will load CoreCLR into the process.
* COM component is activated in the same process. This will go through the new 3.0 `hostfxr` APIs, and to work correctly will require the internal representation of the `first host context`.

If the 2.* `hostfxr` APIs would continue to use the old 2.* `hostpolicy` APIs even if `hostpolicy` is new, then the above scenario will be hard to achieve as there will be no `first host context`. `hostpolicy` could somehow "emulate" the `first host context`, but without `hostpolicy` cooperation this would be hard.

On the other hand switching to use the new `hostpolicy` APIs even in 2.* `hostfxr` APIs is risky for backward compatibility. This will have to be decided during implementation.


### Samples
All samples assume that the native host has found the `hostfxr`, loaded it and got the exports (possibly by using the `nethost`).
Samples in general ignore error handling.

#### Running app with additional runtime properties
``` C++
hostfxr_initialize_parameters params;
params.size = sizeof(params);
params.host_path = get_path_to_the_host_exe(); // Path to the current executable
params.dotnet_root = get_directory(get_directory(get_directory(hostfxr_path))); // Three levels up from hostfxr typically

hostfxr_handle host_context_handle;
hostfxr_initialize_for_dotnet_command_line(
    _argc_,
    _argv_,
    &params,
    &host_context_handle);

size_t buffer_used = 0;
if (hostfxr_get_runtime_property(host_context_handle, "TEST_PROPERTY", nullptr, 0, &buffer_used) == HostApiMissingProperty)
{
    hostfxr_set_runtime_property(host_context_handle, "TEST_PROPERTY", "TRUE");
}

hostfxr_run_app(host_context_handle);

hostfxr_close(host_context_handle);
```

## Impact on hosting components

The exact impact on the `hostfxr`/`hostpolicy` interface needs to be investigated. The assumption is that new APIs will have to be added to `hostpolicy` to implement the proposed functionality.

Part if this investigation will also be compatibility behavior. Currently "any" version of `hostfxr` needs to be able to use "any" version of `hostpolicy`. But the proposed functionality will need both new `hostfxr` and new `hostpolicy` to work. It is likely the proposed APIs will fail if the app resolves to a framework with old `hostpolicy` without the necessary new APIs. Part of the investigation will be if it's feasible to use the new `hostpolicy` APIs to implement existing old `hostfxr` APIs.

# Open issues
* Maybe add `apphost_get_hostfxr_path` on the existing `apphost` - this is to make it even easier to implement custom hosting for entire managed app as the custom host would not need to carry a `nethost` and would get a 100% compatible behavior by using the same `apphost` as the app itself.
