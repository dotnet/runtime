// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
/*===========================================================================
**
** File:    RemotingCpu.cpp
** 
**
**
** Purpose: Defines various remoting related functions for the AMD64 architecture
**
**
** See code:EEStartup#TableOfContents for EE overview
**
=============================================================================*/

#include "common.h"

#ifdef FEATURE_REMOTING

#include "excep.h"
#include "comdelegate.h"
#include "remoting.h"
#include "field.h"
#include "siginfo.hpp"
#include "stackbuildersink.h"
#include "threads.h"
#include "method.hpp"

#include "asmconstants.h"

// External variables
extern DWORD g_dwNonVirtualThunkRemotingLabelOffset;
extern DWORD g_dwNonVirtualThunkReCheckLabelOffset;

//+----------------------------------------------------------------------------
//
//  Method:     CRemotingServices::CheckForContextMatch   public
//
//  Synopsis:   This code generates a check to see if the current context and
//              the context of the proxy match.
// 
//+----------------------------------------------------------------------------
//
// returns zero if contexts match
// returns non-zero if contexts don't match
//
extern "C" UINT_PTR __stdcall CRemotingServices__CheckForContextMatch(Object* pStubData)
{
    // This method cannot have a contract because CreateStubForNonVirtualMethod assumes 
    // it won't trash XMM registers. The code generated for contracts by recent compilers
    // is trashing XMM registers.
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_COOPERATIVE; // due to the Object parameter
    STATIC_CONTRACT_SO_TOLERANT;

    UINT_PTR contextID  = *(UINT_PTR*)pStubData->UnBox();
    UINT_PTR contextCur = (UINT_PTR)GetThread()->m_Context;
    return (contextCur != contextID);   // chosen to match x86 convention
}


//+----------------------------------------------------------------------------
//
//  Method:     CTPMethodTable::CreateThunkForVirtualMethod   private
//
//  Synopsis:   Creates the thunk that pushes the supplied slot number and jumps
//              to TP Stub
// 
//+----------------------------------------------------------------------------
PCODE CTPMethodTable::CreateThunkForVirtualMethod(DWORD dwSlot, BYTE* pbCode)
{
    LIMITED_METHOD_CONTRACT;

    BYTE *pbCodeStart = pbCode;

    // NOTE: if you change the code generated here, update
    // CVirtualThunkMgr::IsThunkByASM, CVirtualThunkMgr::GetMethodDescByASM

    //
    // mov  r10, <dwSlot>
    // mov  rax, TransparentProxyStub
    // jmp  rax
    //
    *pbCode++           = 0x49;
    *pbCode++           = 0xc7;
    *pbCode++           = 0xc2;
    *((DWORD*)pbCode)   = dwSlot;
    pbCode += sizeof(DWORD);
    *pbCode++           = 0x48;
    *pbCode++           = 0xB8;
    *((UINT64*)pbCode)  = (UINT64)(TransparentProxyStub);
    pbCode += sizeof(UINT64);
    *pbCode++           = 0xFF;
    *pbCode++           = 0xE0;

    _ASSERTE(pbCode - pbCodeStart == ConstVirtualThunkSize);
    _ASSERTE(CVirtualThunkMgr::IsThunkByASM((PCODE)pbCodeStart));

    return (PCODE)pbCodeStart;
}


#ifdef HAS_REMOTING_PRECODE

//+----------------------------------------------------------------------------
//
//  Method:     CTPMethodTable::ActivatePrecodeRemotingThunk    private
//
//  Synopsis:   Patch the precode remoting thunk to begin interception
//
//+----------------------------------------------------------------------------
void CTPMethodTable::ActivatePrecodeRemotingThunk()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    PORTABILITY_WARNING("CTPMethodTable::ActivatePrecodeRemotingThunk");
}

#else // HAS_REMOTING_PRECODE

