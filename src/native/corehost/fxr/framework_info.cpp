// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

    if (a.version < b.version)
    {
        return true;
    }

    if (a.version == b.version)
    {
        return a.hive_depth > b.hive_depth;
    }

    return false;
}

/*static*/ void framework_info::get_all_framework_infos(
    const pal::string_t& own_dir,
    const pal::string_t& fx_name,
    std::vector<framework_info>* framework_infos)
{
    std::vector<pal::string_t> hive_dir;
    get_framework_and_sdk_locations(own_dir, &hive_dir);

    int32_t hive_depth = 0;

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
                        fx_ver_t parsed;
                        if (fx_ver_t::parse(ver, &parsed, false))
                        {
                            trace::verbose(_X("Found FX version [%s]"), ver.c_str());

                            framework_info info(fx_name, fx_dir, parsed, hive_depth);
                            framework_infos->push_back(info);
                        }
                    }
                }
            }
        }

        hive_depth++;
    }

    std::sort(framework_infos->begin(), framework_infos->end(), compare_by_name_and_version);
}

/*static*/ bool framework_info::print_all_frameworks(const pal::string_t& own_dir, const pal::string_t& leading_whitespace)
{
    std::vector<framework_info> framework_infos;
    get_all_framework_infos(own_dir, _X(""), &framework_infos);
    for (framework_info info : framework_infos)
    {
        trace::println(_X("%s%s %s [%s]"), leading_whitespace.c_str(), info.name.c_str(), info.version.as_str().c_str(), info.path.c_str());
    }

    return framework_infos.size() > 0;
}
