// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: VirtualCallStub.h
//



//
// See code:VirtualCallStubManager for details
//
// ============================================================================

#ifndef _VIRTUAL_CALL_STUB_H 
#define _VIRTUAL_CALL_STUB_H

#ifndef CROSSGEN_COMPILE

#define CHAIN_LOOKUP

#if defined(_TARGET_X86_)
// If this is uncommented, leaves a file "StubLog_<pid>.log" with statistics on the behavior
// of stub-based interface dispatch.
//#define STUB_LOGGING
#endif

#include "stubmgr.h"

/////////////////////////////////////////////////////////////////////////////////////
// Forward class declarations
class FastTable;
class BucketTable;
class Entry;
class Prober;
class VirtualCallStubManager;
class VirtualCallStubManagerManager;
struct LookupHolder;
struct DispatchHolder;
struct ResolveHolder;
struct VTableCallHolder;

/////////////////////////////////////////////////////////////////////////////////////
// Forward function declarations
extern "C" void InContextTPQuickDispatchAsmStub();

#ifdef FEATURE_PREJIT
extern "C" PCODE STDCALL StubDispatchFixupWorker(TransitionBlock * pTransitionBlock, 
                                                 TADDR siteAddrForRegisterIndirect,
                                                 DWORD sectionIndex, 
                                                 Module * pModule);
#endif

extern "C" PCODE STDCALL VSD_ResolveWorker(TransitionBlock * pTransitionBlock,
                                           TADDR siteAddrForRegisterIndirect,
                                           size_t token
#ifndef _TARGET_X86_
                                           , UINT_PTR flags
#endif                               
                                           );


/////////////////////////////////////////////////////////////////////////////////////
#if defined(_TARGET_X86_) || defined(_TARGET_AMD64_)
typedef INT32 DISPL;
#endif

/////////////////////////////////////////////////////////////////////////////////////
// Represents the struct that is added to the resolve cache
// NOTE: If you change the layout of this struct, you'll need to update various
//       ASM helpers in VirtualCallStubCpu that rely on offsets of members.
//
struct ResolveCacheElem
{
    void *pMT;
    size_t token;   // DispatchToken
    void *target;

    // These are used for chaining
    ResolveCacheElem *pNext;
    ResolveCacheElem *Next()
    { LIMITED_METHOD_CONTRACT; return VolatileLoad(&pNext); }

#ifdef _DEBUG 
    UINT16 debug_hash;
    UINT16 debug_index;
#endif // _DEBUG

    BOOL Equals(size_t token, void *pMT)
    { LIMITED_METHOD_CONTRACT; return (this->pMT == pMT && this->token == token); }

    BOOL Equals(ResolveCacheElem *pElem)
    { WRAPPER_NO_CONTRACT; return Equals(pElem->token, pElem->pMT); }

};

enum
{
    e_resolveCacheElem_sizeof_mt                 = sizeof(void *),
    e_resolveCacheElem_sizeof_token              = sizeof(size_t),
    e_resolveCacheElem_sizeof_target             = sizeof(void *),
    e_resolveCacheElem_sizeof_next               = sizeof(ResolveCacheElem *),

    e_resolveCacheElem_offset_mt                 = 0,
    e_resolveCacheElem_offset_token              = e_resolveCacheElem_offset_mt + e_resolveCacheElem_sizeof_mt,
    e_resolveCacheElem_offset_target             = e_resolveCacheElem_offset_token + e_resolveCacheElem_sizeof_token,
    e_resolveCacheElem_offset_next               = e_resolveCacheElem_offset_target + e_resolveCacheElem_sizeof_target,
};

/////////////////////////////////////////////////////////////////////////////////////
// A utility class to help manipulate a call site
struct StubCallSite
{
    friend class VirtualCallStubManager;

private:

    // On x86 are four possible kinds of callsites when you take into account all features
    //  Relative:                  direct call, e.g. "call addr". Not used currently.
    //  RelativeIndirect (JmpRel): indirect call through a relative address, e.g. "call [addr]"
    //  RegisterIndirect:          indirect call through a register, e.g. "call [eax]"
    //  DelegateCallSite:          anything else, tail called through a register by shuffle thunk, e.g. "jmp [eax]"
    //
    // On all other platforms we always use an indirect call through an indirection cell
    // In these cases all calls are made by the platform equivalent of "call [addr]".
    //
    // DelegateCallSite are particular in that they can come in a variety of forms:
    // a direct delegate call has a sequence defined by the jit but a multicast or secure delegate
    // are defined in a stub and have a different shape
    //
    PTR_PCODE       m_siteAddr;     // Stores the address of an indirection cell
    PCODE           m_returnAddr;

public:

#if defined(_TARGET_X86_) 
    StubCallSite(TADDR siteAddrForRegisterIndirect, PCODE returnAddr);

    PCODE           GetCallerAddress();
#else // !defined(_TARGET_X86_)
    // On platforms where we always use an indirection cell things
    // are much simpler - the siteAddr always stores a pointer to a
    // value that in turn points to the indirection cell.

    StubCallSite(TADDR siteAddr, PCODE returnAddr)
       { LIMITED_METHOD_CONTRACT; m_siteAddr = dac_cast<PTR_PCODE>(siteAddr); m_returnAddr = returnAddr; }

    PCODE           GetCallerAddress()     { LIMITED_METHOD_CONTRACT; return m_returnAddr; }
#endif // !defined(_TARGET_X86_)

    PCODE           GetSiteTarget()        { WRAPPER_NO_CONTRACT; return *(GetIndirectCell()); }
    void            SetSiteTarget(PCODE newTarget);
    PTR_PCODE       GetIndirectCell()      { LIMITED_METHOD_CONTRACT; return dac_cast<PTR_PCODE>(m_siteAddr); }
    PTR_PCODE *     GetIndirectCellAddress() { LIMITED_METHOD_CONTRACT; return &m_siteAddr; }

    PCODE           GetReturnAddress() { LIMITED_METHOD_CONTRACT; return m_returnAddr; }
};

#ifdef FEATURE_PREJIT
extern "C" void StubDispatchFixupStub();              // for lazy fixup of ngen call sites
#endif

// These are the assembly language entry points that the stubs use when they want to go into the EE

extern "C" void ResolveWorkerAsmStub();               // resolve a token and transfer control to that method
extern "C" void ResolveWorkerChainLookupAsmStub();    // for chaining of entries in the cache

#ifdef _TARGET_X86_ 
extern "C" void BackPatchWorkerAsmStub();             // backpatch a call site to point to a different stub
#ifdef FEATURE_PAL
extern "C" void BackPatchWorkerStaticStub(PCODE returnAddr, TADDR siteAddrForRegisterIndirect);
#endif // FEATURE_PAL
#endif // _TARGET_X86_


typedef VPTR(class VirtualCallStubManager) PTR_VirtualCallStubManager;

// VirtualCallStubManager is the heart of the stub dispatch logic. See the book of the runtime entry
// 
// file:../../doc/BookOfTheRuntime/ClassLoader/VirtualStubDispatchDesign.doc
// 
// The basic idea is that a call to an interface (it could also be used for virtual calls in general, but we
// do not do this), is simply the code
// 
//     call [DispatchCell]
// 
// Where we make sure 'DispatchCell' points at stubs that will do the right thing. DispatchCell is writable
// so we can udpate the code over time. There are three basic types of stubs that the dispatch cell can point
// to.
//     * Lookup: The intial stub that has no 'fast path' and simply pushes a ID for interface being called
//         and calls into the runtime at code:VirtualCallStubManager.ResolveWorkerStatic.
//     * Dispatch: Lookup stubs are patched to this stub which has a fast path that checks for a particular
//         Method Table and if that fails jumps to code that
//         * Decrements a 'missCount' (starts out as code:STUB_MISS_COUNT_VALUE). If this count goes to zero
//             code:VirtualCallStubManager.BackPatchWorkerStatic is called, morphs it into a resolve stub
//             (however since this decrementing logic is SHARED among all dispatch stubs, it may take
//             multiples of code:STUB_MISS_COUNT_VALUE if mulitple call sites are actively polymorphic (this
//             seems unlikley).
//         * Calls a resolve stub (Whenever a dispatch stub is created, it always has a cooresponding resolve
//             stub (but the resolve stubs are shared among many dispatch stubs).
//     * Resolve: see code:ResolveStub. This looks up the Method table in a process wide cache (see
//         code:ResolveCacheElem, and if found, jumps to it. This code path is about 17 instructions long (so
//         pretty fast, but certainly much slower than a normal call). If the method table is not found in
//         the cache, it calls into the runtime code:VirtualCallStubManager.ResolveWorkerStatic, which
//         populates it.
// So the general progression is call site's cells
//     * start out life pointing to a lookup stub
//     * On first call they get updated into a dispatch stub. When this misses, it calls a resolve stub,
//         which populates a resovle stub's cache, but does not update the call site' cell (thus it is still
//         pointing at the dispatch cell.
//     * After code:STUB_MISS_COUNT_VALUE misses, we update the call site's cell to point directly at the
//         resolve stub (thus avoiding the overhead of the quick check that always seems to be failing and
//         the miss count update).
//         
// QUESTION: What is the lifetimes of the various stubs and hash table entries?
// 
// QUESTION: There does not seem to be any logic that will change a call site's cell once it becomes a
// Resolve stub. Thus once a particular call site becomes a Resolve stub we live with the Resolve stub's
// (in)efficiency forever.
// 
// see code:#StubDispatchNotes for more
class VirtualCallStubManager : public StubManager
{
    friend class ClrDataAccess;
    friend class VirtualCallStubManagerManager;
    friend class VirtualCallStubManagerIterator;

