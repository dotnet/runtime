// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

class HostAllocator final
{
private:
    static HostAllocator s_hostAllocator;

    HostAllocator()
    {
    }

public:
    void* Alloc(size_t size);

    void* ArrayAlloc(size_t elemSize, size_t numElems);

    void Free(void* p);

    static HostAllocator* getHostAllocator();
};

// Global operator new overloads that work with HostAllocator

inline void* __cdecl operator new(size_t n, HostAllocator* alloc)
{
    return alloc->Alloc(n);
}

inline void* __cdecl operator new[](size_t n, HostAllocator* alloc)
{
    return alloc->Alloc(n);
}
