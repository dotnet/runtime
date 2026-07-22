// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// VirtualCallStubCpu.hpp
//
#ifndef _VIRTUAL_CALL_STUB_PPC64LE_H
#define _VIRTUAL_CALL_STUB_PPC64LE_H

// TODO RESOLVE_STUB
#define DISPATCH_STUB_FIRST_DWORD 0xe80c0028 // ld r0,40(r12)
#define RESOLVE_STUB_FIRST_DWORD 0xe8830000  // ld r4, 0(r3)

#define USES_LOOKUP_STUBS   1

// #include <cassert>

struct LookupStub
{
    inline PCODE entryPoint() { LIMITED_METHOD_CONTRACT; return (PCODE)&_entryPoint[0]; }
    inline size_t token() { LIMITED_METHOD_CONTRACT; return _token; }
    inline size_t size() { LIMITED_METHOD_CONTRACT; return sizeof(LookupStub); }
private:
    friend struct LookupHolder;
    UINT32 _entryPoint[6];    // 6 instructions (24 bytes) 
    PCODE _resolveWorkerTarget; // offset 24
    size_t _token;            // offset 32
};

struct LookupHolder
{
    private:
        LookupStub _stub;
    public:
	static void InitializeStatic() { }

	void Initialize(LookupHolder* pLookupHolderRX, PCODE resolveWorkerTarget, size_t dispatchToken) {
        // r12 points to _entryPoint[0] (stub base), set by the caller.
        
        _stub._entryPoint[0] = 0xe94c0018; // ld r10, 24(r12) -> __resolveWorkerTarget
        _stub._entryPoint[1] = 0xE8AC0020; // ld r5, 32(r12) -> _token
        _stub._entryPoint[2] = 0x7d4903a6; // mtspr CTR, r10
        _stub._entryPoint[3] = 0x4e800420; // bctr
        _stub._entryPoint[4] = 0x60000000; // nop
        _stub._entryPoint[5] = 0x60000000; // nop
        _stub._resolveWorkerTarget = resolveWorkerTarget;
        _stub._token = dispatchToken;
    }

	LookupStub*    stub()        { LIMITED_METHOD_CONTRACT; return &_stub; }
	static LookupHolder*  FromLookupEntry(PCODE lookupEntry)
	{
            LIMITED_METHOD_CONTRACT;
            return (LookupHolder*) ( lookupEntry - offsetof(LookupHolder, _stub) - offsetof(LookupStub, _entryPoint)  );
	}
};

struct DispatchStub
{
    inline PCODE entryPoint()         { LIMITED_METHOD_CONTRACT; return (PCODE)&_entryPoint[0]; }
    inline size_t expectedMT()  { LIMITED_METHOD_CONTRACT; return _expectedMT; }
    inline PCODE implTarget()   { LIMITED_METHOD_CONTRACT; return _implTarget; }
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

    UINT32 _entryPoint[10];  // 10 instructions (40 bytes)
    size_t  _expectedMT;     // offset 40
    PCODE _implTarget;       // oofset 48 
    PCODE _failTarget;       // oofset 56
};

struct DispatchHolder
{
    static void InitializeStatic() { }

