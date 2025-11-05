// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// See makefile.inc.  During the build, this file is converted into a .inc
// file for inclusion by .asm files.  The #defines are converted into EQU's.
//
// Allow multiple inclusion.


#ifndef TARGET_S390X
#error this file should only be used on an S390X platform
#endif // TARGET_S390X

#include "../../inc/switches.h"

#ifndef ASMCONSTANTS_C_ASSERT
#define ASMCONSTANTS_C_ASSERT(cond)
#endif

#ifndef ASMCONSTANTS_RUNTIME_ASSERT
#define ASMCONSTANTS_RUNTIME_ASSERT(cond)
#endif

#define ASMCONSTANT_OFFSETOF_ASSERT(struct, member) \
ASMCONSTANTS_C_ASSERT(OFFSETOF__##struct##__##member == offsetof(struct, member));

#define ASMCONSTANT_SIZEOF_ASSERT(classname) \
ASMCONSTANTS_C_ASSERT(SIZEOF__##classname == sizeof(classname));

#define DynamicHelperFrameFlags_Default     0
#define DynamicHelperFrameFlags_ObjectArg   1
#define DynamicHelperFrameFlags_ObjectArg2  2

#define METHODDESC_REGNUM                     0
#define METHODDESC_REGISTER                 %r0

#if 0

#define PINVOKE_CALLI_TARGET_REGNUM          10
#define PINVOKE_CALLI_TARGET_REGISTER       r10

#define PINVOKE_CALLI_SIGTOKEN_REGNUM        11
#define PINVOKE_CALLI_SIGTOKEN_REGISTER     r11

#define SIZEOF_GSCookie                             0x8
ASMCONSTANTS_C_ASSERT(SIZEOF_GSCookie == sizeof(GSCookie));

#define               OFFSETOF__Frame__m_Next       0x8
ASMCONSTANTS_C_ASSERT(OFFSETOF__Frame__m_Next
                    == offsetof(Frame, m_Next));

#define               OFFSETOF__Thread__m_fPreemptiveGCDisabled     0x0C
ASMCONSTANTS_C_ASSERT(OFFSETOF__Thread__m_fPreemptiveGCDisabled
                    == offsetof(Thread, m_fPreemptiveGCDisabled));
#define Thread_m_fPreemptiveGCDisabled OFFSETOF__Thread__m_fPreemptiveGCDisabled

#define               OFFSETOF__Thread__m_pFrame                    0x10
ASMCONSTANTS_C_ASSERT(OFFSETOF__Thread__m_pFrame
                    == offsetof(Thread, m_pFrame));
#define Thread_m_pFrame OFFSETOF__Thread__m_pFrame

#endif

#define           OFFSETOF__DelegateObject___methodPtr      0x18
ASMCONSTANT_OFFSETOF_ASSERT(DelegateObject, _methodPtr);

#define           OFFSETOF__DelegateObject___target         0x08
ASMCONSTANT_OFFSETOF_ASSERT(DelegateObject, _target);

#define                  CORINFO_NullReferenceException_ASM 0
ASMCONSTANTS_C_ASSERT(   CORINFO_NullReferenceException_ASM
                      == CORINFO_NullReferenceException);

// MachState offsets (s390x/gmscpu.h)

#define               OFFSETOF__MachState__m_Rip            0x00
ASMCONSTANTS_C_ASSERT(OFFSETOF__MachState__m_Rip
                    == offsetof(MachState, m_Rip));

#define               OFFSETOF__MachState__m_Rsp            0x08
ASMCONSTANTS_C_ASSERT(OFFSETOF__MachState__m_Rsp
                    == offsetof(MachState, m_Rsp));

#define               OFFSETOF__MachState__m_Capture        0x10
ASMCONSTANTS_C_ASSERT(OFFSETOF__MachState__m_Capture
                    == offsetof(MachState, m_Capture));

#define               OFFSETOF__MachState__m_Ptrs           0x50
#define               OFFSETOF__MachState___pRetAddr        0x90
#define               OFFSETOF__LazyMachState__m_CaptureRip 0xD8
#define               OFFSETOF__LazyMachState__m_CaptureRsp 0xE0
ASMCONSTANTS_C_ASSERT(OFFSETOF__MachState__m_Ptrs
                    == offsetof(MachState, m_Ptrs));
ASMCONSTANTS_C_ASSERT(OFFSETOF__MachState___pRetAddr
                    == offsetof(MachState, _pRetAddr));
ASMCONSTANTS_C_ASSERT(OFFSETOF__LazyMachState__m_CaptureRip
                    == offsetof(LazyMachState, m_CaptureRip));
ASMCONSTANTS_C_ASSERT(OFFSETOF__LazyMachState__m_CaptureRsp
                    == offsetof(LazyMachState, m_CaptureRsp));

#if 0

#define               OFFSETOF__VASigCookie__pNDirectILStub     0x8
ASMCONSTANTS_C_ASSERT(OFFSETOF__VASigCookie__pNDirectILStub
                    == offsetof(VASigCookie, pNDirectILStub));

#define               SIZEOF__FaultingExceptionFrame  (0x20 + SIZEOF__CONTEXT)
ASMCONSTANTS_C_ASSERT(SIZEOF__FaultingExceptionFrame
                    == sizeof(FaultingExceptionFrame));

#define               OFFSETOF__FaultingExceptionFrame__m_fFilterExecuted 0x10
ASMCONSTANTS_C_ASSERT(OFFSETOF__FaultingExceptionFrame__m_fFilterExecuted
                    == offsetof(FaultingExceptionFrame, m_fFilterExecuted));

// For JIT_PInvokeBegin and JIT_PInvokeEnd helpers
#define               OFFSETOF__InlinedCallFrame__m_Datum 0x10
ASMCONSTANTS_C_ASSERT(OFFSETOF__InlinedCallFrame__m_Datum
                    == offsetof(InlinedCallFrame, m_Datum));

#define               OFFSETOF__InlinedCallFrame__m_pCallSiteSP 0x20
ASMCONSTANTS_C_ASSERT(OFFSETOF__InlinedCallFrame__m_pCallSiteSP
                    == offsetof(InlinedCallFrame, m_pCallSiteSP));

#define               OFFSETOF__InlinedCallFrame__m_pCallerReturnAddress 0x28
ASMCONSTANTS_C_ASSERT(OFFSETOF__InlinedCallFrame__m_pCallerReturnAddress
                    == offsetof(InlinedCallFrame, m_pCallerReturnAddress));

#define               OFFSETOF__InlinedCallFrame__m_pCalleeSavedFP 0x30
ASMCONSTANTS_C_ASSERT(OFFSETOF__InlinedCallFrame__m_pCalleeSavedFP
                    == offsetof(InlinedCallFrame, m_pCalleeSavedFP));

#define               OFFSETOF__InlinedCallFrame__m_pThread 0x38
ASMCONSTANTS_C_ASSERT(OFFSETOF__InlinedCallFrame__m_pThread
                    == offsetof(InlinedCallFrame, m_pThread));

#endif

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

#define FixupPrecodeData__Target 0x00
ASMCONSTANTS_C_ASSERT(FixupPrecodeData__Target            == offsetof(FixupPrecodeData, Target))

#define FixupPrecodeData__MethodDesc 0x08
ASMCONSTANTS_C_ASSERT(FixupPrecodeData__MethodDesc        == offsetof(FixupPrecodeData, MethodDesc))

#define FixupPrecodeData__PrecodeFixupThunk 0x10
ASMCONSTANTS_C_ASSERT(FixupPrecodeData__PrecodeFixupThunk == offsetof(FixupPrecodeData, PrecodeFixupThunk))

#define StubPrecodeData__Target 0x08
ASMCONSTANTS_C_ASSERT(StubPrecodeData__Target            == offsetof(StubPrecodeData, Target))

#define StubPrecodeData__MethodDesc 0x00
ASMCONSTANTS_C_ASSERT(StubPrecodeData__MethodDesc        == offsetof(StubPrecodeData, MethodDesc))

#define CallCountingStubData__RemainingCallCountCell 0x00
ASMCONSTANTS_C_ASSERT(CallCountingStubData__RemainingCallCountCell == offsetof(CallCountingStubData, RemainingCallCountCell))

#define CallCountingStubData__TargetForMethod 0x08
ASMCONSTANTS_C_ASSERT(CallCountingStubData__TargetForMethod == offsetof(CallCountingStubData, TargetForMethod))

#define CallCountingStubData__TargetForThresholdReached 0x10
ASMCONSTANTS_C_ASSERT(CallCountingStubData__TargetForThresholdReached == offsetof(CallCountingStubData, TargetForThresholdReached))

#undef ASMCONSTANTS_RUNTIME_ASSERT
#undef ASMCONSTANTS_C_ASSERT
