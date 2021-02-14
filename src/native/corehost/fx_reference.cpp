// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal.h"
#include "fx_ver.h"
#include "fx_reference.h"
#include "roll_fwd_on_no_candidate_fx_option.h"

bool fx_reference_t::is_compatible_with_higher_version(const fx_ver_t& higher_version) const
{
    assert(fx_version_number <= higher_version);

    if (fx_version_number == higher_version)
    {
        return true;
    }

    // Verify major roll forward
    if (fx_version_number.get_major() != higher_version.get_major()
        && version_compatibility_range < version_compatibility_range_t::major)
    {
        return false;
    }

    // Verify minor roll forward
    if (fx_version_number.get_minor() != higher_version.get_minor()
        && version_compatibility_range < version_compatibility_range_t::minor)
    {
        return false;
    }

    // Verify patch roll forward
    if (get_fx_version_number().get_patch() != higher_version.get_patch()
        && version_compatibility_range == version_compatibility_range_t::patch
        && apply_patches == false)
    {
        return false;
    }

    // In here it means that either everything but pre-release part is the same, or the difference is OK
    // The roll-forward rules don't affect pre-release roll forward except when
    //  - rollForward is Disable - in which case no roll forward should occur, and the versions must exactly match
    //  - rollForward is LatestPatch and applyPatches=false - which would normally mean exactly the same as Disable, but
    //    for backward compat reasons this is a special case. In this case applyPatches is ignored for pre-release versions.
    //    So even if pre-release are different, the versions are compatible.
    if (version_compatibility_range == version_compatibility_range_t::exact)
    {
        // We know the versions are different since we compared 100% equality above, so they're not compatible.
        // In here the versions could differ in patch or pre-release, in both cases they're not compatible.
        return false;
    }

    // Concerning pre-release versions
    //  - Pre-release is allowed to roll to any version (release or pre-release)
    //  - Release should prefer rolling to release, but is allowed to roll to pre-release if no compatible release is available
    // This function only compares framework references, it doesn't resolve framework reference to the available framework on disk.
    // As such it can't implement the "release should prefer release" as that requires the knowledge of all available versions.

    return true;
}

void fx_reference_t::merge_roll_forward_settings_from(const fx_reference_t& from)
{
    if (from.version_compatibility_range < version_compatibility_range)
    {
        version_compatibility_range = from.version_compatibility_range;
    }

    if (from.roll_to_highest_version)
    {
        roll_to_highest_version = from.roll_to_highest_version;
    }

    if (from.apply_patches == false)
    {
        apply_patches = false;
    }

    if (from.prefer_release)
    {
        prefer_release = true;
    }
}
