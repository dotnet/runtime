// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        JIT_FLAG_MIN_OPT                 = 5, // disable all jit optimizations (not necessarily debuggable code)
        JIT_FLAG_ENABLE_CFG              = 6, // generate CFG enabled code
        JIT_FLAG_MCJIT_BACKGROUND        = 7, // Calling from multicore JIT background thread, do not call JitComplete

    #if defined(TARGET_X86)
        JIT_FLAG_PINVOKE_RESTORE_ESP     = 8, // Restore ESP after returning from inlined PInvoke
    #else // !defined(TARGET_X86)
        JIT_FLAG_UNUSED2                 = 8,
    #endif // !defined(TARGET_X86)

        JIT_FLAG_UNUSED3                 = 9,
        JIT_FLAG_UNUSED4                 = 10,
        JIT_FLAG_UNUSED5                 = 11,
        JIT_FLAG_UNUSED6                 = 12,

        JIT_FLAG_OSR                     = 13, // Generate alternate version for On Stack Replacement

        JIT_FLAG_ALT_JIT                 = 14, // JIT should consider itself an ALT_JIT
        JIT_FLAG_FROZEN_ALLOC_ALLOWED    = 15, // JIT is allowed to use *_MAYBEFROZEN allocators
        JIT_FLAG_UNUSED9                 = 16,

    #if defined(TARGET_X86) || defined(TARGET_AMD64) || defined(TARGET_ARM64)
        JIT_FLAG_FEATURE_SIMD            = 17,
    #else
        JIT_FLAG_UNUSED10                = 17,
    #endif // !(defined(TARGET_X86) || defined(TARGET_AMD64) || defined(TARGET_ARM64))

        JIT_FLAG_MAKEFINALCODE           = 18, // Use the final code generator, i.e., not the interpreter.
        JIT_FLAG_READYTORUN              = 19, // Use version-resilient code generation
        JIT_FLAG_PROF_ENTERLEAVE         = 20, // Instrument prologues/epilogues
        JIT_FLAG_UNUSED11                = 21,
        JIT_FLAG_PROF_NO_PINVOKE_INLINE  = 22, // Disables PInvoke inlining
        JIT_FLAG_UNUSED12                = 23,
        JIT_FLAG_PREJIT                  = 24, // jit or prejit is the execution engine.
        JIT_FLAG_RELOC                   = 25, // Generate relocatable code
        JIT_FLAG_IMPORT_ONLY             = 26, // Only import the function
        JIT_FLAG_IL_STUB                 = 27, // method is an IL stub
        JIT_FLAG_PROCSPLIT               = 28, // JIT should separate code into hot and cold sections
        JIT_FLAG_BBINSTR                 = 29, // Collect basic block profile information
        JIT_FLAG_BBOPT                   = 30, // Optimize method based on profile information
        JIT_FLAG_FRAMED                  = 31, // All methods have an EBP frame
        JIT_FLAG_BBINSTR_IF_LOOPS        = 32, // JIT must instrument current method if it has loops
        JIT_FLAG_PUBLISH_SECRET_PARAM    = 33, // JIT must place stub secret param into local 0.  (used by IL stubs)
        JIT_FLAG_UNUSED13                = 34,
        JIT_FLAG_SAMPLING_JIT_BACKGROUND = 35, // JIT is being invoked as a result of stack sampling for hot methods in the background
        JIT_FLAG_USE_PINVOKE_HELPERS     = 36, // The JIT should use the PINVOKE_{BEGIN,END} helpers instead of emitting inline transitions
        JIT_FLAG_REVERSE_PINVOKE         = 37, // The JIT should insert REVERSE_PINVOKE_{ENTER,EXIT} helpers into method prolog/epilog
        JIT_FLAG_TRACK_TRANSITIONS       = 38, // The JIT should insert the helper variants that track transitions.
        JIT_FLAG_TIER0                   = 39, // This is the initial tier for tiered compilation which should generate code as quickly as possible
        JIT_FLAG_TIER1                   = 40, // This is the final tier (for now) for tiered compilation which should generate high quality code

#if defined(TARGET_ARM)
        JIT_FLAG_RELATIVE_CODE_RELOCS    = 41, // JIT should generate PC-relative address computations instead of EE relocation records
#else // !defined(TARGET_ARM)
        JIT_FLAG_UNUSED15                = 41,
#endif // !defined(TARGET_ARM)

        JIT_FLAG_NO_INLINING             = 42, // JIT should not inline any called method into this method

#if defined(TARGET_ARM)
        JIT_FLAG_SOFTFP_ABI              = 43, // On ARM should enable armel calling convention
#else // !defined(TARGET_ARM)
        JIT_FLAG_UNUSED16                = 43,
