// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// VirtualCallStubCpu.hpp
//
#ifndef _VIRTUAL_CALL_STUB_S390X_H
#define _VIRTUAL_CALL_STUB_S390X_H

#define DISPATCH_STUB_FIRST_DWORD 0xc4080000
#define RESOLVE_STUB_FIRST_DWORD 0xe3102000

#define USES_LOOKUP_STUBS   1

struct LookupStub
{
    inline PCODE entryPoint() { LIMITED_METHOD_CONTRACT; return (PCODE)&_entryPoint[0]; }
    inline size_t token() { LIMITED_METHOD_CONTRACT; return _token; }
    inline size_t size() { LIMITED_METHOD_CONTRACT; return sizeof(LookupStub); }
private :
    friend struct LookupHolder;

    UINT16 _entryPoint[8];
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
        _stub._entryPoint[0] = 0xc418;  // lgrl %r1, <_resolveWorkerTarget>
        _stub._entryPoint[1] = 0x0000;
        _stub._entryPoint[2] = 0x0008;  // (16 - 0*2) / 2
        _stub._entryPoint[3] = 0xc408;  // lgrl %r0, <_token>
        _stub._entryPoint[4] = 0x0000;
        _stub._entryPoint[5] = 0x0009;  // (24 - 3*2) / 2
        _stub._entryPoint[6] = 0x07f1;  // br %r1
        _stub._entryPoint[7] = 0x0707;  // padding to ensure 8-byte alignment

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

    UINT16 _entryPoint[16];
    size_t  _expectedMT;
    PCODE _implTarget;
    PCODE _failTarget;
};

struct DispatchHolder
{
    static void InitializeStatic() { }

    void Initialize(DispatchHolder* pDispatchHolderRX, PCODE implTarget, PCODE failTarget, size_t expectedMT)
    {
        _stub._entryPoint[ 0] = 0xc408;  // lgrl %r0,_expectedMT
        _stub._entryPoint[ 1] = 0x0000;
        _stub._entryPoint[ 2] = 0x0010;
        _stub._entryPoint[ 3] = 0xc418;  // lgrl %r1,_implTarget
        _stub._entryPoint[ 4] = 0x0000;
        _stub._entryPoint[ 5] = 0x0011;
        _stub._entryPoint[ 6] = 0xe300;  // clg %r0,0(%r2)
        _stub._entryPoint[ 7] = 0x2000;
        _stub._entryPoint[ 8] = 0x0021;
        _stub._entryPoint[ 9] = 0x0781;  // ber %r1
        _stub._entryPoint[10] = 0xc418;  // lgrl %r1,_failTarget
        _stub._entryPoint[11] = 0x0000;
        _stub._entryPoint[12] = 0x0003;
        _stub._entryPoint[13] = 0x07f1;  // br %r1
        _stub._entryPoint[14] = 0x0707;  // alignment
        _stub._entryPoint[15] = 0x0707;  // alignment

        _stub._expectedMT = expectedMT;
        _stub._implTarget = implTarget;
        _stub._failTarget = failTarget;
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
    inline size_t  token()                   { LIMITED_METHOD_CONTRACT; return _token; }
    inline INT32*  pCounter()                { LIMITED_METHOD_CONTRACT; return _pCounter; }

    inline size_t  size()                    { LIMITED_METHOD_CONTRACT; return sizeof(ResolveStub); }

private:
    friend struct ResolveHolder;
    const static int failEntryPointLen = 10;
    const static int resolveEntryPointLen = 46;

    UINT16 _failEntryPoint[failEntryPointLen];
    UINT16 _resolveEntryPoint[resolveEntryPointLen];
    INT32*  _pCounter;               // Base of the Data Region
    size_t  _cacheAddress;           // lookupCache
    size_t  _token;
    PCODE   _resolveWorkerTarget;
};

struct ResolveHolder
{
    static void  InitializeStatic() { }

