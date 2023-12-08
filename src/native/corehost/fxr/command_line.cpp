// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "command_line.h"
#include <error_codes.h>
#include "framework_info.h"
#include "install_info.h"
#include <pal.h>
#include "sdk_info.h"
#include <trace.h>
#include <utils.h>
#include "bundle/info.h"

namespace
{
    struct host_option
    {
        const pal::char_t* option;
        const pal::char_t* argument;
        const pal::char_t* description;
    };

    const host_option KnownHostOptions[] =
    {
        { _X("--additionalprobingpath"), _X("<path>"), _X("Path containing probing policy and assemblies to probe for.") },
        { _X("--depsfile"), _X("<path>"), _X("Path to <application>.deps.json file.") },
        { _X("--runtimeconfig"), _X("<path>"), _X("Path to <application>.runtimeconfig.json file.") },
        { _X("--fx-version"), _X("<version>"), _X("Version of the installed Shared Framework to use to run the application.") },
        { _X("--roll-forward"), _X("<value>"), _X("Roll forward to framework version (LatestPatch, Minor, LatestMinor, Major, LatestMajor, Disable)") },
        { _X("--additional-deps"), _X("<path>"), _X("Path to additional deps.json file.") },
        { _X("--roll-forward-on-no-candidate-fx"), _X("<n>"), _X("<obsolete>") }
    };
    static_assert((sizeof(KnownHostOptions) / sizeof(*KnownHostOptions)) == static_cast<size_t>(known_options::__last), "Invalid host option count");

    bool is_sdk_dir_present(const pal::string_t& dotnet_root)
    {
        pal::string_t sdk_path = dotnet_root;
        append_path(&sdk_path, _X("sdk"));
        return pal::directory_exists(sdk_path);
    }

    std::vector<known_options> get_known_opts(bool exec_mode, host_mode_t mode, bool for_cli_usage = false)
    {
        std::vector<known_options> known_opts;
        known_opts.reserve(static_cast<int>(known_options::__last));
        known_opts.push_back(known_options::additional_probing_path);
        if (for_cli_usage || exec_mode || mode == host_mode_t::split_fx || mode == host_mode_t::apphost)
        {
            known_opts.push_back(known_options::deps_file);
            known_opts.push_back(known_options::runtime_config);
        }

        if (for_cli_usage || mode == host_mode_t::muxer || mode == host_mode_t::apphost)
        {
            // If mode=host_mode_t::apphost, these are only used when the app is framework-dependent.
            known_opts.push_back(known_options::fx_version);
            known_opts.push_back(known_options::roll_forward);
            known_opts.push_back(known_options::additional_deps);

            if (!for_cli_usage)
            {
                // Intentionally leave this one out of for_cli_usage since we don't want to show it in command line help (it's deprecated).
                known_opts.push_back(known_options::roll_forward_on_no_candidate_fx);
            }
        }

        return known_opts;
    }

    const host_option& get_host_option(known_options opt)
    {
        int idx = static_cast<int>(opt);
        assert(0 <= idx && idx < static_cast<int>(known_options::__last));
        return KnownHostOptions[idx];
    }

    bool parse_known_args(
        const int argc,
        const pal::char_t* argv[],
        const std::vector<known_options>& known_opts,
        // Although multimap would provide this functionality the order of kv, values are
        // not preserved in C++ < C++0x
        opt_map_t* opts,
        int* num_args)
    {
        int arg_i = *num_args;
        while (arg_i < argc)
        {
            const pal::char_t* arg = argv[arg_i];
            pal::string_t arg_lower = to_lower(arg);
            const auto &iter = std::find_if(known_opts.cbegin(), known_opts.cend(),
                [&](const known_options &opt) { return arg_lower == get_host_option(opt).option; });
            if (iter == known_opts.cend())
            {
                // Unknown argument.
                break;
            }

            // Known argument, so expect one more arg (value) to be present.
            if (arg_i + 1 >= argc)
            {
                return false;
            }

            trace::verbose(_X("Parsed known arg %s = %s"), arg, argv[arg_i + 1]);
            (*opts)[*iter].push_back(argv[arg_i + 1]);

            // Increment for both the option and its value.
            arg_i += 2;
        }

        *num_args = arg_i;

        return true;
    }

