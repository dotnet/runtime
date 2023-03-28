// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __TFM_COMPAT_H__
#define __TFM_COMPAT_H__

#include <pal.h>

namespace tfm_compat
{
    bool is_multilevel_lookup_disabled(const pal::string_t& tfm);
    bool is_rid_fallback_graph_disabled(const pal::string_t& tfm);
}

#endif // __TFM_COMPAT_H__
