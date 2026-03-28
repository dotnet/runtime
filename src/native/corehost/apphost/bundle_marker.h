// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __BUNDLE_MARKER_H__
#define __BUNDLE_MARKER_H__

#include "apphost_bundle_marker.h"

// C++ wrapper for backward compatibility with existing C++ code (e.g., corehost.cpp).
// The actual implementation is in apphost_bundle_marker.c.
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

#endif // __BUNDLE_MARKER_H__
