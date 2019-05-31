// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __ROLL_FORWARD_OPTION_H_
#define __ROLL_FORWARD_OPTION_H_

// Specifies the roll forward option value
// High-level notes on roll forward algorithm
//  - Try to use the version which the app asked for. If not possible try to use the closest higher version (unless modified via settings)
//  - Always pick the latest patch for servicing/security
//  - Allow customization of the behavior via rollForward setting
//  - Backward compatible with deprecated settings rollForwardOnNoCandidateFx and applyPatches
enum class roll_forward_option
{
    // The order is in increasing level of relaxation
    // Lower values are more restrictive than higher values

    Disable = 0,     // No roll-forward is allowed - only exact match
    LatestPatch = 1, // Roll forward to latest patch.
    Minor = 2,       // Roll forward to closest minor but same major and then highest patch
    LatestMinor = 3, // Roll forward to highest minor.patch but same major
    Major = 4,       // Roll forward to closest major.minor and then highest patch
    LatestMajor = 5, // Roll forward to highest major.minor.patch

    __Last           // Sentinel value
};

roll_forward_option roll_forward_option_from_string(const pal::string_t& value);

#endif // __ROLL_FORWARD_OPTION_H_
