// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <cassert>
#include "pal.h"
#include "sdk_info.h"
#include "trace.h"
#include "utils.h"

bool compare_by_version_ascending_then_hive_depth_descending(const sdk_info &a, const sdk_info &b)
{
    if (a.version < b.version)
    {
        return true;
    }

    // With multi-level lookup enabled, it is possible to find two SDKs with
    // the same version. For that edge case, we make the ordering put SDKs
    // from farther away (global location) hives earlier than closer ones
    // (current dotnet exe location). Without this tie-breaker, the ordering
    // would be non-deterministic.
    //
    // Furthermore,  nearer earlier than farther is so that the MSBuild resolver
    // can do a linear search from the end of the list to the front to find the
    // best compatible SDK.
    //
    // Example:
    //    * dotnet dir has version 4.0, 5.0, 6.0
    //    * global dir has 5.0
    //    * 6.0 is incompatible with calling msbuild
    //    * 5.0 is compatible with calling msbuild
    //
    // MSBuild should select 5.0 from dotnet dir (matching probe order) in muxer
    // and not 5.0 from global dir.
    if (a.version == b.version)
    {
        return a.hive_depth > b.hive_depth;
    }

    return false;
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
            if (!pal::are_paths_equal_with_normalized_casing(dir, own_dir_temp))
            {
                hive_dir.push_back(dir);
            }
        }
    }

    int32_t hive_depth = 0;

    for (pal::string_t dir : hive_dir)
    {
        auto base_dir = dir;
        trace::verbose(_X("Gathering SDK locations in [%s]"), base_dir.c_str());

        append_path(&base_dir, _X("sdk"));

        if (pal::directory_exists(base_dir))
        {
            std::vector<pal::string_t> versions;
            pal::readdir_onlydirectories(base_dir, &versions);
            for (const auto& ver : versions)
            {
                // Make sure we filter out any non-version folders.
                fx_ver_t parsed(-1, -1, -1);
                if (fx_ver_t::parse(ver, &parsed, false))
                {
                    trace::verbose(_X("Found SDK version [%s]"), ver.c_str());

                    auto full_dir = base_dir;
                    append_path(&full_dir, ver.c_str());

                    sdk_info info(base_dir, full_dir, parsed, hive_depth);

                    sdk_infos->push_back(info);
                }
            }
        }

        hive_depth++;
    }

    std::sort(sdk_infos->begin(), sdk_infos->end(), compare_by_version_ascending_then_hive_depth_descending);
}

/*static*/ bool sdk_info::print_all_sdks(const pal::string_t& own_dir, const pal::string_t& leading_whitespace)
{
    std::vector<sdk_info> sdk_infos;
    get_all_sdk_infos(own_dir, &sdk_infos);
    for (sdk_info info : sdk_infos)
    {
        trace::println(_X("%s%s [%s]"), leading_whitespace.c_str(), info.version.as_str().c_str(), info.base_path.c_str());
    }

    return sdk_infos.size() > 0;
}
