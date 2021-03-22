// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <atomic>
#include <cassert>
#include <condition_variable>
#include <mutex>
#include <error_codes.h>
#include <pal.h>
#include <trace.h>
#include <utils.h>

#include <corehost_context_contract.h>
#include <hostpolicy.h>
#include "corehost_init.h"
#include "deps_format.h"
#include "framework_info.h"
#include "fx_definition.h"
#include "fx_muxer.h"
#include "fx_reference.h"
#include "fx_resolver.h"
#include "fx_ver.h"
#include "host_startup_info.h"
#include "hostpolicy_resolver.h"
#include "runtime_config.h"
#include "sdk_info.h"
#include "sdk_resolver.h"
#include "roll_fwd_on_no_candidate_fx_option.h"
#include "bundle/info.h"

namespace
{
    // hostfxr tracks the context used to load hostpolicy and coreclr as the active host context. This is the first
    // context that is successfully created and used to load the runtime. There can only be one active context.
    // Secondary contexts can only be created once the first context has fully initialized and been used to load the
    // runtime. Any calls to initialize another context while the runtime has not yet been loaded by the first context
    // will block until the first context loads the runtime (or fails).
    std::mutex g_context_lock;

    // Tracks the active host context. This is the context that was used to load and initialize hostpolicy and coreclr.
    // It will only be set once both hostpolicy and coreclr are loaded and initialized. Once set, it should not be changed.
    // This will remain set even if the context is closed through hostfxr_close. Since the context represents the active
    // CoreCLR runtime and the active runtime cannot be unloaded, the active context is never unset.
    std::unique_ptr<host_context_t> g_active_host_context;

    // Tracks whether the first host context is initializing (from creation of the first context to loading the runtime).
    // Initialization of other contexts should block if the first context is initializing (i.e. this is true).
    // The condition variable is used to block on and signal changes to this state.
    std::atomic<bool> g_context_initializing(false);
    std::condition_variable g_context_initializing_cv;

    void handle_initialize_failure_or_abort(const hostpolicy_contract_t *hostpolicy_contract = nullptr)
    {
        {
            std::lock_guard<std::mutex> lock{ g_context_lock };
            assert(g_context_initializing.load());
            assert(g_active_host_context == nullptr);
            g_context_initializing.store(false);
        }

        if (hostpolicy_contract != nullptr && hostpolicy_contract->unload != nullptr)
            hostpolicy_contract->unload();

        g_context_initializing_cv.notify_all();
    }
}

int load_hostpolicy(
    const pal::string_t& lib_dir,
    pal::dll_t* h_host,
    hostpolicy_contract_t& hostpolicy_contract)
{
    int rc = hostpolicy_resolver::load(lib_dir, h_host, hostpolicy_contract);
    if (rc != StatusCode::Success)
    {
        trace::error(_X("An error occurred while loading required library %s from [%s]"), LIBHOSTPOLICY_NAME, lib_dir.c_str());
        return rc;
    }

    return StatusCode::Success;
}

static int execute_app(
    const pal::string_t& impl_dll_dir,
    corehost_init_t* init,
    const int argc,
    const pal::char_t* argv[])
{
    {
        std::unique_lock<std::mutex> lock{ g_context_lock };
        g_context_initializing_cv.wait(lock, [] { return !g_context_initializing.load(); });

        if (g_active_host_context != nullptr)
        {
            trace::error(_X("Hosting components are already initialized. Re-initialization to execute an app is not allowed."));
            return StatusCode::HostInvalidState;
        }

        g_context_initializing.store(true);
    }

    pal::dll_t hostpolicy_dll;
    hostpolicy_contract_t hostpolicy_contract{};
    corehost_main_fn host_main = nullptr;

    int code = load_hostpolicy(impl_dll_dir, &hostpolicy_dll, hostpolicy_contract);

    // Obtain entrypoint symbol
    if (code == StatusCode::Success)
    {
        host_main = hostpolicy_contract.corehost_main;
        if (host_main == nullptr)
        {
            code = StatusCode::CoreHostEntryPointFailure;
        }
    }

    if (code != StatusCode::Success)
    {
        handle_initialize_failure_or_abort();
        return code;
    }

    // Leak hostpolicy - just as we do not unload coreclr, we do not unload hostpolicy

    {
        // Track an empty 'active' context so that host context-based APIs can work properly when
        // the runtime is loaded through non-host context-based APIs. Once set, the context is never
        // unset. This means that if any error occurs after this point (e.g. with loading the runtime),
        // the process will be in a corrupted state and loading the runtime again will not be allowed.
        std::lock_guard<std::mutex> lock{ g_context_lock };
        assert(g_active_host_context == nullptr);
        g_active_host_context.reset(new host_context_t(host_context_type::empty, hostpolicy_contract, {}));
        g_active_host_context->initialize_frameworks(*init);
        g_context_initializing.store(false);
    }

    g_context_initializing_cv.notify_all();

    {
        propagate_error_writer_t propagate_error_writer_to_corehost(hostpolicy_contract.set_error_writer);

        const host_interface_t& intf = init->get_host_init_data();
        if ((code = hostpolicy_contract.load(&intf)) == StatusCode::Success)
        {
            code = host_main(argc, argv);
            (void)hostpolicy_contract.unload();
        }
    }

    return code;
}

