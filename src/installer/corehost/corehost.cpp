// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal.h"
#include <corehost.h>
#include "error_codes.h"
#include "fx_ver.h"
#include "trace.h"
#include "utils.h"

// Declarations of hostfxr entry points
using hostfxr_main_fn = int(*)(const int argc, const pal::char_t* argv[]);
using hostfxr_main_startupinfo_fn = int(*)(
    const int argc,
    const pal::char_t* argv[],
    const pal::char_t* host_path,
    const pal::char_t* dotnet_root,
    const pal::char_t* app_path);
using hostfxr_get_com_activation_delegate_fn = int(*)(
    const pal::char_t* host_path,
    const pal::char_t* dotnet_root,
    const pal::char_t* app_path,
    void **delegate);

bool get_latest_fxr(pal::string_t fxr_root, pal::string_t* out_fxr_path);

// Forward declaration of required custom feature APIs
typedef int(*hostfxr_main_fn) (const int argc, const pal::char_t* argv[]);
typedef int(*hostfxr_main_startupinfo_fn) (const int argc, const pal::char_t* argv[], const pal::char_t* host_path, const pal::char_t* dotnet_root, const pal::char_t* app_path);
typedef void(*hostfxr_error_writer_fn) (const pal::char_t* message);
typedef hostfxr_error_writer_fn(*hostfxr_set_error_writer_fn) (hostfxr_error_writer_fn error_writer);

// Attempt to resolve fxr and the dotnet root using host specific logic
bool resolve_fxr_path(const pal::string_t& root_path, pal::string_t* out_dotnet_root, pal::string_t* out_fxr_path);

#if FEATURE_APPHOST
#define CURHOST_TYPE    _X("apphost")
#define CUREXE_PKG_VER  APPHOST_PKG_VER
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

bool resolve_fxr_path(const pal::string_t& app_root, pal::string_t* out_dotnet_root, pal::string_t* out_fxr_path)
{
    // If a hostfxr exists in app_root, then assume self-contained.
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
    append_path(&fxr_dir, _X("host"));
    append_path(&fxr_dir, _X("fxr"));
    if (!pal::directory_exists(fxr_dir))
    {
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
        return false;
    }

    if (!get_latest_fxr(std::move(fxr_dir), out_fxr_path))
        return false;

    return true;
}

#elif FEATURE_LIBHOST
#define CURHOST_TYPE    _X("libhost")
#define CUREXE_PKG_VER  LIBHOST_PKG_VER
#define CURHOST_LIB

