// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// asmconstants.h -
//
// This header defines field offsets and constants used by assembly code
// Be sure to rebuild clr/src/vm/ceemain.cpp after changing this file, to
// ensure that the constants match the expected C/C++ values

// #ifndef _ARM64_
// #error this file should only be used on an ARM platform
// #endif // _ARM64_

#include "../../inc/switches.h"

//-----------------------------------------------------------------------------

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

#define DynamicHelperFrameFlags_Default     0
#define DynamicHelperFrameFlags_ObjectArg   1
#define DynamicHelperFrameFlags_ObjectArg2  2

#define               Thread__m_fPreemptiveGCDisabled   0x0C
#define               Thread__m_pFrame                  0x10

#ifndef CROSSGEN_COMPILE
ASMCONSTANTS_C_ASSERT(Thread__m_fPreemptiveGCDisabled == offsetof(Thread, m_fPreemptiveGCDisabled));
ASMCONSTANTS_C_ASSERT(Thread__m_pFrame == offsetof(Thread, m_pFrame));
#endif // CROSSGEN_COMPILE

#define Thread_m_pFrame Thread__m_pFrame
#define Thread_m_fPreemptiveGCDisabled Thread__m_fPreemptiveGCDisabled

#define METHODDESC_REGISTER            x12

#define SIZEOF__ArgumentRegisters 0x40
ASMCONSTANTS_C_ASSERT(SIZEOF__ArgumentRegisters == sizeof(ArgumentRegisters))

// There are 8 128-bit registers in FloatArgumentRegisters
#define SIZEOF__FloatArgumentRegisters 0x80
ASMCONSTANTS_C_ASSERT(SIZEOF__FloatArgumentRegisters == sizeof(FloatArgumentRegisters))

#define ASM_ENREGISTERED_RETURNTYPE_MAXSIZE 0x40
ASMCONSTANTS_C_ASSERT(ASM_ENREGISTERED_RETURNTYPE_MAXSIZE == ENREGISTERED_RETURNTYPE_MAXSIZE)

#define CallDescrData__pSrc                     0x00
#define CallDescrData__numStackSlots            0x08
#define CallDescrData__pArgumentRegisters       0x10
#define CallDescrData__pFloatArgumentRegisters  0x18
#define CallDescrData__fpReturnSize             0x20
#define CallDescrData__pTarget                  0x28
#define CallDescrData__pRetBuffArg              0x30
#define CallDescrData__returnValue              0x40

ASMCONSTANTS_C_ASSERT(CallDescrData__pSrc                 == offsetof(CallDescrData, pSrc))
ASMCONSTANTS_C_ASSERT(CallDescrData__numStackSlots        == offsetof(CallDescrData, numStackSlots))
ASMCONSTANTS_C_ASSERT(CallDescrData__pArgumentRegisters   == offsetof(CallDescrData, pArgumentRegisters))
ASMCONSTANTS_C_ASSERT(CallDescrData__pFloatArgumentRegisters == offsetof(CallDescrData, pFloatArgumentRegisters))
ASMCONSTANTS_C_ASSERT(CallDescrData__fpReturnSize         == offsetof(CallDescrData, fpReturnSize))
ASMCONSTANTS_C_ASSERT(CallDescrData__pTarget              == offsetof(CallDescrData, pTarget))
ASMCONSTANTS_C_ASSERT(CallDescrData__pRetBuffArg          == offsetof(CallDescrData, pRetBuffArg))
ASMCONSTANTS_C_ASSERT(CallDescrData__returnValue          == offsetof(CallDescrData, returnValue))

#define                  CORINFO_NullReferenceException_ASM 0
ASMCONSTANTS_C_ASSERT(   CORINFO_NullReferenceException_ASM
                      == CORINFO_NullReferenceException);


#define                  CORINFO_IndexOutOfRangeException_ASM 3
ASMCONSTANTS_C_ASSERT(   CORINFO_IndexOutOfRangeException_ASM
                      == CORINFO_IndexOutOfRangeException);


// Offset of the array containing the address of captured registers in MachState
#define MachState__captureX19_X29 0x0
ASMCONSTANTS_C_ASSERT(MachState__captureX19_X29 == offsetof(MachState, captureX19_X29))

