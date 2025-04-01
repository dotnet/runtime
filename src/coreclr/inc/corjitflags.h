// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

#include "corinfoinstructionset.h"

class CORJIT_FLAGS
{
public:

    // Note: these flags can be #ifdef'ed, but no number should be re-used between different #ifdef conditions,
    // so platform-independent code can know uniquely which number corresponds to which flag.
    enum CorJitFlag
    {
        CORJIT_FLAG_CALL_GETJITFLAGS        = 0xffffffff, // Indicates that the JIT should retrieve flags in the form of a
                                                          // pointer to a CORJIT_FLAGS value via ICorJitInfo::getJitFlags().

        CORJIT_FLAG_SPEED_OPT               = 0, // optimize for speed
        CORJIT_FLAG_SIZE_OPT                = 1, // optimize for code size
        CORJIT_FLAG_DEBUG_CODE              = 2, // generate "debuggable" code (no code-mangling optimizations)
        CORJIT_FLAG_DEBUG_EnC               = 3, // We are in Edit-n-Continue mode
        CORJIT_FLAG_DEBUG_INFO              = 4, // generate line and local-var info
        CORJIT_FLAG_MIN_OPT                 = 5, // disable all jit optimizations (not necessarily debuggable code)
        CORJIT_FLAG_ENABLE_CFG              = 6, // generate CFG enabled code
        CORJIT_FLAG_OSR                     = 7, // Generate alternate version for On Stack Replacement
        CORJIT_FLAG_ALT_JIT                 = 8, // JIT should consider itself an ALT_JIT
        CORJIT_FLAG_FROZEN_ALLOC_ALLOWED    = 9, // JIT is allowed to use *_MAYBEFROZEN allocators
        // CORJIT_FLAG_UNUSED               = 10,
        CORJIT_FLAG_AOT                     = 11, // Do ahead-of-time code generation (ReadyToRun or NativeAOT)
        CORJIT_FLAG_PROF_ENTERLEAVE         = 12, // Instrument prologues/epilogues
        CORJIT_FLAG_PROF_NO_PINVOKE_INLINE  = 13, // Disables PInvoke inlining
        // CORJIT_FLAG_UNUSED               = 14,
        CORJIT_FLAG_RELOC                   = 15, // Generate relocatable code
        CORJIT_FLAG_IL_STUB                 = 16, // method is an IL stub
        CORJIT_FLAG_PROCSPLIT               = 17, // JIT should separate code into hot and cold sections
        CORJIT_FLAG_BBINSTR                 = 18, // Collect basic block profile information
        CORJIT_FLAG_BBINSTR_IF_LOOPS        = 19, // JIT must instrument current method if it has loops
        CORJIT_FLAG_BBOPT                   = 20, // Optimize method based on profile information
        CORJIT_FLAG_FRAMED                  = 21, // All methods have an EBP frame
        CORJIT_FLAG_PUBLISH_SECRET_PARAM    = 22, // JIT must place stub secret param into local 0.  (used by IL stubs)
        CORJIT_FLAG_USE_PINVOKE_HELPERS     = 23, // The JIT should use the PINVOKE_{BEGIN,END} helpers instead of emitting inline transitions
        CORJIT_FLAG_REVERSE_PINVOKE         = 24, // The JIT should insert REVERSE_PINVOKE_{ENTER,EXIT} helpers into method prolog/epilog
        CORJIT_FLAG_TRACK_TRANSITIONS       = 25, // The JIT should insert the helper variants that track transitions.
        CORJIT_FLAG_TIER0                   = 26, // This is the initial tier for tiered compilation which should generate code as quickly as possible
        CORJIT_FLAG_TIER1                   = 27, // This is the final tier (for now) for tiered compilation which should generate high quality code
        CORJIT_FLAG_NO_INLINING             = 28, // JIT should not inline any called method into this method

#if defined(TARGET_ARM)
        CORJIT_FLAG_RELATIVE_CODE_RELOCS    = 29, // JIT should generate PC-relative address computations instead of EE relocation records
        CORJIT_FLAG_SOFTFP_ABI              = 30, // Enable armel calling convention
#endif

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
        instructionSetFlags = other.instructionSetFlags;
    }

    void Reset()
    {
        corJitFlags = 0;
        instructionSetFlags.Reset();
    }

    void Set(CORINFO_InstructionSet instructionSet)
    {
        instructionSetFlags.AddInstructionSet(instructionSet);
    }

    bool IsSet(CORINFO_InstructionSet instructionSet) const
    {
        return instructionSetFlags.HasInstructionSet(instructionSet);
    }

    void Clear(CORINFO_InstructionSet instructionSet)
    {
        instructionSetFlags.RemoveInstructionSet(instructionSet);
    }

    void Set64BitInstructionSetVariants()
    {
        instructionSetFlags.Set64BitInstructionSetVariants();
    }

    void Set(CorJitFlag flag)
    {
        corJitFlags |= 1ULL << (uint64_t)flag;
    }

    void Clear(CorJitFlag flag)
    {
        corJitFlags &= ~(1ULL << (uint64_t)flag);
    }

    bool IsSet(CorJitFlag flag) const
    {
        return (corJitFlags & (1ULL << (uint64_t)flag)) != 0;
    }

    void Add(const CORJIT_FLAGS& other)
    {
        corJitFlags |= other.corJitFlags;
        instructionSetFlags.Add(other.instructionSetFlags);
    }

    bool IsEmpty() const
    {
        return corJitFlags == 0 && instructionSetFlags.IsEmpty();
    }

    void EnsureValidInstructionSetSupport()
    {
        instructionSetFlags = EnsureInstructionSetFlagsAreValid(instructionSetFlags);
    }

    // DO NOT USE THIS FUNCTION! (except in very restricted special cases)
    uint64_t GetFlagsRaw()
    {
        return corJitFlags;
    }

    // DO NOT USE THIS FUNCTION! (except in very restricted special cases)
    uint64_t* GetInstructionSetFlagsRaw()
    {
        return instructionSetFlags.GetFlagsRaw();
    }

    CORINFO_InstructionSetFlags GetInstructionSetFlags()
    {
        return instructionSetFlags;
    }

    const int GetInstructionFlagsFieldCount()
    {
        return instructionSetFlags.GetInstructionFlagsFieldCount();
    }

private:

    uint64_t corJitFlags;
    CORINFO_InstructionSetFlags instructionSetFlags;
};


#endif // _COR_JIT_FLAGS_H_
