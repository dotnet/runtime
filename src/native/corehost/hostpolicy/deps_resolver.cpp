// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <set>
#include <functional>
#include <cassert>

#include <trace.h>
#include "deps_entry.h"
#include "deps_format.h"
#include "deps_resolver.h"
#include "shared_store.h"
#include <utils.h>
#include <fx_ver.h>

namespace
{
    const pal::char_t* MissingAssemblyMessage = _X(
        "%s:\n"
        "  An assembly specified in the application dependencies manifest (%s) was not found:\n"
        "    package: '%s', version: '%s'\n"
        "    path: '%s'");

    const pal::char_t* ManifestListMessage = _X(
        "  This assembly was expected to be in the local runtime store as the application was published using the following target manifest files:\n"
        "    %s");

    const pal::char_t* DuplicateAssemblyWithDifferentExtensionMessage = _X(
        "Error:\n"
        "  An assembly specified in the application dependencies manifest (%s) has already been found but with a different file extension:\n"
        "    package: '%s', version: '%s'\n"
        "    path: '%s'\n"
        "    previously found assembly: '%s'");

    const int AppFxLevel = 0;

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
        // To optimize startup time, we avoid calling realpath here.
        // Because of this, there might be duplicates in the output
        // whenever path is either non-normalized or a symbolic link.
        if (existing->count(path))
        {
            return;
        }

        trace::verbose(_X("Adding to %s path: %s"), deps_entry_t::s_known_asset_types[asset_type], path.c_str());

        if (starts_with(path, svc_dir, false))
        {
            serviced->append(path);
            serviced->push_back(PATH_SEPARATOR);
        }
        else
        {
            non_serviced->append(path);
            non_serviced->push_back(PATH_SEPARATOR);
        }

        existing->insert(path);
    }

    // Return the filename from deps path; a deps path always uses a '/' for the separator.
    pal::string_t get_deps_filename(const pal::string_t& path)
    {
        if (path.empty())
        {
            return path;
        }

        auto name_pos = path.find_last_of('/');
        if (name_pos == pal::string_t::npos)
        {
            return path;
        }

        return path.substr(name_pos + 1);
    }

    // A uniqifying append helper that doesn't let two entries with the same
    // "asset_name" be part of the "items" paths.
    void add_tpa_asset(
        const deps_asset_t& asset,
        const pal::string_t& resolved_path,
        name_to_resolved_asset_map_t* items)
    {
        name_to_resolved_asset_map_t::iterator existing = items->find(asset.name);
        if (existing == items->end())
        {
            if (trace::is_enabled())
            {
                trace::verbose(_X("Adding tpa entry: %s, AssemblyVersion: %s, FileVersion: %s"),
                    resolved_path.c_str(),
                    asset.assembly_version.as_str().c_str(),
                    asset.file_version.as_str().c_str());
            }

            items->emplace(asset.name, deps_resolved_asset_t(asset, resolved_path));
        }
    }

    // -----------------------------------------------------------------------------
    // Load local assemblies by priority order of their file extensions and
    // uniquified by their simple name.
    //
    void get_dir_assemblies(
        const pal::string_t& dir,
        const pal::string_t& dir_name,
        name_to_resolved_asset_map_t* items)
    {
        version_t empty;
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
                if (items->count(file_name))
                {
                    trace::verbose(_X("Skipping %s because the %s already exists in %s assemblies"),
                        file.c_str(),
                        items->find(file_name)->second.asset.relative_path.c_str(),
                        dir_name.c_str());

                    continue;
                }

                // Add entry for this asset
                pal::string_t file_path = dir;
                if (!file_path.empty() && file_path.back() != DIR_SEPARATOR)
                {
                    file_path.push_back(DIR_SEPARATOR);
                }
                file_path.append(file);

                trace::verbose(_X("Adding %s to %s assembly set from %s"),
                    file_name.c_str(),
                    dir_name.c_str(),
                    file_path.c_str());

                deps_asset_t asset(file_name, file, empty, empty);
                add_tpa_asset(asset, file_path, items);
            }
        }
    }
} // end of anonymous namespace

