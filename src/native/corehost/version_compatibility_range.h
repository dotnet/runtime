// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __VERSION_COMPATIBILITY_RANGE_H_
#define __VERSION_COMPATIBILITY_RANGE_H_

// Defines teh allowed range of versions to consider during roll-forward search
enum class version_compatibility_range_t
{
    exact = 0,  // Only the specified version is allowed
    patch = 1,  // Any equal or higher version with the same major.minor
    minor = 2,  // Any equal or higher version with the same major
    major = 3,  // Any equal or higher version

    __last      // Sentinel value
};

pal::string_t version_compatibility_range_to_string(version_compatibility_range_t value);

#endif // __VERSION_COMPATIBILITY_RANGE_H_
