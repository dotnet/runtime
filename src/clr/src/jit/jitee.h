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

    #if defined(_TARGET_X86_) || defined(_TARGET_AMD64_)

        JIT_FLAG_USE_SSE3_4              = 13,
        JIT_FLAG_USE_AVX                 = 14,
        JIT_FLAG_USE_AVX2                = 15,
        JIT_FLAG_USE_AVX_512             = 16,
        JIT_FLAG_FEATURE_SIMD            = 17,

    #else // !defined(_TARGET_X86_) && !defined(_TARGET_AMD64_)

        JIT_FLAG_UNUSED6                 = 13,
        JIT_FLAG_UNUSED7                 = 14,
        JIT_FLAG_UNUSED8                 = 15,
        JIT_FLAG_UNUSED9                 = 16,
        JIT_FLAG_UNUSED10                = 17,

    #endif // !defined(_TARGET_X86_) && !defined(_TARGET_AMD64_)

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

#if COR_JIT_EE_VERSION <= 460

    void SetFromOldFlags(unsigned corJitFlags, unsigned corJitFlags2)
    {
        Reset();

#define CONVERT_OLD_FLAG(oldf, newf)                                                                                   \
    if ((corJitFlags & (oldf)) != 0)                                                                                   \
        this->Set(JitFlags::newf);
#define CONVERT_OLD_FLAG2(oldf, newf)                                                                                  \
    if ((corJitFlags2 & (oldf)) != 0)                                                                                  \
        this->Set(JitFlags::newf);

        CONVERT_OLD_FLAG(CORJIT_FLG_SPEED_OPT, JIT_FLAG_SPEED_OPT)
        CONVERT_OLD_FLAG(CORJIT_FLG_SIZE_OPT, JIT_FLAG_SIZE_OPT)
        CONVERT_OLD_FLAG(CORJIT_FLG_DEBUG_CODE, JIT_FLAG_DEBUG_CODE)
        CONVERT_OLD_FLAG(CORJIT_FLG_DEBUG_EnC, JIT_FLAG_DEBUG_EnC)
        CONVERT_OLD_FLAG(CORJIT_FLG_DEBUG_INFO, JIT_FLAG_DEBUG_INFO)
        CONVERT_OLD_FLAG(CORJIT_FLG_MIN_OPT, JIT_FLAG_MIN_OPT)
        CONVERT_OLD_FLAG(CORJIT_FLG_GCPOLL_CALLS, JIT_FLAG_GCPOLL_CALLS)
        CONVERT_OLD_FLAG(CORJIT_FLG_MCJIT_BACKGROUND, JIT_FLAG_MCJIT_BACKGROUND)

#if defined(_TARGET_X86_)

        CONVERT_OLD_FLAG(CORJIT_FLG_PINVOKE_RESTORE_ESP, JIT_FLAG_PINVOKE_RESTORE_ESP)
        CONVERT_OLD_FLAG(CORJIT_FLG_TARGET_P4, JIT_FLAG_TARGET_P4)
        CONVERT_OLD_FLAG(CORJIT_FLG_USE_FCOMI, JIT_FLAG_USE_FCOMI)
        CONVERT_OLD_FLAG(CORJIT_FLG_USE_CMOV, JIT_FLAG_USE_CMOV)
        CONVERT_OLD_FLAG(CORJIT_FLG_USE_SSE2, JIT_FLAG_USE_SSE2)

#elif defined(_TARGET_AMD64_)

        CONVERT_OLD_FLAG(CORJIT_FLG_USE_SSE3_4, JIT_FLAG_USE_SSE3_4)
        CONVERT_OLD_FLAG(CORJIT_FLG_USE_AVX, JIT_FLAG_USE_AVX)
        CONVERT_OLD_FLAG(CORJIT_FLG_USE_AVX2, JIT_FLAG_USE_AVX2)
        CONVERT_OLD_FLAG(CORJIT_FLG_USE_AVX_512, JIT_FLAG_USE_AVX_512)
        CONVERT_OLD_FLAG(CORJIT_FLG_FEATURE_SIMD, JIT_FLAG_FEATURE_SIMD)

#endif // !defined(_TARGET_X86_) && !defined(_TARGET_AMD64_)

        CONVERT_OLD_FLAG(CORJIT_FLG_MAKEFINALCODE, JIT_FLAG_MAKEFINALCODE)
        CONVERT_OLD_FLAG(CORJIT_FLG_READYTORUN, JIT_FLAG_READYTORUN)
        CONVERT_OLD_FLAG(CORJIT_FLG_PROF_ENTERLEAVE, JIT_FLAG_PROF_ENTERLEAVE)
        CONVERT_OLD_FLAG(CORJIT_FLG_PROF_REJIT_NOPS, JIT_FLAG_PROF_REJIT_NOPS)
        CONVERT_OLD_FLAG(CORJIT_FLG_PROF_NO_PINVOKE_INLINE, JIT_FLAG_PROF_NO_PINVOKE_INLINE)
        CONVERT_OLD_FLAG(CORJIT_FLG_SKIP_VERIFICATION, JIT_FLAG_SKIP_VERIFICATION)
        CONVERT_OLD_FLAG(CORJIT_FLG_PREJIT, JIT_FLAG_PREJIT)
        CONVERT_OLD_FLAG(CORJIT_FLG_RELOC, JIT_FLAG_RELOC)
        CONVERT_OLD_FLAG(CORJIT_FLG_IMPORT_ONLY, JIT_FLAG_IMPORT_ONLY)
        CONVERT_OLD_FLAG(CORJIT_FLG_IL_STUB, JIT_FLAG_IL_STUB)
        CONVERT_OLD_FLAG(CORJIT_FLG_PROCSPLIT, JIT_FLAG_PROCSPLIT)
        CONVERT_OLD_FLAG(CORJIT_FLG_BBINSTR, JIT_FLAG_BBINSTR)
        CONVERT_OLD_FLAG(CORJIT_FLG_BBOPT, JIT_FLAG_BBOPT)
        CONVERT_OLD_FLAG(CORJIT_FLG_FRAMED, JIT_FLAG_FRAMED)
        CONVERT_OLD_FLAG(CORJIT_FLG_ALIGN_LOOPS, JIT_FLAG_ALIGN_LOOPS)
        CONVERT_OLD_FLAG(CORJIT_FLG_PUBLISH_SECRET_PARAM, JIT_FLAG_PUBLISH_SECRET_PARAM)
        CONVERT_OLD_FLAG(CORJIT_FLG_GCPOLL_INLINE, JIT_FLAG_GCPOLL_INLINE)

        CONVERT_OLD_FLAG2(CORJIT_FLG2_SAMPLING_JIT_BACKGROUND, JIT_FLAG_SAMPLING_JIT_BACKGROUND)

#undef CONVERT_OLD_FLAG
#undef CONVERT_OLD_FLAG2
    }

#else // COR_JIT_EE_VERSION > 460

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

        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_USE_SSE3_4, JIT_FLAG_USE_SSE3_4);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_USE_AVX, JIT_FLAG_USE_AVX);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_USE_AVX2, JIT_FLAG_USE_AVX2);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_USE_AVX_512, JIT_FLAG_USE_AVX_512);
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

#undef FLAGS_EQUAL
    }

#endif // COR_JIT_EE_VERSION > 460

private:
    unsigned __int64 m_jitFlags;
};
