// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal.h"
#include "version_compatibility_range.h"

namespace
{
    const pal::char_t* OptionNameMapping[] =
    {
        _X("exact"),
        _X("patch"),
        _X("minor"),
        _X("major")
    };

    static_assert((sizeof(OptionNameMapping) / sizeof(*OptionNameMapping)) == static_cast<size_t>(version_compatibility_range_t::__last), "Invalid option count");
}

pal::string_t version_compatibility_range_to_string(version_compatibility_range_t value)
{
    int idx = static_cast<int>(value);
    assert(0 <= idx && idx < static_cast<int>(version_compatibility_range_t::__last));

    return OptionNameMapping[idx];
}