//+----------------------------------------------------------------------------
//
//  Method:     CTPMethodTable::CreateStubForNonVirtualMethod   public
//
//  Synopsis:   Create a stub for a non virtual method
// 
//+----------------------------------------------------------------------------
Stub* CTPMethodTable::CreateStubForNonVirtualMethod(MethodDesc* pMD, CPUSTUBLINKER* psl,
                                                    LPVOID pvAddrOfCode, Stub* pInnerStub)
{
    STANDARD_VM_CONTRACT;

    // Sanity check

    Stub *pStub = NULL;

    // we need a hash table only for virtual methods
    _ASSERTE(!pMD->IsVirtual());

    // Ensure the TP MethodTable's fields have been initialized.
    EnsureFieldsInitialized();

    /*
    NonVirtualMethodStub<thisReg, pvAddrOfCode, pTPMethodTable, pvTPStub>
    {
      ;; thisReg:   this

      sub     rsp, 0x28

      test    thisReg, thisReg
      je      JmpAddrLabel

      mov     rax, [thisReg]
      mov     r10, <pTPMethodTable>
      cmp     rax, r10
      jne     JmpAddrLabel

      mov     [rsp+0x30], rcx                     ;|
      mov     [rsp+0x38], rdx                     ;|
      mov     [rsp+0x40], r8                      ;|
      mov     [rsp+0x48], r9                      ;|
                                                  ;|
      mov     rax, [thisReg + TransparentProxyObject___stubData] ;|
      call    [thisReg + TransparentProxyObject___stub]          ;| EmitCallToStub<pCtxMismatch>
                                                  ;|
      mov     rcx, [rsp+0x30]                     ;|
      mov     rdx, [rsp+0x38]                     ;|
      mov     r8, [rsp+0x40]                      ;|
      mov     r9, [rsp+0x48]                      ;|
                                                  ;|
      test    rax, rax                            ;|
      jnz     RemotingLabel                       ;|

    JmpAddrLabel:
      mov     rax, <pvAddrOfCode>
      add     rsp, 0x28
      jmp     rax

    RemotingLabel:
      mov     r10, <pMD>
      mov     rax, <pvTPStub>
      add     rsp, 0x20
      jmp     rax
    }
    */

    X86Reg  thisReg = kRCX;
    void*   pvTPStub = TransparentProxyStub_CrossContext;

    // Generate label where a null reference exception will be thrown
    CodeLabel *pJmpAddrLabel = psl->NewCodeLabel();
    // Generate label where remoting code will execute
    CodeLabel *pRemotingLabel = psl->NewCodeLabel();

    // NOTE: if you change any of this code, you must update
    // CNonVirtualThunkMgr::IsThunkByASM.

    // Allocate callee scratch area
    // sub      rsp, 0x28
    psl->X86EmitSubEsp(0x28);

    // test     thisReg, thisReg
    psl->X86EmitR2ROp(0x85, thisReg, thisReg);
    // je       JmpAddrLabel
    psl->X86EmitCondJump(pJmpAddrLabel, X86CondCode::kJE);

    // Emit a label here for the debugger. A breakpoint will
    // be set at the next instruction and the debugger will
    // call CNonVirtualThunkMgr::TraceManager when the
    // breakpoint is hit with the thread's context.
    CodeLabel *pRecheckLabel = psl->NewCodeLabel();
    psl->EmitLabel(pRecheckLabel);

    // mov      rax, [thisReg]
    psl->X86EmitIndexRegLoad(kRAX, thisReg, 0);

    // mov      r10, CTPMethodTable::GetMethodTable()
    psl->X86EmitRegLoad(kR10, (UINT_PTR)CTPMethodTable::GetMethodTable());
    // cmp      rax, r10
    psl->X86EmitR2ROp(0x3B, kRAX, kR10);

    // jne      JmpAddrLabel
    psl->X86EmitCondJump(pJmpAddrLabel, X86CondCode::kJNE);

    // CONSIDER: write all possible stubs in asm to ensure param registers are not trashed

    // mov      [rsp+0x30], rcx
    // mov      [rsp+0x38], rdx
    // mov      [rsp+0x40], r8
    // mov      [rsp+0x48], r9
    psl->X86EmitRegSave(kRCX, 0x30);
    psl->X86EmitRegSave(kRDX, 0x38);
    psl->X86EmitRegSave(kR8, 0x40);
    psl->X86EmitRegSave(kR9, 0x48);

    // mov      rax, [thisReg + TransparentProxyObject___stub]
    psl->X86EmitIndexRegLoad(kRAX, thisReg, TransparentProxyObject___stub);

    // mov      rcx, [thisReg + TransparentProxyObject___stubData]
    psl->X86EmitIndexRegLoad(kRCX, thisReg, TransparentProxyObject___stubData);

    // call      rax
    psl->Emit16(0xd0ff);

    // mov      rcx, [rsp+0x30]
    // mov      rdx, [rsp+0x38]
    // mov      r8, [rsp+0x40]
    // mov      r9, [rsp+0x48]
    psl->X86EmitEspOffset(0x8b, kRCX, 0x30);
    psl->X86EmitEspOffset(0x8b, kRDX, 0x38);
    psl->X86EmitEspOffset(0x8b, kR8, 0x40);
    psl->X86EmitEspOffset(0x8b, kR9, 0x48);

    // test     rax, rax
    psl->X86EmitR2ROp(0x85, kRAX, kRAX);
    // jnz      RemotingLabel
    psl->X86EmitCondJump(pRemotingLabel, X86CondCode::kJNZ);

//  pJmpAddrLabel:
    psl->EmitLabel(pJmpAddrLabel);

    // Make sure that the actual code does not require MethodDesc in r10
    _ASSERTE(!pMD->RequiresMethodDescCallingConvention());

    //       mov      rax, <pvAddrOfCode>
    //       add      rsp, 0x28
    // REX.W jmp      rax 
    psl->X86EmitTailcallWithESPAdjust(psl->NewExternalCodeLabel(pvAddrOfCode), 0x28);

// pRemotingLabel:
    psl->EmitLabel(pRemotingLabel);

    // mov      r10, <pMD>
    psl->X86EmitRegLoad(kR10, (UINT_PTR)pMD);

    //       mov      rax, <pvTPStub>
    //       add      rsp, 0x28
    // REX.W jmp      rax
    psl->X86EmitTailcallWithESPAdjust(psl->NewExternalCodeLabel(pvTPStub), 0x28);

    // Link and produce the stub
    pStub = psl->LinkInterceptor(pMD->GetLoaderAllocator()->GetStubHeap(),
                                    pInnerStub, pvAddrOfCode);

    return pStub;
}


