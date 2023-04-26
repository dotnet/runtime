// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
#include "bundle/info.h"
#include <framework_info.h>

namespace
{
    void trace_hostfxr_entry_point(const pal::char_t *entry_point)
    {
        trace::setup();
        trace::info(_X("--- Invoked %s [commit hash: %s]"), entry_point, _STRINGIFY(REPO_COMMIT_HASH));
    }
}

SHARED_API int HOSTFXR_CALLTYPE hostfxr_main_bundle_startupinfo(const int argc, const pal::char_t* argv[], const pal::char_t* host_path, const pal::char_t* dotnet_root, const pal::char_t* app_path, int64_t bundle_header_offset)
{
    trace_hostfxr_entry_point(_X("hostfxr_main_bundle_startupinfo"));

    StatusCode bundleStatus = bundle::info_t::process_bundle(host_path, app_path, bundle_header_offset);
    if (bundleStatus != StatusCode::Success)
    {
        trace::error(_X("A fatal error occurred while processing application bundle"));
        return bundleStatus;
    }

    if (host_path == nullptr || dotnet_root == nullptr || app_path == nullptr)
    {
        trace::error(_X("Invalid startup info: host_path, dotnet_root, and app_path should not be null."));
        return StatusCode::InvalidArgFailure;
    }

    host_startup_info_t startup_info(host_path, dotnet_root, app_path);
    return fx_muxer_t::execute(pal::string_t(), argc, argv, startup_info, nullptr, 0, nullptr);
}


SHARED_API int HOSTFXR_CALLTYPE hostfxr_main_startupinfo(const int argc, const pal::char_t* argv[], const pal::char_t* host_path, const pal::char_t* dotnet_root, const pal::char_t* app_path)
{
    trace_hostfxr_entry_point(_X("hostfxr_main_startupinfo"));

    if (host_path == nullptr || dotnet_root == nullptr || app_path == nullptr)
    {
        trace::error(_X("Invalid startup info: host_path, dotnet_root, and app_path should not be null."));
        return StatusCode::InvalidArgFailure;
    }

    host_startup_info_t startup_info(host_path, dotnet_root, app_path);
    return fx_muxer_t::execute(pal::string_t(), argc, argv, startup_info, nullptr, 0, nullptr);
}

SHARED_API int HOSTFXR_CALLTYPE hostfxr_main(const int argc, const pal::char_t* argv[])
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
//   or equal to buffer_size (i.e. the buffer is large enough),
//   then the resolved SDK path is copied to the buffer and null
//   terminated. Otherwise, no data is written to the buffer.
//
// String encoding:
//   Windows     - UTF-16 (pal::char_t is 2 byte wchar_t)
//   Unix        - UTF-8  (pal::char_t is 1 byte char)
//
SHARED_API int32_t HOSTFXR_CALLTYPE hostfxr_resolve_sdk(
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

    auto sdk_path = sdk_resolver::from_nearest_global_file(working_dir).resolve(exe_dir);
    if (sdk_path.empty())
    {
        // sdk_resolver::resolve handles tracing for this error case.
        return 0;
    }

    size_t non_negative_buffer_size = static_cast<size_t>(buffer_size);
    if (sdk_path.size() < non_negative_buffer_size)
    {
        size_t length = sdk_path.copy(buffer, non_negative_buffer_size - 1);
        assert(length == sdk_path.size());
        assert(length < non_negative_buffer_size);
        buffer[length] = 0;
    }
    else
    {
        trace::info(_X("hostfxr_resolve_sdk received a buffer that is too small to hold the located SDK path."));
    }

    return static_cast<int32_t>(sdk_path.size() + 1);
}

enum hostfxr_resolve_sdk2_flags_t : int32_t
{
    disallow_prerelease = 0x1,
};

enum class hostfxr_resolve_sdk2_result_key_t : int32_t
{
    resolved_sdk_dir = 0,
    global_json_path = 1,
    requested_version = 2,
};

