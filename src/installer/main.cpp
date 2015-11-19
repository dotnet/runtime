// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal.h"
#include "args.h"
#include "trace.h"
#include "tpafile.h"
#include "utils.h"
#include "coreclr.h"

void get_tpafile_path(const pal::string_t& app_base, const pal::string_t& app_name, pal::string_t& tpapath)
{
    tpapath.reserve(app_base.length() + app_name.length() + 5);

    tpapath.append(app_base);
    tpapath.push_back(DIR_SEPARATOR);

    // Remove the extension from the app_name
    auto ext_location = app_name.find_last_of('.');
    if (ext_location != std::string::npos)
    {
        tpapath.append(app_name.substr(0, ext_location));
    }
    else
    {
        tpapath.append(app_name);
    }
    tpapath.append(_X(".deps"));
}

int run(arguments_t args, pal::string_t app_base, tpafile tpa)
{
    tpa.add_from_local_dir(app_base);

    // Add packages directory
    pal::string_t packages_dir;
    if (!pal::get_default_packages_directory(packages_dir))
    {
        trace::info(_X("did not find local packages directory"));

        // We can continue, the app may have it's dependencies locally
    }
    else
    {
        trace::info(_X("using packages directory: %s"), packages_dir.c_str());
        tpa.add_package_dir(packages_dir);
    }

    // Add native search path
    trace::info(_X("using native search path: %s"), packages_dir.c_str());
    tpa.add_native_search_path(args.clr_path);

    // Build TPA list and search paths
    pal::string_t tpalist;
    tpa.write_tpa_list(tpalist);

    pal::string_t search_paths;
    tpa.write_native_paths(search_paths);

    // Build CoreCLR properties
    const char* property_keys[] = {
        "TRUSTED_PLATFORM_ASSEMBLIES",
        "APP_PATHS",
        "APP_NI_PATHS",
        "NATIVE_DLL_SEARCH_DIRECTORIES",
        "AppDomainCompatSwitch"
    };

    auto tpa_cstr = pal::to_stdstring(tpalist);
    auto app_base_cstr = pal::to_stdstring(app_base);
    auto search_paths_cstr = pal::to_stdstring(search_paths);

    const char* property_values[] = {
        // TRUSTED_PLATFORM_ASSEMBLIES
        tpa_cstr.c_str(),
        // APP_PATHS
        app_base_cstr.c_str(),
        // APP_NI_PATHS
        app_base_cstr.c_str(),
        // NATIVE_DLL_SEARCH_DIRECTORIES
        search_paths_cstr.c_str(),
        // AppDomainCompatSwitch
        "UseLatestBehaviorWhenTFMNotSpecified"
    };

    // Dump TPA list
    trace::verbose(_X("TPA List: %s"), tpalist.c_str());

    // Dump native search paths
    trace::verbose(_X("Native Paths: %s"), search_paths.c_str());

    // Bind CoreCLR
    if (!coreclr::bind(args.clr_path))
    {
        trace::error(_X("failed to bind to coreclr"));
        return 1;
    }

    // Initialize CoreCLR
    coreclr::host_handle_t host_handle;
    coreclr::domain_id_t domain_id;
    auto hr = coreclr::initialize(
        pal::to_stdstring(args.own_path).c_str(),
        "clrhost",
        property_keys,
        property_values,
        sizeof(property_keys) / sizeof(property_keys[0]),
        &host_handle,
        &domain_id);
    if (!SUCCEEDED(hr))
    {
        trace::error(_X("failed to initialize CoreCLR, HRESULT: 0x%X"), hr);
        return 1;
    }

    // Convert the args (probably not the most performant way to do this...)
    auto argv_strs = new std::string[args.app_argc];
    auto argv = new const char*[args.app_argc];
    for (int i = 0; i < args.app_argc; i++)
    {
        argv_strs[i] = pal::to_stdstring(pal::string_t(args.app_argv[i]));
        argv[i] = argv_strs[i].c_str();
    }

    // Execute the application
    unsigned int exit_code = 1;
    hr = coreclr::execute_assembly(
        host_handle,
        domain_id,
        args.app_argc,
        argv,
        pal::to_stdstring(args.managed_application).c_str(),
        &exit_code);
    if (!SUCCEEDED(hr))
    {
        trace::error(_X("failed to execute managed app, HRESULT: 0x%X"), hr);
        return 1;
    }

    // Shut down the CoreCLR
    hr = coreclr::shutdown(host_handle, domain_id);
    if (!SUCCEEDED(hr))
    {
        trace::warning(_X("failed to shut down CoreCLR, HRESULT: 0x%X"), hr);
    }

    coreclr::unload();

    return exit_code;
}

#if defined(_WIN32)
int __cdecl wmain(const int argc, const pal::char_t* argv[])
#else
int main(const int argc, const pal::char_t* argv[])
#endif
{
    arguments_t args;
    if (!parse_arguments(argc, argv, args))
    {
        return 1;
    }

    // Resolve paths
    if (!pal::realpath(args.managed_application))
    {
        trace::error(_X("failed to locate managed application: %s"), args.managed_application.c_str());
        return 1;
    }
    trace::info(_X("preparing to launch managed application: %s"), args.managed_application.c_str());
    trace::info(_X("host path: %s"), args.own_path.c_str());

    pal::string_t argstr;
    for (int i = 0; i < args.app_argc; i++)
    {
        argstr.append(args.app_argv[i]);
        argstr.append(_X(","));
    }
    trace::info(_X("App argc: %d"), args.app_argc);
    trace::info(_X("App argv: %s"), argstr.c_str());

    auto app_base = get_directory(args.managed_application);
    auto app_name = get_filename(args.managed_application);

    // App-local coreclr wins
    {
        pal::string_t candidate;
        candidate.assign(app_base);
        append_path(candidate, LIBCORECLR_NAME);

        if (pal::file_exists(candidate))
        {
            args.clr_path.assign(app_base);
        }
    }

    if (args.clr_path.empty())
    {
        trace::error(_X("failed to locate CLR files, set the DOTNET_HOME environment variable"));
        return 1;
    }

    if (!pal::realpath(args.clr_path))
    {
        trace::error(_X("failed to locate CLR files at %s"), args.clr_path.c_str());
        return 1;
    }

    trace::info(_X("using CLR files from: %s"), args.clr_path.c_str());
    trace::info(_X("preparing to launch: %s"), app_name.c_str());
    trace::info(_X("using app base: %s"), app_base.c_str());

    // Check for and load deps file
    pal::string_t tpafile_path;
    get_tpafile_path(app_base, app_name, tpafile_path);
    trace::info(_X("checking for .deps File at: %s"), tpafile_path.c_str());
    tpafile tpa;
    if (!tpa.load(tpafile_path))
    {
        trace::error(_X("invalid .deps file"));
        return 1;
    }
    return run(args, app_base, tpa);
}
