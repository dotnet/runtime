// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal.h"
#include "trace.h"
#include "roll_forward_option.h"

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
