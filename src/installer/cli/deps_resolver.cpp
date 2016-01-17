// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <set>
#include <functional>
#include <cassert>

#include "trace.h"
#include "deps_resolver.h"
#include "utils.h"

namespace
{
// -----------------------------------------------------------------------------
// Read a single field from the deps entry
//
// Parameters:
//    line  - A deps file entry line
//    buf   - The temporary buffer to use while parsing (with size to contain "line")
//    ofs   - The offset that this method will read from "line" on invocation and
//            the offset that has been consumed by this method upon successful exit
//    field - The current field read from the line
//
// Assumption:
//    The line should be in a CSV format, with commas separating the fields.
//    The fields themselves will be quoted. The escape character is '\\'
//
// Returns:
//    True if parsed successfully. Else, false
//
// Note:
//      Callers cannot call with the same "line" upon an unsuccessful exit.
bool read_field(const pal::string_t& line, pal::char_t* buf, unsigned* ofs, pal::string_t* field)
{
    unsigned& offset = *ofs;
    pal::string_t& value_recv = *field;

    // The first character should be a '"'
    if (line[offset] != '"')
    {
        trace::error(_X("Error reading TPA file"));
        return false;
    }
    offset++;

    auto buf_offset = 0;

    // Iterate through characters in the string
    for (; offset < line.length(); offset++)
    {
        // Is this a '\'?
        if (line[offset] == '\\')
        {
            // Skip this character and read the next character into the buffer
            offset++;
            buf[buf_offset] = line[offset];
        }
        // Is this a '"'?
        else if (line[offset] == '\"')
        {
            // Done! Advance to the pointer after the input
            offset++;
            break;
        }
        else
        {
            // Take the character
            buf[buf_offset] = line[offset];
        }
        buf_offset++;
    }
    buf[buf_offset] = '\0';
    value_recv.assign(buf);

    // Consume the ',' if we have one
    if (line[offset] == ',')
    {
        offset++;
    }
    return true;
}

// -----------------------------------------------------------------------------
// A uniqifying append helper that doesn't let two entries with the same
// "asset_name" be part of the "output" paths.
//
void add_tpa_asset(
    const pal::string_t& asset_name,
    const pal::string_t& asset_path,
    std::set<pal::string_t>* items,
    pal::string_t* output)
{
    if (items->count(asset_name))
    {
        return;
    }

    trace::verbose(_X("Adding tpa entry: %s"), asset_path.c_str());

    // Workaround for CoreFX not being able to resolve sym links.
    pal::string_t real_asset_path = asset_path;
    pal::realpath(&real_asset_path);
    output->append(real_asset_path);

    output->push_back(PATH_SEPARATOR);
    items->insert(asset_name);
}

// -----------------------------------------------------------------------------
// Add mscorlib from the CLR directory. Even if CLR is serviced, we should pick
// mscorlib from the CLR directory. If mscorlib could not be found in the CLR
// location, then leave it to the CLR to pick the right mscorlib.
//
void add_mscorlib_to_tpa(const pal::string_t& clr_dir, std::set<pal::string_t>* items, pal::string_t* output)
{
    pal::string_t mscorlib_ni_path = clr_dir + DIR_SEPARATOR + _X("mscorlib.ni.dll");
    if (pal::file_exists(mscorlib_ni_path))
    {
        add_tpa_asset(_X("mscorlib"), mscorlib_ni_path, items, output);
        return;
    }

    pal::string_t mscorlib_path = clr_dir + DIR_SEPARATOR + _X("mscorlib.dll");
    if (pal::file_exists(mscorlib_path))
    {
        add_tpa_asset(_X("mscorlib"), mscorlib_ni_path, items, output);
        return;
    }
}

// -----------------------------------------------------------------------------
// A uniqifying append helper that doesn't let two "paths" to be identical in
// the "output" string.
//
void add_unique_path(
    const pal::string_t& type,
    const pal::string_t& path,
    std::set<pal::string_t>* existing,
    pal::string_t* output)
{
    // Resolve sym links.
    pal::string_t real = path;
    pal::realpath(&real);

    if (existing->count(real))
    {
        return;
    }

    trace::verbose(_X("Adding to %s path: %s"), type.c_str(), real.c_str());

    output->append(real);

    output->push_back(PATH_SEPARATOR);
    existing->insert(real);
}

} // end of anonymous namespace

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


