// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// asmconstants.h -
//
// This header defines field offsets and constants used by assembly code
// Be sure to rebuild clr/src/vm/ceemain.cpp after changing this file, to
// ensure that the constants match the expected C/C++ values

// 
// If you need to figure out a constant that has changed and is causing
// a compile-time assert, check out USE_COMPILE_TIME_CONSTANT_FINDER.
// TODO: put the constant finder in a common place so other platforms can use it.

#ifndef _TARGET_X86_ 
#error this file should only be used on an X86 platform
#endif

#include "../../inc/switches.h"

#ifndef ASMCONSTANTS_C_ASSERT
#define ASMCONSTANTS_C_ASSERT(cond)
#endif

#ifndef ASMCONSTANTS_RUNTIME_ASSERT
#define ASMCONSTANTS_RUNTIME_ASSERT(cond)
#endif

// Some contants are different in _DEBUG builds.  This macro factors out ifdefs from below.
#ifdef _DEBUG
#define DBG_FRE(dbg,fre) dbg
#else
#define DBG_FRE(dbg,fre) fre
#endif

//***************************************************************************
#if defined(_DEBUG) && defined(_TARGET_X86_) && !defined(FEATURE_CORECLR)
 #define HAS_TRACK_CXX_EXCEPTION_CODE_HACK 1
 #define TRACK_CXX_EXCEPTION_CODE_HACK
#else
 #define HAS_TRACK_CXX_EXCEPTION_CODE_HACK 0
#endif

#define INITIAL_SUCCESS_COUNT               0x100

#define DynamicHelperFrameFlags_Default     0
#define DynamicHelperFrameFlags_ObjectArg   1
#define DynamicHelperFrameFlags_ObjectArg2  2

#ifdef FEATURE_REMOTING
#define TransparentProxyObject___stubData 0x8
ASMCONSTANTS_C_ASSERT(TransparentProxyObject___stubData == offsetof(TransparentProxyObject, _stubData))

#define TransparentProxyObject___stub 0x14
ASMCONSTANTS_C_ASSERT(TransparentProxyObject___stub == offsetof(TransparentProxyObject, _stub))

#define TransparentProxyObject___pMT 0xc
ASMCONSTANTS_C_ASSERT(TransparentProxyObject___pMT == offsetof(TransparentProxyObject, _pMT))
#endif // FEATURE_REMOTING

// CONTEXT from rotor_pal.h
#define CONTEXT_Edi 0x9c
ASMCONSTANTS_C_ASSERT(CONTEXT_Edi == offsetof(CONTEXT,Edi))

#define CONTEXT_Esi 0xa0
ASMCONSTANTS_C_ASSERT(CONTEXT_Esi == offsetof(CONTEXT,Esi))

#define CONTEXT_Ebx 0xa4
ASMCONSTANTS_C_ASSERT(CONTEXT_Ebx == offsetof(CONTEXT,Ebx))

#define CONTEXT_Edx 0xa8
ASMCONSTANTS_C_ASSERT(CONTEXT_Edx == offsetof(CONTEXT,Edx))

#define CONTEXT_Eax 0xb0
ASMCONSTANTS_C_ASSERT(CONTEXT_Eax == offsetof(CONTEXT,Eax))

#define CONTEXT_Ebp 0xb4
ASMCONSTANTS_C_ASSERT(CONTEXT_Ebp == offsetof(CONTEXT,Ebp))

#define CONTEXT_Eip 0xb8
ASMCONSTANTS_C_ASSERT(CONTEXT_Eip == offsetof(CONTEXT,Eip))

#define CONTEXT_Esp 0xc4
ASMCONSTANTS_C_ASSERT(CONTEXT_Esp == offsetof(CONTEXT,Esp))

// SYSTEM_INFO from rotor_pal.h
#define SYSTEM_INFO_dwNumberOfProcessors 20 
ASMCONSTANTS_C_ASSERT(SYSTEM_INFO_dwNumberOfProcessors == offsetof(SYSTEM_INFO,dwNumberOfProcessors))

