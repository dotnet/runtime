// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

class HostAllocator final
{
private:
    HostAllocator()
    {
    }

public:
    template <typename T>
    T* allocate(size_t count)
    {
        ClrSafeInt<size_t> safeElemSize(sizeof(T));
        ClrSafeInt<size_t> safeCount(count);
        ClrSafeInt<size_t> size = safeElemSize * safeCount;
        if (size.IsOverflow())
        {
            return nullptr;
        }

        return static_cast<T*>(allocateHostMemory(size.Value()));
    }

    void deallocate(void* p)
    {
        freeHostMemory(p);
    }

    static HostAllocator getHostAllocator()
    {
        return HostAllocator();
    }

private:
    void* allocateHostMemory(size_t size);
    void freeHostMemory(void* p);
};

// Global operator new overloads that work with HostAllocator

inline void* __cdecl operator new(size_t n, HostAllocator alloc)
{
    return alloc.allocate<char>(n);
}

inline void* __cdecl operator new[](size_t n, HostAllocator alloc)
{
    return alloc.allocate<char>(n);
}
