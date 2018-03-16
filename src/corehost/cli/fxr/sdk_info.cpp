// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <cassert>
#include "pal.h"
#include "sdk_info.h"
#include "trace.h"
#include "utils.h"

bool compare_by_version(const sdk_info &a, const sdk_info &b)
{
    return a.version < b.version;
}

void sdk_info::get_all_sdk_infos(
    const pal::string_t& own_dir,
    std::vector<sdk_info>* sdk_infos)
{
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
        auto sdk_dir = dir;
        trace::verbose(_X("Gathering SDK locations in [%s]"), sdk_dir.c_str());

        append_path(&sdk_dir, _X("sdk"));

        if (pal::directory_exists(sdk_dir))
        {
            std::vector<pal::string_t> versions;
            pal::readdir_onlydirectories(sdk_dir, &versions);
            for (const auto& ver : versions)
            {
                // Make sure we filter out any non-version folders.
                fx_ver_t parsed(-1, -1, -1);
                if (fx_ver_t::parse(ver, &parsed, false))
                {
                    trace::verbose(_X("Found SDK version [%s]"), ver.c_str());

                    sdk_info info(sdk_dir, parsed);

                    sdk_infos->push_back(info);
                }
            }
        }
    }

    std::sort(sdk_infos->begin(), sdk_infos->end(), compare_by_version);
}

/*static*/ bool sdk_info::print_all_sdks(const pal::string_t& own_dir, const pal::string_t& leading_whitespace)
{
    std::vector<sdk_info> sdk_infos;
    get_all_sdk_infos(own_dir, &sdk_infos);
    for (sdk_info info : sdk_infos)
    {
        trace::println(_X("%s%s [%s]"), leading_whitespace.c_str(), info.version.as_str().c_str(), info.path.c_str());
    }

    return sdk_infos.size() > 0;
}
