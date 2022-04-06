// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: VirtualCallStub.h
//



//
// See code:VirtualCallStubManager for details
//
// ============================================================================

#ifndef _VIRTUAL_CALL_STUB_H
#define _VIRTUAL_CALL_STUB_H
#include "typehashingalgorithms.h"
#include "shash.h"

#define CHAIN_LOOKUP

#if defined(TARGET_X86)
// If this is uncommented, leaves a file "StubLog_<pid>.log" with statistics on the behavior
// of stub-based interface dispatch.
//#define STUB_LOGGING
#endif

#include "stubmgr.h"

/////////////////////////////////////////////////////////////////////////////////////
// Forward class declarations
template <class EntryType>
class FastTable;
template <class EntryType>
class BucketTable;
class Entry;
class VirtualCallStubManager;
class VirtualCallStubManagerManager;
struct LookupHolder;
struct DispatchHolder;
struct ResolveHolder;
struct VTableCallHolder;
class LookupEntry;
class ResolveCacheEntry;
class DispatchEntry;
class ResolveEntry;
class VTableCallEntry;

/////////////////////////////////////////////////////////////////////////////////////
// Forward function declarations
extern "C" void InContextTPQuickDispatchAsmStub();

extern "C" PCODE STDCALL VSD_ResolveWorker(TransitionBlock * pTransitionBlock,
                                           TADDR siteAddrForRegisterIndirect,
                                           size_t token
#ifndef TARGET_X86
                                           , UINT_PTR flags
#endif
                                           );


/////////////////////////////////////////////////////////////////////////////////////
#if defined(TARGET_X86) || defined(TARGET_AMD64)
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
    // a direct delegate call has a sequence defined by the jit but a multicast or wrapper delegate
    // are defined in a stub and have a different shape
    //
    PTR_PCODE       m_siteAddr;     // Stores the address of an indirection cell
    PCODE           m_returnAddr;

public:

#if defined(TARGET_X86)
    StubCallSite(TADDR siteAddrForRegisterIndirect, PCODE returnAddr);

    PCODE           GetCallerAddress();
#else // !defined(TARGET_X86)
    // On platforms where we always use an indirection cell things
    // are much simpler - the siteAddr always stores a pointer to a
    // value that in turn points to the indirection cell.

    StubCallSite(TADDR siteAddr, PCODE returnAddr)
       { LIMITED_METHOD_CONTRACT; m_siteAddr = dac_cast<PTR_PCODE>(siteAddr); m_returnAddr = returnAddr; }

    PCODE           GetCallerAddress()     { LIMITED_METHOD_CONTRACT; return m_returnAddr; }
#endif // !defined(TARGET_X86)

    PCODE           GetSiteTarget()        { WRAPPER_NO_CONTRACT; return *(GetIndirectCell()); }
    void            SetSiteTarget(PCODE newTarget);
    PTR_PCODE       GetIndirectCell()      { LIMITED_METHOD_CONTRACT; return dac_cast<PTR_PCODE>(m_siteAddr); }
    PTR_PCODE *     GetIndirectCellAddress() { LIMITED_METHOD_CONTRACT; return &m_siteAddr; }

    PCODE           GetReturnAddress() { LIMITED_METHOD_CONTRACT; return m_returnAddr; }
};

// These are the assembly language entry points that the stubs use when they want to go into the EE

extern "C" void ResolveWorkerAsmStub();               // resolve a token and transfer control to that method
extern "C" void ResolveWorkerChainLookupAsmStub();    // for chaining of entries in the cache

#ifdef TARGET_X86
extern "C" void BackPatchWorkerAsmStub();             // backpatch a call site to point to a different stub
#ifdef TARGET_UNIX
extern "C" void BackPatchWorkerStaticStub(PCODE returnAddr, TADDR siteAddrForRegisterIndirect);
#endif // TARGET_UNIX
#endif // TARGET_X86


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
    friend class VirtualCallStubManagerManager;
    friend class VirtualCallStubManagerIterator;