//+----------------------------------------------------------------------------
//
//  Synopsis:   Find an existing thunk or create a new one for the given
//              method descriptor. NOTE: This is used for the methods that do
//              not go through the vtable such as constructors, private and
//              final methods.
// 
//+----------------------------------------------------------------------------
PCODE CTPMethodTable::CreateNonVirtualThunkForVirtualMethod(MethodDesc* pMD)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pMD));
    }
    CONTRACTL_END;

    CPUSTUBLINKER sl;
    CPUSTUBLINKER* psl = &sl;

    Stub *pStub = NULL;

    // The thunk has not been created yet. Go ahead and create it.
    // Compute the address of the slot
    LPVOID pvEntryPoint = (LPVOID)pMD->GetMethodEntryPoint();

    X86Reg  thisReg = kRCX;
    void*   pvStub = CRemotingServices__DispatchInterfaceCall;

    // Generate label where a null reference exception will be thrown
    CodeLabel *pExceptionLabel = psl->NewCodeLabel();

    //  !!! WARNING WARNING WARNING WARNING WARNING !!!
    //
    //  DO NOT CHANGE this code without changing the thunk recognition
    //  code in CNonVirtualThunkMgr::IsThunkByASM
    //  & CNonVirtualThunkMgr::GetMethodDescByASM
    //
    //  !!! WARNING WARNING WARNING WARNING WARNING !!!

    // NOTE: constant mov's should use an extended register to force a REX
    // prefix and the full 64-bit immediate value, so that
    // g_dwNonVirtualThunkRemotingLabelOffset and
    // g_dwNonVirtualThunkReCheckLabelOffset are the same for all
    // generated code.

    // if this == NULL throw NullReferenceException
    // test rcx, rcx
    psl->X86EmitR2ROp(0x85, thisReg, thisReg);

    // je ExceptionLabel
    psl->X86EmitCondJump(pExceptionLabel, X86CondCode::kJE);

    // Generate label where remoting code will execute
    CodeLabel *pRemotingLabel = psl->NewCodeLabel();

    // Emit a label here for the debugger. A breakpoint will
    // be set at the next instruction and the debugger will
    // call CNonVirtualThunkMgr::TraceManager when the
    // breakpoint is hit with the thread's context.
    CodeLabel *pRecheckLabel = psl->NewCodeLabel();
    psl->EmitLabel(pRecheckLabel);

    // If this.MethodTable == TPMethodTable then do RemotingCall
    // mov      rax, [thisReg]
    psl->X86EmitIndexRegLoad(kRAX, thisReg, 0);
    // mov      r10, CTPMethodTable::GetMethodTable()
    psl->X86EmitRegLoad(kR10, (UINT_PTR)CTPMethodTable::GetMethodTable());
    // cmp      rax, r10
    psl->X86EmitR2ROp(0x3B, kRAX, kR10);
    // je RemotingLabel
    psl->X86EmitCondJump(pRemotingLabel, X86CondCode::kJE);

    // Exception handling and non-remoting share the
    // same codepath
    psl->EmitLabel(pExceptionLabel);

    // Non-RemotingCode
    // Jump to the vtable slot of the method
    // mov rax, pvEntryPoint
    // Encoded the mov manually so that it always uses the 64-bit form.
    //psl->X86EmitRegLoad(kRAX, (UINT_PTR)pvEntryPoint);
    psl->Emit8(REX_PREFIX_BASE | REX_OPERAND_SIZE_64BIT);
    psl->Emit8(0xb8);
    psl->EmitBytes((BYTE*)&pvEntryPoint, 8);
    // jmp rax
    psl->Emit8(0xff);
    psl->Emit8(0xe0);

    // Remoting code. Note: CNonVirtualThunkMgr::TraceManager
    // relies on this label being right after the jmp pvEntryPoint
    // instruction above. If you move this label, update
    // CNonVirtualThunkMgr::DoTraceStub.
    psl->EmitLabel(pRemotingLabel);

    // Save the MethodDesc and goto TPStub
    // push MethodDesc
    psl->X86EmitRegLoad(kR10, (UINT_PTR)pMD);

    // jmp TPStub
    psl->X86EmitNearJump(psl->NewExternalCodeLabel(pvStub));

    // Link and produce the stub
    // FUTURE: Do we have to provide the loader heap ?
    pStub = psl->Link(SystemDomain::GetGlobalLoaderAllocator()->GetExecutableHeap());

    // Grab the offset of the RemotingLabel and RecheckLabel
    // for use in CNonVirtualThunkMgr::DoTraceStub and
    // TraceManager.
    DWORD dwOffset;

    dwOffset = psl->GetLabelOffset(pRemotingLabel);
    ASSERT(!g_dwNonVirtualThunkRemotingLabelOffset || g_dwNonVirtualThunkRemotingLabelOffset == dwOffset);
    g_dwNonVirtualThunkRemotingLabelOffset = dwOffset;

    dwOffset = psl->GetLabelOffset(pRecheckLabel);
    ASSERT(!g_dwNonVirtualThunkReCheckLabelOffset || g_dwNonVirtualThunkReCheckLabelOffset == dwOffset);
    g_dwNonVirtualThunkReCheckLabelOffset = dwOffset;

    return (pStub->GetEntryPoint());
}

