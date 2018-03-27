// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal.h"
#include "utils.h"
#include "trace.h"
#include "libhost.h"
#include "host_startup_info.h"

void get_runtime_config_paths_from_app(const pal::string_t& app, pal::string_t* cfg, pal::string_t* dev_cfg)
{
    auto name = get_filename_without_ext(app);
    auto path = get_directory(app);

    get_runtime_config_paths(path, name, cfg, dev_cfg);
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

void get_runtime_config_paths(const pal::string_t& path, const pal::string_t& name, pal::string_t* cfg, pal::string_t* dev_cfg)
{
    auto json_path = path;
    auto json_name = name + _X(".runtimeconfig.json");
    append_path(&json_path, json_name.c_str());
    cfg->assign(json_path);

    auto dev_json_path = path;
    auto dev_json_name = name + _X(".runtimeconfig.dev.json");
    append_path(&dev_json_path, dev_json_name.c_str());
    dev_cfg->assign(dev_json_path);

    trace::verbose(_X("Runtime config is cfg=%s dev=%s"), json_path.c_str(), dev_json_path.c_str());
}

host_mode_t detect_operating_mode(const host_startup_info_t& host_info)
{
    if (coreclr_exists_in_dir(host_info.dotnet_root) || pal::file_exists(host_info.app_path))
    {
        pal::string_t own_deps_json = host_info.dotnet_root;
        pal::string_t own_deps_filename = host_info.get_app_name() + _X(".deps.json");
        pal::string_t own_config_filename = host_info.get_app_name() + _X(".runtimeconfig.json");
        append_path(&own_deps_json, own_deps_filename.c_str());
        if (trace::is_enabled())
        {
            trace::info(_X("Detecting mode... CoreCLR present in dotnet root [%s] and checking if [%s] file present=[%d]"),
                host_info.dotnet_root.c_str(), own_deps_filename.c_str(), pal::file_exists(own_deps_json));
        }
        return ((pal::file_exists(own_deps_json) || !pal::file_exists(own_config_filename)) && pal::file_exists(host_info.app_path)) ? host_mode_t::apphost : host_mode_t::split_fx;
    }
    else
    {
        return host_mode_t::muxer;
    }
}

void try_patch_roll_forward_in_dir(const pal::string_t& cur_dir, const fx_ver_t& start_ver, pal::string_t* max_str)
{
    pal::string_t path = cur_dir;

    if (trace::is_enabled())
    {
        pal::string_t start_str = start_ver.as_str();
        trace::verbose(_X("Reading patch roll forward candidates in dir [%s] for version [%s]"), path.c_str(), start_str.c_str());
    }

    pal::string_t maj_min_star = start_ver.patch_glob();

    std::vector<pal::string_t> list;
    pal::readdir_onlydirectories(path, maj_min_star, &list);

    fx_ver_t max_ver = start_ver;
    fx_ver_t ver(-1, -1, -1);
    for (const auto& str : list)
    {
        trace::verbose(_X("Considering patch roll forward candidate version [%s]"), str.c_str());
        if (fx_ver_t::parse(str, &ver, true))
        {
            max_ver = std::max(ver, max_ver);
        }
    }
    max_str->assign(max_ver.as_str());

    if (trace::is_enabled())
    {
        pal::string_t start_str = start_ver.as_str();
        trace::verbose(_X("Patch roll forwarded [%s] -> [%s] in [%s]"), start_str.c_str(), max_str->c_str(), path.c_str());
    }
}

void try_prerelease_roll_forward_in_dir(const pal::string_t& cur_dir, const fx_ver_t& start_ver, pal::string_t* max_str)
{
    pal::string_t path = cur_dir;

    if (trace::is_enabled())
    {
        pal::string_t start_str = start_ver.as_str();
        trace::verbose(_X("Reading prerelease roll forward candidates in dir [%s] for version [%s]"), path.c_str(), start_str.c_str());
    }

    pal::string_t maj_min_pat_star = start_ver.prerelease_glob();

    std::vector<pal::string_t> list;
    pal::readdir_onlydirectories(path, maj_min_pat_star, &list);

    fx_ver_t max_ver = start_ver;
    fx_ver_t ver(-1, -1, -1);
    for (const auto& str : list)
    {
        trace::verbose(_X("Considering prerelease roll forward candidate version [%s]"), str.c_str());
        if (fx_ver_t::parse(str, &ver, false)
            && ver.is_prerelease()) // Pre-release can roll forward to only pre-release
        {
            max_ver = std::max(ver, max_ver);
        }
    }
    max_str->assign(max_ver.as_str());

    if (trace::is_enabled())
    {
        pal::string_t start_str = start_ver.as_str();
        trace::verbose(_X("Prerelease roll forwarded [%s] -> [%s] in [%s]"), start_str.c_str(), max_str->c_str(), path.c_str());
    }
}
