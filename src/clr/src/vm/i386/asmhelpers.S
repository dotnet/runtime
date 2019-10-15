// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.intel_syntax noprefix
#include "unixasmmacros.inc"
#include "asmconstants.h"

//
// FramedMethodFrame prolog
//
.macro STUB_PROLOG
    // push ebp-frame
    PROLOG_BEG

    // save CalleeSavedRegisters
    PROLOG_PUSH ebx
    PROLOG_PUSH esi
    PROLOG_PUSH edi

    // push ArgumentRegisters
    PROLOG_PUSH ecx
    PROLOG_PUSH edx

    // set frame pointer
    PROLOG_END
.endm

//
// FramedMethodFrame epilog
//
.macro STUB_EPILOG
    // restore stack pointer
    EPILOG_BEG

    // pop ArgumentRegisters
    EPILOG_POP edx
    EPILOG_POP ecx

    // pop CalleeSavedRegisters
    EPILOG_POP edi
    EPILOG_POP esi
    EPILOG_POP ebx

    // pop ebp-frame
    EPILOG_END
.endm

//
// FramedMethodFrame epilog
//
.macro STUB_EPILOG_RETURN
    // pop ArgumentRegisters
    add esp, 8

    // pop CalleeSavedRegisters
    pop edi
    pop esi
    pop ebx
    pop ebp
.endm

.macro STUB_PROLOG_2_HIDDEN_ARGS
    //
    // The stub arguments are where we want to setup the TransitionBlock. We will
    // setup the TransitionBlock later once we can trash them
    //
    // push ebp-frame
    // push      ebp
    // mov       ebp,esp

    // save CalleeSavedRegisters
    // push      ebx

    push        esi
    push        edi

    // push ArgumentRegisters
    push        ecx
    push        edx

    mov         ecx, [esp + 4*4]
    mov         edx, [esp + 5*4]

    // Setup up proper EBP frame now that the stub arguments can be trashed
    mov         [esp + 4*4], ebx
    mov         [esp + 5*4], ebp
    lea         ebp, [esp + 5*4]
.endm

LEAF_ENTRY ResetCurrentContext, _TEXT
    push eax

    // clear the direction flag (used for rep instructions)
    cld

    // load flags into AX
    fnstcw  [esp - 2]
    mov     ax, [esp - 2]

    fninit                // reset FPU
    and     ax, 0f00h     // preserve precision and rounding control
    or      ax, 007fh     // mask all exceptions

    // preserve precision control
    mov     ax, [esp - 2]
    fldcw   [esp - 2]

    pop eax
    ret
LEAF_END ResetCurrentContext, _TEXT

// Incoming:
//    ESP+4: Pointer to buffer to which FPU state should be saved
LEAF_ENTRY CaptureFPUContext, _TEXT
    mov     ecx, [esp + 4]
    fnstenv [ecx]
    ret     4

LEAF_END CaptureFPUContext, _TEXT

// Incoming:
//  ESP+4: Pointer to buffer from which FPU state should be restored
LEAF_ENTRY RestoreFPUContext, _TEXT
    mov     ecx, [esp + 4]
    fldenv  [ecx]
    ret     4
LEAF_END RestoreFPUContext, _TEXT


LEAF_ENTRY GetSpecificCpuTypeAsm, _TEXT
    push    ebx         // ebx is trashed by the cpuid calls

    // See if the chip supports CPUID
    pushfd
    pop     ecx           // Get the EFLAGS
    mov     eax, ecx      // Save for later testing
    xor     ecx, 200000h  // Invert the ID bit
    push    ecx
    popfd                 // Save the updated flags
    pushfd
    pop     ecx           // Retrieve the updated flags
    xor     ecx, eax      // Test if it actually changed (bit set means yes)
    push    eax
    popfd                 // Restore the flags

    test    ecx, 200000h
    jz      LOCAL_LABEL(Assume486)

    xor     eax, eax
    cpuid

    test    eax, eax
    jz      LOCAL_LABEL(Assume486)     // brif CPUID1 not allowed

    mov     eax, 1
    cpuid

    // filter out everything except family and model
    // Note that some multi-procs have different stepping number for each proc
    and     eax, 0ff0h

    jmp     LOCAL_LABEL(CpuTypeDone)

LOCAL_LABEL(Assume486):
    mov     eax, 0400h   // report 486

LOCAL_LABEL(CpuTypeDone):
    pop     ebx
    ret
LEAF_END GetSpecificCpuTypeAsm, _TEXT

