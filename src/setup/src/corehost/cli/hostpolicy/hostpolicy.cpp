// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <mutex>
#include <pal.h>
#include "args.h"
#include <trace.h>
#include "deps_resolver.h"
#include <fx_muxer.h>
#include <utils.h>
#include "coreclr.h"
#include <cpprest/json.h>
#include <error_codes.h>
#include "breadcrumbs.h"
#include <host_startup_info.h>
#include "hostpolicy_context.h"

namespace
{
    std::mutex g_init_lock;
    bool g_init_done;
    hostpolicy_init_t g_init;

    std::shared_ptr<coreclr_t> g_coreclr;

    std::mutex g_lib_lock;
    std::weak_ptr<coreclr_t> g_lib_coreclr;

    int create_coreclr(const hostpolicy_context_t &context, host_mode_t mode, std::unique_ptr<coreclr_t> &coreclr)
    {
        // Verbose logging
        if (trace::is_enabled())
        {
            context.coreclr_properties.log_properties();
        }

        std::vector<char> host_path;
        pal::pal_clrstring(context.host_path, &host_path);

        const char *app_domain_friendly_name = mode == host_mode_t::libhost ? "clr_libhost" : "clrhost";

        // Create a CoreCLR instance
        trace::verbose(_X("CoreCLR path = '%s', CoreCLR dir = '%s'"), context.clr_path.c_str(), context.clr_dir.c_str());
        auto hr = coreclr_t::create(
            context.clr_dir,
            host_path.data(),
            app_domain_friendly_name,
            context.coreclr_properties,
            coreclr);

        if (!SUCCEEDED(hr))
        {
            trace::error(_X("Failed to create CoreCLR, HRESULT: 0x%X"), hr);
            return StatusCode::CoreClrInitFailure;
        }

        return StatusCode::Success;
    }

    int create_hostpolicy_context(
        hostpolicy_init_t &hostpolicy_init,
        const arguments_t &args,
        bool breadcrumbs_enabled,
        std::shared_ptr<hostpolicy_context_t> &context)
    {

        std::unique_ptr<hostpolicy_context_t> context_local(new hostpolicy_context_t());
        int rc = context_local->initialize(hostpolicy_init, args, breadcrumbs_enabled);
        if (rc != StatusCode::Success)
            return rc;

        context = std::move(context_local);

        return StatusCode::Success;
    }
}

int get_or_create_coreclr(
    hostpolicy_init_t &hostpolicy_init,
    const arguments_t &args,
    host_mode_t mode,
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

        hostpolicy_context_t context {};
        int rc = context.initialize(hostpolicy_init, args, false /* enable_breadcrumbs */);
        if (rc != StatusCode::Success)
            return rc;

        std::unique_ptr<coreclr_t> coreclr_local;
        rc = create_coreclr(context, mode, coreclr_local);
        if (rc != StatusCode::Success)
            return rc;

        assert(g_coreclr == nullptr);
        g_coreclr = std::move(coreclr_local);
        g_lib_coreclr = g_coreclr;
    }

    coreclr = g_coreclr;
    return StatusCode::Success;
}

int run_host_command(
    hostpolicy_init_t &hostpolicy_init,
    const arguments_t &args,
    pal::string_t* out_host_command_result = nullptr)
{
    assert(out_host_command_result != nullptr);

    // Breadcrumbs are not enabled for API calls because they do not execute
    // the app and may be re-entry
    hostpolicy_context_t context {};
    int rc = context.initialize(hostpolicy_init, args, false /* enable_breadcrumbs */);
    if (rc != StatusCode::Success)
        return rc;

    // Check for host command(s)
    if (pal::strcasecmp(hostpolicy_init.host_command.c_str(), _X("get-native-search-directories")) == 0)
    {
        const pal::char_t *value;
        if (!context.coreclr_properties.try_get(common_property::NativeDllSearchDirectories, &value))
        {
            trace::error(_X("get-native-search-directories failed to find NATIVE_DLL_SEARCH_DIRECTORIES property"));
            return StatusCode::HostApiFailed;
        }

        assert(out_host_command_result != nullptr);
        out_host_command_result->assign(value);
        return StatusCode::Success;
    }

    return StatusCode::InvalidArgFailure;
}

