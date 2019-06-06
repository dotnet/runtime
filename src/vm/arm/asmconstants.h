// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// asmconstants.h -
//
// This header defines field offsets and constants used by assembly code
// Be sure to rebuild clr/src/vm/ceemain.cpp after changing this file, to
// ensure that the constants match the expected C/C++ values

// #ifndef _ARM_
// #error this file should only be used on an ARM platform
// #endif // _ARM_

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

#define REDIRECTSTUB_SP_OFFSET_CONTEXT 0

#define                  CORINFO_NullReferenceException_ASM 0
ASMCONSTANTS_C_ASSERT(   CORINFO_NullReferenceException_ASM
                      == CORINFO_NullReferenceException);

#define                  CORINFO_IndexOutOfRangeException_ASM 3
ASMCONSTANTS_C_ASSERT(   CORINFO_IndexOutOfRangeException_ASM
                      == CORINFO_IndexOutOfRangeException);


// Offset of the array containing the address of captured registers in MachState
#define MachState__captureR4_R11 0x0
ASMCONSTANTS_C_ASSERT(MachState__captureR4_R11 == offsetof(MachState, captureR4_R11))

// Offset of the array containing the address of preserved registers in MachState
#define MachState___R4_R11 0x20
ASMCONSTANTS_C_ASSERT(MachState___R4_R11 == offsetof(MachState, _R4_R11))

#define MachState__isValid 0x48
ASMCONSTANTS_C_ASSERT(MachState__isValid == offsetof(MachState, _isValid))

#define LazyMachState_captureR4_R11 MachState__captureR4_R11
ASMCONSTANTS_C_ASSERT(LazyMachState_captureR4_R11 == offsetof(LazyMachState, captureR4_R11))

#define LazyMachState_captureSp     (MachState__isValid+4)
ASMCONSTANTS_C_ASSERT(LazyMachState_captureSp == offsetof(LazyMachState, captureSp))

#define LazyMachState_captureIp     (LazyMachState_captureSp+4)
ASMCONSTANTS_C_ASSERT(LazyMachState_captureIp == offsetof(LazyMachState, captureIp))

#define DelegateObject___methodPtr      0x0c
ASMCONSTANTS_C_ASSERT(DelegateObject___methodPtr == offsetof(DelegateObject, _methodPtr));

#define DelegateObject___target         0x04
ASMCONSTANTS_C_ASSERT(DelegateObject___target == offsetof(DelegateObject, _target));

#define MethodTable__m_BaseSize         0x04
ASMCONSTANTS_C_ASSERT(MethodTable__m_BaseSize == offsetof(MethodTable, m_BaseSize));

#define MethodTable__m_dwFlags         0x0
ASMCONSTANTS_C_ASSERT(MethodTable__m_dwFlags == offsetof(MethodTable, m_dwFlags));

#define MethodTable__m_pWriteableData   DBG_FRE(0x1c, 0x18)
ASMCONSTANTS_C_ASSERT(MethodTable__m_pWriteableData == offsetof(MethodTable, m_pWriteableData));

#define MethodTable__enum_flag_ContainsPointers 0x01000000
ASMCONSTANTS_C_ASSERT(MethodTable__enum_flag_ContainsPointers == MethodTable::enum_flag_ContainsPointers);

#define MethodTable__m_ElementType        DBG_FRE(0x24, 0x20)
ASMCONSTANTS_C_ASSERT(MethodTable__m_ElementType == offsetof(MethodTable, m_pMultipurposeSlot1));

#define SIZEOF__MethodTable             DBG_FRE(0x2c, 0x28)
ASMCONSTANTS_C_ASSERT(SIZEOF__MethodTable == sizeof(MethodTable));

#define MethodTableWriteableData__m_dwFlags 0x00
ASMCONSTANTS_C_ASSERT(MethodTableWriteableData__m_dwFlags == offsetof(MethodTableWriteableData, m_dwFlags));

#define MethodTableWriteableData__enum_flag_Unrestored 0x04
ASMCONSTANTS_C_ASSERT(MethodTableWriteableData__enum_flag_Unrestored == MethodTableWriteableData::enum_flag_Unrestored);

#define ArrayBase__m_NumComponents     0x4
ASMCONSTANTS_C_ASSERT(ArrayBase__m_NumComponents == offsetof(ArrayBase, m_NumComponents));

#define ArrayTypeDesc__m_Arg           0x8
ASMCONSTANTS_C_ASSERT(ArrayTypeDesc__m_Arg == offsetof(ArrayTypeDesc, m_Arg));

#define PtrArray__m_Array              0x8
ASMCONSTANTS_C_ASSERT(PtrArray__m_Array == offsetof(PtrArray, m_Array));