#endif // HAS_REMOTING_PRECODE

//+----------------------------------------------------------------------------
//
//  Method:     CVirtualThunkMgr::DoTraceStub   public
//
//  Synopsis:   Traces the stub given the starting address
// 
//+----------------------------------------------------------------------------
BOOL CVirtualThunkMgr::DoTraceStub(PCODE stubStartAddress, TraceDestination *trace)
{
    LIMITED_METHOD_CONTRACT;

    // <TODO> implement this </TODO>
    return FALSE;
}

//+----------------------------------------------------------------------------
//
//  Method:     CVirtualThunkMgr::IsThunkByASM  public
//
//  Synopsis:   Check assembly to see if this one of our thunks
// 
//+----------------------------------------------------------------------------
BOOL CVirtualThunkMgr::IsThunkByASM(PCODE startaddr)
{
    LIMITED_METHOD_CONTRACT;

    PTR_BYTE pbCode = PTR_BYTE(startaddr);

    // NOTE: this depends on the code generated by
    // CTPMethodTable::CreateThunkForVirtualMethod.

        // mov  r10, <dwSlot>
    return 0x49 == pbCode[0]
        && 0xc7 == pbCode[1]
        && 0xc2 == pbCode[2]
        // mov rax, TransparentProxyStub
        && 0x48 == pbCode[7]
        && 0xb8 == pbCode[8]
        && (TADDR)TransparentProxyStub == *PTR_TADDR(pbCode+9)
        // jmp rax
        && 0xff == pbCode[17]
        && 0xe0 == pbCode[18];
}

