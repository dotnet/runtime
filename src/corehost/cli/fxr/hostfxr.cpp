// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <cassert>
#include "trace.h"
#include "pal.h"
#include "utils.h"
#include "fx_ver.h"
#include "fx_muxer.h"
#include "error_codes.h"
#include "runtime_config.h"
#include "sdk_info.h"
#include "sdk_resolver.h"
#include "hostfxr.h"
#include "host_context.h"

namespace
{
    void trace_hostfxr_entry_point(const pal::char_t *entry_point)
    {
        trace::setup();
        trace::info(_X("--- Invoked %s [commit hash: %s]"), entry_point, _STRINGIFY(REPO_COMMIT_HASH));
    }
}

SHARED_API int hostfxr_main_startupinfo(const int argc, const pal::char_t* argv[], const pal::char_t* host_path, const pal::char_t* dotnet_root, const pal::char_t* app_path)
{
    trace_hostfxr_entry_point(_X("hostfxr_main_startupinfo"));

    host_startup_info_t startup_info(host_path, dotnet_root, app_path);

    return fx_muxer_t::execute(pal::string_t(), argc, argv, startup_info, nullptr, 0, nullptr);
}

SHARED_API int hostfxr_main(const int argc, const pal::char_t* argv[])
{
    trace_hostfxr_entry_point(_X("hostfxr_main"));

    host_startup_info_t startup_info;
    startup_info.parse(argc, argv);

    return fx_muxer_t::execute(pal::string_t(), argc, argv, startup_info, nullptr, 0, nullptr);
}

// [OBSOLETE] Replaced by hostfxr_resolve_sdk2
//
// Determines the directory location of the SDK accounting for
// global.json and multi-level lookup policy.
//
// Invoked via MSBuild SDK resolver to locate SDK props and targets
// from an msbuild other than the one bundled by the CLI.
//
// Parameters:
//    exe_dir
//      The main directory where SDKs are located in sdk\[version]
//      sub-folders. Pass the directory of a dotnet executable to
//      mimic how that executable would search in its own directory.
//      It is also valid to pass nullptr or empty, in which case
//      multi-level lookup can still search other locations if
//      it has not been disabled by the user's environment.
//
//    working_dir
//      The directory where the search for global.json (which can
//      control the resolved SDK version) starts and proceeds
//      upwards.
//
//    buffer
//      The buffer where the resolved SDK path will be written.
//
//    buffer_size
//      The size of the buffer argument in pal::char_t units.
//
// Return value:
//   <0 - Invalid argument
//   0  - SDK could not be found.
//   >0 - The number of characters (including null terminator)
//        required to store the located SDK.
//
//   If resolution succeeds and the positive return value is less than
//   or equal to buffer_size (i.e. the the buffer is large enough),
//   then the resolved SDK path is copied to the buffer and null
//   terminated. Otherwise, no data is written to the buffer.
//
// String encoding:
//   Windows     - UTF-16 (pal::char_t is 2 byte wchar_t)
//   Unix        - UTF-8  (pal::char_t is 1 byte char)
//
SHARED_API int32_t hostfxr_resolve_sdk(
    const pal::char_t* exe_dir,
    const pal::char_t* working_dir,
    pal::char_t buffer[],
    int32_t buffer_size)
{
    trace_hostfxr_entry_point(_X("hostfxr_resolve_sdk"));

    if (buffer_size < 0 || (buffer_size > 0 && buffer == nullptr))
    {
        trace::error(_X("hostfxr_resolve_sdk received an invalid argument."));
        return -1;
    }

    if (exe_dir == nullptr)
    {
        exe_dir = _X("");
    }

    if (working_dir == nullptr)
    {
        working_dir = _X("");
    }

    pal::string_t cli_sdk;
    if (!sdk_resolver_t::resolve_sdk_dotnet_path(exe_dir, working_dir, &cli_sdk))
    {
        // sdk_resolver_t::resolve_sdk_dotnet_path handles tracing for this error case.
        return 0;
    }

    unsigned long non_negative_buffer_size = static_cast<unsigned long>(buffer_size);
    if (cli_sdk.size() < non_negative_buffer_size)
    {
        size_t length = cli_sdk.copy(buffer, non_negative_buffer_size - 1);
        assert(length == cli_sdk.size());
        assert(length < non_negative_buffer_size);
        buffer[length] = 0;
    }
    else
    {
        trace::info(_X("hostfxr_resolve_sdk received a buffer that is too small to hold the located SDK path."));
    }

    return cli_sdk.size() + 1;
}