static int execute_host_command(
    const pal::string_t& impl_dll_dir,
    corehost_init_t* init,
    const int argc,
    const pal::char_t* argv[],
    pal::char_t result_buffer[],
    int32_t buffer_size,
    int32_t* required_buffer_size)
{
    pal::dll_t hostpolicy_dll;
    hostpolicy_contract_t hostpolicy_contract{};
    corehost_main_with_output_buffer_fn host_main = nullptr;

    int code = load_hostpolicy(impl_dll_dir, &hostpolicy_dll, hostpolicy_contract);

    // Obtain entrypoint symbol
    if (code == StatusCode::Success)
    {
        host_main = hostpolicy_contract.corehost_main_with_output_buffer;
        if (host_main == nullptr)
        {
            code = StatusCode::CoreHostEntryPointFailure;
        }
    }

    if (code != StatusCode::Success)
        return code;

    // Leak hostpolicy - just as we do not unload coreclr, we do not unload hostpolicy

    {
        propagate_error_writer_t propagate_error_writer_to_corehost(hostpolicy_contract.set_error_writer);

        const host_interface_t& intf = init->get_host_init_data();
        if ((code = hostpolicy_contract.load(&intf)) == StatusCode::Success)
        {
            code = host_main(argc, argv, result_buffer, buffer_size, required_buffer_size);
            (void)hostpolicy_contract.unload();
        }
    }

    return code;
}

void get_runtime_config_paths_from_arg(const pal::string_t& arg, pal::string_t* cfg, pal::string_t* dev_cfg)
{
    auto name = get_filename_without_ext(arg);

    auto json_name = name + _X(".json");
    auto dev_json_name = name + _X(".dev.json");

    auto json_path = get_directory(arg);
    auto dev_json_path = json_path;

    append_path(&json_path, json_name.c_str());
    append_path(&dev_json_path, dev_json_name.c_str());

    trace::verbose(_X("Runtime config is cfg=%s dev=%s"), json_path.c_str(), dev_json_path.c_str());

    dev_cfg->assign(dev_json_path);
    cfg->assign(json_path);
}

void get_runtime_config_paths_from_app(const pal::string_t& app, pal::string_t* cfg, pal::string_t* dev_cfg)
{
    auto name = get_filename_without_ext(app);
    auto path = get_directory(app);

    get_runtime_config_paths(path, name, cfg, dev_cfg);
}

// Convert "path" to realpath (merging working dir if needed) and append to "realpaths" out param.
void append_probe_realpath(const pal::string_t& path, std::vector<pal::string_t>* realpaths, const pal::string_t& tfm)
{
    pal::string_t probe_path = path;

    if (pal::realpath(&probe_path, true))
    {
        realpaths->push_back(probe_path);
    }
    else
    {
        // Check if we can extrapolate |arch|<DIR_SEPARATOR>|tfm| for probing stores
        // Check for for both forward and back slashes
        pal::string_t placeholder = _X("|arch|\\|tfm|");
        auto pos_placeholder = probe_path.find(placeholder);
        if (pos_placeholder == pal::string_t::npos)
        {
            placeholder = _X("|arch|/|tfm|");
            pos_placeholder = probe_path.find(placeholder);
        }

        if (pos_placeholder != pal::string_t::npos)
        {
            pal::string_t segment = get_arch();
            segment.push_back(DIR_SEPARATOR);
            segment.append(tfm);
            probe_path.replace(pos_placeholder, placeholder.length(), segment);

            if (pal::realpath(&probe_path, true))
            {
                realpaths->push_back(probe_path);
            }
            else
            {
                trace::verbose(_X("Ignoring host interpreted additional probing path %s as it does not exist."), probe_path.c_str());
            }
        }
        else
        {
            trace::verbose(_X("Ignoring additional probing path %s as it does not exist."), probe_path.c_str());
        }
    }
}

namespace
{
    int read_config(
        fx_definition_t& app,
        const pal::string_t& app_candidate,
        pal::string_t& runtime_config,
        const runtime_config_t::settings_t& override_settings)
    {
        // Check for the runtimeconfig.json file specified at the command line
        if (!runtime_config.empty() && !pal::realpath(&runtime_config))
        {
            trace::error(_X("The specified runtimeconfig.json [%s] does not exist"), runtime_config.c_str());
            return StatusCode::InvalidConfigFile;
        }

        pal::string_t config_file, dev_config_file;

        if (runtime_config.empty())
        {
            trace::verbose(_X("App runtimeconfig.json from [%s]"), app_candidate.c_str());
            get_runtime_config_paths_from_app(app_candidate, &config_file, &dev_config_file);
        }
        else
        {
            trace::verbose(_X("Specified runtimeconfig.json from [%s]"), runtime_config.c_str());
            get_runtime_config_paths_from_arg(runtime_config, &config_file, &dev_config_file);
        }

        app.parse_runtime_config(config_file, dev_config_file, override_settings);
        if (!app.get_runtime_config().is_valid())
        {
            trace::error(_X("Invalid runtimeconfig.json [%s] [%s]"), app.get_runtime_config().get_path().c_str(), app.get_runtime_config().get_dev_path().c_str());
            return StatusCode::InvalidConfigFile;
        }

        return StatusCode::Success;
    }

