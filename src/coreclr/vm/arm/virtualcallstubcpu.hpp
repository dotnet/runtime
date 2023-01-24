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
    inline PCODE entryPoint()       { LIMITED_METHOD_CONTRACT; return (PCODE)&_entryPoint[0] + THUMB_CODE; }
    inline size_t token()           { LIMITED_METHOD_CONTRACT; return _token; }
    inline size_t size()            { LIMITED_METHOD_CONTRACT; return sizeof(LookupStub); }

private:
    friend struct LookupHolder;
    const static int entryPointLen = 4;

    WORD    _entryPoint[entryPointLen];
    PCODE   _resolveWorkerTarget;   // xx xx xx xx               target address
    size_t  _token;	            // xx xx xx xx               32-bit constant
};

/* LookupHolders are the containers for LookupStubs, they provide for any alignment of
stubs as necessary.  In the case of LookupStubs, alignment is necessary since
LookupStubs are placed in a hash table keyed by token. */
struct LookupHolder
{
    static void InitializeStatic() { LIMITED_METHOD_CONTRACT; }

    void  Initialize(LookupHolder* pLookupHolderRX, PCODE resolveWorkerTarget, size_t dispatchToken);

    LookupStub*    stub()               { LIMITED_METHOD_CONTRACT;  return &_stub;    }

    static LookupHolder*  FromLookupEntry(PCODE lookupEntry);

private:
    friend struct LookupStub;

    LookupStub _stub;
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
    inline PCODE entryPoint()         { LIMITED_METHOD_CONTRACT;  return (PCODE)(&_entryPoint[0]) + THUMB_CODE; }

    inline size_t       expectedMT()  { LIMITED_METHOD_CONTRACT;  return _expectedMT;     }
    inline PCODE        implTarget()  { LIMITED_METHOD_CONTRACT;  return _implTarget; }

    inline TADDR implTargetSlot(EntryPointSlots::SlotType *slotTypeRef) const
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(slotTypeRef != nullptr);

        *slotTypeRef = EntryPointSlots::SlotType_Executable;
        return (TADDR)&_implTarget;
    }

    inline PCODE        failTarget()  { LIMITED_METHOD_CONTRACT;  return _failTarget; }
    inline size_t       size()        { LIMITED_METHOD_CONTRACT;  return sizeof(DispatchStub); }

private:
    friend struct DispatchHolder;
    const static int entryPointLen = 12;

    WORD _entryPoint[entryPointLen];
    size_t  _expectedMT;
    PCODE _failTarget;
    PCODE _implTarget;
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
    static void InitializeStatic()
    {
        LIMITED_METHOD_CONTRACT;

        // Check that _implTarget is aligned in the DispatchHolder for backpatching
        static_assert_no_msg(((offsetof(DispatchHolder, _stub) + offsetof(DispatchStub, _implTarget)) % sizeof(void *)) == 0);
    }

    void  Initialize(DispatchHolder* pDispatchHolderRX, PCODE implTarget, PCODE failTarget, size_t expectedMT);

    DispatchStub* stub()      { LIMITED_METHOD_CONTRACT;  return &_stub; }

    static DispatchHolder*  FromDispatchEntry(PCODE dispatchEntry);

private:
    //force expectedMT to be aligned since used as key in hash tables.
    DispatchStub _stub;
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
    inline PCODE failEntryPoint()            { LIMITED_METHOD_CONTRACT; return (PCODE)(&_failEntryPoint[0]) + THUMB_CODE;    }
    inline PCODE resolveEntryPoint()         { LIMITED_METHOD_CONTRACT; return (PCODE)(&_resolveEntryPoint[0]) + THUMB_CODE; }
    inline PCODE slowEntryPoint()            { LIMITED_METHOD_CONTRACT; return (PCODE)(&_slowEntryPoint[0]) + THUMB_CODE; }

    inline INT32*  pCounter()                { LIMITED_METHOD_CONTRACT; return _pCounter; }
    inline UINT32  hashedToken()             { LIMITED_METHOD_CONTRACT; return _hashedToken >> LOG2_PTRSIZE;    }
    inline size_t  cacheAddress()            { LIMITED_METHOD_CONTRACT; return _cacheAddress;   }
    inline size_t  token()                   { LIMITED_METHOD_CONTRACT; return _token;          }
    inline size_t  size()                    { LIMITED_METHOD_CONTRACT; return sizeof(ResolveStub); }