// SpinConstants from clr/src/vars.h
#define SpinConstants_dwInitialDuration 0 
ASMCONSTANTS_C_ASSERT(SpinConstants_dwInitialDuration == offsetof(SpinConstants,dwInitialDuration))

#define SpinConstants_dwMaximumDuration 4 
ASMCONSTANTS_C_ASSERT(SpinConstants_dwMaximumDuration == offsetof(SpinConstants,dwMaximumDuration))

#define SpinConstants_dwBackoffFactor 8
ASMCONSTANTS_C_ASSERT(SpinConstants_dwBackoffFactor == offsetof(SpinConstants,dwBackoffFactor))

// EHContext from clr/src/vm/i386/cgencpu.h
#define EHContext_Eax 0x00
ASMCONSTANTS_C_ASSERT(EHContext_Eax == offsetof(EHContext,Eax))

#define EHContext_Ebx 0x04
ASMCONSTANTS_C_ASSERT(EHContext_Ebx == offsetof(EHContext,Ebx))

#define EHContext_Ecx 0x08
ASMCONSTANTS_C_ASSERT(EHContext_Ecx == offsetof(EHContext,Ecx))

#define EHContext_Edx 0x0c
ASMCONSTANTS_C_ASSERT(EHContext_Edx == offsetof(EHContext,Edx))

#define EHContext_Esi 0x10
ASMCONSTANTS_C_ASSERT(EHContext_Esi == offsetof(EHContext,Esi))

#define EHContext_Edi 0x14
ASMCONSTANTS_C_ASSERT(EHContext_Edi == offsetof(EHContext,Edi))

#define EHContext_Ebp 0x18
ASMCONSTANTS_C_ASSERT(EHContext_Ebp == offsetof(EHContext,Ebp))

#define EHContext_Esp 0x1c
ASMCONSTANTS_C_ASSERT(EHContext_Esp == offsetof(EHContext,Esp))

#define EHContext_Eip 0x20
ASMCONSTANTS_C_ASSERT(EHContext_Eip == offsetof(EHContext,Eip))


// from clr/src/fjit/helperframe.h
#define SIZEOF_MachState          40
ASMCONSTANTS_C_ASSERT(SIZEOF_MachState == sizeof(MachState))

#define MachState__pEdi           0
ASMCONSTANTS_C_ASSERT(MachState__pEdi == offsetof(MachState, _pEdi))

#define MachState__edi            4
ASMCONSTANTS_C_ASSERT(MachState__edi == offsetof(MachState, _edi))

#define MachState__pEsi           8
ASMCONSTANTS_C_ASSERT(MachState__pEsi == offsetof(MachState, _pEsi))

#define MachState__esi            12
ASMCONSTANTS_C_ASSERT(MachState__esi == offsetof(MachState, _esi))

#define MachState__pEbx           16
ASMCONSTANTS_C_ASSERT(MachState__pEbx == offsetof(MachState, _pEbx))

#define MachState__ebx            20
ASMCONSTANTS_C_ASSERT(MachState__ebx == offsetof(MachState, _ebx))

#define MachState__pEbp           24
ASMCONSTANTS_C_ASSERT(MachState__pEbp == offsetof(MachState, _pEbp))

#define MachState__ebp            28
ASMCONSTANTS_C_ASSERT(MachState__ebp == offsetof(MachState, _ebp))

#define MachState__esp            32
ASMCONSTANTS_C_ASSERT(MachState__esp == offsetof(MachState, _esp))

#define MachState__pRetAddr       36
ASMCONSTANTS_C_ASSERT(MachState__pRetAddr == offsetof(MachState, _pRetAddr))

#define LazyMachState_captureEbp  40
ASMCONSTANTS_C_ASSERT(LazyMachState_captureEbp == offsetof(LazyMachState, captureEbp))

#define LazyMachState_captureEsp  44
ASMCONSTANTS_C_ASSERT(LazyMachState_captureEsp == offsetof(LazyMachState, captureEsp))

#define LazyMachState_captureEip  48
ASMCONSTANTS_C_ASSERT(LazyMachState_captureEip == offsetof(LazyMachState, captureEip))


