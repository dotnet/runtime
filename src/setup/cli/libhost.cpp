// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal.h"
#include "utils.h"
#include "trace.h"
#include "libhost.h"

pal::string_t get_runtime_config_from_file(const pal::string_t& file, pal::string_t* dev_cfg)
{
    auto name = get_filename_without_ext(file);
    auto json_name = name + _X(".runtimeconfig.json");
    auto dev_json_name = name + _X(".runtimeconfig.dev.json");
    auto json_path = get_directory(file);
    auto dev_json_path = json_path;

    append_path(&json_path, json_name.c_str());
    append_path(&dev_json_path, dev_json_name.c_str());
    trace::verbose(_X("Runtime config is cfg=%s dev=%s"), json_path.c_str(), dev_json_path.c_str());

    dev_cfg->assign(dev_json_path);
    return json_path;
}

host_mode_t detect_operating_mode(const int argc, const pal::char_t* argv[], pal::string_t* p_own_dir)
{
    pal::string_t own_path;
    if (!pal::get_own_executable_path(&own_path) || !pal::realpath(&own_path))
    {
        trace::error(_X("Failed to locate current executable"));
        return host_mode_t::invalid;
    }

    pal::string_t own_name = get_filename(own_path);
    pal::string_t own_dir = get_directory(own_path);
    if (p_own_dir)
    {
        p_own_dir->assign(own_dir);
    }

    pal::string_t own_dll_filename = get_executable(own_name) + _X(".dll");
    pal::string_t own_dll = own_dir;
    append_path(&own_dll, own_dll_filename.c_str());
    trace::info(_X("Own DLL path=[%s]"), own_dll.c_str());
    if (coreclr_exists_in_dir(own_dir) || pal::file_exists(own_dll))
    {
        pal::string_t own_deps_json = own_dir;
        pal::string_t own_deps_filename = strip_file_ext(own_name) + _X(".deps.json");
        pal::string_t own_config_filename = strip_file_ext(own_name) + _X(".runtimeconfig.json");
        append_path(&own_deps_json, own_deps_filename.c_str());
        if (trace::is_enabled())
        {
            trace::info(_X("Detecting mode... CoreCLR present in own dir [%s] and checking if [%s] file present=[%d]"),
                own_dir.c_str(), own_deps_filename.c_str(), pal::file_exists(own_deps_json));
        }
        return ((pal::file_exists(own_deps_json) || !pal::file_exists(own_config_filename)) && pal::file_exists(own_dll)) ? host_mode_t::standalone : host_mode_t::split_fx;
    }
    else
    {
        return host_mode_t::muxer;
    }
}

void try_patch_roll_forward_in_dir(const pal::string_t& cur_dir, const fx_ver_t& start_ver, pal::string_t* max_str, bool only_production)
{
    pal::string_t path = cur_dir;

    if (trace::is_enabled())
    {
        pal::string_t start_str = start_ver.as_str();
        trace::verbose(_X("Reading roll forward candidates in dir [%s] for version [%s]"), path.c_str(), start_str.c_str());
    }

    pal::string_t maj_min_star = pal::to_string(start_ver.get_major()) + _X(".") + pal::to_string(start_ver.get_minor()) + _X("*");

    std::vector<pal::string_t> list;
    pal::readdir(path, maj_min_star, &list);

    fx_ver_t max_ver = start_ver;
    fx_ver_t ver(-1, -1, -1);
    for (const auto& str : list)
    {
        trace::verbose(_X("Considering roll forward candidate version [%s]"), str.c_str());
        if (fx_ver_t::parse(str, &ver, only_production))
        {
            max_ver = std::max(ver, max_ver);
        }
    }
    max_str->assign(max_ver.as_str());

    if (trace::is_enabled())
    {
        pal::string_t start_str = start_ver.as_str();
        trace::verbose(_X("Roll forwarded [%s] -> [%s] in [%s]"), start_str.c_str(), max_str->c_str(), path.c_str());
    }
}
