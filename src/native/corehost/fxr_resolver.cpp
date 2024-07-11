// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <pal.h>
#include "fxr_resolver.h"
#include <fx_ver.h>
#include <trace.h>
#include <utils.h>

namespace
{
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
            if (fx_ver_t::parse(ver, &fx_ver, /* parse_only_production */ false))
            {
                max_ver = std::max(max_ver, fx_ver);
            }
        }

        if (max_ver == fx_ver_t())
        {
            trace::error(_X("Error: [%s] does not contain any version-numbered child folders"), fxr_root.c_str());
            return false;
        }

        pal::string_t max_ver_str = max_ver.as_str();
        append_path(&fxr_root, max_ver_str.c_str());
        trace::info(_X("Detected latest fxr version=[%s]..."), fxr_root.c_str());

        if (file_exists_in_dir(fxr_root, LIBFXR_NAME, out_fxr_path))
        {
            trace::info(_X("Resolved fxr [%s]..."), out_fxr_path->c_str());
            return true;
        }

        trace::error(_X("Error: the required library %s could not be found in [%s]"), LIBFXR_NAME, fxr_root.c_str());

        return false;
    }

    // The default is defined as app-local, environment variables, and global install locations
    const fxr_resolver::search_location s_default_search = static_cast<fxr_resolver::search_location>(
        fxr_resolver::search_location_app_local | fxr_resolver::search_location_environment_variable | fxr_resolver::search_location_global);
}

bool fxr_resolver::try_get_path(const pal::string_t& root_path, pal::string_t* out_dotnet_root, pal::string_t* out_fxr_path)
{
    return try_get_path(root_path, search_location_default, nullptr, out_dotnet_root, out_fxr_path);
}