// DWORD __stdcall GetSpecificCpuFeaturesAsm(DWORD *pInfo);
LEAF_ENTRY GetSpecificCpuFeaturesAsm, _TEXT
    push    ebx         // ebx is trashed by the cpuid calls

    // See if the chip supports CPUID
    pushfd
    pop     ecx          // Get the EFLAGS
    mov     eax, ecx     // Save for later testing
    xor     ecx, 200000h // Invert the ID bit.
    push    ecx
    popfd                // Save the updated flags.
    pushfd
    pop     ecx          // Retrieve the updated flags
    xor     ecx, eax     // Test if it actually changed (bit set means yes)
    push    eax
    popfd                // Restore the flags

    test    ecx, 200000h
    jz      LOCAL_LABEL(CpuFeaturesFail)

    xor     eax, eax
    cpuid

    test    eax, eax
    jz      LOCAL_LABEL(CpuFeaturesDone) // br if CPUID1 not allowed

    mov     eax, 1
    cpuid
    mov     eax, edx        // return all feature flags
    mov     edx, [esp + 8]
    test    edx, edx
    jz      LOCAL_LABEL(CpuFeaturesDone)
    mov     [edx],ebx       // return additional useful information
    jmp     LOCAL_LABEL(CpuFeaturesDone)

LOCAL_LABEL(CpuFeaturesFail):
    xor     eax, eax    // Nothing to report

LOCAL_LABEL(CpuFeaturesDone):
    pop     ebx
    ret     4
LEAF_END GetSpecificCpuFeaturesAsm, _TEXT


// -----------------------------------------------------------------------
//  The out-of-line portion of the code to enable preemptive GC.
//  After the work is done, the code jumps back to the "pRejoinPoint"
//  which should be emitted right after the inline part is generated.
//
//  Assumptions:
//       ebx = Thread
//  Preserves
//       all registers except ecx.
//
// -----------------------------------------------------------------------
NESTED_ENTRY StubRareEnable, _TEXT, NoHandler
    push    eax
    push    edx

    push    ebx

    CHECK_STACK_ALIGNMENT
    call    C_FUNC(StubRareEnableWorker)

    pop     edx
    pop     eax
    ret
NESTED_END StubRareEnable, _TEXT

NESTED_ENTRY StubRareDisableTHROW, _TEXT, NoHandler
    push    eax
    push    edx

    push    ebx     // Thread

    CHECK_STACK_ALIGNMENT
    call    C_FUNC(StubRareDisableTHROWWorker)

    pop     edx
    pop     eax
    ret
NESTED_END StubRareDisableTHROW, _TEXT

// ------------------------------------------------------------------------------
//  This helper routine enregisters the appropriate arguments and makes the
//  actual call.
// ------------------------------------------------------------------------------
//  void STDCALL CallDescrWorkerInternal(CallDescrWorkerParams *  pParams)
NESTED_ENTRY CallDescrWorkerInternal, _TEXT, NoHandler
    PROLOG_BEG
    PROLOG_PUSH ebx
    PROLOG_END

    mov     ebx, [esp + ((2 + 1) * 4)]

    // compute padding size
    mov     eax, esp
    mov     ecx, [ebx + CallDescrData__numStackSlots]
    shl     ecx, 2
    sub     eax, ecx
    and     eax, 15
    // adjust stack offset
    sub     esp, eax

    // copy the stack
    mov     ecx, [ebx +CallDescrData__numStackSlots]
    mov     eax, [ebx +CallDescrData__pSrc]
    test    ecx, ecx
    jz      LOCAL_LABEL(donestack)
    lea     eax, [eax + 4*ecx - 4] // last argument
    push    DWORD PTR [eax]
    dec     ecx
    jz      LOCAL_LABEL(donestack)
    sub     eax, 4
    push    DWORD PTR [eax]
    dec     ecx
    jz      LOCAL_LABEL(donestack)

LOCAL_LABEL(stackloop):
    sub     eax, 4
    push    DWORD PTR [eax]
    dec     ecx
    jnz     LOCAL_LABEL(stackloop)

LOCAL_LABEL(donestack):
    // now we must push each field of the ArgumentRegister structure
    mov     eax, [ebx + CallDescrData__pArgumentRegisters]
    mov     edx, DWORD PTR [eax]
    mov     ecx, DWORD PTR [eax + 4]

    CHECK_STACK_ALIGNMENT
    call    [ebx + CallDescrData__pTarget]
#ifdef _DEBUG
    nop     // This is a tag that we use in an assert.  Fcalls expect to
            // be called from Jitted code or from certain blessed call sites like
            // this one.  (See HelperMethodFrame::InsureInit)
#endif

    // Save FP return value if necessary
    mov     ecx, [ebx + CallDescrData__fpReturnSize]
    cmp     ecx, 0
    je      LOCAL_LABEL(ReturnsInt)

    cmp     ecx, 4
    je      LOCAL_LABEL(ReturnsFloat)
    cmp     ecx, 8
    je      LOCAL_LABEL(ReturnsDouble)
    // unexpected
    jmp     LOCAL_LABEL(Epilog)

