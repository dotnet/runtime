//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// asmconstants.h -
//
// This header defines field offsets and constants used by assembly code
// Be sure to rebuild clr/src/vm/ceemain.cpp after changing this file, to
// ensure that the constants match the expected C/C++ values

// #ifndef _ARM64_
// #error this file should only be used on an ARM platform
// #endif // _ARM64_

#include "..\..\inc\switches.h"

//-----------------------------------------------------------------------------

#ifndef ASMCONSTANTS_C_ASSERT
#define ASMCONSTANTS_C_ASSERT(cond)
#endif

#ifndef ASMCONSTANTS_RUNTIME_ASSERT
#define ASMCONSTANTS_RUNTIME_ASSERT(cond)
#endif

#define               Thread__m_fPreemptiveGCDisabled   0x0C
#define               Thread__m_pFrame                  0x10

#ifndef CROSSGEN_COMPILE
ASMCONSTANTS_C_ASSERT(Thread__m_fPreemptiveGCDisabled == offsetof(Thread, m_fPreemptiveGCDisabled));
ASMCONSTANTS_C_ASSERT(Thread__m_pFrame == offsetof(Thread, m_pFrame));
#endif // CROSSGEN_COMPILE

#define Thread_m_pFrame Thread__m_pFrame
#define Thread_m_fPreemptiveGCDisabled Thread__m_fPreemptiveGCDisabled

#ifndef CROSSGEN_COMPILE
#define               Thread__m_pDomain                 0x20
ASMCONSTANTS_C_ASSERT(Thread__m_pDomain == offsetof(Thread, m_pDomain));

#define               AppDomain__m_dwId                 0x08
ASMCONSTANTS_C_ASSERT(AppDomain__m_dwId == offsetof(AppDomain, m_dwId));
#endif

#define METHODDESC_REGISTER            x12

#define SIZEOF__ArgumentRegisters 0x40
ASMCONSTANTS_C_ASSERT(SIZEOF__ArgumentRegisters == sizeof(ArgumentRegisters))

#define SIZEOF__FloatArgumentRegisters 0x40
ASMCONSTANTS_C_ASSERT(SIZEOF__FloatArgumentRegisters == sizeof(FloatArgumentRegisters))

#define CallDescrData__pSrc                0x00
#define CallDescrData__numStackSlots       0x08
#define CallDescrData__pArgumentRegisters  0x10
#define CallDescrData__pFloatArgumentRegisters 0x18
#define CallDescrData__fpReturnSize        0x20
#define CallDescrData__pTarget             0x28
#define CallDescrData__returnValue         0x30

ASMCONSTANTS_C_ASSERT(CallDescrData__pSrc                 == offsetof(CallDescrData, pSrc))
ASMCONSTANTS_C_ASSERT(CallDescrData__numStackSlots        == offsetof(CallDescrData, numStackSlots))
ASMCONSTANTS_C_ASSERT(CallDescrData__pArgumentRegisters   == offsetof(CallDescrData, pArgumentRegisters))
ASMCONSTANTS_C_ASSERT(CallDescrData__pFloatArgumentRegisters == offsetof(CallDescrData, pFloatArgumentRegisters))
ASMCONSTANTS_C_ASSERT(CallDescrData__fpReturnSize         == offsetof(CallDescrData, fpReturnSize))
ASMCONSTANTS_C_ASSERT(CallDescrData__pTarget              == offsetof(CallDescrData, pTarget))
ASMCONSTANTS_C_ASSERT(CallDescrData__returnValue          == offsetof(CallDescrData, returnValue))

#define                  CORINFO_NullReferenceException_ASM 0
ASMCONSTANTS_C_ASSERT(   CORINFO_NullReferenceException_ASM
                      == CORINFO_NullReferenceException);


// Offset of the array containing the address of captured registers in MachState
#define MachState__captureX19_X28 0x0
ASMCONSTANTS_C_ASSERT(MachState__captureX19_X28 == offsetof(MachState, captureX19_X28))

// Offset of the array containing the address of preserved registers in MachState
#define MachState__ptrX19_X28 0x50
ASMCONSTANTS_C_ASSERT(MachState__ptrX19_X28 == offsetof(MachState, ptrX19_X28))

#define MachState__isValid 0xb8
ASMCONSTANTS_C_ASSERT(MachState__isValid == offsetof(MachState, _isValid))

#define LazyMachState_captureX19_X28 MachState__captureX19_X28
ASMCONSTANTS_C_ASSERT(LazyMachState_captureX19_X28 == offsetof(LazyMachState, captureX19_X28))

#define LazyMachState_captureSp     (MachState__isValid+8) // padding for alignment
ASMCONSTANTS_C_ASSERT(LazyMachState_captureSp == offsetof(LazyMachState, captureSp))

#define LazyMachState_captureIp     (LazyMachState_captureSp+8)
ASMCONSTANTS_C_ASSERT(LazyMachState_captureIp == offsetof(LazyMachState, captureIp))

#define LazyMachState_captureFp     (LazyMachState_captureSp+16)
ASMCONSTANTS_C_ASSERT(LazyMachState_captureFp == offsetof(LazyMachState, captureFp))

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


#ifdef FEATURE_COMINTEROP

#define SIZEOF__ComMethodFrame 0x68
ASMCONSTANTS_C_ASSERT(SIZEOF__ComMethodFrame == sizeof(ComMethodFrame));

#define UnmanagedToManagedFrame__m_pvDatum 0x10
ASMCONSTANTS_C_ASSERT(UnmanagedToManagedFrame__m_pvDatum == offsetof(UnmanagedToManagedFrame, m_pvDatum));

#endif // FEATURE_COMINTEROP


#define UMEntryThunk__m_pUMThunkMarshInfo 0x18
ASMCONSTANTS_C_ASSERT(UMEntryThunk__m_pUMThunkMarshInfo == offsetof(UMEntryThunk, m_pUMThunkMarshInfo))

#define UMEntryThunk__m_dwDomainId 0x20
ASMCONSTANTS_C_ASSERT(UMEntryThunk__m_dwDomainId == offsetof(UMEntryThunk, m_dwDomainId))

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

#undef ASMCONSTANTS_RUNTIME_ASSERT
#undef ASMCONSTANTS_C_ASSERT
