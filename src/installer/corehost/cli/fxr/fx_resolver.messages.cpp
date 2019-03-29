// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "fx_resolver.h"
#include "framework_info.h"

/**
* When the framework is referenced more than once in a non-compatible way, display detailed error message
*   about available frameworks and installation of new framework.
*/
void fx_resolver_t::display_incompatible_framework_error(
    const pal::string_t& higher,
    const fx_reference_t& lower)
{
    assert(lower.get_patch_roll_fwd() != nullptr);
    assert(lower.get_roll_fwd_on_no_candidate_fx() != nullptr);

    trace::error(_X("The specified framework '%s', version '%s', patch_roll_fwd=%d, roll_fwd_on_no_candidate_fx=%d cannot roll-forward to the previously referenced version '%s'."),
        lower.get_fx_name().c_str(),
        lower.get_fx_version().c_str(),
        *lower.get_patch_roll_fwd(),
        *lower.get_roll_fwd_on_no_candidate_fx(),
        higher.c_str());
}

void fx_resolver_t::display_compatible_framework_trace(
    const pal::string_t& higher,
    const fx_reference_t& lower)
{
    if (trace::is_enabled())
    {
        assert(lower.get_patch_roll_fwd() != nullptr);
        assert(lower.get_roll_fwd_on_no_candidate_fx() != nullptr);

        trace::verbose(_X("--- The specified framework '%s', version '%s', patch_roll_fwd=%d, roll_fwd_on_no_candidate_fx=%d is compatible with the previously referenced version '%s'."),
            lower.get_fx_name().c_str(),
            lower.get_fx_version().c_str(),
            *lower.get_patch_roll_fwd(),
            *lower.get_roll_fwd_on_no_candidate_fx(),
            higher.c_str());
    }
}

void fx_resolver_t::display_retry_framework_trace(
    const fx_reference_t& fx_existing,
    const fx_reference_t& fx_new)
{
    if (trace::is_enabled())
    {
        assert(fx_new.get_patch_roll_fwd() != nullptr);
        assert(fx_new.get_roll_fwd_on_no_candidate_fx() != nullptr);

        trace::verbose(_X("--- Restarting all framework resolution because the previously resolved framework '%s', version '%s' must be re-resolved with the new version '%s', patch_roll_fwd=%d, roll_fwd_on_no_candidate_fx=%d ."),
            fx_existing.get_fx_name().c_str(),
            fx_existing.get_fx_version().c_str(),
            fx_new.get_fx_version().c_str(),
            *fx_new.get_patch_roll_fwd(),
            *fx_new.get_roll_fwd_on_no_candidate_fx());
    }
}

void fx_resolver_t::display_summary_of_frameworks(
    const fx_definition_vector_t& fx_definitions,
    const fx_name_to_fx_reference_map_t& newest_references)
{
    if (trace::is_enabled())
    {
        trace::verbose(_X("--- Summary of all frameworks:"));

        bool is_app = true;
        for (const auto& fx : fx_definitions)
        {
            if (is_app)
            {
                is_app = false; // skip the app
            }
            else
            {
                auto newest_ref = newest_references.find(fx->get_name());
                assert(newest_ref != newest_references.end());
                assert(newest_ref->second.get_fx_version() == fx->get_found_version());
                assert(newest_ref->second.get_patch_roll_fwd() != nullptr);
                assert(newest_ref->second.get_roll_fwd_on_no_candidate_fx() != nullptr);

                trace::verbose(_X("     framework:'%s', lowest requested version='%s', found version='%s', patch_roll_fwd=%d, roll_fwd_on_no_candidate_fx=%d, folder=%s"),
                    fx->get_name().c_str(),
                    fx->get_requested_version().c_str(),
                    fx->get_found_version().c_str(),
                    *newest_ref->second.get_patch_roll_fwd(),
                    *newest_ref->second.get_roll_fwd_on_no_candidate_fx(),
                    fx->get_dir().c_str());
            }
        }
    }
}

/**
* When the framework is not found, display detailed error message
*   about available frameworks and installation of new framework.
*/
void fx_resolver_t::display_missing_framework_error(
    const pal::string_t& fx_name,
    const pal::string_t& fx_version,
    const pal::string_t& fx_dir,
    const pal::string_t& dotnet_root)
{
    std::vector<framework_info> framework_infos;
    pal::string_t fx_ver_dirs;
    if (fx_dir.length())
    {
        fx_ver_dirs = fx_dir;
        framework_info::get_all_framework_infos(get_directory(fx_dir), fx_name, &framework_infos);
    }
    else
    {
        fx_ver_dirs = dotnet_root;
    }

    framework_info::get_all_framework_infos(dotnet_root, fx_name, &framework_infos);

    // Display the error message about missing FX.
    if (fx_version.length())
    {
        trace::error(_X("The specified framework '%s', version '%s' was not found."), fx_name.c_str(), fx_version.c_str());
    }
    else
    {
        trace::error(_X("The specified framework '%s' was not found."), fx_name.c_str());
    }

    if (framework_infos.size())
    {
        trace::error(_X("  - The following frameworks were found:"));
        for (const framework_info& info : framework_infos)
        {
            trace::error(_X("      %s at [%s]"), info.version.as_str().c_str(), info.path.c_str());
        }
    }
    else
    {
        trace::error(_X("  - No frameworks were found."));
    }

    trace::error(_X(""));
    trace::error(_X("You can resolve the problem by installing the specified framework and/or SDK."));
    trace::error(_X(""));
    trace::error(_X("The .NET Core frameworks can be found at:"));
    trace::error(_X("  - %s"), DOTNET_CORE_DOWNLOAD_URL);
}