LOCAL_LABEL(ReturnsInt):
    mov     [ebx + CallDescrData__returnValue], eax
    mov     [ebx + CallDescrData__returnValue + 4], edx

LOCAL_LABEL(Epilog):
    // restore the stake pointer
    lea     esp, [ebp - 4]

    EPILOG_BEG
    EPILOG_POP ebx
    EPILOG_END
    ret     4

LOCAL_LABEL(ReturnsFloat):
    fstp    DWORD PTR [ebx + CallDescrData__returnValue]  // Spill the Float return value
    jmp     LOCAL_LABEL(Epilog)

LOCAL_LABEL(ReturnsDouble):
    fstp    QWORD PTR [ebx + CallDescrData__returnValue]  // Spill the Double return value
    jmp     LOCAL_LABEL(Epilog)
NESTED_END CallDescrWorkerInternal, _TEXT

#ifdef _DEBUG
// int __fastcall HelperMethodFrameRestoreState(HelperMethodFrame*, struct MachState *)
LEAF_ENTRY HelperMethodFrameRestoreState, _TEXT
    mov         eax, edx        // eax = MachState*
#else // _DEBUG
// int __fastcall HelperMethodFrameRestoreState(struct MachState *)
LEAF_ENTRY HelperMethodFrameRestoreState, _TEXT
    mov         eax, ecx        // eax = MachState*
#endif // _DEBUG
    // restore the registers from the m_MachState stucture.  Note that
    // we only do this for register that where not saved on the stack
    // at the time the machine state snapshot was taken.

    cmp         dword ptr [eax+MachState__pRetAddr], 0

#ifdef _DEBUG
    jnz         LOCAL_LABEL(noConfirm)

    mov         eax, ebp

    // Create a minimal EBP-frame (for clean stack trace under debugger)
    PROLOG_BEG
    PROLOG_END

    // Call HelperMethodFrameConfirmState (with stack alignment padding)
    #define STACK_ALIGN_PADDING 4
    sub         esp, STACK_ALIGN_PADDING
    push        eax     // eax = ebp
    push        ebx
    push        edi
    push        esi
    push        ecx     // HelperFrame*
    CHECK_STACK_ALIGNMENT
    call        C_FUNC(HelperMethodFrameConfirmState)
    add         esp, STACK_ALIGN_PADDING
    #undef STACK_ALIGN_PADDING

    // Restore EBP
    EPILOG_BEG
    EPILOG_END

    // on return, eax = MachState*
    cmp         DWORD PTR [eax + MachState__pRetAddr], 0
LOCAL_LABEL(noConfirm):
#endif // _DEBUG

    jz          LOCAL_LABEL(doRet)

    lea         edx, [eax + MachState__esi]     // Did we have to spill ESI
    cmp         [eax + MachState__pEsi], edx
    jnz         LOCAL_LABEL(SkipESI)
    mov         esi, [edx]                      // Then restore it

LOCAL_LABEL(SkipESI):
    lea         edx, [eax + MachState__edi]     // Did we have to spill EDI
    cmp         [eax + MachState__pEdi], edx
    jnz         LOCAL_LABEL(SkipEDI)
    mov         edi, [edx]                      // Then restore it

LOCAL_LABEL(SkipEDI):
    lea         edx, [eax + MachState__ebx]     // Did we have to spill EBX
    cmp         [eax + MachState__pEbx], edx
    jnz         LOCAL_LABEL(SkipEBX)
    mov         ebx, [edx]                      // Then restore it

LOCAL_LABEL(SkipEBX):
    lea         edx, [eax + MachState__ebp]     // Did we have to spill EBP
    cmp         [eax + MachState__pEbp], edx
    jnz         LOCAL_LABEL(SkipEBP)
    mov         ebp, [edx]                      // Then restore it

LOCAL_LABEL(SkipEBP):
LOCAL_LABEL(doRet):
    xor         eax, eax
    ret
LEAF_END HelperMethodFrameRestoreState, _TEXT

#ifdef FEATURE_HIJACK

// A JITted method's return address was hijacked to return to us here.
// VOID OnHijackTripThread()
NESTED_ENTRY OnHijackTripThread, _TEXT, NoHandler
    // Don't fiddle with this unless you change HijackFrame::UpdateRegDisplay
    // and HijackArgs
    push    eax         // make room for the real return address (Eip)
    push    ebp
    push    eax
    push    ecx
    push    edx
    push    ebx
    push    esi
    push    edi

    // unused space for floating point state
    sub     esp,12

    push    esp
    CHECK_STACK_ALIGNMENT
    call    C_FUNC(OnHijackWorker)

    // unused space for floating point state
    add     esp,12

    pop     edi
    pop     esi
    pop     ebx
    pop     edx
    pop     ecx
    pop     eax
    pop     ebp
    ret                 // return to the correct place, adjusted by our caller
