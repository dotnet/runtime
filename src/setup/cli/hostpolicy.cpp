// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal.h"
#include "args.h"
#include "trace.h"
#include "deps_resolver.h"
#include "fx_muxer.h"
#include "utils.h"
#include "coreclr.h"
#include "cpprest/json.h"
#include "libhost.h"
#include "error_codes.h"


corehost_init_t* g_init = nullptr;

int run(const corehost_init_t* init, const runtime_config_t& config, const arguments_t& args)
{
    // Load the deps resolver
    deps_resolver_t resolver(init->fx_dir(), &config, args);

    if (!resolver.valid())
    {
        trace::error(_X("Invalid .deps file"));
        return StatusCode::ResolverInitFailure;
    }

    // Add packages directory
    pal::string_t packages_dir = init->probe_dir();
    if (packages_dir.empty() || !pal::directory_exists(packages_dir))
    {
        (void)pal::get_default_packages_directory(&packages_dir);
    }
    trace::info(_X("Package directory: %s"), packages_dir.empty() ? _X("not specified") : packages_dir.c_str());

    pal::string_t clr_path = resolver.resolve_coreclr_dir(args.app_dir, packages_dir, args.dotnet_packages_cache);
    if (clr_path.empty() || !pal::realpath(&clr_path))
    {
        trace::error(_X("Could not resolve coreclr path"));
        return StatusCode::CoreClrResolveFailure;
    }
    else
    {
        trace::info(_X("CoreCLR directory: %s"), clr_path.c_str());
    }

    probe_paths_t probe_paths;
    if (!resolver.resolve_probe_paths(args.app_dir, packages_dir, args.dotnet_packages_cache, clr_path, &probe_paths))
    {
        return StatusCode::ResolverResolveFailure;
    }

    // TODO: config.get_runtime_properties();

    // Build CoreCLR properties
    const char* property_keys[] = {
        "TRUSTED_PLATFORM_ASSEMBLIES",
        "APP_PATHS",
        "APP_NI_PATHS",
        "NATIVE_DLL_SEARCH_DIRECTORIES",
        "PLATFORM_RESOURCE_ROOTS",
        "AppDomainCompatSwitch",
        "SERVER_GC",
        // Workaround: mscorlib does not resolve symlinks for AppContext.BaseDirectory dotnet/coreclr/issues/2128
        "APP_CONTEXT_BASE_DIRECTORY",
        "APP_CONTEXT_DEPS_FILES"
    };

    auto tpa_paths_cstr = pal::to_stdstring(probe_paths.tpa);
    auto app_base_cstr = pal::to_stdstring(args.app_dir);
    auto native_dirs_cstr = pal::to_stdstring(probe_paths.native);
    auto resources_dirs_cstr = pal::to_stdstring(probe_paths.resources);

    // Workaround for dotnet/cli Issue #488 and #652
    pal::string_t server_gc;
    std::string server_gc_cstr = (pal::getenv(_X("COREHOST_SERVER_GC"), &server_gc) && !server_gc.empty()) ? pal::to_stdstring(server_gc) : "0";
    
    std::string deps = pal::to_stdstring(resolver.get_deps_file() + _X(";") + resolver.get_fx_deps_file());

    const char* property_values[] = {
        // TRUSTED_PLATFORM_ASSEMBLIES
        tpa_paths_cstr.c_str(),
        // APP_PATHS
        app_base_cstr.c_str(),
        // APP_NI_PATHS
        app_base_cstr.c_str(),
        // NATIVE_DLL_SEARCH_DIRECTORIES
        native_dirs_cstr.c_str(),
        // PLATFORM_RESOURCE_ROOTS
        resources_dirs_cstr.c_str(),
        // AppDomainCompatSwitch
        "UseLatestBehaviorWhenTFMNotSpecified",
        // SERVER_GC
        server_gc_cstr.c_str(),
        // APP_CONTEXT_BASE_DIRECTORY
        app_base_cstr.c_str(),
        // APP_CONTEXT_DEPS_FILES,
        deps.c_str(),
    };

    size_t property_size = sizeof(property_keys) / sizeof(property_keys[0]);

    // Bind CoreCLR
    if (!coreclr::bind(clr_path))
    {
        trace::error(_X("Failed to bind to coreclr"));
        return StatusCode::CoreClrBindFailure;
    }

    // Verbose logging
    if (trace::is_enabled())
    {
        for (size_t i = 0; i < property_size; ++i)
        {
            pal::string_t key, val;
            pal::to_palstring(property_keys[i], &key);
            pal::to_palstring(property_values[i], &val);
            trace::verbose(_X("Property %s = %s"), key.c_str(), val.c_str());
        }
    }

    std::string own_path;
    pal::to_stdstring(args.own_path.c_str(), &own_path);

    // Initialize CoreCLR
    coreclr::host_handle_t host_handle;
    coreclr::domain_id_t domain_id;
    auto hr = coreclr::initialize(
        own_path.c_str(),
        "clrhost",
        property_keys,
        property_values,
        property_size,
        &host_handle,
        &domain_id);
    if (!SUCCEEDED(hr))
    {
        trace::error(_X("Failed to initialize CoreCLR, HRESULT: 0x%X"), hr);
        return StatusCode::CoreClrInitFailure;
    }

    if (trace::is_enabled())
    {
        pal::string_t arg_str;
        for (int i = 0; i < args.app_argc; i++)
        {
            arg_str.append(args.app_argv[i]);
            arg_str.append(_X(","));
        }
        trace::info(_X("Launch host: %s app: %s, argc: %d args: %s"), args.own_path.c_str(),
            args.managed_application.c_str(), args.app_argc, arg_str.c_str());
    }

    // Initialize with empty strings
    std::vector<std::string> argv_strs(args.app_argc);
    std::vector<const char*> argv(args.app_argc);
    for (int i = 0; i < args.app_argc; i++)
    {
        pal::to_stdstring(args.app_argv[i], &argv_strs[i]);
        argv[i] = argv_strs[i].c_str();
    }

    std::string managed_app = pal::to_stdstring(args.managed_application);

    // Execute the application
    unsigned int exit_code = 1;
    hr = coreclr::execute_assembly(
        host_handle,
        domain_id,
        argv.size(),
        argv.data(),
        managed_app.c_str(),
        &exit_code);
    if (!SUCCEEDED(hr))
    {
        trace::error(_X("Failed to execute managed app, HRESULT: 0x%X"), hr);
        return StatusCode::CoreClrExeFailure;
    }

    // Shut down the CoreCLR
    hr = coreclr::shutdown(host_handle, domain_id);
    if (!SUCCEEDED(hr))
    {
        trace::warning(_X("Failed to shut down CoreCLR, HRESULT: 0x%X"), hr);
    }

    coreclr::unload();

    return exit_code;
}

SHARED_API int corehost_load(corehost_init_t* init)
{
    g_init = init;
    return 0;
}

SHARED_API int corehost_main(const int argc, const pal::char_t* argv[])
{
    trace::setup();

    assert(g_init);

    // Take care of arguments
    arguments_t args;
    if (!parse_arguments(g_init->deps_file(), g_init->probe_dir(), g_init->host_mode(), argc, argv, &args))
    {
        return StatusCode::LibHostInvalidArgs;
    }

    if (g_init->runtime_config())
    {
        return run(g_init, *g_init->runtime_config(), args);
    }
    else
    {
        runtime_config_t config(get_runtime_config_json(args.managed_application));
        if (!config.is_valid())
        {
            trace::error(_X("Invalid runtimeconfig.json [%s]"), config.get_path().c_str());
            return StatusCode::InvalidConfigFile;
        }
        return run(g_init, config, args);
    }
}

SHARED_API int corehost_unload()
{
    g_init = nullptr;
    return 0;
}
