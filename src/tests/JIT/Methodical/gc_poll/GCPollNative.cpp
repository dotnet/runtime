// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <cstdio>
#include <cstdint>
#include <atomic>

#if defined(_MSC_VER)

#define STDMETHODCALLTYPE __stdcall
#define EXPORT(type) extern "C" type __declspec(dllexport)

#else // !defined(_MSC_VER)

#ifdef __i386__
#define STDMETHODCALLTYPE __attribute__((stdcall))
#else
#define STDMETHODCALLTYPE 
#endif
#define EXPORT(type) extern "C" __attribute__((visibility("default"))) type

#endif // defined(_MSC_VER)

namespace
{
    std::atomic<uint64_t> _n{ 0 };

    template<typename T>
    T NextUInt(T t)
    {
        return (T)((++_n) + t);
    }
}

EXPORT(uint32_t) STDMETHODCALLTYPE NextUInt32(uint32_t t)
{
    return NextUInt(t);
}

EXPORT(uint64_t) STDMETHODCALLTYPE NextUInt64(uint64_t t)
{
    return NextUInt(t);
}