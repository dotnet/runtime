// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal.h"
#include "utils.h"
#include "deps_entry.h"
#include "trace.h"


bool deps_entry_t::to_path(const pal::string_t& base, bool look_in_base, pal::string_t* str) const
{
    pal::string_t& candidate = *str;

    candidate.clear();

    // Base directory must be present to obtain full path
    if (base.empty())
    {
        return false;
    }

    // Entry relative path contains '/' separator, sanitize it to use
    // platform separator. Perf: avoid extra copy if it matters.
    pal::string_t pal_relative_path = relative_path;
    if (_X('/') != DIR_SEPARATOR)
    {
        replace_char(&pal_relative_path, _X('/'), DIR_SEPARATOR);
    }

    // Reserve space for the path below
    candidate.reserve(base.length() +
        pal_relative_path.length() + 3);

    candidate.assign(base);
    pal::string_t sub_path = look_in_base ? get_filename(pal_relative_path) : pal_relative_path;
    append_path(&candidate, sub_path.c_str());

    bool exists = pal::file_exists(candidate);
    const pal::char_t* query_type = look_in_base ? _X("Local") : _X("Relative");
    if (!exists)
    {
        trace::verbose(_X("    %s path query did not exist %s"), query_type, candidate.c_str());
        candidate.clear();
    }
    else
    {
        trace::verbose(_X("    %s path query exists %s"), query_type, candidate.c_str());
    }
    return exists;
}

// -----------------------------------------------------------------------------
// Given a "base" directory, yield the local path of this file
//
// Parameters:
//    base - The base directory to look for the relative path of this entry
//    str  - If the method returns true, contains the file path for this deps
//           entry relative to the "base" directory
//
// Returns:
//    If the file exists in the path relative to the "base" directory.
//
bool deps_entry_t::to_dir_path(const pal::string_t& base, pal::string_t* str) const
{
    if (asset_type == asset_types::resources)
    {
        pal::string_t pal_relative_path = relative_path;
        if (_X('/') != DIR_SEPARATOR)
        {
            replace_char(&pal_relative_path, _X('/'), DIR_SEPARATOR);
        }
        pal::string_t ietf_dir = get_directory(pal_relative_path);
        pal::string_t ietf = get_filename(ietf_dir);
        pal::string_t base_ietf_dir = base;
        append_path(&base_ietf_dir, ietf.c_str());
        trace::verbose(_X("Detected a resource asset, will query dir/ietf-tag/resource base: %s asset: %s"), base_ietf_dir.c_str(), asset_name.c_str());
        return to_path(base_ietf_dir, true, str);
    }
    return to_path(base, true, str);
}
// -----------------------------------------------------------------------------
// Given a "base" directory, yield the relative path of this file in the package
// layout.
//
// Parameters:
//    base - The base directory to look for the relative path of this entry
//    str  - If the method returns true, contains the file path for this deps
//           entry relative to the "base" directory
//
// Returns:
//    If the file exists in the path relative to the "base" directory.
//
bool deps_entry_t::to_rel_path(const pal::string_t& base, pal::string_t* str) const
{
    return to_path(base, false, str);
}

// -----------------------------------------------------------------------------
// Given a "base" directory, yield the relative path of this file in the package
// layout.
//
// Parameters:
//    base - The base directory to look for the relative path of this entry
//    str  - If the method returns true, contains the file path for this deps
//           entry relative to the "base" directory
//
// Returns:
//    If the file exists in the path relative to the "base" directory.
//
bool deps_entry_t::to_full_path(const pal::string_t& base, pal::string_t* str) const
{
    str->clear();

    // Base directory must be present to obtain full path
    if (base.empty())
    {
        return false;
    }

    pal::string_t new_base = base;

    if (library_path.empty())
    {
        append_path(&new_base, library_name.c_str());
        append_path(&new_base, library_version.c_str());
    }
    else
    {
        append_path(&new_base, library_path.c_str());
    }

    return to_rel_path(new_base, str);
}

// -----------------------------------------------------------------------------
// Given a "base" directory, yield the relative path of this file in the package
// layout if the entry hash matches the hash file in the "base" directory
//
// Parameters:
//    base - The base directory to look for the relative path of this entry and
//           the hash file.
//    str  - If the method returns true, contains the file path for this deps
//           entry relative to the "base" directory
//
// Description:
//    Looks for a file named "{PackageName}.{PackageVersion}.nupkg.{HashAlgorithm}"
//    If the deps entry's {HashAlgorithm}-{HashValue} matches the contents then
//    yields the relative path of this entry in the "base" dir.
//
// Returns:
//    If the file exists in the path relative to the "base" directory and there
//    was hash file match with this deps entry.
//
// See: to_full_path(base, str)
//
bool deps_entry_t::to_hash_matched_path(const pal::string_t& base, pal::string_t* str) const
{
    pal::string_t& candidate = *str;

    candidate.clear();

    // Base directory must be present to perform hash lookup.
    if (base.empty())
    {
        return false;
    }

    // First detect position of hyphen in [Algorithm]-[Hash] in the string.
    size_t pos = library_hash.find(_X("-"));
    if (pos == 0 || pos == pal::string_t::npos)
    {
        trace::verbose(_X("Invalid hash %s value for deps file entry: %s"), library_hash.c_str(), library_name.c_str());
        return false;
    }
    
    // Build the relative hash path (what is added to the package directory path).
    pal::string_t relative_hash_path;
    if (library_hash_path.empty())
    {
        // Reserve approx 8 char_t's for the algorithm name.
        relative_hash_path.reserve(library_name.length() + 1 + library_version.length() + 16);
        relative_hash_path.append(library_name);
        relative_hash_path.append(_X("."));
        relative_hash_path.append(library_version);
        relative_hash_path.append(_X(".nupkg."));
        relative_hash_path.append(library_hash.substr(0, pos));
    }
    else
    {
        relative_hash_path.assign(library_hash_path);
    }

    // Build the directory that contains the hash file.
    pal::string_t hash_file;
    if (library_path.empty())
    {
        hash_file.reserve(base.length() + 1 +
            library_name.length() + 1 + library_version.length() + 1 +
            relative_hash_path.length());
        hash_file.assign(base);

        append_path(&hash_file, library_name.c_str());
        append_path(&hash_file, library_version.c_str());
    }
    else
    {
        hash_file.reserve(base.length() + 1 +
            library_path.length() + 1 +
            relative_hash_path.length());
        hash_file.assign(base);

        append_path(&hash_file, library_path.c_str());
    }

    // Append the relative path to the hash file.
    append_path(&hash_file, relative_hash_path.c_str());
    
    // Read the contents of the hash file.
    pal::ifstream_t fstream(hash_file);
    if (!fstream.good())
    {
        trace::verbose(_X("The hash file is invalid [%s]"), hash_file.c_str());
        return false;
    }

    // Obtain the hash from the file.
    std::string hash;
    hash.assign(pal::istreambuf_iterator_t(fstream),
        pal::istreambuf_iterator_t());
    pal::string_t pal_hash;
    if (!pal::utf8_palstring(hash.c_str(), &pal_hash))
    {
        return false;
    }

    // Check if contents match deps entry.
    pal::string_t entry_hash = library_hash.substr(pos + 1);
    if (entry_hash != pal_hash)
    {
        trace::verbose(_X("The file hash [%s][%d] did not match entry hash [%s][%d]"),
            pal_hash.c_str(), pal_hash.length(), entry_hash.c_str(), entry_hash.length());
        return false;
    }

    // All good, just append the relative dir to base.
    return to_full_path(base, &candidate);
}
