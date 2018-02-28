// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//////////////////////////////////////////////////////////////////////////////////////////////////////////
//
// NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE
//
// The JIT/EE interface is versioned. By "interface", we mean mean any and all communication between the
// JIT and the EE. Any time a change is made to the interface, the JIT/EE interface version identifier
// must be updated. See code:JITEEVersionIdentifier for more information.
//
// NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE
//
//////////////////////////////////////////////////////////////////////////////////////////////////////////

#ifndef _COR_JIT_FLAGS_H_
#define _COR_JIT_FLAGS_H_

class CORJIT_FLAGS
{
public:

    enum CorJitFlag
    {
        CORJIT_FLAG_CALL_GETJITFLAGS        = 0xffffffff, // Indicates that the JIT should retrieve flags in the form of a
                                                          // pointer to a CORJIT_FLAGS value via ICorJitInfo::getJitFlags().
        CORJIT_FLAG_SPEED_OPT               = 0,
        CORJIT_FLAG_SIZE_OPT                = 1,
        CORJIT_FLAG_DEBUG_CODE              = 2, // generate "debuggable" code (no code-mangling optimizations)
        CORJIT_FLAG_DEBUG_EnC               = 3, // We are in Edit-n-Continue mode
        CORJIT_FLAG_DEBUG_INFO              = 4, // generate line and local-var info
        CORJIT_FLAG_MIN_OPT                 = 5, // disable all jit optimizations (not necesarily debuggable code)
        CORJIT_FLAG_GCPOLL_CALLS            = 6, // Emit calls to JIT_POLLGC for thread suspension.
        CORJIT_FLAG_MCJIT_BACKGROUND        = 7, // Calling from multicore JIT background thread, do not call JitComplete

    #if defined(_TARGET_X86_)

        CORJIT_FLAG_PINVOKE_RESTORE_ESP     = 8, // Restore ESP after returning from inlined PInvoke
        CORJIT_FLAG_TARGET_P4               = 9,
        CORJIT_FLAG_USE_FCOMI               = 10, // Generated code may use fcomi(p) instruction
        CORJIT_FLAG_USE_CMOV                = 11, // Generated code may use cmov instruction
        CORJIT_FLAG_USE_SSE2                = 12, // Generated code may use SSE-2 instructions

    #else // !defined(_TARGET_X86_)

        CORJIT_FLAG_UNUSED1                 = 8,
        CORJIT_FLAG_UNUSED2                 = 9,
        CORJIT_FLAG_UNUSED3                 = 10,
        CORJIT_FLAG_UNUSED4                 = 11,
        CORJIT_FLAG_UNUSED5                 = 12,

    #endif // !defined(_TARGET_X86_)

        CORJIT_FLAG_UNUSED6                 = 13,

    #if defined(_TARGET_X86_) || defined(_TARGET_AMD64_)

        CORJIT_FLAG_USE_AVX                 = 14,
        CORJIT_FLAG_USE_AVX2                = 15,
        CORJIT_FLAG_USE_AVX_512             = 16,

    #else // !defined(_TARGET_X86_) && !defined(_TARGET_AMD64_)

        CORJIT_FLAG_UNUSED7                 = 14,
        CORJIT_FLAG_UNUSED8                 = 15,
        CORJIT_FLAG_UNUSED9                 = 16,

    #endif // !defined(_TARGET_X86_) && !defined(_TARGET_AMD64_)

    #if defined(_TARGET_X86_) || defined(_TARGET_AMD64_) || defined(_TARGET_ARM64_)
        CORJIT_FLAG_FEATURE_SIMD            = 17,
    #else
        CORJIT_FLAG_UNUSED10                = 17,
    #endif // !(defined(_TARGET_X86_) || defined(_TARGET_AMD64_) || defined(_TARGET_ARM64_))

