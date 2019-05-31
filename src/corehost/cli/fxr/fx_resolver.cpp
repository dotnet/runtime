// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "fx_resolver.h"
#include "host_startup_info.h"
#include "trace.h"

namespace
{
    const int Max_Framework_Resolve_Retries = 100;

    static_assert(roll_forward_option::LatestPatch > roll_forward_option::Disable, "Code assumes ordering of roll-forward options from least restrictive to most restrictive");
    static_assert(roll_forward_option::Minor > roll_forward_option::LatestPatch, "Code assumes ordering of roll-forward options from least restrictive to most restrictive");
    static_assert(roll_forward_option::LatestMinor > roll_forward_option::Minor, "Code assumes ordering of roll-forward options from least restrictive to most restrictive");
    static_assert(roll_forward_option::Major > roll_forward_option::LatestMinor, "Code assumes ordering of roll-forward options from least restrictive to most restrictive");
    static_assert(roll_forward_option::LatestMajor > roll_forward_option::Major, "Code assumes ordering of roll-forward options from least restrictive to most restrictive");

    fx_ver_t search_for_best_framework_match_without_roll_to_latest_patch(
        const std::vector<fx_ver_t>& version_list,
        const fx_reference_t& fx_ref,
        bool release_only)
    {
        fx_ver_t best_match_version;

        if (fx_ref.get_version_compatibility_range() >= version_compatibility_range_t::patch)
        {
            trace::verbose(
                _X("'Roll forward' enabled with version_compatibility_range [%s]. Looking for the %s %s greater than or equal version to [%s]"),
                version_compatibility_range_to_string(fx_ref.get_version_compatibility_range()).c_str(),
                fx_ref.get_roll_to_highest_version() ? _X("highest") : _X("lowest"),
                release_only ? _X("release") : _X("release/pre-release"),
                fx_ref.get_fx_version().c_str());

            for (const auto& ver : version_list)
            {
                if ((!release_only || !ver.is_prerelease()) && ver >= fx_ref.get_fx_version_number())
                {
                    // Ignore incompatible versions
                    if (!fx_ref.is_compatible_with_higher_version(ver))
                    {
                        continue;
                    }

                    best_match_version = (best_match_version == fx_ver_t())
                        ? ver
                        : (fx_ref.get_roll_to_highest_version() ? std::max(best_match_version, ver) : std::min(best_match_version, ver));
                }
            }

            if (best_match_version == fx_ver_t())
            {
                trace::verbose(_X("No match greater than or equal to [%s] found."), fx_ref.get_fx_version().c_str());
            }
            else
            {
                trace::verbose(_X("Found version [%s]"), best_match_version.as_str().c_str());
            }
        }

        return best_match_version;
    }

    fx_ver_t automatic_roll_to_latest_patch(
        const std::vector<fx_ver_t>& version_list,
        const fx_reference_t& fx_ref,
        const fx_ver_t& start_with_version,
        bool release_only)
    {
        fx_ver_t best_match_version = start_with_version;

        // For Disable, there's no roll forward (in fact we should not even get here).
        // For backward compatibility reasons we also need to consider the apply_patches setting
        // For backward compatibility reasons the apply_patches for pre-release framework reference only applies to the patch portion of the version,
        //   we can still roll on the pre-release part of the version even if apply_patches=false.
        // If we've found a pre-release version match, then don't apply automatic roll to latest patch.
        if (fx_ref.get_version_compatibility_range() >= version_compatibility_range_t::patch &&
            !best_match_version.is_prerelease())
        {
            fx_ver_t apply_patch_from_version = start_with_version;
            if (apply_patch_from_version.is_empty())
            {
                apply_patch_from_version = fx_ref.get_fx_version_number();
            }

            trace::verbose(
                _X("Applying patch roll forward from [%s] on %s"),
                apply_patch_from_version.as_str().c_str(),
                release_only ? _X("release only") : _X("release/pre-release"));

            for (const auto& ver : version_list)
            {
                trace::verbose(_X("Inspecting version... [%s]"), ver.as_str().c_str());

                if ((!release_only || !ver.is_prerelease()) &&
                    (fx_ref.get_apply_patches() || ver.get_patch() == apply_patch_from_version.get_patch()) &&
                    ver >= apply_patch_from_version &&
                    ver.get_major() == apply_patch_from_version.get_major() &&
                    ver.get_minor() == apply_patch_from_version.get_minor())
                {
                    // Pick the greatest that differs only in patch.
                    best_match_version = std::max(ver, best_match_version);
                }
            }
        }

        return best_match_version;
    }