#define VASigCookie__StubOffset 4
ASMCONSTANTS_C_ASSERT(VASigCookie__StubOffset == offsetof(VASigCookie, pNDirectILStub))

#define SIZEOF_TailCallFrame 32
ASMCONSTANTS_C_ASSERT(SIZEOF_TailCallFrame == sizeof(TailCallFrame))

#define SIZEOF_GSCookie 4

// ICodeManager::SHADOW_SP_IN_FILTER from clr/src/inc/eetwain.h
#define SHADOW_SP_IN_FILTER_ASM 0x1
ASMCONSTANTS_C_ASSERT(SHADOW_SP_IN_FILTER_ASM == ICodeManager::SHADOW_SP_IN_FILTER)

// from clr/src/inc/corinfo.h
#define CORINFO_NullReferenceException_ASM 0
ASMCONSTANTS_C_ASSERT(CORINFO_NullReferenceException_ASM == CORINFO_NullReferenceException)

#define CORINFO_IndexOutOfRangeException_ASM 3
ASMCONSTANTS_C_ASSERT(CORINFO_IndexOutOfRangeException_ASM == CORINFO_IndexOutOfRangeException)

#define CORINFO_OverflowException_ASM 4
ASMCONSTANTS_C_ASSERT(CORINFO_OverflowException_ASM == CORINFO_OverflowException)

#define CORINFO_SynchronizationLockException_ASM 5
ASMCONSTANTS_C_ASSERT(CORINFO_SynchronizationLockException_ASM == CORINFO_SynchronizationLockException)

#define CORINFO_ArrayTypeMismatchException_ASM 6
ASMCONSTANTS_C_ASSERT(CORINFO_ArrayTypeMismatchException_ASM == CORINFO_ArrayTypeMismatchException)

#define CORINFO_ArgumentNullException_ASM 8
ASMCONSTANTS_C_ASSERT(CORINFO_ArgumentNullException_ASM == CORINFO_ArgumentNullException)

#define CORINFO_ArgumentException_ASM 9
ASMCONSTANTS_C_ASSERT(CORINFO_ArgumentException_ASM == CORINFO_ArgumentException)


#ifndef CROSSGEN_COMPILE

// from clr/src/vm/threads.h
#if defined(TRACK_CXX_EXCEPTION_CODE_HACK) // Is C++ exception code tracking turned on?
    #define Thread_m_LastCxxSEHExceptionCode      0x20
    ASMCONSTANTS_C_ASSERT(Thread_m_LastCxxSEHExceptionCode == offsetof(Thread, m_LastCxxSEHExceptionCode))

    #define Thread_m_Context    0x3C
#else
    #define Thread_m_Context    0x38
#endif // TRACK_CXX_EXCEPTION_CODE_HACK
ASMCONSTANTS_C_ASSERT(Thread_m_Context == offsetof(Thread, m_Context))

#define Thread_m_State      0x04
ASMCONSTANTS_C_ASSERT(Thread_m_State == offsetof(Thread, m_State))
#endif // CROSSGEN_COMPILE

#define Thread_m_fPreemptiveGCDisabled     0x08
#ifndef CROSSGEN_COMPILE
ASMCONSTANTS_C_ASSERT(Thread_m_fPreemptiveGCDisabled == offsetof(Thread, m_fPreemptiveGCDisabled))
#endif // CROSSGEN_COMPILE

#define Thread_m_pFrame     0x0C
#ifndef CROSSGEN_COMPILE
ASMCONSTANTS_C_ASSERT(Thread_m_pFrame == offsetof(Thread, m_pFrame))
#endif // CROSSGEN_COMPILE

#ifndef CROSSGEN_COMPILE
#define Thread_m_dwLockCount 0x18
ASMCONSTANTS_C_ASSERT(Thread_m_dwLockCount == offsetof(Thread, m_dwLockCount))

#define Thread_m_ThreadId 0x1C
ASMCONSTANTS_C_ASSERT(Thread_m_ThreadId == offsetof(Thread, m_ThreadId))

