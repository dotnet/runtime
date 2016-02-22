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
#include "corehost.h"
#include "policy_load.h"

typedef web::json::value json_value;

pal::string_t fx_muxer_t::resolve_fx_dir(const pal::string_t& muxer_dir, runtime_config_t* runtime, const pal::string_t& app_path)
{
    const auto fx_name = runtime->get_fx_name();
    const auto fx_ver = runtime->get_fx_version();
    const auto roll_fwd = runtime->get_fx_roll_fwd();

    fx_ver_t specified(-1, -1, -1);
    if (!fx_ver_t::parse(fx_ver, &specified, false))
    {
        return pal::string_t();
    }

    auto fx_dir = muxer_dir;
    append_path(&fx_dir, _X("Shared"));
    append_path(&fx_dir, fx_name.c_str());

    // If not roll forward or if pre-release, just return.
    if (!roll_fwd || specified.is_prerelease())
    {
        append_path(&fx_dir, fx_ver.c_str());
    }
    else
    {
        std::vector<pal::string_t> list;
        pal::readdir(fx_dir, &list);
        fx_ver_t max_specified = specified;
        for (const auto& version : list)
        {
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
    trace::verbose(_X("Found fx in: %s"), fx_dir.c_str());
    return pal::directory_exists(fx_dir) ? fx_dir : pal::string_t();
}

pal::string_t fx_muxer_t::resolve_cli_version(const pal::string_t& global_json)
{
    pal::string_t retval;
    if (!pal::file_exists(global_json))
    {
        return retval;
    }

    pal::ifstream_t file(global_json);
    if (!file.good())
    {
        return retval;
    }

    try
    {
        const auto root = json_value::parse(file);
        const auto& json = root.as_object();
        const auto sdk_iter = json.find(_X("sdk"));
        if (sdk_iter == json.end() || sdk_iter->second.is_null())
        {
            return retval;
        }

        const auto& sdk_obj = sdk_iter->second.as_object();
        const auto ver_iter = sdk_obj.find(_X("version"));
        if (ver_iter == sdk_obj.end() || ver_iter->second.is_null())
        {
            return retval;
        }
        retval = ver_iter->second.as_string();
    }
    catch (...)
    {
    }
    trace::verbose(_X("Found cli in: %s"), retval.c_str());
    return retval;
}

bool fx_muxer_t::resolve_sdk_dotnet_path(const pal::string_t& own_dir, pal::string_t* cli_sdk)
{
    pal::string_t cwd;
    pal::string_t global;
    if (pal::getcwd(&cwd))
    {
        for (pal::string_t parent_dir, cur_dir = cwd; true; cur_dir = parent_dir)
        {
            pal::string_t file = cur_dir;
            append_path(&file, _X("global.json"));
            if (pal::file_exists(file))
            {
                global = file;
                break;
            }
            parent_dir = get_directory(cur_dir);
            if (parent_dir.empty() || parent_dir.size() == cur_dir.size())
            {
                break;
            }
        }
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
                retval = sdk_path;
            }
        }
    }
    if (retval.empty())
    {
        pal::string_t sdk_path = own_dir;
        append_path(&sdk_path, _X("sdk"));

        std::vector<pal::string_t> versions;
        pal::readdir(sdk_path, &versions);
        fx_ver_t max_ver(-1, -1, -1);
        for (const auto& version : versions)
        {
            fx_ver_t ver(-1, -1, -1);
            if (fx_ver_t::parse(version, &ver, true))
            {
                max_ver = std::max(ver, max_ver);
            }
        }
        pal::string_t max_ver_str = max_ver.as_str();
        append_path(&sdk_path, max_ver_str.c_str());
        if (pal::directory_exists(sdk_path))
        {
            retval = sdk_path;
        }
    }
    cli_sdk->assign(retval);
    trace::verbose(_X("Found cli sdk in: %s"), cli_sdk->c_str());
    return !retval.empty();
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

    auto own_dir = get_directory(own_path);

    if (argc <= 1)
    {
        return StatusCode::InvalidArgFailure;
    }
    if (ends_with(argv[1], _X(".dll"), false))
    {
        pal::string_t app_path = argv[1];

        if (!pal::realpath(&app_path))
        {
            return StatusCode::LibHostExecModeFailure;
        }

        runtime_config_t config(get_runtime_config_json(app_path));
        if (!config.is_valid())
        {
            trace::error(_X("Invalid runtimeconfig.json [%s]"), config.get_path().c_str());
            return StatusCode::InvalidConfigFile;
        }
        if (config.get_portable())
        {
            pal::string_t fx_dir = resolve_fx_dir(own_dir, &config, app_path);
            corehost_init_t init(_X(""), _X(""), fx_dir, host_mode_t::muxer, &config);
            return policy_load_t::execute_app(fx_dir, &init, argc, argv);
        }
        else
        {
            corehost_init_t init(_X(""), _X(""), _X(""), host_mode_t::muxer, &config);
            return policy_load_t::execute_app(get_directory(app_path), &init, argc, argv);
        }
    }
    else
    {
        if (pal::strcasecmp(_X("exec"), argv[1]) == 0)
        {
            std::vector<pal::string_t> known_opts = { _X("--depsfile"), _X("--additionalprobingpath") };

            int num_args = 0;
            std::unordered_map<pal::string_t, pal::string_t> opts;
            if (!parse_known_args(argc - 2, &argv[2], known_opts, &opts, &num_args))
            {
                return InvalidArgFailure;
            }
            int cur_i = 2 + num_args;
            if (cur_i >= argc)
            {
                return InvalidArgFailure;
            }

            // Transform dotnet exec [--additionalprobingpath path] [--depsfile file] dll [args] -> dotnet dll [args]

            std::vector<const pal::char_t*> new_argv(argc - cur_i + 1); // +1 for dotnet
            memcpy(new_argv.data() + 1, argv + cur_i, (argc - cur_i) * sizeof(pal::char_t*));
            new_argv[0] = argv[0];

            pal::string_t deps_file = opts.count(_X("--depsfile")) ? opts[_X("--depsfile")] : _X("");
            pal::string_t probe_path = opts.count(_X("--additionalprobingpath")) ? opts[_X("--additionalprobingpath")] : _X("");

            pal::string_t app_path = argv[cur_i];
            runtime_config_t config(get_runtime_config_json(app_path));
            if (!config.is_valid())
            {
                trace::error(_X("Invalid runtimeconfig.json [%s]"), config.get_path().c_str());
                return StatusCode::InvalidConfigFile;
            }
            if (config.get_portable())
            {
                pal::string_t fx_dir = resolve_fx_dir(own_dir, &config, app_path);
                corehost_init_t init(deps_file, probe_path, fx_dir, host_mode_t::muxer, &config);
                return policy_load_t::execute_app(fx_dir, &init, new_argv.size(), new_argv.data());
            }
            else
            {
                corehost_init_t init(deps_file, probe_path, _X(""), host_mode_t::muxer, &config);
                pal::string_t impl_dir = get_directory(deps_file.empty() ? app_path : deps_file);
                return policy_load_t::execute_app(impl_dir, &init, new_argv.size(), new_argv.data());
            }
        }
        else
        {
            pal::string_t sdk_dotnet;
            if (!resolve_sdk_dotnet_path(own_dir, &sdk_dotnet))
            {
                return StatusCode::LibHostSdkFindFailure;
            }
            append_path(&sdk_dotnet, _X("dotnet.dll"));
            // Transform dotnet [command] [args] -> dotnet [dotnet.dll] [command] [args]

            std::vector<const pal::char_t*> new_argv(argc + 1);
            memcpy(&new_argv.data()[2], argv + 1, (argc - 1) * sizeof(pal::char_t*));
            new_argv[0] = argv[0];
            new_argv[1] = sdk_dotnet.c_str();

            trace::verbose(_X("Using SDK dll=[%s]"), sdk_dotnet.c_str());

            assert(ends_with(sdk_dotnet, _X(".dll"), false));

            runtime_config_t config(get_runtime_config_json(sdk_dotnet));

            if (config.get_portable())
            {
                pal::string_t fx_dir = resolve_fx_dir(own_dir, &config, sdk_dotnet);
                corehost_init_t init(_X(""), _X(""), fx_dir, host_mode_t::muxer, &config);
                return policy_load_t::execute_app(fx_dir, &init, new_argv.size(), new_argv.data());
            }
            else
            {
                corehost_init_t init(_X(""), _X(""), _X(""), host_mode_t::muxer, &config);
                return policy_load_t::execute_app(get_directory(sdk_dotnet), &init, new_argv.size(), new_argv.data());
            }
        }
    }
}

SHARED_API int hostfxr_main(const int argc, const pal::char_t* argv[])
{
    trace::setup();
    return fx_muxer_t().execute(argc, argv);
}
