// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <cassert>
#include "trace.h"
#include "pal.h"
#include "utils.h"
#include "fx_ver.h"
#include "fx_muxer.h"
#include "error_codes.h"
#include "libhost.h"
#include "corehost.h"
#include "runtime_config.h"
#include "sdk_info.h"
#include "sdk_resolver.h"

SHARED_API int hostfxr_main_startupinfo(const int argc, const pal::char_t* argv[], const pal::char_t* host_path, const pal::char_t* dotnet_root, const pal::char_t* app_path)
{
    trace::setup();

    trace::info(_X("--- Invoked hostfxr v2 [commit hash: %s] main"), _STRINGIFY(REPO_COMMIT_HASH));

    host_startup_info_t startup_info(host_path, dotnet_root, app_path);

    return fx_muxer_t::execute(pal::string_t(), argc, argv, startup_info, nullptr, 0, nullptr);
}

SHARED_API int hostfxr_main(const int argc, const pal::char_t* argv[])
{
    trace::setup();

    trace::info(_X("--- Invoked hostfxr [commit hash: %s] main"), _STRINGIFY(REPO_COMMIT_HASH));

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
    trace::setup();

    trace::info(_X("--- Invoked hostfxr [commit hash: %s] hostfxr_resolve_sdk"), _STRINGIFY(REPO_COMMIT_HASH));

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

    if (cli_sdk.size() < buffer_size)
    {
        size_t length = cli_sdk.copy(buffer, buffer_size - 1);
        assert(length == cli_sdk.size());
        assert(length < buffer_size);
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
    trace::setup();

    trace::info(_X("--- Invoked hostfxr [commit hash: %s] hostfxr_resolve_sdk2"), _STRINGIFY(REPO_COMMIT_HASH));

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
    trace::setup();

    trace::info(_X("--- Invoked hostfxr [commit hash: %s] hostfxr_get_available_sdks"), _STRINGIFY(REPO_COMMIT_HASH));

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
    trace::setup();

    trace::info(_X("--- Invoked hostfxr_get_native_search_directories [commit hash: %s] main"), _STRINGIFY(REPO_COMMIT_HASH));

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

//
// Gets a typed delegate to perform an action on the currently loaded CoreCLR or on a newly created one.
//
// Parameters:
//     libhost_path
//          Absolute path of the entry hosting library
//     dotnet_root
//     app_path
//     delegate
//          COM activation delegate.
// Return value:
//     The error code result.
//
// A new CoreCLR instance will be created or reused if the existing instance can satisfy the configuration
// requirements supplied by the runtimeconfig.json file.
//
SHARED_API int32_t hostfxr_get_com_activation_delegate(
    const pal::char_t* libhost_path,
    const pal::char_t* dotnet_root,
    const pal::char_t* app_path,
    void **delegate)
{
    if (libhost_path == nullptr || dotnet_root == nullptr || delegate == nullptr)
        return StatusCode::InvalidArgFailure;

    trace::setup();

    trace::info(_X("--- Invoked hostfxr comhost [commit hash: %s]"), _STRINGIFY(REPO_COMMIT_HASH));

    host_startup_info_t startup_info{ libhost_path, dotnet_root, app_path };

    return fx_muxer_t::get_com_activation_delegate(startup_info, delegate);
}