#define TypeHandle_CanCast 0x1 // TypeHandle::CanCast

#define SIZEOF__GSCookie              0x4
ASMCONSTANTS_C_ASSERT(SIZEOF__GSCookie == sizeof(GSCookie));

#define SIZEOF__Frame                 0x8
ASMCONSTANTS_C_ASSERT(SIZEOF__Frame == sizeof(Frame));

#define SIZEOF__CONTEXT               0x1a0
ASMCONSTANTS_C_ASSERT(SIZEOF__CONTEXT == sizeof(T_CONTEXT));

#define SIZEOF__CalleeSavedRegisters 0x24
ASMCONSTANTS_C_ASSERT(SIZEOF__CalleeSavedRegisters == sizeof(CalleeSavedRegisters))

#define SIZEOF__ArgumentRegisters 0x10
ASMCONSTANTS_C_ASSERT(SIZEOF__ArgumentRegisters == sizeof(ArgumentRegisters))

#define SIZEOF__FloatArgumentRegisters 0x40
ASMCONSTANTS_C_ASSERT(SIZEOF__FloatArgumentRegisters == sizeof(FloatArgumentRegisters))

#define ASM_ENREGISTERED_RETURNTYPE_MAXSIZE 0x20
ASMCONSTANTS_C_ASSERT(ASM_ENREGISTERED_RETURNTYPE_MAXSIZE == ENREGISTERED_RETURNTYPE_MAXSIZE)

#define UMEntryThunk__m_pUMThunkMarshInfo 0x0C
ASMCONSTANTS_C_ASSERT(UMEntryThunk__m_pUMThunkMarshInfo == offsetof(UMEntryThunk, m_pUMThunkMarshInfo))

#define UMThunkMarshInfo__m_pILStub 0x00
ASMCONSTANTS_C_ASSERT(UMThunkMarshInfo__m_pILStub == offsetof(UMThunkMarshInfo, m_pILStub))

#define UMThunkMarshInfo__m_cbActualArgSize 0x04
ASMCONSTANTS_C_ASSERT(UMThunkMarshInfo__m_cbActualArgSize == offsetof(UMThunkMarshInfo, m_cbActualArgSize))


#define MethodDesc__m_wFlags DBG_FRE(0x1A, 0x06)
ASMCONSTANTS_C_ASSERT(MethodDesc__m_wFlags == offsetof(MethodDesc, m_wFlags))

#define MethodDesc__mdcClassification 0x7
ASMCONSTANTS_C_ASSERT(MethodDesc__mdcClassification == mdcClassification)

#ifdef FEATURE_COMINTEROP

#define MethodDesc__mcComInterop 0x6
ASMCONSTANTS_C_ASSERT(MethodDesc__mcComInterop == mcComInterop)

#define Stub__m_pCode DBG_FRE(0x10, 0x0c)
ASMCONSTANTS_C_ASSERT(Stub__m_pCode == sizeof(Stub))

#define SIZEOF__ComMethodFrame 0x24
ASMCONSTANTS_C_ASSERT(SIZEOF__ComMethodFrame == sizeof(ComMethodFrame))

#define UnmanagedToManagedFrame__m_pvDatum 0x08
ASMCONSTANTS_C_ASSERT(UnmanagedToManagedFrame__m_pvDatum == offsetof(UnmanagedToManagedFrame, m_pvDatum))

// In ComCallPreStub and GenericComPlusCallStub, we setup R12 to contain address of ComCallMethodDesc after doing the following:
// 
// mov r12, pc
//
// This constant defines where ComCallMethodDesc is post execution of the above instruction.
#define ComCallMethodDesc_Offset_FromR12 0x8

#endif // FEATURE_COMINTEROP

#define               Thread__m_fPreemptiveGCDisabled   0x08
#ifndef CROSSGEN_COMPILE
ASMCONSTANTS_C_ASSERT(Thread__m_fPreemptiveGCDisabled == offsetof(Thread, m_fPreemptiveGCDisabled));
#endif // CROSSGEN_COMPILE
#define Thread_m_fPreemptiveGCDisabled Thread__m_fPreemptiveGCDisabled

#define               Thread__m_pFrame                  0x0C
#ifndef CROSSGEN_COMPILE
ASMCONSTANTS_C_ASSERT(Thread__m_pFrame == offsetof(Thread, m_pFrame));
#endif // CROSSGEN_COMPILE
#define Thread_m_pFrame Thread__m_pFrame

