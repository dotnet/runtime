// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// Methods to support the implementation of Constrained Execution Regions (CERs). This includes logic to walk the IL of methods to
// determine the statically determinable call graph and prepare each submethod (jit, prepopulate generic dictionaries etc.,
// everything needed to ensure that the runtime won't generate implicit failure points during the execution of said call graph).
//

//


#ifndef __CONSTRAINED_EXECUTION_REGION_H
#define __CONSTRAINED_EXECUTION_REGION_H


#include <corhlpr.h>
#include <typestring.h>


// An enumeration that abstracts the interesting information (from our point of view) present in a reliability contract decorating a
// method.
enum ReliabilityContractLevel
{
    RCL_UNKNOWN             = -1,   // The contract attribute hasn't been read yet
    RCL_NO_CONTRACT         = 0,    // No contract (or a fairly useless one) was specified
    RCL_BASIC_CONTRACT      = 1,    // The contract promises enough for the method to be a legal member of a CER call graph
    RCL_PREPARE_CONTRACT    = 2,    // The contract promises enough to be worth preparing the method as part of a CER call graph
};

// Various definitions used to parse reliability contracts. These must be kept synchronized with the managed version in
// BCL\System\Runtime\Reliability\ReliabilityContractAttribute.cs

#define RELIABILITY_CONTRACT_NAME       "System.Runtime.ConstrainedExecution.ReliabilityContractAttribute"
#define RC_CONSISTENCY_PROP_NAME        "ConsistencyGuarantee"
#define RC_CER_PROP_NAME                "Cer"

enum {
    RC_CONSISTENCY_CORRUPT_PROCESS      = 0,
    RC_CONSISTENCY_CORRUPT_APPDOMAIN    = 1,
    RC_CONSISTENCY_CORRUPT_INSTANCE     = 2,
    RC_CONSISTENCY_CORRUPT_NOTHING      = 3,
    RC_CONSISTENCY_UNDEFINED            = 4,
    RC_CER_NONE                         = 0,
    RC_CER_MAYFAIL                      = 1,
    RC_CER_SUCCESS                      = 2,
    RC_CER_UNDEFINED                    = 3
};


// We compact the reliability contract states above into a single DWORD format easy to cache at the assembly and class level
// opaquely. We also encode in this DWORD whether a given part of the state has been defined yet (an assembly might set a
// consistency level without specifying a cer level, for instance, and this information is vital when merging states between
// assembly, class and method levels).
// The macros below handle the encoding so nobody else needs to know the details.

// The base state for an encoded DWORD: neither consistency or cer defined.
#define RC_NULL RC_ENCODE(RC_CONSISTENCY_UNDEFINED, RC_CER_UNDEFINED)

// Extract the raw consistency value from an encoded DWORD.
#define RC_CONSISTENCY(_encoded) ((_encoded) >> 2)

// Extract the raw cer value from an encoded DWORD.
#define RC_CER(_encoded) ((_encoded) & 3)

// Produce an encoded DWORD from a pair of raw consistency and cer values. Values must have been range validated first.
#define RC_ENCODE(_consistency, _cer) (DWORD)(((_consistency) << 2) | (_cer))

// Produce an abstracted ReliabilityContractLevel from an encoded DWORD, see CheckForReliabilityContract for details of the rules.
#define RC_ENCODED_TO_LEVEL(_encoded)                                                   \
    ((RC_CONSISTENCY(_encoded) == RC_CONSISTENCY_UNDEFINED ||                           \
      RC_CONSISTENCY(_encoded) < RC_CONSISTENCY_CORRUPT_INSTANCE) ? RCL_NO_CONTRACT :   \
     (RC_CER(_encoded) == RC_CER_UNDEFINED ||                                           \
      RC_CER(_encoded) == RC_CER_NONE) ? RCL_BASIC_CONTRACT :                           \
     RCL_PREPARE_CONTRACT)

// Given two DWORD encodings presumed to come from different scopes (e.g. method and class) merge them to find the effective
// contract state. It's presumed the first encoding is the most tightly scoped (i.e. method would go first in the example above) and
// therefore takes precedence.
#define RC_MERGE(_old, _new) RC_ENCODE((RC_CONSISTENCY(_old) == RC_CONSISTENCY_UNDEFINED) ? \
                                       RC_CONSISTENCY(_new) : RC_CONSISTENCY(_old),         \
                                       (RC_CER(_old) == RC_CER_UNDEFINED) ?                 \
                                       RC_CER(_new) : RC_CER(_old))                         \

