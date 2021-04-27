// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*  EXCEP.CPP
 *
 */
//

//

#include "common.h"

#include "frames.h"
#include "threads.h"
#include "excep.h"
#include "object.h"
#include "field.h"
#include "dbginterface.h"
#include "cgensys.h"
#include "comutilnative.h"
#include "sigformat.h"
#include "siginfo.hpp"
#include "gcheaputilities.h"
#include "eedbginterfaceimpl.h" //so we can clearexception in COMPlusThrow
#include "asmconstants.h"

#include "exceptionhandling.h"
#include "virtualcallstub.h"



#if !defined(DACCESS_COMPILE)

VOID ResetCurrentContext()
{
    LIMITED_METHOD_CONTRACT;
}

LONG CLRNoCatchHandler(EXCEPTION_POINTERS* pExceptionInfo, PVOID pv)
{
    return EXCEPTION_CONTINUE_SEARCH;
}

#endif // !DACCESS_COMPILE

inline PTR_CONTEXT GetCONTEXTFromRedirectedStubStackFrameWorker(UINT_PTR establisherFrame)
{
    LIMITED_METHOD_DAC_CONTRACT;

    SIZE_T rbp = establisherFrame + REDIRECTSTUB_ESTABLISHER_OFFSET_RBP;
    PTR_PTR_CONTEXT ppContext = dac_cast<PTR_PTR_CONTEXT>((TADDR)rbp + REDIRECTSTUB_RBP_OFFSET_CONTEXT);
    return *ppContext;
}

PTR_CONTEXT GetCONTEXTFromRedirectedStubStackFrame(DISPATCHER_CONTEXT * pDispatcherContext)
{
    LIMITED_METHOD_DAC_CONTRACT;

    return GetCONTEXTFromRedirectedStubStackFrameWorker(pDispatcherContext->EstablisherFrame);
}

PTR_CONTEXT GetCONTEXTFromRedirectedStubStackFrame(CONTEXT * pContext)
{
    LIMITED_METHOD_DAC_CONTRACT;

    return GetCONTEXTFromRedirectedStubStackFrameWorker(pContext->Rbp);
}

#if !defined(DACCESS_COMPILE)

FaultingExceptionFrame *GetFrameFromRedirectedStubStackFrame (DISPATCHER_CONTEXT *pDispatcherContext)
{
    LIMITED_METHOD_CONTRACT;

    return (FaultingExceptionFrame*)(pDispatcherContext->EstablisherFrame + THROWSTUB_ESTABLISHER_OFFSET_FaultingExceptionFrame);
}

#endif // !DACCESS_COMPILE

#if !defined(DACCESS_COMPILE)

#define AMD64_SIZE64_PREFIX 0x48
#define AMD64_ADD_IMM8_OP 0x83
#define AMD64_ADD_IMM32_OP 0x81
#define AMD64_JMP_IMM8_OP 0xeb
#define AMD64_JMP_IMM32_OP 0xe9
#define AMD64_JMP_IND_OP 0xff
#define AMD64_JMP_IND_RAX 0x20
#define AMD64_LEA_OP 0x8d
#define AMD64_POP_OP 0x58
#define AMD64_RET_OP 0xc3
#define AMD64_RET_OP_2 0xc2
#define AMD64_REP_PREFIX 0xf3
#define AMD64_NOP 0x90
#define AMD64_INT3 0xCC

#define AMD64_IS_REX_PREFIX(x)      (((x) & 0xf0) == 0x40)

#define FAKE_PROLOG_SIZE 1
#define FAKE_FUNCTION_CODE_SIZE 1

#ifdef DEBUGGING_SUPPORTED
//
// If there is an Int3 opcode at the Address then this tries to get the
// correct Opcode for the address from the managed patch table. If this is
// called on an address which doesn't currently have an Int3 then the current
// opcode is returned. If there is no managed patch in the patch table
// corresponding to this address then the current opcode (0xCC) at Address is
// is returned. If a 0xCC is returned from this function it indicates an
// unmanaged patch at the address.
//
// If there is a managed patch at the address HasManagedBreakpoint is set to true.
//
// If there is a 0xCC at the address before the call to GetPatchedOpcode and
// still a 0xCC when we return then this is considered an unmanaged patch and
// HasManagedBreakpoint is set to true.
//
UCHAR GetOpcodeFromManagedBPForAddress(ULONG64 Address, BOOL* HasManagedBreakpoint, BOOL* HasUnmanagedBreakpoint)
{
    // If we don't see a breakpoint then quickly return.
    if (((UCHAR)*(BYTE*)Address) != AMD64_INT3)
    {
        return ((UCHAR)*(BYTE*)Address);
    }

    UCHAR PatchedOpcode;
    PatchedOpcode = (UCHAR)g_pDebugInterface->GetPatchedOpcode((CORDB_ADDRESS_TYPE*)(BYTE*)Address);

    // If a non Int3 opcode is returned from GetPatchedOpcode then
    // this function has a managed breakpoint
    if (PatchedOpcode != AMD64_INT3)
    {
        (*HasManagedBreakpoint) = TRUE;
    }
    else
    {
        (*HasUnmanagedBreakpoint) = TRUE;
    }

    return PatchedOpcode;
}
#endif // DEBUGGING_SUPPORTED

