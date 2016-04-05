// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <set>
#include <functional>
#include <cassert>

#include "trace.h"
#include "deps_entry.h"
#include "deps_format.h"
#include "deps_resolver.h"
#include "utils.h"
#include "fx_ver.h"
#include "libhost.h"

namespace
{
// -----------------------------------------------------------------------------
// A uniqifying append helper that doesn't let two entries with the same
// "asset_name" be part of the "output" paths.
//
void add_tpa_asset(
    const pal::string_t& asset_name,
    const pal::string_t& asset_path,
    std::unordered_set<pal::string_t>* items,
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
// A uniqifying append helper that doesn't let two "paths" to be identical in
// the "output" string.
//
void add_unique_path(
    deps_entry_t::asset_types asset_type,
    const pal::string_t& path,
    std::unordered_set<pal::string_t>* existing,
    pal::string_t* output)
{
    // Resolve sym links.
    pal::string_t real = path;
    pal::realpath(&real);

    if (existing->count(real))
    {
        return;
    }

    trace::verbose(_X("Adding to %s path: %s"), deps_entry_t::s_known_asset_types[asset_type], real.c_str());

    output->append(real);

    output->push_back(PATH_SEPARATOR);
    existing->insert(real);
}

} // end of anonymous namespace

// -----------------------------------------------------------------------------
// Load local assemblies by priority order of their file extensions and
// unique-fied  by their simple name.
//
void deps_resolver_t::get_dir_assemblies(
    const pal::string_t& dir,
    const pal::string_t& dir_name,
    dir_assemblies_t* dir_assemblies)
{
    trace::verbose(_X("Adding files from %s dir %s"), dir_name.c_str(), dir.c_str());

    // Managed extensions in priority order, pick DLL over EXE and NI over IL.
    const pal::string_t managed_ext[] = { _X(".ni.dll"), _X(".dll"), _X(".ni.exe"), _X(".exe") };

    // List of files in the dir
    std::vector<pal::string_t> files;
    pal::readdir(dir, &files);

    for (const auto& ext : managed_ext)
    {
        for (const auto& file : files)
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

            // Already added entry for this asset, by priority order skip this ext
            if (dir_assemblies->count(file_name))
            {
                trace::verbose(_X("Skipping %s because the %s already exists in %s assemblies"), file.c_str(), dir_assemblies->find(file_name)->second.c_str(), dir_name.c_str());
                continue;
            }

            // Add entry for this asset
            pal::string_t file_path = dir + DIR_SEPARATOR + file;
            trace::verbose(_X("Adding %s to %s assembly set from %s"), file_name.c_str(), dir_name.c_str(), file_path.c_str());
            dir_assemblies->emplace(file_name, file_path);
        }
    }
}

bool deps_resolver_t::try_roll_forward(const deps_entry_t& entry,
    const pal::string_t& probe_dir,
    pal::string_t* candidate)
{
    trace::verbose(_X("Attempting a roll forward for [%s/%s/%s] in [%s]"), entry.library_name.c_str(), entry.library_version.c_str(), entry.relative_path.c_str(), probe_dir.c_str());

    const pal::string_t& lib_ver = entry.library_version;

    fx_ver_t cur_ver(-1, -1, -1);
    if (!fx_ver_t::parse(lib_ver, &cur_ver, false))
    {
        trace::verbose(_X("No roll forward as specified version [%s] could not be parsed"), lib_ver.c_str());
        return false;
    }

    // Extract glob string of the form: 1.0.* from the version 1.0.0-prerelease-00001.
    size_t pat_start = lib_ver.find(_X('.'), lib_ver.find(_X('.')) + 1);
    pal::string_t maj_min_star = lib_ver.substr(0, pat_start + 1) + _X('*');

    pal::string_t path = probe_dir;
    append_path(&path, entry.library_name.c_str());

    pal::string_t cache_key = path;
    append_path(&cache_key, maj_min_star.c_str());

    pal::string_t max_str;
    if (m_roll_forward_cache.count(cache_key))
    {
        max_str = m_roll_forward_cache[cache_key];
        trace::verbose(_X("Found cached roll forward version [%s] -> [%s]"), lib_ver.c_str(), max_str.c_str());
    }
    else
    {
        try_patch_roll_forward_in_dir(path, cur_ver, &max_str, true);
        m_roll_forward_cache[cache_key] = max_str;
    }

    append_path(&path, max_str.c_str());

    return entry.to_rel_path(path, candidate);
}

