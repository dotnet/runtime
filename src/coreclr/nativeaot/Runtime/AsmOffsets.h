// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// This file is used by AsmOffsets.cpp to validate that our
// assembly-code offsets always match their C++ counterparts.

// You must #define PLAT_ASM_OFFSET and PLAT_ASM_SIZEOF before you #include this file

#ifdef HOST_64BIT
#define ASM_OFFSET(offset32, offset64, cls, member) PLAT_ASM_OFFSET(offset64, cls, member)
#define ASM_SIZEOF(sizeof32, sizeof64, cls        ) PLAT_ASM_SIZEOF(sizeof64, cls)
#define ASM_CONST(const32, const64, expr)           PLAT_ASM_CONST(const64, expr)
#else
#define ASM_OFFSET(offset32, offset64, cls, member) PLAT_ASM_OFFSET(offset32, cls, member)
#define ASM_SIZEOF(sizeof32, sizeof64, cls        ) PLAT_ASM_SIZEOF(sizeof32, cls)
#define ASM_CONST(const32, const64, expr)           PLAT_ASM_CONST(const32, expr)
#endif

// NOTE: the values MUST be in hex notation WITHOUT the 0x prefix

//        32-bit,64-bit, constant symbol
ASM_CONST(   400,   800, CLUMP_SIZE)
ASM_CONST(     a,     b, LOG2_CLUMP_SIZE)

//        32-bit,64-bit, class, member
ASM_OFFSET(    0,     0, Object, m_pEEType)

ASM_OFFSET(    4,     8, Array, m_Length)

ASM_OFFSET(    4,     8, String, m_Length)
ASM_OFFSET(    8,     C, String, m_FirstChar)
ASM_CONST(     2,     2, STRING_COMPONENT_SIZE)
ASM_CONST(     E,    16, STRING_BASE_SIZE)
ASM_CONST(3FFFFFDF,3FFFFFDF,MAX_STRING_LENGTH)

ASM_OFFSET(    0,     0, MethodTable, m_usComponentSize)
ASM_OFFSET(    0,     0, MethodTable, m_uFlags)
ASM_OFFSET(    4,     4, MethodTable, m_uBaseSize)
ASM_OFFSET(   14,    18, MethodTable, m_VTable)

ASM_OFFSET(    0,     0, Thread, m_rgbAllocContextBuffer)
ASM_OFFSET(   28,    38, Thread, m_ThreadStateFlags)
ASM_OFFSET(   2c,    40, Thread, m_pTransitionFrame)
ASM_OFFSET(   30,    48, Thread, m_pDeferredTransitionFrame)
ASM_OFFSET(   40,    68, Thread, m_ppvHijackedReturnAddressLocation)
ASM_OFFSET(   44,    70, Thread, m_pvHijackedReturnAddress)
#ifdef HOST_64BIT
ASM_OFFSET(    0,    78, Thread, m_uHijackedReturnValueFlags)
#endif
ASM_OFFSET(   48,    80, Thread, m_pExInfoStackHead)
ASM_OFFSET(   4c,    88, Thread, m_threadAbortException)

ASM_OFFSET(   50,    90, Thread, m_pThreadLocalModuleStatics)
ASM_OFFSET(   54,    98, Thread, m_numThreadLocalModuleStatics)

ASM_SIZEOF(   14,    20, EHEnum)

ASM_OFFSET(    0,     0, gc_alloc_context, alloc_ptr)
ASM_OFFSET(    4,     8, gc_alloc_context, alloc_limit)

#ifdef FEATURE_CACHED_INTERFACE_DISPATCH
ASM_OFFSET(    4,     8, InterfaceDispatchCell, m_pCache)
#ifndef HOST_64BIT
ASM_OFFSET(    8,     0, InterfaceDispatchCache, m_pCell)
#endif
ASM_OFFSET(   10,    20, InterfaceDispatchCache, m_rgEntries)
ASM_SIZEOF(    8,    10, InterfaceDispatchCacheEntry)
#endif

#ifdef FEATURE_DYNAMIC_CODE
ASM_OFFSET(    0,     0, CallDescrData, pSrc)
ASM_OFFSET(    4,     8, CallDescrData, numStackSlots)
ASM_OFFSET(    8,     C, CallDescrData, fpReturnSize)
ASM_OFFSET(    C,    10, CallDescrData, pArgumentRegisters)
ASM_OFFSET(   10,    18, CallDescrData, pFloatArgumentRegisters)
ASM_OFFSET(   14,    20, CallDescrData, pTarget)
ASM_OFFSET(   18,    28, CallDescrData, pReturnBuffer)
#endif

// Undefine macros that are only used in this header for convenience.
#undef ASM_OFFSET
#undef ASM_SIZEOF
#undef ASM_CONST

// Define platform specific offsets
#include "AsmOffsetsCpu.h"

//#define USE_COMPILE_TIME_CONSTANT_FINDER // Uncomment this line to use the constant finder
#if defined(__cplusplus) && defined(USE_COMPILE_TIME_CONSTANT_FINDER)
// This class causes the compiler to emit an error with the constant we're interested in
// in the error message. This is useful if a size or offset changes. To use, comment out
// the compile-time assert that is firing, enable the constant finder, add the appropriate
// constant to find to BogusFunction(), and build.
//
// Here's a sample compiler error:
// In file included from nativeaot/Runtime/AsmOffsetsVerify.cpp:38:
// nativeaot/Runtime/Full/../AsmOffsets.h:117:61: error: calling a private constructor of class
//      'AsmOffsets::FindCompileTimeConstant<25>'
//    FindCompileTimeConstant<offsetof(ExInfo, m_passNumber)> bogus_variable;
//                                                            ^
// nativeaot/Runtime/Full/../AsmOffsets.h:111:5: note: declared private here
//    FindCompileTimeConstant();
//    ^
template<size_t N>
class FindCompileTimeConstant
{
private:
    FindCompileTimeConstant();
};

void BogusFunction()
{
    // Sample usage to generate the error
    FindCompileTimeConstant<sizeof(ExInfo)> bogus_variable;
    FindCompileTimeConstant<offsetof(ExInfo, m_notifyDebuggerSP)> bogus_variable2;
    FindCompileTimeConstant<sizeof(StackFrameIterator)> bogus_variable3;
    FindCompileTimeConstant<sizeof(PAL_LIMITED_CONTEXT)> bogus_variable4;
    FindCompileTimeConstant<offsetof(PAL_LIMITED_CONTEXT, IP)> bogus_variable5;
}
#endif // defined(__cplusplus) && defined(USE_COMPILE_TIME_CONSTANT_FINDER)