#if defined(DACCESS_COMPILE)
    friend class ClrDataAccess;
    friend class DacDbiInterfaceImpl;
#endif // DACCESS_COMPILE

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
#ifdef TARGET_AMD64
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

#ifdef TARGET_AMD64
    // Used to allocate a long jump dispatch stub. See comment around
    // m_fShouldAllocateLongJumpDispatchStubs for explaination.
    DispatchHolder *GenerateDispatchStubLong(PCODE addrOfCode,
                                             PCODE addrOfFail,
                                             void *pMTExpected,
                                             size_t dispatchToken);
#endif

    ResolveHolder *GenerateResolveStub(PCODE addrOfResolver,
                                       PCODE addrOfPatcher,
                                       size_t dispatchToken
#if defined(TARGET_X86) && !defined(UNIX_X86_ABI)
                                       , size_t stackArgumentsSize
#endif
                                       );

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
#ifndef TARGET_X86
                                   , UINT_PTR flags
#endif
                                   );

#if defined(TARGET_X86) && defined(TARGET_UNIX)
    friend void BackPatchWorkerStaticStub(PCODE returnAddr, TADDR siteAddrForRegisterIndirect);
#endif

    //These are the entrypoints that the stubs actually end up calling via the
    // xxxAsmStub methods above
    static void STDCALL BackPatchWorkerStatic(PCODE returnAddr, TADDR siteAddrForRegisterIndirect);

    void ResolveWorkerInternal(StubCallSite* pCallSite,
                                DispatchToken token,
                                StubKind stubKind,
                                MethodTable* objectType,
                                BOOL patch,
                                PCODE target,
                                BOOL bCallToShorterLivedTarget,
                                PCODE stub,
                                VirtualCallStubManager *pCalleeMgr);
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

#ifdef TARGET_AMD64
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

    BucketTable<LookupEntry> *   lookups;            // hash table of lookups keyed by tokens
    BucketTable<ResolveCacheEntry> *   cache_entries;      // hash table of dispatch token/target structs for dispatch cache
    BucketTable<DispatchEntry> *   dispatchers;        // hash table of dispatching stubs keyed by tokens/actualtype
    BucketTable<ResolveEntry> *   resolvers;          // hash table of resolvers keyed by tokens/resolverstub
    BucketTable<VTableCallEntry> *   vtableCallers;      // hash table of vtable call stubs keyed by slot values

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

//#define PRIME_SIZE_VSD_BUCKET_TABLE
//#define XXHASH_HASH_FUNCTION_FASTTABLE

#define CALL_STUB_BIT_SHIFT 16


//size and mask of the cache used by resolve stubs
// CALL_STUB_CACHE_SIZE must be equal to 2^CALL_STUB_CACHE_NUM_BITS
#define CALL_STUB_CACHE_NUM_BITS 12 //10
#define CALL_STUB_CACHE_SIZE 4096 //1024
#define CALL_STUB_CACHE_MASK (CALL_STUB_CACHE_SIZE-1)
#define CALL_STUB_CACHE_PROBES 5
//min sizes for BucketTable and buckets and the growth and hashing constants
#ifdef PRIME_SIZE_VSD_BUCKET_TABLE
#define CALL_STUB_MIN_BUCKETS 29
#define CALL_STUB_MORE_BUCKETS 59
#else
#define CALL_STUB_MIN_BUCKETS 32
#endif

#define CALL_STUB_HASH_CONST1 1327
#define CALL_STUB_HASH_CONST2 43627
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
    struct Key_t
    {
        public:
        Key_t() : keyA(0), keyB(0) {}
        Key_t(size_t a, size_t b) : keyA(a), keyB(b) {}
        const size_t keyA;
        const size_t keyB;
    };
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
    static Key_t GetKey(size_t hashtableContents)
    {
        _ASSERTE(VirtualCallStubManager::isLookupStubStatic((PCODE)hashtableContents));
        LookupStub* stub = LookupHolder::FromLookupEntry((PCODE)hashtableContents)->stub();
        if (stub != NULL)
            return Key_t(stub->token(), 0);
        else
            return Key_t{};
    }
};
#endif // USES_LOOKUP_STUBS