    VPTR_VTABLE_CLASS(VirtualCallStubManager, StubManager)

public:
#ifdef _DEBUG
    virtual const char * DbgGetName() { LIMITED_METHOD_CONTRACT; return "VirtualCallStubManager"; }
#endif    

    // The reason for our existence, return a callstub for type id and slot number
    // where type id = 0 for the class contract (i.e. a virtual call), and type id > 0 for an
    // interface invoke where the id indicates which interface it is.
    //
    // The function is idempotent, i.e.
    // you'll get the same callstub twice if you call it with identical inputs.
    PCODE GetCallStub(TypeHandle ownerType, MethodDesc *pMD);
    PCODE GetCallStub(TypeHandle ownerType, DWORD slot);

    // Stubs for vtable-based virtual calls with no lookups
    PCODE GetVTableCallStub(DWORD slot);

    // Generate an fresh indirection cell.
    BYTE* GenerateStubIndirection(PCODE stub, BOOL fUseRecycledCell = FALSE);

    // Set up static data structures - called during EEStartup
    static void InitStatic();
    static void UninitStatic();

    // Per instance initialization - called during AppDomain::Init and ::Uninit and for collectible loader allocators
    void Init(BaseDomain* pDomain, LoaderAllocator *pLoaderAllocator);
    void Uninit();

    //@TODO: the logging should be tied into the VMs normal loggin mechanisms,
    //@TODO: for now we just always write a short log file called "StubLog_<pid>.log"
    static void StartupLogging();
    static void LoggingDump();
    static void FinishLogging();

    static void ResetCache();

    // Reclaim/rearrange any structures that can only be done during a gc sync point.
    // This is the mechanism we are using to avoid synchronization of alot of our
    // cache and hash table accesses.  We are requiring that during a gc sync point we are not
    // executing any stub code at all, hence at this time we are serialized on a single thread (gc)
    // and no other thread is accessing the data structures.
    static void ReclaimAll();
    void Reclaim();

#ifndef DACCESS_COMPILE 
    VirtualCallStubManager()
        : StubManager(),
          lookup_rangeList(),
          resolve_rangeList(),
          dispatch_rangeList(),
          cache_entry_rangeList(),
          vtable_rangeList(),
          parentDomain(NULL),
          m_loaderAllocator(NULL),
          m_initialReservedMemForHeaps(NULL),
          m_FreeIndCellList(NULL),
          m_RecycledIndCellList(NULL),
          indcell_heap(NULL),
          cache_entry_heap(NULL),
          lookup_heap(NULL),
          dispatch_heap(NULL),
          resolve_heap(NULL),
#ifdef _TARGET_AMD64_
          m_fShouldAllocateLongJumpDispatchStubs(FALSE),
#endif
          lookups(NULL),
          cache_entries(NULL),
          dispatchers(NULL),
          resolvers(NULL),
          m_counters(NULL),
          m_cur_counter_block(NULL),
          m_cur_counter_block_for_reclaim(NULL),
          m_cur_counter_block_for_reclaim_index(NULL),
          m_pNext(NULL)
    {
        LIMITED_METHOD_CONTRACT;
        ZeroMemory(&stats, sizeof(stats));
    }

    ~VirtualCallStubManager();
#endif // !DACCESS_COMPILE


    enum StubKind { 
        SK_UNKNOWN, 
        SK_LOOKUP,      // Lookup Stubs are SLOW stubs that simply call into the runtime to do all work.
        SK_DISPATCH,    // Dispatch Stubs have a fast check for one type otherwise jumps to runtime.  Works for monomorphic sites
        SK_RESOLVE,     // Resolve Stubs do a hash lookup before fallling back to the runtime.  Works for polymorphic sites.
        SK_VTABLECALL,  // Stub that jumps to a target method using vtable-based indirections. Works for non-interface calls.
        SK_BREAKPOINT 
    };

    // peek at the assembly code and predict which kind of a stub we have
    StubKind predictStubKind(PCODE stubStartAddress);

    /* know thine own stubs.  It is possible that when multiple
    virtualcallstub managers are built that these may need to become
    non-static, and the callers modified accordingly */
    StubKind getStubKind(PCODE stubStartAddress, BOOL usePredictStubKind = TRUE)
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;

        // This method can called with stubStartAddress==NULL, e.g. when handling null reference exceptions
        // caused by IP=0. Early out for this case to avoid confusing handled access violations inside predictStubKind.
        if (PCODEToPINSTR(stubStartAddress) == NULL)
            return SK_UNKNOWN;

        // Rather than calling IsInRange(stubStartAddress) for each possible stub kind
        // we can peek at the assembly code and predict which kind of a stub we have
        StubKind predictedKind = (usePredictStubKind) ? predictStubKind(stubStartAddress) : SK_UNKNOWN;

        if (predictedKind == SK_DISPATCH)
        {
            if (isDispatchingStub(stubStartAddress))
                return SK_DISPATCH;
        }
        else if (predictedKind == SK_LOOKUP)
        {
            if (isLookupStub(stubStartAddress))
                return SK_LOOKUP;
        }
        else if (predictedKind == SK_RESOLVE)
        {
            if (isResolvingStub(stubStartAddress))
                return SK_RESOLVE;
        }
        else if (predictedKind == SK_VTABLECALL)
        {
            if (isVTableCallStub(stubStartAddress))
                return SK_VTABLECALL;
        }

        // This is the slow case. If the predict returned SK_UNKNOWN, SK_BREAKPOINT,
        // or the predict was found to be incorrect when checked against the RangeLists
        // (isXXXStub), then we'll check each stub heap in sequence.
        if (isDispatchingStub(stubStartAddress))
            return SK_DISPATCH;
        else if (isLookupStub(stubStartAddress))
            return SK_LOOKUP;
        else if (isResolvingStub(stubStartAddress))
            return SK_RESOLVE;
        else if (isVTableCallStub(stubStartAddress))
            return SK_VTABLECALL;

        return SK_UNKNOWN;
    }

    inline BOOL isStub(PCODE stubStartAddress)
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;

        return (getStubKind(stubStartAddress) != SK_UNKNOWN);
    }

    BOOL isDispatchingStub(PCODE stubStartAddress)
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;

        return GetDispatchRangeList()->IsInRange(stubStartAddress);
    }

    BOOL isResolvingStub(PCODE stubStartAddress)
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;

        return GetResolveRangeList()->IsInRange(stubStartAddress);
    }

    BOOL isLookupStub(PCODE stubStartAddress)
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;

        return GetLookupRangeList()->IsInRange(stubStartAddress);
    }

    BOOL isVTableCallStub(PCODE stubStartAddress)
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;

        return GetVTableCallRangeList()->IsInRange(stubStartAddress);
    }

    static BOOL isDispatchingStubStatic(PCODE addr)
    {
        WRAPPER_NO_CONTRACT;
        StubKind stubKind;
        FindStubManager(addr, &stubKind);
        return stubKind == SK_DISPATCH;
    }

    static BOOL isResolvingStubStatic(PCODE addr)
    {
        WRAPPER_NO_CONTRACT;
        StubKind stubKind;
        FindStubManager(addr, &stubKind);
        return stubKind == SK_RESOLVE;
    }

    static BOOL isLookupStubStatic(PCODE addr)
    {
        WRAPPER_NO_CONTRACT;
        StubKind stubKind;
        FindStubManager(addr, &stubKind);
        return stubKind == SK_LOOKUP;
    }

    static BOOL isVtableCallStubStatic(PCODE addr)
    {
        WRAPPER_NO_CONTRACT;
        StubKind stubKind;
        FindStubManager(addr, &stubKind);
        return stubKind == SK_VTABLECALL;
    }

    //use range lists to track the chunks of memory that are part of each heap
    LockedRangeList lookup_rangeList;
    LockedRangeList resolve_rangeList;
    LockedRangeList dispatch_rangeList;
    LockedRangeList cache_entry_rangeList;
    LockedRangeList vtable_rangeList;

    // Get dac-ized pointers to rangelist.
    RangeList* GetLookupRangeList() 
    {
        SUPPORTS_DAC;

        TADDR addr = PTR_HOST_MEMBER_TADDR(VirtualCallStubManager, this, lookup_rangeList);
        return PTR_RangeList(addr);
    }
    RangeList* GetResolveRangeList() 
    {
        SUPPORTS_DAC;

        TADDR addr = PTR_HOST_MEMBER_TADDR(VirtualCallStubManager, this, resolve_rangeList);
        return PTR_RangeList(addr);
    }
    RangeList* GetDispatchRangeList() 
    {
        SUPPORTS_DAC;

        TADDR addr = PTR_HOST_MEMBER_TADDR(VirtualCallStubManager, this, dispatch_rangeList);
        return PTR_RangeList(addr);
    }
    RangeList* GetCacheEntryRangeList() 
    {
        SUPPORTS_DAC;
        TADDR addr = PTR_HOST_MEMBER_TADDR(VirtualCallStubManager, this, cache_entry_rangeList);
        return PTR_RangeList(addr);
    }
    RangeList* GetVTableCallRangeList()
    {
        SUPPORTS_DAC;
        TADDR addr = PTR_HOST_MEMBER_TADDR(VirtualCallStubManager, this, vtable_rangeList);
        return PTR_RangeList(addr);
    }

