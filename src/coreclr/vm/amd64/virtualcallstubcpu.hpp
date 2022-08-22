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

#include "dbginterface.h"

//#define STUB_LOGGING

#pragma pack(push, 1)
// since we are placing code, we want byte packing of the structs

// Codes of the instruction in the stub where the instruction access violation
// is converted to NullReferenceException at the caller site.
#ifdef UNIX_AMD64_ABI
#define X64_INSTR_CMP_IND_THIS_REG_RAX 0x073948
#define X64_INSTR_MOV_RAX_IND_THIS_REG 0x078b48
#else // UNIX_AMD64_ABI
#define X64_INSTR_CMP_IND_THIS_REG_RAX 0x013948
#define X64_INSTR_MOV_RAX_IND_THIS_REG 0x018b48
#endif // UNIX_AMD64_ABI

#define USES_LOOKUP_STUBS       1

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
    inline PCODE entryPoint()           { LIMITED_METHOD_CONTRACT; return (PCODE)&_entryPoint[0]; }

    inline size_t  token()              { LIMITED_METHOD_CONTRACT; return _token; }
    inline size_t  size()               { LIMITED_METHOD_CONTRACT; return sizeof(LookupStub); }

private:
    friend struct LookupHolder;

    // The lookup entry point starts with a nop in order to allow us to quickly see
    // if the stub is lookup stub or a dispatch stub.  We can read thye first byte
    // of a stub to find out what kind of a stub we have.

    BYTE    _entryPoint [3];      // 90                       nop
                                  // 48 B8                    mov    rax,
    size_t  _token;               // xx xx xx xx xx xx xx xx              64-bit address
    BYTE    part2 [3];            // 50                       push   rax
                                  // 48 B8                    mov    rax,
    size_t  _resolveWorkerAddr;   // xx xx xx xx xx xx xx xx              64-bit address
    BYTE    part3 [2];            // FF E0                    jmp    rax
};

/* LookupHolders are the containers for LookupStubs, they provide for any alignment of
stubs as necessary.  In the case of LookupStubs, alignment is necessary since
LookupStubs are placed in a hash table keyed by token. */
struct LookupHolder
{
    static void InitializeStatic();

    void  Initialize(LookupHolder* pLookupHolderRX, PCODE resolveWorkerTarget, size_t dispatchToken);

    LookupStub*    stub()         { LIMITED_METHOD_CONTRACT;  return &_stub;    }

    static LookupHolder* FromLookupEntry(PCODE lookupEntry);

private:
    friend struct LookupStub;

    LookupStub _stub;
};

#endif // USES_LOOKUP_STUBS

struct DispatchStub;
struct DispatchStubShort;
struct DispatchStubLong;
struct DispatchHolder;

/*DispatchStub**************************************************************************************
The structure of a full dispatch stub in memory is a DispatchStub followed contiguously in memory
by either a DispatchStubShort of a DispatchStubLong. DispatchStubShort is used when the resolve
stub (failTarget()) is reachable by a rel32 (DISPL) jump. We make a pretty good effort to make sure
that the stub heaps are set up so that this is the case. If we allocate enough stubs that the heap
end up allocating in a new block that is further away than a DISPL jump can go, then we end up using
a DispatchStubLong which is bigger but is a full 64-bit jump. */

/*DispatchStubShort*********************************************************************************
This is the logical continuation of DispatchStub for the case when the failure target is within
a rel32 jump (DISPL). */
struct DispatchStubShort
{
    friend struct DispatchHolder;
    friend struct DispatchStub;

    static BOOL isShortStub(LPCBYTE pCode);
    inline PCODE implTarget() const { LIMITED_METHOD_CONTRACT;  return (PCODE) _implTarget; }

    inline TADDR implTargetSlot() const
    {
        LIMITED_METHOD_CONTRACT;
        return (TADDR)&_implTarget;
    }

