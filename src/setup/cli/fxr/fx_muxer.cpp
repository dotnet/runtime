// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <cassert>
#include "pal.h"
#include "utils.h"
#include "libhost.h"
#include "args.h"
#include "fx_ver.h"
#include "fx_muxer.h"
#include "trace.h"
#include "runtime_config.h"
#include "cpprest/json.h"
#include "error_codes.h"
#include "deps_format.h"

pal::string_t fx_muxer_t::resolve_fx_dir(const pal::string_t& muxer_dir, runtime_config_t* runtime)
{
    trace::verbose(_X("--- Resolving FX directory from muxer dir [%s]"), muxer_dir.c_str());
    const auto fx_name = runtime->get_fx_name();
    const auto fx_ver = runtime->get_fx_version();
    const auto patch_roll_fwd = runtime->get_patch_roll_fwd();

    fx_ver_t specified(-1, -1, -1);
    if (!fx_ver_t::parse(fx_ver, &specified, false))
    {
        trace::error(_X("The specified runtimeconfig.json version [%s] could not be parsed"), fx_ver.c_str());
        return pal::string_t();
    }

    auto fx_dir = muxer_dir;
    append_path(&fx_dir, _X("shared"));
    append_path(&fx_dir, fx_name.c_str());

    bool do_roll_forward = false;
    if (!specified.is_prerelease())
    {
        // If production and no roll forward use given version.
        do_roll_forward = patch_roll_fwd;
    }
    else
    {
        // Prerelease, but roll forward only if version doesn't exist.
        pal::string_t ver_dir = fx_dir;
        append_path(&ver_dir, fx_ver.c_str());
        do_roll_forward = !pal::directory_exists(ver_dir);
    }

    if (!do_roll_forward)
    {
        trace::verbose(_X("Did not roll forward because patch_roll_fwd=%d, chose [%s]"), patch_roll_fwd, fx_ver.c_str());
        append_path(&fx_dir, fx_ver.c_str());
    }
    else
    {
        trace::verbose(_X("Attempting FX roll forward starting from [%s]"), fx_ver.c_str());

        std::vector<pal::string_t> list;
        pal::readdir(fx_dir, &list);
        fx_ver_t most_compatible = specified;
        for (const auto& version : list)
        {
            trace::verbose(_X("Inspecting version... [%s]"), version.c_str());
            fx_ver_t ver(-1, -1, -1);
            if (!specified.is_prerelease() && fx_ver_t::parse(version, &ver, true) && // true -- only prod. prevents roll forward to prerelease.
                ver.get_major() == specified.get_major() &&
                ver.get_minor() == specified.get_minor())
            {
                // Pick the greatest production that differs only in patch.
                most_compatible = std::max(ver, most_compatible);
            }
            if (specified.is_prerelease() && fx_ver_t::parse(version, &ver, false) && // false -- implies both production and prerelease.
                ver.is_prerelease() && // prevent roll forward to production.
                ver.get_major() == specified.get_major() &&
                ver.get_minor() == specified.get_minor() &&
                ver.get_patch() == specified.get_patch() &&
                ver > specified)
            {
                // Pick the smallest prerelease that is greater than specified.
                most_compatible = (most_compatible == specified) ? ver : std::min(ver, most_compatible);
            }
        }
        pal::string_t most_compatible_str = most_compatible.as_str();
        append_path(&fx_dir, most_compatible_str.c_str());
    }

    trace::verbose(_X("Chose FX version [%s]"), fx_dir.c_str());
    return fx_dir;
}