void deps_resolver_t::setup_shared_store_probes(
    const std::vector<pal::string_t>& shared_stores)
{
    for (const pal::string_t& shared : shared_stores)
    {
        if (pal::directory_exists(shared))
        {
            m_probes.push_back(probe_config_t::lookup(shared));
            m_needs_file_existence_checks = true;
        }
    }
}

pal::string_t deps_resolver_t::get_lookup_probe_directories()
{
    pal::string_t directories;
    for (const auto& pc : m_probes)
    {
        if (pc.is_lookup())
        {
            directories.append(pc.probe_dir);
            directories.push_back(PATH_SEPARATOR);
        }
    }

    return directories;
}

void deps_resolver_t::setup_probe_config(
    const std::vector<pal::string_t>& shared_stores,
    const std::vector<pal::string_t>& additional_probe_paths)
{
    if (pal::directory_exists(m_core_servicing))
    {
        pal::string_t ext_ni = m_core_servicing;
        append_path(&ext_ni, get_current_arch_name());
        if (pal::directory_exists(ext_ni))
        {
            // Servicing NI probe.
            m_probes.push_back(probe_config_t::svc_ni(ext_ni));
        }

        // Servicing normal probe.
        pal::string_t ext_pkgs = m_core_servicing;
        append_path(&ext_pkgs, _X("pkgs"));
        m_probes.push_back(probe_config_t::svc(ext_pkgs));

        m_needs_file_existence_checks = true;
    }

    // The published deps directory to be probed: either app or FX directory.
    // The probe directory will be available at probe time.
    m_probes.push_back(probe_config_t::published_deps_dir());

    // The framework locations, starting with highest level framework.
    for (int32_t i = 1; i < static_cast<int32_t>(m_fx_definitions.size()); ++i)
    {
        if (pal::directory_exists(m_fx_definitions[i]->get_dir()))
        {
            m_probes.push_back(probe_config_t::fx(m_fx_definitions[i]->get_dir(), m_fx_deps[i].get(), i));
        }
    }

    setup_shared_store_probes(shared_stores);

    if (!additional_probe_paths.empty())
    {
        for (const auto& probe : additional_probe_paths)
        {
            // Additional paths
            m_probes.push_back(probe_config_t::lookup(probe));
        }

        m_needs_file_existence_checks = true;
    }

    if (trace::is_enabled())
    {
        trace::verbose(_X("-- Probe configurations:"));
        for (const auto& pc : m_probes)
        {
            trace::verbose(_X("  probe %s"), pc.as_str().c_str());
        }
    }
}

/**
 * Given a deps entry, do a probe (lookup) for the file, based on the probe config.
 *   -- When crossgen-ed folders are looked up, look up only "runtime" (managed) assets.
 *   -- When servicing directories are looked up, look up only if the deps file marks the entry as serviceable.
 *   -- When a deps json based probe is performed, the deps entry's package name and version must match.
 *   -- When looking into a published dir, for rid specific assets lookup rid split folders; for non-rid assets lookup the layout dir.
 * The path to the resolved file is returned in candidate out parameter
 * If the candidate is embedded within the single-file bundle (rather than an actual file on disk), found_in_bundle will be set to true.
 */
