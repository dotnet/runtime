// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This class wraps the CORJIT_FLAGS type in the JIT-EE interface (in corjit.h) such that the JIT can
// build with either the old flags (COR_JIT_EE_VERSION <= 460) or the new flags (COR_JIT_EE_VERSION > 460).
// It actually is exactly the same as the new definition, and must be kept up-to-date with the new definition.
// When built against an old JIT-EE interface, the old flags are converted into this structure.
class JitFlags
{
public:
    // clang-format off
    enum JitFlag
    {
        JIT_FLAG_SPEED_OPT               = 0,
        JIT_FLAG_SIZE_OPT                = 1,
        JIT_FLAG_DEBUG_CODE              = 2, // generate "debuggable" code (no code-mangling optimizations)
        JIT_FLAG_DEBUG_EnC               = 3, // We are in Edit-n-Continue mode
        JIT_FLAG_DEBUG_INFO              = 4, // generate line and local-var info
        JIT_FLAG_MIN_OPT                 = 5, // disable all jit optimizations (not necesarily debuggable code)
        JIT_FLAG_GCPOLL_CALLS            = 6, // Emit calls to JIT_POLLGC for thread suspension.
        JIT_FLAG_MCJIT_BACKGROUND        = 7, // Calling from multicore JIT background thread, do not call JitComplete

    #if defined(_TARGET_X86_)

        JIT_FLAG_PINVOKE_RESTORE_ESP     = 8, // Restore ESP after returning from inlined PInvoke
        JIT_FLAG_TARGET_P4               = 9,
        JIT_FLAG_USE_FCOMI               = 10, // Generated code may use fcomi(p) instruction
        JIT_FLAG_USE_CMOV                = 11, // Generated code may use cmov instruction
        JIT_FLAG_USE_SSE2                = 12, // Generated code may use SSE-2 instructions

    #else // !defined(_TARGET_X86_)

        JIT_FLAG_UNUSED1                 = 8,
        JIT_FLAG_UNUSED2                 = 9,
        JIT_FLAG_UNUSED3                 = 10,
        JIT_FLAG_UNUSED4                 = 11,
        JIT_FLAG_UNUSED5                 = 12,

    #endif // !defined(_TARGET_X86_)

        JIT_FLAG_UNUSED6                 = 13,

    #if defined(_TARGET_X86_) || defined(_TARGET_AMD64_)

        JIT_FLAG_USE_AVX                 = 14,
        JIT_FLAG_USE_AVX2                = 15,
        JIT_FLAG_USE_AVX_512             = 16,

    #else // !defined(_TARGET_X86_) && !defined(_TARGET_AMD64_)

        JIT_FLAG_UNUSED7                 = 14,
        JIT_FLAG_UNUSED8                 = 15,
        JIT_FLAG_UNUSED9                 = 16,

    #endif // !defined(_TARGET_X86_) && !defined(_TARGET_AMD64_)

    #if defined(_TARGET_X86_) || defined(_TARGET_AMD64_) || defined(_TARGET_ARM64_)
        JIT_FLAG_FEATURE_SIMD            = 17,
    #else
        JIT_FLAG_UNUSED10                = 17,
    #endif // !(defined(_TARGET_X86_) || defined(_TARGET_AMD64_) || defined(_TARGET_ARM64_))

