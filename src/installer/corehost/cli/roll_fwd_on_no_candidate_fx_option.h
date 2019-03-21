// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __ROLL_FWD_ON_NO_CANDIDATE_FX_OPTION_H_
#define __ROLL_FWD_ON_NO_CANDIDATE_FX_OPTION_H_

// Specifies the roll forward capability for finding the closest (most compatible) framework
// Note that the "applyPatches" bool option is separate from this and occurs after roll forward.
enum class roll_fwd_on_no_candidate_fx_option
{
    disabled = 0,
    minor,          // also inludes patch
    major           // also inludes minor and patch
};

#endif // __ROLL_FWD_ON_NO_CANDIDATE_FX_OPTION_H_
