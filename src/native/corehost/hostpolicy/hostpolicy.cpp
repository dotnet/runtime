// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <atomic>
#include <condition_variable>
#include <mutex>
#include <pal.h>
#include "args.h"
#include <trace.h>
#include "deps_resolver.h"
#include <fx_muxer.h>
#include <utils.h>
#include "coreclr.h"
#include <error_codes.h>
#include "breadcrumbs.h"
#include <host_startup_info.h>
#include <corehost_context_contract.h>
#include <hostpolicy.h>
#include "hostpolicy_context.h"
#include "bundle/runner.h"

namespace
{
    // Initialization information set through corehost_load. All other entry points assume this has already
    // been set and use it to perform the requested operation. Note that this being initialized does not
    // indicate that the runtime is loaded or that the runtime will be loaded (e.g. host commands).
    std::mutex g_init_lock;
    bool g_init_done;
    hostpolicy_init_t g_init;

    // hostpolicy tracks the context used to load and initialize coreclr. This is the first context that
    // is successfully created and used to load the runtime. There can only be one hostpolicy context.
    std::mutex g_context_lock;

    // Tracks the hostpolicy context. This is the one and only hostpolicy context. It represents the information
    // that hostpolicy will use or has already used to load and initialize coreclr. It will be set once a context
    // is initialized and updated to hold coreclr once the runtime is loaded.
    std::shared_ptr<hostpolicy_context_t> g_context;

    // Tracks whether the hostpolicy context is initializing (from start of creation of the first context
    // to loading coreclr). It will be false before initialization starts and after it succeeds or fails.
    // Attempts to get/create a context should block if the first context is initializing (i.e. this is true).
    // The condition variable is used to block on and signal changes to this state.
    std::atomic<bool> g_context_initializing(false);
    std::condition_variable g_context_initializing_cv;

    int HOSTPOLICY_CALLTYPE create_coreclr()
    {
        int rc;
        {
            std::lock_guard<std::mutex> context_lock { g_context_lock };
            if (g_context == nullptr)
            {
                trace::error(_X("Hostpolicy has not been initialized"));
                return StatusCode::HostInvalidState;
            }

            if (g_context->coreclr != nullptr)
            {
                trace::error(_X("CoreClr has already been loaded"));
                return StatusCode::HostInvalidState;
            }

            // Verbose logging
            if (trace::is_enabled())
                g_context->coreclr_properties.log_properties();

            std::vector<char> host_path;
            pal::pal_clrstring(g_context->host_path, &host_path);
            const char *app_domain_friendly_name = g_context->host_mode == host_mode_t::libhost ? "clr_libhost" : "clrhost";

            // Create a CoreCLR instance
            trace::verbose(_X("CoreCLR path = '%s', CoreCLR dir = '%s'"), g_context->clr_path.c_str(), g_context->clr_dir.c_str());
            auto hr = coreclr_t::create(
                g_context->clr_dir,
                host_path.data(),
                app_domain_friendly_name,
                g_context->coreclr_properties,
                g_context->coreclr);

            if (!SUCCEEDED(hr))
            {
                trace::error(_X("Failed to create CoreCLR, HRESULT: 0x%X"), hr);
                rc = StatusCode::CoreClrInitFailure;
            }
            else
            {
                rc = StatusCode::Success;
            }

            g_context_initializing.store(false);
        }

        g_context_initializing_cv.notify_all();
        return rc;
    }

    int create_hostpolicy_context(
        hostpolicy_init_t &hostpolicy_init,
        const int argc,
        const pal::char_t *argv[],
        bool breadcrumbs_enabled,
        /*out*/ arguments_t *out_args = nullptr)
    {
        {
            std::unique_lock<std::mutex> lock{ g_context_lock };
            g_context_initializing_cv.wait(lock, [] { return !g_context_initializing.load(); });

            const hostpolicy_context_t *existing_context = g_context.get();
            if (existing_context != nullptr)
            {
                trace::info(_X("Host context has already been initialized"));
                assert(existing_context->coreclr != nullptr);
                return StatusCode::Success_HostAlreadyInitialized;
            }

            g_context_initializing.store(true);
        }

        g_context_initializing_cv.notify_all();

        arguments_t args;
        if (!parse_arguments(hostpolicy_init, argc, argv, args))
            return StatusCode::LibHostInvalidArgs;

        if (out_args != nullptr)
            *out_args = args;

        std::unique_ptr<hostpolicy_context_t> context_local(new hostpolicy_context_t());
        int rc = context_local->initialize(hostpolicy_init, args, breadcrumbs_enabled);
        if (rc != StatusCode::Success)
        {
            {
                std::lock_guard<std::mutex> lock{ g_context_lock };
                g_context_initializing.store(false);
            }

            g_context_initializing_cv.notify_all();
            return rc;
        }

        {
            std::lock_guard<std::mutex> lock{ g_context_lock };
            g_context.reset(context_local.release());
        }

        return StatusCode::Success;
    }