enum hostfxr_resolve_sdk2_flags_t : int32_t
{
    disallow_prerelease = 0x1,
};

enum class hostfxr_resolve_sdk2_result_key_t : int32_t
{
    resolved_sdk_dir = 0,
    global_json_path = 1,
};

typedef void (*hostfxr_resolve_sdk2_result_fn)(
    hostfxr_resolve_sdk2_result_key_t key,
    const pal::char_t* value);

//
// Determines the directory location of the SDK accounting for
// global.json and multi-level lookup policy.
//
// Invoked via MSBuild SDK resolver to locate SDK props and targets
// from an msbuild other than the one bundled by the CLI.
//
// Parameters:
//    exe_dir
//      The main directory where SDKs are located in sdk\[version]
//      sub-folders. Pass the directory of a dotnet executable to
//      mimic how that executable would search in its own directory.
//      It is also valid to pass nullptr or empty, in which case
//      multi-level lookup can still search other locations if
//      it has not been disabled by the user's environment.
//
//    working_dir
//      The directory where the search for global.json (which can
//      control the resolved SDK version) starts and proceeds
//      upwards.
//
//   flags
//      Bitwise flags that influence resolution.
//         disallow_prerelease (0x1)
//           do not allow resolution to return a prerelease SDK version
//           unless  prerelease version was specified via global.json.
//
//   result
//      Callback invoked to return values. It can be invoked more
//      than once. String values passed are valid only for the
//      duration of a call.
//
//      If resolution succeeds, result will be invoked with
//      resolved_sdk_dir key and the value will hold the
//      path to the resolved SDK director, otherwise it will
//      be null.
//
//      If global.json is used then result will be invoked with
//      global_json_path key and the value  will hold the path
//      to global.json. If there was no global.json found,
//      or the contents of global.json did not impact resolution
//      (e.g. no version specified), then result will not be
//      invoked with global_json_path key.
//
// Return value:
//   0 on success, otherwise failure
//   0x8000809b - SDK could not be resolved (SdkResolverResolveFailure)
//
// String encoding:
//   Windows     - UTF-16 (pal::char_t is 2 byte wchar_t)
//   Unix        - UTF-8  (pal::char_t is 1 byte char)
//
SHARED_API int32_t hostfxr_resolve_sdk2(
    const pal::char_t* exe_dir,
    const pal::char_t* working_dir,
    int32_t flags,
    hostfxr_resolve_sdk2_result_fn result)
{
    trace_hostfxr_entry_point(_X("hostfxr_resolve_sdk2"));

    if (exe_dir == nullptr)
    {
        exe_dir = _X("");
    }

    if (working_dir == nullptr)
    {
        working_dir = _X("");
    }

    pal::string_t resolved_sdk_dir;
    pal::string_t global_json_path;

    bool success = sdk_resolver_t::resolve_sdk_dotnet_path(
        exe_dir,
        working_dir,
        &resolved_sdk_dir,
        (flags & hostfxr_resolve_sdk2_flags_t::disallow_prerelease) != 0,
        &global_json_path);

    if (success)
    {
        result(
            hostfxr_resolve_sdk2_result_key_t::resolved_sdk_dir,
            resolved_sdk_dir.c_str());
    }

    if (!global_json_path.empty())
    {
        result(
            hostfxr_resolve_sdk2_result_key_t::global_json_path,
            global_json_path.c_str());
    }

    return success
        ? StatusCode::Success
        : StatusCode::SdkResolverResolveFailure;
}


