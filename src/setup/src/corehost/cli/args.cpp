// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "args.h"
#include "utils.h"
#include "coreclr.h"
#include "libhost.h"

arguments_t::arguments_t() :
    managed_application(_X("")),
    own_path(_X("")),
    app_dir(_X("")),
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

    // Try to use argv[0] as own_path to allow for hosts located elsewhere
    if (argc >= 1)
    {
        args.own_path = argv[0];
        if (!args.own_path.empty() && (!pal::realpath(&args.own_path) || !pal::file_exists(args.own_path)))
        {
            trace::warning(_X("Failed to resolve argv[0] as path [%s]. Using location of current executable instead."), args.own_path.c_str());
            args.own_path.clear();
        }
    }

    // Get the full name of the application
    if (args.own_path.empty() && (!pal::get_own_executable_path(&args.own_path) || !pal::realpath(&args.own_path)))
    {
        trace::error(_X("Failed to resolve full path of the current executable [%s]"), args.own_path.c_str());
        return false;
    }

    auto own_name = get_filename(args.own_path);
    auto own_dir = get_directory(args.own_path);
    
    if (init.host_mode != host_mode_t::standalone)
    {
        // corerun mode. First argument is managed app
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
        args.app_dir = get_directory(args.managed_application);
        args.app_argc = argc - 2;
        args.app_argv = &argv[2];
    }
    else
    {
        // coreconsole mode. Find the managed app in the same directory
        pal::string_t managed_app(own_dir);

        managed_app.append(get_executable(own_name));
        managed_app.append(_X(".dll"));
        args.managed_application = managed_app;
        if (!pal::realpath(&args.managed_application))
        {
            trace::error(_X("Failed to locate managed application [%s]"), args.managed_application.c_str());
            return false;
        }
        args.app_dir = own_dir;
        args.app_argv = &argv[1];
        args.app_argc = argc - 1;
    }

    if (!init.deps_file.empty())
    {
        args.deps_path = init.deps_file;
        args.app_dir = get_directory(args.deps_path);
    }

    for (const auto& probe : init.probe_paths)
    {
        args.probe_paths.push_back(probe);
    }
    
    if (args.deps_path.empty())
    {
        const auto& app_base = args.app_dir;
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

    setup_shared_store_paths(init, own_dir, &args);

    return true;
}
