// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
//    str  - (out parameter) If the method returns true, contains the file path for this deps entry
//    search_options - Flags to instruct where to look for this deps entry
//    found_in_bundle - (out parameter) True if the candidate is located within the single-file bundle.
//
// Returns:
//    If the file exists in the path relative to the "base" directory within the
//    single-file or on disk.

bool deps_entry_t::to_path(const pal::string_t& base, const pal::string_t& ietf_dir, pal::string_t* str, uint32_t search_options, bool &found_in_bundle) const
{
    pal::string_t& candidate = *str;

    candidate.clear();
    found_in_bundle = false;

    // Base directory must be present to obtain full path
    if (base.empty())
    {
        return false;
    }

    pal::string_t normalized_path = normalize_dir_separator(asset.relative_path);

    // Reserve space for the path below
    candidate.reserve(base.length() + ietf_dir.length() + normalized_path.length() + 3);

    bool look_in_base = search_options & deps_entry_t::search_options::look_in_base;
    bool look_in_bundle = search_options & deps_entry_t::search_options::look_in_bundle;
    bool is_servicing = search_options & deps_entry_t::search_options::is_servicing;
    pal::string_t file_path = look_in_base ? get_filename(normalized_path) : normalized_path;
    pal::string_t sub_path = ietf_dir;
    append_path(&sub_path, file_path.c_str());

    assert(!is_servicing || !look_in_bundle);

    if (look_in_bundle && bundle::info_t::is_single_file_bundle())
    {
        const bundle::runner_t* app = bundle::runner_t::app();

        if (app->has_base(base))
        {
            // If sub_path is found in the single-file bundle,
            // app::locate() will set candidate to the full-path to the assembly extracted out to disk.
            bool extracted_to_disk = false;
            if (app->locate(sub_path, candidate, extracted_to_disk))
            {
                found_in_bundle = !extracted_to_disk;
                trace::verbose(_X("    %s found in bundle [%s] %s"), sub_path.c_str(), candidate.c_str(), extracted_to_disk ? _X("(extracted)") : _X(""));
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

    const pal::char_t* query_type = look_in_base ? _X("Local") : _X("Relative");
    if (search_options & deps_entry_t::search_options::file_existence)
    {
        if (!pal::file_exists(candidate))
        {
            trace::verbose(_X("    %s path query did not exist %s"), query_type, candidate.c_str());
            candidate.clear();
            return false;
        }

        trace::verbose(_X("    %s path query exists %s"), query_type, candidate.c_str());
    }
    else
    {
        trace::verbose(_X("    %s path query %s (skipped file existence check)"), query_type, candidate.c_str());
    }

    // If a file is resolved to the servicing directory, mark it as disabled in the bundle.
    // This step is necessary because runtime will try to resolve assemblies from the bundle
    // before it uses the TPA. So putting the servicing entry into TPA is not enough, since runtime would
    // resolve it from the bundle first anyway. Disabling the file's entry in the bundle
    // ensures that the servicing entry in the TPA gets priority.
    if (is_servicing && bundle::info_t::is_single_file_bundle())
    {
        bundle::runner_t* app = bundle::runner_t::mutable_app();
        assert(!app->has_base(base));
        assert(!found_in_bundle);

        if (app->disable(sub_path))
        {
            trace::verbose(_X("    %s disabled in bundle because of servicing override %s"), sub_path.c_str(), candidate.c_str());
        }
    }

    return true;
}

// -----------------------------------------------------------------------------
// Given a "base" directory, yield the local path of this file
//
// Parameters:
//    base - The base directory to look for the relative path of this entry
//    str  - If the method returns true, contains the file path for this deps entry
//    search_options - Flags to instruct where to look for this deps entry
//    look_in_bundle - Whether to look within the single-file bundle
//
// Returns:
//    If the file exists in the path relative to the "base" directory.
//
bool deps_entry_t::to_dir_path(const pal::string_t& base, pal::string_t* str, uint32_t search_options, bool& found_in_bundle) const
{
    pal::string_t ietf_dir;

    if (asset_type == asset_types::resources)
    {
        pal::string_t pal_relative_path = normalize_dir_separator(asset.relative_path);

        // Resources are represented as "lib/<netstandrd_ver>/<ietf-code>/<ResourceAssemblyName.dll>" in the deps.json.
        // The <ietf-code> is the "directory" in the pal_relative_path below, so extract it.
        ietf_dir = get_directory(pal_relative_path);

        // get_directory returns with DIR_SEPARATOR appended that we need to remove.
        remove_trailing_dir_separator(&ietf_dir);

        // Extract IETF code from "lib/<netstandrd_ver>/<ietf-code>"
        ietf_dir = get_filename(ietf_dir);

        trace::verbose(_X("Detected a resource asset, will query dir/ietf-tag/resource base: %s ietf: %s asset: %s"),
                        base.c_str(), ietf_dir.c_str(), asset.name.c_str());
    }


    search_options |= deps_entry_t::search_options::look_in_base;
    search_options &= ~deps_entry_t::search_options::is_servicing;
    return to_path(base, ietf_dir, str, search_options, found_in_bundle);
}

// -----------------------------------------------------------------------------
// Given a "base" directory, yield the relative path of this file in the package
// layout or servicing location.
//
// Parameters:
//    base - The base directory to look for the relative path of this entry
//    str  - If the method returns true, contains the file path for this deps entry
//    search_options - Flags to instruct where to look for this deps entry
//
// Returns:
//    If the file exists in the path relative to the "base" directory.
//
bool deps_entry_t::to_rel_path(const pal::string_t& base, pal::string_t* str, uint32_t search_options) const
{
    bool found_in_bundle;
    search_options &= ~deps_entry_t::search_options::look_in_base;
    bool result = to_path(base, _X(""), str, search_options, found_in_bundle);
    assert(!found_in_bundle);
    return result;
}

// -----------------------------------------------------------------------------
// Given a "base" directory, yield the relative path of this file in the package
// layout.
//
// Parameters:
//    base - The base directory to look for the relative path of this entry
//    str  - If the method returns true, contains the file path for this deps entry
//    search_options - Flags to instruct where to look for this deps entry
//
// Returns:
//    If the file exists in the path relative to the "base" directory.
//
bool deps_entry_t::to_full_path(const pal::string_t& base, pal::string_t* str, uint32_t search_options) const
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

    search_options &= ~deps_entry_t::search_options::look_in_bundle;
    return to_rel_path(new_base, str, search_options);
}