// Offset of the array containing the address of preserved registers in MachState
#define MachState__ptrX19_X29 0x58
ASMCONSTANTS_C_ASSERT(MachState__ptrX19_X29 == offsetof(MachState, ptrX19_X29))

#define MachState__isValid 0xc0
ASMCONSTANTS_C_ASSERT(MachState__isValid == offsetof(MachState, _isValid))

#define LazyMachState_captureX19_X29 MachState__captureX19_X29
ASMCONSTANTS_C_ASSERT(LazyMachState_captureX19_X29 == offsetof(LazyMachState, captureX19_X29))

#define LazyMachState_captureSp     (MachState__isValid+8) // padding for alignment
ASMCONSTANTS_C_ASSERT(LazyMachState_captureSp == offsetof(LazyMachState, captureSp))

#define LazyMachState_captureIp     (LazyMachState_captureSp+8)
ASMCONSTANTS_C_ASSERT(LazyMachState_captureIp == offsetof(LazyMachState, captureIp))

#define VASigCookie__pNDirectILStub 0x8
ASMCONSTANTS_C_ASSERT(VASigCookie__pNDirectILStub == offsetof(VASigCookie, pNDirectILStub))

#define DelegateObject___methodPtr      0x18
ASMCONSTANTS_C_ASSERT(DelegateObject___methodPtr == offsetof(DelegateObject, _methodPtr));

#define DelegateObject___target         0x08
ASMCONSTANTS_C_ASSERT(DelegateObject___target == offsetof(DelegateObject, _target));

#define SIZEOF__GSCookie 0x8
ASMCONSTANTS_C_ASSERT(SIZEOF__GSCookie == sizeof(GSCookie));

#define SIZEOF__Frame                 0x10
ASMCONSTANTS_C_ASSERT(SIZEOF__Frame == sizeof(Frame));

#define SIZEOF__CONTEXT               0x390
ASMCONSTANTS_C_ASSERT(SIZEOF__CONTEXT == sizeof(T_CONTEXT));


//=========================================
#define MethodTable__m_dwFlags         0x0
ASMCONSTANTS_C_ASSERT(MethodTable__m_dwFlags == offsetof(MethodTable, m_dwFlags));

#define MethodTable__m_BaseSize         0x04
ASMCONSTANTS_C_ASSERT(MethodTable__m_BaseSize == offsetof(MethodTable, m_BaseSize));

#define MethodTable__m_ElementType     DBG_FRE(0x38, 0x30)
ASMCONSTANTS_C_ASSERT(MethodTable__m_ElementType == offsetof(MethodTable, m_pMultipurposeSlot1));

#define ArrayBase__m_NumComponents     0x8
ASMCONSTANTS_C_ASSERT(ArrayBase__m_NumComponents == offsetof(ArrayBase, m_NumComponents));

#define PtrArray__m_Array              0x10
ASMCONSTANTS_C_ASSERT(PtrArray__m_Array == offsetof(PtrArray, m_Array));

#define TypeHandle_CanCast 0x1 // TypeHandle::CanCast

//=========================================



#ifdef FEATURE_COMINTEROP

#define SIZEOF__ComMethodFrame 0x70
ASMCONSTANTS_C_ASSERT(SIZEOF__ComMethodFrame == sizeof(ComMethodFrame));

#define UnmanagedToManagedFrame__m_pvDatum 0x10
ASMCONSTANTS_C_ASSERT(UnmanagedToManagedFrame__m_pvDatum == offsetof(UnmanagedToManagedFrame, m_pvDatum));

#endif // FEATURE_COMINTEROP


#define UMEntryThunk__m_pUMThunkMarshInfo 0x18
ASMCONSTANTS_C_ASSERT(UMEntryThunk__m_pUMThunkMarshInfo == offsetof(UMEntryThunk, m_pUMThunkMarshInfo))

#define UMThunkMarshInfo__m_pILStub 0x00
ASMCONSTANTS_C_ASSERT(UMThunkMarshInfo__m_pILStub == offsetof(UMThunkMarshInfo, m_pILStub))

#define UMThunkMarshInfo__m_cbActualArgSize 0x08
ASMCONSTANTS_C_ASSERT(UMThunkMarshInfo__m_cbActualArgSize == offsetof(UMThunkMarshInfo, m_cbActualArgSize))

#define REDIRECTSTUB_SP_OFFSET_CONTEXT 0    

