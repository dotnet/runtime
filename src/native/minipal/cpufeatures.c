// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdint.h>
#include <stdbool.h>
#include <stddef.h>
#include <stdio.h>
#include <string.h>
#include <assert.h>

#include "cpufeatures.h"
#include "cpuid.h"

#if HOST_WINDOWS

#include <Windows.h>

#ifndef PF_ARM_SVE_INSTRUCTIONS_AVAILABLE
#define PF_ARM_SVE_INSTRUCTIONS_AVAILABLE (46)
#endif

#ifndef PF_ARM_SVE2_INSTRUCTIONS_AVAILABLE
#define PF_ARM_SVE2_INSTRUCTIONS_AVAILABLE (47)
#endif

#else // HOST_WINDOWS

#include "minipalconfig.h"

#if HAVE_AUXV_HWCAP_H

#include <sys/auxv.h>
#include <asm/hwcap.h>

// Light-up for hardware capabilities that are not present in older headers used by the portable build.
#ifndef HWCAP_ASIMDRDM
#define HWCAP_ASIMDRDM  (1 << 12)
#endif
#ifndef HWCAP_LRCPC
#define HWCAP_LRCPC     (1 << 15)
#endif
#ifndef HWCAP_ILRCPC
#define HWCAP_ILRCPC    (1 << 26)
#endif
#ifndef HWCAP_ASIMDDP
#define HWCAP_ASIMDDP   (1 << 20)
#endif
#ifndef HWCAP_SVE
#define HWCAP_SVE   (1 << 22)
#endif
#ifndef HWCAP2_SVE2
#define HWCAP2_SVE2   (1 << 1)
#endif

#endif

#if HAVE_SYSCTLBYNAME
#include <sys/sysctl.h>
#endif

#if HAVE_HWPROBE_H

#include <asm/hwprobe.h>
#include <asm/unistd.h>
#include <unistd.h>

#endif // HAVE_HWPROBE_H

#endif // !HOST_WINDOWS

#if defined(HOST_UNIX)
#if defined(HOST_X86) || defined(HOST_AMD64)

static uint32_t xmmYmmStateSupport()
{
#if defined(HOST_X86)
    // We don't support saving XState context on linux-x86 platforms yet, so we
    // need to disable any AVX support that uses the extended registers.
    return 0;
#else
    uint32_t eax;
    __asm("  xgetbv\n" \
        : "=a"(eax) /*output in eax*/\
        : "c"(0) /*inputs - 0 in ecx*/\
        : "edx" /* registers that are clobbered*/
      );
    // check OS has enabled both XMM and YMM state support
    return ((eax & 0x06) == 0x06) ? 1 : 0;
#endif // HOST_X86
}

#ifndef XSTATE_MASK_AVX512
#define XSTATE_MASK_AVX512 (0xE0) /* 0b1110_0000 */
#endif // XSTATE_MASK_AVX512

#ifndef XSTATE_MASK_APX
#define XSTATE_MASK_APX (0x80000)
#endif // XSTATE_MASK_APX

static uint32_t avx512StateSupport()
{
#if defined(HOST_APPLE)
    // MacOS has specialized behavior where it reports AVX512 support but doesnt
    // actually enable AVX512 until the first instruction is executed and does so
    // on a per thread basis. It does this by catching the faulting instruction and
    // checking for the EVEX encoding. The kmov instructions, despite being part
    // of the AVX512 instruction set are VEX encoded and dont trigger the enablement
    //
    // See https://github.com/apple/darwin-xnu/blob/main/osfmk/i386/fpu.c#L174

    // TODO-AVX512: Enabling this for OSX requires ensuring threads explicitly trigger
    // the AVX-512 enablement so that arbitrary usage doesn't cause downstream problems

    return false;
#else
    uint32_t eax;
    __asm("  xgetbv\n" \
        : "=a"(eax) /*output in eax*/\
        : "c"(0) /*inputs - 0 in ecx*/\
        : "edx" /* registers that are clobbered*/
      );
    // check OS has enabled XMM, YMM and ZMM state support
    return ((eax & 0xE6) == 0x0E6) ? 1 : 0;
#endif
}

