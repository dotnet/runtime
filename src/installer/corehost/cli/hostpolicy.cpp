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
#include "breadcrumbs.h"
#include "host_startup_info.h"

hostpolicy_init_t g_init;

int run(const arguments_t& args, pal::string_t* out_host_command_result = nullptr)
{
    // Load the deps resolver
    deps_resolver_t resolver(
        args,
        g_init.fx_definitions,
        /* root_framework_rid_fallback_graph */ nullptr, // This means that the fx_definitions contains the root framework
        g_init.is_framework_dependent);

    pal::string_t resolver_errors;
    if (!resolver.valid(&resolver_errors))
    {
        trace::error(_X("Error initializing the dependency resolver: %s"), resolver_errors.c_str());
        return StatusCode::ResolverInitFailure;
    }

    // Setup breadcrumbs. Breadcrumbs are not enabled for API calls because they do not execute
    // the app and may be re-entry
    probe_paths_t probe_paths;
    std::unordered_set<pal::string_t> breadcrumbs;
    bool breadcrumbs_enabled = (out_host_command_result == nullptr);
    if (breadcrumbs_enabled)
    {
        pal::string_t policy_name = _STRINGIFY(HOST_POLICY_PKG_NAME);
        pal::string_t policy_version = _STRINGIFY(HOST_POLICY_PKG_VER);

        // Always insert the hostpolicy that the code is running on.
        breadcrumbs.insert(policy_name);
        breadcrumbs.insert(policy_name + _X(",") + policy_version);

        if (!resolver.resolve_probe_paths(&probe_paths, &breadcrumbs))
        {
            return StatusCode::ResolverResolveFailure;
        }
    }
    else
    {
        if (!resolver.resolve_probe_paths(&probe_paths, nullptr))
        {
            return StatusCode::ResolverResolveFailure;
        }
    }

    pal::string_t clr_path = probe_paths.coreclr;
    if (clr_path.empty() || !pal::realpath(&clr_path))
    {
        trace::error(_X("Could not resolve CoreCLR path. For more details, enable tracing by setting COREHOST_TRACE environment variable to 1"));;
        return StatusCode::CoreClrResolveFailure;
    }

    // Get path in which CoreCLR is present.
    pal::string_t clr_dir = get_directory(clr_path);

    // System.Private.CoreLib.dll is expected to be next to CoreCLR.dll - add its path to the TPA list.
    pal::string_t corelib_path = clr_dir;
    append_path(&corelib_path, CORELIB_NAME);

    // Append CoreLib path
    if (probe_paths.tpa.back() != PATH_SEPARATOR)
    {
        probe_paths.tpa.push_back(PATH_SEPARATOR);
    }

    probe_paths.tpa.append(corelib_path);
    probe_paths.tpa.push_back(PATH_SEPARATOR);

    pal::string_t clrjit_path = probe_paths.clrjit;
    if (clrjit_path.empty())
    {
        trace::warning(_X("Could not resolve CLRJit path"));
    }
    else if (pal::realpath(&clrjit_path))
    {
        trace::verbose(_X("The resolved JIT path is '%s'"), clrjit_path.c_str());
    }
    else
    {
        clrjit_path.clear();
        trace::warning(_X("Could not resolve symlink to CLRJit path '%s'"), probe_paths.clrjit.c_str());
    }

    // Build CoreCLR properties
    std::vector<const char*> property_keys = {
        "TRUSTED_PLATFORM_ASSEMBLIES",
        "NATIVE_DLL_SEARCH_DIRECTORIES",
        "PLATFORM_RESOURCE_ROOTS",
        "AppDomainCompatSwitch",
        // Workaround: mscorlib does not resolve symlinks for AppContext.BaseDirectory dotnet/coreclr/issues/2128
        "APP_CONTEXT_BASE_DIRECTORY",
        "APP_CONTEXT_DEPS_FILES",
        "FX_DEPS_FILE",
        "PROBING_DIRECTORIES",
        "FX_PRODUCT_VERSION"
    };

    // Note: these variables' lifetime should be longer than coreclr_initialize.
    std::vector<char> tpa_paths_cstr, app_base_cstr, native_dirs_cstr, resources_dirs_cstr, fx_deps, deps, clrjit_path_cstr, probe_directories, clr_library_version, startup_hooks_cstr;
    pal::pal_clrstring(probe_paths.tpa, &tpa_paths_cstr);
    pal::pal_clrstring(args.app_root, &app_base_cstr);
    pal::pal_clrstring(probe_paths.native, &native_dirs_cstr);
    pal::pal_clrstring(probe_paths.resources, &resources_dirs_cstr);

    pal::string_t fx_deps_str;
    if (resolver.get_fx_definitions().size() >= 2)
    {
        // Use the root fx to define FX_DEPS_FILE
        fx_deps_str = get_root_framework(resolver.get_fx_definitions()).get_deps_file();
    }
    pal::pal_clrstring(fx_deps_str, &fx_deps);

    // Get all deps files
    pal::string_t allDeps;
    for (int i = 0; i < resolver.get_fx_definitions().size(); ++i)
    {
        allDeps += resolver.get_fx_definitions()[i]->get_deps_file();
        if (i < resolver.get_fx_definitions().size() - 1)
        {
            allDeps += _X(";");
        }
    }
    pal::pal_clrstring(allDeps, &deps);

    pal::pal_clrstring(resolver.get_lookup_probe_directories(), &probe_directories);

    if (resolver.is_framework_dependent())
    {
        pal::pal_clrstring(get_root_framework(resolver.get_fx_definitions()).get_found_version() , &clr_library_version);
    }
    else
    {
        pal::pal_clrstring(resolver.get_coreclr_library_version(), &clr_library_version);
    }

    std::vector<const char*> property_values = {
        // TRUSTED_PLATFORM_ASSEMBLIES
        tpa_paths_cstr.data(),
        // NATIVE_DLL_SEARCH_DIRECTORIES
        native_dirs_cstr.data(),
        // PLATFORM_RESOURCE_ROOTS
        resources_dirs_cstr.data(),
        // AppDomainCompatSwitch
        "UseLatestBehaviorWhenTFMNotSpecified",
        // APP_CONTEXT_BASE_DIRECTORY
        app_base_cstr.data(),
        // APP_CONTEXT_DEPS_FILES,
        deps.data(),
        // FX_DEPS_FILE
        fx_deps.data(),
        //PROBING_DIRECTORIES
        probe_directories.data(),
        //FX_PRODUCT_VERSION
        clr_library_version.data()
    };

    if (!clrjit_path.empty())
    {
        pal::pal_clrstring(clrjit_path, &clrjit_path_cstr);
        property_keys.push_back("JIT_PATH");
        property_values.push_back(clrjit_path_cstr.data());
    }

    bool set_app_paths = false;

    // Runtime options config properties.
    for (int i = 0; i < g_init.cfg_keys.size(); ++i)
    {
        // Provide opt-in compatible behavior by using the switch to set APP_PATHS
        if (pal::cstrcasecmp(g_init.cfg_keys[i].data(), "Microsoft.NETCore.DotNetHostPolicy.SetAppPaths") == 0)
        {
            set_app_paths = (pal::cstrcasecmp(g_init.cfg_values[i].data(), "true") == 0);
        }

        property_keys.push_back(g_init.cfg_keys[i].data());
        property_values.push_back(g_init.cfg_values[i].data());
    }

    // App paths and App NI paths
    if (set_app_paths)
    {
        property_keys.push_back("APP_PATHS");
        property_keys.push_back("APP_NI_PATHS");
        property_values.push_back(app_base_cstr.data());
        property_values.push_back(app_base_cstr.data());
    }

    // Startup hooks
    pal::string_t startup_hooks;
    if (pal::getenv(_X("DOTNET_STARTUP_HOOKS"), &startup_hooks))
    {
        pal::pal_clrstring(startup_hooks, &startup_hooks_cstr);
        property_keys.push_back("STARTUP_HOOKS");
        property_values.push_back(startup_hooks_cstr.data());
    }

    size_t property_size = property_keys.size();
    assert(property_keys.size() == property_values.size());

    unsigned int exit_code = 1;

    // Check for host command(s)
    if (pal::strcasecmp(g_init.host_command.c_str(), _X("get-native-search-directories")) == 0)
    {
        // Verify property_keys[1] contains the correct information
        if (pal::cstrcasecmp(property_keys[1], "NATIVE_DLL_SEARCH_DIRECTORIES"))
        {
            trace::error(_X("get-native-search-directories failed to find NATIVE_DLL_SEARCH_DIRECTORIES property"));
            exit_code = HostApiFailed;
        }
        else
        {
            assert(out_host_command_result != nullptr);
            pal::clr_palstring(property_values[1], out_host_command_result);
            exit_code = 0; // Success
        }

        return exit_code;
    }

    // Bind CoreCLR
    trace::verbose(_X("CoreCLR path = '%s', CoreCLR dir = '%s'"), clr_path.c_str(), clr_dir.c_str());
    if (!coreclr::bind(clr_dir))
    {
        trace::error(_X("Failed to bind to CoreCLR at '%s'"), clr_path.c_str());
        return StatusCode::CoreClrBindFailure;
    }

    // Verbose logging
    if (trace::is_enabled())
    {
        for (size_t i = 0; i < property_size; ++i)
        {
            pal::string_t key, val;
            pal::clr_palstring(property_keys[i], &key);
            pal::clr_palstring(property_values[i], &val);
            trace::verbose(_X("Property %s = %s"), key.c_str(), val.c_str());
        }
    }

    std::vector<char> host_path;
    pal::pal_clrstring(args.host_path, &host_path);

    // Initialize CoreCLR
    coreclr::host_handle_t host_handle;
    coreclr::domain_id_t domain_id;
    auto hr = coreclr::initialize(
        host_path.data(),
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

    // Initialize clr strings for arguments
    std::vector<std::vector<char>> argv_strs(args.app_argc);
    std::vector<const char*> argv(args.app_argc);
    for (int i = 0; i < args.app_argc; i++)
    {
        pal::pal_clrstring(args.app_argv[i], &argv_strs[i]);
        argv[i] = argv_strs[i].data();
    }

    if (trace::is_enabled())
    {
        pal::string_t arg_str;
        for (int i = 0; i < argv.size(); i++)
        {
            pal::string_t cur;
            pal::clr_palstring(argv[i], &cur);
            arg_str.append(cur);
            arg_str.append(_X(","));
        }
        trace::info(_X("Launch host: %s, app: %s, argc: %d, args: %s"), args.host_path.c_str(),
            args.managed_application.c_str(), args.app_argc, arg_str.c_str());
    }

    std::vector<char> managed_app;
    pal::pal_clrstring(args.managed_application, &managed_app);

    // Leave breadcrumbs for servicing.
    breadcrumb_writer_t writer(breadcrumbs_enabled, &breadcrumbs);
    writer.begin_write();

    // Previous hostpolicy trace messages must be printed before executing assembly
    trace::flush();

    // Execute the application
    hr = coreclr::execute_assembly(
        host_handle,
        domain_id,
        argv.size(),
        argv.data(),
        managed_app.data(),
        &exit_code);

    if (!SUCCEEDED(hr))
    {
        trace::error(_X("Failed to execute managed app, HRESULT: 0x%X"), hr);
        return StatusCode::CoreClrExeFailure;
    }

    // Shut down the CoreCLR
    hr = coreclr::shutdown(host_handle, domain_id, (int*)&exit_code);
    if (!SUCCEEDED(hr))
    {
        trace::warning(_X("Failed to shut down CoreCLR, HRESULT: 0x%X"), hr);
    }

    coreclr::unload();

    return exit_code;

    // The breadcrumb destructor will join to the background thread to finish writing
}

void trace_hostpolicy_entrypoint_invocation(const pal::string_t& entryPointName)
{
    trace::info(_X("--- Invoked hostpolicy [commit hash: %s] [%s,%s,%s][%s] %s = {"),
        _STRINGIFY(REPO_COMMIT_HASH),
        _STRINGIFY(HOST_POLICY_PKG_NAME),
        _STRINGIFY(HOST_POLICY_PKG_VER),
        _STRINGIFY(HOST_POLICY_PKG_REL_DIR),
        get_arch(),
        entryPointName.c_str());
}

SHARED_API int corehost_load(host_interface_t* init)
{
    trace::setup();

    // Re-initialize global state in case of re-entry
    g_init = hostpolicy_init_t();

    if (!hostpolicy_init_t::init(init, &g_init))
    {
        return StatusCode::LibHostInitFailure;
    }
    
    return 0;
}

int corehost_main_init(const int argc, const pal::char_t* argv[], const pal::string_t& location, arguments_t& args)
{
    if (trace::is_enabled())
    {
        trace_hostpolicy_entrypoint_invocation(location);

        for (int i = 0; i < argc; ++i)
        {
            trace::info(_X("%s"), argv[i]);
        }
        trace::info(_X("}"));

        trace::info(_X("Deps file: %s"), g_init.deps_file.c_str());
        for (const auto& probe : g_init.probe_paths)
        {
            trace::info(_X("Additional probe dir: %s"), probe.c_str());
        }
    }

    // Take care of arguments
    if (!g_init.host_info.is_valid())
    {
        // For backwards compat (older hostfxr), default the host_info
        g_init.host_info.parse(argc, argv);
    }

    if (!parse_arguments(g_init, argc, argv, args))
    {
        return StatusCode::LibHostInvalidArgs;
    }

    args.trace();
    return 0;
}

SHARED_API int corehost_main(const int argc, const pal::char_t* argv[])
{
    arguments_t args;
    int rc = corehost_main_init(argc, argv, _X("corehost_main"), args);
    if (!rc)
    {
        rc = run(args);
    }

    return rc;
}

SHARED_API int corehost_main_with_output_buffer(const int argc, const pal::char_t* argv[], pal::char_t buffer[], int32_t buffer_size, int32_t* required_buffer_size)
{
    arguments_t args;

    int rc = corehost_main_init(argc, argv, _X("corehost_main_with_output_buffer "), args);
    if (!rc)
    {
        if (g_init.host_command == _X("get-native-search-directories"))
        {
            pal::string_t output_string;
            rc = run(args, &output_string);
            if (!rc)
            {
                // Get length in character count not including null terminator
                int len = output_string.length();

                if (len + 1 > buffer_size)
                {
                    rc = HostApiBufferTooSmall;
                    *required_buffer_size = len + 1;
                    trace::info(_X("get-native-search-directories failed with buffer too small"), output_string.c_str());
                }
                else
                {
                    output_string.copy(buffer, len);
                    buffer[len] = '\0';
                    *required_buffer_size = 0;
                    trace::info(_X("get-native-search-directories success: %s"), output_string.c_str());
                }
            }
        }
        else
        {
            trace::error(_X("Unknown command: %s"), g_init.host_command.c_str());
            rc = LibHostUnknownCommand;
        }
    }

    return rc;
}

SHARED_API int corehost_unload()
{
    return 0;
}

typedef void(*corehost_resolve_component_dependencies_result_fn)(
    const pal::char_t* assembly_paths,
    const pal::char_t* native_search_paths,
    const pal::char_t* resource_search_paths);

SHARED_API int corehost_resolve_component_dependencies(
    const pal::char_t *component_main_assembly_path,
    corehost_resolve_component_dependencies_result_fn result)
{
    if (trace::is_enabled())
    {
        trace_hostpolicy_entrypoint_invocation(_X("corehost_resolve_component_dependencies"));

        trace::info(_X("  Component main assembly path: %s"), component_main_assembly_path);
        trace::info(_X("}"));

        for (const auto& probe : g_init.probe_paths)
        {
            trace::info(_X("Additional probe dir: %s"), probe.c_str());
        }
    }

    // TODO: Need to redirect error writing (trace::error even with tracing disabled)
    // to some local buffer and return the buffer to the caller as detailed error message.
    // Like this the error is written to the stderr of the process which is pretty bad.
    // It makes sense for startup code path as there's no other way to report it to the user.
    // But with API call from managed code, the error should be invisible outside of exception.
    // Tracing should still contain the error just like now.

    // IMPORTANT: g_init is static/global and thus potentially accessed from multiple threads
    // We must only use it as read-only here (unlike the run scenarios which own it).
    // For example the frameworks in g_init.fx_definitions can't be used "as-is" by the resolver
    // right now as it would try to re-parse the .deps.json and thus modify the objects.

    // The assumption is that component dependency resolution will only be called
    // when the coreclr is hosted through this hostpolicy and thus it will
    // have already called corehost_main_init.
    if (!g_init.host_info.is_valid())
    {
        trace::error(_X("Hostpolicy must be initialized and corehost_main must have been called before calling corehost_resolve_component_dependencies."));
        return StatusCode::CoreHostLibLoadFailure;
    }

    // Initialize arguments (basically the structure describing the input app/component to resolve)
    arguments_t args;
    if (!init_arguments(
            component_main_assembly_path,
            g_init.host_info,
            g_init.tfm,
            g_init.host_mode,
            /* additional_deps_serialized */ pal::string_t(), // Additiona deps - don't use those from the app, they're already in the app
            /* deps_file */ pal::string_t(), // Avoid using any other deps file than the one next to the component
            g_init.probe_paths,
            args))
    {
        return StatusCode::LibHostInvalidArgs;
    }

    args.trace();

    // Initialize the "app" framework definition.
    auto app = new fx_definition_t();

    // For now intentionally don't process .runtimeconfig.json since we don't perform framework resolution.

    // Call parse_runtime_config since it initializes the defaults for various settings
    // but we don't have any .runtimeconfig.json for the component, so pass in empty paths.
    // Empty paths is a valid case and the method will simply skip parsing anything.
    app->parse_runtime_config(pal::string_t(), pal::string_t(), fx_reference_t(), fx_reference_t());
    if (!app->get_runtime_config().is_valid())
    {
        // This should really never happen, but fail gracefully if it does anyway.
        assert(false);
        trace::error(_X("Failed to initialize empty runtime config for the component."));
        return StatusCode::InvalidConfigFile;
    }

    // For components we don't want to resolve anything from the frameworks, since those will be supplied by the app.
    // So only use the component as the "app" framework.
    fx_definition_vector_t component_fx_definitions;
    component_fx_definitions.push_back(std::unique_ptr<fx_definition_t>(app));
    
    // TODO Review: Since we're only passing the one component framework, the resolver will not consider
    // frameworks from the app for probing paths. So potential references to paths inside frameworks will not resolve.

    // The RID graph still has to come from the actuall root framework, so take that from the g_init.fx_definitions
    // which are the frameworks for the app.
    deps_resolver_t resolver(
        args,
        component_fx_definitions,
        &get_root_framework(g_init.fx_definitions).get_deps().get_rid_fallback_graph(),
        true);

    pal::string_t resolver_errors;
    if (!resolver.valid(&resolver_errors))
    {
        trace::error(_X("Error initializing the dependency resolver: %s"), resolver_errors.c_str());
        return StatusCode::ResolverInitFailure;
    }

    // Don't write breadcrumbs since we're not executing the app, just resolving dependencies
    // doesn't guarantee that they will actually execute.

    probe_paths_t probe_paths;
    if (!resolver.resolve_probe_paths(&probe_paths, nullptr, /* ignore_missing_assemblies */ true))
    {
        return StatusCode::ResolverResolveFailure;
    }

    if (trace::is_enabled())
    {
        trace::info(_X("corehost_resolve_component_dependencies results: {"));
        trace::info(_X("  assembly_paths: '%s'"), probe_paths.tpa.data());
        trace::info(_X("  native_search_paths: '%s'"), probe_paths.native.data());
        trace::info(_X("  resource_search_paths: '%s'"), probe_paths.resources.data());
        trace::info(_X("}"));
    }

    result(
        probe_paths.tpa.data(),
        probe_paths.native.data(),
        probe_paths.resources.data());
    
    return 0;
}
