;
; Copyright (c) Microsoft. All rights reserved.
; Licensed under the MIT license. See LICENSE file in the project root for full license information. 
;

;; ==++==
;;

;;
;; ==--==
#include "ksarm.h"

#include "asmconstants.h"

#include "asmmacros.h"

    SETALIAS CTPMethodTable__s_pThunkTable, ?s_pThunkTable@CTPMethodTable@@0PAVMethodTable@@A
    SETALIAS g_pObjectClass, ?g_pObjectClass@@3PAVMethodTable@@A

    IMPORT GetThread
    IMPORT JIT_InternalThrow
    IMPORT JIT_WriteBarrier
    IMPORT TheUMEntryPrestubWorker
    IMPORT CreateThreadBlockThrow
    IMPORT UMThunkStubRareDisableWorker
    IMPORT UM2MDoADCallBack
    IMPORT PreStubWorker
    IMPORT NDirectImportWorker
    IMPORT ObjIsInstanceOfNoGC
    IMPORT ArrayStoreCheck
    IMPORT VSD_ResolveWorker
    IMPORT $g_pObjectClass

#ifdef WRITE_BARRIER_CHECK
    SETALIAS g_GCShadow, ?g_GCShadow@@3PAEA
    SETALIAS g_GCShadowEnd, ?g_GCShadowEnd@@3PAEA

    IMPORT g_lowest_address
    IMPORT $g_GCShadow
    IMPORT $g_GCShadowEnd
#endif // WRITE_BARRIER_CHECK


#ifdef FEATURE_REMOTING
    IMPORT $CTPMethodTable__s_pThunkTable
    IMPORT VSD_GetTargetForTPWorker
    IMPORT VSD_GetTargetForTPWorkerQuick
    IMPORT TransparentProxyStubWorker
#endif
#ifdef FEATURE_COMINTEROP
    IMPORT CLRToCOMWorker
    IMPORT ComPreStubWorker
    IMPORT COMToCLRWorker
#endif
    IMPORT CallDescrWorkerUnwindFrameChainHandler
    IMPORT UMEntryPrestubUnwindFrameChainHandler
    IMPORT UMThunkStubUnwindFrameChainHandler
#ifdef FEATURE_COMINTEROP
    IMPORT ReverseComUnwindFrameChainHandler
#endif

#ifdef FEATURE_HIJACK
    IMPORT OnHijackObjectWorker
    IMPORT OnHijackInteriorPointerWorker
    IMPORT OnHijackScalarWorker
#endif ; FEATURE_HIJACK

    IMPORT GetCurrentSavedRedirectContext

#ifdef FEATURE_MIXEDMODE
    SETALIAS IJWNOADThunk__FindThunkTarget, ?FindThunkTarget@IJWNOADThunk@@QAAPBXXZ
    IMPORT  $IJWNOADThunk__FindThunkTarget
#endif

    ;; Imports to support virtual import fixup for ngen images
    IMPORT VirtualMethodFixupWorker
    ;; Import to support cross-moodule external method invocation in ngen images
    IMPORT ExternalMethodFixupWorker
    IMPORT StubDispatchFixupWorker

#ifdef FEATURE_READYTORUN
    IMPORT DynamicHelperWorker
#endif

    IMPORT JIT_RareDisableHelperWorker
    IMPORT DoJITFailFast
    IMPORT s_gsCookie
    IMPORT g_TrapReturningThreads

    ;; Imports for singleDomain statics helpers
    IMPORT JIT_GetSharedNonGCStaticBase_Helper
    IMPORT JIT_GetSharedGCStaticBase_Helper
    
    TEXTAREA

;; LPVOID __stdcall GetCurrentIP(void);
    LEAF_ENTRY GetCurrentIP
        mov     r0, lr
        bx      lr
    LEAF_END

;; LPVOID __stdcall GetCurrentSP(void);
    LEAF_ENTRY GetCurrentSP
        mov     r0, sp
        bx      lr
    LEAF_END

