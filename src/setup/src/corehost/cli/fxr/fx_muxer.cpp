// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <cassert>
#include <mutex>
#include <error_codes.h>
#include <pal.h>
#include <trace.h>
#include <utils.h>

#include <cpprest/json.h>
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

using corehost_main_fn = int(*) (const int argc, const pal::char_t* argv[]);
using corehost_get_delegate_fn = int(*)(coreclr_delegate_type type, void** delegate);
using corehost_main_with_output_buffer_fn = int(*) (const int argc, const pal::char_t* argv[], pal::char_t buffer[], int32_t buffer_size, int32_t* required_buffer_size);

template<typename T>
int load_hostpolicy(
    const pal::string_t& lib_dir,
    pal::dll_t* h_host,
    hostpolicy_contract &host_contract,
    const char *main_entry_symbol,
    T* main_fn)
{
    assert(main_entry_symbol != nullptr && main_fn != nullptr);

    int rc = hostpolicy_resolver::load(lib_dir, h_host, host_contract);
    if (rc != StatusCode::Success)
    {
        trace::error(_X("An error occurred while loading required library %s from [%s]"), LIBHOSTPOLICY_NAME, lib_dir.c_str());
        return rc;
    }

    // Obtain entrypoint symbol
    *main_fn = (T)pal::get_symbol(*h_host, main_entry_symbol);
    if (*main_fn == nullptr)
        return StatusCode::CoreHostEntryPointFailure;

    return StatusCode::Success;
}