// -----------------------------------------------------------------------------
// Load the deps file and parse its "entry" lines which contain the "fields" of
// the entry. Populate an array of these entries.
//
bool deps_resolver_t::load()
{
    // If file doesn't exist, then assume parsed.
    if (!pal::file_exists(m_deps_path))
    {
        return true;
    }

    // Somehow the file stream could not be opened. This is an error.
    pal::ifstream_t file(m_deps_path);
    if (!file.good())
    {
        return false;
    }

    // Parse the "entry" lines of the deps file.
    std::string stdline;
    while (std::getline(file, stdline))
    {
        pal::string_t line;
        pal::to_palstring(stdline.c_str(), &line);

        deps_entry_t entry;
        pal::string_t is_serviceable;
        pal::string_t* fields[] = {
            &entry.library_type,
            &entry.library_name,
            &entry.library_version,
            &entry.library_hash,
            &entry.asset_type,
            &entry.asset_name,
            &entry.relative_path,
            // TODO: Add when the deps file support is enabled.
            // &is_serviceable
        };

        std::vector<pal::char_t> buf(line.length());

        for (unsigned i = 0, offset = 0; i < sizeof(fields) / sizeof(fields[0]); ++i)
        {
            if (!(read_field(line, buf.data(), &offset, fields[i])))
            {
                return false;
            }
        }

        // Serviceable, if not false, default is true.
        entry.is_serviceable = pal::strcasecmp(is_serviceable.c_str(), _X("false")) != 0;

        // TODO: Deps file does not follow spec. It uses '\\', should use '/'
        replace_char(&entry.relative_path, _X('\\'), _X('/'));

        m_deps_entries.push_back(entry);
    }
    return true;
}

// -----------------------------------------------------------------------------
// Parse the deps file.
//
// Returns:
//    True if the file parse is successful or if file doesn't exist. False,
//    when there is an error parsing the file.
//
bool deps_resolver_t::parse_deps_file(const arguments_t& args)
{
    m_deps_path = args.deps_path;

    return load();
}

// -----------------------------------------------------------------------------
// Load local assemblies by priority order of their file extensions and
// unique-fied  by their simple name.
//
void deps_resolver_t::get_local_assemblies(const pal::string_t& dir)
{
    trace::verbose(_X("Adding files from dir %s"), dir.c_str());

    // Managed extensions in priority order, pick DLL over EXE and NI over IL.
    const pal::string_t managed_ext[] = { _X(".ni.dll"), _X(".dll"), _X(".ni.exe"), _X(".exe") };

    // List of files in the dir
    std::vector<pal::string_t> files;
    pal::readdir(dir, &files);

    for (const auto& file : files)
    {
        for (const auto& ext : managed_ext)
        {
            // Nothing to do if file length is smaller than expected ext.
            if (file.length() <= ext.length())
            {
                continue;
            }

            auto file_name = file.substr(0, file.length() - ext.length());
            auto file_ext = file.substr(file_name.length());

            // Ext did not match expected ext, skip this file.
            if (pal::strcasecmp(file_ext.c_str(), ext.c_str()))
            {
                continue;
            }

            // TODO: Do a case insensitive lookup.
            // Already added entry for this asset, by priority order skip this ext
            if (m_local_assemblies.count(file_name))
            {
                trace::verbose(_X("Skipping %s because the %s already exists in local assemblies"), file.c_str(), m_local_assemblies.find(file_name)->second.c_str());
                continue;
            }

            // Add entry for this asset
            pal::string_t file_path = dir + DIR_SEPARATOR + file;
            trace::verbose(_X("Adding %s to local assembly set from %s"), file_name.c_str(), file_path.c_str());
            m_local_assemblies.emplace(file_name, file_path);
        }
    }
}

// -----------------------------------------------------------------------------
// Resolve the TPA list order.
//
// Description:
//    First, add mscorlib to the TPA. Then for each deps entry, check if they
//    are serviced. If they are not serviced, then look if they are present
//    app local. Worst case, default to the primary and seconday package
//    caches. Finally, for cases where deps file may not be present or if deps
//    did not have an entry for an app local assembly, just use them from the
//    app dir in the TPA path.
//
//  Parameters:
//     app_dir           - The application local directory
//     package_dir       - The directory path to where packages are restored
//     package_cache_dir - The directory path to secondary cache for packages
//     clr_dir           - The directory where the host loads the CLR
//
//  Returns:
//     output - Pointer to a string that will hold the resolved TPA paths
//
void deps_resolver_t::resolve_tpa_list(
        const pal::string_t& app_dir,
        const pal::string_t& package_dir,
        const pal::string_t& package_cache_dir,
        const pal::string_t& clr_dir,
        pal::string_t* output)
{
    // Obtain the local assemblies in the app dir.
    get_local_assemblies(app_dir);

    std::set<pal::string_t> items;

    add_mscorlib_to_tpa(clr_dir, &items, output);

    for (const deps_entry_t& entry : m_deps_entries)
    {
        // Is this asset a "runtime" type?
        if (entry.asset_type != _X("runtime") || items.count(entry.asset_name))
        {
            continue;
        }

        pal::string_t candidate;

        // Is this a serviceable entry and is there an entry in the servicing index?
        if (entry.is_serviceable && entry.library_type == _X("Package") &&
                m_svc.find_redirection(entry.library_name, entry.library_version, entry.relative_path, &candidate))
        {
            add_tpa_asset(entry.asset_name, candidate, &items, output);
        }
        // Is this entry present in the secondary package cache?
        else if (entry.to_hash_matched_path(package_cache_dir, &candidate))
        {
            add_tpa_asset(entry.asset_name, candidate, &items, output);
        }
        // Is this entry present locally?
        else if (m_local_assemblies.count(entry.asset_name))
        {
            // TODO: Case insensitive look up?
            add_tpa_asset(entry.asset_name, m_local_assemblies.find(entry.asset_name)->second, &items, output);
        }
        // Is this entry present in the package restore dir?
        else if (entry.to_full_path(package_dir, &candidate))
        {
            add_tpa_asset(entry.asset_name, candidate, &items, output);
        }
    }

    // Finally, if the deps file wasn't present or has missing entries, then
    // add the app local assemblies to the TPA.
    for (const auto& kv : m_local_assemblies)
    {
        add_tpa_asset(kv.first, kv.second, &items, output);
    }
}