static uint32_t apxStateSupport()
{
#if defined(HOST_APPLE)
    return 0;
#elif defined(TARGET_X86)
    return 0;
#else
    uint32_t eax;
    __asm("  xgetbv\n" \
        : "=a"(eax) /*output in eax*/\
        : "c"(0) /*inputs - 0 in ecx*/\
        : "edx" /* registers that are clobbered*/
      );
    return ((eax & 0x80000) == 0x80000) ? 1 : 0;
#endif  // TARGET_AMD64
}

static bool IsAvxEnabled()
{
    return true;
}

static bool IsAvx512Enabled()
{
    return true;
}

static bool IsApxEnabled()
{
#if defined(TARGET_X86)
    return false;
#else
    return true;
#endif  // TARGET_AMD64
}
#endif // defined(HOST_X86) || defined(HOST_AMD64)
#endif // HOST_UNIX

#if defined(HOST_WINDOWS)
#if defined(HOST_X86) || defined(HOST_AMD64)
static uint32_t xmmYmmStateSupport()
{
    // check OS has enabled both XMM and YMM state support
    return ((_xgetbv(0) & 0x06) == 0x06) ? 1 : 0;
}

static uint32_t avx512StateSupport()
{
    // check OS has enabled XMM, YMM and ZMM state support
    return ((_xgetbv(0) & 0xE6) == 0x0E6) ? 1 : 0;
}

static uint32_t apxStateSupport()
{
#if defined(TARGET_X86)
    return 0;
#else
    return ((_xgetbv(0) & 0x80000) == 0x80000) ? 1 : 0;
#endif
}

static bool IsAvxEnabled()
{
    DWORD64 FeatureMask = GetEnabledXStateFeatures();
    return ((FeatureMask & XSTATE_MASK_AVX) != 0);
}

static bool IsAvx512Enabled()
{
    DWORD64 FeatureMask = GetEnabledXStateFeatures();
    return ((FeatureMask & XSTATE_MASK_AVX512) != 0);
}

// TODO-XArch-APX:
// we will eventually need to remove this macro when windows officially supports APX.
#ifndef XSTATE_MASK_APX
#define XSTATE_MASK_APX (0x80000)
#endif // XSTATE_MASK_APX

static bool IsApxEnabled()
{
#ifdef TARGET_X86
    return false;
#else
    DWORD64 FeatureMask = GetEnabledXStateFeatures();
    return ((FeatureMask & XSTATE_MASK_APX) != 0);
#endif
}

#endif // defined(HOST_X86) || defined(HOST_AMD64)
#endif // HOST_WINDOWS

