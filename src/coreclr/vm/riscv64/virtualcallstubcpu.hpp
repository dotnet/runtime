// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// VirtualCallStubCpu.hpp
//
#ifndef _VIRTUAL_CALL_STUB_RISCV64_H
#define _VIRTUAL_CALL_STUB_RISCV64_H

#define DISPATCH_STUB_FIRST_DWORD 0x00000e97
#define RESOLVE_STUB_FIRST_DWORD 0x00053e03
#define VTABLECALL_STUB_FIRST_DWORD 0x00053e83

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

        _stub._entryPoint[0] = LOOKUP_STUB_FIRST_DWORD; // auipc t6, 0  //0x00000f97
        _stub._entryPoint[1] = 0x018fb383; //ld   t2, 24(t6)
        _stub._entryPoint[2] = 0x010fbf83; //ld   t6, 16(t6)
        _stub._entryPoint[3] = 0x000f8067; //jalr x0, t6, 0

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
        // auipc t4,0
        // ld    t0, 0(a0)     // methodTable from object in $a0
        // ld    t6, 32(t4)    // t6 _expectedMT
        // bne   t6, t0, failLabel
        // ld    t4, 40(t4)    // t4 _implTarget
        // jalr  x0, t4, 0
        // failLabel:
        // ld    t4, 48(t4)    // t4 _failTarget
        // jalr  x0, t4, 0
        //
        //
        // _expectedMT
        // _implTarget
        // _failTarget

        _stub._entryPoint[0] = DISPATCH_STUB_FIRST_DWORD; // auipc t4,0  // 0x00000e97
        _stub._entryPoint[1] = 0x00053283; // ld    t0, 0(a0)     // methodTable from object in $a0
        _stub._entryPoint[2] = 0x020ebf83; // ld    t6, 32(t4)    // t6 _expectedMT
        _stub._entryPoint[3] = 0x005f9663; // bne   t6, t0, failLabel
        _stub._entryPoint[4] = 0x028ebe83; // ld    t4, 40(t4)    // t4 _implTarget
        _stub._entryPoint[5] = 0x000e8067; // jalr  x0, t4, 0
        _stub._entryPoint[6] = 0x030ebe83; // ld    t4, 48(t4)    // t4 _failTarget
        _stub._entryPoint[7] = 0x000e8067; // jalr  x0, t4, 0

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
    inline PCODE slowEntryPoint()            { LIMITED_METHOD_CONTRACT; return (PCODE)&_slowEntryPoint[0]; }
    inline size_t  token()                   { LIMITED_METHOD_CONTRACT; return _token; }
    inline INT32*  pCounter()                { LIMITED_METHOD_CONTRACT; return _pCounter; }

    inline UINT32  hashedToken()             { LIMITED_METHOD_CONTRACT; return _hashedToken >> LOG2_PTRSIZE;    }
    inline size_t  cacheAddress()            { LIMITED_METHOD_CONTRACT; return _cacheAddress;   }
    inline size_t  size()                    { LIMITED_METHOD_CONTRACT; return sizeof(ResolveStub); }

private:
    friend struct ResolveHolder;
    const static int resolveEntryPointLen = 20;
    const static int slowEntryPointLen = 4;
    const static int failEntryPointLen = 9;

    DWORD _resolveEntryPoint[resolveEntryPointLen];
    DWORD _slowEntryPoint[slowEntryPointLen];
    DWORD _failEntryPoint[failEntryPointLen];
    UINT32  _hashedToken;
    INT32*  _pCounter;               //Base of the Data Region
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
        int n=0;
        INT32 pc_offset;

