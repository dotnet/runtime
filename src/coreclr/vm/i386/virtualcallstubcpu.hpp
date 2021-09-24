// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: virtualcallstubcpu.hpp
//


//

//
// ============================================================================

#ifndef _VIRTUAL_CALL_STUB_X86_H
#define _VIRTUAL_CALL_STUB_X86_H

#define DISPATCH_STUB_FIRST_WORD 0xa150
#define RESOLVE_STUB_FIRST_WORD 0x2d83
#define LOOKUP_STUB_FIRST_WORD 0xff50
#define VTABLECALL_STUB_FIRST_WORD 0x018b

#ifdef DECLARE_DATA
#include "asmconstants.h"
#endif

#include <pshpack1.h>  // Since we are placing code, we want byte packing of the structs

/*VTableCallStub**************************************************************************************
These are jump stubs that perform a vtable-base virtual call. These stubs assume that an object is placed
in the first argument register (this pointer). From there, the stub extracts the MethodTable pointer, followed by the
vtable pointer, and finally jumps to the target method at a given slot in the vtable.
*/
struct VTableCallStub
{
    friend struct VTableCallHolder;

    inline size_t size()
    {
        LIMITED_METHOD_CONTRACT;

        BYTE* pStubCode = (BYTE *)this;

        size_t cbSize = 2;                                      // First mov instruction
        cbSize += (pStubCode[cbSize + 1] == 0x80 ? 6 : 3);      // Either 8B 80 or 8B 40: mov eax,[eax+offset]
        cbSize += (pStubCode[cbSize + 1] == 0xa0 ? 6 : 3);      // Either FF A0 or FF 60: jmp dword ptr [eax+slot]
        cbSize += 4;                                            // Slot value (data storage, not a real instruction)

        return cbSize;
    }

    inline PCODE        entryPoint()        const { LIMITED_METHOD_CONTRACT;  return (PCODE)&_entryPoint[0]; }

    inline size_t token()
    {
        LIMITED_METHOD_CONTRACT;
        DWORD slot = *(DWORD*)(reinterpret_cast<BYTE*>(this) + size() - 4);
        return DispatchToken::CreateDispatchToken(slot).To_SIZE_T();
    }

private:
    BYTE    _entryPoint[0];         // Dynamically sized stub. See Initialize() for more details.
};

/* VTableCallHolders are the containers for VTableCallStubs, they provide for any alignment of
stubs as necessary.  */
struct VTableCallHolder
{
    void  Initialize(unsigned slot);

    VTableCallStub* stub() { LIMITED_METHOD_CONTRACT;  return reinterpret_cast<VTableCallStub *>(this); }

    size_t size() { return stub()->size(); }
    static size_t GetHolderSize(unsigned slot)
    {
        STATIC_CONTRACT_WRAPPER;
        unsigned offsetOfIndirection = MethodTable::GetVtableOffset() + MethodTable::GetIndexOfVtableIndirection(slot) * TARGET_POINTER_SIZE;
        unsigned offsetAfterIndirection = MethodTable::GetIndexAfterVtableIndirection(slot) * TARGET_POINTER_SIZE;
        return 2 + (offsetOfIndirection >= 0x80 ? 6 : 3) + (offsetAfterIndirection >= 0x80 ? 6 : 3) + 4;
    }

    static VTableCallHolder* FromVTableCallEntry(PCODE entry) { LIMITED_METHOD_CONTRACT; return (VTableCallHolder*)entry; }

private:
    // VTableCallStub follows here. It is dynamically sized on allocation because it could
    // use short/long instruction sizes for the mov/jmp, depending on the slot value.
};

#include <poppack.h>


#ifdef DECLARE_DATA

#ifndef DACCESS_COMPILE

#ifdef _MSC_VER