PEXCEPTION_ROUTINE
RtlVirtualUnwind (
          IN ULONG HandlerType,
          IN ULONG64 ImageBase,
          IN ULONG64 ControlPc,
          IN PT_RUNTIME_FUNCTION FunctionEntry,
          IN OUT PCONTEXT ContextRecord,
          OUT PVOID *HandlerData,
          OUT PULONG64 EstablisherFrame,
          IN OUT PKNONVOLATILE_CONTEXT_POINTERS ContextPointers OPTIONAL
          )
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // The indirection should be taken care of by the caller
    _ASSERTE((FunctionEntry->UnwindData & RUNTIME_FUNCTION_INDIRECT) == 0);

#ifdef DEBUGGING_SUPPORTED
    if (CORDebuggerAttached())
    {
        return RtlVirtualUnwind_Worker(HandlerType, ImageBase, ControlPc, FunctionEntry, ContextRecord, HandlerData, EstablisherFrame, ContextPointers);
    }
    else
#endif // DEBUGGING_SUPPORTED
    {
        return RtlVirtualUnwind_Unsafe(HandlerType, ImageBase, ControlPc, FunctionEntry, ContextRecord, HandlerData, EstablisherFrame, ContextPointers);
    }
}

#ifdef DEBUGGING_SUPPORTED
PEXCEPTION_ROUTINE
RtlVirtualUnwind_Worker (
          IN ULONG HandlerType,
          IN ULONG64 ImageBase,
          IN ULONG64 ControlPc,
          IN PT_RUNTIME_FUNCTION FunctionEntry,
          IN OUT PCONTEXT ContextRecord,
          OUT PVOID *HandlerData,
          OUT PULONG64 EstablisherFrame,
          IN OUT PKNONVOLATILE_CONTEXT_POINTERS ContextPointers OPTIONAL
          )
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // b/c we're only called by the safe RtlVirtualUnwind we are guaranteed
    // that the debugger is attched when we get here.
    _ASSERTE(CORDebuggerAttached());

    LOG((LF_CORDB, LL_EVERYTHING, "RVU_CBSW: in RtlVitualUnwind_ClrDbgSafeWorker, ControlPc=0x%p\n", ControlPc));

    BOOL     InEpilogue = FALSE;
    BOOL     HasManagedBreakpoint = FALSE;
    BOOL     HasUnmanagedBreakpoint = FALSE;
    UCHAR    TempOpcode = NULL;
    PUCHAR   NextByte;
    ULONG    CurrentOffset;
    ULONG    FrameRegister;
    ULONG64  BranchTarget;
    PUNWIND_INFO UnwindInfo;

    // 64bit Whidbey does NOT support interop debugging, so if this
    // is not managed code, normal unwind
    if (!ExecutionManager::IsManagedCode((PCODE) ControlPc))
    {
        goto NORMAL_UNWIND;
    }

    UnwindInfo = (PUNWIND_INFO)(FunctionEntry->UnwindData + ImageBase);
    CurrentOffset = (ULONG)(ControlPc - (FunctionEntry->BeginAddress + ImageBase));

    // control stopped in prologue, normal unwind
    if (CurrentOffset < UnwindInfo->SizeOfProlog)
    {
        goto NORMAL_UNWIND;
    }

    // ASSUMPTION: only the first byte of an opcode will be patched by the CLR debugging code

    // determine if we're in an epilog and if there is at least one managed breakpoint
    NextByte = (PUCHAR)ControlPc;

    TempOpcode = GetOpcodeFromManagedBPForAddress((ULONG64)NextByte, &HasManagedBreakpoint, &HasUnmanagedBreakpoint);

    // TempOpcode == NextByte[0] unless NextByte[0] is a breakpoint
    _ASSERTE(TempOpcode == NextByte[0] || NextByte[0] == AMD64_INT3);

    // Check for an indication of the start of a epilogue:
    //   add rsp, imm8
    //   add rsp, imm32
    //   lea rsp, -disp8[fp]
    //   lea rsp, -disp32[fp]
    if ((TempOpcode == AMD64_SIZE64_PREFIX)
        && (NextByte[1] == AMD64_ADD_IMM8_OP)
        && (NextByte[2] == 0xc4))
    {
        // add rsp, imm8.
        NextByte += 4;
    }
    else if ((TempOpcode == AMD64_SIZE64_PREFIX)
            && (NextByte[1] == AMD64_ADD_IMM32_OP)
            && (NextByte[2] == 0xc4))
    {
        // add rsp, imm32.
        NextByte += 7;
    }
    else if (((TempOpcode & 0xf8) == AMD64_SIZE64_PREFIX)
            && (NextByte[1] == AMD64_LEA_OP))
    {
        FrameRegister = ((TempOpcode & 0x7) << 3) | (NextByte[2] & 0x7);

        if ((FrameRegister != 0)
            && (FrameRegister == UnwindInfo->FrameRegister))
        {
            if ((NextByte[2] & 0xf8) == 0x60)
            {
                // lea rsp, disp8[fp].
                NextByte += 4;
            }
            else if ((NextByte[2] &0xf8) == 0xa0)
            {
                // lea rsp, disp32[fp].
                NextByte += 7;
            }
        }
    }

    // if we haven't eaten any of the code stream detecting a stack adjustment
    // then TempOpcode is still valid
    if (((ULONG64)NextByte) != ControlPc)
    {
        TempOpcode = GetOpcodeFromManagedBPForAddress((ULONG64)NextByte, &HasManagedBreakpoint, &HasUnmanagedBreakpoint);
    }

    // TempOpcode == NextByte[0] unless NextByte[0] is a breakpoint
    _ASSERTE(TempOpcode == NextByte[0] || NextByte[0] == AMD64_INT3);

    // Check for any number of:
    //   pop nonvolatile-integer-register[0..15].
    while (TRUE)
    {
        if ((TempOpcode & 0xf8) == AMD64_POP_OP)
        {
            NextByte += 1;
        }
        else if (AMD64_IS_REX_PREFIX(TempOpcode)
                && ((NextByte[1] & 0xf8) == AMD64_POP_OP))
        {
            NextByte += 2;
        }
        else
        {
            // when we break out here TempOpcode will hold the next Opcode so there
            // is no need to call GetOpcodeFromManagedBPForAddress again
            break;
        }
        TempOpcode = GetOpcodeFromManagedBPForAddress((ULONG64)NextByte, &HasManagedBreakpoint, &HasUnmanagedBreakpoint);

        // TempOpcode == NextByte[0] unless NextByte[0] is a breakpoint
        _ASSERTE(TempOpcode == NextByte[0] || NextByte[0] == AMD64_INT3);
    }

    // TempOpcode == NextByte[0] unless NextByte[0] is a breakpoint
    _ASSERTE(TempOpcode == NextByte[0] || NextByte[0] == AMD64_INT3);

    // If the next instruction is a return, then control is currently in
    // an epilogue and execution of the epilogue should be emulated.
    // Otherwise, execution is not in an epilogue and the prologue should
    // be unwound.
    if (TempOpcode == AMD64_RET_OP || TempOpcode == AMD64_RET_OP_2)
    {
        // A return is an unambiguous indication of an epilogue
        InEpilogue = TRUE;
        NextByte += 1;
    }
    else if (TempOpcode == AMD64_REP_PREFIX && NextByte[1] == AMD64_RET_OP)
    {
        // A return is an unambiguous indication of an epilogue
        InEpilogue = TRUE;
        NextByte += 2;
    }
    else if (TempOpcode == AMD64_JMP_IMM8_OP || TempOpcode == AMD64_JMP_IMM32_OP)
    {
        // An unconditional branch to a target that is equal to the start of
        // or outside of this routine is logically a call to another function.
        BranchTarget = (ULONG64)NextByte - ImageBase;

        if (TempOpcode == AMD64_JMP_IMM8_OP)
        {
            BranchTarget += 2 + (CHAR)NextByte[1];
            NextByte += 2;
        }
        else
        {
            BranchTarget += 5 + *((LONG UNALIGNED *)&NextByte[1]);
            NextByte += 5;
        }

        // Now determine whether the branch target refers to code within this
        // function. If not, then it is an epilogue indicator.
        //
        // A branch to the start of self implies a recursive call, so
        // is treated as an epilogue.
        if (BranchTarget <= FunctionEntry->BeginAddress ||
            BranchTarget >= FunctionEntry->EndAddress)
        {
            _ASSERTE((UnwindInfo->Flags & UNW_FLAG_CHAININFO) == 0);
            InEpilogue = TRUE;
        }
    }
    else if ((TempOpcode == AMD64_JMP_IND_OP) && (NextByte[1] == 0x25))
    {
        // An unconditional jump indirect.

        // This is a jmp outside of the function, probably a tail call
        // to an import function.
        InEpilogue = TRUE;
        NextByte += 2;
    }
    else if (((TempOpcode & 0xf8) == AMD64_SIZE64_PREFIX)
            && (NextByte[1] == AMD64_JMP_IND_OP)
            && (NextByte[2] & 0x38) == AMD64_JMP_IND_RAX)
    {
        //
        // This is an indirect jump opcode: 0x48 0xff /4.  The 64-bit
        // flag (REX.W) is always redundant here, so its presence is
        // overloaded to indicate a branch out of the function - a tail
        // call.
        //
        // Such an opcode is an unambiguous epilogue indication.
        //
        InEpilogue = TRUE;
        NextByte += 3;
    }

    if (InEpilogue && HasUnmanagedBreakpoint)
    {
        STRESS_LOG1(LF_CORDB, LL_ERROR, "RtlVirtualUnwind is about to fail b/c the ControlPc (0x%p) is in the epilog of a function which has a 0xCC in its epilog.", ControlPc);
        _ASSERTE(!"RtlVirtualUnwind is about to fail b/c you are unwinding through\n"
                  "the epilogue of a function and have a 0xCC in the codestream. This is\n"
                  "probably caused by having set that breakpoint yourself in the debugger,\n"
                  "you might try to remove the bp and ignore this assert.");
    }

    if (!(InEpilogue && HasManagedBreakpoint))
    {
        goto NORMAL_UNWIND;
    }
    else
    {
        // InEpilogue && HasManagedBreakpoint, this means we have to make the fake code buffer

        // We explicitly handle the case where the new below can't allocate, but we're still
        // getting an assert from inside new b/c we can be called within a FAULT_FORBID scope.
        //
        // If new does fail we will still end up crashing, but the debugger doesn't have to
        // be OOM hardened in Whidbey and this is a debugger only code path so we're ok in
        // that department.
        FAULT_NOT_FATAL();

        LOG((LF_CORDB, LL_EVERYTHING, "RVU_CBSW: Function has >1 managed bp in the epilogue, and we are in the epilogue, need a code buffer for RtlVirtualUnwind\n"));

        // IMPLEMENTATION NOTE:
        // It is of note that we are significantly pruning the funtion here in making the fake
        // code buffer, all that we are making room for is 1 byte for the prologue, 1 byte for
        // function code and what is left of the epilogue to be executed. This is _very_ closely
        // tied to the implmentation of RtlVirtualUnwind and the knowledge that by passing the
        // the test above and having InEpilogue==TRUE then the code path which will be followed
        // through RtlVirtualUnwind is known.
        //
        // In making this fake code buffer we need to ensure that we don't mess with the outcome
        // of the test in RtlVirtualUnwind to determine that control stopped within a function
        // epilogue, or the unwinding that will happen when that test comes out TRUE. To that end
        // we have preserved a single byte representing the Prologue as a section of the buffer
        // as well as a single byte representation of the Function code so that tests to make sure
        // that we're out of the prologue will not fail.

        T_RUNTIME_FUNCTION FakeFunctionEntry;

        //
        // The buffer contains 4 sections
        //
        // UNWIND_INFO:     The fake UNWIND_INFO will be first, we are making a copy within the
        //                  buffer because it needs to be addressable through a 32bit offset
        //                  of NewImageBase like the fake code buffer
        //
        // Prologue:        A single byte representing the function Prologue
        //
        // Function Code:   A single byte representing the Function's code
        //
        // Epilogue:        This contains what is left to be executed of the Epilogue which control
        //                  stopped in, it can be as little as a "return" type statement or as much
        //                  as the whole Epilogue containing a stack adjustment, pops and "return"
        //                  type statement.
        //
        //
        // Here is the layout of the buffer:
        //
        // UNWIND_INFO copy:
        //    pBuffer[0]
        //     ...
        //    pBuffer[sizeof(UNWIND_INFO) - 1]
        // PROLOGUE:
        //    pBuffer[sizeof(UNWIND_INFO) + 0] <----------------- THIS IS THE START OF pCodeBuffer
        // FUNCTION CODE:
        //    pBuffer[sizeof(UNWIND_INFO) + FAKE_PROLOG_SIZE]
        // EPILOGUE
        //    pBuffer[sizeof(UNWIND_INFO) + FAKE_PROLOG_SIZE + FAKE_FUNCTION_CODE_SIZE]
        //     ...
        //    pBuffer[sizeof(UNWIND_INFO) + FAKE_PROLOG_SIZE + FAKE_FUNCTION_CODE_SIZE + SizeOfEpilogue]
        //
        ULONG   SizeOfEpilogue = (ULONG)((ULONG64)NextByte - ControlPc);
        ULONG   SizeOfBuffer = (ULONG)(sizeof(UNWIND_INFO) + FAKE_PROLOG_SIZE + FAKE_FUNCTION_CODE_SIZE + SizeOfEpilogue);
        BYTE   *pBuffer = (BYTE*) new (nothrow) BYTE[SizeOfBuffer];
        BYTE   *pCodeBuffer;
        ULONG64 NewImageBase;
        ULONG64 NewControlPc;

        // <TODO> This WILL fail during unwind because we KNOW there is a managed breakpoint
        // in the epilog and we're in the epilog, but we could not allocate a buffer to
        // put our cleaned up code into, what to do? </TODO>
        if (pBuffer == NULL)
        {
            // TODO: can we throw OOM here? or will we just go recursive b/c that will eventually get to the same place?
            _ASSERTE(!"OOM when trying to allocate buffer for virtual unwind cleaned code, BIG PROBLEM!!");
            goto NORMAL_UNWIND;
        }

        NewImageBase = ((((ULONG64)pBuffer) >> 32) << 32);
        pCodeBuffer = pBuffer + sizeof(UNWIND_INFO);

#if defined(_DEBUG)
        // Fill the buffer up to the rest of the epilogue to be executed with Int3
        for (int i=0; i<(FAKE_PROLOG_SIZE + FAKE_FUNCTION_CODE_SIZE); i++)
        {
            pCodeBuffer[i] = AMD64_INT3;
        }
#endif

        // Copy the UNWIND_INFO and the Epilogue into the buffer
        memcpy(pBuffer, (const void*)UnwindInfo, sizeof(UNWIND_INFO));
        memcpy(&(pCodeBuffer[FAKE_PROLOG_SIZE + FAKE_FUNCTION_CODE_SIZE]), (const void*)(BYTE*)ControlPc, SizeOfEpilogue);

        _ASSERTE((UCHAR)*(BYTE*)ControlPc == (UCHAR)pCodeBuffer[FAKE_PROLOG_SIZE+FAKE_FUNCTION_CODE_SIZE]);

        HasManagedBreakpoint = FALSE;
        HasUnmanagedBreakpoint = FALSE;

        // The buffer cleaning implementation here just runs through the buffer byte by byte trying
        // to get a real opcode from the patch table for any 0xCC that it finds. There is the
        // possiblity that the epilogue will contain a 0xCC in an immediate value for which a
        // patch won't be found and this will report a false positive for HasUnmanagedBreakpoint.
        BYTE* pCleanCodePc = pCodeBuffer + FAKE_PROLOG_SIZE + FAKE_FUNCTION_CODE_SIZE;
        BYTE* pRealCodePc = (BYTE*)ControlPc;
        while (pCleanCodePc < (pCodeBuffer + FAKE_PROLOG_SIZE + FAKE_FUNCTION_CODE_SIZE + SizeOfEpilogue))
        {
            // If we have a breakpoint at the address then try to get the correct opcode from
            // the managed patch using GetOpcodeFromManagedBPForAddress.
            if (AMD64_INT3 == ((UCHAR)*pCleanCodePc))
            {
                (*pCleanCodePc) = GetOpcodeFromManagedBPForAddress((ULONG64)pRealCodePc, &HasManagedBreakpoint, &HasUnmanagedBreakpoint);
            }

            pCleanCodePc++;
            pRealCodePc++;
        }

        // On the second pass through the epilogue assuming things are working as
        // they should we should once again have at least one managed breakpoint...
        // otherwise why are we here?
        _ASSERTE(HasManagedBreakpoint == TRUE);

        // This would be nice to assert, but we can't w/ current buffer cleaning implementation, see note above.
        // _ASSERTE(HasUnmanagedBreakpoint == FALSE);

        ((PUNWIND_INFO)pBuffer)->SizeOfProlog = FAKE_PROLOG_SIZE;

        FakeFunctionEntry.BeginAddress = (ULONG)((ULONG64)pCodeBuffer - NewImageBase);
        FakeFunctionEntry.EndAddress   = (ULONG)((ULONG64)(pCodeBuffer + (FAKE_PROLOG_SIZE + FAKE_FUNCTION_CODE_SIZE + SizeOfEpilogue)) - NewImageBase);
        FakeFunctionEntry.UnwindData   = (ULONG)((ULONG64)pBuffer - NewImageBase);

        NewControlPc = (ULONG64)(pCodeBuffer + FAKE_PROLOG_SIZE + FAKE_FUNCTION_CODE_SIZE);

        RtlVirtualUnwind_Unsafe((ULONG)HandlerType, (ULONG64)NewImageBase, (ULONG64)NewControlPc, &FakeFunctionEntry, ContextRecord, HandlerData, EstablisherFrame, ContextPointers);

        // Make sure to delete the whole buffer and not just the code buffer
        delete[] pBuffer;

        return NULL; // if control left in the epilog then RtlVirtualUnwind will not return an exception handler
    }