typedef void (*hostfxr_get_available_sdks_result_fn)(
    int32_t sdk_count,
    const pal::char_t *sdk_dirs[]);

//
// Returns the list of all available SDKs ordered by ascending version.
//
// Invoked by MSBuild resolver when the latest SDK used without global.json
// present is incompatible with the current MSBuild version. It will select
// the compatible SDK that is closest to the end of this list.
//
// Parameters:
//    exe_dir
//      The path to the dotnet executable.
//
//    result
//      Callback invoke to return the list of SDKs by their directory paths.
//      String array and its elements are valid for the duration of the call.
//
// Return value:
//   0 on success, otherwise failure
//
// String encoding:
//   Windows     - UTF-16 (pal::char_t is 2 byte wchar_t)
//   Unix        - UTF-8  (pal::char_t is 1 byte char)
//
SHARED_API int32_t hostfxr_get_available_sdks(
    const pal::char_t* exe_dir,
    hostfxr_get_available_sdks_result_fn result)
{
    trace_hostfxr_entry_point(_X("hostfxr_get_available_sdks"));

    if (exe_dir == nullptr)
    {
        exe_dir = _X("");
    }

    std::vector<sdk_info> sdk_infos;
    sdk_info::get_all_sdk_infos(exe_dir, &sdk_infos);

    if (sdk_infos.empty())
    {
        result(0, nullptr);
    }
    else
    {
        std::vector<const pal::char_t*> sdk_dirs;
        sdk_dirs.reserve(sdk_infos.size());

        for (const auto& sdk_info : sdk_infos)
        {
            sdk_dirs.push_back(sdk_info.full_path.c_str());
        }

        result(sdk_dirs.size(), &sdk_dirs[0]);
    }

    return StatusCode::Success;
}

//
// Returns the native directories of the runtime based upon
// the specified app.
//
// Returned format is a list of paths separated by PATH_SEPARATOR
// which is a semicolon (;) on Windows and a colon (:) otherwise.
// The returned string is null-terminated.
//
// Invoked from ASP.NET in order to help load a native assembly
// before the clr is initialized (through a custom host).
//
// Parameters:
//    argc
//      The number of argv arguments
//
//    argv
//      The standard arguments normally passed to dotnet.exe
//      for launching the application.
//
//    buffer
//      The buffer where the native paths and null terminator
//      will be written.
//
//    buffer_size
//      The size of the buffer argument in pal::char_t units.
//
//    required_buffer_size
//      If the return value is HostApiBufferTooSmall, then
//      required_buffer_size is set to the minimium buffer
//      size necessary to contain the result including the
//      null terminator.
//
// Return value:
//   0 on success, otherwise failure
//   0x80008098 - Buffer is too small (HostApiBufferTooSmall)
//
// String encoding:
//   Windows     - UTF-16 (pal::char_t is 2 byte wchar_t)
//   Unix        - UTF-8  (pal::char_t is 1 byte char)
//
SHARED_API int32_t hostfxr_get_native_search_directories(const int argc, const pal::char_t* argv[], pal::char_t buffer[], int32_t buffer_size, int32_t* required_buffer_size)
{
    trace_hostfxr_entry_point(_X("hostfxr_get_native_search_directories"));

    if (buffer_size < 0 || (buffer_size > 0 && buffer == nullptr) || required_buffer_size == nullptr)
    {
        trace::error(_X("hostfxr_get_native_search_directories received an invalid argument."));
        return InvalidArgFailure;
    }

    host_startup_info_t startup_info;
    startup_info.parse(argc, argv);

    int rc = fx_muxer_t::execute(_X("get-native-search-directories"), argc, argv, startup_info, buffer, buffer_size, required_buffer_size);
    return rc;
}