NESTED_END OnHijackTripThread, _TEXT

// VOID OnHijackFPTripThread()
NESTED_ENTRY OnHijackFPTripThread, _TEXT, NoHandler
    // Don't fiddle with this unless you change HijackFrame::UpdateRegDisplay
    // and HijackArgs
    push    eax         // make room for the real return address (Eip)
    push    ebp
    push    eax
    push    ecx
    push    edx
    push    ebx
    push    esi
    push    edi

    sub     esp,12

    // save top of the floating point stack (there is return value passed in it)
    // save full 10 bytes to avoid precision loss
    fstp    QWORD PTR [esp]

    push    esp
    CHECK_STACK_ALIGNMENT
    call    C_FUNC(OnHijackWorker)

    // restore top of the floating point stack
    fld     QWORD PTR [esp]

    add     esp,12

    pop     edi
    pop     esi
    pop     ebx
    pop     edx
    pop     ecx
    pop     eax
    pop     ebp
    ret                 // return to the correct place, adjusted by our caller
NESTED_END OnHijackFPTripThread, _TEXT

#endif // FEATURE_HIJACK

// ==========================================================================
//  This function is reached only via the embedded ImportThunkGlue code inside
//  an NDirectMethodDesc. It's purpose is to load the DLL associated with an
//  N/Direct method, then backpatch the DLL target into the methoddesc.
//
//  Initial state:
//
//       Preemptive GC is *enabled*: we are actually in an unmanaged state.
//
//
//       [esp+...]   - The *unmanaged* parameters to the DLL target.
//       [esp+4]     - Return address back into the JIT'ted code that made
//                     the DLL call.
//       [esp]       - Contains the "return address." Because we got here
//                     thru a call embedded inside a MD, this "return address"
//                     gives us an easy to way to find the MD (which was the
//                     whole purpose of the embedded call manuever.)
//
//
//
// ==========================================================================
LEAF_ENTRY NDirectImportThunk, _TEXT
    // Preserve argument registers
    push    ecx
    push    edx

    // Invoke the function that does the real work.
    push    eax
    call    C_FUNC(NDirectImportWorker)

    // Restore argument registers
    pop     edx
    pop     ecx

    // If we got back from NDirectImportWorker, the MD has been successfully
    // linked and "eax" contains the DLL target. Proceed to execute the
    // original DLL call.
    jmp     eax     // Jump to DLL target
LEAF_END NDirectImportThunk, _TEXT

// ==========================================================================
// The call in fixup precode initally points to this function.
// The pupose of this function is to load the MethodDesc and forward the call the prestub.
LEAF_ENTRY PrecodeFixupThunk, _TEXT
    // Pop the return address. It points right after the call instruction in the precode.
    pop     eax
    push    esi
    push    edi

    // Inline computation done by FixupPrecode::GetMethodDesc()
    movzx   esi, BYTE PTR [eax + 2]    // m_PrecodeChunkIndex
    movzx   edi, BYTE PTR [eax + 1]    // m_MethodDescChunkIndex
    mov     eax, DWORD PTR [eax + esi*8 +3]
    lea     eax, [eax + edi*4]

    pop     edi
    pop     esi
    jmp     C_FUNC(ThePreStub)
LEAF_END PrecodeFixupThunk, _TEXT

NESTED_ENTRY UMThunkStubRareDisable, _TEXT, NoHandler
    push    eax
    push    ecx

    sub     esp, 12
    push    eax          // Push the UMEntryThunk
    push    ecx          // Push thread
    CHECK_STACK_ALIGNMENT
    call    C_FUNC(UMThunkStubRareDisableWorker)
    add     esp, 12

    pop     ecx
    pop     eax
    ret
NESTED_END UMThunkStubRareDisable, _TEXT

//
// Used to get the current instruction pointer value
//
// UINT_PTR __stdcall GetCurrentIP(void);
LEAF_ENTRY GetCurrentIP, _TEXT
    mov     eax, [esp]
    ret
LEAF_END GetCurrentIP, _TEXT

// LPVOID __stdcall GetCurrentSP(void);
LEAF_ENTRY GetCurrentSP, _TEXT
    mov     eax, esp
    add     eax, 4
    ret
LEAF_END GetCurrentSP, _TEXT

// ==========================================================================
// Invoked for vararg forward P/Invoke calls as a stub.
// Except for secret return buffer, arguments come on the stack so EDX is available as scratch.
// EAX       - the NDirectMethodDesc
// ECX       - may be return buffer address
// [ESP + 4] - the VASigCookie
//
NESTED_ENTRY VarargPInvokeStub, _TEXT, NoHandler
    // EDX <- VASigCookie
    mov     edx, [esp + 4]           // skip retaddr

    mov     edx, [edx + VASigCookie__StubOffset]
    test    edx, edx

    jz      LOCAL_LABEL(GoCallVarargWorker)
    // ---------------------------------------

    // EAX contains MD ptr for the IL stub
    jmp     edx

