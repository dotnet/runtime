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

#ifdef DECLARE_DATA
#include "asmconstants.h"
#endif

#include <pshpack1.h>  // Since we are placing code, we want byte packing of the structs

#define USES_LOOKUP_STUBS	1

/*********************************************************************************************
Stubs that contain code are all part of larger structs called Holders.  There is a
Holder for each kind of stub, i.e XXXStub is contained with XXXHolder.  Holders are
essentially an implementation trick that allowed rearranging the code sequences more
easily while trying out different alternatives, and for dealing with any alignment
issues in a way that was mostly immune to the actually code sequences.  These Holders
should be revisited when the stub code sequences are fixed, since in many cases they
add extra space to a stub that is not really needed.

Stubs are placed in cache and hash tables.  Since unaligned access of data in memory
is very slow, the keys used in those tables should be aligned.  The things used as keys
typically also occur in the generated code, e.g. a token as an immediate part of an instruction.
For now, to avoid alignment computations as different code strategies are tried out, the key
fields are all in the Holders.  Eventually, many of these fields should be dropped, and the instruction
streams aligned so that the immediate fields fall on aligned boundaries.
*/

#if USES_LOOKUP_STUBS

struct LookupStub;
struct LookupHolder;

/*LookupStub**************************************************************************************
Virtual and interface call sites are initially setup to point at LookupStubs.
This is because the runtime type of the <this> pointer is not yet known,
so the target cannot be resolved.  Note: if the jit is able to determine the runtime type
of the <this> pointer, it should be generating a direct call not a virtual or interface call.
This stub pushes a lookup token onto the stack to identify the sought after method, and then
jumps into the EE (VirtualCallStubManager::ResolveWorkerStub) to effectuate the lookup and
transfer of control to the appropriate target method implementation, perhaps patching of the call site
along the way to point to a more appropriate stub.  Hence callsites that point to LookupStubs
get quickly changed to point to another kind of stub.
*/
struct LookupStub
{
    inline PCODE entryPoint()       { LIMITED_METHOD_CONTRACT; return (PCODE)&_entryPoint[0]; }
    inline size_t token()           { LIMITED_METHOD_CONTRACT; return _token; }
    inline size_t size()            { LIMITED_METHOD_CONTRACT; return sizeof(LookupStub); }

private:
    friend struct LookupHolder;

    // DispatchStub:: _entryPoint expects:
    //       ecx: object (the "this" pointer)
    //       eax: siteAddrForRegisterIndirect if this is a RegisterIndirect dispatch call
    BYTE    _entryPoint [2];    // 50           push    eax             ;save siteAddrForRegisterIndirect - this may be an indirect call
                                // 68           push
    size_t  _token;             // xx xx xx xx          32-bit constant
#ifdef STUB_LOGGING
    BYTE cntr2[2];              // ff 05        inc
    size_t* c_lookup;           // xx xx xx xx          [call_lookup_counter]
#endif //STUB_LOGGING
    BYTE part2 [1];             // e9           jmp
    DISPL   _resolveWorkerDispl;// xx xx xx xx          pc-rel displ
};

/* LookupHolders are the containers for LookupStubs, they provide for any alignment of
stubs as necessary.  In the case of LookupStubs, alignment is necessary since
LookupStubs are placed in a hash table keyed by token. */
struct LookupHolder
{
    static void InitializeStatic();

    void  Initialize(LookupHolder* pLookupHolderRX, PCODE resolveWorkerTarget, size_t dispatchToken);

    LookupStub*    stub()               { LIMITED_METHOD_CONTRACT;  return &_stub;    }

    static LookupHolder*  FromLookupEntry(PCODE lookupEntry);

private:
    friend struct LookupStub;

    BYTE align[(sizeof(void*)-(offsetof(LookupStub,_token)%sizeof(void*)))%sizeof(void*)];
    LookupStub _stub;
    BYTE pad[sizeof(void*) -
             ((sizeof(void*)-(offsetof(LookupStub,_token)%sizeof(void*))) +
              (sizeof(LookupStub))
             ) % sizeof(void*)];    //complete DWORD

    static_assert_no_msg((sizeof(void*) -
             ((sizeof(void*)-(offsetof(LookupStub,_token)%sizeof(void*))) +
              (sizeof(LookupStub))
             ) % sizeof(void*)) != 0);
};

#endif // USES_LOOKUP_STUBS