    host_mode_t detect_operating_mode(const host_startup_info_t& host_info)
    {
        if (bundle::info_t::is_single_file_bundle())
        {
            return host_mode_t::apphost;
        }

        if (coreclr_exists_in_dir(host_info.dotnet_root))
        {
            // Detect between standalone apphost or legacy split mode (specifying --depsfile and --runtimeconfig)

            pal::string_t deps_in_dotnet_root = host_info.dotnet_root;
            pal::string_t deps_filename = host_info.get_app_name() + _X(".deps.json");
            append_path(&deps_in_dotnet_root, deps_filename.c_str());
            bool deps_exists = pal::file_exists(deps_in_dotnet_root);

            trace::info(_X("Detecting mode... CoreCLR present in dotnet root [%s] and checking if [%s] file present=[%d]"),
                host_info.dotnet_root.c_str(), deps_filename.c_str(), deps_exists);

            // Name of runtimeconfig file; since no path is included here the check is in the current working directory
            pal::string_t config_in_cwd = host_info.get_app_name() + _X(".runtimeconfig.json");

            return (deps_exists || !pal::file_exists(config_in_cwd)) && pal::file_exists(host_info.app_path) ? host_mode_t::apphost : host_mode_t::split_fx;
        }

        if (pal::file_exists(host_info.app_path))
        {
            // Framework-dependent apphost
            return host_mode_t::apphost;
        }

        return host_mode_t::muxer;
    }

    std::vector<pal::string_t> get_probe_realpaths(
        const fx_definition_vector_t &fx_definitions,
        const std::vector<pal::string_t> &specified_probing_paths)
    {
        // The tfm is taken from the app.
        pal::string_t tfm = get_app(fx_definitions).get_runtime_config().get_tfm();

        // Append specified probe paths first and then config file probe paths into realpaths.
        std::vector<pal::string_t> probe_realpaths;
        for (const auto& path : specified_probing_paths)
        {
            append_probe_realpath(path, &probe_realpaths, tfm);
        }

        // Each framework can add probe paths
        for (const auto& fx : fx_definitions)
        {
            for (const auto& path : fx->get_runtime_config().get_probe_paths())
            {
                append_probe_realpath(path, &probe_realpaths, tfm);
            }
        }

        return probe_realpaths;
    }

