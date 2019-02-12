// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <mutex>
#include "pal.h"
#include "args.h"
#include "trace.h"
#include "deps_resolver.h"
#include "fx_muxer.h"
#include "utils.h"
#include "coreclr.h"
#include "corehost.h"
#include "cpprest/json.h"
#include "libhost.h"
#include "error_codes.h"
#include "breadcrumbs.h"
#include "host_startup_info.h"

namespace
{
    std::mutex g_init_lock;
    bool g_init_done;
    hostpolicy_init_t g_init;

    std::shared_ptr<coreclr_t> g_coreclr;

    std::mutex g_lib_lock;
    std::weak_ptr<coreclr_t> g_lib_coreclr;

    class prepare_to_run_t
    {
    public:
        prepare_to_run_t(
            hostpolicy_init_t &hostpolicy_init,
            const arguments_t& args,
            bool breadcrumbs_enabled)
            : _hostpolicy_init{ hostpolicy_init }
            , _resolver
                {
                    args,
                    hostpolicy_init.fx_definitions,
                    /* root_framework_rid_fallback_graph */ nullptr, // This means that the fx_definitions contains the root framework
                    hostpolicy_init.is_framework_dependent
                }
            , _breadcrumbs_enabled{ breadcrumbs_enabled }
        { }

        int build_coreclr_properties(
            coreclr_property_bag_t &properties,
            pal::string_t &clr_path,
            pal::string_t &clr_dir)
        {
            pal::string_t resolver_errors;
            if (!_resolver.valid(&resolver_errors))
            {
                trace::error(_X("Error initializing the dependency resolver: %s"), resolver_errors.c_str());
                return StatusCode::ResolverInitFailure;
            }

            probe_paths_t probe_paths;

            // Setup breadcrumbs.
            if (_breadcrumbs_enabled)
            {
                pal::string_t policy_name = _STRINGIFY(HOST_POLICY_PKG_NAME);
                pal::string_t policy_version = _STRINGIFY(HOST_POLICY_PKG_VER);

                // Always insert the hostpolicy that the code is running on.
                _breadcrumbs.insert(policy_name);
                _breadcrumbs.insert(policy_name + _X(",") + policy_version);

                if (!_resolver.resolve_probe_paths(&probe_paths, &_breadcrumbs))
                {
                    return StatusCode::ResolverResolveFailure;
                }
            }
            else
            {
                if (!_resolver.resolve_probe_paths(&probe_paths, nullptr))
                {
                    return StatusCode::ResolverResolveFailure;
                }
            }

            clr_path = probe_paths.coreclr;
            if (clr_path.empty() || !pal::realpath(&clr_path))
            {
                trace::error(_X("Could not resolve CoreCLR path. For more details, enable tracing by setting COREHOST_TRACE environment variable to 1"));;
                return StatusCode::CoreClrResolveFailure;
            }

            // Get path in which CoreCLR is present.
            clr_dir = get_directory(clr_path);

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

            pal::pal_clrstring(probe_paths.tpa, &_tpa_paths_cstr);
            pal::pal_clrstring(_resolver.get_app_dir(), &_app_base_cstr);
            pal::pal_clrstring(probe_paths.native, &_native_dirs_cstr);
            pal::pal_clrstring(probe_paths.resources, &_resources_dirs_cstr);

            const fx_definition_vector_t &fx_definitions = _resolver.get_fx_definitions();

            pal::string_t fx_deps_str;
            if (_resolver.is_framework_dependent())
            {
                // Use the root fx to define FX_DEPS_FILE
                fx_deps_str = get_root_framework(fx_definitions).get_deps_file();
            }
            pal::pal_clrstring(fx_deps_str, &_fx_deps);

            fx_definition_vector_t::iterator fx_begin;
            fx_definition_vector_t::iterator fx_end;
            _resolver.get_app_fx_definition_range(&fx_begin, &fx_end);

            pal::string_t app_context_deps_str;
            fx_definition_vector_t::iterator fx_curr = fx_begin;
            while (fx_curr != fx_end)
            {
                if (fx_curr != fx_begin)
                    app_context_deps_str += _X(';');

                app_context_deps_str += (*fx_curr)->get_deps_file();
                ++fx_curr;
            }

            pal::pal_clrstring(app_context_deps_str, &_app_context_deps);
            pal::pal_clrstring(_resolver.get_lookup_probe_directories(), &_probe_directories);

            if (_resolver.is_framework_dependent())
            {
                pal::pal_clrstring(get_root_framework(fx_definitions).get_found_version(), &_clr_library_version);
            }
            else
            {
                pal::pal_clrstring(_resolver.get_coreclr_library_version(), &_clr_library_version);
            }

            properties.add(common_property::TrustedPlatformAssemblies, _tpa_paths_cstr.data());
            properties.add(common_property::NativeDllSearchDirectories, _native_dirs_cstr.data());
            properties.add(common_property::PlatformResourceRoots, _resources_dirs_cstr.data());
            properties.add(common_property::AppDomainCompatSwitch, "UseLatestBehaviorWhenTFMNotSpecified");
            properties.add(common_property::AppContextBaseDirectory, _app_base_cstr.data());
            properties.add(common_property::AppContextDepsFiles, _app_context_deps.data());
            properties.add(common_property::FxDepsFile, _fx_deps.data());
            properties.add(common_property::ProbingDirectories, _probe_directories.data());
            properties.add(common_property::FxProductVersion, _clr_library_version.data());

            if (!clrjit_path.empty())
            {
                pal::pal_clrstring(clrjit_path, &_clrjit_path_cstr);
                properties.add(common_property::JitPath, _clrjit_path_cstr.data());
            }

            bool set_app_paths = false;

            // Runtime options config properties.
            for (int i = 0; i < _hostpolicy_init.cfg_keys.size(); ++i)
            {
                // Provide opt-in compatible behavior by using the switch to set APP_PATHS
                if (pal::cstrcasecmp(_hostpolicy_init.cfg_keys[i].data(), "Microsoft.NETCore.DotNetHostPolicy.SetAppPaths") == 0)
                {
                    set_app_paths = (pal::cstrcasecmp(_hostpolicy_init.cfg_values[i].data(), "true") == 0);
                }

                properties.add(_hostpolicy_init.cfg_keys[i].data(), _hostpolicy_init.cfg_values[i].data());
            }

            // App paths and App NI paths.
            // Note: Keep this check outside of the loop above since the _last_ key wins
            // and that could indicate the app paths shouldn't be set.
            if (set_app_paths)
            {
                properties.add(common_property::AppPaths, _app_base_cstr.data());
                properties.add(common_property::AppNIPaths, _app_base_cstr.data());
            }

            // Startup hooks
            pal::string_t startup_hooks;
            if (pal::getenv(_X("DOTNET_STARTUP_HOOKS"), &startup_hooks))
            {
                pal::pal_clrstring(startup_hooks, &_startup_hooks_cstr);
                properties.add(common_property::StartUpHooks, _startup_hooks_cstr.data());
            }

            return StatusCode::Success;
        }