;;-----------------------------------------------------------------------------
;; This helper routine enregisters the appropriate arguments and makes the
;; actual call.
;;-----------------------------------------------------------------------------
;;void CallDescrWorkerInternal(CallDescrData * pCallDescrData);
        NESTED_ENTRY CallDescrWorkerInternal,,CallDescrWorkerUnwindFrameChainHandler
        PROLOG_PUSH         {r4,r5,r7,lr}
        PROLOG_STACK_SAVE   r7

        mov     r5,r0 ; save pCallDescrData in r5

        ldr     r1, [r5,#CallDescrData__numStackSlots]
        cbz     r1, Ldonestack

        ;; Add frame padding to ensure frame size is a multiple of 8 (a requirement of the OS ABI).
        ;; We push four registers (above) and numStackSlots arguments (below). If this comes to an odd number
        ;; of slots we must pad with another. This simplifies to "if the low bit of numStackSlots is set,
        ;; extend the stack another four bytes".
        lsls    r2, r1, #2
        and     r3, r2, #4
        sub     sp, sp, r3

        ;; This loop copies numStackSlots words
        ;; from [pSrcEnd-4,pSrcEnd-8,...] to [sp-4,sp-8,...]
        ldr     r0, [r5,#CallDescrData__pSrc]
        add     r0,r0,r2
Lstackloop
        ldr     r2, [r0,#-4]!
        str     r2, [sp,#-4]!
        subs    r1, r1, #1
        bne     Lstackloop
Ldonestack

        ;; If FP arguments are supplied in registers (r3 != NULL) then initialize all of them from the pointer
        ;; given in r3. Do not use "it" since it faults in floating point even when the instruction is not executed.
        ldr     r3, [r5,#CallDescrData__pFloatArgumentRegisters]
        cbz     r3, LNoFloatingPoint
        vldm    r3, {s0-s15}
LNoFloatingPoint

        ;; Copy [pArgumentRegisters, ..., pArgumentRegisters + 12]
        ;; into r0, ..., r3

        ldr     r4, [r5,#CallDescrData__pArgumentRegisters]
        ldm     r4, {r0-r3}

        CHECK_STACK_ALIGNMENT

        ;; call pTarget
        ;; Note that remoting expect target in r4.
        ldr     r4, [r5,#CallDescrData__pTarget]
        blx     r4

        ldr     r3, [r5,#CallDescrData__fpReturnSize]

        ;; Save FP return value if appropriate
        cbz     r3, LFloatingPointReturnDone

        ;; Float return case
        ;; Do not use "it" since it faults in floating point even when the instruction is not executed.
        cmp     r3, #4
        bne     LNoFloatReturn
        vmov    r0, s0
        b       LFloatingPointReturnDone
LNoFloatReturn

        ;; Double return case
        ;; Do not use "it" since it faults in floating point even when the instruction is not executed.
        cmp     r3, #8
        bne     LNoDoubleReturn
        vmov    r0, r1, s0, s1
        b       LFloatingPointReturnDone
LNoDoubleReturn

        add     r2, r5, #CallDescrData__returnValue

        cmp     r3, #16
        bne     LNoFloatHFAReturn
        vstm    r2, {s0-s3}
        b       LReturnDone
LNoFloatHFAReturn

        cmp     r3, #32
        bne     LNoDoubleHFAReturn
        vstm    r2, {d0-d3}
        b       LReturnDone
LNoDoubleHFAReturn

        EMIT_BREAKPOINT ; Unreachable

LFloatingPointReturnDone

        ;; Save return value into retbuf
        str     r0, [r5, #(CallDescrData__returnValue + 0)]
        str     r1, [r5, #(CallDescrData__returnValue + 4)]

LReturnDone

#ifdef _DEBUG
        ;; trash the floating point registers to ensure that the HFA return values 
        ;; won't survive by accident
        vldm    sp, {d0-d3}
#endif

        EPILOG_STACK_RESTORE    r7
        EPILOG_POP              {r4,r5,r7,pc}

        NESTED_END


;;-----------------------------------------------------------------------------
;; This helper routine is where returns for irregular tail calls end up
:: so they can dynamically pop their stack arguments.
;;-----------------------------------------------------------------------------
; 
; Stack Layout (stack grows up, 0 at the top, offsets relative to frame pointer, r7):
; 
; sp ->     callee stack arguments
;           :
;           :
;    -0Ch   gsCookie
; TailCallHelperFrame ->
;    -08h   __VFN_table
;    -04h   m_Next
; r7 ->
;    +00h   m_calleeSavedRgisters.r4
;    +04h                        .r5
;    +08h                        .r6
;    +0Ch                        .r7
;    +10h                        .r8
;    +14h                        .r9
;    +18h                        .r10
; r11->
;    +1Ch                        .r11
;    +20h                        .r14 -or- m_ReturnAddress
;
; r6 -> GetThread()
; r5 -> r6->m_pFrame (old Frame chain head)
; r11 is used to preserve the ETW call stack

    NESTED_ENTRY TailCallHelperStub
        ;
        ; This prolog is never executed, but we keep it here for reference
        ; and for the unwind data it generates
        ;
        
        ; Spill callee saved registers and return address.
        PROLOG_PUSH         {r4-r11,lr}
        
        PROLOG_STACK_SAVE   r7

        ;
        ; This is the code that would have to run to setup this frame
        ; like the C++ helper does before calling RtlRestoreContext
        ;
        ; Allocate space for the rest of the frame and GSCookie.
        ; PROLOG_STACK_ALLOC  0x0C
        ;
        ; Set r11 for frame chain
        ;add     r11, r7, 0x1C 
        ;
        ; Set the vtable for TailCallFrame
        ;bl      TCF_GETMETHODFRAMEVPTR
        ;str     r0, [r7, #-8]
        ;
        ; Initialize the GSCookie within the Frame
        ;ldr     r0, =s_gsCookie
        ;str     r0, [r7, #-0x0C]
        ;
        ; Link the TailCallFrameinto the Frame chain
        ; and initialize r5 & r6 for unlinking later
        ;CALL_GETTHREAD
        ;mov     r6, r0
        ;ldr     r5, [r6, #Thread__m_pFrame]        
        ;str     r5, [r7, #-4]
        ;sub     r0, r7, 8
        ;str     r0, [r6, #Thread__m_pFrame]
        ;
        ; None of the previous stuff is ever executed,
        ; but we keep it here for reference
        ;

        ;
        ; Here's the pretend call (make it real so the unwinder
        ; doesn't think we're in the prolog)
        ;
        bl      TailCallHelperStub
        ;
        ; with the real return address pointing to this real epilog
        ;
JIT_TailCallHelperStub_ReturnAddress
        EXPORT JIT_TailCallHelperStub_ReturnAddress

        ;
        ; Our epilog (which also unlinks the StubHelperFrame)
        ; Be careful not to trash the return registers
        ;

#ifdef _DEBUG
        ldr     r3, =s_gsCookie
        ldr     r3, [r3]
        ldr     r2, [r7, #-0x0C]
        cmp     r2, r3
        beq     GoodGSCookie
        bl      DoJITFailFast
GoodGSCookie
#endif ; _DEBUG

        ;
        ; unlink the TailCallFrame
        ;
        str     r5, [r6, #Thread__m_pFrame]

        ; 
        ; epilog
        ;
        EPILOG_STACK_RESTORE    r7
        EPILOG_POP              {r4-r11,lr}
        EPILOG_RETURN
        
    NESTED_END

; ------------------------------------------------------------------

; void LazyMachStateCaptureState(struct LazyMachState *pState);
    LEAF_ENTRY LazyMachStateCaptureState

        ;; marks that this is not yet valid
        mov     r1, #0
        str     r1, [r0, #MachState__isValid]

        str     lr, [r0, #LazyMachState_captureIp]
        str     sp, [r0, #LazyMachState_captureSp]

        add     r1, r0, #LazyMachState_captureR4_R11
        stm     r1, {r4-r11}

        mov     pc, lr

    LEAF_END

; void SinglecastDelegateInvokeStub(Delegate *pThis)
    LEAF_ENTRY SinglecastDelegateInvokeStub
        cmp     r0, #0
        beq     LNullThis

        ldr     r12, [r0, #DelegateObject___methodPtr]
        ldr     r0, [r0, #DelegateObject___target]

        bx      r12

LNullThis
        mov     r0, #CORINFO_NullReferenceException_ASM
        b       JIT_InternalThrow

    LEAF_END

;
; r12 = UMEntryThunk*
;
        NESTED_ENTRY TheUMEntryPrestub,,UMEntryPrestubUnwindFrameChainHandler

        PROLOG_PUSH         {r0-r4,lr}
        PROLOG_VPUSH        {d0-d7}

        CHECK_STACK_ALIGNMENT

        mov     r0, r12
        bl      TheUMEntryPrestubWorker

        ; Record real target address in r12.
        mov     r12, r0

        ; Epilog
        EPILOG_VPOP         {d0-d7}
        EPILOG_POP          {r0-r4,lr}
        EPILOG_BRANCH_REG   r12

        NESTED_END

;
; r12 = UMEntryThunk*
;
        NESTED_ENTRY UMThunkStub,,UMThunkStubUnwindFrameChainHandler
        PROLOG_PUSH         {r4,r5,r7,r11,lr}
        PROLOG_PUSH         {r0-r3,r12}
        PROLOG_STACK_SAVE   r7

        GBLA UMThunkStub_HiddenArg ; offset of saved UMEntryThunk *
        GBLA UMThunkStub_StackArgs ; offset of original stack args (total size of UMThunkStub frame)
UMThunkStub_HiddenArg SETA 4*4
UMThunkStub_StackArgs SETA 10*4

        CHECK_STACK_ALIGNMENT

        bl                  GetThread
        cbz                 r0, UMThunkStub_DoThreadSetup

UMThunkStub_HaveThread
        mov                 r5, r0                  ; r5 = Thread *

        ldr                 r2, =g_TrapReturningThreads

        mov                 r4, 1
        str                 r4, [r5, #Thread__m_fPreemptiveGCDisabled]

        ldr                 r3, [r2]
        cbnz                r3, UMThunkStub_DoTrapReturningThreads

UMThunkStub_InCooperativeMode
        ldr                 r12, [r7, #UMThunkStub_HiddenArg]

        ldr                 r0, [r5, #Thread__m_pDomain]
        ldr                 r1, [r12, #UMEntryThunk__m_dwDomainId]
        ldr                 r0, [r0, #AppDomain__m_dwId]
        ldr                 r3, [r12, #UMEntryThunk__m_pUMThunkMarshInfo]
        cmp                 r0, r1
        bne                 UMThunkStub_WrongAppDomain

        ldr                 r2, [r3, #UMThunkMarshInfo__m_cbActualArgSize]
        cbz                 r2, UMThunkStub_ArgumentsSetup

        add                 r0, r7, #UMThunkStub_StackArgs ; Source pointer
        add                 r0, r0, r2
        lsr                 r1, r2, #2      ; Count of stack slots to copy

        and                 r2, r2, #4      ; Align the stack
        sub                 sp, sp, r2

UMThunkStub_StackLoop
        ldr                 r2, [r0,#-4]!
        str                 r2, [sp,#-4]!
        subs                r1, r1, #1
        bne                 UMThunkStub_StackLoop

UMThunkStub_ArgumentsSetup
        ldr                 r4, [r3, #UMThunkMarshInfo__m_pILStub]

        ; reload argument registers
        ldm                 r7, {r0-r3}

        CHECK_STACK_ALIGNMENT

        blx                 r4

UMThunkStub_PostCall
        mov                 r4, 0
        str                 r4, [r5, #Thread__m_fPreemptiveGCDisabled]

        EPILOG_STACK_RESTORE r7
        EPILOG_STACK_FREE   4 * 5
        EPILOG_POP          {r4,r5,r7,r11,pc}

UMThunkStub_DoThreadSetup
        sub                 sp, #SIZEOF__FloatArgumentRegisters
        vstm                sp, {d0-d7}
        bl                  CreateThreadBlockThrow
        vldm                sp, {d0-d7}
        add                 sp, #SIZEOF__FloatArgumentRegisters
        b                   UMThunkStub_HaveThread

UMThunkStub_DoTrapReturningThreads
        sub                 sp, #SIZEOF__FloatArgumentRegisters
        vstm                sp, {d0-d7}
        mov                 r0, r5              ; Thread* pThread
        ldr                 r1, [r7, #UMThunkStub_HiddenArg]  ; UMEntryThunk* pUMEntry
        bl                  UMThunkStubRareDisableWorker
        vldm                sp, {d0-d7}
        add                 sp, #SIZEOF__FloatArgumentRegisters
        b                   UMThunkStub_InCooperativeMode

UMThunkStub_WrongAppDomain
        sub                 sp, #SIZEOF__FloatArgumentRegisters
        vstm                sp, {d0-d7}

        ldr                 r0, [r7, #UMThunkStub_HiddenArg]  ; UMEntryThunk* pUMEntry
        mov                 r2, r7              ; void * pArgs
        ; remaining arguments are unused
        bl                  UM2MDoADCallBack

        ; Restore non-FP return value.
        ldr                 r0, [r7, #0]
        ldr                 r1, [r7, #4]

        ; Restore FP return value or HFA.
        vldm                sp, {d0-d3}
        b                   UMThunkStub_PostCall

        NESTED_END

; UM2MThunk_WrapperHelper(void *pThunkArgs,             // r0
;                         int cbStackArgs,              // r1 (unused)
;                         void *pAddr,                  // r2 (unused)
;                         UMEntryThunk *pEntryThunk,    // r3
;                         Thread *pThread)              // [sp, #0]

        NESTED_ENTRY UM2MThunk_WrapperHelper

        PROLOG_PUSH         {r4-r7,r11,lr}
        PROLOG_STACK_SAVE   r7

        CHECK_STACK_ALIGNMENT

        mov                 r12, r3                     // r12 = UMEntryThunk *

        ;
        ; Note that layout of the arguments is given by UMThunkStub frame
        ;
        mov                 r5, r0                      // r5 = pArgs

        ldr                 r3, [r12, #UMEntryThunk__m_pUMThunkMarshInfo]
        
        ldr                 r2, [r3, #UMThunkMarshInfo__m_cbActualArgSize]
        cbz                 r2, UM2MThunk_WrapperHelper_ArgumentsSetup

        add                 r0, r5, #UMThunkStub_StackArgs ; Source pointer
        add                 r0, r0, r2
        lsr                 r1, r2, #2      ; Count of stack slots to copy

        and                 r2, r2, #4      ; Align the stack
        sub                 sp, sp, r2

UM2MThunk_WrapperHelper_StackLoop
        ldr                 r2, [r0,#-4]!
        str                 r2, [sp,#-4]!
        subs                r1, r1, #1
        bne                 UM2MThunk_WrapperHelper_StackLoop

UM2MThunk_WrapperHelper_ArgumentsSetup
        ldr                 r4, [r3, #UMThunkMarshInfo__m_pILStub]

        ; reload floating point registers
        sub                 r6, r5, #SIZEOF__FloatArgumentRegisters
        vldm                r6, {d0-d7}

        ; reload argument registers
        ldm                 r5, {r0-r3}

        CHECK_STACK_ALIGNMENT

        blx                 r4

        ; Save non-floating point return
        str                 r0, [r5, #0]
        str                 r1, [r5, #4]

        ; Save FP return value or HFA.
        vstm                r6, {d0-d3}

#ifdef _DEBUG
        ;; trash the floating point registers to ensure that the HFA return values 
        ;; won't survive by accident
        vldm                sp, {d0-d3}
#endif

        EPILOG_STACK_RESTORE r7
        EPILOG_POP          {r4-r7,r11,pc}

        NESTED_END

; ------------------------------------------------------------------
;
; IJWNOADThunk::MakeCall
;
; On entry:
;   r12 : IJWNOADThunk *
;
; On exit:
;  Tail calls to real managed target
;

#ifdef FEATURE_MIXEDMODE
        NESTED_ENTRY IJWNOADThunk__MakeCall

        ; Can't pass C++ mangled names to NESTED_ENTRY and my attempts to use EQU to define an alternate name
        ; for a symbol didn't work. Just define a label for the decorated name of the method and export it
        ; manually.
|?MakeCall@IJWNOADThunk@@KAXXZ|
        EXPORT |?MakeCall@IJWNOADThunk@@KAXXZ|

        PROLOG_PUSH {r0-r4,lr}
        PROLOG_VPUSH {d0-d7}

        CHECK_STACK_ALIGNMENT

        mov         r0, r12     ; IJWNOADThunk * is this pointer for IJWNOADThunk::FindThunkTarget
        bl          $IJWNOADThunk__FindThunkTarget
        mov         r12, r0     ; Returns real jump target in r0, save this in r12

        EPILOG_VPOP {d0-d7}
        EPILOG_POP  {r0-r4,lr}
        EPILOG_BRANCH_REG r12

        NESTED_END
#endif

; ------------------------------------------------------------------

        NESTED_ENTRY ThePreStub

        PROLOG_WITH_TRANSITION_BLOCK

        add         r0, sp, #__PWTB_TransitionBlock ; pTransitionBlock
        mov         r1, r12                         ; pMethodDesc

        bl          PreStubWorker

        mov         r12, r0

        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
        EPILOG_BRANCH_REG   r12

        NESTED_END

; ------------------------------------------------------------------
; This method does nothing. It's just a fixed function for the debugger to put a breakpoint on.
        LEAF_ENTRY ThePreStubPatch
        nop
ThePreStubPatchLabel
        EXPORT ThePreStubPatchLabel
        bx      lr
        LEAF_END

; ------------------------------------------------------------------
; The call in ndirect import precode points to this function.
        NESTED_ENTRY NDirectImportThunk

        PROLOG_PUSH {r0-r4,lr}                          ; Spill general argument registers, return address and
                                                        ; arbitrary register to keep stack aligned
        PROLOG_VPUSH {d0-d7}                            ; Spill floating point argument registers

        CHECK_STACK_ALIGNMENT

        mov     r0, r12
        bl      NDirectImportWorker
        mov     r12, r0

        EPILOG_VPOP {d0-d7}
        EPILOG_POP {r0-r4,lr}

        ; If we got back from NDirectImportWorker, the MD has been successfully
        ; linked. Proceed to execute the original DLL call.
        EPILOG_BRANCH_REG r12

        NESTED_END

; ------------------------------------------------------------------
; The call in fixup precode initally points to this function.
; The pupose of this function is to load the MethodDesc and forward the call the prestub.
        NESTED_ENTRY PrecodeFixupThunk

        ; r12 = FixupPrecode *

        PROLOG_PUSH     {r0-r1}

        ; Inline computation done by FixupPrecode::GetMethodDesc()
        ldrb    r0, [r12, #3]           ; m_PrecodeChunkIndex
        ldrb    r1, [r12, #2]           ; m_MethodDescChunkIndex

        add     r12,r12,r0,lsl #3
        add     r0,r12,r0,lsl #2
        ldr     r0, [r0,#8]
        add     r12,r0,r1,lsl #2

        EPILOG_POP      {r0-r1}
        EPILOG_BRANCH ThePreStub

        NESTED_END

; ------------------------------------------------------------------
; void ResolveWorkerAsmStub(r0, r1, r2, r3, r4:IndirectionCellAndFlags, r12:DispatchToken)
;
; The stub dispatch thunk which transfers control to VSD_ResolveWorker.
        NESTED_ENTRY ResolveWorkerAsmStub

        PROLOG_WITH_TRANSITION_BLOCK

        add         r0, sp, #__PWTB_TransitionBlock ; pTransitionBlock
        mov         r2, r12                         ; token

        ; indirection cell in r4 - should be consistent with REG_ARM_STUB_SPECIAL
        bic         r1, r4, #3          ; indirection cell
        and         r3, r4, #3          ; flags

        bl          VSD_ResolveWorker

        mov         r12, r0

        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
        EPILOG_BRANCH_REG   r12

        NESTED_END

; ------------------------------------------------------------------
; void ResolveWorkerChainLookupAsmStub(r0, r1, r2, r3, r4:IndirectionCellAndFlags, r12:DispatchToken)
        NESTED_ENTRY ResolveWorkerChainLookupAsmStub

        ; ARMSTUB TODO: implement chained lookup
        b           ResolveWorkerAsmStub

        NESTED_END

#if defined(FEATURE_REMOTING) || defined(FEATURE_COMINTEROP)

; ------------------------------------------------------------------
; setStubReturnValue
; r0 - size of floating point return value (MetaSig::GetFPReturnSize())
; r1 - pointer to the return buffer in the stub frame
        LEAF_ENTRY setStubReturnValue

        cbz     r0, NoFloatingPointRetVal

        ;; Float return case
        ;; Do not use "it" since it faults in floating point even when the instruction is not executed.
        cmp     r0, #4
        bne     LNoFloatRetVal
        vldr    s0, [r1]
        bx      lr
LNoFloatRetVal

        ;; Double return case
        ;; Do not use "it" since it faults in floating point even when the instruction is not executed.
        cmp     r0, #8
        bne     LNoDoubleRetVal
        vldr    d0, [r1]
        bx      lr
LNoDoubleRetVal

        cmp     r0, #16
        bne     LNoFloatHFARetVal
        vldm    r1, {s0-s3}
        bx      lr
LNoFloatHFARetVal

        cmp     r0, #32
        bne     LNoDoubleHFARetVal
        vldm    r1, {d0-d3}
        bx      lr
LNoDoubleHFARetVal

        EMIT_BREAKPOINT ; Unreachable

NoFloatingPointRetVal

        ;; Restore the return value from retbuf
        ldr     r0, [r1]
        ldr     r1, [r1, #4]
        bx      lr

        LEAF_END

#endif // FEATURE_REMOTING || FEATURE_COMINTEROP

#ifdef FEATURE_REMOTING

; ------------------------------------------------------------------
; Remoting stub used to dispatch a method invocation. This is the choke point for all remoting calls; all
; scenarios where we determine we're not a local or a COM call, regardless of whether the dispatch is
; interface, virtual or direct will wind up here sooner or later.
;
; On entry:
;   r0          : transparent proxy
;   r12         : target MethodDesc or slot number
;   plus user arguments in registers and on the stack
;
        NESTED_ENTRY TransparentProxyStub_CrossContext

        PROLOG_WITH_TRANSITION_BLOCK 0x20

        add         r0, sp, #__PWTB_TransitionBlock ; pTransitionBlock
        mov         r1, r12                         ; pMethodDesc

        bl          TransparentProxyStubWorker

        ; r0 = fpRetSize

        ; return value is stored before float argument registers
        add         r1, sp, #(__PWTB_FloatArgumentRegisters - 0x20)
        bl          setStubReturnValue

        EPILOG_WITH_TRANSITION_BLOCK_RETURN

        NESTED_END

; ------------------------------------------------------------------
; This method does nothing. It's just a fixed function for the debugger to put a breakpoint on.
        LEAF_ENTRY TransparentProxyStubPatch
        add r0, r1, r2
TransparentProxyStubPatchLabel
        EXPORT TransparentProxyStubPatchLabel
        bx      lr
        LEAF_END

; ------------------------------------------------------------------
; VSD helper for performing an in-context interface dispatch on a TransparentProxy. This only happens for
; ContextBoundObjects that are invoked in the correct context, never for general remoting.
;
; On entry:
;   r0          : transparent proxy
;   r12         : interface MethodDesc
;   plus user arguments in registers and on the stack
;
; On exit:
;   Tail calls to actual target which returns as normal to the caller.
;
        NESTED_ENTRY InContextTPQuickDispatchAsmStub

        ; Spill caller's volatile argument registers and some other state we wish to preserve.
        PROLOG_PUSH    {r0-r3,r12,lr}
        PROLOG_VPUSH   {d0-d7}

        CHECK_STACK_ALIGNMENT

        ; Set up arguments for VSD_GetTargetForTPWorkerQuick
        ; mov      r0, r0      ; this
        mov        r1, r12     ; Interface MethodDesc

        bl         VSD_GetTargetForTPWorkerQuick

        ; If we didn't find a target head for the slow path.
        cbz        r0, CacheMiss

        ; Save target address since we're about to restore the value of r0. Can't place it directly into r12
        ; since that's about to be restored as well. Instead we overwrite the saved version of r12 on the
        ; stack (we don't need it any more since the lookup succeeded).
        str        r0, [sp, #((16 * 4) + (4 * 4))]

        ; Restore caller's argument registers.
        EPILOG_VPOP    {d0-d7}
        EPILOG_POP     {r0-r3,r12,lr}

        ; Tail call to the real code using the previously computed target address.
        EPILOG_BRANCH_REG r12

CacheMiss
        ; Restore caller's argument registers.
        EPILOG_VPOP    {d0-d7}
        EPILOG_POP     {r0-r3,r12,lr}

        EPILOG_BRANCH  InContextTPDispatchAsmStub

        NESTED_END

; ------------------------------------------------------------------

        NESTED_ENTRY InContextTPDispatchAsmStub

        PROLOG_WITH_TRANSITION_BLOCK

        add         r0, sp, #__PWTB_TransitionBlock ; pTransitionBlock
        mov         r1, r12                         ; pMethodDesc / token

        bl          VSD_GetTargetForTPWorker

        mov         r12, r0

        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
        EPILOG_BRANCH_REG   r12

        NESTED_END

; ------------------------------------------------------------------
; Macro used to compare a MethodTable with that of __TransparentProxy. Sets the Z condition flag to indicate
; the result (Z=1 for a match, Z=0 for a mismatch).
;
    MACRO
        TP_TYPE_CHECK $methodTableReg, $scratchReg

        ldr         $scratchReg, =$CTPMethodTable__s_pThunkTable
        ldr         $scratchReg, [$scratchReg]
        cmp         $scratchReg, $methodTableReg
    MEND

; ------------------------------------------------------------------
; Macro used to perform a context check.
;
; Calls a user customizable routine that determines whether the current execution context warrants a context
; transition for the call. Regular remoting (as opposed to context transitioning based on ContextBoundObjects)
; always returns a context-mismatch from this call.
;
; On entry:
;   r0          : this (TranparentProxy object)
;
; On exit:
;   r0          : check result (0 == contexts match, non-zero == contexts mismatch)
;   r1-r3,r12,lr: trashed
;
    MACRO
        TP_CONTEXT_CHECK

        ldr         r1, [r0, #TransparentProxyObject___stub]
        ldr         r0, [r0, #TransparentProxyObject___stubData]
        blx         r1
    MEND

; ------------------------------------------------------------------
; Used by the remoting precode for non-virtual dispatch to instance methods which might be remoted. Performs a
; context and transparent proxy check and if both of these are negative (or the call has been made on a null
; 'this') we simply return and the precode will dispatch the call locally as normal. Otherwise we redirect to
; the remoting system and never return.
;
; On entry:
;   r0          : this (may or may not be a TransparentProxy)
;   r1          : trashed
;   lr          : return address into RemotingPrecode (RemotingPrecode* + REMOTING_PRECODE_RET_OFFSET)
;   [sp, #0]    : caller's saved r1
;   [sp, #4]    : caller's saved lr (i.e. return address into caller of RemotingPrecode)
;   plus user arguments in registers and on the stack
;
        LEAF_ENTRY PrecodeRemotingThunk

        ; Send null 'this' case to local dispatch case (else we'd need to handle an A/V from this stub).
        cbz         r0, LocalDispatch   ; predicted not taken

        ; Load MethodTable* in r12.
        ldr         r12, [r0]

        ; Compare MethodTable in 'this' with that of __TransparentProxy; if they're not equal we dispatch
        ; locally.
        TP_TYPE_CHECK r12, r1   ; r1 is a scratch register
        beq         TransparentProxyDispatch    ; predicted not taken

LocalDispatch
        ; Recover target MethodDesc pointer from the RemotingPrecode (we have the address of this +
        ; REMOTING_PRECODE_RET_OFFSET in lr). Subtract extra 1 to account for the low-bit being set in LR to
        ; indicate thumb mode.
        ; We do this here because even the local case needs r12 initialized.
        ldr         r12, [lr, #(RemotingPrecode__m_pMethodDesc - REMOTING_PRECODE_RET_OFFSET - 1)]

        bx          lr

        LEAF_END

; ------------------------------------------------------------------
; Handles the atypical path for the remoting precode above (typically the non-local dispatch cases). The
; regular entry point defined by NESTED_ENTRY below is never called directly; it serves only to generate
; prolog unwind data matching the pushes of the caller's r1 and lr done in the remoting precode so we can
; unwind out of this frame. The real entry point is TransparentProxyDispatch called directly from
; PrecodeRemotingThunk.
;
        NESTED_ENTRY TransparentProxyDispatch_FakeProlog

        ; Match what the remoting precode has pushed.
        PROLOG_PUSH {r1,lr}

        ; This is where execution really starts.
TransparentProxyDispatch

        ; We need some temporary registers and to preserve lr.
        PROLOG_PUSH     {r0,r2-r5,lr}

        CHECK_STACK_ALIGNMENT

        ; Recover target MethodDesc pointer from the RemotingPrecode (we have the address of this +
        ; REMOTING_PRECODE_RET_OFFSET in lr). Subtract extra 1 to account for the low-bit being set in LR to
        ; indicate thumb mode. Stash the result in a non-volatile register to preserve it over the call to
        ; TP_CONTEXT_CHECK below.
        ldr         r4, [lr, #(RemotingPrecode__m_pMethodDesc - REMOTING_PRECODE_RET_OFFSET - 1)]

        ; Check whether the TP is already in the correct context. This can happen for ContextBoundObjects
        ; only. The following macro will trash volatile registers and lr and return the result in r0 (0 ==
        ; context match, non-zero for everything else). All other registers are preserved.
        TP_CONTEXT_CHECK

        ; Place MethodDesc* in r12 ready for wherever we dispatch to next.
        mov         r12, r4

        ; Check the result of TP_CONTEXT_CHECK
        cbnz        r0, ContextMismatch1

        ; At this point we know we're being called on a transparent proxy but the source and destination
        ; contexts match. This only happens for a ContextBoundObject. For an non-interface dispatch we can
        ; just return to the local dispatch case; the precode will eventually redirect to the jitted code
        ; which knows how to handle a TP-wrapped ContextBoundObject. For interface calls we need to hand off
        ; to VSD so it can resolve to the real target method. The quickest way to determine which of these
        ; cases we need is to look at the classification of the method desc. All interface methods for which a
        ; remoting precode is used are marked as mcComInterop, which though non-intuitive is generally OK
        ; since only COM interop and remoting can dispatch directly on an interface method desc. (Generic
        ; interface methods are not classified as mcComInterop but we use a different mechanism to intercept
        ; those).
        ldrh        r0, [r4, #MethodDesc__m_wFlags]
        and         r0, #MethodDesc__mdcClassification
        cmp         r0, #MethodDesc__mcComInterop
        bne         LocalDispatch1

        ; Local interface dispatch case. Restore argument registers saved here and in the RemotingPrecode,
        ; discard return address into the RemotingPrecode (we're not going back there) and restore the real
        ; caller's return address to LR before tail calling into the interface dispatch helper.
        EPILOG_POP      {r0,r2-r5,lr}   ; Restore arg registers saved by this routine and RemotingPrecode lr
        EPILOG_POP      {r1,lr}         ; Restore r1 saved by RemotingPrecode and real return address
        EPILOG_BRANCH   InContextTPQuickDispatchAsmStub

LocalDispatch1

        ; Local dispatch case. Restore argument registers saved here and return to the remoting precode.
        EPILOG_POP      {r0,r2-r5,pc}

ContextMismatch1
        ; Context-mismatch (remoted) dispatch case. Restore argument registers saved here and in the
        ; RemotingPrecode, discard return address into the RemotingPrecode (we're not going back there) and
        ; restore the real caller's return address to LR before tail calling into the cross-context helper.
        EPILOG_POP      {r0,r2-r5,lr}   ; Restore arg registers saved by this routine and RemotingPrecode lr
        EPILOG_POP      {r1,lr}         ; Restore r1 saved by RemotingPrecode and real return address
        EPILOG_BRANCH   TransparentProxyStub_CrossContext

        NESTED_END

; ------------------------------------------------------------------
; Used to dispatch an interface call that is possibly be cross-context or remoted. Normally this is handled
; by the remoting precode stub above but there is an edge case for generic interface methods that falls
; through the cracks (it is not easy to cover since the precode stub makes use of it as a quick means
; to differentiate between interface and non-interface calls in the non-cross context case).
;
; On entry:
;   r0          : this (TransparentProxy object)
;   r12         : interface MethodDesc
;   plus user arguments in registers and on the stack
;
; On exit:
;   Tail calls to the VSD in-context TP dispatcher or remoting system as appropriate.
;
        NESTED_ENTRY CRemotingServices__DispatchInterfaceCall

        PROLOG_PUSH     {r0-r3,r12,lr}

        CHECK_STACK_ALIGNMENT

        ; Check whether the TP is already in the correct context. This can happen for ContextBoundObjects
        ; only. The following macro will trash volatile registers and lr and return the result in r0 (0 ==
        ; context match, non-zero for everything else). All other registers are preserved.
        TP_CONTEXT_CHECK
        cbnz        r0, ContextMismatch2

        ; Local interface dispatch case. Tail call to VSD helper specifically for the in-context TP dispatch
        ; scenario. Interface MethodDesc is restored to r12.
        EPILOG_POP      {r0-r3,r12,lr}
        EPILOG_BRANCH   InContextTPQuickDispatchAsmStub

ContextMismatch2
        ; Context-mismatch (remoted) dispatch case. Tail call to the general remoting dispatch code. Interface
        ; MethodDesc is restored to r12.
        EPILOG_POP      {r0-r3,r12,lr}
        EPILOG_BRANCH   TransparentProxyStub_CrossContext

        NESTED_END

; ------------------------------------------------------------------
; Common stub used for vtable dispatch of remoted methods. A small prestub will load the vtable slot index
; into r12 and then jump here. This stub determines whether we're already in the correct context (which can
; only happen for ContextBoundObjects). Depending on the answers we'll either dispatch the call locally or
; re-direct it to the remoting system (via TransparentProxyStub_CrossContext).
;
; On entry:
;   r0          : this (TransparentProxy object)
;   r12         : virtual method slot number
;   plus user arguments in registers and on the stack
;
; On exit:
;   Tail calls to the VSD in-context TP dispatcher or remoting system as appropriate.
;
        NESTED_ENTRY TransparentProxyStub

        PROLOG_PUSH     {r0-r3,r12,lr}

        CHECK_STACK_ALIGNMENT

        ; Check whether the TP is already in the correct context. This can happen for ContextBoundObjects
        ; only. The following macro will trash volatile registers and lr and return the result in r0 (0 ==
        ; context match, non-zero for everything else). All other registers are preserved.
        TP_CONTEXT_CHECK
        cbnz        r0, ContextMismatch3

        ; We need to perform a local vtable dispatch on the ContextBoundObject. Obviously this needs to be on
        ; the real type held in the proxy, not TransparentProxy's MethodTable or we'll just end up back here
        ; recursively.

        ; Recover 'this' pointer and slot number.
        ldr         r0, [sp]
        ldr         r12, [sp, #0x10]

        ; Extract real type from the TP.
        ldr         r0, [r0, #TransparentProxyObject___pMT]

        ; Vtables are no longer a linear array. Instead they use a two-level indirection with the first level
        ; consisting of fixed sized chunks of function pointer arrays. R12 has our slot number.

        ; Calculate first level chunk index.
        lsr         r1, r12, #ASM__VTABLE_SLOTS_PER_CHUNK_LOG2

        ; Load the address of the chunk from the MethodTable (the chunk table immediately follows the
        ; MethodTable structure).
        add         r0, #SIZEOF__MethodTable
        ldr         r2, [r0, r1, lsl #2]

        ; Calculate the slot index within the chunk.
        and         r0, r12, #(ASM__VTABLE_SLOTS_PER_CHUNK - 1)

        ; Load the target address into r12 (we no longer need the slot number and we're about to restore the
        ; other registers).
        ldr         r12, [r2, r0, lsl #2]

        ; Restore the stack state and tail call to the local target.
        EPILOG_POP      {r0-r3}
        EPILOG_STACK_FREE 4         ; Skip restore of r12 since we've overwritten it
        EPILOG_POP      {lr}
        EPILOG_BRANCH_REG r12

ContextMismatch3
        ; Contexts don't match so we have to dispatch through remoting. Clean up the stack and tail call to
        ; the helper.
        EPILOG_POP      {r0-r3,r12,lr}
        EPILOG_BRANCH   TransparentProxyStub_CrossContext

        NESTED_END

#endif // FEATURE_REMOTING
#if defined(FEATURE_REMOTING) || defined(FEATURE_COMINTEROP)
; ------------------------------------------------------------------
; Function used by remoting/COM interop to get floating point return value (since it's not in the same
; register(s) as non-floating point values).
;
; On entry;
;   r0          : size of the FP result (4 or 8 bytes)
;   r1          : pointer to 64-bit buffer to receive result
;
; On exit:
;   buffer pointed to by r1 on entry contains the float or double argument as appropriate
;
        LEAF_ENTRY getFPReturn

        cmp         r0, #4
        bne         LgetFP8
        vmov        r2, s0
        str         r2, [r1]
        bx          lr
LgetFP8
        vmov        r2, r3, d0
        strd        r2, r3, [r1]
        bx          lr

        LEAF_END

; ------------------------------------------------------------------
; Function used by remoting/COM interop to set floating point return value (since it's not in the same
; register(s) as non-floating point values).
;
; On entry:
;   r0          : size of the FP result (4 or 8 bytes)
;   r2/r3       : 32-bit or 64-bit FP result
;
; On exit:
;   s0          : float result if r0 == 4
;   d0          : double result if r0 == 8
;
        LEAF_ENTRY setFPReturn

        cmp         r0, #4
        bne         LsetFP8
        vmov        s0, r2
        bx          lr
LsetFP8
        vmov        d0, r2, r3
        bx          lr

        LEAF_END

#endif defined(FEATURE_REMOTING) || defined(FEATURE_COMINTEROP)
#ifdef FEATURE_REMOTING

; ------------------------------------------------------------------
; Tail call Object.FieldGetter remotely with the given arguments.
;
; On entry:
;   r0          : pMD   (MethodDesc * of the Object.FieldGetter method)
;   r1          : pThis (the transparent proxy)
;   r2          : pFirst
;   r3          : pSecond
;   [sp, #0]    : pThird
;
; On exit:
;   Tail calls to the managed method
;
        LEAF_ENTRY CRemotingServices__CallFieldGetter

        mov         r12, r0
        mov         r0, r1
        mov         r1, r2
        mov         r2, r3
        ldr         r3, [sp, #0]

        b           TransparentProxyStub_CrossContext

        LEAF_END

; ------------------------------------------------------------------
; Tail call Object.FieldSetter remotely with the given arguments.
;
; On entry:
;   r0          : pMD   (MethodDesc * of the Object.FieldSetter method)
;   r1          : pThis (the transparent proxy)
;   r2          : pFirst
;   r3          : pSecond
;   [sp, #0]    : pThird
;
; On exit:
;   Tail calls to the managed method
;
        LEAF_ENTRY CRemotingServices__CallFieldSetter

        mov         r12, r0
        mov         r0, r1
        mov         r1, r2
        mov         r2, r3
        ldr         r3, [sp, #0]

        b           TransparentProxyStub_CrossContext

        LEAF_END

; ------------------------------------------------------------------
; General purpose remoting helper used to call given target with two parameters.
;
; On entry:
;   r0          : pTarget
;   r1          : pFirst
;   r2          : pSecond
;
;
        NESTED_ENTRY CTPMethodTable__CallTargetHelper2,,CallDescrWorkerUnwindFrameChainHandler
        
        PROLOG_PUSH {r11, lr}

        mov         r12, r0
        mov         r0, r1
        mov         r1, r2

        blx          r12

        ; Adding a nop so that unwind does not result in the IP being in epilog.
        ; This ensures that the OS unwinder looks up the personality routine for this method.
        nop
        
        EPILOG_POP {r11, pc}

        NESTED_END

; ------------------------------------------------------------------
; General purpose remoting helper used to call given target with three parameters.
;
; On entry:
;   r0          : pTarget
;   r1          : pFirst
;   r2          : pSecond
;   r3          : pThird
;
;
        NESTED_ENTRY CTPMethodTable__CallTargetHelper3,,CallDescrWorkerUnwindFrameChainHandler

        PROLOG_PUSH {r11, lr}

        mov         r12, r0
        mov         r0, r1
        mov         r1, r2
        mov         r2, r3

        blx          r12

        ; Adding a nop so that unwind does not result in the IP being in epilog.
        ; This ensures that the OS unwinder looks up the personality routine for this method.
        nop
        
        EPILOG_POP {r11, pc}

        NESTED_END

#endif // FEATURE_REMOTING

#ifdef FEATURE_COMINTEROP
; ------------------------------------------------------------------
; GenericComPlusCallStub that erects a ComPlusMethodFrame and calls into the runtime
; (CLRToCOMWorker) to dispatch rare cases of the interface call.
;
; On entry:
;   r0          : 'this' object
;   r12         : Interface MethodDesc*
;   plus user arguments in registers and on the stack
;
; On exit:
;   r0/r1/s0/d0 set to return value of the call as appropriate
;
        NESTED_ENTRY GenericComPlusCallStub

        PROLOG_WITH_TRANSITION_BLOCK 0x20

        add         r0, sp, #__PWTB_TransitionBlock ; pTransitionBlock
        mov         r1, r12                         ; pMethodDesc

        ; Call CLRToCOMWorker(pFrame). This call will set up the rest of the frame (including the vfptr, 
        ; the GS cookie and linking to the thread), make the client call and return with correct registers set 
        ; (r0/r1/s0-s3/d0-d3 as appropriate).

        bl          CLRToCOMWorker

        ; r0 = fpRetSize

        ; return value is stored before float argument registers
        add         r1, sp, #(__PWTB_FloatArgumentRegisters - 0x20)
        bl          setStubReturnValue

        EPILOG_WITH_TRANSITION_BLOCK_RETURN

        NESTED_END

; ------------------------------------------------------------------
; COM to CLR stub called the first time a particular method is invoked.
;
; On entry:
;   r12         : (MethodDesc* - ComCallMethodDesc_Offset_FromR12) provided by prepad thunk
;   plus user arguments in registers and on the stack
;
; On exit:
;   tail calls to real method
;
        NESTED_ENTRY ComCallPreStub

        GBLA ComCallPreStub_FrameSize
        GBLA ComCallPreStub_FramePad
        GBLA ComCallPreStub_StackAlloc
        GBLA ComCallPreStub_Frame
        GBLA ComCallPreStub_ErrorReturn

; Set the defaults
ComCallPreStub_FramePad SETA 8                          ; error return
ComCallPreStub_FrameSize SETA (ComCallPreStub_FramePad + SIZEOF__GSCookie + SIZEOF__ComMethodFrame)

        IF ComCallPreStub_FrameSize:MOD:8 != 0
ComCallPreStub_FramePad     SETA ComCallPreStub_FramePad + 4
ComCallPreStub_FrameSize    SETA ComCallPreStub_FrameSize + 4
        ENDIF

ComCallPreStub_StackAlloc   SETA ComCallPreStub_FrameSize - SIZEOF__ArgumentRegisters - 2 * 4
ComCallPreStub_Frame        SETA SIZEOF__FloatArgumentRegisters + ComCallPreStub_FramePad + SIZEOF__GSCookie
ComCallPreStub_ErrorReturn  SETA SIZEOF__FloatArgumentRegisters

        PROLOG_PUSH {r0-r3}                             ; Spill general argument registers
        PROLOG_PUSH {r11,lr}                            ; Save return address
        PROLOG_STACK_ALLOC ComCallPreStub_StackAlloc    ; Alloc non-spill portion of stack frame
        PROLOG_VPUSH {d0-d7}                            ; Spill floating point argument registers

        CHECK_STACK_ALIGNMENT

        ; Finish initializing the frame. The C++ helper will fill in the GS cookie and vfptr and link us to
        ; the Thread frame chain (see ComPrestubMethodFrame::Push). That leaves us with m_pFuncDesc.
        ; The prepad thunk passes us a value which is the MethodDesc* - ComCallMethodDesc_Offset_FromR12 (due to encoding limitations in the
        ; thunk). So we must correct this by adding 4 before storing the pointer.
        add         r12, #(ComCallMethodDesc_Offset_FromR12)
        str         r12, [sp, #(ComCallPreStub_Frame + UnmanagedToManagedFrame__m_pvDatum)]

        ; Call the C++ worker: ComPreStubWorker(&Frame)
        add         r0, sp, #(ComCallPreStub_Frame)
        add         r1, sp, #(ComCallPreStub_ErrorReturn)
        bl          ComPreStubWorker

        ; Handle failure case.
        cbz         r0, ErrorExit

        ; Stash real target address where it won't be overwritten by restoring the calling state.
        mov         r12, r0

        EPILOG_VPOP {d0-d7}         ; Restore floating point argument registers
        EPILOG_STACK_FREE ComCallPreStub_StackAlloc
        EPILOG_POP  {r11,lr}
        EPILOG_POP  {r0-r3}         ; Restore argument registers
        ; Tail call the real target. Actually ComPreStubWorker returns the address of the prepad thunk on ARM,
        ; that way we don't run out of volatile registers trying to remember both the new target address and
        ; the hidden MethodDesc* argument. ComPreStubWorker patched the prepad though so the second time
        ; through we won't end up here again.
        EPILOG_BRANCH_REG r12

ErrorExit
        ; Failed to find a stub to call. Retrieve the return value ComPreStubWorker set for us.
        ldr         r0, [sp, #(ComCallPreStub_ErrorReturn)]
        ldr         r1, [sp, #(ComCallPreStub_ErrorReturn+4)]
        EPILOG_STACK_FREE ComCallPreStub_StackAlloc + SIZEOF__FloatArgumentRegisters
        EPILOG_POP  {r11,lr}
        EPILOG_STACK_FREE SIZEOF__ArgumentRegisters
        EPILOG_RETURN

        NESTED_END

; ------------------------------------------------------------------
; COM to CLR stub which sets up a ComMethodFrame and calls COMToCLRWorker.
;
; On entry:
;   r12         : (MethodDesc* - ComCallMethodDesc_Offset_FromR12) provided by prepad thunk
;   plus user arguments in registers and on the stack
;
; On exit:
;   Result in r0/r1/s0/d0 as per the real method being called
;
        NESTED_ENTRY GenericComCallStub,,ReverseComUnwindFrameChainHandler

; Calculate space needed on stack for alignment padding, a GS cookie and a ComMethodFrame (minus the last
; field, m_ReturnAddress, which we'll push explicitly).

        GBLA GenericComCallStub_FrameSize
        GBLA GenericComCallStub_FramePad
        GBLA GenericComCallStub_StackAlloc
        GBLA GenericComCallStub_Frame

; Set the defaults
GenericComCallStub_FramePad SETA 0
GenericComCallStub_FrameSize SETA (GenericComCallStub_FramePad + SIZEOF__GSCookie + SIZEOF__ComMethodFrame)

        IF GenericComCallStub_FrameSize:MOD:8 != 0
GenericComCallStub_FramePad     SETA 4
GenericComCallStub_FrameSize    SETA GenericComCallStub_FrameSize + GenericComCallStub_FramePad
        ENDIF

GenericComCallStub_StackAlloc   SETA GenericComCallStub_FrameSize - SIZEOF__ArgumentRegisters - 2 * 4
GenericComCallStub_Frame        SETA SIZEOF__FloatArgumentRegisters + GenericComCallStub_FramePad + SIZEOF__GSCookie

        PROLOG_PUSH {r0-r3}                             ; Spill general argument registers
        PROLOG_PUSH {r11,lr}                            ; Save return address
        PROLOG_STACK_ALLOC GenericComCallStub_StackAlloc ; Alloc non-spill portion of stack frame
        PROLOG_VPUSH {d0-d7}                            ; Spill floating point argument registers

        CHECK_STACK_ALIGNMENT

        ; Store MethodDesc* in frame. Due to a limitation of the prepad, r12 actually contains a value
        ; "ComCallMethodDesc_Offset_FromR12" less than the pointer we want, so fix that up.
        add         r12, r12, #(ComCallMethodDesc_Offset_FromR12)
        str         r12, [sp, #(GenericComCallStub_Frame + UnmanagedToManagedFrame__m_pvDatum)]

        ; Call COMToCLRWorker(pThread, pFrame). Note that pThread is computed inside the method so we don't
        ; need to set it up here.
        ;
        ; Setup R1 to point to the start of the explicit frame. We account for alignment padding and
        ; space for GSCookie.
        add         r1, sp, #(GenericComCallStub_Frame)
        bl          COMToCLRWorker

        EPILOG_STACK_FREE GenericComCallStub_StackAlloc + SIZEOF__FloatArgumentRegisters
        EPILOG_POP  {r11,lr}
        EPILOG_STACK_FREE SIZEOF__ArgumentRegisters
        EPILOG_RETURN

        NESTED_END

; ------------------------------------------------------------------
; COM to CLR stub called from COMToCLRWorker that actually dispatches to the real managed method.
;
; On entry:
;   r0          : dwStackSlots, count of argument stack slots to copy
;   r1          : pFrame, ComMethodFrame pushed by GenericComCallStub above
;   r2          : pTarget, address of code to call
;   r3          : pSecretArg, hidden argument passed to target above in r12
;   [sp, #0]    : pDangerousThis, managed 'this' reference
;
; On exit:
;   Result in r0/r1/s0/d0 as per the real method being called
;
        NESTED_ENTRY COMToCLRDispatchHelper,,CallDescrWorkerUnwindFrameChainHandler

        PROLOG_PUSH {r4-r5,r7,lr}
        PROLOG_STACK_SAVE r7

        ; Copy stack-based arguments. Make sure the eventual SP ends up 8-byte aligned. Note that the
        ; following calculations assume that the prolog has left the stack already aligned.
        CHECK_STACK_ALIGNMENT

        cbz         r0, COMToCLRDispatchHelper_ArgumentsSetup

        lsl         r4, r0, #2      ; r4 = (dwStackSlots * 4)
        and         r5, r4, #4      ; Align the stack
        sub         sp, sp, r5

        add         r5, r1, #SIZEOF__ComMethodFrame
        add         r5, r5, r4

COMToCLRDispatchHelper_StackLoop
        ldr         r4, [r5,#-4]!
        str         r4, [sp,#-4]!
        subs        r0, r0, #1
        bne         COMToCLRDispatchHelper_StackLoop

        CHECK_STACK_ALIGNMENT

COMToCLRDispatchHelper_ArgumentsSetup
        ; Load floating point argument registers.
        sub         r4, r1, #(GenericComCallStub_Frame)
        vldm        r4, {d0-d7}

        ; Prepare the call target and hidden argument prior to overwriting r0-r3.
        mov         r12, r3             ; r12 = hidden argument
        mov         lr, r2              ; lr = target code

        ; Load general argument registers except r0.
        add         r4, r1, #(SIZEOF__ComMethodFrame - SIZEOF__ArgumentRegisters + 4)
        ldm         r4, {r1-r3}

        ; Load r0 from the managed this, not the original incoming IUnknown*.
        ldr         r0, [r7, #(4 * 4)]

        ; Make the call.
        blx         lr

        EPILOG_STACK_RESTORE r7
        EPILOG_POP  {r4-r5,r7,pc}

        NESTED_END

#endif // FEATURE_COMINTEROP

#ifdef PROFILING_SUPPORTED

PROFILE_ENTER           equ 1
PROFILE_LEAVE           equ 2
PROFILE_TAILCALL        equ 4

        ; Define the layout of the PROFILE_PLATFORM_SPECIFIC_DATA we push on the stack for all profiler
        ; helpers.
        map 0
            field 4                                 ; r0
            field 4                                 ; r1
            field 4                                 ; r11
            field 4                                 ; Pc (caller's PC, i.e. LR)
            field SIZEOF__FloatArgumentRegisters    ; spilled floating point argument registers
functionId  field 4
probeSp     field 4
profiledSp  field 4
hiddenArg   field 4
flags       field 4

SIZEOF__PROFILE_PLATFORM_SPECIFIC_DATA field 0

; ------------------------------------------------------------------
; Macro used to generate profiler helpers. In all cases we push a partially initialized
; PROFILE_PLATFORM_SPECIFIC_DATA structure on the stack and call into a C++ helper to continue processing.
;
; On entry:
;   r0      : clientInfo
;   r1/r2 : return values (in case of leave)
;   frame pointer(r11) must be set (in case of enter)
;   all arguments are on stack at frame pointer (r11) + 8bytes (save lr & prev r11).
;
; On exit:
;   All register values are preserved including volatile registers
;
        MACRO
            DefineProfilerHelper $HelperName, $Flags

        GBLS __ProfilerHelperFunc
__ProfilerHelperFunc SETS "$HelperName":CC:"Naked"

        NESTED_ENTRY $__ProfilerHelperFunc

        IMPORT $HelperName                  ; The C++ helper which does most of the work

        PROLOG_PUSH         {r0,r3,r9,r12}  ; save volatile general purpose registers. remaining r1 & r2 are saved below...saving r9 as it is required for virtualunwinding
        PROLOG_STACK_ALLOC  (6*4)           ; Reserve space for tail end of structure (5*4 bytes) and extra 4 bytes is for aligning the stack at 8-byte boundary
        PROLOG_VPUSH        {d0-d7}         ; Spill floting point argument registers
        PROLOG_PUSH         {r1,r11,lr}     ; Save possible return value in r1, frame pointer and return address
        PROLOG_PUSH         {r2}            ; Save possible return value in r0. Before calling Leave Hook Jit moves contents of r0 to r2
                                            ; so pushing r2 instead of r0. This push statement cannot be combined with the above push 
                                            ; as r2 gets pushed before r1.

        CHECK_STACK_ALIGNMENT

        ; Zero r1 for use clearing fields in the PROFILE_PLATFORM_SPECIFIC_DATA.
        eor         r1, r1

        ; Clear functionId.
        str         r1, [sp, #functionId]

        ; Save caller's SP (at the point this helper was called).
        add         r2, sp, #(SIZEOF__PROFILE_PLATFORM_SPECIFIC_DATA + 20)
        str         r2, [sp, #probeSp]

        ; Save caller's SP (at the point where only argument registers have been spilled).
        ldr         r2, [r11]
        add         r2, r2, #8             ; location of arguments is at frame pointer(r11) + 8 (lr & prev frame ptr is saved before changing 
        str         r2, [sp, #profiledSp]

        ; Clear hiddenArg.
        str         r1, [sp, #hiddenArg]

        ; Set flags to indicate type of helper called.
        mov         r1, #($Flags)
        str         r1, [sp, #flags]

        ; Call C++ portion of helper (<$HelperName>(clientInfo, &profilePlatformSpecificData)).
        mov         r1, sp
        bl          $HelperName

        EPILOG_POP          {r2}
        EPILOG_POP          {r1,r11,lr}
        EPILOG_VPOP         {d0-d7}
        EPILOG_STACK_FREE   (6*4)
        EPILOG_POP          {r0,r3,r9,r12}

        EPILOG_RETURN

        NESTED_END

        MEND

        DefineProfilerHelper ProfileEnter, PROFILE_ENTER
        DefineProfilerHelper ProfileLeave, PROFILE_LEAVE
        DefineProfilerHelper ProfileTailcall, PROFILE_TAILCALL

#endif // PROFILING_SUPPORTED

        ;
        ; If a preserved register were pushed onto the stack between
        ; the managed caller and the H_M_F, _R4_R11 will point to its
        ; location on the stack and it would have been updated on the
        ; stack by the GC already and it will be popped back into the
        ; appropriate register when the appropriate epilog is run.
        ;
        ; Otherwise, the register is preserved across all the code
        ; in this HCALL or FCALL, so we need to update those registers
        ; here because the GC will have updated our copies in the
        ; frame.
        ;
        ; So, if _R4_R11 points into the MachState, we need to update
        ; the register here.  That's what this macro does.
        ;

        MACRO
            RestoreRegMS $regIndex, $reg

        ; Incoming:
        ;
        ; R0 = address of MachState
        ;
        ; $regIndex: Index of the register (R4-R11). For R4, index is 4.
        ;            For R5, index is 5, and so on.
        ;
        ; $reg: Register name (e.g. R4, R5, etc)
        ;
        ; Get the address of the specified captured register from machine state
        add     r2, r0, #(MachState__captureR4_R11 + (($regIndex-4)*4))

        ; Get the address of the specified preserved register from machine state
        ldr     r3, [r0, #(MachState___R4_R11 + (($regIndex-4)*4))]

        cmp     r2, r3
        bne     %FT0
        ldr     $reg, [r2]
0

        MEND

; EXTERN_C int __fastcall HelperMethodFrameRestoreState(
;         INDEBUG_COMMA(HelperMethodFrame *pFrame)
;         MachState *pState
;         )
        LEAF_ENTRY HelperMethodFrameRestoreState

#ifdef _DEBUG
        mov r0, r1
#endif

        ; If machine state is invalid, then simply exit
        ldr r1, [r0, #MachState__isValid]
        cmp r1, #0
        beq Done

        RestoreRegMS 4, R4
        RestoreRegMS 5, R5
        RestoreRegMS 6, R6
        RestoreRegMS 7, R7
        RestoreRegMS 8, R8
        RestoreRegMS 9, R9
        RestoreRegMS 10, R10
        RestoreRegMS 11, R11
Done
        ; Its imperative that the return value of HelperMethodFrameRestoreState is zero
        ; as it is used in the state machine to loop until it becomes zero.
        ; Refer to HELPER_METHOD_FRAME_END macro for details.
        mov r0,#0
        bx lr

        LEAF_END

#ifdef FEATURE_HIJACK

; ------------------------------------------------------------------
; Hijack function for functions which return a reference type
        NESTED_ENTRY OnHijackObjectTripThread
        PROLOG_PUSH {r0,r4-r11,lr}

        CHECK_STACK_ALIGNMENT

        mov r0, sp
        bl OnHijackObjectWorker

        EPILOG_POP {r0,r4-r11,pc}
        NESTED_END

; ------------------------------------------------------------------
; Hijack function for functions which return an interior pointer within an object allocated in managed heap
        NESTED_ENTRY OnHijackInteriorPointerTripThread
        PROLOG_PUSH {r0,r4-r11,lr}

        CHECK_STACK_ALIGNMENT

        mov r0, sp
        bl OnHijackInteriorPointerWorker

        EPILOG_POP {r0,r4-r11,pc}
        NESTED_END

; ------------------------------------------------------------------
; Hijack function for functions which return a value type
        NESTED_ENTRY OnHijackScalarTripThread
        PROLOG_PUSH {r0,r4-r11,lr}

        PROLOG_VPUSH {d0-d3}    ; saving as d0-d3 can have the floating point return value
        PROLOG_PUSH {r1}        ; saving as r1 can have partial return value when return is > 32 bits
        PROLOG_STACK_ALLOC 4    ; 8 byte align

        CHECK_STACK_ALIGNMENT

        add r0, sp, #40
        bl OnHijackScalarWorker

        EPILOG_STACK_FREE 4
        EPILOG_POP {r1}
        EPILOG_VPOP {d0-d3}

        EPILOG_POP {r0,r4-r11,pc}
        NESTED_END

#endif ; FEATURE_HIJACK

; ------------------------------------------------------------------
; Macro to generate Redirection Stubs
;
; $reason : reason for redirection
;                     Eg. GCThreadControl
; NOTE: If you edit this macro, make sure you update GetCONTEXTFromRedirectedStubStackFrame.
; This function is used by both the personality routine and the debugger to retrieve the original CONTEXT.
        MACRO
        GenerateRedirectedHandledJITCaseStub $reason

        GBLS __RedirectionStubFuncName
        GBLS __RedirectionStubEndFuncName
        GBLS __RedirectionFuncName
__RedirectionStubFuncName SETS "RedirectedHandledJITCaseFor":CC:"$reason":CC:"_Stub"
__RedirectionStubEndFuncName SETS "RedirectedHandledJITCaseFor":CC:"$reason":CC:"_StubEnd"
__RedirectionFuncName SETS "|?RedirectedHandledJITCaseFor":CC:"$reason":CC:"@Thread@@CAXXZ|"

        IMPORT $__RedirectionFuncName

        NESTED_ENTRY $__RedirectionStubFuncName

        PROLOG_PUSH {r7,lr}     ; return address
        PROLOG_STACK_ALLOC 4    ; stack slot to save the CONTEXT *
        PROLOG_STACK_SAVE r7

        ;REDIRECTSTUB_SP_OFFSET_CONTEXT is defined in asmconstants.h
        ;If CONTEXT is not saved at 0 offset from SP it must be changed as well.
        ASSERT REDIRECTSTUB_SP_OFFSET_CONTEXT == 0

        ; Runtime check for 8-byte alignment. This check is necessary as this function can be
        ; entered before complete execution of the prolog of another function.
        and r0, r7, #4
        sub sp, sp, r0

        ; stack must be 8 byte aligned
        CHECK_STACK_ALIGNMENT

        ;
        ; Save a copy of the redirect CONTEXT*.
        ; This is needed for the debugger to unwind the stack.
        ;
        bl GetCurrentSavedRedirectContext
        str r0, [r7]

        ;
        ; Fetch the interrupted pc and save it as our return address.
        ;
        ldr r1, [r0, #CONTEXT_Pc]
        str r1, [r7, #8]

        ;
        ; Call target, which will do whatever we needed to do in the context
        ; of the target thread, and will RtlRestoreContext when it is done.
        ;
        bl $__RedirectionFuncName

        EMIT_BREAKPOINT ; Unreachable

; Put a label here to tell the debugger where the end of this function is.
$__RedirectionStubEndFuncName
        EXPORT $__RedirectionStubEndFuncName

        NESTED_END

        MEND

; ------------------------------------------------------------------
; Redirection Stub for GC in fully interruptible method
        GenerateRedirectedHandledJITCaseStub GCThreadControl
; ------------------------------------------------------------------
        GenerateRedirectedHandledJITCaseStub DbgThreadControl
; ------------------------------------------------------------------
        GenerateRedirectedHandledJITCaseStub UserSuspend
; ------------------------------------------------------------------
        GenerateRedirectedHandledJITCaseStub YieldTask

#ifdef _DEBUG
; ------------------------------------------------------------------
; Redirection Stub for GC Stress
        GenerateRedirectedHandledJITCaseStub GCStress
#endif

; ------------------------------------------------------------------
; Functions to probe for stack space
; Input reg r4 = amount of stack to probe for
; value of reg r4 is preserved on exit from function
; r12 is trashed
; The below two functions were copied from vctools\crt\crtw32\startup\arm\chkstk.asm

    NESTED_ENTRY checkStack
    subs        r12,sp,r4
    mrc         p15,#0,r4,c13,c0,#2 ; get TEB *
    ldr         r4,[r4,#8]          ; get Stack limit
    bcc         checkStack_neg      ; if r12 is less then 0 set it to 0
checkStack_label1
    cmp         r12, r4
    bcc         stackProbe          ; must probe to extend guardpage if r12 is beyond stackLimit
    sub         r4, sp, r12         ; restore value of r4
    EPILOG_RETURN
checkStack_neg
    mov         r12, #0
    b           checkStack_label1
    NESTED_END

    NESTED_ENTRY stackProbe
    PROLOG_PUSH {r5,r6}
    mov         r6, r12
    bfc         r6, #0, #0xc  ; align down (4K)
stackProbe_loop
    sub         r4,r4,#0x1000 ; dec stack Limit by 4K as page size is 4K
    ldr         r5,[r4]       ; try to read ... this should move the guard page
    cmp         r4,r6
    bne         stackProbe_loop
    EPILOG_POP {r5,r6}
    EPILOG_NOP  sub r4,sp,r12
    EPILOG_RETURN
    NESTED_END

;------------------------------------------------
; VirtualMethodFixupStub
;
; In NGEN images, virtual slots inherited from cross-module dependencies
; point to a jump thunk that calls into the following function that will
; call into a VM helper. The VM helper is responsible for patching up
; thunk, upon executing the precode, so that all subsequent calls go directly
; to the actual method body.
;
; This is done lazily for performance reasons.
;
; On entry:
;
; R0 = "this" pointer
; R12 = Address of thunk + 4

    NESTED_ENTRY VirtualMethodFixupStub

    ; Save arguments and return address
    PROLOG_PUSH {r0-r3, lr}

    ; Align stack
    PROLOG_STACK_ALLOC  SIZEOF__FloatArgumentRegisters + 4
    vstm                sp, {d0-d7}


    CHECK_STACK_ALIGNMENT

    ; R12 contains an address that is 4 bytes ahead of
    ; where the thunk starts. Refer to ZapImportVirtualThunk::Save
    ; for details on this.
    ;
    ; Move the correct thunk start address in R1
    sub r1, r12, #4

    ; Call the helper in the VM to perform the actual fixup
    ; and tell us where to tail call. R0 already contains
    ; the this pointer.
    bl VirtualMethodFixupWorker

    ; On return, R0 contains the target to tailcall to
    mov         r12, r0

    ; pop the stack and restore original register state
    vldm               sp, {d0-d7}
    EPILOG_STACK_FREE  SIZEOF__FloatArgumentRegisters + 4
    EPILOG_POP {r0-r3, lr}

    PATCH_LABEL VirtualMethodFixupPatchLabel

    ; and tailcall to the actual method
    EPILOG_BRANCH_REG r12

    NESTED_END

;------------------------------------------------
; ExternalMethodFixupStub
;
; In NGEN images, calls to cross-module external methods initially
; point to a jump thunk that calls into the following function that will
; call into a VM helper. The VM helper is responsible for patching up the
; thunk, upon executing the precode, so that all subsequent calls go directly
; to the actual method body.
;
; This is done lazily for performance reasons.
;
; On entry:
;
; R12 = Address of thunk + 4

    NESTED_ENTRY ExternalMethodFixupStub

    PROLOG_WITH_TRANSITION_BLOCK

    add         r0, sp, #__PWTB_TransitionBlock ; pTransitionBlock

    ; Adjust (read comment above for details) and pass the address of the thunk
    sub         r1, r12, #4                     ; pThunk

    mov         r2, #0  ; sectionIndex
    mov         r3, #0  ; pModule
    bl          ExternalMethodFixupWorker

    ; mov the address we patched to in R12 so that we can tail call to it
    mov         r12, r0

    EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
    PATCH_LABEL ExternalMethodFixupPatchLabel
    EPILOG_BRANCH_REG   r12

    NESTED_END

;------------------------------------------------
; StubDispatchFixupStub
;
; In NGEN images, calls to interface methods initially
; point to a jump thunk that calls into the following function that will
; call into a VM helper. The VM helper is responsible for patching up the
; thunk with actual stub dispatch stub.
;
; On entry:
;
; R4 = Address of indirection cell

    NESTED_ENTRY StubDispatchFixupStub

    PROLOG_WITH_TRANSITION_BLOCK

    ; address of StubDispatchFrame
    add         r0, sp, #__PWTB_TransitionBlock ; pTransitionBlock
    mov         r1, r4  ; siteAddrForRegisterIndirect
    mov         r2, #0  ; sectionIndex
    mov         r3, #0  ; pModule

    bl          StubDispatchFixupWorker

    ; mov the address we patched to in R12 so that we can tail call to it
    mov         r12, r0

    EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
    PATCH_LABEL StubDispatchFixupPatchLabel
    EPILOG_BRANCH_REG   r12
 
    NESTED_END

;------------------------------------------------
; JIT_RareDisableHelper
;
; The JIT expects this helper to preserve registers used for return values
;
    NESTED_ENTRY JIT_RareDisableHelper

    PROLOG_PUSH {r0-r1, r11, lr} ; save integer return value
    PROLOG_VPUSH {d0-d3}         ; floating point return value

    CHECK_STACK_ALIGNMENT

    bl          JIT_RareDisableHelperWorker

    EPILOG_VPOP {d0-d3}
    EPILOG_POP {r0-r1, r11, pc}

    NESTED_END


#ifdef FEATURE_CORECLR
;
; JIT Static access helpers for single appdomain case
;

; ------------------------------------------------------------------
; void* JIT_GetSharedNonGCStaticBase(SIZE_T moduleDomainID, DWORD dwClassDomainID)

    LEAF_ENTRY JIT_GetSharedNonGCStaticBase_SingleAppDomain

    ; If class is not initialized, bail to C++ helper
    add     r2, r0, #DomainLocalModule__m_pDataBlob
    ldrb    r2, [r2, r1]
    tst     r2, #1
    beq      CallCppHelper1

    bx      lr

CallCppHelper1
    ; Tail call JIT_GetSharedNonGCStaticBase_Helper
    b     JIT_GetSharedNonGCStaticBase_Helper
    LEAF_END


; ------------------------------------------------------------------
; void* JIT_GetSharedNonGCStaticBaseNoCtor(SIZE_T moduleDomainID, DWORD dwClassDomainID)

    LEAF_ENTRY JIT_GetSharedNonGCStaticBaseNoCtor_SingleAppDomain

    bx lr
    LEAF_END


; ------------------------------------------------------------------
; void* JIT_GetSharedGCStaticBase(SIZE_T moduleDomainID, DWORD dwClassDomainID)

    LEAF_ENTRY JIT_GetSharedGCStaticBase_SingleAppDomain

    ; If class is not initialized, bail to C++ helper
    add     r2, r0, #DomainLocalModule__m_pDataBlob
    ldrb    r2, [r2, r1]
    tst     r2, #1
    beq      CallCppHelper3

    ldr     r0, [r0, #DomainLocalModule__m_pGCStatics]
    bx lr

CallCppHelper3
    ; Tail call Jit_GetSharedGCStaticBase_Helper
    b     JIT_GetSharedGCStaticBase_Helper
    LEAF_END


; ------------------------------------------------------------------
; void* JIT_GetSharedGCStaticBaseNoCtor(SIZE_T moduleDomainID, DWORD dwClassDomainID)

    LEAF_ENTRY JIT_GetSharedGCStaticBaseNoCtor_SingleAppDomain

    ldr     r0, [r0, #DomainLocalModule__m_pGCStatics]
    bx lr
    LEAF_END

#endif

; ------------------------------------------------------------------
; __declspec(naked) void F_CALL_CONV JIT_Stelem_Ref(PtrArray* array, unsigned idx, Object* val)
    LEAF_ENTRY JIT_Stelem_Ref
    
    ; We retain arguments as they were passed and use r0 == array; r1 == idx; r2 == val

    ; check for null array
    cbz     r0, ThrowNullReferenceException

    ; idx bounds check
    ldr     r3,[r0,#ArrayBase__m_NumComponents]
    cmp     r3,r1
    bls     ThrowIndexOutOfRangeException

    ; fast path to null assignment (doesn't need any write-barriers)
    cbz     r2, AssigningNull

    ; Verify the array-type and val-type matches before writing
    ldr     r12, [r0] ; r12 = array MT
    ldr     r3, [r2] ; r3 = val->GetMethodTable()
    ldr     r12, [r12, #MethodTable__m_ElementType] ; array->GetArrayElementTypeHandle()
    cmp     r3, r12
    beq     JIT_Stelem_DoWrite

    ; Types didnt match but allow writing into an array of objects
    ldr     r3, =$g_pObjectClass
    ldr     r3, [r3]  ; r3 = *g_pObjectClass
    cmp     r3, r12   ; array type matches with Object*
    beq     JIT_Stelem_DoWrite

    ; array type and val type do not exactly match. Raise frame and do detailed match
    b       JIT_Stelem_Ref_NotExactMatch   

AssigningNull
    ; Assigning null doesn't need write barrier
    adds    r0, r1, LSL #2               ; r0 = r0 + (r1 x 4) = array->m_array[idx]
    str     r2, [r0, #PtrArray__m_Array] ; array->m_array[idx] = val
    bx      lr
   
ThrowNullReferenceException
    ; Tail call JIT_InternalThrow(NullReferenceException)
    ldr     r0, =CORINFO_NullReferenceException_ASM
    b       JIT_InternalThrow

ThrowIndexOutOfRangeException
    ; Tail call JIT_InternalThrow(NullReferenceException)
    ldr     r0, =CORINFO_IndexOutOfRangeException_ASM
    b       JIT_InternalThrow

    LEAF_END

; ------------------------------------------------------------------
; __declspec(naked) void F_CALL_CONV JIT_Stelem_Ref_NotExactMatch(PtrArray* array,
;                                                       unsigned idx, Object* val)
;   r12 = array->GetArrayElementTypeHandle()
;
    NESTED_ENTRY JIT_Stelem_Ref_NotExactMatch
    PROLOG_PUSH   {lr}
    PROLOG_PUSH   {r0-r2}

    CHECK_STACK_ALIGNMENT 

    ; allow in case val can be casted to array element type
    ; call ObjIsInstanceOfNoGC(val, array->GetArrayElementTypeHandle())
    mov     r1, r12 ; array->GetArrayElementTypeHandle()
    mov     r0, r2
    bl      ObjIsInstanceOfNoGC
    cmp     r0, TypeHandle_CanCast 
    beq     DoWrite             ; ObjIsInstance returned TypeHandle::CanCast

    ; check via raising frame
NeedFrame
    mov     r1, sp              ; r1 = &array
    adds    r0, sp, #8          ; r0 = &val
    bl      ArrayStoreCheck     ; ArrayStoreCheck(&val, &array)
 
DoWrite
    EPILOG_POP  {r0-r2}
    EPILOG_POP  {lr}
    EPILOG_BRANCH JIT_Stelem_DoWrite

    NESTED_END

; ------------------------------------------------------------------
; __declspec(naked) void F_CALL_CONV JIT_Stelem_DoWrite(PtrArray* array, unsigned idx, Object* val)
    LEAF_ENTRY  JIT_Stelem_DoWrite

    ; Setup args for JIT_WriteBarrier. r0 = &array->m_array[idx]; r1 = val
    adds    r0, #PtrArray__m_Array     ; r0 = &array->m_array
    adds    r0, r1, LSL #2
    mov     r1, r2                     ; r1 = val

    ; Branch to the write barrier (which is already correctly overwritten with
    ; single or multi-proc code based on the current CPU
    b       JIT_WriteBarrier

    LEAF_END

; ------------------------------------------------------------------
; GC write barrier support.
;
; There's some complexity here for a couple of reasons:
;
; Firstly, there are a few variations of barrier types (input registers, checked vs unchecked, UP vs MP etc.).
; So first we define a number of helper macros that perform fundamental pieces of a barrier and then we define
; the final barrier functions by assembling these macros in various combinations.
;
; Secondly, for performance reasons we believe it's advantageous to be able to modify the barrier functions
; over the lifetime of the CLR. Specifically ARM has real problems reading the values of external globals (we
; need two memory indirections to do this) so we'd like to be able to directly set the current values of
; various GC globals (e.g. g_lowest_address and g_card_table) into the barrier code itself and then reset them
; every time they change (the GC already calls the VM to inform it of these changes). The handle this without
; creating too much fragility such as hardcoding instruction offsets in the VM update code, we wrap write
; barrier creation and GC globals access in a set of macros that create a table of descriptors describing each
; offset that must be patched.
;

; Many of the following macros need a scratch register. Define a name for it here so it's easy to modify this
; in the future.
        GBLS __wbscratch
__wbscratch SETS "r3"

;
; First define the meta-macros used to support dynamically patching write barriers.
;

    ; WRITEBARRIERAREA
    ;
    ; As we assemble each write barrier function we build a descriptor for the offsets within that function
    ; that need to be patched at runtime. We write these descriptors into a read-only portion of memory. Use a
    ; specially-named linker section for this to ensure all the descriptors are contiguous and form a table.
    ; During the final link of the CLR this section should be merged into the regular read-only data section.
    ;
    ; This macro handles switching assembler output to the above section (similar to the TEXTAREA or
    ; RODATAAREA macros defined by kxarm.h).
    ;
    MACRO
      WRITEBARRIERAREA
        AREA |.clrwb|,DATA,READONLY
    MEND

    ; BEGIN_WRITE_BARRIERS
    ;
    ; This macro must be invoked before any write barriers are defined. It sets up and exports a symbol,
    ; g_rgWriteBarrierDescriptors, used by the VM to locate the start of the table describing the offsets in
    ; each write barrier that need to be modified dynamically.
    ;
    MACRO
      BEGIN_WRITE_BARRIERS

        ; Define a global boolean to track whether we're currently in a BEGIN_WRITE_BARRIERS section. This is
        ; used purely to catch incorrect attempts to define a write barrier outside the section.
        GBLL  __defining_write_barriers
__defining_write_barriers SETL {true}

        ; Switch to the descriptor table section.
        WRITEBARRIERAREA

        ; Define and export a symbol pointing to the start of the descriptor table.
g_rgWriteBarrierDescriptors
        EXPORT g_rgWriteBarrierDescriptors

        ; Switch back to the code section.
        TEXTAREA
    MEND

    ; END_WRITE_BARRIERS
    ;
    ; This macro must be invoked after all write barriers have been defined. It finalizes the creation of the
    ; barrier descriptor table by writing a sentinel value at the end.
    ;
    MACRO
      END_WRITE_BARRIERS

        ASSERT __defining_write_barriers
__defining_write_barriers SETL {false}

        ; Switch to the descriptor table section.
        WRITEBARRIERAREA

        ; Write the sentinel value to the end of the descriptor table (a function entrypoint address of zero).
        DCD 0

        ; Switch back to the code section.
        TEXTAREA
    MEND

    ; WRITE_BARRIER_ENTRY
    ;
    ; Declare the start of a write barrier function. Use similarly to NESTED_ENTRY. This is the only legal way
    ; to declare a write barrier function.
    ;
    MACRO
      WRITE_BARRIER_ENTRY $name

        ; Ensure we're called inside a BEGIN_WRITE_BARRIERS section.
        ASSERT __defining_write_barriers

        ; Do the standard function declaration logic. Must use a NESTED_ENTRY since we require unwind info to
        ; be registered (for the case where the barrier AVs and the runtime needs to recover).
        LEAF_ENTRY $name

        ; Record the function name as it's used as the basis for unique label name creation in some of the
        ; macros below.
        GBLS __write_barrier_name
__write_barrier_name SETS "$name"

        ; Declare globals to collect the values of the offsets of instructions that load GC global values.
        GBLA __g_lowest_address_offset
        GBLA __g_highest_address_offset
        GBLA __g_ephemeral_low_offset
        GBLA __g_ephemeral_high_offset
        GBLA __g_card_table_offset

        ; Initialize the above offsets to 0xffff. The default of zero is unsatisfactory because we could
        ; legally have an offset of zero and we need some way to distinguish unset values (both for debugging
        ; and because some write barriers don't use all the globals).
__g_lowest_address_offset SETA 0xffff
__g_highest_address_offset SETA 0xffff
__g_ephemeral_low_offset SETA 0xffff
__g_ephemeral_high_offset SETA 0xffff
__g_card_table_offset SETA 0xffff

    MEND

    ; WRITE_BARRIER_END
    ;
    ; The partner to WRITE_BARRIER_ENTRY, used like NESTED_END.
    ;
    MACRO
      WRITE_BARRIER_END

        LTORG  ; force the literal pool to be emitted here so that copy code picks it up
        ; Use the standard macro to end the function definition.
        LEAF_END_MARKED $__write_barrier_name

; Define a local string to hold the name of a label identifying the end of the write barrier function.
        LCLS __EndLabelName
__EndLabelName SETS "$__write_barrier_name":CC:"_End"

        ; Switch to the descriptor table section.
        WRITEBARRIERAREA

        ; Emit the descripter for this write barrier. The order of these datums must be kept in sync with the
        ; definition of the WriteBarrierDescriptor structure in vm\arm\stubs.cpp.
        DCD     $__write_barrier_name
        DCD     $__EndLabelName
        DCD     __g_lowest_address_offset
        DCD     __g_highest_address_offset
        DCD     __g_ephemeral_low_offset
        DCD     __g_ephemeral_high_offset
        DCD     __g_card_table_offset

        ; Switch back to the code section.
        TEXTAREA

    MEND

    ; LOAD_GC_GLOBAL
    ;
    ; Used any time we want to load the value of one of the supported GC globals into a register. This records
    ; the offset of the instructions used to do this (a movw/movt pair) so we can modify the actual value
    ; loaded at runtime.
    ;
    ; Note that a given write barrier can only load a given global once (which will be compile-time asserted
    ; below).
    ;
    MACRO
      LOAD_GC_GLOBAL $regName, $globalName

        ; Map the GC global name to the name of the variable tracking the offset for this function.
        LCLS __offset_name
__offset_name SETS "__$globalName._offset"

        ; Ensure that we only attempt to load this global at most once in the current barrier function (we
        ; have this limitation purely because we only record one offset for each GC global).
        ASSERT $__offset_name == 0xffff

        ; Define a unique name for a label we're about to define used in the calculation of the current
        ; function offset.
        LCLS __offset_label_name
__offset_label_name SETS "$__write_barrier_name$__offset_name"

        ; Define the label.
$__offset_label_name

        ; Write the current function offset into the tracking variable.
$__offset_name SETA ($__offset_label_name - $__FuncStartLabel)

        ; Emit the instructions which will be patched to provide the value of the GC global (we start with a
        ; value of zero, so the write barriers have to be patched at least once before first use).
        movw    $regName, #0
        movt    $regName, #0
    MEND

;
; Now define the macros used in the bodies of write barrier implementations.
;

    ; UPDATE_GC_SHADOW
    ;
    ; Update the GC shadow heap to aid debugging (no-op unless WRITE_BARRIER_CHECK is defined). Assumes the
    ; location being written lies on the GC heap (either we've already performed the dynamic check or this is
    ; statically asserted by the JIT by calling the unchecked version of the write barrier).
    ;
    ;   Input:
    ;       $ptrReg : register containing the location (in the real heap) to be updated
    ;       $valReg : register containing the value (an objref) to be written to the location above
    ;
    ;   Output:
    ;       $__wbscratch : trashed
    ;
    MACRO
      UPDATE_GC_SHADOW $ptrReg, $valReg
#ifdef WRITE_BARRIER_CHECK

        ; Need one additional temporary register to hold the shadow pointer. Assume r7 is OK for now (and
        ; assert it). If this becomes a problem in the future the register choice can be parameterized.
        LCLS pShadow
pShadow  SETS "r7"
        ASSERT "$ptrReg" != "$pShadow"
        ASSERT "$valReg" != "$pShadow"

        push    {$pShadow}

        ; Compute address of shadow heap location:
        ;   pShadow = g_GCShadow + ($ptrReg - g_lowest_address)
        ldr     $__wbscratch, =g_lowest_address
        ldr     $__wbscratch, [$__wbscratch]
        sub     $pShadow, $ptrReg, $__wbscratch
        ldr     $__wbscratch, =$g_GCShadow
        ldr     $__wbscratch, [$__wbscratch]
        add     $pShadow, $__wbscratch

        ; if (pShadow >= g_GCShadow) goto end
        ldr     $__wbscratch, =$g_GCShadowEnd
        ldr     $__wbscratch, [$__wbscratch]
        cmp     $pShadow, $__wbscratch
        bhs     %FT0

        ; *pShadow = $valReg
        str     $valReg, [$pShadow]

        ; Ensure that the write to the shadow heap occurs before the read from the GC heap so that race
        ; conditions are caught by INVALIDGCVALUE.
        dmb

        ; if (*$ptrReg == $valReg) goto end
        ldr     $__wbscratch, [$ptrReg]
        cmp     $__wbscratch, $valReg
        beq     %FT0

        ; *pShadow = INVALIDGCVALUE (0xcccccccd)
        movw    $__wbscratch, #0xcccd
        movt    $__wbscratch, #0xcccc
        str     $__wbscratch, [$pShadow]

0
        pop     {$pShadow}
#endif // WRITE_BARRIER_CHECK
    MEND

    ; UPDATE_CARD_TABLE
    ;
    ; Update the card table as necessary (if the object reference being assigned in the barrier refers to an
    ; object in the ephemeral generation). Otherwise this macro is a no-op. Assumes the location being written
    ; lies on the GC heap (either we've already performed the dynamic check or this is statically asserted by
    ; the JIT by calling the unchecked version of the write barrier).
    ;
    ; Additionally this macro can produce a uni-proc or multi-proc variant of the code. This governs whether
    ; we bother to check if the card table has been updated before making our own update (on an MP system it
    ; can be helpful to perform this check to avoid cache line thrashing, on an SP system the code path length
    ; is more important).
    ;
    ;   Input:
    ;       $ptrReg : register containing the location to be updated
    ;       $valReg : register containing the value (an objref) to be written to the location above
    ;       $mp     : boolean indicating whether the code will run on an MP system
    ;       $tmpReg : additional register that can be trashed (can alias $ptrReg or $valReg if needed)
    ;
    ;   Output:
    ;       $tmpReg : trashed (defaults to $ptrReg)
    ;       $__wbscratch : trashed
    ;
    MACRO
      UPDATE_CARD_TABLE $ptrReg, $valReg, $mp, $postGrow, $tmpReg
        ASSERT "$ptrReg" != "$__wbscratch"
        ASSERT "$valReg" != "$__wbscratch"
        ASSERT "$tmpReg" != "$__wbscratch"

        ; In most cases the callers of this macro are fine with scratching $ptrReg, the exception being the
        ; ref write barrier, which wants to scratch $valReg instead. Ideally we could set $ptrReg as the
        ; default for the $tmpReg parameter, but limitations in armasm won't allow that. Similarly it doesn't
        ; seem to like us trying to redefine $tmpReg in the body of the macro. Instead we define a new local
        ; string variable and set that either with the value of $tmpReg or $ptrReg if $tmpReg wasn't
        ; specified.
        LCLS tempReg
        IF "$tmpReg" == ""
tempReg     SETS "$ptrReg"
        ELSE
tempReg     SETS "$tmpReg"
        ENDIF

        ; Check whether the value object lies in the ephemeral generations. If not we don't have to update the
        ; card table.
        LOAD_GC_GLOBAL $__wbscratch, g_ephemeral_low
        cmp     $valReg, $__wbscratch
        blo     %FT0
        ; Only in post grow higher generation can be beyond ephemeral segment 
        IF $postGrow
            LOAD_GC_GLOBAL $__wbscratch, g_ephemeral_high
            cmp     $valReg, $__wbscratch
            bhs     %FT0
        ENDIF

        ; Update the card table.
        LOAD_GC_GLOBAL $__wbscratch, g_card_table
        add     $__wbscratch, $__wbscratch, $ptrReg, lsr #10

        ; On MP systems make sure the card hasn't already been set first to avoid thrashing cache lines
        ; between CPUs.
        ; @ARMTODO: Check that the conditional store doesn't unconditionally gain exclusive access to the
        ; cache line anyway. Compare perf with a branch over and verify that omitting the compare on uniproc
        ; machines really is a perf win.
        IF $mp
            ldrb    $tempReg, [$__wbscratch]
            cmp     $tempReg, #0xff
            movne   $tempReg, #0xff
            strbne  $tempReg, [$__wbscratch]
        ELSE
            mov     $tempReg, #0xff
            strb    $tempReg, [$__wbscratch]
        ENDIF
0
    MEND

    ; CHECK_GC_HEAP_RANGE
    ;
    ; Verifies that the given value points into the GC heap range. If so the macro will fall through to the
    ; following code. Otherwise (if the value points outside the GC heap) a branch to the supplied label will
    ; be made.
    ;
    ;   Input:
    ;       $ptrReg : register containing the location to be updated
    ;       $label  : label branched to on a range check failure
    ;
    ;   Output:
    ;       $__wbscratch : trashed
    ;
    MACRO
      CHECK_GC_HEAP_RANGE $ptrReg, $label
        ASSERT "$ptrReg" != "$__wbscratch"

        LOAD_GC_GLOBAL $__wbscratch, g_lowest_address
        cmp     $ptrReg, $__wbscratch
        blo     $label
        LOAD_GC_GLOBAL $__wbscratch, g_highest_address
        cmp     $ptrReg, $__wbscratch
        bhs     $label
    MEND

;
; Finally define the write barrier functions themselves. Currently we don't provide variations that use
; different input registers. If the JIT wants this at a later stage in order to improve code quality it would
; be a relatively simply change to implement via an additional macro parameter to WRITE_BARRIER_ENTRY.
;
; The calling convention for the first batch of write barriers is:
;
; On entry:
;   r0  : the destination address (LHS of the assignment)
;   r1  : the object reference (RHS of the assignment)
;
; On exit:
;   r0  : trashed
;   $__wbscratch : trashed
;

    ; If you update any of the writebarrier be sure to update the sizes of patchable 
    ; writebarriers in 
    ; see ValidateWriteBarriers()

    ; The write barriers are macro taking arguments like 
    ; $name: Name of the write barrier
    ; $mp: {true} for multi-proc, {false} otherwise
    ; $post: {true} for post-grow version, {false} otherwise

    MACRO
        JIT_WRITEBARRIER $name, $mp, $post
    WRITE_BARRIER_ENTRY $name
        IF $mp
            dmb                                 ; Perform a memory barrier
        ENDIF
        str     r1, [r0]                        ; Write the reference
        UPDATE_GC_SHADOW  r0, r1                ; Update the shadow GC heap for debugging
        UPDATE_CARD_TABLE r0, r1, $mp, $post ; Update the card table if necessary
        bx lr
    WRITE_BARRIER_END
    MEND

    MACRO
        JIT_CHECKEDWRITEBARRIER_SP $name, $post
    WRITE_BARRIER_ENTRY $name
        str     r1, [r0]                        ; Write the reference
        CHECK_GC_HEAP_RANGE r0, %F1             ; Check whether the destination is in the GC heap
        UPDATE_GC_SHADOW  r0, r1                ; Update the shadow GC heap for debugging
        UPDATE_CARD_TABLE r0, r1, {false}, $post; Update the card table if necessary
1
        bx lr
    WRITE_BARRIER_END
    MEND

    MACRO
        JIT_CHECKEDWRITEBARRIER_MP $name, $post
    WRITE_BARRIER_ENTRY $name
        CHECK_GC_HEAP_RANGE r0, %F1             ; Check whether the destination is in the GC heap
        dmb                                     ; Perform a memory barrier
        str     r1, [r0]                        ; Write the reference
        UPDATE_GC_SHADOW  r0, r1                ; Update the shadow GC heap for debugging
        UPDATE_CARD_TABLE r0, r1, {true}, $post ; Update the card table if necessary
        bx      lr
1
        str     r1, [r0]                        ; Write the reference
        bx      lr
    WRITE_BARRIER_END
    MEND

; The ByRef write barriers have a slightly different interface:
;
; On entry:
;   r0  : the destination address (object reference written here)
;   r1  : the source address (points to object reference to write)
;
; On exit:
;   r0  : incremented by 4
;   r1  : incremented by 4
;   r2  : trashed
;   $__wbscratch : trashed
;
    MACRO
        JIT_BYREFWRITEBARRIER $name, $mp, $post
    WRITE_BARRIER_ENTRY $name
        IF $mp
            dmb                                 ; Perform a memory barrier
        ENDIF
        ldr     r2, [r1]                        ; Load target object ref from source pointer
        str     r2, [r0]                        ; Write the reference to the destination pointer
        CHECK_GC_HEAP_RANGE r0, %F1             ; Check whether the destination is in the GC heap
        UPDATE_GC_SHADOW  r0, r2                ; Update the shadow GC heap for debugging
        UPDATE_CARD_TABLE r0, r2, $mp, $post, r2 ; Update the card table if necessary (trash r2 rather than r0)
1
        add     r0, #4                          ; Increment the destination pointer by 4
        add     r1, #4                          ; Increment the source pointer by 4
        bx      lr
    WRITE_BARRIER_END
    MEND

    BEGIN_WRITE_BARRIERS

    ; There 4 versions of each write barriers. A 2x2 combination of multi-proc/single-proc and pre/post grow version
    JIT_WRITEBARRIER JIT_WriteBarrier_SP_Pre,  {false}, {false}
    JIT_WRITEBARRIER JIT_WriteBarrier_SP_Post, {false}, {true}
    JIT_WRITEBARRIER JIT_WriteBarrier_MP_Pre,  {true}, {false}
    JIT_WRITEBARRIER JIT_WriteBarrier_MP_Post, {true}, {true}
    
    JIT_CHECKEDWRITEBARRIER_SP JIT_CheckedWriteBarrier_SP_Pre,  {false}
    JIT_CHECKEDWRITEBARRIER_SP JIT_CheckedWriteBarrier_SP_Post, {true}
    JIT_CHECKEDWRITEBARRIER_MP JIT_CheckedWriteBarrier_MP_Pre,  {false}
    JIT_CHECKEDWRITEBARRIER_MP JIT_CheckedWriteBarrier_MP_Post, {true}

    JIT_BYREFWRITEBARRIER JIT_ByRefWriteBarrier_SP_Pre,  {false}, {false}
    JIT_BYREFWRITEBARRIER JIT_ByRefWriteBarrier_SP_Post, {false}, {true}
    JIT_BYREFWRITEBARRIER JIT_ByRefWriteBarrier_MP_Pre,  {true},  {false}
    JIT_BYREFWRITEBARRIER JIT_ByRefWriteBarrier_MP_Post, {true},  {true}

    END_WRITE_BARRIERS

#ifdef FEATURE_READYTORUN

    NESTED_ENTRY DelayLoad_MethodCall_FakeProlog

    ; Match what the lazy thunk has pushed. The actual method arguments will be spilled later.
    PROLOG_PUSH         {r1-r3}

        ; This is where execution really starts.
DelayLoad_MethodCall
    EXPORT              DelayLoad_MethodCall

    PROLOG_PUSH         {r0}

    PROLOG_WITH_TRANSITION_BLOCK 0x0, {true}, DoNotPushArgRegs

    ; Load the helper arguments
    ldr         r5, [sp,#(__PWTB_TransitionBlock+10*4)] ; pModule
    ldr         r6, [sp,#(__PWTB_TransitionBlock+11*4)] ; sectionIndex
    ldr         r7, [sp,#(__PWTB_TransitionBlock+12*4)] ; indirection

    ; Spill the actual method arguments
    str         r1, [sp,#(__PWTB_TransitionBlock+10*4)]
    str         r2, [sp,#(__PWTB_TransitionBlock+11*4)]
    str         r3, [sp,#(__PWTB_TransitionBlock+12*4)]

    add         r0, sp, #__PWTB_TransitionBlock ; pTransitionBlock

    mov         r1, r7          ; pIndirection
    mov         r2, r6          ; sectionIndex
    mov         r3, r5          ; pModule

    bl          ExternalMethodFixupWorker

    ; mov the address we patched to in R12 so that we can tail call to it
    mov         r12, r0

    EPILOG_WITH_TRANSITION_BLOCK_TAILCALL

    ; Share the patch label
    EPILOG_BRANCH ExternalMethodFixupPatchLabel

    NESTED_END


    MACRO
        DynamicHelper $frameFlags, $suffix

        GBLS __FakePrologName
__FakePrologName SETS "DelayLoad_Helper":CC:"$suffix":CC:"_FakeProlog"

        NESTED_ENTRY $__FakePrologName

        ; Match what the lazy thunk has pushed. The actual method arguments will be spilled later.
        PROLOG_PUSH         {r1-r3}

        GBLS __RealName
__RealName SETS "DelayLoad_Helper":CC:"$suffix"

        ; This is where execution really starts.
$__RealName
        EXPORT              $__RealName

        PROLOG_PUSH         {r0}

        PROLOG_WITH_TRANSITION_BLOCK 0x4, {true}, DoNotPushArgRegs

        ; Load the helper arguments
        ldr         r5, [sp,#(__PWTB_TransitionBlock+10*4)] ; pModule
        ldr         r6, [sp,#(__PWTB_TransitionBlock+11*4)] ; sectionIndex
        ldr         r7, [sp,#(__PWTB_TransitionBlock+12*4)] ; indirection

        ; Spill the actual method arguments
        str         r1, [sp,#(__PWTB_TransitionBlock+10*4)]
        str         r2, [sp,#(__PWTB_TransitionBlock+11*4)]
        str         r3, [sp,#(__PWTB_TransitionBlock+12*4)]

        add         r0, sp, #__PWTB_TransitionBlock ; pTransitionBlock

        mov         r1, r7          ; pIndirection
        mov         r2, r6          ; sectionIndex
        mov         r3, r5          ; pModule

        mov         r4, $frameFlags
        str         r4, [sp,#0]

        bl          DynamicHelperWorker

        cbnz        r0, %FT0
        ldr         r0, [sp,#(__PWTB_TransitionBlock+9*4)]  ; The result is stored in the argument area of the transition block

        EPILOG_WITH_TRANSITION_BLOCK_RETURN

0
        mov         r12, r0
        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
        EPILOG_BRANCH_REG   r12

        NESTED_END

    MEND

    DynamicHelper DynamicHelperFrameFlags_Default
    DynamicHelper DynamicHelperFrameFlags_ObjectArg, _Obj
    DynamicHelper DynamicHelperFrameFlags_ObjectArg | DynamicHelperFrameFlags_ObjectArg2, _ObjObj

#endif // FEATURE_READYTORUN

; Must be at very end of file
    END
