//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//
// VirtualCallStubCpu.hpp
//
#ifndef _VIRTUAL_CALL_STUB_ARM_H
#define _VIRTUAL_CALL_STUB_ARM_H

#define DISPATCH_STUB_FIRST_DWORD 0xf9400008
#define RESOLVE_STUB_FIRST_DWORD 0xF940000C

struct ARM64EncodeHelpers
{
     inline static DWORD ADR_PATCH(DWORD offset)
     {
        DWORD immLO = (offset & 0x03)<<29 ;
  
        if (immLO ==0 )
            return  (offset<<3);
        else 
            return immLO<<29 | (offset -immLO)<<3;
     }

};

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

    void  Initialize(PCODE resolveWorkerTarget, size_t dispatchToken) 
    {
        // adr x9, _resolveWorkerTarget
        // ldp x10, x12, [x9]
        // br x10
        // _resolveWorkerTarget
        // _token
        _stub._entryPoint[0] = 0x10000089;
        _stub._entryPoint[1] = 0xa940312a;
        _stub._entryPoint[2] = 0xd61f0140;
        //4th element of _entryPoint array is padding for 8byte alignment
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
    static void InitializeStatic() { }

    void  Initialize(PCODE implTarget, PCODE failTarget, size_t expectedMT)
    {
        // ldr x8, [x0] ; methodTable from object in x0
        // adr x9, _expectedMT ; _expectedMT is at offset 28 from pc
        // ldp x10, x12, [x9] ; x10 = _expectedMT & x12 = _implTarget
        // cmp x8, x10
        // bne failLabel
        // br x12
        // failLabel
        // ldr x9, _failTarget ; _failTarget is at offset 24 from pc
        // br x9
        // _expectedMT
        // _implTarget
        // _failTarget

        _stub._entryPoint[0] = DISPATCH_STUB_FIRST_DWORD; // 0xf9400008
        _stub._entryPoint[1] = 0x100000e9;
        _stub._entryPoint[2] = 0xa940312a;
        _stub._entryPoint[3] = 0xeb0a011f;
        _stub._entryPoint[4] = 0x54000041;
        _stub._entryPoint[5] = 0xd61f0180;
        _stub._entryPoint[6] = 0x580000c9;
        _stub._entryPoint[7] = 0xd61f0120;

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
    inline PCODE resolveEntryPoint()       { LIMITED_METHOD_CONTRACT; return (PCODE)&_resolveEntryPoint[0]; }
    inline PCODE slowEntryPoint()          { LIMITED_METHOD_CONTRACT; return (PCODE)&_slowEntryPoint[0]; }
    inline size_t  token()                   { LIMITED_METHOD_CONTRACT; return _token; }
    inline INT32*  pCounter()                { LIMITED_METHOD_CONTRACT; return _pCounter; }

    inline UINT32  hashedToken()             { LIMITED_METHOD_CONTRACT; return _hashedToken >> LOG2_PTRSIZE;    }
    inline size_t  cacheAddress()            { LIMITED_METHOD_CONTRACT; return _cacheAddress;   }
    inline size_t  size()                    { LIMITED_METHOD_CONTRACT; return sizeof(ResolveStub); }

private:
    friend struct ResolveHolder;
    const static int resolveEntryPointLen = 19;
    const static int slowEntryPointLen = 4;
    const static int failEntryPointLen = 8;
    
    DWORD _resolveEntryPoint[resolveEntryPointLen];
    DWORD _slowEntryPoint[slowEntryPointLen];
    DWORD _failEntryPoint[failEntryPointLen];
    INT32*  _pCounter;               //Base of the Data Region
    size_t  _cacheAddress; // lookupCache
    size_t  _token;
    PCODE   _resolveWorkerTarget;
    UINT32  _hashedToken;
};

struct ResolveHolder
{
    static void  InitializeStatic() { }

    void Initialize(PCODE resolveWorkerTarget, PCODE patcherTarget, 
                    size_t dispatchToken, UINT32 hashedToken,
                    void * cacheAddr, INT32 * counterAddr)
    {
         int n=0;
         DWORD offset;
         int br_nextEntry[2];
/******** Rough Convention of used in this routine
         ;;x9 current variable
         ;;x10 base address of the data region
         ;;x11 indirection cell
         ;;x12 passed MT
         ;;X13 data read from data region
         ;;X14 computed address into data region
         ;;X15 this._token
         ;;cachemask => [CALL_STUB_CACHE_MASK * sizeof(void*)]
*********/         
         // Called directly by JITTED code
         // ResolveStub._resolveEntryPoint(x0:Object*, x1 ...,r7, x11:IndirectionCellAndFlags)
         // {
         //    MethodTable mt = x0.m_pMethTab;
         //    int i = ((mt + mt >> 12) ^ this._hashedToken) & _cacheMask
         //    ResolveCacheElem e = this._cacheAddress + i
         //    do
         //    {
         //        if (mt == e.pMT && this._token == e.token) (e.target)(x0, x1,...,x7);
         //        e = e.pNext;
         //    } while (e != null)
         //    (this._slowEntryPoint)(x0, x1,.., x7, x11);
         // }
         //

#define Dataregionbase  _pCounter      
#define DATA_OFFSET(_fieldHigh) (DWORD)((offsetof(ResolveStub, _fieldHigh ) - offsetof(ResolveStub, Dataregionbase)) & 0xffffffff)
#define PC_REL_OFFSET(_field) (DWORD)((offsetof(ResolveStub, _field) - (offsetof(ResolveStub, _resolveEntryPoint[n]))) & 0xffffffff)

         //ldr x12, [x0,#Object.m_pMethTab ] ; methodTable from object in x0
         _stub._resolveEntryPoint[n++] = RESOLVE_STUB_FIRST_DWORD; //0xF940000C
         
         //  ;; Compute i = ((mt + mt >> 12) ^ this._hashedToken) & _cacheMask
        
         //add x9, x12, x12 lsr #12
         _stub._resolveEntryPoint[n++] = 0x8B4C3189;

         //;;adr x10, #Dataregionbase of ResolveStub 
         _stub._resolveEntryPoint[n++] = 0x1000000A | ARM64EncodeHelpers::ADR_PATCH(PC_REL_OFFSET(Dataregionbase));

         //w13- this._hashedToken
         //ldr w13, [x10 + DATA_OFFSET(_hashedToken)]
         offset = DATA_OFFSET(_hashedToken);
         _ASSERTE(offset >=0 && offset%8 == 0);
         _stub._resolveEntryPoint[n++] = 0xB940014D | offset<<7;

         //eor x9,x9,x13
         _stub._resolveEntryPoint[n++] = 0xCA0D0129;

         _ASSERTE(CALL_STUB_CACHE_MASK * sizeof(void*) == 0x7FF8);
         //x9-i
         //and x9,x9,#cachemask
         _stub._resolveEntryPoint[n++] = 0x927D2D29;

         //;; ResolveCacheElem e = this._cacheAddress + i
         //
         //ldr x13, [x10 + DATA_OFFSET(_cacheAddress)]
         offset=DATA_OFFSET(_cacheAddress);
         _ASSERTE(offset >=0 && offset%8 == 0);
         _stub._resolveEntryPoint[n++] = 0xF940014D | offset<<7;
         
         //ldr x9, [x13, x9] ;; x9 = e = this._cacheAddress + i
         _stub._resolveEntryPoint[n++] = 0xF86969A9  ;

         //hoisting loop invariant this._token F940014F
         //
         //ldr x15, [x10 + DATA_OFFSET(_token)]
         offset = DATA_OFFSET(_token);
         _ASSERTE(offset >=0 && offset%8 == 0);
         _stub._resolveEntryPoint[n++] = 0xF940014F | offset<<7;

         int loop=n;
         //do {

         //;; Check mt == e.pMT
         //
         //
         //ldr x13, [x9, #offsetof(ResolveCacheElem, pMT) ]
         offset = offsetof(ResolveCacheElem, pMT) & 0x000001ff;
         _ASSERTE(offset >=0 && offset%8 == 0);
         _stub._resolveEntryPoint[n++] = 0xF940012D | offset<<7;
         
         //cmp x12, x13
         _stub._resolveEntryPoint[n++] = 0xEB0D019F;

         //;; bne nextEntry 
         //place holder for the above instruction
         br_nextEntry[0]=n++;
         
         //;; Check this._token == e.token
         //x15: this._token
         //
         //ldr x13, [x9, #offsetof(ResolveCacheElem, token) ]
         offset = offsetof(ResolveCacheElem, token) & 0xffffffff;
         _ASSERTE(offset >=0 && offset%8 == 0);
         _stub._resolveEntryPoint[n++] = 0xF940012D | offset<<7;

         //cmp x15, x13
         _stub._resolveEntryPoint[n++] = 0xEB0D01FF;

         //;; bne nextEntry
         //place holder for the above instruction
         br_nextEntry[1]=n++;

         //ldr x12, [x9, #offsetof(ResolveCacheElem, target) ]
         offset = offsetof(ResolveCacheElem, target) & 0xffffffff;
         _ASSERTE(offset >=0 && offset%8 == 0);
         _stub._resolveEntryPoint[n++] = 0xF940012C | offset<<7;
         
         // ;; Branch to e.target
         // br x12
         _stub._resolveEntryPoint[n++] = 0xD61F0180;

         //;;nextEntry:
         //back patching the call sites as now we know the offset to nextEntry
         //bne #offset
         for(auto i: br_nextEntry)
         {
            _stub._resolveEntryPoint[i] = 0x54000001 | ((((n-i)*sizeof(DWORD))<<3) & 0x3FFFFFF);  
         }
 
         //;; e = e.pNext;
         //ldr x9, [x9, #offsetof(ResolveCacheElem,pNext ) ]
         offset = offsetof(ResolveCacheElem, pNext) & 0xffffffff;
         _ASSERTE(offset >=0 && offset%8 == 0);
         _stub._resolveEntryPoint[n++] = 0xF9400129 | offset<<7;
         
         //;; } while(e != null);
         //;; cbnz x9, loop 
         offset = (DWORD)(loop - n)*sizeof(DWORD);
         _stub._resolveEntryPoint[n++] = 0xB5000009 | ((offset<<3) & 0xFFFFF0);
         

         _ASSERTE(n == ResolveStub::resolveEntryPointLen);
         _ASSERTE(_stub._resolveEntryPoint + n == _stub._slowEntryPoint);
         
         // ResolveStub._slowEntryPoint(x0:MethodToken, x1..x7, x11:IndirectionCellAndFlags)
         // {
         //     x12 = this._token;
         //     this._resolveWorkerTarget(x0,.., x7, x12);
         // }

#undef PC_REL_OFFSET
#define PC_REL_OFFSET(_field) (DWORD)((offsetof(ResolveStub, _field) - (offsetof(ResolveStub, _slowEntryPoint[n]))) & 0xffffffff )
         n = 0;
         // ;;slowEntryPoint:
         // ;;fall through to the slow case

         //;;adr x10, #Dataregionbase 
         _stub._slowEntryPoint[n++] = 0x1000000A | ARM64EncodeHelpers::ADR_PATCH(PC_REL_OFFSET(Dataregionbase));

         //ldr x12, [x10 , DATA_OFFSET(_token)]
         offset=DATA_OFFSET(_token);
         _ASSERTE(offset >=0 && offset%8 == 0);
         _stub._slowEntryPoint[n++] = 0xF940014C | (offset<<7);
         
         //
         //ldr x9, [x10 , DATA_OFFSET(_resolveWorkerTarget)]
         offset=DATA_OFFSET(_resolveWorkerTarget);
         _ASSERTE(offset >=0 && offset%8 == 0);
         _stub._slowEntryPoint[n++] = 0xF9400149 | (offset<<7);
         
         //  br x9 
         _stub._slowEntryPoint[n++] = 0xD61F0120;

         _ASSERTE(n == ResolveStub::slowEntryPointLen);
         // ResolveStub._failEntryPoint(x0:MethodToken, x1,.., x7, x11:IndirectionCellAndFlags)
         // {
         //     if(--*(this._pCounter) < 0) x11 = x11 | SDF_ResolveBackPatch;
         //     this._resolveEntryPoint(x0,..,x7);
         // }

#undef PC_REL_OFFSET //NOTE Offset can be negative
#define PC_REL_OFFSET(_field) (DWORD)((offsetof(ResolveStub, _field) - (offsetof(ResolveStub, _failEntryPoint[n]))) & 0xffffffff)
         n = 0;


         //;;failEntryPoint
         //;;adr x10, #Dataregionbase 
         _stub._failEntryPoint[n++] = 0x1000000A | ARM64EncodeHelpers::ADR_PATCH(PC_REL_OFFSET(Dataregionbase));
         
         //
         //ldr x13, [x10]
         offset=DATA_OFFSET(_pCounter);
         _ASSERTE(offset >=0 && offset%8 == 0);
         _stub._failEntryPoint[n++] = 0xF940014D | offset<<7; 

         //ldr x9, [x13]
         _stub._failEntryPoint[n++] = 0xF94001A9;
         //subs x9,x9,#1
         _stub._failEntryPoint[n++] = 0xF1000529;
         //str x9, [x13]
         _stub._failEntryPoint[n++] = 0xF90001A9;

         //;;bge resolveEntryPoint
         offset = PC_REL_OFFSET(_resolveEntryPoint);
         _stub._failEntryPoint[n++] = 0x5400000A | ((offset <<3)& 0x00FFFFF0) ;
         
         // ;; orr x11, x11, SDF_ResolveBackPatch
         // orr x11, x11, #1
         _ASSERTE(SDF_ResolveBackPatch == 0x1);
         _stub._failEntryPoint[n++] = 0xB240016B;

         //;;b resolveEntryPoint:
         offset = PC_REL_OFFSET(_resolveEntryPoint);
         _stub._failEntryPoint[n++] = 0x14000000 | ((offset>>2) & 0x3FFFFFF);
         
         _ASSERTE(n == ResolveStub::failEntryPointLen);
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


#endif // DACCESS_COMPILE

VirtualCallStubManager::StubKind VirtualCallStubManager::predictStubKind(PCODE stubStartAddress)
{

    SUPPORTS_DAC;
#ifdef DACCESS_COMPILE

    _ASSERTE(!"ARM64:NYI");
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

        if (firstDword == DISPATCH_STUB_FIRST_DWORD) // assembly of first instruction of DispatchStub : ldr x8, [x0]
        {
            stubKind = SK_DISPATCH;
        }
        else if (firstDword == RESOLVE_STUB_FIRST_DWORD) // assembly of first instruction of ResolveStub : ldr x12, [x0,#Object.m_pMethTab ]
        {
            stubKind = SK_RESOLVE;
        }
        else if (firstDword == 0x10000089) // assembly of first instruction of LookupStub : adr x9, _resolveWorkerTarget
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