#define TS_CatchAtSafePoint_ASM 0x5F
ASMCONSTANTS_C_ASSERT(Thread::TS_CatchAtSafePoint == TS_CatchAtSafePoint_ASM)

#ifdef FEATURE_HIJACK
#define TS_Hijacked_ASM 0x80
ASMCONSTANTS_C_ASSERT(Thread::TS_Hijacked == TS_Hijacked_ASM)
#endif

#endif // CROSSGEN_COMPILE


// from clr/src/vm/appdomain.hpp

#define AppDomain__m_dwId 0x4
ASMCONSTANTS_C_ASSERT(AppDomain__m_dwId == offsetof(AppDomain, m_dwId));

// from clr/src/vm/ceeload.cpp
#ifdef FEATURE_MIXEDMODE
#define IJWNOADThunk__m_cache 0x1C
ASMCONSTANTS_C_ASSERT(IJWNOADThunk__m_cache == offsetof(IJWNOADThunk, m_cache))

#define IJWNOADThunk__NextCacheOffset 0x8
ASMCONSTANTS_C_ASSERT(IJWNOADThunk__NextCacheOffset == sizeof(IJWNOADThunkStubCache))

#define IJWNOADThunk__CodeAddrOffsetFromADID 0x4
ASMCONSTANTS_C_ASSERT(IJWNOADThunk__CodeAddrOffsetFromADID == offsetof(IJWNOADThunkStubCache, m_CodeAddr))
#endif //FEATURE_MIXEDMODE

// from clr/src/vm/syncblk.h
#define SizeOfSyncTableEntry_ASM 8
ASMCONSTANTS_C_ASSERT(sizeof(SyncTableEntry) == SizeOfSyncTableEntry_ASM)

#define SyncBlockIndexOffset_ASM 4
ASMCONSTANTS_C_ASSERT(sizeof(ObjHeader) - offsetof(ObjHeader, m_SyncBlockValue) == SyncBlockIndexOffset_ASM)

#ifndef __GNUC__
#define SyncTableEntry_m_SyncBlock 0
ASMCONSTANTS_C_ASSERT(offsetof(SyncTableEntry, m_SyncBlock) == SyncTableEntry_m_SyncBlock)

#define SyncBlock_m_Monitor 0
ASMCONSTANTS_C_ASSERT(offsetof(SyncBlock, m_Monitor) == SyncBlock_m_Monitor)

#define AwareLock_m_MonitorHeld 0
ASMCONSTANTS_C_ASSERT(offsetof(AwareLock, m_MonitorHeld) == AwareLock_m_MonitorHeld)
#else
// The following 3 offsets have value of 0, and must be
// defined to be an empty string. Otherwise, gas may generate assembly
// code with 0 displacement if 0 is left in the displacement field
// of an instruction.
#define SyncTableEntry_m_SyncBlock // 0
ASMCONSTANTS_C_ASSERT(offsetof(SyncTableEntry, m_SyncBlock) == 0)

#define SyncBlock_m_Monitor // 0
ASMCONSTANTS_C_ASSERT(offsetof(SyncBlock, m_Monitor) == 0)

#define AwareLock_m_MonitorHeld // 0
ASMCONSTANTS_C_ASSERT(offsetof(AwareLock, m_MonitorHeld) == 0)
#endif // !__GNUC__

#define AwareLock_m_HoldingThread 8
ASMCONSTANTS_C_ASSERT(offsetof(AwareLock, m_HoldingThread) == AwareLock_m_HoldingThread)

#define AwareLock_m_Recursion 4
ASMCONSTANTS_C_ASSERT(offsetof(AwareLock, m_Recursion) == AwareLock_m_Recursion)

#define BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX_ASM 0x08000000
ASMCONSTANTS_C_ASSERT(BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX_ASM == BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX)

#define BIT_SBLK_SPIN_LOCK_ASM 0x10000000
ASMCONSTANTS_C_ASSERT(BIT_SBLK_SPIN_LOCK_ASM == BIT_SBLK_SPIN_LOCK)

#define SBLK_MASK_LOCK_THREADID_ASM 0x000003FF   // special value of 0 + 1023 thread ids
ASMCONSTANTS_C_ASSERT(SBLK_MASK_LOCK_THREADID_ASM == SBLK_MASK_LOCK_THREADID)