private:

    //allocate and initialize a stub of the desired kind
    DispatchHolder *GenerateDispatchStub(PCODE addrOfCode,
                                         PCODE addrOfFail,
                                         void *pMTExpected,
                                         size_t dispatchToken);

#ifdef _TARGET_AMD64_
    // Used to allocate a long jump dispatch stub. See comment around
    // m_fShouldAllocateLongJumpDispatchStubs for explaination.
    DispatchHolder *GenerateDispatchStubLong(PCODE addrOfCode,
                                             PCODE addrOfFail,
                                             void *pMTExpected,
                                             size_t dispatchToken);
#endif

    ResolveHolder *GenerateResolveStub(PCODE addrOfResolver,
                                       PCODE addrOfPatcher,
                                       size_t dispatchToken);

    LookupHolder *GenerateLookupStub(PCODE addrOfResolver,
                                     size_t dispatchToken);

    VTableCallHolder* GenerateVTableCallStub(DWORD slot);

    template <typename STUB_HOLDER>
    void AddToCollectibleVSDRangeList(STUB_HOLDER *holder)
    {
        if (m_loaderAllocator->IsCollectible())
        {
            parentDomain->GetCollectibleVSDRanges()->AddRange(reinterpret_cast<BYTE *>(holder->stub()),
                reinterpret_cast<BYTE *>(holder->stub()) + holder->stub()->size(),
                this);
        }
    }

    // The resolve cache is static across all AppDomains
    ResolveCacheElem *GenerateResolveCacheElem(void *addrOfCode,
                                               void *pMTExpected,
                                               size_t token);

    ResolveCacheElem *GetResolveCacheElem(void *pMT,
                                          size_t token,
                                          void *target);

    //Given a dispatch token, an object and a method table, determine the
    //target address to go to.  The return value (BOOL) states whether this address
    //is cacheable or not.
    static BOOL Resolver(MethodTable   * pMT,
                         DispatchToken   token,
                         OBJECTREF     * protectedObj,
                         PCODE         * ppTarget,
                         BOOL          throwOnConflict);

    // This can be used to find a target without needing the ability to throw
    static BOOL TraceResolver(Object *pObj, DispatchToken token, TraceDestination *trace);

public:
    // Return the MethodDesc corresponding to this token.
    static MethodDesc *GetRepresentativeMethodDescFromToken(DispatchToken token, MethodTable *pMT);
    static MethodDesc *GetInterfaceMethodDescFromToken(DispatchToken token);
    static MethodTable *GetTypeFromToken(DispatchToken token);

    //This is used to get the token out of a stub
    static size_t GetTokenFromStub(PCODE stub);

    //This is used to get the token out of a stub and we know the stub manager and stub kind
    static size_t GetTokenFromStubQuick(VirtualCallStubManager * pMgr, PCODE stub, StubKind kind);

    // General utility functions
    // Quick lookup in the cache. NOTHROW, GC_NOTRIGGER
    static PCODE CacheLookup(size_t token, UINT16 tokenHash, MethodTable *pMT);

    // Full exhaustive lookup. THROWS, GC_TRIGGERS
    static PCODE GetTarget(DispatchToken token, MethodTable *pMT, BOOL throwOnConflict);

private:
    // Given a dispatch token, return true if the token represents an interface, false if just a slot.
    static BOOL IsInterfaceToken(DispatchToken token);

    // Given a dispatch token, return true if the token represents a slot on the target.
    static BOOL IsClassToken(DispatchToken token);

#ifdef CHAIN_LOOKUP 
    static ResolveCacheElem* __fastcall PromoteChainEntry(ResolveCacheElem *pElem);
#endif

    // Flags used by the non-x86 versions of VSD_ResolveWorker

#define SDF_ResolveBackPatch        (0x01)
#define SDF_ResolvePromoteChain     (0x02)
#define SDF_ResolveFlags            (0x03)

    // These method needs to call the instance methods.
    friend PCODE VSD_ResolveWorker(TransitionBlock * pTransitionBlock,
                                   TADDR siteAddrForRegisterIndirect,
                                   size_t token
#ifndef _TARGET_X86_
                                   , UINT_PTR flags
#endif                            
                                   );

#if defined(_TARGET_X86_) && defined(FEATURE_PAL)
    friend void BackPatchWorkerStaticStub(PCODE returnAddr, TADDR siteAddrForRegisterIndirect);
#endif

    //These are the entrypoints that the stubs actually end up calling via the
    // xxxAsmStub methods above
    static void STDCALL BackPatchWorkerStatic(PCODE returnAddr, TADDR siteAddrForRegisterIndirect);

public:
    PCODE ResolveWorker(StubCallSite* pCallSite, OBJECTREF *protectedObj, DispatchToken token, StubKind stubKind);
    void BackPatchWorker(StubCallSite* pCallSite);

    //Change the callsite to point to stub
    void BackPatchSite(StubCallSite* pCallSite, PCODE stub);

public:
    /* the following two public functions are to support tracing or stepping thru
    stubs via the debugger. */
    virtual BOOL CheckIsStub_Internal(PCODE stubStartAddress);
    virtual BOOL TraceManager(Thread *thread,
                              TraceDestination *trace,
                              T_CONTEXT *pContext,
                              BYTE **pRetAddr);
    size_t GetSize()
    {
        LIMITED_METHOD_CONTRACT;
        size_t retval=0;
        if(indcell_heap)
            retval+=indcell_heap->GetSize();
        if(cache_entry_heap)
            retval+=cache_entry_heap->GetSize();
        if(lookup_heap)
            retval+=lookup_heap->GetSize();
         if(dispatch_heap)
            retval+=dispatch_heap->GetSize();
         if(resolve_heap)
            retval+=resolve_heap->GetSize();
         return retval;
    };

private:
    /* the following two private functions are to support tracing or stepping thru
    stubs via the debugger. */
    virtual BOOL DoTraceStub(PCODE stubStartAddress,
                             TraceDestination *trace);

private:
    // The parent domain of this manager
    PTR_BaseDomain  parentDomain;

    PTR_LoaderAllocator m_loaderAllocator;

    BYTE *          m_initialReservedMemForHeaps;

    static const UINT32 INDCELLS_PER_BLOCK = 32;    // 32 indirection cells per block.

    CrstExplicitInit m_indCellLock;

    // List of free indirection cells. The cells were directly allocated from the loader heap
    // (code:VirtualCallStubManager::GenerateStubIndirection) 
    BYTE * m_FreeIndCellList;

    // List of recycled indirection cells. The cells were recycled from finalized dynamic methods
    // (code:LCGMethodResolver::RecycleIndCells).
    BYTE * m_RecycledIndCellList;

