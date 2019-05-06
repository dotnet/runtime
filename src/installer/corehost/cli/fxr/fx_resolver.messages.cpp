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
    trace::error(_X("The specified framework '%s', version '%s', apply_patches=%d, roll_forward=%s cannot roll-forward to the previously referenced version '%s'."),
        lower.get_fx_name().c_str(),
        lower.get_fx_version().c_str(),
        lower.get_apply_patches(),
        roll_forward_option_to_string(lower.get_roll_forward()).c_str(),
        higher.c_str());
}

void fx_resolver_t::display_compatible_framework_trace(
    const pal::string_t& higher,
    const fx_reference_t& lower)
{
    if (trace::is_enabled())
    {
        trace::verbose(_X("--- The specified framework '%s', version '%s', apply_patches=%d, roll_forward=%s is compatible with the previously referenced version '%s'."),
            lower.get_fx_name().c_str(),
            lower.get_fx_version().c_str(),
            lower.get_apply_patches(),
            roll_forward_option_to_string(lower.get_roll_forward()).c_str(),
            higher.c_str());
    }
}

void fx_resolver_t::display_retry_framework_trace(
    const fx_reference_t& fx_existing,
    const fx_reference_t& fx_new)
{
    if (trace::is_enabled())
    {
        trace::verbose(_X("--- Restarting all framework resolution because the previously resolved framework '%s', version '%s' must be re-resolved with the new version '%s', apply_patches=%d, roll_forward=%s ."),
            fx_existing.get_fx_name().c_str(),
            fx_existing.get_fx_version().c_str(),
            fx_new.get_fx_version().c_str(),
            fx_new.get_apply_patches(),
            roll_forward_option_to_string(fx_new.get_roll_forward()).c_str());
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

                trace::verbose(_X("     framework:'%s', lowest requested version='%s', found version='%s', effective reference version='%s' apply_patches=%d, roll_forward=%s, folder=%s"),
                    fx->get_name().c_str(),
                    fx->get_requested_version().c_str(),
                    fx->get_found_version().c_str(),
                    newest_ref->second.get_fx_version().c_str(),
                    newest_ref->second.get_apply_patches(),
                    roll_forward_option_to_string(newest_ref->second.get_roll_forward()).c_str(),
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

void fx_resolver_t::display_incompatible_loaded_framework_error(
    const pal::string_t& loaded_version,
    const fx_reference_t& fx_ref)
{
    trace::error(_X("The specified framework '%s', version '%s', apply_patches=%d, roll_forward=%s is incompatible with the previously loaded version '%s'."),
        fx_ref.get_fx_name().c_str(),
        fx_ref.get_fx_version().c_str(),
        fx_ref.get_apply_patches(),
        roll_forward_option_to_string(fx_ref.get_roll_forward()).c_str(),
        loaded_version.c_str());
}

void fx_resolver_t::display_missing_loaded_framework_error(const pal::string_t& fx_name)
{
    trace::error(_X("The specified framework '%s' is not present in the previously loaded runtime."), fx_name.c_str());
}