class VTableCallEntry : public Entry
{
public:
    static Key_t GetKey(size_t hashtableContents)
    {
        _ASSERTE(VirtualCallStubManager::isVtableCallStubStatic((PCODE)hashtableContents));
        VTableCallStub* stub = VTableCallHolder::FromVTableCallEntry((PCODE)hashtableContents)->stub();
        if (stub != NULL)
            return Key_t(stub->token(), 0);
        else
            return Key_t{};
    }
};

/**********************************************************************************************
ResolveCacheEntry wraps a ResolveCacheElem and provides lookup functionality for entries that
were created that may be added to the ResolveCache
*/
class ResolveCacheEntry : public Entry
{
public:
    static Key_t GetKey(size_t hashtableContents)
    {
        ResolveCacheElem *pElem = (ResolveCacheElem*) hashtableContents;
        if (pElem != NULL)
            return Key_t(pElem->token, (size_t)pElem->pMT);
        else
            return Key_t{};
    }

    static const BYTE* Target(size_t hashtableContents)
    {
        ResolveCacheElem *pElem = (ResolveCacheElem*) hashtableContents;
        if (pElem != NULL)
            return (const BYTE *)pElem->target;
        else
            return NULL;
    }
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
    static Key_t GetKey(size_t hashtableContents)
    {
        _ASSERTE(VirtualCallStubManager::isResolvingStubStatic((PCODE)hashtableContents));
        ResolveStub* stub = ResolveHolder::FromResolveEntry((PCODE)hashtableContents)->stub();

        if (stub != NULL)
            return Key_t(stub->token(), 0);
        else
            return Key_t{};
    }
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
    static Key_t GetKey(size_t hashtableContents)
    {
        _ASSERTE(VirtualCallStubManager::isDispatchingStubStatic((PCODE)hashtableContents));
        auto stub = DispatchHolder::FromDispatchEntry((PCODE)hashtableContents)->stub();
        if (stub)
        {
            ResolveHolder * resolveHolder = ResolveHolder::FromFailEntry(stub->failTarget());
            size_t token = resolveHolder->stub()->token();
            _ASSERTE(token == VirtualCallStubManager::GetTokenFromStub((PCODE)stub));

            return Key_t(token, (size_t)(stub->expectedMT()));
        }
        else
        {
            return Key_t{};
        }
    }

    static PCODE Target(size_t hashtableContents)
    {
        _ASSERTE(VirtualCallStubManager::isDispatchingStubStatic((PCODE)hashtableContents));
        auto stub = DispatchHolder::FromDispatchEntry((PCODE)hashtableContents)->stub();
        return stub ? stub->implTarget()  : 0;
    }
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

template<class EntryType>
class EntryHashTraits : public DefaultSHashTraits<size_t>
{
public:
    typedef typename DefaultSHashTraits<size_t>::element_t element_t;
    typedef typename DefaultSHashTraits<size_t>::count_t count_t;

    typedef const Entry::Key_t key_t;
    static const bool s_supports_remove = false;
    static const bool s_EnableAutomaticGrowth = false;
    static const bool s_UseVolatileStoreWithBarrierDuringAdd = true;