typedef void(*hostfxr_error_writer_fn)(const pal::char_t* message);

//
// Sets a callback which is to be used to write errors to.
//
// Parameters:
//     error_writer
//         A callback function which will be invoked every time an error is to be reported.
//         Or nullptr to unregister previously registered callback and return to the default behavior.
// Return value:
//     The previously registered callback (which is now unregistered), or nullptr if no previous callback
//     was registered
//
// The error writer is registered per-thread, so the registration is thread-local. On each thread
// only one callback can be registered. Subsequent registrations overwrite the previous ones.
//
// By default no callback is registered in which case the errors are written to stderr.
//
// Each call to the error writer is sort of like writing a single line (the EOL character is omitted).
// Multiple calls to the error writer may occure for one failure.
//
// If the hostfxr invokes functions in hostpolicy as part of its operation, the error writer
// will be propagated to hostpolicy for the duration of the call. This means that errors from
// both hostfxr and hostpolicy will be reporter through the same error writer.
//
SHARED_API hostfxr_error_writer_fn hostfxr_set_error_writer(hostfxr_error_writer_fn error_writer)
{
    return trace::set_error_writer(error_writer);
}

namespace
{
    int populate_startup_info(const hostfxr_initialize_parameters *parameters, host_startup_info_t &startup_info)
    {
        if (parameters != nullptr)
        {
            if (parameters->host_path != nullptr)
                startup_info.host_path = parameters->host_path;

            if (parameters->dotnet_root != nullptr)
                startup_info.dotnet_root = parameters->dotnet_root;
        }

        if (startup_info.host_path.empty())
        {
            if (!pal::get_own_executable_path(&startup_info.host_path) || !pal::realpath(&startup_info.host_path))
            {
                trace::error(_X("Failed to resolve full path of the current host [%s]"), startup_info.host_path.c_str());
                return StatusCode::CoreHostCurHostFindFailure;
            }
        }

        if (startup_info.dotnet_root.empty())
        {
            pal::string_t mod_path;
            if (!pal::get_own_module_path(&mod_path))
                return StatusCode::CoreHostCurHostFindFailure;

            startup_info.dotnet_root = get_dotnet_root_from_fxr_path(mod_path);
            if (!pal::realpath(&startup_info.dotnet_root))
            {
                trace::error(_X("Failed to resolve full path of dotnet root [%s]"), startup_info.dotnet_root.c_str());
                return StatusCode::CoreHostCurHostFindFailure;
            }
        }

        return StatusCode::Success;
    }
}

//
// Initializes the hosting components for running an application
//
// Parameters:
//    argc
//      Number of argv arguments
//    argv
//      Arguments for the application. If argc is 0, this is ignored.
//    app_path
//      Path to the managed application. If this is nullptr, the first argument in argv is assumed to be
//      the application path.
//    parameters
//      Optional. Additional parameters for initialization
//    host_context_handle
//      On success, this will be populated with an opaque value representing the initalized host context
//
// Return value:
//    Success          - Hosting components were successfully initialized
//    HostInvalidState - Hosting components are already initialized
//
// The app_path will be used to find the corresponding .runtimeconfig.json and .deps.json with which to
// resolve frameworks and dependencies and prepare everything needed to load the runtime.
//
// This function does not load the runtime.
//
SHARED_API int32_t __cdecl hostfxr_initialize_for_app(
    int argc,
    const pal::char_t *argv[],
    const pal::char_t *app_path,
    const hostfxr_initialize_parameters * parameters,
    /*out*/ hostfxr_handle * host_context_handle)
{
    trace_hostfxr_entry_point(_X("hostfxr_initialize_for_app"));

    if (host_context_handle == nullptr || (argv == nullptr && argc != 0) || (app_path == nullptr && argc == 0))
        return StatusCode::InvalidArgFailure;

    *host_context_handle = nullptr;

    host_startup_info_t startup_info{};
    int new_argc;
    const pal::char_t **new_argv;
    if (app_path != nullptr)
    {
        startup_info.app_path = app_path;
        new_argc = argc;
        new_argv = argv;
    }
    else
    {
        // Take the first argument as the app path
        startup_info.app_path = argv[0];
        new_argc = argc-1;
        new_argv = argc > 0 ? &argv[1] : nullptr;
    }

    int rc = populate_startup_info(parameters, startup_info);
    if (rc != StatusCode::Success)
        return rc;

    return fx_muxer_t::initialize_for_app(
        startup_info,
        new_argc,
        new_argv,
        host_context_handle);
}