typedef void (HOSTFXR_CALLTYPE *hostfxr_resolve_sdk2_result_fn)(
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
//      If resolution succeeds, then result will be invoked with
//      resolved_sdk_dir key and the value will hold the path to
//      the resolved SDK directory.
//
//      If global.json is used, then result will be invoked with
//      global_json_path key and the value will hold the path
//      to global.json. If there was no global.json found,
//      or the contents of global.json did not impact resolution
//      (e.g. no version specified), then result will not be
//      invoked with global_json_path key. This will occur for
//      both resolution success and failure.
//
//      If a specific version is requested (via global.json), then
//      result will be invoked with requested_version key and the
//      value will hold the requested version. This will occur for
//      both resolution success and failure.
//
// Return value:
//   0 on success, otherwise failure
//   0x8000809b - SDK could not be resolved (SdkResolverResolveFailure)
//
// String encoding:
//   Windows     - UTF-16 (pal::char_t is 2 byte wchar_t)
//   Unix        - UTF-8  (pal::char_t is 1 byte char)
//
SHARED_API int32_t HOSTFXR_CALLTYPE hostfxr_resolve_sdk2(
    const pal::char_t* exe_dir,
    const pal::char_t* working_dir,
    int32_t flags,
    hostfxr_resolve_sdk2_result_fn result)
{
    trace_hostfxr_entry_point(_X("hostfxr_resolve_sdk2"));
    trace::info(
        _X("  exe_dir=%s\n")
        _X("  working_dir=%s\n")
        _X("  flags=%d"),
        exe_dir == nullptr ? _X("<nullptr>") : exe_dir,
        working_dir == nullptr ? _X("<nullptr>") : working_dir,
        flags);

    if (exe_dir == nullptr)
    {
        exe_dir = _X("");
    }

    if (working_dir == nullptr)
    {
        working_dir = _X("");
    }

    auto resolver = sdk_resolver::from_nearest_global_file(
        working_dir,
        (flags & hostfxr_resolve_sdk2_flags_t::disallow_prerelease) == 0);

    auto resolved_sdk_dir = resolver.resolve(exe_dir);
    if (!resolved_sdk_dir.empty())
    {
        result(
            hostfxr_resolve_sdk2_result_key_t::resolved_sdk_dir,
            resolved_sdk_dir.c_str());
    }

    if (!resolver.global_file_path().empty())
    {
        result(
            hostfxr_resolve_sdk2_result_key_t::global_json_path,
            resolver.global_file_path().c_str());
    }

    if (!resolver.get_requested_version().is_empty())
    {
        result(
            hostfxr_resolve_sdk2_result_key_t::requested_version,
            resolver.get_requested_version().as_str().c_str());
    }

    return !resolved_sdk_dir.empty()
        ? StatusCode::Success
        : StatusCode::SdkResolverResolveFailure;
}


typedef void (HOSTFXR_CALLTYPE *hostfxr_get_available_sdks_result_fn)(
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
SHARED_API int32_t HOSTFXR_CALLTYPE hostfxr_get_available_sdks(
    const pal::char_t* exe_dir,
    hostfxr_get_available_sdks_result_fn result)
{
    trace_hostfxr_entry_point(_X("hostfxr_get_available_sdks"));
    trace::info(_X("  exe_dir=%s"), exe_dir == nullptr ? _X("<nullptr>") : exe_dir);

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

        result(static_cast<int32_t>(sdk_dirs.size()), &sdk_dirs[0]);
    }

    return StatusCode::Success;
}

SHARED_API int32_t HOSTFXR_CALLTYPE hostfxr_get_dotnet_environment_info(
    const pal::char_t* dotnet_root,
    void* reserved,
    hostfxr_get_dotnet_environment_info_result_fn result,
    void* result_context)
{
    trace_hostfxr_entry_point(_X("hostfxr_get_dotnet_environment_info"));
    trace::info(_X("  dotnet_root=%s"), dotnet_root == nullptr ? _X("<nullptr>") : dotnet_root);

    if (result == nullptr)
    {
        trace::error(_X("hostfxr_get_dotnet_environment_info received an invalid argument: result should not be null."));
        return StatusCode::InvalidArgFailure;
    }

    if (reserved != nullptr)
    {
        trace::error(_X("hostfxr_get_dotnet_environment_info received an invalid argument: reserved should be null."));
        return StatusCode::InvalidArgFailure;
    }

    pal::string_t dotnet_dir;
    if (dotnet_root == nullptr)
    {
        if (pal::get_dotnet_self_registered_dir(&dotnet_dir) || pal::get_default_installation_dir(&dotnet_dir))
        {
            trace::info(_X("Using global installation location [%s]."), dotnet_dir.c_str());
        }
        else
        {
            trace::info(_X("No default dotnet installation could be obtained."));
        }
    }
    else
    {
        dotnet_dir = dotnet_root;
    }

    std::vector<sdk_info> sdk_infos;
    sdk_info::get_all_sdk_infos(dotnet_dir, &sdk_infos);

    std::vector<hostfxr_dotnet_environment_sdk_info> environment_sdk_infos;
    std::vector<pal::string_t> sdk_versions;
    if (!sdk_infos.empty())
    {
        environment_sdk_infos.reserve(sdk_infos.size());
        sdk_versions.reserve(sdk_infos.size());
        for (const sdk_info& info : sdk_infos)
        {
            sdk_versions.push_back(info.version.as_str());
            hostfxr_dotnet_environment_sdk_info sdk
            {
                sizeof(hostfxr_dotnet_environment_sdk_info),
                sdk_versions.back().c_str(),
                info.full_path.c_str()
            };

            environment_sdk_infos.push_back(sdk);
        }
    }

    std::vector<framework_info> framework_infos;
    framework_info::get_all_framework_infos(dotnet_dir, _X(""), /*disable_multilevel_lookup*/ true, &framework_infos);

    std::vector<hostfxr_dotnet_environment_framework_info> environment_framework_infos;
    std::vector<pal::string_t> framework_versions;
    if (!framework_infos.empty())
    {
        environment_framework_infos.reserve(framework_infos.size());
        framework_versions.reserve(framework_infos.size());
        for (const framework_info& info : framework_infos)
        {
            framework_versions.push_back(info.version.as_str());
            hostfxr_dotnet_environment_framework_info fw
            {
                sizeof(hostfxr_dotnet_environment_framework_info),
                info.name.c_str(),
                framework_versions.back().c_str(),
                info.path.c_str()
            };

            environment_framework_infos.push_back(fw);
        }
    }

    const hostfxr_dotnet_environment_info environment_info
    {
        sizeof(hostfxr_dotnet_environment_info),
        _STRINGIFY(HOST_FXR_PKG_VER),
        _STRINGIFY(REPO_COMMIT_HASH),
        environment_sdk_infos.size(),
        (environment_sdk_infos.empty()) ? nullptr : &environment_sdk_infos[0],
        environment_framework_infos.size(),
        (environment_framework_infos.empty()) ? nullptr : &environment_framework_infos[0]
    };

    result(&environment_info, result_context);
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
//      required_buffer_size is set to the minimum buffer
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
SHARED_API int32_t HOSTFXR_CALLTYPE hostfxr_get_native_search_directories(const int argc, const pal::char_t* argv[], pal::char_t buffer[], int32_t buffer_size, int32_t* required_buffer_size)
{
    trace_hostfxr_entry_point(_X("hostfxr_get_native_search_directories"));
    if (trace::is_enabled())
    {
        trace::info(_X("  args=["));
        for (int i = 0; i < argc; ++i)
        {
            trace::info(_X("    %s"), argv[i]);
        }
        trace::info(_X("  ]"));
    }

    if (buffer_size < 0 || (buffer_size > 0 && buffer == nullptr) || required_buffer_size == nullptr)
    {
        trace::error(_X("hostfxr_get_native_search_directories received an invalid argument."));
        return InvalidArgFailure;
    }

    // Reset the output buffer to empty, so that if the below fails, we return a valid value.
    *required_buffer_size = 0;
    if (buffer_size > 0)
    {
        buffer[0] = '\0';
    }

    host_startup_info_t startup_info;
    startup_info.parse(argc, argv);

    int rc = fx_muxer_t::execute(_X("get-native-search-directories"), argc, argv, startup_info, buffer, buffer_size, required_buffer_size);
    return rc;
}

SHARED_API hostfxr_error_writer_fn HOSTFXR_CALLTYPE hostfxr_set_error_writer(hostfxr_error_writer_fn error_writer)
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
            if (!pal::get_method_module_path(&mod_path, (void*)&hostfxr_set_error_writer))
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

SHARED_API int32_t HOSTFXR_CALLTYPE hostfxr_initialize_for_dotnet_command_line(
    int argc,
    const pal::char_t *argv[],
    const hostfxr_initialize_parameters * parameters,
    /*out*/ hostfxr_handle * host_context_handle)
{
    trace_hostfxr_entry_point(_X("hostfxr_initialize_for_dotnet_command_line"));

    if (host_context_handle == nullptr || argv == nullptr || argc == 0)
        return StatusCode::InvalidArgFailure;

    *host_context_handle = nullptr;

    host_startup_info_t startup_info{};
    int rc = populate_startup_info(parameters, startup_info);
    if (rc != StatusCode::Success)
        return rc;

    int new_argoff;
    opt_map_t opts;
    rc = command_line::parse_args_for_mode(
        host_mode_t::muxer,
        startup_info,
        argc,
        argv,
        &new_argoff,
        startup_info.app_path,
        opts,
        false /*args_include_running_executable*/);
    if (rc != StatusCode::Success)
        return rc;

    new_argoff++; // Skip the app path to get to app args
    int app_argc = argc - new_argoff;
    const pal::char_t **app_argv = app_argc > 0 ? &argv[new_argoff] : nullptr;
    return fx_muxer_t::initialize_for_app(
        startup_info,
        app_argc,
        app_argv,
        opts,
        host_context_handle);
}

SHARED_API int32_t HOSTFXR_CALLTYPE hostfxr_initialize_for_runtime_config(
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

SHARED_API int32_t HOSTFXR_CALLTYPE hostfxr_run_app(const hostfxr_handle host_context_handle)
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
        case hostfxr_delegate_type::hdt_com_activation:
            return coreclr_delegate_type::com_activation;
        case hostfxr_delegate_type::hdt_load_in_memory_assembly:
            return coreclr_delegate_type::load_in_memory_assembly;
        case hostfxr_delegate_type::hdt_winrt_activation:
            return coreclr_delegate_type::winrt_activation;
        case hostfxr_delegate_type::hdt_com_register:
            return coreclr_delegate_type::com_register;
        case hostfxr_delegate_type::hdt_com_unregister:
            return coreclr_delegate_type::com_unregister;
        case hostfxr_delegate_type::hdt_load_assembly_and_get_function_pointer:
            return coreclr_delegate_type::load_assembly_and_get_function_pointer;
        case hostfxr_delegate_type::hdt_get_function_pointer:
            return coreclr_delegate_type::get_function_pointer;
        case hostfxr_delegate_type::hdt_load_assembly:
            return coreclr_delegate_type::load_assembly;
        case hostfxr_delegate_type::hdt_load_assembly_bytes:
            return coreclr_delegate_type::load_assembly_bytes;
        }
        return coreclr_delegate_type::invalid;
    }
}