// Return true if either consistency or cer has not been specified in the encoded DWORD given.
#define RC_INCOMPLETE(_encoded) (RC_CONSISTENCY(_encoded) == RC_CONSISTENCY_UNDEFINED || RC_CER(_encoded) == RC_CER_UNDEFINED)

// Look for reliability contracts at the method, class and assembly level and parse them to extract the information we're interested
// in from a runtime preparation viewpoint. This information is abstracted in the form of the ReliabilityContractLevel enumeration.
ReliabilityContractLevel CheckForReliabilityContract(MethodDesc *pMD);


// Structure used to track enough information to identify a method (possibly generic or belonging to a generic class). Includes a
// MethodDesc pointer and a SigTypeContext (values of class and method type parameters to narrow down the exact method being refered
// to). Similar to MethodContext (see ConstrainedExecutionRegion.cpp), but without the unneeded list link field (we expect to embed
// these in arrays, hence the name).
struct MethodContextElement
{
    FixupPointer<PTR_MethodDesc> m_pMethodDesc; // Pointer to a MethodDesc
    FixupPointer<PTR_MethodTable> m_pExactMT;   // Exact type to disambiguate code shared by instantiations

    MethodDesc * GetMethodDesc()
    {
        return m_pMethodDesc.GetValue();
    }

    MethodTable * GetExactMT()
    {
        return m_pExactMT.GetValue();
    }
};


// Base structure used to track which CERs have been prepared so far.
// These structures are looked up via a per-assembly hash table using the root method desc as a key.
// Used to avoid extra work in both the jit and PrepareMethod calls. The latter case is more involved because we support preparing a
// CER with generic type parameters (the instantiation is passed in along with the method in the PrepareMethod call). In that case
// we need to track exactly which instantiations we've prepared for a given method.
struct CerPrepInfo
{
    CerPrepInfo() :
        m_fFullyPrepared(false),
        m_fRequiresInstantiation(false),
        m_fMethodHasCallsWithinExplicitCer(false)
    {
        CONTRACTL {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
        } CONTRACTL_END;

        if (!m_sIsInitAtInstHash.Init(17, NULL, NULL, FALSE))
            COMPlusThrowOM();
    }

    bool                        m_fFullyPrepared;           // True implies we've prep'd this once and there are no shared instantiations
    bool                        m_fRequiresInstantiation;   // True implies that this method is shared amongst multiple instantiations
    bool                        m_fMethodHasCallsWithinExplicitCer; // True if method contains calls out from within an explicit PCER range
    EEInstantiationHashTable    m_sIsInitAtInstHash;        // Hash of instantiations we've prepared this CER for 
};


#ifdef FEATURE_PREJIT

// Structure used to represent a CER by a root method and a list of MethodContextElements that indicate all the methods contained.
// The MethodContextElement list is terminated with a sentinel entry (m_pMethodDesc set to NULL).
// Keep this structure small since we'll access the whole array of them randomly at runtime; density is our best friend.
struct CerRoot
{
    MethodDesc                 *m_pRootMD;                  // Root method (no type context since it never has type params)
    MethodContextElement       *m_pList;                    // List of methods in this CER
};

// Class used to track all the CERs rooted at methods defined within a given module that are discovered at ngen time. This data is
// then used at runtime to determine when and how to perform necessary restoration work so that the CERs don't encounter any
// unexpected failure points during execution.
// During ngen this class keeps a dynamically expanded array of CER roots (both the class and the array are allocated from a win32
// heap). When we save the image to storage (and thus know the final size of the table) we combine the two so that at runtime
// they're adjacent and exactly the right size.
class CerNgenRootTable
{
#ifdef DACCESS_COMPILE
    friend class NativeImageDumper;
#endif

    DWORD                      *m_pRestoreBitmap;           // Pointer to array of restored flag bits
    DWORD                       m_cRoots;                   // Count of root methods represented
    DWORD                       m_cSlots;                   // Extra empty slots at the tail of the array below (ngen time only)
    CerRoot                    *m_pRoots;                   // Pointer to array of CER roots (sorted by RootMD address)
    MethodContextElement      **m_pRootsInCompilationOrder; // Pointer to array of CerRoot::m_pList (in the order AddRoot is called)

public:

