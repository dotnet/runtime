// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#define STRINGIFY(L) #L
#define MAKESTRING(M, L) M(L)
#define STRINGIZE(X) MAKESTRING(STRINGIFY, X)

#if defined(__clang__)
#define BUILD_COMPILER                                                                                                 \
    "Clang " STRINGIZE(__clang_major__) "." STRINGIZE(__clang_minor__) "." STRINGIZE(__clang_patchlevel__)
#elif defined(_MSC_VER)
#define BUILD_COMPILER "MSVC " STRINGIZE(_MSC_FULL_VER)
#elif defined(__GNUC__)
#define BUILD_COMPILER "GCC " STRINGIZE(__GNUC__) "." STRINGIZE(__GNUC_MINOR__) "." STRINGIZE(__GNUC_PATCHLEVEL__)
#else
#define BUILD_COMPILER "Unknown"
#endif

#if defined(TARGET_X86)
#define TARGET_ARCH_STRING "x86"
#elif defined(TARGET_AMD64)
#define TARGET_ARCH_STRING "x64"
#elif defined(TARGET_ARM)
#define TARGET_ARCH_STRING "arm32"
#elif defined(TARGET_ARM64)
#define TARGET_ARCH_STRING "arm64"
#elif defined(TARGET_LOONGARCH64)
#define TARGET_ARCH_STRING "loongarch64"
#else
#define TARGET_ARCH_STRING "Unknown"
#endif

#if defined(UNIX_AMD64_ABI) || defined(UNIX_X86_ABI)
#define TARGET_OS_STRING "unix"
#elif defined(WINDOWS_AMD64_ABI) || defined(TARGET_X86)
#define TARGET_OS_STRING "win"
#else
#define TARGET_OS_STRING "universal"
#endif

#ifndef DLLEXPORT
#define DLLEXPORT
#endif

// This string is used by superpmi.py when measuring throughput impact of
// changes to validate that the baseline and diff JITs are comparable.
extern "C" DLLEXPORT const char jitBuildString[] =
    "RyuJIT built by " BUILD_COMPILER " targeting " TARGET_OS_STRING "-" TARGET_ARCH_STRING;
