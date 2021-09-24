// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// VirtualCallStubCpu.hpp
//
#ifndef _VIRTUAL_CALL_STUB_ARM_H
#define _VIRTUAL_CALL_STUB_ARM_H

#ifdef DECLARE_DATA
#include "asmconstants.h"
#endif

//#define STUB_LOGGING

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

        size_t cbSize = 4;                                      // First ldr instruction

        // If we never save r0 to the red zone, we have the short version of the stub
        if (*(UINT32*)(&pStubCode[cbSize]) != 0x0c04f84d)
        {
            return
                4 +         // ldr r12,[r0]
                4 +         // ldr r12,[r12+offset]
                4 +         // ldr r12,[r12+offset]
                2 +         // bx r12
                4;          // Slot value (data storage, not a real instruction)
        }

        cbSize += 4;                                                    // Saving r0 into red zone
        cbSize += (*(WORD*)(&pStubCode[cbSize]) == 0xf8dc ? 4 : 12);    // Loading of vtable into r12
        cbSize += (*(WORD*)(&pStubCode[cbSize]) == 0xf8dc ? 4 : 12);    // Loading of targe address into r12

        return cbSize + 6 /* Restore r0, bx*/ + 4 /* Slot value */;
    }

    inline PCODE entryPoint() const { LIMITED_METHOD_CONTRACT; return (PCODE)&_entryPoint[0] + THUMB_CODE; }

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

        int indirectionsSize = (offsetOfIndirection > 0xFFF ? 12 : 4) + (offsetAfterIndirection > 0xFFF ? 12 : 4);
        if (offsetOfIndirection > 0xFFF || offsetAfterIndirection > 0xFFF)
            indirectionsSize += 8;    // Save/restore r0 using red zone

        return 6 + indirectionsSize + 4;
    }

    static VTableCallHolder* FromVTableCallEntry(PCODE entry)
    {
        LIMITED_METHOD_CONTRACT;
        return (VTableCallHolder*)(entry & ~THUMB_CODE);
    }

private:
    // VTableCallStub follows here. It is dynamically sized on allocation because it could
    // use short/long instruction sizes for the mov/jmp, depending on the slot value.
};

#include <poppack.h>


#ifdef DECLARE_DATA

#ifndef DACCESS_COMPILE

TADDR StubDispatchFrame_MethodFrameVPtr;

void MovRegImm(BYTE* p, int reg, TADDR imm);

void VTableCallHolder::Initialize(unsigned slot)
{
    unsigned offsetOfIndirection = MethodTable::GetVtableOffset() + MethodTable::GetIndexOfVtableIndirection(slot) * TARGET_POINTER_SIZE;
    unsigned offsetAfterIndirection = MethodTable::GetIndexAfterVtableIndirection(slot) * TARGET_POINTER_SIZE;

    VTableCallStub* pStub = stub();
    BYTE* p = (BYTE*)(pStub->entryPoint() & ~THUMB_CODE);

    // ldr r12,[r0] : r12 = MethodTable pointer
    *(UINT32*)p = 0xc000f8d0; p += 4;

    if (offsetOfIndirection > 0xFFF || offsetAfterIndirection > 0xFFF)
    {
        // str r0, [sp, #-4]. Save r0 in the red zone
        *(UINT32*)p = 0x0c04f84d; p += 4;
    }

    if (offsetOfIndirection > 0xFFF)
    {
        // mov r0, offsetOfIndirection
        MovRegImm(p, 0, offsetOfIndirection); p += 8;
        // ldr r12, [r12, r0]
        *(UINT32*)p = 0xc000f85c; p += 4;
    }
    else
    {
        // ldr r12, [r12 + offset]
        *(WORD *)p = 0xf8dc; p += 2;
        *(WORD *)p = (WORD)(offsetOfIndirection | 0xc000); p += 2;
    }

    if (offsetAfterIndirection > 0xFFF)
    {
        // mov r0, offsetAfterIndirection
        MovRegImm(p, 0, offsetAfterIndirection); p += 8;
        // ldr r12, [r12, r0]
        *(UINT32*)p = 0xc000f85c; p += 4;
    }
    else
    {
        // ldr r12, [r12 + offset]
        *(WORD *)p = 0xf8dc; p += 2;
        *(WORD *)p = (WORD)(offsetAfterIndirection | 0xc000); p += 2;
    }

    if (offsetOfIndirection > 0xFFF || offsetAfterIndirection > 0xFFF)
    {
        // ldr r0, [sp, #-4]. Restore r0 from the red zone.
        *(UINT32*)p = 0x0c04f85d; p += 4;
    }

    // bx r12
    *(UINT16*)p = 0x4760; p += 2;

    // Store the slot value here for convenience. Not a real instruction (unreachable anyways)
    *(UINT32*)p = slot; p += 4;

    _ASSERT(p == (BYTE*)(stub()->entryPoint() & ~THUMB_CODE) + VTableCallHolder::GetHolderSize(slot));
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
    TADDR pInstr = PCODEToPINSTR(stubStartAddress);

    EX_TRY
    {
        // If stubStartAddress is completely bogus, then this might AV,
        // so we protect it with SEH. An AV here is OK.
        AVInRuntimeImplOkayHolder AVOkay;

        WORD firstWord = *((WORD*) pInstr);

        if (*((UINT32*)pInstr) == 0xc000f8d0)
        {
            // Confirm the thrid word belongs to the vtable stub pattern
            WORD thirdWord = ((WORD*)pInstr)[2];
            if (thirdWord == 0xf84d /* Part of str r0, [sp, #-4] */  ||
                thirdWord == 0xf8dc /* Part of ldr r12, [r12 + offset] */)
                stubKind = SK_VTABLECALL;
        }

        if (stubKind == SK_UNKNOWN)
        {
            //Assuming that RESOLVE_STUB_FIRST_WORD & DISPATCH_STUB_FIRST_WORD have same values
            _ASSERTE(RESOLVE_STUB_FIRST_WORD == DISPATCH_STUB_FIRST_WORD);
            if (firstWord == DISPATCH_STUB_FIRST_WORD)
            {
                WORD thirdWord = ((WORD*)pInstr)[2];
                if (thirdWord == DISPATCH_STUB_THIRD_WORD)
                {
                    stubKind = SK_DISPATCH;
                }
                else if (thirdWord == RESOLVE_STUB_THIRD_WORD)
                {
                    stubKind = SK_RESOLVE;
                }
            }
            else if (firstWord == LOOKUP_STUB_FIRST_WORD)
            {
                stubKind = SK_LOOKUP;
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

#endif // _VIRTUAL_CALL_STUB_ARM_H