    int get_init_info_for_app(
        const pal::string_t &host_command,
        const host_startup_info_t &host_info,
        const pal::string_t &app_candidate,
        const opt_map_t &opts,
        host_mode_t mode,
        /*out*/ pal::string_t &hostpolicy_dir,
        /*out*/ std::unique_ptr<corehost_init_t> &init)
    {
        pal::string_t runtime_config = command_line::get_option_value(opts, known_options::runtime_config, _X(""));

        // This check is for --depsfile option, which must be an actual file.
        pal::string_t deps_file = command_line::get_option_value(opts, known_options::deps_file, _X(""));
        if (!deps_file.empty() && !pal::realpath(&deps_file))
        {
            trace::error(_X("The specified deps.json [%s] does not exist"), deps_file.c_str());
            return StatusCode::InvalidArgFailure;
        }

        runtime_config_t::settings_t override_settings;

        // `Roll forward` is set to Minor (2) (roll_forward_option::Minor) by default.
        // For backward compatibility there are two settings:
        //  - rollForward (the new one) which has more possible values
        //  - rollForwardOnNoCandidateFx (the old one) with only 0-Off, 1-Minor, 2-Major
        // It can be changed through:
        // 1. Command line argument --roll-forward or --roll-forward-on-no-candidate-fx
        // 2. DOTNET_ROLL_FORWARD env var.
        // 3. Runtimeconfig json file ('rollForward' or 'rollForwardOnNoCandidateFx' property in "framework" section).
        // 4. Runtimeconfig json file ('rollForward' or 'rollForwardOnNoCandidateFx' property in a "runtimeOptions" section).
        // 5. DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX env var.
        // The conflicts will be resolved by following the priority rank described above (from 1 to 5, lower number wins over higher number).
        // The env var condition is verified in the config file processing

        pal::string_t roll_forward = command_line::get_option_value(opts, known_options::roll_forward, _X(""));
        if (roll_forward.length() > 0)
        {
            auto val = roll_forward_option_from_string(roll_forward);
            if (val == roll_forward_option::__Last)
            {
                trace::error(_X("Invalid value for command line argument '%s'"), command_line::get_option_name(known_options::roll_forward).c_str());
                return StatusCode::InvalidArgFailure;
            }

            override_settings.set_roll_forward(val);
        }

        pal::string_t roll_fwd_on_no_candidate_fx = command_line::get_option_value(opts, known_options::roll_forward_on_no_candidate_fx, _X(""));
        if (roll_fwd_on_no_candidate_fx.length() > 0)
        {
            if (override_settings.has_roll_forward)
            {
                trace::error(_X("It's invalid to use both '%s' and '%s' command line options."),
                    command_line::get_option_name(known_options::roll_forward).c_str(),
                    command_line::get_option_name(known_options::roll_forward_on_no_candidate_fx).c_str());
                return StatusCode::InvalidArgFailure;
            }

            auto val = static_cast<roll_fwd_on_no_candidate_fx_option>(pal::xtoi(roll_fwd_on_no_candidate_fx.c_str()));
            override_settings.set_roll_forward(roll_fwd_on_no_candidate_fx_to_roll_forward(val));
        }

        // Read config
        fx_definition_vector_t fx_definitions;
        auto app = new fx_definition_t();
        fx_definitions.push_back(std::unique_ptr<fx_definition_t>(app));
        int rc = read_config(*app, app_candidate, runtime_config, override_settings);
        if (rc != StatusCode::Success)
            return rc;

        runtime_config_t app_config = app->get_runtime_config();
        bool is_framework_dependent = app_config.get_is_framework_dependent();

        pal::string_t additional_deps_serialized;
        if (is_framework_dependent)
        {
            // Apply the --fx-version option to the first framework
            pal::string_t fx_version_specified = command_line::get_option_value(opts, known_options::fx_version, _X(""));
            if (fx_version_specified.length() > 0)
            {
                // This will also set roll forward defaults on the ref
                app_config.set_fx_version(fx_version_specified);
            }

            // Determine additional deps
            pal::string_t additional_deps = command_line::get_option_value(opts, known_options::additional_deps, _X(""));
            additional_deps_serialized = additional_deps;
            if (additional_deps_serialized.empty())
            {
                // additional_deps_serialized stays empty if DOTNET_ADDITIONAL_DEPS env var is not defined
                pal::getenv(_X("DOTNET_ADDITIONAL_DEPS"), &additional_deps_serialized);
            }

            // If invoking using FX dotnet.exe, use own directory.
            if (mode == host_mode_t::split_fx)
            {
                auto fx = new fx_definition_t(app_config.get_frameworks()[0].get_fx_name(), host_info.dotnet_root, pal::string_t(), pal::string_t());
                fx_definitions.push_back(std::unique_ptr<fx_definition_t>(fx));
            }
            else
            {
                rc = fx_resolver_t::resolve_frameworks_for_app(host_info, override_settings, app_config, fx_definitions);
                if (rc != StatusCode::Success)
                {
                    return rc;
                }
            }
        }

        const known_options opts_probe_path = known_options::additional_probing_path;
        std::vector<pal::string_t> spec_probe_paths = opts.count(opts_probe_path) ? opts.find(opts_probe_path)->second : std::vector<pal::string_t>();
        std::vector<pal::string_t> probe_realpaths = get_probe_realpaths(fx_definitions, spec_probe_paths);

        trace::verbose(_X("Executing as a %s app as per config file [%s]"),
            (is_framework_dependent ? _X("framework-dependent") : _X("self-contained")), app_config.get_path().c_str());

        if (!hostpolicy_resolver::try_get_dir(mode, host_info.dotnet_root, fx_definitions, app_candidate, deps_file, probe_realpaths, &hostpolicy_dir))
        {
            return StatusCode::CoreHostLibMissingFailure;
        }

        init.reset(new corehost_init_t(host_command, host_info, deps_file, additional_deps_serialized, probe_realpaths, mode, fx_definitions));

        return StatusCode::Success;
    }

    int read_config_and_execute(
        const pal::string_t& host_command,
        const host_startup_info_t& host_info,
        const pal::string_t& app_candidate,
        const opt_map_t& opts,
        int new_argc,
        const pal::char_t** new_argv,
        host_mode_t mode,
        pal::char_t out_buffer[],
        int32_t buffer_size,
        int32_t* required_buffer_size)
    {
        pal::string_t hostpolicy_dir;
        std::unique_ptr<corehost_init_t> init;
        int rc = get_init_info_for_app(
            host_command,
            host_info,
            app_candidate,
            opts,
            mode,
            hostpolicy_dir,
            init);
        if (rc != StatusCode::Success)
            return rc;

        if (host_command.size() == 0)
        {
            rc = execute_app(hostpolicy_dir, init.get(), new_argc, new_argv);
        }
        else
        {
            rc = execute_host_command(hostpolicy_dir, init.get(), new_argc, new_argv, out_buffer, buffer_size, required_buffer_size);
        }

        return rc;
    }
}