LOCAL_LABEL(GoCallVarargWorker):
    //
    // MD ptr in EAX, VASigCookie ptr at [esp+4]
    //
    STUB_PROLOG

    mov         esi, esp

    // save pMD
    push        eax

    push        eax                     // pMD
    push        dword ptr [esi + 4*7]   // pVaSigCookie
    push        esi                     // pTransitionBlock

    call        C_FUNC(VarargPInvokeStubWorker)

    // restore pMD
    pop     eax

    STUB_EPILOG

    // jump back to the helper - this time it won't come back here as the stub already exists
    jmp C_FUNC(VarargPInvokeStub)
NESTED_END VarargPInvokeStub, _TEXT

// ==========================================================================
// Invoked for marshaling-required unmanaged CALLI calls as a stub.
// EAX       - the unmanaged target
// ECX, EDX  - arguments
// [ESP + 4] - the VASigCookie
//
LEAF_ENTRY GenericPInvokeCalliHelper, _TEXT
    // save the target
    push    eax

    // EAX <- VASigCookie
    mov     eax, [esp + 8]           // skip target and retaddr

    mov     eax, [eax + VASigCookie__StubOffset]
    test    eax, eax

    jz      LOCAL_LABEL(GoCallCalliWorker)
    // ---------------------------------------

    push    eax

    // stack layout at this point:
    //
    // |         ...          |
    // |   stack arguments    | ESP + 16
    // +----------------------+
    // |     VASigCookie*     | ESP + 12
    // +----------------------+
    // |    return address    | ESP + 8
    // +----------------------+
    // | CALLI target address | ESP + 4
    // +----------------------+
    // |   stub entry point   | ESP + 0
    // ------------------------

    // remove VASigCookie from the stack
    mov     eax, [esp + 8]
    mov     [esp + 12], eax

    // move stub entry point below the RA
    mov     eax, [esp]
    mov     [esp + 8], eax

    // load EAX with the target address
    pop     eax
    pop     eax

    // stack layout at this point:
    //
    // |         ...          |
    // |   stack arguments    | ESP + 8
    // +----------------------+
    // |    return address    | ESP + 4
    // +----------------------+
    // |   stub entry point   | ESP + 0
    // ------------------------

    // CALLI target address is in EAX
    ret

LOCAL_LABEL(GoCallCalliWorker):
    // the target is on the stack and will become m_Datum of PInvokeCalliFrame
    // call the stub generating worker
    pop     eax

    //
    // target ptr in EAX, VASigCookie ptr in EDX
    //

    STUB_PROLOG

    mov         esi, esp

    // save target
    push        eax

    push        eax                         // unmanaged target
    push        dword ptr [esi + 4*7]       // pVaSigCookie (first stack argument)
    push        esi                         // pTransitionBlock

    call        C_FUNC(GenericPInvokeCalliStubWorker)

    // restore target
    pop     eax

    STUB_EPILOG

    // jump back to the helper - this time it won't come back here as the stub already exists
    jmp C_FUNC(GenericPInvokeCalliHelper)
LEAF_END GenericPInvokeCalliHelper, _TEXT

#ifdef FEATURE_PREJIT
// =========================================================================
NESTED_ENTRY StubDispatchFixupStub, _TEXT, NoHandler
    STUB_PROLOG

    mov         esi, esp

.att_syntax
    pushl       $0
    pushl       $0
    pushl       $0
.intel_syntax noprefix

    push        eax             // siteAddrForRegisterIndirect (for tailcalls)
    push        esi             // pTransitionBlock

    CHECK_STACK_ALIGNMENT
    call        C_FUNC(StubDispatchFixupWorker)

    mov         esp, esi
    STUB_EPILOG

PATCH_LABEL StubDispatchFixupPatchLabel
    // Tailcall target
    jmp eax

    // This will never be executed. It is just to help out stack-walking logic
    // which disassembles the epilog to unwind the stack.
    ret
NESTED_END StubDispatchFixupStub, _TEXT
#endif // FEATURE_PREJIT

// ==========================================================================
NESTED_ENTRY ExternalMethodFixupStub, _TEXT_ NoHandler
    // pop off the return address to the stub
    // leaving the actual caller's return address on top of the stack
    pop     eax

    STUB_PROLOG

    mov         esi, esp

    // EAX is return address into CORCOMPILE_EXTERNAL_METHOD_THUNK. Subtract 5 to get start address.
    sub         eax, 5

.att_syntax
    pushl       $0
    pushl       $0