#define SBLK_MASK_LOCK_RECLEVEL_ASM 0x0000FC00   // 64 recursion levels
ASMCONSTANTS_C_ASSERT(SBLK_MASK_LOCK_RECLEVEL_ASM == SBLK_MASK_LOCK_RECLEVEL)

#define SBLK_LOCK_RECLEVEL_INC_ASM 0x00000400   // each level is this much higher than the previous one
ASMCONSTANTS_C_ASSERT(SBLK_LOCK_RECLEVEL_INC_ASM == SBLK_LOCK_RECLEVEL_INC)

#define BIT_SBLK_IS_HASHCODE_ASM 0x04000000
ASMCONSTANTS_C_ASSERT(BIT_SBLK_IS_HASHCODE_ASM == BIT_SBLK_IS_HASHCODE)

#define MASK_SYNCBLOCKINDEX_ASM  0x03ffffff // ((1<<SYNCBLOCKINDEX_BITS)-1)
ASMCONSTANTS_C_ASSERT(MASK_SYNCBLOCKINDEX_ASM == MASK_SYNCBLOCKINDEX)

// BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX_ASM + BIT_SBLK_SPIN_LOCK_ASM + 
// SBLK_MASK_LOCK_THREADID_ASM + SBLK_MASK_LOCK_RECLEVEL_ASM
#define SBLK_COMBINED_MASK_ASM 0x1800ffff
ASMCONSTANTS_C_ASSERT(SBLK_COMBINED_MASK_ASM == (BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX + BIT_SBLK_SPIN_LOCK + SBLK_MASK_LOCK_THREADID + SBLK_MASK_LOCK_RECLEVEL))

// BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX_ASM + BIT_SBLK_SPIN_LOCK_ASM
#define BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX_SPIN_LOCK_ASM 0x18000000
ASMCONSTANTS_C_ASSERT(BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX_SPIN_LOCK_ASM == (BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX + BIT_SBLK_SPIN_LOCK))

// BIT_SBLK_IS_HASHCODE + BIT_SBLK_SPIN_LOCK
#define BIT_SBLK_IS_HASHCODE_OR_SPIN_LOCK_ASM 0x14000000
ASMCONSTANTS_C_ASSERT(BIT_SBLK_IS_HASHCODE_OR_SPIN_LOCK_ASM == (BIT_SBLK_IS_HASHCODE + BIT_SBLK_SPIN_LOCK))

// This is the offset from EBP at which the original CONTEXT is stored in one of the 
// RedirectedHandledJITCase*_Stub functions.
#define REDIRECTSTUB_EBP_OFFSET_CONTEXT (-4)

#define MethodTable_m_wNumInterfaces    0x0E
ASMCONSTANTS_C_ASSERT(MethodTable_m_wNumInterfaces == offsetof(MethodTable, m_wNumInterfaces))

#define MethodTable_m_dwFlags           0x0
ASMCONSTANTS_C_ASSERT(MethodTable_m_dwFlags == offsetof(MethodTable, m_dwFlags))

#define MethodTable_m_pInterfaceMap     DBG_FRE(0x28, 0x24)
ASMCONSTANTS_C_ASSERT(MethodTable_m_pInterfaceMap == offsetof(MethodTable, m_pMultipurposeSlot2))

#define SIZEOF_MethodTable              DBG_FRE(0x2C, 0x28)
ASMCONSTANTS_C_ASSERT(SIZEOF_MethodTable == sizeof(MethodTable))

#define SIZEOF_InterfaceInfo_t          0x4
ASMCONSTANTS_C_ASSERT(SIZEOF_InterfaceInfo_t == sizeof(InterfaceInfo_t))

#ifdef FEATURE_COMINTEROP