/**
*  Main entrypoint to detect operating mode and perform corehost, muxer,
*  standalone application activation and the SDK activation.
*/
int fx_muxer_t::execute(
    const pal::string_t host_command,
    const int argc,
    const pal::char_t* argv[],
    const host_startup_info_t& host_info,
    pal::char_t result_buffer[],
    int32_t buffer_size,
    int32_t* required_buffer_size)
{
    // Detect invocation mode
    host_mode_t mode = detect_operating_mode(host_info);

    int new_argoff;
    pal::string_t app_candidate;
    opt_map_t opts;
    int result = command_line::parse_args_for_mode(mode, host_info, argc, argv, &new_argoff, app_candidate, opts);
    if (static_cast<StatusCode>(result) == AppArgNotRunnable)
    {
        if (host_command.empty())
        {
            return handle_cli(host_info, argc, argv, app_candidate);
        }
        else
        {
            return result;
        }
    }

    if (!result)
    {
        // Transform dotnet [exec] [--additionalprobingpath path] [--depsfile file] [dll] [args] -> dotnet [dll] [args]
        result = handle_exec_host_command(
            host_command,
            host_info,
            app_candidate,
            opts,
            argc,
            argv,
            new_argoff,
            mode,
            result_buffer,
            buffer_size,
            required_buffer_size);
    }

    return result;
}

namespace
{
    int get_init_info_for_component(
        const host_startup_info_t &host_info,
        host_mode_t mode,
        pal::string_t &runtime_config_path,
        /*out*/ pal::string_t &hostpolicy_dir,
        /*out*/ std::unique_ptr<corehost_init_t> &init)
    {
        // Read config
        fx_definition_vector_t fx_definitions;
        auto app = new fx_definition_t();
        fx_definitions.push_back(std::unique_ptr<fx_definition_t>(app));

        const runtime_config_t::settings_t override_settings;
        int rc = read_config(*app, host_info.app_path, runtime_config_path, override_settings);
        if (rc != StatusCode::Success)
            return rc;

        const runtime_config_t app_config = app->get_runtime_config();
        if (!app_config.get_is_framework_dependent())
        {
            trace::error(_X("Initialization for self-contained components is not supported"));
            return StatusCode::InvalidConfigFile;
        }

        rc = fx_resolver_t::resolve_frameworks_for_app(host_info, override_settings, app_config, fx_definitions);
        if (rc != StatusCode::Success)
            return rc;

        const std::vector<pal::string_t> probe_realpaths = get_probe_realpaths(fx_definitions, std::vector<pal::string_t>() /* specified_probing_paths */);

        trace::verbose(_X("Libhost loading occurring for a framework-dependent component per config file [%s]"), app_config.get_path().c_str());

        const pal::string_t deps_file;
        if (!hostpolicy_resolver::try_get_dir(mode, host_info.dotnet_root, fx_definitions, host_info.app_path, deps_file, probe_realpaths, &hostpolicy_dir))
        {
            return StatusCode::CoreHostLibMissingFailure;
        }

        const pal::string_t additional_deps_serialized;
        init.reset(new corehost_init_t(pal::string_t{}, host_info, deps_file, additional_deps_serialized, probe_realpaths, mode, fx_definitions));

        return StatusCode::Success;
    }

    int get_init_info_for_secondary_component(
        const host_startup_info_t& host_info,
        host_mode_t mode,
        pal::string_t& runtime_config_path,
        const host_context_t* existing_context,
        /*out*/ std::unordered_map<pal::string_t, pal::string_t>& config_properties)
    {
        // Read config
        fx_definition_t app;
        const runtime_config_t::settings_t override_settings;
        int rc = read_config(app, host_info.app_path, runtime_config_path, override_settings);
        if (rc != StatusCode::Success)
            return rc;

        const runtime_config_t app_config = app.get_runtime_config();
        if (!app_config.get_is_framework_dependent())
        {
            trace::error(_X("Initialization for self-contained components is not supported"));
            return StatusCode::InvalidConfigFile;
        }

        // Validate the current context is acceptable for this request (frameworks)
        if (!existing_context->fx_versions_by_name.empty())
        {
            // Framework dependent apps always know their frameworks
            if (!fx_resolver_t::is_config_compatible_with_frameworks(app_config, existing_context->fx_versions_by_name))
                return StatusCode::CoreHostIncompatibleConfig;
        }
        else if (!existing_context->included_fx_versions_by_name.empty())
        {
            // Self-contained apps can include information about their frameworks in `includedFrameworks` property in runtime config
            if (!fx_resolver_t::is_config_compatible_with_frameworks(app_config, existing_context->included_fx_versions_by_name))
                return StatusCode::CoreHostIncompatibleConfig;
        }
        else
        {
            trace::verbose(_X("Skipped framework validation for loading a component in a self-contained app without information about included frameworks"));
        }

        app_config.combine_properties(config_properties);
        return StatusCode::Success;
    }

