// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This class wraps the CORJIT_FLAGS type in the JIT-EE interface (in corjit.h).
// If this changes, also change spmidumphelper.cpp.
class JitFlags
{
public:
    // clang-format off
    enum JitFlag
    {
        JIT_FLAG_SPEED_OPT               = 0, // optimize for speed
        JIT_FLAG_SIZE_OPT                = 1, // optimize for code size
        JIT_FLAG_DEBUG_CODE              = 2, // generate "debuggable" code (no code-mangling optimizations)
        JIT_FLAG_DEBUG_EnC               = 3, // We are in Edit-n-Continue mode
        JIT_FLAG_DEBUG_INFO              = 4, // generate line and local-var info
        JIT_FLAG_MIN_OPT                 = 5, // disable all jit optimizations (not necessarily debuggable code)
        JIT_FLAG_ENABLE_CFG              = 6, // generate CFG enabled code
        JIT_FLAG_OSR                     = 7, // Generate alternate version for On Stack Replacement
        JIT_FLAG_ALT_JIT                 = 8, // JIT should consider itself an ALT_JIT
        JIT_FLAG_FROZEN_ALLOC_ALLOWED    = 9, // JIT is allowed to use *_MAYBEFROZEN allocators
        JIT_FLAG_MAKEFINALCODE           = 10, // Use the final code generator, i.e., not the interpreter.
        JIT_FLAG_READYTORUN              = 11, // Use version-resilient code generation
        JIT_FLAG_PROF_ENTERLEAVE         = 12, // Instrument prologues/epilogues
        JIT_FLAG_PROF_NO_PINVOKE_INLINE  = 13, // Disables PInvoke inlining
        JIT_FLAG_PREJIT                  = 14, // prejit is the execution engine.
        JIT_FLAG_RELOC                   = 15, // Generate relocatable code
        JIT_FLAG_IL_STUB                 = 16, // method is an IL stub
        JIT_FLAG_PROCSPLIT               = 17, // JIT should separate code into hot and cold sections
        JIT_FLAG_BBINSTR                 = 18, // Collect basic block profile information
        JIT_FLAG_BBINSTR_IF_LOOPS        = 19, // JIT must instrument current method if it has loops
        JIT_FLAG_BBOPT                   = 20, // Optimize method based on profile information
        JIT_FLAG_FRAMED                  = 21, // All methods have an EBP frame
        JIT_FLAG_PUBLISH_SECRET_PARAM    = 22, // JIT must place stub secret param into local 0.  (used by IL stubs)
        JIT_FLAG_USE_PINVOKE_HELPERS     = 23, // The JIT should use the PINVOKE_{BEGIN,END} helpers instead of emitting inline transitions
        JIT_FLAG_REVERSE_PINVOKE         = 24, // The JIT should insert REVERSE_PINVOKE_{ENTER,EXIT} helpers into method prolog/epilog
        JIT_FLAG_TRACK_TRANSITIONS       = 25, // The JIT should insert the helper variants that track transitions.
        JIT_FLAG_TIER0                   = 26, // This is the initial tier for tiered compilation which should generate code as quickly as possible
        JIT_FLAG_TIER1                   = 27, // This is the final tier (for now) for tiered compilation which should generate high quality code
        JIT_FLAG_NO_INLINING             = 28, // JIT should not inline any called method into this method

#if defined(TARGET_ARM)
        JIT_FLAG_RELATIVE_CODE_RELOCS    = 29, // JIT should generate PC-relative address computations instead of EE relocation records
        JIT_FLAG_SOFTFP_ABI              = 30, // Enable armel calling convention
#endif

#if defined(TARGET_XARCH)
        JIT_FLAG_VECTOR512_THROTTLING    = 31, // On Xarch, 512-bit vector usage may incur CPU frequency throttling
#endif

        // Note: the mcs tool uses the currently unused upper flags bits when outputting SuperPMI MC file flags.
        // See EXTRA_JIT_FLAGS and spmidumphelper.cpp. Currently, these are bits 56 through 63. If they overlap,
        // something needs to change.
    };
    // clang-format on

    JitFlags()
        : m_jitFlags(0)
    {
        // empty
    }

    // Convenience constructor to set exactly one flags.
    JitFlags(JitFlag flag)
        : m_jitFlags(0)
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
        m_jitFlags |= 1ULL << (uint64_t)flag;
    }

    void Clear(JitFlag flag)
    {
        m_jitFlags &= ~(1ULL << (uint64_t)flag);
    }

    bool IsSet(JitFlag flag) const
    {
        return (m_jitFlags & (1ULL << (uint64_t)flag)) != 0;
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
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_OSR, JIT_FLAG_OSR);
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
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_USE_PINVOKE_HELPERS, JIT_FLAG_USE_PINVOKE_HELPERS);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_REVERSE_PINVOKE, JIT_FLAG_REVERSE_PINVOKE);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_TRACK_TRANSITIONS, JIT_FLAG_TRACK_TRANSITIONS);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_TIER0, JIT_FLAG_TIER0);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_TIER1, JIT_FLAG_TIER1);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_NO_INLINING, JIT_FLAG_NO_INLINING);

#if defined(TARGET_ARM)
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_RELATIVE_CODE_RELOCS, JIT_FLAG_RELATIVE_CODE_RELOCS);
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_SOFTFP_ABI, JIT_FLAG_SOFTFP_ABI);
#endif // TARGET_ARM

#if defined(TARGET_X86) || defined(TARGET_AMD64)
        FLAGS_EQUAL(CORJIT_FLAGS::CORJIT_FLAG_VECTOR512_THROTTLING, JIT_FLAG_VECTOR512_THROTTLING);
#endif // TARGET_ARM

#undef FLAGS_EQUAL
    }

private:
    uint64_t                    m_jitFlags;
    CORINFO_InstructionSetFlags m_instructionSetFlags;
};
