# Native hosting

Native hosting is the ability to host the .NET runtime in an arbitrary process, one which didn't start from .NET Core produced binaries.

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
App (native or .NET Core both) which needs to use some of the other services provided by the .NET Core hosting components. For example the ability to locate available SDKs and so on.


## Existing support
* **C-style ABI in `coreclr`**
`coreclr` exposes ABI to host the .NET runtime and run managed code already using C-style APIs. See this [header file](https://github.com/dotnet/runtime/blob/main/src/coreclr/hosts/inc/coreclrhost.h) for the exposed functions.
This API requires the native host to locate the runtime and to fully specify all startup parameters for the runtime. There's no inherent interoperability between these APIs and the .NET SDK.
* **COM-style ABI in `coreclr`**
`coreclr` exposes COM-style ABI to host the .NET runtime and perform a wide range of operations on it. See this [header file](https://github.com/dotnet/runtime/blob/main/src/coreclr/pal/prebuilt/inc/mscoree.h) for more details.
Similarly to the C-style ABI the COM-style ABI also requires the native host to locate the runtime and to fully specify all startup parameters.
There's no inherent interoperability between these APIs and the .NET SDK.
The COM-style ABI is deprecated and should not be used going forward.
* **`hostfxr` and `hostpolicy` APIs**
The hosting components of .NET Core already exposes some functionality as C-style ABI on either the `hostfxr` or `hostpolicy` libraries. These can execute application, determine available SDKs, determine native dependency locations, resolve component dependencies and so on.
Unlike the above `coreclr` based APIs these don't require the caller to fully specify all startup parameters, instead these APIs understand artifacts produced by .NET SDK making it much easier to consume SDK produced apps/libraries.
The native host is still required to locate the `hostfxr` or `hostpolicy` libraries. These APIs are also designed for specific narrow scenarios, any usage outside of these bounds is typically not possible.


## Scope
This document focuses on hosting which cooperates with the .NET SDK and consumes the artifacts produced by building the managed app/libraries directly. It completely ignores the COM-style ABI as it's hard to use from some programming languages.

As such the document explicitly excludes any hosting based on directly loading `coreclr`. Instead it focuses on using the existing .NET Core hosting components in new ways. For details on the .NET Core hosting components see [this document](https://github.com/dotnet/runtime/tree/main/docs/design/features/host-components.md).


## Longer term vision
This section describes how we think the hosting components should evolve. It is expected that this won't happen in any single release, but each change should fit into this picture and hopefully move us closer to this goal. It is also expected that existing APIs won't change (no breaking changes) and thus it can happen that some of the APIs don't fit nicely into the picture.

The hosting component APIs should provide functionality to cover a wide range of applications. At the same time it needs to stay in sync with what .NET Core SDK can produce so that the end-to-end experience is available.

The overall goal of hosting components is to enable loading and executing managed code, this consists of four main steps:
* **Locate and load hosting components** - How does a native host find and load the hosting components libraries.
* **Locate the managed code and all its dependencies** - Determining where the managed code comes from (application, component, ...), what frameworks it relies on (framework resolution) and what dependencies it needs (`.deps.json` processing)
* **Load the managed code into the process** - Specify the environment and settings for the runtime, locating or starting the runtime, deciding how to load the managed code into the runtime (which `AssemblyLoadContext` for example) and actually loading the managed code and its dependencies.
* **Accessing and executing managed code** - Locate the managed entry point (for example assembly entry point `Main`, or a specific method to call, or some other means to transition control to the managed code) and exposing it into the native code (function pointer, COM interface, direct execution, ...).

To achieve these we will structure the hosting components APIs into similar (but not exactly) 4 buckets.

### Locating hosting components
This means providing APIs to find and load the hosting components. The end result should be that the native host has the right version of `hostfxr` loaded and can call its APIs. The exact solution is dependent on the native host application and its tooling. Ideally we would have a solution which works great for several common environments like C/C++ apps built in VS, C/C++ apps on Mac/Linux built with CMake (or similar) and so on.

First step in this direction is the introduction of `nethost` library described below.

### Initializing host context
The end result of this step should be an initialized host context for a given managed code input which the native host can use to load the managed code and execute it. The step should not actually load any runtime or managed code into the process. Finally this step must handle both processes which don't have any runtime loaded as well as processes which already have runtime loaded.

Functionality performed by this step:
* Locating and understanding the managed code input (app/component)
* Resolving frameworks or handling self-contained
* Resolving dependencies (`.deps.json` processing)
* Populating runtime properties

The vision is to expose a set of APIs in the form of `hostfxr_initialize_for_...` which would all perform the above functionality and would differ on how the managed code is located and used. So there should be a different API for loading applications and components at the very least. Going forward we can add API which can initialize the context from in-memory configurations, or API which can initialize an "empty" context (no custom code involved, just frameworks) and so on.

First step in this direction is the introduction of these APIs as described below:
* `hostfxr_initialize_for_dotnet_command_line` - this is the initialize for an application. The API is a bit more complicated as it allows for command line parsing as well. Eventually we might want to add another "initialize app" API without the command line support.
* `hostfxr_initialize_for_runtime_config` - this is the initialize for a component (naming is not ideal, but can't be changed anymore).

After this step it should be possible for the native host to inspect and potentially modify runtime properties. APIs like `hostfxr_get_runtime_property_value` and similar described below are an example of this.

### Loading managed code
This step might be "virtual" in the sense that it's a way to setup the context for the desired functionality. In several scenarios it's not practical to be able to perform this step in separation from the "execute code" step below. The goal of this step is to determine which managed code to load, where it should be loaded into, and to setup the correct inputs so that runtime can perform the right dependency resolution.

Logically this step finds/loads the runtime and initializes it before it can load any managed code into the process.

The main flexibility this step should provide is to determine which assembly load context will be used for loading which code:
* Load all managed code into the default load context
* Load the specified custom managed code into an isolated load context

Eventually we could also add functionality for example to setup the isolated load context for unloadability and so on.

Note that there are no APIs proposed in this document which would only perform this step for now. All the APIs so far fold this step into the next one and perform both loading and executing of the managed code in one step. We should try to make this step more explicit in the future to allow for greater flexibility.

After this step it is no longer possible to modify runtime properties (inspection is still allowed) since the runtime has been loaded.

### Accessing and executing managed code
The end result of this step is either execution of the desired managed code, or returning a native callable representation of the managed code.

The different options should include at least:
* running an application (where the API takes over the thread and runs the application on behalf of the calling thread) - the `hostfxr_run_app` API described bellow is an example of this.
* getting a native function pointer to a managed method - the `hostfxr_get_runtime_delegate` and the `hdt_load_assembly_and_get_function_pointer` described below is an example of this.
* getting a native callable interface (COM) to a managed instance - the `hostfxr_get_runtime_delegate` and the `hdt_com_activation` described below is an example of this.
* and more...

### Combinations
Ideally it should be possible to combine the different types of initialization, loading and executing of managed code in any way the native host desires. In reality not all combinations are possible or practical. Few examples of combinations which we should probably NOT support (note that this may change in the future):
* running a component as an application - runtime currently makes too many assumptions on what it means to "Run an application"
* loading application into an isolated load context - for now there are too many limitations in the frameworks around secondary load contexts (several large framework features don't work well in a secondary ALC), so the hosting components should prevent this as a way to prevent users from getting into a bad situation.
* loading multiple copies of the runtime into the process - support for side-by-side loading of different .NET Core runtime in a single process is not something we want to implement (at least yet). Hosting components should not allow this to make the user experience predictable. This means that full support for loading self-contained components at all times is not supported.

Lot of other combinations will not be allowed simply as a result of shipping decisions. There's only so much functionality we can fit into any given release, so many combinations will be explicitly disabled to reduce the work necessary to actually ship this. The document below should describe which combinations are allowed in which release.

## High-level proposal
In .NET Core 3.0 the hosting components (see [here](https://github.com/dotnet/runtime/tree/main/docs/design/features/host-components.md)) ships with several hosts. These are binaries which act as the entry points to the .NET Core hosting components/runtime:
* The "muxer" (`dotnet.exe`)
* The `apphost` (`.exe` which is part of the app)
* The `comhost` (`.dll` which is part of the app and acts as COM server)
* The `ijwhost` (`.dll` consumed via `.lib` used by IJW assemblies)

Every one of these hosts serve different scenario and expose different APIs. The one thing they have in common is that their main purpose is to find the right `hostfxr`, load it and call into it to execute the desired scenario. For the most part all these hosts are basically just wrappers around functionality provided by `hostfxr`.

The proposal is to add a new host library `nethost` which can be used by native host to easily locate `hostfxr`. Going forward the library could also include easy-to-use APIs for common scenarios - basically just a simplification of the `hostfxr` API surface.

At the same time add the ability to pass additional runtime properties when starting the runtime through the hosting components APIs (starting app, loading component). This can be used by the native host to:
* Register startup hook without modifying environment variables (which are inherited by child processes)
* Introduce new runtime knobs which are only available for native hosts without the need to update the hosting components APIs every time.


*Technical note: All strings in the proposed APIs are using the `char_t` in this document for simplicity. In real implementation they are of the type `pal::char_t`. In particular:*
* *On Windows - they are `WCHAR *` using `UTF16` encoding*
* *On Linux/macOS - they are `char *` using `UTF8` encoding*


## New host binary for finding `hostfxr`
New library `nethost` which provides a way to locate the right `hostfxr`.
This is a dynamically loaded library (`.dll`, `.so`, `.dylib`). For ease of use there is a header file for C/C++ apps as well as `.lib` for easy linking on Windows.
Native hosts ship this library as part of the app. Unlike the `apphost`, `comhost` and `ijwhost`, the `nethost` will not be directly supported by the .NET SDK since it's target usage is not from .NET Core apps.

The `nethost` is part of the `Microsoft.NETCore.DotNetAppHost` package. Users are expected to either download the package directly or rely on .NET SDK to pull it down.

The binary itself should be signed by Microsoft as there will be no support for modifying the binary as part of custom application build (unlike `apphost`).


### Locate `hostfxr`
``` C++
struct get_hostfxr_parameters {
    size_t size;
    const char_t * assembly_path;
    const char_t * dotnet_root;
};

int get_hostfxr_path(
    char_t * result_buffer,
    size_t * buffer_size,
    const get_hostfxr_parameters * parameters);
```

This API locates the `hostfxr` library and returns its path by populating `result_buffer`.

* `result_buffer` - Buffer that will be populated with the hostfxr path, including a null terminator.
* `buffer_size` - On input this points to the size of the `result_buffer` in `char_t` units. On output this points to the number of `char_t` units used from the `result_buffer` (including the null terminator). If `result_buffer` is `NULL` the input value is ignored and only the minimum required size in `char_t` units is set on output.
* `parameters` - Optional. Additional parameters that modify the behaviour for locating the `hostfxr` library. If `NULL`, `hostfxr` is located using the environment variable or global registration
  * `size` - Size of the structure. This is used for versioning and should be set to `sizeof(get_hostfxr_parameters)`.
  * `assembly_path` - Optional. Path to the application or to the component's assembly.
    * If specified, `hostfxr` is located as if the `assembly_path` is an application with `apphost`
  * `dotnet_root` - Optional. Path to the root of a .NET Core installation (i.e. folder containing the dotnet executable).
    * If specified, `hostfxr` is located as if an application is started using `dotnet app.dll`, which means it will be searched for under the `dotnet_root` path and the `assembly_path` is ignored.

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
* Load managed component and get native function pointer for managed method
  * From native app start the runtime and load an assembly
  * The assembly is loaded in isolation and with all its dependencies as directed by `.deps.json`
  * The native app can get back a native function pointer which calls specified managed method
* Get native function pointer for managed method
  * From native code get a native function pointer to already loaded managed method

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
typedef void* hostfxr_handle;

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

This function supports both framework-dependent and self-contained applications.

*Note: This is effectively a replacement for `hostfxr_main_startupinfo` and `hostfxr_main`. Currently it is not a goal to fully replace these APIs because they also support SDK commands which are special in lot of ways and don't fit well with the rest of the native hosting. There's no scenario right now which would require the ability to issue SDK commands from a native host. That said nothing in this proposal should block enabling even SDK commands through these APIs.*


``` C
int hostfxr_initialize_for_runtime_config(
    const char_t * runtime_config_path,
    const hostfxr_initialize_parameters * parameters,
    hostfxr_handle * host_context_handle
);
```

This function would load the specified `.runtimeconfig.json`, resolve all frameworks, resolve all the assets from those frameworks and then prepare runtime initialization where the TPA contains only frameworks. Note that this case does NOT consume any `.deps.json` from the app/component (only processes the framework's `.deps.json`). This entry point is intended for `comhost`/`ijwhost`/`nethost` and similar scenarios.
* `runtime_config_path` - path to the `.runtimeconfig.json` file to process. Unlike with `hostfxr_initialize_for_dotnet_command_line`, any `.deps.json` from the app/component will not be processed by the hosting components during the initialize call.
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

The specified `.runtimeconfig.json` must be for a framework dependent component. That is it must specify at least one shared framework in its `frameworks` section. Self-contained components are not supported.

### Inspect and modify host context

#### Runtime properties
These functions allow the native host to inspect and modify runtime properties.
* If the `host_context_handle` represents the first initialized context in the process, these functions expose all properties from runtime configurations as well as those computed by the hosting components. These functions will allow modification of the properties via `hostfxr_set_runtime_property_value`.
* If the `host_context_handle` represents any other context (so not the first one), these functions expose only properties from runtime configuration. These functions won't allow modification of the properties.

It is possible to access runtime properties of the first initialized context in the process at any time (for reading only), by specifying `NULL` as the `host_context_handle`.

``` C
int hostfxr_get_runtime_property_value(
    const hostfxr_handle host_context_handle,
    const char_t * name,
    const char_t ** value);
```

Returns the value of a runtime property specified by its name.
* `host_context_handle` - the initialized host context. If set to `NULL` the function will operate on runtime properties of the first host context in the process.
* `name` - the name of the runtime property to get. Must not be `NULL`.
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
* `host_context_handle` - the initialized host context. (Must not be `NULL`)
* `name` - the name of the runtime property to set. Must not be `NULL`.
* `value` - the value of the property to set. If the property already has a value in the host context, this function will overwrite it. When set to `NULL` and if the property already has a value then the property is "unset" - removed from the runtime property collection.

Setting properties is only supported on the first host context in the process. This is really a limitation of the runtime for which the runtime properties are immutable. Once the first host context is initialized and starts a runtime there's no way to change these properties. For now we will not consider the scenario where the host context is initialized but the runtime hasn't started yet, mainly for simplicity of implementation and lack of requirements.


``` C
int hostfxr_get_runtime_properties(
    const hostfxr_handle host_context_handle,
    size_t * count,
    const char_t **keys,
    const char_t **values);
```

Returns the full set of all runtime properties for the specified host context.
* `host_context_handle` - the initialized host context. If set to `NULL` the function will operate on runtime properties of the first host context in the process.
* `count` - in/out parameter which must not be `NULL`. On input it specifies the size of the the `keys` and `values` buffers. On output it contains the number of entries used from `keys` and `values` buffers - the number of properties returned. If the size of the buffers is too small, the function returns a specific error code and fill the `count` with the number of available properties. If `keys` or `values` is `NULL` the function ignores the input value of `count` and just returns the number of properties.
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
  * `hdt_load_assembly_and_get_function_pointer` - entry point which loads an assembly (with dependencies) and returns function pointer for a specified static method. See below for details (Loading and calling managed components)
  * `hdt_com_activation`, `hdt_com_register`, `hdt_com_unregister` - COM activation entry-points - see [COM activation](https://github.com/dotnet/runtime/tree/main/docs/design/features/COM-activation.md) for more details.
  * `hdt_load_in_memory_assembly` - IJW entry-point - see [IJW activation](https://github.com/dotnet/runtime/tree/main/docs/design/features/IJW-activation.md) for more details.
  * `hdt_winrt_activation` **[.NET 3.\* only]** - WinRT activation entry-point - see [WinRT activation](https://github.com/dotnet/runtime/tree/main/docs/design/features/WinRT-activation.md) for more details. The delegate is not supported for .NET 5 and above.
  * `hdt_get_function_pointer` **[.NET 5 and above]** - entry-point which finds a managed method and returns a function pointer to it. See below for details (Calling managed function).
* `delegate` - when successful, the native function pointer to the requested runtime functionality.

In .NET Core 3.0 the function only works if `hostfxr_initialize_for_runtime_config` was used to initialize the host context.
In .NET 5 the function also works if `hostfxr_initialize_for_dotnet_command_line` was used to initialize the host context. Also for .NET 5 it will only be allowed to request `hdt_load_assembly_and_get_function_pointer` or `hdt_get_function_pointer` on a context initialized via `hostfxr_initialize_for_dotnet_command_line`, all other runtime delegates will not be supported in this case.


### Cleanup
``` C
int hostfxr_close(const hostfxr_handle host_context_handle);
```
Closes a host context.
* `host_context_handle` - handle to the initialized host context to close.


### Loading and calling managed components
To load managed components from native app directly (not using COM or WinRT) the hosting components exposes a new runtime helper/delegate `hdt_load_assembly_and_get_function_pointer`. Calling the `hostfxr_get_runtime_delegate(handle, hdt_load_assembly_and_get_function_pointer, &helper)` returns a function pointer to the runtime helper with this signature:
```C
int load_assembly_and_get_function_pointer_fn(
    const char_t *assembly_path,
    const char_t *type_name,
    const char_t *method_name,
    const char_t *delegate_type_name,
    void         *reserved,
    /*out*/ void **delegate)
```

Calling this function will load the specified assembly in isolation (into its own `AssemblyLoadContext`) and it will use `AssemblyDependencyResolver` on it to provide dependency resolution. Once loaded it will find the specified type and method and return a native function pointer to that method. The method's signature can be specified via the delegate type name.
* `assembly_path` - Path to the assembly to load. In case of complex component, this should be the main assembly of the component (the one with the `.deps.json` next to it). Note that this does not have to be the assembly from which the `type_name` and `method_name` are.
* `type_name` - Assembly qualified type name to find
* `method_name` - Name of the method on the `type_name` to find. The method must be `static` and must match the signature of `delegate_type_name`.
* `delegate_type_name` - Assembly qualified delegate type name for the method signature, or null. If this is null, the method signature is assumed to be:
    ```C#
    public delegate int ComponentEntryPoint(IntPtr args, int sizeBytes);
    ```
    This maps to native signature:
    ```C
    int component_entry_point_fn(void *arg, int32_t arg_size_in_bytes);
    ```
    **[.NET 5 and above]** The `delegate_type_name` can be also specified as `UNMANAGEDCALLERSONLY_METHOD` (defined as `(const char_t*)-1`) which means that the managed method is marked with `UnmanagedCallersOnlyAttribute`.
* `reserved` - parameter reserved for future extensibility, currently unused and must be `NULL`.
* `delegate` - out parameter which receives the native function pointer to the requested managed method.

The helper will always load the assembly into an isolated load context. This is the case regardless if the requested assembly is also available in the default load context or not.

**[.NET 5 and above]** It is allowed to call this helper on a host context which came from `hostfxr_initialize_for_dotnet_command_line` which will make all the application assemblies available in the default load context. In such case it is recommended to only use this helper for loading plugins external to the application. Using the helper to load assembly from the application itself will lead to duplicate copies of assemblies and duplicate types.

It is allowed to call the returned runtime helper many times for different assemblies or different methods from the same assembly. It is not required to get the helper every time. The implementation of the helper will cache loaded assemblies, so requests to load the same assembly twice will load it only once and reuse it from that point onward. Ideally components should not take a dependency on this behavior, which means components should not have global state. Global state in components is typically just cause for problems. For example it may create ordering issues or unintended side effects and so on.

The returned native function pointer to managed method has the lifetime of the process and can be used to call the method many times over. Currently there's no way to unload the managed component or otherwise free the native function pointer. Such support may come in future releases.


### Calling managed function **[.NET 5 and above]**
In .NET 5 the hosting components add a new functionality which allows just getting a native function pointer for already available managed method. This is implemented in a runtime helper `hdt_get_function_pointer`. Calling the `hostfxr_get_runtime_delegate(handle, hdt_get_function_pointer, &helper)` returns a function pointer to the runtime helper with this signature:
```C
int get_function_pointer_fn(
    const char_t *type_name,
    const char_t *method_name,
    const char_t *delegate_type_name,
    void         *load_context,
    void         *reserved,
    /*out*/ void **delegate)
```

Calling this function will find the specified type in the default load context, locate the required method on it and return a native function pointer to that method. The method's signature can be specified via the delegate type name.
* `type_name` - Assembly qualified type name to find
* `method_name` - Name of the method on the `type_name` to find. The method must be `static` and must match the signature of `delegate_type_name`.
* `delegate_type_name` - Assembly qualified delegate type name for the method signature, or null. If this is null, the method signature is assumed to be:
    ```C#
    public delegate int ComponentEntryPoint(IntPtr args, int sizeBytes);
    ```
    This maps to native signature:
    ```C
    int component_entry_point_fn(void *arg, int32_t arg_size_in_bytes);
    ```
    The `delegate_type_name` can be also specified as `UNMANAGEDCALLERSONLY_METHOD` (defined as `(const char_t*)-1`) which means that the managed method is marked with `UnmanagedCallersOnlyAttribute`.
* `load_context` - eventually this parameter should support specifying which load context should be used to locate the type/method specified in previous parameters. For .NET 5 this parameter must be `NULL` and the API will only locate the type/method in the default load context.
* `reserved` - parameter reserved for future extensibility, currently unused and must be `NULL`.
* `delegate` - out parameter which receives the native function pointer to the requested managed method.

The helper will lookup the `type_name` from the default load context (`AssemblyLoadContext.Default`) and then return method on it. If the type and method lookup requires assemblies which have not been loaded by the Default ALC yet, this process will resolve them against the Default ALC and load them there (most likely from TPA). This helper will not register any additional assembly resolution logic onto the Default ALC, it will solely rely on the existing functionality of the Default ALC.

It is allowed to ask for this helper on any valid host context. Because the helper operates on default load context only it should mostly be used with context initialized via `hostfxr_initialize_for_dotnet_command_line` as in that case the default load context will have the application code available in it. Contexts initialized via `hostfxr_initialize_for_runtime_config` have only the framework assemblies available in the default load context. The `type_name` must resolve within the default load context, so in the case where only framework assemblies are loaded into the default load context it would have to come from one of the framework assemblies only.

It is allowed to call the returned runtime helper many times for different types or methods. It is not required to get the helper every time.

The returned native function pointer to managed method has the lifetime of the process and can be used to call the method many times over. Currently there's no way to "release" the native function pointer (and the respective managed delegate), this functionality may be added in a future release.


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

### API usage
The `hostfxr` exports are defined in the [hostfxr.h](https://github.com/dotnet/runtime/blob/main/src/native/corehost/hostfxr.h) header file.
The runtime helper and method signatures for loading managed components are defined in [coreclr_delegates.h](https://github.com/dotnet/runtime/blob/main/src/native/corehost/coreclr_delegates.h) header file.

Currently we don't plan to ship these files, but it's possible to take them from the repo and use it.


### Support for older versions

Since `hostfxr` and the other hosting components are versioned independently there are several interesting cases of version mismatches:

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
if (hostfxr_get_runtime_property(host_context_handle, "TEST_PROPERTY", NULL, 0, &buffer_used) == HostApiMissingProperty)
{
    hostfxr_set_runtime_property(host_context_handle, "TEST_PROPERTY", "TRUE");
}

hostfxr_run_app(host_context_handle);

hostfxr_close(host_context_handle);
```

#### Getting a function pointer to call a managed method
```C++
using load_assembly_and_get_function_pointer_fn = int (STDMETHODCALLTYPE *)(
    const char_t *assembly_path,
    const char_t *type_name,
    const char_t *method_name,
    const char_t *delegate_type_name,
    void *reserved,
    void **delegate);

hostfxr_handle host_context_handle;
hostfxr_initialize_for_runtime_config(config_path, NULL, &host_context_handle);

load_assembly_and_get_function_pointer_fn runtime_delegate = NULL;
hostfxr_get_runtime_delegate(
    host_context_handle,
    hostfxr_delegate_type::load_assembly_and_get_function_pointer,
    (void **)&runtime_delegate);

using managed_entry_point_fn = int (STDMETHODCALLTYPE *)(void *arg, int argSize);

managed_entry_point_fn entry_point = NULL;
runtime_delegate(assembly_path,
                 type_name,
                 method_name,
                 NULL,
                 NULL,
                 (void **)&entry_point);

ArgStruct arg;
entry_point(&arg, sizeof(ArgStruct));

hostfxr_close(host_context_handle);
```

## Impact on hosting components

The exact impact on the `hostfxr`/`hostpolicy` interface needs to be investigated. The assumption is that new APIs will have to be added to `hostpolicy` to implement the proposed functionality.

Part if this investigation will also be compatibility behavior. Currently "any" version of `hostfxr` needs to be able to use "any" version of `hostpolicy`. But the proposed functionality will need both new `hostfxr` and new `hostpolicy` to work. It is likely the proposed APIs will fail if the app resolves to a framework with old `hostpolicy` without the necessary new APIs. Part of the investigation will be if it's feasible to use the new `hostpolicy` APIs to implement existing old `hostfxr` APIs.

## Incompatible with trimming
Native hosting support on managed side is disabled by default on trimmed apps. Native hosting and trimming are incompatible since the trimmer cannot analyze methods that are called by native hosts. Native hosting support for trimming can be managed through the [feature switch](https://github.com/dotnet/runtime/blob/main/docs/workflow/trimming/feature-switches.md) settings specific to each native host.