.intel_syntax noprefix

    push        eax

    // pTransitionBlock
    push        esi

    call        C_FUNC(ExternalMethodFixupWorker)

    // eax now contains replacement stub. PreStubWorker will never return
    // NULL (it throws an exception if stub creation fails.)

    // From here on, mustn't trash eax

    STUB_EPILOG

PATCH_LABEL ExternalMethodFixupPatchLabel
    // Tailcall target
    jmp eax

    // This will never be executed. It is just to help out stack-walking logic
    // which disassembles the epilog to unwind the stack.
    ret
NESTED_END ExternalMethodFixupStub, _TEXT

#ifdef FEATURE_READYTORUN
NESTED_ENTRY DynamicHelperArgsStub, _TEXT, NoHandler
    .cfi_def_cfa_offset 16
    CHECK_STACK_ALIGNMENT
    call  eax
    add   esp, 12
    ret
NESTED_END DynamicHelperArgsStub, _TEXT

// ==========================================================================
NESTED_ENTRY DelayLoad_MethodCall, _TEXT, NoHandler
    STUB_PROLOG_2_HIDDEN_ARGS

    mov         esi, esp

    #define STACK_ALIGN_PADDING 4
    sub         esp, STACK_ALIGN_PADDING

    push        ecx
    push        edx
    push        eax
    push        esi // pTransitionBlock
    CHECK_STACK_ALIGNMENT
    call        C_FUNC(ExternalMethodFixupWorker)

    add         esp, STACK_ALIGN_PADDING
    #undef STACK_ALIGN_PADDING

    // eax now contains replacement stub. PreStubWorker will never return
    // NULL (it throws an exception if stub creation fails.)

    // From here on, mustn't trash eax

    STUB_EPILOG

    // Share the patch label
    jmp C_FUNC(ExternalMethodFixupPatchLabel)

    // This will never be executed. It is just to help out stack-walking logic
    // which disassembles the epilog to unwind the stack.
    ret
NESTED_END DelayLoad_MethodCall, _TEXT

#endif // FEATURE_READYTORUN

#ifdef FEATURE_PREJIT
// =======================================================================================
//  The call in softbound vtable slots initially points to this function.
//  The pupose of this function is to transfer the control to right target and
//  to optionally patch the target of the jump so that we do not take this slow path again.
//
NESTED_ENTRY VirtualMethodFixupStub, _TEXT, NoHandler
    // Get the return address. It points right after the call instruction in the thunk.
    mov     eax, [esp]
    // Calculate the address of the thunk
    sub     eax, 5

    // Push ebp frame to get good callstack under debugger
    PROLOG_BEG

    // Preserve argument registers
    PROLOG_PUSH ecx
    PROLOG_PUSH edx

    // Set frame pointer
    PROLOG_END

    sub     esp, 8
    push    eax         // address of the thunk
    push    ecx         // this ptr
    CHECK_STACK_ALIGNMENT
    call    C_FUNC(VirtualMethodFixupWorker)
    add     esp, 8

    // Restore stack pointer
    EPILOG_BEG

    // Restore argument registers
    EPILOG_POP edx
    EPILOG_POP ecx

    // Pop ebp frame
    EPILOG_END

    // Pop return address
    add   esp, 4

PATCH_LABEL VirtualMethodFixupPatchLabel
    // Proceed to execute the actual method.
    jmp     eax

    // This will never be executed. It is just to help out stack-walking logic
    // which disassembles the epilog to unwind the stack.
    ret
NESTED_END VirtualMethodFixupStub, _TEXT
#endif // FEATURE_PREJIT

NESTED_ENTRY ThePreStub, _TEXT, NoHandler
    STUB_PROLOG

    mov         esi, esp

    // Compute padding size
    lea         ebx, [esp - 8]
    and         ebx, 15
    // Adjust stack offset
    sub         esp, ebx

    // EAX contains MethodDesc* from the precode. Push it here as argument
    // for PreStubWorker
    push        eax

    push        esi

    CHECK_STACK_ALIGNMENT
    call        C_FUNC(PreStubWorker)

    // eax now contains replacement stub. PreStubWorker will never return
    // NULL (it throws an exception if stub creation fails.)

    // From here on, mustn't trash eax

    // Restore stack pointer
    mov         esp, esi

    STUB_EPILOG

    // Tailcall target
    jmp eax

    // This will never be executed. It is just to help out stack-walking logic
    // which disassembles the epilog to unwind the stack.
    ret
NESTED_END ThePreStub, _TEXT

// This method does nothing.  It's just a fixed function for the debugger to put a breakpoint
// on so that it can trace a call target.
LEAF_ENTRY ThePreStubPatch, _TEXT
    // make sure that the basic block is unique
    test eax,34

PATCH_LABEL ThePreStubPatchLabel
    ret
LEAF_END ThePreStubPatch, _TEXT