pal::string_t fx_muxer_t::resolve_cli_version(const pal::string_t& global_json)
{
    trace::verbose(_X("--- Resolving CLI version from global json [%s]"), global_json.c_str());

    pal::string_t retval;
    if (!pal::file_exists(global_json))
    {
        trace::verbose(_X("[%s] does not exist"), global_json.c_str());
        return retval;
    }

    pal::ifstream_t file(global_json);
    if (!file.good())
    {
        trace::verbose(_X("[%s] could not be opened"), global_json.c_str());
        return retval;
    }

    if (skip_utf8_bom(&file))
    {
        trace::verbose(_X("UTF-8 BOM skipped while reading [%s]"), global_json.c_str());
    }

    try
    {
        const auto root = json_value::parse(file);
        const auto& json = root.as_object();
        const auto sdk_iter = json.find(_X("sdk"));
        if (sdk_iter == json.end() || sdk_iter->second.is_null())
        {
            trace::verbose(_X("CLI '/sdk/version' field not present/null in [%s]"), global_json.c_str());
            return retval;
        }

        const auto& sdk_obj = sdk_iter->second.as_object();
        const auto ver_iter = sdk_obj.find(_X("version"));
        if (ver_iter == sdk_obj.end() || ver_iter->second.is_null())
        {
            trace::verbose(_X("CLI 'sdk/version' field not present/null in [%s]"), global_json.c_str());
            return retval;
        }
        retval = ver_iter->second.as_string();
    }
    catch (const web::json::json_exception& je)
    {
        pal::string_t jes = pal::to_palstring(je.what());
        trace::error(_X("A JSON parsing exception occurred in [%s]: %s"), global_json.c_str(), jes.c_str());
    }
    trace::verbose(_X("CLI version is [%s] in global json file [%s]"), retval.c_str(), global_json.c_str());
    return retval;
}

pal::string_t resolve_sdk_version(pal::string_t sdk_path)
{
    trace::verbose(_X("--- Resolving SDK version from SDK dir [%s]"), sdk_path.c_str());

    pal::string_t retval;
    std::vector<pal::string_t> versions;

    pal::readdir(sdk_path, &versions);
    fx_ver_t max_ver(-1, -1, -1);
    fx_ver_t max_pre(-1, -1, -1);
    for (const auto& version : versions)
    {
        trace::verbose(_X("Considering version... [%s]"), version.c_str());

        fx_ver_t ver(-1, -1, -1);
        if (fx_ver_t::parse(version, &ver, true))
        {
            max_ver = std::max(ver, max_ver);
        }
        if (fx_ver_t::parse(version, &ver, false))
        {
            max_pre = std::max(ver, max_pre);
        }
    }

    // No production, use the max pre-release.
    if (max_ver == fx_ver_t(-1, -1, -1))
    {
        trace::verbose(_X("No production version found, so using latest prerelease"));
        max_ver = max_pre;
    }

    pal::string_t max_ver_str = max_ver.as_str();
    append_path(&sdk_path, max_ver_str.c_str());

    trace::verbose(_X("Checking if resolved SDK dir [%s] exists"), sdk_path.c_str());
    if (pal::directory_exists(sdk_path))
    {
        retval = sdk_path;
    }

    trace::verbose(_X("Resolved SDK dir is [%s]"), retval.c_str());
    return retval;
}