private:
    friend struct ResolveHolder;
    const static int resolveEntryPointLen = 32;
    const static int slowEntryPointLen = 4;
    const static int failEntryPointLen = 14;

    WORD _resolveEntryPoint[resolveEntryPointLen];
    WORD _slowEntryPoint[slowEntryPointLen];
    WORD _failEntryPoint[failEntryPointLen];
    INT32*  _pCounter;
    UINT32  _hashedToken;
    size_t  _cacheAddress; // lookupCache
    size_t  _token;
    size_t  _tokenSlow;
    PCODE   _resolveWorkerTarget;
    UINT32  _cacheMask;
};

/* ResolveHolders are the containers for ResolveStubs,  They provide
for any alignment of the stubs as necessary. The stubs are placed in a hash table keyed by
the token for which they are built.  Efficiency of access requires that this token be aligned.
For now, we have copied that field into the ResolveHolder itself, if the resolve stub is arranged such that
any of its inlined tokens (non-prehashed) is aligned, then the token field in the ResolveHolder
is not needed. */
struct ResolveHolder
{
    static void  InitializeStatic() { LIMITED_METHOD_CONTRACT; }

    void  Initialize(ResolveHolder* pResolveHolderRX,
                     PCODE resolveWorkerTarget, PCODE patcherTarget,
                     size_t dispatchToken, UINT32 hashedToken,
                     void * cacheAddr, INT32 * counterAddr);

    ResolveStub* stub()      { LIMITED_METHOD_CONTRACT;  return &_stub; }

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

#ifdef STUB_LOGGING
extern size_t g_lookup_inline_counter;
extern size_t g_mono_call_counter;
extern size_t g_mono_miss_counter;
extern size_t g_poly_call_counter;
extern size_t g_poly_miss_counter;
#endif

TADDR StubDispatchFrame_MethodFrameVPtr;

LookupHolder* LookupHolder::FromLookupEntry(PCODE lookupEntry)
{
    lookupEntry = lookupEntry & ~THUMB_CODE;
    return (LookupHolder*) ( lookupEntry - offsetof(LookupHolder, _stub) - offsetof(LookupStub, _entryPoint)  );
}


/* Template used to generate the stub.  We generate a stub by allocating a block of
   memory and copy the template over it and just update the specific fields that need
   to be changed.
*/
DispatchStub dispatchInit;

DispatchHolder* DispatchHolder::FromDispatchEntry(PCODE dispatchEntry)
{
    LIMITED_METHOD_CONTRACT;
    dispatchEntry = dispatchEntry & ~THUMB_CODE;
    DispatchHolder* dispatchHolder = (DispatchHolder*) ( dispatchEntry - offsetof(DispatchHolder, _stub) - offsetof(DispatchStub, _entryPoint) );
    //    _ASSERTE(dispatchHolder->_stub._entryPoint[0] == dispatchInit._entryPoint[0]);
    return dispatchHolder;
}


/* Template used to generate the stub.  We generate a stub by allocating a block of
   memory and copy the template over it and just update the specific fields that need
   to be changed.
*/

ResolveStub resolveInit;

ResolveHolder* ResolveHolder::FromFailEntry(PCODE failEntry)
{
    LIMITED_METHOD_CONTRACT;
    failEntry = failEntry & ~THUMB_CODE;
    ResolveHolder* resolveHolder = (ResolveHolder*) ( failEntry - offsetof(ResolveHolder, _stub) - offsetof(ResolveStub, _failEntryPoint) );
    //    _ASSERTE(resolveHolder->_stub._resolveEntryPoint[0] == resolveInit._resolveEntryPoint[0]);
    return resolveHolder;
}

ResolveHolder* ResolveHolder::FromResolveEntry(PCODE resolveEntry)
{
    LIMITED_METHOD_CONTRACT;
    resolveEntry = resolveEntry & ~THUMB_CODE;
    ResolveHolder* resolveHolder = (ResolveHolder*) ( resolveEntry - offsetof(ResolveHolder, _stub) - offsetof(ResolveStub, _resolveEntryPoint) );
    //    _ASSERTE(resolveHolder->_stub._resolveEntryPoint[0] == resolveInit._resolveEntryPoint[0]);
    return resolveHolder;
}

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
            if (firstWord == DISPATCH_STUB_FIRST_WORD)
            {
                WORD thirdWord = ((WORD*)pInstr)[2];
                if (thirdWord == 0xf84d)
                {
                    stubKind = SK_DISPATCH;
                }
                else if (thirdWord == 0xb460)
                {
                    stubKind = SK_RESOLVE;
                }
            }
            else if (firstWord == 0xf8df)
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