    inline PCODE failTarget() const { LIMITED_METHOD_CONTRACT;  return (PCODE) &_failDispl + sizeof(DISPL) + _failDispl; }

private:
    BYTE    part1 [2];            // 48 B8                    mov    rax,
    size_t  _implTarget;          // xx xx xx xx xx xx xx xx              64-bit address
    BYTE    part2[2];             // 0f 85                    jne
    DISPL   _failDispl;           // xx xx xx xx                     failEntry         ;must be forward jmp for perf reasons
    BYTE    part3 [2];            // FF E0                    jmp    rax
};

#define DispatchStubShort_offsetof_failDisplBase (offsetof(DispatchStubLong, _failDispl) + sizeof(DISPL))

inline BOOL DispatchStubShort::isShortStub(LPCBYTE pCode)
{
    LIMITED_METHOD_CONTRACT;
    return reinterpret_cast<DispatchStubShort const *>(pCode)->part2[0] == 0x0f;
}


/*DispatchStubLong**********************************************************************************
This is the logical continuation of DispatchStub for the case when the failure target is not
reachable by a rel32 jump (DISPL). */
struct DispatchStubLong
{
    friend struct DispatchHolder;
    friend struct DispatchStub;

    static inline BOOL isLongStub(LPCBYTE pCode);
    inline PCODE implTarget() const { LIMITED_METHOD_CONTRACT;  return (PCODE) _implTarget; }

    inline TADDR implTargetSlot() const
    {
        LIMITED_METHOD_CONTRACT;
        return (TADDR)&_implTarget;
    }

    inline PCODE failTarget() const { LIMITED_METHOD_CONTRACT;  return (PCODE) _failTarget; }

private:
    BYTE    part1[2];             // 48 B8                    mov    rax,
    size_t  _implTarget;          // xx xx xx xx xx xx xx xx              64-bit address
    BYTE    part2 [1];            // 75                       jne
    BYTE    _failDispl;           //    xx                           failLabel
    BYTE    part3 [2];            // FF E0                    jmp    rax
    // failLabel:
    BYTE    part4 [2];            // 48 B8                    mov    rax,
    size_t  _failTarget;          // xx xx xx xx xx xx xx xx              64-bit address
    BYTE    part5 [2];            // FF E0                    jmp    rax
};

#define DispatchStubLong_offsetof_failDisplBase (offsetof(DispatchStubLong, _failDispl) + sizeof(BYTE))
#define DispatchStubLong_offsetof_failLabel (offsetof(DispatchStubLong, part4[0]))

inline BOOL DispatchStubLong::isLongStub(LPCBYTE pCode)
{
    LIMITED_METHOD_CONTRACT;
    return reinterpret_cast<DispatchStubLong const *>(pCode)->part2[0] == 0x75;
}

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
    friend struct DispatchHolder;

    enum DispatchStubType
    {
        e_TYPE_SHORT,
        e_TYPE_LONG,
    };

    inline DispatchStubType type() const
    {
        LIMITED_METHOD_CONTRACT;
        CONSISTENCY_CHECK(DispatchStubShort::isShortStub(reinterpret_cast<LPCBYTE>(this + 1))
                          || DispatchStubLong::isLongStub(reinterpret_cast<LPCBYTE>(this + 1)));
        return DispatchStubShort::isShortStub((BYTE *)(this + 1)) ? e_TYPE_SHORT : e_TYPE_LONG;
    }

    inline static size_t size(DispatchStubType type)
    {
        STATIC_CONTRACT_LEAF;
        return sizeof(DispatchStub) +
            ((type == e_TYPE_SHORT) ? sizeof(DispatchStubShort) : sizeof(DispatchStubLong));
    }

    inline PCODE        entryPoint() const { LIMITED_METHOD_CONTRACT;  return (PCODE)&_entryPoint[0]; }
    inline size_t       expectedMT() const { LIMITED_METHOD_CONTRACT;  return _expectedMT;     }
    inline size_t       size()       const { WRAPPER_NO_CONTRACT; return size(type()); }

    inline PCODE implTarget() const
    {
        LIMITED_METHOD_CONTRACT;
        if (type() == e_TYPE_SHORT)
            return getShortStub()->implTarget();
        else
            return getLongStub()->implTarget();
    }

    inline TADDR implTargetSlot(EntryPointSlots::SlotType *slotTypeRef) const
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(slotTypeRef != nullptr);

        *slotTypeRef = EntryPointSlots::SlotType_Executable;
        if (type() == e_TYPE_SHORT)
            return getShortStub()->implTargetSlot();
        else
            return getLongStub()->implTargetSlot();
    }

    inline PCODE failTarget() const
    {
        if (type() == e_TYPE_SHORT)
            return getShortStub()->failTarget();
        else
            return getLongStub()->failTarget();
    }

