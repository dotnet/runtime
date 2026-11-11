// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Access to the x64 extended processor state saved in a Linux signal context, shared by the CoreCLR PAL and NativeAOT.
// The including file must provide the platform config (HAVE_PUBLIC_XSTATE_STRUCT, HAVE__FPX_SW_BYTES_WITH_XSTATE_BV,
// HAVE_FPSTATE_GLIBC_RESERVED1) and _ASSERTE.

#ifndef _NATIVE_CONTEXT_XSTATE_H_
#define _NATIVE_CONTEXT_XSTATE_H_

#if defined(XSTATE_SUPPORTED) && defined(HOST_AMD64) && !defined(__APPLE__)

#include <signal.h>
#include <stddef.h>
#include <stdint.h>
#include <minipal/cpuid.h>

#if !HAVE_PUBLIC_XSTATE_STRUCT
namespace asm_sigcontext
{
#include <asm/sigcontext.h>
};
using asm_sigcontext::_fpx_sw_bytes;
using asm_sigcontext::_xstate;
#endif // !HAVE_PUBLIC_XSTATE_STRUCT

#ifndef FPREG_Fpstate
#define FPREG_Fpstate(uc) ((uc)->uc_mcontext.fpregs)
#endif

#if HAVE_FPSTATE_GLIBC_RESERVED1
#define FPSTATE_RESERVED __glibc_reserved1
#else
#define FPSTATE_RESERVED padding
#endif

// Presence for various extended state registers is detected via the xfeatures (formerly xstate_bv) field. On some
// Linux distros, this definition is only in internal headers, so we define it here. The masks are extracted from
// the processor xstate bit vector register, so the value is OS independent.

#ifndef XSTATE_APX
#define XSTATE_APX 19
#endif // XSTATE_APX

#ifndef XFEATURE_MASK_APX
#define XFEATURE_MASK_APX (1 << XSTATE_APX)
#endif  // XFEATURE_MASK_APX

#if HAVE__FPX_SW_BYTES_WITH_XSTATE_BV
#define FPREG_FpxSwBytes_xfeatures(uc) FPREG_FpxSwBytes(uc)->xstate_bv
#else
#define FPREG_FpxSwBytes_xfeatures(uc) FPREG_FpxSwBytes(uc)->xfeatures
#endif

// The internal _xstate struct is exposed as fpstate, xstate_hdr, ymmh. However, in reality this is
// fpstate, xstate_hdr, extended_state_area and "technically" we are supposed to be determining the
// offset and size of each XFEATURE_MASK_* via CPUID. The extended region always starts at offset
// 576 which is the same as the address of ymmh

#define FPREG_Xstate_ExtendedStateArea_Offset offsetof(_xstate, ymmh)
#define FPREG_Xstate_ExtendedStateArea(uc) (reinterpret_cast<uint8_t *>(FPREG_Fpstate(uc)) + \
                                            FPREG_Xstate_ExtendedStateArea_Offset)

struct Xstate_ExtendedFeature
{
    bool     initialized;
    uint32_t offset;
    uint32_t size;
};

#define Xstate_ExtendedFeatures_Count (XSTATE_APX + 1)
inline Xstate_ExtendedFeature Xstate_ExtendedFeatures[Xstate_ExtendedFeatures_Count];

inline _fpx_sw_bytes *FPREG_FpxSwBytes(const ucontext_t *uc)
{
    // Bytes 464..511 in the FXSAVE format are available for software to use for any purpose. In this case, they are used to
    // indicate information about extended state.
    _ASSERTE(reinterpret_cast<uint8_t *>(&FPREG_Fpstate(uc)->FPSTATE_RESERVED[12]) - reinterpret_cast<uint8_t *>(FPREG_Fpstate(uc)) == 464);

    _ASSERTE(FPREG_Fpstate(uc) != nullptr);

    return reinterpret_cast<_fpx_sw_bytes *>(&FPREG_Fpstate(uc)->FPSTATE_RESERVED[12]);
}

inline uint32_t FPREG_ExtendedSize(const ucontext_t *uc)
{
    _ASSERTE(FPREG_FpxSwBytes(uc)->magic1 == FP_XSTATE_MAGIC1);
    return FPREG_FpxSwBytes(uc)->extended_size;
}