#define CONTEXT_Pc 0x108
ASMCONSTANTS_C_ASSERT(CONTEXT_Pc == offsetof(T_CONTEXT,Pc))

#define SIZEOF__FaultingExceptionFrame                  (SIZEOF__Frame + 0x10 + SIZEOF__CONTEXT)
#define FaultingExceptionFrame__m_fFilterExecuted       SIZEOF__Frame
ASMCONSTANTS_C_ASSERT(SIZEOF__FaultingExceptionFrame        == sizeof(FaultingExceptionFrame));
ASMCONSTANTS_C_ASSERT(FaultingExceptionFrame__m_fFilterExecuted == offsetof(FaultingExceptionFrame, m_fFilterExecuted));

#define SIZEOF__FixupPrecode                 24
#define Offset_PrecodeChunkIndex             15
#define Offset_MethodDescChunkIndex          14
#define MethodDesc_ALIGNMENT_SHIFT           3
#define FixupPrecode_ALIGNMENT_SHIFT_1       3
#define FixupPrecode_ALIGNMENT_SHIFT_2       4

ASMCONSTANTS_C_ASSERT(SIZEOF__FixupPrecode == sizeof(FixupPrecode));
ASMCONSTANTS_C_ASSERT(Offset_PrecodeChunkIndex == offsetof(FixupPrecode, m_PrecodeChunkIndex));
ASMCONSTANTS_C_ASSERT(Offset_MethodDescChunkIndex == offsetof(FixupPrecode, m_MethodDescChunkIndex));
ASMCONSTANTS_C_ASSERT(MethodDesc_ALIGNMENT_SHIFT == MethodDesc::ALIGNMENT_SHIFT);
ASMCONSTANTS_C_ASSERT((1<<FixupPrecode_ALIGNMENT_SHIFT_1) + (1<<FixupPrecode_ALIGNMENT_SHIFT_2)  == sizeof(FixupPrecode));

#ifndef CROSSGEN_COMPILE
#define ResolveCacheElem__target      0x10
#define ResolveCacheElem__pNext       0x18
ASMCONSTANTS_C_ASSERT(ResolveCacheElem__target == offsetof(ResolveCacheElem, target));
ASMCONSTANTS_C_ASSERT(ResolveCacheElem__pNext == offsetof(ResolveCacheElem, pNext));
#endif // CROSSGEN_COMPILE

#define DomainLocalModule__m_pDataBlob 0x30
#define DomainLocalModule__m_pGCStatics 0x20
ASMCONSTANTS_C_ASSERT(DomainLocalModule__m_pDataBlob == offsetof(DomainLocalModule, m_pDataBlob));
ASMCONSTANTS_C_ASSERT(DomainLocalModule__m_pGCStatics == offsetof(DomainLocalModule, m_pGCStatics));


// For JIT_PInvokeBegin and JIT_PInvokeEnd helpers
#define               Frame__m_Next 0x08
ASMCONSTANTS_C_ASSERT(Frame__m_Next == offsetof(Frame, m_Next))

#define               InlinedCallFrame__m_Datum 0x10
ASMCONSTANTS_C_ASSERT(InlinedCallFrame__m_Datum == offsetof(InlinedCallFrame, m_Datum))

#define               InlinedCallFrame__m_pCallSiteSP 0x20
ASMCONSTANTS_C_ASSERT(InlinedCallFrame__m_pCallSiteSP == offsetof(InlinedCallFrame, m_pCallSiteSP))

#define               InlinedCallFrame__m_pCallerReturnAddress 0x28
ASMCONSTANTS_C_ASSERT(InlinedCallFrame__m_pCallerReturnAddress == offsetof(InlinedCallFrame, m_pCallerReturnAddress))

#define               InlinedCallFrame__m_pCalleeSavedFP 0x30
ASMCONSTANTS_C_ASSERT(InlinedCallFrame__m_pCalleeSavedFP == offsetof(InlinedCallFrame, m_pCalleeSavedFP))

#define               InlinedCallFrame__m_pThread 0x38
ASMCONSTANTS_C_ASSERT(InlinedCallFrame__m_pThread == offsetof(InlinedCallFrame, m_pThread))


#undef ASMCONSTANTS_RUNTIME_ASSERT
#undef ASMCONSTANTS_C_ASSERT