/******** Rough Convention of used in this routine
        ;;ra  temp base address of loading data region
        ;;t5  indirection cell
        ;;t3  MethodTable (from object ref in a0), out: this._token
        ;;t0  hash scratch
        ;;t1  temp
        ;;t2  temp
        ;;t6 hash scratch
        ;;cachemask => [CALL_STUB_CACHE_MASK * sizeof(void*)]

        // Called directly by JITTED code
        // ResolveStub._resolveEntryPoint(a0:Object*, a1 ...,a7, t8:IndirectionCellAndFlags)
        // {
        //    MethodTable mt = a0.m_pMethTab;
        //    int i = ((mt + mt >> 12) ^ this._hashedToken) & _cacheMask
        //    ResolveCacheElem e = this._cacheAddress + i
        //    t1 = e = this._cacheAddress + i
        //    if (mt == e.pMT && this._token == e.token)
        //    {
        //        (e.target)(a0, [a1,...,a7]);
        //    }
        //    else
        //    {
        //        t3 = this._token;
        //        (this._slowEntryPoint)(a0, [a1,.., a7], t5, t3);
        //    }
        // }
 ********/

        ///;;resolveEntryPoint
        // Called directly by JITTED code
        // ResolveStub._resolveEntryPoint(a0:Object*, a1 ...,a7, t5:IndirectionCellAndFlags)

        // 	ld  t3, 0(a0)
        _stub._resolveEntryPoint[n++] = RESOLVE_STUB_FIRST_DWORD;
        // 	srli  t0, t3, 0xc
        _stub._resolveEntryPoint[n++] = 0x00ce5293;
        // 	add  t1, t3, t0
        _stub._resolveEntryPoint[n++] = 0x005e0333;
        // 	auipc t0, 0
        _stub._resolveEntryPoint[n++] = 0x00000297;
        //  addi t0, t0, -12
        _stub._resolveEntryPoint[n++] = 0xff428293;

        // 	lw  t6, 0(t0)  #t6 = this._hashedToken
        _stub._resolveEntryPoint[n++] = 0x0002af83 | (33 << 22); //(20+4+9)*4<<20;
        _ASSERTE((ResolveStub::resolveEntryPointLen+ResolveStub::slowEntryPointLen+ResolveStub::failEntryPointLen) == 33);
        _ASSERTE((33<<2) == (offsetof(ResolveStub, _hashedToken) -offsetof(ResolveStub, _resolveEntryPoint[0])));

        // 	xor	 t1, t1, t6
        _stub._resolveEntryPoint[n++] = 0x01f34333;
        // 	cachemask
        _ASSERTE(CALL_STUB_CACHE_MASK * sizeof(void*) == 0x7ff8);
        // lui  t6, 0x7ff8
        _stub._resolveEntryPoint[n++] = 0x07ff8fb7;
        // srliw  t6, t6, 12
        _stub._resolveEntryPoint[n++] = 0x00cfdf9b;
        // 	and  t1, t1, t6
        _stub._resolveEntryPoint[n++] = 0x01f37333;
        // 	ld  t6, 0(t0)    # t6 = this._cacheAddress
        _stub._resolveEntryPoint[n++] = 0x0002bf83 | (36 << 22); //(20+4+9+1+2)*4<<20;
        _ASSERTE((ResolveStub::resolveEntryPointLen+ResolveStub::slowEntryPointLen+ResolveStub::failEntryPointLen+1+2) == 36);
        _ASSERTE((36<<2) == (offsetof(ResolveStub, _cacheAddress) -offsetof(ResolveStub, _resolveEntryPoint[0])));
        //  add t1, t6, t1
        _stub._resolveEntryPoint[n++] = 0x006f8333;
        // 	ld  t1, 0(t1)    # t1 = e = this._cacheAddress[i]
        _stub._resolveEntryPoint[n++] = 0x00033303;

        // 	ld  t6, 0(t1)    # t6 = Check mt == e.pMT;
        _stub._resolveEntryPoint[n++] = 0x00033f83 | ((offsetof(ResolveCacheElem, pMT) & 0xfff) << 20);
        // 	ld  t2, 0(t0)  #  $t2 = this._token
        _stub._resolveEntryPoint[n++] = 0x0002b383 | (38<<22);//(20+4+9+1+2+2)*4<<20;
        _ASSERTE((ResolveStub::resolveEntryPointLen+ResolveStub::slowEntryPointLen+ResolveStub::failEntryPointLen+1+4) == 38);
        _ASSERTE((38<<2) == (offsetof(ResolveStub, _token) -offsetof(ResolveStub, _resolveEntryPoint[0])));

        // 	bne  t6, t3, next
        _stub._resolveEntryPoint[n++] = 0x01cf9a63;// | PC_REL_OFFSET(_slowEntryPoint[0], n);

        // 	ld  t6, 0(t1)    # t6 = e.token;
        _stub._resolveEntryPoint[n++] = 0x00033f83 | ((offsetof(ResolveCacheElem, token) & 0xfff)<<10);
        // 	bne	 t6, t2, next
        _stub._resolveEntryPoint[n++] = 0x007f9663;// | PC_REL_OFFSET(_slowEntryPoint[0], n);

         pc_offset = offsetof(ResolveCacheElem, target) & 0xffffffff;
         _ASSERTE(pc_offset >=0 && pc_offset%8 == 0);
        // 	ld  t3, 0(t1)    # t3 = e.target;
        _stub._resolveEntryPoint[n++] = 0x00033e03 | ((offsetof(ResolveCacheElem, target) & 0xfff)<<10);
        // 	jalr x0, t3, 0
        _stub._resolveEntryPoint[n++] = 0x000e0067;

        _ASSERTE(n == ResolveStub::resolveEntryPointLen);
        _ASSERTE(_stub._resolveEntryPoint + n == _stub._slowEntryPoint);

        // ResolveStub._slowEntryPoint(a0:MethodToken, [a1..a7], t5:IndirectionCellAndFlags)
        // {
        //     t2 = this._token;
        //     this._resolveWorkerTarget(a0, [a1..a7], t5, t2);
        // }
