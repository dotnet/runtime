// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef VXSORT_DEFS_H
#define VXSORT_DEFS_H

#include <minipal/utils.h>

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
#else
// MSVC
#include <intrin.h>
#define mess_up_cmov() _ReadBarrier();
#endif
#else
// GCC + Clang
#define mess_up_cmov()
#endif

#define INLINE FORCEINLINE

using std::max;
using std::min;
#endif  // VXSORT_DEFS_H
