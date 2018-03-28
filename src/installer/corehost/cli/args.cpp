// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "args.h"
#include "utils.h"
#include "coreclr.h"
#include "libhost.h"

arguments_t::arguments_t() :
    managed_application(_X("")),
    host_path(_X("")),
    app_root(_X("")),
    app_argc(0),
    app_argv(nullptr),
    core_servicing(_X("")),
    deps_path(_X(""))
{
}

/**
 *
 * Setup the shared store directories.
 *
 *  o %DOTNET_SHARED_STORE% -- multiple delimited paths
 *  o $HOME/.dotnet/{x86|x64}/store/arch/tfm or %USERPROFILE%\.dotnet\{x86|x64}\store\<arch>\<tfm>
 *  o dotnet.exe relative shared store\<arch>\<tfm>
 *  o Global location
 *      Windows: C:\Program Files (x86) or
 *      Unix: directory of dotnet on the path.\<arch>\<tfm>
 */
void setup_shared_store_paths(const hostpolicy_init_t& init, const pal::string_t& own_dir, arguments_t* args)
{
    if (init.tfm.empty())
    {
        // Old (MNA < 1.1.*) "runtimeconfig.json" files do not contain TFM property.
        return;
    }

    // Environment variable DOTNET_SHARED_STORE
    (void) get_env_shared_store_dirs(&args->env_shared_store, get_arch(), init.tfm);

    // "dotnet.exe" relative shared store folder
    if (init.host_mode == host_mode_t::muxer)
    {
        args->dotnet_shared_store = own_dir;
        append_path(&args->dotnet_shared_store, RUNTIME_STORE_DIRECTORY_NAME);
        append_path(&args->dotnet_shared_store, get_arch());
        append_path(&args->dotnet_shared_store, init.tfm.c_str());
    }

    // Global shared store dir
    bool multilevel_lookup = multilevel_lookup_enabled();
    if (multilevel_lookup)
    {
        get_global_shared_store_dirs(&args->global_shared_stores, get_arch(), init.tfm);
    }
}

bool parse_arguments(
    const hostpolicy_init_t& init,
    const int argc, const pal::char_t* argv[], arguments_t* arg_out)
{
    arguments_t& args = *arg_out;

    args.host_path = init.host_info.host_path;

    if (init.host_mode != host_mode_t::apphost)
    {
        // First argument is managed app
        if (argc < 2)
        {
            return false;
        }
        args.managed_application = pal::string_t(argv[1]);
        if (!pal::realpath(&args.managed_application))
        {
            trace::error(_X("Failed to locate managed application [%s]"), args.managed_application.c_str());
            return false;
        }
        args.app_root = get_directory(args.managed_application);
        args.app_argc = argc - 2;
        args.app_argv = &argv[2];
    }
    else
    {
        // Find the managed app in the same directory
        args.managed_application = init.host_info.app_path;
        if (!pal::realpath(&args.managed_application))
        {
            trace::error(_X("Failed to locate managed application [%s]"), args.managed_application.c_str());
            return false;
        }
        args.app_root = get_directory(init.host_info.app_path);
        args.app_argv = &argv[1];
        args.app_argc = argc - 1;
    }

    if (!init.deps_file.empty())
    {
        args.deps_path = init.deps_file;
        args.app_root = get_directory(args.deps_path);
    }

    for (const auto& probe : init.probe_paths)
    {
        args.probe_paths.push_back(probe);
    }
    
    if (args.deps_path.empty())
    {
        const auto& app_base = args.app_root;
        auto app_name = get_filename(args.managed_application);

        args.deps_path.reserve(app_base.length() + 1 + app_name.length() + 5);
        args.deps_path.append(app_base);

        if (!app_base.empty() && app_base.back() != DIR_SEPARATOR)
        {
            args.deps_path.push_back(DIR_SEPARATOR);
        }
        args.deps_path.append(app_name, 0, app_name.find_last_of(_X(".")));
        args.deps_path.append(_X(".deps.json"));
    }

    pal::get_default_servicing_directory(&args.core_servicing);

    setup_shared_store_paths(init, get_directory(args.host_path), &args);

    return true;
}