struct DispatchStub;
struct DispatchHolder;

/*DispatchStub**************************************************************************************
Monomorphic and mostly monomorphic call sites eventually point to DispatchStubs.
A dispatch stub has an expected type (expectedMT), target address (target) and fail address (failure).
If the calling frame does in fact have the <this> type be of the expected type, then
control is transferred to the target address, the method implementation.  If not,
then control is transferred to the fail address, a fail stub (see below) where a polymorphic
lookup is done to find the correct address to go to.

implementation note: Order, choice of instructions, and branch directions
should be carefully tuned since it can have an inordinate effect on performance.  Particular
attention needs to be paid to the effects on the BTB and branch prediction, both in the small
and in the large, i.e. it needs to run well in the face of BTB overflow--using static predictions.
Note that since this stub is only used for mostly monomorphic callsites (ones that are not, get patched
to something else), therefore the conditional jump "jne failure" is mostly not taken, and hence it is important
that the branch prediction statically predict this, which means it must be a forward jump.  The alternative
is to reverse the order of the jumps and make sure that the resulting conditional jump "je implTarget"
is statically predicted as taken, i.e a backward jump. The current choice was taken since it was easier
to control the placement of the stubs than control the placement of the jitted code and the stubs. */
struct DispatchStub
{
    inline PCODE        entryPoint()  { LIMITED_METHOD_CONTRACT;  return (PCODE)&_entryPoint[0]; }

    inline size_t       expectedMT()  { LIMITED_METHOD_CONTRACT;  return _expectedMT;     }
    inline PCODE        implTarget()  { LIMITED_METHOD_CONTRACT;  return (PCODE) &_implDispl + sizeof(DISPL) + _implDispl; }

    inline TADDR implTargetSlot(EntryPointSlots::SlotType *slotTypeRef) const
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(slotTypeRef != nullptr);

        *slotTypeRef = EntryPointSlots::SlotType_ExecutableRel32;
        return (TADDR)&_implDispl;
    }

    inline PCODE        failTarget()  { LIMITED_METHOD_CONTRACT;  return (PCODE) &_failDispl + sizeof(DISPL) + _failDispl; }
    inline size_t       size()        { LIMITED_METHOD_CONTRACT;  return sizeof(DispatchStub); }

private:
    friend struct DispatchHolder;

    // DispatchStub:: _entryPoint expects:
    //       ecx: object (the "this" pointer)
    //       eax: siteAddrForRegisterIndirect if this is a RegisterIndirect dispatch call
#ifndef STUB_LOGGING
    BYTE    _entryPoint [2];    // 81 39        cmp  [ecx],                   ; This is the place where we are going to fault on null this.
    size_t  _expectedMT;        // xx xx xx xx              expectedMT        ; If you change it, change also AdjustContextForVirtualStub in excep.cpp!!!
    BYTE    jmpOp1[2];          // 0f 85        jne
    DISPL   _failDispl;         // xx xx xx xx              failEntry         ;must be forward jmp for perf reasons
    BYTE jmpOp2;                // e9           jmp
    DISPL   _implDispl;         // xx xx xx xx              implTarget
#else //STUB_LOGGING
    BYTE    _entryPoint [2];    // ff 05        inc
    size_t* d_call;             // xx xx xx xx              [call_mono_counter]
    BYTE cmpOp [2];             // 81 39        cmp  [ecx],
    size_t  _expectedMT;        // xx xx xx xx              expectedMT
    BYTE jmpOp1[2];             // 0f 84        je
    DISPL   _implDispl;         // xx xx xx xx              implTarget        ;during logging, perf is not so important
    BYTE fail [2];              // ff 05        inc
    size_t* d_miss;             // xx xx xx xx      [miss_mono_counter]
    BYTE jmpFail;               // e9           jmp
    DISPL   _failDispl;         // xx xx xx xx              failEntry
#endif //STUB_LOGGING
};

/* DispatchHolders are the containers for DispatchStubs, they provide for any alignment of
stubs as necessary.  DispatchStubs are placed in a hashtable and in a cache.  The keys for both
are the pair expectedMT and token.  Efficiency of the of the hash table is not a big issue,
since lookups in it are fairly rare.  Efficiency of the cache is paramount since it is accessed frequently
o(see ResolveStub below).  Currently we are storing both of these fields in the DispatchHolder to simplify
alignment issues.  If inlineMT in the stub itself was aligned, then it could be the expectedMT field.
While the token field can be logically gotten by following the failure target to the failEntryPoint
of the ResolveStub and then to the token over there, for perf reasons of cache access, it is duplicated here.
This allows us to use DispatchStubs in the cache.  The alternative is to provide some other immutable struct
for the cache composed of the triplet (expectedMT, token, target) and some sort of reclaimation scheme when
they are thrown out of the cache via overwrites (since concurrency will make the obvious approaches invalid).
*/

