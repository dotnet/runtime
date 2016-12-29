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
    pal::string_t* serviced,
    pal::string_t* non_serviced,
    const pal::string_t& svc_dir)
{
    // Resolve sym links.
    pal::string_t real = path;
    pal::realpath(&real);

    if (existing->count(real))
    {
        return;
    }

    trace::verbose(_X("Adding to %s path: %s"), deps_entry_t::s_known_asset_types[asset_type], real.c_str());

    if (starts_with(real, svc_dir, false))
    {
        serviced->append(real);
        serviced->push_back(PATH_SEPARATOR);
    }
    else
    {
        non_serviced->append(real);
        non_serviced->push_back(PATH_SEPARATOR);
    }


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
    bool patch_roll_fwd,
    bool prerelease_roll_fwd,
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
    pal::string_t path = probe_dir;
    append_path(&path, entry.library_name.c_str());
    pal::string_t max_str = lib_ver;
    if (cur_ver.is_prerelease() && prerelease_roll_fwd)
    {
        pal::string_t maj_min_pat_star = cur_ver.prerelease_glob();

        pal::string_t cache_key = path;
        append_path(&cache_key, maj_min_pat_star.c_str());

        if (m_prerelease_roll_forward_cache.count(cache_key))
        {
            max_str = m_prerelease_roll_forward_cache[cache_key];
            trace::verbose(_X("Found cached roll forward version [%s] -> [%s]"), lib_ver.c_str(), max_str.c_str());
        }
        else
        {
            try_prerelease_roll_forward_in_dir(path, cur_ver, &max_str);
            m_prerelease_roll_forward_cache[cache_key] = max_str;
        }
    }
    if (!cur_ver.is_prerelease() && patch_roll_fwd)
    {
        // Extract glob string of the form: 1.0.* from the version 1.0.0-prerelease-00001.
        pal::string_t maj_min_star = cur_ver.patch_glob();

        pal::string_t cache_key = path;
        append_path(&cache_key, maj_min_star.c_str());

        if (m_patch_roll_forward_cache.count(cache_key))
        {
            max_str = m_patch_roll_forward_cache[cache_key];
            trace::verbose(_X("Found cached roll forward version [%s] -> [%s]"), lib_ver.c_str(), max_str.c_str());
        }
        else
        {
            try_patch_roll_forward_in_dir(path, cur_ver, &max_str);
            m_patch_roll_forward_cache[cache_key] = max_str;
        }
    }
    append_path(&path, max_str.c_str());

    return entry.to_rel_path(path, candidate);
}

