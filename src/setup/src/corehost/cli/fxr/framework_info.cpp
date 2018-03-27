// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <cassert>
#include "framework_info.h"
#include "pal.h"
#include "trace.h"
#include "utils.h"

bool compare_by_name_and_version(const framework_info &a, const framework_info &b)
{
    if (a.name < b.name)
    {
        return true;
    }

    if (a.name > b.name)
    {
        return false;
    }

    return a.version < b.version;
}

/*static*/ void framework_info::get_all_framework_infos(
    host_mode_t mode,
    const pal::string_t& own_dir,
    const pal::string_t& fx_name,
    std::vector<framework_info>* framework_infos)
{
    // No FX resolution for mixed apps 
    if (mode == host_mode_t::split_fx)
    {
        trace::verbose(_X("Split/FX mode detected. Not gathering shared FX locations"));
        return;
    }

    std::vector<pal::string_t> global_dirs;
    bool multilevel_lookup = multilevel_lookup_enabled();

    // own_dir contains DIR_SEPARATOR appended that we need to remove.
    pal::string_t own_dir_temp = own_dir;
    remove_trailing_dir_seperator(&own_dir_temp);

    std::vector<pal::string_t> hive_dir;
    hive_dir.push_back(own_dir_temp);

    if (multilevel_lookup && pal::get_global_dotnet_dirs(&global_dirs))
    {
        for (pal::string_t dir : global_dirs)
        {
            if (dir != own_dir_temp)
            {
                hive_dir.push_back(dir);
            }
        }
    }

    for (pal::string_t dir : hive_dir)
    {
        auto fx_shared_dir = dir;
        append_path(&fx_shared_dir, _X("shared"));

        if (pal::directory_exists(fx_shared_dir))
        {
            std::vector<pal::string_t> fx_names;
            if (fx_name.length())
            {
                // Use the provided framework name
                fx_names.push_back(fx_name);
            }
            else
            {
                // Read all frameworks, including "Microsoft.NETCore.App"
                pal::readdir_onlydirectories(fx_shared_dir, &fx_names);
            }

            for (pal::string_t fx_name : fx_names)
            {
                auto fx_dir = fx_shared_dir;
                append_path(&fx_dir, fx_name.c_str());

                if (pal::directory_exists(fx_dir))
                {
                    trace::verbose(_X("Gathering FX locations in [%s]"), fx_dir.c_str());

                    std::vector<pal::string_t> versions;
                    pal::readdir_onlydirectories(fx_dir, &versions);
                    for (const auto& ver : versions)
                    {
                        // Make sure we filter out any non-version folders.
                        fx_ver_t parsed(-1, -1, -1);
                        if (fx_ver_t::parse(ver, &parsed, false))
                        {
                            trace::verbose(_X("Found FX version [%s]"), ver.c_str());

                            framework_info info(fx_name, fx_dir, parsed);
                            framework_infos->push_back(info);
                        }
                    }
                }
            }
        }
    }

    std::sort(framework_infos->begin(), framework_infos->end(), compare_by_name_and_version);
}

/*static*/ bool framework_info::print_all_frameworks(const pal::string_t& own_dir, const pal::string_t& leading_whitespace)
{
    std::vector<framework_info> framework_infos;
    get_all_framework_infos(host_mode_t::muxer, own_dir, _X(""), &framework_infos);
    for (framework_info info : framework_infos)
    {
        trace::println(_X("%s%s %s [%s]"), leading_whitespace.c_str(), info.name.c_str(), info.version.as_str().c_str(), info.path.c_str());
    }

    return framework_infos.size() > 0;
}