inline bool FPREG_HasExtendedState(const ucontext_t *uc)
{
    // See comments in /usr/include/x86_64-linux-gnu/asm/sigcontext.h for info on how to detect if extended state is present
    static_assert(FP_XSTATE_MAGIC2_SIZE == sizeof(uint32_t));

    if (FPREG_FpxSwBytes(uc)->magic1 != FP_XSTATE_MAGIC1)
    {
        return false;
    }

    uint32_t extendedSize = FPREG_ExtendedSize(uc);
    if (extendedSize < sizeof(_xstate))
    {
        return false;
    }

    _ASSERTE(extendedSize >= FP_XSTATE_MAGIC2_SIZE);
    if (*reinterpret_cast<uint32_t *>(reinterpret_cast<uint8_t *>(FPREG_Fpstate(uc)) + (extendedSize - FP_XSTATE_MAGIC2_SIZE))
        != FP_XSTATE_MAGIC2)
    {
        return false;
    }

    return true;
}

inline void *FPREG_Xstate_ExtendedFeature(const ucontext_t *uc, uint32_t *featureSize, uint32_t featureIndex)
{
    _ASSERTE(featureSize != nullptr);
    _ASSERTE(featureIndex < (sizeof(Xstate_ExtendedFeatures) / sizeof(Xstate_ExtendedFeature)));
    _ASSERTE(FPREG_Xstate_ExtendedStateArea_Offset == 576);

    Xstate_ExtendedFeature *extendedFeature = &Xstate_ExtendedFeatures[featureIndex];

    if (!extendedFeature->initialized)
    {
        int cpuidInfo[4];

        const int CPUID_EAX = 0;
        const int CPUID_EBX = 1;
        const int CPUID_ECX = 2;
        const int CPUID_EDX = 3;

#ifdef _DEBUG
        // We should only be calling this function if we know the extended feature exists

        __cpuid(cpuidInfo, 0x00000000);
        _ASSERTE(static_cast<uint32_t>(cpuidInfo[CPUID_EAX]) >= 0x0D);

        __cpuidex(cpuidInfo, 0x0000000D, 0x00000000);
        _ASSERTE((cpuidInfo[CPUID_EAX] & (1 << featureIndex)) != 0);
#endif // _DEBUG

        __cpuidex(cpuidInfo, 0x0000000D, static_cast<int32_t>(featureIndex));

        _ASSERTE(static_cast<uint32_t>(cpuidInfo[CPUID_EAX]) > 0);
        _ASSERTE(static_cast<uint32_t>(cpuidInfo[CPUID_EBX]) >= FPREG_Xstate_ExtendedStateArea_Offset);

        extendedFeature->size   = static_cast<uint32_t>(cpuidInfo[CPUID_EAX]);
        extendedFeature->offset = static_cast<uint32_t>(cpuidInfo[CPUID_EBX] - FPREG_Xstate_ExtendedStateArea_Offset);

        extendedFeature->initialized = true;
    }

    *featureSize = extendedFeature->size;
    return (FPREG_Xstate_ExtendedStateArea(uc) + extendedFeature->offset);
}

inline bool Xstate_IsApxSupported()
{
    static int Xstate_ApxSupported = -1;

    if (Xstate_ApxSupported == -1)
    {
        int cpuidInfo[4];

        const int CPUID_EAX = 0;
        const int CPUID_EBX = 1;
        const int CPUID_ECX = 2;
        const int CPUID_EDX = 3;

#ifdef _DEBUG
        // We should only be calling this function if we know the extended feature exists
        __cpuid(cpuidInfo, 0x00000000);
        _ASSERTE(static_cast<uint32_t>(cpuidInfo[CPUID_EAX]) >= 0x0D);
#endif // _DEBUG

        __cpuidex(cpuidInfo, 0x0000000D, 0x00000000);

        if ((cpuidInfo[CPUID_EAX] & XFEATURE_MASK_APX) == XFEATURE_MASK_APX)
        {
            __cpuidex(cpuidInfo, 0x00000007, 0x00000001);

            const int requiredApxFlags = (1 << 21);

            if ((cpuidInfo[CPUID_EDX] & requiredApxFlags) == requiredApxFlags)
            {
                Xstate_ApxSupported = 1;
            }
        }

        if (Xstate_ApxSupported == -1)
        {
            Xstate_ApxSupported = 0;
        }
    }

    return Xstate_ApxSupported == 1;
}

inline bool FPREG_HasApxRegisters(const ucontext_t *uc)
{
    if (!FPREG_HasExtendedState(uc))
    {
        return false;
    }

    if ((FPREG_FpxSwBytes_xfeatures(uc) & XFEATURE_MASK_APX) != XFEATURE_MASK_APX)
    {
        return false;
    }

    return Xstate_IsApxSupported();
}

inline void *FPREG_Xstate_Egpr(const ucontext_t *uc, uint32_t *featureSize)
{
    _ASSERTE(FPREG_HasApxRegisters(uc));
    return FPREG_Xstate_ExtendedFeature(uc, featureSize, XSTATE_APX);
}

#endif // XSTATE_SUPPORTED && HOST_AMD64 && !__APPLE__

#endif // _NATIVE_CONTEXT_XSTATE_H_
