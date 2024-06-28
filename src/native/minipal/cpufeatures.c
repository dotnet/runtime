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

#ifdef HOST_WINDOWS

#include <Windows.h>

#ifndef PF_ARM_SVE_INSTRUCTIONS_AVAILABLE
#define PF_ARM_SVE_INSTRUCTIONS_AVAILABLE (46)
#endif

#else // HOST_WINDOWS

#include "minipalconfig.h"

#if HAVE_AUXV_HWCAP_H

#include <sys/auxv.h>
#include <asm/hwcap.h>

#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wunused-macros"
#endif

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

#ifndef XSTATE_MASK_AVX512
#define XSTATE_MASK_AVX512 (0xE0) /* 0b1110_0000 */
#endif // XSTATE_MASK_AVX512

#ifdef __clang__
#pragma clang diagnostic pop
#endif

#endif

#if HAVE_SYSCTLBYNAME
#include <sys/sysctl.h>
#endif

#endif // !HOST_WINDOWS

#if defined(HOST_UNIX)
#if defined(HOST_X86) || defined(HOST_AMD64)

static uint32_t xmmYmmStateSupport(void)
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

static uint32_t avx512StateSupport(void)
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

static bool IsAvxEnabled(void)
{
    return true;
}

static bool IsAvx512Enabled(void)
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
    assert(maxCpuId >= 1);

    __cpuid(cpuidInfo, 0x00000001);

    assert((cpuidInfo[CPUID_EDX] & (1 << 25)) != 0);                                                            // SSE
    assert((cpuidInfo[CPUID_EDX] & (1 << 26)) != 0);                                                            // SSE2

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
                        if (IsAvxEnabled() && (xmmYmmStateSupport() == 1))                                      // XGETBV == 11
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

                                    if (IsAvx512Enabled() && (avx512StateSupport() == 1))                       // XGETBV XRC0[7:5] == 111
                                    {
                                        if ((((uint32_t)cpuidInfo[CPUID_EBX] & ((uint32_t)1 << 16)) != 0) &&                        // AVX512F
                                            (((uint32_t)cpuidInfo[CPUID_EBX] & ((uint32_t)1 << 30)) != 0) &&                        // AVX512BW
                                            (((uint32_t)cpuidInfo[CPUID_EBX] & ((uint32_t)1 << 28)) != 0) &&                        // AVX512CD
                                            (((uint32_t)cpuidInfo[CPUID_EBX] & ((uint32_t)1 << 17)) != 0) &&                        // AVX512DQ
                                            (((uint32_t)cpuidInfo[CPUID_EBX] & ((uint32_t)1 << 31)) != 0))                          // AVX512VL
                                        {
                                            // While the AVX-512 ISAs can be individually lit-up, they really
                                            // need F, BW, CD, DQ, and VL to be fully functional without adding
                                            // significant complexity into the JIT. Additionally, unlike AVX/AVX2
                                            // there was never really any hardware that didn't provide all 5 at
                                            // once, with the notable exception being Knight's Landing which
                                            // provided a similar but not quite the same feature.

                                            result |= XArchIntrinsicConstants_Evex;
                                            result |= XArchIntrinsicConstants_Avx512;

                                            if ((cpuidInfo[CPUID_ECX] & (1 << 1)) != 0)                         // AVX512VBMI
                                            {
                                                result |= XArchIntrinsicConstants_Avx512Vbmi;
                                            }
                                        }
                                    }

                                    __cpuidex(cpuidInfo, 0x00000007, 0x00000001);

                                    if ((cpuidInfo[CPUID_EAX] & (1 << 4)) != 0)                                 // AVX-VNNI
                                    {
                                        result |= XArchIntrinsicConstants_AvxVnni;
                                    }

                                    if ((cpuidInfo[CPUID_EDX] & (1 << 19)) != 0)                                // Avx10
                                    {
                                        __cpuidex(cpuidInfo, 0x00000024, 0x00000000);
                                        uint8_t avx10Version = (uint8_t)(cpuidInfo[CPUID_EBX] & 0xFF);

                                        if((avx10Version >= 1) &&
                                           ((cpuidInfo[CPUID_EBX] & (1 << 16)) != 0) &&                         // Avx10/V128
                                           ((cpuidInfo[CPUID_EBX] & (1 << 17)) != 0))                           // Avx10/V256
                                        {
                                            result |= XArchIntrinsicConstants_Evex;
                                            result |= XArchIntrinsicConstants_Avx10v1;

                                            bool isV512Supported = (cpuidInfo[CPUID_EBX] & (1 << 18)) != 0;     // Avx10/V512

                                            if (isV512Supported)
                                            {
                                                result |= XArchIntrinsicConstants_Avx10v1_V512;
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

    __cpuid(cpuidInfo, (int)0x80000000);
    uint32_t maxCpuIdEx = (uint32_t)cpuidInfo[CPUID_EAX];

    if (maxCpuIdEx >= 0x80000001)
    {
        __cpuid(cpuidInfo, (int)0x80000001);

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
        result |= ARM64IntrinsicConstants_AdvSimd;

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

    result |= ARM64IntrinsicConstants_AdvSimd;
#endif // HAVE_AUXV_HWCAP_H
#endif // HOST_UNIX

#if defined(HOST_WINDOWS)
    // FP and SIMD support are enabled by default
    result |= ARM64IntrinsicConstants_AdvSimd;

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

    if (IsProcessorFeaturePresent(PF_ARM_SVE_INSTRUCTIONS_AVAILABLE))
    {
        result |= ARM64IntrinsicConstants_Sve;
    }

#endif // HOST_WINDOWS

#endif // HOST_ARM64

    return result;
}

static bool GetCpuBrand(char* brand, size_t bufferSize)
{
#if defined(HOST_AMD64) || defined(HOST_X86)
    // Check for CPU brand indicating emulation
    int regs[4];

    // Get the maximum value for extended function CPUID info
    __cpuid(regs, (int)0x80000000);
    if ((unsigned int)regs[0] < 0x80000004)
    {
        brand[0] = '\0'; // Extended CPUID not supported, return empty string or handle error
        return false;
    }

    // Retrieve the CPU brand string directly into the caller-provided buffer
    for (unsigned int i = 0x80000002; i <= 0x80000004; ++i)
    {
        __cpuid(regs, (int)i);
        memcpy(brand + (i - 0x80000002) * sizeof(regs), regs, sizeof(regs));
    }

    brand[bufferSize - 1] = '\0';

    return true;
#else
    (void)brand;
    (void)bufferSize;
    return false;
#endif // HOST_AMD64 || HOST_X86
}

// Detect if the current process is running under the Apple Rosetta x64 emulator
bool minipal_detect_rosetta(void)
{
    char brand[49];

    // Check if CPU brand indicates emulation
    return GetCpuBrand(brand, sizeof(brand)) && (strstr(brand, "VirtualApple") != NULL);
}

#if !defined(HOST_WINDOWS)

// Detect if the current process is running under QEMU
bool minipal_detect_qemu(void)
{
    char brand[49];

    // Check if CPU brand indicates emulation
    if (GetCpuBrand(brand, sizeof(brand)) && strstr(brand, "QEMU") != NULL)
    {
        return true;
    }

    // Check for process name of PID 1 indicating emulation
    char cmdline[256];
    FILE *cmdline_file = fopen("/proc/1/cmdline", "r");
    if (cmdline_file != NULL)
    {
        fgets(cmdline, sizeof(cmdline), cmdline_file);
        fclose(cmdline_file);

        if (strstr(cmdline, "qemu-") != NULL)
        {
            return true;
        }
    }

    // Check for process-level emulation using ps command
    FILE *ps_output = popen("ps -e -o comm=", "r");
    if (ps_output != NULL)
    {
        char process_name[256];
        while (fgets(process_name, sizeof(process_name), ps_output) != NULL)
        {
            if (strstr(process_name, "qemu") != NULL)
            {
                pclose(ps_output);
                return true;
            }
        }
        pclose(ps_output);
    }

    return false;
}

#endif // !HOST_WINDOWS