private:
    inline DispatchStubShort const *getShortStub() const
        { LIMITED_METHOD_CONTRACT; return reinterpret_cast<DispatchStubShort const *>(this + 1); }

    inline DispatchStubLong const *getLongStub() const
        { LIMITED_METHOD_CONTRACT; return reinterpret_cast<DispatchStubLong const *>(this + 1); }

    BYTE    _entryPoint [2];      // 48 B8                    mov    rax,
    size_t  _expectedMT;          // xx xx xx xx xx xx xx xx              64-bit address
    BYTE    part1 [3];            // 48 39 XX                 cmp    [THIS_REG], rax
    BYTE    nopOp;                // 90                       nop                      ; 1-byte nop to align _implTarget

    // Followed by either DispatchStubShort or DispatchStubLong, depending
    // on whether we were able to make a rel32 or had to make an abs64 jump
    // to the resolve stub on failure.

};

/* DispatchHolders are the containers for DispatchStubs, they provide for any alignment of
stubs as necessary.  DispatchStubs are placed in a hashtable and in a cache.  The keys for both
are the pair expectedMT and token.  Efficiency of the of the hash table is not a big issue,
since lookups in it are fairly rare.  Efficiency of the cache is paramount since it is accessed frequently
(see ResolveStub below).  Currently we are storing both of these fields in the DispatchHolder to simplify
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

    void  Initialize(DispatchHolder* pDispatchHolderRX, PCODE implTarget, PCODE failTarget, size_t expectedMT,
                     DispatchStub::DispatchStubType type);

    static size_t GetHolderSize(DispatchStub::DispatchStubType type)
        { STATIC_CONTRACT_WRAPPER; return DispatchStub::size(type); }

    static BOOL CanShortJumpDispatchStubReachFailTarget(PCODE failTarget, LPCBYTE stubMemory)
    {
        STATIC_CONTRACT_WRAPPER;
        LPCBYTE pFrom = stubMemory + sizeof(DispatchStub) + DispatchStubShort_offsetof_failDisplBase;
        size_t cbRelJump = failTarget - (PCODE)pFrom;
        return FitsInI4(cbRelJump);
    }

    DispatchStub* stub()      { LIMITED_METHOD_CONTRACT;  return reinterpret_cast<DispatchStub *>(this); }

    static DispatchHolder* FromDispatchEntry(PCODE dispatchEntry);

private:
    // DispatchStub follows here. It is dynamically sized on allocation
    // because it could be a DispatchStubLong or a DispatchStubShort
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
    inline PCODE failEntryPoint()       { LIMITED_METHOD_CONTRACT; return (PCODE)&_failEntryPoint[0];    }
    inline PCODE resolveEntryPoint()    { LIMITED_METHOD_CONTRACT; return (PCODE)&_resolveEntryPoint[0]; }
    inline PCODE slowEntryPoint()       { LIMITED_METHOD_CONTRACT; return (PCODE)&_slowEntryPoint[0]; }

    inline INT32* pCounter()            { LIMITED_METHOD_CONTRACT; return _pCounter; }
    inline UINT32 hashedToken()         { LIMITED_METHOD_CONTRACT; return _hashedToken >> LOG2_PTRSIZE;    }
    inline size_t cacheAddress()        { LIMITED_METHOD_CONTRACT; return _cacheAddress;   }
    inline size_t token()               { LIMITED_METHOD_CONTRACT; return _token;          }
    inline size_t size()                { LIMITED_METHOD_CONTRACT; return sizeof(LookupStub); }

private:
    friend struct ResolveHolder;

    BYTE    _resolveEntryPoint[6];//                resolveStub:
                                  // 48 8B XX                 mov    rax, [THIS_REG]
                                  // 52                       push   rdx
                                  // 49 BA                    mov    r10,
    size_t  _cacheAddress;        // xx xx xx xx xx xx xx xx              64-bit address
    BYTE    part1 [12];           //                                                     ; Compute hash = ((MT + MT>>12) ^ prehash)
                                  // 48 8B D0                 mov    rdx, rax            ; rdx <- current MethodTable
                                  // 48 C1 E8 0C              shr    rax, 12
                                  // 48 03 C2                 add    rax, rdx
                                  // 48 35                    xor    rax,
    UINT32  _hashedToken;         // xx xx xx xx                          hashedtoken    ; xor with pre-hashed token
    BYTE    part2 [2];            // 48 25                    and    rax,
    UINT32  mask;                 // xx xx xx xx                          cache_mask     ; and with cache mask
    BYTE    part3 [6];            // 4A 8B 04 10              mov    rax, [r10 + rax]    ; get cache entry address
                                  // 49 BA                    mov    r10,
    size_t  _token;               // xx xx xx xx xx xx xx xx              64-bit address
    BYTE    part4 [3];            // 48 3B 50                 cmp    rdx, [rax+          ; compare our MT vs. cache MT
    BYTE    mtOffset;             // xx                                        ResolverCacheElem.pMT]
    BYTE    part5 [1];            // 75                       jne
    BYTE    toMiss1;              // xx                              miss                ; must be forward jump, for perf reasons
    BYTE    part6 [3];            // 4C 3B 50                 cmp    r10, [rax+          ; compare our token vs. cache token
    BYTE    tokenOffset;          // xx                                        ResolverCacheElem.token]
    BYTE    part7 [1];            // 75                       jne
    BYTE    toMiss2;              // xx                              miss                ; must be forward jump, for perf reasons
    BYTE    part8 [3];            // 48 8B 40                 mov    rax, [rax+          ; setup rax with method impl address
    BYTE    targetOffset;         // xx                                        ResolverCacheElem.target]
    BYTE    part9 [3];            // 5A                       pop    rdx
                                  // FF E0                    jmp    rax
                                  //                failStub:
    BYTE    _failEntryPoint [2];  // 48 B8                    mov    rax,
    INT32*  _pCounter;            // xx xx xx xx xx xx xx xx              64-bit address
    BYTE    part11 [4];           // 83 00 FF                 add    dword ptr [rax], -1
                                  // 7d                       jnl
    BYTE    toResolveStub1;       // xx                              resolveStub
    BYTE    part12 [4];           // 49 83 CB 01              or     r11, 1
    BYTE    _slowEntryPoint [3];  // 52             slow:     push   rdx
                                  // 49 BA                    mov    r10,
    size_t  _tokenSlow;           // xx xx xx xx xx xx xx xx              64-bit address
//  BYTE    miss [5];             // 5A             miss:     pop    rdx                 ; don't pop rdx
//                                // 41 52                    push   r10                 ; don't push r10 leave it setup with token
    BYTE    miss [3];             // 50                       push   rax                 ; push ptr to cache elem
                                  // 48 B8                    mov    rax,
    size_t  _resolveWorker;       // xx xx xx xx xx xx xx xx              64-bit address
    BYTE    part10 [2];           // FF E0                    jmp    rax
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
                     void * cacheAddr, INT32* counterAddr);

    ResolveStub* stub()      { LIMITED_METHOD_CONTRACT;  return &_stub; }

    static ResolveHolder* FromFailEntry(PCODE resolveEntry);
    static ResolveHolder* FromResolveEntry(PCODE resolveEntry);

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

LookupStub        lookupInit;
DispatchStub      dispatchInit;
DispatchStubShort dispatchShortInit;
DispatchStubLong  dispatchLongInit;
ResolveStub       resolveInit;

#define INSTR_INT3 0xcc
#define INSTR_NOP  0x90

#ifndef DACCESS_COMPILE

#include "asmconstants.h"

#ifdef STUB_LOGGING
extern size_t g_lookup_inline_counter;
extern size_t g_call_inline_counter;
extern size_t g_miss_inline_counter;
extern size_t g_call_cache_counter;
extern size_t g_miss_cache_counter;
#endif

/* Template used to generate the stub.  We generate a stub by allocating a block of
   memory and copy the template over it and just update the specific fields that need
   to be changed.
*/

