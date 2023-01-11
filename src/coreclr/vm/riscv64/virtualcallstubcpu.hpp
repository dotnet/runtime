// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// VirtualCallStubCpu.hpp
//
#ifndef _VIRTUAL_CALL_STUB_ARM_H
#define _VIRTUAL_CALL_STUB_ARM_H

#define DISPATCH_STUB_FIRST_DWORD 0x0
#define RESOLVE_STUB_FIRST_DWORD 0x0
#define VTABLECALL_STUB_FIRST_DWORD 0x0

#define LOOKUP_STUB_FIRST_DWORD 0x00000f97

#define USES_LOOKUP_STUBS   1

struct LookupStub
{
    inline PCODE entryPoint() { LIMITED_METHOD_CONTRACT; return (PCODE)&_entryPoint[0]; }
    inline size_t token() { LIMITED_METHOD_CONTRACT; return _token; }
    inline size_t size() { LIMITED_METHOD_CONTRACT; return sizeof(LookupStub); }
private :
    friend struct LookupHolder;

    DWORD _entryPoint[4];
    PCODE _resolveWorkerTarget;
    size_t _token;
};

struct LookupHolder
{
private:
    LookupStub _stub;
public:
    static void InitializeStatic() { }

    void  Initialize(LookupHolder* pLookupHolderRX, PCODE resolveWorkerTarget, size_t dispatchToken)
    {
        // auipc t6, 0
        // ld    t2, (12 + 12)(ra)
        // ld    t6, (4 + 12)(ra)
        // jalr  x0, t6, 0
        //
        // _resolveWorkerTarget
        // _token

        _stub._entryPoint[0] = LOOKUP_STUB_FIRST_DWORD; //auipc ra, 0  //0x00000097
        _stub._entryPoint[1] = 0x018fb383; //ld   t2, 24(ra)
        _stub._entryPoint[2] = 0x010fbf83; //ld   ra, 16(ra)
        _stub._entryPoint[3] = 0x000f8067; //jalr r0, ra, 0

        _stub._resolveWorkerTarget = resolveWorkerTarget;
        _stub._token               = dispatchToken;
    }

    LookupStub*    stub()        { LIMITED_METHOD_CONTRACT; return &_stub; }
    static LookupHolder*  FromLookupEntry(PCODE lookupEntry)
    {
        return (LookupHolder*) ( lookupEntry - offsetof(LookupHolder, _stub) - offsetof(LookupStub, _entryPoint)  );
    }
};

struct DispatchStub
{
    inline PCODE entryPoint()         { LIMITED_METHOD_CONTRACT; return (PCODE)&_entryPoint[0]; }

    inline size_t expectedMT()  { LIMITED_METHOD_CONTRACT; return _expectedMT; }
    inline PCODE implTarget()  { LIMITED_METHOD_CONTRACT; return _implTarget; }

    inline TADDR implTargetSlot(EntryPointSlots::SlotType *slotTypeRef) const
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(slotTypeRef != nullptr);

        *slotTypeRef = EntryPointSlots::SlotType_Executable;
        return (TADDR)&_implTarget;
    }

    inline PCODE failTarget()  { LIMITED_METHOD_CONTRACT; return _failTarget; }
    inline size_t size()        { LIMITED_METHOD_CONTRACT; return sizeof(DispatchStub); }

private:
    friend struct DispatchHolder;

    DWORD _entryPoint[8];
    size_t  _expectedMT;
    PCODE _implTarget;
    PCODE _failTarget;
};

struct DispatchHolder
{
    static void InitializeStatic()
    {
        LIMITED_METHOD_CONTRACT;

        // Check that _implTarget is aligned in the DispatchHolder for backpatching
        static_assert_no_msg(((offsetof(DispatchHolder, _stub) + offsetof(DispatchStub, _implTarget)) % sizeof(void *)) == 0);
    }

    void  Initialize(DispatchHolder* pDispatchHolderRX, PCODE implTarget, PCODE failTarget, size_t expectedMT)
    {
        _ASSERTE(!"RISCV64:NYI");
    }

    DispatchStub* stub()      { LIMITED_METHOD_CONTRACT; return &_stub; }

    static DispatchHolder*  FromDispatchEntry(PCODE dispatchEntry)
    {
        LIMITED_METHOD_CONTRACT;
        DispatchHolder* dispatchHolder = (DispatchHolder*) ( dispatchEntry - offsetof(DispatchHolder, _stub) - offsetof(DispatchStub, _entryPoint) );
        return dispatchHolder;
    }

private:
    DispatchStub _stub;
};

