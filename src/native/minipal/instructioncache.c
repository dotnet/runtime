// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <minipal/instructioncache.h>
#include <stdint.h>

#if defined(HOST_WINDOWS)
#include <windows.h>
#elif defined(__linux__) && defined(HOST_ARM)
#include <minipal/ospagesize.h>
#elif defined(HOST_RISCV64)
#include <unistd.h>
#include <sys/syscall.h>
#elif defined(HOST_APPLE) && !defined(HOST_OSX)
#include <libkern/OSCacheControl.h>
#endif

void minipal_flush_instruction_cache(const void* address, size_t size)
{
#if defined(HOST_WINDOWS)
    FlushInstructionCache(GetCurrentProcess(), address, size);
#else
    if (address == NULL)
    {
        return;
    }

#if defined(__linux__) && defined(HOST_ARM)
    // On Linux/arm (at least on 3.10) we found that there is a problem with __do_cache_op (arch/arm/kernel/traps.c)
    // implementing cacheflush syscall. cacheflush flushes only the first page in range [address, address + size)
    // and leaves other pages in undefined state which causes random tests failures (often due to SIGSEGV) with no particular pattern.
    //
    // As a workaround, we call __builtin___clear_cache on each page separately.

    const size_t pageSize = minipal_getpagesize();
    uintptr_t begin = (uintptr_t)address;
    const uintptr_t end = begin + size;

    while (begin < end)
    {
        uintptr_t endOrNextPageBegin = (begin + pageSize) & ~(pageSize - 1);
        if (endOrNextPageBegin > end)
            endOrNextPageBegin = end;

        __builtin___clear_cache((char *)begin, (char *)endOrNextPageBegin);
        begin = endOrNextPageBegin;
    }
#elif defined(HOST_RISCV64)
    // __clear_cache() expanded from __builtin___clear_cache() is not implemented
    // on Linux/RISCV64, at least in Clang 14, and we have to make syscall directly.
    //
    // TODO-RISCV64: use __builtin___clear_cache() in future. See https://github.com/llvm/llvm-project/issues/63551

#ifndef __NR_riscv_flush_icache
    #define __NR_riscv_flush_icache 259
#endif

    syscall(__NR_riscv_flush_icache, (char *)address, (char *)address + size, 0 /* all harts */);
#elif defined(HOST_WASM)
    // Nothing to do, there is no instruction cache to flush.
    (void)size;
#elif defined(HOST_APPLE) && !defined(HOST_OSX)
    sys_icache_invalidate((void *)address, size);
#else
    __builtin___clear_cache((char *)address, (char *)address + size);
#endif
#endif // HOST_WINDOWS
}
