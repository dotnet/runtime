// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "args.h"
#include <utils.h>

arguments_t::arguments_t()
    : host_mode(host_mode_t::invalid)
    , host_path(_X(""))
    , app_root(_X(""))
    , deps_path(_X(""))
    , core_servicing(_X(""))
    , managed_application(_X(""))
    , app_argc(0)
    , app_argv(nullptr)
{
}

/**
 *
 * Setup the shared store directories.
 *
 *  o %DOTNET_SHARED_STORE% -- multiple delimited paths
 *  o dotnet.exe relative shared store\<arch>\<tfm>
 *  o Global location
 *      Windows: global default location (Program Files) or globally registered location (registry) + store\<arch>\<tfm>
 *      Linux/macOS: none (no global locations are considered)
 */
void setup_shared_store_paths(const pal::string_t& tfm, host_mode_t host_mode,const pal::string_t& own_dir, arguments_t* args)
{
    if (tfm.empty())
    {
        // Old (MNA < 1.1.*) "runtimeconfig.json" files do not contain TFM property.
        return;
    }

    // Environment variable DOTNET_SHARED_STORE
    (void) get_env_shared_store_dirs(&args->env_shared_store, get_arch(), tfm);

    // "dotnet.exe" relative shared store folder
    if (host_mode == host_mode_t::muxer)
    {
        args->dotnet_shared_store = own_dir;
        append_path(&args->dotnet_shared_store, RUNTIME_STORE_DIRECTORY_NAME);
        append_path(&args->dotnet_shared_store, get_arch());
        append_path(&args->dotnet_shared_store, tfm.c_str());
    }

    // Global shared store dir
    bool multilevel_lookup = multilevel_lookup_enabled();
    if (multilevel_lookup)
    {
        get_global_shared_store_dirs(&args->global_shared_stores, get_arch(), tfm);
    }
}

bool parse_arguments(
    const hostpolicy_init_t& init,
    const int argc, const pal::char_t* argv[],
    arguments_t& args)
{
    pal::string_t managed_application_path;
    if (init.host_mode == host_mode_t::apphost)
    {
        // Find the managed app in the same directory
        managed_application_path = init.host_info.app_path;

        args.app_argv = &argv[1];
        args.app_argc = argc - 1;
    }
    else if (init.host_mode == host_mode_t::libhost)
    {
        // Find the managed assembly in the same directory
        managed_application_path = init.host_info.app_path;

        assert(argc == 0 && argv == nullptr);
    }
    else
    {
        // First argument is managed app
        if (argc < 2)
        {
            return false;
        }

        managed_application_path = pal::string_t(argv[1]);

        args.app_argc = argc - 2;
        args.app_argv = &argv[2];
    }

    return init_arguments(
        managed_application_path,
        init.host_info,
        init.tfm,
        init.host_mode,
        init.additional_deps_serialized,
        init.deps_file,
        init.probe_paths,
        args);
}

bool init_arguments(
    const pal::string_t& managed_application_path,
    const host_startup_info_t& host_info,
    const pal::string_t& tfm,
    host_mode_t host_mode,
    const pal::string_t& additional_deps_serialized,
    const pal::string_t& deps_file,
    const std::vector<pal::string_t>& probe_paths,
    arguments_t& args)
{
    args.host_mode = host_mode;
    args.host_path = host_info.host_path;
    args.additional_deps_serialized = additional_deps_serialized;

    args.managed_application = managed_application_path;
    if (!args.managed_application.empty() && !pal::realpath(&args.managed_application))
    {
        trace::error(_X("Failed to locate managed application [%s]"), args.managed_application.c_str());
        return false;
    }
    args.app_root = get_directory(args.managed_application);

    if (!deps_file.empty())
    {
        args.deps_path = deps_file;
        args.app_root = get_directory(args.deps_path);
    }

    for (const auto& probe : probe_paths)
    {
        args.probe_paths.push_back(probe);
    }

    if (args.deps_path.empty())
    {
        args.deps_path = get_deps_from_app_binary(args.app_root, args.managed_application);
    }

    pal::get_default_servicing_directory(&args.core_servicing);

    setup_shared_store_paths(tfm, host_mode, get_directory(args.host_path), &args);

    return true;
}