void deps_resolver_t::setup_probe_config(
    const hostpolicy_init_t& init,
    const arguments_t& args)
{
    if (pal::directory_exists(args.core_servicing))
    {
        pal::string_t ext_ni = args.core_servicing;
        append_path(&ext_ni, get_arch());
        if (pal::directory_exists(ext_ni))
        {
            // Servicing NI probe.
            m_probes.push_back(probe_config_t::svc_ni(ext_ni, false, false));
        }

        // Servicing normal probe.
        pal::string_t ext_pkgs = args.core_servicing;
        append_path(&ext_pkgs, _X("pkgs"));
        m_probes.push_back(probe_config_t::svc(ext_pkgs, false, false));
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
        m_probes.push_back(probe_config_t::additional(probe));
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
                assert(!config.is_roll_fwd_set());
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
        else if (!config.is_roll_fwd_set())
        {
            if (entry.to_full_path(probe_dir, candidate))
            {
                trace::verbose(_X("    Specified no roll forward; matched [%s]"), candidate->c_str());
                return true;
            }
            trace::verbose(_X("    Skipping... not found in probe dir"));
        }
        else if (config.is_roll_fwd_set())
        {
            if (try_roll_forward(entry, probe_dir, config.patch_roll_fwd, config.prerelease_roll_fwd, candidate))
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

/**
 * Probe helper for a deps entry. Lookup all probe configurations and then
 * lookup in the directory where the deps file is present. For app dirs,
 *     1. RID specific entries are present in the package relative structure.
 *     2. Non-RID entries are present in the directory path.
 */
bool deps_resolver_t::probe_deps_entry(const deps_entry_t& entry, const pal::string_t& deps_dir, pal::string_t* candidate)
{
    if (probe_entry_in_configs(entry, candidate))
    {
        return true;
    }
    if (entry.is_rid_specific && entry.to_rel_path(deps_dir, candidate))
    {
        return true;
    }
    if (!entry.is_rid_specific && entry.to_dir_path(deps_dir, candidate))
    {
        return true;
    }
    return false;
}

/**
 *  Resovle the TPA assembly locations
 */
bool deps_resolver_t::resolve_tpa_list(
        pal::string_t* output,
        std::unordered_set<pal::string_t>* breadcrumb)
{
    const std::vector<deps_entry_t> empty(0);
    std::unordered_set<pal::string_t> items;

    auto process_entry = [&](const pal::string_t& deps_dir, deps_json_t* deps, const deps_entry_t& entry) -> bool
    {
        if (entry.is_serviceable)
        {
            breadcrumb->insert(entry.library_name + _X(",") + entry.library_version);
            breadcrumb->insert(entry.library_name);
        }
        if (items.count(entry.asset_name))
        {
            return true;
        }
        // Ignore placeholders
        if (ends_with(entry.relative_path, _X("/_._"), false))
        {
            return true;
        }

        pal::string_t candidate;

        trace::info(_X("Processing TPA for deps entry [%s, %s, %s]"), entry.library_name.c_str(), entry.library_version.c_str(), entry.relative_path.c_str());

        if (probe_deps_entry(entry, deps_dir, &candidate))
        {
            add_tpa_asset(entry.asset_name, candidate, &items, output);
            return true;
        }
        else
        {
            trace::error(_X("Error: assembly specified in the dependencies manifest was not found -- package: '%s', version: '%s', path: '%s'"), 
                entry.library_name.c_str(), entry.library_version.c_str(), entry.relative_path.c_str());
            return false;
        }
    };

    // First add managed assembly to the TPA.
    // TODO: Remove: the deps should contain the managed DLL.
    // Workaround for: csc.deps.json doesn't have the csc.dll
    pal::string_t managed_app_asset = get_filename_without_ext(m_managed_app);
    add_tpa_asset(managed_app_asset, m_managed_app, &items, output);

    const auto& deps_entries = m_deps->get_entries(deps_entry_t::asset_types::runtime);
    for (const auto& entry : deps_entries)
    {
        if (!process_entry(m_app_dir, m_deps.get(), entry))
        {
            return false;
        }
    }

    // Finally, if the deps file wasn't present or has missing entries, then
    // add the app local assemblies to the TPA.
    if (!m_deps->exists())
    {
        dir_assemblies_t local_assemblies;

        // Obtain the local assemblies in the app dir.
        get_dir_assemblies(m_app_dir, _X("local"), &local_assemblies);
        for (const auto& kv : local_assemblies)
        {
            add_tpa_asset(kv.first, kv.second, &items, output);
        }
    }

    // Probe FX deps entries after app assemblies are added.
    const auto& fx_entries = m_portable ? m_fx_deps->get_entries(deps_entry_t::asset_types::runtime) : empty;
    for (const auto& entry : fx_entries)
    {
        if (!process_entry(m_fx_dir, m_fx_deps.get(), entry))
        {
            return false;
        }
    }

    return true;
}

/**
 * Initialize resolved paths to known entries like coreclr, jit.
 */
void deps_resolver_t::init_known_entry_path(const deps_entry_t& entry, const pal::string_t& path)
{
    if (entry.asset_type != deps_entry_t::asset_types::native)
    {
        return;
    }
    if (m_coreclr_path.empty() && ends_with(entry.relative_path, _X("/") + pal::string_t(LIBCORECLR_NAME), false))
    {
        m_coreclr_path = path;
        return;
    }
    if (m_clrjit_path.empty() && ends_with(entry.relative_path, _X("/") + pal::string_t(LIBCLRJIT_NAME), false))
    {
        m_clrjit_path = path;
        return;
    }
}

/**
 *  Resolve native and culture assembly directories based on "asset_type" parameter.
 */
bool deps_resolver_t::resolve_probe_dirs(
        deps_entry_t::asset_types asset_type,
        pal::string_t* output,
        std::unordered_set<pal::string_t>* breadcrumb)
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
    // Action for post processing the resolved path
    std::function<pal::string_t(const pal::string_t&)>& action = is_resources ? resources : native;

    // Set for de-duplication
    std::unordered_set<pal::string_t> items;

    pal::string_t core_servicing = m_core_servicing;
    pal::realpath(&core_servicing);

    // Filter out non-serviced assets so the paths can be added after servicing paths.
    pal::string_t non_serviced;

    std::vector<deps_entry_t> empty(0);
    const auto& entries = m_deps->get_entries(asset_type);
    const auto& fx_entries = m_portable ? m_fx_deps->get_entries(asset_type) : empty;

    pal::string_t candidate;

    auto add_package_cache_entry = [&](const deps_entry_t& entry, const pal::string_t& deps_dir) -> bool
    {
        if (entry.is_serviceable)
        {
            breadcrumb->insert(entry.library_name + _X(",") + entry.library_version);
            breadcrumb->insert(entry.library_name);
        }
        if (items.count(entry.asset_name))
        {
            return true;
        }
        // Ignore placeholders
        if (ends_with(entry.relative_path, _X("/_._"), false))
        {
            return true;
        }

        trace::verbose(_X("Processing native/culture for deps entry [%s, %s, %s]"), 
            entry.library_name.c_str(), entry.library_version.c_str(), entry.relative_path.c_str());

        if (probe_deps_entry(entry, deps_dir, &candidate))
        {
            init_known_entry_path(entry, candidate);
            add_unique_path(asset_type, action(candidate), &items, output, &non_serviced, core_servicing);
        }
        else
        {
            // For standalone apps, apphost.exe will be renamed. Do not use the full package name
            // because of rid-fallback could happen (ex: CentOS falling back to RHEL)
            if ((ends_with(entry.library_name, _X(".Microsoft.NETCore.DotNetHost"), false) && entry.asset_name == _X("dotnet")) ||
                (ends_with(entry.library_name, _X(".Microsoft.NETCore.DotNetAppHost"), false) && entry.asset_name == _X("apphost")))
            {
                trace::warning(_X("Warning: assembly specified in the dependencies manifest was not found -- package: '%s', version: '%s', path: '%s'"), 
                    entry.library_name.c_str(), entry.library_version.c_str(), entry.relative_path.c_str());
                return true;
            }
            trace::error(_X("Error: assembly specified in the dependencies manifest was not found -- package: '%s', version: '%s', path: '%s'"), 
                entry.library_name.c_str(), entry.library_version.c_str(), entry.relative_path.c_str());
            return false;
        }

        if (m_api_set_paths.empty() && pal::need_api_sets() &&
                ends_with(entry.library_name, _X("Microsoft.NETCore.Windows.ApiSets"), false))
        {
            m_api_set_paths.insert(action(candidate));
        }

        return true;
    };

    for (const auto& entry : entries)
    {
        if (!add_package_cache_entry(entry, m_app_dir))
        {
            return false;
        }
    }

    // If the deps file is missing add known locations.
    if (!m_deps->exists())
    {
        // App local path
        add_unique_path(asset_type, m_app_dir, &items, output, &non_serviced, core_servicing);

        (void) library_exists_in_dir(m_app_dir, LIBCORECLR_NAME, &m_coreclr_path);

        (void) library_exists_in_dir(m_app_dir, LIBCLRJIT_NAME, &m_clrjit_path);
    }
    
    for (const auto& entry : fx_entries)
    {
        if (!add_package_cache_entry(entry, m_fx_dir))
        {
            return false;
        }
    }

    output->append(non_serviced);

    return true;
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
bool deps_resolver_t::resolve_probe_paths(probe_paths_t* probe_paths, std::unordered_set<pal::string_t>* breadcrumb)
{
    if (!resolve_tpa_list(&probe_paths->tpa, breadcrumb))
    {
        return false;
    }

    if (!resolve_probe_dirs(deps_entry_t::asset_types::native, &probe_paths->native, breadcrumb))
    {
        return false;
    }

    if (!resolve_probe_dirs(deps_entry_t::asset_types::resources, &probe_paths->resources, breadcrumb))
    {
        return false;
    }

    // If we found coreclr and the jit during native path probe, set the paths now.
    probe_paths->coreclr = m_coreclr_path;
    probe_paths->clrjit = m_clrjit_path;

    return true;
}