    static key_t GetKey(element_t e)
    {
        LIMITED_METHOD_CONTRACT;
        return EntryType::GetKey(e);
    }
    static BOOL Equals(key_t k1, key_t k2)
    {
        LIMITED_METHOD_CONTRACT;
        return k1.keyA == k2.keyA && k1.keyB == k2.keyB;
    }
    static count_t Hash(key_t k)
    {
        LIMITED_METHOD_CONTRACT;
#ifndef XXHASH_HASH_FUNCTION_FASTTABLE
        size_t keyAAdjusted = k.keyA;
        size_t keyBAdjusted = k.keyB;
#ifdef TARGET_64BIT
        keyAAdjusted ^= keyAAdjusted >> 32;
        keyBAdjusted ^= keyBAdjusted >> 32;
#endif
        size_t a = ((keyAAdjusted>>CALL_STUB_BIT_SHIFT) + k.keyA);
        size_t b = ((keyBAdjusted>>CALL_STUB_BIT_SHIFT) ^ k.keyB);
        size_t localIndex = (((a*CALL_STUB_HASH_CONST1)>>4)+((b*CALL_STUB_HASH_CONST2)>>4)+CALL_STUB_HASH_CONST1);
#else
#ifdef TARGET_64BIT
        int localIndex = CombineFourValuesIntoHash((UINT32)key1, (UINT32)(key1 >> 32), (UINT32)key2, (UINT32)(key2 >> 32));
#else
        int localIndex = CombineTwoValuesIntoHash(key1, key2);
#endif
#endif
        return (count_t)localIndex;
    }

    static element_t Null() { LIMITED_METHOD_CONTRACT; return 0; }
    static bool IsNull(const element_t &e) { LIMITED_METHOD_CONTRACT; return e == 0; }
};

class CleanupDuringGC
{
    static CrstStatic _cleanupListCrst;
    static CleanupDuringGC* dead;
public:
    static void Init();
    static void Reclaim();
    static void AddToList(CleanupDuringGC* newItem);

    CleanupDuringGC *next = nullptr;
    virtual ~CleanupDuringGC() {}
};

template<class EntryType>
class FastTable
{
    typedef SHash< EntryHashTraits<EntryType>> SHashType;

    struct SHashWithCleanup : public CleanupDuringGC
    {
        SHashType _hash;
    };

    // Make SHash a pointer here, so that we can 
    SHashWithCleanup *_pshash;
    Crst _hashLock;
public:
    FastTable() : _pshash(nullptr), _hashLock(CrstLeafLock, CRST_UNSAFE_COOPGC)
    {
        LIMITED_METHOD_CONTRACT;
        VolatileStore(&_pshash, new SHashWithCleanup());
    }

    ~FastTable() { LIMITED_METHOD_CONTRACT; }

    //find the requested entry (keys of prober), if not there return CALL_STUB_EMPTY_ENTRY
    size_t Find(Entry::Key_t key)
    {
        static_assert_no_msg(0 == CALL_STUB_EMPTY_ENTRY);
        auto shash = VolatileLoadWithoutBarrier(&_pshash);
        // This SHash lookup is not done under a lock, and follows a careful set of rules for when 
        return shash->_hash.Lookup(key);
    }
    //add the entry, if it is not already there.  Probe is used to search.
    //Return the entry actually containted (existing or added)
    size_t FindOrAdd(size_t entry, Entry::Key_t key)
    {
        SHashWithCleanup *oldSHash = nullptr;
        size_t result = 0;

        do
        {
            ForbidSuspendThreadHolder suspend;
            CrstHolder ch(&_hashLock);
            _ASSERTE(key.keyA == EntryHashTraits<EntryType>::GetKey(entry).keyA);
            _ASSERTE(key.keyB == EntryHashTraits<EntryType>::GetKey(entry).keyB);
            size_t lookupResult1 = _pshash->_hash.Lookup(key);
            if (lookupResult1 != 0)
            {
                result = lookupResult1;
                break;
            }

            if (_pshash->_hash.GetCount() == _pshash->_hash.GetCapacity())
            {
                // Since we can't grow a SHash in place and maintain lock-free access
                // Build a new SHash, and then drop it in.
                // 

                NewHolder<SHashWithCleanup> newHash = new SHashWithCleanup();

                newHash->_hash.Reallocate(_pshash->_hash.ExpectedNewSize());
                for (auto cur = _pshash->_hash.Begin(), end = _pshash->_hash.End();
                    (cur != end); cur++)
                {
                    newHash->_hash.AddNonAsynchronous(*cur);
                }
                // At this point the newHash is a complete copy of the old hash
                // And the store cannot fail.
                oldSHash = _pshash;
                VolatileStore(&_pshash, newHash.Extract());
            }
            _pshash->_hash.Add(entry);
            result = entry;
        } while(0);

        if (oldSHash != nullptr)
        {
            CleanupDuringGC::AddToList(oldSHash);
        }

        return result;
    }
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
template<class EntryType>
class BucketTable
{
public:
    BucketTable(size_t numberOfBuckets)
    {
        WRAPPER_NO_CONTRACT;
#ifdef PRIME_SIZE_VSD_BUCKET_TABLE
        size_t size = numberOfBuckets;
        _bucketCount = numberOfBuckets;
#else
        size_t size = CALL_STUB_MIN_BUCKETS;
        while (size < numberOfBuckets) {size = size<<1;};
        _bucketMask = size - 1;
#endif
        buckets = new FastTable<EntryType>[size];
    }

