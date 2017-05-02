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

void deps_resolver_t::setup_shared_store_probes(
    const hostpolicy_init_t& init,
    const arguments_t& args)
{
    for (const auto& shared : args.env_shared_store)
    {
        if (pal::directory_exists(shared))
        {
            // Shared Store probe: DOTNET_SHARED_STORE
            m_probes.push_back(probe_config_t::lookup(shared));
        }
    }

    if (pal::directory_exists(args.local_shared_store))
    {
        // Shared Store probe: $HOME/.dotnet/store or %USERPROFILE%\.dotnet\store
        m_probes.push_back(probe_config_t::lookup(args.local_shared_store));
    }

    if (pal::directory_exists(args.dotnet_shared_store))
    {
        m_probes.push_back(probe_config_t::lookup(args.dotnet_shared_store));
    }

    if (args.global_shared_store != args.dotnet_shared_store && pal::directory_exists(args.global_shared_store))
    {
        // Shared Store probe: /usr/share/dotnet/store or C:\Program Files (x86)\dotnet\store
        m_probes.push_back(probe_config_t::lookup(args.global_shared_store));
    }
}

pal::string_t deps_resolver_t::get_probe_directories()
{
    pal::string_t directories;
    for (const auto& pc : m_probes)
    {
        directories.append(pc.probe_dir);
        directories.push_back(PATH_SEPARATOR);
    }

    return directories;
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
            m_probes.push_back(probe_config_t::svc_ni(ext_ni));
        }

        // Servicing normal probe.
        pal::string_t ext_pkgs = args.core_servicing;
        append_path(&ext_pkgs, _X("pkgs"));
        m_probes.push_back(probe_config_t::svc(ext_pkgs));
    }

    if (pal::directory_exists(m_fx_dir))
    {
        // FX probe
        m_probes.push_back(probe_config_t::fx(m_fx_dir, m_fx_deps.get()));
    }

    // The published deps directory to be probed: either app or FX directory.
    // The probe directory will be available at probe time.
    m_probes.push_back(probe_config_t::published_deps_dir());

    setup_shared_store_probes(init, args);

    for (const auto& probe : m_additional_probes)
    {
        // Additional paths
        m_probes.push_back(probe_config_t::lookup(probe));
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

/**
 * Given a deps entry, do a probe (lookup) for the file, based on the probe config.
 *   -- When crossgen-ed folders are looked up, look up only "runtime" (managed) assets.
 *   -- When servicing directories are looked up, look up only if the deps file marks the entry as serviceable.
 *   -- When a deps json based probe is performed, the deps entry's package name and version must match.
 *   -- When looking into a published dir, for rid specific assets lookup rid split folders; for non-rid assets lookup the layout dir.
 */
bool deps_resolver_t::probe_deps_entry(const deps_entry_t& entry, const pal::string_t& deps_dir, pal::string_t* candidate)
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
       
		if (config.probe_deps_json)
        {
            // If the deps json has the package name and version, then someone has already done rid selection and
            // put the right asset in the dir. So checking just package name and version would suffice.
            // No need to check further for the exact asset relative sub path.
            if (config.probe_deps_json->has_package(entry.library_name, entry.library_version) && entry.to_dir_path(probe_dir, candidate))
            {
                trace::verbose(_X("    Probed deps json and matched '%s'"), candidate->c_str());
                return true;
            }
            trace::verbose(_X("    Skipping... probe in deps json failed"));
        }
        else if (config.probe_publish_dir)
        {
            // This is a published dir probe, so look up rid specific assets in the rid folders.
            if (entry.is_rid_specific && entry.to_rel_path(deps_dir, candidate))
            {
                trace::verbose(_X("    Probed deps dir and matched '%s'"), candidate->c_str());
                return true;
            }
            // Non-rid assets, lookup in the published dir.
            if (!entry.is_rid_specific && entry.to_dir_path(deps_dir, candidate))
            {
                trace::verbose(_X("    Probed deps dir and matched '%s'"), candidate->c_str());
                return true;
            }
            trace::verbose(_X("    Skipping... probe in deps dir '%s' failed"), deps_dir.c_str());
        }
        else if (entry.to_full_path(probe_dir, candidate))
        {
            trace::verbose(_X("    Probed package dir and matched '%s'"), candidate->c_str());
            return true;
        }

        trace::verbose(_X("    Skipping... not found in probe dir '%s'"), probe_dir.c_str());
        // continue to try next probe config
    }
    return false;
}