/* @workaround for ee resolution - Since the EE does not currently have a resolver function that
does what we want, see notes in implementation of VirtualCallStubManager::Resolver, we are
using dispatch stubs to siumulate what we want.  That means that inlineTarget, which should be immutable
is in fact written.  Hence we have moved target out into the holder and aligned it so we can
atomically update it.  When we get a resolver function that does what we want, we can drop this field,
and live with just the inlineTarget field in the stub itself, since immutability will hold.*/
struct DispatchHolder
{
    static void InitializeStatic();

    void  Initialize(DispatchHolder* pDispatchHolderRX, PCODE implTarget, PCODE failTarget, size_t expectedMT);

    DispatchStub* stub()      { LIMITED_METHOD_CONTRACT;  return &_stub; }

    static DispatchHolder*  FromDispatchEntry(PCODE dispatchEntry);

private:
    // Force _implDispl to be aligned so that it is backpatchable for tiering
    BYTE align[(sizeof(void*) - (offsetof(DispatchStub, _implDispl) % sizeof(void*))) % sizeof(void*)];
    DispatchStub _stub;
    BYTE pad[(sizeof(void*) - (sizeof(DispatchStub) % sizeof(void*)) + offsetof(DispatchStub, _implDispl)) % sizeof(void*)];	//complete DWORD
};

struct ResolveStub;
struct ResolveHolder;

/*ResolveStub**************************************************************************************
Polymorphic call sites and monomorphic calls that fail end up in a ResolverStub.  There is only
one resolver stub built for any given token, even though there may be many call sites that
use that token and many distinct <this> types that are used in the calling call frames.  A resolver stub
actually has two entry points, one for polymorphic call sites and one for dispatch stubs that fail on their
expectedMT test.  There is a third part of the resolver stub that enters the ee when a decision should
be made about changing the callsite.  Therefore, we have defined the resolver stub as three distinct pieces,
even though they are actually allocated as a single contiguous block of memory.  These pieces are:

A ResolveStub has two entry points:

FailEntry - where the dispatch stub goes if the expected MT test fails.  This piece of the stub does
a check to see how often we are actually failing. If failures are frequent, control transfers to the
patch piece to cause the call site to be changed from a mostly monomorphic callsite
(calls dispatch stub) to a polymorphic callsize (calls resolve stub).  If failures are rare, control
transfers to the resolve piece (see ResolveStub).  The failEntryPoint decrements a counter
every time it is entered.  The ee at various times will add a large chunk to the counter.

ResolveEntry - does a lookup via in a cache by hashing the actual type of the calling frame s
<this> and the token identifying the (contract,method) pair desired.  If found, control is transferred
to the method implementation.  If not found in the cache, the token is pushed and the ee is entered via
the ResolveWorkerStub to do a full lookup and eventual transfer to the correct method implementation.  Since
there is a different resolve stub for every token, the token can be inlined and the token can be pre-hashed.
The effectiveness of this approach is highly sensitive to the effectiveness of the hashing algorithm used,
as well as its speed.  It turns out it is very important to make the hash function sensitive to all
of the bits of the method table, as method tables are laid out in memory in a very non-random way.  Before
making any changes to the code sequences here, it is very important to measure and tune them as perf
can vary greatly, in unexpected ways, with seeming minor changes.

Implementation note - Order, choice of instructions, and branch directions
should be carefully tuned since it can have an inordinate effect on performance.  Particular
attention needs to be paid to the effects on the BTB and branch prediction, both in the small
and in the large, i.e. it needs to run well in the face of BTB overflow--using static predictions.
Note that this stub is called in highly polymorphic cases, but the cache should have been sized
and the hash function chosen to maximize the cache hit case.  Hence the cmp/jcc instructions should
mostly be going down the cache hit route, and it is important that this be statically predicted as so.
Hence the 3 jcc instrs need to be forward jumps.  As structured, there is only one jmp/jcc that typically
gets put in the BTB since all the others typically fall straight thru.  Minimizing potential BTB entries
is important. */