#ifndef DACCESS_COMPILE 
    // This methods returns the a free cell from m_FreeIndCellList. It returns NULL if the list is empty.
    BYTE * GetOneFreeIndCell()
    {
        WRAPPER_NO_CONTRACT;

        return GetOneIndCell(&m_FreeIndCellList);
    }

    // This methods returns the a recycled cell from m_RecycledIndCellList. It returns NULL if the list is empty.
    BYTE * GetOneRecycledIndCell()
    {
        WRAPPER_NO_CONTRACT;

        return GetOneIndCell(&m_RecycledIndCellList);
    }

    // This methods returns the a cell from ppList. It returns NULL if the list is empty.
    BYTE * GetOneIndCell(BYTE ** ppList)
    {
        CONTRACT (BYTE*) {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(CheckPointer(ppList));
            PRECONDITION(m_indCellLock.OwnedByCurrentThread());        
        } CONTRACT_END;

        BYTE * temp = *ppList;

        if (temp)
        {
            BYTE * pNext = *((BYTE **)temp);
            *ppList = pNext;
            RETURN temp;
        }

        RETURN NULL;
    }

    // insert a linked list of indirection cells at the beginning of m_FreeIndCellList
    void InsertIntoFreeIndCellList(BYTE * head, BYTE * tail)
    {
        WRAPPER_NO_CONTRACT;

        InsertIntoIndCellList(&m_FreeIndCellList, head, tail);
    }

    // insert a linked list of indirection cells at the beginning of ppList
    void InsertIntoIndCellList(BYTE ** ppList, BYTE * head, BYTE * tail)
    {
        CONTRACTL {
            NOTHROW;
            GC_NOTRIGGER;
            PRECONDITION(CheckPointer(ppList));
            PRECONDITION(CheckPointer(head));
            PRECONDITION(CheckPointer(tail));
            PRECONDITION(m_indCellLock.OwnedByCurrentThread());        
        } CONTRACTL_END;

        BYTE * temphead = *ppList;
        *((BYTE**)tail) = temphead;
        *ppList = head;
    }
#endif // !DACCESS_COMPILE

    PTR_LoaderHeap  indcell_heap;       // indirection cells go here
    PTR_LoaderHeap  cache_entry_heap;   // resolve cache elem entries go here
    PTR_LoaderHeap  lookup_heap;        // lookup stubs go here
    PTR_LoaderHeap  dispatch_heap;      // dispatch stubs go here
    PTR_LoaderHeap  resolve_heap;       // resolve stubs go here
    PTR_LoaderHeap  vtable_heap;        // vtable-based jump stubs go here

#ifdef _TARGET_AMD64_
    // When we layout the stub heaps, we put them close together in a sequential order
    // so that we maximize performance with respect to branch predictions. On AMD64,
    // dispatch stubs use a rel32 jump on failure to the resolve stub. This works for
    // a while because of the ordering, but as soon as we have to start allocating more
    // memory for either the dispatch or resolve heaps we have a chance that we'll be
    // further away than a rel32 jump can reach, because we're in a 64-bit address
    // space. As such, this flag will indicate when we allocate the first dispatch stub
    // that cannot reach a resolve stub, and when this happens we'll switch over to
    // allocating the larger version of the dispatch stub which contains an abs64 jump.
    //@TODO: This is a bit of a workaround, but the limitations of LoaderHeap require that we
    //@TODO: take this approach. Hopefully in Orcas we'll have a chance to rewrite LoaderHeap.
    BOOL            m_fShouldAllocateLongJumpDispatchStubs; // Defaults to FALSE.
#endif

    BucketTable *   lookups;            // hash table of lookups keyed by tokens
    BucketTable *   cache_entries;      // hash table of dispatch token/target structs for dispatch cache
    BucketTable *   dispatchers;        // hash table of dispatching stubs keyed by tokens/actualtype
    BucketTable *   resolvers;          // hash table of resolvers keyed by tokens/resolverstub
    BucketTable *   vtableCallers;      // hash table of vtable call stubs keyed by slot values

    // This structure is used to keep track of the fail counters.
    // We only need one fail counter per ResolveStub,
    //  and most programs use less than 250 ResolveStubs
    // We allocate these on the main heap using "new counter block"
    struct counter_block
    {
        static const UINT32 MAX_COUNTER_ENTRIES = 256-2;  // 254 counters should be enough for most cases.

        counter_block *  next;                            // the next block
        UINT32           used;                            // the index of the next free entry
        INT32            block[MAX_COUNTER_ENTRIES];      // the counters
    };

    counter_block *m_counters;                            // linked list of counter blocks of failure counters
    counter_block *m_cur_counter_block;                   // current block for updating counts
    counter_block *m_cur_counter_block_for_reclaim;       // current block for updating
    UINT32         m_cur_counter_block_for_reclaim_index; // index into the current block for updating

    // Used to keep track of all the VCSManager objects in the system.
    PTR_VirtualCallStubManager m_pNext;            // Linked list pointer

public:
    // Given a stub address, find the VCSManager that owns it.
    static VirtualCallStubManager *FindStubManager(PCODE addr,
                                                   StubKind* wbStubKind = NULL,
                                                   BOOL usePredictStubKind = TRUE);

#ifndef DACCESS_COMPILE
    // insert a linked list of indirection cells at the beginning of m_RecycledIndCellList
    void InsertIntoRecycledIndCellList_Locked(BYTE * head, BYTE * tail)
    {
        CONTRACTL {
            NOTHROW;
            GC_TRIGGERS;
            MODE_ANY;
        } CONTRACTL_END;

        CrstHolder lh(&m_indCellLock);

        InsertIntoIndCellList(&m_RecycledIndCellList, head, tail);
    }
#endif // !DACCESS_COMPILE

    // These are the counters for keeping statistics
    struct
    {
        UINT32 site_counter;            //# of call sites
        UINT32 stub_lookup_counter;     //# of lookup stubs
        UINT32 stub_poly_counter;       //# of resolve stubs
        UINT32 stub_mono_counter;       //# of dispatch stubs
        UINT32 stub_vtable_counter;     //# of vtable call stubs
        UINT32 site_write;              //# of call site backpatch writes
        UINT32 site_write_poly;         //# of call site backpatch writes to point to resolve stubs
        UINT32 site_write_mono;         //# of call site backpatch writes to point to dispatch stubs
        UINT32 worker_call;             //# of calls into ResolveWorker
        UINT32 worker_call_no_patch;    //# of times call_worker resulted in no patch
        UINT32 worker_collide_to_mono;  //# of times we converted a poly stub to a mono stub instead of writing the cache entry
        UINT32 stub_space;              //# of bytes of stubs
        UINT32 cache_entry_counter;     //# of cache structs
        UINT32 cache_entry_space;       //# of bytes used by cache lookup structs
    } stats;

    void LogStats();

#ifdef DACCESS_COMPILE 
protected:
    virtual void DoEnumMemoryRegions(CLRDataEnumMemoryFlags flags);
    virtual LPCWSTR GetStubManagerName(PCODE addr)
    {
        WRAPPER_NO_CONTRACT;
        CONSISTENCY_CHECK(isStub(addr));

        if (isLookupStub(addr))
        {
            return W("VSD_LookupStub");
        }
        else if (isDispatchingStub(addr))
        {
            return W("VSD_DispatchStub");
        }
        else
        {
            CONSISTENCY_CHECK(isResolvingStub(addr));
            return W("VSD_ResolveStub");
        }
    }
#endif
};

/********************************************************************************************************
********************************************************************************************************/
typedef VPTR(class VirtualCallStubManagerManager) PTR_VirtualCallStubManagerManager;

class VirtualCallStubManagerIterator;
class VirtualCallStubManagerManager : public StubManager
{
    VPTR_VTABLE_CLASS(VirtualCallStubManagerManager, StubManager)

    friend class StubManager;
    friend class VirtualCallStubManager;
    friend class VirtualCallStubManagerIterator;
    friend class StubManagerIterator;

  public:
    virtual BOOL TraceManager(Thread *thread, TraceDestination *trace,
                              T_CONTEXT *pContext, BYTE **pRetAddr);

    virtual BOOL CheckIsStub_Internal(PCODE stubStartAddress);

    virtual BOOL DoTraceStub(PCODE stubStartAddress, TraceDestination *trace);

    static MethodDesc *Entry2MethodDesc(PCODE stubStartAddress, MethodTable *pMT);

#ifdef DACCESS_COMPILE
    virtual void DoEnumMemoryRegions(CLRDataEnumMemoryFlags flags);
    virtual LPCWSTR GetStubManagerName(PCODE addr)
        { WRAPPER_NO_CONTRACT; return FindVirtualCallStubManager(addr)->GetStubManagerName(addr); }
#endif

  private:
    // Used to keep track of all the VCSManager objects in the system.
    PTR_VirtualCallStubManager m_pManagers;  // Head of the linked list

#ifndef DACCESS_COMPILE
    // Ctor. This is only used by StaticInit.
    VirtualCallStubManagerManager();
#endif
    
    // A cache element to quickly check the last matched manager.
    Volatile<VirtualCallStubManager*> m_pCacheElem;

    // RW lock for reading entries and removing them.
    SimpleRWLock m_RWLock;

    // This will look through all the managers in an intelligent fashion to
    // find the manager that owns the address.
    VirtualCallStubManager *FindVirtualCallStubManager(PCODE stubAddress);