    int parse_args(
        const host_startup_info_t &host_info,
        int argoff,
        int argc,
        const pal::char_t *argv[],
        bool exec_mode,
        host_mode_t mode,
        /*out*/ int *new_argoff,
        /*out*/ pal::string_t &app_candidate,
        /*out*/ opt_map_t &opts)
    {
        std::vector<known_options> known_opts = get_known_opts(exec_mode, mode);

        // Parse the known arguments if any.
        int num_parsed = 0;
        if (!parse_known_args(argc - argoff, &argv[argoff], known_opts, &opts, &num_parsed))
        {
            trace::error(_X("Failed to parse supported options or their values:"));
            for (const auto& opt : known_opts)
            {
                const host_option &arg = get_host_option(opt);
                trace::error(_X("  %s %-*s  %s"), arg.option, 36 - (int)pal::strlen(arg.option), arg.argument, arg.description);
            }
            return StatusCode::InvalidArgFailure;
        }

        *new_argoff = argoff + num_parsed;
        bool doesAppExist = false;
        if (mode == host_mode_t::apphost)
        {
            app_candidate = host_info.app_path;
            doesAppExist = bundle::info_t::is_single_file_bundle() || pal::realpath(&app_candidate);
        }
        else
        {
            trace::verbose(_X("Using the provided arguments to determine the application to execute."));
            if (*new_argoff >= argc)
            {
                command_line::print_muxer_usage(!is_sdk_dir_present(host_info.dotnet_root));
                return StatusCode::InvalidArgFailure;
            }

            app_candidate = argv[*new_argoff];

            bool is_app_managed = utils::ends_with(app_candidate, _X(".dll"), false) || utils::ends_with(app_candidate, _X(".exe"), false);
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
}

pal::string_t command_line::get_option_value(
    const opt_map_t &opts,
    known_options opt,
    const pal::string_t &default_value)
{
    if (opts.count(opt))
    {
        const auto& val = opts.find(opt)->second;
        return val[val.size() - 1];
    }
    return default_value;
}

const pal::char_t* command_line::get_option_name(known_options opt)
{
    return get_host_option(opt).option;
}

int command_line::parse_args_for_mode(
    host_mode_t mode,
    const host_startup_info_t &host_info,
    const int argc,
    const pal::char_t *argv[],
    /*out*/ int *new_argoff,
    /*out*/ pal::string_t &app_candidate,
    /*out*/ opt_map_t &opts,
    bool args_include_running_executable)
{
    int argoff = args_include_running_executable ? 1 : 0;
    int result;
    if (mode == host_mode_t::split_fx)
    {
        // Invoked as corehost
        trace::verbose(_X("--- Executing in split/FX mode..."));
        result = parse_args(host_info, argoff, argc, argv, false, mode, new_argoff, app_candidate, opts);
    }
    else if (mode == host_mode_t::apphost)
    {
        // Invoked from the application base.
        trace::verbose(_X("--- Executing in a native executable mode..."));
        result = parse_args(host_info, argoff, argc, argv, false, mode, new_argoff, app_candidate, opts);
    }
    else
    {
        // Invoked as the dotnet.exe muxer.
        assert(mode == host_mode_t::muxer);
        trace::verbose(_X("--- Executing in muxer mode..."));

        if (argc <= argoff)
        {
            command_line::print_muxer_usage(!is_sdk_dir_present(host_info.dotnet_root));
            return StatusCode::InvalidArgFailure;
        }

        if (pal::strcasecmp(_X("exec"), argv[argoff]) == 0)
        {
            // arg offset +1 for exec
            argoff++;
            result = parse_args(host_info, argoff, argc, argv, true, mode, new_argoff, app_candidate, opts);
        }
        else
        {
            result = parse_args(host_info, argoff, argc, argv, false, mode, new_argoff, app_candidate, opts);
        }
    }

    return result;
}

int command_line::parse_args_for_sdk_command(
    const host_startup_info_t& host_info,
    const int argc,
    const pal::char_t* argv[],
    /*out*/ int *new_argoff,
    /*out*/ pal::string_t &app_candidate,
    /*out*/ opt_map_t &opts)
{
    // arg offset 1 for dotnet
    return parse_args(host_info, 1, argc, argv, false, host_mode_t::muxer, new_argoff, app_candidate, opts);
}

void command_line::print_muxer_info(const pal::string_t &dotnet_root, const pal::string_t &global_json_path, bool skip_sdk_info_output)
{
    pal::string_t commit = _STRINGIFY(REPO_COMMIT_HASH);
    trace::println(_X("\n")
        _X("Host:\n")
        _X("  Version:      ") _STRINGIFY(HOST_VERSION) _X("\n")
        _X("  Architecture: ") _STRINGIFY(CURRENT_ARCH_NAME) _X("\n")
        _X("  Commit:       %s"),
        commit.substr(0, 10).c_str());

    if (!skip_sdk_info_output)
        trace::println(_X("  RID:          %s"), get_runtime_id().c_str());

    trace::println(_X("\n")
        _X(".NET SDKs installed:"));
    if (!sdk_info::print_all_sdks(dotnet_root, _X("  ")))
    {
        trace::println(_X("  No SDKs were found."));
    }

    trace::println(_X("\n")
        _X(".NET runtimes installed:"));
    if (!framework_info::print_all_frameworks(dotnet_root, _X("  ")))
    {
        trace::println(_X("  No runtimes were found."));
    }

    trace::println(_X("\n")
        _X("Other architectures found:"));
    if (!install_info::print_other_architectures(_X("  ")))
    {
        trace::println(_X("  None"));
    }

    trace::println(_X("\n")
        _X("Environment variables:"));
    if (!install_info::print_environment(_X("  ")))
    {
        trace::println(_X("  Not set"));
    }

    trace::println(_X("\n")
        _X("global.json file:\n")
        _X("  %s"),
        global_json_path.empty() ? _X("Not found") : global_json_path.c_str());

    trace::println(_X("\n")
        _X("Learn more:\n")
        _X("  ") DOTNET_INFO_URL);

    trace::println(_X("\n")
        _X("Download .NET:\n")
        _X("  ") DOTNET_CORE_DOWNLOAD_URL);
}

void command_line::print_muxer_usage(bool is_sdk_present)
{
    std::vector<known_options> known_opts = get_known_opts(true, host_mode_t::invalid, /*for_cli_usage*/ true);

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

    for (const auto& opt : known_opts)
    {
        const host_option &arg = get_host_option(opt);
        trace::println(_X("  %s %-*s  %s"), arg.option, 29 - (int)pal::strlen(arg.option), arg.argument, arg.description);
    }
    trace::println(_X("  --list-runtimes                 Display the installed runtimes"));
    trace::println(_X("  --list-sdks                     Display the installed SDKs"));

    if (!is_sdk_present)
    {
        trace::println();
        trace::println(_X("Common Options:"));
        trace::println(_X("  -h|--help                       Displays this help."));
        trace::println(_X("  --info                          Display .NET information."));
    }
}
