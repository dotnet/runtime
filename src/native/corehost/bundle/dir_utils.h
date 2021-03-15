// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __DIR_UTIL_H__
#define __DIR_UTIL_H__

#include <cstdint>
#include "pal.h"

namespace bundle
{
    class dir_utils_t
    {
    public:
        static bool has_dirs_in_path(const pal::string_t &path);
        static void remove_directory_tree(const pal::string_t &path);
        static void create_directory_tree(const pal::string_t &path);
        static void fixup_path_separator(pal::string_t& path);
        static bool rename_with_retries(pal::string_t& old_name, pal::string_t& new_name, bool &new_dir_exists);
    };
}

#endif // __DIR_UTIL_H__