#define SIZEOF_FrameHandlerExRecord 0x0c
#define OFFSETOF__FrameHandlerExRecord__m_ExReg__Next 0
#define OFFSETOF__FrameHandlerExRecord__m_ExReg__Handler 4
#define OFFSETOF__FrameHandlerExRecord__m_pEntryFrame 8
ASMCONSTANTS_C_ASSERT(SIZEOF_FrameHandlerExRecord == sizeof(FrameHandlerExRecord))
ASMCONSTANTS_C_ASSERT(OFFSETOF__FrameHandlerExRecord__m_ExReg__Next == offsetof(FrameHandlerExRecord, m_ExReg) + offsetof(EXCEPTION_REGISTRATION_RECORD, Next))
ASMCONSTANTS_C_ASSERT(OFFSETOF__FrameHandlerExRecord__m_ExReg__Handler == offsetof(FrameHandlerExRecord, m_ExReg) + offsetof(EXCEPTION_REGISTRATION_RECORD, Handler))
ASMCONSTANTS_C_ASSERT(OFFSETOF__FrameHandlerExRecord__m_pEntryFrame == offsetof(FrameHandlerExRecord, m_pEntryFrame))

#ifdef _DEBUG
#ifndef STACK_OVERWRITE_BARRIER_SIZE 
#define STACK_OVERWRITE_BARRIER_SIZE 20
#endif
#ifndef STACK_OVERWRITE_BARRIER_VALUE 
#define STACK_OVERWRITE_BARRIER_VALUE 0xabcdefab
#endif

#define SIZEOF_FrameHandlerExRecordWithBarrier 0x5c
ASMCONSTANTS_C_ASSERT(SIZEOF_FrameHandlerExRecordWithBarrier == sizeof(FrameHandlerExRecordWithBarrier))
#endif


#ifdef MDA_SUPPORTED
#define SIZEOF_StackImbalanceCookie 0x14
ASMCONSTANTS_C_ASSERT(SIZEOF_StackImbalanceCookie == sizeof(StackImbalanceCookie))

#define StackImbalanceCookie__m_pMD            0x00
#define StackImbalanceCookie__m_pTarget        0x04
#define StackImbalanceCookie__m_dwStackArgSize 0x08
#define StackImbalanceCookie__m_callConv       0x0c
#define StackImbalanceCookie__m_dwSavedEsp     0x10
#define StackImbalanceCookie__HAS_FP_RETURN_VALUE 0x80000000

ASMCONSTANTS_C_ASSERT(StackImbalanceCookie__m_pMD            == offsetof(StackImbalanceCookie, m_pMD))
ASMCONSTANTS_C_ASSERT(StackImbalanceCookie__m_pTarget        == offsetof(StackImbalanceCookie, m_pTarget))
ASMCONSTANTS_C_ASSERT(StackImbalanceCookie__m_dwStackArgSize == offsetof(StackImbalanceCookie, m_dwStackArgSize))
ASMCONSTANTS_C_ASSERT(StackImbalanceCookie__m_callConv       == offsetof(StackImbalanceCookie, m_callConv))
ASMCONSTANTS_C_ASSERT(StackImbalanceCookie__m_dwSavedEsp     == offsetof(StackImbalanceCookie, m_dwSavedEsp))
ASMCONSTANTS_C_ASSERT(StackImbalanceCookie__HAS_FP_RETURN_VALUE == StackImbalanceCookie::HAS_FP_RETURN_VALUE)
#endif // MDA_SUPPORTED

#define MethodDesc_m_wFlags                   DBG_FRE(0x1a, 0x06)
ASMCONSTANTS_C_ASSERT(MethodDesc_m_wFlags == offsetof(MethodDesc, m_wFlags))

#define MethodDesc_mdcClassification          7
ASMCONSTANTS_C_ASSERT(MethodDesc_mdcClassification == mdcClassification)

#define MethodDesc_mcComInterop               6
ASMCONSTANTS_C_ASSERT(MethodDesc_mcComInterop == mcComInterop)

#define ComPlusCallMethodDesc__m_pComPlusCallInfo DBG_FRE(0x1C, 0x8)
ASMCONSTANTS_C_ASSERT(ComPlusCallMethodDesc__m_pComPlusCallInfo == offsetof(ComPlusCallMethodDesc, m_pComPlusCallInfo))

