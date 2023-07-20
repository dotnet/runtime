// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdint.h>
#include <stdbool.h>
#include <stddef.h>

#include "cpufeatures.h"
#include "cpuid.h"

#if TARGET_WINDOWS

#include <Windows.h>

#else // TARGET_WINDOWS

#include "minipalconfig.h"

#if HAVE_AUXV_HWCAP_H
#include <sys/auxv.h>
#include <asm/hwcap.h>
#endif

#if HAVE_SYSCTLBYNAME
#include <sys/sysctl.h>
#endif

#endif // !TARGET_WINDOWS

#if defined(TARGET_UNIX)
#if defined(TARGET_X86) || defined(TARGET_AMD64)

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
#if defined(TARGET_APPLE)
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
#endif // defined(TARGET_X86) || defined(TARGET_AMD64)
#endif // TARGET_UNIX

#if defined(TARGET_WINDOWS)
#if defined(TARGET_X86) || defined(TARGET_AMD64)
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

static HMODULE LoadKernel32dll()
{
    return LoadLibraryExW(L"kernel32", NULL, LOAD_LIBRARY_SEARCH_SYSTEM32);
}

static bool IsAvxEnabled()
{
    typedef DWORD64(WINAPI* PGETENABLEDXSTATEFEATURES)();
    PGETENABLEDXSTATEFEATURES pfnGetEnabledXStateFeatures = NULL;

    HMODULE hMod = LoadKernel32dll();
    if (hMod == NULL)
        return FALSE;

    pfnGetEnabledXStateFeatures = (PGETENABLEDXSTATEFEATURES)GetProcAddress(hMod, "GetEnabledXStateFeatures");

    if (pfnGetEnabledXStateFeatures == NULL)
    {
        return FALSE;
    }

    DWORD64 FeatureMask = pfnGetEnabledXStateFeatures();
    if ((FeatureMask & XSTATE_MASK_AVX) == 0)
    {
        return FALSE;
    }

    return TRUE;
}

static bool IsAvx512Enabled()
{
    typedef DWORD64(WINAPI* PGETENABLEDXSTATEFEATURES)();
    PGETENABLEDXSTATEFEATURES pfnGetEnabledXStateFeatures = NULL;

    HMODULE hMod = LoadKernel32dll();
    if (hMod == NULL)
        return FALSE;

    pfnGetEnabledXStateFeatures = (PGETENABLEDXSTATEFEATURES)GetProcAddress(hMod, "GetEnabledXStateFeatures");

    if (pfnGetEnabledXStateFeatures == NULL)
    {
        return FALSE;
    }

    DWORD64 FeatureMask = pfnGetEnabledXStateFeatures();
    if ((FeatureMask & XSTATE_MASK_AVX512) == 0)
    {
        return FALSE;
    }

    return TRUE;
}
#endif // defined(TARGET_X86) || defined(TARGET_AMD64)
#endif // TARGET_WINDOWS