int minipal_getcpufeatures(void)
{
    int result = 0;

#if defined(HOST_X86) || defined(HOST_AMD64)

    int cpuidInfo[4];

    const int CPUID_EAX = 0;
    const int CPUID_EBX = 1;
    const int CPUID_ECX = 2;
    const int CPUID_EDX = 3;

    __cpuid(cpuidInfo, 0x00000000);
    uint32_t maxCpuId = (uint32_t)cpuidInfo[CPUID_EAX];
    assert(maxCpuId >= 1);

    __cpuid(cpuidInfo, 0x00000001);

    bool hasAvx2Dependencies = false;
    bool hasAvx10v1Dependencies = false;

    assert((cpuidInfo[CPUID_EDX] & (1 << 25)) != 0);                                                            // SSE
    assert((cpuidInfo[CPUID_EDX] & (1 << 26)) != 0);                                                            // SSE2

    if (((cpuidInfo[CPUID_ECX] & (1 << 25)) != 0) &&                                                            // AESNI
        ((cpuidInfo[CPUID_ECX] & (1 << 1)) != 0))                                                               // PCLMULQDQ
    {
        result |= XArchIntrinsicConstants_Aes;
    }

    if (((cpuidInfo[CPUID_ECX] & (1 << 0)) != 0) &&                                                             // SSE3
        ((cpuidInfo[CPUID_ECX] & (1 << 9)) != 0) &&                                                             // SSSE3
        ((cpuidInfo[CPUID_ECX] & (1 << 19)) != 0) &&                                                            // SSE4.1
        ((cpuidInfo[CPUID_ECX] & (1 << 20)) != 0) &&                                                            // SSE4.2
        ((cpuidInfo[CPUID_ECX] & (1 << 23)) != 0))                                                              // POPCNT
    {
        result |= XArchIntrinsicConstants_Sse42;

        if (((cpuidInfo[CPUID_ECX] & (1 << 27)) != 0) &&                                                        // OSXSAVE
            ((cpuidInfo[CPUID_ECX] & (1 << 28)) != 0))                                                          // AVX
        {
            if (IsAvxEnabled() && (xmmYmmStateSupport() == 1))                                                  // XGETBV == 11
            {
                result |= XArchIntrinsicConstants_Avx;

                if (((cpuidInfo[CPUID_ECX] & (1 << 29)) != 0) &&                                                // F16C
                    ((cpuidInfo[CPUID_ECX] & (1 << 12)) != 0) &&                                                // FMA
                    ((cpuidInfo[CPUID_ECX] & (1 << 22)) != 0))                                                  // MOVBE
                {
                    hasAvx2Dependencies = true;
                }
            }
        }
    }

    __cpuid(cpuidInfo, 0x80000000);
    uint32_t maxCpuIdEx = (uint32_t)cpuidInfo[CPUID_EAX];

    if (maxCpuIdEx >= 0x80000001)
    {
        __cpuid(cpuidInfo, 0x80000001);

        if (hasAvx2Dependencies)
        {
            if ((cpuidInfo[CPUID_ECX] & (1 << 5)) == 0)                                                         // LZCNT
            {
                hasAvx2Dependencies = false;
            }
        }
    }
    else
    {
        hasAvx2Dependencies = false;
    }

    if (maxCpuId >= 0x07)
    {
        __cpuidex(cpuidInfo, 0x00000007, 0x00000000);

        if ((result & XArchIntrinsicConstants_Avx) != 0)
        {
            if (((cpuidInfo[CPUID_EBX] & (1 << 5)) != 0) &&                                                     // AVX2
                ((cpuidInfo[CPUID_EBX] & (1 << 3)) != 0) &&                                                     // BMI1
                ((cpuidInfo[CPUID_EBX] & (1 << 8)) != 0) &&                                                     // BMI2
                hasAvx2Dependencies)                                                                            // F16C, FMA, LZCNT, MOVBE
            {
                result |= XArchIntrinsicConstants_Avx2;

                if (((cpuidInfo[CPUID_EBX] & (1 << 16)) != 0) &&                                                // AVX512F
                    ((cpuidInfo[CPUID_EBX] & (1 << 30)) != 0) &&                                                // AVX512BW
                    ((cpuidInfo[CPUID_EBX] & (1 << 28)) != 0) &&                                                // AVX512CD
                    ((cpuidInfo[CPUID_EBX] & (1 << 17)) != 0) &&                                                // AVX512DQ
                    ((cpuidInfo[CPUID_EBX] & (1 << 31)) != 0))                                                  // AVX512VL
                {
                    if (IsAvx512Enabled() && (avx512StateSupport() == 1))                                       // XGETBV XRC0[7:5] == 111
                    {
                        result |= XArchIntrinsicConstants_Avx512;

                        if (((cpuidInfo[CPUID_EBX] & (1 << 21)) != 0) &&                                        // AVX512-IFMA
                            ((cpuidInfo[CPUID_ECX] & (1 << 1)) != 0))                                           // AVX512-VBMI
                        {
                            result |= XArchIntrinsicConstants_Avx512v2;

                            if (((cpuidInfo[CPUID_ECX] & (1 << 12)) != 0) &&                                    // AVX512-BITALG
                                ((cpuidInfo[CPUID_ECX] & (1 << 6)) != 0) &&                                     // AVX512-VBMI2
                                ((cpuidInfo[CPUID_ECX] & (1 << 11)) != 0) &&                                    // AVX512-VNNI
                                ((cpuidInfo[CPUID_ECX] & (1 << 14)) != 0))                                      // AVX512-VPOPCNTDQ
                            {
                                result |= XArchIntrinsicConstants_Avx512v3;

                                if ((cpuidInfo[CPUID_EDX] & (1 << 23)) != 0)                                    // AVX512-FP16
                                {
                                    hasAvx10v1Dependencies = true;
                                }
                            }
                        }

                        if ((cpuidInfo[CPUID_EDX] & (1 << 8)) != 0)                                             // AVX512-VP2INTERSECT
                        {
                            result |= XArchIntrinsicConstants_Avx512Vp2intersect;
                        }
                    }
                }
            }
            else
            {
                hasAvx2Dependencies = false;
            }

            if ((result & XArchIntrinsicConstants_Aes) != 0)
            {
                if (((cpuidInfo[CPUID_ECX] & (1 << 9)) != 0) &&                                                 // VAES
                    ((cpuidInfo[CPUID_ECX] & (1 << 10)) != 0))                                                  // VPCLMULQDQ
                {
                    result |= XArchIntrinsicConstants_Vaes;
                }
            }
        }

        if ((cpuidInfo[CPUID_ECX] & (1 << 8)) != 0)                                                             // GFNI
        {
            result |= XArchIntrinsicConstants_Gfni;
        }

        if ((cpuidInfo[CPUID_EBX] & (1 << 29)) != 0)                                                            // SHA
        {
            result |= XArchIntrinsicConstants_Sha;
        }

        if ((cpuidInfo[CPUID_ECX] & (1 << 5)) != 0)                                                             // WAITPKG
        {
            result |= XArchIntrinsicConstants_WaitPkg;
        }

        if ((cpuidInfo[CPUID_EDX] & (1 << 14)) != 0)                                                            // SERIALIZE
        {
            result |= XArchIntrinsicConstants_X86Serialize;
        }

        __cpuidex(cpuidInfo, 0x00000007, 0x00000001);

        if ((result & XArchIntrinsicConstants_Avx2) != 0)
        {
            if ((cpuidInfo[CPUID_EAX] & (1 << 4)) != 0)                                                         // AVX-VNNI
            {
                result |= XArchIntrinsicConstants_AvxVnni;
            }

            if ((cpuidInfo[CPUID_EAX] & (1 << 23)) != 0)                                                        // AVX-IFMA
            {
                result |= XArchIntrinsicConstants_AvxIfma;
            }

            if (hasAvx10v1Dependencies)
            {
                if ((cpuidInfo[CPUID_EAX] & (1 << 5)) == 0)                                                     // AVX512-BF16
                {
                    hasAvx10v1Dependencies = false;
                }
            }

            if (IsApxEnabled() && apxStateSupport())
            {
                if ((cpuidInfo[CPUID_EDX] & (1 << 21)) != 0)                                                    // Apx
                {
                    result |= XArchIntrinsicConstants_Apx;
                }
            }
        }

        if (maxCpuId >= 0x24)
        {
            if ((cpuidInfo[CPUID_EDX] & (1 << 19)) != 0)                                                        // Avx10
            {
                // While AVX10 was originally spec'd to allow no V512 support
                // this was later changed and all implementations must provide
                // V512 support

                __cpuidex(cpuidInfo, 0x00000024, 0x00000000);

                if (((cpuidInfo[CPUID_EBX] & (1 << 16)) != 0) &&                                                // Avx10/V128
                    ((cpuidInfo[CPUID_EBX] & (1 << 17)) != 0) &&                                                // Avx10/V256
                    ((cpuidInfo[CPUID_EBX] & (1 << 18)) != 0) &&                                                // Avx10/V512
                    hasAvx10v1Dependencies)                                                                     // AVX512-BF16, AVX512-FP16
                {
                    uint8_t avx10Version = (uint8_t)(cpuidInfo[CPUID_EBX] & 0xFF);

                    if (avx10Version >= 1)                                                                      // Avx10.1
                    {
                        result |= XArchIntrinsicConstants_Avx10v1;
                    }

                    if (avx10Version >= 2)                                                                      // Avx10.2
                    {
                        result |= XArchIntrinsicConstants_Avx10v2;
                    }
                }
                else
                {
                    hasAvx10v1Dependencies = false;
                }
            }
        }
    }
#endif // HOST_X86 || HOST_AMD64

#if defined(HOST_ARM64)
#if defined(HOST_UNIX)

#if HAVE_AUXV_HWCAP_H
    unsigned long hwCap = getauxval(AT_HWCAP);

    assert(hwCap & HWCAP_ASIMD);

    if (hwCap & HWCAP_AES)
        result |= ARM64IntrinsicConstants_Aes;

    if (hwCap & HWCAP_ATOMICS)
        result |= ARM64IntrinsicConstants_Atomics;

    if (hwCap & HWCAP_CRC32)
        result |= ARM64IntrinsicConstants_Crc32;

    if (hwCap & HWCAP_ASIMDDP)
        result |= ARM64IntrinsicConstants_Dp;

    if (hwCap & HWCAP_LRCPC)
        result |= ARM64IntrinsicConstants_Rcpc;

    if (hwCap & HWCAP_ILRCPC)
        result |= ARM64IntrinsicConstants_Rcpc2;

    if (hwCap & HWCAP_SHA1)
        result |= ARM64IntrinsicConstants_Sha1;

    if (hwCap & HWCAP_SHA2)
        result |= ARM64IntrinsicConstants_Sha256;

    if (hwCap & HWCAP_ASIMDRDM)
        result |= ARM64IntrinsicConstants_Rdm;

    if (hwCap & HWCAP_SVE)
        result |= ARM64IntrinsicConstants_Sve;

    unsigned long hwCap2 = getauxval(AT_HWCAP2);

    if (hwCap2 & HWCAP2_SVE2)
        result |= ARM64IntrinsicConstants_Sve2;

#else // !HAVE_AUXV_HWCAP_H

#if HAVE_SYSCTLBYNAME
    int64_t valueFromSysctl = 0;
    size_t sz = sizeof(valueFromSysctl);

    if ((sysctlbyname("hw.optional.arm.FEAT_AES", &valueFromSysctl, &sz, NULL, 0) == 0) && (valueFromSysctl != 0))
        result |= ARM64IntrinsicConstants_Aes;

    if ((sysctlbyname("hw.optional.armv8_crc32", &valueFromSysctl, &sz, NULL, 0) == 0) && (valueFromSysctl != 0))
        result |= ARM64IntrinsicConstants_Crc32;

    if ((sysctlbyname("hw.optional.arm.FEAT_DotProd", &valueFromSysctl, &sz, NULL, 0) == 0) && (valueFromSysctl != 0))
        result |= ARM64IntrinsicConstants_Dp;

    if ((sysctlbyname("hw.optional.arm.FEAT_RDM", &valueFromSysctl, &sz, NULL, 0) == 0) && (valueFromSysctl != 0))
        result |= ARM64IntrinsicConstants_Rdm;

    if ((sysctlbyname("hw.optional.arm.FEAT_SHA1", &valueFromSysctl, &sz, NULL, 0) == 0) && (valueFromSysctl != 0))
        result |= ARM64IntrinsicConstants_Sha1;

    if ((sysctlbyname("hw.optional.arm.FEAT_SHA256", &valueFromSysctl, &sz, NULL, 0) == 0) && (valueFromSysctl != 0))
        result |= ARM64IntrinsicConstants_Sha256;

    if ((sysctlbyname("hw.optional.armv8_1_atomics", &valueFromSysctl, &sz, NULL, 0) == 0) && (valueFromSysctl != 0))
        result |= ARM64IntrinsicConstants_Atomics;

    if ((sysctlbyname("hw.optional.arm.FEAT_LRCPC", &valueFromSysctl, &sz, NULL, 0) == 0) && (valueFromSysctl != 0))
        result |= ARM64IntrinsicConstants_Rcpc;

    if ((sysctlbyname("hw.optional.arm.FEAT_LRCPC2", &valueFromSysctl, &sz, NULL, 0) == 0) && (valueFromSysctl != 0))
        result |= ARM64IntrinsicConstants_Rcpc2;
#endif // HAVE_SYSCTLBYNAME
#endif // HAVE_AUXV_HWCAP_H
#endif // HOST_UNIX

#if defined(HOST_WINDOWS)
    if (IsProcessorFeaturePresent(PF_ARM_V8_CRYPTO_INSTRUCTIONS_AVAILABLE))
    {
        result |= ARM64IntrinsicConstants_Aes;
        result |= ARM64IntrinsicConstants_Sha1;
        result |= ARM64IntrinsicConstants_Sha256;
    }

    if (IsProcessorFeaturePresent(PF_ARM_V8_CRC32_INSTRUCTIONS_AVAILABLE))
    {
        result |= ARM64IntrinsicConstants_Crc32;
    }

    if (IsProcessorFeaturePresent(PF_ARM_V81_ATOMIC_INSTRUCTIONS_AVAILABLE))
    {
        result |= ARM64IntrinsicConstants_Atomics;
    }

    if (IsProcessorFeaturePresent(PF_ARM_V82_DP_INSTRUCTIONS_AVAILABLE))
    {
        result |= ARM64IntrinsicConstants_Dp;

        // IsProcessorFeaturePresent does not have a dedicated flag for RDM, so we enable it by implication.
        // 1) DP is an optional instruction set for Armv8.2, which may be included only in processors implementing at least Armv8.1.
        // 2) Armv8.1 requires RDM when AdvSIMD is implemented, and AdvSIMD is a baseline requirement of .NET.
        //
        // Therefore, by documented standard, DP cannot exist here without RDM. In practice, there is only one CPU supported
        // by Windows that includes RDM without DP, so this implication also has little practical chance of a false negative.
        //
        // See: https://developer.arm.com/-/media/Arm%20Developer%20Community/PDF/Learn%20the%20Architecture/Understanding%20the%20Armv8.x%20extensions.pdf
        //      https://developer.arm.com/documentation/109697/2024_09/Feature-descriptions/The-Armv8-1-architecture-extension
        result |= ARM64IntrinsicConstants_Rdm;
    }

    if (IsProcessorFeaturePresent(PF_ARM_V83_LRCPC_INSTRUCTIONS_AVAILABLE))
    {
        result |= ARM64IntrinsicConstants_Rcpc;
    }

    // TODO: IsProcessorFeaturePresent doesn't support LRCPC2 yet.

    if (IsProcessorFeaturePresent(PF_ARM_SVE_INSTRUCTIONS_AVAILABLE))
    {
        result |= ARM64IntrinsicConstants_Sve;
    }

    if (IsProcessorFeaturePresent(PF_ARM_SVE2_INSTRUCTIONS_AVAILABLE))
    {
        result |= ARM64IntrinsicConstants_Sve2;
    }

#endif // HOST_WINDOWS

#endif // HOST_ARM64

#if defined(HOST_RISCV64)

#if defined(HOST_UNIX)

#if HAVE_HWPROBE_H

    struct riscv_hwprobe pairs[1] = {{RISCV_HWPROBE_KEY_IMA_EXT_0, 0}};

    if (syscall(__NR_riscv_hwprobe, pairs, 1, 0, NULL, 0) == 0)
    {
        // Our baseline support is for RV64GC (see #73437)
        assert(pairs[0].value & RISCV_HWPROBE_IMA_FD);
        assert(pairs[0].value & RISCV_HWPROBE_IMA_C);

        if (pairs[0].value & RISCV_HWPROBE_EXT_ZBA)
        {
            result |= RiscV64IntrinsicConstants_Zba;
        }

        if (pairs[0].value & RISCV_HWPROBE_EXT_ZBB)
        {
            result |= RiscV64IntrinsicConstants_Zbb;
        }
    }

#endif // HAVE_HWPROBE_H

#endif // HOST_UNIX

#endif // HOST_RISCV64

    return result;
}

// Detect if the current process is running under the Apple Rosetta x64 emulator
bool minipal_detect_rosetta(void)
{
#if defined(HOST_AMD64) || defined(HOST_X86)
    // Check for CPU brand indicating emulation
    int regs[4];
    char brand[49];

    // Get the maximum value for extended function CPUID info
    __cpuid(regs, (int)0x80000000);
    if ((unsigned int)regs[0] < 0x80000004)
    {
        return false; // Extended CPUID not supported
    }

    // Retrieve the CPU brand string
    for (unsigned int i = 0x80000002; i <= 0x80000004; ++i)
    {
        __cpuid(regs, (int)i);
        memcpy(brand + (i - 0x80000002) * sizeof(regs), regs, sizeof(regs));
    }
    brand[sizeof(brand) - 1] = '\0';

    // Check if CPU brand indicates emulation
    if (strstr(brand, "VirtualApple") != NULL)
    {
        return true;
    }
#endif // HOST_AMD64 || HOST_X86

    return false;
}