#ifdef FEATURE_READYTORUN
// ==========================================================================
//  Define helpers for delay loading of readytorun helpers

.macro DYNAMICHELPER frameFlags, suffix

NESTED_ENTRY DelayLoad_Helper\suffix, _TEXT, NoHandler
    STUB_PROLOG_2_HIDDEN_ARGS

    mov         esi, esp

.att_syntax
    pushl       $\frameFlags
.intel_syntax noprefix
    push        ecx             // module
    push        edx             // section index

    push        eax             // indirection cell address.
    push        esi             // pTransitionBlock

    CHECK_STACK_ALIGNMENT
    call        C_FUNC(DynamicHelperWorker)
    test        eax,eax
    jnz         LOCAL_LABEL(TailCallDelayLoad_Helper\suffix)

    mov         eax, [esi]      // The result is stored in the argument area of the transition block
    STUB_EPILOG_RETURN
    ret

LOCAL_LABEL(TailCallDelayLoad_Helper\suffix):
    STUB_EPILOG
    jmp eax
NESTED_END DelayLoad_Helper\suffix, _TEXT
.endm

DYNAMICHELPER DynamicHelperFrameFlags_Default
DYNAMICHELPER DynamicHelperFrameFlags_ObjectArg, _Obj
DYNAMICHELPER (DynamicHelperFrameFlags_ObjectArg | DynamicHelperFrameFlags_ObjectArg2), _ObjObj

#endif // FEATURE_READYTORUN

//
// Entry stack:
//    dispatch token
//    siteAddrForRegisterIndirect (used only if this is a RegisterIndirect dispatch call)
//    return address of caller to stub
//
// Please see vm/i386/virtualcallstubcpu.hpp for details
//
NESTED_ENTRY ResolveWorkerAsmStub, _TEXT, NoHandler
    //
    // The stub arguments are where we want to setup the TransitionBlock. We will
    // setup the TransitionBlock later once we can trash them
    //
    // push ebp-frame
    // push      ebp
    // mov       ebp,esp

    // save CalleeSavedRegisters
    // push      ebx

    push        esi
    push        edi

    // push ArgumentRegisters
    push        ecx
    push        edx

    mov         esi, esp

    #define STACK_ALIGN_PADDING 8

    sub         esp, STACK_ALIGN_PADDING
    push        [esi + 4*4]     // dispatch token
    push        [esi + 5*4]     // siteAddrForRegisterIndirect
    push        esi             // pTransitionBlock

    // Setup up proper EBP frame now that the stub arguments can be trashed
    mov         [esi + 4*4], ebx
    mov         [esi + 5*4], ebp
    lea         ebp, [esi + 5*4]

    // Make the call
    CHECK_STACK_ALIGNMENT
    call        C_FUNC(VSD_ResolveWorker)

    add         esp, STACK_ALIGN_PADDING

    #undef STACK_ALIGN_PADDING

    // From here on, mustn't trash eax

    // pop ArgumentRegisters
    pop         edx
    pop         ecx

    // pop CalleeSavedRegisters
    pop         edi
    pop         esi
    pop         ebx
    pop         ebp

    // Now jump to the target
    jmp         eax // continue on into the method
NESTED_END ResolveWorkerAsmStub, _TEXT

#ifdef FEATURE_STUBS_AS_IL
// ==========================================================================
// void SinglecastDelegateInvokeStub();
//
LEAF_ENTRY SinglecastDelegateInvokeStub, _TEXT

    test        ecx, ecx
    jz          LOCAL_LABEL(NullObject)

    mov         eax, [ecx + DelegateObject___methodPtr]
    mov         ecx, [ecx + DelegateObject___target]    // replace "this" pointer
    jmp         eax

LOCAL_LABEL(NullObject):

    mov         ecx, CORINFO_NullReferenceException_ASM
    jmp         C_FUNC(JIT_InternalThrow)

LEAF_END SinglecastDelegateInvokeStub, _TEXT
#endif // FEATURE_STUBS_AS_IL

#ifndef CROSSGEN_COMPILE
// =======================================================================================
// void ResolveWorkerChainLookupAsmStub();
//
//  This will perform a chained lookup of the entry if the initial cache lookup fails
//
//  Entry stack:
//           dispatch token
//           siteAddrForRegisterIndirect (used only if this is a RegisterIndirect dispatch call)
//           return address of caller to stub
//  Also, EAX contains the pointer to the first ResolveCacheElem pointer for the calculated
//  bucket in the cache table.
//
NESTED_ENTRY ResolveWorkerChainLookupAsmStub, _TEXT, NoHandler

#define CALL_STUB_CACHE_INITIAL_SUCCESS_COUNT   0x100

