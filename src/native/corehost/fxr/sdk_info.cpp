// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <cassert>
#include "pal.h"
#include "sdk_info.h"
#include "sdk_resolver.h"
#include "trace.h"
#include "utils.h"

bool compare_by_version_ascending_then_hive_depth_descending(const sdk_info &a, const sdk_info &b)
{
    if (a.version < b.version)
    {
        return true;
    }

    // When global.json specifies custom SDK paths, it is possible to find two
    // SDKs with the same version in different search locations. Without a
    // tie-breaker, the ordering would be non-deterministic.
    //
    // Consumers of this list (the MSBuild resolver via hostfxr_get_available_sdks)
    // scan backwards from the end to find the best compatible SDK, so the entry
    // that should win has to come last. Search locations are in priority order -
    // the first match wins - so the lowest hive depth sorts last.
    if (a.version == b.version)
    {
        return a.hive_depth > b.hive_depth;
    }

    return false;
}

void sdk_info::enumerate_sdk_paths(
    const pal::string_t& sdk_dir,
    std::function<bool(const fx_ver_t&, const pal::string_t&)> should_skip_version,
    std::function<void(const fx_ver_t&, const pal::string_t&, const pal::string_t&)> callback)
{
    std::vector<pal::string_t> versions;
    pal::readdir_onlydirectories(sdk_dir, &versions);
    for (const pal::string_t& version_str : versions)
    {
        // Make sure we filter out any non-version folders.
        fx_ver_t version;
        if (!fx_ver_t::parse(version_str, &version))
        {
            trace::verbose(_X("Ignoring invalid version [%s]"), version_str.c_str());
            continue;
        }

        if (should_skip_version(version, version_str))
            continue;

        // Check for the existence of dotnet.dll
        pal::string_t sdk_version_dir = sdk_dir;
        append_path(&sdk_version_dir, version_str.c_str());
        if (!file_exists_in_dir(sdk_version_dir, SDK_DOTNET_DLL, nullptr))
        {
            trace::verbose(_X("Ignoring version [%s] without ") SDK_DOTNET_DLL, version_str.c_str());
            continue;
        }

        callback(version, version_str, sdk_version_dir);
    }
}

void sdk_info::get_all_sdk_infos(
    const pal::string_t& dotnet_dir,
    std::vector<sdk_info>* sdk_infos)
{
    sdk_resolver resolver = sdk_resolver::from_nearest_global_file();
    std::vector<pal::string_t> hive_dir = resolver.get_search_paths(dotnet_dir);

    int32_t hive_depth = 0;
    for (pal::string_t dir : hive_dir)
    {
        trace::verbose(_X("Gathering SDK locations in [%s]"), dir.c_str());
        append_path(&dir, _X("sdk"));
        enumerate_sdk_paths(
            dir,
            [](const fx_ver_t&, const pal::string_t&) { return false; },
            [&](const fx_ver_t& version, const pal::string_t& version_str, const pal::string_t& full_path)
            {
                trace::verbose(_X("Found SDK version [%s]"), version_str.c_str());
                sdk_info info(dir, full_path, version, hive_depth);
                sdk_infos->push_back(info);
            }
        );

        hive_depth++;
    }

    std::sort(sdk_infos->begin(), sdk_infos->end(), compare_by_version_ascending_then_hive_depth_descending);
}

/*static*/ bool sdk_info::print_all_sdks(const pal::string_t& dotnet_dir, const pal::char_t* leading_whitespace)
{
    assert(leading_whitespace != nullptr);

    std::vector<sdk_info> sdk_infos;
    get_all_sdk_infos(dotnet_dir, &sdk_infos);
    for (sdk_info info : sdk_infos)
    {
        trace::println(_X("%s%s [%s]"), leading_whitespace, info.version.as_str().c_str(), info.base_path.c_str());
    }

    return sdk_infos.size() > 0;
}