    void Initialize(ResolveHolder* pResolveHolderRX,
                    PCODE resolveWorkerTarget, PCODE patcherTarget,
                    size_t dispatchToken, UINT32 hashedToken,
                    void * cacheAddr, INT32 * counterAddr)
    {
        // Fill in the stub specific fields
        _stub._cacheAddress        = (size_t) cacheAddr;
        _stub._token               = dispatchToken;
        _stub._resolveWorkerTarget = (size_t) resolveWorkerTarget;
        _stub._pCounter            = counterAddr;

        // Fill in fail stub
        //
        // On input to the fail stub:
        //  8(%r15)  contains the address of the indirection cell (with the flags in the low bits)
        //  %r2      contains the object ref

        // Decrement counter
        _stub._failEntryPoint[0] = 0xc418;  // lgrl %r1,_pCounter
        _stub._failEntryPoint[1] = 0x0000;
        _stub._failEntryPoint[2] = 0x0038;
        _stub._failEntryPoint[3] = 0xebff;  // asi 0(%r1),-1
        _stub._failEntryPoint[4] = 0x1000;
        _stub._failEntryPoint[5] = 0x006a;

        // If counter is now negative, set the SDF_ResolveBackPatch flag
        // Either way, fall then through to the resolve stub
        _stub._failEntryPoint[6] = 0xa7b4;  // jnl _resolveEntryPoint
        _stub._failEntryPoint[7] = 0x0004;
        _stub._failEntryPoint[8] = 0x9601;  // oi 15(%r15),1
        _stub._failEntryPoint[9] = 0xf00f;

        // Fill in resolve stub
        //
        // On input to the resolve stub:
        //  8(%r15)  contains the address of the indirection cell (with the flags in the low bits)
        //  %r2      contains the object ref

        // Compute hash = ((MT + MT>>12) ^ prehash) & cachemask
        _stub._resolveEntryPoint[ 0] = 0xe310;  // lg %r1,0(%r2)
        _stub._resolveEntryPoint[ 1] = 0x2000;
        _stub._resolveEntryPoint[ 2] = 0x0004;
        _stub._resolveEntryPoint[ 3] = 0xeb01;  // srlk %r0,%r1,12
        _stub._resolveEntryPoint[ 4] = 0x000c;
        _stub._resolveEntryPoint[ 5] = 0x00de;
        _stub._resolveEntryPoint[ 6] = 0xb908;  // agr %r0,%r1
        _stub._resolveEntryPoint[ 7] = 0x0001;
        _stub._resolveEntryPoint[ 8] = 0xc007;  // xilf %r0,(hashedToken << LOG2_PTRSIZE)
        _stub._resolveEntryPoint[ 9] = (UINT16)((hashedToken << LOG2_PTRSIZE) >> 16);
        _stub._resolveEntryPoint[10] = (UINT16)(hashedToken << LOG2_PTRSIZE);
        _stub._resolveEntryPoint[11] = 0xc00b;  // nilf %r0,(CALL_STUB_CACHE_MASK << LOG2_PTRSIZE)
        _stub._resolveEntryPoint[12] = (UINT16)((CALL_STUB_CACHE_MASK << LOG2_PTRSIZE) >> 16);
        _stub._resolveEntryPoint[13] = (UINT16)(CALL_STUB_CACHE_MASK << LOG2_PTRSIZE);

        // Get cache entry address associated with hash
        _stub._resolveEntryPoint[14] = 0xc418;  // lgrl %r1,_cacheAddress
        _stub._resolveEntryPoint[15] = 0x0000;
        _stub._resolveEntryPoint[16] = 0x0024;
        _stub._resolveEntryPoint[17] = 0xb918;  // agfr %r1,%r1
        _stub._resolveEntryPoint[18] = 0x0010;
        _stub._resolveEntryPoint[19] = 0xe310;  // lg %r1,0(%r1)
        _stub._resolveEntryPoint[20] = 0x1000;
        _stub._resolveEntryPoint[21] = 0x0004;

        // Load token into %r0 (may be used by resolve worker)
        _stub._resolveEntryPoint[22] = 0xc408;  // lgrl %r0,_token
        _stub._resolveEntryPoint[23] = 0x0000;
        _stub._resolveEntryPoint[24] = 0x0020;

        // Check whether cache entry matches our pMT and token
        _stub._resolveEntryPoint[25] = 0xd507;  // clc 0(8,%r2),pMT(%r1)
        _stub._resolveEntryPoint[26] = 0x2000;
        _stub._resolveEntryPoint[27] = 0x1000 | offsetof(ResolveCacheElem, pMT);
        _stub._resolveEntryPoint[28] = 0xa774;  // jne miss
        _stub._resolveEntryPoint[29] = 0x000b;
        _stub._resolveEntryPoint[30] = 0xe300;  // clg %r0,token(%r1)
        _stub._resolveEntryPoint[31] = 0x1000 | offsetof(ResolveCacheElem, token);
        _stub._resolveEntryPoint[32] = 0x0021;
        _stub._resolveEntryPoint[33] = 0xa774;  // jne miss
        _stub._resolveEntryPoint[34] = 0x0006;

        // Yes: directly call cached target entry point
        _stub._resolveEntryPoint[35] = 0xe310;  // lg %r1,target(%r1)
        _stub._resolveEntryPoint[36] = 0x1000 | offsetof(ResolveCacheElem, target);
        _stub._resolveEntryPoint[37] = 0x0004;
        _stub._resolveEntryPoint[38] = 0x07f1;  // br %r1

        // No: store cache entry address and call resolve worker
        _stub._resolveEntryPoint[39] = 0xe310;  // miss: stg %r1,16(%r15)
        _stub._resolveEntryPoint[40] = 0xf010;
        _stub._resolveEntryPoint[41] = 0x0024;
        _stub._resolveEntryPoint[42] = 0xc418;  // lgrl %r1,_resolveWorker
        _stub._resolveEntryPoint[43] = 0x0000;
        _stub._resolveEntryPoint[44] = 0x0010;
        _stub._resolveEntryPoint[45] = 0x07f1;  // br %r1

        // On input to the resolve worker:
        //  16(%r15) contains the pointer to the ResolveCacheElem
        //  8(%r15)  contains the address of the indirection cell (with the flags in the low bits)
        //  %r0      contains the dispatch token
        //  %r2      contains the object ref
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
        _ASSERTE(!"S390X:NYI");
        return 0;
    }

    inline PCODE        entryPoint()        const { LIMITED_METHOD_CONTRACT;  return (PCODE)&_entryPoint[0]; }

    inline size_t token()
    {
        _ASSERTE(!"S390X:NYI");
        return 0;
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
        _ASSERTE(!"S390X:NYI");
        return 0;
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
    _ASSERTE(!"S390X:NYI");
}

#endif // DACCESS_COMPILE

#endif //DECLARE_DATA

#endif // _VIRTUAL_CALL_STUB_S390X_H