bool report_missing_assembly_in_manifest(const deps_entry_t& entry)
{
    if (!entry.runtime_store_manifest_list.empty())
    {
        trace::error(_X("Error: assembly specified in the dependencies manifest was not found probably due to missing runtime store associated with %s -- package: '%s', version: '%s', path: '%s'"), 
                entry.runtime_store_manifest_list.c_str(), entry.library_name.c_str(), entry.library_version.c_str(), entry.relative_path.c_str());
    }
    else
    {
        trace::error(_X("Error: assembly specified in the dependencies manifest was not found -- package: '%s', version: '%s', path: '%s'"), 
                entry.library_name.c_str(), entry.library_version.c_str(), entry.relative_path.c_str());
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
            return report_missing_assembly_in_manifest(entry);
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

    // If the deps file wasn't present or has missing entries, then
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

    // If additional deps files were specified that need to be treated as part of the
    // application, then add them to the mix as well.
    for (const auto& additional_deps : m_additional_deps)
    {
        auto additional_deps_entries = additional_deps->get_entries(deps_entry_t::asset_types::runtime);
        for (auto entry : additional_deps_entries)
        {
            if (!process_entry(m_app_dir, additional_deps.get(), entry))
            {
                return false;
            }
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

void deps_resolver_t::resolve_additional_deps(const hostpolicy_init_t& init)
{
    if (!m_portable)
    {
        // Additional deps.json support is only available for portable apps due to the following constraints:
        //
        // 1) Unlike Portable Apps, Standalone apps do not have details of the SharedFX and Version they target.
        // 2) Unlike Portable Apps, Standalone apps do not have RID fallback graph that is required for looking up
        //    the correct native assets from nuget packages.

        return;
    }

    pal::string_t additional_deps_serialized = init.additional_deps_serialized;
    pal::string_t fx_name = init.fx_name;
    pal::string_t fx_ver = init.fx_ver;

    if (additional_deps_serialized.empty())
    {
        return;
    }

    pal::string_t additional_deps_path;
    pal::stringstream_t ss(additional_deps_serialized);

    // Process the delimiter separated custom deps files.
    while (std::getline(ss, additional_deps_path, PATH_SEPARATOR))
    {
        // If it's a single deps file, insert it in 'm_additional_deps_files'
        if (ends_with(additional_deps_path, _X(".deps.json"), false))
        {
            if (pal::file_exists(additional_deps_path))
            {
                trace::verbose(_X("Using specified additional deps.json: '%s'"), 
                    additional_deps_path.c_str());
                    
                m_additional_deps_files.push_back(additional_deps_path);
            }
            else
            {
                trace::warning(_X("Warning: Specified additional deps.json does not exist: '%s'"), 
                    additional_deps_path.c_str());
            }
        }
        else
        {
            // We'll search deps files in 'base_dir'/shared/fx_name/fx_ver
            append_path(&additional_deps_path, _X("shared"));
            append_path(&additional_deps_path, fx_name.c_str());
            append_path(&additional_deps_path, fx_ver.c_str());

            // The resulting list will be empty if 'additional_deps_path' is not a valid directory path
            std::vector<pal::string_t> list;
            pal::readdir(additional_deps_path, _X("*.deps.json"), &list);
            for (pal::string_t json_file : list)
            {
                pal::string_t json_full_path = additional_deps_path;
                append_path(&json_full_path, json_file.c_str());
                m_additional_deps_files.push_back(json_full_path);

                trace::verbose(_X("Using specified additional deps.json: '%s'"), 
                    json_full_path.c_str());
            }
        }
    }

    for (pal::string_t json_file : m_additional_deps_files)
    {
        m_additional_deps.push_back(std::unique_ptr<deps_json_t>(
            new deps_json_t(true, json_file, m_fx_deps->get_rid_fallback_graph())));
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
            if ((entry.asset_name == _X("apphost")) && ends_with(entry.library_name, _X(".Microsoft.NETCore.DotNetAppHost"), false))
            {
                trace::warning(_X("Warning: assembly specified in the dependencies manifest was not found -- package: '%s', version: '%s', path: '%s'"), 
                    entry.library_name.c_str(), entry.library_version.c_str(), entry.relative_path.c_str());
                return true;
            }

            return report_missing_assembly_in_manifest(entry);
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

    // Handle any additional deps.json that were specified.
    for (const auto& additional_deps : m_additional_deps)
    {
        const auto additional_deps_entries = additional_deps->get_entries(deps_entry_t::asset_types::runtime);
        for (const auto entry : additional_deps_entries)
        {
            if (!add_package_cache_entry(entry, m_app_dir))
            {
                return false;
            }
        }
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