bool deps_resolver_t::probe_deps_entry(const deps_entry_t& entry, const pal::string_t& deps_dir, int fx_level, pal::string_t* candidate, bool & found_in_bundle)
{
    candidate->clear();
    found_in_bundle = false;

    for (const auto& config : m_probes)
    {
        trace::verbose(_X("  Using probe config: %s"), config.as_str().c_str());

        if (config.is_servicing() && !entry.is_serviceable)
        {
            trace::verbose(_X("    Skipping... not serviceable asset"));
            continue;
        }
        if (config.only_runtime_assets && entry.asset_type != deps_entry_t::asset_types::runtime)
        {
            trace::verbose(_X("    Skipping... not runtime asset"));
            continue;
        }
        if (config.is_app() && fx_level != AppFxLevel)
        {
            trace::verbose(_X("    Skipping... not app asset"));
            continue;
        }
        if (config.is_fx() && fx_level > config.fx_level)
        {
            // Only probe frameworks that are the same level or lower than the current entry because
            // a lower-level fx should not have a dependency on a higher-level fx and because starting
            // with fx_level allows it to override a higher-level fx location if the entry is newer.
            // Note that fx_level 0 is the highest level (the app)
            trace::verbose(_X("    Skipping... framework is a higher level than entry"));
            continue;
        }

        uint32_t search_options = m_needs_file_existence_checks ? deps_entry_t::search_options::file_existence : deps_entry_t::search_options::none;

        if (config.is_fx())
        {
            assert(fx_level <= config.fx_level);

            // If the deps json has the package name and version, then someone has already done rid selection and
            // put the right asset in the dir. So checking just package name and version would suffice.
            // No need to check further for the exact asset relative sub path.
            if (config.probe_deps_json->has_package(entry.library_name, entry.library_version) && entry.to_dir_path(config.probe_dir, candidate, search_options, found_in_bundle))
            {
                assert(!found_in_bundle);
                trace::verbose(_X("    Probed deps json and matched '%s'"), candidate->c_str());
                return true;
            }

            trace::verbose(_X("    Skipping... not found in deps json."));
        }
        else if (config.is_app())
        {
            assert(fx_level == AppFxLevel);
            if (entry.is_rid_specific)
            {
                // Look up rid specific assets in the rid folders.
                if (entry.to_rel_path(deps_dir, candidate, search_options | deps_entry_t::search_options::look_in_bundle))
                {
                    trace::verbose(_X("    Probed deps dir and matched '%s'"), candidate->c_str());
                    return true;
                }
            }
            else
            {
                // Non-rid assets, lookup in the published dir.
                if (entry.to_dir_path(deps_dir, candidate, search_options | deps_entry_t::search_options::look_in_bundle, found_in_bundle))
                {
                    trace::verbose(_X("    Probed deps dir and matched '%s'"), candidate->c_str());
                    return true;
                }
            }

            trace::verbose(_X("    Skipping... not found in deps dir '%s'"), deps_dir.c_str());
        }
        else
        {
            if (entry.to_full_path(config.probe_dir, candidate, search_options | (config.is_servicing() ? deps_entry_t::search_options::is_servicing : 0)))
            {
                trace::verbose(_X("    Probed package dir and matched '%s'"), candidate->c_str());
                return true;
            }

            trace::verbose(_X("    Skipping... not found in probe dir '%s'"), config.probe_dir.c_str());
        }

        // continue to try next probe config
    }
    return false;
}

bool report_missing_assembly_in_manifest(const deps_entry_t& entry, bool continueResolving = false)
{
    bool showManifestListMessage = !entry.runtime_store_manifest_list.empty();

    if (entry.asset_type == deps_entry_t::asset_types::resources)
    {
        // Treat missing resource assemblies as informational.
        continueResolving = true;

        trace::info(MissingAssemblyMessage, _X("Info"),
            entry.deps_file.c_str(), entry.library_name.c_str(), entry.library_version.c_str(), entry.asset.relative_path.c_str());

        if (showManifestListMessage)
        {
            trace::info(ManifestListMessage, entry.runtime_store_manifest_list.c_str());
        }
    }
    else if (continueResolving)
    {
        trace::warning(MissingAssemblyMessage, _X("Warning"),
            entry.deps_file.c_str(), entry.library_name.c_str(), entry.library_version.c_str(), entry.asset.relative_path.c_str());

        if (showManifestListMessage)
        {
            trace::warning(ManifestListMessage, entry.runtime_store_manifest_list.c_str());
        }
    }
    else
    {
        trace::error(MissingAssemblyMessage, _X("Error"),
            entry.deps_file.c_str(), entry.library_name.c_str(), entry.library_version.c_str(), entry.asset.relative_path.c_str());

        if (showManifestListMessage)
        {
            trace::error(ManifestListMessage, entry.runtime_store_manifest_list.c_str());
        }
    }

    return continueResolving;
}

