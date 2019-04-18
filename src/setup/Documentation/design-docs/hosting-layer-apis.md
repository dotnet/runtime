# Hosting layer APIs

Functionality for advanced hosting scenarios is exposed on the `hostfxr` and `hostpolicy` libraries through C-style APIs.

The `char_t` strings in the below API descriptions are defined based on the platform:
* Windows     - UTF-16 (2-byte `wchar_t`)
  * Note that `wchar_t` is defined as a [native type](https://docs.microsoft.com/cpp/build/reference/zc-wchar-t-wchar-t-is-native-type), which is the default in Visual Studio.
* Unix        - UTF-8  (1-byte `char`)

## Host FXR

All exported functions and function pointers in the `hostfxr` library use the `__cdecl` calling convention on the x86 platform.

### .NET Core 1.0+

``` C
int hostfxr_main(const int argc, const char_t *argv[])
```

Run an application.
* `argc` / `argv` - command-line arguments

This function does not return until the application completes execution.

### .NET Core 2.0+

``` C
int32_t hostfxr_resolve_sdk(
    const char_t *exe_dir,
    const char_t *working_dir,
    char_t buffer[],
    int32_t buffer_size)
```

Obsolete. Use `hostfxr_resolve_sdk2`.

### .NET Core 2.1+

``` C
int hostfxr_main_startupinfo(
    const int argc,
    const char_t *argv[],
    const char_t *host_path,
    const char_t *dotnet_root,
    const char_t *app_path)
```

Run an application.
* `argc` / `argv` - command-line arguments
* `host_path` - path to the host application
* `dotnet_root` - path to the .NET Core installation root
* `app_path` - path to the application to run

This function does not return until the application completes execution.

``` C
enum hostfxr_resolve_sdk2_flags_t
{
    disallow_prerelease = 0x1,
};

enum hostfxr_resolve_sdk2_result_key_t
{
    resolved_sdk_dir = 0,
    global_json_path = 1,
};

typedef void (*hostfxr_resolve_sdk2_result_fn)(
    hostfxr_resolve_sdk2_result_key_t key,
    const char_t* value);

int32_t hostfxr_resolve_sdk2(
    const char_t *exe_dir,
    const char_t *working_dir,
    int32_t flags,
    hostfxr_resolve_sdk2_result_fn result)
```

Determine the directory location of the SDK, accounting for global.json and multi-level lookup policy.
* `exe_dir` - main directory where SDKs are located in `sdk\[version]` sub-folders.
* `working_dir` - directory where the search for `global.json` will start and proceed upwards
* `flags` - flags that influence resolution
  * `disallow_prerelease` - do not allow resolution to return a pre-release SDK version unless a pre-release version was specified via `global.json`
* `result` - callback invoked to return resolved values. The callback may be invoked more than once. Strings passed to the callback are valid only for the duration of the call.

If resolution succeeds, `result` will be invoked with `resolved_sdk_dir` key and the value will hold the path to the resolved SDK directory. If resolution does not succeed, `result` will be invoked with `resolved_sdk_dir` key and the value will be `nullptr`.

If `global.json` is used, `result` will be invoked with `global_json_path` key and the value will hold the path to `global.json`. If there was no `global.json` found, or the contents of global.json did not impact resolution (e.g. no version specified), then `result` will not be invoked with `global_json_path` key.

``` C
typedef void (*hostfxr_get_available_sdks_result_fn)(
    int32_t sdk_count,
    const char_t *sdk_dirs[]);

int32_t hostfxr_get_available_sdks(
    const char_t *exe_dir,
    hostfxr_get_available_sdks_result_fn result)
```

Get the list of all available SDKs ordered by ascending version.
* `exe_dir` - path to the dotnet executable
* `result` - callback invoked to return the list of SDKs by their directory paths. String array and its elements are valid only for the duration of the call.

``` C
int32_t hostfxr_get_native_search_directories(
    const int argc,
    const char_t *argv[],
    char_t buffer[],
    int32_t buffer_size,
    int32_t *required_buffer_size)
```

Get the native search directories of the runtime based upon the specified app.
* `argc` / `argv` - command-line arguments
* `buffer` - buffer to populate with the native search directories (including a null terminator).
* `buffer_size` - size of `buffer` in `char_t` units
* `required_buffer_size` - if `buffer` is too small, this will be populated with the minimum required buffer size (including a null terminator). Otherwise, this will be set to 0.

The native search directories will be a list of paths separated by `PATH_SEPARATOR`, which is a semicolon (;) on Windows and a colon (:) otherwise.

If `buffer_size` is less than the minimum required buffer size, this function will return `HostApiBufferTooSmall` and `buffer` will be unchanged.

### .NET Core 3.0+

``` C
typedef void(*hostfxr_error_writer_fn)(const char_t *message);

hostfxr_error_writer_fn hostfxr_set_error_writer(hostfxr_error_writer_fn error_writer)
```

Set a callback which will be used to report error messages. By default no callback is registered and the errors are written to standard error.
* `error_writer` - callback function which will be invoked every time an error is reported. When set to `nullptr`, this function unregisters any previously registered callback and the default behaviour is restored.

The return value is the previously registered callback (which is now unregistered) or `nullptr` if there was no previously registered callback.

The error writer is registered per-thread. On each thread, only one callback can be registered. Subsequent registrations overwrite the previous ones.

If `hostfxr` invokes functions in `hostpolicy` as part of its operation, the error writer will be propagated to `hostpolicy` for the duration of the call. This means that errors from both `hostfxr` and `hostpolicy` will be reported through the same error writer.


## Host Policy

All exported functions and function pointers in the `hostpolicy` library use the `__cdecl` calling convention on the x86 platform.

### .NET Core 1.0+

``` C
int corehost_load(host_interface_t *init)
```

Initialize `hostpolicy`. This stores information that will be required to do all the processing necessary to start CoreCLR, but it does not actually do any of that processing.
* `init` - structure defining how the library should be initialized

If already initalized, this function returns success without reinitializing (`init` is ignored).

``` C
int corehost_main(const int argc, const char_t* argv[])
```

Run an application.
* `argc` / `argv` - command-line arguments

This function does not return until the application completes execution. It will shutdown CoreCLR after the application executes.

``` C
int corehost_unload()
```

Uninitialize `hostpolicy`.

### .NET Core 2.1+
``` C
int corehost_main_with_output_buffer(
    const int argc,
    const char_t *argv[],
    char_t buffer[],
    int32_t buffer_size,
    int32_t *required_buffer_size)
```

Run a host command and return the output. `corehost_load(init)` should have been called with `init->host_command` set. This function operates in the hosting layer and does not actually run CoreCLR.
* `argc` / `argv` - command-line arguments
* `buffer` - buffer to populate with the output (including a null terminator).
* `buffer_size` - size of `buffer` in `char_t` units
* `required_buffer_size` - if `buffer` is too small, this will be populated with the minimum required buffer size (including a null terminator). Otherwise, this will be set to 0.

If `buffer_size` is less than the minimum required buffer size, this function will return `HostApiBufferTooSmall` and `buffer` will be unchanged.

### .NET Core 3.0+

``` C
int corehost_get_coreclr_delegate(coreclr_delegate_type type, void **delegate)
```

Get a delegate for CoreCLR functionality
* `type` - requested type of runtime functionality
* `delegate` - function pointer to the requested runtime functionality

``` C
typedef void(*corehost_resolve_component_dependencies_result_fn)(
    const char_t *assembly_paths,
    const char_t *native_search_paths,
    const char_t *resource_search_paths);

int corehost_resolve_component_dependencies(
    const char_t *component_main_assembly_path,
    corehost_resolve_component_dependencies_result_fn result)
```

Resolve dependencies for the specified component.
* `component_main_assembly_path` - path to the component
* `result` - callback which will receive the results of the component dependency resolution

See [Component dependency resolution support in host](host-component-dependencies-resolution.md)

``` C
typedef void(*corehost_error_writer_fn)(const char_t *message);

corehost_error_writer_fn corehost_set_error_writer(corehost_error_writer_fn error_writer)
```

Set a callback which will be used to report error messages. By default no callback is registered and the errors are written to standard error.
* `error_writer` - callback function which will be invoked every time an error is reported. When set to `nullptr`, this function unregisters any previously registered callback and the default behaviour is restored.

The return value is the previouly registered callback (which is now unregistered) or `nullptr` if there was no previously registered callback.

The error writer is registered per-thread. On each thread, only one callback can be registered. Subsequent registrations overwrite the previous ones.

### [Proposed] .NET Core 3.0+

#### Removal

`corehost_get_coreclr_delegate` will be removed and the equivalent functionality provided through the proposed additions below. Since this function is new in 3.0, it should not be required for backwards compatibility.

#### Addition

``` C
typedef void* context_handle;

struct corehost_context_contract
{
    size_t version;
    context_handle instance;
    int (*get_property_value)(
        context_handle instance,
        const char_t *key,
        const char_t **value);
    int (*set_property_value)(
        context_handle instance,
        const char_t *key,
        const char_t *value);
    int (*get_properties)(
        context_handle instance,
        size_t *count,
        const char_t **keys,
        const char_t **values);
    int (*run_app)(
        const context_handle instance,
        const int argc,
        const char_t* argv[]);
    int (*get_runtime_delegate)(
        const context_handle instance,
        coreclr_delegate_type type,
        void** delegate);
};
```

Contract for performing operations on an initialized host context.
* `version` - version of the struct.
* `instance` - opaque handle to the `corehost_context_contract` state.
* `get_property_value` - function pointer for getting a property on the host context.
  * `key` - key of the property to get.
  * `value` - pointer to a buffer with the retrieved property value.
* `set_property_value` - function pointer for setting a property on the host context.
  * `key` - key of the property to set.
  * `value` - value of the property to set. If `nullptr`, the property is removed.
* `get_properties` - function pointer for getting all properties on the host context.
  * `count` - size of `keys` and `values`. If the size is too small, it will be populated with the required size. Otherwise, it will be populated with the size used.
  * `keys` - buffer to populate with the property keys.
  * `values` - buffer to populate with the property values.
* `run_app` - function pointer for running an application.
  * `argc` / `argv` - command-line arguments.
* `get_runtime_delegate` - function pointer for getting a delegate for CoreCLR functionality
  * `type` - requested type of runtime functionality
  * `delegate` - function pointer to the requested runtime functionality

``` C
int corehost_initialize_context(const host_interface_t *init, corehost_context_contract *context_contract)
```

Initializes the host context. This calculates everything required to start CoreCLR (but does not actually do so).
* `init` - struct defining how the host context should be initialized. If the host context is already initialized, this function will check if `init` is compatible with the active context.
* `context_contract` - if initialization is successful, this is populated with the contract for operating on the initialized host context.

``` C
int corehost_close_context(corehost_context_contract *context_contract)
```

Closes the host context.
* `context_contract` - contract of the context to close.
