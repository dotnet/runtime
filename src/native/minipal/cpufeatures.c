// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdint.h>
#include <stdbool.h>
#include <stddef.h>

#include "cpufeatures.h"
#include "cpuid.h"

#if HOST_WINDOWS

#include <Windows.h>

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

#endif

#if HAVE_SYSCTLBYNAME
#include <sys/sysctl.h>
#endif

#endif // !HOST_WINDOWS

#if defined(HOST_UNIX)
#if defined(HOST_X86) || defined(HOST_AMD64)

static uint32_t xmmYmmStateSupport()
{
    uint32_t eax;
    __asm("  xgetbv\n" \
        : "=a"(eax) /*output in eax*/\
        : "c"(0) /*inputs - 0 in ecx*/\
        : "edx" /* registers that are clobbered*/
      );
    // check OS has enabled both XMM and YMM state support
    return ((eax & 0x06) == 0x06) ? 1 : 0;
}

#ifndef XSTATE_MASK_AVX512
#define XSTATE_MASK_AVX512 (0xE0) /* 0b1110_0000 */
#endif // XSTATE_MASK_AVX512

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

static bool IsAvxEnabled()
{
    return true;
}

static bool IsAvx512Enabled()
{
    return true;
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

    if (maxCpuId >= 1)
    {
        __cpuid(cpuidInfo, 0x00000001);

        const int requiredBaselineEdxFlags = (1 << 25)                                                                  // SSE
                                           | (1 << 26);                                                                 // SSE2

        if ((cpuidInfo[CPUID_EDX] & requiredBaselineEdxFlags) == requiredBaselineEdxFlags)
        {
            result |= XArchIntrinsicConstants_VectorT128;

            if ((cpuidInfo[CPUID_ECX] & (1 << 25)) != 0)                                                                // AESNI
            {
                result |= XArchIntrinsicConstants_Aes;
            }

            if ((cpuidInfo[CPUID_ECX] & (1 << 1)) != 0)                                                                 // PCLMULQDQ
            {
                result |= XArchIntrinsicConstants_Pclmulqdq;
            }

            if ((cpuidInfo[CPUID_ECX] & (1 << 0)) != 0)                                                                 // SSE3
            {
                result |= XArchIntrinsicConstants_Sse3;

                if ((cpuidInfo[CPUID_ECX] & (1 << 9)) != 0)                                                             // SSSE3
                {
                    result |= XArchIntrinsicConstants_Ssse3;

                    if ((cpuidInfo[CPUID_ECX] & (1 << 19)) != 0)                                                        // SSE4.1
                    {
                        result |= XArchIntrinsicConstants_Sse41;

                        if ((cpuidInfo[CPUID_ECX] & (1 << 20)) != 0)                                                    // SSE4.2
                        {
                            result |= XArchIntrinsicConstants_Sse42;

                            if ((cpuidInfo[CPUID_ECX] & (1 << 22)) != 0)                                                // MOVBE
                            {
                                result |= XArchIntrinsicConstants_Movbe;
                            }

                            if ((cpuidInfo[CPUID_ECX] & (1 << 23)) != 0)                                                // POPCNT
                            {
                                result |= XArchIntrinsicConstants_Popcnt;
                            }

                            const int requiredAvxEcxFlags = (1 << 27)                                                   // OSXSAVE
                                                          | (1 << 28);                                                  // AVX

                            if ((cpuidInfo[CPUID_ECX] & requiredAvxEcxFlags) == requiredAvxEcxFlags)
                            {
                                if (IsAvxEnabled() && (xmmYmmStateSupport() == 1))                                   // XGETBV == 11
                                {
                                    result |= XArchIntrinsicConstants_Avx;

                                    if ((cpuidInfo[CPUID_ECX] & (1 << 12)) != 0)                                        // FMA
                                    {
                                        result |= XArchIntrinsicConstants_Fma;
                                    }

                                    if (maxCpuId >= 0x07)
                                    {
                                        __cpuidex(cpuidInfo, 0x00000007, 0x00000000);

                                        if ((cpuidInfo[CPUID_EBX] & (1 << 5)) != 0)                                     // AVX2
                                        {
                                            result |= XArchIntrinsicConstants_Avx2;
                                            result |= XArchIntrinsicConstants_VectorT256;

                                            if (IsAvx512Enabled() && (avx512StateSupport() == 1))                    // XGETBV XRC0[7:5] == 111
                                            {
                                                if ((cpuidInfo[CPUID_EBX] & (1 << 16)) != 0)                            // AVX512F
                                                {
                                                    result |= XArchIntrinsicConstants_Avx512f;
                                                    result |= XArchIntrinsicConstants_VectorT512;

                                                    bool isAVX512_VLSupported = false;
                                                    if ((cpuidInfo[CPUID_EBX] & (1 << 31)) != 0)                        // AVX512VL
                                                    {
                                                        result |= XArchIntrinsicConstants_Avx512f_vl;
                                                        isAVX512_VLSupported = true;
                                                    }

                                                    if ((cpuidInfo[CPUID_EBX] & (1 << 30)) != 0)                        // AVX512BW
                                                    {
                                                        result |= XArchIntrinsicConstants_Avx512bw;
                                                        if (isAVX512_VLSupported)                                       // AVX512BW_VL
                                                        {
                                                            result |= XArchIntrinsicConstants_Avx512bw_vl;
                                                        }
                                                    }

                                                    if ((cpuidInfo[CPUID_EBX] & (1 << 28)) != 0)                        // AVX512CD
                                                    {
                                                        result |= XArchIntrinsicConstants_Avx512cd;
                                                        if (isAVX512_VLSupported)                                       // AVX512CD_VL
                                                        {
                                                            result |= XArchIntrinsicConstants_Avx512cd_vl;
                                                        }
                                                    }

                                                    if ((cpuidInfo[CPUID_EBX] & (1 << 17)) != 0)                        // AVX512DQ
                                                    {
                                                        result |= XArchIntrinsicConstants_Avx512dq;
                                                        if (isAVX512_VLSupported)                                       // AVX512DQ_VL
                                                        {
                                                            result |= XArchIntrinsicConstants_Avx512dq_vl;
                                                        }
                                                    }

                                                    if ((cpuidInfo[CPUID_ECX] & (1 << 1)) != 0)                         // AVX512VBMI
                                                    {
                                                        result |= XArchIntrinsicConstants_Avx512Vbmi;
                                                        if (isAVX512_VLSupported)                                       // AVX512VBMI_VL
                                                        {
                                                            result |= XArchIntrinsicConstants_Avx512Vbmi_vl;
                                                        }
                                                    }
                                                }
                                            }

                                            __cpuidex(cpuidInfo, 0x00000007, 0x00000001);

                                            if ((cpuidInfo[CPUID_EAX] & (1 << 4)) != 0)                                 // AVX-VNNI
                                            {
                                                result |= XArchIntrinsicConstants_AvxVnni;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        if (maxCpuId >= 0x07)
        {
            __cpuidex(cpuidInfo, 0x00000007, 0x00000000);

            if ((cpuidInfo[CPUID_EBX] & (1 << 3)) != 0)                                                           // BMI1
            {
                result |= XArchIntrinsicConstants_Bmi1;
            }

            if ((cpuidInfo[CPUID_EBX] & (1 << 8)) != 0)                                                           // BMI2
            {
                result |= XArchIntrinsicConstants_Bmi2;
            }

            if ((cpuidInfo[CPUID_EDX] & (1 << 14)) != 0)
            {
                result |= XArchIntrinsicConstants_Serialize;                                               // SERIALIZE
            }
        }
    }

    __cpuid(cpuidInfo, 0x80000000);
    uint32_t maxCpuIdEx = (uint32_t)cpuidInfo[CPUID_EAX];

    if (maxCpuIdEx >= 0x80000001)
    {
        __cpuid(cpuidInfo, 0x80000001);

        if ((cpuidInfo[CPUID_ECX] & (1 << 5)) != 0)                                                               // LZCNT
        {
            result |= XArchIntrinsicConstants_Lzcnt;
        }

    }
#endif // HOST_X86 || HOST_AMD64

#if defined(HOST_ARM64)
#if defined(HOST_UNIX)

#if HAVE_AUXV_HWCAP_H
    unsigned long hwCap = getauxval(AT_HWCAP);

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

    if (hwCap & HWCAP_ASIMD)
        result |= ARM64IntrinsicConstants_AdvSimd | ARM64IntrinsicConstants_VectorT128;

    if (hwCap & HWCAP_ASIMDRDM)
        result |= ARM64IntrinsicConstants_Rdm;

    if (hwCap & HWCAP_SVE)
        result |= ARM64IntrinsicConstants_Sve;

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

    // Every ARM64 CPU should support SIMD and FP
    // If the OS have no function to query for CPU capabilities we set just these

    result |= ARM64IntrinsicConstants_AdvSimd | ARM64IntrinsicConstants_VectorT128;
#endif // HAVE_AUXV_HWCAP_H
#endif // HOST_UNIX

#if defined(HOST_WINDOWS)
    // FP and SIMD support are enabled by default
    result |= ARM64IntrinsicConstants_AdvSimd | ARM64IntrinsicConstants_VectorT128;

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
    }

    if (IsProcessorFeaturePresent(PF_ARM_V83_LRCPC_INSTRUCTIONS_AVAILABLE))
    {
        result |= ARM64IntrinsicConstants_Rcpc;
    }

    // TODO: IsProcessorFeaturePresent doesn't support LRCPC2 yet.

#endif // HOST_WINDOWS

#endif // HOST_ARM64

    return result;
}