    fx_ver_t search_for_best_framework_match(
        const std::vector<fx_ver_t>& version_list,
        const fx_reference_t& fx_ref,
        bool release_only)
    {
        // Roll forward to the best version match without applying automatic roll forward to latest patch
        fx_ver_t best_match_version = search_for_best_framework_match_without_roll_to_latest_patch(version_list, fx_ref, release_only);

        if (!best_match_version.is_empty())
        {
            // Apply automatic roll to the latest patch
            best_match_version = automatic_roll_to_latest_patch(version_list, fx_ref, best_match_version, release_only);
        }

        return best_match_version;
    }

    fx_ver_t resolve_framework_reference_from_version_list(
        const std::vector<fx_ver_t>& version_list,
        const fx_reference_t& fx_ref)
    {
        trace::verbose(
            _X("Attempting FX roll forward starting from version='[%s]', apply_patches=%d, version_compatibility_range=%s, roll_to_highest_version=%d, prefer_release=%d"),
            fx_ref.get_fx_version().c_str(),
            fx_ref.get_apply_patches(),
            version_compatibility_range_to_string(fx_ref.get_version_compatibility_range()).c_str(),
            fx_ref.get_roll_to_highest_version(),
            fx_ref.get_prefer_release());

        // If the framework reference prefers release, then search for release versions only first.
        if (fx_ref.get_prefer_release())
        {
            fx_ver_t best_match_release_only = search_for_best_framework_match(
                version_list,
                fx_ref,
                /*release_only*/ true);

            if (best_match_release_only != fx_ver_t())
            {
                return best_match_release_only;
            }
        }

        // If release-only didn't find anything, or the framework reference has no preference to release,
        // do a full search on all versions.
        fx_ver_t best_match = search_for_best_framework_match(
            version_list,
            fx_ref,
            /*release_only*/ false);

        if (best_match == fx_ver_t())
        {
            // This is not strictly necessary, we just need to return version which doesn't exist.
            // But it's cleaner to return the desider reference then invalid -1.-1.-1 version.
            best_match = fx_ref.get_fx_version_number();
            trace::verbose(_X("Framework reference didn't resolve to any available version."));
        }
        else
        {
            trace::verbose(_X("Framework reference resolved to version '%s'."), best_match.as_str().c_str());
        }

        return best_match;
    }