void LookupHolder::InitializeStatic()
{
    static_assert_no_msg((sizeof(LookupHolder) % sizeof(void*)) == 0);

    // The first instruction of a LookupStub is nop
    // and we use it in order to differentiate the first two bytes
    // of a LookupStub and a ResolveStub
    lookupInit._entryPoint [0]     = INSTR_NOP;
    lookupInit._entryPoint [1]     = 0x48;
    lookupInit._entryPoint [2]     = 0xB8;
    lookupInit._token              = 0xcccccccccccccccc;
    lookupInit.part2 [0]           = 0x50;
    lookupInit.part2 [1]           = 0x48;
    lookupInit.part2 [2]           = 0xB8;
    lookupInit._resolveWorkerAddr  = 0xcccccccccccccccc;
    lookupInit.part3 [0]           = 0xFF;
    lookupInit.part3 [1]           = 0xE0;
}

void  LookupHolder::Initialize(LookupHolder* pLookupHolderRX, PCODE resolveWorkerTarget, size_t dispatchToken)
{
    _stub = lookupInit;

    //fill in the stub specific fields
    _stub._token              = dispatchToken;
    _stub._resolveWorkerAddr  = (size_t) resolveWorkerTarget;
}

/* Template used to generate the stub.  We generate a stub by allocating a block of
   memory and copy the template over it and just update the specific fields that need
   to be changed.
*/