bool fxr_resolver::try_get_path(
    const pal::string_t& root_path,
    search_location search,
    /*opt*/ pal::string_t* app_relative_dotnet_root,
    /*out*/ pal::string_t* out_dotnet_root,
    /*out*/ pal::string_t* out_fxr_path)
{
#if defined(FEATURE_APPHOST) || defined(FEATURE_LIBHOST)
    if (search == search_location_default)
        search = s_default_search;

    // For apphost and libhost, root_path is expected to be a directory.
    // For libhost, it may be empty if app-local search is not desired (e.g. com/ijw/winrt hosts, nethost when no assembly path is specified)
    // If a hostfxr exists in root_path, then assume self-contained.
    bool search_app_local = (search & search_location_app_local) != 0;
    if (search_app_local && root_path.length() > 0 && file_exists_in_dir(root_path, LIBFXR_NAME, out_fxr_path))
    {
        trace::info(_X("Using app-local location [%s] as runtime location."), root_path.c_str());
        trace::info(_X("Resolved fxr [%s]..."), out_fxr_path->c_str());
        out_dotnet_root->assign(root_path);
        return true;
    }

    // Check in priority order (if specified by search location options):
    //   - App-relative .NET root location
    //   - Environment variables: DOTNET_ROOT_<ARCH> and DOTNET_ROOT
    //   - Global installs:
    //      - self-registered install location
    //      - default install location
    bool search_app_relative = (search & search_location_app_relative) != 0 && app_relative_dotnet_root != nullptr && !app_relative_dotnet_root->empty();
    bool search_env = (search & search_location_environment_variable) != 0;
    bool search_global = (search & search_location_global) != 0;
    pal::string_t default_install_location;
    pal::string_t dotnet_root_env_var_name;
    if (search_app_relative && pal::realpath(app_relative_dotnet_root))
    {
        trace::info(_X("Using app-relative location [%s] as runtime location."), app_relative_dotnet_root->c_str());
        out_dotnet_root->assign(*app_relative_dotnet_root);
        if (file_exists_in_dir(*app_relative_dotnet_root, LIBFXR_NAME, out_fxr_path))
        {
            trace::info(_X("Resolved fxr [%s]..."), out_fxr_path->c_str());
            return true;
        }
    }
    else if (search_env && get_dotnet_root_from_env(&dotnet_root_env_var_name, out_dotnet_root))
    {
        trace::info(_X("Using environment variable %s=[%s] as runtime location."), dotnet_root_env_var_name.c_str(), out_dotnet_root->c_str());
    }
    else if (search_global)
    {
        if (pal::get_dotnet_self_registered_dir(&default_install_location) || pal::get_default_installation_dir(&default_install_location))
        {
            trace::info(_X("Using global install location [%s] as runtime location."), default_install_location.c_str());
            out_dotnet_root->assign(default_install_location);
        }
        else
        {
            trace::error(_X("Error: the default install location cannot be obtained."));
            return false;
        }
    }

    pal::string_t fxr_dir = *out_dotnet_root;
    append_path(&fxr_dir, _X("host"));
    append_path(&fxr_dir, _X("fxr"));
    if (pal::directory_exists(fxr_dir))
        return get_latest_fxr(std::move(fxr_dir), out_fxr_path);

    // Failed to find hostfxr
    if (trace::is_enabled())
    {
        trace::verbose(_X("The required library %s could not be found. Search location options [0x%x]"), LIBFXR_NAME, search);
        if (search_app_local)
            trace::verbose(_X("  app-local: [%s]"), root_path.c_str());

        if (search_app_relative)
            trace::verbose(_X("  app-relative: [%s]"), app_relative_dotnet_root->c_str());

        if (search_env)
            trace::verbose(_X("  environment variable: [%s]"), dotnet_root_env_var_name.c_str());

        if (search_global)
        {
            if (default_install_location.empty())
            {
                pal::get_dotnet_self_registered_dir(&default_install_location);
            }
            if (default_install_location.empty())
            {
                pal::get_default_installation_dir(&default_install_location);
            }

            pal::string_t self_registered_config_location = pal::get_dotnet_self_registered_config_location(get_current_arch());
            trace::verbose(_X("  global install location [%s]\n  self-registered config location [%s]"),
                default_install_location.c_str(),
                self_registered_config_location.c_str());
        }
    }

    pal::string_t host_path;
    pal::get_own_executable_path(&host_path);

    pal::string_t location = _X("Not found");
    if (search != s_default_search)
    {
        location.append(_X(" - search options: ["));
        if (search_app_local)
            location.append(_X(" app_local"));

        if (search_app_relative)
            location.append(_X(" app_relative"));

        if (search_env)
            location.append(_X(" environment_variable"));

        if (search_global)
            location.append(_X(" global"));

        location.append(_X(" ]"));
        if (search_app_relative)
        {
            location.append(_X(", app-relative path: "));
            location.append(app_relative_dotnet_root->c_str());
        }
    }

    trace::error(
        MISSING_RUNTIME_ERROR_FORMAT,
        INSTALL_NET_ERROR_MESSAGE,
        host_path.c_str(),
        get_current_arch_name(),
        _STRINGIFY(HOST_VERSION),
        location.c_str(),
        get_download_url().c_str(),
        _STRINGIFY(HOST_VERSION));
    return false;

#else // !FEATURE_APPHOST && !FEATURE_LIBHOST
    // For non-apphost and non-libhost (i.e. muxer), root_path is expected to be the full path to the host
    pal::string_t host_dir;
    host_dir.assign(get_directory(root_path));

    out_dotnet_root->assign(host_dir);

    return fxr_resolver::try_get_path_from_dotnet_root(*out_dotnet_root, out_fxr_path);
#endif // !FEATURE_APPHOST && !FEATURE_LIBHOST
}

bool fxr_resolver::try_get_path_from_dotnet_root(const pal::string_t& dotnet_root, pal::string_t* out_fxr_path)
{
    pal::string_t fxr_dir = dotnet_root;
    append_path(&fxr_dir, _X("host"));
    append_path(&fxr_dir, _X("fxr"));
    if (!pal::directory_exists(fxr_dir))
    {
        trace::error(_X("Error: [%s] does not exist"), fxr_dir.c_str());
        return false;
    }

    return get_latest_fxr(std::move(fxr_dir), out_fxr_path);
}

bool fxr_resolver::try_get_existing_fxr(pal::dll_t* out_fxr, pal::string_t* out_fxr_path)
{
    if (!pal::get_loaded_library(LIBFXR_NAME, "hostfxr_main", out_fxr, out_fxr_path))
        return false;

    trace::verbose(_X("Found previously loaded library %s [%s]."), LIBFXR_NAME, out_fxr_path->c_str());
    return true;
}