        JIT_FLAG_MAKEFINALCODE           = 18, // Use the final code generator, i.e., not the interpreter.
        JIT_FLAG_READYTORUN              = 19, // Use version-resilient code generation
        JIT_FLAG_PROF_ENTERLEAVE         = 20, // Instrument prologues/epilogues
        JIT_FLAG_PROF_REJIT_NOPS         = 21, // Insert NOPs to ensure code is re-jitable
        JIT_FLAG_PROF_NO_PINVOKE_INLINE  = 22, // Disables PInvoke inlining
        JIT_FLAG_SKIP_VERIFICATION       = 23, // (lazy) skip verification - determined without doing a full resolve. See comment below
        JIT_FLAG_PREJIT                  = 24, // jit or prejit is the execution engine.
        JIT_FLAG_RELOC                   = 25, // Generate relocatable code
        JIT_FLAG_IMPORT_ONLY             = 26, // Only import the function
        JIT_FLAG_IL_STUB                 = 27, // method is an IL stub
        JIT_FLAG_PROCSPLIT               = 28, // JIT should separate code into hot and cold sections
        JIT_FLAG_BBINSTR                 = 29, // Collect basic block profile information
        JIT_FLAG_BBOPT                   = 30, // Optimize method based on profile information
        JIT_FLAG_FRAMED                  = 31, // All methods have an EBP frame
        JIT_FLAG_ALIGN_LOOPS             = 32, // add NOPs before loops to align them at 16 byte boundaries
        JIT_FLAG_PUBLISH_SECRET_PARAM    = 33, // JIT must place stub secret param into local 0.  (used by IL stubs)
        JIT_FLAG_GCPOLL_INLINE           = 34, // JIT must inline calls to GCPoll when possible
        JIT_FLAG_SAMPLING_JIT_BACKGROUND = 35, // JIT is being invoked as a result of stack sampling for hot methods in the background
        JIT_FLAG_USE_PINVOKE_HELPERS     = 36, // The JIT should use the PINVOKE_{BEGIN,END} helpers instead of emitting inline transitions
        JIT_FLAG_REVERSE_PINVOKE         = 37, // The JIT should insert REVERSE_PINVOKE_{ENTER,EXIT} helpers into method prolog/epilog
        JIT_FLAG_DESKTOP_QUIRKS          = 38, // The JIT should generate desktop-quirk-compatible code
        JIT_FLAG_TIER0                   = 39, // This is the initial tier for tiered compilation which should generate code as quickly as possible
        JIT_FLAG_TIER1                   = 40, // This is the final tier (for now) for tiered compilation which should generate high quality code

#if defined(_TARGET_ARM_)
        JIT_FLAG_RELATIVE_CODE_RELOCS    = 41, // JIT should generate PC-relative address computations instead of EE relocation records
#else // !defined(_TARGET_ARM_)
        JIT_FLAG_UNUSED11                = 41,
#endif // !defined(_TARGET_ARM_)

        JIT_FLAG_NO_INLINING             = 42, // JIT should not inline any called method into this method

#if defined(_TARGET_ARM64_)

        JIT_FLAG_HAS_ARM64_AES           = 43, // ID_AA64ISAR0_EL1.AES is 1 or better
        JIT_FLAG_HAS_ARM64_ATOMICS       = 44, // ID_AA64ISAR0_EL1.Atomic is 2 or better
        JIT_FLAG_HAS_ARM64_CRC32         = 45, // ID_AA64ISAR0_EL1.CRC32 is 1 or better
        JIT_FLAG_HAS_ARM64_DCPOP         = 46, // ID_AA64ISAR1_EL1.DPB is 1 or better
        JIT_FLAG_HAS_ARM64_DP            = 47, // ID_AA64ISAR0_EL1.DP is 1 or better
        JIT_FLAG_HAS_ARM64_FCMA          = 48, // ID_AA64ISAR1_EL1.FCMA is 1 or better
        JIT_FLAG_HAS_ARM64_FP            = 49, // ID_AA64PFR0_EL1.FP is 0 or better
        JIT_FLAG_HAS_ARM64_FP16          = 50, // ID_AA64PFR0_EL1.FP is 1 or better
        JIT_FLAG_HAS_ARM64_JSCVT         = 51, // ID_AA64ISAR1_EL1.JSCVT is 1 or better
        JIT_FLAG_HAS_ARM64_LRCPC         = 52, // ID_AA64ISAR1_EL1.LRCPC is 1 or better
        JIT_FLAG_HAS_ARM64_PMULL         = 53, // ID_AA64ISAR0_EL1.AES is 2 or better
        JIT_FLAG_HAS_ARM64_SHA1          = 54, // ID_AA64ISAR0_EL1.SHA1 is 1 or better
        JIT_FLAG_HAS_ARM64_SHA256        = 55, // ID_AA64ISAR0_EL1.SHA2 is 1 or better
        JIT_FLAG_HAS_ARM64_SHA512        = 56, // ID_AA64ISAR0_EL1.SHA2 is 2 or better
        JIT_FLAG_HAS_ARM64_SHA3          = 57, // ID_AA64ISAR0_EL1.SHA3 is 1 or better
        JIT_FLAG_HAS_ARM64_ADVSIMD      = 58, // ID_AA64PFR0_EL1.AdvSIMD is 0 or better
        JIT_FLAG_HAS_ARM64_ADVSIMD_V81  = 59, // ID_AA64ISAR0_EL1.RDM is 1 or better
        JIT_FLAG_HAS_ARM64_ADVSIMD_FP16 = 60, // ID_AA64PFR0_EL1.AdvSIMD is 1 or better
        JIT_FLAG_HAS_ARM64_SM3           = 61, // ID_AA64ISAR0_EL1.SM3 is 1 or better
        JIT_FLAG_HAS_ARM64_SM4           = 62, // ID_AA64ISAR0_EL1.SM4 is 1 or better
        JIT_FLAG_HAS_ARM64_SVE           = 63  // ID_AA64PFR0_EL1.SVE is 1 or better

#elif defined(_TARGET_X86_) || defined(_TARGET_AMD64_)