//
// Initializes the hosting components using a .runtimeconfig.json file
//
// Parameters:
//    runtime_config_path
//      Path to the .runtimeconfig.json file
//    parameters
//      Optional. Additional parameters for initialization
//    host_context_handle
//      On success, this will be populated with an opaque value representing the initalized host context
//
// Return value:
//    Success                     - Hosting components were successfully initialized
//    CoreHostAlreadyInitialized  - Config is compatible with already initialized hosting components
// [TODO]
//    CoreHostIncompatibleConfig  - Config is incompatible with already initialized hosting components
//    CoreHostDifferentProperties - Config has runtime properties that differ from already initialized hosting components
//
// This function will process the .runtimeconfig.json to resolve frameworks and prepare everything needed
// to load the runtime. It will only process the .deps.json from frameworks (not any app/component that
// may be next to the .runtimeconfig.json).
//
// This function does not load the runtime.
//
// If called when the runtime has already been loaded, this function will check if the specified runtime
// config is compatible with the existing runtime.
//
SHARED_API int32_t __cdecl hostfxr_initialize_for_runtime_config(
    const pal::char_t *runtime_config_path,
    const hostfxr_initialize_parameters *parameters,
    /*out*/ hostfxr_handle *host_context_handle)
{
    trace_hostfxr_entry_point(_X("hostfxr_initialize_for_runtime_config"));

    if (runtime_config_path == nullptr || host_context_handle == nullptr)
        return StatusCode::InvalidArgFailure;

    *host_context_handle = nullptr;

    host_startup_info_t startup_info{};
    int rc = populate_startup_info(parameters, startup_info);
    if (rc != StatusCode::Success)
        return rc;

    return fx_muxer_t::initialize_for_runtime_config(
        startup_info,
        runtime_config_path,
        host_context_handle);
}

//
// Load CoreCLR and run the application for an initialized host context
//
// Parameters:
//     host_context_handle
//       Handle to the initialized host context
//
// Return value:
//     If the app was successfully run, the exit code of the application. Otherwise, the error code result.
//
// The host_context_handle must have been initialized using hostfxr_initialize_for_app.
//
// This function will not return until the managed application exits.
//
SHARED_API int32_t __cdecl hostfxr_run_app(const hostfxr_handle host_context_handle)
{
    trace_hostfxr_entry_point(_X("hostfxr_run_app"));

    host_context_t *context = host_context_t::from_handle(host_context_handle);
    if (context == nullptr)
        return StatusCode::InvalidArgFailure;

    return fx_muxer_t::run_app(context);
}

namespace
{
    coreclr_delegate_type hostfxr_delegate_to_coreclr_delegate(hostfxr_delegate_type type)
    {
        switch (type)
        {
        case hostfxr_delegate_type::com_activation:
            return coreclr_delegate_type::com_activation;
        case hostfxr_delegate_type::load_in_memory_assembly:
            return coreclr_delegate_type::load_in_memory_assembly;
        case hostfxr_delegate_type::winrt_activation:
            return coreclr_delegate_type::winrt_activation;
        }
        return coreclr_delegate_type::invalid;
    }
}