void DispatchHolder::InitializeStatic()
{
    // Check that _implTarget is aligned in the DispatchStub for backpatching
    static_assert_no_msg(((sizeof(DispatchStub) + offsetof(DispatchStubShort, _implTarget)) % sizeof(void *)) == 0);
    static_assert_no_msg(((sizeof(DispatchStub) + offsetof(DispatchStubLong, _implTarget)) % sizeof(void *)) == 0);

    static_assert_no_msg(((sizeof(DispatchStub) + sizeof(DispatchStubShort)) % sizeof(void*)) == 0);
    static_assert_no_msg(((sizeof(DispatchStub) + sizeof(DispatchStubLong)) % sizeof(void*)) == 0);
    static_assert_no_msg((DispatchStubLong_offsetof_failLabel - DispatchStubLong_offsetof_failDisplBase) < INT8_MAX);

    // Common dispatch stub initialization
    dispatchInit._entryPoint [0]      = 0x48;
    dispatchInit._entryPoint [1]      = 0xB8;
    dispatchInit._expectedMT          = 0xcccccccccccccccc;
    dispatchInit.part1 [0]            = X64_INSTR_CMP_IND_THIS_REG_RAX & 0xff;
    dispatchInit.part1 [1]            = (X64_INSTR_CMP_IND_THIS_REG_RAX >> 8) & 0xff;
    dispatchInit.part1 [2]            = (X64_INSTR_CMP_IND_THIS_REG_RAX >> 16) & 0xff;
    dispatchInit.nopOp                = 0x90;

    // Short dispatch stub initialization
    dispatchShortInit.part1 [0]       = 0x48;
    dispatchShortInit.part1 [1]       = 0xb8;
    dispatchShortInit._implTarget     = 0xcccccccccccccccc;
    dispatchShortInit.part2 [0]       = 0x0F;
    dispatchShortInit.part2 [1]       = 0x85;
    dispatchShortInit._failDispl      = 0xcccccccc;
    dispatchShortInit.part3 [0]       = 0xFF;
    dispatchShortInit.part3 [1]       = 0xE0;

    // Long dispatch stub initialization
    dispatchLongInit.part1 [0]        = 0x48;
    dispatchLongInit.part1 [1]        = 0xb8;
    dispatchLongInit._implTarget      = 0xcccccccccccccccc;
    dispatchLongInit.part2 [0]        = 0x75;
    dispatchLongInit._failDispl       = BYTE(DispatchStubLong_offsetof_failLabel - DispatchStubLong_offsetof_failDisplBase);
    dispatchLongInit.part3 [0]        = 0xFF;
    dispatchLongInit.part3 [1]        = 0xE0;
        // failLabel:
    dispatchLongInit.part4 [0]        = 0x48;
    dispatchLongInit.part4 [1]        = 0xb8;
    dispatchLongInit._failTarget      = 0xcccccccccccccccc;
    dispatchLongInit.part5 [0]        = 0xFF;
    dispatchLongInit.part5 [1]        = 0xE0;
};