        CORJIT_FLAG_MAKEFINALCODE           = 18, // Use the final code generator, i.e., not the interpreter.
        CORJIT_FLAG_READYTORUN              = 19, // Use version-resilient code generation
        CORJIT_FLAG_PROF_ENTERLEAVE         = 20, // Instrument prologues/epilogues
        CORJIT_FLAG_PROF_REJIT_NOPS         = 21, // Insert NOPs to ensure code is re-jitable
        CORJIT_FLAG_PROF_NO_PINVOKE_INLINE  = 22, // Disables PInvoke inlining
        CORJIT_FLAG_SKIP_VERIFICATION       = 23, // (lazy) skip verification - determined without doing a full resolve. See comment below
        CORJIT_FLAG_PREJIT                  = 24, // jit or prejit is the execution engine.
        CORJIT_FLAG_RELOC                   = 25, // Generate relocatable code
        CORJIT_FLAG_IMPORT_ONLY             = 26, // Only import the function
        CORJIT_FLAG_IL_STUB                 = 27, // method is an IL stub
        CORJIT_FLAG_PROCSPLIT               = 28, // JIT should separate code into hot and cold sections
        CORJIT_FLAG_BBINSTR                 = 29, // Collect basic block profile information
        CORJIT_FLAG_BBOPT                   = 30, // Optimize method based on profile information
        CORJIT_FLAG_FRAMED                  = 31, // All methods have an EBP frame
        CORJIT_FLAG_ALIGN_LOOPS             = 32, // add NOPs before loops to align them at 16 byte boundaries
        CORJIT_FLAG_PUBLISH_SECRET_PARAM    = 33, // JIT must place stub secret param into local 0.  (used by IL stubs)
        CORJIT_FLAG_GCPOLL_INLINE           = 34, // JIT must inline calls to GCPoll when possible
        CORJIT_FLAG_SAMPLING_JIT_BACKGROUND = 35, // JIT is being invoked as a result of stack sampling for hot methods in the background
        CORJIT_FLAG_USE_PINVOKE_HELPERS     = 36, // The JIT should use the PINVOKE_{BEGIN,END} helpers instead of emitting inline transitions
        CORJIT_FLAG_REVERSE_PINVOKE         = 37, // The JIT should insert REVERSE_PINVOKE_{ENTER,EXIT} helpers into method prolog/epilog
        CORJIT_FLAG_DESKTOP_QUIRKS          = 38, // The JIT should generate desktop-quirk-compatible code
        CORJIT_FLAG_TIER0                   = 39, // This is the initial tier for tiered compilation which should generate code as quickly as possible
        CORJIT_FLAG_TIER1                   = 40, // This is the final tier (for now) for tiered compilation which should generate high quality code

#if defined(_TARGET_ARM_)
        CORJIT_FLAG_RELATIVE_CODE_RELOCS    = 41, // JIT should generate PC-relative address computations instead of EE relocation records
#else // !defined(_TARGET_ARM_)
        CORJIT_FLAG_UNUSED11                = 41,
#endif // !defined(_TARGET_ARM_)

        CORJIT_FLAG_NO_INLINING             = 42, // JIT should not inline any called method into this method

#if defined(_TARGET_ARM64_)

        CORJIT_FLAG_HAS_ARM64_AES           = 43, // ID_AA64ISAR0_EL1.AES is 1 or better
        CORJIT_FLAG_HAS_ARM64_ATOMICS       = 44, // ID_AA64ISAR0_EL1.Atomic is 2 or better
        CORJIT_FLAG_HAS_ARM64_CRC32         = 45, // ID_AA64ISAR0_EL1.CRC32 is 1 or better
        CORJIT_FLAG_HAS_ARM64_DCPOP         = 46, // ID_AA64ISAR1_EL1.DPB is 1 or better
        CORJIT_FLAG_HAS_ARM64_DP            = 47, // ID_AA64ISAR0_EL1.DP is 1 or better
        CORJIT_FLAG_HAS_ARM64_FCMA          = 48, // ID_AA64ISAR1_EL1.FCMA is 1 or better
        CORJIT_FLAG_HAS_ARM64_FP            = 49, // ID_AA64PFR0_EL1.FP is 0 or better
        CORJIT_FLAG_HAS_ARM64_FP16          = 50, // ID_AA64PFR0_EL1.FP is 1 or better
        CORJIT_FLAG_HAS_ARM64_JSCVT         = 51, // ID_AA64ISAR1_EL1.JSCVT is 1 or better
        CORJIT_FLAG_HAS_ARM64_LRCPC         = 52, // ID_AA64ISAR1_EL1.LRCPC is 1 or better
        CORJIT_FLAG_HAS_ARM64_PMULL         = 53, // ID_AA64ISAR0_EL1.AES is 2 or better
        CORJIT_FLAG_HAS_ARM64_SHA1          = 54, // ID_AA64ISAR0_EL1.SHA1 is 1 or better
        CORJIT_FLAG_HAS_ARM64_SHA256        = 55, // ID_AA64ISAR0_EL1.SHA2 is 1 or better
        CORJIT_FLAG_HAS_ARM64_SHA512        = 56, // ID_AA64ISAR0_EL1.SHA2 is 2 or better
        CORJIT_FLAG_HAS_ARM64_SHA3          = 57, // ID_AA64ISAR0_EL1.SHA3 is 1 or better
        CORJIT_FLAG_HAS_ARM64_SIMD          = 58, // ID_AA64PFR0_EL1.AdvSIMD is 0 or better
        CORJIT_FLAG_HAS_ARM64_SIMD_V81      = 59, // ID_AA64ISAR0_EL1.RDM is 1 or better
        CORJIT_FLAG_HAS_ARM64_SIMD_FP16     = 60, // ID_AA64PFR0_EL1.AdvSIMD is 1 or better
        CORJIT_FLAG_HAS_ARM64_SM3           = 61, // ID_AA64ISAR0_EL1.SM3 is 1 or better
        CORJIT_FLAG_HAS_ARM64_SM4           = 62, // ID_AA64ISAR0_EL1.SM4 is 1 or better
        CORJIT_FLAG_HAS_ARM64_SVE           = 63  // ID_AA64PFR0_EL1.SVE is 1 or better

#elif defined(_TARGET_X86_) || defined(_TARGET_AMD64_)