    int initialize_context(
        const pal::string_t hostpolicy_dir,
        corehost_init_t &init,
        int32_t initialize_options,
        /*out*/ std::unique_ptr<host_context_t> &context)
    {
        pal::dll_t hostpolicy_dll;
        hostpolicy_contract_t hostpolicy_contract{};
        int rc = hostpolicy_resolver::load(hostpolicy_dir, &hostpolicy_dll, hostpolicy_contract);
        if (rc != StatusCode::Success)
        {
            trace::error(_X("An error occurred while loading required library %s from [%s]"), LIBHOSTPOLICY_NAME, hostpolicy_dir.c_str());
        }
        else
        {
            rc = host_context_t::create(hostpolicy_contract, init, initialize_options, context);
        }

        // Leak hostpolicy - just as we do not unload coreclr, we do not unload hostpolicy

        if (rc != StatusCode::Success)
            handle_initialize_failure_or_abort(&hostpolicy_contract);

        return rc;
    }
}

int fx_muxer_t::initialize_for_app(
    const host_startup_info_t &host_info,
    int argc,
    const pal::char_t* argv[],
    const opt_map_t &opts,
    hostfxr_handle *host_context_handle)
{
    {
        std::unique_lock<std::mutex> lock{ g_context_lock };
        g_context_initializing_cv.wait(lock, [] { return !g_context_initializing.load(); });

        if (g_active_host_context != nullptr)
        {
            trace::error(_X("Hosting components are already initialized. Re-initialization for an app is not allowed."));
            return StatusCode::HostInvalidState;
        }

        g_context_initializing.store(true);
    }

    host_mode_t mode = host_mode_t::apphost;
    pal::string_t hostpolicy_dir;
    std::unique_ptr<corehost_init_t> init;
    int rc = get_init_info_for_app(
        pal::string_t{} /*host_command*/,
        host_info,
        host_info.app_path,
        opts,
        mode,
        hostpolicy_dir,
        init);
    if (rc != StatusCode::Success)
    {
        handle_initialize_failure_or_abort();
        return rc;
    }

    std::unique_ptr<host_context_t> context;
    rc = initialize_context(hostpolicy_dir, *init, initialization_options_t::none, context);
    if (rc != StatusCode::Success)
    {
        trace::error(_X("Failed to initialize context for app: %s. Error code: 0x%x"), host_info.app_path.c_str(), rc);
        return rc;
    }

    context->is_app = true;
    for (int i = 0; i < argc; ++i)
        context->argv.push_back(argv[i]);

    trace::info(_X("Initialized context for app: %s"), host_info.app_path.c_str());
    *host_context_handle = context.release();
    return rc;
}

int fx_muxer_t::initialize_for_runtime_config(
    const host_startup_info_t &host_info,
    const pal::char_t *runtime_config_path,
    hostfxr_handle *host_context_handle)
{
    uint32_t initialization_options = initialization_options_t::none;
    const host_context_t *existing_context;
    {
        std::unique_lock<std::mutex> lock{ g_context_lock };
        g_context_initializing_cv.wait(lock, [] { return !g_context_initializing.load(); });

        existing_context = g_active_host_context.get();
        if (existing_context == nullptr)
        {
            g_context_initializing.store(true);
        }
        else if (existing_context->type == host_context_type::invalid)
        {
            return StatusCode::HostInvalidState;
        }
        else if (existing_context->type == host_context_type::empty)
        {
            initialization_options |= initialization_options_t::wait_for_initialized;
        }
    }

    bool already_initialized = existing_context != nullptr;

    int rc;
    host_mode_t mode = host_mode_t::libhost;
    pal::string_t runtime_config = runtime_config_path;
    std::unique_ptr<host_context_t> context;
    if (already_initialized)
    {
        std::unordered_map<pal::string_t, pal::string_t> config_properties;
        rc = get_init_info_for_secondary_component(host_info, mode, runtime_config, existing_context, config_properties);
        if (rc != StatusCode::Success)
            return rc;

        rc = host_context_t::create_secondary(existing_context->hostpolicy_contract, config_properties, initialization_options, context);
    }
    else
    {
        pal::string_t hostpolicy_dir;
        std::unique_ptr<corehost_init_t> init;
        rc = get_init_info_for_component(host_info, mode, runtime_config, hostpolicy_dir, init);
        if (rc != StatusCode::Success)
        {
            handle_initialize_failure_or_abort();
            return rc;
        }

        rc = initialize_context(hostpolicy_dir, *init, initialization_options, context);
    }

    if (!STATUS_CODE_SUCCEEDED(rc))
    {
        trace::error(_X("Failed to initialize context for config: %s. Error code: 0x%x"), runtime_config_path, rc);
        return rc;
    }

    context->is_app = false;

    trace::info(_X("Initialized %s for config: %s"), already_initialized ? _X("secondary context") : _X("context"), runtime_config_path);
    *host_context_handle = context.release();
    return rc;
}