#ifdef CHAIN_LOOKUP
/* This will perform a chained lookup of the entry if the initial cache lookup fails

   Entry stack:
            dispatch token
            siteAddrForRegisterIndirect (used only if this is a RegisterIndirect dispatch call)
            return address of caller to stub
        Also, EAX contains the pointer to the first ResolveCacheElem pointer for the calculated
        bucket in the cache table.
*/
__declspec (naked) void ResolveWorkerChainLookupAsmStub()
{
    enum
    {
        e_token_size                = 4,
        e_indirect_addr_size        = 4,
        e_caller_ret_addr_size      = 4,
    };
    enum
    {
        // this is the part of the stack that is present as we enter this function:
        e_token                     = 0,
        e_indirect_addr             = e_token + e_token_size,
        e_caller_ret_addr           = e_indirect_addr + e_indirect_addr_size,
        e_ret_esp                   = e_caller_ret_addr + e_caller_ret_addr_size,
    };
    enum
    {
        e_spilled_reg_size          = 8,
    };

    // main loop setup
    __asm {
#ifdef STUB_LOGGING
        inc     g_chained_lookup_call_counter
#endif
        // spill regs
        push    edx
        push    ecx
        // move the token into edx
        mov     edx,[esp+e_spilled_reg_size+e_token]
        // move the MT into ecx
        mov     ecx,[ecx]
    }
    main_loop:
    __asm {
        // get the next entry in the chain (don't bother checking the first entry again)
        mov     eax,[eax+e_resolveCacheElem_offset_next]
        // test if we hit a terminating NULL
        test    eax,eax
        jz      fail
        // compare the MT of the ResolveCacheElem
        cmp     ecx,[eax+e_resolveCacheElem_offset_mt]
        jne     main_loop
        // compare the token of the ResolveCacheElem
        cmp     edx,[eax+e_resolveCacheElem_offset_token]
        jne     main_loop
        // success
        // decrement success counter and move entry to start if necessary
        sub     g_dispatch_cache_chain_success_counter,1
        //@TODO: Perhaps this should be a jl for better branch prediction?
        jge     nopromote
        // be quick to reset the counter so we don't get a bunch of contending threads
        add     g_dispatch_cache_chain_success_counter,CALL_STUB_CACHE_INITIAL_SUCCESS_COUNT
        // promote the entry to the beginning of the chain
        mov     ecx,eax
        call    VirtualCallStubManager::PromoteChainEntry
    }
    nopromote:
    __asm {
        // clean up the stack and jump to the target
        pop     ecx
        pop     edx
        add     esp,(e_caller_ret_addr - e_token)
        mov     eax,[eax+e_resolveCacheElem_offset_target]
        jmp     eax
    }
    fail:
    __asm {
#ifdef STUB_LOGGING
        inc     g_chained_lookup_miss_counter
#endif
        // restore registers
        pop     ecx
        pop     edx
        jmp     ResolveWorkerAsmStub
    }
}
#endif

/* Call the resolver, it will return where we are supposed to go.
   There is a little stack magic here, in that we are entered with one
   of the arguments for the resolver (the token) on the stack already.
   We just push the other arguments, <this> in the call frame and the call site pointer,
   and call the resolver.

   On return we have the stack frame restored to the way it was when the ResolveStub
   was called, i.e. as it was at the actual call site.  The return value from
   the resolver is the address we need to transfer control to, simulating a direct
   call from the original call site.  If we get passed back NULL, it means that the
   resolution failed, an unimpelemented method is being called.

   Entry stack:
            dispatch token
            siteAddrForRegisterIndirect (used only if this is a RegisterIndirect dispatch call)
            return address of caller to stub

   Call stack:
            pointer to TransitionBlock
            call site
            dispatch token
            TransitionBlock
                ArgumentRegisters (ecx, edx)
                CalleeSavedRegisters (ebp, ebx, esi, edi)
            return address of caller to stub
   */