static int execute_app(
    const pal::string_t& impl_dll_dir,
    corehost_init_t* init,
    const int argc,
    const pal::char_t* argv[])
{
    pal::dll_t corehost;
    hostpolicy_contract host_contract{};
    corehost_main_fn host_main = nullptr;

    int code = load_hostpolicy(impl_dll_dir, &corehost, host_contract, "corehost_main", &host_main);
    if (code != StatusCode::Success)
        return code;

    // Previous hostfxr trace messages must be printed before calling trace::setup in hostpolicy
    trace::flush();

    {
        propagate_error_writer_t propagate_error_writer_to_corehost(host_contract.set_error_writer);

        const host_interface_t& intf = init->get_host_init_data();
        if ((code = host_contract.load(&intf)) == StatusCode::Success)
        {
            code = host_main(argc, argv);
            (void)host_contract.unload();
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
    pal::dll_t corehost;
    hostpolicy_contract host_contract{};
    corehost_main_with_output_buffer_fn host_main = nullptr;

    int code = load_hostpolicy(impl_dll_dir, &corehost, host_contract, "corehost_main_with_output_buffer", &host_main);
    if (code != StatusCode::Success)
        return code;

    // Previous hostfxr trace messages must be printed before calling trace::setup in hostpolicy
    trace::flush();

    {
        propagate_error_writer_t propagate_error_writer_to_corehost(host_contract.set_error_writer);

        const host_interface_t& intf = init->get_host_init_data();
        if ((code = host_contract.load(&intf)) == StatusCode::Success)
        {
            code = host_main(argc, argv, result_buffer, buffer_size, required_buffer_size);
            (void)host_contract.unload();
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

bool is_sdk_dir_present(const pal::string_t& dotnet_root)
{
    pal::string_t sdk_path = dotnet_root;
    append_path(&sdk_path, _X("sdk"));
    return pal::directory_exists(sdk_path);
}

void muxer_info(pal::string_t dotnet_root)
{
    trace::println();
    trace::println(_X("Host (useful for support):"));
    trace::println(_X("  Version: %s"), _STRINGIFY(HOST_FXR_PKG_VER));

    pal::string_t commit = _STRINGIFY(REPO_COMMIT_HASH);
    trace::println(_X("  Commit:  %s"), commit.substr(0, 10).c_str());

    trace::println();
    trace::println(_X(".NET Core SDKs installed:"));
    if (!sdk_info::print_all_sdks(dotnet_root, _X("  ")))
    {
        trace::println(_X("  No SDKs were found."));
    }

    trace::println();
    trace::println(_X(".NET Core runtimes installed:"));
    if (!framework_info::print_all_frameworks(dotnet_root, _X("  ")))
    {
        trace::println(_X("  No runtimes were found."));
    }

    trace::println();
    trace::println(_X("To install additional .NET Core runtimes or SDKs:"));
    trace::println(_X("  %s"), DOTNET_CORE_DOWNLOAD_URL);
}

void fx_muxer_t::muxer_usage(bool is_sdk_present)
{
    std::vector<host_option> known_opts = fx_muxer_t::get_known_opts(true, host_mode_t::invalid, true);

    if (!is_sdk_present)
    {
        trace::println();
        trace::println(_X("Usage: dotnet [host-options] [path-to-application]"));
        trace::println();
        trace::println(_X("path-to-application:"));
        trace::println(_X("  The path to an application .dll file to execute."));
    }
    trace::println();
    trace::println(_X("host-options:"));

    for (const auto& arg : known_opts)
    {
        trace::println(_X("  %-37s  %s"), (arg.option + _X(" ") + arg.argument).c_str(), arg.description.c_str());
    }
    trace::println(_X("  --list-runtimes                        Display the installed runtimes"));
    trace::println(_X("  --list-sdks                            Display the installed SDKs"));

    if (!is_sdk_present)
    {
        trace::println();
        trace::println(_X("Common Options:"));
        trace::println(_X("  -h|--help                           Displays this help."));
        trace::println(_X("  --info                              Display .NET Core information."));
    }
}

// Convert "path" to realpath (merging working dir if needed) and append to "realpaths" out param.
void append_probe_realpath(const pal::string_t& path, std::vector<pal::string_t>* realpaths, const pal::string_t& tfm)
{
    pal::string_t probe_path = path;

    if (pal::realpath(&probe_path))
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

            if (pal::realpath(&probe_path))
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

std::vector<host_option> fx_muxer_t::get_known_opts(bool exec_mode, host_mode_t mode, bool get_all_options)
{
    std::vector<host_option> known_opts = { { _X("--additionalprobingpath"), _X("<path>"), _X("Path containing probing policy and assemblies to probe for.") } };
    if (get_all_options || exec_mode || mode == host_mode_t::split_fx || mode == host_mode_t::apphost)
    {
        known_opts.push_back({ _X("--depsfile"), _X("<path>"), _X("Path to <application>.deps.json file.")});
        known_opts.push_back({ _X("--runtimeconfig"), _X("<path>"), _X("Path to <application>.runtimeconfig.json file.")});
    }

    if (get_all_options || mode == host_mode_t::muxer || mode == host_mode_t::apphost)
    {
        // If mode=host_mode_t::apphost, these are only used when the app is framework-dependent.
        known_opts.push_back({ _X("--fx-version"), _X("<version>"), _X("Version of the installed Shared Framework to use to run the application.")});
        known_opts.push_back({ _X("--roll-forward"), _X("<value>"), _X("Roll forward to framework version (LatestPatch, Minor, LatestMinor, Major, LatestMajor, Disable)")});
        known_opts.push_back({ _X("--roll-forward-on-no-candidate-fx"), _X("<n>"), _X("Roll forward on no candidate framework (0=off, 1=roll minor, 2=roll major & minor).")});
        known_opts.push_back({ _X("--additional-deps"), _X("<path>"), _X("Path to additional deps.json file.")});
    }

    return known_opts;
}

// Returns '0' on success, 'AppArgNotRunnable' if should be routed to CLI, otherwise error code.
int fx_muxer_t::parse_args(
    const host_startup_info_t& host_info,
    int argoff,
    int argc,
    const pal::char_t* argv[],
    bool exec_mode,
    host_mode_t mode,
    int* new_argoff,
    pal::string_t& app_candidate,
    opt_map_t& opts)
{
    std::vector<host_option> known_opts = get_known_opts(exec_mode, mode);

    // Parse the known arguments if any.
    int num_parsed = 0;
    if (!parse_known_args(argc - argoff, &argv[argoff], known_opts, &opts, &num_parsed))
    {
        trace::error(_X("Failed to parse supported options or their values:"));
        for (const auto& arg : known_opts)
        {
            trace::error(_X("  %-37s  %s"), (arg.option + _X(" ") + arg.argument).c_str(), arg.description.c_str());
        }
        return StatusCode::InvalidArgFailure;
    }

    app_candidate = host_info.app_path;
    *new_argoff = argoff + num_parsed;
    bool doesAppExist = false;
    if (mode == host_mode_t::apphost)
    {
        doesAppExist = pal::realpath(&app_candidate);
    }
    else
    {
        trace::verbose(_X("Using the provided arguments to determine the application to execute."));
        if (*new_argoff >= argc)
        {
            muxer_usage(!is_sdk_dir_present(host_info.dotnet_root));
            return StatusCode::InvalidArgFailure;
        }

        app_candidate = argv[*new_argoff];

        bool is_app_managed = ends_with(app_candidate, _X(".dll"), false) || ends_with(app_candidate, _X(".exe"), false);
        if (!is_app_managed)
        {
            trace::verbose(_X("Application '%s' is not a managed executable."), app_candidate.c_str());
            if (!exec_mode)
            {
                // Route to CLI.
                return StatusCode::AppArgNotRunnable;
            }
        }

        doesAppExist = pal::realpath(&app_candidate);
        if (!doesAppExist)
        {
            trace::verbose(_X("Application '%s' does not exist."), app_candidate.c_str());
            if (!exec_mode)
            {
                // Route to CLI.
                return StatusCode::AppArgNotRunnable;
            }
        }

        if (!is_app_managed && doesAppExist)
        {
            assert(exec_mode == true);
            trace::error(_X("dotnet exec needs a managed .dll or .exe extension. The application specified was '%s'"), app_candidate.c_str());
            return StatusCode::InvalidArgFailure;
        }
    }

    // App is managed executable.
    if (!doesAppExist)
    {
        trace::error(_X("The application to execute does not exist: '%s'"), app_candidate.c_str());
        return StatusCode::InvalidArgFailure;
    }

    return StatusCode::Success;
}

namespace
{
    int read_config(
        fx_definition_t& app,
        const pal::string_t& app_candidate,
        pal::string_t& runtime_config,
        const runtime_config_t::settings_t& override_settings)
    {
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
        pal::string_t &impl_dir,
        std::unique_ptr<corehost_init_t> &init)
    {
        pal::string_t opts_fx_version = _X("--fx-version");
        pal::string_t opts_roll_forward = _X("--roll-forward");
        pal::string_t opts_roll_fwd_on_no_candidate_fx = _X("--roll-forward-on-no-candidate-fx");
        pal::string_t opts_deps_file = _X("--depsfile");
        pal::string_t opts_probe_path = _X("--additionalprobingpath");
        pal::string_t opts_additional_deps = _X("--additional-deps");
        pal::string_t opts_runtime_config = _X("--runtimeconfig");

        pal::string_t runtime_config = get_last_known_arg(opts, opts_runtime_config, _X(""));

        pal::string_t deps_file = get_last_known_arg(opts, opts_deps_file, _X(""));
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

        pal::string_t roll_forward = get_last_known_arg(opts, opts_roll_forward, _X(""));
        if (roll_forward.length() > 0)
        {
            auto val = roll_forward_option_from_string(roll_forward);
            if (val == roll_forward_option::__Last)
            {
                trace::error(_X("Invalid value for command line argument '%s'"), opts_roll_forward.c_str());
                return StatusCode::InvalidArgFailure;
            }

            override_settings.set_roll_forward(val);
        }

        pal::string_t roll_fwd_on_no_candidate_fx = get_last_known_arg(opts, opts_roll_fwd_on_no_candidate_fx, _X(""));
        if (roll_fwd_on_no_candidate_fx.length() > 0)
        {
            if (override_settings.has_roll_forward)
            {
                trace::error(_X("It's invalid to use both '%s' and '%s' command line options."), opts_roll_forward.c_str(), opts_roll_fwd_on_no_candidate_fx.c_str());
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
            pal::string_t fx_version_specified = get_last_known_arg(opts, opts_fx_version, _X(""));
            if (fx_version_specified.length() > 0)
            {
                // This will also set roll forward defaults on the ref
                app_config.set_fx_version(fx_version_specified);
            }

            // Determine additional deps
            pal::string_t additional_deps = get_last_known_arg(opts, opts_additional_deps, _X(""));
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

        std::vector<pal::string_t> spec_probe_paths = opts.count(opts_probe_path) ? opts.find(opts_probe_path)->second : std::vector<pal::string_t>();
        std::vector<pal::string_t> probe_realpaths = get_probe_realpaths(fx_definitions, spec_probe_paths);

        trace::verbose(_X("Executing as a %s app as per config file [%s]"),
            (is_framework_dependent ? _X("framework-dependent") : _X("self-contained")), app_config.get_path().c_str());

        if (!hostpolicy_resolver::try_get_dir(mode, host_info.dotnet_root, fx_definitions, app_candidate, deps_file, probe_realpaths, &impl_dir))
        {
            return CoreHostLibMissingFailure;
        }

        init.reset(new corehost_init_t(host_command, host_info, deps_file, additional_deps_serialized, probe_realpaths, mode, fx_definitions));

        return StatusCode::Success;
    }
}

int fx_muxer_t::read_config_and_execute(
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
    pal::string_t impl_dir;
    std::unique_ptr<corehost_init_t> init;
    int rc = get_init_info_for_app(
        host_command,
        host_info,
        app_candidate,
        opts,
        mode,
        impl_dir,
        init);
    if (rc != StatusCode::Success)
        return rc;

    if (host_command.size() == 0)
    {
        rc = execute_app(impl_dir, init.get(), new_argc, new_argv);
    }
    else
    {
        rc = execute_host_command(impl_dir, init.get(), new_argc, new_argv, out_buffer, buffer_size, required_buffer_size);
    }

    return rc;
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
    int result;

    if (mode == host_mode_t::split_fx)
    {
        // Invoked as corehost
        trace::verbose(_X("--- Executing in split/FX mode..."));
        result = parse_args(host_info, 1, argc, argv, false, mode, &new_argoff, app_candidate, opts);
    }
    else if (mode == host_mode_t::apphost)
    {
        // Invoked from the application base.
        trace::verbose(_X("--- Executing in a native executable mode..."));
        result = parse_args(host_info, 1, argc, argv, false, mode, &new_argoff, app_candidate, opts);
    }
    else
    {
        // Invoked as the dotnet.exe muxer.
        assert(mode == host_mode_t::muxer);
        trace::verbose(_X("--- Executing in muxer mode..."));

        if (argc <= 1)
        {
            muxer_usage(!is_sdk_dir_present(host_info.dotnet_root));
            return StatusCode::InvalidArgFailure;
        }

        if (pal::strcasecmp(_X("exec"), argv[1]) == 0)
        {
            result = parse_args(host_info, 2, argc, argv, true, mode, &new_argoff, app_candidate, opts); // arg offset 2 for dotnet, exec
        }
        else
        {
            result = parse_args(host_info, 1, argc, argv, false, mode, &new_argoff, app_candidate, opts); // arg offset 1 for dotnet

            if (result == AppArgNotRunnable)
            {
                return handle_cli(host_info, argc, argv);
            }
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
    int get_delegate_from_runtime(
        const pal::string_t& impl_dll_dir,
        corehost_init_t* init,
        coreclr_delegate_type type,
        void** delegate)
    {
        pal::dll_t corehost;
        hostpolicy_contract host_contract{};
        corehost_get_delegate_fn coreclr_delegate = nullptr;

        int code = load_hostpolicy(impl_dll_dir, &corehost, host_contract, "corehost_get_coreclr_delegate", &coreclr_delegate);
        if (code != StatusCode::Success)
        {
            trace::error(_X("This component must target .NET Core 3.0 or a higher version."));
            return code;
        }

        // Previous hostfxr trace messages must be printed before calling trace::setup in hostpolicy
        trace::flush();

        {
            propagate_error_writer_t propagate_error_writer_to_corehost(host_contract.set_error_writer);

            const host_interface_t& intf = init->get_host_init_data();

            if ((code = host_contract.load(&intf)) == StatusCode::Success)
            {
                code = coreclr_delegate(type, delegate);
            }
        }

        return code;
    }

    int get_init_info_for_component(
        const host_startup_info_t &host_info,
        host_mode_t mode,
        pal::string_t &runtime_config_path,
        pal::string_t &impl_dir,
        std::unique_ptr<corehost_init_t> &init)
    {
        // Read config
        fx_definition_vector_t fx_definitions;
        auto app = new fx_definition_t();
        fx_definitions.push_back(std::unique_ptr<fx_definition_t>(app));

        runtime_config_t::settings_t override_settings;
        int rc = read_config(*app, host_info.app_path, runtime_config_path, override_settings);
        if (rc != StatusCode::Success)
            return rc;

        runtime_config_t app_config = app->get_runtime_config();
        bool is_framework_dependent = app_config.get_is_framework_dependent();
        if (is_framework_dependent)
        {
            rc = fx_resolver_t::resolve_frameworks_for_app(host_info, override_settings, app_config, fx_definitions);
            if (rc != StatusCode::Success)
            {
                return rc;
            }
        }

        std::vector<pal::string_t> probe_realpaths = get_probe_realpaths(fx_definitions, std::vector<pal::string_t>() /* specified_probing_paths */);

        trace::verbose(_X("Libhost loading occurring as a %s app as per config file [%s]"),
            (is_framework_dependent ? _X("framework-dependent") : _X("self-contained")), app_config.get_path().c_str());

        pal::string_t deps_file;
        if (!hostpolicy_resolver::try_get_dir(mode, host_info.dotnet_root, fx_definitions, host_info.app_path, deps_file, probe_realpaths, &impl_dir))
        {
            return StatusCode::CoreHostLibMissingFailure;
        }

        pal::string_t additional_deps_serialized;
        init.reset(new corehost_init_t(pal::string_t{}, host_info, deps_file, additional_deps_serialized, probe_realpaths, mode, fx_definitions));

        return StatusCode::Success;
    }
}

int fx_muxer_t::load_runtime_and_get_delegate(
    const host_startup_info_t& host_info,
    host_mode_t mode,
    coreclr_delegate_type delegate_type,
    void** delegate)
{
    assert(host_info.is_valid(mode));

    pal::string_t runtime_config = _X("");
    pal::string_t impl_dir;
    std::unique_ptr<corehost_init_t> init;
    int rc = get_init_info_for_component(host_info, mode, runtime_config, impl_dir, init);
    if (rc != StatusCode::Success)
        return rc;

    rc = get_delegate_from_runtime(impl_dir, init.get(), delegate_type, delegate);
    return rc;
}

int fx_muxer_t::handle_exec(
    const host_startup_info_t& host_info,
    const pal::string_t& app_candidate,
    const opt_map_t& opts,
    int argc,
    const pal::char_t* argv[],
    int argoff,
    host_mode_t mode)
{
    return handle_exec_host_command(
        pal::string_t(),
        host_info,
        app_candidate,
        opts,
        argc,
        argv,
        argoff,
        mode,
        nullptr,
        0,
        nullptr);
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
        new_argc = vec_argv.size();
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
    const pal::char_t* argv[])
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
    // Did not exececute the app or run other commands, so try the CLI SDK dotnet.dll
    //

    pal::string_t sdk_dotnet;
    if (!sdk_resolver_t::resolve_sdk_dotnet_path(host_info.dotnet_root, &sdk_dotnet))
    {
        assert(argc > 1);
        if (pal::strcasecmp(_X("-h"), argv[1]) == 0 ||
            pal::strcasecmp(_X("--help"), argv[1]) == 0 ||
            pal::strcasecmp(_X("-?"), argv[1]) == 0 ||
            pal::strcasecmp(_X("/?"), argv[1]) == 0)
        {
            muxer_usage(false);
            return StatusCode::InvalidArgFailure;
        }
        else if (pal::strcasecmp(_X("--info"), argv[1]) == 0)
        {
            muxer_info(host_info.dotnet_root);
            return StatusCode::Success;
        }

        return StatusCode::LibHostSdkFindFailure;
    }

    append_path(&sdk_dotnet, _X("dotnet.dll"));

    if (!pal::file_exists(sdk_dotnet))
    {
        trace::error(_X("Found dotnet SDK, but did not find dotnet.dll at [%s]"), sdk_dotnet.c_str());
        return StatusCode::LibHostSdkFindFailure;
    }

    // Transform dotnet [command] [args] -> dotnet dotnet.dll [command] [args]

    std::vector<const pal::char_t*> new_argv;
    new_argv.reserve(argc + 1);
    new_argv.push_back(argv[0]);
    new_argv.push_back(sdk_dotnet.c_str());
    new_argv.insert(new_argv.end(), argv + 1, argv + argc);

    trace::verbose(_X("Using dotnet SDK dll=[%s]"), sdk_dotnet.c_str());

    int new_argoff;
    pal::string_t app_candidate;
    opt_map_t opts;

    int result = parse_args(host_info, 1, new_argv.size(), new_argv.data(), false, host_mode_t::muxer, &new_argoff, app_candidate, opts); // arg offset 1 for dotnet
    if (!result)
    {
        // Transform dotnet [exec] [--additionalprobingpath path] [--depsfile file] [dll] [args] -> dotnet [dll] [args]
        result = handle_exec(host_info, app_candidate, opts, new_argv.size(), new_argv.data(), new_argoff, host_mode_t::muxer);
    }

    if (pal::strcasecmp(_X("--info"), argv[1]) == 0)
    {
        muxer_info(host_info.dotnet_root);
    }

    return result;
}