        CORJIT_FLAG_USE_SSE3                = 43,
        CORJIT_FLAG_USE_SSSE3               = 44,
        CORJIT_FLAG_USE_SSE41               = 45,
        CORJIT_FLAG_USE_SSE42               = 46,
        CORJIT_FLAG_USE_AES                 = 47,
        CORJIT_FLAG_USE_BMI1                = 48,
        CORJIT_FLAG_USE_BMI2                = 49,
        CORJIT_FLAG_USE_FMA                 = 50,
        CORJIT_FLAG_USE_LZCNT               = 51,
        CORJIT_FLAG_USE_PCLMULQDQ           = 52,
        CORJIT_FLAG_USE_POPCNT              = 53,
        CORJIT_FLAG_UNUSED23                = 54,
        CORJIT_FLAG_UNUSED24                = 55,
        CORJIT_FLAG_UNUSED25                = 56,
        CORJIT_FLAG_UNUSED26                = 57,
        CORJIT_FLAG_UNUSED27                = 58,
        CORJIT_FLAG_UNUSED28                = 59,
        CORJIT_FLAG_UNUSED29                = 60,
        CORJIT_FLAG_UNUSED30                = 61,
        CORJIT_FLAG_UNUSED31                = 62,
        CORJIT_FLAG_UNUSED32                = 63


#else // !defined(_TARGET_ARM64_) &&!defined(_TARGET_X86_) && !defined(_TARGET_AMD64_)

        CORJIT_FLAG_UNUSED12                = 43,
        CORJIT_FLAG_UNUSED13                = 44,
        CORJIT_FLAG_UNUSED14                = 45,
        CORJIT_FLAG_UNUSED15                = 46,
        CORJIT_FLAG_UNUSED16                = 47,
        CORJIT_FLAG_UNUSED17                = 48,
        CORJIT_FLAG_UNUSED18                = 49,
        CORJIT_FLAG_UNUSED19                = 50,
        CORJIT_FLAG_UNUSED20                = 51,
        CORJIT_FLAG_UNUSED21                = 52,
        CORJIT_FLAG_UNUSED22                = 53,
        CORJIT_FLAG_UNUSED23                = 54,
        CORJIT_FLAG_UNUSED24                = 55,
        CORJIT_FLAG_UNUSED25                = 56,
        CORJIT_FLAG_UNUSED26                = 57,
        CORJIT_FLAG_UNUSED27                = 58,
        CORJIT_FLAG_UNUSED28                = 59,
        CORJIT_FLAG_UNUSED29                = 60,
        CORJIT_FLAG_UNUSED30                = 61,
        CORJIT_FLAG_UNUSED31                = 62,
        CORJIT_FLAG_UNUSED32                = 63

#endif // !defined(_TARGET_ARM64_) &&!defined(_TARGET_X86_) && !defined(_TARGET_AMD64_)
    };

    CORJIT_FLAGS()
        : corJitFlags(0)
    {
        // empty
    }

    // Convenience constructor to set exactly one flag.
    CORJIT_FLAGS(CorJitFlag flag)
        : corJitFlags(0)
    {
        Set(flag);
    }

    CORJIT_FLAGS(const CORJIT_FLAGS& other)
    {
        corJitFlags = other.corJitFlags;
    }

    void Reset()
    {
        corJitFlags = 0;
    }

    void Set(CorJitFlag flag)
    {
        corJitFlags |= 1ULL << (unsigned __int64)flag;
    }

    void Clear(CorJitFlag flag)
    {
        corJitFlags &= ~(1ULL << (unsigned __int64)flag);
    }

    bool IsSet(CorJitFlag flag) const
    {
        return (corJitFlags & (1ULL << (unsigned __int64)flag)) != 0;
    }

    void Add(const CORJIT_FLAGS& other)
    {
        corJitFlags |= other.corJitFlags;
    }

    void Remove(const CORJIT_FLAGS& other)
    {
        corJitFlags &= ~other.corJitFlags;
    }

    bool IsEmpty() const
    {
        return corJitFlags == 0;
    }

    // DO NOT USE THIS FUNCTION! (except in very restricted special cases)
    unsigned __int64 GetFlagsRaw()
    {
        return corJitFlags;
    }

private:

    unsigned __int64 corJitFlags;
};


#endif // _COR_JIT_FLAGS_H_