//
// Gets a typed delegate from the currently loaded CoreCLR or from a newly created one.
//
// Parameters:
//     host_context_handle
//       Handle to the initialized host context
//     type
//       Type of runtime delegate requested
//     delegate
//       An out parameter that will be assigned the delegate.
//
// Return value:
//     The error code result.
//
// The host_context_handle must have been initialized using hostfxr_initialize_for_runtime_config.
//
SHARED_API int32_t __cdecl hostfxr_get_runtime_delegate(
    const hostfxr_handle host_context_handle,
    hostfxr_delegate_type type,
    /*out*/ void **delegate)
{
    trace_hostfxr_entry_point(_X("hostfxr_get_runtime_delegate"));

    if (delegate == nullptr)
        return StatusCode::InvalidArgFailure;

    *delegate = nullptr;

    host_context_t *context = host_context_t::from_handle(host_context_handle);
    if (context == nullptr)
        return StatusCode::InvalidArgFailure;

    return fx_muxer_t::get_runtime_delegate(context, hostfxr_delegate_to_coreclr_delegate(type), delegate);
}

//
// Gets the runtime property value for an initialized host context
//
// Parameters:
//     host_context_handle
//       Handle to the initialized host context
//     name
//       Runtime property name
//     value
//       Out parameter. Pointer to a buffer with the property value.
//
// Return value:
//     The error code result.
//
// The buffer pointed to by value is owned by the host context. The lifetime of the buffer is only
// guaranteed until any of the below occur:
//   - a 'run' method is called for the host context
//   - properties are changed via hostfxr_set_runtime_property_value
//   - the host context is closed via 'hostfxr_close'
//
// If host_context_handle is nullptr and an active host context exists, this function will get the
// property value for the active host context.
//
SHARED_API int32_t __cdecl hostfxr_get_runtime_property_value(
    const hostfxr_handle host_context_handle,
    const pal::char_t *name,
    /*out*/ const pal::char_t **value)
{
    trace_hostfxr_entry_point(_X("hostfxr_get_runtime_property_value"));

    if (name == nullptr || value == nullptr)
        return StatusCode::InvalidArgFailure;

    const host_context_t *context;
    if (host_context_handle == nullptr)
    {
        const host_context_t *context_maybe = fx_muxer_t::get_active_host_context();
        if (context_maybe == nullptr || context_maybe->type != host_context_type::active)
        {
            trace::error(_X("Hosting components context has not been initialized. Cannot get runtime properties."));
            return StatusCode::HostInvalidState;
        }

        context = context_maybe;
    }
    else
    {
        context = host_context_t::from_handle(host_context_handle);
        if (context == nullptr)
            return StatusCode::InvalidArgFailure;
    }


    if (context->type == host_context_type::secondary)
    {
        const std::unordered_map<pal::string_t, pal::string_t> &properties = context->config_properties;
        auto iter = properties.find(name);
        if (iter == properties.cend())
            return StatusCode::HostPropertyNotFound;

        *value = (*iter).second.c_str();
        return StatusCode::Success;
    }

    assert(context->type == host_context_type::initialized || context->type == host_context_type::active);
    const corehost_context_contract &contract = context->hostpolicy_context_contract;
    return contract.get_property_value(name, value);
}

//
// Sets the value of a runtime property for an initialized host context
//
// Parameters:
//     host_context_handle
//       Handle to the initialized host context
//     name
//       Runtime property name
//     value
//       Value to set
//
// Return value:
//     The error code result.
//
// Setting properties is only supported for the first host context, before the runtime has been loaded.
//
// If the property already exists in the host context, it will be overwritten. If value is nullptr, the
// property will be removed.
//
SHARED_API int32_t __cdecl hostfxr_set_runtime_property_value(
    const hostfxr_handle host_context_handle,
    const pal::char_t *name,
    const pal::char_t *value)
{
    trace_hostfxr_entry_point(_X("hostfxr_set_runtime_property_value"));

    if (name == nullptr)
        return StatusCode::InvalidArgFailure;

    host_context_t *context = host_context_t::from_handle(host_context_handle);
    if (context == nullptr)
        return StatusCode::InvalidArgFailure;

    if (context->type != host_context_type::initialized)
    {
        trace::error(_X("Setting properties is not allowed once runtime has been loaded."));
        return StatusCode::InvalidArgFailure;
    }

    const corehost_context_contract &contract = context->hostpolicy_context_contract;
    return contract.set_property_value(name, value);
}

