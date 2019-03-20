// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal.h"
#include "fxr_resolver.h"
#include "error_codes.h"
#include "fx_ver.h"
#include "trace.h"
#include "utils.h"

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
}

bool resolve_fxr_path(const pal::string_t& root_path, pal::string_t* out_dotnet_root, pal::string_t* out_fxr_path)
{
    pal::string_t fxr_dir;
#if FEATURE_APPHOST || FEATURE_LIBHOST
    // If a hostfxr exists in app_root, then assume self-contained.
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
        if (pal::get_dotnet_self_registered_dir(&default_install_location) || pal::get_default_installation_dir(&default_install_location))
        {
            trace::info(_X("Using global installation location [%s] as runtime location."), default_install_location.c_str());
            out_dotnet_root->assign(default_install_location);
        }
        else
        {
            trace::error(_X("A fatal error occurred, the default install location cannot be obtained."));
            return false;
        }
        trace::info(_X("Using default installation location [%s] as runtime location."), default_install_location.c_str());
        out_dotnet_root->assign(default_install_location);
    }

    fxr_dir = *out_dotnet_root;
    append_path(&fxr_dir, _X("host"));
    append_path(&fxr_dir, _X("fxr"));
    if (!pal::directory_exists(fxr_dir))
    {
        if (default_install_location.empty())
        {
            pal::get_dotnet_self_registered_dir(&default_install_location);
        }
        if (default_install_location.empty())
        {
            pal::get_default_installation_dir(&default_install_location);
        }

        trace::error(_X("A fatal error occurred. The required library %s could not be found.\n"
            "If this is a self-contained application, that library should exist in [%s].\n"
            "If this is a framework-dependent application, install the runtime in the global location [%s] or use the %s environment variable to specify the runtime location."),
            LIBFXR_NAME,
            root_path.c_str(),
            default_install_location.c_str(),
            dotnet_root_env_var_name.c_str());
        return false;
    }
#else // !FEATURE_APPHOST && !FEATURE_LIBHOST
    pal::string_t host_dir;
    host_dir.assign(get_directory(root_path));

    out_dotnet_root->assign(host_dir);

    fxr_dir = *out_dotnet_root;
    append_path(&fxr_dir, _X("host"));
    append_path(&fxr_dir, _X("fxr"));
    if (!pal::directory_exists(fxr_dir))
    {
        trace::error(_X("A fatal error occurred. The folder [%s] does not exist"), fxr_dir.c_str());
        return false;
    }
#endif // !FEATURE_APPHOST && !FEATURE_LIBHOST

    if (!get_latest_fxr(std::move(fxr_dir), out_fxr_path))
        return false;

    return true;
}