SHARED_API int32_t HOSTFXR_CALLTYPE hostfxr_get_runtime_delegate(
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

    coreclr_delegate_type delegate_type = hostfxr_delegate_to_coreclr_delegate(type);
    if (delegate_type == coreclr_delegate_type::invalid)
        return StatusCode::InvalidArgFailure;

    return fx_muxer_t::get_runtime_delegate(context, delegate_type, delegate);
}

SHARED_API int32_t HOSTFXR_CALLTYPE hostfxr_get_runtime_property_value(
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
        if (context_maybe == nullptr)
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

SHARED_API int32_t HOSTFXR_CALLTYPE hostfxr_set_runtime_property_value(
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

SHARED_API int32_t HOSTFXR_CALLTYPE hostfxr_get_runtime_properties(
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
        if (context_maybe == nullptr)
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

SHARED_API int32_t HOSTFXR_CALLTYPE hostfxr_close(const hostfxr_handle host_context_handle)
{
    trace_hostfxr_entry_point(_X("hostfxr_close"));

    // Allow contexts with a type of invalid as we still need to clean them up
    host_context_t *context = host_context_t::from_handle(host_context_handle, /*allow_invalid_type*/ true);
    if (context == nullptr)
        return StatusCode::InvalidArgFailure;

    return fx_muxer_t::close_host_context(context);
}