struct ResolveStub
{
    inline PCODE failEntryPoint()           { LIMITED_METHOD_CONTRACT; return (PCODE)&_failEntryPoint[0];    }
    inline PCODE resolveEntryPoint()        { LIMITED_METHOD_CONTRACT; return (PCODE)&_resolveEntryPoint; }
    inline PCODE slowEntryPoint()           { LIMITED_METHOD_CONTRACT; return (PCODE)&_slowEntryPoint[0]; }

    inline INT32* pCounter()                { LIMITED_METHOD_CONTRACT; return _pCounter; }
    inline UINT32 hashedToken()             { LIMITED_METHOD_CONTRACT; return _hashedToken >> LOG2_PTRSIZE;    }
    inline size_t cacheAddress()            { LIMITED_METHOD_CONTRACT; return _cacheAddress;   }
    inline size_t token()                   { LIMITED_METHOD_CONTRACT; return _token;          }
    inline size_t size()                    { LIMITED_METHOD_CONTRACT; return sizeof(ResolveStub); }
#ifndef UNIX_X86_ABI
    inline static size_t offsetOfThisDeref(){ LIMITED_METHOD_CONTRACT; return offsetof(ResolveStub, part1) - offsetof(ResolveStub, _resolveEntryPoint); }
    inline size_t stackArgumentsSize()      { LIMITED_METHOD_CONTRACT; return _stackArgumentsSize; }
#endif

private:
    friend struct ResolveHolder;

    // ResolveStub::_failEntryPoint expects:
    //       ecx: object (the "this" pointer)
    //       eax: siteAddrForRegisterIndirect if this is a RegisterIndirect dispatch call
    BYTE   _failEntryPoint [2];     // 83 2d        sub
    INT32* _pCounter;               // xx xx xx xx          [counter],
    BYTE   part0 [2];               // 01                   01
                                    // 7c           jl
    BYTE toPatcher;                 // xx                   backpatcher     ;must be forward jump, for perf reasons
                                    //                                      ;fall into the resolver stub

    // ResolveStub::_resolveEntryPoint expects:
    //       ecx: object (the "this" pointer)
    //       eax: siteAddrForRegisterIndirect if this is a RegisterIndirect dispatch call
    BYTE    _resolveEntryPoint;     // 50           push    eax             ;save siteAddrForRegisterIndirect - this may be an indirect call
    BYTE    part1 [11];             // 8b 01        mov     eax,[ecx]       ;get the method table from the "this" pointer. This is the place
                                    //                                      ;    where we are going to fault on null this. If you change it,
                                    //                                      ;    change also AdjustContextForVirtualStub in excep.cpp!!!
                                    // 52           push    edx
                                    // 8b d0        mov     edx, eax
                                    // c1 e8 0C     shr     eax,12          ;we are adding upper bits into lower bits of mt
                                    // 03 c2        add     eax,edx
                                    // 35           xor     eax,
    UINT32  _hashedToken;           // xx xx xx xx              hashedToken ;along with pre-hashed token
    BYTE    part2 [1];              // 25           and     eax,
    size_t mask;                    // xx xx xx xx              cache_mask
    BYTE part3 [2];                 // 8b 80        mov     eax, [eax+
    size_t  _cacheAddress;          // xx xx xx xx                lookupCache]
#ifdef STUB_LOGGING
    BYTE cntr1[2];                  // ff 05        inc
    size_t* c_call;                 // xx xx xx xx          [call_cache_counter]
#endif //STUB_LOGGING
    BYTE part4 [2];                 // 3b 10        cmp     edx,[eax+
    // BYTE mtOffset;               //                          ResolverCacheElem.pMT]
    BYTE part5 [1];                 // 75           jne
    BYTE toMiss1;                   // xx                   miss            ;must be forward jump, for perf reasons
    BYTE part6 [2];                 // 81 78        cmp     [eax+
    BYTE tokenOffset;               // xx                        ResolverCacheElem.token],
    size_t  _token;                 // xx xx xx xx              token
    BYTE part7 [1];                 // 75           jne
    BYTE toMiss2;                   // xx                   miss            ;must be forward jump, for perf reasons
    BYTE part8 [2];                 // 8B 40 xx     mov     eax,[eax+
    BYTE targetOffset;              //                          ResolverCacheElem.target]
    BYTE part9 [6];                 // 5a           pop     edx
                                    // 83 c4 04     add     esp,4           ;throw away siteAddrForRegisterIndirect - we don't need it now
                                    // ff e0        jmp     eax
                                    //         miss:
    BYTE    miss [1];               // 5a           pop     edx             ; don't pop siteAddrForRegisterIndirect - leave it on the stack for use by ResolveWorkerChainLookupAsmStub and/or ResolveWorkerAsmStub
    BYTE    _slowEntryPoint[1];     // 68           push
    size_t  _tokenPush;             // xx xx xx xx          token
#ifdef STUB_LOGGING
    BYTE cntr2[2];                  // ff 05        inc
    size_t* c_miss;                 // xx xx xx xx          [miss_cache_counter]
#endif //STUB_LOGGING
    BYTE part10 [1];                // e9           jmp
    DISPL   _resolveWorkerDispl;    // xx xx xx xx          resolveWorker == ResolveWorkerChainLookupAsmStub or ResolveWorkerAsmStub
    BYTE  patch[1];                 // e8           call
    DISPL _backpatcherDispl;        // xx xx xx xx          backpatcherWorker  == BackPatchWorkerAsmStub
    BYTE  part11 [1];               // eb           jmp
    BYTE toResolveStub;             // xx                   resolveStub, i.e. go back to _resolveEntryPoint
#ifndef UNIX_X86_ABI
    size_t _stackArgumentsSize;     // xx xx xx xx
#endif
};

