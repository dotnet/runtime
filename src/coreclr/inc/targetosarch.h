// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef targetosarch_h
#define targetosarch_h

class TargetOS
{
public:
#ifdef TARGET_WINDOWS
#define TARGET_WINDOWS_POSSIBLY_SUPPORTED
    static const bool IsWindows = true;
    static const bool IsUnix = false;
    static const bool IsMacOS = false;
#elif defined(TARGET_UNIX)
#define TARGET_UNIX_POSSIBLY_SUPPORTED
    static const bool IsWindows = false;
    static const bool IsUnix = true;
#if defined(TARGET_OSX)
    static const bool IsMacOS = true;
#else
    static const bool IsMacOS = false;
#endif
#else
#define TARGET_WINDOWS_POSSIBLY_SUPPORTED
#define TARGET_UNIX_POSSIBLY_SUPPORTED
#define TARGET_OS_RUNTIMEDETERMINED
    static bool OSSettingConfigured;
    static bool IsWindows;
    static bool IsUnix;
    static bool IsMacOS;
#endif
};

class TargetArchitecture
{
public:
#ifdef TARGET_ARM
    static const bool IsX86 = false;
    static const bool IsX64 = false;
    static const bool IsArm64 = false;
    static const bool IsArm32 = true;
    static const bool IsArmArch = true;
    static const bool IsLoongArch64 = false;
    static const bool IsRiscv64 = false;
#elif defined(TARGET_ARM64)
    static const bool IsX86 = false;
    static const bool IsX64 = false;
    static const bool IsArm64 = true;
    static const bool IsArm32 = false;
    static const bool IsArmArch = true;
    static const bool IsLoongArch64 = false;
    static const bool IsRiscv64 = false;
#elif defined(TARGET_AMD64)
    static const bool IsX86 = false;
    static const bool IsX64 = true;
    static const bool IsArm64 = false;
    static const bool IsArm32 = false;
    static const bool IsArmArch = false;
    static const bool IsLoongArch64 = false;
    static const bool IsRiscv64 = false;
#elif defined(TARGET_X86)
    static const bool IsX86 = true;
    static const bool IsX64 = false;
    static const bool IsArm64 = false;
    static const bool IsArm32 = false;
    static const bool IsArmArch = false;
    static const bool IsLoongArch64 = false;
    static const bool IsRiscv64 = false;
#elif defined(TARGET_LOONGARCH64)
    static const bool IsX86 = false;
    static const bool IsX64 = false;
    static const bool IsArm64 = false;
    static const bool IsArm32 = false;
    static const bool IsArmArch = false;
    static const bool IsLoongArch64 = true;
    static const bool IsRiscv64 = false;
#elif defined(TARGET_RISCV64)
    static const bool IsX86 = false;
    static const bool IsX64 = false;
    static const bool IsArm64 = false;
    static const bool IsArm32 = false;
    static const bool IsArmArch = false;
    static const bool IsLoongArch64 = false;
    static const bool IsRiscv64 = true;
#else
#error Unknown architecture
#endif
};

#endif // targetosarch_h
