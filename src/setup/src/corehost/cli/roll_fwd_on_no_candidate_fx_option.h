// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