        const std::unordered_set<pal::string_t>& breadcrumbs() const
        {
            return _breadcrumbs;
        }

    private:
        hostpolicy_init_t &_hostpolicy_init;

        deps_resolver_t _resolver;
        const bool _breadcrumbs_enabled;
        std::unordered_set<pal::string_t> _breadcrumbs;

        // Note: these variables' lifetime should be longer than a call to coreclr_initialize.
        std::vector<char> _tpa_paths_cstr;
        std::vector<char> _app_base_cstr;
        std::vector<char> _native_dirs_cstr;
        std::vector<char> _resources_dirs_cstr;
        std::vector<char> _fx_deps;
        std::vector<char> _app_context_deps;
        std::vector<char> _clrjit_path_cstr;
        std::vector<char> _probe_directories;
        std::vector<char> _clr_library_version;
        std::vector<char> _startup_hooks_cstr;
    };
}

int run_as_lib(
    hostpolicy_init_t &hostpolicy_init,
    const arguments_t &args,
    std::shared_ptr<coreclr_t> &coreclr)
{
    coreclr = g_lib_coreclr.lock();
    if (coreclr != nullptr)
    {
        // [TODO] Validate the current CLR instance is acceptable for this request

        trace::info(_X("Using existing CoreClr instance"));
        return StatusCode::Success;
    }

    {
        std::lock_guard<std::mutex> lock{ g_lib_lock };
        coreclr = g_lib_coreclr.lock();
        if (coreclr != nullptr)
        {
            trace::info(_X("Using existing CoreClr instance"));
            return StatusCode::Success;
        }

        prepare_to_run_t prep{ hostpolicy_init, args, false /* breadcrumbs_enabled */ };

        // Build variables for CoreCLR instantiation
        coreclr_property_bag_t properties;
        pal::string_t clr_path;
        pal::string_t clr_dir;
        int rc = prep.build_coreclr_properties(properties, clr_path, clr_dir);
        if (rc != StatusCode::Success)
            return rc;

        // Verbose logging
        if (trace::is_enabled())
        {
            properties.log_properties();
        }

        std::vector<char> host_path;
        pal::pal_clrstring(args.host_path, &host_path);

        // Create a CoreCLR instance
        trace::verbose(_X("CoreCLR path = '%s', CoreCLR dir = '%s'"), clr_path.c_str(), clr_dir.c_str());
        std::unique_ptr<coreclr_t> coreclr_local;
        auto hr = coreclr_t::create(
            clr_dir,
            host_path.data(),
            "clr_libhost",
            properties,
            coreclr_local);

        if (!SUCCEEDED(hr))
        {
            trace::error(_X("Failed to create CoreCLR, HRESULT: 0x%X"), hr);
            return StatusCode::CoreClrInitFailure;
        }

        assert(g_coreclr == nullptr);
        g_coreclr = std::move(coreclr_local);
        g_lib_coreclr = g_coreclr;
    }

    coreclr = g_coreclr;
    return StatusCode::Success;
}