    CerNgenRootTable() :
        m_pRestoreBitmap(NULL),
        m_cRoots(0),
        m_cSlots(0),
        m_pRoots(NULL),
        m_pRootsInCompilationOrder(NULL)
    {
    }

    ~CerNgenRootTable()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            SO_TOLERANT;
            MODE_ANY;
        }
        CONTRACTL_END;
        delete m_pRestoreBitmap;
        delete m_pRoots;
        delete m_pRootsInCompilationOrder;
    }

    // Add a new root to the table, expanding it as necessary. Note that this method must be called with the CerCrst already held.
    void AddRoot(MethodDesc *pRootMD, MethodContextElement *pList);

    // Retrieve the address of the list of methods for the CER rooted at the given index.
    inline MethodContextElement *GetList(DWORD dwIndex) { LIMITED_METHOD_CONTRACT; _ASSERTE(dwIndex < m_cRoots); return m_pRoots[dwIndex].m_pList; }

    // Retrieve the address of the list of methods for the CER rooted at the given method. (The root must exist).
    inline MethodContextElement *GetList(MethodDesc *pRootMD) { WRAPPER_NO_CONTRACT; return GetList(FindIndex(pRootMD)); }

    // Indicate whether the given method has ngen restoration information associated with it.
    inline bool IsNgenRootMethod(MethodDesc *pRootMD) { WRAPPER_NO_CONTRACT; return FindIndex(pRootMD) != NoSuchRoot; }

    // Prepare the CER rooted at the given method (it's OK to pass a MethodDesc* that doesn't root a CER, in which case the method
    // is a no-op).
    void Restore(MethodDesc *pRootMD);

    // Ngen callouts to help serialize this structure and its children to storage.
    void Save(DataImage *image, CorProfileData *profileData);
    void Fixup(DataImage *image);
    void FixupRVAs(DataImage *image);

    // Calculate (in bytes) the size of bitmap to allocate to record whether each CER has been restored at runtime. Size is
    // rounded up to DWORD alignment.
    inline DWORD SizeOfRestoreBitmap()
    {
        LIMITED_METHOD_CONTRACT;
        return ((m_cRoots + 31) / 32) * sizeof(DWORD);
    }

    inline DWORD GetRootCount() { LIMITED_METHOD_CONTRACT; return m_cRoots; }
    inline CerRoot *GetRoots() { LIMITED_METHOD_CONTRACT; return m_pRoots; }
    inline DWORD *GetRestoreBitmap() { LIMITED_METHOD_CONTRACT; return m_pRestoreBitmap; }

private:
    enum { NoSuchRoot = 0xffffffff };

    // Locate the index of a given CerRoot record in the array given the root method. This is used to access the array and to locate
    // the restored flag for the entry in the restored bitmap. NoSuchRoot is returned if the root cannot be found.
    DWORD FindIndex(MethodDesc *pRootMD);
};

#endif


// Default initial size for hash table used to track CerPrepInfo structures on a per-module basis.
#define CER_DEFAULT_HASH_SIZE   17


// Structure used to track a single exception handling range (catch, finally etc.). We build an array of these and then track which
// ones have become 'activated' by virtue of having their try clause immediately preceded by a call to our preparation marker
// method. This allows us to determine which call sites in the method body should be followed during method preparation.
struct EHClauseRange
{
    DWORD   m_dwTryOffset;
    DWORD   m_dwHandlerOffset;
    DWORD   m_dwHandlerLength;
    bool    m_fActive;
};


// Structure used to track enough information to identify a method (possibly generic or belonging to a generic class). Includes a
// MethodDesc pointer and a SigTypeContext (values of class and method type parameters to narrow down the exact method being refered
// to). The structure also contains a next pointer so that it can be placed in a singly linked list (see MethodContextStack below).
struct MethodContext
{
    MethodContext  *m_pNext;            // Next MethodContext in a MethodContextStack list
    MethodDesc     *m_pMethodDesc;      // Pointer to a MethodDesc
    SigTypeContext  m_sTypeContext;     // Additional type parameter information to qualify the exact method targetted
    bool            m_fRoot;            // Does this method contain a CER root of its own?

