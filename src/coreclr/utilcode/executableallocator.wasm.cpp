// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "executableallocator.h"

// Loader heaps and interpreter method data share this interface with native code heaps.
// WASM allocations are ordinary read-write linear memory, never executable mappings.

ExecutableAllocator* ExecutableAllocator::g_instance = nullptr;
ExecutableAllocator::FatalErrorHandler ExecutableAllocator::g_fatalErrorHandler = nullptr;

ExecutableAllocator* ExecutableAllocator::Instance() noexcept
{
    LIMITED_METHOD_CONTRACT;
    return g_instance;
}

HRESULT ExecutableAllocator::StaticInitialize(FatalErrorHandler fatalErrorHandler)
{
    LIMITED_METHOD_CONTRACT;

    g_fatalErrorHandler = fatalErrorHandler;
    g_instance = new (nothrow) ExecutableAllocator();
    return g_instance != nullptr ? S_OK : E_OUTOFMEMORY;
}

ExecutableAllocator::~ExecutableAllocator() = default;

bool ExecutableAllocator::IsWXORXEnabled()
{
    LIMITED_METHOD_CONTRACT;
    return false;
}

void ExecutableAllocator::InitLazyPreferredRange(size_t, size_t, int)
{
    LIMITED_METHOD_CONTRACT;
}

void ExecutableAllocator::ResetLazyPreferredRangeHint()
{
    LIMITED_METHOD_CONTRACT;
}

void ExecutableAllocator::InitPreferredRange()
{
    LIMITED_METHOD_CONTRACT;
}

bool ExecutableAllocator::IsPreferredExecutableRange(void*)
{
    LIMITED_METHOD_CONTRACT;
    return false;
}

void* ExecutableAllocator::Reserve(size_t size)
{
    LIMITED_METHOD_CONTRACT;
    return VMToOSInterface::AllocateMemory(nullptr, size, VMToOSInterface::MemoryProtection::ReadWrite);
}

void* ExecutableAllocator::ReserveWithinRange(size_t size, const void*, const void*)
{
    LIMITED_METHOD_CONTRACT;
    return Reserve(size);
}

void* ExecutableAllocator::Commit(void* pStart, size_t, bool)
{
    LIMITED_METHOD_CONTRACT;
    return pStart;
}

void ExecutableAllocator::Release(void* address, size_t size) noexcept
{
    LIMITED_METHOD_CONTRACT;
    if (!VMToOSInterface::ReleaseMemoryReservation(address, size))
    {
        g_fatalErrorHandler(COR_E_EXECUTIONENGINE, W("Releasing the memory reservation failed"));
    }
}

void* ExecutableAllocator::MapRW(void* address, size_t, CacheableMapping)
{
    LIMITED_METHOD_CONTRACT;
    return address;
}

void ExecutableAllocator::UnmapRW(void*) noexcept
{
    LIMITED_METHOD_CONTRACT;
}
