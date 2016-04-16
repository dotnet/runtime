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
    dotnet_packages_cache(_X("")),
    core_servicing(_X("")),
    deps_path(_X(""))
{
}

bool parse_arguments(
    const pal::string_t& deps_path,
    const std::vector<pal::string_t>& probe_paths,
    host_mode_t mode,
    const int argc, const pal::char_t* argv[], arguments_t* arg_out)
{
    arguments_t& args = *arg_out;
    // Get the full name of the application
    if (!pal::get_own_executable_path(&args.own_path) || !pal::realpath(&args.own_path))
    {
        trace::error(_X("Failed to locate current executable"));
        return false;
    }

    auto own_name = get_filename(args.own_path);
    auto own_dir = get_directory(args.own_path);
    
    if (mode != host_mode_t::standalone)
    {
        // corerun mode. First argument is managed app
        if (argc < 2)
        {
            return false;
        }
        args.managed_application = pal::string_t(argv[1]);
        if (!pal::realpath(&args.managed_application))
        {
            trace::error(_X("Failed to locate managed application: %s"), args.managed_application.c_str());
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
        managed_app.push_back(DIR_SEPARATOR);
        managed_app.append(get_executable(own_name));
        managed_app.append(_X(".dll"));
        args.managed_application = managed_app;
        if (!pal::realpath(&args.managed_application))
        {
            trace::error(_X("Failed to locate managed application: %s"), args.managed_application.c_str());
            return false;
        }
        args.app_dir = own_dir;
        args.app_argv = &argv[1];
        args.app_argc = argc - 1;
    }

    if (!deps_path.empty())
    {
        args.deps_path = deps_path;
        args.app_dir = get_directory(args.deps_path);
    }

    for (const auto& probe : probe_paths)
    {
        args.probe_paths.push_back(probe);
    }
    
    if (args.deps_path.empty())
    {
        const auto& app_base = args.app_dir;
        auto app_name = get_filename(args.managed_application);

        args.deps_path.reserve(app_base.length() + 1 + app_name.length() + 5);
        args.deps_path.append(app_base);
        args.deps_path.push_back(DIR_SEPARATOR);
        args.deps_path.append(app_name, 0, app_name.find_last_of(_X(".")));
        args.deps_path.append(_X(".deps.json"));
    }

    pal::getenv(_X("DOTNET_HOSTING_OPTIMIZATION_CACHE"), &args.dotnet_packages_cache);
    pal::get_default_servicing_directory(&args.core_servicing);

    return true;
}