    // Allocate and initialize a MethodContext from the per-thread stacking allocator (we assume the checkpoint has already been
    // taken).
    static MethodContext* PerThreadAllocate(MethodDesc *pMD, SigTypeContext *pTypeContext)
    {
        CONTRACTL {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
        } CONTRACTL_END;


        MethodContext *pContext = new (&GetThread()->m_MarshalAlloc) MethodContext();
        pContext->m_pMethodDesc = pMD;
        pContext->m_sTypeContext = *pTypeContext;
        pContext->m_fRoot = false;

        return pContext;
    }

    // Determine if two MethodContexts are equivalent (same MethodDesc pointer and identical arrays of TypeHandles in the
    // TypeContext).
    bool Equals(MethodContext *pOther)
    {
        WRAPPER_NO_CONTRACT;

        if (pOther->m_pMethodDesc != m_pMethodDesc)
            return false;

        if (pOther->m_sTypeContext.m_classInst.GetNumArgs() != m_sTypeContext.m_classInst.GetNumArgs())
            return false;

        if (pOther->m_sTypeContext.m_methodInst.GetNumArgs() != m_sTypeContext.m_methodInst.GetNumArgs())
            return false;

        DWORD i;

        for (i = 0; i < m_sTypeContext.m_classInst.GetNumArgs(); i++)
            if (pOther->m_sTypeContext.m_classInst[i] != m_sTypeContext.m_classInst[i])
                return false;

        for (i = 0; i < m_sTypeContext.m_methodInst.GetNumArgs(); i++)
            if (pOther->m_sTypeContext.m_methodInst[i] != m_sTypeContext.m_methodInst[i])
                return false;

        return true;
    }

#ifdef _DEBUG
#define CER_DBG_MAX_OUT 4096
    char *ToString()
    {
        STATIC_CONTRACT_THROWS;
        STATIC_CONTRACT_GC_TRIGGERS;
        STATIC_CONTRACT_MODE_ANY;

        // Support up to two ToString calls before we re-use a buffer and overwrite previous output.
        static char szOut1[CER_DBG_MAX_OUT];
        static char szOut2[CER_DBG_MAX_OUT];
        static char *pszOut = szOut1;

        StackSString ssBuffer;
        StackScratchBuffer ssScratch;

        TypeString::AppendMethod(ssBuffer, m_pMethodDesc, m_sTypeContext.m_classInst, TypeString::FormatNamespace | TypeString::FormatAngleBrackets);
        sprintf_s(&pszOut[0], CER_DBG_MAX_OUT, "%s", ssBuffer.GetUTF8(ssScratch));

        char *pszReturn = pszOut;
        pszOut = pszOut == szOut1 ? szOut2 : szOut1;
        return pszReturn;
    }
#endif
};

// Maintains a stack of MethodContexts (implemented as a singly linked list with insert and remove operations only at the head).
class MethodContextStack
{
    MethodContext  *m_pFirst;       // The head of the linked list
    DWORD           m_cElements;    // Count of elements in the stack

public:

    // Initialize to an empty list.
    MethodContextStack()
    {
        LIMITED_METHOD_CONTRACT;

        m_pFirst = NULL;
        m_cElements = 0;
    }

    // Push a MethodContext pointer on the head of the list.
    void Push(MethodContext *pContext)
    {
        LIMITED_METHOD_CONTRACT;

        pContext->m_pNext = m_pFirst;
        m_pFirst = pContext;
        m_cElements++;
    }

    // Remove and retrieve the most recently pushed MethodContext. Return NULL if no more entries exist.
    MethodContext *Pop()
    {
        LIMITED_METHOD_CONTRACT;

        MethodContext* pContext = m_pFirst;
        if (pContext == NULL)
            return NULL;

        m_pFirst = pContext->m_pNext;
        m_cElements--;

        return pContext;
    }

    // Return true if an MethodContext equivalent to the argument exists in the stack.
    bool IsInStack(MethodContext *pMatchContext)
    {
        WRAPPER_NO_CONTRACT;

        MethodContext* pContext = m_pFirst;
        while (pContext) {
            if (pContext->Equals(pMatchContext))
                return true;
            pContext = pContext->m_pNext;
        }

        return false;
    }

    // Get count of elements in the stack.
    DWORD GetCount()
    {
        LIMITED_METHOD_CONTRACT;

        return m_cElements;
    }
};