int minipal_getcpufeatures(void)
{
    int result = 0;

#if defined(TARGET_X86) || defined(TARGET_AMD64)

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
#endif // TARGET_X86 || TARGET_AMD64

#if defined(TARGET_ARM64)
#if defined(TARGET_UNIX)
    #if HAVE_AUXV_HWCAP_H
    unsigned long hwCap = getauxval(AT_HWCAP);

    // HWCAP_* flags are introduced by ARM into the Linux kernel as new extensions are published.
    // For a given kernel, some of these flags may not be present yet.
    // Use ifdef for each to allow for compilation with any vintage kernel.
    // From a single binary distribution perspective, compiling with latest kernel asm/hwcap.h should
    // include all published flags.  Given flags are merged to kernel and published before silicon is
    // available, using the latest kernel for release should be sufficient.

#ifdef HWCAP_AES
    if (hwCap & HWCAP_AES)
        result |= ARM64IntrinsicConstants_Aes;
#endif
#ifdef HWCAP_ATOMICS
    if (hwCap & HWCAP_ATOMICS)
        result |= ARM64IntrinsicConstants_Atomics;
#endif
#ifdef HWCAP_CRC32
    if (hwCap & HWCAP_CRC32)
        result |= ARM64IntrinsicConstants_Crc32;
#endif
#ifdef HWCAP_DCPOP
//    if (hwCap & HWCAP_DCPOP)
//        result |= ARM64IntrinsicConstants_???;
#endif
#ifdef HWCAP_ASIMDDP
    if (hwCap & HWCAP_ASIMDDP)
        result |= ARM64IntrinsicConstants_Dp;
#endif
#ifdef HWCAP_FCMA
//    if (hwCap & HWCAP_FCMA)
//        result |= ARM64IntrinsicConstants_???;
#endif
#ifdef HWCAP_FP
//    if (hwCap & HWCAP_FP)
//        result |= ARM64IntrinsicConstants_???;
#endif
#ifdef HWCAP_FPHP
//    if (hwCap & HWCAP_FPHP)
//        result |= ARM64IntrinsicConstants_???;
#endif
#ifdef HWCAP_JSCVT
//    if (hwCap & HWCAP_JSCVT)
//        result |= ARM64IntrinsicConstants_???;
#endif
#ifdef HWCAP_LRCPC
      if (hwCap & HWCAP_LRCPC)
          result |= ARM64IntrinsicConstants_Rcpc;
#endif
#ifdef HWCAP_PMULL
//    if (hwCap & HWCAP_PMULL)
//        result |= ARM64IntrinsicConstants_???;
#endif
#ifdef HWCAP_SHA1
    if (hwCap & HWCAP_SHA1)
        result |= ARM64IntrinsicConstants_Sha1;
#endif
#ifdef HWCAP_SHA2
    if (hwCap & HWCAP_SHA2)
        result |= ARM64IntrinsicConstants_Sha256;
#endif
#ifdef HWCAP_SHA512
//    if (hwCap & HWCAP_SHA512)
//        result |= ARM64IntrinsicConstants_???;
#endif
#ifdef HWCAP_SHA3
//    if (hwCap & HWCAP_SHA3)
//        result |= ARM64IntrinsicConstants_???;
#endif
#ifdef HWCAP_ASIMD
    if (hwCap & HWCAP_ASIMD)
        result |= ARM64IntrinsicConstants_AdvSimd | ARM64IntrinsicConstants_VectorT128;
#endif
#ifdef HWCAP_ASIMDRDM
    if (hwCap & HWCAP_ASIMDRDM)
        result |= ARM64IntrinsicConstants_Rdm;
#endif
#ifdef HWCAP_ASIMDHP
//    if (hwCap & HWCAP_ASIMDHP)
//        result |= ARM64IntrinsicConstants_???;
#endif
#ifdef HWCAP_SM3
//    if (hwCap & HWCAP_SM3)
//        result |= ARM64IntrinsicConstants_???;
#endif
#ifdef HWCAP_SM4
//    if (hwCap & HWCAP_SM4)
//        result |= ARM64IntrinsicConstants_???;
#endif
#ifdef HWCAP_SVE
//    if (hwCap & HWCAP_SVE)
//        result |= ARM64IntrinsicConstants_???;
#endif

#ifdef AT_HWCAP2
    unsigned long hwCap2 = getauxval(AT_HWCAP2);

#ifdef HWCAP2_DCPODP
//    if (hwCap2 & HWCAP2_DCPODP)
//        result |= ARM64IntrinsicConstants_???;
#endif
#ifdef HWCAP2_SVE2
//    if (hwCap2 & HWCAP2_SVE2)
//        result |= ARM64IntrinsicConstants_???;
#endif
#ifdef HWCAP2_SVEAES
//    if (hwCap2 & HWCAP2_SVEAES)
//        result |= ARM64IntrinsicConstants_???;
#endif
#ifdef HWCAP2_SVEPMULL
//    if (hwCap2 & HWCAP2_SVEPMULL)
//        result |= ARM64IntrinsicConstants_???;
#endif
#ifdef HWCAP2_SVEBITPERM
//    if (hwCap2 & HWCAP2_SVEBITPERM)
//        result |= ARM64IntrinsicConstants_???;
#endif
#ifdef HWCAP2_SVESHA3
//    if (hwCap2 & HWCAP2_SVESHA3)
//        result |= ARM64IntrinsicConstants_???;
#endif
#ifdef HWCAP2_SVESM4
//    if (hwCap2 & HWCAP2_SVESM4)
//        result |= ARM64IntrinsicConstants_???;
#endif
#ifdef HWCAP2_FLAGM2
//    if (hwCap2 & HWCAP2_FLAGM2)
//        result |= ARM64IntrinsicConstants_???;
#endif
#ifdef HWCAP2_FRINT
//    if (hwCap2 & HWCAP2_FRINT)
//        result |= ARM64IntrinsicConstants_???;
#endif

#endif // AT_HWCAP2

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
#endif // HAVE_SYSCTLBYNAME

    // Every ARM64 CPU should support SIMD and FP
    // If the OS have no function to query for CPU capabilities we set just these

    result |= ARM64IntrinsicConstants_AdvSimd | ARM64IntrinsicConstants_VectorT128;
#endif // HAVE_AUXV_HWCAP_H
#endif // TARGET_UNIX

#if defined(TARGET_WINDOWS)
// Older version of SDK would return false for these intrinsics
// but make sure we pass the right values to the APIs
#ifndef PF_ARM_V81_ATOMIC_INSTRUCTIONS_AVAILABLE
#define PF_ARM_V81_ATOMIC_INSTRUCTIONS_AVAILABLE 34
#endif
#ifndef PF_ARM_V82_DP_INSTRUCTIONS_AVAILABLE
#define PF_ARM_V82_DP_INSTRUCTIONS_AVAILABLE 43
#endif
#ifndef PF_ARM_V83_LRCPC_INSTRUCTIONS_AVAILABLE
#define PF_ARM_V83_LRCPC_INSTRUCTIONS_AVAILABLE 45
#endif

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
#endif // TARGET_WINDOWS

#endif // TARGET_ARM64

    return result;
}
