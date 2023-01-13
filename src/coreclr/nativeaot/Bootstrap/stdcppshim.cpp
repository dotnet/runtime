// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdlib.h>

namespace std
{
    struct nothrow_t {};
    extern const nothrow_t nothrow = {};
}

void* operator new(size_t n, const std::nothrow_t&) noexcept
{
    return malloc(n);
}

void* operator new[](size_t n, const std::nothrow_t&) noexcept
{
    return malloc(n);
}

void operator delete(void *p) noexcept
{
    free(p);
}

void operator delete[](void *p) noexcept
{
    free(p);
}
