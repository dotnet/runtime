// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <cassert>
#include "framework_info.h"
#include "fx_definition.h"
#include "fx_muxer.h"
#include "fx_reference.h"
#include "fx_ver.h"
#include "pal.h"
#include "trace.h"

/**
* When the framework is referenced more than once in a non-compatible way, display detailed error message
*   about available frameworks and installation of new framework.
*/
void fx_muxer_t::display_incompatible_framework_error(
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

void fx_muxer_t::display_compatible_framework_trace(
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

void fx_muxer_t::display_retry_framework_trace(
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

void fx_muxer_t::display_summary_of_frameworks(
    const fx_definition_vector_t& fx_definitions,
    const fx_name_to_fx_reference_map_t& newest_references
)
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