  protected:
    // Add a VCSManager to the linked list.
    void AddStubManager(VirtualCallStubManager *pMgr);

    // Remove a VCSManager from the linked list.
    void RemoveStubManager(VirtualCallStubManager *pMgr);

    VirtualCallStubManager *FirstManager()
        { WRAPPER_NO_CONTRACT; return m_pManagers; }

#ifndef DACCESS_COMPILE
    static void InitStatic();
#endif

  public:
    SPTR_DECL(VirtualCallStubManagerManager, g_pManager);

    static VirtualCallStubManagerManager *GlobalManager()
        { LIMITED_METHOD_DAC_CONTRACT; CONSISTENCY_CHECK(CheckPointer(g_pManager)); return g_pManager; }

    VirtualCallStubManagerIterator IterateVirtualCallStubManagers();

#ifdef _DEBUG
    // Debug helper to help identify stub-managers.
    virtual const char * DbgGetName() { LIMITED_METHOD_CONTRACT; return "VirtualCallStubManagerManager"; }
#endif
};

/********************************************************************************************************
********************************************************************************************************/
class VirtualCallStubManagerIterator
{
    friend class VirtualCallStubManagerManager;

  public:
    BOOL Next();
    VirtualCallStubManager *Current();

    // Copy ctor
    inline VirtualCallStubManagerIterator(const VirtualCallStubManagerIterator &it);

  protected:
    inline VirtualCallStubManagerIterator(VirtualCallStubManagerManager *pMgr);

    BOOL                    m_fIsStart;
    VirtualCallStubManager *m_pCurMgr;
};

/////////////////////////////////////////////////////////////////////////////////////////////
// Ctor
inline VirtualCallStubManagerIterator::VirtualCallStubManagerIterator(VirtualCallStubManagerManager *pMgr)
    : m_fIsStart(TRUE), m_pCurMgr(pMgr->m_pManagers)
{
    LIMITED_METHOD_DAC_CONTRACT;
    CONSISTENCY_CHECK(CheckPointer(pMgr));
}

/////////////////////////////////////////////////////////////////////////////////////////////
// Copy ctor
inline VirtualCallStubManagerIterator::VirtualCallStubManagerIterator(const VirtualCallStubManagerIterator &it)
    : m_fIsStart(it.m_fIsStart), m_pCurMgr(it.m_pCurMgr)
{
    LIMITED_METHOD_DAC_CONTRACT;
}

/********************************************************************************************************
#StubDispatchNotes

A note on approach.  The cache and hash tables used by the stub and lookup mechanism
are designed with an eye to minimizing interlocking and/or syncing and/or locking operations.
They are intended to run in a highly concurrent environment.  Since there is no magic,
some tradeoffs and and some implementation constraints are required.  The basic notion
is that if all reads and writes are atomic and if all functions and operations operate
correctly in the face of commutative reorderings of the visibility of all reads and writes
across threads, then we don't have to interlock, sync, or serialize.  Our approximation of
this is:

1. All reads and all writes to tables must be atomic.  This effectively limits the actual entry
size in a table to be a pointer or a pointer sized thing.

2. All functions, like comparisons for equality or computation of hash values must function
correctly in the face of concurrent updating of the underlying table.  This is accomplished
by making the underlying structures/entries effectively immutable, if concurrency is in anyway possible.
By effectively immutatable, we mean that the stub or token structure is either immutable or that
if it is ever written, all possibley concurrent writes are attempting to write the same value (atomically)
or that the competing (atomic) values do not affect correctness, and that the function operates correctly whether
or not any of the writes have taken place (is visible yet).  The constraint we maintain is that all competeing
updates (and their visibility or lack thereof) do not alter the correctness of the program.

3. All tables are inexact.  The counts they hold (e.g. number of contained entries) may be inaccurrate,
but that inaccurracy cannot affect their correctness.  Table modifications, such as insertion of
an new entry may not succeed, but such failures cannot affect correctness.  This implies that just
because a stub/entry is not present in a table, e.g. has been removed, that does not mean that
it is not in use.  It also implies that internal table structures, such as discarded hash table buckets,
cannot be freely recycled since another concurrent thread may still be walking thru it.

4. Occassionaly it is necessary to pick up the pieces that have been dropped on the floor
so to speak, e.g. actually recycle hash buckets that aren't in use.  Since we have a natural
sync point already in the GC, we use that to provide cleanup points.  We need to make sure that code that
is walking our structures is not a GC safe point.  Hence if the GC calls back into us inside the GC
sync point, we know that nobody is inside our stuctures and we can safely rearrange and recycle things.
********************************************************************************************************/

//initial and increment value for fail stub counters
#ifdef STUB_LOGGING 
extern UINT32 STUB_MISS_COUNT_VALUE;
extern UINT32 STUB_COLLIDE_WRITE_PCT;
extern UINT32 STUB_COLLIDE_MONO_PCT;
#else // !STUB_LOGGING
#define STUB_MISS_COUNT_VALUE   100
#define STUB_COLLIDE_WRITE_PCT  100
#define STUB_COLLIDE_MONO_PCT     0
#endif // !STUB_LOGGING

//size and mask of the cache used by resolve stubs
// CALL_STUB_CACHE_SIZE must be equal to 2^CALL_STUB_CACHE_NUM_BITS
#define CALL_STUB_CACHE_NUM_BITS 12 //10
#define CALL_STUB_CACHE_SIZE 4096 //1024
#define CALL_STUB_CACHE_MASK (CALL_STUB_CACHE_SIZE-1)
#define CALL_STUB_CACHE_PROBES 5
//min sizes for BucketTable and buckets and the growth and hashing constants
#define CALL_STUB_MIN_BUCKETS 32
#define CALL_STUB_MIN_ENTRIES 4
//this is so that the very first growth will jump from 4 to 32 entries, then double from there.
#define CALL_STUB_SECONDARY_ENTRIES 8
#define CALL_STUB_GROWTH_FACTOR 2
#define CALL_STUB_LOAD_FACTOR 90
#define CALL_STUB_HASH_CONST1 1327
#define CALL_STUB_HASH_CONST2 43627
#define LARGE_PRIME 7199369
//internal layout of buckets=size-1,count,entries....
#define CALL_STUB_MASK_INDEX 0
#define CALL_STUB_COUNT_INDEX 1
#define CALL_STUB_DEAD_LINK 2
#define CALL_STUB_FIRST_INDEX 3
//marker entries in cache and hash tables
#define CALL_STUB_EMPTY_ENTRY   0
// number of successes for a chained element before it gets moved to the front
#define CALL_STUB_CACHE_INITIAL_SUCCESS_COUNT (0x100)

/*******************************************************************************************************
Entry is an abstract class.  We will make specific subclasses for each kind of
entry.  Entries hold references to stubs or tokens.  The principle thing they provide
is a virtual Equals function that is used by the caching and hashing tables within which
the stubs and tokens are stored.  Entries are typically stack allocated by the routines
that call into the hash and caching functions, and the functions stuff stubs into the entry
to do the comparisons.  Essentially specific entry subclasses supply a vtable to a stub
as and when needed.  This means we don't have to have vtables attached to stubs.

Summarizing so far, there is a struct for each kind of stub or token of the form XXXXStub.
They provide that actual storage layouts.
There is a stuct in which each stub which has code is containted of the form XXXXHolder.
They provide alignment and anciliary storage for the stub code.
There is a subclass of Entry for each kind of stub or token, of the form XXXXEntry.
They provide the specific implementations of the virtual functions declared in Entry. */
class Entry
{
public:
    //access and compare the keys of the entry
    virtual BOOL Equals(size_t keyA, size_t keyB)=0;
    virtual size_t KeyA()=0;
    virtual size_t KeyB()=0;

    //contents is the struct or token that the entry exposes
    virtual void SetContents(size_t contents)=0;
};

/* define the platform specific Stubs and stub holders */

#include <virtualcallstubcpu.hpp>

#if USES_LOOKUP_STUBS 
/**********************************************************************************************
LookupEntry wraps LookupStubs and provide the concrete implementation of the abstract class Entry.
Virtual and interface call sites when they are first jitted point to LookupStubs.  The hash table
that contains look up stubs is keyed by token, hence the Equals function uses the embedded token in
the stub for comparison purposes.  Since we are willing to allow duplicates in the hash table (as
long as they are relatively rare) we do use direct comparison of the tokens rather than extracting
the fields from within the tokens, for perf reasons. */
class LookupEntry : public Entry
{
public:
    //Creates an entry that wraps lookup stub s
    LookupEntry(size_t s)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(VirtualCallStubManager::isLookupStubStatic((PCODE)s));
        stub = (LookupStub*) s;
    }

    //default contructor to allow stack and inline allocation of lookup entries
    LookupEntry() {LIMITED_METHOD_CONTRACT; stub = NULL;}

    //implementations of abstract class Entry
    BOOL Equals(size_t keyA, size_t keyB)
         { WRAPPER_NO_CONTRACT; return stub && (keyA == KeyA()) && (keyB == KeyB()); }

    size_t KeyA() { WRAPPER_NO_CONTRACT; return Token(); }
    size_t KeyB() { WRAPPER_NO_CONTRACT; return (size_t)0; }

    void SetContents(size_t contents)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(VirtualCallStubManager::isLookupStubStatic((PCODE)contents));
        stub = LookupHolder::FromLookupEntry((PCODE)contents)->stub();
    }

    //extract the token of the underlying lookup stub

    inline size_t Token()                 { LIMITED_METHOD_CONTRACT; return stub ? stub->token() : 0; }

