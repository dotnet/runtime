// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal.h"
#include "trace.h"
#include "roll_forward_option.h"
#include "roll_fwd_on_no_candidate_fx_option.h"

roll_forward_option roll_fwd_on_no_candidate_fx_to_roll_forward(roll_fwd_on_no_candidate_fx_option roll_fwd_on_no_candidate_fx)
{
    switch (roll_fwd_on_no_candidate_fx)
    {
    case roll_fwd_on_no_candidate_fx_option::disabled:
        return roll_forward_option::LatestPatch;
    case roll_fwd_on_no_candidate_fx_option::minor:
        return roll_forward_option::Minor;
    case roll_fwd_on_no_candidate_fx_option::major:
        return roll_forward_option::Major;
    default:
        assert(false);
        return roll_forward_option::Disable;
    }
}

namespace
{
    const pal::char_t* OptionNameMapping[] =
    {
        _X("Disable"),
        _X("LatestPatch"),
        _X("Minor"),
        _X("LatestMinor"),
        _X("Major"),
        _X("LatestMajor")
    };

    static_assert((sizeof(OptionNameMapping) / sizeof(*OptionNameMapping)) == static_cast<size_t>(roll_forward_option::__Last), "Invalid option count");
}

roll_forward_option roll_forward_option_from_string(const pal::string_t& value)
{
    for (int idx = 0; idx < static_cast<int>(roll_forward_option::__Last); idx++)
    {
        if (pal::strcasecmp(OptionNameMapping[idx], value.c_str()) == 0)
        {
            return static_cast<roll_forward_option>(idx);
        }
    }

    trace::error(_X("Unrecognized roll forward setting value '%s'."), value.c_str());
    return roll_forward_option::__Last;
}

