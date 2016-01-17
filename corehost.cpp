// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "trace.h"
#include "pal.h"
#include "utils.h"
#include "libhost.h"

extern int corehost_main(const int argc, const pal::char_t* argv[]);

namespace
{
enum StatusCode
{
    Success                   = 0,
    CoreHostLibLoadFailure    = 0x41,
    CoreHostLibMissingFailure = 0x42,
    CoreHostEntryPointFailure = 0x43,
    CoreHostCurExeFindFailure = 0x44,
};

typedef int (*corehost_main_fn) (const int argc, const pal::char_t* argv[]);

// -----------------------------------------------------------------------------
// Load the corehost library from the path specified
//
// Parameters:
//    lib_dir      - dir path to the corehost library
//    h_host       - handle to the library which will be kept live
//    main_fn      - Contains the entrypoint "corehost_main" when returns success.
//
// Returns:
//    Non-zero exit code on failure. "main_fn" contains "corehost_main"
//    entrypoint on success.
//
StatusCode load_host_lib(const pal::string_t& lib_dir, pal::dll_t* h_host, corehost_main_fn* main_fn)
{
    pal::string_t host_path = lib_dir;
    append_path(&host_path, LIBHOST_NAME);

    // Missing library
    if (!pal::file_exists(host_path))
    {
        return StatusCode::CoreHostLibMissingFailure;
    }

    // Load library
    if (!pal::load_library(host_path.c_str(), h_host))
    {
        trace::info(_X("Load library of %s failed"), host_path.c_str());
        return StatusCode::CoreHostLibLoadFailure;
    }

    // Obtain entrypoint symbol
    *main_fn = (corehost_main_fn) pal::get_symbol(*h_host, "corehost_main");

    return (*main_fn != nullptr)
                ? StatusCode::Success
                : StatusCode::CoreHostEntryPointFailure;
}

}; // end of anonymous namespace

#if defined(_WIN32)
int __cdecl wmain(const int argc, const pal::char_t* argv[])
#else
int main(const int argc, const pal::char_t* argv[])
#endif
{
    trace::setup();

    pal::dll_t corehost;

#ifdef COREHOST_PACKAGE_SERVICING
    // No custom host asked, so load the corehost if serviced first.
    pal::string_t svc_dir;
    if (pal::getenv(_X("DOTNET_SERVICING"), &svc_dir))
    {
        pal::string_t path = svc_dir;
        append_path(&path, COREHOST_PACKAGE_NAME);
        append_path(&path, COREHOST_PACKAGE_VERSION);
        append_path(&path, COREHOST_PACKAGE_COREHOST_RELATIVE_DIR);

        corehost_main_fn host_main;
        StatusCode code = load_host_lib(path, &corehost, &host_main);
        if (code != StatusCode::Success)
        {
            trace::info(_X("Failed to load host library from servicing dir: %s; Status=%08X"), path.c_str(), code);
            // Ignore all errors for the servicing case, and proceed to the next step.
        }
        else
        {
            trace::info(_X("Calling host entrypoint from library at servicing dir %s"), path.c_str());
            return host_main(argc, argv);
        }
    }
#endif

    // Get current path to look for the library app locally.
    pal::string_t own_path;
    if (!pal::get_own_executable_path(&own_path) || !pal::realpath(&own_path))
    {
        trace::error(_X("Failed to locate current executable"));
        return StatusCode::CoreHostCurExeFindFailure;
    }

    // Local load of the corehost library.
    auto own_dir = get_directory(own_path);

    corehost_main_fn host_main;
    StatusCode code = load_host_lib(own_dir, &corehost, &host_main);
    switch (code)
    {
    // Success, call the entrypoint.
    case StatusCode::Success:
        trace::info(_X("Calling host entrypoint from library at own dir %s"), own_dir.c_str());
        return host_main(argc, argv);

    // Some other fatal error including StatusCode::CoreHostLibMissingFailure.
    default:
        trace::error(_X("Error loading the host library from own dir: %s; Status=%08X"), own_dir.c_str(), code);
        return code;
    }
}
