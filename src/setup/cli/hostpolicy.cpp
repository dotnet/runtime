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
    deps_resolver_t resolver(init, config, args);

    if (!resolver.valid())
    {
        trace::error(_X("Invalid .deps file"));
        return StatusCode::ResolverInitFailure;
    }

    pal::string_t clr_path = resolver.resolve_coreclr_dir();
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
    if (!resolver.resolve_probe_paths(clr_path, &probe_paths))
    {
        return StatusCode::ResolverResolveFailure;
    }

    // Build CoreCLR properties
    std::vector<const char*> property_keys = {
        "TRUSTED_PLATFORM_ASSEMBLIES",
        "APP_PATHS",
        "APP_NI_PATHS",
        "NATIVE_DLL_SEARCH_DIRECTORIES",
        "PLATFORM_RESOURCE_ROOTS",
        "AppDomainCompatSwitch",
        // Workaround: mscorlib does not resolve symlinks for AppContext.BaseDirectory dotnet/coreclr/issues/2128
        "APP_CONTEXT_BASE_DIRECTORY",
        "APP_CONTEXT_DEPS_FILES"
    };

    auto tpa_paths_cstr = pal::to_stdstring(probe_paths.tpa);
    auto app_base_cstr = pal::to_stdstring(args.app_dir);
    auto native_dirs_cstr = pal::to_stdstring(probe_paths.native);
    auto resources_dirs_cstr = pal::to_stdstring(probe_paths.resources);

    std::string deps = pal::to_stdstring(resolver.get_deps_file() + _X(";") + resolver.get_fx_deps_file());

    std::vector<const char*> property_values = {
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
        // APP_CONTEXT_BASE_DIRECTORY
        app_base_cstr.c_str(),
        // APP_CONTEXT_DEPS_FILES,
        deps.c_str(),
    };

    
    std::vector<std::string> cfg_keys;
    std::vector<std::string> cfg_values;
    config.config_kv(&cfg_keys, &cfg_values);

    for (int i = 0; i < cfg_keys.size(); ++i)
    {
        property_keys.push_back(cfg_keys[i].c_str());
        property_values.push_back(cfg_values[i].c_str());
    }

    size_t property_size = property_keys.size();
    assert(property_keys.size() == property_values.size());

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
        property_keys.data(),
        property_values.data(),
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
    if (g_init->version() != corehost_init_t::s_version)
    {
        trace::error(_X("The structure of init data has changed, do not know how to interpret it"));
        return StatusCode::LibHostInitFailure;
    }
    return 0;
}

SHARED_API int corehost_main(const int argc, const pal::char_t* argv[])
{
    trace::setup();

    assert(g_init);

    if (trace::is_enabled())
    {
        trace::info(_X("--- Invoked policy [%s/%s/%s] main = {"),
            _STRINGIFY(HOST_POLICY_PKG_NAME),
            _STRINGIFY(HOST_POLICY_PKG_VER),
            _STRINGIFY(HOST_POLICY_PKG_REL_DIR));

        for (int i = 0; i < argc; ++i)
        {
            trace::info(_X("%s"), argv[i]);
        }
        trace::info(_X("}"));

        trace::info(_X("Host mode: %d"), g_init->host_mode());
        trace::info(_X("Deps file: %s"), g_init->deps_file().c_str());
        for (const auto& probe : g_init->probe_paths())
        {
            trace::info(_X("Additional probe dir: %s"), probe.c_str());
        }
    }

    // Take care of arguments
    arguments_t args;
    if (!parse_arguments(g_init->deps_file(), g_init->probe_paths(), g_init->host_mode(), argc, argv, &args))
    {
        return StatusCode::LibHostInvalidArgs;
    }
    if (trace::is_enabled())
    {
        args.print();
    }

    if (g_init->runtime_config())
    {
        return run(g_init, *g_init->runtime_config(), args);
    }
    else
    {
        pal::string_t dev_config_file;
        auto config_path = get_runtime_config_from_file(args.managed_application, &dev_config_file);
        runtime_config_t config(config_path, dev_config_file);
        if (!config.is_valid())
        {
            trace::error(_X("Invalid runtimeconfig.json [%s] [%s]"), config.get_path().c_str(), config.get_dev_path().c_str());
            return StatusCode::InvalidConfigFile;
        }
        // TODO: This is ugly. The whole runtime config/probe paths business should all be resolved by and come from the hostfxr.cpp.
        args.probe_paths.insert(args.probe_paths.end(), config.get_probe_paths().begin(), config.get_probe_paths().end());
        return run(g_init, config, args);
    }
}

SHARED_API int corehost_unload()
{
    g_init = nullptr;
    return 0;
}