__declspec (naked) void ResolveWorkerAsmStub()
{
    CANNOT_HAVE_CONTRACT;

    __asm {
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

        push        [esi + 4*4]     // dispatch token
        push        [esi + 5*4]     // siteAddrForRegisterIndirect
        push        esi             // pTransitionBlock

        // Setup up proper EBP frame now that the stub arguments can be trashed
        mov         [esi + 4*4],ebx
        mov         [esi + 5*4],ebp
        lea         ebp, [esi + 5*4]

        // Make the call
        call        VSD_ResolveWorker

        // From here on, mustn't trash eax

        // pop ArgumentRegisters
        pop     edx
        pop     ecx

        // pop CalleeSavedRegisters
        pop edi
        pop esi
        pop ebx
        pop ebp

        // Now jump to the target
        jmp     eax             // continue on into the method
    }
}


/* Call the callsite back patcher.  The fail stub piece of the resolver is being
call too often, i.e. dispatch stubs are failing the expect MT test too often.
In this stub wraps the call to the BackPatchWorker to take care of any stack magic
needed.
*/
__declspec (naked) void BackPatchWorkerAsmStub()
{
    CANNOT_HAVE_CONTRACT;

    __asm {
        push EBP
        mov ebp,esp
        push EAX        // it may contain siteAddrForRegisterIndirect
        push ECX
        push EDX
        push EAX        //  push any indirect call address as the second arg to BackPatchWorker
        push [EBP+8]    //  and push return address as the first arg to BackPatchWorker
        call VirtualCallStubManager::BackPatchWorkerStatic
        pop EDX
        pop ECX
        pop EAX
        mov esp,ebp
        pop ebp
        ret
    }
}

#endif // _MSC_VER

#ifdef _DEBUG
//
// This function verifies that a pointer to an indirection cell lives inside a delegate object.
// In the delegate case the indirection cell is held by the delegate itself in _methodPtrAux, when the delegate Invoke is
// called the shuffle thunk is first invoked and that will call into the virtual dispatch stub.
// Before control is given to the virtual dispatch stub a pointer to the indirection cell (thus an interior pointer to the delegate)
// is pushed in EAX
//
BOOL isDelegateCall(BYTE *interiorPtr)
{
    LIMITED_METHOD_CONTRACT;

    if (GCHeapUtilities::GetGCHeap()->IsHeapPointer((void*)interiorPtr))
    {
        Object *delegate = (Object*)(interiorPtr - DelegateObject::GetOffsetOfMethodPtrAux());
        VALIDATEOBJECTREF(ObjectToOBJECTREF(delegate));
        _ASSERTE(delegate->GetMethodTable()->IsDelegate());

        return TRUE;
    }
    return FALSE;
}
#endif

StubCallSite::StubCallSite(TADDR siteAddrForRegisterIndirect, PCODE returnAddr)
{
    LIMITED_METHOD_CONTRACT;

    // Not used
    // if (isCallRelative(returnAddr))
    // {
    //      m_siteAddr = returnAddr - sizeof(DISPL);
    // }
    // else
    if (isCallRelativeIndirect((BYTE *)returnAddr))
    {
        m_siteAddr = *dac_cast<PTR_PTR_PCODE>(returnAddr - sizeof(PCODE));
    }
    else
    {
        _ASSERTE(isCallRegisterIndirect((BYTE *)returnAddr) || isDelegateCall((BYTE *)siteAddrForRegisterIndirect));
        m_siteAddr = dac_cast<PTR_PCODE>(siteAddrForRegisterIndirect);
    }
}

// the special return address for VSD tailcalls
extern "C" void STDCALL JIT_TailCallReturnFromVSD();

PCODE StubCallSite::GetCallerAddress()
{
    LIMITED_METHOD_CONTRACT;

#ifdef UNIX_X86_ABI
    return m_returnAddr;
#else // UNIX_X86_ABI
    if (m_returnAddr != (PCODE)JIT_TailCallReturnFromVSD)
        return m_returnAddr;

    // Find the tailcallframe in the frame chain and get the actual caller from the first TailCallFrame
    return TailCallFrame::FindTailCallFrame(GetThread()->GetFrame())->GetCallerAddress();
#endif // UNIX_X86_ABI
}