bool fx_muxer_t::resolve_sdk_dotnet_path(const pal::string_t& own_dir, pal::string_t* cli_sdk)
{
    trace::verbose(_X("--- Resolving dotnet from working dir"));
    pal::string_t cwd;
    pal::string_t global;
    if (pal::getcwd(&cwd))
    {
        for (pal::string_t parent_dir, cur_dir = cwd; true; cur_dir = parent_dir)
        {
            pal::string_t file = cur_dir;
            append_path(&file, _X("global.json"));

            trace::verbose(_X("Probing path [%s] for global.json"), file.c_str());
            if (pal::file_exists(file))
            {
                global = file;
                trace::verbose(_X("Found global.json [%s]"), global.c_str());
                break;
            }
            parent_dir = get_directory(cur_dir);
            if (parent_dir.empty() || parent_dir.size() == cur_dir.size())
            {
                trace::verbose(_X("Terminating global.json search at [%s]"), parent_dir.c_str());
                break;
            }
        }
    }
    else
    {
        trace::verbose(_X("Failed to obtain current working dir"));
    }

    pal::string_t retval;
    if (!global.empty())
    {
        pal::string_t cli_version = resolve_cli_version(global);
        if (!cli_version.empty())
        {
            pal::string_t sdk_path = own_dir;
            append_path(&sdk_path, _X("sdk"));
            append_path(&sdk_path, cli_version.c_str());

            if (pal::directory_exists(sdk_path))
            {
                trace::verbose(_X("CLI directory [%s] from global.json exists"), sdk_path.c_str());
                retval = sdk_path;
            }
            else
            {
                trace::verbose(_X("CLI directory [%s] from global.json doesn't exist"), sdk_path.c_str());
            }
        }
    }
    if (retval.empty())
    {
        pal::string_t sdk_path = own_dir;
        append_path(&sdk_path, _X("sdk"));
        retval = resolve_sdk_version(sdk_path);
    }
    cli_sdk->assign(retval);
    trace::verbose(_X("Found CLI SDK in: %s"), cli_sdk->c_str());
    return !retval.empty();
}

int muxer_usage()
{
    trace::error(_X("Usage: dotnet [--help | app.dll]"));
    return StatusCode::InvalidArgFailure;
}

int fx_muxer_t::parse_args_and_execute(
    const pal::string_t& own_dir,
    const pal::string_t& own_dll,
    int argoff, int argc, const pal::char_t* argv[], bool exec_mode, host_mode_t mode, bool* is_an_app)
{
    *is_an_app = true;

    std::vector<pal::string_t> known_opts = { _X("--additionalprobingpath") };
    if (exec_mode || mode == host_mode_t::split_fx || mode == host_mode_t::standalone)
    {
        known_opts.push_back(_X("--depsfile"));
        known_opts.push_back(_X("--runtimeconfig"));
    }

    // Parse the known arguments if any.
    int num_parsed = 0;
    std::unordered_map<pal::string_t, std::vector<pal::string_t>> opts;
    if (!parse_known_args(argc - argoff, &argv[argoff], known_opts, &opts, &num_parsed))
    {
        trace::error(_X("Failed to parse supported arguments."));
        return InvalidArgFailure;
    }

    const pal::char_t** new_argv = argv;
    int new_argc = argc;
    std::vector<const pal::char_t*> vec_argv;
    pal::string_t app_candidate = own_dll;
    int cur_i = argoff + num_parsed;
    if (mode != host_mode_t::standalone)
    {
        trace::verbose(_X("App not in standalone mode, so expecting more arguments..."));
        if (cur_i >= argc)
        {
            return muxer_usage();
        }

        app_candidate = argv[cur_i];
        bool is_app_runnable = ends_with(app_candidate, _X(".dll"), false) || ends_with(app_candidate, _X(".exe"), false);
        trace::verbose(_X("App %s runnable=[%d]"), app_candidate.c_str(), is_app_runnable);
        // If exec mode is on, then check we have a dll at this point
        if (exec_mode)
        {
            if (!is_app_runnable)
            {
                trace::error(_X("dotnet exec needs a dll to execute. Try dotnet [--help]"));
                return InvalidArgFailure;
            }
        }
        // For non-exec, non-standalone there is CLI invocation or app.dll execution after known args.
        else
        {
            // Test if we have a real dll at this point.
            if (!is_app_runnable)
            {
                // No we don't have a dll, this must be routed to the CLI.
                *is_an_app = false;
                return AppArgNotRunnable;
            }
        }
    }
    if (cur_i != 1)
    {
        vec_argv.resize(argc - cur_i + 1, 0); // +1 for dotnet
        memcpy(vec_argv.data() + 1, argv + cur_i, (argc - cur_i) * sizeof(pal::char_t*));
        vec_argv[0] = argv[0];
        new_argv = vec_argv.data();
        new_argc = vec_argv.size();
    }

    // Transform dotnet [exec] [--additionalprobingpath path] [--depsfile file] [dll] [args] -> dotnet [dll] [args]
    return read_config_and_execute(own_dir, app_candidate, opts, new_argc, new_argv, mode);
}