/* ResolveHolders are the containers for ResolveStubs,  They provide
for any alignment of the stubs as necessary. The stubs are placed in a hash table keyed by
the token for which they are built.  Efficiency of access requires that this token be aligned.
For now, we have copied that field into the ResolveHolder itself, if the resolve stub is arranged such that
any of its inlined tokens (non-prehashed) is aligned, then the token field in the ResolveHolder
is not needed. */
struct ResolveHolder
{
    static void  InitializeStatic();

    void  Initialize(ResolveHolder* pResolveHolderRX,
                     PCODE resolveWorkerTarget, PCODE patcherTarget,
                     size_t dispatchToken, UINT32 hashedToken,
                     void * cacheAddr, INT32 * counterAddr
#ifndef UNIX_X86_ABI
                     , size_t stackArgumentsSize
#endif
                     );

    ResolveStub* stub()      { LIMITED_METHOD_CONTRACT;  return &_stub; }

    static ResolveHolder*  FromFailEntry(PCODE failEntry);
    static ResolveHolder*  FromResolveEntry(PCODE resolveEntry);

private:
    //align _token in resolve stub

    BYTE align[(sizeof(void*)-((offsetof(ResolveStub,_token))%sizeof(void*)))%sizeof(void*)
#ifdef STUB_LOGGING // This turns out to be zero-sized in stub_logging case, and is an error. So round up.
               +sizeof(void*)
#endif
              ];

