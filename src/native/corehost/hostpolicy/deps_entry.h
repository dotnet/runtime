// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __DEPS_ENTRY_H_
#define __DEPS_ENTRY_H_

#include <iostream>
#include <array>
#include <vector>
#include "pal.h"
#include "version.h"

struct deps_asset_t
{
    deps_asset_t() : deps_asset_t(_X(""), _X(""), version_t(), version_t(), _X("")) { }

    deps_asset_t(const pal::string_t& name, const pal::string_t& relative_path, const version_t& assembly_version, const version_t& file_version)
        : deps_asset_t(name, relative_path, assembly_version, file_version, _X("")) { }

    deps_asset_t(const pal::string_t& name, const pal::string_t& relative_path, const version_t& assembly_version, const version_t& file_version, const pal::string_t& local_path)
        : name(name)
        , relative_path(get_replaced_char(relative_path, _X('\\'), _X('/'))) // Deps file does not follow spec. It uses '\\', should use '/'
        , assembly_version(assembly_version)
        , file_version(file_version)
        , local_path(local_path.empty() ? pal::string_t() : get_replaced_char(local_path, _X('\\'), _X('/'))) { }

    pal::string_t name;
    pal::string_t relative_path;
    version_t assembly_version;
    version_t file_version;
    pal::string_t local_path;
};

struct deps_entry_t
{
    enum asset_types
    {
        runtime = 0,
        resources,
        native,
        count
    };

    enum search_options : uint32_t
    {
        none = 0x0,
        // unused = 0x1,
        look_in_bundle = 0x2,   // Look for entry within the single-file bundle
        is_servicing = 0x4,     // Whether the base directory is the core-servicing directory
        file_existence = 0x8,   // Check for entry file existence
    };

    static const std::array<const pal::char_t*, deps_entry_t::asset_types::count> s_known_asset_types;

    pal::string_t deps_file;
    pal::string_t library_type;
    pal::string_t library_name;
    pal::string_t library_version;
    pal::string_t library_hash;
    pal::string_t library_path;
    pal::string_t library_hash_path;
    pal::string_t runtime_store_manifest_list;
    asset_types asset_type;
    deps_asset_t asset;
    bool is_serviceable;
    bool is_rid_specific;

    // Given a "base" dir, yield the file path within this directory or single-file bundle.
    bool to_dir_path(const pal::string_t& base, pal::string_t* str, uint32_t search_options, bool& found_in_bundle) const;

    // Given a "base" dir, yield the relative path in the package layout or servicing directory.
    bool to_package_path(const pal::string_t& base, pal::string_t* str, uint32_t search_options) const;

    // Given a "base" dir, yield the relative path with package name/version in the package layout or servicing location.
    bool to_library_package_path(const pal::string_t& base, pal::string_t* str, uint32_t search_options) const;
};

#endif // __DEPS_ENTRY_H_
