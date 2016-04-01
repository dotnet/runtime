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
    const auto roll_fwd = runtime->get_fx_roll_fwd();

    fx_ver_t specified(-1, -1, -1);
    if (!fx_ver_t::parse(fx_ver, &specified, false))
    {
        trace::error(_X("The specified runtimeconfig.json version [%s] could not be parsed"), fx_ver.c_str());
        return pal::string_t();
    }

    auto fx_dir = muxer_dir;
    append_path(&fx_dir, _X("shared"));
    append_path(&fx_dir, fx_name.c_str());

    // If not roll forward or if pre-release, just return.
    if (!roll_fwd || specified.is_prerelease())
    {
        trace::verbose(_X("Did not roll forward because rollfwd=%d and [%s] is prerelease=%d"),
                roll_fwd, fx_ver.c_str(), specified.is_prerelease());
        append_path(&fx_dir, fx_ver.c_str());
    }
    else
    {
        trace::verbose(_X("Attempting production FX roll forward starting from [%s]"), fx_ver.c_str());

        std::vector<pal::string_t> list;
        pal::readdir(fx_dir, &list);
        fx_ver_t max_specified = specified;
        for (const auto& version : list)
        {
            trace::verbose(_X("Inspecting version... [%s]"), version.c_str());
            fx_ver_t ver(-1, -1, -1);
            if (fx_ver_t::parse(version, &ver, true) &&
                ver.get_major() == max_specified.get_major() &&
                ver.get_minor() == max_specified.get_minor())
            {
                max_specified.set_patch(std::max(ver.get_patch(), max_specified.get_patch()));
            }
        }
        pal::string_t max_specified_str = max_specified.as_str();
        append_path(&fx_dir, max_specified_str.c_str());
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
        trace::info(_X("A JSON parsing exception occurred: %s"), jes.c_str());
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

/* static */
int fx_muxer_t::execute(const int argc, const pal::char_t* argv[])
{
    trace::verbose(_X("--- Executing in muxer mode..."));

    pal::string_t own_path;

    // Get the full name of the application
    if (!pal::get_own_executable_path(&own_path) || !pal::realpath(&own_path))
    {
        trace::error(_X("Failed to locate current executable"));
        return StatusCode::LibHostCurExeFindFailure;
    }

    auto own_dir = get_directory(own_path);

    if (argc <= 1)
    {
        trace::error(_X("Usage: dotnet [--help | app.dll]"));
        return StatusCode::InvalidArgFailure;
    }
    if (ends_with(argv[1], _X(".dll"), false))
    {
        pal::string_t app_path = argv[1];

        if (!pal::realpath(&app_path))
        {
            trace::error(_X("Could not resolve app's full path [%s]"), app_path.c_str());
            return StatusCode::LibHostExecModeFailure;
        }

        auto config_file = get_runtime_config_from_file(app_path);
        runtime_config_t config(config_file);
        if (!config.is_valid())
        {
            trace::error(_X("Invalid runtimeconfig.json [%s]"), config.get_path().c_str());
            return StatusCode::InvalidConfigFile;
        }
        if (config.get_portable())
        {
            trace::verbose(_X("Executing as a portable app as per config file [%s]"), config_file.c_str());
            pal::string_t fx_dir = resolve_fx_dir(own_dir, &config);
            corehost_init_t init(_X(""), config.get_probe_paths(), fx_dir, host_mode_t::muxer, &config);
            return execute_app(fx_dir, &init, argc, argv);
        }
        else
        {
            trace::verbose(_X("Executing as a standlone app as per config file [%s]"), config_file.c_str());
            corehost_init_t init(_X(""), config.get_probe_paths(), _X(""), host_mode_t::muxer, &config);
            return execute_app(get_directory(app_path), &init, argc, argv);
        }
    }
    else
    {
        if (pal::strcasecmp(_X("exec"), argv[1]) == 0)
        {
            std::vector<pal::string_t> known_opts = { _X("--depsfile"), _X("--additionalprobingpath") };

            trace::verbose(_X("Exec mode, parsing known args"));
            int num_args = 0;
            std::unordered_map<pal::string_t, pal::string_t> opts;
            if (!parse_known_args(argc - 2, &argv[2], known_opts, &opts, &num_args))
            {
                trace::error(_X("Failed to parse known arguments."));
                return InvalidArgFailure;
            }
            int cur_i = 2 + num_args;
            if (cur_i >= argc)
            {
                trace::error(_X("Parsed known args, but need more arguments."));
                return InvalidArgFailure;
            }

            // Transform dotnet exec [--additionalprobingpath path] [--depsfile file] dll [args] -> dotnet dll [args]

            std::vector<const pal::char_t*> new_argv(argc - cur_i + 1); // +1 for dotnet
            memcpy(new_argv.data() + 1, argv + cur_i, (argc - cur_i) * sizeof(pal::char_t*));
            new_argv[0] = argv[0];

            pal::string_t opts_deps_file = _X("--depsfile");
            pal::string_t opts_probe_path = _X("--additionalprobingpath");
            pal::string_t deps_file = opts.count(opts_deps_file) ? opts[opts_deps_file] : _X("");
            pal::string_t probe_path = opts.count(opts_probe_path) ? opts[opts_probe_path] : _X("");

            trace::verbose(_X("Current argv is %s"), argv[cur_i]);

            pal::string_t app_or_deps = deps_file.empty() ? argv[cur_i] : deps_file;
            pal::string_t no_json = argv[cur_i];
            auto config_file = get_runtime_config_from_file(no_json);
            runtime_config_t config(config_file);
            if (!config.is_valid())
            {
                trace::error(_X("Invalid runtimeconfig.json [%s]"), config.get_path().c_str());
                return StatusCode::InvalidConfigFile;
            }
            if (!deps_file.empty() && !pal::file_exists(deps_file))
            {
                trace::error(_X("Deps file [%s] specified but doesn't exist"), deps_file.c_str());
                return StatusCode::InvalidArgFailure;
            }
            std::vector<pal::string_t> probe_paths = { probe_path };
            if (config.get_portable())
            {
                trace::verbose(_X("Executing as a portable app as per config file [%s]"), config_file.c_str());
                pal::string_t fx_dir = resolve_fx_dir(own_dir, &config);
                corehost_init_t init(deps_file, probe_paths, fx_dir, host_mode_t::muxer, &config);
                return execute_app(fx_dir, &init, new_argv.size(), new_argv.data());
            }
            else
            {
                trace::verbose(_X("Executing as a standalone app as per config file [%s]"), config_file.c_str());
                pal::string_t impl_dir = get_directory(app_or_deps);
                if (!library_exists_in_dir(impl_dir, LIBHOSTPOLICY_NAME, nullptr) && !probe_path.empty() && !deps_file.empty())
                {
                    deps_json_t deps_json(false, deps_file);
                    pal::string_t candidate = impl_dir;
                    if (!deps_json.has_hostpolicy_entry() ||
                        !deps_json.get_hostpolicy_entry().to_full_path(probe_path, &candidate))
                    {
                        trace::error(_X("Policy library either not found in deps [%s] or not found in [%s]"), deps_file.c_str(), probe_path.c_str());
                        return StatusCode::CoreHostLibMissingFailure;
                    }
                    impl_dir = get_directory(candidate);
                }
                corehost_init_t init(deps_file, probe_paths, _X(""), host_mode_t::muxer, &config);
                return execute_app(impl_dir, &init, new_argv.size(), new_argv.data());
            }
        }
        else
        {
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

            // Transform dotnet [command] [args] -> dotnet [dotnet.dll] [command] [args]

            std::vector<const pal::char_t*> new_argv(argc + 1);
            memcpy(&new_argv.data()[2], argv + 1, (argc - 1) * sizeof(pal::char_t*));
            new_argv[0] = argv[0];
            new_argv[1] = sdk_dotnet.c_str();

            trace::verbose(_X("Using dotnet SDK dll=[%s]"), sdk_dotnet.c_str());

            auto config_file = get_runtime_config_from_file(sdk_dotnet);
            runtime_config_t config(config_file);

            if (config.get_portable())
            {
                trace::verbose(_X("Executing dotnet.dll as a portable app as per config file [%s]"), config_file.c_str());
                pal::string_t fx_dir = resolve_fx_dir(own_dir, &config);
                corehost_init_t init(_X(""), std::vector<pal::string_t>(), fx_dir, host_mode_t::muxer, &config);
                return execute_app(fx_dir, &init, new_argv.size(), new_argv.data());
            }
            else
            {
                trace::verbose(_X("Executing dotnet.dll as a standalone app as per config file [%s]"), config_file.c_str());
                corehost_init_t init(_X(""), std::vector<pal::string_t>(), _X(""), host_mode_t::muxer, &config);
                return execute_app(get_directory(sdk_dotnet), &init, new_argv.size(), new_argv.data());
            }
        }
    }
}

