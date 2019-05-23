// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ==++==
//

//
// ==--==
#include "unixasmmacros.inc"
#include "asmconstants.h"

.syntax unified
.thumb

// LPVOID __stdcall GetCurrentIP(void)//
    LEAF_ENTRY GetCurrentIP, _TEXT
        mov     r0, lr
        bx      lr
    LEAF_END GetCurrentIP, _TEXT

// LPVOID __stdcall GetCurrentSP(void)//
    LEAF_ENTRY GetCurrentSP, _TEXT
        mov     r0, sp
        bx      lr
    LEAF_END GetCurrentSP, _TEXT

//-----------------------------------------------------------------------------
// This helper routine enregisters the appropriate arguments and makes the
// actual call.
//-----------------------------------------------------------------------------
//void CallDescrWorkerInternal(CallDescrData * pCallDescrData)//
        NESTED_ENTRY CallDescrWorkerInternal,_TEXT,NoHandler
        PROLOG_PUSH         "{r4,r5,r7,lr}"
        PROLOG_STACK_SAVE_OFFSET   r7, #8

        mov     r5,r0 // save pCallDescrData in r5

        ldr     r1, [r5,#CallDescrData__numStackSlots]
        cbz     r1, LOCAL_LABEL(Ldonestack)

        // Add frame padding to ensure frame size is a multiple of 8 (a requirement of the OS ABI).
        // We push four registers (above) and numStackSlots arguments (below). If this comes to an odd number
        // of slots we must pad with another. This simplifies to "if the low bit of numStackSlots is set,
        // extend the stack another four bytes".
        lsls    r2, r1, #2
        and     r3, r2, #4
        sub     sp, sp, r3

        // This loop copies numStackSlots words
        // from [pSrcEnd-4,pSrcEnd-8,...] to [sp-4,sp-8,...]
        ldr     r0, [r5,#CallDescrData__pSrc]
        add     r0,r0,r2
LOCAL_LABEL(Lstackloop):
        ldr     r2, [r0,#-4]!
        str     r2, [sp,#-4]!
        subs    r1, r1, #1
        bne     LOCAL_LABEL(Lstackloop)
LOCAL_LABEL(Ldonestack):

        // If FP arguments are supplied in registers (r3 != NULL) then initialize all of them from the pointer
        // given in r3. Do not use "it" since it faults in floating point even when the instruction is not executed.
        ldr     r3, [r5,#CallDescrData__pFloatArgumentRegisters]
        cbz     r3, LOCAL_LABEL(LNoFloatingPoint)
        vldm    r3, {s0-s15}
LOCAL_LABEL(LNoFloatingPoint):

        // Copy [pArgumentRegisters, ..., pArgumentRegisters + 12]
        // into r0, ..., r3

        ldr     r4, [r5,#CallDescrData__pArgumentRegisters]
        ldm     r4, {r0-r3}

        CHECK_STACK_ALIGNMENT

        // call pTarget
        // Note that remoting expect target in r4.
        ldr     r4, [r5,#CallDescrData__pTarget]
        blx     r4

        ldr     r3, [r5,#CallDescrData__fpReturnSize]

        // Save FP return value if appropriate
        cbz     r3, LOCAL_LABEL(LFloatingPointReturnDone)

        // Float return case
        // Do not use "it" since it faults in floating point even when the instruction is not executed.
        cmp     r3, #4
        bne     LOCAL_LABEL(LNoFloatReturn)
        vmov    r0, s0
        b       LOCAL_LABEL(LFloatingPointReturnDone)
LOCAL_LABEL(LNoFloatReturn):

        // Double return case
        // Do not use "it" since it faults in floating point even when the instruction is not executed.
        cmp     r3, #8
        bne     LOCAL_LABEL(LNoDoubleReturn)
        vmov    r0, r1, s0, s1
        b       LOCAL_LABEL(LFloatingPointReturnDone)
LOCAL_LABEL(LNoDoubleReturn):

        add     r2, r5, #CallDescrData__returnValue

        cmp     r3, #16
        bne     LOCAL_LABEL(LNoFloatHFAReturn)
        vstm    r2, {s0-s3}
        b       LOCAL_LABEL(LReturnDone)
LOCAL_LABEL(LNoFloatHFAReturn):

        cmp     r3, #32
        bne     LOCAL_LABEL(LNoDoubleHFAReturn)
        vstm    r2, {d0-d3}
        b       LOCAL_LABEL(LReturnDone)
LOCAL_LABEL(LNoDoubleHFAReturn):

        EMIT_BREAKPOINT // Unreachable

LOCAL_LABEL(LFloatingPointReturnDone):

        // Save return value into retbuf
        str     r0, [r5, #(CallDescrData__returnValue + 0)]
        str     r1, [r5, #(CallDescrData__returnValue + 4)]

LOCAL_LABEL(LReturnDone):

#ifdef _DEBUG
        // trash the floating point registers to ensure that the HFA return values 
        // won't survive by accident
        vldm    sp, {d0-d3}
#endif

        EPILOG_STACK_RESTORE_OFFSET   r7, #8
        EPILOG_POP              "{r4,r5,r7,pc}"

        NESTED_END CallDescrWorkerInternal,_TEXT


//-----------------------------------------------------------------------------
// This helper routine is where returns for irregular tail calls end up
// so they can dynamically pop their stack arguments.
//-----------------------------------------------------------------------------
// 
// Stack Layout (stack grows up, 0 at the top, offsets relative to frame pointer, r7):
// 
// sp ->     callee stack arguments
//           :
//           :
//    -0Ch   gsCookie
// TailCallHelperFrame ->
//    -08h   __VFN_table
//    -04h   m_Next
// r7 ->
//    +00h   m_calleeSavedRgisters.r4
//    +04h                        .r5
//    +08h                        .r6
//    +0Ch                        .r7
//    +10h                        .r8
//    +14h                        .r9
//    +18h                        .r10
// r11->
//    +1Ch                        .r11
//    +20h                        .r14 -or- m_ReturnAddress
//
// r6 -> GetThread()
// r5 -> r6->m_pFrame (old Frame chain head)
// r11 is used to preserve the ETW call stack

    NESTED_ENTRY TailCallHelperStub, _TEXT, NoHandler
        //
        // This prolog is never executed, but we keep it here for reference
        // and for the unwind data it generates
        //
        
        // Spill callee saved registers and return address.
        PROLOG_PUSH         "{r4-r11,lr}"
        
        PROLOG_STACK_SAVE_OFFSET   r7, #12

        //
        // This is the code that would have to run to setup this frame
        // like the C++ helper does before calling RtlRestoreContext
        //
        // Allocate space for the rest of the frame and GSCookie.
        // PROLOG_STACK_ALLOC  0x0C
        //
        // Set r11 for frame chain
        //add     r11, r7, 0x1C 
        //
        // Set the vtable for TailCallFrame
        //bl      TCF_GETMETHODFRAMEVPTR
        //str     r0, [r7, #-8]
        //
        // Initialize the GSCookie within the Frame
        //ldr     r0, =s_gsCookie
        //str     r0, [r7, #-0x0C]
        //
        // Link the TailCallFrameinto the Frame chain
        // and initialize r5 & r6 for unlinking later
        //CALL_GETTHREAD
        //mov     r6, r0
        //ldr     r5, [r6, #Thread__m_pFrame]        
        //str     r5, [r7, #-4]
        //sub     r0, r7, 8
        //str     r0, [r6, #Thread__m_pFrame]
        //
        // None of the previous stuff is ever executed,
        // but we keep it here for reference
        //

        //
        // Here's the pretend call (make it real so the unwinder
        // doesn't think we're in the prolog)
        //
        bl      C_FUNC(TailCallHelperStub)
        //
        // with the real return address pointing to this real epilog
        //
C_FUNC(JIT_TailCallHelperStub_ReturnAddress):
.global C_FUNC(JIT_TailCallHelperStub_ReturnAddress)

        //
        // Our epilog (which also unlinks the StubHelperFrame)
        // Be careful not to trash the return registers
        //

#ifdef _DEBUG
        ldr     r3, =s_gsCookie
        ldr     r3, [r3]
        ldr     r2, [r7, #-0x0C]
        cmp     r2, r3
        beq     LOCAL_LABEL(GoodGSCookie)
        bl      C_FUNC(DoJITFailFast)
LOCAL_LABEL(GoodGSCookie):
#endif // _DEBUG

        //
        // unlink the TailCallFrame
        //
        str     r5, [r6, #Thread__m_pFrame]

        // 
        // epilog
        //
        EPILOG_STACK_RESTORE_OFFSET   r7, #12
        EPILOG_POP              "{r4-r11,lr}"
        bx lr
        
    NESTED_END TailCallHelperStub, _TEXT

// ------------------------------------------------------------------

// void LazyMachStateCaptureState(struct LazyMachState *pState)//
    LEAF_ENTRY LazyMachStateCaptureState, _TEXT

        // marks that this is not yet valid
        mov     r1, #0
        str     r1, [r0, #MachState__isValid]

        str     lr, [r0, #LazyMachState_captureIp]
        str     sp, [r0, #LazyMachState_captureSp]

        add     r1, r0, #LazyMachState_captureR4_R11
        stm     r1, {r4-r11}

        mov     pc, lr

    LEAF_END LazyMachStateCaptureState, _TEXT

// void SinglecastDelegateInvokeStub(Delegate *pThis)
    LEAF_ENTRY SinglecastDelegateInvokeStub, _TEXT
        cmp     r0, #0
        beq     LOCAL_LABEL(LNullThis)

        ldr     r12, [r0, #DelegateObject___methodPtr]
        ldr     r0, [r0, #DelegateObject___target]

        bx      r12

LOCAL_LABEL(LNullThis):
        mov     r0, #CORINFO_NullReferenceException_ASM
        b       C_FUNC(JIT_InternalThrow)

    LEAF_END SinglecastDelegateInvokeStub, _TEXT

//
// r12 = UMEntryThunk*
//
        NESTED_ENTRY TheUMEntryPrestub,_TEXT,NoHandler

        PROLOG_PUSH "{r0-r4,r7,r8,lr}" // add r8 to make stack aligned by 8B
        PROLOG_STACK_SAVE_OFFSET r7, #20
        PROLOG_VPUSH {d0-d7}

        CHECK_STACK_ALIGNMENT

        mov     r0, r12
        bl      C_FUNC(TheUMEntryPrestubWorker)

        // Record real target address in r12.
        mov     r12, r0

        // Epilog
        EPILOG_VPOP {d0-d7}
        EPILOG_POP "{r0-r4,r7,r8,lr}"
        bx r12

        NESTED_END TheUMEntryPrestub,_TEXT

//
// r12 = UMEntryThunk*
//
        NESTED_ENTRY UMThunkStub,_TEXT,UnhandledExceptionHandlerUnix
        PROLOG_PUSH         "{r4,r5,r7,r11,lr}"
        PROLOG_STACK_SAVE_OFFSET   r7, #8

        alloc_stack         4 * 5
        stm                 sp, {r0-r3,r12}

        //GBLA UMThunkStub_HiddenArgOffest // offset of saved UMEntryThunk *
        //GBLA UMThunkStub_StackArgsOffest // offset of original stack args
        //GBLA UMThunkStub_StackArgsSize   // total size of UMThunkStub frame
UMThunkStub_HiddenArgOffset = (-3)*4
UMThunkStub_StackArgsOffset = 3*4
UMThunkStub_StackArgsSize = 10*4

        CHECK_STACK_ALIGNMENT

        bl                  C_FUNC(GetThread)
        cbz                 r0, LOCAL_LABEL(UMThunkStub_DoThreadSetup)

LOCAL_LABEL(UMThunkStub_HaveThread):
        mov                 r5, r0                  // r5 = Thread *

        ldr                 r2, =g_TrapReturningThreads

        mov                 r4, 1
        str                 r4, [r5, #Thread__m_fPreemptiveGCDisabled]

        ldr                 r3, [r2]
        cbnz                r3, LOCAL_LABEL(UMThunkStub_DoTrapReturningThreads)

LOCAL_LABEL(UMThunkStub_InCooperativeMode):
        ldr                 r12, [r7, #UMThunkStub_HiddenArgOffset]
        ldr                 r3, [r12, #UMEntryThunk__m_pUMThunkMarshInfo]
        ldr                 r2, [r3, #UMThunkMarshInfo__m_cbActualArgSize]
        cbz                 r2, LOCAL_LABEL(UMThunkStub_ArgumentsSetup)

        add                 r0, r7, #UMThunkStub_StackArgsOffset // Source pointer
        add                 r0, r0, r2
        lsr                 r1, r2, #2      // Count of stack slots to copy

        and                 r2, r2, #4      // Align the stack
        sub                 sp, sp, r2

LOCAL_LABEL(UMThunkStub_StackLoop):
        ldr                 r2, [r0,#-4]!
        str                 r2, [sp,#-4]!
        subs                r1, r1, #1
        bne                 LOCAL_LABEL(UMThunkStub_StackLoop)

LOCAL_LABEL(UMThunkStub_ArgumentsSetup):
        ldr                 r4, [r3, #UMThunkMarshInfo__m_pILStub]

        // reload argument registers
        sub                 r0, r7, #28
        ldm                 r0, {r0-r3}

        CHECK_STACK_ALIGNMENT

        blx                 r4

LOCAL_LABEL(UMThunkStub_PostCall):
        mov                 r4, 0
        str                 r4, [r5, #Thread__m_fPreemptiveGCDisabled]

        EPILOG_STACK_RESTORE_OFFSET    r7, #8
        EPILOG_POP           "{r4,r5,r7,r11,pc}"

LOCAL_LABEL(UMThunkStub_DoThreadSetup):
        sub                 sp, #SIZEOF__FloatArgumentRegisters
        vstm                sp, {d0-d7}
        bl                  C_FUNC(CreateThreadBlockThrow)
        vldm                sp, {d0-d7}
        add                 sp, #SIZEOF__FloatArgumentRegisters
        b                   LOCAL_LABEL(UMThunkStub_HaveThread)

LOCAL_LABEL(UMThunkStub_DoTrapReturningThreads):
        sub                 sp, #SIZEOF__FloatArgumentRegisters
        vstm                sp, {d0-d7}
        mov                 r0, r5              // Thread* pThread
        ldr                 r1, [r7, #UMThunkStub_HiddenArgOffset]  // UMEntryThunk* pUMEntry
        bl                  C_FUNC(UMThunkStubRareDisableWorker)
        vldm                sp, {d0-d7}
        add                 sp, #SIZEOF__FloatArgumentRegisters
        b                   LOCAL_LABEL(UMThunkStub_InCooperativeMode)

        NESTED_END UMThunkStub,_TEXT

// ------------------------------------------------------------------

        NESTED_ENTRY ThePreStub, _TEXT, NoHandler

        PROLOG_WITH_TRANSITION_BLOCK

        add         r0, sp, #__PWTB_TransitionBlock // pTransitionBlock
        mov         r1, r12                         // pMethodDesc

        bl          C_FUNC(PreStubWorker)

        mov         r12, r0

        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
        bx r12

        NESTED_END ThePreStub, _TEXT

// ------------------------------------------------------------------
        NESTED_ENTRY ThePreStubCompactARM, _TEXT, NoHandler

        // r12 - address of compact entry point + PC_REG_RELATIVE_OFFSET

        PROLOG_WITH_TRANSITION_BLOCK

        mov         r0, r12

        bl          C_FUNC(PreStubGetMethodDescForCompactEntryPoint)

        mov         r12, r0                                          // pMethodDesc

        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL

        b           C_FUNC(ThePreStub)

        NESTED_END ThePreStubCompactARM, _TEXT
// ------------------------------------------------------------------
// This method does nothing. It's just a fixed function for the debugger to put a breakpoint on.
        LEAF_ENTRY ThePreStubPatch, _TEXT
        nop
ThePreStubPatchLabel:
        .global ThePreStubPatchLabel
        bx      lr
        LEAF_END ThePreStubPatch, _TEXT

// ------------------------------------------------------------------
// The call in ndirect import precode points to this function.
        NESTED_ENTRY NDirectImportThunk, _TEXT, NoHandler

        PROLOG_PUSH "{r0-r4,r7,r8,lr}"                  // Spill general argument registers, return address and
        PROLOG_STACK_SAVE_OFFSET r7, #20
                                                        // arbitrary register to keep stack aligned
        PROLOG_VPUSH {d0-d7}                                   // Spill floating point argument registers

        CHECK_STACK_ALIGNMENT

        mov     r0, r12
        bl      C_FUNC(NDirectImportWorker)
        mov     r12, r0

        EPILOG_VPOP {d0-d7}
        EPILOG_POP "{r0-r4,r7,r8,lr}"

        // If we got back from NDirectImportWorker, the MD has been successfully
        // linked. Proceed to execute the original DLL call.
        bx r12

        NESTED_END NDirectImportThunk, _TEXT

// ------------------------------------------------------------------
// The call in fixup precode initally points to this function.
// The pupose of this function is to load the MethodDesc and forward the call the prestub.
        NESTED_ENTRY PrecodeFixupThunk, _TEXT, NoHandler

        // r12 = FixupPrecode *

        PROLOG_PUSH     "{r0-r1}"

        // Inline computation done by FixupPrecode::GetMethodDesc()
        ldrb    r0, [r12, #3]           // m_PrecodeChunkIndex
        ldrb    r1, [r12, #2]           // m_MethodDescChunkIndex

        add     r12,r12,r0,lsl #3
        add     r0,r12,r0,lsl #2
        ldr     r0, [r0,#8]
        add     r12,r0,r1,lsl #2

        EPILOG_POP      "{r0-r1}"
        b C_FUNC(ThePreStub)

        NESTED_END PrecodeFixupThunk, _TEXT

// ------------------------------------------------------------------
// void ResolveWorkerAsmStub(r0, r1, r2, r3, r4:IndirectionCellAndFlags, r12:DispatchToken)
//
// The stub dispatch thunk which transfers control to VSD_ResolveWorker.
        NESTED_ENTRY ResolveWorkerAsmStub, _TEXT, NoHandler

        PROLOG_WITH_TRANSITION_BLOCK

        add         r0, sp, #__PWTB_TransitionBlock // pTransitionBlock
        mov         r2, r12                         // token

        // indirection cell in r4 - should be consistent with REG_ARM_STUB_SPECIAL
        bic         r1, r4, #3          // indirection cell
        and         r3, r4, #3          // flags

        bl          C_FUNC(VSD_ResolveWorker)

        mov         r12, r0

        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
        bx r12

        NESTED_END ResolveWorkerAsmStub, _TEXT

// ------------------------------------------------------------------
// void ResolveWorkerChainLookupAsmStub(r0, r1, r2, r3, r4:IndirectionCellAndFlags, r12:DispatchToken)
        NESTED_ENTRY ResolveWorkerChainLookupAsmStub, _TEXT, NoHandler

        // ARMSTUB TODO: implement chained lookup
        b           C_FUNC(ResolveWorkerAsmStub)

        NESTED_END ResolveWorkerChainLookupAsmStub, _TEXT

        //
        // If a preserved register were pushed onto the stack between
        // the managed caller and the H_M_F, _R4_R11 will point to its
        // location on the stack and it would have been updated on the
        // stack by the GC already and it will be popped back into the
        // appropriate register when the appropriate epilog is run.
        //
        // Otherwise, the register is preserved across all the code
        // in this HCALL or FCALL, so we need to update those registers
        // here because the GC will have updated our copies in the
        // frame.
        //
        // So, if _R4_R11 points into the MachState, we need to update
        // the register here.  That's what this macro does.
        //

        .macro RestoreRegMS regIndex, reg

        // Incoming:
        //
        // R0 = address of MachState
        //
        // $regIndex: Index of the register (R4-R11). For R4, index is 4.
        //            For R5, index is 5, and so on.
        //
        // $reg: Register name (e.g. R4, R5, etc)
        //
        // Get the address of the specified captured register from machine state
        add     r2, r0, #(MachState__captureR4_R11 + ((\regIndex-4)*4))

        // Get the address of the specified preserved register from machine state
        ldr     r3, [r0, #(MachState___R4_R11 + ((\regIndex-4)*4))]

        cmp     r2, r3
        bne     0f
        ldr     \reg, [r2]
0:

        .endm

//
// EXTERN_C void ProfileEnterNaked(FunctionIDOrClientID functionIDOrClientID);
//
NESTED_ENTRY ProfileEnterNaked, _TEXT, NoHandler
    PROLOG_PUSH "{r4, r5, r7, r11, lr}"
    PROLOG_STACK_SAVE_OFFSET r7, #8

    // fields of PROFILE_PLATFORM_SPECIFIC_DATA, in reverse order

    // UINT32      r0;         // Keep r0 & r1 contiguous to make returning 64-bit results easier
    // UINT32      r1;
    // void       *r11;
    // void       *Pc;
    // union                   // Float arg registers as 32-bit (s0-s15) and 64-bit (d0-d7)
    // {
    //     UINT32  s[16];
    //     UINT64  d[8];
    // };
    // FunctionID  functionId;
    // void       *probeSp;    // stack pointer of managed function 
    // void       *profiledSp; // location of arguments on stack
    // LPVOID      hiddenArg;
    // UINT32      flags;
    movw        r4, #1
    push        { /* flags      */ r4 }
    movw        r4, #0
    push        { /* hiddenArg  */ r4 }
    add         r5, r11, #8
    push        { /* profiledSp */ r5 }
    add         r5, sp, #32
    push        { /* probeSp    */ r5 }
    push        { /* functionId */ r0 }
    vpush.64    { d0 - d7 }
    push        { lr }
    push        { r11 }
    push        { /* return value, r4 is NULL */ r4 }
    push        { /* return value, r4 is NULL */ r4 }
    mov         r1, sp
    bl          C_FUNC(ProfileEnter)
    EPILOG_STACK_RESTORE_OFFSET r7, #8
    EPILOG_POP "{r4, r5, r7, r11, pc}"
NESTED_END ProfileEnterNaked, _TEXT

//
// EXTERN_C void ProfileLeaveNaked(FunctionIDOrClientID functionIDOrClientID);
//
NESTED_ENTRY ProfileLeaveNaked, _TEXT, NoHandler
    PROLOG_PUSH "{r1, r2, r4, r5, r7, r11, lr}"
    PROLOG_STACK_SAVE_OFFSET r7, #16

    // fields of PROFILE_PLATFORM_SPECIFIC_DATA, in reverse order

    // UINT32      r0;         // Keep r0 & r1 contiguous to make returning 64-bit results easier
    // UINT32      r1;
    // void       *r11;
    // void       *Pc;
    // union                   // Float arg registers as 32-bit (s0-s15) and 64-bit (d0-d7)
    // {
    //     UINT32  s[16];
    //     UINT64  d[8];
    // };
    // FunctionID  functionId;
    // void       *probeSp;    // stack pointer of managed function 
    // void       *profiledSp; // location of arguments on stack
    // LPVOID      hiddenArg;
    // UINT32      flags;
    movw        r4, #2
    push        { /* flags      */ r4 }
    movw        r4, #0
    push        { /* hiddenArg  */ r4 }
    add         r5, r11, #8
    push        { /* profiledSp */ r5 }
    add         r5, sp, #40
    push        { /* probeSp    */ r5 }
    push        { /* functionId */ r0 }
    vpush.64    { d0 - d7 }
    push        { lr }
    push        { r11 }
    push        { r1 }
    push        { r0 }
    mov         r1, sp
    bl          C_FUNC(ProfileLeave)
    EPILOG_STACK_RESTORE_OFFSET r7, #16
    EPILOG_POP "{r1, r2, r4, r5, r7, r11, pc}"
NESTED_END ProfileLeaveNaked, _TEXT

//
// EXTERN_C void ProfileTailcallNaked(FunctionIDOrClientID functionIDOrClientID);
//
NESTED_ENTRY ProfileTailcallNaked, _TEXT, NoHandler
    PROLOG_PUSH "{r1, r2, r4, r5, r7, r11, lr}"
    PROLOG_STACK_SAVE_OFFSET r7, #16

    // fields of PROFILE_PLATFORM_SPECIFIC_DATA, in reverse order

    // UINT32      r0;         // Keep r0 & r1 contiguous to make returning 64-bit results easier
    // UINT32      r1;
    // void       *r11;
    // void       *Pc;
    // union                   // Float arg registers as 32-bit (s0-s15) and 64-bit (d0-d7)
    // {
    //     UINT32  s[16];
    //     UINT64  d[8];
    // };
    // FunctionID  functionId;
    // void       *probeSp;    // stack pointer of managed function 
    // void       *profiledSp; // location of arguments on stack
    // LPVOID      hiddenArg;
    // UINT32      flags;
    movw        r4, #2
    push        { /* flags      */ r4 }
    movw        r4, #0
    push        { /* hiddenArg  */ r4 }
    add         r5, r11, #8
    push        { /* profiledSp */ r5 }
    add         r5, sp, #40
    push        { /* probeSp    */ r5 }
    push        { /* functionId */ r0 }
    vpush.64    { d0 - d7 }
    push        { lr }
    push        { r11 }
    push        { r1 }
    push        { r0 }
    mov         r1, sp
    bl          C_FUNC(ProfileTailcall)
    EPILOG_STACK_RESTORE_OFFSET r7, #16
    EPILOG_POP "{r1, r2, r4, r5, r7, r11, pc}"
NESTED_END ProfileTailcallNaked, _TEXT

// EXTERN_C int __fastcall HelperMethodFrameRestoreState(
//         INDEBUG_COMMA(HelperMethodFrame *pFrame)
//         MachState *pState
//         )
        LEAF_ENTRY HelperMethodFrameRestoreState, _TEXT

#ifdef _DEBUG
        mov r0, r1
#endif

        // If machine state is invalid, then simply exit
        ldr r1, [r0, #MachState__isValid]
        cmp r1, #0
        beq LOCAL_LABEL(Done)

        RestoreRegMS 4, R4
        RestoreRegMS 5, R5
        RestoreRegMS 6, R6
        RestoreRegMS 7, R7
        RestoreRegMS 8, R8
        RestoreRegMS 9, R9
        RestoreRegMS 10, R10
        RestoreRegMS 11, R11
LOCAL_LABEL(Done):
        // Its imperative that the return value of HelperMethodFrameRestoreState is zero
        // as it is used in the state machine to loop until it becomes zero.
        // Refer to HELPER_METHOD_FRAME_END macro for details.
        mov r0,#0
        bx lr

        LEAF_END HelperMethodFrameRestoreState, _TEXT

#if 0
// ------------------------------------------------------------------
// Macro to generate Redirection Stubs
//
// $reason : reason for redirection
//                     Eg. GCThreadControl
// NOTE: If you edit this macro, make sure you update GetCONTEXTFromRedirectedStubStackFrame.
// This function is used by both the personality routine and the debugger to retrieve the original CONTEXT.
        .macro GenerateRedirectedHandledJITCaseStub reason

        NESTED_ENTRY RedirectedHandledJITCaseFor\reason\()_Stub, _TEXT, NoHandler

        PROLOG_PUSH "{r7,lr}"   // return address
        PROLOG_STACK_SAVE r7
        alloc_stack 4           // stack slot to save the CONTEXT *

        //REDIRECTSTUB_SP_OFFSET_CONTEXT is defined in asmconstants.h
        //If CONTEXT is not saved at 0 offset from SP it must be changed as well.
        //ASSERT REDIRECTSTUB_SP_OFFSET_CONTEXT == 0

        // Runtime check for 8-byte alignment. This check is necessary as this function can be
        // entered before complete execution of the prolog of another function.
        and r0, r7, #4
        sub sp, sp, r0

        // stack must be 8 byte aligned
        CHECK_STACK_ALIGNMENT

        //
        // Save a copy of the redirect CONTEXT*.
        // This is needed for the debugger to unwind the stack.
        //
        bl GetCurrentSavedRedirectContext
        str r0, [r7]

        //
        // Fetch the interrupted pc and save it as our return address.
        //
        ldr r1, [r0, #CONTEXT_Pc]
        str r1, [r7, #8]

        //
        // Call target, which will do whatever we needed to do in the context
        // of the target thread, and will RtlRestoreContext when it is done.
        //
        bl _RedirectedHandledJITCaseFor\reason\()_Stub@Thread@@CAXXZ

        EMIT_BREAKPOINT // Unreachable

// Put a label here to tell the debugger where the end of this function is.
RedirectedHandledJITCaseFor\reason\()_StubEnd:
        .global RedirectedHandledJITCaseFor\reason\()_StubEnd

        NESTED_END RedirectedHandledJITCaseFor\reason\()_Stub, _TEXT

        .endm

// ------------------------------------------------------------------
// Redirection Stub for GC in fully interruptible method
        GenerateRedirectedHandledJITCaseStub GCThreadControl
// ------------------------------------------------------------------
        GenerateRedirectedHandledJITCaseStub DbgThreadControl
// ------------------------------------------------------------------
        GenerateRedirectedHandledJITCaseStub UserSuspend

#ifdef _DEBUG
// ------------------------------------------------------------------
// Redirection Stub for GC Stress
        GenerateRedirectedHandledJITCaseStub GCStress
#endif

#endif

// ------------------------------------------------------------------
// Functions to probe for stack space
// Input reg r4 = amount of stack to probe for
// value of reg r4 is preserved on exit from function
// r12 is trashed
// The below two functions were copied from vctools\crt\crtw32\startup\arm\chkstk.asm

    NESTED_ENTRY checkStack, _TEXT, NoHandler
    subs        r12,sp,r4
    mrc         p15,#0,r4,c13,c0,#2 // get TEB *
    ldr         r4,[r4,#8]          // get Stack limit
    bcc         LOCAL_LABEL(checkStack_neg) // if r12 is less then 0 set it to 0
LOCAL_LABEL(checkStack_label1):
    cmp         r12, r4
    bcc         C_FUNC(stackProbe)  // must probe to extend guardpage if r12 is beyond stackLimit
    sub         r4, sp, r12         // restore value of r4
    bx lr
LOCAL_LABEL(checkStack_neg):
    mov         r12, #0
    b           LOCAL_LABEL(checkStack_label1)
    NESTED_END checkStack, _TEXT

    NESTED_ENTRY stackProbe, _TEXT, NoHandler
    PROLOG_PUSH "{r5,r6}"
    mov         r6, r12
    bfc         r6, #0, #0xc  // align down (4K)
LOCAL_LABEL(stackProbe_loop):
    sub         r4,r4,#0x1000 // dec stack Limit by 4K as page size is 4K
    ldr         r5,[r4]       // try to read ... this should move the guard page
    cmp         r4,r6
    bne         LOCAL_LABEL(stackProbe_loop)
    EPILOG_POP "{r5,r6}"
    sub r4,sp,r12
    bx lr
    NESTED_END stackProbe, _TEXT

#ifdef FEATURE_PREJIT
//------------------------------------------------
// VirtualMethodFixupStub
//
// In NGEN images, virtual slots inherited from cross-module dependencies
// point to a jump thunk that calls into the following function that will
// call into a VM helper. The VM helper is responsible for patching up
// thunk, upon executing the precode, so that all subsequent calls go directly
// to the actual method body.
//
// This is done lazily for performance reasons.
//
// On entry:
//
// R0 = "this" pointer
// R12 = Address of thunk + 4

    NESTED_ENTRY VirtualMethodFixupStub, _TEXT, NoHandler

    // Save arguments and return address
    PROLOG_PUSH "{r0-r3, r7,r8, lr}" // keep increase by 8B for alignment
    PROLOG_STACK_SAVE_OFFSET r7, #20

    // Align stack
    alloc_stack  SIZEOF__FloatArgumentRegisters + 4
    vstm                sp, {d0-d7}


    CHECK_STACK_ALIGNMENT

    // R12 contains an address that is 4 bytes ahead of
    // where the thunk starts. Refer to ZapImportVirtualThunk::Save
    // for details on this.
    //
    // Move the correct thunk start address in R1
    sub r1, r12, #4

    // Call the helper in the VM to perform the actual fixup
    // and tell us where to tail call. R0 already contains
    // the this pointer.
    bl C_FUNC(VirtualMethodFixupWorker)

    // On return, R0 contains the target to tailcall to
    mov         r12, r0

    // pop the stack and restore original register state
    vldm               sp, {d0-d7}
    free_stack SIZEOF__FloatArgumentRegisters + 4
    EPILOG_POP "{r0-r3, r7,r8, lr}"

    PATCH_LABEL VirtualMethodFixupPatchLabel

    // and tailcall to the actual method
    bx r12

    NESTED_END VirtualMethodFixupStub, _TEXT
#endif // FEATURE_PREJIT

//------------------------------------------------
// ExternalMethodFixupStub
//
// In NGEN images, calls to cross-module external methods initially
// point to a jump thunk that calls into the following function that will
// call into a VM helper. The VM helper is responsible for patching up the
// thunk, upon executing the precode, so that all subsequent calls go directly
// to the actual method body.
//
// This is done lazily for performance reasons.
//
// On entry:
//
// R12 = Address of thunk + 4

    NESTED_ENTRY ExternalMethodFixupStub, _TEXT, NoHandler

    PROLOG_WITH_TRANSITION_BLOCK

    add         r0, sp, #__PWTB_TransitionBlock // pTransitionBlock

    // Adjust (read comment above for details) and pass the address of the thunk
    sub         r1, r12, #4                     // pThunk

    mov         r2, #0  // sectionIndex
    mov         r3, #0  // pModule
    bl          C_FUNC(ExternalMethodFixupWorker)

    // mov the address we patched to in R12 so that we can tail call to it
    mov         r12, r0

    EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
    PATCH_LABEL ExternalMethodFixupPatchLabel
    bx r12

    NESTED_END ExternalMethodFixupStub, _TEXT

#ifdef FEATURE_PREJIT
//------------------------------------------------
// StubDispatchFixupStub
//
// In NGEN images, calls to interface methods initially
// point to a jump thunk that calls into the following function that will
// call into a VM helper. The VM helper is responsible for patching up the
// thunk with actual stub dispatch stub.
//
// On entry:
//
// R4 = Address of indirection cell

    NESTED_ENTRY StubDispatchFixupStub, _TEXT, NoHandler

    PROLOG_WITH_TRANSITION_BLOCK

    // address of StubDispatchFrame
    add         r0, sp, #__PWTB_TransitionBlock // pTransitionBlock
    mov         r1, r4  // siteAddrForRegisterIndirect
    mov         r2, #0  // sectionIndex
    mov         r3, #0  // pModule

    bl          C_FUNC(StubDispatchFixupWorker)

    // mov the address we patched to in R12 so that we can tail call to it
    mov         r12, r0

    EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
    PATCH_LABEL StubDispatchFixupPatchLabel
    bx   r12
 
    NESTED_END StubDispatchFixupStub, _TEXT
#endif // FEATURE_PREJIT

//------------------------------------------------
// JIT_RareDisableHelper
//
// The JIT expects this helper to preserve registers used for return values
//
    NESTED_ENTRY JIT_RareDisableHelper, _TEXT, NoHandler

    PROLOG_PUSH "{r0-r1, r7,r8, r11, lr}" // save integer return value
    PROLOG_STACK_SAVE_OFFSET r7, #8
    PROLOG_VPUSH {d0-d3}                  // floating point return value

    CHECK_STACK_ALIGNMENT

    bl          C_FUNC(JIT_RareDisableHelperWorker)

    EPILOG_VPOP {d0-d3}
    EPILOG_POP "{r0-r1, r7,r8, r11, pc}"

    NESTED_END JIT_RareDisableHelper, _TEXT


#ifdef FEATURE_CORECLR
//
// JIT Static access helpers for single appdomain case
//

// ------------------------------------------------------------------
// void* JIT_GetSharedNonGCStaticBase(SIZE_T moduleDomainID, DWORD dwClassDomainID)

    LEAF_ENTRY JIT_GetSharedNonGCStaticBase_SingleAppDomain, _TEXT

    // If class is not initialized, bail to C++ helper
    add     r2, r0, #DomainLocalModule__m_pDataBlob
    ldrb    r2, [r2, r1]
    tst     r2, #1
    beq     LOCAL_LABEL(CallCppHelper1)

    bx      lr

LOCAL_LABEL(CallCppHelper1):
    // Tail call JIT_GetSharedNonGCStaticBase_Helper
    b     C_FUNC(JIT_GetSharedNonGCStaticBase_Helper)
    LEAF_END JIT_GetSharedNonGCStaticBase_SingleAppDomain, _TEXT


// ------------------------------------------------------------------
// void* JIT_GetSharedNonGCStaticBaseNoCtor(SIZE_T moduleDomainID, DWORD dwClassDomainID)

    LEAF_ENTRY JIT_GetSharedNonGCStaticBaseNoCtor_SingleAppDomain, _TEXT

    bx lr
    LEAF_END JIT_GetSharedNonGCStaticBaseNoCtor_SingleAppDomain, _TEXT


// ------------------------------------------------------------------
// void* JIT_GetSharedGCStaticBase(SIZE_T moduleDomainID, DWORD dwClassDomainID)

    LEAF_ENTRY JIT_GetSharedGCStaticBase_SingleAppDomain, _TEXT

    // If class is not initialized, bail to C++ helper
    add     r2, r0, #DomainLocalModule__m_pDataBlob
    ldrb    r2, [r2, r1]
    tst     r2, #1
    beq     LOCAL_LABEL(CallCppHelper3)

    ldr     r0, [r0, #DomainLocalModule__m_pGCStatics]
    bx lr

LOCAL_LABEL(CallCppHelper3):
    // Tail call Jit_GetSharedGCStaticBase_Helper
    b     C_FUNC(JIT_GetSharedGCStaticBase_Helper)
    LEAF_END JIT_GetSharedGCStaticBase_SingleAppDomain, _TEXT


// ------------------------------------------------------------------
// void* JIT_GetSharedGCStaticBaseNoCtor(SIZE_T moduleDomainID, DWORD dwClassDomainID)

    LEAF_ENTRY JIT_GetSharedGCStaticBaseNoCtor_SingleAppDomain, _TEXT

    ldr     r0, [r0, #DomainLocalModule__m_pGCStatics]
    bx lr
    LEAF_END JIT_GetSharedGCStaticBaseNoCtor_SingleAppDomain, _TEXT

#endif

// ------------------------------------------------------------------
// __declspec(naked) void F_CALL_CONV JIT_Stelem_Ref(PtrArray* array, unsigned idx, Object* val)
    LEAF_ENTRY JIT_Stelem_Ref, _TEXT
    
    // We retain arguments as they were passed and use r0 == array// r1 == idx// r2 == val

    // check for null array
    cbz     r0, LOCAL_LABEL(ThrowNullReferenceException)

    // idx bounds check
    ldr     r3,[r0,#ArrayBase__m_NumComponents]
    cmp     r3,r1
    bls     LOCAL_LABEL(ThrowIndexOutOfRangeException)

    // fast path to null assignment (doesn't need any write-barriers)
    cbz     r2, LOCAL_LABEL(AssigningNull)

    // Verify the array-type and val-type matches before writing
    ldr     r12, [r0] // r12 = array MT
    ldr     r3, [r2] // r3 = val->GetMethodTable()
    ldr     r12, [r12, #MethodTable__m_ElementType] // array->GetArrayElementTypeHandle()
    cmp     r3, r12
    beq     C_FUNC(JIT_Stelem_DoWrite)

    // Types didnt match but allow writing into an array of objects
    ldr     r3, =g_pObjectClass
    ldr     r3, [r3]  // r3 = *g_pObjectClass
    cmp     r3, r12   // array type matches with Object*
    beq     C_FUNC(JIT_Stelem_DoWrite)

    // array type and val type do not exactly match. Raise frame and do detailed match
    b       C_FUNC(JIT_Stelem_Ref_NotExactMatch)

LOCAL_LABEL(AssigningNull):
    // Assigning null doesn't need write barrier
    adds    r0, r1, LSL #2               // r0 = r0 + (r1 x 4) = array->m_array[idx]
    str     r2, [r0, #PtrArray__m_Array] // array->m_array[idx] = val
    bx      lr
   
LOCAL_LABEL(ThrowNullReferenceException):
    // Tail call JIT_InternalThrow(NullReferenceException)
    ldr     r0, =CORINFO_NullReferenceException_ASM
    b       C_FUNC(JIT_InternalThrow)

LOCAL_LABEL(ThrowIndexOutOfRangeException):
    // Tail call JIT_InternalThrow(NullReferenceException)
    ldr     r0, =CORINFO_IndexOutOfRangeException_ASM
    b       C_FUNC(JIT_InternalThrow)

    LEAF_END JIT_Stelem_Ref, _TEXT

// ------------------------------------------------------------------
// __declspec(naked) void F_CALL_CONV JIT_Stelem_Ref_NotExactMatch(PtrArray* array,
//                                                       unsigned idx, Object* val)
//   r12 = array->GetArrayElementTypeHandle()
//
    NESTED_ENTRY JIT_Stelem_Ref_NotExactMatch, _TEXT, NoHandler
    PROLOG_PUSH {lr}
    PROLOG_PUSH {r0-r2}

    CHECK_STACK_ALIGNMENT 

    // allow in case val can be casted to array element type
    // call ObjIsInstanceOfNoGC(val, array->GetArrayElementTypeHandle())
    mov     r1, r12 // array->GetArrayElementTypeHandle()
    mov     r0, r2
    bl      C_FUNC(ObjIsInstanceOfNoGC)
    cmp     r0, TypeHandle_CanCast 
    beq     LOCAL_LABEL(DoWrite)             // ObjIsInstance returned TypeHandle::CanCast

    // check via raising frame
LOCAL_LABEL(NeedFrame):
    mov     r1, sp                  // r1 = &array
    adds    r0, sp, #8              // r0 = &val
    bl      C_FUNC(ArrayStoreCheck) // ArrayStoreCheck(&val, &array)
 
LOCAL_LABEL(DoWrite):
    EPILOG_POP {r0-r2}
    EPILOG_POP {lr}
    b C_FUNC(JIT_Stelem_DoWrite)

    NESTED_END JIT_Stelem_Ref_NotExactMatch, _TEXT

// ------------------------------------------------------------------
// __declspec(naked) void F_CALL_CONV JIT_Stelem_DoWrite(PtrArray* array, unsigned idx, Object* val)
    LEAF_ENTRY  JIT_Stelem_DoWrite, _TEXT

    // Setup args for JIT_WriteBarrier. r0 = &array->m_array[idx]// r1 = val
    adds    r0, #PtrArray__m_Array     // r0 = &array->m_array
    adds    r0, r1, LSL #2
    mov     r1, r2                     // r1 = val

    // Branch to the write barrier (which is already correctly overwritten with
    // single or multi-proc code based on the current CPU
    b       C_FUNC(JIT_WriteBarrier)

    LEAF_END JIT_Stelem_DoWrite, _TEXT
    
#define __wbScratch r3
#define pShadow r7

    .macro START_WRITE_BARRIER name
        __\name\()__g_lowest_address_offset = 0xffff
        __\name\()__g_highest_address_offset = 0xffff
        __\name\()__g_ephemeral_low_offset = 0xffff
        __\name\()__g_ephemeral_high_offset = 0xffff
        __\name\()__g_card_table_offset = 0xffff
     .endm
    
    .macro LOAD_GC_GLOBAL name, regName, globalName
\name\()__\globalName\()_offset:
    __\name\()__\globalName\()_offset = (\name\()__\globalName\()_offset - \name)
        movw \regName, #0
        movt \regName, #0
    .endm
    
    .macro UPDATE_GC_SHADOW name, ptrReg, valReg
        // Todo: implement, debugging helper
    .endm
    
    .macro UPDATE_CARD_TABLE name, ptrReg, valReg, mp, postGrow, tmpReg
            
        LOAD_GC_GLOBAL \name, __wbScratch, g_ephemeral_low
        cmp \valReg, __wbScratch
        blo 0f
        
        .if(\postGrow)
            LOAD_GC_GLOBAL \name, __wbScratch, g_ephemeral_high
            cmp \valReg, __wbScratch
            bhs 0f
        .endif
        
        LOAD_GC_GLOBAL \name, __wbScratch, g_card_table
        add __wbScratch, __wbScratch, \ptrReg, lsr #10
        
        .if(\mp)
            ldrb \tmpReg, [__wbScratch]
            cmp \tmpReg, #0xff
            itt ne
            movne \tmpReg, 0xff
            strbne \tmpReg, [__wbScratch]
        .else
            mov \tmpReg, #0xff
            strb \tmpReg, [__wbScratch]
        .endif
        
0:
    .endm
    
    .macro CHECK_GC_HEAP_RANGE name, ptrReg, label
        LOAD_GC_GLOBAL \name, __wbScratch, g_lowest_address
        cmp \ptrReg, __wbScratch
        blo \label
        LOAD_GC_GLOBAL \name, __wbScratch, g_highest_address
        cmp \ptrReg, __wbScratch
        bhs \label
    .endm
    
    .macro JIT_WRITEBARRIER name, mp, post
    LEAF_ENTRY \name, _TEXT
        START_WRITE_BARRIER \name
        .if(\mp)
            dmb
        .endif
        
        str r1, [r0]
        UPDATE_GC_SHADOW \name, r0, r1
        UPDATE_CARD_TABLE \name, r0, r1, \mp, \post, r0
        bx lr
    LEAF_END_MARKED \name, _TEXT
    .endm
    
    .macro JIT_CHECKEDWRITEBARRIER_SP name, post
    LEAF_ENTRY \name, _TEXT
        START_WRITE_BARRIER \name
        str r1, [r0]
        CHECK_GC_HEAP_RANGE \name, r0, 1f
        UPDATE_GC_SHADOW \name, r0, r1
        UPDATE_CARD_TABLE \name, r0, r1, 0, \post, r0
1:
        bx lr
    LEAF_END_MARKED \name, _TEXT
    .endm
    
    .macro JIT_CHECKEDWRITEBARRIER_MP name, post
    LEAF_ENTRY \name, _TEXT
        START_WRITE_BARRIER \name
        dmb
        str r1, [r0]
        CHECK_GC_HEAP_RANGE \name, r0, 1f
        UPDATE_GC_SHADOW \name, r0, r1
        UPDATE_CARD_TABLE \name, r0, r1, 1, \post, r0
        bx lr
1:
        str r1, [r0]
        bx lr
    LEAF_END_MARKED \name, _TEXT
    .endm
    
    .macro JIT_BYREFWRITEBARRIER name, mp, post
    LEAF_ENTRY \name, _TEXT
        START_WRITE_BARRIER \name
        .if(\mp)
            dmb
        .endif
        
        ldr r2, [r1]
        str r2, [r0]
        CHECK_GC_HEAP_RANGE \name, r0, 1f
        UPDATE_GC_SHADOW \name, r0, r2
        UPDATE_CARD_TABLE \name, r0, r2, \mp, \post, r2
1:
        add r0, #4
        add r1, #4
        bx lr
    LEAF_END_MARKED \name, _TEXT
    .endm
    
    .macro JIT_WRITEBARRIER_DESCRIPTOR name
        .word \name
        .word \name\()_End
        .word __\name\()__g_lowest_address_offset
        .word __\name\()__g_highest_address_offset
        .word __\name\()__g_ephemeral_low_offset
        .word __\name\()__g_ephemeral_high_offset
        .word __\name\()__g_card_table_offset
    .endm

    // There 4 versions of each write barriers. A 2x2 combination of multi-proc/single-proc and pre/post grow version
    JIT_WRITEBARRIER JIT_WriteBarrier_SP_Pre,  0, 0
    JIT_WRITEBARRIER JIT_WriteBarrier_SP_Post, 0, 1
    JIT_WRITEBARRIER JIT_WriteBarrier_MP_Pre,  1, 0
    JIT_WRITEBARRIER JIT_WriteBarrier_MP_Post, 1, 1
    
    JIT_CHECKEDWRITEBARRIER_SP JIT_CheckedWriteBarrier_SP_Pre,  0
    JIT_CHECKEDWRITEBARRIER_SP JIT_CheckedWriteBarrier_SP_Post, 1
    JIT_CHECKEDWRITEBARRIER_MP JIT_CheckedWriteBarrier_MP_Pre,  0
    JIT_CHECKEDWRITEBARRIER_MP JIT_CheckedWriteBarrier_MP_Post, 1

    JIT_BYREFWRITEBARRIER JIT_ByRefWriteBarrier_SP_Pre,  0, 0
    JIT_BYREFWRITEBARRIER JIT_ByRefWriteBarrier_SP_Post, 0, 1
    JIT_BYREFWRITEBARRIER JIT_ByRefWriteBarrier_MP_Pre,  1, 0
    JIT_BYREFWRITEBARRIER JIT_ByRefWriteBarrier_MP_Post, 1, 1
    
//    .section .clrwb, "d"
g_rgWriteBarrierDescriptors:

    JIT_WRITEBARRIER_DESCRIPTOR JIT_WriteBarrier_SP_Pre
    JIT_WRITEBARRIER_DESCRIPTOR JIT_WriteBarrier_SP_Post
    JIT_WRITEBARRIER_DESCRIPTOR JIT_WriteBarrier_MP_Pre
    JIT_WRITEBARRIER_DESCRIPTOR JIT_WriteBarrier_MP_Post
    
    JIT_WRITEBARRIER_DESCRIPTOR JIT_CheckedWriteBarrier_SP_Pre
    JIT_WRITEBARRIER_DESCRIPTOR JIT_CheckedWriteBarrier_SP_Post
    JIT_WRITEBARRIER_DESCRIPTOR JIT_CheckedWriteBarrier_MP_Pre
    JIT_WRITEBARRIER_DESCRIPTOR JIT_CheckedWriteBarrier_MP_Post
    
    JIT_WRITEBARRIER_DESCRIPTOR JIT_ByRefWriteBarrier_SP_Pre
    JIT_WRITEBARRIER_DESCRIPTOR JIT_ByRefWriteBarrier_SP_Post
    JIT_WRITEBARRIER_DESCRIPTOR JIT_ByRefWriteBarrier_MP_Pre
    JIT_WRITEBARRIER_DESCRIPTOR JIT_ByRefWriteBarrier_MP_Post
    
    // Sentinel value
    .word 0
    
//    .text
    
    .global g_rgWriteBarrierDescriptors

#ifdef FEATURE_READYTORUN

    NESTED_ENTRY DelayLoad_MethodCall_FakeProlog, _TEXT, NoHandler

    // Match what the lazy thunk has pushed. The actual method arguments will be spilled later.
    PROLOG_PUSH  {r1-r3}

        // This is where execution really starts.
DelayLoad_MethodCall:
    .global DelayLoad_MethodCall

    PROLOG_PUSH  {r0}

    PROLOG_WITH_TRANSITION_BLOCK 0x0, 1, DoNotPushArgRegs

    // Load the helper arguments
    ldr         r5, [sp,#(__PWTB_TransitionBlock+10*4)] // pModule
    ldr         r6, [sp,#(__PWTB_TransitionBlock+11*4)] // sectionIndex
    ldr         r8, [sp,#(__PWTB_TransitionBlock+12*4)] // indirection

    // Spill the actual method arguments
    str         r1, [sp,#(__PWTB_TransitionBlock+10*4)]
    str         r2, [sp,#(__PWTB_TransitionBlock+11*4)]
    str         r3, [sp,#(__PWTB_TransitionBlock+12*4)]

    add         r0, sp, #__PWTB_TransitionBlock // pTransitionBlock

    mov         r1, r8          // pIndirection
    mov         r2, r6          // sectionIndex
    mov         r3, r5          // pModule

    bl          C_FUNC(ExternalMethodFixupWorker)

    // mov the address we patched to in R12 so that we can tail call to it
    mov         r12, r0

    EPILOG_WITH_TRANSITION_BLOCK_TAILCALL

    // Share the patch label
    b C_FUNC(ExternalMethodFixupPatchLabel)

    NESTED_END DelayLoad_MethodCall_FakeProlog, _TEXT


    .macro DynamicHelper frameFlags, suffix

        NESTED_ENTRY DelayLoad_Helper\suffix\()_FakeProlog, _TEXT, NoHandler

        // Match what the lazy thunk has pushed. The actual method arguments will be spilled later.
        PROLOG_PUSH  {r1-r3}

        // This is where execution really starts.
DelayLoad_Helper\suffix:
        .global DelayLoad_Helper\suffix

        PROLOG_PUSH  {r0}

        PROLOG_WITH_TRANSITION_BLOCK 0x4, 0, DoNotPushArgRegs

        // Load the helper arguments
        ldr         r5, [sp,#(__PWTB_TransitionBlock+10*4)] // pModule
        ldr         r6, [sp,#(__PWTB_TransitionBlock+11*4)] // sectionIndex
        ldr         r8, [sp,#(__PWTB_TransitionBlock+12*4)] // indirection

        // Spill the actual method arguments
        str         r1, [sp,#(__PWTB_TransitionBlock+10*4)]
        str         r2, [sp,#(__PWTB_TransitionBlock+11*4)]
        str         r3, [sp,#(__PWTB_TransitionBlock+12*4)]

        add         r0, sp, #__PWTB_TransitionBlock // pTransitionBlock

        mov         r1, r8          // pIndirection
        mov         r2, r6          // sectionIndex
        mov         r3, r5          // pModule

        mov         r4, \frameFlags
        str         r4, [sp,#0]

        bl          C_FUNC(DynamicHelperWorker)

        cbnz        r0, 0f
        ldr         r0, [sp,#(__PWTB_TransitionBlock+9*4)]  // The result is stored in the argument area of the transition block

        EPILOG_WITH_TRANSITION_BLOCK_RETURN

0:
        mov         r12, r0
        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
        bx   r12

        NESTED_END DelayLoad_Helper\suffix\()_FakeProlog, _TEXT

    .endm

    DynamicHelper DynamicHelperFrameFlags_Default
    DynamicHelper DynamicHelperFrameFlags_ObjectArg, _Obj
    DynamicHelper DynamicHelperFrameFlags_ObjectArg | DynamicHelperFrameFlags_ObjectArg2, _ObjObj

#endif // FEATURE_READYTORUN

#ifdef FEATURE_HIJACK

// ------------------------------------------------------------------
// Hijack function for functions which return a value type
        NESTED_ENTRY OnHijackTripThread, _TEXT, NoHandler
        PROLOG_PUSH "{r0,r4-r11,lr}"

        PROLOG_VPUSH "{d0-d3}"    // saving as d0-d3 can have the floating point return value
        PROLOG_PUSH "{r1}"        // saving as r1 can have partial return value when return is > 32 bits
        alloc_stack 4             // 8 byte align

        CHECK_STACK_ALIGNMENT

        add r0, sp, #40
        bl C_FUNC(OnHijackWorker)

        free_stack 4
        EPILOG_POP "{r1}"
        EPILOG_VPOP "{d0-d3}"

        EPILOG_POP "{r0,r4-r11,pc}"
        NESTED_END OnHijackTripThread, _TEXT
#endif