#define ComPlusCallInfo__m_pRetThunk 0x10
ASMCONSTANTS_C_ASSERT(ComPlusCallInfo__m_pRetThunk == offsetof(ComPlusCallInfo, m_pRetThunk))

#endif // FEATURE_COMINTEROP

#define               NonTrivialInterfaceCastFlags (0x00080000 + 0x40000000 + 0x00400000)
ASMCONSTANTS_C_ASSERT(NonTrivialInterfaceCastFlags == MethodTable::public_enum_flag_NonTrivialInterfaceCast)

#define ASM__VTABLE_SLOTS_PER_CHUNK 8
ASMCONSTANTS_C_ASSERT(ASM__VTABLE_SLOTS_PER_CHUNK == VTABLE_SLOTS_PER_CHUNK)

#define ASM__VTABLE_SLOTS_PER_CHUNK_LOG2 3
ASMCONSTANTS_C_ASSERT(ASM__VTABLE_SLOTS_PER_CHUNK_LOG2 == VTABLE_SLOTS_PER_CHUNK_LOG2)

#define TLS_GETTER_MAX_SIZE_ASM DBG_FRE(0x20, 0x10)
ASMCONSTANTS_C_ASSERT(TLS_GETTER_MAX_SIZE_ASM == TLS_GETTER_MAX_SIZE)

#define JIT_TailCall_StackOffsetToFlags       0x08

#define CallDescrData__pSrc                0x00
#define CallDescrData__numStackSlots       0x04
#define CallDescrData__pArgumentRegisters  0x08
#define CallDescrData__fpReturnSize        0x0C
#define CallDescrData__pTarget             0x10
#ifndef __GNUC__
#define CallDescrData__returnValue         0x18
#else
#define CallDescrData__returnValue         0x14
#endif

ASMCONSTANTS_C_ASSERT(CallDescrData__pSrc                 == offsetof(CallDescrData, pSrc))
ASMCONSTANTS_C_ASSERT(CallDescrData__numStackSlots        == offsetof(CallDescrData, numStackSlots))
ASMCONSTANTS_C_ASSERT(CallDescrData__pArgumentRegisters   == offsetof(CallDescrData, pArgumentRegisters))
ASMCONSTANTS_C_ASSERT(CallDescrData__fpReturnSize         == offsetof(CallDescrData, fpReturnSize))
ASMCONSTANTS_C_ASSERT(CallDescrData__pTarget              == offsetof(CallDescrData, pTarget))
ASMCONSTANTS_C_ASSERT(CallDescrData__returnValue          == offsetof(CallDescrData, returnValue))

#undef ASMCONSTANTS_C_ASSERT
#undef ASMCONSTANTS_RUNTIME_ASSERT

// #define USE_COMPILE_TIME_CONSTANT_FINDER // Uncomment this line to use the constant finder
#if defined(__cplusplus) && defined(USE_COMPILE_TIME_CONSTANT_FINDER)
// This class causes the compiler to emit an error with the constant we're interested in
// in the error message. This is useful if a size or offset changes. To use, comment out
// the compile-time assert that is firing, enable the constant finder, add the appropriate
// constant to find to BogusFunction(), and build.
// 
// Here's a sample compiler error:
// d:\dd\clr\src\ndp\clr\src\vm\i386\asmconstants.h(326) : error C2248: 'FindCompileTimeConstant<N>::FindCompileTimeConstant' : cannot access private member declared in class 'FindCompileTimeConstant<N>'
//         with
//         [
//             N=1520
//         ]
//         d:\dd\clr\src\ndp\clr\src\vm\i386\asmconstants.h(321) : see declaration of 'FindCompileTimeConstant<N>::FindCompileTimeConstant'
//         with
//         [
//             N=1520
//         ]
template<size_t N>
class FindCompileTimeConstant
{
private:
	FindCompileTimeConstant();
};

void BogusFunction()
{
	// Sample usage to generate the error
	FindCompileTimeConstant<offsetof(AppDomain, m_dwId)> bogus_variable;
}
#endif // defined(__cplusplus) && defined(USE_COMPILE_TIME_CONSTANT_FINDER)
