// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "args.h"
#include "utils.h"
#include "coreclr.h"

arguments_t::arguments_t() :
    managed_application(_X("")),
    own_path(_X("")),
    app_dir(_X("")),
    app_argc(0),
    app_argv(nullptr)
{
}

void display_help()
{
    xerr <<
        _X("Usage: " HOST_EXE_NAME " [ASSEMBLY] [ARGUMENTS]\n")
        _X("Execute the specified managed assembly with the passed in arguments\n\n")
        _X("The Host's behavior can be altered using the following environment variables:\n")
        _X(" DOTNET_HOME            Set the dotnet home directory. The CLR is expected to be in the runtime subdirectory of this directory. Overrides all other values for CLR search paths\n")
        _X(" COREHOST_TRACE          Set to affect trace levels (0 = Errors only (default), 1 = Warnings, 2 = Info, 3 = Verbose)\n");
}

bool parse_arguments(const int argc, const pal::char_t* argv[], arguments_t& args)
{
    // Get the full name of the application
    if (!pal::get_own_executable_path(&args.own_path) || !pal::realpath(&args.own_path))
    {
        trace::error(_X("Failed to locate current executable"));
        return false;
    }

    auto own_name = get_filename(args.own_path);
    auto own_dir = get_directory(args.own_path);

    if (own_name.compare(HOST_EXE_NAME) == 0)
    {
        // corerun mode. First argument is managed app
        if (argc < 2)
        {
            display_help();
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

    pal::getenv(_X("DOTNET_PACKAGES"), &args.dotnet_packages);
    pal::getenv(_X("DOTNET_PACKAGES_CACHE"), &args.dotnet_packages_cache);
    pal::getenv(_X("DOTNET_SERVICING"), &args.dotnet_servicing);
    pal::getenv(_X("DOTNET_RUNTIME_SERVICING"), &args.dotnet_runtime_servicing);
    pal::getenv(_X("DOTNET_HOME"), &args.dotnet_home);
    return true;
}