struct ResolveStub
{
    inline PCODE failEntryPoint()            { LIMITED_METHOD_CONTRACT; return (PCODE)&_failEntryPoint[0]; }
    inline PCODE resolveEntryPoint()         { LIMITED_METHOD_CONTRACT; return (PCODE)&_resolveEntryPoint[0]; }
    inline PCODE slowEntryPoint()            { LIMITED_METHOD_CONTRACT; return (PCODE)&_slowEntryPoint[0]; }
    inline size_t  token()                   { LIMITED_METHOD_CONTRACT; return _token; }
    inline INT32*  pCounter()                { LIMITED_METHOD_CONTRACT; return _pCounter; }

    inline UINT32  hashedToken()             { LIMITED_METHOD_CONTRACT; return _hashedToken >> LOG2_PTRSIZE;    }
    inline size_t  cacheAddress()            { LIMITED_METHOD_CONTRACT; return _cacheAddress;   }
    inline size_t  size()                    { LIMITED_METHOD_CONTRACT; return sizeof(ResolveStub); }

private:
    friend struct ResolveHolder;
    const static int resolveEntryPointLen = 0; // TODO RISCV64
    const static int slowEntryPointLen = 0;
    const static int failEntryPointLen = 0;

    DWORD _resolveEntryPoint[resolveEntryPointLen];
    DWORD _slowEntryPoint[slowEntryPointLen];
    DWORD _failEntryPoint[failEntryPointLen];
    INT32*  _pCounter;               //Base of the Data Region
    size_t  _cacheAddress;           // lookupCache
    size_t  _token;
    PCODE   _resolveWorkerTarget;
    UINT32  _hashedToken;
};

struct ResolveHolder
{
    static void  InitializeStatic() { }

    void Initialize(ResolveHolder* pResolveHolderRX,
                    PCODE resolveWorkerTarget, PCODE patcherTarget,
                    size_t dispatchToken, UINT32 hashedToken,
                    void * cacheAddr, INT32 * counterAddr)
    {

        _ASSERTE(!"RISCV64:NYI");
#undef DATA_OFFSET
#undef PC_REL_OFFSET
#undef Dataregionbase
    }

    ResolveStub* stub()      { LIMITED_METHOD_CONTRACT; return &_stub; }

    static ResolveHolder*  FromFailEntry(PCODE failEntry);
    static ResolveHolder*  FromResolveEntry(PCODE resolveEntry);
private:
    ResolveStub _stub;
};


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
        _ASSERTE(!"RISCV64:NYI");
        return 0;
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

    static size_t GetHolderSize(unsigned slot)
    {
        STATIC_CONTRACT_WRAPPER;
        unsigned offsetOfIndirection = MethodTable::GetVtableOffset() + MethodTable::GetIndexOfVtableIndirection(slot) * TARGET_POINTER_SIZE;
        unsigned offsetAfterIndirection = MethodTable::GetIndexAfterVtableIndirection(slot) * TARGET_POINTER_SIZE;
        int indirectionsCodeSize = (offsetOfIndirection >= 0x1000 ? 12 : 4) + (offsetAfterIndirection >= 0x1000 ? 12 : 4);
        int indirectionsDataSize = (offsetOfIndirection >= 0x1000 ? 4 : 0) + (offsetAfterIndirection >= 0x1000 ? 4 : 0);
        return 12 + indirectionsCodeSize + ((indirectionsDataSize > 0) ? (indirectionsDataSize + 4) : 0);
    }

    static VTableCallHolder* FromVTableCallEntry(PCODE entry) { LIMITED_METHOD_CONTRACT; return (VTableCallHolder*)entry; }

private:
    // VTableCallStub follows here. It is dynamically sized on allocation because it could
    // use short/long instruction sizes for LDR, depending on the slot value.
};


#ifdef DECLARE_DATA

#ifndef DACCESS_COMPILE
ResolveHolder* ResolveHolder::FromFailEntry(PCODE failEntry)
{
    LIMITED_METHOD_CONTRACT;
    ResolveHolder* resolveHolder = (ResolveHolder*) ( failEntry - offsetof(ResolveHolder, _stub) - offsetof(ResolveStub, _failEntryPoint) );
    return resolveHolder;
}