void  DispatchHolder::Initialize(DispatchHolder* pDispatchHolderRX, PCODE implTarget, PCODE failTarget, size_t expectedMT,
                               DispatchStub::DispatchStubType type)
{
    //
    // Initialize the common area
    //

    // initialize the static data
    *stub() = dispatchInit;

    // fill in the dynamic data
    stub()->_expectedMT  = expectedMT;

    //
    // Initialize the short/long areas
    //
    if (type == DispatchStub::e_TYPE_SHORT)
    {
        DispatchStubShort *shortStubRW = const_cast<DispatchStubShort *>(stub()->getShortStub());
        DispatchStubShort *shortStubRX = const_cast<DispatchStubShort *>(pDispatchHolderRX->stub()->getShortStub());

        // initialize the static data
        *shortStubRW = dispatchShortInit;

        // fill in the dynamic data
        size_t displ = (failTarget - ((PCODE) &shortStubRX->_failDispl + sizeof(DISPL)));
        CONSISTENCY_CHECK(FitsInI4(displ));
        shortStubRW->_failDispl   = (DISPL) displ;
        shortStubRW->_implTarget  = (size_t) implTarget;
        CONSISTENCY_CHECK((PCODE)&shortStubRX->_failDispl + sizeof(DISPL) + shortStubRX->_failDispl == failTarget);
    }
    else
    {
        CONSISTENCY_CHECK(type == DispatchStub::e_TYPE_LONG);
        DispatchStubLong *longStub = const_cast<DispatchStubLong *>(stub()->getLongStub());

        // initialize the static data
        *longStub = dispatchLongInit;

        // fill in the dynamic data
        longStub->_implTarget = implTarget;
        longStub->_failTarget = failTarget;
    }
}

/* Template used to generate the stub.  We generate a stub by allocating a block of
   memory and copy the template over it and just update the specific fields that need
   to be changed.
*/