namespace
{
    int load_runtime(host_context_t *context)
    {
        assert(context->type == host_context_type::initialized || context->type == host_context_type::active);
        if (context->type == host_context_type::active)
            return StatusCode::Success;

        const corehost_context_contract &contract = context->hostpolicy_context_contract;
        int rc = contract.load_runtime();

        // Mark the context as active or invalid
        context->type = rc == StatusCode::Success ? host_context_type::active : host_context_type::invalid;

        {
            std::lock_guard<std::mutex> lock{ g_context_lock };
            assert(g_active_host_context == nullptr);
            g_active_host_context.reset(context);
            g_context_initializing.store(false);
        }

        g_context_initializing_cv.notify_all();
        return rc;
    }
}

int fx_muxer_t::run_app(host_context_t *context)
{
    if (!context->is_app)
        return StatusCode::InvalidArgFailure;

    size_t argc = context->argv.size();
    std::vector<const pal::char_t*> argv;
    argv.reserve(argc);
    for (const auto& str : context->argv)
        argv.push_back(str.c_str());

    const corehost_context_contract &contract = context->hostpolicy_context_contract;
    {
        propagate_error_writer_t propagate_error_writer_to_corehost(context->hostpolicy_contract.set_error_writer);

        int rc = load_runtime(context);
        if (rc != StatusCode::Success)
            return rc;

        return contract.run_app((int32_t)argc, argv.data());
    }
}

int fx_muxer_t::get_runtime_delegate(host_context_t *context, coreclr_delegate_type type, void **delegate)
{
    switch (type)
    {
    case coreclr_delegate_type::com_activation:
    case coreclr_delegate_type::load_in_memory_assembly:
    case coreclr_delegate_type::winrt_activation:
    case coreclr_delegate_type::com_register:
    case coreclr_delegate_type::com_unregister:
        if (context->is_app)
            return StatusCode::HostApiUnsupportedScenario;
        break;
    default:
        // Always allowed
        break;
    }

    // last_known_delegate_type was added in 5.0, so old versions won't set it and it will be zero.
    // But when get_runtime_delegate was originally implemented in 3.0,
    // it supported up to load_assembly_and_get_function_pointer so we check that first.
    if (type > coreclr_delegate_type::load_assembly_and_get_function_pointer
        && (size_t)type > context->hostpolicy_context_contract.last_known_delegate_type)
    {
        trace::error(_X("The requested delegate type is not available in the target framework."));
        return StatusCode::HostApiUnsupportedVersion;
    }

    const corehost_context_contract &contract = context->hostpolicy_context_contract;
    {
        propagate_error_writer_t propagate_error_writer_to_corehost(context->hostpolicy_contract.set_error_writer);

        if (context->type != host_context_type::secondary)
        {
            int rc = load_runtime(context);
            if (rc != StatusCode::Success)
                return rc;
        }

        return contract.get_runtime_delegate(type, delegate);
    }
}

const host_context_t* fx_muxer_t::get_active_host_context()
{
    std::lock_guard<std::mutex> lock{ g_context_lock };
    if (g_active_host_context == nullptr)
        return nullptr;

    if (g_active_host_context->type == host_context_type::active)
        return g_active_host_context.get();

    if (g_active_host_context->type != host_context_type::empty)
        return nullptr;

    // Try to populate the contract for the 'empty' active context (i.e. created through non-context-based APIs)
    const hostpolicy_contract_t &hostpolicy_contract = g_active_host_context->hostpolicy_contract;
    if (hostpolicy_contract.initialize == nullptr)
    {
        trace::warning(_X("Getting the contract for the initialized hostpolicy is only supprted for .NET Core 3.0 or a higher version."));
        return nullptr;
    }

    corehost_context_contract hostpolicy_context_contract = {};
    {
        hostpolicy_context_contract.version = sizeof(corehost_context_contract);
        propagate_error_writer_t propagate_error_writer_to_corehost(hostpolicy_contract.set_error_writer);
        uint32_t options = initialization_options_t::get_contract | initialization_options_t::context_contract_version_set;
        int rc = hostpolicy_contract.initialize(nullptr, options, &hostpolicy_context_contract);
        if (rc != StatusCode::Success)
        {
            trace::error(_X("Failed to get contract for existing initialized hostpolicy: 0x%x"), rc);
            return nullptr;
        }
    }

    // Set the hostpolicy context contract on the active host context and mark it as active
    g_active_host_context->hostpolicy_context_contract = hostpolicy_context_contract;
    g_active_host_context->type = host_context_type::active;
    return g_active_host_context.get();
}

int fx_muxer_t::close_host_context(host_context_t *context)
{
    if (context->type == host_context_type::initialized)
    {
        // The first context is being closed without being used to start the runtime
        assert(g_active_host_context == nullptr);
        handle_initialize_failure_or_abort(&context->hostpolicy_contract);
    }

    context->close();

    // Do not delete the active context.
    {
        std::lock_guard<std::mutex> lock{ g_context_lock };
        if (context != g_active_host_context.get())
            delete context;
    }

    return StatusCode::Success;
}

