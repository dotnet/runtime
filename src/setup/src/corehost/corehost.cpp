// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "error_codes.h"
#include "fx_ver.h"
#include "pal.h"
#include "trace.h"
#include "utils.h"

#if FEATURE_APPHOST
#include "startup_config.h"
#define CURHOST_TYPE    _X("apphost")
#define CUREXE_PKG_VER APPHOST_PKG_VER
#else // !FEATURE_APPHOST
#define CURHOST_TYPE    _X("dotnet")
#define CUREXE_PKG_VER HOST_PKG_VER
#endif // !FEATURE_APPHOST

typedef int(*hostfxr_main_fn) (const int argc, const pal::char_t* argv[]);
typedef int(*hostfxr_main_startupinfo_fn) (const int argc, const pal::char_t* argv[], const pal::char_t* host_path, const pal::char_t* dotnet_root, const pal::char_t* app_path);

#if FEATURE_APPHOST

/**
 * Detect if the apphost executable is allowed to load and execute a managed assembly.
 *
 *    - The exe is built with a known hash string at some offset in the image
 *    - The exe is useless as is with the built-in hash value, and will fail with an error message
 *    - The hash value should be replaced with the managed DLL filename using "NUL terminated UTF-8" by "dotnet build"
 *    - The exe may be signed at this point by the app publisher
 *    - When the exe runs, the managed DLL name is validated against the executable's own name
 *    - If validation passes, the embedded managed DLL name will be loaded by the exe
 *    - Note: the maximum size of the managed DLL file name can be 1024 bytes in UTF-8 (not including NUL)
 *        o https://en.wikipedia.org/wiki/Comparison_of_file_systems
 *          has more details on maximum file name sizes.
 */