    fx_definition_t* resolve_framework_reference(
        const fx_reference_t & fx_ref,
        const pal::string_t & oldest_requested_version,
        const pal::string_t & dotnet_dir)
    {
#if defined(DEBUG)
        assert(!fx_ref.get_fx_name().empty());
        assert(!fx_ref.get_fx_version().empty());

        fx_ver_t _debug_ver;
        assert(fx_ver_t::parse(fx_ref.get_fx_version(), &_debug_ver, false));
        assert(_debug_ver == fx_ref.get_fx_version_number());
#endif // defined(DEBUG)

        trace::verbose(_X("--- Resolving FX directory, name '%s' version '%s'"),
            fx_ref.get_fx_name().c_str(), fx_ref.get_fx_version().c_str());

        std::vector<pal::string_t> hive_dir;
        get_framework_and_sdk_locations(dotnet_dir, &hive_dir);

        pal::string_t selected_fx_dir;
        pal::string_t selected_fx_version;
        fx_ver_t selected_ver;

        for (pal::string_t dir : hive_dir)
        {
            auto fx_dir = dir;
            trace::verbose(_X("Searching FX directory in [%s]"), fx_dir.c_str());

            append_path(&fx_dir, _X("shared"));
            append_path(&fx_dir, fx_ref.get_fx_name().c_str());

            // Roll forward is disabled when:
            //   roll_forward is set to Disable
            //   roll_forward is set to LatestPatch AND
            //     apply_patches is false AND
            //     release framework reference (this is for backward compat with pre-release rolling over pre-release portion of version ignoring apply_patches)
            //   use exact version is set (this is when --fx-version was used on the command line)
            if ((fx_ref.get_version_compatibility_range() == version_compatibility_range_t::exact) ||
                ((fx_ref.get_version_compatibility_range() == version_compatibility_range_t::patch) && (!fx_ref.get_apply_patches() && !fx_ref.get_fx_version_number().is_prerelease())))
            {
                trace::verbose(
                    _X("Did not roll forward because apply_patches=%d, version_compatibility_range=%s chose [%s]"),
                    fx_ref.get_apply_patches(),
                    version_compatibility_range_to_string(fx_ref.get_version_compatibility_range()).c_str(),
                    fx_ref.get_fx_version().c_str());

                append_path(&fx_dir, fx_ref.get_fx_version().c_str());
                if (pal::directory_exists(fx_dir))
                {
                    selected_fx_dir = fx_dir;
                    selected_fx_version = fx_ref.get_fx_version();
                    break;
                }
            }
            else
            {
                std::vector<pal::string_t> list;
                std::vector<fx_ver_t> version_list;
                pal::readdir_onlydirectories(fx_dir, &list);

                for (const auto& version : list)
                {
                    fx_ver_t ver;
                    if (fx_ver_t::parse(version, &ver, false))
                    {
                        version_list.push_back(ver);
                    }
                }

                fx_ver_t resolved_ver = resolve_framework_reference_from_version_list(version_list, fx_ref);

                pal::string_t resolved_ver_str = resolved_ver.as_str();
                append_path(&fx_dir, resolved_ver_str.c_str());

                if (pal::directory_exists(fx_dir))
                {
                    if (selected_ver != fx_ver_t())
                    {
                        // Compare the previous hive_dir selection with the current hive_dir to see which one is the better match
                        std::vector<fx_ver_t> version_list;
                        version_list.push_back(resolved_ver);
                        version_list.push_back(selected_ver);
                        resolved_ver = resolve_framework_reference_from_version_list(version_list, fx_ref);
                    }

                    if (resolved_ver != selected_ver)
                    {
                        trace::verbose(_X("Changing Selected FX version from [%s] to [%s]"), selected_fx_dir.c_str(), fx_dir.c_str());
                        selected_ver = resolved_ver;
                        selected_fx_dir = fx_dir;
                        selected_fx_version = resolved_ver_str;
                    }
                }
            }
        }

        if (selected_fx_dir.empty())
        {
            trace::error(_X("It was not possible to find any compatible framework version"));
            return nullptr;
        }

        trace::verbose(_X("Chose FX version [%s]"), selected_fx_dir.c_str());

        return new fx_definition_t(fx_ref.get_fx_name(), selected_fx_dir, oldest_requested_version, selected_fx_version);
    }
}

StatusCode fx_resolver_t::reconcile_fx_references_helper(
    const fx_reference_t& lower_fx_ref,
    const fx_reference_t& higher_fx_ref,
    /*out*/ fx_reference_t& effective_fx_ref)
{
    if (!lower_fx_ref.is_compatible_with_higher_version(higher_fx_ref.get_fx_version_number()))
    {
        // Error condition - not compatible with the other reference
        display_incompatible_framework_error(higher_fx_ref.get_fx_version(), lower_fx_ref);
        return StatusCode::FrameworkCompatFailure;
    }

    effective_fx_ref = fx_reference_t(higher_fx_ref); // copy
    effective_fx_ref.merge_roll_forward_settings_from(lower_fx_ref);

    display_compatible_framework_trace(higher_fx_ref.get_fx_version(), lower_fx_ref);
    return StatusCode::Success;
}

// Reconciles two framework references into a new effective framework reference
// This process is sometimes also called "soft roll forward" (soft as in no IO)
// - fx_ref_a - one of the framework references to reconcile
// - fx_ref_b - the other framework reference to reconcile
// - effective_fx_ref - the resulting effective framework reference
//
// The function will
//   - Validate that the two references are compatible, of not it returns appropriate error code
//   - Pick the higher version from the two references and use that in the effective reference
//   - Merge roll forward settings and use the result in the effective reference
StatusCode fx_resolver_t::reconcile_fx_references(
    const fx_reference_t& fx_ref_a,
    const fx_reference_t& fx_ref_b,
    /*out*/ fx_reference_t& effective_fx_ref)
{
    // The function is split into the helper because the various tracing messages
    // make more sense if they're always written with higher/lower versions ordered in particular way.
    if (fx_ref_a.get_fx_version_number() >= fx_ref_b.get_fx_version_number())
    {
        return reconcile_fx_references_helper(fx_ref_b, fx_ref_a, effective_fx_ref);
    }
    else
    {
        return reconcile_fx_references_helper(fx_ref_a, fx_ref_b, effective_fx_ref);
    }
}

