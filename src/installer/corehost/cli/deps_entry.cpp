// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal.h"
#include "utils.h"
#include "deps_entry.h"
#include "trace.h"
#include "bundle/runner.h"

static pal::string_t normalize_dir_separator(const pal::string_t& path)
{
    // Entry relative path contains '/' separator, sanitize it to use
    // platform separator. Perf: avoid extra copy if it matters.
    pal::string_t normalized_path = path;
    if (_X('/') != DIR_SEPARATOR)
    {
        replace_char(&normalized_path, _X('/'), DIR_SEPARATOR);
    }

    return normalized_path;
}

// -----------------------------------------------------------------------------
// Given a "base" directory, determine the resolved path for this file.
//
// * If this file exists within the single-file bundle candidate is
//   the full-path to the extracted file.
// * Otherwise, candidate is the full local path of the file.
//
// Parameters:
//    base - The base directory to look for the relative path of this entry
//    ietf_dir - If this is a resource asset, the IETF intermediate directory
//    look_in_base - Whether to search as a relative path
//    look_in_bundle - Whether to look within the single-file bundle
//    str  - If the method returns true, contains the file path for this deps entry
//    
// Returns:
//    If the file exists in the path relative to the "base" directory within the 
//    single-file or on disk.
bool deps_entry_t::to_path(const pal::string_t& base, const pal::string_t& ietf_dir, bool look_in_base, bool look_in_bundle, pal::string_t* str) const
{
    pal::string_t& candidate = *str;

    candidate.clear();

    // Base directory must be present to obtain full path
    if (base.empty())
    {
        return false;
    }

    pal::string_t normalized_path = normalize_dir_separator(asset.relative_path);

    // Reserve space for the path below
    candidate.reserve(base.length() + ietf_dir.length() + normalized_path.length() + 3);

    pal::string_t file_path = look_in_base ? get_filename(normalized_path) : normalized_path;
    pal::string_t sub_path = ietf_dir;
    append_path(&sub_path, file_path.c_str());

    if (look_in_bundle && bundle::info_t::is_single_file_bundle())
    {
        const bundle::runner_t* app = bundle::runner_t::app();

        if (base.compare(app->base_path()) == 0)
        {
            // If sub_path is found in the single-file bundle,
            // app::locate() will set candidate to the full-path to the assembly extracted out to disk.
            if (app->locate(sub_path, candidate))
            {
                trace::verbose(_X("    %s found in bundle [%s]"), sub_path.c_str(), candidate.c_str());
                return true;
            }
            else
            {
                trace::verbose(_X("    %s not found in bundle"), sub_path.c_str());
            }
        }
        else
        {
            trace::verbose(_X("    %s not searched in bundle base path %s doesn't match bundle base %s."),
                             sub_path.c_str(), base.c_str(), app->base_path().c_str());
        }
    }

    candidate.assign(base);
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
//    str  - If the method returns true, contains the file path for this deps entry 
//
// Returns:
//    If the file exists in the path relative to the "base" directory.
//
bool deps_entry_t::to_dir_path(const pal::string_t& base, bool look_in_bundle, pal::string_t* str) const
{
    pal::string_t ietf_dir;

    if (asset_type == asset_types::resources)
    {
        pal::string_t pal_relative_path = normalize_dir_separator(asset.relative_path);

        // Resources are represented as "lib/<netstandrd_ver>/<ietf-code>/<ResourceAssemblyName.dll>" in the deps.json.
        // The <ietf-code> is the "directory" in the pal_relative_path below, so extract it.
        ietf_dir = get_directory(pal_relative_path);

        // get_directory returns with DIR_SEPARATOR appended that we need to remove.
        remove_trailing_dir_seperator(&ietf_dir);

        // Extract IETF code from "lib/<netstandrd_ver>/<ietf-code>"
        ietf_dir = get_filename(ietf_dir);

        trace::verbose(_X("Detected a resource asset, will query dir/ietf-tag/resource base: %s ietf: %s asset: %s"),
                        base.c_str(), ietf_dir.c_str(), asset.name.c_str());
    }

    return to_path(base, ietf_dir, true, look_in_bundle, str);
}

// -----------------------------------------------------------------------------
// Given a "base" directory, yield the relative path of this file in the package
// layout.
//
// Parameters:
//    base - The base directory to look for the relative path of this entry
//    str  - If the method returns true, contains the file path for this deps entry 
//
// Returns:
//    If the file exists in the path relative to the "base" directory.
//
bool deps_entry_t::to_rel_path(const pal::string_t& base, bool look_in_bundle, pal::string_t* str) const
{
    return to_path(base, _X(""), false, look_in_bundle, str);
}

// -----------------------------------------------------------------------------
// Given a "base" directory, yield the relative path of this file in the package
// layout.
//
// Parameters:
//    base - The base directory to look for the relative path of this entry
//    str  - If the method returns true, contains the file path for this deps entry 
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

    return to_rel_path(new_base, false, str);
}
