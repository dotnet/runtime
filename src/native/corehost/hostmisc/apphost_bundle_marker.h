// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef APPHOST_BUNDLE_MARKER_H
#define APPHOST_BUNDLE_MARKER_H

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
#endif

#endif // APPHOST_BUNDLE_MARKER_H