void fx_resolver_t::update_newest_references(
    const runtime_config_t& config)
{
    // Loop through each reference and update the list of newest references before we resolve_framework_reference.
    for (const fx_reference_t& fx_ref : config.get_frameworks())
    {
        const pal::string_t& fx_name = fx_ref.get_fx_name();
        auto temp_ref = m_effective_fx_references.find(fx_name);
        if (temp_ref == m_effective_fx_references.end())
        {
            m_effective_fx_references.insert({ fx_name, fx_ref });
            m_oldest_fx_references.insert({ fx_name, fx_ref });
        }
        else
        {
            if (fx_ref.get_fx_version_number() < m_oldest_fx_references[fx_name].get_fx_version_number())
            {
                m_oldest_fx_references[fx_name] = fx_ref;
            }
        }
    }
}

// Processes one framework's runtime configuration.
// For the most part this is about resolving framework references.
// - host_info
//     Information about the host - mainly used to determine where to search for frameworks.
// - override_settings
//     Framework resolution settings which will win over anything found (settings comming from command line).
//     Passed as fx_reference_t for simplicity, the version part of that structure is ignored.
// - config
//     Parsed runtime configuration to process.
// - effective_parent_fx_ref
//     The framework reference which was used to resolve the framework we're about to read.
//     Some settings are propagated from the parent framework reference to the processing of the framework itself
//     so this is used to access those settings.
// - fx_definitions
//     List of "hard" resolved frameworks, that is frameworks actually found on the disk.
//     Frameworks are added to the list as they are resolved.
//     The order in the list is maintained such that the app is always the first and then the framework in their dependency order.
//     That means that the root framework (typically Microsoft.NETCore.App) is the last.
//     Frameworks are never removed as there's no operation which would "remove" a framework reference.
//     Frameworks are never updated in the list. If such operation is required, instead the function returns FrameworkCompatRetry
//     and the caller will restart the framework resolution process (with new fx_definitions).
// Return value
//     Success - all frameworks were successfully resolved and the final (disk resolved) frameworks are in the fx_definitions.
//     FrameworkCompatRetry - the resolution algorithm needs to restart as some of already processed references has changed.
//     FrameworkCompatFailure - the resolution failed with unrecoverable error which is due to framework resolution algorithm itself.
//     FrameworkMissingFailure - the resolution failed because the requested framework doesn't exist on disk.
//     InvalidConfigFile - reading of a runtime config for some of the processed frameworks has failed.
StatusCode fx_resolver_t::read_framework(
    const host_startup_info_t & host_info,
    const runtime_config_t::settings_t& override_settings,
    const runtime_config_t & config,
    const fx_reference_t * effective_parent_fx_ref,
    fx_definition_vector_t & fx_definitions)
{
    // This reconciles duplicate references to minimize the number of resolve retries.
    update_newest_references(config);

    StatusCode rc = StatusCode::Success;

    // Loop through each reference and resolve the framework
    for (const fx_reference_t& original_fx_ref : config.get_frameworks())
    {
        fx_reference_t fx_ref = original_fx_ref;

        // Propagate the roll_to_highest_version into all framework references inside the framework
        if (effective_parent_fx_ref != nullptr && effective_parent_fx_ref->get_roll_to_highest_version())
        {
            fx_ref.set_roll_to_highest_version(true);
        }

        const pal::string_t& fx_name = fx_ref.get_fx_name();
        const fx_reference_t& current_effective_fx_ref = m_effective_fx_references[fx_name];
        fx_reference_t new_effective_fx_ref;

        auto existing_framework = std::find_if(
            fx_definitions.begin(),
            fx_definitions.end(),
            [&](const std::unique_ptr<fx_definition_t> & fx) { return fx_name == fx->get_name(); });

        if (existing_framework == fx_definitions.end())
        {
            // Reconcile the framework reference with the most up to date so far we have for the framework.
            // This does not read any physical framework folders yet.
            // Since we didn't find the framework in the resolved list yet, it's OK to update the effective reference
            // as we haven't processed it yet.
            rc = reconcile_fx_references(fx_ref, current_effective_fx_ref, new_effective_fx_ref);
            if (rc)
            {
                break; // Error case
            }

            m_effective_fx_references[fx_name] = new_effective_fx_ref;

            // Resolve the effective framework reference against the the existing physical framework folders
            fx_definition_t* fx = resolve_framework_reference(new_effective_fx_ref, m_oldest_fx_references[fx_name].get_fx_version(), host_info.dotnet_root);
            if (fx == nullptr)
            {
                display_missing_framework_error(fx_name, new_effective_fx_ref.get_fx_version(), pal::string_t(), host_info.dotnet_root);
                return FrameworkMissingFailure;
            }

            // Do NOT update the effective reference to have the same version as the resolved framework.
            // This could prevent correct resolution in some cases.
            // For example if the resolution starts with reference "2.1.0 LatestMajor" the resolution could
            // return "3.0.0". If later on we find another reference "2.1.0 Minor", while the two references are compatible
            // we would not be able to resolve it, since we would compare "2.1.0 Minor" with "3.0.0 LatestMajor" which are
            // not compatible.
            // So instead leave the effective reference as is. If the above situation occurs, the reference reconciliation
            // will change the effective reference from "2.1.0 LatestMajor" to "2.1.0 Minor" and restart the framework resolution process.
            // So during the second run we will resolve for example "2.2.0" which will be compatible with both framework references.

            fx_definitions.push_back(std::unique_ptr<fx_definition_t>(fx));

            // Recursively process the base frameworks
            pal::string_t config_file;
            pal::string_t dev_config_file;
            get_runtime_config_paths(fx->get_dir(), fx_name, &config_file, &dev_config_file);
            fx->parse_runtime_config(config_file, dev_config_file, override_settings);

            runtime_config_t new_config = fx->get_runtime_config();
            if (!new_config.is_valid())
            {
                trace::error(_X("Invalid framework config.json [%s]"), new_config.get_path().c_str());
                return StatusCode::InvalidConfigFile;
            }

            rc = read_framework(host_info, override_settings, new_config, &new_effective_fx_ref, fx_definitions);
            if (rc)
            {
                break; // Error case
            }
        }
        else
        {
            // Reconcile the framework reference with the most up to date so far we have for the framework.
            // Note that since we found the framework in the already resolved frameworks
            // any update to the effective framework reference needs to restart the resolution process
            // so that we re-resolve the framework against disk.
            rc = reconcile_fx_references(fx_ref, current_effective_fx_ref, new_effective_fx_ref);
            if (rc)
            {
                break; // Error case
            }

            if (new_effective_fx_ref != current_effective_fx_ref)
            {
                display_retry_framework_trace(current_effective_fx_ref, fx_ref);

                m_effective_fx_references[fx_name] = new_effective_fx_ref;
                return StatusCode::FrameworkCompatRetry;
            }

            // Success but move it to the back (without calling dtors) so that lower-level frameworks come last including Microsoft.NetCore.App
            std::rotate(existing_framework, existing_framework + 1, fx_definitions.end());
        }
    }

    return rc;
}

