// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <minipal/utils.h>

#ifdef __wasm__
// The OS page size used by CoreCLR on WASM (16KB).
// WASM has no hardware pages; getpagesize() returns the 64KB memory.grow granularity,
// which is too coarse for GC alignment and thresholds.
int minipal_getpagesize(void)
{
    return 16 * 1024;
}
#elif HOST_WINDOWS
#include <Windows.h>
int minipal_getpagesize(void)
{
    static int cached_page_size = 0;
    if (cached_page_size == 0)
    {
        SYSTEM_INFO sysInfo;
        GetSystemInfo(&sysInfo);
        cached_page_size = (int)sysInfo.dwPageSize;
    }
    return cached_page_size;
}
#else
#include <unistd.h>
int minipal_getpagesize(void)
{
    static int cached_page_size = 0;
    if (cached_page_size == 0)
        cached_page_size = getpagesize();
    return cached_page_size;
}
#endif
