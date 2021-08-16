// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "bundle_marker.h"
#include "pal.h"
#include "trace.h"
#include "utils.h"

int64_t bundle_marker_t::header_offset()
{
    // Contains the bundle_placeholder default value at compile time.
    // If this is a single-file bundle, the first 8 bytes are replaced 
    // by "dotnet publish" with the offset where the bundle_header is located.
    static volatile uint8_t placeholder[] = 
    {
        // 8 bytes represent the bundle header-offset 
        // Zero for non-bundle apphosts (default).
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        // 64 bytes represent the bundle signature: SHA-256 for ".net core bundle"
        0x8b, 0x12, 0x02, 0xb9, 0x6a, 0x61, 0x20, 0x38,
        0x72, 0x7b, 0x93, 0x02, 0x14, 0xd7, 0xa0, 0x32,
        0x13, 0xf5, 0xb9, 0xe6, 0xef, 0xae, 0x33, 0x18,
        0xee, 0x3b, 0x2d, 0xce, 0x24, 0xb3, 0x6a, 0xae
    };

    volatile bundle_marker_t* marker = reinterpret_cast<volatile bundle_marker_t *>(placeholder);

    return marker->locator.bundle_header_offset;
}
