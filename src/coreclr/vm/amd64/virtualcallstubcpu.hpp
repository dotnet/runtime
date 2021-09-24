// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: AMD64/VirtualCallStubCpu.hpp
//



//

// See code:VirtualCallStubManager for details
//
// ============================================================================

#ifndef _VIRTUAL_CALL_STUB_AMD64_H
#define _VIRTUAL_CALL_STUB_AMD64_H

#define DISPATCH_STUB_FIRST_WORD 0x8B48
#define DISPATCH_STUB_THIRD_BYTE 0x05
#define RESOLVE_STUB_FIRST_WORD 0x4C52
#define LOOKUP_STUB_FIRST_WORD 0x35FF
#define VTABLECALL_STUB_FIRST_WORD 0x8B48

#include "dbginterface.h"

//#define STUB_LOGGING
#pragma pack(push, 1)

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

        size_t cbSize = 3;                                      // First mov instruction
        cbSize += (pStubCode[cbSize + 2] == 0x80 ? 7 : 4);      // Either 48 8B 80 or 48 8B 40: mov rax,[rax+offset]
        cbSize += (pStubCode[cbSize + 1] == 0xa0 ? 6 : 3);      // Either FF A0 or FF 60: jmp qword ptr [rax+slot]
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
        return 3 + (offsetOfIndirection >= 0x80 ? 7 : 4) + (offsetAfterIndirection >= 0x80 ? 6 : 3) + 4;
    }

    static VTableCallHolder* FromVTableCallEntry(PCODE entry) { LIMITED_METHOD_CONTRACT; return (VTableCallHolder*)entry; }

private:
    // VTableCallStub follows here. It is dynamically sized on allocation because it could
    // use short/long instruction sizes for mov/jmp, depending on the slot value.
};
#pragma pack(pop)

#ifdef DECLARE_DATA

#define INSTR_INT3 0xcc
#define INSTR_NOP  0x90

#ifndef DACCESS_COMPILE

#include "asmconstants.h"

#endif // DACCESS_COMPILE

void VTableCallHolder::Initialize(unsigned slot)
{
    unsigned offsetOfIndirection = MethodTable::GetVtableOffset() + MethodTable::GetIndexOfVtableIndirection(slot) * TARGET_POINTER_SIZE;
    unsigned offsetAfterIndirection = MethodTable::GetIndexAfterVtableIndirection(slot) * TARGET_POINTER_SIZE;

    VTableCallStub* pStub = stub();
    BYTE* p = (BYTE*)pStub->entryPoint();

#ifdef UNIX_AMD64_ABI
    // mov rax,[rdi] : rax = MethodTable pointer
    *(UINT32 *)p = 0x078b48; p += 3;
#else
    // mov rax,[rcx] : rax = MethodTable pointer
    *(UINT32 *)p = 0x018b48; p += 3;
#endif

    // mov rax,[rax+vtable offset] : rax = vtable pointer
    if (offsetOfIndirection >= 0x80)
    {
        *(UINT32*)p = 0x00808b48; p += 3;
        *(UINT32*)p = offsetOfIndirection; p += 4;
    }
    else
    {
        *(UINT32*)p = 0x00408b48; p += 3;
        *p++ = (BYTE)offsetOfIndirection;
    }

    // jmp qword ptr [rax+slot]
    if (offsetAfterIndirection >= 0x80)
    {
        *(UINT32*)p = 0xa0ff; p += 2;
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

VirtualCallStubManager::StubKind VirtualCallStubManager::predictStubKind(PCODE stubStartAddress)
{
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

        if (firstWord == DISPATCH_STUB_FIRST_WORD && *((BYTE*)stubStartAddress + 2) == DISPATCH_STUB_THIRD_BYTE)
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

            if ((firstByte  == INSTR_INT3) || (secondByte == INSTR_INT3))
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

#endif // _VIRTUAL_CALL_STUB_AMD64_H