NORMAL_UNWIND:
    return RtlVirtualUnwind_Unsafe(HandlerType, ImageBase, ControlPc, FunctionEntry, ContextRecord, HandlerData, EstablisherFrame, ContextPointers);
}
#endif // DEBUGGING_SUPPORTED

#undef FAKE_PROLOG_SIZE
#undef FAKE_FUNCTION_CODE_SIZE

#undef AMD64_SIZE64_PREFIX
#undef AMD64_ADD_IMM8_OP
#undef AMD64_ADD_IMM32_OP
#undef AMD64_JMP_IMM8_OP
#undef AMD64_JMP_IMM32_OP
#undef AMD64_JMP_IND_OP
#undef AMD64_JMP_IND_RAX
#undef AMD64_POP_OP
#undef AMD64_RET_OP
#undef AMD64_RET_OP_2
#undef AMD64_NOP
#undef AMD64_INT3

#endif // !DACCESS_COMPILE

#ifndef DACCESS_COMPILE
// Returns TRUE if caller should resume execution.
BOOL
AdjustContextForVirtualStub(
        EXCEPTION_RECORD *pExceptionRecord,
        CONTEXT *pContext)
{
    LIMITED_METHOD_CONTRACT;

    Thread * pThread = GetThreadNULLOk();

    // We may not have a managed thread object. Example is an AV on the helper thread.
    // (perhaps during StubManager::IsStub)
    if (pThread == NULL)
    {
        return FALSE;
    }

    PCODE f_IP = GetIP(pContext);

    VirtualCallStubManager::StubKind sk;
    VirtualCallStubManager::FindStubManager(f_IP, &sk);

    if (sk == VirtualCallStubManager::SK_DISPATCH)
    {
        if ((*PTR_DWORD(f_IP) & 0xffffff) != X64_INSTR_CMP_IND_THIS_REG_RAX) // cmp [THIS_REG], rax
        {
            _ASSERTE(!"AV in DispatchStub at unknown instruction");
            return FALSE;
        }
    }
    else
    if (sk == VirtualCallStubManager::SK_RESOLVE)
    {
        if ((*PTR_DWORD(f_IP) & 0xffffff) != X64_INSTR_MOV_RAX_IND_THIS_REG) // mov rax, [THIS_REG]
        {
            _ASSERTE(!"AV in ResolveStub at unknown instruction");
            return FALSE;
        }
        SetSP(pContext, dac_cast<PCODE>(dac_cast<PTR_BYTE>(GetSP(pContext)) + sizeof(void*))); // rollback push rdx
    }
    else
    {
        return FALSE;
    }

    PCODE callsite = *dac_cast<PTR_PCODE>(GetSP(pContext)); 
    if (pExceptionRecord != NULL)
    {
        pExceptionRecord->ExceptionAddress = (PVOID)callsite;
    }
    SetIP(pContext, callsite);
    SetSP(pContext, dac_cast<PCODE>(dac_cast<PTR_BYTE>(GetSP(pContext)) + sizeof(void*))); // Move SP to where it was at the call site

    return TRUE;
}

#endif

