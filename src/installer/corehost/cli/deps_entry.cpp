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

void deps_entry_t::append_resource_path(pal::string_t& base) const
{
    assert(asset_type == asset_types::resources);

    pal::string_t pal_relative_path = normalize_dir_separator(asset.relative_path);

    // Resources are represented as "lib/<netstandrd_ver>/<ietf-code>/<ResourceAssemblyName.dll>" in the deps.json.
    // The <ietf-code> is the "directory" in the pal_relative_path below, so extract it.
    pal::string_t ietf_dir = get_directory(pal_relative_path);
    pal::string_t ietf = ietf_dir;

    // get_directory returns with DIR_SEPARATOR appended that we need to remove.
    remove_trailing_dir_seperator(&ietf);

    // Extract IETF code from "lib/<netstandrd_ver>/<ietf-code>"
    ietf = get_filename(ietf);

    append_path(&base, ietf.c_str());
    trace::verbose(_X("Detected a resource asset, will query dir/ietf-tag/resource base: %s asset: %s"), base.c_str(), asset.name.c_str());
}

bool deps_entry_t::to_path(const pal::string_t& base, bool look_in_base, pal::string_t* str) const
{
    pal::string_t& candidate = *str;

    candidate.clear();

    // Base directory must be present to obtain full path
    if (base.empty())
    {
        return false;
    }

    pal::string_t pal_relative_path = normalize_dir_separator(asset.relative_path);

    // Reserve space for the path below
    candidate.reserve(base.length() + pal_relative_path.length() + 3);

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
//    str  - If the method returns true, contains the file path for this deps entry 
//
// Returns:
//    If the file exists in the path relative to the "base" directory.
//
bool deps_entry_t::to_dir_path(const pal::string_t& base, pal::string_t* str) const
{
    if (asset_type == asset_types::resources)
    {
        pal::string_t base_ietf_dir = base;
        append_resource_path(base_ietf_dir);

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
//    str  - If the method returns true, contains the file path for this deps entry 
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

    return to_rel_path(new_base, str);
}

// -----------------------------------------------------------------------------
// Given a "base" directory, if this file exists within the single-file bundle,
// return
//  * If the file was extracted to disk, the full-path to the extracted file.
//  * Otherwise, the path within the bundle, relative to the "base" directory.
//    The runtime expects the entries in the TPA,  NativeDllSearchDirectories, etc 
//    to be absolute paths. Therefore, the relative-paths within the bundle are
//    expressed as absolute paths with respect to the location of the bundle-file.
// 
// Parameters:
//    base - The directory containing the single-file bundle.
//    str  - If the method returns true, contains the file path for this deps entry 
//
// Returns:
//    If the file exists in the single-file bundle
//
bool deps_entry_t::to_bundle_path(const pal::string_t& base, pal::string_t* str) const
{
    if (!bundle::info_t::is_single_file_bundle())
    {
        return false;
    }

    const bundle::runner_t* app = bundle::runner_t::app();

    // Bundled files are only searched relative to the app-directory.
    if (base.compare(app->base_path()) != 0)
    {
        trace::verbose(_X("    Base directory %s is different from bundle-base %s"), base.c_str(), app->base_path().c_str());
        return false;
    }

    pal::string_t& candidate = *str;
    candidate.clear();

    pal::string_t file_name = get_filename(normalize_dir_separator(asset.relative_path));
    pal::string_t sub_path;
    if (asset_type == asset_types::resources)
    {
        append_resource_path(sub_path);
        sub_path.append(file_name);
    }
    else
    {
        sub_path = file_name;
    }

    bool exists = app->locate(sub_path, candidate);

    if (exists)
    {
        trace::verbose(_X("    %s found in bundle [%s]"), sub_path.c_str(), candidate.c_str());
    }
    else
    {
        trace::verbose(_X("    %s not found in bundle"), sub_path.c_str());
    }

    return exists;
}
