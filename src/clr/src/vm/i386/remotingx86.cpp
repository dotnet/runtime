// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
//
// 
// File: remotingx86.cpp
// 

// 
// 
// Purpose: Defines various remoting related functions for the x86 architecture
//

//
// 

//

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
#include "interoputil.h"
#include "virtualcallstub.h"

#ifdef FEATURE_COMINTEROP 
#include "comcallablewrapper.h"
#include "comcache.h"
#endif // FEATURE_COMINTEROP

//+----------------------------------------------------------------------------
//
//  Method:     CTPMethodTable::CreateThunkForVirtualMethod   private
//
//  Synopsis:   Creates the thunk that pushes the supplied slot number and jumps
//              to TP Stub
//
//+----------------------------------------------------------------------------
PCODE CTPMethodTable::CreateThunkForVirtualMethod(DWORD dwSlot, BYTE *startaddr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(startaddr));
    }
    CONTRACTL_END;

    BYTE *pCode = startaddr;

    // 0000   B8 67 45 23 01     MOV  EAX, dwSlot
    // 0005   E9 ?? ?? ?? ??     JMP  TransparentProxyStub
    *pCode++ = 0xB8;
    *((DWORD *) pCode) = dwSlot;
    pCode += sizeof(DWORD);
    *pCode++ = 0xE9;
    // self-relative call, based on the start of the next instruction.
    *((LONG *) pCode) = (LONG)((size_t)GetTPStubEntryPoint() - (size_t) (pCode + sizeof(LONG)));

    _ASSERTE(CVirtualThunkMgr::IsThunkByASM((PCODE)startaddr));

    return (PCODE)startaddr;
}


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

    // Before activation:
    // 0000 C3                  ret
    // 0001 90                  nop

    // After activation:
    // 0000 85 C9               test     ecx,ecx

    // 0002 74 XX               je       RemotingDone
    // 0004 81 39 XX XX XX XX   cmp      dword ptr [ecx],11111111h
    // 000A 74 XX               je       RemotingCheck

    // Switch offset and size of patch based on the jump opcode used.
    BYTE* pCode = (BYTE*)PrecodeRemotingThunk;

    SIZE_T mtOffset = 0x0006;
    SIZE_T size = 0x000A;

    // Patch "ret + nop" to "test ecx,ecx"
    *(UINT16 *)pCode = 0xC985;

    // Replace placeholder value with the actual address of TP method table
    _ASSERTE(*(PVOID*)(pCode+mtOffset) == (PVOID*)0x11111111);
    *(PVOID*)(pCode+mtOffset) = GetMethodTable();

    FlushInstructionCache(GetCurrentProcess(), pCode, size);
}

//+----------------------------------------------------------------------------
//
//  Method:     CVirtualThunkMgr::DoTraceStub   public
//
//  Synopsis:   Traces the stub given the starting address
//
//+----------------------------------------------------------------------------
BOOL CVirtualThunkMgr::DoTraceStub(PCODE stubStartAddress, TraceDestination *trace)
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

    BOOL bIsStub = FALSE;

    // Find a thunk whose code address matching the starting address
    LPBYTE pThunk = FindThunk((LPBYTE)stubStartAddress);
    if(NULL != pThunk)
    {
        LPBYTE pbAddr = NULL;
        LONG destAddress = 0;
        if((LPBYTE)stubStartAddress == pThunk)
        {

            // Extract the long which gives the self relative address
            // of the destination
            pbAddr = pThunk + sizeof(BYTE) + sizeof(DWORD) + sizeof(BYTE);
            destAddress = *(LONG *)pbAddr;

            // Calculate the absolute address by adding the offset of the next
            // instruction after the call instruction
            destAddress += (LONG)(size_t)(pbAddr + sizeof(LONG));

        }

        // We cannot tell where the stub will end up until OnCall is reached.
        // So we tell the debugger to run till OnCall is reached and then
        // come back and ask us again for the actual destination address of
        // the call

        Stub *stub = Stub::RecoverStub((TADDR)destAddress);

        trace->InitForFramePush(stub->GetPatchAddress());
        bIsStub = TRUE;
    }

    return bIsStub;
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
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(startaddr != NULL);
    }
    CONTRACTL_END;

    PTR_BYTE pbCode = PTR_BYTE(startaddr);

    return ((pbCode[0] == 0xB8) &&
            (pbCode[5] == 0xe9) &&
            (rel32Decode((TADDR)(pbCode + 6)) == CTPMethodTable::GetTPStubEntryPoint()));
}

//+----------------------------------------------------------------------------
//
//  Method:     CVirtualThunkMgr::GetMethodDescByASM   public
//
//  Synopsis:   Parses MethodDesc out of assembly code
//
//+----------------------------------------------------------------------------
MethodDesc *CVirtualThunkMgr::GetMethodDescByASM(PCODE startaddr, MethodTable *pMT)
{
    CONTRACT (MethodDesc*)
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(startaddr != NULL);
        PRECONDITION(CheckPointer(pMT));
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    RETURN (pMT->GetMethodDescForSlot(*((DWORD *) (startaddr + 1))));
}

#endif// FEATURE_REMOTING