private:
    LookupStub* stub;   //the stub the entry wrapping
};
#endif // USES_LOOKUP_STUBS

class VTableCallEntry : public Entry
{
public:
    //Creates an entry that wraps vtable call stub
    VTableCallEntry(size_t s)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(VirtualCallStubManager::isVtableCallStubStatic((PCODE)s));
        stub = (VTableCallStub*)s;
    }

    //default contructor to allow stack and inline allocation of vtable call entries
    VTableCallEntry() { LIMITED_METHOD_CONTRACT; stub = NULL; }

    //implementations of abstract class Entry
    BOOL Equals(size_t keyA, size_t keyB)
    {
        WRAPPER_NO_CONTRACT; return stub && (keyA == KeyA()) && (keyB == KeyB());
    }

    size_t KeyA() { WRAPPER_NO_CONTRACT; return Token(); }
    size_t KeyB() { WRAPPER_NO_CONTRACT; return (size_t)0; }

    void SetContents(size_t contents)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(VirtualCallStubManager::isVtableCallStubStatic((PCODE)contents));
        stub = VTableCallHolder::FromVTableCallEntry((PCODE)contents)->stub();
    }

    //extract the token of the underlying lookup stub

    inline size_t Token() { LIMITED_METHOD_CONTRACT; return stub ? stub->token() : 0; }

private:
    VTableCallStub* stub;   //the stub the entry wrapping
};

/**********************************************************************************************
ResolveCacheEntry wraps a ResolveCacheElem and provides lookup functionality for entries that
were created that may be added to the ResolveCache
*/
class ResolveCacheEntry : public Entry
{
public:
    ResolveCacheEntry(size_t elem)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(elem != 0);
        pElem = (ResolveCacheElem*) elem;
    }

    //default contructor to allow stack and inline allocation of lookup entries
    ResolveCacheEntry() { LIMITED_METHOD_CONTRACT; pElem = NULL; }

    //access and compare the keys of the entry
    virtual BOOL Equals(size_t keyA, size_t keyB)
        { WRAPPER_NO_CONTRACT; return pElem && (keyA == KeyA()) && (keyB == KeyB()); }
    virtual size_t KeyA()
        { LIMITED_METHOD_CONTRACT; return pElem != NULL ? pElem->token : 0; }
    virtual size_t KeyB()
        { LIMITED_METHOD_CONTRACT; return pElem != NULL ? (size_t) pElem->pMT : 0; }

    //contents is the struct or token that the entry exposes
    virtual void SetContents(size_t contents)
    {
        LIMITED_METHOD_CONTRACT;
        pElem = (ResolveCacheElem*) contents;
    }

    inline const BYTE *Target()
    {
        LIMITED_METHOD_CONTRACT;
        return pElem != NULL ? (const BYTE *)pElem->target : NULL;
    }

private:
    ResolveCacheElem *pElem;
};

/**********************************************************************************************
ResolveEntry wraps ResolveStubs and provide the concrete implementation of the abstract class Entry.
Polymorphic call sites and monomorphic calls that fail end up in a ResolveStub.  Resolve stubs
are stored in hash tables keyed by token, hence the Equals function uses the embedded token in
the stub for comparison purposes.  Since we are willing to allow duplicates in the hash table (as
long as they are relatively rare) we do use direct comparison of the tokens rather than extracting
the fields from within the tokens, for perf reasons. */
class ResolveEntry : public Entry
{
public:
    //Creates an entry that wraps resolve stub s
    ResolveEntry (size_t s)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(VirtualCallStubManager::isResolvingStubStatic((PCODE)s));
        stub = (ResolveStub*) s;
    }
    //default contructor to allow stack and inline allocation of resovler entries
    ResolveEntry()  { LIMITED_METHOD_CONTRACT;    stub = CALL_STUB_EMPTY_ENTRY; }

    //implementations of abstract class Entry
    inline BOOL Equals(size_t keyA, size_t keyB)
         { WRAPPER_NO_CONTRACT; return stub && (keyA == KeyA()) && (keyB == KeyB()); }
    inline size_t KeyA() { WRAPPER_NO_CONTRACT; return Token(); }
    inline size_t KeyB() { WRAPPER_NO_CONTRACT; return (size_t)0; }

    void SetContents(size_t contents)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(VirtualCallStubManager::isResolvingStubStatic((PCODE)contents));
        stub = ResolveHolder::FromResolveEntry((PCODE)contents)->stub();
    }
    //extract the token of the underlying resolve stub
    inline size_t Token()  { WRAPPER_NO_CONTRACT; return stub ? (size_t)(stub->token()) : 0; }
private:
    ResolveStub* stub;          //the stub the entry is wrapping
};

/**********************************************************************************************
DispatchEntry wraps DispatchStubs and provide the concrete implementation of the abstract class Entry.
Monomorphic and mostly monomorphic call sites eventually point to DispatchStubs.  Dispatch stubs
are placed in hash and cache tables keyed by the expected Method Table and token they are built for.
Since we are willing to allow duplicates in the hash table (as long as they are relatively rare)
we do use direct comparison of the tokens rather than extracting the fields from within the tokens,
for perf reasons.*/
class DispatchEntry : public Entry
{
public:
    //Creates an entry that wraps dispatch stub s
    DispatchEntry (size_t s)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(VirtualCallStubManager::isDispatchingStubStatic((PCODE)s));
        stub = (DispatchStub*) s;
    }
    //default contructor to allow stack and inline allocation of resovler entries
    DispatchEntry()                       { LIMITED_METHOD_CONTRACT;    stub = CALL_STUB_EMPTY_ENTRY; }

    //implementations of abstract class Entry
    inline BOOL Equals(size_t keyA, size_t keyB)
         { WRAPPER_NO_CONTRACT; return stub && (keyA == KeyA()) && (keyB == KeyB()); }
    inline size_t KeyA() { WRAPPER_NO_CONTRACT; return Token(); }
    inline size_t KeyB() { WRAPPER_NO_CONTRACT; return ExpectedMT();}

    void SetContents(size_t contents)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(VirtualCallStubManager::isDispatchingStubStatic((PCODE)contents));
        stub = DispatchHolder::FromDispatchEntry((PCODE)contents)->stub();
    }

    //extract the fields of the underlying dispatch stub
    inline size_t ExpectedMT()
          { WRAPPER_NO_CONTRACT; return stub ? (size_t)(stub->expectedMT()) : 0; }

    size_t Token()
    {
        WRAPPER_NO_CONTRACT;
        if (stub)
        {
            ResolveHolder * resolveHolder = ResolveHolder::FromFailEntry(stub->failTarget());
            size_t token = resolveHolder->stub()->token();
            _ASSERTE(token == VirtualCallStubManager::GetTokenFromStub((PCODE)stub));
            return token;
        }
        else
        {
            return 0;
        }
    }

    inline PCODE Target()
          { WRAPPER_NO_CONTRACT; return stub ? stub->implTarget()  : 0; }

private:
    DispatchStub* stub;
};

/*************************************************************************************************
DispatchCache is the cache table that the resolve stubs use for inline polymorphic resolution
of a call.  The cache entry is logically a triplet of (method table, token, impl address) where method table
is the type of the calling frame's <this>, token identifies the method being invoked,
i.e. is a (type id,slot #) pair, and impl address is the address of the method implementation.
*/
class DispatchCache
{
public:
    static const UINT16 INVALID_HASH = (UINT16)(-1);

    DispatchCache();

    //read and write the cache keyed by (method table,token) pair.
    inline ResolveCacheElem* Lookup(size_t token, void* mt)
        { WRAPPER_NO_CONTRACT; return Lookup(token, INVALID_HASH, mt);}

    ResolveCacheElem* Lookup(size_t token, UINT16 tokenHash, void* mt);

    enum InsertKind {IK_NONE, IK_DISPATCH, IK_RESOLVE, IK_SHARED, IK_EXTERNAL};

    BOOL Insert(ResolveCacheElem* elem, InsertKind insertKind);
#ifdef CHAIN_LOOKUP 
    void PromoteChainEntry(ResolveCacheElem* elem);
#endif