        JIT_FLAG_USE_SSE3                = 43,
        JIT_FLAG_USE_SSSE3               = 44,
        JIT_FLAG_USE_SSE41               = 45,
        JIT_FLAG_USE_SSE42               = 46,
        JIT_FLAG_USE_AES                 = 47,
        JIT_FLAG_USE_BMI1                = 48,
        JIT_FLAG_USE_BMI2                = 49,
        JIT_FLAG_USE_FMA                 = 50,
        JIT_FLAG_USE_LZCNT               = 51,
        JIT_FLAG_USE_PCLMULQDQ           = 52,
        JIT_FLAG_USE_POPCNT              = 53,
        JIT_FLAG_UNUSED23                = 54,
        JIT_FLAG_UNUSED24                = 55,
        JIT_FLAG_UNUSED25                = 56,
        JIT_FLAG_UNUSED26                = 57,
        JIT_FLAG_UNUSED27                = 58,
        JIT_FLAG_UNUSED28                = 59,
        JIT_FLAG_UNUSED29                = 60,
        JIT_FLAG_UNUSED30                = 61,
        JIT_FLAG_UNUSED31                = 62,
        JIT_FLAG_UNUSED32                = 63


#else // !defined(_TARGET_ARM64_) && !defined(_TARGET_X86_) && !defined(_TARGET_AMD64_)

        JIT_FLAG_UNUSED12                = 43,
        JIT_FLAG_UNUSED13                = 44,
        JIT_FLAG_UNUSED14                = 45,
        JIT_FLAG_UNUSED15                = 46,
        JIT_FLAG_UNUSED16                = 47,
        JIT_FLAG_UNUSED17                = 48,
        JIT_FLAG_UNUSED18                = 49,
        JIT_FLAG_UNUSED19                = 50,
        JIT_FLAG_UNUSED20                = 51,
        JIT_FLAG_UNUSED21                = 52,
        JIT_FLAG_UNUSED22                = 53,
        JIT_FLAG_UNUSED23                = 54,
        JIT_FLAG_UNUSED24                = 55,
        JIT_FLAG_UNUSED25                = 56,
        JIT_FLAG_UNUSED26                = 57,
        JIT_FLAG_UNUSED27                = 58,
        JIT_FLAG_UNUSED28                = 59,
        JIT_FLAG_UNUSED29                = 60,
        JIT_FLAG_UNUSED30                = 61,
        JIT_FLAG_UNUSED31                = 62,
        JIT_FLAG_UNUSED32                = 63

#endif // !defined(_TARGET_ARM64_) && !defined(_TARGET_X86_) && !defined(_TARGET_AMD64_)

    };
    // clang-format on