    ResolveStub _stub;

//#ifdef STUB_LOGGING // This turns out to be zero-sized in non stub_logging case, and is an error. So remove
    BYTE pad[(sizeof(void*)-((sizeof(ResolveStub))%sizeof(void*))+offsetof(ResolveStub,_token))%sizeof(void*)];	//fill out DWORD
//#endif
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

#ifdef STUB_LOGGING
extern size_t g_lookup_inline_counter;
extern size_t g_mono_call_counter;
extern size_t g_mono_miss_counter;
extern size_t g_poly_call_counter;
extern size_t g_poly_miss_counter;
#endif

/* Template used to generate the stub.  We generate a stub by allocating a block of
   memory and copy the template over it and just update the specific fields that need
   to be changed.
*/
LookupStub lookupInit;

void LookupHolder::InitializeStatic()
{
    static_assert_no_msg(((offsetof(LookupStub, _token)+offsetof(LookupHolder, _stub)) % sizeof(void*)) == 0);
    static_assert_no_msg((sizeof(LookupHolder) % sizeof(void*)) == 0);

    lookupInit._entryPoint [0]     = 0x50;
    lookupInit._entryPoint [1]     = 0x68;
    static_assert_no_msg(sizeof(lookupInit._entryPoint) == 2);
    lookupInit._token              = 0xcccccccc;
#ifdef STUB_LOGGING
    lookupInit.cntr2 [0]           = 0xff;
    lookupInit.cntr2 [1]           = 0x05;
    static_assert_no_msg(sizeof(lookupInit.cntr2) == 2);
    lookupInit.c_lookup            = &g_call_lookup_counter;
#endif //STUB_LOGGING
    lookupInit.part2 [0]           = 0xe9;
    static_assert_no_msg(sizeof(lookupInit.part2) == 1);
    lookupInit._resolveWorkerDispl = 0xcccccccc;
}

void  LookupHolder::Initialize(LookupHolder* pLookupHolderRX, PCODE resolveWorkerTarget, size_t dispatchToken)
{
    _stub = lookupInit;

    //fill in the stub specific fields
    //@TODO: Get rid of this duplication of data.
    _stub._token              = dispatchToken;
    _stub._resolveWorkerDispl = resolveWorkerTarget - ((PCODE) &pLookupHolderRX->_stub._resolveWorkerDispl + sizeof(DISPL));
}

LookupHolder* LookupHolder::FromLookupEntry(PCODE lookupEntry)
{
    LIMITED_METHOD_CONTRACT;
    LookupHolder* lookupHolder = (LookupHolder*) ( lookupEntry - offsetof(LookupHolder, _stub) - offsetof(LookupStub, _entryPoint)  );
    //    _ASSERTE(lookupHolder->_stub._entryPoint[0] == lookupInit._entryPoint[0]);
    return lookupHolder;
}


/* Template used to generate the stub.  We generate a stub by allocating a block of
   memory and copy the template over it and just update the specific fields that need
   to be changed.
*/
DispatchStub dispatchInit;

void DispatchHolder::InitializeStatic()
{
    // Check that _implDispl is aligned in the DispatchHolder for backpatching
    static_assert_no_msg(((offsetof(DispatchHolder, _stub) + offsetof(DispatchStub, _implDispl)) % sizeof(void*)) == 0);
    static_assert_no_msg((sizeof(DispatchHolder) % sizeof(void*)) == 0);

#ifndef STUB_LOGGING
    dispatchInit._entryPoint [0] = 0x81;
    dispatchInit._entryPoint [1] = 0x39;
    static_assert_no_msg(sizeof(dispatchInit._entryPoint) == 2);

    dispatchInit._expectedMT     = 0xcccccccc;
    dispatchInit.jmpOp1 [0]      = 0x0f;
    dispatchInit.jmpOp1 [1]      = 0x85;
    static_assert_no_msg(sizeof(dispatchInit.jmpOp1) == 2);

    dispatchInit._failDispl      = 0xcccccccc;
    dispatchInit.jmpOp2          = 0xe9;
    dispatchInit._implDispl      = 0xcccccccc;
#else //STUB_LOGGING
    dispatchInit._entryPoint [0] = 0xff;
    dispatchInit._entryPoint [1] = 0x05;
    static_assert_no_msg(sizeof(dispatchInit._entryPoint) == 2);

    dispatchInit.d_call          = &g_mono_call_counter;
    dispatchInit.cmpOp [0]       = 0x81;
    dispatchInit.cmpOp [1]       = 0x39;
    static_assert_no_msg(sizeof(dispatchInit.cmpOp) == 2);

    dispatchInit._expectedMT     = 0xcccccccc;
    dispatchInit.jmpOp1 [0]      = 0x0f;
    dispatchInit.jmpOp1 [1]      = 0x84;
    static_assert_no_msg(sizeof(dispatchInit.jmpOp1) == 2);

    dispatchInit._implDispl      = 0xcccccccc;
    dispatchInit.fail [0]        = 0xff;
    dispatchInit.fail [1]        = 0x05;
    static_assert_no_msg(sizeof(dispatchInit.fail) == 2);

    dispatchInit.d_miss          = &g_mono_miss_counter;
    dispatchInit.jmpFail         = 0xe9;
    dispatchInit._failDispl      = 0xcccccccc;
#endif //STUB_LOGGING
};

void  DispatchHolder::Initialize(DispatchHolder* pDispatchHolderRX, PCODE implTarget, PCODE failTarget, size_t expectedMT)
{
    _stub = dispatchInit;

    //fill in the stub specific fields
    _stub._expectedMT  = (size_t) expectedMT;
    _stub._failDispl   = failTarget - ((PCODE) &pDispatchHolderRX->_stub._failDispl + sizeof(DISPL));
    _stub._implDispl   = implTarget - ((PCODE) &pDispatchHolderRX->_stub._implDispl + sizeof(DISPL));
}

DispatchHolder* DispatchHolder::FromDispatchEntry(PCODE dispatchEntry)
{
    LIMITED_METHOD_CONTRACT;
    DispatchHolder* dispatchHolder = (DispatchHolder*) ( dispatchEntry - offsetof(DispatchHolder, _stub) - offsetof(DispatchStub, _entryPoint) );
    //    _ASSERTE(dispatchHolder->_stub._entryPoint[0] == dispatchInit._entryPoint[0]);
    return dispatchHolder;
}


/* Template used to generate the stub.  We generate a stub by allocating a block of
   memory and copy the template over it and just update the specific fields that need
   to be changed.
*/

ResolveStub resolveInit;

void ResolveHolder::InitializeStatic()
{
    //Check that _token is aligned in ResolveHolder
    static_assert_no_msg(((offsetof(ResolveHolder, _stub) + offsetof(ResolveStub, _token)) % sizeof(void*)) == 0);
    static_assert_no_msg((sizeof(ResolveHolder) % sizeof(void*)) == 0);

    resolveInit._failEntryPoint [0]    = 0x83;
    resolveInit._failEntryPoint [1]    = 0x2d;
    static_assert_no_msg(sizeof(resolveInit._failEntryPoint) == 2);

    resolveInit._pCounter              = (INT32 *) (size_t) 0xcccccccc;
    resolveInit.part0 [0]              = 0x01;
    resolveInit.part0 [1]              = 0x7c;
    static_assert_no_msg(sizeof(resolveInit.part0) == 2);

    resolveInit.toPatcher              = (offsetof(ResolveStub, patch) - (offsetof(ResolveStub, toPatcher) + 1)) & 0xFF;

    resolveInit._resolveEntryPoint     = 0x50;
    resolveInit.part1 [0]              = 0x8b;
    resolveInit.part1 [1]              = 0x01;
    resolveInit.part1 [2]              = 0x52;
    resolveInit.part1 [3]              = 0x8b;
    resolveInit.part1 [4]              = 0xd0;
    resolveInit.part1 [5]              = 0xc1;
    resolveInit.part1 [6]              = 0xe8;
    resolveInit.part1 [7]              = CALL_STUB_CACHE_NUM_BITS;
    resolveInit.part1 [8]              = 0x03;
    resolveInit.part1 [9]              = 0xc2;
    resolveInit.part1 [10]             = 0x35;
    static_assert_no_msg(sizeof(resolveInit.part1) == 11);

    resolveInit._hashedToken           = 0xcccccccc;
    resolveInit.part2 [0]              = 0x25;
    static_assert_no_msg(sizeof(resolveInit.part2) == 1);

    resolveInit.mask                   = (CALL_STUB_CACHE_MASK << LOG2_PTRSIZE);
    resolveInit.part3 [0]              = 0x8b;
    resolveInit.part3 [1]              = 0x80;;
    static_assert_no_msg(sizeof(resolveInit.part3) == 2);

    resolveInit._cacheAddress          = 0xcccccccc;
#ifdef STUB_LOGGING
    resolveInit.cntr1 [0]              = 0xff;
    resolveInit.cntr1 [1]              = 0x05;
    static_assert_no_msg(sizeof(resolveInit.cntr1) == 2);

    resolveInit.c_call                 = &g_poly_call_counter;
#endif //STUB_LOGGING
    resolveInit.part4 [0]              = 0x3b;
    resolveInit.part4 [1]              = 0x10;
    static_assert_no_msg(sizeof(resolveInit.part4) == 2);

    // resolveInit.mtOffset               = offsetof(ResolveCacheElem,pMT) & 0xFF;
    static_assert_no_msg(offsetof(ResolveCacheElem,pMT) == 0);

    resolveInit.part5 [0]              = 0x75;
    static_assert_no_msg(sizeof(resolveInit.part5) == 1);

    resolveInit.toMiss1                = offsetof(ResolveStub,miss)-(offsetof(ResolveStub,toMiss1)+1);

    resolveInit.part6 [0]              = 0x81;
    resolveInit.part6 [1]              = 0x78;
    static_assert_no_msg(sizeof(resolveInit.part6) == 2);

    resolveInit.tokenOffset            = offsetof(ResolveCacheElem,token) & 0xFF;

    resolveInit._token                 = 0xcccccccc;

    resolveInit.part7 [0]              = 0x75;
    static_assert_no_msg(sizeof(resolveInit.part7) == 1);

    resolveInit.part8 [0]              = 0x8b;
    resolveInit.part8 [1]              = 0x40;
    static_assert_no_msg(sizeof(resolveInit.part8) == 2);

    resolveInit.targetOffset           = offsetof(ResolveCacheElem,target) & 0xFF;

    resolveInit.toMiss2                = offsetof(ResolveStub,miss)-(offsetof(ResolveStub,toMiss2)+1);

    resolveInit.part9 [0]              = 0x5a;
    resolveInit.part9 [1]              = 0x83;
    resolveInit.part9 [2]              = 0xc4;
    resolveInit.part9 [3]              = 0x04;
    resolveInit.part9 [4]              = 0xff;
    resolveInit.part9 [5]              = 0xe0;
    static_assert_no_msg(sizeof(resolveInit.part9) == 6);

    resolveInit.miss [0]               = 0x5a;
//    resolveInit.miss [1]               = 0xb8;
//    resolveInit._hashedTokenMov        = 0xcccccccc;
    resolveInit._slowEntryPoint [0]    = 0x68;
    resolveInit._tokenPush             = 0xcccccccc;
#ifdef STUB_LOGGING
    resolveInit.cntr2 [0]              = 0xff;
    resolveInit.cntr2 [1]              = 0x05;
    resolveInit.c_miss                 = &g_poly_miss_counter;
#endif //STUB_LOGGING
    resolveInit.part10 [0]             = 0xe9;
    resolveInit._resolveWorkerDispl    = 0xcccccccc;

    resolveInit.patch [0]              = 0xe8;
    resolveInit._backpatcherDispl      = 0xcccccccc;
    resolveInit.part11 [0]             = 0xeb;
    resolveInit.toResolveStub          = (offsetof(ResolveStub, _resolveEntryPoint) - (offsetof(ResolveStub, toResolveStub) + 1)) & 0xFF;
};

void  ResolveHolder::Initialize(ResolveHolder* pResolveHolderRX,
                                PCODE resolveWorkerTarget, PCODE patcherTarget,
                                size_t dispatchToken, UINT32 hashedToken,
                                void * cacheAddr, INT32 * counterAddr
#ifndef UNIX_X86_ABI
                                , size_t stackArgumentsSize
#endif
                                )
{
    _stub = resolveInit;

    //fill in the stub specific fields
    _stub._pCounter           = counterAddr;
    _stub._hashedToken        = hashedToken << LOG2_PTRSIZE;
    _stub._cacheAddress       = (size_t) cacheAddr;
    _stub._token              = dispatchToken;
//    _stub._hashedTokenMov     = hashedToken;
    _stub._tokenPush          = dispatchToken;
    _stub._resolveWorkerDispl = resolveWorkerTarget - ((PCODE) &pResolveHolderRX->_stub._resolveWorkerDispl + sizeof(DISPL));
    _stub._backpatcherDispl   = patcherTarget       - ((PCODE) &pResolveHolderRX->_stub._backpatcherDispl   + sizeof(DISPL));
#ifndef UNIX_X86_ABI
    _stub._stackArgumentsSize = stackArgumentsSize;
#endif
}

ResolveHolder* ResolveHolder::FromFailEntry(PCODE failEntry)
{
    LIMITED_METHOD_CONTRACT;
    ResolveHolder* resolveHolder = (ResolveHolder*) ( failEntry - offsetof(ResolveHolder, _stub) - offsetof(ResolveStub, _failEntryPoint) );
    //    _ASSERTE(resolveHolder->_stub._resolveEntryPoint[0] == resolveInit._resolveEntryPoint[0]);
    return resolveHolder;
}

ResolveHolder* ResolveHolder::FromResolveEntry(PCODE resolveEntry)
{
    LIMITED_METHOD_CONTRACT;
    ResolveHolder* resolveHolder = (ResolveHolder*) ( resolveEntry - offsetof(ResolveHolder, _stub) - offsetof(ResolveStub, _resolveEntryPoint) );
    //    _ASSERTE(resolveHolder->_stub._resolveEntryPoint[0] == resolveInit._resolveEntryPoint[0]);
    return resolveHolder;
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

#endif //DECLARE_DATA

#endif // _VIRTUAL_CALL_STUB_X86_H
