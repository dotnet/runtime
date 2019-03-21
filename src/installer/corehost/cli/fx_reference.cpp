// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal.h"
#include "fx_ver.h"
#include "fx_reference.h"
#include "roll_fwd_on_no_candidate_fx_option.h"

bool fx_reference_t::is_roll_forward_compatible(const fx_ver_t& other) const
{
    // We expect the version to be <
    assert(get_fx_version_number() < other);

    if (get_fx_version_number() == other)
    {
        return true;
    }

    if (get_use_exact_version())
    {
        return false;
    }

    // Verify major roll forward
    if (get_fx_version_number().get_major() != other.get_major()
        && roll_fwd_on_no_candidate_fx != roll_fwd_on_no_candidate_fx_option::major)
    {
        return false;
    }

    // Verify minor roll forward
    if (get_fx_version_number().get_minor() != other.get_minor()
        && roll_fwd_on_no_candidate_fx != roll_fwd_on_no_candidate_fx_option::major
        && roll_fwd_on_no_candidate_fx != roll_fwd_on_no_candidate_fx_option::minor)
    {
        return false;
    }

    // Verify patch roll forward
    // We do not distinguish here whether a previous framework reference found a patch version based on:
    //  - initial reference matching a patch version,
    //  - or roll_fwd_on_no_candidate_fx_option=major\minor finding a compatible patch version as initial framework,
    //  - or applyPatches=true finding a newer patch version
    if (get_fx_version_number().get_patch() != other.get_patch()
        && patch_roll_fwd == false
        && roll_fwd_on_no_candidate_fx != roll_fwd_on_no_candidate_fx_option::major
        && roll_fwd_on_no_candidate_fx != roll_fwd_on_no_candidate_fx_option::minor)
    {
        return false;
    }

    // Release cannot roll forward to pre-release
    if (!get_fx_version_number().is_prerelease() && other.is_prerelease())
    {
        return false;
    }

    return true;
}

void fx_reference_t::apply_settings_from(const fx_reference_t& from)
{
    if (from.get_fx_version().length() > 0)
    {
        set_fx_version(from.get_fx_version());
    }

    const roll_fwd_on_no_candidate_fx_option* from_rollfwd = from.get_roll_fwd_on_no_candidate_fx();
    if (from_rollfwd != nullptr)
    {
        set_roll_fwd_on_no_candidate_fx(*from_rollfwd);
    }

    const bool* from_patch = from.get_patch_roll_fwd();
    if (from_patch != nullptr)
    {
        set_patch_roll_fwd(*from_patch);
    }
}

void fx_reference_t::merge_roll_forward_settings_from(const fx_reference_t& from)
{
    const roll_fwd_on_no_candidate_fx_option* from_rollfwd = from.get_roll_fwd_on_no_candidate_fx();
    if (from_rollfwd != nullptr)
    {
        const roll_fwd_on_no_candidate_fx_option* to_rollfwd = get_roll_fwd_on_no_candidate_fx();
        if (to_rollfwd == nullptr ||
            *from_rollfwd < *to_rollfwd)
        {
            set_roll_fwd_on_no_candidate_fx(*from_rollfwd);
        }
    }

    const bool* from_patch = from.get_patch_roll_fwd();
    if (from_patch != nullptr)
    {
        const bool* to_patch = get_patch_roll_fwd();
        if (to_patch == nullptr ||
            *from_patch == false)
        {
            set_patch_roll_fwd(*from_patch);
        }
    }
}