    ~BucketTable()
    {
        LIMITED_METHOD_CONTRACT;
        delete [] buckets;
    }

    //find the requested entry (keys of prober), if not there return CALL_STUB_EMPTY_ENTRY
    inline size_t Find(Entry::Key_t key)
    {
        WRAPPER_NO_CONTRACT;
        return buckets[ComputeBucketIndex(key)].Find(key);
    }
    //add the entry, if it is not already there.  Probe is used to search.
    template<class Generator>
    size_t FindOrAdd(Entry::Key_t key, Generator &gen)
    {
        WRAPPER_NO_CONTRACT;
        FastTable<EntryType>* bucket = &buckets[ComputeBucketIndex(key)];
        size_t result = bucket->Find(key);
        if (result == CALL_STUB_EMPTY_ENTRY)
        {
            size_t newValue = gen();
            _ASSERTE(newValue != CALL_STUB_EMPTY_ENTRY);
            result = bucket->FindOrAdd(newValue, key);
        }
        return result;
    }

private:
#ifdef PRIME_SIZE_VSD_BUCKET_TABLE
    inline size_t bucketCount() {LIMITED_METHOD_CONTRACT; return _bucketCount; }
#else
    inline size_t bucketMask() {LIMITED_METHOD_CONTRACT; return _bucketMask; }
    inline size_t bucketCount() {LIMITED_METHOD_CONTRACT; return bucketMask()+1;}
#endif
    inline size_t ComputeBucketIndex(Entry::Key_t key)
    {
        LIMITED_METHOD_CONTRACT;

        size_t keyAAdjusted = key.keyA;
        size_t keyBAdjusted = key.keyB;
#ifdef TARGET_64BIT
        keyAAdjusted ^= keyAAdjusted >> 32;
        keyBAdjusted ^= keyBAdjusted >> 32;
#endif
        size_t a = ((keyAAdjusted>>CALL_STUB_BIT_SHIFT) + key.keyA);
        size_t b = ((keyBAdjusted>>CALL_STUB_BIT_SHIFT) ^ key.keyB);
#ifdef PRIME_SIZE_VSD_BUCKET_TABLE
        return (((((a*CALL_STUB_HASH_CONST2)>>5)^((b*CALL_STUB_HASH_CONST1)>>5))+CALL_STUB_HASH_CONST2) % bucketCount());
#else
        return (((((a*CALL_STUB_HASH_CONST2)>>5)^((b*CALL_STUB_HASH_CONST1)>>5))+CALL_STUB_HASH_CONST2) & bucketMask());
#endif
    }

    FastTable<EntryType> *buckets;
#ifdef PRIME_SIZE_VSD_BUCKET_TABLE
    size_t _bucketCount;
#else
    size_t _bucketMask;
#endif
};


#endif // !_VIRTUAL_CALL_STUB_H
