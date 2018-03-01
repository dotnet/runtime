// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef __SDK_INFO_H_
#define __SDK_INFO_H_

#include "libhost.h"

struct sdk_info
{
    sdk_info(pal::string_t path, fx_ver_t version)
        : path(path)
        , version(version) { }

    static void get_all_sdk_infos(
        const pal::string_t& own_dir,
        std::vector<sdk_info>* sdk_infos);

    static bool print_all_sdks(const pal::string_t& own_dir, const pal::string_t& leading_whitespace);

    pal::string_t path;
    fx_ver_t version;
};

#endif // __SDK_INFO_H_
