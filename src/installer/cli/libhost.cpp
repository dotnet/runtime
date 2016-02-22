// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal.h"
#include "utils.h"
#include "trace.h"
#include "libhost.h"

pal::string_t get_runtime_config_json(const pal::string_t& app_path)
{
    auto name = get_filename_without_ext(app_path);
    auto json_name = name + _X(".runtimeconfig.json");
    auto json_path = get_directory(app_path);

    append_path(&json_path, json_name.c_str());
    if (pal::file_exists(json_path))
    {
        return json_path;
    }
    return pal::string_t();
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

    pal::string_t own_dll_filename = strip_file_ext(own_name) + _X(".dll");
    pal::string_t own_dll = own_dir;
    append_path(&own_dll, own_dll_filename.c_str());
    trace::info(_X("Exists %s"), own_dll.c_str());
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