    // This is the heavyweight hashing algorithm. Use sparingly.
    static UINT16 HashToken(size_t token);

    inline void GetLoadFactor(size_t *total, size_t *used)
    {
        LIMITED_METHOD_CONTRACT;

        *total = CALL_STUB_CACHE_SIZE;
        size_t count = 0;
        for (size_t i = 0; i < CALL_STUB_CACHE_SIZE; i++)
            if (cache[i] != empty)
                count++;
        *used = count;
    }

    inline void *GetCacheBaseAddr()
        { LIMITED_METHOD_CONTRACT; return &cache[0]; }
    inline size_t GetCacheCount()
        { LIMITED_METHOD_CONTRACT; return CALL_STUB_CACHE_SIZE; }
    inline ResolveCacheElem *GetCacheEntry(size_t idx)
        { LIMITED_METHOD_CONTRACT; return VolatileLoad(&cache[idx]); }
    inline BOOL IsCacheEntryEmpty(size_t idx)
        { LIMITED_METHOD_CONTRACT; return cache[idx] == empty; }

    inline void SetCacheEntry(size_t idx, ResolveCacheElem *elem)
    {
        LIMITED_METHOD_CONTRACT;
#ifdef STUB_LOGGING 
          cacheData[idx].numWrites++;
#endif
#ifdef CHAIN_LOOKUP 
        CONSISTENCY_CHECK(m_writeLock.OwnedByCurrentThread());
#endif
          cache[idx] = elem;
        }

    inline void ClearCacheEntry(size_t idx)
    {
        LIMITED_METHOD_CONTRACT;
#ifdef STUB_LOGGING 
          cacheData[idx].numClears++;
#endif
          cache[idx] = empty;
        }

    struct
    {
        UINT32 insert_cache_external;     //# of times Insert was called for IK_EXTERNAL
        UINT32 insert_cache_shared;       //# of times Insert was called for IK_SHARED
        UINT32 insert_cache_dispatch;     //# of times Insert was called for IK_DISPATCH
        UINT32 insert_cache_resolve;      //# of times Insert was called for IK_RESOLVE
        UINT32 insert_cache_hit;          //# of times Insert found an empty cache entry
        UINT32 insert_cache_miss;         //# of times Insert already had a matching cache entry
        UINT32 insert_cache_collide;      //# of times Insert found a used cache entry
        UINT32 insert_cache_write;        //# of times Insert wrote a cache entry
    } stats;

    void LogStats();

    // Unlocked iterator of entries. Use only when read/write access to the cache
    // is safe. This would typically be at GC sync points, currently needed during
    // appdomain unloading.
    class Iterator
    {
      public:
        Iterator(DispatchCache *pCache);
        inline BOOL IsValid()
        { WRAPPER_NO_CONTRACT; return (m_curBucket < (INT32)m_pCache->GetCacheCount()); }
        void Next();
        // Unlink the current entry.
        // **NOTE** Using this method implicitly performs a call to Next to move
        //          past the unlinked entry. Thus, one could accidentally skip
        //          entries unless you take this into consideration.
        ResolveCacheElem *UnlinkEntry();
        inline ResolveCacheElem *Entry()
        { LIMITED_METHOD_CONTRACT; CONSISTENCY_CHECK(IsValid()); return *m_ppCurElem; }

      private:
        void NextValidBucket();
        inline void NextBucket()
        { LIMITED_METHOD_CONTRACT; m_curBucket++; m_ppCurElem = &m_pCache->cache[m_curBucket]; }

        DispatchCache     *m_pCache;
        INT32              m_curBucket;
        ResolveCacheElem **m_ppCurElem;
    };

private:
#ifdef CHAIN_LOOKUP 
    Crst m_writeLock;
#endif

    //the following hash computation is also inlined in the resolve stub in asm (SO NO TOUCHIE)
    inline static UINT16 HashMT(UINT16 tokenHash, void* mt)
    {
        LIMITED_METHOD_CONTRACT;

        UINT16 hash;

        size_t mtHash = (size_t) mt;
        mtHash = (((mtHash >> CALL_STUB_CACHE_NUM_BITS) + mtHash) >> LOG2_PTRSIZE) & CALL_STUB_CACHE_MASK;
        hash  = (UINT16) mtHash;

        hash ^= (tokenHash & CALL_STUB_CACHE_MASK);

        return hash;
    }

    ResolveCacheElem* cache[CALL_STUB_CACHE_SIZE]; //must be first
    ResolveCacheElem* empty;                    //empty entry, initialized to fail all comparisons
#ifdef STUB_LOGGING 
public:
    struct CacheEntryData {
        UINT32 numWrites;
        UINT16 numClears;
    };
    CacheEntryData cacheData[CALL_STUB_CACHE_SIZE];
#endif // STUB_LOGGING
};

/**************************************************************************************************
The hash tables are accessed via instances of the Prober.  Prober is a probe into a bucket
of the hash table, and therefore has an index which is the current probe position.
It includes a count of the number of probes done in that bucket so far and a stride
to step thru the bucket with.  To do comparisons, it has a reference to an entry with which
it can do comparisons (Equals(...)) of the entries (stubs) inside the hash table.  It also has
the key pair (keyA, keyB) that it is looking for.

Typically, an entry of the appropriate type is created on the stack and then the prober is created passing
in a reference to the entry.  The prober is used for a  complete operation, such as look for and find an
entry (stub), creating and inserting it as necessary.

The initial index and the stride are orthogonal hashes of the key pair, i.e. we are doing a varient of
double hashing.  When we initialize the prober (see FormHash below) we set the initial probe based on
one hash.  The stride (used as a modulo addition of the probe position) is based on a different hash and
is such that it will vist every location in the bucket before repeating.  Hence it is imperative that
the bucket size and the stride be relative prime wrt each other.  We have chosen to make bucket sizes
a power of 2, so we force stride to be odd.

Note -- it must be assumed that multiple probers are walking the same tables and buckets at the same time.
Additionally, the counts may not be accurate, and there may be duplicates in the tables.  Since the tables
do not allow concurrrent deletion, some of the concurrency issues are ameliorated.
*/
class Prober
{
    friend class FastTable;
    friend class BucketTable;
public:
    Prober(Entry* e) {LIMITED_METHOD_CONTRACT; comparer = e;}
    //find the requested entry, if not there return CALL_STUB_EMPTY_ENTRY
    size_t Find();
    //add the entry into the bucket, if it is not already in the bucket.
    //return the entry actually in the bucket (existing or added)
    size_t Add(size_t entry);
private:
    //return the bucket (FastTable*) that the prober is currently walking
    inline size_t* items() {LIMITED_METHOD_CONTRACT; return &base[-CALL_STUB_FIRST_INDEX];}
    //are there more probes possible, or have we probed everything in the bucket
    inline BOOL NoMore() {LIMITED_METHOD_CONTRACT; return probes>mask;} //both probes and mask are (-1)
    //advance the probe to a new place in the bucket
    inline BOOL Next()
    {
        WRAPPER_NO_CONTRACT;
        index = (index + stride) & mask;
        probes++;
        return !NoMore();
    }
    inline size_t Read()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(base);
        return VolatileLoad(&base[index]);
    }
    //initialize a prober across a bucket (table) for the specified keys.
    void InitProber(size_t key1, size_t key2, size_t* table);
    //set up the initial index and stride and probe count
    inline void FormHash()
    {
        LIMITED_METHOD_CONTRACT;

        probes = 0;
        //these two hash functions have not been formally measured for effectiveness
        //but they are at least orthogonal

        size_t a = ((keyA>>16) + keyA);
        size_t b = ((keyB>>16) ^ keyB);
        index    = (((a*CALL_STUB_HASH_CONST1)>>4)+((b*CALL_STUB_HASH_CONST2)>>4)+CALL_STUB_HASH_CONST1) & mask;
        stride   = ((a+(b*CALL_STUB_HASH_CONST1)+CALL_STUB_HASH_CONST2) | 1) & mask;
    }
    //atomically grab an empty slot so we can insert a new entry into the bucket
    BOOL GrabEntry(size_t entryValue);
    size_t keyA;        //key pair we are looking for
    size_t keyB;
    size_t* base;       //we have our own pointer to the bucket, so races don't matter.
                        //  We won't care if we do the lookup in an
                        //  outdated bucket (has grown out from under us).
                        //  All that will happen is possibly dropping an entry
                        //  on the floor or adding a duplicate.
    size_t index;       //current probe point in the bucket
    size_t stride;      //amount to step on each successive probe, must be relatively prime wrt the bucket size
    size_t mask;        //size of bucket - 1
    size_t probes;      //number probes - 1
    Entry* comparer;//used to compare an entry against the sought after key pair
};

