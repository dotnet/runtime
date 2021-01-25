// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __FRAMEWORK_INFO_H_
#define __FRAMEWORK_INFO_H_

#include "pal.h"
#include "fx_ver.h"

struct framework_info
{
    framework_info(pal::string_t name, pal::string_t path, fx_ver_t version)
        : name(name)
        , path(path)
        , version(version) { }

    static void get_all_framework_infos(
        const pal::string_t& own_dir,
        const pal::string_t& fx_name,
        std::vector<framework_info>* framework_infos);

    static bool print_all_frameworks(const pal::string_t& own_dir, const pal::string_t& leading_whitespace);

    pal::string_t name;
    pal::string_t path;
    fx_ver_t version;
};

#endif // __FRAMEWORK_INFO_H_