void deps_resolver_t::setup_probe_config(
    const corehost_init_t* init,
    const runtime_config_t& config,
    const arguments_t& args)
{
    if (pal::directory_exists(args.dotnet_extensions))
    {
        pal::string_t ext_ni = args.dotnet_extensions;
        append_path(&ext_ni, get_arch());
        if (pal::directory_exists(ext_ni))
        {
            // Servicing NI probe.
            m_probes.push_back(probe_config_t::svc_ni(ext_ni, config.get_fx_roll_fwd()));
        }

        // Servicing normal probe.
        m_probes.push_back(probe_config_t::svc(args.dotnet_extensions, config.get_fx_roll_fwd()));
    }

    if (pal::directory_exists(args.dotnet_packages_cache))
    {
        pal::string_t ni_packages_cache = args.dotnet_packages_cache;
        append_path(&ni_packages_cache, get_arch());
        if (pal::directory_exists(ni_packages_cache))
        {
            // Packages cache NI probe
            m_probes.push_back(probe_config_t::cache_ni(ni_packages_cache));
        }

        // Packages cache probe
        m_probes.push_back(probe_config_t::cache(args.dotnet_packages_cache));
    }

    if (pal::directory_exists(m_fx_dir))
    {
        // FX probe
        m_probes.push_back(probe_config_t::fx(m_fx_dir, m_fx_deps.get()));
    }

    for (const auto& probe : m_additional_probes)
    {
        // Additional paths
        bool roll_fwd = config.get_fx_roll_fwd();
        m_probes.push_back(probe_config_t::additional(probe, roll_fwd));
    }

    if (trace::is_enabled())
    {
        trace::verbose(_X("-- Listing probe configurations..."));
        for (const auto& pc : m_probes)
        {
            pc.print();
        }
    }
}

void deps_resolver_t::setup_additional_probes(const std::vector<pal::string_t>& probe_paths)
{
    m_additional_probes.assign(probe_paths.begin(), probe_paths.end());

    for (auto iter = m_additional_probes.begin(); iter != m_additional_probes.end(); )
    {
        if (pal::directory_exists(*iter))
        {
            ++iter;
        }
        else
        {
            iter = m_additional_probes.erase(iter);
        }
    }
}

bool deps_resolver_t::probe_entry_in_configs(const deps_entry_t& entry, pal::string_t* candidate)
{
    candidate->clear();
    for (const auto& config : m_probes)
    {
        trace::verbose(_X("  Considering entry [%s/%s/%s] and probe dir [%s]"), entry.library_name.c_str(), entry.library_version.c_str(), entry.relative_path.c_str(), config.probe_dir.c_str());

        if (config.only_serviceable_assets && !entry.is_serviceable)
        {
            trace::verbose(_X("    Skipping... not serviceable asset"));
            continue;
        }
        if (config.only_runtime_assets && entry.asset_type != deps_entry_t::asset_types::runtime)
        {
            trace::verbose(_X("    Skipping... not runtime asset"));
            continue;
        }
        pal::string_t probe_dir = config.probe_dir;
        if (config.match_hash)
        {
            if (entry.to_hash_matched_path(probe_dir, candidate))
            {
                assert(!config.roll_forward);
                trace::verbose(_X("    Matched hash for [%s]"), candidate->c_str());
                return true;
            }
            trace::verbose(_X("    Skipping... match hash failed"));
        }
        else if (config.probe_deps_json)
        {
            // If the deps json has it then someone has already done rid selection and put the right stuff in the dir.
            // So checking just package name and version would suffice. No need to check further for the exact asset relative path.
            if (config.probe_deps_json->has_package(entry.library_name, entry.library_version) && entry.to_dir_path(probe_dir, candidate))
            {
                trace::verbose(_X("    Probed deps json and matched [%s]"), candidate->c_str());
                return true;
            }
            trace::verbose(_X("    Skipping... probe in deps json failed"));
        }
        else if (!config.roll_forward)
        {
            if (entry.to_full_path(probe_dir, candidate))
            {
                trace::verbose(_X("    Specified no roll forward; matched [%s]"), candidate->c_str());
                return true;
            }
            trace::verbose(_X("    Skipping... not found in probe dir"));
        }
        else if (config.roll_forward)
        {
            if (try_roll_forward(entry, probe_dir, candidate))
            {
                trace::verbose(_X("    Specified roll forward; matched [%s]"), candidate->c_str());
                return true;
            }
            trace::verbose(_X("    Skipping... could not roll forward and match in probe dir"));
        }

        // continue to try next probe config
    }
    return false;
}

