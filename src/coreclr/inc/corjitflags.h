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

    enum CorJitFlag
    {
        CORJIT_FLAG_CALL_GETJITFLAGS        = 0xffffffff, // Indicates that the JIT should retrieve flags in the form of a
                                                          // pointer to a CORJIT_FLAGS value via ICorJitInfo::getJitFlags().
        CORJIT_FLAG_SPEED_OPT               = 0,
        CORJIT_FLAG_SIZE_OPT                = 1,
        CORJIT_FLAG_DEBUG_CODE              = 2, // generate "debuggable" code (no code-mangling optimizations)
        CORJIT_FLAG_DEBUG_EnC               = 3, // We are in Edit-n-Continue mode
        CORJIT_FLAG_DEBUG_INFO              = 4, // generate line and local-var info
        CORJIT_FLAG_MIN_OPT                 = 5, // disable all jit optimizations (not necessarily debuggable code)
        CORJIT_FLAG_ENABLE_CFG              = 6, // generate control-flow guard checks
        CORJIT_FLAG_MCJIT_BACKGROUND        = 7, // Calling from multicore JIT background thread, do not call JitComplete

    #if defined(TARGET_X86)
        CORJIT_FLAG_PINVOKE_RESTORE_ESP     = 8, // Restore ESP after returning from inlined PInvoke
    #else // !defined(TARGET_X86)
        CORJIT_FLAG_UNUSED2                 = 8,
    #endif // !defined(TARGET_X86)

        CORJIT_FLAG_UNUSED3                 = 9,
        CORJIT_FLAG_UNUSED4                 = 10,
        CORJIT_FLAG_UNUSED5                 = 11,
        CORJIT_FLAG_UNUSED6                 = 12,

        CORJIT_FLAG_OSR                     = 13, // Generate alternate method for On Stack Replacement

        CORJIT_FLAG_ALT_JIT                 = 14, // JIT should consider itself an ALT_JIT
        CORJIT_FLAG_FROZEN_ALLOC_ALLOWED    = 15, // JIT is allowed to use *_MAYBEFROZEN allocators
        CORJIT_FLAG_UNUSED9                 = 16,
        CORJIT_FLAG_UNUSED10                = 17,

        CORJIT_FLAG_MAKEFINALCODE           = 18, // Use the final code generator, i.e., not the interpreter.
        CORJIT_FLAG_READYTORUN              = 19, // Use version-resilient code generation
        CORJIT_FLAG_PROF_ENTERLEAVE         = 20, // Instrument prologues/epilogues
        CORJIT_FLAG_UNUSED11                = 21,
        CORJIT_FLAG_PROF_NO_PINVOKE_INLINE  = 22, // Disables PInvoke inlining
        CORJIT_FLAG_UNUSED12                = 23,
        CORJIT_FLAG_PREJIT                  = 24, // jit or prejit is the execution engine.
        CORJIT_FLAG_RELOC                   = 25, // Generate relocatable code
        CORJIT_FLAG_UNUSED13                = 26,
        CORJIT_FLAG_IL_STUB                 = 27, // method is an IL stub
        CORJIT_FLAG_PROCSPLIT               = 28, // JIT should separate code into hot and cold sections
        CORJIT_FLAG_BBINSTR                 = 29, // Collect basic block profile information
        CORJIT_FLAG_BBOPT                   = 30, // Optimize method based on profile information
        CORJIT_FLAG_FRAMED                  = 31, // All methods have an EBP frame
        CORJIT_FLAG_BBINSTR_IF_LOOPS        = 32, // JIT must instrument current method if it has loops
        CORJIT_FLAG_PUBLISH_SECRET_PARAM    = 33, // JIT must place stub secret param into local 0.  (used by IL stubs)
        CORJIT_FLAG_UNUSED14                = 34,
        CORJIT_FLAG_SAMPLING_JIT_BACKGROUND = 35, // JIT is being invoked as a result of stack sampling for hot methods in the background
        CORJIT_FLAG_USE_PINVOKE_HELPERS     = 36, // The JIT should use the PINVOKE_{BEGIN,END} helpers instead of emitting inline transitions
        CORJIT_FLAG_REVERSE_PINVOKE         = 37, // The JIT should insert REVERSE_PINVOKE_{ENTER,EXIT} helpers into method prolog/epilog
        CORJIT_FLAG_TRACK_TRANSITIONS       = 38, // The JIT should insert the REVERSE_PINVOKE helper variants that track transitions.
        CORJIT_FLAG_TIER0                   = 39, // This is the initial tier for tiered compilation which should generate code as quickly as possible
        CORJIT_FLAG_TIER1                   = 40, // This is the final tier (for now) for tiered compilation which should generate high quality code

#if defined(TARGET_ARM)
        CORJIT_FLAG_RELATIVE_CODE_RELOCS    = 41, // JIT should generate PC-relative address computations instead of EE relocation records
#else // !defined(TARGET_ARM)
        CORJIT_FLAG_UNUSED15                = 41,
#endif // !defined(TARGET_ARM)

        CORJIT_FLAG_NO_INLINING             = 42, // JIT should not inline any called method into this method

#if defined(TARGET_ARM)
        CORJIT_FLAG_SOFTFP_ABI              = 43, // On ARM should enable armel calling convention
#elif defined(TARGET_X86) || defined(TARGET_AMD64)
        CORJIT_FLAG_VECTOR512_THROTTLING    = 43, // On Xarch, 512-bit vector usage may incur CPU frequency throttling
#else
        CORJIT_FLAG_UNUSED16                = 43,
#endif // !defined(TARGET_ARM)

        CORJIT_FLAG_UNUSED17                = 44,
        CORJIT_FLAG_UNUSED18                = 45,
        CORJIT_FLAG_UNUSED19                = 46,
        CORJIT_FLAG_UNUSED20                = 47,
        CORJIT_FLAG_UNUSED21                = 48,
        CORJIT_FLAG_UNUSED22                = 49,
        CORJIT_FLAG_UNUSED23                = 50,
        CORJIT_FLAG_UNUSED24                = 51,
        CORJIT_FLAG_UNUSED25                = 52,
        CORJIT_FLAG_UNUSED26                = 53,
        CORJIT_FLAG_UNUSED27                = 54,
        CORJIT_FLAG_UNUSED28                = 55,
        CORJIT_FLAG_UNUSED29                = 56,
        CORJIT_FLAG_UNUSED30                = 57,
        CORJIT_FLAG_UNUSED31                = 58,
        CORJIT_FLAG_UNUSED32                = 59,
        CORJIT_FLAG_UNUSED33                = 60,
        CORJIT_FLAG_UNUSED34                = 61,
        CORJIT_FLAG_UNUSED35                = 62,
        CORJIT_FLAG_UNUSED36                = 63
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
