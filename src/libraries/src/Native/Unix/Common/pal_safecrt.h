// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include <string.h>
#include <assert.h>
#include <errno.h>

typedef int errno_t;

static errno_t memcpy_s(void* dst, size_t sizeInBytes, const void* src, size_t count)
{
    if (count > 0)
    {
        assert(dst != nullptr);
        assert(src != nullptr);
        assert(sizeInBytes >= count);
        assert( // should be using memmove if this fails
            (static_cast<const char*>(dst) + count <= static_cast<const char*>(src)) ||
            (static_cast<const char*>(src) + count <= static_cast<const char*>(dst)));

        if (dst == nullptr)
        {
            return EINVAL;
        }

        if (src == nullptr || sizeInBytes < count)
        {
            memset(dst, 0, sizeInBytes);
            return src == nullptr ? EINVAL : ERANGE;
        }
  
        memcpy(dst, src, count);
    }

    return 0;
}
