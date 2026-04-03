// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal.h"
#include "hostfxr.h"
#include "fxr_resolver.h"
#include "error_codes.h"
#include "fx_ver.h"
#include "trace.h"
#include "utils.h"
#include "hostfxr_resolver.h"
#include <cinttypes>

#if !defined(FEATURE_LIBHOST)
#define CURHOST_TYPE    _X("dotnet")
#define CURHOST_EXE
#endif

void need_newer_framework_error(const pal::string_t& dotnet_root, const pal::string_t& host_path)
{
    trace::error(
        MISSING_RUNTIME_ERROR_FORMAT,
        INSTALL_OR_UPDATE_NET_ERROR_MESSAGE,
        host_path.c_str(),
        get_current_arch_name(),
        _STRINGIFY(HOST_VERSION),
        dotnet_root.c_str(),
        get_download_url().c_str(),
        _STRINGIFY(HOST_VERSION));
}

#if defined(CURHOST_EXE)

int exe_start(const int argc, const pal::char_t* argv[])
{
    // Use realpath to find the path of the host, resolving any symlinks.
    // hostfxr (for dotnet) and the app dll (for apphost) are found relative to the host.
    pal::string_t host_path;
    if (!pal::get_own_executable_path(&host_path) || !pal::fullpath(&host_path))
    {
        trace::error(_X("Failed to resolve full path of the current executable [%s]"), host_path.c_str());
        return StatusCode::CurrentHostFindFailure;
    }

    pal::string_t app_path;
    pal::string_t app_root;
    pal::string_t own_name = strip_executable_ext(get_filename(host_path));

    if (pal::strcasecmp(own_name.c_str(), CURHOST_TYPE) != 0)
    {
        // The reason for this check is security.
        // dotnet.exe is signed by Microsoft. It is technically possible to rename the file MyApp.exe and include it in the application.
        // Then one can create a shortcut for "MyApp.exe MyApp.dll" which works. The end result is that MyApp looks like it's signed by Microsoft.
        // To prevent this dotnet.exe must not be renamed, otherwise it won't run.
        trace::error(_X("Error: cannot execute %s when renamed to %s."), CURHOST_TYPE, own_name.c_str());
        return StatusCode::CoreHostEntryPointFailure;
    }

    if (argc <= 1)
    {
        trace::println();
        trace::println(_X("Usage: dotnet [path-to-application]"));
        trace::println(_X("Usage: dotnet [commands]"));
        trace::println();
        trace::println(_X("path-to-application:"));
        trace::println(_X("  The path to an application .dll file to execute."));
        trace::println();
        trace::println(_X("commands:"));
        trace::println(_X("  -h|--help                         Display help."));
        trace::println(_X("  --info                            Display .NET information."));
        trace::println(_X("  --list-runtimes [--arch <arch>]   Display the installed runtimes matching the host or specified architecture. Example architectures: arm64, x64, x86."));
        trace::println(_X("  --list-sdks [--arch <arch>]       Display the installed SDKs matching the host or specified architecture. Example architectures: arm64, x64, x86."));
        return StatusCode::InvalidArgFailure;
    }

    app_root.assign(host_path);
    app_path.assign(get_directory(app_root));
    append_path(&app_path, own_name.c_str());
    app_path.append(_X(".dll"));

    hostfxr_resolver_t fxr{app_root};

    // Obtain the entrypoints.
    int rc = fxr.status_code();
    if (rc != StatusCode::Success)
        return rc;

    auto hostfxr_main_startupinfo = fxr.resolve_main_startupinfo();
    if (hostfxr_main_startupinfo != nullptr)
    {
        const pal::char_t* host_path_cstr = host_path.c_str();
        const pal::char_t* dotnet_root_cstr = fxr.dotnet_root().empty() ? nullptr : fxr.dotnet_root().c_str();
        const pal::char_t* app_path_cstr = app_path.empty() ? nullptr : app_path.c_str();

        trace::info(_X("Invoking fx resolver [%s] hostfxr_main_startupinfo"), fxr.fxr_path().c_str());
        trace::info(_X("Host path: [%s]"), host_path.c_str());
        trace::info(_X("Dotnet path: [%s]"), fxr.dotnet_root().c_str());
        trace::info(_X("App path: [%s]"), app_path.c_str());

        auto set_error_writer = fxr.resolve_set_error_writer();
        propagate_error_writer_t propagate_error_writer_to_hostfxr(set_error_writer);

        rc = hostfxr_main_startupinfo(argc, argv, host_path_cstr, dotnet_root_cstr, app_path_cstr);

        // This check exists to provide an error message for apps when running 3.0 apps on 2.0 only hostfxr, which doesn't support error writer redirection.
        // Note that this is not only for UI apps - on Windows we always write errors to event log as well (regardless of UI) and it uses
        // the same mechanism of redirecting error writers.
        if (trace::get_error_writer() != nullptr && rc == static_cast<int>(StatusCode::FrameworkMissingFailure) && set_error_writer == nullptr)
        {
            need_newer_framework_error(fxr.dotnet_root(), host_path);
        }
    }
    else
    {
        trace::info(_X("Invoking fx resolver [%s] v1"), fxr.fxr_path().c_str());

        // Previous corehost trace messages must be printed before calling trace::setup in hostfxr
        trace::flush();

        // For compat, use the v1 interface. This requires additional file I\O to re-parse parameters and
        // for apphost, does not support DOTNET_ROOT or dll with different name for exe.
        auto main_fn_v1 = fxr.resolve_main_v1();
        if (main_fn_v1 != nullptr)
        {
            rc = main_fn_v1(argc, argv);
        }
        else
        {
            trace::error(_X("The required library %s does not contain the expected entry point."), fxr.fxr_path().c_str());
            rc = StatusCode::CoreHostEntryPointFailure;
        }
    }

    return rc;
}

#if defined(_WIN32)
int __cdecl wmain(const int argc, const pal::char_t* argv[])
#else
int main(const int argc, const pal::char_t* argv[])
#endif
{
    trace::setup();

    if (trace::is_enabled())
    {
        trace::info(_X("--- Invoked %s [version: %s] main = {"), CURHOST_TYPE, get_host_version_description().c_str());
        for (int i = 0; i < argc; ++i)
        {
            trace::info(_X("%s"), argv[i]);
        }
        trace::info(_X("}"));
    }

    int exit_code = exe_start(argc, argv);

    // Flush traces before exit - just to be sure
    trace::flush();

    return exit_code;
}

#endif