#ifndef CROSSGEN_COMPILE
#define               DomainLocalModule__m_pDataBlob                 0x18
ASMCONSTANTS_C_ASSERT(DomainLocalModule__m_pDataBlob == offsetof(DomainLocalModule, m_pDataBlob));

#define               DomainLocalModule__m_pGCStatics                 0x10
ASMCONSTANTS_C_ASSERT(DomainLocalModule__m_pGCStatics == offsetof(DomainLocalModule, m_pGCStatics));

#endif

#define ASM__VTABLE_SLOTS_PER_CHUNK 8
ASMCONSTANTS_C_ASSERT(ASM__VTABLE_SLOTS_PER_CHUNK == VTABLE_SLOTS_PER_CHUNK)

#define ASM__VTABLE_SLOTS_PER_CHUNK_LOG2 3
ASMCONSTANTS_C_ASSERT(ASM__VTABLE_SLOTS_PER_CHUNK_LOG2 == VTABLE_SLOTS_PER_CHUNK_LOG2)

#define VASigCookie__pNDirectILStub 0x4
ASMCONSTANTS_C_ASSERT(VASigCookie__pNDirectILStub == offsetof(VASigCookie, pNDirectILStub))

#define CONTEXT_Pc 0x040
ASMCONSTANTS_C_ASSERT(CONTEXT_Pc == offsetof(T_CONTEXT,Pc))

#define CallDescrData__pSrc                0x00
#define CallDescrData__numStackSlots       0x04
#define CallDescrData__pArgumentRegisters  0x08
#define CallDescrData__pFloatArgumentRegisters 0x0C
#define CallDescrData__fpReturnSize        0x10
#define CallDescrData__pTarget             0x14
#define CallDescrData__returnValue         0x18

ASMCONSTANTS_C_ASSERT(CallDescrData__pSrc                 == offsetof(CallDescrData, pSrc))
ASMCONSTANTS_C_ASSERT(CallDescrData__numStackSlots        == offsetof(CallDescrData, numStackSlots))
ASMCONSTANTS_C_ASSERT(CallDescrData__pArgumentRegisters   == offsetof(CallDescrData, pArgumentRegisters))
ASMCONSTANTS_C_ASSERT(CallDescrData__pFloatArgumentRegisters == offsetof(CallDescrData, pFloatArgumentRegisters))
ASMCONSTANTS_C_ASSERT(CallDescrData__fpReturnSize         == offsetof(CallDescrData, fpReturnSize))
ASMCONSTANTS_C_ASSERT(CallDescrData__pTarget              == offsetof(CallDescrData, pTarget))
ASMCONSTANTS_C_ASSERT(CallDescrData__returnValue          == offsetof(CallDescrData, returnValue))

#define SIZEOF__FaultingExceptionFrame                  (SIZEOF__Frame + 0x8 + SIZEOF__CONTEXT)
#define FaultingExceptionFrame__m_fFilterExecuted       SIZEOF__Frame
ASMCONSTANTS_C_ASSERT(SIZEOF__FaultingExceptionFrame        == sizeof(FaultingExceptionFrame))
ASMCONSTANTS_C_ASSERT(FaultingExceptionFrame__m_fFilterExecuted == offsetof(FaultingExceptionFrame, m_fFilterExecuted))

// For JIT_PInvokeBegin and JIT_PInvokeEnd helpers
#define               Frame__m_Next 0x04
ASMCONSTANTS_C_ASSERT(Frame__m_Next == offsetof(Frame, m_Next))

#define               InlinedCallFrame__m_Datum 0x08
ASMCONSTANTS_C_ASSERT(InlinedCallFrame__m_Datum == offsetof(InlinedCallFrame, m_Datum))

#define               InlinedCallFrame__m_pCallSiteSP 0x0C
ASMCONSTANTS_C_ASSERT(InlinedCallFrame__m_pCallSiteSP == offsetof(InlinedCallFrame, m_pCallSiteSP))

#define               InlinedCallFrame__m_pCallerReturnAddress 0x10
ASMCONSTANTS_C_ASSERT(InlinedCallFrame__m_pCallerReturnAddress == offsetof(InlinedCallFrame, m_pCallerReturnAddress))

#define               InlinedCallFrame__m_pCalleeSavedFP 0x14
ASMCONSTANTS_C_ASSERT(InlinedCallFrame__m_pCalleeSavedFP == offsetof(InlinedCallFrame, m_pCalleeSavedFP))

#define               InlinedCallFrame__m_pThread 0x18
ASMCONSTANTS_C_ASSERT(InlinedCallFrame__m_pThread == offsetof(InlinedCallFrame, m_pThread))

#undef ASMCONSTANTS_RUNTIME_ASSERT
#undef ASMCONSTANTS_C_ASSERT