    JitFlags() : m_jitFlags(0)
    {
        // empty
    }

    // Convenience constructor to set exactly one flags.
    JitFlags(JitFlag flag) : m_jitFlags(0)
    {
        Set(flag);
    }

    void Reset()
    {
        m_jitFlags = 0;
    }

    void Set(JitFlag flag)
    {
        m_jitFlags |= 1ULL << (unsigned __int64)flag;
    }

    void Clear(JitFlag flag)
    {
        m_jitFlags &= ~(1ULL << (unsigned __int64)flag);
    }

    bool IsSet(JitFlag flag) const
    {
        return (m_jitFlags & (1ULL << (unsigned __int64)flag)) != 0;
    }

    void Add(const JitFlags& other)
    {
        m_jitFlags |= other.m_jitFlags;
    }

    void Remove(const JitFlags& other)
    {
        m_jitFlags &= ~other.m_jitFlags;
    }

    bool IsEmpty() const
    {
        return m_jitFlags == 0;
    }

    void SetFromFlags(CORJIT_FLAGS flags)
    {
        // We don't want to have to check every one, so we assume it is exactly the same values as the JitFlag
        // values defined in this type.
        m_jitFlags = flags.GetFlagsRaw();

        C_ASSERT(sizeof(m_jitFlags) == sizeof(CORJIT_FLAGS));

#define FLAGS_EQUAL(a, b) C_ASSERT((unsigned)(a) == (unsigned)(b))

        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_SPEED_OPT, JIT_FLAG_SPEED_OPT);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_SIZE_OPT, JIT_FLAG_SIZE_OPT);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_DEBUG_CODE, JIT_FLAG_DEBUG_CODE);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_DEBUG_EnC, JIT_FLAG_DEBUG_EnC);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_DEBUG_INFO, JIT_FLAG_DEBUG_INFO);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_MIN_OPT, JIT_FLAG_MIN_OPT);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_GCPOLL_CALLS, JIT_FLAG_GCPOLL_CALLS);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_MCJIT_BACKGROUND, JIT_FLAG_MCJIT_BACKGROUND);

#if defined(_TARGET_X86_)

        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_PINVOKE_RESTORE_ESP, JIT_FLAG_PINVOKE_RESTORE_ESP);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_TARGET_P4, JIT_FLAG_TARGET_P4);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_USE_FCOMI, JIT_FLAG_USE_FCOMI);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_USE_CMOV, JIT_FLAG_USE_CMOV);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_USE_SSE2, JIT_FLAG_USE_SSE2);

#endif

#if defined(_TARGET_X86_) || defined(_TARGET_AMD64_)

        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_USE_AVX, JIT_FLAG_USE_AVX);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_USE_AVX2, JIT_FLAG_USE_AVX2);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_USE_AVX_512, JIT_FLAG_USE_AVX_512);

#endif

#if defined(_TARGET_X86_) || defined(_TARGET_AMD64_) || defined(_TARGET_ARM64_)

        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_FEATURE_SIMD, JIT_FLAG_FEATURE_SIMD);

