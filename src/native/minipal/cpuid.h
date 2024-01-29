// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_CPUID_H
#define HAVE_MINIPAL_CPUID_H

#if defined(HOST_X86) || defined(HOST_AMD64)

#if defined(HOST_WINDOWS)

#include <intrin.h>

#endif // HOST_WINDOWS

#if defined(HOST_UNIX)

#include <minipal/utils.h>

// MSVC directly defines intrinsics for __cpuid and __cpuidex matching the below signatures
// We define matching signatures for use on Unix platforms.
//
// IMPORTANT: Unlike MSVC, Unix does not explicitly zero ECX for __cpuid

#if !__has_builtin(__cpuid)
static void __cpuid(int cpuInfo[4], int function_id)
{
    // Based on the Clang implementation provided in cpuid.h:
    // https://github.com/llvm/llvm-project/blob/main/clang/lib/Headers/cpuid.h

    __asm("  cpuid\n" \
        : "=a"(cpuInfo[0]), "=b"(cpuInfo[1]), "=c"(cpuInfo[2]), "=d"(cpuInfo[3]) \
        : "0"(function_id)
        );
}
#else
void __cpuid(int cpuInfo[4], int function_id);
#endif

#if !__has_builtin(__cpuidex)
static void __cpuidex(int cpuInfo[4], int function_id, int subFunction_id)
{
    // Based on the Clang implementation provided in cpuid.h:
    // https://github.com/llvm/llvm-project/blob/main/clang/lib/Headers/cpuid.h

    __asm("  cpuid\n" \
        : "=a"(cpuInfo[0]), "=b"(cpuInfo[1]), "=c"(cpuInfo[2]), "=d"(cpuInfo[3]) \
        : "0"(function_id), "2"(subFunction_id)
        );
}
#else
void __cpuidex(int cpuInfo[4], int function_id, int subFunction_id);
#endif

#endif // HOST_UNIX
#endif // defined(HOST_X86) || defined(HOST_AMD64)

#endif