/**
 *  Resolve the TPA assembly locations
 */
bool deps_resolver_t::resolve_tpa_list(
        pal::string_t* output,
        std::unordered_set<pal::string_t>* breadcrumb,
        bool ignore_missing_assemblies)
{
    name_to_resolved_asset_map_t items;

    auto process_entry = [&](const pal::string_t& deps_dir, const deps_entry_t& entry, int fx_level) -> bool
    {
        if (breadcrumb != nullptr && entry.is_serviceable)
        {
            breadcrumb->insert(entry.library_name + _X(",") + entry.library_version);
            breadcrumb->insert(entry.library_name);
        }

        // Ignore placeholders
        if (ends_with(entry.asset.relative_path, _X("/_._"), false))
        {
            return true;
        }

        trace::info(_X("Processing TPA for deps entry [%s, %s, %s] with fx level: %d"),
            entry.library_name.c_str(), entry.library_version.c_str(), entry.asset.relative_path.c_str(), fx_level);

        pal::string_t resolved_path;

        name_to_resolved_asset_map_t::iterator existing = items.find(entry.asset.name);
        if (existing == items.end())
        {
            bool found_in_bundle = false;
            if (probe_deps_entry(entry, deps_dir, fx_level, &resolved_path, found_in_bundle))
            {
                // Assemblies loaded directly from the bundle are not added to the TPA list.
                // The runtime directly probes the bundle-manifest using a host-callback.
                if (!found_in_bundle)
                {
                    add_tpa_asset(entry.asset, resolved_path, &items);
                }

                return true;
            }

            return report_missing_assembly_in_manifest(entry, ignore_missing_assemblies);
        }
        else
        {
            // Verify the extension is the same as the previous verified entry
            if (get_deps_filename(entry.asset.relative_path) != get_filename(existing->second.resolved_path))
            {
                trace::error(
                    DuplicateAssemblyWithDifferentExtensionMessage,
                    entry.deps_file.c_str(),
                    entry.library_name.c_str(),
                    entry.library_version.c_str(),
                    entry.asset.relative_path.c_str(),
                    existing->second.resolved_path.c_str());

                return false;
            }

            deps_resolved_asset_t* existing_entry = &existing->second;

            // If deps entry is same or newer than existing, then see if it should be replaced
            if (entry.asset.assembly_version > existing_entry->asset.assembly_version ||
                (entry.asset.assembly_version == existing_entry->asset.assembly_version && entry.asset.file_version >= existing_entry->asset.file_version))
            {
                bool found_in_bundle = false;
                if (probe_deps_entry(entry, deps_dir, fx_level, &resolved_path, found_in_bundle))
                {
                    // If the path is the same, then no need to replace
                    if (resolved_path != existing_entry->resolved_path)
                    {
                        trace::verbose(_X("Replacing deps entry [%s, AssemblyVersion:%s, FileVersion:%s] with [%s, AssemblyVersion:%s, FileVersion:%s]"),
                            existing_entry->resolved_path.c_str(), existing_entry->asset.assembly_version.as_str().c_str(), existing_entry->asset.file_version.as_str().c_str(),
                            resolved_path.c_str(), entry.asset.assembly_version.as_str().c_str(), entry.asset.file_version.as_str().c_str());

                        existing_entry = nullptr;
                        items.erase(existing);

                        if (!found_in_bundle)
                        {
                            deps_asset_t asset(entry.asset.name, entry.asset.relative_path, entry.asset.assembly_version, entry.asset.file_version);
                            add_tpa_asset(asset, resolved_path, &items);
                        }
                    }
                }
                else if (fx_level != AppFxLevel)
                {
                    // The framework is missing a newer package, so this is an error.
                    // For compat, it is not an error for the app; this can occur for the main application assembly when using --depsfile
                    // and the app assembly does not exist with the deps file.
                    return report_missing_assembly_in_manifest(entry);
                }
            }

            return true;
        }
    };

    // We do not support self-contained in a libhost scenario since in the self-contained scenario,
    // we cannot determine what assemblies are framework assemblies, and what assemblies are app-local assemblies.
    if (m_host_mode != host_mode_t::libhost)
    {
        // First add managed assembly to the TPA.
        // TODO: Remove: the deps should contain the managed DLL.
        // Workaround for: csc.deps.json doesn't have the csc.dll

        // If this is a single-file bundle, app.dll is expected to be within the bundle, unless it is explicitly excluded from the bundle.
        // In all other cases, add its path to the TPA list.
        pal::string_t managed_app_name = get_filename(m_managed_app);
        if (!bundle::info_t::is_single_file_bundle() ||
            bundle::runner_t::app()->probe(managed_app_name) == nullptr)
        {
            deps_asset_t asset(get_filename_without_ext(m_managed_app), managed_app_name, version_t(), version_t());
            add_tpa_asset(asset, m_managed_app, &items);
        }

        // Add the app's entries
        const auto& deps_entries = get_app_deps().get_entries(deps_entry_t::asset_types::runtime);
        for (const auto& entry : deps_entries)
        {
            if (!process_entry(m_app_dir, entry, 0))
            {
                return false;
            }
        }

        // If the deps file wasn't present or has missing entries, then
        // add the app local assemblies to the TPA. This is only valid
        // in non-libhost scenarios (e.g. comhost).
        if (!get_app_deps().exists())
        {
            // Obtain the local assemblies in the app dir.
            get_dir_assemblies(m_app_dir, _X("local"), &items);
        }
    }

    // There should be no additional deps files in a libhost scenario.
    // See comments during additional deps.json resolution.
    assert(m_additional_deps.empty() || m_host_mode != host_mode_t::libhost);

    // If additional deps files were specified that need to be treated as part of the
    // application, then add them to the mix as well.
    for (const auto& additional_deps : m_additional_deps)
    {
        auto additional_deps_entries = additional_deps->get_entries(deps_entry_t::asset_types::runtime);
        for (auto entry : additional_deps_entries)
        {
            if (!process_entry(m_app_dir, entry, 0))
            {
                return false;
            }
        }
    }

    // Probe FX deps entries after app assemblies are added.
    if (m_is_framework_dependent)
    {
        for (int32_t i = 1; i < static_cast<int32_t>(m_fx_definitions.size()); ++i)
        {
            const auto& deps_entries = m_fx_deps[i]->get_entries(deps_entry_t::asset_types::runtime);
            for (const auto& entry : deps_entries)
            {
                if (!process_entry(m_fx_definitions[i]->get_dir(), entry, i))
                {
                    return false;
                }
            }
        }
    }

    // Convert the paths into a string and return it
    for (const auto& item : items)
    {
        output->append(item.second.resolved_path);
        output->push_back(PATH_SEPARATOR);
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

    assert(pal::is_path_rooted(path));
    if (m_coreclr_path.empty() && ends_with(path, DIR_SEPARATOR + pal::string_t(LIBCORECLR_NAME), false))
    {
        m_coreclr_path = path;
        return;
    }
}

void deps_resolver_t::resolve_additional_deps(const pal::char_t* additional_deps_serialized, const deps_json_t::rid_resolution_options_t& rid_resolution_options)
{
    if (!m_is_framework_dependent
        || m_host_mode == host_mode_t::libhost)
    {
        // Additional deps.json support is only available for framework-dependent apps due to the following constraints:
        //
        // 1) Unlike framework-dependent Apps, self-contained apps do not have details of the SharedFX and Version they target.
        // 2) Unlike framework-dependent Apps, self-contained apps do not have RID fallback graph that is required for looking up
        //    the correct native assets from nuget packages.
        //
        // Additional deps.json support is not available for libhost scenarios. For example, if CoreCLR is instantiated from a
        // library context (i.e. comhost) the activation of classes are assumed to be performed in an AssemblyLoadContext. This
        // assumption is made because it is possible an existing CoreCLR was already activated and it may not satisfy the current
        // needs of the new class.

        return;
    }

    if (additional_deps_serialized == nullptr || pal::strlen(additional_deps_serialized) == 0)
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

                m_additional_deps.push_back(deps_json_t::create_for_framework_dependent(additional_deps_path, rid_resolution_options));
            }
            else
            {
                trace::warning(_X("Warning: Specified additional deps.json does not exist: '%s'"),
                    additional_deps_path.c_str());
            }
        }
        else
        {
            for (size_t i = 1; i < m_fx_definitions.size(); ++i)
            {
                fx_ver_t most_compatible_deps_folder_version;
                fx_ver_t framework_found_version;
                fx_ver_t::parse(m_fx_definitions[i]->get_found_version(), &framework_found_version);

                // We'll search deps directories in 'base_dir'/shared/fx_name/ for closest compatible patch version
                pal::string_t additional_deps_path_fx = additional_deps_path;
                append_path(&additional_deps_path_fx, _X("shared"));
                append_path(&additional_deps_path_fx, m_fx_definitions[i]->get_name().c_str());
                trace::verbose(_X("Searching for most compatible deps directory in [%s]"), additional_deps_path_fx.c_str());
                std::vector<pal::string_t> deps_dirs;
                pal::readdir_onlydirectories(additional_deps_path_fx, &deps_dirs);

                for (pal::string_t dir : deps_dirs)
                {
                    fx_ver_t ver;
                    if (fx_ver_t::parse(dir, &ver))
                    {
                        if (ver > most_compatible_deps_folder_version &&
                            ver <= framework_found_version &&
                            ver.get_major() == framework_found_version.get_major() &&
                            ver.get_minor() == framework_found_version.get_minor())
                        {
                            most_compatible_deps_folder_version = ver;
                        }
                    }
                }

                if (most_compatible_deps_folder_version == fx_ver_t())
                {
                    trace::verbose(_X("No additional deps directory less than or equal to [%s] found with same major and minor version."), framework_found_version.as_str().c_str());
                }
                else
                {
                    trace::verbose(_X("Found additional deps directory [%s]"), most_compatible_deps_folder_version.as_str().c_str());

                    append_path(&additional_deps_path_fx, most_compatible_deps_folder_version.as_str().c_str());

                    // The resulting list will be empty if 'additional_deps_path_fx' is not a valid directory path
                    std::vector<pal::string_t> list;
                    pal::readdir(additional_deps_path_fx, _X("*.deps.json"), &list);
                    for (pal::string_t json_file : list)
                    {
                        pal::string_t json_full_path = additional_deps_path_fx;
                        append_path(&json_full_path, json_file.c_str());

                        trace::verbose(_X("Using specified additional deps.json: '%s'"),
                            json_full_path.c_str());

                        m_additional_deps.push_back(deps_json_t::create_for_framework_dependent(json_full_path, rid_resolution_options));
                    }
                }
            }
        }
    }

    if (!m_additional_deps.empty())
        m_needs_file_existence_checks = true;
}