#define EMBED_HASH_HI_PART_UTF8 "c3ab8ff13720e8ad9047dd39466b3c89" // SHA-256 of "foobar" in UTF-8
#define EMBED_HASH_LO_PART_UTF8 "74e592c2fa383d4a3960714caef0c4f2"
#define EMBED_HASH_FULL_UTF8    (EMBED_HASH_HI_PART_UTF8 EMBED_HASH_LO_PART_UTF8) // NUL terminated
bool is_exe_enabled_for_execution(const pal::string_t& host_path, pal::string_t* app_dll)
{
    constexpr int EMBED_SZ = sizeof(EMBED_HASH_FULL_UTF8) / sizeof(EMBED_HASH_FULL_UTF8[0]);
    constexpr int EMBED_MAX = (EMBED_SZ > 1025 ? EMBED_SZ : 1025); // 1024 DLL name length, 1 NUL

    // Contains the embed hash value at compile time or the managed DLL name replaced by "dotnet build".
    // Must not be 'const' because std::string(&embed[0]) below would bind to a const string ctor plus length
    // where length is determined at compile time (=64) instead of the actual length of the string at runtime.
    static char embed[EMBED_MAX] = EMBED_HASH_FULL_UTF8;     // series of NULs followed by embed hash string

    static const char hi_part[] = EMBED_HASH_HI_PART_UTF8;
    static const char lo_part[] = EMBED_HASH_LO_PART_UTF8;

    std::string binding(&embed[0]);
    if (!pal::utf8_palstring(binding, app_dll))
    {
        trace::error(_X("The managed DLL bound to this executable could not be retrieved from the executable image."));
        return false;
    }

    // Since the single static string is replaced by editing the executable, a reference string is needed to do the compare.
    // So use two parts of the string that will be unaffected by the edit.
    size_t hi_len = (sizeof(hi_part) / sizeof(hi_part[0])) - 1;
    size_t lo_len = (sizeof(lo_part) / sizeof(lo_part[0])) - 1;

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

bool resolve_app_root(const pal::string_t& host_path, pal::string_t* out_app_root, bool* requires_v2_hostfxr_interface)
{
    // For self-contained, the startupconfig.json specifies app_root which is used later to assign dotnet_root
    // For framework-dependent, the startupconfig.json specifies app_root which does not affect dotnet_root
    pal::string_t config_path = strip_executable_ext(host_path);
    config_path += _X(".startupconfig.json");
    startup_config_t startup_config;
    startup_config.parse(config_path);
    if (!startup_config.is_valid())
    {
        return false;
    }

    if (startup_config.get_app_root().empty())
    {
        out_app_root->assign(get_directory(host_path));
    }
    else
    {
        *requires_v2_hostfxr_interface = true;
        if (pal::is_path_rooted(startup_config.get_app_root()))
        {
            out_app_root->assign(startup_config.get_app_root());
        }
        else
        {
            out_app_root->assign(get_directory(host_path));
            append_path(out_app_root, startup_config.get_app_root().c_str());
        }
        if (!pal::realpath(out_app_root))
        {
            trace::error(_X("The app root [%s] specified in [%s] does not exist."),
                out_app_root->c_str(), config_path.c_str());
            return false;
        }
    }

    return true;
}
#endif

bool resolve_fxr_path(const pal::string_t& host_path, const pal::string_t& app_root, pal::string_t* out_dotnet_root, pal::string_t* out_fxr_path)
{
    pal::string_t host_dir;
    host_dir.assign(get_directory(host_path));

#if FEATURE_APPHOST
    // If a hostfxr exists in app_root, then assumed self-contained.
    if (library_exists_in_dir(app_root, LIBFXR_NAME, out_fxr_path))
    {
        trace::info(_X("Resolved fxr [%s]..."), out_fxr_path->c_str());
        out_dotnet_root->assign(app_root);
        return true;
    }

    // For framework-dependent apps, use DOTNET_ROOT

    pal::string_t default_install_location;
    pal::string_t dotnet_root_env_var_name = get_dotnet_root_env_var_name();
    if (get_file_path_from_env(dotnet_root_env_var_name.c_str(), out_dotnet_root))
    {
        trace::info(_X("Using environment variable %s=[%s] as runtime location."), dotnet_root_env_var_name.c_str(), out_dotnet_root->c_str());
    }
    else
    {
        // Check default installation root as fallback
        if (!pal::get_default_installation_dir(&default_install_location))
        {
            trace::error(_X("A fatal error occurred, the default install location cannot be obtained."));
            return false;
        }
        trace::info(_X("Using default installation location [%s] as runtime location."), default_install_location.c_str());
        out_dotnet_root->assign(default_install_location);
    }

    pal::string_t fxr_dir = *out_dotnet_root;
#else
    out_dotnet_root->assign(host_dir);
    pal::string_t fxr_dir = host_dir;
#endif
    append_path(&fxr_dir, _X("host"));
    append_path(&fxr_dir, _X("fxr"));
    if (!pal::directory_exists(fxr_dir))
    {
#if FEATURE_APPHOST
        if (default_install_location.empty())
        {
            pal::get_default_installation_dir(&default_install_location);
        }

        trace::error(_X("A fatal error occurred, the required library %s could not be found.\n"
            "If this is a self-contained application, that library should exist in [%s].\n"
            "If this is a framework-dependent application, install the runtime in the default location [%s] or use the %s environment variable to specify the runtime location."),
            LIBFXR_NAME,
            app_root.c_str(),
            default_install_location.c_str(),
            dotnet_root_env_var_name.c_str());
#else
        trace::error(_X("A fatal error occurred, the folder [%s] does not exist"), fxr_dir.c_str()); 
#endif
        return false;
    }

    trace::info(_X("Reading fx resolver directory=[%s]"), fxr_dir.c_str());

    std::vector<pal::string_t> list;
    pal::readdir_onlydirectories(fxr_dir, &list);

    fx_ver_t max_ver(-1, -1, -1);
    for (const auto& dir : list)
    {
        trace::info(_X("Considering fxr version=[%s]..."), dir.c_str());

        pal::string_t ver = get_filename(dir);

        fx_ver_t fx_ver(-1, -1, -1);
        if (fx_ver_t::parse(ver, &fx_ver, false))
        {
            max_ver = std::max(max_ver, fx_ver);
        }
    }

    if (max_ver == fx_ver_t(-1, -1, -1))
    {
        trace::error(_X("A fatal error occurred, the folder [%s] does not contain any version-numbered child folders"), fxr_dir.c_str());
        return false;
    }

    pal::string_t max_ver_str = max_ver.as_str();
    append_path(&fxr_dir, max_ver_str.c_str());
    trace::info(_X("Detected latest fxr version=[%s]..."), fxr_dir.c_str());

    if (library_exists_in_dir(fxr_dir, LIBFXR_NAME, out_fxr_path))
    {
        trace::info(_X("Resolved fxr [%s]..."), out_fxr_path ->c_str());
        return true;
    }

    trace::error(_X("A fatal error occurred, the required library %s could not be found in [%s]"), LIBFXR_NAME, fxr_dir.c_str());

    return false;
}

int run(const int argc, const pal::char_t* argv[])
{
    pal::string_t host_path;
    if (!pal::get_own_executable_path(&host_path) || !pal::realpath(&host_path))
    {
        trace::error(_X("Failed to resolve full path of the current executable [%s]"), host_path.c_str());
        return StatusCode::CoreHostCurExeFindFailure;
    }

    pal::string_t app_root;
    pal::string_t app_path;
    bool requires_v2_hostfxr_interface = false;

#if FEATURE_APPHOST
    pal::string_t app_dll_name;
    if (!is_exe_enabled_for_execution(host_path, &app_dll_name))
    {
        trace::error(_X("A fatal error was encountered. This executable was not bound to load a managed DLL."));
        return StatusCode::AppHostExeNotBoundFailure;
    }

    if (!resolve_app_root(host_path, &app_root, &requires_v2_hostfxr_interface))
    {
        return StatusCode::LibHostAppRootFindFailure;
    }

    app_path.assign(app_root);
    append_path(&app_path, app_dll_name.c_str());
#else
    pal::string_t own_name = strip_executable_ext(get_filename(host_path));

    if (pal::strcasecmp(own_name.c_str(), CURHOST_TYPE) != 0)
    {
        trace::error(_X("A fatal error was encountered. Cannot execute %s when renamed to  %s."), CURHOST_TYPE,own_name.c_str());
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
        trace::println(_X("  --info            Display .NET Core information.."));
        trace::println(_X("  --list-sdks       Display the installed SDKs."));
        trace::println(_X("  --list-runtimes   Display the installed runtimes."));
        trace::println();
        trace::println(_X("path-to-application:"));
        trace::println(_X("  The path to an application .dll file to execute."));
        return StatusCode::InvalidArgFailure;
    }

    app_root.assign(host_path);
    app_path.assign(app_root);
    append_path(&app_path, own_name.c_str());
    app_path.append(_X(".dll"));
#endif

    pal::string_t dotnet_root;
    pal::string_t fxr_path;
    if (!resolve_fxr_path(host_path, app_root, &dotnet_root, &fxr_path))
    {
        return StatusCode::CoreHostLibMissingFailure;
    }

    // Load library
    pal::dll_t fxr;
    if (!pal::load_library(&fxr_path, &fxr))
    {
        trace::error(_X("The library %s was found, but loading it from %s failed"), LIBFXR_NAME, fxr_path.c_str());
        trace::error(_X("  - Installing .NET Core prerequisites might help resolve this problem."));
        trace::error(_X("     %s"), DOTNET_CORE_GETTING_STARTED_URL);
        return StatusCode::CoreHostLibLoadFailure;
    }

    // Obtain the entrypoints.
    int rc;
    hostfxr_main_startupinfo_fn main_fn_v2 = (hostfxr_main_startupinfo_fn)pal::get_symbol(fxr, "hostfxr_main_startupinfo");
    if (main_fn_v2)
    {
        const pal::char_t* host_path_cstr = host_path.c_str();
        const pal::char_t* dotnet_root_cstr = dotnet_root.empty() ? nullptr : dotnet_root.c_str();
        const pal::char_t* app_path_cstr = app_path.empty() ? nullptr : app_path.c_str();

        trace::info(_X("Invoking fx resolver [%s] v2"), fxr_path.c_str());
        trace::info(_X("Host path: [%s]"), host_path.c_str());
        trace::info(_X("Dotnet path: [%s]"), dotnet_root.c_str());
        trace::info(_X("App path: [%s]"), app_path.c_str());

        // Previous corehost trace messages must be printed before calling trace::setup in hostfxr
        trace::flush();

        rc = main_fn_v2(argc, argv, host_path_cstr, dotnet_root_cstr, app_path_cstr);
    }
    else
    {
        if (requires_v2_hostfxr_interface)
        {
            trace::error(_X("The required library %s does not support startupconfig.json functionality."), fxr_path.c_str());
            rc = StatusCode::CoreHostEntryPointFailure;
        }
        else
        {
            trace::info(_X("Invoking fx resolver [%s] v1"), fxr_path.c_str());

            // Previous corehost trace messages must be printed before calling trace::setup in hostfxr
            trace::flush();

            // For compat, use the v1 interface. This requires additional file I\O to re-parse parameters and
            // for apphost, does not support DOTNET_ROOT or dll with different name for exe.
            hostfxr_main_fn main_fn_v1 = (hostfxr_main_fn)pal::get_symbol(fxr, "hostfxr_main");
            if (main_fn_v1)
            {
                rc = main_fn_v1(argc, argv);
            }
            else
            {
                trace::error(_X("The required library %s does not contain the expected entry point."), fxr_path.c_str());
                rc = StatusCode::CoreHostEntryPointFailure;
            }
        }
    }

    pal::unload_library(fxr);
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

    return run(argc, argv);
}