//+----------------------------------------------------------------------------
//
//  Method:     CVirtualThunkMgr::GetMethodDescByASM   public
//
//  Synopsis:   Parses MethodDesc out of assembly code
//
//+----------------------------------------------------------------------------
MethodDesc *CVirtualThunkMgr::GetMethodDescByASM(PCODE pbThunkCode, MethodTable *pMT)
{
    LIMITED_METHOD_CONTRACT;

    // NOTE: this depends on the code generated by
    // CTPMethodTable::CreateThunkForVirtualMethod.

    return pMT->GetMethodDescForSlot(*((DWORD *) (pbThunkCode + 3)));
}


#ifndef HAS_REMOTING_PRECODE

//+----------------------------------------------------------------------------
//
//  Method:     CNonVirtualThunkMgr::TraceManager   public
//
//  Synopsis:   Traces the stub given the current context
// 
//+----------------------------------------------------------------------------
BOOL CNonVirtualThunkMgr::TraceManager(Thread* thread,
                                       TraceDestination* trace,
                                       CONTEXT* pContext,
                                       BYTE** pRetAddr)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(thread, NULL_OK));
        PRECONDITION(CheckPointer(trace));
        PRECONDITION(CheckPointer(pContext));
        PRECONDITION(CheckPointer(pRetAddr));
    }
    CONTRACTL_END;

    BOOL bRet = FALSE;

    MethodDesc * pMD = GetMethodDescByASM(GetIP(pContext) - g_dwNonVirtualThunkReCheckLabelOffset);

    LPBYTE pThis = (LPBYTE)pContext->Rcx;

    if ((pThis != NULL) &&
        (*(LPBYTE*)(SIZE_T)pThis == (LPBYTE)(SIZE_T)CTPMethodTable::GetMethodTable()))
    {
        // <TODO>We know that we've got a proxy
        // in the way. If the proxy is to a remote call, with no
        // managed code in between, then the debugger doesn't care and
        // we should just be able to return FALSE.
        //
        // </TODO>
        bRet = FALSE;
    }
    else
    {
        // No proxy in the way, so figure out where we're really going
        // to and let the stub manager try to pickup the trace from
        // there.
        LPBYTE stubStartAddress = (LPBYTE)GetIP(pContext) -
            g_dwNonVirtualThunkReCheckLabelOffset;
        
        // Extract the address of the destination
        BYTE* pbAddr = (BYTE *)(SIZE_T)(stubStartAddress +
                                g_dwNonVirtualThunkRemotingLabelOffset - 2 - sizeof(void *));

        SIZE_T destAddress = *(SIZE_T *)pbAddr;

        // Ask the stub manager to trace the destination address
        bRet = StubManager::TraceStub((PCODE)(BYTE *)(size_t)destAddress, trace);
    }

    // While we may have made it this far, further tracing may reveal
    // that the debugger can't continue on. Therefore, since there is
    // no frame currently pushed, we need to tell the debugger where
    // we're returning to just in case it hits such a situtation.  We
    // know that the return address is on the top of the thread's
    // stack.
    (*pRetAddr) = *((BYTE**)(size_t)(GetSP(pContext)));

    return bRet;
}