void deps_resolver_t::enum_app_context_deps_files(std::function<void(const pal::string_t&)> callback)
{
    auto begin_iter = m_fx_deps.cbegin();
    auto end_iter = m_fx_deps.cend();

    if ((m_host_mode == host_mode_t::libhost
        || (bundle::info_t::is_single_file_bundle() && !bundle::runner_t::app()->is_netcoreapp3_compat_mode()))
        && begin_iter != end_iter)
    {
        // Neither in a libhost scenario nor in a bundled app
        // the deps files should be exposed in the app context
        // properties.
        assert(begin_iter->get() == &get_app_deps());
        ++begin_iter;
    }

    for (auto it = begin_iter; it != m_fx_deps.cend(); ++it)
    {
        callback((*it)->get_deps_file());
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
    pal::realpath(&core_servicing, true);

    // Filter out non-serviced assets so the paths can be added after servicing paths.
    pal::string_t non_serviced;

    pal::string_t candidate;

    auto add_package_cache_entry = [&](const deps_entry_t& entry, const pal::string_t& deps_dir, int fx_level) -> bool
    {
        if (breadcrumb != nullptr && entry.is_serviceable)
        {
            breadcrumb->insert(entry.library_name + _X(",") + entry.library_version);
            breadcrumb->insert(entry.library_name);
        }

        if (items.count(entry.asset.name))
        {
            return true;
        }

        // Ignore placeholders
        if (ends_with(entry.asset.relative_path, _X("/_._"), false))
        {
            return true;
        }

        trace::verbose(_X("Processing native/culture for deps entry [%s, %s, %s]"),
            entry.library_name.c_str(), entry.library_version.c_str(), entry.asset.relative_path.c_str());

        bool found_in_bundle = false;
        if (probe_deps_entry(entry, deps_dir, fx_level, &candidate, found_in_bundle))
        {
            if (!found_in_bundle)
            {
                init_known_entry_path(entry, candidate);
                add_unique_path(asset_type, action(candidate), &items, output, &non_serviced, core_servicing);
            }
        }
        else
        {
            // For self-contained apps do not use the full package name
            // because of rid-fallback could happen (ex: CentOS falling back to RHEL)
            if ((entry.asset.name == _X("apphost")) && ends_with(entry.library_name, _X(".Microsoft.NETCore.DotNetAppHost"), false))
            {
                return report_missing_assembly_in_manifest(entry, true);
            }

            return report_missing_assembly_in_manifest(entry);
        }

        return true;
    };

    // Add app entries
    const auto& entries = get_app_deps().get_entries(asset_type);
    for (const auto& entry : entries)
    {
        if (!add_package_cache_entry(entry, m_app_dir, 0))
        {
            return false;
        }
    }

    // If the deps file is missing add known locations.
    if (!get_app_deps().exists())
    {
        // App local path
        add_unique_path(asset_type, m_app_dir, &items, output, &non_serviced, core_servicing);

        (void) library_exists_in_dir(m_app_dir, LIBCORECLR_NAME, &m_coreclr_path);
    }

    // Handle any additional deps.json that were specified.
    for (const auto& additional_deps : m_additional_deps)
    {
        const auto additional_deps_entries = additional_deps->get_entries(asset_type);
        for (const auto& entry : additional_deps_entries)
        {
            if (!add_package_cache_entry(entry, m_app_dir, 0))
            {
                return false;
            }
        }
    }

    // Add fx package locations to fx_dir
    for (int32_t i = 1; i < static_cast<int32_t>(m_fx_definitions.size()); ++i)
    {
        const auto& fx_entries = m_fx_deps[i]->get_entries(asset_type);

        for (const auto& entry : fx_entries)
        {
            if (!add_package_cache_entry(entry, m_fx_definitions[i]->get_dir(), i))
            {
                return false;
            }
        }
    }

    // If this is a single-file app, add the app's dir to the native search directories.
    if (bundle::info_t::is_single_file_bundle() && !is_resources)
    {
        auto bundle = bundle::runner_t::app();
        add_unique_path(asset_type, bundle->base_path(), &items, output, &non_serviced, core_servicing);

        // Add the extraction path if it exists.
        if (pal::directory_exists(bundle->extraction_path()))
        {
            add_unique_path(asset_type, bundle->extraction_path(), &items, output, &non_serviced, core_servicing);
        }
    }

    output->append(non_serviced);

    return true;
}


// -----------------------------------------------------------------------------
// Entrypoint to resolve TPA, native and resources path ordering to pass to CoreCLR.
//
//  Parameters:
//     probe_paths       - Pointer to struct containing fields that will contain
//                         resolved path ordering.
//     breadcrumb        - set of breadcrumb paths - or null if no breadcrumbs should be collected.
//     ignore_missing_assemblies - if set to true, resolving TPA assemblies will not fail if an assembly can't be found on disk
//                                 instead such entry will simply be ignored.
//
//
bool deps_resolver_t::resolve_probe_paths(probe_paths_t* probe_paths, std::unordered_set<pal::string_t>* breadcrumb, bool ignore_missing_assemblies)
{
    if (!resolve_tpa_list(&probe_paths->tpa, breadcrumb, ignore_missing_assemblies))
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

    return true;
}