int fx_muxer_t::read_config_and_execute(
    const pal::string_t& own_dir, 
    const pal::string_t& app_candidate,
    const std::unordered_map<pal::string_t, std::vector<pal::string_t>>& opts,
    int new_argc, const pal::char_t** new_argv, host_mode_t mode)
{
    pal::string_t opts_deps_file = _X("--depsfile");
    pal::string_t opts_probe_path = _X("--additionalprobingpath");
    pal::string_t opts_runtime_config = _X("--runtimeconfig");

    pal::string_t deps_file = get_last_known_arg(opts, opts_deps_file, _X(""));
    pal::string_t runtime_config = get_last_known_arg(opts, opts_runtime_config, _X(""));
    std::vector<pal::string_t> probe_paths = opts.count(opts_probe_path) ? opts.find(opts_probe_path)->second : std::vector<pal::string_t>();

    pal::string_t app_or_deps = deps_file.empty() ? app_candidate : deps_file;
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

    runtime_config_t config(config_file, dev_config_file);
    for (const auto& path : config.get_probe_paths())
    {
        probe_paths.push_back(path);
    }

    if (!config.is_valid())
    {
        trace::error(_X("Invalid runtimeconfig.json [%s] [%s]"), config.get_path().c_str(), config.get_dev_path().c_str());
        return StatusCode::InvalidConfigFile;
    }
    if (!deps_file.empty() && !pal::file_exists(deps_file))
    {
        trace::error(_X("Deps file [%s] specified but doesn't exist"), deps_file.c_str());
        return StatusCode::InvalidArgFailure;
    }

    if (config.get_portable())
    {
        trace::verbose(_X("Executing as a portable app as per config file [%s]"), config_file.c_str());
        pal::string_t fx_dir = (mode == host_mode_t::split_fx) ? own_dir : resolve_fx_dir(own_dir, &config);
        corehost_init_t init(deps_file, probe_paths, fx_dir, mode, config);

        pal::string_t impl_dir;

        // First lookup hostpolicy.dll in servicing with the version of hostpolicy.dll that was compiled lock step with hostfxr.
        if (!hostpolicy_exists_in_svc(&impl_dir))
        {
            impl_dir = fx_dir;
        }
        return execute_app(impl_dir, &init, new_argc, new_argv);
    }
    else
    {
        pal::string_t impl_dir;
        trace::verbose(_X("Executing as a standalone app as per config file [%s]"), config_file.c_str());

        // First lookup hostpolicy.dll in servicing with the version of hostpolicy.dll that was compiled lock step with hostfxr.
        if (!hostpolicy_exists_in_svc(&impl_dir))
        {
            if (mode == host_mode_t::standalone || mode == host_mode_t::split_fx)
            {
                impl_dir = own_dir;
            }
            else if (mode == host_mode_t::muxer)
            {
                impl_dir = get_directory(app_or_deps);
            }
        }
        trace::verbose(_X("The host impl directory before probing deps is [%s]"), impl_dir.c_str());
        if (!library_exists_in_dir(impl_dir, LIBHOSTPOLICY_NAME, nullptr) && !probe_paths.empty() && !deps_file.empty())
        {
            bool found = false;
            pal::string_t candidate = impl_dir;
            deps_json_t deps_json(false, deps_file);
            for (const auto& probe_path : probe_paths)
            {
                trace::verbose(_X("Considering %s for hostpolicy library"), probe_path.c_str());
                if (deps_json.is_valid() &&
                    deps_json.has_hostpolicy_entry() &&
                    deps_json.get_hostpolicy_entry().to_full_path(probe_path, &candidate))
                {
                    found = true; // candidate contains the right path.
                    break;
                }
            }
            if (!found)
            {
                trace::error(_X("Policy library either not found in deps [%s] or not found in %d probe paths."), deps_file.c_str(), probe_paths.size());
                return StatusCode::CoreHostLibMissingFailure;
            }
            impl_dir = get_directory(candidate);
        }
        corehost_init_t init(deps_file, probe_paths, _X(""), mode, config);
        return execute_app(impl_dir, &init, new_argc, new_argv);
    }
}

