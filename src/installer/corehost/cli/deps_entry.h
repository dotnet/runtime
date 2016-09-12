// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef __DEPS_ENTRY_H_
#define __DEPS_ENTRY_H_

#include <iostream>
#include <array>
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

    static const std::array<const pal::char_t*, deps_entry_t::asset_types::count> s_known_asset_types;

    pal::string_t library_type;
    pal::string_t library_name;
    pal::string_t library_version;
    pal::string_t library_hash;
    pal::string_t library_path;
    pal::string_t library_hash_path;
    asset_types asset_type;
    pal::string_t asset_name;
    pal::string_t relative_path;
    bool is_serviceable;
    bool is_rid_specific;


    // Given a "base" dir, yield the filepath within this directory or relative to this directory based on "look_in_base"
    bool to_path(const pal::string_t& base, bool look_in_base, pal::string_t* str) const;

    // Given a "base" dir, yield the file path within this directory.
    bool to_dir_path(const pal::string_t& base, pal::string_t* str) const;

    // Given a "base" dir, yield the relative path in the package layout.
    bool to_rel_path(const pal::string_t& base, pal::string_t* str) const;

    // Given a "base" dir, yield the relative path with package name, version in the package layout.
    bool to_full_path(const pal::string_t& root, pal::string_t* str) const;

    // Given a "base" dir, yield the relative path with package name, version in the package layout only if
    // the hash matches contents of the hash file.
    bool to_hash_matched_path(const pal::string_t& root, pal::string_t* str) const;
};

#endif // __DEPS_ENTRY_H_