/********************************************************************************************************
FastTable is used to implement the buckets of a BucketTable, a bucketized hash table.  A FastTable is
an array of entries (contents).  The first two slots of contents store the size-1 and count of entries
actually in the FastTable.  Note that the count may be inaccurate and there may be duplicates.  Careful
attention must be paid to eliminate the need for interlocked or serialized or locked operations in face
of concurrency.
*/
#ifdef _MSC_VER 
#pragma warning(push)
#pragma warning(disable : 4200)     // disable zero-sized array warning
#endif // _MSC_VER
class FastTable
{
    friend class BucketTable;
public:
private:
    FastTable() { LIMITED_METHOD_CONTRACT; }
    ~FastTable() { LIMITED_METHOD_CONTRACT; }

    //initialize a prober for the specified keys.
    inline BOOL SetUpProber(size_t keyA, size_t keyB, Prober* probe)
    {
        CONTRACTL {
            NOTHROW;
            GC_NOTRIGGER;
            FORBID_FAULT;
        } CONTRACTL_END;

        _ASSERTE(probe);
        _ASSERTE(contents);
        probe->InitProber(keyA, keyB, &contents[0]);
        return TRUE;
    }
    //find the requested entry (keys of prober), if not there return CALL_STUB_EMPTY_ENTRY
    size_t Find(Prober* probe);
    //add the entry, if it is not already there.  Probe is used to search.
    //Return the entry actually containted (existing or added)
    size_t Add(size_t entry, Prober* probe);
    void IncrementCount();

    // Create a FastTable with space for numberOfEntries. Please note that this method
    // does not throw on OOM. **YOU MUST CHECK FOR NULL RETURN**
    static FastTable* MakeTable(size_t numberOfEntries)
    {
        CONTRACTL {
            THROWS;
            GC_TRIGGERS;
            INJECT_FAULT(COMPlusThrowOM(););
        } CONTRACTL_END;

        size_t size = CALL_STUB_MIN_ENTRIES;
        while (size < numberOfEntries) {size = size<<1;}
//        if (size == CALL_STUB_MIN_ENTRIES)
//            size += 3;
        size_t* bucket = new size_t[(sizeof(FastTable)/sizeof(size_t))+size+CALL_STUB_FIRST_INDEX];
        FastTable* table = new (bucket) FastTable();
        table->InitializeContents(size);
        return table;
    }
    //Initialize as empty
    void InitializeContents(size_t size)
    {
        LIMITED_METHOD_CONTRACT;
        memset(&contents[0], CALL_STUB_EMPTY_ENTRY, (size+CALL_STUB_FIRST_INDEX)*sizeof(BYTE*));
        contents[CALL_STUB_MASK_INDEX] = size-1;
    }
    inline size_t tableMask() {LIMITED_METHOD_CONTRACT; return (size_t) (contents[CALL_STUB_MASK_INDEX]);}
    inline size_t tableSize() {LIMITED_METHOD_CONTRACT; return tableMask()+1;}
    inline size_t tableCount() {LIMITED_METHOD_CONTRACT; return (size_t) (contents[CALL_STUB_COUNT_INDEX]);}
    inline BOOL isFull()
    {
        LIMITED_METHOD_CONTRACT;
        return (tableCount()+1) * 100 / CALL_STUB_LOAD_FACTOR >= tableSize();
    }
    //we store (size-1) in bucket[CALL_STUB_MASK_INDEX==0],
    //we store the used count in bucket[CALL_STUB_COUNT_INDEX==1],
    //we have an unused cell to use as a temp at bucket[CALL_STUB_DEAD_LINK==2],
    //and the table starts at bucket[CALL_STUB_FIRST_INDEX==3],
    size_t contents[0];
};
#ifdef _MSC_VER 
#pragma warning(pop)
#endif

/******************************************************************************************************
BucketTable is a bucketized hash table.  It uses FastTables for its buckets.  The hash tables
used by the VirtualCallStubManager are BucketTables.  The number of buckets is fixed at the time
the table is created.  The actual buckets are allocated as needed, and grow as necessary.  The reason
for using buckets is primarily to reduce the cost of growing, since only a single bucket is actually
grown at any given time.  Since the hash tables are accessed infrequently, the load factor that
controls growth is quite high (90%).  Since we use hashing to pick the bucket, and we use hashing to
lookup inside the bucket, it is important that the hashing function used here is orthogonal to the ones
used in the buckets themselves (see FastTable::FormHash).
*/
class BucketTable
{
public:
    BucketTable(size_t numberOfBuckets)
    {
        WRAPPER_NO_CONTRACT;
        size_t size = CALL_STUB_MIN_BUCKETS;
        while (size < numberOfBuckets) {size = size<<1;}
        buckets = AllocateBuckets(size);
        // Initialize statistics counters
        memset(&stats, 0, sizeof(stats));
    }

    ~BucketTable()
    {
        LIMITED_METHOD_CONTRACT;
        if(buckets != NULL)
        {
            size_t size = bucketCount()+CALL_STUB_FIRST_INDEX;
            for(size_t ix = CALL_STUB_FIRST_INDEX; ix < size; ix++) delete (FastTable*)(buckets[ix]);
            delete buckets;
        }
    }

    //initialize a prober for the specified keys.
    BOOL SetUpProber(size_t keyA, size_t keyB, Prober *prober);
    //find the requested entry (keys of prober), if not there return CALL_STUB_EMPTY_ENTRY
    inline size_t Find(Prober* probe) {WRAPPER_NO_CONTRACT; return probe->Find();}
    //add the entry, if it is not already there.  Probe is used to search.
    size_t Add(size_t entry, Prober* probe);
    //reclaim abandoned buckets.  Buckets are abaondoned when they need to grow.
    //needs to be called inside a gc sync point.
    static void Reclaim();

    struct
    {
        UINT32 bucket_space;                    //# of bytes in caches and tables, not including the stubs themselves
        UINT32 bucket_space_dead;               //# of bytes of abandoned buckets not yet recycled.
    } stats;

    void LogStats();

private:
    inline size_t bucketMask() {LIMITED_METHOD_CONTRACT; return (size_t) (buckets[CALL_STUB_MASK_INDEX]);}
    inline size_t bucketCount() {LIMITED_METHOD_CONTRACT; return bucketMask()+1;}
    inline size_t ComputeBucketIndex(size_t keyA, size_t keyB)
    {
        LIMITED_METHOD_CONTRACT;
        size_t a = ((keyA>>16) + keyA);
        size_t b = ((keyB>>16) ^ keyB);
        return CALL_STUB_FIRST_INDEX+(((((a*CALL_STUB_HASH_CONST2)>>5)^((b*CALL_STUB_HASH_CONST1)>>5))+CALL_STUB_HASH_CONST2) & bucketMask());
    }
    //grows the bucket referenced by probe.
    BOOL GetMoreSpace(const Prober* probe);
    //creates storage in which to store references to the buckets
    static size_t* AllocateBuckets(size_t size)
    {
        LIMITED_METHOD_CONTRACT;
        size_t* buckets = new size_t[size+CALL_STUB_FIRST_INDEX];
        if (buckets != NULL)
        {
            memset(&buckets[0], CALL_STUB_EMPTY_ENTRY, (size+CALL_STUB_FIRST_INDEX)*sizeof(void*));
            buckets[CALL_STUB_MASK_INDEX] =  size-1;
        }
        return buckets;
    }
    inline size_t Read(size_t index)
    {
        LIMITED_METHOD_CONTRACT;
        CONSISTENCY_CHECK(index <= bucketMask()+CALL_STUB_FIRST_INDEX);
        return VolatileLoad(&buckets[index]);
    }

#ifdef _MSC_VER
#pragma warning(disable: 4267) //work-around for the compiler
#endif
    inline void Write(size_t index, size_t value)
    {
        LIMITED_METHOD_CONTRACT;
        CONSISTENCY_CHECK(index <= bucketMask()+CALL_STUB_FIRST_INDEX);
        VolatileStore(&buckets[index], value);
    }
#ifdef _MSC_VER
#pragma warning(default: 4267)
#endif

    // We store (#buckets-1) in    bucket[CALL_STUB_MASK_INDEX  ==0]
    // We have two unused cells at bucket[CALL_STUB_COUNT_INDEX ==1]
    //                         and bucket[CALL_STUB_DEAD_LINK   ==2]
    // and the table starts at     bucket[CALL_STUB_FIRST_INDEX ==3]
    // the number of elements is   bucket[CALL_STUB_MASK_INDEX]+CALL_STUB_FIRST_INDEX
    size_t* buckets;
    static FastTable* dead;             //linked list head of to be deleted (abandoned) buckets
};

#endif // !CROSSGEN_COMPILE

#endif // !_VIRTUAL_CALL_STUB_H
