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

#if defined(FEATURE_APPHOST)
#include "bundle_marker.h"

#if defined(_WIN32)
#include "apphost/apphost.windows.h"
#endif

#define CURHOST_TYPE    _X("apphost")
#define CUREXE_PKG_VER  COMMON_HOST_PKG_VER
#define CURHOST_EXE

/**
 * Detect if the apphost executable is allowed to load and execute a managed assembly.
 *
 *    - The exe is built with a known hash string at some offset in the image
 *    - The exe is useless as is with the built-in hash value, and will fail with an error message
 *    - The hash value should be replaced with the managed DLL filename with optional relative path
 *    - The optional path is relative to the location of the apphost executable
 *    - The relative path plus filename are verified to reference a valid file
 *    - The filename should be "NUL terminated UTF-8" by "dotnet build"
 *    - The managed DLL filename does not have to be the same name as the apphost executable name
 *    - The exe may be signed at this point by the app publisher
 *    - Note: the maximum size of the filename and relative path is 1024 bytes in UTF-8 (not including NUL)
 *        o https://en.wikipedia.org/wiki/Comparison_of_file_systems
 *          has more details on maximum file name sizes.
 */
#define EMBED_HASH_HI_PART_UTF8 "c3ab8ff13720e8ad9047dd39466b3c89" // SHA-256 of "foobar" in UTF-8
#define EMBED_HASH_LO_PART_UTF8 "74e592c2fa383d4a3960714caef0c4f2"
#define EMBED_HASH_FULL_UTF8    (EMBED_HASH_HI_PART_UTF8 EMBED_HASH_LO_PART_UTF8) // NUL terminated

bool is_exe_enabled_for_execution(pal::string_t* app_dll)
{
    constexpr int EMBED_SZ = sizeof(EMBED_HASH_FULL_UTF8) / sizeof(EMBED_HASH_FULL_UTF8[0]);
    constexpr int EMBED_MAX = (EMBED_SZ > 1025 ? EMBED_SZ : 1025); // 1024 DLL name length, 1 NUL

    // Contains the EMBED_HASH_FULL_UTF8 value at compile time or the managed DLL name replaced by "dotnet build".
    // Must not be 'const' because std::string(&embed[0]) below would bind to a const string ctor plus length
    // where length is determined at compile time (=64) instead of the actual length of the string at runtime.
    static char embed[EMBED_MAX] = EMBED_HASH_FULL_UTF8;     // series of NULs followed by embed hash string

    static const char hi_part[] = EMBED_HASH_HI_PART_UTF8;
    static const char lo_part[] = EMBED_HASH_LO_PART_UTF8;

    if (!pal::clr_palstring(embed, app_dll))
    {
        trace::error(_X("The managed DLL bound to this executable could not be retrieved from the executable image."));
        return false;
    }

    // Since the single static string is replaced by editing the executable, a reference string is needed to do the compare.
    // So use two parts of the string that will be unaffected by the edit.
    size_t hi_len = (sizeof(hi_part) / sizeof(hi_part[0])) - 1;
    size_t lo_len = (sizeof(lo_part) / sizeof(lo_part[0])) - 1;

    std::string binding(&embed[0]);
    if ((binding.size() >= (hi_len + lo_len)) &&
        binding.compare(0, hi_len, &hi_part[0]) == 0 &&
        binding.compare(hi_len, lo_len, &lo_part[0]) == 0)
    {
        trace::error(_X("This executable is not bound to a managed DLL to execute. The binding value is: '%s'"), app_dll->c_str());
        return false;
    }

    trace::info(_X("The managed DLL bound to this executable is: '%s'"), app_dll->c_str());
    return true;
}

#elif !defined(FEATURE_LIBHOST)
#define CURHOST_TYPE    _X("dotnet")
#define CUREXE_PKG_VER  HOST_PKG_VER
#define CURHOST_EXE
#endif

void need_newer_framework_error(const pal::string_t& dotnet_root, const pal::string_t& host_path)
{
    trace::error(
        MISSING_RUNTIME_ERROR_FORMAT,
        INSTALL_OR_UPDATE_NET_ERROR_MESSAGE,
        host_path.c_str(),
        get_current_arch_name(),
        _STRINGIFY(COMMON_HOST_PKG_VER),
        dotnet_root.c_str(),
        get_download_url().c_str(),
        _STRINGIFY(COMMON_HOST_PKG_VER));
}

#if defined(CURHOST_EXE)