ResolveHolder* ResolveHolder::FromResolveEntry(PCODE resolveEntry)
{
    LIMITED_METHOD_CONTRACT;
    ResolveHolder* resolveHolder = (ResolveHolder*) ( resolveEntry - offsetof(ResolveHolder, _stub) - offsetof(ResolveStub, _resolveEntryPoint) );
    return resolveHolder;
}

void VTableCallHolder::Initialize(unsigned slot)
{
    unsigned offsetOfIndirection = MethodTable::GetVtableOffset() + MethodTable::GetIndexOfVtableIndirection(slot) * TARGET_POINTER_SIZE;
    unsigned offsetAfterIndirection = MethodTable::GetIndexAfterVtableIndirection(slot) * TARGET_POINTER_SIZE;

    VTableCallStub* pStub = stub();
    BYTE* p = (BYTE*)pStub->entryPoint();

    // ld  t4,0(a0) : t4 = MethodTable pointer
    *(UINT32*)p = 0x00053e83; // VTABLECALL_STUB_FIRST_DWORD
    p += 4;

    if ((offsetOfIndirection >= 0x1000) || (offsetAfterIndirection >= 0x1000))
    {
        *(UINT32*)p = 0x00000317; // auipc t1, 0
        p += 4;
    }

    if (offsetOfIndirection >= 0x1000)
    {
        uint dataOffset = 20 + (offsetAfterIndirection >= 0x1000 ? 12 : 4);

        // lwu t3,dataOffset(t1)
        *(DWORD*)p = 0x00036e03 | ((UINT32)dataOffset << 20); p += 4;
        // add t4, t4, t3
        *(DWORD*)p = 0x01ce8eb3; p += 4;
        // ld t4, offsetOfIndirection(t4)
        *(DWORD*)p = 0x000ebe83; p += 4;
    }
    else
    {
        // ld t4, offsetOfIndirection(t4)
        *(DWORD*)p = 0x000ebe83 | ((UINT32)offsetOfIndirection << 20); p += 4;
    }

    if (offsetAfterIndirection >= 0x1000)
    {
        uint indirectionsCodeSize = (offsetOfIndirection >= 0x1000 ? 12 : 4);
        uint indirectionsDataSize = (offsetOfIndirection >= 0x1000 ? 4 : 0);
        uint dataOffset = 20 + indirectionsCodeSize + indirectionsDataSize;

        // ldw t3,dataOffset(t1)
        *(DWORD*)p = 0x00036e03 | ((UINT32)dataOffset << 20); p += 4;
        // add t4, t4, t3
        *(DWORD*)p = 0x01ce8eb3; p += 4;
        // ld t4, 0(t4)
        *(DWORD*)p = 0x000ebe83; p += 4;
    }
    else
    {
        // ld t4, offsetOfIndirection(t4)
        *(DWORD*)p = 0x000ebe83 | ((UINT32)offsetAfterIndirection << 20); p += 4;
    }

    // jalr x0, t4, 0
    *(UINT32*)p = 0x000e8067; p += 4;

    // data labels:
    if (offsetOfIndirection >= 0x1000)
    {
        *(UINT32*)p = (UINT32)offsetOfIndirection;
        p += 4;
    }
    if (offsetAfterIndirection >= 0x1000)
    {
        *(UINT32*)p = (UINT32)offsetAfterIndirection;
        p += 4;
    }

    // Store the slot value here for convenience. Not a real instruction (unreachable anyways)
    // NOTE: Not counted in codeSize above.
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
    TADDR pInstr = PCODEToPINSTR(stubStartAddress);

    EX_TRY
    {
        // If stubStartAddress is completely bogus, then this might AV,
        // so we protect it with SEH. An AV here is OK.
        AVInRuntimeImplOkayHolder AVOkay;

        DWORD firstDword = *((DWORD*) pInstr);

        if (firstDword == DISPATCH_STUB_FIRST_DWORD) // assembly of first instruction of DispatchStub :
        {
            stubKind = SK_DISPATCH;
        }
        else if (firstDword == RESOLVE_STUB_FIRST_DWORD) // assembly of first instruction of ResolveStub :
        {
            stubKind = SK_RESOLVE;
        }
        else if (firstDword == VTABLECALL_STUB_FIRST_DWORD) // assembly of first instruction of VTableCallStub :
        {
            stubKind = SK_VTABLECALL;
        }
        else if (firstDword == LOOKUP_STUB_FIRST_DWORD) // first instruction of LookupStub :
        {
            stubKind = SK_LOOKUP;
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