int run_as_app(
    const std::shared_ptr<coreclr_t> &coreclr,
    const hostpolicy_context_t &context,
    int argc,
    const pal::char_t **argv)
{
    // Initialize clr strings for arguments
    std::vector<std::vector<char>> argv_strs(argc);
    std::vector<const char*> argv_local(argc);
    for (int i = 0; i < argc; i++)
    {
        pal::pal_clrstring(argv[i], &argv_strs[i]);
        argv_local[i] = argv_strs[i].data();
    }

    if (trace::is_enabled())
    {
        pal::string_t arg_str;
        for (int i = 0; i < argv_local.size(); i++)
        {
            pal::string_t cur;
            pal::clr_palstring(argv_local[i], &cur);
            arg_str.append(cur);
            arg_str.append(_X(","));
        }
        trace::info(_X("Launch host: %s, app: %s, argc: %d, args: %s"), context.host_path.c_str(),
            context.application.c_str(), argc, arg_str.c_str());
    }

    std::vector<char> managed_app;
    pal::pal_clrstring(context.application, &managed_app);

    // Leave breadcrumbs for servicing.
    breadcrumb_writer_t writer(context.breadcrumbs_enabled, context.breadcrumbs);
    writer.begin_write();

    // Previous hostpolicy trace messages must be printed before executing assembly
    trace::flush();

    // Execute the application
    unsigned int exit_code;
    auto hr = g_coreclr->execute_assembly(
        argv_local.size(),
        argv_local.data(),
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

int corehost_init(
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

    if (!parse_arguments(hostpolicy_init, argc, argv, args))
    {
        return StatusCode::LibHostInvalidArgs;
    }

    args.trace();
    return StatusCode::Success;
}

int corehost_main_init(
    hostpolicy_init_t &hostpolicy_init,
    const int argc,
    const pal::char_t* argv[],
    const pal::string_t& location,
    arguments_t& args)
{
    // Take care of arguments
    if (!hostpolicy_init.host_info.is_valid(hostpolicy_init.host_mode))
    {
        // For backwards compat (older hostfxr), default the host_info
        hostpolicy_init.host_info.parse(argc, argv);
    }

    return corehost_init(hostpolicy_init, argc, argv, location, args);
}

SHARED_API int corehost_main(const int argc, const pal::char_t* argv[])
{
    arguments_t args;
    int rc = corehost_main_init(g_init, argc, argv, _X("corehost_main"), args);
    if (rc != StatusCode::Success)
        return rc;

    std::shared_ptr<hostpolicy_context_t> context;
    rc = create_hostpolicy_context(g_init, args, true /* breadcrumbs_enabled */, context);
    if (rc != StatusCode::Success)
        return rc;

    std::unique_ptr<coreclr_t> coreclr;
    rc = create_coreclr(*context, g_init.host_mode, coreclr);
    if (rc != StatusCode::Success)
        return rc;

    assert(g_coreclr == nullptr);
    g_coreclr = std::move(coreclr);

    {
        std::lock_guard<std::mutex> lock{ g_lib_lock };
        g_lib_coreclr = g_coreclr;
    }

    rc = run_as_app(g_coreclr, *context, args.app_argc, args.app_argv);
    return rc;
}

SHARED_API int corehost_main_with_output_buffer(const int argc, const pal::char_t* argv[], pal::char_t buffer[], int32_t buffer_size, int32_t* required_buffer_size)
{
    arguments_t args;
    int rc = corehost_main_init(g_init, argc, argv, _X("corehost_main_with_output_buffer"), args);
    if (rc != StatusCode::Success)
        return rc;

    if (g_init.host_command == _X("get-native-search-directories"))
    {
        pal::string_t output_string;
        rc = run_host_command(g_init, args, &output_string);
        if (rc != StatusCode::Success)
            return rc;

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
    else
    {
        trace::error(_X("Unknown command: %s"), g_init.host_command.c_str());
        rc = StatusCode::LibHostUnknownCommand;
    }

    return rc;
}

int corehost_libhost_init(hostpolicy_init_t &hostpolicy_init, const pal::string_t& location, arguments_t& args)
{
    // Host info should always be valid in the delegate scenario
    assert(hostpolicy_init.host_info.is_valid(host_mode_t::libhost));

    return corehost_init(hostpolicy_init, 0, nullptr, location, args);
}

namespace
{
    int get_coreclr_delegate(const std::shared_ptr<coreclr_t> &coreclr, coreclr_delegate_type type, void** delegate)
    {
        switch (type)
        {
        case coreclr_delegate_type::com_activation:
            return coreclr->create_delegate(
                "System.Private.CoreLib",
                "Internal.Runtime.InteropServices.ComActivator",
                "GetClassFactoryForTypeInternal",
                delegate);
        case coreclr_delegate_type::load_in_memory_assembly:
            return coreclr->create_delegate(
                "System.Private.CoreLib",
                "Internal.Runtime.InteropServices.InMemoryAssemblyLoader",
                "LoadInMemoryAssembly",
                delegate);
        case coreclr_delegate_type::winrt_activation:
            return coreclr->create_delegate(
                "System.Private.CoreLib",
                "Internal.Runtime.InteropServices.WindowsRuntime.ActivationFactoryLoader",
                "GetActivationFactory",
                delegate);
        default:
            return StatusCode::LibHostInvalidArgs;
        }
    }
}

SHARED_API int corehost_get_coreclr_delegate(coreclr_delegate_type type, void** delegate)
{
    arguments_t args;

    int rc = corehost_libhost_init(g_init, _X("corehost_get_coreclr_delegate"), args);
    if (rc != StatusCode::Success)
        return rc;

    std::shared_ptr<coreclr_t> coreclr;
    rc = get_or_create_coreclr(g_init, args, g_init.host_mode, coreclr);
    if (rc != StatusCode::Success)
        return rc;

    return get_coreclr_delegate(coreclr, type, delegate);
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
    if (!g_init.host_info.is_valid(g_init.host_mode))
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
    app->parse_runtime_config(pal::string_t(), pal::string_t(), runtime_config_t::settings_t());
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
