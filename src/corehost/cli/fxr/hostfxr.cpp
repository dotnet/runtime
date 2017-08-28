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
#include "runtime_config.h"

typedef int(*corehost_load_fn) (const host_interface_t* init);
typedef int(*corehost_main_fn) (const int argc, const pal::char_t* argv[]);
typedef int(*corehost_unload_fn) ();

int load_host_library(
    const pal::string_t& lib_dir,
    pal::dll_t* h_host,
    corehost_load_fn* load_fn,
    corehost_main_fn* main_fn,
    corehost_unload_fn* unload_fn)
{
    pal::string_t host_path;
    if (!library_exists_in_dir(lib_dir, LIBHOSTPOLICY_NAME, &host_path))
    {
        return StatusCode::CoreHostLibMissingFailure;
    }

    // Load library
    if (!pal::load_library(&host_path, h_host))
    {
        trace::info(_X("Load library of %s failed"), host_path.c_str());
        return StatusCode::CoreHostLibLoadFailure;
    }

    // Obtain entrypoint symbols
    *load_fn = (corehost_load_fn)pal::get_symbol(*h_host, "corehost_load");
    *main_fn = (corehost_main_fn)pal::get_symbol(*h_host, "corehost_main");
    *unload_fn = (corehost_unload_fn)pal::get_symbol(*h_host, "corehost_unload");

    return (*main_fn) && (*load_fn) && (*unload_fn)
        ? StatusCode::Success
        : StatusCode::CoreHostEntryPointFailure;
}

int execute_app(
    const pal::string_t& impl_dll_dir,
    corehost_init_t* init,
    const int argc,
    const pal::char_t* argv[])
{
    pal::dll_t corehost;
    corehost_main_fn host_main = nullptr;
    corehost_load_fn host_load = nullptr;
    corehost_unload_fn host_unload = nullptr;

    int code = load_host_library(impl_dll_dir, &corehost, &host_load, &host_main, &host_unload);

    if (code != StatusCode::Success)
    {
        trace::error(_X("An error occurred while loading required library %s from [%s]"), LIBHOSTPOLICY_NAME, impl_dll_dir.c_str());
        return code;
    }

    // Previous hostfxr trace messages must be printed before calling trace::setup in hostpolicy
    trace::flush();

    const host_interface_t& intf = init->get_host_init_data();
    if ((code = host_load(&intf)) == 0)
    {
        code = host_main(argc, argv);
        (void)host_unload();
    }

    pal::unload_library(corehost);

    return code;
}

static char sccsid[] = "@(#)"            \
                       HOST_PKG_VER      \
                       "; Commit Hash: " \
                       REPO_COMMIT_HASH  \
                       "; Built on: "    \
                       __DATE__          \
                       " "               \
                       __TIME__          \
                       ;

SHARED_API int hostfxr_main(const int argc, const pal::char_t* argv[])
{
    trace::setup();
    
    trace::info(_X("--- Invoked hostfxr [commit hash: %s] main"), _STRINGIFY(REPO_COMMIT_HASH));

    fx_muxer_t muxer;
    return muxer.execute(argc, argv);
}

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
    if (!fx_muxer_t::resolve_sdk_dotnet_path(exe_dir, working_dir, &cli_sdk))
    {
        // fx_muxer_t::resolve_sdk_dotnet_path handles tracing for this error case.
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