// this is the part of the stack that is present as we enter this function:
#define ChainLookup__token                  0x00
#define ChainLookup__indirect_addr          0x04
#define ChainLookup__caller_ret_addr        0x08
#define ChainLookup__ret_esp                0x0c

#define ChainLookup_spilled_reg_size        8

    // spill regs
    push    edx
    push    ecx

    // move the token into edx
    mov     edx, [esp + ChainLookup_spilled_reg_size + ChainLookup__token]

    // move the MT into ecx
    mov     ecx, [ecx]

LOCAL_LABEL(main_loop):

    // get the next entry in the chain (don't bother checking the first entry again)
    mov     eax, [eax + ResolveCacheElem__pNext]

    // test if we hit a terminating NULL
    test    eax, eax
    jz      LOCAL_LABEL(fail)

    // compare the MT of the ResolveCacheElem
    cmp     ecx, [eax + ResolveCacheElem__pMT]
    jne     LOCAL_LABEL(main_loop)

    // compare the token of the ResolveCacheElem
    cmp     edx, [eax + ResolveCacheElem__token]
    jne     LOCAL_LABEL(main_loop)

    // success
    // decrement success counter and move entry to start if necessary
    PREPARE_EXTERNAL_VAR g_dispatch_cache_chain_success_counter, edx
    mov     ecx, dword ptr [edx]
    sub     ecx, 1
    mov     dword ptr [edx], ecx

    //@TODO: Perhaps this should be a jl for better branch prediction?
    jge     LOCAL_LABEL(nopromote)

    // be quick to reset the counter so we don't get a bunch of contending threads
    mov     dword ptr [edx], CALL_STUB_CACHE_INITIAL_SUCCESS_COUNT

    #define STACK_ALIGN_PADDING 12
    sub     esp, STACK_ALIGN_PADDING

    // promote the entry to the beginning of the chain
    mov     ecx, eax

    CHECK_STACK_ALIGNMENT
    // call C_FUNC(VirtualCallStubManager::PromoteChainEntry)
    call    C_FUNC(_ZN22VirtualCallStubManager17PromoteChainEntryEP16ResolveCacheElem)

    add     esp, STACK_ALIGN_PADDING
    #undef  STACK_ALIGN_PADDING

LOCAL_LABEL(nopromote):

    pop     ecx
    pop     edx
    add     esp, (ChainLookup__caller_ret_addr - ChainLookup__token)
    mov     eax, [eax + ResolveCacheElem__target]
    jmp     eax

LOCAL_LABEL(fail):

    // restore registers
    pop     ecx
    pop     edx
    jmp     ResolveWorkerAsmStub

NESTED_END ResolveWorkerChainLookupAsmStub, _TEXT
#endif // CROSSGEN_COMPILE

// backpatch a call site to point to a different stub
NESTED_ENTRY BackPatchWorkerAsmStub, _TEXT, NoHandler
    PROLOG_BEG
    PROLOG_PUSH eax // it may contain siteAddrForRegisterIndirect
    PROLOG_PUSH ecx
    PROLOG_PUSH edx
    PROLOG_END

    // Call BackPatchWorkerStaticStub
    //
    // Here is expected stack layout at this point:
    //  | saved edx |
    //  | saved ecx |
    //  | saved eax |
    //  +-----------+ <- ebp
    //  | saved ebp |
    //  | saved eip |
    //  +-----------+ <- CFA of BackPatchWorkerAsmStub
    //  | saved eip |
    //  +-----------+ <- CFA of ResolveStub (16-byte aligned)
    // (Please refer to ResolveStub in vm/i386/virtualcallstubcpu.hpp for details)
    //
    push    eax                       //  any indirect call address as the 2nd arg
    push    DWORD PTR [ebp + 8]       //  return address (of ResolveStub) as the 1st arg

    CHECK_STACK_ALIGNMENT
    call    C_FUNC(BackPatchWorkerStaticStub)

    // Clean up arguments and alignment padding
    add     esp, 2*4

    EPILOG_BEG
    EPILOG_POP edx
    EPILOG_POP ecx
    EPILOG_POP eax
    EPILOG_END
    ret
NESTED_END BackPatchWorkerAsmStub, _TEXT

NESTED_ENTRY ProfileEnterNaked, _TEXT, NoHandler
    ret
NESTED_END ProfileEnterNaked, _TEXT

NESTED_ENTRY ProfileLeaveNaked, _TEXT, NoHandler
    ret
NESTED_END ProfileLeaveNaked, _TEXT

NESTED_ENTRY ProfileTailcallNaked, _TEXT, NoHandler
    ret
NESTED_END ProfileTailcallNaked, _TEXT

NESTED_ENTRY JIT_ProfilerEnterLeaveTailcallStub, _TEXT, NoHandler
    ret
NESTED_END JIT_ProfilerEnterLeaveTailcallStub, _TEXT