void VTableCallHolder::Initialize(unsigned slot)
{
    unsigned offsetOfIndirection = MethodTable::GetVtableOffset() + MethodTable::GetIndexOfVtableIndirection(slot) * TARGET_POINTER_SIZE;
    unsigned offsetAfterIndirection = MethodTable::GetIndexAfterVtableIndirection(slot) * TARGET_POINTER_SIZE;

    VTableCallStub* pStub = stub();
    BYTE* p = (BYTE*)pStub->entryPoint();

    // mov eax,[ecx] : eax = MethodTable pointer
    *(UINT16*)p = 0x018b; p += 2;

    // mov eax,[eax+vtable offset] : eax = vtable pointer
    if (offsetOfIndirection >= 0x80)
    {
        *(UINT16*)p = 0x808b; p += 2;
        *(UINT32*)p = offsetOfIndirection; p += 4;
    }
    else
    {
        *(UINT16*)p = 0x408b; p += 2;
        *p++ = (BYTE)offsetOfIndirection;
    }

    // jmp dword ptr [eax+slot]
    if (offsetAfterIndirection >= 0x80)
    {
        *(UINT16*)p = 0xa0ff; p += 2;
        *(UINT32*)p = offsetAfterIndirection; p += 4;
    }
    else
    {
        *(UINT16*)p = 0x60ff; p += 2;
        *p++ = (BYTE)offsetAfterIndirection;
    }

    // Store the slot value here for convenience. Not a real instruction (unreachable anyways)
    *(UINT32*)p = slot; p += 4;

    _ASSERT(p == (BYTE*)stub()->entryPoint() + VTableCallHolder::GetHolderSize(slot));
    _ASSERT(stub()->size() == VTableCallHolder::GetHolderSize(slot));
}

#endif // DACCESS_COMPILE

VirtualCallStubManager::StubKind VirtualCallStubManager::predictStubKind(PCODE stubStartAddress)
{
    SUPPORTS_DAC;
#ifdef DACCESS_COMPILE

    return SK_BREAKPOINT;  // Dac always uses the slower lookup

#else

    StubKind stubKind = SK_UNKNOWN;

    EX_TRY
    {
        // If stubStartAddress is completely bogus, then this might AV,
        // so we protect it with SEH. An AV here is OK.
        AVInRuntimeImplOkayHolder AVOkay;

        WORD firstWord = *((WORD*) stubStartAddress);

#ifndef STUB_LOGGING
        if (firstWord == DISPATCH_STUB_FIRST_WORD)
#else //STUB_LOGGING
#error
        if (firstWord == 0x05ff)
#endif
        {
            stubKind = SK_DISPATCH;
        }
        else if (firstWord == LOOKUP_STUB_FIRST_WORD)
        {
            stubKind = SK_LOOKUP;
        }
        else if (firstWord == RESOLVE_STUB_FIRST_WORD)
        {
            stubKind = SK_RESOLVE;
        }
        else if (firstWord == VTABLECALL_STUB_FIRST_WORD)
        {
            stubKind = SK_VTABLECALL;
        }
        else
        {
            BYTE firstByte  = ((BYTE*) stubStartAddress)[0];
            BYTE secondByte = ((BYTE*) stubStartAddress)[1];

            if ((firstByte  == X86_INSTR_INT3) ||
                (secondByte == X86_INSTR_INT3))
            {
                stubKind = SK_BREAKPOINT;
            }
        }
    }
    EX_CATCH
    {
        stubKind = SK_UNKNOWN;
    }
    EX_END_CATCH(SwallowAllExceptions);

    return stubKind;

#endif // DACCESS_COMPILE
}

#endif //DECLARE_DATA

#endif // _VIRTUAL_CALL_STUB_X86_H