// -----------------------------------------------------------------------------
// Resolve coreclr directory from the deps file.
//
// Description:
//    Look for CoreCLR from the dependency list in the package cache and then
//    the packages directory.
//
pal::string_t deps_resolver_t::resolve_coreclr_dir()
{
    auto process_coreclr = [&]
        (bool is_portable, const pal::string_t& deps_dir, deps_json_t* deps) -> pal::string_t
    {
        pal::string_t candidate;

        if (deps->has_coreclr_entry())
        {
            const deps_entry_t& entry = deps->get_coreclr_entry();
            if (probe_entry_in_configs(entry, &candidate))
            {
                return get_directory(candidate);
            }
            else if (entry.is_rid_specific && entry.to_rel_path(deps_dir, &candidate))
            {
                return get_directory(candidate);
            }
        }
        else
        {
            trace::verbose(_X("Deps has no coreclr entry."));
        }

        // App/FX main dir or standalone app dir.
        trace::verbose(_X("Probing for CoreCLR in deps directory=[%s]"), deps_dir.c_str());
        if (coreclr_exists_in_dir(deps_dir))
        {
            return deps_dir;
        }

        return pal::string_t();
    };

    trace::info(_X("-- Starting CoreCLR Probe from app deps.json"));
    pal::string_t clr_dir = process_coreclr(m_portable, m_app_dir, m_deps.get());
    if (clr_dir.empty() && m_portable)
    {
        trace::info(_X("-- Starting CoreCLR Probe from FX deps.json"));
        clr_dir = process_coreclr(false, m_fx_dir, m_fx_deps.get());
    }
    if (!clr_dir.empty())
    {
        return clr_dir;
    }

    // Use platform-specific search algorithm
    pal::string_t install_dir;
    if (pal::find_coreclr(&install_dir))
    {
        return install_dir;
    }

    return pal::string_t();
}

void deps_resolver_t::resolve_tpa_list(
        const pal::string_t& clr_dir,
        pal::string_t* output)
{
    const std::vector<deps_entry_t> empty(0);

    // Obtain the local assemblies in the app dir.
    get_dir_assemblies(m_app_dir, _X("local"), &m_local_assemblies);
    if (m_portable)
    {
        // For portable also obtain FX dir assemblies.
        get_dir_assemblies(m_fx_dir, _X("fx"), &m_fx_assemblies);
    }

    std::unordered_set<pal::string_t> items;

    auto process_entry = [&](const pal::string_t& deps_dir, deps_json_t* deps, const dir_assemblies_t& dir_assemblies, const deps_entry_t& entry)
    {
        if (items.count(entry.asset_name))
        {
            return;
        }

        pal::string_t candidate;

        trace::info(_X("Processing TPA for deps entry [%s, %s, %s]"), entry.library_name.c_str(), entry.library_version.c_str(), entry.relative_path.c_str());

        // Try to probe from the shared locations.
        if (probe_entry_in_configs(entry, &candidate))
        {
            add_tpa_asset(entry.asset_name, candidate, &items, output);
        }
        // The rid asset should be picked up from app relative subpath.
        else if (entry.is_rid_specific && entry.to_rel_path(deps_dir, &candidate))
        {
            add_tpa_asset(entry.asset_name, candidate, &items, output);
        }
        // The rid-less asset should be picked up from the app base.
        else if (dir_assemblies.count(entry.asset_name))
        {
            add_tpa_asset(entry.asset_name, dir_assemblies.find(entry.asset_name)->second, &items, output);
        }
        else
        {
            // FIXME: Consider this error as a fail fast?
            trace::verbose(_X("Error: Could not resolve path to assembly: [%s, %s, %s]"), entry.library_name.c_str(), entry.library_version.c_str(), entry.relative_path.c_str());
        }
    };
    
    const auto& deps_entries = m_deps->get_entries(deps_entry_t::asset_types::runtime);
    std::for_each(deps_entries.begin(), deps_entries.end(), [&](const deps_entry_t& entry) {
        process_entry(m_app_dir, m_deps.get(), m_local_assemblies, entry);
    });

    // Finally, if the deps file wasn't present or has missing entries, then
    // add the app local assemblies to the TPA.
    for (const auto& kv : m_local_assemblies)
    {
        add_tpa_asset(kv.first, kv.second, &items, output);
    }

    const auto& fx_entries = m_portable ? m_fx_deps->get_entries(deps_entry_t::asset_types::runtime) : empty;
    std::for_each(fx_entries.begin(), fx_entries.end(), [&](const deps_entry_t& entry) {
        process_entry(m_fx_dir, m_fx_deps.get(), m_fx_assemblies, entry);
    });

    for (const auto& kv : m_fx_assemblies)
    {
        add_tpa_asset(kv.first, kv.second, &items, output);
    }
}