fx_resolver_t::fx_resolver_t()
{
}

StatusCode fx_resolver_t::resolve_frameworks_for_app(
    const host_startup_info_t & host_info,
    const runtime_config_t::settings_t& override_settings,
    const runtime_config_t & app_config,
    fx_definition_vector_t & fx_definitions)
{
    fx_resolver_t resolver;

    // Read the shared frameworks; retry is necessary when a framework is already resolved, but then a newer compatible version is processed.
    StatusCode rc = StatusCode::Success;
    int retry_count = 0;
    do
    {
        fx_definitions.resize(1); // Erase any existing frameworks for re-try
        rc = resolver.read_framework(host_info, override_settings, app_config, /*effective_parent_fx_ref*/ nullptr,  fx_definitions);
    } while (rc == StatusCode::FrameworkCompatRetry && retry_count++ < Max_Framework_Resolve_Retries);

    assert(retry_count < Max_Framework_Resolve_Retries);

    if (rc == StatusCode::Success)
    {
        display_summary_of_frameworks(fx_definitions, resolver.m_effective_fx_references);
    }

    return rc;
}

bool fx_resolver_t::is_config_compatible_with_frameworks(
    const runtime_config_t &config,
    const std::unordered_map<pal::string_t, const fx_ver_t> &existing_framework_versions_by_name)
{
    // Loop through each reference and check if it is compatible
    for (const fx_reference_t& fx_ref : config.get_frameworks())
    {
        const pal::string_t& fx_name = fx_ref.get_fx_name();
        auto iter = existing_framework_versions_by_name.find(fx_name);
        if (iter == existing_framework_versions_by_name.cend())
        {
            display_missing_loaded_framework_error(fx_name);
            return false;
        }

        const fx_ver_t &existing_version = (*iter).second;

        if (existing_version < fx_ref.get_fx_version_number()
            || !fx_ref.is_compatible_with_higher_version(existing_version))
        {
            display_incompatible_loaded_framework_error(existing_version.as_str(), fx_ref);
            return false;
        }

        // Base frameworks must already have been resolved / checked if this framework is already available
    }

    return true;
}