//#undef PC_REL_OFFSET
//#define PC_REL_OFFSET(_member, _index) (((INT32)(offsetof(ResolveStub, _member) - (offsetof(ResolveStub, _slowEntryPoint[_index])))) & 0xffff)
        // ;;slowEntryPoint:
        // ;;fall through to the slow case

        // 	auipc t0, 0
        _stub._slowEntryPoint[0] = 0x00000297;
        // 	ld  t6, 0(t0)    # r21 = _resolveWorkerTarget;
        _ASSERTE((0x14*4) == ((INT32)(offsetof(ResolveStub, _resolveWorkerTarget) - (offsetof(ResolveStub, _slowEntryPoint[0])))));
        _ASSERTE((ResolveStub::slowEntryPointLen + ResolveStub::failEntryPointLen+1+3*2) == 0x14);
        _stub._slowEntryPoint[1] = 0x0002bf83 | ((0x14 * 4) << 20);

        // 	ld  t2, 0(t0)    # t2 = this._token;
        _stub._slowEntryPoint[2] = 0x0002b383 | ((0x12 * 4) << 20); //(18*4=72=0x48)<<20
        _ASSERTE((ResolveStub::slowEntryPointLen+ResolveStub::failEntryPointLen+1+4)*4 == (0x12 * 4));
        _ASSERTE((0x12 * 4) == (offsetof(ResolveStub, _token) -offsetof(ResolveStub, _slowEntryPoint[0])));

        // 	jalr  x0, t6, 0
        _stub._slowEntryPoint[3] = 0x000f8067;

         _ASSERTE(4 == ResolveStub::slowEntryPointLen);

        // ResolveStub._failEntryPoint(a0:MethodToken, a1,.., a7, t5:IndirectionCellAndFlags)
        // {
        //     if(--*(this._pCounter) < 0) t5 = t5 | SDF_ResolveBackPatch;
        //     this._resolveEntryPoint(a0, [a1..a7]);
        // }
