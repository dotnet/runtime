// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <platformdefines.h>
#include <stdint.h>
#ifdef _WIN32
#include <memoryapi.h>
#else
#include <sys/mman.h>
#endif

extern "C" DLL_EXPORT void* AllocWithGuard(uintptr_t size)
{
#ifdef _WIN32
    void* reservePtr = VirtualAlloc(nullptr, size * 2, MEM_RESERVE, PAGE_READWRITE);
    if (reservePtr != nullptr)
    {
        void* ptr = VirtualAlloc(reservePtr, size, MEM_COMMIT, PAGE_READWRITE);
        if (ptr == nullptr)
        {
            VirtualFree(reservePtr, 0, MEM_RELEASE);
        }
        return ptr;
    }
#else
    uint8_t* ptr = (uint8_t*)mmap(nullptr, size * 2, PROT_READ | PROT_WRITE, MAP_PRIVATE | MAP_ANONYMOUS, -1, 0);
    if (ptr != MAP_FAILED)
    {
        if (mprotect(ptr + size, size, PROT_NONE) == 0)
        {
            return ptr;
        }
        munmap(ptr, size * 2);
    }
#endif
    return nullptr;
}

extern "C" DLL_EXPORT void Free(void* ptr, uintptr_t size)
{
#ifdef _WIN32
    VirtualFree(ptr, 0, MEM_RELEASE);
#else
    munmap(ptr, size * 2);
#endif
}
