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
// * If this file exists within the single-file bundle:
//     - if extracted, candidate is the full-path to the extracted file.
//     - if not extracted, candidate is empty.
// * Otherwise, candidate is the full local path of the file.
//
// Parameters:
//    base - The base directory to look for the relative path of this entry
//    relative_path - Relative path of to look for this entry
//    str  - (out) If the method returns true, contains the file path for this deps entry
//    search_options - Flags to instruct where to look for this deps entry
//    found_in_bundle - (out) True if the candidate is located within the single-file bundle and not extracted.
//
// Returns:
//    If the file exists in the path relative to the "base" directory within the
//    single-file or on disk.

static bool to_path(const pal::string_t& base, const pal::string_t& relative_path, pal::string_t* str, uint32_t search_options, bool &found_in_bundle)
{
    pal::string_t& candidate = *str;

    candidate.clear();
    found_in_bundle = false;

    // Base directory must be present to obtain full path
    if (base.empty())
    {
        return false;
    }

    // Reserve space for the path below
    candidate.reserve(base.length() + relative_path.length() + 2); // +2 for directory separator and null terminator

    bool look_in_bundle = search_options & deps_entry_t::search_options::look_in_bundle;
    bool is_servicing = search_options & deps_entry_t::search_options::is_servicing;

    assert(!is_servicing || !look_in_bundle);

    if (look_in_bundle && bundle::info_t::is_single_file_bundle())
    {
        const bundle::runner_t* app = bundle::runner_t::app();

        if (app->has_base(base))
        {
            // If relative_path is found in the single-file bundle,
            // app::locate() will set candidate to the full-path to the assembly extracted out to disk.
            bool extracted_to_disk = false;
            if (app->locate(relative_path, candidate, extracted_to_disk))
            {
                found_in_bundle = !extracted_to_disk;
                trace::verbose(_X("    %s found in bundle [%s] %s"), relative_path.c_str(), candidate.c_str(), extracted_to_disk ? _X("(extracted)") : _X(""));
                return true;
            }
            else
            {
                trace::verbose(_X("    %s not found in bundle"), relative_path.c_str());
            }
        }
        else
        {
            trace::verbose(_X("    %s not searched in bundle base path %s doesn't match bundle base %s."),
                             relative_path.c_str(), base.c_str(), app->base_path().c_str());
        }
    }

    candidate.assign(base);
    append_path(&candidate, relative_path.c_str());

    if (search_options & deps_entry_t::search_options::file_existence)
    {
        if (!pal::file_exists(candidate))
        {
            trace::verbose(_X("    Does not exist: %s"), candidate.c_str());
            candidate.clear();
            return false;
        }

        trace::verbose(_X("    Exists: %s"), candidate.c_str());
    }
    else
    {
        trace::verbose(_X("    Skipped file existence check: %s"), candidate.c_str());
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

        if (app->disable(relative_path))
        {
            trace::verbose(_X("    %s disabled in bundle because of servicing override %s"), relative_path.c_str(), candidate.c_str());
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
//    found_in_bundle - Whether the asset was found in the single-file bundle
//
// Returns:
//    If the file exists in the path relative to the "base" directory.
//
bool deps_entry_t::to_dir_path(const pal::string_t& base, pal::string_t* str, uint32_t search_options, bool& found_in_bundle) const
{
    pal::string_t relative_path = normalize_dir_separator(asset.local_path);
    if (relative_path.empty())
    {
        relative_path = normalize_dir_separator(asset.relative_path);
        if (library_type != _X("runtimepack")) // runtimepack assets set the path to the local path
        {
            pal::string_t file_name = get_filename(relative_path);

            // Compute the expected relative path for this asset.
            //   resource: <ietf-code>/<asset_file_name>
            //   runtime/native: <asset_file_name>
            if (asset_type == asset_types::resources)
            {
                // Resources are represented as "lib/<netstandrd_ver>/<ietf-code>/<ResourceAssemblyName.dll>" in the deps.json.
                // The <ietf-code> is the "directory" in the relative_path below, so extract it.
                pal::string_t ietf_dir = get_directory(relative_path);

                // get_directory returns with DIR_SEPARATOR appended that we need to remove.
                assert(ietf_dir.back() == DIR_SEPARATOR);
                remove_trailing_dir_separator(&ietf_dir);

                // Extract IETF code from "lib/<netstandrd_ver>/<ietf-code>"
                ietf_dir = get_filename(ietf_dir);

                trace::verbose(_X("  Detected a resource asset, will query <base>/<ietf>/<file_name> base: %s ietf: %s asset: %s"),
                    base.c_str(), ietf_dir.c_str(), asset.name.c_str());

                relative_path = ietf_dir;
                append_path(&relative_path, file_name.c_str());
            }
            else
            {
                relative_path = std::move(file_name);
            }
        }

        trace::verbose(_X("  Computed relative path: %s"), relative_path.c_str());
    }

    search_options &= ~deps_entry_t::search_options::is_servicing;
    return to_path(base, relative_path, str, search_options, found_in_bundle);
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
bool deps_entry_t::to_package_path(const pal::string_t& base, pal::string_t* str, uint32_t search_options) const
{
    bool found_in_bundle;
    bool result = to_path(base, normalize_dir_separator(asset.relative_path), str, search_options, found_in_bundle);
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
bool deps_entry_t::to_library_package_path(const pal::string_t& base, pal::string_t* str, uint32_t search_options) const
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
    return to_package_path(new_base, str, search_options);
}