// -----------------------------------------------------------------------------
// Resolve the directories order for resources/native lookup
//
// Description:
//    This general purpose function specifies priority order of directory lookup
//    for both native images and resources specific resource images. Lookup for
//    resources assemblies is done by looking up two levels above from the file
//    path. Lookup for native images is done by looking up one level from the
//    file path.
//
//  Parameters:
//     asset_type        - The type of the asset that needs lookup, currently
//                         supports "resources" and "native"
//     app_dir           - The application local directory
//     package_dir       - The directory path to where packages are restored
//     package_cache_dir - The directory path to secondary cache for packages
//     clr_dir           - The directory where the host loads the CLR
//
//  Returns:
//     output - Pointer to a string that will hold the resolved lookup dirs
//
void deps_resolver_t::resolve_probe_dirs(
        deps_entry_t::asset_types asset_type,
        const pal::string_t& clr_dir,
        pal::string_t* output)
{
    bool is_resources = asset_type == deps_entry_t::asset_types::resources;
    assert(is_resources || asset_type == deps_entry_t::asset_types::native);

    // For resources assemblies, we need to provide the base directory of the resources path.
    // For example: .../Foo/en-US/Bar.dll, then, the resolved path is .../Foo
    std::function<pal::string_t(const pal::string_t&)> resources = [] (const pal::string_t& str) {
        return get_directory(get_directory(str));
    };
    // For native assemblies, obtain the directory path from the file path
    std::function<pal::string_t(const pal::string_t&)> native = [] (const pal::string_t& str) {
        return get_directory(str);
    };
    std::function<pal::string_t(const pal::string_t&)>& action = is_resources ? resources : native;
    std::unordered_set<pal::string_t> items;

    std::vector<deps_entry_t> empty(0);
    const auto& entries = m_deps->get_entries(asset_type);
    const auto& fx_entries = m_portable ? m_fx_deps->get_entries(asset_type) : empty;

    pal::string_t candidate;

    bool track_api_sets = true;
    auto add_package_cache_entry = [&](const deps_entry_t& entry)
    {
        if (probe_entry_in_configs(entry, &candidate))
        {
            // For standalone apps, on win7, coreclr needs ApiSets which has to be in the DLL search path.
            const pal::string_t result_dir = action(candidate);

            if (track_api_sets && pal::need_api_sets() &&
                ends_with(entry.library_name, _X("Microsoft.NETCore.Windows.ApiSets"), false))
            {
                // For standalone and portable apps, get the ApiSets DLL directory,
                // as they could come from servicing or other probe paths.
                // Note: in portable apps, the API set would come from FX deps
                // which is actually a standalone deps (rid specific API set).
                // If the portable app relied on its version of API sets, then
                // the rid selection fallback would have already been performed
                // by the host (deps_format.cpp)
                m_api_set_paths.insert(result_dir);
            }

            add_unique_path(asset_type, result_dir, &items, output);
        }
    };
    std::for_each(entries.begin(), entries.end(), add_package_cache_entry);
    track_api_sets = m_api_set_paths.empty();
    std::for_each(fx_entries.begin(), fx_entries.end(), add_package_cache_entry);
    track_api_sets = m_api_set_paths.empty();

    // For portable rid specific assets, the app relative directory must be used.
    if (m_portable)
    {
        std::for_each(entries.begin(), entries.end(), [&](const deps_entry_t& entry)
        {
            if (entry.is_rid_specific && entry.asset_type == asset_type && entry.to_rel_path(m_app_dir, &candidate))
            {
                add_unique_path(asset_type, action(candidate), &items, output);
            }

            // App called out an explicit API set dependency.
            if (track_api_sets && entry.is_rid_specific && pal::need_api_sets() &&
                ends_with(entry.library_name, _X("Microsoft.NETCore.Windows.ApiSets"), false))
            {
                m_api_set_paths.insert(action(candidate));
            }
        });
    }

    track_api_sets = m_api_set_paths.empty();

    // App local path
    add_unique_path(asset_type, m_app_dir, &items, output);

    // If API sets is not found (i.e., empty) in the probe paths above:
    // 1. For standalone app, do nothing as all are sxs.
    // 2. For portable app, add FX dir.

    // FX path if present
    if (!m_fx_dir.empty())
    {
        // For portable apps, if we didn't find api sets in probe paths
        // add the FX directory.
        if (track_api_sets && pal::need_api_sets())
        {
            m_api_set_paths.insert(m_fx_dir);
        }
        add_unique_path(asset_type, m_fx_dir, &items, output);
    }

    // CLR path
    add_unique_path(asset_type, clr_dir, &items, output);
}


// -----------------------------------------------------------------------------
// Entrypoint to resolve TPA, native and resources path ordering to pass to CoreCLR.
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
bool deps_resolver_t::resolve_probe_paths(const pal::string_t& clr_dir, probe_paths_t* probe_paths)
{
    resolve_tpa_list(clr_dir, &probe_paths->tpa);
    resolve_probe_dirs(deps_entry_t::asset_types::native, clr_dir, &probe_paths->native);
    resolve_probe_dirs(deps_entry_t::asset_types::resources, clr_dir, &probe_paths->resources);
    return true;
}