class MethodCallGraphPreparer
{
    MethodDesc *m_pRootMD;
    SigTypeContext *m_pRootTypeContext;

    COR_ILMETHOD_DECODER *m_pMethodDecoder; 
    
    MethodContextStack  m_sLeftToProcess;       // MethodContexts we have yet to look at in this call graph
    MethodContextStack  m_sAlreadySeen;         // MethodContexts we've already processed at least once

    EHClauseRange      *m_pEHClauses;           // Array of exception handling clauses in current method (only if !fEntireMethod)
    DWORD               m_cEHClauses;           // Number of elements in above array
    CerPrepInfo        *m_pCerPrepInfo;         // Context recording how much preparation this region has had
    MethodContextStack  m_sPersist;             // MethodContexts we need to keep around past the 'prepare' phase of preparation
#ifdef FEATURE_NATIVE_IMAGE_GENERATION
    bool                m_fNgen;                // True when being called as part of an ngen
    MethodContextStack  m_sRootMethods;         // Methods containing a sub-CER (excludes the real root)
#endif // FEATURE_NATIVE_IMAGE_GENERATION
    Thread             *m_pThread;              // Cached managed thread pointer (for allocations and the like)
    bool                m_fPartialPreparation;  // True if we have unbound type vars at the CER root and can only prep one instantiation at a time

    bool                m_fEntireMethod;        // True if are preparing for the entire method
    bool                m_fExactTypeContext;
    bool                m_fMethodHasCallsWithinExplicitCer; // True if method contains calls out from within an explicit PCER range

    bool                m_fIgnoreVirtualCERCallMDA;  // True if VirtualCER MDA is not desirable to be fired

    MethodCallGraphPreparer * m_pNext;          // Links this instance on a per-thread stack used to detect
                                                // and defeat recursive preparations

  public:
    MethodCallGraphPreparer(MethodDesc *pRootMD, SigTypeContext *pRootTypeContext, bool fEntireMethod, bool fExactTypeContext, bool fIgnoreVirtualCERCallMDA = false);

    // Walk the call graph of the method given by m_pRootMD (and type context in m_pRootTypeContext which provides instantiation information
    // for generic methods/classes).
    //
    // If fEntireMethod is true then the entire body of pRootMD is scanned for callsites, otherwise we assume that one or more CER
    // exception handlers exist in the method and only the finally and catch blocks of such handlers are scanned for graph roots.
    //
    // Each method we come across in the call graph (excluding late bound invocation destinations precipitated by virtual or interface
    // calls) is jitted and has any generic dictionary information we can determine at jit time prepopulated. This includes implicit
    // cctor invocations. If this method is called at ngen time we will attach extra fixup information to the affected method to ensure
    // that fixing up the root method of the graph will cause all methods in the graph to be fixed up at that point also.
    //
    // Some generic dictionary entries may not be prepopulated if unbound type variables exist at the root of the call tree. Such cases
    // will be ignored (as for the virtual/interface dispatch case we assume the caller will use an out-of-band mechanism to pre-prepare
    // these entries explicitly).
    //
    // Returns true if the m_pRootMD contains a CER that calls outside the method. 
    //
    bool Run();
    
    // Methods used to control re-entrancy on the same thread. Essentially we'd like to avoid all re-entrancy
    // (since it can lead to unbounded recursion easily) but unfortunately jitting methods during the
    // preparation phase can cause this both directly (if we spot a sub-root) or indirectly (where implicit
    // jit execution of a cctor causes a possibly unrelated CER graph to be prepared). The algorithm we use to
    // avoid this records a stack of preparations attempts on the current thread (implemented via a singly
    // linked list of the MethodCallGraphPreparer instances). Re-entrant prepare requests become noops if
    // they're for a root method we're already processing (anywhere in the stack) and run to completion
    // otherwise. This prevents infinite recursion since it removes at least one method from the intersection
    // of the CER call graphs on each iteration. Theoretically it might not be the most efficient solution
    // since there might still be a lot of overlap between graphs, but in practice the number of sub-CER roots
    // is likely to be small and we won't recurse very far. This will still allow a re-entrant preparation
    // that is the result of running a cctor to potentially early-out (and thus allow code to run before its
    // CERs have been fully prepped). But this should only happen when a CER causes (directly or indirectly) a
    // cctor to run that depends on that CER having been prepared already, which we really can't do much
    // about.
    //
    BOOL CanPreparationProceed(MethodDesc * pMD, SigTypeContext * pTypeContext);

