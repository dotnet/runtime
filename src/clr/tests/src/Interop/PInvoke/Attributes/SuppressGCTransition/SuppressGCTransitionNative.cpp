// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <xplatform.h>
#include <cstdint>
#include <atomic>
#include <string>
#include <algorithm>

namespace
{
    std::atomic<uint32_t> _n{ 0 };
}

extern "C"
BOOL DLL_EXPORT NextUInt(/* out */ uint32_t *n)
{
    if (n == nullptr)
        return FALSE;

    *n = (++_n);
    return TRUE;
}
