// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    std::vector<pal::string_t> hive_dir;
    get_framework_and_sdk_locations(own_dir, /*disable_multilevel_lookup*/ true, &hive_dir);

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
                fx_ver_t parsed;
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
