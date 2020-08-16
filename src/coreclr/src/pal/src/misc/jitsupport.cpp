// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#include "pal/palinternal.h"
#include "pal/dbgmsg.h"
SET_DEFAULT_DEBUG_CHANNEL(MISC);

#include "../../../inc/corjitflags.h"

#if HAVE_AUXV_HWCAP_H
#include <sys/auxv.h>
#include <asm/hwcap.h>
#endif

#if defined(HOST_ARM64) && defined(__linux__)
struct CpuCapability
{
    const char* name;
    unsigned long hwCapFlag;
};

static const CpuCapability CpuCapabilities[] = {
    //{ "fp", HWCAP_FP },
#ifdef HWCAP_ASIMD
    { "asimd", HWCAP_ASIMD },
#endif
    //{ "evtstrm", HWCAP_EVTSTRM },
#ifdef HWCAP_AES
    { "aes", HWCAP_AES },
#endif
    //{ "pmull", HWCAP_PMULL },
#ifdef HWCAP_SHA1
    { "sha1", HWCAP_SHA1 },
#endif
#ifdef HWCAP_SHA2
    { "sha2", HWCAP_SHA2 },
#endif
#ifdef HWCAP_CRC32
    { "crc32", HWCAP_CRC32 },
#endif
#ifdef HWCAP_ATOMICS
    { "atomics", HWCAP_ATOMICS },
#endif
    //{ "fphp", HWCAP_FPHP },
    //{ "asimdhp", HWCAP_ASIMDHP },
    //{ "cpuid", HWCAP_CPUID },
#ifdef HWCAP_ASIMDRDM
    { "asimdrdm", HWCAP_ASIMDRDM },
#endif
    //{ "jscvt", HWCAP_JSCVT },
    //{ "fcma", HWCAP_FCMA },
    //{ "lrcpc", HWCAP_LRCPC },
    //{ "dcpop", HWCAP_DCPOP },
    //{ "sha3", HWCAP_SHA3 },
    //{ "sm3", HWCAP_SM3 },
    //{ "sm4", HWCAP_SM4 },
#ifdef HWCAP_ASIMDDP
    { "asimddp", HWCAP_ASIMDDP },
#endif
    //{ "sha512", HWCAP_SHA512 },
    //{ "sve", HWCAP_SVE },
    //{ "asimdfhm", HWCAP_ASIMDFHM },
    //{ "dit", HWCAP_DIT },
    //{ "uscat", HWCAP_USCAT },
    //{ "ilrcpc", HWCAP_ILRCPC },
    //{ "flagm", HWCAP_FLAGM },
    //{ "ssbs", HWCAP_SSBS },
    //{ "sb", HWCAP_SB },
    //{ "paca", HWCAP_PACA },
    //{ "pacg", HWCAP_PACG },

    // Ensure the array is never empty
    { "", 0 }
};

// Returns the HWCAP_* flag corresponding to the given capability name.
// If the capability name is not recognized or unused at present, zero is returned.
static unsigned long LookupCpuCapabilityFlag(const char* start, size_t length)
{
    for (int i = 0; i < _countof(CpuCapabilities); i++)
    {
        const char* capabilityName = CpuCapabilities[i].name;
        if ((length == strlen(capabilityName)) && (memcmp(start, capabilityName, length) == 0))
        {
            return CpuCapabilities[i].hwCapFlag;
        }
    }
    return 0;
}

// Reads the first Features entry from /proc/cpuinfo (assuming other entries are essentially
// identical) and translates it into a set of HWCAP_* flags.
static unsigned long GetCpuCapabilityFlagsFromCpuInfo()
{
    unsigned long capabilityFlags = 0;
    FILE* cpuInfoFile = fopen("/proc/cpuinfo", "r");

    if (cpuInfoFile != NULL)
    {
        char* line = nullptr;
        size_t lineLen = 0;

        while (getline(&line, &lineLen, cpuInfoFile) != -1)
        {
            char* p = line;
            while (isspace(*p)) p++;

            if (memcmp(p, "Features", 8) != 0)
                continue;

            // Skip "Features" and look for ':'
            p += 8;

            while (isspace(*p)) p++;
            if (*p != ':')
                continue;

            // Skip ':' and parse the list
            p++;

            while (true)
            {
                while (isspace(*p)) p++;
                if (*p == 0)
                    break;

                char* start = p++;
                while ((*p != 0) && !isspace(*p)) p++;

                capabilityFlags |= LookupCpuCapabilityFlag(start, p - start);
            }

            break;
        }

        free(line);
        fclose(cpuInfoFile);
    }

    return capabilityFlags;
}
#endif // defined(HOST_ARM64) && defined(__linux__)