bool resolve_fxr_path(const pal::string_t& root_path, pal::string_t* out_dotnet_root, pal::string_t* out_fxr_path)
{
    // If a hostfxr exists in root_path, then assume self-contained.
    if (library_exists_in_dir(root_path, LIBFXR_NAME, out_fxr_path))
    {
        trace::info(_X("Resolved fxr [%s]..."), out_fxr_path->c_str());
        out_dotnet_root->assign(root_path);
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
        pal::string_t default_install_location;
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
    append_path(&fxr_dir, _X("host"));
    append_path(&fxr_dir, _X("fxr"));
    if (!pal::directory_exists(fxr_dir))
    {
        trace::error(_X("A fatal error occurred, the required library %s could not be found.\n"
            "If this is a self-contained application, that library should exist in [%s].\n"
            "If this is a framework-dependent application, install the runtime in the default location [%s]."),
            LIBFXR_NAME,
            root_path.c_str(),
            default_install_location.c_str());
        return false;
    }

    if (!get_latest_fxr(std::move(fxr_dir), out_fxr_path))
        return false;

    return true;
}

#else // !FEATURE_APPHOST && !FEATURE_LIBHOST
#define CURHOST_TYPE    _X("dotnet")
#define CUREXE_PKG_VER  HOST_PKG_VER
#define CURHOST_EXE

bool resolve_fxr_path(const pal::string_t& host_path, pal::string_t* out_dotnet_root, pal::string_t* out_fxr_path)
{
    pal::string_t host_dir;
    host_dir.assign(get_directory(host_path));

    out_dotnet_root->assign(host_dir);

    pal::string_t fxr_dir = *out_dotnet_root;
    append_path(&fxr_dir, _X("host"));
    append_path(&fxr_dir, _X("fxr"));
    if (!pal::directory_exists(fxr_dir))
    {
        trace::error(_X("A fatal error occurred, the folder [%s] does not exist"), fxr_dir.c_str());
        return false;
    }

    if (!get_latest_fxr(std::move(fxr_dir), out_fxr_path))
        return false;

    return true;
}

#endif // !FEATURE_APPHOST && !FEATURE_LIBHOST

bool get_latest_fxr(pal::string_t fxr_root, pal::string_t* out_fxr_path)
{
    trace::info(_X("Reading fx resolver directory=[%s]"), fxr_root.c_str());

    std::vector<pal::string_t> list;
    pal::readdir_onlydirectories(fxr_root, &list);

    fx_ver_t max_ver;
    for (const auto& dir : list)
    {
        trace::info(_X("Considering fxr version=[%s]..."), dir.c_str());

        pal::string_t ver = get_filename(dir);

        fx_ver_t fx_ver;
        if (fx_ver_t::parse(ver, &fx_ver, false))
        {
            max_ver = std::max(max_ver, fx_ver);
        }
    }

    if (max_ver == fx_ver_t())
    {
        trace::error(_X("A fatal error occurred, the folder [%s] does not contain any version-numbered child folders"), fxr_root.c_str());
        return false;
    }

    pal::string_t max_ver_str = max_ver.as_str();
    append_path(&fxr_root, max_ver_str.c_str());
    trace::info(_X("Detected latest fxr version=[%s]..."), fxr_root.c_str());

    if (library_exists_in_dir(fxr_root, LIBFXR_NAME, out_fxr_path))
    {
        trace::info(_X("Resolved fxr [%s]..."), out_fxr_path->c_str());
        return true;
    }

    trace::error(_X("A fatal error occurred, the required library %s could not be found in [%s]"), LIBFXR_NAME, fxr_root.c_str());

    return false;
}

#if defined(CURHOST_LIB)

int get_com_activation_delegate(pal::string_t *app_path, com_activation_fn *delegate)
{
    pal::string_t host_path;
    if (!pal::get_own_module_path(&host_path) || !pal::realpath(&host_path))
    {
        trace::error(_X("Failed to resolve full path of the current host module [%s]"), host_path.c_str());
        return StatusCode::CoreHostCurHostFindFailure;
    }

    pal::string_t dotnet_root;
    pal::string_t fxr_path;
    if (!resolve_fxr_path(host_path, &dotnet_root, &fxr_path))
    {
        return StatusCode::CoreHostLibMissingFailure;
    }

    // Load library
    pal::dll_t fxr;
    if (!pal::load_library(&fxr_path, &fxr))
    {
        trace::error(_X("The library %s was found, but loading it from %s failed"), LIBFXR_NAME, fxr_path.c_str());
        trace::error(_X("  - Installing .NET Core prerequisites might help resolve this problem."));
        trace::error(_X("     %s"), DOTNET_CORE_INSTALL_PREREQUISITES_URL);
        return StatusCode::CoreHostLibLoadFailure;
    }

    // Leak fxr

    auto get_com_delegate = (hostfxr_get_com_activation_delegate_fn)pal::get_symbol(fxr, "hostfxr_get_com_activation_delegate");
    if (get_com_delegate == nullptr)
        return StatusCode::CoreHostEntryPointFailure;

    pal::string_t app_path_local{ host_path };

    // Strip the comhost suffix to get the 'app'
    size_t idx = app_path_local.rfind(_X(".comhost.dll"));
    assert(idx != pal::string_t::npos);
    app_path_local.replace(app_path_local.begin() + idx, app_path_local.end(), _X(".dll"));

    *app_path = std::move(app_path_local);

    auto set_error_writer_fn = (hostfxr_set_error_writer_fn)pal::get_symbol(fxr, "hostfxr_set_error_writer");
    propagate_error_writer_t propagate_error_writer_to_hostfxr(set_error_writer_fn);

    return get_com_delegate(host_path.c_str(), dotnet_root.c_str(), app_path->c_str(), (void**)delegate);
}

#elif defined(CURHOST_EXE)

int exe_start(const int argc, const pal::char_t* argv[])
{
    pal::string_t host_path;
    if (!pal::get_own_executable_path(&host_path) || !pal::realpath(&host_path))
    {
        trace::error(_X("Failed to resolve full path of the current executable [%s]"), host_path.c_str());
        return StatusCode::CoreHostCurHostFindFailure;
    }

    pal::string_t app_path;
    pal::string_t app_root;
    bool requires_v2_hostfxr_interface = false;

#if FEATURE_APPHOST
    pal::string_t embedded_app_name;
    if (!is_exe_enabled_for_execution(&embedded_app_name))
    {
        trace::error(_X("A fatal error was encountered. This executable was not bound to load a managed DLL."));
        return StatusCode::AppHostExeNotBoundFailure;
    }

    if (_X('/') != DIR_SEPARATOR)
    {
        replace_char(&embedded_app_name, _X('/'), DIR_SEPARATOR);
    }

    auto pos_path_char = embedded_app_name.find(DIR_SEPARATOR);
    if (pos_path_char != pal::string_t::npos)
    {
        requires_v2_hostfxr_interface = true;
    }

    app_path.assign(get_directory(host_path));
    append_path(&app_path, embedded_app_name.c_str());
    if (!pal::realpath(&app_path))
    {
        trace::error(_X("The application to execute does not exist: '%s'."), app_path.c_str());
        return StatusCode::LibHostAppRootFindFailure;
    }

    app_root.assign(get_directory(app_path));
#else
    pal::string_t own_name = strip_executable_ext(get_filename(host_path));

    if (pal::strcasecmp(own_name.c_str(), CURHOST_TYPE) != 0)
    {
        trace::error(_X("A fatal error was encountered. Cannot execute %s when renamed to %s."), CURHOST_TYPE, own_name.c_str());
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
        trace::println(_X("  --info            Display .NET Core information."));
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

    pal::string_t dotnet_root;
    pal::string_t fxr_path;
    if (!resolve_fxr_path(app_root, &dotnet_root, &fxr_path))
    {
        return StatusCode::CoreHostLibMissingFailure;
    }

    // Load library
    pal::dll_t fxr;
    if (!pal::load_library(&fxr_path, &fxr))
    {
        trace::error(_X("The library %s was found, but loading it from %s failed"), LIBFXR_NAME, fxr_path.c_str());
        trace::error(_X("  - Installing .NET Core prerequisites might help resolve this problem."));
        trace::error(_X("     %s"), DOTNET_CORE_INSTALL_PREREQUISITES_URL);
        return StatusCode::CoreHostLibLoadFailure;
    }

    // Obtain the entrypoints.
    int rc;
    hostfxr_main_startupinfo_fn main_fn_v2 = (hostfxr_main_startupinfo_fn)pal::get_symbol(fxr, "hostfxr_main_startupinfo");
    if (main_fn_v2 != nullptr)
    {
        const pal::char_t* host_path_cstr = host_path.c_str();
        const pal::char_t* dotnet_root_cstr = dotnet_root.empty() ? nullptr : dotnet_root.c_str();
        const pal::char_t* app_path_cstr = app_path.empty() ? nullptr : app_path.c_str();

        trace::info(_X("Invoking fx resolver [%s] v2"), fxr_path.c_str());
        trace::info(_X("Host path: [%s]"), host_path.c_str());
        trace::info(_X("Dotnet path: [%s]"), dotnet_root.c_str());
        trace::info(_X("App path: [%s]"), app_path.c_str());

        hostfxr_set_error_writer_fn set_error_writer_fn = (hostfxr_set_error_writer_fn)pal::get_symbol(fxr, "hostfxr_set_error_writer");

        // Previous corehost trace messages must be printed before calling trace::setup in hostfxr
        trace::flush();

        propagate_error_writer_t propagate_error_writer_to_hostfxr(set_error_writer_fn);

        rc = main_fn_v2(argc, argv, host_path_cstr, dotnet_root_cstr, app_path_cstr);
    }
    else
    {
        if (requires_v2_hostfxr_interface)
        {
            trace::error(_X("The required library %s does not support relative app dll paths."), fxr_path.c_str());
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
            if (main_fn_v1 != nullptr)
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

#if defined(_WIN32) && defined(FEATURE_APPHOST)
pal::string_t g_buffered_errors;

void buffering_trace_writer(const pal::char_t* message)
{
    g_buffered_errors.append(message).append(_X("\n"));
}

// Determines if the current module (should be the apphost.exe) is marked as Windows GUI application
// in case it's not a GUI application (so should be CUI) or in case of any error the function returns false.
bool get_windows_graphical_user_interface_bit()
{
    HMODULE module = ::GetModuleHandleW(NULL);
    BYTE *bytes = (BYTE *)module;

    // https://en.wikipedia.org/wiki/Portable_Executable
    UINT32 pe_header_offset = ((IMAGE_DOS_HEADER *)bytes)->e_lfanew;
    UINT16 subsystem = ((IMAGE_NT_HEADERS *)(bytes + pe_header_offset))->OptionalHeader.Subsystem;

    return subsystem == IMAGE_SUBSYSTEM_WINDOWS_GUI;
}

#endif

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
    if (get_windows_graphical_user_interface_bit())
    {
        // If this is a GUI application, buffer errors to display them later. Without this any errors are effectively lost
        // unless the caller explicitly redirects stderr. This leads to bad experience of running the GUI app and nothing happening.
        trace::set_error_writer(buffering_trace_writer);
    }
#endif

    int exit_code = exe_start(argc, argv);

    // Flush traces before exit - just to be sure, and also if we're showing a popup below the error should show up in traces
    // by the time the popup is displayed.
    trace::flush();

#if defined(_WIN32) && defined(FEATURE_APPHOST)
    // No need to unregister the error writer since we're exiting anyway.
    if (!g_buffered_errors.empty())
    {
        // If there are errors buffered, display them as a dialog. We only buffer if there's no console attached.
        pal::string_t executable_name;
        if (pal::get_own_executable_path(&executable_name))
        {
            executable_name = get_filename(executable_name);
        }

        ::MessageBoxW(NULL, g_buffered_errors.c_str(), executable_name.c_str(), MB_OK);
    }
#endif

    return exit_code;
}

#else

#error A host binary format must be defined

#endif