void ResolveHolder::InitializeStatic()
{
    static_assert_no_msg((sizeof(ResolveHolder) % sizeof(void*)) == 0);

    resolveInit._resolveEntryPoint [0] = X64_INSTR_MOV_RAX_IND_THIS_REG & 0xff;
    resolveInit._resolveEntryPoint [1] = (X64_INSTR_MOV_RAX_IND_THIS_REG >> 8) & 0xff;
    resolveInit._resolveEntryPoint [2] = (X64_INSTR_MOV_RAX_IND_THIS_REG >> 16) & 0xff;
    resolveInit._resolveEntryPoint [3] = 0x52;
    resolveInit._resolveEntryPoint [4] = 0x49;
    resolveInit._resolveEntryPoint [5] = 0xBA;
    resolveInit._cacheAddress          = 0xcccccccccccccccc;
    resolveInit.part1 [ 0]             = 0x48;
    resolveInit.part1 [ 1]             = 0x8B;
    resolveInit.part1 [ 2]             = 0xD0;
    resolveInit.part1 [ 3]             = 0x48;
    resolveInit.part1 [ 4]             = 0xC1;
    resolveInit.part1 [ 5]             = 0xE8;
    resolveInit.part1 [ 6]             = CALL_STUB_CACHE_NUM_BITS;
    resolveInit.part1 [ 7]             = 0x48;
    resolveInit.part1 [ 8]             = 0x03;
    resolveInit.part1 [ 9]             = 0xC2;
    resolveInit.part1 [10]             = 0x48;
    resolveInit.part1 [11]             = 0x35;
// Review truncation from unsigned __int64 to UINT32 of a constant value.
#if defined(_MSC_VER)
#pragma warning(push)
#pragma warning(disable:4305 4309)
#endif // defined(_MSC_VER)

    resolveInit._hashedToken           = 0xcccccccc;

#if defined(_MSC_VER)
#pragma warning(pop)
#endif // defined(_MSC_VER)

    resolveInit.part2 [ 0]             = 0x48;
    resolveInit.part2 [ 1]             = 0x25;
    resolveInit.mask                   = CALL_STUB_CACHE_MASK*sizeof(void *);
    resolveInit.part3 [0]              = 0x4A;
    resolveInit.part3 [1]              = 0x8B;
    resolveInit.part3 [2]              = 0x04;
    resolveInit.part3 [3]              = 0x10;
    resolveInit.part3 [4]              = 0x49;
    resolveInit.part3 [5]              = 0xBA;
    resolveInit._token                 = 0xcccccccccccccccc;
    resolveInit.part4 [0]              = 0x48;
    resolveInit.part4 [1]              = 0x3B;
    resolveInit.part4 [2]              = 0x50;
    resolveInit.mtOffset               = offsetof(ResolveCacheElem,pMT) & 0xFF;
    resolveInit.part5 [0]              = 0x75;
    resolveInit.toMiss1                = (offsetof(ResolveStub,miss)-(offsetof(ResolveStub,toMiss1)+1)) & 0xFF;
    resolveInit.part6 [0]              = 0x4C;
    resolveInit.part6 [1]              = 0x3B;
    resolveInit.part6 [2]              = 0x50;
    resolveInit.tokenOffset            = offsetof(ResolveCacheElem,token) & 0xFF;
    resolveInit.part7 [0]              = 0x75;
    resolveInit.toMiss2                = (offsetof(ResolveStub,miss)-(offsetof(ResolveStub,toMiss2)+1)) & 0xFF;
    resolveInit.part8 [0]              = 0x48;
    resolveInit.part8 [1]              = 0x8B;
    resolveInit.part8 [2]              = 0x40;
    resolveInit.targetOffset           = offsetof(ResolveCacheElem,target) & 0xFF;
    resolveInit.part9 [0]              = 0x5A;
    resolveInit.part9 [1]              = 0xFF;
    resolveInit.part9 [2]              = 0xE0;
    resolveInit._failEntryPoint [0]    = 0x48;
    resolveInit._failEntryPoint [1]    = 0xB8;
    resolveInit._pCounter              = (INT32*) (size_t) 0xcccccccccccccccc;
    resolveInit.part11 [0]             = 0x83;
    resolveInit.part11 [1]             = 0x00;
    resolveInit.part11 [2]             = 0xFF;
    resolveInit.part11 [3]             = 0x7D;
    resolveInit.toResolveStub1         = (offsetof(ResolveStub, _resolveEntryPoint) - (offsetof(ResolveStub, toResolveStub1)+1)) & 0xFF;
    resolveInit.part12 [0]             = 0x49;
    resolveInit.part12 [1]             = 0x83;
    resolveInit.part12 [2]             = 0xCB;
    resolveInit.part12 [3]             = 0x01;
    resolveInit._slowEntryPoint [0]    = 0x52;
    resolveInit._slowEntryPoint [1]    = 0x49;
    resolveInit._slowEntryPoint [2]    = 0xBA;
    resolveInit._tokenSlow             = 0xcccccccccccccccc;
    resolveInit.miss [0]               = 0x50;
    resolveInit.miss [1]               = 0x48;
    resolveInit.miss [2]               = 0xB8;
    resolveInit._resolveWorker         = 0xcccccccccccccccc;
    resolveInit.part10 [0]             = 0xFF;
    resolveInit.part10 [1]             = 0xE0;
};