PALIMPORT
VOID
PALAPI
PAL_GetJitCpuCapabilityFlags(CORJIT_FLAGS *flags)
{
    _ASSERTE(flags);

    CORJIT_FLAGS &CPUCompileFlags = *flags;

#if defined(HOST_ARM64)
#if HAVE_AUXV_HWCAP_H
    unsigned long hwCap = getauxval(AT_HWCAP);

#if defined(__linux__)
    // getauxval(AT_HWCAP) returns zero on WSL1 (https://github.com/microsoft/WSL/issues/3682),
    // fall back to reading capabilities from /proc/cpuinfo.
    if (hwCap == 0)
        hwCap = GetCpuCapabilityFlagsFromCpuInfo();
#endif

// HWCAP_* flags are introduced by ARM into the Linux kernel as new extensions are published.
// For a given kernel, some of these flags may not be present yet.
// Use ifdef for each to allow for compilation with any vintage kernel.
// From a single binary distribution perspective, compiling with latest kernel asm/hwcap.h should
// include all published flags.  Given flags are merged to kernel and published before silicon is
// available, using the latest kernel for release should be sufficient.
    CPUCompileFlags.Set(InstructionSet_ArmBase);
#ifdef HWCAP_AES
    if (hwCap & HWCAP_AES)
        CPUCompileFlags.Set(InstructionSet_Aes);
#endif
#ifdef HWCAP_ATOMICS
    if (hwCap & HWCAP_ATOMICS)
        CPUCompileFlags.Set(InstructionSet_Atomics);
#endif
#ifdef HWCAP_CRC32
    if (hwCap & HWCAP_CRC32)
        CPUCompileFlags.Set(InstructionSet_Crc32);
#endif
#ifdef HWCAP_DCPOP
//    if (hwCap & HWCAP_DCPOP)
//        CPUCompileFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_DCPOP);
#endif
#ifdef HWCAP_ASIMDDP
    if (hwCap & HWCAP_ASIMDDP)
        CPUCompileFlags.Set(InstructionSet_Dp);
#endif
#ifdef HWCAP_FCMA
//    if (hwCap & HWCAP_FCMA)
//        CPUCompileFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_FCMA);
#endif
#ifdef HWCAP_FP
//    if (hwCap & HWCAP_FP)
//        CPUCompileFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_FP);
#endif
#ifdef HWCAP_FPHP
//    if (hwCap & HWCAP_FPHP)
//        CPUCompileFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_FP16);
#endif
#ifdef HWCAP_JSCVT
//    if (hwCap & HWCAP_JSCVT)
//        CPUCompileFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_JSCVT);
#endif
#ifdef HWCAP_LRCPC
//    if (hwCap & HWCAP_LRCPC)
//        CPUCompileFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_LRCPC);
#endif
#ifdef HWCAP_PMULL
//    if (hwCap & HWCAP_PMULL)
//        CPUCompileFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_PMULL);
#endif
#ifdef HWCAP_SHA1
    if (hwCap & HWCAP_SHA1)
        CPUCompileFlags.Set(InstructionSet_Sha1);
#endif
#ifdef HWCAP_SHA2
    if (hwCap & HWCAP_SHA2)
        CPUCompileFlags.Set(InstructionSet_Sha256);
#endif
#ifdef HWCAP_SHA512
//    if (hwCap & HWCAP_SHA512)
//        CPUCompileFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_SHA512);
#endif
#ifdef HWCAP_SHA3
//    if (hwCap & HWCAP_SHA3)
//        CPUCompileFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_SHA3);
#endif
#ifdef HWCAP_ASIMD
    if (hwCap & HWCAP_ASIMD)
        CPUCompileFlags.Set(InstructionSet_AdvSimd);
#endif
#ifdef HWCAP_ASIMDRDM
    if (hwCap & HWCAP_ASIMDRDM)
        CPUCompileFlags.Set(InstructionSet_Rdm);
#endif
#ifdef HWCAP_ASIMDHP
//    if (hwCap & HWCAP_ASIMDHP)
//        CPUCompileFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_ADVSIMD_FP16);
#endif
#ifdef HWCAP_SM3
//    if (hwCap & HWCAP_SM3)
//        CPUCompileFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_SM3);
#endif
#ifdef HWCAP_SM4
//    if (hwCap & HWCAP_SM4)
//        CPUCompileFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_SM4);
#endif
#ifdef HWCAP_SVE
//    if (hwCap & HWCAP_SVE)
//        CPUCompileFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_SVE);
#endif
#else // !HAVE_AUXV_HWCAP_H
    // CoreCLR SIMD and FP support is included in ARM64 baseline
    // On exceptional basis platforms may leave out support, but CoreCLR does not
    // yet support such platforms
    // Set baseline flags if OS has not exposed mechanism for us to determine CPU capabilities
    CPUCompileFlags.Set(InstructionSet_ArmBase);
    CPUCompileFlags.Set(InstructionSet_AdvSimd);
    //    CPUCompileFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_FP);
#endif // HAVE_AUXV_HWCAP_H
#elif defined(TARGET_ARM64)
    // Enable ARM64 based flags by default so we always crossgen
    // ARM64 intrinsics for Linux
    CPUCompileFlags.Set(InstructionSet_ArmBase);
    CPUCompileFlags.Set(InstructionSet_AdvSimd);
#endif // defined(HOST_ARM64)
}
