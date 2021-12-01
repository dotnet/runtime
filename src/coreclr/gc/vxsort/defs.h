// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef VXSORT_DEFS_H
#define VXSORT_DEFS_H

#if _MSC_VER
#ifdef _M_X86
#define ARCH_X86
#endif
#ifdef _M_X64
#define ARCH_X64
#endif
#ifdef _M_ARM64
#define ARCH_ARM
#endif
#else
#ifdef __i386__
#define ARCH_X86
#endif
#ifdef __amd64__
#define ARCH_X64
#endif
#ifdef __arm__
#define ARCH_ARM
#endif
#endif

#ifdef _MSC_VER
#ifdef __clang__
#define mess_up_cmov()
#define INLINE __attribute__((always_inline))
#define NOINLINE __attribute__((noinline))
#else
// MSVC
#include <intrin.h>
#define mess_up_cmov() _ReadBarrier();
#define INLINE __forceinline
#define NOINLINE __declspec(noinline)
#endif
#else
// GCC + Clang
#define mess_up_cmov()
#define INLINE __attribute__((always_inline))
#define NOINLINE __attribute__((noinline))
#endif

namespace std {
template <class _Ty>
class numeric_limits {
   public:
    static constexpr _Ty Max() { static_assert(sizeof(_Ty) != sizeof(_Ty), "func must be specialized!"); return _Ty(); }
    static constexpr _Ty Min() { static_assert(sizeof(_Ty) != sizeof(_Ty), "func must be specialized!"); return _Ty(); }
};

template <>
class numeric_limits<int32_t> {
public:
    static constexpr int32_t Max() { return 0x7fffffff; }
    static constexpr int32_t Min() { return -0x7fffffff - 1; }
};

template <>
class numeric_limits<uint32_t> {
public:
    static constexpr uint32_t Max() { return 0xffffffff; }
    static constexpr uint32_t Min() { return 0; }
};

template <>
class numeric_limits<int64_t> {
   public:
    static constexpr int64_t Max() { return 0x7fffffffffffffffi64; }

    static constexpr int64_t Min() { return -0x7fffffffffffffffi64 - 1; }
};
}  // namespace std

#ifndef max
template <typename T>
T max(T a, T b) {
    if (a > b)
        return a;
    else
        return b;
}
#endif

#endif  // VXSORT_DEFS_H