void  ResolveHolder::Initialize(ResolveHolder* pResolveHolderRX,
                                PCODE resolveWorkerTarget, PCODE patcherTarget,
                                size_t dispatchToken, UINT32 hashedToken,
                                void * cacheAddr, INT32* counterAddr)
{
    _stub = resolveInit;

    //fill in the stub specific fields
    _stub._cacheAddress       = (size_t) cacheAddr;
    _stub._hashedToken        = hashedToken << LOG2_PTRSIZE;
    _stub._token              = dispatchToken;
    _stub._tokenSlow          = dispatchToken;
    _stub._resolveWorker      = (size_t) resolveWorkerTarget;
    _stub._pCounter           = counterAddr;
}

ResolveHolder* ResolveHolder::FromFailEntry(PCODE failEntry)
{
    LIMITED_METHOD_CONTRACT;
    ResolveHolder* resolveHolder = (ResolveHolder*) ( failEntry - offsetof(ResolveHolder, _stub) - offsetof(ResolveStub, _failEntryPoint) );
    _ASSERTE(resolveHolder->_stub._resolveEntryPoint[1] == resolveInit._resolveEntryPoint[1]);
    return resolveHolder;
}

#endif // DACCESS_COMPILE

LookupHolder* LookupHolder::FromLookupEntry(PCODE lookupEntry)
{
    LIMITED_METHOD_CONTRACT;
    LookupHolder* lookupHolder = (LookupHolder*) ( lookupEntry - offsetof(LookupHolder, _stub) - offsetof(LookupStub, _entryPoint)  );
    _ASSERTE(lookupHolder->_stub._entryPoint[2] == lookupInit._entryPoint[2]);
    return lookupHolder;
}


DispatchHolder* DispatchHolder::FromDispatchEntry(PCODE dispatchEntry)
{
    LIMITED_METHOD_CONTRACT;
    DispatchHolder* dispatchHolder = (DispatchHolder*) ( dispatchEntry - offsetof(DispatchStub, _entryPoint) );
    _ASSERTE(dispatchHolder->stub()->_entryPoint[1] == dispatchInit._entryPoint[1]);
    return dispatchHolder;
}


ResolveHolder* ResolveHolder::FromResolveEntry(PCODE resolveEntry)
{
    LIMITED_METHOD_CONTRACT;
    ResolveHolder* resolveHolder = (ResolveHolder*) ( resolveEntry - offsetof(ResolveHolder, _stub) - offsetof(ResolveStub, _resolveEntryPoint) );
    _ASSERTE(resolveHolder->_stub._resolveEntryPoint[1] == resolveInit._resolveEntryPoint[1]);
    return resolveHolder;
}

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

        if (firstWord == 0xB848)
        {
            stubKind = SK_DISPATCH;
        }
        else if (firstWord == 0x4890)
        {
            stubKind = SK_LOOKUP;
        }
        else if (firstWord == 0x4952)
        {
            stubKind = SK_RESOLVE;
        }
        else if (firstWord == 0x48F8)
        {
            stubKind = SK_LOOKUP;
        }
        else if (firstWord == 0x8B48)
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