    static void BeginPrepareCerForHolder(MethodCallGraphPreparer *pPrepState);
    static void EndPrepareCerForHolder(MethodCallGraphPreparer *pPrepState);

    typedef Holder<MethodCallGraphPreparer*, BeginPrepareCerForHolder, EndPrepareCerForHolder> ThreadPreparingCerHolder;

  private:
    void GetEHClauses();
    void MarkEHClauseActivatedByCERCall(MethodContext *pContext, BYTE *pbIL, DWORD cbIL);
    bool CheckIfCallsiteWithinCER(DWORD dwOffset);
    bool ShouldGatherExplicitCERCallInfo();
    void LookForInterestingCallsites(MethodContext *pContext);
    void PrepareMethods();
    bool RecordResults();
};

// Determines whether the given method contains a CER root that can be pre-prepared (i.e. prepared at jit time).
bool ContainsPrePreparableCerRoot(MethodDesc *pMD);

// Prepares the critical finalizer call graph for the given object type (which must derive from CriticalFinalizerObject). This
// involves preparing at least the finalizer method and possibly some others (for SafeHandle and CriticalHandle derivations). If a
// module pointer is supplied then only the critical methods introduced in that module are prepared (this is used at ngen time to
// ensure that we're only generating ngen preparation info for the targetted module).
void PrepareCriticalFinalizerObject(MethodTable *pMT, Module *pModule = NULL);

void PrepareMethodDesc(MethodDesc* pMD, Instantiation classInst = Instantiation(), Instantiation methodInst = Instantiation(), BOOL onlyContractedMethod = FALSE, BOOL fIgnoreVirtualCERCallMDA = FALSE);
// Determine whether the method given as a parameter is the root of a CER.
// @todo: Need an x86 offset as well and logic to determine whether we're actually in a root-CER portion of the method (if the whole
// thing isn't the root).
bool IsCerRootMethod(MethodDesc *pMD);

// Fill the cache of overflowed generic dictionary entries that the jit maintains with all the overflow slots stored so far in the
// dictionary layout.
void PrepopulateGenericHandleCache(DictionaryLayout  *pDictionaryLayout,
                                   MethodDesc        *pMD,
                                   MethodTable       *pMT);
    
DWORD GetReliabilityContract(IMDInternalImport *pImport, mdToken tkParent);

#ifdef FEATURE_PREJIT

// Prepare the class if it is derived from CriticalFinalizerObject. This is used at ngen time since such classes are normally 
// prepared at runtime (at instantiation) and would therefore miss the ngen image.
void PrepareCriticalType(MethodTable *pMT);

// Prepare a method and its statically determinable call graph if a hint attribute has been applied. This is only called at ngen
// time to save additional preparation information into the ngen image that wouldn't normally be there (and thus lower runtime
// overheads).
void PrePrepareMethodIfNecessary(CORINFO_METHOD_HANDLE hMethod);

#endif


// A fixed sized hash table keyed by pointers and storing two bits worth of value for every entry. The value is stored in the low
// order bits of the keys, so the pointers must be at least DWORD aligned. No hash table expansion occurs so new entries will sooner
// or later overwrite old. The implementation uses no locks (all accesses are single aligned pointer sized operations and therefore
// inherently atomic).
// The purpose of this table is to store a smallish number of reliability contract levels for the most recently queried methods,
// mainly for the purpose of speeding up thread abort processing (where we will walk the stack probing methods for contracts,
// sometimes repeatedly). So we use a small fixed sized hash to speed up lookups on average but avoid impacting working set.
#define PHC_BUCKETS     29
#define PHC_CHAIN       5
#define PHC_DATA_MASK   3

class PtrHashCache
{
public:
    PtrHashCache();
    bool Lookup(void *pKey, DWORD *pdwValue);
    void Add(void *pKey, DWORD dwValue);

#ifdef _DEBUG
    void DbgDumpStats();
#endif

private:
    DWORD GetHash(void *pKey);

    UINT_PTR    m_rEntries[PHC_BUCKETS * PHC_CHAIN];

#ifdef _DEBUG
    DWORD       m_dwHits;
    DWORD       m_dwMisses;
#endif
};

#endif
