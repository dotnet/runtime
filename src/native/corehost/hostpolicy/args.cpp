// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "args.h"
#include <utils.h>
#include "bundle/runner.h"

arguments_t::arguments_t()
    : host_mode(host_mode_t::invalid)
    , app_root(_X(""))
    , deps_path(_X(""))
    , managed_application(_X(""))
    , app_argc(0)
    , app_argv(nullptr)
{
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

    bool success = init_arguments(
        managed_application_path,
        init.host_mode,
        init.deps_file,
        /* init_from_file_system */ false,
        args);
    if (success)
        args.trace();

    return success;
}

bool set_root_from_app(const pal::string_t& managed_application_path,
                       bool file_system_lookup_only,
                       arguments_t& args)
{
    args.managed_application = managed_application_path;

    if (args.managed_application.empty())
    {
        // Managed app being empty by itself is not a failure. Host may be initialized from a config file.
        assert(args.host_mode != host_mode_t::apphost);
        return true;
    }

    if (!file_system_lookup_only && bundle::info_t::is_single_file_bundle())
    {
        const bundle::runner_t* app = bundle::runner_t::app();
        args.app_root = app->base_path();

        // Check for the main app within the bundle.
        // locate() sets args.managed_application to the full path of the app extracted to disk.
        pal::string_t managed_application_name = get_filename(managed_application_path);
        if (app->locate(managed_application_name, args.managed_application))
        {
            return true;
        }

        trace::info(_X("Managed application [%s] not found in single-file bundle"), managed_application_name.c_str());

        // The locate call above will clear the string when it returns false so reinitialize to the specified
        // path before continuing.
        args.managed_application = managed_application_path;

        // If the main assembly is not found in the bundle, continue checking on disk
        // for very unlikely case where the main app.dll was itself excluded from the app bundle.
        // Note that unlike non-single-file we don't want to set the app_root to the location of the app.dll
        // it needs to stay the location of the single-file bundle.
        return pal::fullpath(&args.managed_application);
    }

    if (pal::fullpath(&args.managed_application))
    {
        args.app_root = get_directory(args.managed_application);
        return true;
    }

    return false;
}

bool init_arguments(
    const pal::string_t& managed_application_path,
    host_mode_t host_mode,
    const pal::string_t& deps_file,
    bool init_from_file_system,
    arguments_t& args)
{
    args.host_mode = host_mode;

    // Components are never loaded from the bundle, the managed_application_path always means a file system path for a component case.
    if (!set_root_from_app(managed_application_path, /* file_system_lookup_only */ init_from_file_system, args))
    {
        trace::error(_X("Failed to locate managed application [%s]"), args.managed_application.c_str());
        return false;
    }

    if (!deps_file.empty())
    {
        args.deps_path = deps_file;
        args.app_root = get_directory(args.deps_path);
    }

    if (args.deps_path.empty())
    {
        args.deps_path = get_deps_from_app_binary(args.app_root, args.managed_application);
    }

    return true;
}