#endif

        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_MAKEFINALCODE, JIT_FLAG_MAKEFINALCODE);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_READYTORUN, JIT_FLAG_READYTORUN);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_PROF_ENTERLEAVE, JIT_FLAG_PROF_ENTERLEAVE);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_PROF_REJIT_NOPS, JIT_FLAG_PROF_REJIT_NOPS);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_PROF_NO_PINVOKE_INLINE, JIT_FLAG_PROF_NO_PINVOKE_INLINE);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_SKIP_VERIFICATION, JIT_FLAG_SKIP_VERIFICATION);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_PREJIT, JIT_FLAG_PREJIT);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_RELOC, JIT_FLAG_RELOC);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_IMPORT_ONLY, JIT_FLAG_IMPORT_ONLY);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_IL_STUB, JIT_FLAG_IL_STUB);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_PROCSPLIT, JIT_FLAG_PROCSPLIT);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_BBINSTR, JIT_FLAG_BBINSTR);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_BBOPT, JIT_FLAG_BBOPT);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_FRAMED, JIT_FLAG_FRAMED);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_ALIGN_LOOPS, JIT_FLAG_ALIGN_LOOPS);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_PUBLISH_SECRET_PARAM, JIT_FLAG_PUBLISH_SECRET_PARAM);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_GCPOLL_INLINE, JIT_FLAG_GCPOLL_INLINE);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_SAMPLING_JIT_BACKGROUND, JIT_FLAG_SAMPLING_JIT_BACKGROUND);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_USE_PINVOKE_HELPERS, JIT_FLAG_USE_PINVOKE_HELPERS);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_REVERSE_PINVOKE, JIT_FLAG_REVERSE_PINVOKE);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_DESKTOP_QUIRKS, JIT_FLAG_DESKTOP_QUIRKS);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_TIER0, JIT_FLAG_TIER0);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_TIER1, JIT_FLAG_TIER1);

#if defined(_TARGET_ARM_)

        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_RELATIVE_CODE_RELOCS, JIT_FLAG_RELATIVE_CODE_RELOCS);

#endif // _TARGET_ARM_

        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_NO_INLINING, JIT_FLAG_NO_INLINING);

#if defined(_TARGET_ARM64_)

        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_AES, JIT_FLAG_HAS_ARM64_AES);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_ATOMICS, JIT_FLAG_HAS_ARM64_ATOMICS);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_CRC32, JIT_FLAG_HAS_ARM64_CRC32);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_DCPOP, JIT_FLAG_HAS_ARM64_DCPOP);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_DP, JIT_FLAG_HAS_ARM64_DP);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_FCMA, JIT_FLAG_HAS_ARM64_FCMA);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_FP, JIT_FLAG_HAS_ARM64_FP);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_FP16, JIT_FLAG_HAS_ARM64_FP16);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_JSCVT, JIT_FLAG_HAS_ARM64_JSCVT);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_LRCPC, JIT_FLAG_HAS_ARM64_LRCPC);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_PMULL, JIT_FLAG_HAS_ARM64_PMULL);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_SHA1, JIT_FLAG_HAS_ARM64_SHA1);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_SHA256, JIT_FLAG_HAS_ARM64_SHA256);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_SHA512, JIT_FLAG_HAS_ARM64_SHA512);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_SHA3, JIT_FLAG_HAS_ARM64_SHA3);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_ADVSIMD, JIT_FLAG_HAS_ARM64_ADVSIMD);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_ADVSIMD_V81, JIT_FLAG_HAS_ARM64_ADVSIMD_V81);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_ADVSIMD_FP16, JIT_FLAG_HAS_ARM64_ADVSIMD_FP16);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_SM3, JIT_FLAG_HAS_ARM64_SM3);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_SM4, JIT_FLAG_HAS_ARM64_SM4);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_SVE, JIT_FLAG_HAS_ARM64_SVE);

#elif defined(_TARGET_X86_) || defined(_TARGET_AMD64_)

        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_USE_SSE3, JIT_FLAG_USE_SSE3);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_USE_SSSE3, JIT_FLAG_USE_SSSE3);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_USE_SSE41, JIT_FLAG_USE_SSE41);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_USE_SSE42, JIT_FLAG_USE_SSE42);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_USE_AES, JIT_FLAG_USE_AES);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_USE_BMI1, JIT_FLAG_USE_BMI1);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_USE_BMI2, JIT_FLAG_USE_BMI2);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_USE_FMA, JIT_FLAG_USE_FMA);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_USE_LZCNT, JIT_FLAG_USE_LZCNT);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_USE_PCLMULQDQ, JIT_FLAG_USE_PCLMULQDQ);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_USE_POPCNT, JIT_FLAG_USE_POPCNT);

#endif // _TARGET_X86_ || _TARGET_AMD64_

#undef FLAGS_EQUAL
    }

private:
    unsigned __int64 m_jitFlags;
};