//
// Gets all the runtime properties for an initialized host context
//
// Parameters:
//     host_context_handle
//       Handle to the initialized host context
//     count
//       [in] Size of the keys and values buffers
//       [out] Number of properties returned (size of keys/values buffers used). If the input value is too
//             small or keys/values is nullptr, this is populated with the number of available properties
//     keys
//       Array of pointers to buffers with runtime property keys
//     values
//       Array of pointers to buffers with runtime property values
//
// Return value:
//     The error code result.
//
// The buffers pointed to by keys and values are owned by the host context. The lifetime of the buffers is only
// guaranteed until any of the below occur:
//   - a 'run' method is called for the host context
//   - properties are changed via hostfxr_set_runtime_property_value
//   - the host context is closed via 'hostfxr_close'
//
// If host_context_handle is nullptr and an active host context exists, this function will get the
// properties for the active host context.
//
SHARED_API int32_t __cdecl hostfxr_get_runtime_properties(
    const hostfxr_handle host_context_handle,
    /*inout*/ size_t * count,
    /*out*/ const pal::char_t **keys,
    /*out*/ const pal::char_t **values)
{
    trace_hostfxr_entry_point(_X("hostfxr_get_runtime_properties"));

    if (count == nullptr)
        return StatusCode::InvalidArgFailure;

    const host_context_t *context;
    if (host_context_handle == nullptr)
    {
        const host_context_t *context_maybe = fx_muxer_t::get_active_host_context();
        if (context_maybe == nullptr || context_maybe->type != host_context_type::active)
        {
            trace::error(_X("Hosting components context has not been initialized. Cannot get runtime properties."));
            return StatusCode::HostInvalidState;
        }

        context = context_maybe;
    }
    else
    {
        context = host_context_t::from_handle(host_context_handle);
        if (context == nullptr)
            return StatusCode::InvalidArgFailure;
    }

    if (context->type == host_context_type::secondary)
    {
        const std::unordered_map<pal::string_t, pal::string_t> &properties = context->config_properties;
        size_t actualCount = properties.size();
        size_t input_count = *count;
        *count = actualCount;
        if (input_count < actualCount || keys == nullptr || values == nullptr)
            return StatusCode::HostApiBufferTooSmall;

        int i = 0;
        for (const auto& kv : properties)
        {
            keys[i] = kv.first.data();
            values[i] = kv.second.data();
            ++i;
        }

        return StatusCode::Success;
    }

    assert(context->type == host_context_type::initialized || context->type == host_context_type::active);
    const corehost_context_contract &contract = context->hostpolicy_context_contract;
    return contract.get_properties(count, keys, values);
}

//
// Closes an initialized host context
//
// Parameters:
//     host_context_handle
//       Handle to the initialized host context
//
// Return value:
//     The error code result.
//
SHARED_API int32_t __cdecl hostfxr_close(const hostfxr_handle host_context_handle)
{
    trace_hostfxr_entry_point(_X("hostfxr_close"));

    // Allow contexts with a type of invalid as we still need to clean them up
    host_context_t *context = host_context_t::from_handle(host_context_handle, /*allow_invalid_type*/ true);
    if (context == nullptr)
        return StatusCode::InvalidArgFailure;

    return fx_muxer_t::close_host_context(context);
}