    const std::shared_ptr<hostpolicy_context_t> get_hostpolicy_context(bool require_runtime)
    {
        std::lock_guard<std::mutex> lock{ g_context_lock };

        const std::shared_ptr<hostpolicy_context_t> existing_context = g_context;
        if (existing_context == nullptr)
        {
            trace::error(_X("Hostpolicy context has not been created"));
            return nullptr;
        }

        if (require_runtime && existing_context->coreclr == nullptr)
        {
            trace::error(_X("Runtime has not been loaded and initialized"));
            return nullptr;
        }

        return existing_context;
    }
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

int run_app_for_context(
    const hostpolicy_context_t &context,
    int argc,
    const pal::char_t **argv)
{
    assert(context.coreclr != nullptr);

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
        for (size_t i = 0; i < argv_local.size(); i++)
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
    std::shared_ptr<breadcrumb_writer_t> writer;
    if (!context.breadcrumbs.empty())
    {
        writer = breadcrumb_writer_t::begin_write(context.breadcrumbs);
        assert(context.breadcrumbs.empty());
    }

    // Previous hostpolicy trace messages must be printed before executing assembly
    trace::flush();

    // Execute the application
    unsigned int exit_code;
    auto hr = context.coreclr->execute_assembly(
        (int32_t)argv_local.size(),
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
    hr = context.coreclr->shutdown(reinterpret_cast<int*>(&exit_code));
    if (!SUCCEEDED(hr))
    {
        trace::warning(_X("Failed to shut down CoreCLR, HRESULT: 0x%X"), hr);
    }

    if (writer)
    {
        writer->end_write();
    }

    return exit_code;
}

int HOSTPOLICY_CALLTYPE run_app(const int argc, const pal::char_t *argv[])
{
    const std::shared_ptr<hostpolicy_context_t> context = get_hostpolicy_context(/*require_runtime*/ true);
    if (context == nullptr)
        return StatusCode::HostInvalidState;

    return run_app_for_context(*context, argc, argv);
}

void trace_hostpolicy_entrypoint_invocation(const pal::string_t& entryPointName)
{
    trace::info(_X("--- Invoked hostpolicy [commit hash: %s] [%s,%s,%s][%s] %s = {"),
        _STRINGIFY(REPO_COMMIT_HASH),
        _STRINGIFY(HOST_POLICY_PKG_NAME),
        _STRINGIFY(HOST_POLICY_PKG_VER),
        _STRINGIFY(HOST_POLICY_PKG_REL_DIR),
        get_current_arch_name(),
        entryPointName.c_str());
}

//
// Loads and initializes the hostpolicy.
//
// If hostpolicy is already initialized, the library will not be
// reinitialized.
//
SHARED_API int HOSTPOLICY_CALLTYPE corehost_load(const host_interface_t* init)
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

void trace_corehost_init(
    const hostpolicy_init_t &hostpolicy_init,
    const int argc,
    const pal::char_t* argv[],
    const pal::string_t& location)
{
    if (trace::is_enabled())
    {
        trace_hostpolicy_entrypoint_invocation(location);

        for (int i = 0; i < argc; ++i)
        {
            trace::info(_X("%s"), argv[i]);
        }
        trace::info(_X("}"));

        const pal::char_t *host_mode_str;
        switch (hostpolicy_init.host_mode)
        {
            case host_mode_t::muxer:
                host_mode_str = _X("muxer");
                break;
            case host_mode_t::apphost:
                host_mode_str = _X("apphost");
                break;
            case host_mode_t::split_fx:
                host_mode_str = _X("split_fx");
                break;
            case host_mode_t::libhost:
                host_mode_str = _X("libhost");
                break;
            case host_mode_t::invalid:
            default:
                host_mode_str = _X("invalid");
                break;
        }

        trace::info(_X("Mode: %s"), host_mode_str);
        trace::info(_X("Deps file: %s"), hostpolicy_init.deps_file.c_str());
        for (const auto& probe : hostpolicy_init.probe_paths)
        {
            trace::info(_X("Additional probe dir: %s"), probe.c_str());
        }
    }
}

int corehost_main_init(
    hostpolicy_init_t& hostpolicy_init,
    const int argc,
    const pal::char_t* argv[],
    const pal::string_t& location)
{
    // Take care of arguments
    if (!hostpolicy_init.host_info.is_valid(hostpolicy_init.host_mode))
    {
        // For backwards compat (older hostfxr), default the host_info
        hostpolicy_init.host_info.parse(argc, argv);
    }

    if (bundle::info_t::is_single_file_bundle())
    {
        const bundle::runner_t* bundle = bundle::runner_t::app();
        StatusCode status = bundle->process_manifest_and_extract();
        if (status != StatusCode::Success)
        {
            return status;
        }

        if (bundle->is_netcoreapp3_compat_mode())
        {
            auto extracted_assembly = bundle->extraction_path();
            auto app_name = hostpolicy_init.host_info.get_app_name() + _X(".dll");
            append_path(&extracted_assembly, app_name.c_str());
            assert(pal::file_exists(extracted_assembly));
            hostpolicy_init.host_info.app_path = extracted_assembly;
        }
    }

    trace_corehost_init(hostpolicy_init, argc, argv, location);
    return StatusCode::Success;
}

SHARED_API int HOSTPOLICY_CALLTYPE corehost_main(const int argc, const pal::char_t* argv[])
{
    int rc = corehost_main_init(g_init, argc, argv, _X("corehost_main"));
    if (rc != StatusCode::Success)
        return rc;

    arguments_t args;
    assert(g_context == nullptr);
    rc = create_hostpolicy_context(g_init, argc, argv, true /* breadcrumbs_enabled */, &args);
    if (rc != StatusCode::Success)
        return rc;

    rc = create_coreclr();
    if (rc != StatusCode::Success)
        return rc;

    return run_app(args.app_argc, args.app_argv);
}

SHARED_API int HOSTPOLICY_CALLTYPE corehost_main_with_output_buffer(const int argc, const pal::char_t* argv[], pal::char_t buffer[], int32_t buffer_size, int32_t* required_buffer_size)
{
    int rc = corehost_main_init(g_init, argc, argv, _X("corehost_main_with_output_buffer"));
    if (rc != StatusCode::Success)
        return rc;

    if (g_init.host_command == _X("get-native-search-directories"))
    {
        arguments_t args;
        if (!parse_arguments(g_init, argc, argv, args))
            return StatusCode::LibHostInvalidArgs;

        pal::string_t output_string;
        rc = run_host_command(g_init, args, &output_string);
        if (rc != StatusCode::Success)
            return rc;

        // Get length in character count not including null terminator
        int32_t len = static_cast<int32_t>(output_string.length());

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

void trace_corehost_libhost_init(const hostpolicy_init_t &hostpolicy_init, const pal::string_t& location)
{
    // Host info should always be valid in the delegate scenario
    assert(hostpolicy_init.host_info.is_valid(host_mode_t::libhost));

    // Single-file bundle is only expected in apphost mode.
    assert(!bundle::info_t::is_single_file_bundle());

    trace_corehost_init(hostpolicy_init, 0, nullptr, location);
}

namespace
{
    int HOSTPOLICY_CALLTYPE get_delegate(coreclr_delegate_type type, void **delegate)
    {
        if (delegate == nullptr)
            return StatusCode::InvalidArgFailure;

        const std::shared_ptr<hostpolicy_context_t> context = get_hostpolicy_context(/*require_runtime*/ true);
        if (context == nullptr)
            return StatusCode::HostInvalidState;

        coreclr_t *coreclr = context->coreclr.get();
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
            return StatusCode::InvalidArgFailure;
        case coreclr_delegate_type::com_register:
            return coreclr->create_delegate(
                "System.Private.CoreLib",
                "Internal.Runtime.InteropServices.ComActivator",
                "RegisterClassForTypeInternal",
                delegate);
        case coreclr_delegate_type::com_unregister:
            return coreclr->create_delegate(
                "System.Private.CoreLib",
                "Internal.Runtime.InteropServices.ComActivator",
                "UnregisterClassForTypeInternal",
                delegate);
        case coreclr_delegate_type::load_assembly_and_get_function_pointer:
            return coreclr->create_delegate(
                "System.Private.CoreLib",
                "Internal.Runtime.InteropServices.ComponentActivator",
                "LoadAssemblyAndGetFunctionPointer",
                delegate);
        case coreclr_delegate_type::get_function_pointer:
            return coreclr->create_delegate(
                "System.Private.CoreLib",
                "Internal.Runtime.InteropServices.ComponentActivator",
                "GetFunctionPointer",
                delegate);
        default:
            return StatusCode::LibHostInvalidArgs;
        }
    }

    int HOSTPOLICY_CALLTYPE get_property(const pal::char_t *key, const pal::char_t **value)
    {
        if (key == nullptr)
            return StatusCode::InvalidArgFailure;

        const std::shared_ptr<hostpolicy_context_t> context = get_hostpolicy_context(/*require_runtime*/ false);
        if (context == nullptr)
            return StatusCode::HostInvalidState;

        if (!context->coreclr_properties.try_get(key, value))
            return StatusCode::HostPropertyNotFound;

        return StatusCode::Success;
    }

    int HOSTPOLICY_CALLTYPE set_property(const pal::char_t *key, const pal::char_t *value)
    {
        if (key == nullptr)
            return StatusCode::InvalidArgFailure;

        std::lock_guard<std::mutex> lock{ g_context_lock };
        if (g_context == nullptr || g_context->coreclr != nullptr)
        {
            trace::error(_X("Setting properties is only allowed before runtime has been loaded and initialized"));
            return HostInvalidState;
        }

        if (value != nullptr)
        {
            g_context->coreclr_properties.add(key, value);
        }
        else
        {
            g_context->coreclr_properties.remove(key);
        }

        return StatusCode::Success;
    }

    int HOSTPOLICY_CALLTYPE get_properties(size_t * count, const pal::char_t **keys, const pal::char_t **values)
    {
        if (count == nullptr)
            return StatusCode::InvalidArgFailure;

        const std::shared_ptr<hostpolicy_context_t> context = get_hostpolicy_context(/*require_runtime*/ false);
        if (context == nullptr)
            return StatusCode::HostInvalidState;

        size_t actualCount = context->coreclr_properties.count();
        size_t input_count = *count;
        *count = actualCount;
        if (input_count < actualCount || keys == nullptr || values == nullptr)
            return StatusCode::HostApiBufferTooSmall;

        int index = 0;
        std::function<void (const pal::string_t &,const pal::string_t &)> callback = [&] (const pal::string_t& key, const pal::string_t& value)
        {
            keys[index] = key.data();
            values[index] = value.data();
            ++index;
        };
        context->coreclr_properties.enumerate(callback);

        return StatusCode::Success;
    }

    bool matches_existing_properties(const coreclr_property_bag_t &properties, const corehost_initialize_request_t *init_request)
    {
        bool hasDifferentProperties = false;
        size_t len = init_request->config_keys.len;
        for (size_t i = 0; i < len; ++i)
        {
            const pal::char_t *key = init_request->config_keys.arr[i];
            const pal::char_t *value = init_request->config_values.arr[i];

            const pal::char_t *existingValue;
            if (properties.try_get(key, &existingValue))
            {
                if (pal::strcmp(existingValue, value) != 0)
                {
                    trace::warning(_X("The property [%s] has a different value [%s] from that in the previously loaded runtime [%s]"), key, value, existingValue);
                    hasDifferentProperties = true;
                }
            }
            else
            {
                trace::warning(_X("The property [%s] is not present in the previously loaded runtime."), key);
                hasDifferentProperties = true;
            }
        }

        if (len > 0 && !hasDifferentProperties)
            trace::info(_X("All specified properties match those in the previously loaded runtime"));

        return !hasDifferentProperties;
    }
}

// Initializes hostpolicy. Calculates everything required to start the runtime and creates a context to track
// that information
//
// Parameters:
//    init_request
//      struct containing information about the initialization request. If hostpolicy is not yet initialized,
//      this is expected to be nullptr. If hostpolicy is already initialized, this should not be nullptr and
//      this function will use the struct to check for compatibility with the way in which hostpolicy was
//      previously initialized.
//    options
//      initialization options
//    context_contract
//      [out] if initialization is successful, populated with a contract for performing operations on hostpolicy
//
// Return value:
//    Success                            - Initialization was successful
//    Success_HostAlreadyInitialized     - Request is compatible with already initialized hostpolicy
//    Success_DifferentRuntimeProperties - Request has runtime properties that differ from already initialized hostpolicy
//
// This function does not load the runtime
//
// If a previous request to initialize hostpolicy was made, but the runtime was not yet loaded, this function will
// block until the runtime is loaded.
//
// This function assumes corehost_load has already been called. It uses the init information set through that
// call - not the struct passed into this function - to create a context.
//
// Both Success_HostAlreadyInitialized and Success_DifferentRuntimeProperties codes are considered successful
// initializations. In the case of Success_DifferentRuntimeProperties, it is left to the consumer to verify that
// the difference in properties is acceptable.
//
SHARED_API int HOSTPOLICY_CALLTYPE corehost_initialize(const corehost_initialize_request_t *init_request, uint32_t options, /*out*/ corehost_context_contract *context_contract)
{
    if (context_contract == nullptr)
        return StatusCode::InvalidArgFailure;

    bool version_set = (options & initialization_options_t::context_contract_version_set) != 0;
    bool wait_for_initialized = (options & initialization_options_t::wait_for_initialized) != 0;
    bool get_contract = (options & initialization_options_t::get_contract) != 0;
    if (wait_for_initialized && get_contract)
    {
        trace::error(_X("Specifying both initialization options for wait_for_initialized and get_contract is not allowed"));
        return StatusCode::InvalidArgFailure;
    }

    if (get_contract)
    {
        if (init_request != nullptr)
        {
            trace::error(_X("Initialization request is expected to be null when getting the already initialized contract"));
            return StatusCode::InvalidArgFailure;
        }
    }
    else
    {
        std::unique_lock<std::mutex> lock { g_context_lock };
        bool already_initializing = g_context_initializing.load();
        bool already_initialized = g_context.get() != nullptr;

        if (wait_for_initialized)
        {
            trace::verbose(_X("Initialization option to wait for initialize request is set"));
            if (init_request == nullptr)
            {
                trace::error(_X("Initialization request is expected to be non-null when waiting for initialize request option is set"));
                return StatusCode::InvalidArgFailure;
            }

            // If we are not already initializing or done initializing, wait until another context initialization has started
            if (!already_initialized && !already_initializing)
            {
                trace::info(_X("Waiting for another request to initialize hostpolicy"));
                g_context_initializing_cv.wait(lock, [&] { return g_context_initializing.load(); });
            }
        }
        else
        {
            if (init_request != nullptr && !already_initialized && !already_initializing)
            {
                trace::error(_X("Initialization request is expected to be null for the first initialization request"));
                return StatusCode::InvalidArgFailure;
            }

            if (init_request == nullptr && (already_initializing || already_initialized))
            {
                trace::error(_X("Initialization request is expected to be non-null for requests other than the first one"));
                return StatusCode::InvalidArgFailure;
            }
        }
    }

    // Trace entry point information using previously set init information.
    // This function does not modify any global state.
    trace_corehost_libhost_init(g_init, _X("corehost_initialize"));

    int rc;
    if (wait_for_initialized)
    {
        // Wait for context initialization to complete
        std::unique_lock<std::mutex> lock{ g_context_lock };
        g_context_initializing_cv.wait(lock, [] { return !g_context_initializing.load(); });

        const hostpolicy_context_t *existing_context = g_context.get();
        if (existing_context == nullptr || existing_context->coreclr == nullptr)
        {
            trace::info(_X("Option to wait for initialize request was set, but that request did not result in initialization"));
            return StatusCode::HostInvalidState;
        }

        rc = StatusCode::Success_HostAlreadyInitialized;
    }
    else if (get_contract)
    {
        const std::shared_ptr<hostpolicy_context_t> context = get_hostpolicy_context(/*require_runtime*/ true);
        if (context == nullptr)
        {
            trace::error(_X("Option to get the contract for the initialized hostpolicy was set, but hostpolicy has not been initialized"));
            return StatusCode::HostInvalidState;
        }

        rc = StatusCode::Success;
    }
    else
    {
        rc = create_hostpolicy_context(g_init, 0 /*argc*/, nullptr /*argv*/, g_init.host_mode != host_mode_t::libhost);
        if (rc != StatusCode::Success && rc != StatusCode::Success_HostAlreadyInitialized)
            return rc;
    }

    if (rc == StatusCode::Success_HostAlreadyInitialized)
    {
        assert(init_request != nullptr
            && init_request->version >= offsetof(corehost_initialize_request_t, config_values) + sizeof(init_request->config_values)
            && init_request->config_keys.len == init_request->config_values.len);

        const std::shared_ptr<hostpolicy_context_t> context = get_hostpolicy_context(/*require_runtime*/ true);
        if (context == nullptr)
            return StatusCode::HostInvalidState;

        // Compare the current context with this request (properties)
        if (!matches_existing_properties(context->coreclr_properties, init_request))
            rc = StatusCode::Success_DifferentRuntimeProperties;
    }

    // If version wasn't set, then it would have the original size of corehost_context_contract, which is 7 * sizeof(size_t).
    size_t version_lo = version_set ? context_contract->version : 7 * sizeof(size_t);
    context_contract->version = sizeof(corehost_context_contract);
    context_contract->get_property_value = get_property;
    context_contract->set_property_value = set_property;
    context_contract->get_properties = get_properties;
    context_contract->load_runtime = create_coreclr;
    context_contract->run_app = run_app;
    context_contract->get_runtime_delegate = get_delegate;

    // An old hostfxr may not have provided enough space for these fields.
    // The version_lo (sizeof) the old hostfxr saw at build time will be
    // smaller and we should not attempt to write the fields in that case.
    if (version_lo >= offsetof(corehost_context_contract, last_known_delegate_type) + sizeof(context_contract->last_known_delegate_type))
    {
        context_contract->last_known_delegate_type = (size_t)coreclr_delegate_type::__last - 1;
    }

    return rc;
}

SHARED_API int HOSTPOLICY_CALLTYPE corehost_unload()
{
    {
        std::lock_guard<std::mutex> lock{ g_context_lock };
        if (g_context != nullptr && g_context->coreclr != nullptr)
            return StatusCode::Success;

        // Allow re-initializing if runtime has not been loaded
        g_context.reset();
        g_context_initializing.store(false);
    }

    g_context_initializing_cv.notify_all();

    std::lock_guard<std::mutex> init_lock{ g_init_lock };
    g_init_done = false;

    return StatusCode::Success;
}

SHARED_API int HOSTPOLICY_CALLTYPE corehost_resolve_component_dependencies(
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
            /* init_from_file_system */ true,
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
        delete app;
        app = nullptr;
        trace::error(_X("Failed to initialize empty runtime config for the component."));
        return StatusCode::InvalidConfigFile;
    }

    // For components we don't want to resolve anything from the frameworks, since those will be supplied by the app.
    // So only use the component as the "app" framework.
    fx_definition_vector_t component_fx_definitions;
    component_fx_definitions.push_back(std::unique_ptr<fx_definition_t>(app));

    // TODO Review: Since we're only passing the one component framework, the resolver will not consider
    // frameworks from the app for probing paths. So potential references to paths inside frameworks will not resolve.

    // The RID graph still has to come from the actual root framework, so take that from the g_init.fx_definitions
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
// Multiple calls to the error writer may occur for one failure.
//
SHARED_API corehost_error_writer_fn HOSTPOLICY_CALLTYPE corehost_set_error_writer(corehost_error_writer_fn error_writer)
{
    return trace::set_error_writer(error_writer);
}
