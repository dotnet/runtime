// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Platform Abstraction Layer (PAL) implementation of functionality not covered by Unix APIs on WASM.
//

#include "common.h"
#include "CommonTypes.h"
#include "CommonMacros.h"
#include "daccess.h"
#include "Pal.h"

#include <stdlib.h>
#include <string.h>

#define PAGE_READWRITE 0x04
#define MEM_COMMIT     0x1000
#define MEM_RELEASE    0x8000

#ifndef FEATURE_WASM_MANAGED_THREADS
//
// Note that we return the native stack bounds here, not shadow stack ones. Currently this functionality is mainly
// used for RuntimeHelpers.TryEnsureSufficientExecutionStack, and we do use the native stack in codegen, so this
// is an acceptable approximation.
//
extern "C" unsigned char __stack_low;
extern "C" unsigned char __stack_high;
void PalGetMaximumStackBounds_SingleThreadedWasm(void** ppStackLowOut, void** ppStackHighOut)
{
    // See https://github.com/emscripten-core/emscripten/pull/18057 and https://reviews.llvm.org/D135910.
    unsigned char* pStackLow = &__stack_low;
    unsigned char* pStackHigh = &__stack_high;

    // Sanity check that we have the expected memory layout.
    ASSERT((pStackHigh - pStackLow) >= 64 * 1024);
    if (pStackLow >= pStackHigh)
    {
        PalPrintFatalError("\nFatal error. Unexpected stack layout.\n");
        RhFailFast();
    }

    *ppStackLowOut = pStackLow;
    *ppStackHighOut = pStackHigh;
}

#ifdef TARGET_WASI
using Dtor = void(*)(void*);

// Due to a bug in the toolchain, we have to provide an implementation of thread-local destruction.
// Since this is the single-threaded case, we simply delegate to the static destruction mechanism.
// Reference: https://github.com/llvm/llvm-project/blob/main/libcxxabi/src/cxa_thread_atexit.cpp.
//
extern "C" int __cxa_thread_atexit(Dtor dtor, void* obj, void*)
{
    struct DtorList
    {
        Dtor dtor;
        void* obj;
        DtorList* next;
    };

    struct DtorsManager
    {
        DtorList* m_dtors = nullptr;

        ~DtorsManager()
        {
            while (DtorList* head = m_dtors)
            {
                m_dtors = head->next;
                head->dtor(head->obj);
                free(head);
            }
        }
    };

    // The linked list of "thread-local" destructors to run.
    static DtorsManager s_dtorsManager;

    DtorList* head = static_cast<DtorList*>(malloc(sizeof(DtorList)));
    if (head == nullptr)
    {
        return -1;
    }

    head->dtor = dtor;
    head->obj = obj;
    head->next = s_dtorsManager.m_dtors;
    s_dtorsManager.m_dtors = head;

    return 0;
}
#endif // TARGET_WASI
#endif // !FEATURE_WASM_MANAGED_THREADS

// Recall that WASM's model is extremely simple: we have one linear memory, which can only be grown, in chunks
// of 64K pages. Thus, "mmap"/"munmap" fundamentally cannot be faithfully recreated and the Unix emulators we
// layer on top of (Emscripten/WASI libc) reflect this by not supporting the scenario. Fortunately, the current
// runtime does not require this functionality either, and so we can implement this function in terms of simple
// "malloc".
//
_Ret_maybenull_ _Post_writable_byte_size_(size) void* PalVirtualAlloc(uintptr_t size, uint32_t protect)
{
    if (protect != PAGE_READWRITE)
    {
        RhFailFast(); // Not supported per the above.
    }

    void* pRetVal;
    if (posix_memalign(&pRetVal, OS_PAGE_SIZE, size) != 0)
    {
        return nullptr;
    }

    memset(pRetVal, 0, size);
    return pRetVal;
}

void PalVirtualFree(_In_ void* pAddress, uintptr_t size)
{
    free(pAddress);
}

UInt32_BOOL PalVirtualProtect(_In_ void* pAddress, size_t size, uint32_t protect)
{
    if (protect == PAGE_READWRITE)
    {
        return UInt32_TRUE;
    }

    // WASM does not support page protection. All memory is always readable and writeable.
    RhFailFast();
    return UInt32_FALSE;
}
