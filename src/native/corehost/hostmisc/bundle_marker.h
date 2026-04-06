// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef BUNDLE_MARKER_H
#define BUNDLE_MARKER_H

#include <stdint.h>
#include <stdbool.h>

#ifdef __cplusplus
extern "C" {
#endif

// Returns the bundle header offset. Zero for non-bundle apphosts.
int64_t bundle_marker_header_offset(void);

// Returns true if this is a bundled single-file app.
static inline bool bundle_marker_is_bundle(void)
{
    return bundle_marker_header_offset() != 0;
}

#ifdef __cplusplus
}

// C++ wrapper for backward compatibility with existing C++ code (e.g., dotnet/dotnet.cpp).
struct bundle_marker_t
{
    static int64_t header_offset()
    {
        return bundle_marker_header_offset();
    }

    static bool is_bundle()
    {
        return bundle_marker_is_bundle();
    }
};
#endif

#endif // BUNDLE_MARKER_H