// -----------------------------------------------------------------------------
// Resolve the directories order for culture/native lookup
//
// Description:
//    This general purpose function specifies priority order of directory lookup
//    for both native images and culture specific resource images. Lookup for
//    culture assemblies is done by looking up two levels above from the file
//    path. Lookup for native images is done by looking up one level from the
//    file path.
//
//  Parameters:
//     asset_type        - The type of the asset that needs lookup, currently
//                         supports "culture" and "native"
//     app_dir           - The application local directory
//     package_dir       - The directory path to where packages are restored
//     package_cache_dir - The directory path to secondary cache for packages
//     clr_dir           - The directory where the host loads the CLR
//
//  Returns:
//     output - Pointer to a string that will hold the resolved lookup dirs
//
void deps_resolver_t::resolve_probe_dirs(
        const pal::string_t& asset_type,
        const pal::string_t& app_dir,
        const pal::string_t& package_dir,
        const pal::string_t& package_cache_dir,
        const pal::string_t& clr_dir,
        pal::string_t* output)
{
    assert(asset_type == _X("culture") || asset_type == _X("native"));

    // For culture assemblies, we need to provide the base directory of the culture path.
    // For example: .../Foo/en-US/Bar.dll, then, the resolved path is .../Foo
    std::function<pal::string_t(const pal::string_t&)> culture = [] (const pal::string_t& str) {
        return get_directory(get_directory(str));
    };
    // For native assemblies, obtain the directory path from the file path
    std::function<pal::string_t(const pal::string_t&)> native = [] (const pal::string_t& str) {
        return get_directory(str);
    };
    std::function<pal::string_t(const pal::string_t&)>& action = (asset_type == _X("culture")) ? culture : native;

    std::set<pal::string_t> items;

    // Fill the "output" with serviced DLL directories if they are serviceable
    // and have an entry present.
    for (const deps_entry_t& entry : m_deps_entries)
    {
        pal::string_t redirection_path;
        if (entry.is_serviceable && entry.asset_type == asset_type && entry.library_type == _X("Package") &&
                m_svc.find_redirection(entry.library_name, entry.library_version, entry.relative_path, &redirection_path))
        {
            add_unique_path(asset_type, action(redirection_path), &items, output);
        }
    }

    pal::string_t candidate;

    // Take care of the secondary cache path
    for (const deps_entry_t& entry : m_deps_entries)
    {
        if (entry.asset_type == asset_type && entry.to_hash_matched_path(package_cache_dir, &candidate))
        {
            add_unique_path(asset_type, action(candidate), &items, output);
        }
    }

    // App local path
    add_unique_path(asset_type, app_dir, &items, output);

    // Take care of the package restore path
    for (const deps_entry_t& entry : m_deps_entries)
    {
        if (entry.asset_type == asset_type && entry.to_full_path(package_dir, &candidate))
        {
            add_unique_path(asset_type, action(candidate), &items, output);
        }
    }

    // CLR path
    add_unique_path(asset_type, clr_dir, &items, output);
}


// -----------------------------------------------------------------------------
// Entrypoint to resolve TPA, native and culture path ordering to pass to CoreCLR.
//
//  Parameters:
//     app_dir           - The application local directory
//     package_dir       - The directory path to where packages are restored
//     package_cache_dir - The directory path to secondary cache for packages
//     clr_dir           - The directory where the host loads the CLR
//     probe_paths       - Pointer to struct containing fields that will contain
//                         resolved path ordering.
//
//
bool deps_resolver_t::resolve_probe_paths(
    const pal::string_t& app_dir,
    const pal::string_t& package_dir,
    const pal::string_t& package_cache_dir,
    const pal::string_t& clr_dir,
    probe_paths_t* probe_paths)
{
    resolve_tpa_list(app_dir, package_dir, package_cache_dir, clr_dir, &probe_paths->tpa);
    resolve_probe_dirs(_X("native"), app_dir, package_dir, package_cache_dir, clr_dir, &probe_paths->native);
    resolve_probe_dirs(_X("culture"), app_dir, package_dir, package_cache_dir, clr_dir, &probe_paths->culture);
    return true;
}