    void Initialize(DispatchHolder* pDispatchHolderRX, PCODE implTarget, PCODE failTarget, size_t expectedMT)
    {
        // r12 points to _entryPoint[0] (stub base), set by the caller.
         _stub._entryPoint[0] = 0xe80c0028; // ld r0, 40(r12)
        _stub._entryPoint[1] = 0xe8830000; // ld r4, 0(r3)
        _stub._entryPoint[2] = 0x7c240000; // cmpd cr0, r4, r0
        _stub._entryPoint[3] = 0x41820010; // beq target (+16)
        _stub._entryPoint[4] = 0xe98c0038; // ld r12, 56(r12)
        _stub._entryPoint[5] = 0x7d8903a6; // mtspr CTR, r12
        _stub._entryPoint[6] = 0x4e800420; // bctr
        _stub._entryPoint[7] = 0xe98c0030; // target: ld r12, 48(r12)
        _stub._entryPoint[8] = 0x7d8903a6; // mtspr CTR, r12
        _stub._entryPoint[9] = 0x4e800420; // bctr
	
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
    const static int failEntryPointLen = 12;
    const static int resolveEntryPointLen = 26;

    UINT32 _failEntryPoint[failEntryPointLen];
    UINT32 _resolveEntryPoint[resolveEntryPointLen];
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

	// -------------------------------
    // failEntryPoint (48 bytes)
    // -------------------------------
    // r12 = stub base
    // retry:
    //   ld    r10,152(r12)     ; pCounter*
    //   lwarx r0,0,r10
    //   addi  r0,r0,-1
    //   stwcx r0,0,r10
    //   bne-  retry            ; -16 from NIA (to lwarx)
    //   cmpwi r0,0
    //   bge   +24              ; to resolveEntryPoint start
    //   ld    r0,8(r1)         ; indirection cell
    //   ori   r0,r0,1          ; set backpatch flag
    //   std   r0,8(r1)
    //   nop; nop
        _stub._failEntryPoint[0]  = 0xe94c0098; // ld    r10,152(r12)
        _stub._failEntryPoint[1]  = 0x7c0a0280; // lwarx r0,0,r10
        _stub._failEntryPoint[2]  = 0x3800ffff; // addi  r0,r0,-1
        _stub._failEntryPoint[3]  = 0x7c0a02ac; // stwcx r0,0,r10
        _stub._failEntryPoint[4]  = 0x40c2fff0; // bne-  -16
        _stub._failEntryPoint[5]  = 0x2c000000; // cmpwi r0,0
        _stub._failEntryPoint[6]  = 0x40800018; // bge   +24
        _stub._failEntryPoint[7]  = 0xe8010008; // ld    r0,8(r1)
        _stub._failEntryPoint[8]  = 0x60000001; // ori   r0,r0,1
        _stub._failEntryPoint[9]  = 0xf8010008; // std   r0,8(r1)
        _stub._failEntryPoint[10] = 0x60000000; // nop
        _stub._failEntryPoint[11] = 0x60000000; // nop
	
	// -------------------------------
    // resolveEntryPoint (104 bytes)
    // -------------------------------
    //   ld    r4,0(r3)               ; MT
    //   srdi  r0,r4,12
    //   add   r0,r0,r4
    //   xori  r0,r0,hashedToken
    //   andi. r0,r0,CALL_STUB_CACHE_MASK
    //   sldi  r0,r0,3
    //   ld    r10,160(r12)           ; cache base
    //   add   r10,r10,r0             ; cache slot
    //   ld    r0,0(r10)              ; cached MT
    //   cmpd  cr0,r4,r0
    //   bne   miss                   ; +28
    //   ld    r0,8(r10)              ; cached token
    //   ld    r5,168(r12)            ; this stub's token
    //   cmpd  cr0,r5,r0
    //   bne   miss                   ; +12
    //   ld    r10,16(r10)            ; cached target
    //   mtspr CTR,r10
    //   bctr
    // miss:
    //   std   r10,16(r1)             ; pass cache entry to worker (optional)
    //   ld    r5,168(r12)            ; pass token to worker
    //   ld    r10,176(r12)           ; ResolveWorker
    //   mtspr CTR,r10
    //   bctr
    //   nop; nop; nop
        _stub._resolveEntryPoint[0]  = 0xe8830000;                          // ld    r4,0(r3)
        _stub._resolveEntryPoint[1]  = 0x7800e904;                          // srdi  r0,r4,12
        _stub._resolveEntryPoint[2]  = 0x7c004214;                          // add   r0,r0,r4
        _stub._resolveEntryPoint[3]  = 0x68000000 | (hashedToken & 0xFFFF); // xori  r0,r0,imm16
        _stub._resolveEntryPoint[4]  = 0x70000000 | (CALL_STUB_CACHE_MASK & 0xFFFF); // andi. r0,r0,imm16
        _stub._resolveEntryPoint[5]  = 0x7800c104;                          // sldi  r0,r0,3
        _stub._resolveEntryPoint[6]  = 0xe94c00a0;                          // ld    r10,160(r12)
        _stub._resolveEntryPoint[7]  = 0x7d4a0214;                          // add   r10,r10,r0
        _stub._resolveEntryPoint[8]  = 0xe80a0000;                          // ld    r0,0(r10)
        _stub._resolveEntryPoint[9]  = 0x7c240000;                          // cmpd  cr0,r4,r0
        _stub._resolveEntryPoint[10] = 0x4082001c;                          // bne   +28 -> miss
        _stub._resolveEntryPoint[11] = 0xe80a0008;                          // ld    r0,8(r10)
        _stub._resolveEntryPoint[12] = 0xe8ac00a8;                          // ld    r5,168(r12)
        _stub._resolveEntryPoint[13] = 0x7c250000;                          // cmpd  cr0,r5,r0
        _stub._resolveEntryPoint[14] = 0x4082000c;                          // bne   +12 -> miss
        _stub._resolveEntryPoint[15] = 0xe94a0010;                          // ld    r10,16(r10)
        _stub._resolveEntryPoint[16] = 0x7d4903a6;                          // mtspr CTR,r10
        _stub._resolveEntryPoint[17] = 0x4e800420;                          // bctr
        // miss:
        _stub._resolveEntryPoint[18] = 0xf94a0030;                          // std   r10,48(r1)
        _stub._resolveEntryPoint[19] = 0xe8ac00a8;                          // ld    r5,168(r12)
        _stub._resolveEntryPoint[20] = 0xe94c00b0;                          // ld    r10,176(r12)
        _stub._resolveEntryPoint[21] = 0x7d4903a6;                          // mtspr CTR,r10
        _stub._resolveEntryPoint[22] = 0x4e800420;                          // bctr
        _stub._resolveEntryPoint[23] = 0x60000000;                          // nop
        _stub._resolveEntryPoint[24] = 0x60000000;                          // nop
        _stub._resolveEntryPoint[25] = 0x60000000;                          // nop
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
        _ASSERTE(!"PPC64LE:NYI");
	return 0;
    }

    inline PCODE        entryPoint()        const { LIMITED_METHOD_CONTRACT;  return (PCODE)&_entryPoint[0]; }

    inline size_t token()
    {
        _ASSERTE(!"PPC64LE:NYI");
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
        _ASSERTE(!"PPC64LE:NYI");
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
    _ASSERTE(!"TARGET_POWERPC64:NYI");
}

#endif // DACCESS_COMPILE

#endif //DECLARE_DATA

#endif // #endif // _VIRTUAL_CALL_STUB_PPC64LE_H