#endif // !defined(TARGET_ARM)

        JIT_FLAG_UNUSED17                = 44,
        JIT_FLAG_UNUSED18                = 45,
        JIT_FLAG_UNUSED19                = 46,
        JIT_FLAG_UNUSED20                = 47,
        JIT_FLAG_UNUSED21                = 48,
        JIT_FLAG_UNUSED22                = 49,
        JIT_FLAG_UNUSED23                = 50,
        JIT_FLAG_UNUSED24                = 51,
        JIT_FLAG_UNUSED25                = 52,
        JIT_FLAG_UNUSED26                = 53,
        JIT_FLAG_UNUSED27                = 54,
        JIT_FLAG_UNUSED28                = 55,
        JIT_FLAG_UNUSED29                = 56,
        JIT_FLAG_UNUSED30                = 57,
        JIT_FLAG_UNUSED31                = 58,
        JIT_FLAG_UNUSED32                = 59,
        JIT_FLAG_UNUSED33                = 60,
        JIT_FLAG_UNUSED34                = 61,
        JIT_FLAG_UNUSED35                = 62,
        JIT_FLAG_UNUSED36                = 63

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

    CORINFO_InstructionSetFlags GetInstructionSetFlags() const
    {
        return m_instructionSetFlags;
    }

    void SetInstructionSetFlags(CORINFO_InstructionSetFlags instructionSetFlags)
    {
        m_instructionSetFlags = instructionSetFlags;
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

    bool IsEmpty() const
    {
        return m_jitFlags == 0;
    }

    void SetFromFlags(CORJIT_FLAGS flags)
    {
        // We don't want to have to check every one, so we assume it is exactly the same values as the JitFlag
        // values defined in this type.
        m_jitFlags            = flags.GetFlagsRaw();
        m_instructionSetFlags = flags.GetInstructionSetFlags();

        C_ASSERT(sizeof(JitFlags) == sizeof(CORJIT_FLAGS));

#define FLAGS_EQUAL(a, b) C_ASSERT((unsigned)(a) == (unsigned)(b))

        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_SPEED_OPT, JIT_FLAG_SPEED_OPT);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_SIZE_OPT, JIT_FLAG_SIZE_OPT);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_DEBUG_CODE, JIT_FLAG_DEBUG_CODE);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_DEBUG_EnC, JIT_FLAG_DEBUG_EnC);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_DEBUG_INFO, JIT_FLAG_DEBUG_INFO);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_MIN_OPT, JIT_FLAG_MIN_OPT);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_ENABLE_CFG, JIT_FLAG_ENABLE_CFG);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_MCJIT_BACKGROUND, JIT_FLAG_MCJIT_BACKGROUND);

#if defined(TARGET_X86)
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_PINVOKE_RESTORE_ESP, JIT_FLAG_PINVOKE_RESTORE_ESP);
#endif

        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_ALT_JIT, JIT_FLAG_ALT_JIT);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_FROZEN_ALLOC_ALLOWED, JIT_FLAG_FROZEN_ALLOC_ALLOWED);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_MAKEFINALCODE, JIT_FLAG_MAKEFINALCODE);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_READYTORUN, JIT_FLAG_READYTORUN);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_PROF_ENTERLEAVE, JIT_FLAG_PROF_ENTERLEAVE);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_PROF_NO_PINVOKE_INLINE, JIT_FLAG_PROF_NO_PINVOKE_INLINE);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_PREJIT, JIT_FLAG_PREJIT);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_RELOC, JIT_FLAG_RELOC);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_IL_STUB, JIT_FLAG_IL_STUB);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_PROCSPLIT, JIT_FLAG_PROCSPLIT);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_BBINSTR, JIT_FLAG_BBINSTR);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_BBINSTR_IF_LOOPS, JIT_FLAG_BBINSTR_IF_LOOPS);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_BBOPT, JIT_FLAG_BBOPT);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_FRAMED, JIT_FLAG_FRAMED);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_PUBLISH_SECRET_PARAM, JIT_FLAG_PUBLISH_SECRET_PARAM);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_SAMPLING_JIT_BACKGROUND, JIT_FLAG_SAMPLING_JIT_BACKGROUND);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_USE_PINVOKE_HELPERS, JIT_FLAG_USE_PINVOKE_HELPERS);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_REVERSE_PINVOKE, JIT_FLAG_REVERSE_PINVOKE);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_TIER0, JIT_FLAG_TIER0);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_TIER1, JIT_FLAG_TIER1);

#if defined(TARGET_ARM)
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_RELATIVE_CODE_RELOCS, JIT_FLAG_RELATIVE_CODE_RELOCS);
#endif // TARGET_ARM

        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_NO_INLINING, JIT_FLAG_NO_INLINING);

#if defined(TARGET_ARM)
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_SOFTFP_ABI, JIT_FLAG_SOFTFP_ABI);
#endif // TARGET_ARM

#undef FLAGS_EQUAL
    }

private:
    unsigned __int64            m_jitFlags;
    CORINFO_InstructionSetFlags m_instructionSetFlags;
};
