// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Implementation of minipal_getpagesize for non-WASM platforms. On WASM the page
// size is a compile-time constant and the function is defined inline in the
// header; this file is excluded from the build on WASM by
// src/native/minipal/CMakeLists.txt to avoid an empty translation unit.

#include "ospagesize.h"

#ifdef _WIN32
#include <windows.h>
#else
#include <unistd.h>
#endif

uint32_t minipal_getpagesize(void)
{
    // Process-wide constant. Any thread that races to initialize the cache writes
    // the same value, so no synchronization is required.
    static uint32_t cached_page_size = 0;
    uint32_t page_size = cached_page_size;
    if (page_size == 0)
    {
#ifdef _WIN32
        SYSTEM_INFO sysInfo;
        GetSystemInfo(&sysInfo);
        page_size = (uint32_t)sysInfo.dwPageSize;
#else
        page_size = (uint32_t)getpagesize();
#endif
        cached_page_size = page_size;
    }
    return page_size;
}
