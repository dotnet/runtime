// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// VirtualCallStubCpu.hpp
//

#ifndef _VIRTUAL_CALL_STUB_WASM_H
#define _VIRTUAL_CALL_STUB_WASM_H

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
     inline PCODE entryPoint()       { _ASSERTE("LookupStub::entryPoint not implemented on wasm"); return 0; }
     inline size_t token()           { _ASSERTE("LookupStub::token not implemented on wasm"); return 0; }
     inline size_t size()            { _ASSERTE("LookupStub::size not implemented on wasm"); return 0; }
};

// /* LookupHolders are the containers for LookupStubs, they provide for any alignment of
// stubs as necessary.  In the case of LookupStubs, alignment is necessary since
// LookupStubs are placed in a hash table keyed by token. */
struct LookupHolder
{
     static void InitializeStatic() { LIMITED_METHOD_CONTRACT; }

     void  Initialize(LookupHolder* pLookupHolderRX, PCODE resolveWorkerTarget, size_t dispatchToken);

     LookupStub*    stub()               { _ASSERTE("LookupHolder::stub not implemented on wasm"); return nullptr; }

     static LookupHolder*  FromLookupEntry(PCODE lookupEntry);
};


#endif // USES_LOOKUP_STUBS

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
        _ASSERTE("VTableCallStub::size not implemented on wasm");
        return 0;
    }

    inline PCODE entryPoint() const { _ASSERTE("VTableCallStub::entryPoint not implemented on wasm"); return 0; }

    inline size_t token()
    {
        _ASSERTE("VTableCallStub::token not implemented on wasm");
        return 0;
    }
};

/* VTableCallHolders are the containers for VTableCallStubs, they provide for any alignment of
stubs as necessary.  */
struct VTableCallHolder
{
    void  Initialize(unsigned slot);

    VTableCallStub* stub() { _ASSERTE("VTableCallHolder::stub not implemented on wasm"); return nullptr; }

    static size_t GetHolderSize(unsigned slot)
    {
           _ASSERTE("VTableCallHolder::GetHolderSize not implemented on wasm");
           return 0;
    }

    static VTableCallHolder* FromVTableCallEntry(PCODE entry)
    {
        _ASSERTE("VTableCallHolder::FromVTableCallEntry not implemented on wasm");
        return nullptr;
    }
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
     inline PCODE failEntryPoint()            { _ASSERTE("ResolveStub::failEntryPoint not implemented on wasm"); return 0; }
     inline PCODE resolveEntryPoint()         { _ASSERTE("ResolveStub::resolveEntryPoint not implemented on wasm"); return 0; }

     inline INT32*  pCounter()                { _ASSERTE("ResolveStub::pCounter not implemented on wasm"); return nullptr; }
     inline size_t  token()                   { _ASSERTE("ResolveStub::token not implemented on wasm"); return 0; }
     inline size_t  size()                    { _ASSERTE("ResolveStub::size not implemented on wasm"); return 0; }
};

/* ResolveHolders are the containers for ResolveStubs,  They provide
for any alignment of the stubs as necessary. The stubs are placed in a hash table keyed by
the token for which they are built.  Efficiency of access requires that this token be aligned.
For now, we have copied that field into the ResolveHolder itself, if the resolve stub is arranged such that
any of its inlined tokens (non-prehashed) is aligned, then the token field in the ResolveHolder
is not needed. */
struct ResolveHolder
{
    static void  InitializeStatic() { _ASSERTE("ResolveHolder::InitializeStatic not implemented on wasm"); }

    void  Initialize(ResolveHolder* pResolveHolderRX,
                     PCODE resolveWorkerTarget, PCODE patcherTarget,
                     size_t dispatchToken, UINT32 hashedToken,
                     void * cacheAddr, INT32 * counterAddr);

     ResolveStub* stub()      { _ASSERTE("ResolveHolder::stub not implemented on wasm"); return nullptr; }

     static ResolveHolder*  FromFailEntry(PCODE failEntry) { _ASSERTE("ResolveHolder::FromFailEntry not implemented on wasm"); return nullptr; }
     static ResolveHolder*  FromResolveEntry(PCODE resolveEntry) { _ASSERTE("ResolveHolder::FromResolveEntry not implemented on wasm"); return nullptr; }
};

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
    inline PCODE entryPoint()         { _ASSERTE("DispatchStub::entryPoint not implemented on wasm"); return 0; }

    inline size_t       expectedMT()  { _ASSERTE("DispatchStub::expectedMT not implemented on wasm"); return 0; }
    inline PCODE        implTarget()  { _ASSERTE("DispatchStub::implTarget not implemented on wasm"); return 0; }

    inline TADDR implTargetSlot(EntryPointSlots::SlotType *slotTypeRef) const
    {
         _ASSERTE("DispatchStub::implTargetSlot not implemented on wasm");
         return 0;
    }

    inline PCODE        failTarget()  { _ASSERTE("DispatchStub::failTarget not implemented on wasm"); return 0; }
    inline size_t       size()        { _ASSERTE("DispatchStub::size not implemented on wasm"); return 0; }
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
        _ASSERTE("DispatchHolder::InitializeStatic not implemented on wasm");
    }

    void  Initialize(DispatchHolder* pDispatchHolderRX, PCODE implTarget, PCODE failTarget, size_t expectedMT);

    DispatchStub* stub()      { _ASSERTE("DispatchHolder::stub not implemented on wasm"); return nullptr; }

    static DispatchHolder*  FromDispatchEntry(PCODE dispatchEntry) { _ASSERTE("DispatchHolder::FromDispatchEntry not implemented on wasm"); return nullptr; }
};

#endif // _VIRTUAL_CALL_STUB_WASM_H