//#undef PC_REL_OFFSET
//#define PC_REL_OFFSET(_member, _index) (((INT32)(offsetof(ResolveStub, _member) - (offsetof(ResolveStub, _failEntryPoint[_index])))) & 0xffff)
        //;;failEntryPoint

        // 	auipc t0, 0
        _stub._failEntryPoint[0] = 0x00000297;
        // 	ld  t1, 0(t0)    # t1 = _pCounter;  0x2800000=((failEntryPointLen+1)*4)<<20.
        _stub._failEntryPoint[1] = 0x0002b303 | 0x2800000;
        _ASSERTE((((ResolveStub::failEntryPointLen+1)*4)<<20) == 0x2800000);
        _ASSERTE((0x2800000>>20) == ((INT32)(offsetof(ResolveStub, _pCounter) - (offsetof(ResolveStub, _failEntryPoint[0])))));
        // 	lw  t6, 0(t1)
        _stub._failEntryPoint[2] = 0x00032f83;
        // 	addi  t6, t6, -1
        _stub._failEntryPoint[3] = 0xffff8f93;

        // 	sw  t6, 0(t1)
        _stub._failEntryPoint[4] = 0x01f32023;

        _ASSERTE(SDF_ResolveBackPatch == 0x1);
        // ;; ori t5, t5, t6 >=0 ? SDF_ResolveBackPatch:0;
        // 	slti t6, t6, 0
        _stub._failEntryPoint[5] = 0x000faf93;
        // 	xori t6, t6, 1
        _stub._failEntryPoint[6] = 0x001fcf93;
        // 	or  t5, t5, t6
        _stub._failEntryPoint[7] = 0x01ff6f33;

        // 	j	_resolveEntryPoint   // pc - 128 = pc + 4 - resolveEntryPointLen * 4 - slowEntryPointLen * 4 - failEntryPointLen * 4;
        _stub._failEntryPoint[8] = 0xf81ff06f;

        _ASSERTE(9 == ResolveStub::failEntryPointLen);
         _stub._pCounter = counterAddr;
        _stub._hashedToken         = hashedToken << LOG2_PTRSIZE;
        _stub._cacheAddress        = (size_t) cacheAddr;
        _stub._token               = dispatchToken;
        _stub._resolveWorkerTarget = resolveWorkerTarget;

        _ASSERTE(resolveWorkerTarget == (PCODE)ResolveWorkerChainLookupAsmStub);
        _ASSERTE(patcherTarget == NULL);

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
        LIMITED_METHOD_CONTRACT;

        BYTE* pStubCode = (BYTE *)this;


        if ((*(DWORD*)(&pStubCode[12])) == 0x000e8067)
        {
            // jalr x0, t4, 0
            return 20;//4*ins + slot = 4*4 + 4;
        }

        //auipc t1, 0
        assert((*(DWORD*)(&pStubCode[4])) == 0x00000317);

        size_t cbSize = 36;

        // ld t4, 0(t4)
        if ((*(DWORD*)(&pStubCode[16])) == 0x000ebe83)
        {
            if ((*(DWORD*)(&pStubCode[28])) == 0x000ebe83)
                cbSize += 12;
        }

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

    static size_t GetHolderSize(unsigned slot)
    {
        STATIC_CONTRACT_WRAPPER;
        unsigned offsetOfIndirection = MethodTable::GetVtableOffset() + MethodTable::GetIndexOfVtableIndirection(slot) * TARGET_POINTER_SIZE;
        unsigned offsetAfterIndirection = MethodTable::GetIndexAfterVtableIndirection(slot) * TARGET_POINTER_SIZE;
        int indirectionsCodeSize = (offsetOfIndirection > 2047 ? 12 : 4) + (offsetAfterIndirection > 2047 ? 12 : 4);
        int indirectionsDataSize = (offsetOfIndirection > 2047 ? 4 : 0) + (offsetAfterIndirection > 2047 ? 4 : 0);
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

    // ld  t4, 0(a0) : t4 = MethodTable pointer
    *(UINT32*)p = 0x00053e83; // VTABLECALL_STUB_FIRST_DWORD
    p += 4;

    if ((offsetOfIndirection > 2047) || (offsetAfterIndirection > 2047))
    {
        *(UINT32*)p = 0x00000317; // auipc t1, 0
        p += 4;
    }

    if (offsetOfIndirection > 2047)
    {
        uint dataOffset = 20 + (offsetAfterIndirection > 2047 ? 12 : 4);

        // lwu t3,dataOffset(t1)
        *(DWORD*)p = 0x00036e03 | ((UINT32)dataOffset << 20); p += 4;
        // add t4, t4, t3
        *(DWORD*)p = 0x01ce8eb3; p += 4;
        // ld t4, 0(t4)
        *(DWORD*)p = 0x000ebe83; p += 4;
    }
    else
    {
        // ld t4, offsetOfIndirection(t4)
        *(DWORD*)p = 0x000ebe83 | ((UINT32)offsetOfIndirection << 20); p += 4;
    }

    if (offsetAfterIndirection > 2047)
    {
        uint indirectionsCodeSize = (offsetOfIndirection > 2047 ? 12 : 4);
        uint indirectionsDataSize = (offsetOfIndirection > 2047 ? 4 : 0);
        uint dataOffset = 20 + indirectionsCodeSize + indirectionsDataSize;

        // lwu t3,dataOffset(t1)
        *(DWORD*)p = 0x00036e03 | ((UINT32)dataOffset << 20); p += 4;
        // add t4, t4, t3
        *(DWORD*)p = 0x01ce8eb3; p += 4;
        // ld t4, 0(t4)
        *(DWORD*)p = 0x000ebe83; p += 4;
    }
    else
    {
        // ld t4, offsetAfterIndirection(t4)
        *(DWORD*)p = 0x000ebe83 | ((UINT32)offsetAfterIndirection << 20); p += 4;
    }

    // jalr x0, t4, 0
    *(UINT32*)p = 0x000e8067; p += 4;

    // data labels:
    if (offsetOfIndirection > 2047)
    {
        *(UINT32*)p = (UINT32)offsetOfIndirection;
        p += 4;
    }
    if (offsetAfterIndirection > 2047)
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

#endif //DECLARE_DATA

#endif // _VIRTUAL_CALL_STUB_RISCV64_H
