// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal.h"
#include "utils.h"
#include "deps_entry.h"
#include "trace.h"

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
        library_name.length() +
        library_version.length() +
        pal_relative_path.length() + 3);

    candidate.assign(base);
    append_path(&candidate, library_name.c_str());
    append_path(&candidate, library_version.c_str());
    append_path(&candidate, pal_relative_path.c_str());

    bool exists = pal::file_exists(candidate);
    if (!exists)
    {
        candidate.clear();
    }
    return exists;
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

    // Build the nupkg file name. Just reserve approx 8 char_t's for the algorithm name.
    pal::string_t nupkg_filename;
    nupkg_filename.reserve(library_name.length() + 1 + library_version.length() + 16);
    nupkg_filename.append(library_name);
    nupkg_filename.append(_X("."));
    nupkg_filename.append(library_version);
    nupkg_filename.append(_X(".nupkg."));
    nupkg_filename.append(library_hash.substr(0, pos));

    // Build the hash file path str.
    pal::string_t hash_file;
    hash_file.reserve(base.length() + library_name.length() + library_version.length() + nupkg_filename.length() + 3);
    hash_file.assign(base);
    append_path(&hash_file, library_name.c_str());
    append_path(&hash_file, library_version.c_str());
    append_path(&hash_file, nupkg_filename.c_str());

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
    pal::to_palstring(hash.c_str(), &pal_hash);

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