int fx_muxer_t::handle_exec_host_command(
    const pal::string_t& host_command,
    const host_startup_info_t& host_info,
    const pal::string_t& app_candidate,
    const opt_map_t& opts,
    int argc,
    const pal::char_t* argv[],
    int argoff,
    host_mode_t mode,
    pal::char_t result_buffer[],
    int32_t buffer_size,
    int32_t* required_buffer_size)
{
    const pal::char_t** new_argv = argv;
    int new_argc = argc;
    std::vector<const pal::char_t*> vec_argv;

    if (argoff != 1)
    {
        vec_argv.reserve(argc - argoff + 1); // +1 for dotnet
        vec_argv.push_back(argv[0]);
        vec_argv.insert(vec_argv.end(), argv + argoff, argv + argc);
        new_argv = vec_argv.data();
        new_argc = (int32_t)vec_argv.size();
    }

    trace::info(_X("Using dotnet root path [%s]"), host_info.dotnet_root.c_str());

    // Transform dotnet [exec] [--additionalprobingpath path] [--depsfile file] [dll] [args] -> dotnet [dll] [args]
    return read_config_and_execute(
        host_command,
        host_info,
        app_candidate,
        opts,
        new_argc,
        new_argv,
        mode,
        result_buffer,
        buffer_size,
        required_buffer_size);
}

int fx_muxer_t::handle_cli(
    const host_startup_info_t& host_info,
    int argc,
    const pal::char_t* argv[],
    const pal::string_t& app_candidate)
{
    // Check for commands that don't depend on the CLI SDK to be loaded
    if (pal::strcasecmp(_X("--list-sdks"), argv[1]) == 0)
    {
        sdk_info::print_all_sdks(host_info.dotnet_root, _X(""));
        return StatusCode::Success;
    }
    else if (pal::strcasecmp(_X("--list-runtimes"), argv[1]) == 0)
    {
        framework_info::print_all_frameworks(host_info.dotnet_root, _X(""));
        return StatusCode::Success;
    }

    //
    // Did not execute the app or run other commands, so try the CLI SDK dotnet.dll
    //

    sdk_resolver resolver = sdk_resolver::from_nearest_global_file();
    auto sdk_dotnet = resolver.resolve(host_info.dotnet_root, false /*print_errors*/);
    if (sdk_dotnet.empty())
    {
        assert(argc > 1);
        if (pal::strcasecmp(_X("-h"), argv[1]) == 0 ||
            pal::strcasecmp(_X("--help"), argv[1]) == 0 ||
            pal::strcasecmp(_X("-?"), argv[1]) == 0 ||
            pal::strcasecmp(_X("/?"), argv[1]) == 0)
        {
            command_line::print_muxer_usage(false);
            return StatusCode::InvalidArgFailure;
        }
        else if (pal::strcasecmp(_X("--info"), argv[1]) == 0)
        {
            command_line::print_muxer_info(host_info.dotnet_root);
            return StatusCode::Success;
        }

        trace::error(_X("Could not execute because the application was not found or a compatible .NET SDK is not installed."));
        trace::error(_X("Possible reasons for this include:"));
        trace::error(_X("  * You intended to execute a .NET program:"));
        trace::error(_X("      The application '%s' does not exist."), app_candidate.c_str());
        trace::error(_X("  * You intended to execute a .NET SDK command:"));
        resolver.print_resolution_error(host_info.dotnet_root, _X("      "));

        return StatusCode::LibHostSdkFindFailure;
    }

    append_path(&sdk_dotnet, _X("dotnet.dll"));

    if (!pal::file_exists(sdk_dotnet))
    {
        trace::error(_X("Found .NET SDK, but did not find dotnet.dll at [%s]"), sdk_dotnet.c_str());
        return StatusCode::LibHostSdkFindFailure;
    }

    // Transform dotnet [command] [args] -> dotnet dotnet.dll [command] [args]

    std::vector<const pal::char_t*> new_argv;
    new_argv.reserve(argc + 1);
    new_argv.push_back(argv[0]);
    new_argv.push_back(sdk_dotnet.c_str());
    new_argv.insert(new_argv.end(), argv + 1, argv + argc);

    trace::verbose(_X("Using .NET SDK dll=[%s]"), sdk_dotnet.c_str());

    int new_argoff;
    pal::string_t sdk_app_candidate;
    opt_map_t opts;
    int result = command_line::parse_args_for_sdk_command(host_info, (int32_t)new_argv.size(), new_argv.data(), &new_argoff, sdk_app_candidate, opts);
    if (!result)
    {
        // Transform dotnet [exec] [--additionalprobingpath path] [--depsfile file] [dll] [args] -> dotnet [dll] [args]
        result = handle_exec_host_command(
            pal::string_t{} /*host_command*/,
            host_info,
            sdk_app_candidate,
            opts,
            (int32_t)new_argv.size(),
            new_argv.data(),
            new_argoff,
            host_mode_t::muxer,
            nullptr /*result_buffer*/,
            0 /*buffer_size*/,
            nullptr/*required_buffer_size*/);
    }

    if (pal::strcasecmp(_X("--info"), argv[1]) == 0)
    {
        command_line::print_muxer_info(host_info.dotnet_root);
    }

    return result;
}