//+----------------------------------------------------------------------------
//
//  Method:     CNonVirtualThunkMgr::DoTraceStub   public
//
//  Synopsis:   Traces the stub given the starting address
// 
//+----------------------------------------------------------------------------
BOOL CNonVirtualThunkMgr::DoTraceStub(PCODE stubStartAddress,
                                      TraceDestination* trace)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(stubStartAddress != NULL);
        PRECONDITION(CheckPointer(trace));
    }
    CONTRACTL_END;

    BOOL bRet = FALSE;

    if (!IsThunkByASM(stubStartAddress))
        return FALSE;

    CNonVirtualThunk* pThunk = FindThunk((const BYTE *)stubStartAddress);

    if(NULL != pThunk)
    {
        // We can either jump to 
        // (1) a slot in the transparent proxy table (UNMANAGED)
        // (2) a slot in the non virtual part of the vtable
        // ... so, we need to return TRACE_MGR_PUSH with the address
        // at which we want to be called back with the thread's context
        // so we can figure out which way we're gonna go.
        if((const BYTE *)stubStartAddress == pThunk->GetThunkCode())
        {
            trace->InitForManagerPush(
                (PCODE) (stubStartAddress + g_dwNonVirtualThunkReCheckLabelOffset),
                this);
            bRet = TRUE;
        }
    }

    return bRet;
}

//+----------------------------------------------------------------------------
//
//  Method:     CNonVirtualThunkMgr::IsThunkByASM  public
//
//  Synopsis:   Check assembly to see if this one of our thunks
// 
//+----------------------------------------------------------------------------
BOOL CNonVirtualThunkMgr::IsThunkByASM(PCODE startaddr)
{
    LIMITED_METHOD_CONTRACT;

    PTR_BYTE pbCode = PTR_BYTE(startaddr);

    // test rcx, rcx ; 3 bytes
    return 0x48 == pbCode[0]
        && 0x85 == pbCode[1]
        && 0xc9 == pbCode[2]
        // je ...  ; 2 bytes
        && 0x74 == pbCode[3]
        // mov rax, [rcx]  ; 3 bytes
        // mov r10, CTPMethodTable::GetMethodTable()  ; 2 bytes + MethodTable*
        && (TADDR)CTPMethodTable::GetMethodTable() == *PTR_TADDR(pbCode + 10);
}

//+----------------------------------------------------------------------------
//
//  Method:     CNonVirtualThunkMgr::GetMethodDescByASM   public
//
//  Synopsis:   Parses MethodDesc out of assembly code
// 
//+----------------------------------------------------------------------------
MethodDesc* CNonVirtualThunkMgr::GetMethodDescByASM(PCODE pbThunkCode)
{
    LIMITED_METHOD_CONTRACT;

    return *((MethodDesc **) (pbThunkCode + g_dwNonVirtualThunkRemotingLabelOffset + 2));
}

#endif // HAS_REMOTING_PRECODE


//+----------------------------------------------------------------------------
//
//  Method:     CTPMethodTable::GenericCheckForContextMatch private
//
//  Synopsis:   Calls the stub in the TP & returns TRUE if the contexts
//              match, FALSE otherwise.
//
//  Note:       1. Called during FieldSet/Get, used for proxy extensibility
// 
//+----------------------------------------------------------------------------
BOOL __stdcall CTPMethodTable__GenericCheckForContextMatch(Object* orTP)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;    // due to the Object parameter
        SO_TOLERANT;
    }
    CONTRACTL_END;

    Object *StubData = OBJECTREFToObject(((TransparentProxyObject*)orTP)->GetStubData());
    CTPMethodTable::CheckContextCrossingProc *pfnCheckContextCrossing =
            (CTPMethodTable::CheckContextCrossingProc*)(((TransparentProxyObject*)orTP)->GetStub());
    return pfnCheckContextCrossing(StubData) == 0;
}

#endif // FEATURE_REMOTING


