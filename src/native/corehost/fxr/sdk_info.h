// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __SDK_INFO_H_
#define __SDK_INFO_H_

#include "pal.h"
#include "fx_ver.h"

struct sdk_info
{
    sdk_info(const pal::string_t& base_path, const pal::string_t& full_path, const fx_ver_t& version, int32_t hive_depth)
        : base_path(base_path)
        , full_path(full_path)
        , version(version)
        , hive_depth(hive_depth) { }

    static void get_all_sdk_infos(
        const pal::string_t& own_dir,
        std::vector<sdk_info>* sdk_infos);

    static bool print_all_sdks(const pal::string_t& own_dir, const pal::string_t& leading_whitespace);

    pal::string_t base_path;
    pal::string_t full_path;
    fx_ver_t version;
    int32_t hive_depth;
};

#endif // __SDK_INFO_H_
