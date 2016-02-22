// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef __DEPS_ENTRY_H_
#define __DEPS_ENTRY_H_

#include <iostream>
#include <vector>
#include "pal.h"

struct deps_entry_t
{
    enum asset_types
    {
        runtime = 0,
        resources,
        native,
        count
    };

    pal::string_t library_type;
    pal::string_t library_name;
    pal::string_t library_version;
    pal::string_t library_hash;
    pal::string_t asset_type;
    pal::string_t asset_name;
    pal::string_t relative_path;
    bool is_serviceable;

    // Given a "base" dir, yield the relative path in the package layout.
    bool to_full_path(const pal::string_t& root, pal::string_t* str) const;

    // Given a "base" dir, yield the relative path in the package layout only if
    // the hash matches contents of the hash file.
    bool to_hash_matched_path(const pal::string_t& root, pal::string_t* str) const;
};

#endif // __DEPS_ENTRY_H_