int run_as_app(
    hostpolicy_init_t &hostpolicy_init,
    const arguments_t &args,
    pal::string_t* out_host_command_result = nullptr)
{
    // Breadcrumbs are not enabled for API calls because they do not execute
    // the app and may be re-entry
    bool breadcrumbs_enabled = (out_host_command_result == nullptr);
    prepare_to_run_t prep{ hostpolicy_init, args, breadcrumbs_enabled };

    // Build variables for CoreCLR instantiation
    coreclr_property_bag_t properties;
    pal::string_t clr_path;
    pal::string_t clr_dir;
    int rc = prep.build_coreclr_properties(properties, clr_path, clr_dir);
    if (rc != StatusCode::Success)
        return rc;

    // Check for host command(s)
    if (pal::strcasecmp(hostpolicy_init.host_command.c_str(), _X("get-native-search-directories")) == 0)
    {
        const char *value;
        if (!properties.try_get(common_property::NativeDllSearchDirectories, &value))
        {
            trace::error(_X("get-native-search-directories failed to find NATIVE_DLL_SEARCH_DIRECTORIES property"));
            return StatusCode::HostApiFailed;
        }

        assert(out_host_command_result != nullptr);
        pal::clr_palstring(value, out_host_command_result);
        return StatusCode::Success;
    }

    // Verbose logging
    if (trace::is_enabled())
    {
        properties.log_properties();
    }

    std::vector<char> host_path;
    pal::pal_clrstring(args.host_path, &host_path);

    // Create a CoreCLR instance
    trace::verbose(_X("CoreCLR path = '%s', CoreCLR dir = '%s'"), clr_path.c_str(), clr_dir.c_str());
    std::unique_ptr<coreclr_t> coreclr;
    auto hr = coreclr_t::create(
        clr_dir,
        host_path.data(),
        "clrhost",
        properties,
        coreclr);

    if (!SUCCEEDED(hr))
    {
        trace::error(_X("Failed to create CoreCLR, HRESULT: 0x%X"), hr);
        return StatusCode::CoreClrInitFailure;
    }

    assert(g_coreclr == nullptr);
    g_coreclr = std::move(coreclr);

    {
        std::lock_guard<std::mutex> lock{ g_lib_lock };
        g_lib_coreclr = g_coreclr;
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
    breadcrumb_writer_t writer(breadcrumbs_enabled, prep.breadcrumbs());
    writer.begin_write();

    // Previous hostpolicy trace messages must be printed before executing assembly
    trace::flush();

    // Execute the application
    unsigned int exit_code;
    hr = g_coreclr->execute_assembly(
        argv.size(),
        argv.data(),
        managed_app.data(),
        &exit_code);

    if (!SUCCEEDED(hr))
    {
        trace::error(_X("Failed to execute managed app, HRESULT: 0x%X"), hr);
        return StatusCode::CoreClrExeFailure;
    }

    trace::info(_X("Execute managed assembly exit code: 0x%X"), exit_code);

    // Shut down the CoreCLR
    hr = g_coreclr->shutdown((int*)&exit_code);
    if (!SUCCEEDED(hr))
    {
        trace::warning(_X("Failed to shut down CoreCLR, HRESULT: 0x%X"), hr);
    }

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

//
// Loads and initilizes the hostpolicy.
//
// If hostpolicy is already initalized, the library will not be
// reinitialized.
//
SHARED_API int corehost_load(host_interface_t* init)
{
    assert(init != nullptr);
    std::lock_guard<std::mutex> lock{ g_init_lock };

    if (g_init_done)
    {
        // Since the host command is set during load _and_
        // load is considered re-entrant due to how testing is
        // done, permit the re-initialization of the host command.
        hostpolicy_init_t::init_host_command(init, &g_init);
        return StatusCode::Success;
    }

    trace::setup();

    g_init = hostpolicy_init_t{};

    if (!hostpolicy_init_t::init(init, &g_init))
    {
        g_init_done = false;
        return StatusCode::LibHostInitFailure;
    }

    g_init_done = true;
    return StatusCode::Success;
}

int corehost_main_init(
    hostpolicy_init_t &hostpolicy_init,
    const int argc,
    const pal::char_t* argv[],
    const pal::string_t& location,
    arguments_t& args)
{
    if (trace::is_enabled())
    {
        trace_hostpolicy_entrypoint_invocation(location);

        for (int i = 0; i < argc; ++i)
        {
            trace::info(_X("%s"), argv[i]);
        }
        trace::info(_X("}"));

        trace::info(_X("Deps file: %s"), hostpolicy_init.deps_file.c_str());
        for (const auto& probe : hostpolicy_init.probe_paths)
        {
            trace::info(_X("Additional probe dir: %s"), probe.c_str());
        }
    }

    // Take care of arguments
    if (!hostpolicy_init.host_info.is_valid())
    {
        // For backwards compat (older hostfxr), default the host_info
        hostpolicy_init.host_info.parse(argc, argv);
    }

    if (!parse_arguments(hostpolicy_init, argc, argv, args))
    {
        return StatusCode::LibHostInvalidArgs;
    }

    args.trace();
    return StatusCode::Success;
}

SHARED_API int corehost_main(const int argc, const pal::char_t* argv[])
{
    arguments_t args;
    int rc = corehost_main_init(g_init, argc, argv, _X("corehost_main"), args);
    if (!rc)
    {
        rc = run_as_app(g_init, args);
    }

    return rc;
}

SHARED_API int corehost_main_with_output_buffer(const int argc, const pal::char_t* argv[], pal::char_t buffer[], int32_t buffer_size, int32_t* required_buffer_size)
{
    arguments_t args;
    int rc = corehost_main_init(g_init, argc, argv, _X("corehost_main_with_output_buffer"), args);
    if (!rc)
    {
        if (g_init.host_command == _X("get-native-search-directories"))
        {
            pal::string_t output_string;
            rc = run_as_app(g_init, args, &output_string);
            if (!rc)
            {
                // Get length in character count not including null terminator
                int len = output_string.length();

                if (len + 1 > buffer_size)
                {
                    rc = StatusCode::HostApiBufferTooSmall;
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
            rc = StatusCode::LibHostUnknownCommand;
        }
    }

    return rc;
}

int corehost_libhost_init(hostpolicy_init_t &hostpolicy_init, const pal::string_t& location, arguments_t& args)
{
    if (trace::is_enabled())
    {
        trace_hostpolicy_entrypoint_invocation(location);
        trace::info(_X("}"));

        trace::info(_X("Deps file: %s"), hostpolicy_init.deps_file.c_str());
        for (const auto& probe : hostpolicy_init.probe_paths)
        {
            trace::info(_X("Additional probe dir: %s"), probe.c_str());
        }
    }

    // Host info should always be valid in the delegate scenario
    assert(hostpolicy_init.host_info.is_valid());

    if (!parse_arguments(hostpolicy_init, 0, nullptr, args))
    {
        return StatusCode::LibHostInvalidArgs;
    }

    args.trace();
    return StatusCode::Success;
}

SHARED_API int corehost_get_com_activation_delegate(void **delegate)
{
    arguments_t args;

    int rc = corehost_libhost_init(g_init, _X("corehost_get_com_activation_delegate"), args);
    if (rc != StatusCode::Success)
        return rc;

    std::shared_ptr<coreclr_t> coreclr;
    rc = run_as_lib(g_init, args, coreclr);
    if (rc != StatusCode::Success)
        return rc;

    return coreclr->create_delegate(
        "System.Private.CoreLib",
        "Internal.Runtime.InteropServices.ComActivator",
        "GetClassFactoryForTypeInternal",
        delegate);
}

SHARED_API int corehost_unload()
{
    return StatusCode::Success;
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

    // If the current host mode is libhost, use apphost instead.
    host_mode_t host_mode = g_init.host_mode == host_mode_t::libhost ? host_mode_t::apphost : g_init.host_mode;

    // Initialize arguments (basically the structure describing the input app/component to resolve)
    arguments_t args;
    if (!init_arguments(
            component_main_assembly_path,
            g_init.host_info,
            g_init.tfm,
            host_mode,
            /* additional_deps_serialized */ pal::string_t(), // Additional deps - don't use those from the app, they're already in the app
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


typedef void(*corehost_error_writer_fn)(const pal::char_t* message);

//
// Sets a callback which is to be used to write errors to.
//
// Parameters:
//     error_writer 
//         A callback function which will be invoked every time an error is to be reported.
//         Or nullptr to unregister previously registered callback and return to the default behavior.
// Return value:
//     The previously registered callback (which is now unregistered), or nullptr if no previous callback
//     was registered
// 
// The error writer is registered per-thread, so the registration is thread-local. On each thread
// only one callback can be registered. Subsequent registrations overwrite the previous ones.
// 
// By default no callback is registered in which case the errors are written to stderr.
// 
// Each call to the error writer is sort of like writing a single line (the EOL character is omitted).
// Multiple calls to the error writer may occure for one failure.
//
SHARED_API corehost_error_writer_fn corehost_set_error_writer(corehost_error_writer_fn error_writer)
{
    return trace::set_error_writer(error_writer);
}