/* static */
int fx_muxer_t::execute(const int argc, const pal::char_t* argv[])
{
    pal::string_t own_path;

    // Get the full name of the application
    if (!pal::get_own_executable_path(&own_path) || !pal::realpath(&own_path))
    {
        trace::error(_X("Failed to locate current executable"));
        return StatusCode::LibHostCurExeFindFailure;
    }
    pal::string_t own_name = get_filename(own_path);
    pal::string_t own_dir = get_directory(own_path);

    pal::string_t own_dll_filename = get_executable(own_name) + _X(".dll");
    pal::string_t own_dll = own_dir;
    append_path(&own_dll, own_dll_filename.c_str());

    trace::info(_X("Own DLL path=[%s]"), own_dll.c_str());
    auto mode = detect_operating_mode(own_dir, own_dll, own_name);
    bool is_an_app = true;
    if (mode == host_mode_t::split_fx)
    {
        trace::verbose(_X("--- Executing in split/FX mode..."));
        return parse_args_and_execute(own_dir, own_dll, 1, argc, argv, false, host_mode_t::split_fx, &is_an_app);
    }
    if (mode == host_mode_t::standalone)
    {
        trace::verbose(_X("--- Executing in standalone mode..."));
        return parse_args_and_execute(own_dir, own_dll, 1, argc, argv, false, host_mode_t::standalone, &is_an_app);
    }

    trace::verbose(_X("--- Executing in muxer mode..."));

    if (argc <= 1)
    {
        return muxer_usage();
    }

    if (pal::strcasecmp(_X("exec"), argv[1]) == 0)
    {
        return parse_args_and_execute(own_dir, own_dll, 2, argc, argv, true, host_mode_t::muxer, &is_an_app); // arg offset 2 for dotnet, exec
    }

    int result = parse_args_and_execute(own_dir, own_dll, 1, argc, argv, false, host_mode_t::muxer, &is_an_app); // arg offset 1 for dotnet
    if (is_an_app)
    {
        return result;
    }

    // Could not execute as an app, try the CLI SDK dotnet.dll
    pal::string_t sdk_dotnet;
    if (!resolve_sdk_dotnet_path(own_dir, &sdk_dotnet))
    {
        trace::error(_X("Could not resolve SDK directory from [%s]"), own_dir.c_str());
        return StatusCode::LibHostSdkFindFailure;
    }
    append_path(&sdk_dotnet, _X("dotnet.dll"));

    if (!pal::file_exists(sdk_dotnet))
    {
        trace::error(_X("Could not find dotnet.dll at [%s]"), sdk_dotnet.c_str());
        return StatusCode::LibHostSdkFindFailure;
    }

    // Transform dotnet [command] [args] -> dotnet dotnet.dll [command] [args]

    std::vector<const pal::char_t*> new_argv(argc + 1);
    memcpy(&new_argv.data()[2], argv + 1, (argc - 1) * sizeof(pal::char_t*));
    new_argv[0] = argv[0];
    new_argv[1] = sdk_dotnet.c_str();

    trace::verbose(_X("Using dotnet SDK dll=[%s]"), sdk_dotnet.c_str());
    return parse_args_and_execute(own_dir, own_dll, 1, new_argv.size(), new_argv.data(), false, host_mode_t::muxer, &is_an_app);
}