int exe_start(const int argc, const pal::char_t* argv[])
{
    pal::initialize_createdump();

    pal::string_t host_path;
    if (!pal::get_own_executable_path(&host_path) || !pal::realpath(&host_path))
    {
        trace::error(_X("Failed to resolve full path of the current executable [%s]"), host_path.c_str());
        return StatusCode::CoreHostCurHostFindFailure;
    }

    pal::string_t app_path;
    pal::string_t app_root;
    bool requires_hostfxr_startupinfo_interface = false;

#if defined(FEATURE_APPHOST)
    pal::string_t embedded_app_name;
    if (!is_exe_enabled_for_execution(&embedded_app_name))
    {
        return StatusCode::AppHostExeNotBoundFailure;
    }

    if (_X('/') != DIR_SEPARATOR)
    {
        replace_char(&embedded_app_name, _X('/'), DIR_SEPARATOR);
    }

    auto pos_path_char = embedded_app_name.find(DIR_SEPARATOR);
    if (pos_path_char != pal::string_t::npos)
    {
        requires_hostfxr_startupinfo_interface = true;
    }

    app_path.assign(get_directory(host_path));
    append_path(&app_path, embedded_app_name.c_str());

    if (bundle_marker_t::is_bundle())
    {
        trace::info(_X("Detected Single-File app bundle"));
    }
    else if (!pal::realpath(&app_path))
    {
        trace::error(_X("The application to execute does not exist: '%s'."), app_path.c_str());
        return StatusCode::LibHostAppRootFindFailure;
    }

    app_root.assign(get_directory(app_path));

#else
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
        trace::println(_X("Usage: dotnet [options]"));
        trace::println(_X("Usage: dotnet [path-to-application]"));
        trace::println();
        trace::println(_X("Options:"));
        trace::println(_X("  -h|--help         Display help."));
        trace::println(_X("  --info            Display .NET information."));
        trace::println(_X("  --list-sdks       Display the installed SDKs."));
        trace::println(_X("  --list-runtimes   Display the installed runtimes."));
        trace::println();
        trace::println(_X("path-to-application:"));
        trace::println(_X("  The path to an application .dll file to execute."));
        return StatusCode::InvalidArgFailure;
    }

    app_root.assign(host_path);
    app_path.assign(get_directory(app_root));
    append_path(&app_path, own_name.c_str());
    app_path.append(_X(".dll"));
#endif

    hostfxr_resolver_t fxr{app_root};

    // Obtain the entrypoints.
    int rc = fxr.status_code();
    if (rc != StatusCode::Success)
    {
        return rc;
    }

#if defined(FEATURE_APPHOST)
    if (bundle_marker_t::is_bundle())
    {
        auto hostfxr_main_bundle_startupinfo = fxr.resolve_main_bundle_startupinfo();
        if (hostfxr_main_bundle_startupinfo != nullptr)
        {
            const pal::char_t* host_path_cstr = host_path.c_str();
            const pal::char_t* dotnet_root_cstr = fxr.dotnet_root().empty() ? nullptr : fxr.dotnet_root().c_str();
            const pal::char_t* app_path_cstr = app_path.empty() ? nullptr : app_path.c_str();
            int64_t bundle_header_offset = bundle_marker_t::header_offset();

            trace::info(_X("Invoking fx resolver [%s] hostfxr_main_bundle_startupinfo"), fxr.fxr_path().c_str());
            trace::info(_X("Host path: [%s]"), host_path.c_str());
            trace::info(_X("Dotnet path: [%s]"), fxr.dotnet_root().c_str());
            trace::info(_X("App path: [%s]"), app_path.c_str());
            trace::info(_X("Bundle Header Offset: [%" PRId64 "]"), bundle_header_offset);

            auto set_error_writer = fxr.resolve_set_error_writer();
            propagate_error_writer_t propagate_error_writer_to_hostfxr(set_error_writer);
            rc = hostfxr_main_bundle_startupinfo(argc, argv, host_path_cstr, dotnet_root_cstr, app_path_cstr, bundle_header_offset);
        }
        else
        {
            // An outdated hostfxr can only be found for framework-related apps.
            trace::error(_X("The required library %s does not support single-file apps."), fxr.fxr_path().c_str());
            need_newer_framework_error(fxr.dotnet_root(), host_path);
            rc = StatusCode::FrameworkMissingFailure;
        }
    }
    else
#endif // defined(FEATURE_APPHOST)
    {
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
#if !defined(FEATURE_STATIC_HOST)
        else
        {
            if (requires_hostfxr_startupinfo_interface)
            {
                trace::error(_X("The required library %s does not support relative app dll paths."), fxr.fxr_path().c_str());
                rc = StatusCode::CoreHostEntryPointFailure;
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
        }
#endif // defined(FEATURE_STATIC_HOST)
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
        trace::info(_X("--- Invoked %s [version: %s, commit hash: %s] main = {"), CURHOST_TYPE, _STRINGIFY(CUREXE_PKG_VER), _STRINGIFY(REPO_COMMIT_HASH));
        for (int i = 0; i < argc; ++i)
        {
            trace::info(_X("%s"), argv[i]);
        }
        trace::info(_X("}"));
    }

#if defined(_WIN32) && defined(FEATURE_APPHOST)
    // Buffer errors to use them later.
    apphost::buffer_errors();
#endif

    int exit_code = exe_start(argc, argv);

    // Flush traces before exit - just to be sure
    trace::flush();

#if defined(_WIN32) && defined(FEATURE_APPHOST)
    // No need to unregister the error writer since we're exiting anyway.
    apphost::write_buffered_errors(exit_code);
#endif

    return exit_code;
}

#endif
