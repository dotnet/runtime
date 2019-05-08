// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Header:  RuntimeCallableWrapper.h
**
**
** Purpose: Contains types and method signatures for the RCW class
**
** 

===========================================================*/
//---------------------------------------------------------------------------------
// Runtime Callable WRAPPERS on COM objects
//  Purpose: wrap com objects to behave as CLR objects
//  Reqmts:  Wrapper has to have the same layout as the CLR objects
//
//  Data members of wrapper, are basically COM2 Interface pointers on the COM2 object
//  Interfaces that belong to the same object are stored in the same wrapper, IUnknown
//  pointer determines the identity of the object.
//  As new COM2 interfaces are seen on the same object, they need to be added to the 
//  wrapper, wrapper is allocated as a fixed size object with overflow chain.
//  
//  struct IPMap 
//  {
//      MethodTable *pMT; // identifies the managed interface class
//      IUnknown*   m_ip; // COM IP
//  }
//  
//  Issues : Performance/Identity trade-offs, create new wrappers or find and reuse wrappers
//      we use a hash table to track the wrappers and reuse them, maintains identity
//  RCWCache class maintains the lookup table and handles the clean up
//  Cast operations: requires a QI, unless a QI for that interface was done previously
//  
//  Threading : apartment model COM objects have thread affinity
//              choices: COM+ can guarantee thread affinity by making sure
//                       the calls are always made on the right thread
//              Advantanges: avoid an extra marshalling 
//              Dis.Advt.  : need to make sure legacy apartment semantics are preserved
//                           this includes any weird behaviour currently built into DCOM.
//
//  RCWs: Interface map (IMap) won't have any entries, the method table of RCWs
//  have a special flag to indicate that these managed objects
//  require special treatment for interface cast, call interface operations.
//  
//  Stubs : need to find the COM2 interface ptr, and the slot within the interface to
//          re-direct the call  
//  Marshaling params and results (common case should be fast)
//  
//-----------------------------------------------------------------------------------


#ifndef _RUNTIMECALLABLEWRAPPER_H
#define _RUNTIMECALLABLEWRAPPER_H

#ifndef FEATURE_COMINTEROP
#error FEATURE_COMINTEROP is required for this file
#endif // FEATURE_COMINTEROP

#include "utilcode.h"
#include "vars.hpp"
#include "spinlock.h"
#include "interoputil.h"
#include "mngstdinterfaces.h"
#include "excep.h"
#include "comcache.h"
#include "threads.h"
#include "comcache.h"
#include "jupiterobject.h"

class Object;
class ComCallWrapper;
class Thread;

#define GC_PRESSURE_PROCESS_LOCAL 3456
#define GC_PRESSURE_MACHINE_LOCAL 4004
#define GC_PRESSURE_REMOTE 4824

#ifdef _WIN64
#define GC_PRESSURE_WINRT_BASE    1000
#define GC_PRESSURE_WINRT_LOW     12000
#define GC_PRESSURE_WINRT_MEDIUM  120000
#define GC_PRESSURE_WINRT_HIGH    1200000
#else // _WIN64
#define GC_PRESSURE_WINRT_BASE    750
#define GC_PRESSURE_WINRT_LOW     8000
#define GC_PRESSURE_WINRT_MEDIUM  80000
#define GC_PRESSURE_WINRT_HIGH    800000
#endif // _WIN64

extern bool g_fShutDownCOM;

enum {INTERFACE_ENTRY_CACHE_SIZE = 8};

struct RCWAuxiliaryData;
typedef DPTR(RCWAuxiliaryData) PTR_RCWAuxiliaryData;

#define VARIANCE_STUB_TARGET_USE_STRING        ((OBJECTHANDLE)(INT_PTR)0x1)
#define VARIANCE_STUB_TARGET_USE_T             ((OBJECTHANDLE)(INT_PTR)0x2)
#define VARIANCE_STUB_TARGET_IS_HANDLE(handle) (((INT_PTR)(handle) & ~0x3) != 0)

// Additional RCW data used for generic interop and auxiliary interface pointer cache.
// This structure is lazily allocated and associated with the RCW via the m_pAuxiliaryData
// field. It's needed only if the RCW supports IEnumerable<T> or another interface with
// variance, or if a QI result could not be saved in the inline interface pointer cache
// (code:RCW.m_aInterfaceEntries).
struct RCWAuxiliaryData
{
    RCWAuxiliaryData()
    {
        WRAPPER_NO_CONTRACT;
        
        m_pGetEnumeratorMethod = NULL;
        m_prVariantInterfaces = NULL;
        m_VarianceCacheCrst.Init(CrstLeafLock);
        m_pInterfaceCache = NULL;
        m_ohObjectVariantCallTarget_IEnumerable = NULL;
        m_ohObjectVariantCallTarget_IReadOnlyList = NULL;
        m_AuxFlags.m_dwFlags = 0;
    }

    ~RCWAuxiliaryData();

    struct InterfaceEntryEx;
    typedef DPTR(InterfaceEntryEx) PTR_InterfaceEntryEx;

    // Augments code:InterfaceEntry with a next pointer and context entry field.
    struct InterfaceEntryEx
    {
        PTR_InterfaceEntryEx m_pNext;

        InterfaceEntry       m_BaseEntry;
        PTR_CtxEntry         m_pCtxEntry;

        ~InterfaceEntryEx()
        {
            WRAPPER_NO_CONTRACT;
            if (m_pCtxEntry != NULL)
            {
                m_pCtxEntry->Release();
            }
        }
    };

    // Iterator for cached interface entries.
    class InterfaceEntryIterator
    {
        PTR_InterfaceEntryEx m_pCurrent;
        bool m_fFirst;

    public:
        inline InterfaceEntryIterator(PTR_RCWAuxiliaryData pAuxiliaryData)
        {
            LIMITED_METHOD_CONTRACT;
            m_pCurrent = (pAuxiliaryData == NULL ? NULL : pAuxiliaryData->m_pInterfaceCache);
            m_fFirst = true;
        }

        // Move to the next item returning TRUE if an item exists or FALSE if we've run off the end
        inline bool Next()
        {
            LIMITED_METHOD_CONTRACT;
            if (m_fFirst)
            {
                m_fFirst = false;
            }
            else
            {
                m_pCurrent = m_pCurrent->m_pNext;
            }
            return (m_pCurrent != NULL);
        }

        inline InterfaceEntry *GetEntry()
        {
            LIMITED_METHOD_CONTRACT;
            return &m_pCurrent->m_BaseEntry;
        }

        inline LPVOID GetCtxCookie()
        {
            LIMITED_METHOD_CONTRACT;
            return (m_pCurrent->m_pCtxEntry == NULL ? NULL : m_pCurrent->m_pCtxEntry->GetCtxCookie());
        }

        inline CtxEntry *GetCtxEntry()
        {
            LIMITED_METHOD_CONTRACT;

            m_pCurrent->m_pCtxEntry->AddRef();
            return m_pCurrent->m_pCtxEntry;
        }

        inline CtxEntry *GetCtxEntryNoAddRef()
        {
            LIMITED_METHOD_CONTRACT;
            return m_pCurrent->m_pCtxEntry;
        }

        inline void ResetCtxEntry()
        {
            LIMITED_METHOD_CONTRACT;
            m_pCurrent->m_pCtxEntry = NULL;
        }

#ifndef DACCESS_COMPILE
        inline void SetCtxCookie(LPVOID pCtxCookie)
        {
            CONTRACTL
            {
                THROWS;
                GC_TRIGGERS;
                MODE_ANY;
            }
            CONTRACTL_END;

            CtxEntry *pCtxEntry = NULL;
            if (pCtxCookie != NULL)
            {
                pCtxEntry = CtxEntryCache::GetCtxEntryCache()->FindCtxEntry(pCtxCookie, GetThread());
            }
            m_pCurrent->m_pCtxEntry = pCtxEntry;
        }
#endif // !DACCESS_COMPILE
    };

    void CacheVariantInterface(MethodTable *pMT);

    void CacheInterfacePointer(MethodTable *pMT, IUnknown *pUnk, LPVOID pCtxCookie);
    IUnknown *FindInterfacePointer(MethodTable *pMT, LPVOID pCtxCookie);
    
    inline InterfaceEntryIterator IterateInterfacePointers()
    {
        LIMITED_METHOD_CONTRACT;
        return InterfaceEntryIterator(dac_cast<PTR_RCWAuxiliaryData>(this));
    }

    // GetEnumerator method of the first IEnumerable<T> interface we successfully QI'ed for
    PTR_MethodDesc       m_pGetEnumeratorMethod;

    // Interfaces with variance that we successfully QI'ed for
    ArrayList           *m_prVariantInterfaces;

    // Lock to protect concurrent access to m_prVariantInterfaces
    CrstExplicitInit     m_VarianceCacheCrst;

    // Linked list of cached interface pointers
    PTR_InterfaceEntryEx m_pInterfaceCache;

    // Cached object handles wrapping delegate objects that point to the right GetEnumerator/Indexer_Get
    // stubs that should be used when calling these methods via IEnumerable<object>/IReadOnlyList<object>.
    // Can also contain the special VARIANCE_STUB_TARGET_USE_STRING and VARIANCE_STUB_TARGET_USE_T values.
    OBJECTHANDLE         m_ohObjectVariantCallTarget_IEnumerable;    // GetEnumerator
    OBJECTHANDLE         m_ohObjectVariantCallTarget_IReadOnlyList;  // Indexer_Get

    // Rarely used RCW flags (keep the commonly used ones in code:RCW::RCWFlags)
    union RCWAuxFlags
    {
        DWORD       m_dwFlags;

        struct
        {
            // InterfaceVarianceBehavior for rarely used instantiations that could be supported via string:
            DWORD   m_InterfaceVarianceBehavior_OfIEnumerable:4;
            DWORD   m_InterfaceVarianceBehavior_OfIEnumerableOfChar:4;
        };
    }
    m_AuxFlags;
};

typedef DPTR(RCW) PTR_RCW;

//----------------------------------------------------------------------------
// RCW, internal class
// caches the IPs for a single com object, this wrapper is
// not in the GC heap, this allows us to grab a pointer to this block
// and play with-it without worrying about GC
struct RCW 
{
    enum CreationFlags
    {
        CF_None                 = 0x00,
        CF_SupportsIInspectable = 0x01, // the underlying object supports IInspectable
        CF_QueryForIdentity     = 0x02, // Need to QI for the real identity IUnknown during creating RCW
        CF_IsWeakReference      = 0x04, // mark the RCW as "weak"
        CF_NeedUniqueObject     = 0x08, // always create a new RCW/object even if we have one cached already
        CF_DontResolveClass     = 0x10, // don't attempt to create a strongly typed RCW
        CF_DetectDCOMProxy      = 0x20, // attempt to determine if the RCW is for a DCOM proxy
    };

    static CreationFlags CreationFlagsFromObjForComIPFlags(ObjFromComIP::flags flags);

    // List of RCW instances that have been freed since the last RCW cleanup.
    static SLIST_HEADER s_RCWStandbyList;

    // Simple read-only iterator for all cached interface pointers.
    class CachedInterfaceEntryIterator
    {
        PTR_RCW   m_pRCW;
        int       m_InlineCacheIndex;
        RCWAuxiliaryData::InterfaceEntryIterator m_AuxIterator;

    public:
        inline CachedInterfaceEntryIterator(PTR_RCW pRCW)
            : m_AuxIterator(pRCW->m_pAuxiliaryData)
        {
            LIMITED_METHOD_CONTRACT;
            m_pRCW = pRCW;
            m_InlineCacheIndex = -1;
        }

        // Move to the next item returning TRUE if an item exists or FALSE if we've run off the end
        inline bool Next()
        {
            LIMITED_METHOD_CONTRACT;

            if (m_InlineCacheIndex < INTERFACE_ENTRY_CACHE_SIZE)
            {
                // stop incrementing m_InlineCacheIndex once we reach INTERFACE_ENTRY_CACHE_SIZE
                if (++m_InlineCacheIndex < INTERFACE_ENTRY_CACHE_SIZE)
                {
                    return TRUE;
                }
            }
            return m_AuxIterator.Next();
        }

        inline InterfaceEntry *GetEntry()
        {
            LIMITED_METHOD_CONTRACT;
            
            _ASSERTE_MSG(m_InlineCacheIndex >= 0, "Iterator starts before the first element, you need to call Next");
            if (m_InlineCacheIndex < INTERFACE_ENTRY_CACHE_SIZE)
            {
                return &m_pRCW->m_aInterfaceEntries[m_InlineCacheIndex];
            }
            return m_AuxIterator.GetEntry();
        }

        inline LPVOID GetCtxCookie()
        {
            LIMITED_METHOD_CONTRACT;
            
            _ASSERTE_MSG(m_InlineCacheIndex >= 0, "Iterator starts before the first element, you need to call Next");
            if (m_InlineCacheIndex < INTERFACE_ENTRY_CACHE_SIZE)
            {
                return m_pRCW->GetWrapperCtxCookie();
            }
            return m_AuxIterator.GetCtxCookie();
        }
    };

    // constructor
    RCW()
    {
        WRAPPER_NO_CONTRACT;
        ZeroMemory(this, sizeof(*this));
    }

    // Deletes all items in code:s_RCWStandbyList.
    static void FlushStandbyList();

    // Create a new wrapper for given IUnk, IDispatch
    static RCW* CreateRCW(IUnknown *pUnk, DWORD dwSyncBlockIndex, DWORD flags, MethodTable *pClassMT);

    //-------------------------------------------------
    // initialize IUnknown and Identity, and associate with the managed object.
    void Initialize(IUnknown* pUnk, DWORD dwSyncBlockIndex, MethodTable *pClassMT);

    enum MarshalingType
     {
         MarshalingType_Unknown = 0,      /* The MarshalingType has not been set*/
         MarshalingType_Inhibit = 1,      /* This value is same as the MarshalingType.Inhibit*/
         MarshalingType_FreeThreaded = 2, /* This value is same as the MarshalingType.FreeThreaded*/
         MarshalingType_Standard = 3      /* This value is same as the MarshalingType.Standard*/
     };
    
    //-------------------------------------------------
    // Get the MarshalingType of the associated managed object.
    MarshalingType GetMarshalingType(IUnknown* pUnk, MethodTable *pClassMT);


    //-----------------------------------------------
    // Free GC handle and remove SyncBlock entry
    void DecoupleFromObject();

    //---------------------------------------------------
    // Cleanup free all interface pointers
    void Cleanup();

    //-----------------------------------------------------
    // called during GC to do minor cleanup and schedule the ips to be
    // released
    void MinorCleanup();

    //-----------------------------------------------------
    // The amount of GC pressure we apply has one of a few possible values.
    // We save space in the RCW structure by tracking this instead of the
    // actual value.
    enum GCPressureSize
    {
        GCPressureSize_None         = 0,
        GCPressureSize_ProcessLocal = 1,
        GCPressureSize_MachineLocal = 2,
        GCPressureSize_Remote       = 3,
        GCPressureSize_WinRT_Base   = 4,
        GCPressureSize_WinRT_Low    = 5,
        GCPressureSize_WinRT_Medium = 6,
        GCPressureSize_WinRT_High   = 7,
        GCPressureSize_COUNT        = 8
    };

    //---------------------------------------------------
    // Add memory pressure to the GC representing the native cost
    void AddMemoryPressure(GCPressureSize pressureSize);

    //---------------------------------------------------
    // Remove memory pressure from the GC representing the native cost
    void RemoveMemoryPressure();

    //-----------------------------------------------------
    // AddRef
    LONG AddRef(RCWCache* pCache);

    //-----------------------------------------------------
    // Release
    static INT32 ExternalRelease(OBJECTREF* objPROTECTED);
    static void FinalExternalRelease(OBJECTREF* objPROTECTED);

    // Create a new wrapper for a different method table that represents the same
    // COM object as the original wrapper.
    void CreateDuplicateWrapper(MethodTable *pNewMT, RCWHolder* pNewRCW);

    AppDomain* GetDomain();

#ifndef DACCESS_COMPILE

    //-------------------------------------------------
    // return exposed ComObject
    COMOBJECTREF GetExposedObject()
    {
        CONTRACT(COMOBJECTREF)
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_COOPERATIVE;
            PRECONDITION(m_SyncBlockIndex != 0);
            POSTCONDITION(RETVAL != NULL);
        }
        CONTRACT_END;
        
        RETURN (COMOBJECTREF) ObjectToOBJECTREF(g_pSyncTable[m_SyncBlockIndex].m_Object);
    }

    //-------------------------------------------------
    // returns the sync block for the RCW
    SyncBlock *GetSyncBlock()
    {
        CONTRACT(SyncBlock*)
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_COOPERATIVE;
            PRECONDITION(m_SyncBlockIndex != 0);
            POSTCONDITION(CheckPointer(RETVAL));
        }
        CONTRACT_END;
        
        RETURN g_pSyncTable[m_SyncBlockIndex].m_SyncBlock;
    }

    //--------------------------------------------------------------------------
    // out of line call, takes a lock, does a QI if the interface was not found in local cache
    IUnknown* GetComIPFromRCW(MethodTable* pMT);
       
    //-----------------------------------------------------------------
    // out of line call
    IUnknown* GetComIPFromRCW(REFIID iid);

#endif // #ifndef DACCESS_COMPILE

    enum InterfaceRedirectionKind
    {
        InterfaceRedirection_None,
        InterfaceRedirection_IEnumerable,                 // IEnumerable`1 - based interface
        InterfaceRedirection_IEnumerable_RetryOnFailure,  // IEnumerable`1 - based interface, retry on QI failure
        InterfaceRedirection_UnresolvedIEnumerable,       // unknown IEnumerable`1 instantiation
        InterfaceRedirection_Other,                       // other interface
        InterfaceRedirection_Other_RetryOnFailure,        // non-generic redirected interface
    };

    // Returns a redirected collection interface corresponding to a given ICollection<T>, IReadOnlyCollection<T>, or NULL.
    static MethodTable *ResolveICollectionInterface(MethodTable *pItfMT, BOOL fPreferIDictionary, BOOL *pfChosenIDictionary);

    // Returns an interface with variance corresponding to pMT or NULL if pMT does not support variance.
    static MethodTable *GetVariantMethodTable(MethodTable *pMT);
    static MethodTable *ComputeVariantMethodTable(MethodTable *pMT);

    // Determines the interface that should be QI'ed for when the RCW is cast to pItfMT.
    // Returns the kind of interface redirection that has been performed.
    InterfaceRedirectionKind GetInterfaceForQI(MethodTable *pItfMT, MethodTable **pNewItfMT);
    static InterfaceRedirectionKind GetInterfacesForQI(MethodTable *pItfMT, MethodTable **ppNewItfMT1, MethodTable **ppNewItfMT2);
    static InterfaceRedirectionKind ComputeInterfacesForQI(MethodTable *pItfMT, MethodTable **ppNewItfMT1, MethodTable **ppNewItfMT2);

    // Performs QI for the given interface, optionally instantiating it with the given generic args.
    HRESULT CallQueryInterface(MethodTable *pMT, Instantiation inst, IID *piid, IUnknown **ppUnk);

    // Performs QI for interfaces that are castable to pMT using co-/contra-variance.
    HRESULT CallQueryInterfaceUsingVariance(MethodTable *pMT, IUnknown **ppUnk);
    
    // Returns the GetEnumerator method of the first IEnumerable<T> this RCW was successfully
    // cast to, or NULL if no such cast has ever succeeded.
    MethodDesc *GetGetEnumeratorMethod();

    // Sets the first "known" GetEnumerator method if not set already.
    void SetGetEnumeratorMethod(MethodTable *pMT);

    // Retrieve cached GetEnumerator method or compute the right one for a specific type
    static MethodDesc *GetOrComputeGetEnumeratorMethodForType(MethodTable *pMT);

    // Compute the first GetEnumerator for a specific type
    static MethodDesc *ComputeGetEnumeratorMethodForType(MethodTable *pMT);

    // Get the GetEnumerator method for IEnumerable<T> or IIterable<T>
    static MethodDesc *ComputeGetEnumeratorMethodForTypeInternal(MethodTable *pMT);
    
    // Notifies the RCW of an interface that is known to be supported by the COM object.
    void SetSupportedInterface(MethodTable *pItfMT, Instantiation originalInst);

    //-----------------------------------------------------------------
    // Retrieve correct COM IP for the current apartment.
    // use the cache /update the cache
    IUnknown* GetComIPForMethodTableFromCache(MethodTable * pMT);

    // helpers to get to IUnknown, IDispatch, and IInspectable interfaces
    // Returns an addref'd pointer - caller must Release
    IUnknown*  GetWellKnownInterface(REFIID riid);

    IUnknown*  GetIUnknown();
    IUnknown*  GetIUnknown_NoAddRef();
    IDispatch* GetIDispatch();
    IInspectable* GetIInspectable();

    ULONG GetRefCount()
    {
        return m_cbRefCount;
    }

    IJupiterObject *GetJupiterObjectNoCheck()
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(IsJupiterObject());
        
        // We saved IJupiterObject * on the first slot
        _ASSERTE((IUnknown *)m_aInterfaceEntries[0].m_pUnknown != NULL);
        _ASSERTE((MethodTable *)m_aInterfaceEntries[0].m_pMT == NULL);
        
        return (IJupiterObject *)m_aInterfaceEntries[0].m_pUnknown.Load();    
    }
    
    IJupiterObject *GetJupiterObject()
    {
        LIMITED_METHOD_CONTRACT;
    
        if (IsJupiterObject())
        {
            return GetJupiterObjectNoCheck();
        }

        return NULL;            
    }

    void GetCachedInterfaceTypes(BOOL bIInspectableOnly, 
                        SArray<PTR_MethodTable> * rgItfTables)
    {
        LIMITED_METHOD_DAC_CONTRACT;

        CachedInterfaceEntryIterator it = IterateCachedInterfacePointers();
        while (it.Next())
        {
            PTR_MethodTable pMT = dac_cast<PTR_MethodTable>((TADDR)(it.GetEntry()->m_pMT.Load()));
            if (pMT != NULL && 
                (!bIInspectableOnly || pMT->IsProjectedFromWinRT() || pMT->SupportsGenericInterop(TypeHandle::Interop_NativeToManaged)))
            {
                // Don't return mscorlib-internal declarations of WinRT types.
                if (!(pMT->GetModule()->IsSystem() && pMT->IsProjectedFromWinRT()))
                {
                    rgItfTables->Append(pMT);
                }
            }
        }
    }

    void GetCachedInterfacePointers(BOOL bIInspectableOnly,
                        SArray<TADDR> * rgItfPtrs)
    {
        LIMITED_METHOD_DAC_CONTRACT;

        CachedInterfaceEntryIterator it = IterateCachedInterfacePointers();
        while (it.Next())
        {
            PTR_MethodTable pMT = dac_cast<PTR_MethodTable>((TADDR)(it.GetEntry()->m_pMT.Load()));
            if (pMT != NULL && 
                (!bIInspectableOnly || pMT->IsProjectedFromWinRT() || pMT->SupportsGenericInterop(TypeHandle::Interop_NativeToManaged)))
            {
                TADDR taUnk = (TADDR)(it.GetEntry()->m_pUnknown.Load());
                if (taUnk != NULL)
                {
                    rgItfPtrs->Append(taUnk);
                }
            }
        }
    }

    // Save IJupiterObject * on the first slot
    // Only call this in Initialize code
    void SetJupiterObject(IJupiterObject *pJupiterObject)
    {

        LIMITED_METHOD_CONTRACT;
    
        m_Flags.m_fIsJupiterObject = 1;
        
        //
        // Save pJupiterObject* on the first SLOT
        // Only AddRef if not aggregated
        //
        _ASSERTE(m_aInterfaceEntries[0].IsFree());
        
        m_aInterfaceEntries[0].Init(NULL, pJupiterObject);
    }

    LPVOID     GetVTablePtr() { LIMITED_METHOD_CONTRACT; return m_vtablePtr; }

    // Remoting aware QI that will attempt to re-unmarshal on object disconnect.
    HRESULT SafeQueryInterfaceRemoteAware(REFIID iid, IUnknown** pResUnk);

    BOOL IsValid()
    {
        LIMITED_METHOD_CONTRACT;
        
        return m_SyncBlockIndex != 0;
    }

    BOOL SupportsIProvideClassInfo();

    VOID MarkURTAggregated();

    VOID MarkURTContained()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(m_Flags.m_fURTAggregated == 0);
        }
        CONTRACTL_END;
        
        m_Flags.m_fURTContained = 1;
    }


    BOOL IsURTAggregated()
    {
        LIMITED_METHOD_CONTRACT;    
        return m_Flags.m_fURTAggregated == 1;
    }
        
    BOOL IsURTContained()
    {
        LIMITED_METHOD_CONTRACT;
        return m_Flags.m_fURTContained == 1;
    }

    BOOL SupportsIInspectable()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_Flags.m_fSupportsIInspectable == 1;
    }

    //
    // This COM object aggregates FTM?
    //
    bool IsFreeThreaded()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return (m_Flags.m_MarshalingType == MarshalingType_FreeThreaded) ;
    }

    //
    // Is this COM object a DCOM Proxy? (For WinRT the RCW must have been created with CF_DetectDCOMProxy)
    // 
    bool IsDCOMProxy()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_Flags.m_fIsDCOMProxy == 1;
    }

    //
    // This COM object implements INoMarshal?
    //
    bool IsMarshalingInhibited()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return (m_Flags.m_MarshalingType == MarshalingType_Inhibit) ;
    }

    BOOL IsJupiterObject()
    {
        LIMITED_METHOD_CONTRACT;
        
        return m_Flags.m_fIsJupiterObject == 1;
    }

    // Returns TRUE if this RCW has been detached. Detached RCWs are fully functional but have been found
    // dead during GC, before finalizable/f-reachable objects were promoted. If we ever find such an RCW
    // in the RCW cache during marshaling (i.e. an interface pointer with the same identity enters managed
    // code), we re-insert it as "unique", and create a new RCW. This is to prevent unexpected resurrection
    // of objects that may already be finalized.
    BOOL IsDetached()
    {
        LIMITED_METHOD_CONTRACT;
        
        return m_Flags.m_Detached == 1;
    }

    BOOL MatchesCleanupBucket(RCW *pOtherRCW)
    {
        LIMITED_METHOD_CONTRACT;
        
        return (IsFreeThreaded() == pOtherRCW->IsFreeThreaded() &&
                m_Flags.m_fAllowEagerSTACleanup == pOtherRCW->m_Flags.m_fAllowEagerSTACleanup &&
                GetSTAThread() == pOtherRCW->GetSTAThread() &&
                GetWrapperCtxCookie() == pOtherRCW->GetWrapperCtxCookie()
                );
    }

    // Note that this is not a simple field getter
    BOOL AllowEagerSTACleanup();

    // GetWrapper context cookie
    LPVOID GetWrapperCtxCookie()
    {
        CONTRACT (LPVOID)
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            POSTCONDITION(CheckPointer(RETVAL));
        }
        CONTRACT_END;
        
        RETURN m_UnkEntry.m_pCtxCookie;
    }

    inline Thread *GetSTAThread()
    {
        CONTRACT (Thread *)
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        }
        CONTRACT_END;
        
        CtxEntry *pCtxEntry = GetWrapperCtxEntryNoRef();
        if (pCtxEntry)
            RETURN pCtxEntry->GetSTAThread();
        RETURN NULL;       
    }

    // Function to enter the context. The specified callback function will
    // be called from within the context.
    HRESULT EnterContext(PFNCTXCALLBACK pCallbackFunc, LPVOID pData);    
    
    inline CachedInterfaceEntryIterator IterateCachedInterfacePointers()
    {
        LIMITED_METHOD_CONTRACT;
        return CachedInterfaceEntryIterator(dac_cast<PTR_RCW>(this));
    }

    //---------------------------------------------------------------------
    // Returns RCWAuxiliaryData associated with this RCW. Allocates the
    // structure if it does not exist already.
    PTR_RCWAuxiliaryData GetOrCreateAuxiliaryData();

    //---------------------------------------------------------------------
    // Returns true iff pItfMT is a "standard managed" interface, such as
    // IEnumerator, and the RCW supports the interface through classic COM
    // interop mechanisms.
    bool SupportsMngStdInterface(MethodTable *pItfMT);

    //---------------------------------------------------------------------
    // Determines whether a call through the given interface should use new
    // WinRT interop (as opposed to classic COM). pItfMT should be a non-generic
    // redirected interface such as IEnumerable whose interop behavior is
    // ambiguous. This is a NoGC variant, if it returns TypeHandle::MaybeCast,
    // SupportsWinRTInteropInterface should be called.
    TypeHandle::CastResult SupportsWinRTInteropInterfaceNoGC(MethodTable *pItfMT);

    //---------------------------------------------------------------------
    // This is a GC-triggering variant of code:SupportsWinRTInteropInterfaceNoGC.
    bool SupportsWinRTInteropInterface(MethodTable *pItfMT);

    //---------------------------------------------------------------------
    // True if the object supports legacy (not WinRT) IEnumerable marshaling.
    bool SupportsLegacyEnumerableInterface()
    {
        LIMITED_METHOD_CONTRACT;
        
        _ASSERTE(SupportsWinRTInteropInterfaceNoGC(MscorlibBinder::GetExistingClass(CLASS__IENUMERABLE)) == TypeHandle::CannotCast);
        return m_Flags.m_RedirectionBehavior_IEnumerable_LegacySupported;
    }

    enum RedirectionBehavior
    {
        RedirectionBehaviorComputed = 1, // the second bit is valid
        RedirectionBehaviorEnabled  = 2  // if RedirectionBehaviorComputed is set, true means the interface is redirected on this RCW
    };

    enum InterfaceVarianceBehavior
    {
        IEnumerableSupported                            = 1,  // IEnumerable<T> is supported on this RCW
        IEnumerableSupportedViaStringInstantiation      = 2,  // the object failed QI for IEnumerable<T> but succeeded QI for IEnumerable<string>

        IReadOnlyListSupported                          = 4,  // IReadOnlyList<T> is supported on this RCW
        IReadOnlyListSupportedViaStringInstantiation    = 8,  // the object failed QI for IReadOnlyList<T> but succeeded QI for IReadOnlyList<string>
    };

    // Returns a delegate object that points to the right GetEnumerator/Indexer_Get stub that should be used when calling these methods via
    // IEnumerable<object>/IReadOnlyList<object> or NULL in which case the BOOL argument are relevant:
    // *pfUseString == true means that the caller should use IEnumerable<string>/IReadOnlyList<string>
    // *pfUseT == true means that the caller should handle the call as normal, i.e. invoking the stub instantiated over T.
    OBJECTREF GetTargetForAmbiguousVariantCall(BOOL fIsEnumerable, WinRTInterfaceRedirector::WinRTLegalStructureBaseType baseType, BOOL *pfUseString, BOOL *pfUseT)
    {
        LIMITED_METHOD_CONTRACT;
         
        if (m_pAuxiliaryData != NULL)
        {
            if (baseType == WinRTInterfaceRedirector::BaseType_Object)
            {
                if (fIsEnumerable)
                {
                    if (VARIANCE_STUB_TARGET_IS_HANDLE(m_pAuxiliaryData->m_ohObjectVariantCallTarget_IEnumerable))
                        return ObjectFromHandle(m_pAuxiliaryData->m_ohObjectVariantCallTarget_IEnumerable);

                    if (m_pAuxiliaryData->m_ohObjectVariantCallTarget_IEnumerable == VARIANCE_STUB_TARGET_USE_STRING)
                        *pfUseString = TRUE;
                    else if (m_pAuxiliaryData->m_ohObjectVariantCallTarget_IEnumerable == VARIANCE_STUB_TARGET_USE_T)
                        *pfUseT = TRUE;
                }
                else
                {
                    if (VARIANCE_STUB_TARGET_IS_HANDLE(m_pAuxiliaryData->m_ohObjectVariantCallTarget_IReadOnlyList))
                        return ObjectFromHandle(m_pAuxiliaryData->m_ohObjectVariantCallTarget_IReadOnlyList);

                    if (m_pAuxiliaryData->m_ohObjectVariantCallTarget_IReadOnlyList == VARIANCE_STUB_TARGET_USE_STRING)
                        *pfUseString = TRUE;
                    else if (m_pAuxiliaryData->m_ohObjectVariantCallTarget_IReadOnlyList == VARIANCE_STUB_TARGET_USE_T)
                        *pfUseT = TRUE;
                }
            }
            else
            {
                InterfaceVarianceBehavior varianceBehavior = (baseType == WinRTInterfaceRedirector::BaseType_IEnumerable) ?
                    (InterfaceVarianceBehavior)m_pAuxiliaryData->m_AuxFlags.m_InterfaceVarianceBehavior_OfIEnumerable :
                    (InterfaceVarianceBehavior)m_pAuxiliaryData->m_AuxFlags.m_InterfaceVarianceBehavior_OfIEnumerableOfChar;

                if (fIsEnumerable)
                {
                    if ((varianceBehavior & IEnumerableSupported) != 0)
                    {
                        if ((varianceBehavior & IEnumerableSupportedViaStringInstantiation) != 0)
                            *pfUseString = TRUE;
                        else
                            *pfUseT = TRUE;
                    }
                }
                else
                {
                    if ((varianceBehavior & IReadOnlyListSupported) != 0)
                    {
                        if ((varianceBehavior & IReadOnlyListSupportedViaStringInstantiation) != 0)
                            *pfUseString = TRUE;
                        else
                            *pfUseT = TRUE;
                    }
                }
            }
        }
        return NULL;
    }

#ifdef _DEBUG
    // Does not throw if m_UnkEntry.m_pUnknown is no longer valid, debug only.
    IUnknown *GetRawIUnknown_NoAddRef_NoThrow()
    {
        LIMITED_METHOD_CONTRACT;
        return m_UnkEntry.GetRawIUnknown_NoAddRef_NoThrow();
    }
#endif // _DEBUG

    IUnknown *GetRawIUnknown_NoAddRef()
    {
        WRAPPER_NO_CONTRACT;
        return m_UnkEntry.GetRawIUnknown_NoAddRef();
    }

    bool IsDisconnected()
    {
        LIMITED_METHOD_CONTRACT;
        return m_UnkEntry.IsDisconnected();
    }
    
    void IncrementUseCount()
    {
        LIMITED_METHOD_CONTRACT;
        InterlockedIncrement(&m_cbUseCount);
    }

    void DecrementUseCount()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;
    
        if (InterlockedDecrement(&m_cbUseCount) == 0)
        {
            // this was the final decrement, go ahead and delete/recycle the RCW
            {
                GCX_PREEMP();
                m_UnkEntry.Free();
            }

            if (g_fEEShutDown)
            {
                delete this;
            }
            else
            {
                InterlockedPushEntrySList(&RCW::s_RCWStandbyList, (PSLIST_ENTRY)this);
            }
        }
    }

private:
    //---------------------------------------------------------------------
    // Computes the result of code:SupportsWinRTInteropInterface.
    RedirectionBehavior ComputeRedirectionBehavior(MethodTable *pItfMT, bool *pfLegacySupported);

    //---------------------------------------------------------------------
    // Callback called to release the interfaces in the auxiliary cache.
    static HRESULT __stdcall ReleaseAuxInterfacesCallBack(LPVOID pData);

    //---------------------------------------------------------------------
    // Callback called to release the IUnkEntry and the InterfaceEntries,
    static HRESULT __stdcall ReleaseAllInterfacesCallBack(LPVOID pData);

    //---------------------------------------------------------------------
    // Helper function called from ReleaseAllInterfaces_CallBack do do the 
    // actual releases.
    void ReleaseAllInterfaces();
    
public:
    // Points to the next RCW bucket if this RCW is part of a code:RCWCleanupList
    PTR_RCW             m_pNextCleanupBucket;

    // interface entries
    InterfaceEntry      m_aInterfaceEntries[INTERFACE_ENTRY_CACHE_SIZE];

    // Identity
    LPVOID              m_pIdentity;

    // Sync block index for the exposed managed object
    DWORD               m_SyncBlockIndex;
    
    //ref-count
    ULONG               m_cbRefCount;

    // Wrapper Cache
    RCWCache*           m_pRCWCache;

    // thread in which the wrapper has been created
    // if this thread is an STA thread, then when the STA dies
    // we need to cleanup this wrapper
    Thread*             m_pCreatorThread;

    union RCWFlags
    {
        DWORD       m_dwFlags;

        struct
        {
            static_assert((1 << 4) > INTERFACE_ENTRY_CACHE_SIZE, "m_iEntryToRelease needs a bigger data type");
            DWORD       m_iEntryToRelease:4;

            DWORD       m_fURTAggregated:1;        // this RCW represents a COM object aggregated by a managed object
            DWORD       m_fURTContained:1;         // this RCW represents a COM object contained by a managed object
            DWORD       m_fAllowEagerSTACleanup:1; // this RCW can be cleaned up eagerly (as opposed to via CleanupUnusedObjectsInCurrentContext)
            DWORD       m_fSupportsIInspectable:1; // the underlying COM object is known to support IInspectable
            DWORD       m_fIsJupiterObject:1;      // this RCW represents a COM object from Jupiter

            static_assert((1 << 3) >= GCPressureSize_COUNT, "m_GCPressure needs a bigger data type");
            DWORD       m_GCPressure:3;            // index into s_rGCPressureTable

            // RedirectionBehavior of non-generic redirected interfaces:
            DWORD       m_RedirectionBehavior_IEnumerable:2;
            DWORD       m_RedirectionBehavior_IEnumerable_LegacySupported:1; // one extra bit for IEnumerable

            DWORD       m_RedirectionBehavior_ICollection:2;
            DWORD       m_RedirectionBehavior_IList:2;
            DWORD       m_RedirectionBehavior_INotifyCollectionChanged:2;
            DWORD       m_RedirectionBehavior_INotifyPropertyChanged:2;
            DWORD       m_RedirectionBehavior_ICommand:2;
            DWORD       m_RedirectionBehavior_IDisposable:2;

            // Reserve 2 bits for marshaling behavior
            DWORD       m_MarshalingType:2;        // MarshalingBehavior of the COM object.

            DWORD       m_Detached:1;              // set if the RCW was found dead during GC

            DWORD       m_fIsDCOMProxy:1;          // Is the object a proxy to a remote process
        };
    }
    m_Flags;

    static_assert(sizeof(RCWFlags) == 4, "Flags don't fit in 4 bytes, there's too many of them");

    // GC pressure sizes in bytes
    static const int s_rGCPressureTable[GCPressureSize_COUNT];

    // Tracks concurrent access to this RCW to prevent using RCW instances that have already been released
    LONG                m_cbUseCount;

    // additional RCW data used for generic interop and advanced interface pointer caching (NULL unless needed)
    PTR_RCWAuxiliaryData m_pAuxiliaryData;

    PTR_RCW             m_pNextRCW;

    // This field is useful for debugging purposes, please do not remove. The typical scenario is a crash in
    // SafeRelease because the COM object disappeared. Knowing the vtable usually helps find the culprit.
    LPVOID              m_vtablePtr;

private :   
    // cookies for tracking IUnknown on the correct thread  
    IUnkEntry           m_UnkEntry;    

    // IUnkEntry needs to access m_UnkEntry field
    friend IUnkEntry;
    
private :
    static RCW* CreateRCWInternal(IUnknown *pUnk, DWORD dwSyncBlockIndex, DWORD flags, MethodTable *pClassMT);
    
    // Returns an addref'ed context entry
    CtxEntry* GetWrapperCtxEntry()
    {
        CONTRACT (CtxEntry*)
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(!IsFreeThreaded());         // Must not be free-threaded, otherwise CtxEntry = NULL
            POSTCONDITION(CheckPointer(RETVAL));
        }
        CONTRACT_END;
        
        CtxEntry *pCtxEntry = m_UnkEntry.GetCtxEntry();
        pCtxEntry->AddRef();
        RETURN pCtxEntry;
    }

    // Returns an non-addref'ed context entry
    CtxEntry *GetWrapperCtxEntryNoRef()
    {
        CONTRACT (CtxEntry *)
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        }
        CONTRACT_END;
        
        CtxEntry *pCtxEntry = m_UnkEntry.GetCtxEntry();
        RETURN pCtxEntry;
    }    
};

inline RCW::CreationFlags operator|(RCW::CreationFlags lhs, RCW::CreationFlags rhs)
{
    LIMITED_METHOD_CONTRACT;
    return static_cast<RCW::CreationFlags>(static_cast<DWORD>(lhs) | static_cast<DWORD>(rhs));
}
inline RCW::CreationFlags operator|=(RCW::CreationFlags & lhs, RCW::CreationFlags rhs)
{
    LIMITED_METHOD_CONTRACT;
    lhs = static_cast<RCW::CreationFlags>(static_cast<DWORD>(lhs) | static_cast<DWORD>(rhs));
    return lhs;
}

// In order to save vtablePtr in minidumps, we put it on the stack as a volatile local
// (so it's not optimized away by the compiler). Most places where we call out to COM
// can absorb the cost of one stack slot and one instruction to improve debuggability.
#define RCW_VTABLEPTR(pRCW) Volatile<LPVOID> __vtablePtr = (pRCW)->m_vtablePtr


// 01 REQUIRE_IINSPECTABLE            01 ITF_MARSHAL_INSP_ITF         01 CF_SupportsIInspectable 
// 02 SUPPRESS_ADDREF                 02 ITF_MARSHAL_SUPPRESS_ADDREF  02 CF_SuppressAddRef       
//                                                                    04 CF_IsWeakReference      
// 04 CLASS_IS_HINT                   04 ITF_MARSHAL_CLASS_IS_HINT
// 08 UNIQUE_OBJECT                                                   08 CF_NeedUniqueObject
//                                    08 ITF_MARSHAL_DISP_ITF
// 10 IGNORE_WINRT_AND_SKIP_UNBOXING                                  10 CF_DontResolveClass
//                                    10 ITF_MARSHAL_USE_BASIC_ITF
//                                    20 ITF_MARSHAL_WINRT_SCENARIO
inline RCW::CreationFlags RCW::CreationFlagsFromObjForComIPFlags(ObjFromComIP::flags dwFlags)
{
    LIMITED_METHOD_CONTRACT;

    static_assert_no_msg(CF_NeedUniqueObject     == ObjFromComIP::UNIQUE_OBJECT);
    static_assert_no_msg(CF_SupportsIInspectable == ObjFromComIP::REQUIRE_IINSPECTABLE);
    static_assert_no_msg(CF_DontResolveClass     == ObjFromComIP::IGNORE_WINRT_AND_SKIP_UNBOXING);

    RCW::CreationFlags result = (RCW::CreationFlags)(dwFlags & 
                                        (ObjFromComIP::UNIQUE_OBJECT
                                       | ObjFromComIP::IGNORE_WINRT_AND_SKIP_UNBOXING));
    if ((dwFlags & (ObjFromComIP::REQUIRE_IINSPECTABLE|ObjFromComIP::CLASS_IS_HINT))
        == (ObjFromComIP::REQUIRE_IINSPECTABLE|ObjFromComIP::CLASS_IS_HINT))
    {
        result |= CF_SupportsIInspectable;
    }
    return result;
}


// RCW data attached to MethodTable's that represent interesting types. Types without RCWPerTypeData
// (i.e. those with MethodTable::GetRCWPerTypeData() == NULL) are not interesting and are assumed to
// use NULL/default values for m_pVariantMT/m_pMTForQI1/m_pMTForQI2/m_pGetEnumeratorMethod.
struct RCWPerTypeData
{
    // Corresponding type with variance or NULL if the type does not exhibit variant behavior.
    MethodTable *m_pVariantMT;
    
    // Types that should be used for QI. m_pMTForQI1 is tried first; if it fails and m_pMTForQI2
    // is not NULL, QI for m_pMTForQI2 is performed. We need two types to supports ambiguous casts
    // to ICollection<KeyValuePair<K, V>>.
    MethodTable *m_pMTForQI1;
    MethodTable *m_pMTForQI2;

    // The corresponding IEnumerator<T>::GetEnumerator instantiation or NULL if the type does not
    // act like IEnumerable.
    MethodDesc *m_pGetEnumeratorMethod;

    // The kind of redirection performed by QI'ing for m_pMTForQI1.
    RCW::InterfaceRedirectionKind m_RedirectionKind;

    enum
    {
        VariantTypeInited       = 0x01,     // m_pVariantMT is set
        RedirectionInfoInited   = 0x02,     // m_pMTForQI1, m_pMTForQI2, and m_RedirectionKind are set
        GetEnumeratorInited     = 0x04,     // m_pGetEnumeratorMethod is set
        InterfaceFlagsInited    = 0x08,     // IsRedirectedInterface and IsICollectionGeneric are set

        IsRedirectedInterface   = 0x10,     // the type is a redirected interface
        IsICollectionGeneric    = 0x20,     // the type is ICollection`1
    };
    DWORD m_dwFlags;
};

#ifdef FEATURE_COMINTEROP_UNMANAGED_ACTIVATION

class ComClassFactory;
class WinRTClassFactory;
class WinRTManagedClassFactory;

class ClassFactoryBase
{
public:
    //-------------------------------------------------------------
    // Function to clean up
    virtual void Cleanup() = 0;

    ComClassFactory *AsComClassFactory()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(m_pClassMT == NULL || (!m_pClassMT->IsProjectedFromWinRT() && !m_pClassMT->IsExportedToWinRT()));
        return (ComClassFactory *)this;
    }

    WinRTClassFactory *AsWinRTClassFactory()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(m_pClassMT->IsProjectedFromWinRT() || m_pClassMT->IsExportedToWinRT());
        return (WinRTClassFactory *)this;
    }

    WinRTManagedClassFactory *AsWinRTManagedClassFactory()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(m_pClassMT->IsExportedToWinRT());
        return (WinRTManagedClassFactory *)this;
    }

protected:
    ClassFactoryBase(MethodTable *pClassMT = NULL)
        : m_pClassMT(pClassMT)
    {
        LIMITED_METHOD_CONTRACT;
    }

    MethodTable *m_pClassMT;
};

class ComClassFactoryCreator;
//-------------------------------------------------------------------------
// Class that wraps an IClassFactory
// This class allows a Reflection Class to wrap an IClassFactory
// Class::GetClassFromProgID("ProgID", "Server") can be used to get a Class
// object that wraps an IClassFactory.
// Calling class.CreateInstance() will create an instance of the COM object and
// wrap it with a RCW, the wrapper can be cast to the appropriate interface
// and used.
// 
class ComClassFactory : public ClassFactoryBase
{
protected:
    friend ComClassFactoryCreator;

    // We have two types of ComClassFactory:
    // 1. We build for reflection purpose.  We should not clean up.
    // 2. We build for IClassFactory.  We should clean up.
    //-----------------------------------------------------------
    // constructor
    ComClassFactory(REFCLSID rclsid) 
    {
        WRAPPER_NO_CONTRACT;
        
        m_pwszProgID = NULL;
        m_pwszServer = NULL;

        // Default to unmanaged version.
        m_bManagedVersion = FALSE;
        m_rclsid = rclsid;
    }
   
public :
    //---------------------------------------------------------
    // Mark this instance as Managed Version, so we will not do clean up.
    void SetManagedVersion()
    {
        LIMITED_METHOD_CONTRACT;
        m_bManagedVersion = TRUE;
    }
    
    //--------------------------------------------------------------
    // Init the ComClassFactory
    void Init(__in_opt WCHAR* pwszProgID, __in_opt WCHAR* pwszServer, MethodTable* pClassMT);

    //-------------------------------------------------------------
    // create instance, calls IClassFactory::CreateInstance
    OBJECTREF CreateInstance(MethodTable* pMTClass, BOOL ForManaged = FALSE);

    //-------------------------------------------------------------
    // Function to clean up
    void Cleanup();

protected :
#ifndef CROSSGEN_COMPILE
    //-------------------------------------------------------------
    // Create instance. Overridable from child classes    
    virtual IUnknown *CreateInstanceInternal(IUnknown *pOuter, BOOL *pfDidContainment);
#endif
    //-------------------------------------------------------------
    // Throw exception message
    void ThrowHRMsg(HRESULT hr, DWORD dwMsgResID);

    
private:
    //-------------------------------------------------------------
    // ComClassFactory::CreateAggregatedInstance(MethodTable* pMTClass)
    // create a COM+ instance that aggregates a COM instance
    OBJECTREF CreateAggregatedInstance(MethodTable* pMTClass, BOOL ForManaged);

    //--------------------------------------------------------------
    // Retrieve the IClassFactory.
    IClassFactory *GetIClassFactory();

    //--------------------------------------------------------------
    // Create an instance of the component from the class factory.
    IUnknown *CreateInstanceFromClassFactory(IClassFactory *pClassFact, IUnknown *punkOuter, BOOL *pfDidContainment);
    
public:;
    WCHAR*          m_pwszProgID;   // progId 
    CLSID           m_rclsid;       // CLSID
    WCHAR*          m_pwszServer;   // server name
    
private:
    BOOL            m_bManagedVersion;
};

//
// WinRT override information for ToString/GetHashCode/Equals
//
struct WinRTOverrideInfo
{
    MethodDesc *m_pToStringMD;
    MethodDesc *m_pGetHashCodeMD;
    MethodDesc *m_pEqualsMD;    

    WinRTOverrideInfo(EEClass *pClass);
    static WinRTOverrideInfo *GetOrCreateWinRTOverrideInfo(MethodTable *pMT);
    MethodDesc* GetIStringableToStringMD(MethodTable *pMT);
};

//--------------------------------------------------------------
// Special ComClassFactory for AppX scenarios only
// Call CoCreateInstanceFromApp to ensure compatibility
class AppXComClassFactory : public ComClassFactory
{
protected :
    friend ComClassFactoryCreator;

    AppXComClassFactory(REFCLSID rclsid)
        :ComClassFactory(rclsid)
    {
        LIMITED_METHOD_CONTRACT;
    }

protected :
#ifndef CROSSGEN_COMPILE
    //-------------------------------------------------------------
    // Create instance using CoCreateInstanceFromApp
    virtual IUnknown *CreateInstanceInternal(IUnknown *pOuter, BOOL *pfDidContainment);
#endif
};

//--------------------------------------------------------------
// Creates the right ComClassFactory for you
class ComClassFactoryCreator
{
public :
    static ComClassFactory *Create(REFCLSID rclsid)
    {
        CONTRACT(ComClassFactory *)
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACT_END;

#ifdef FEATURE_APPX
        if (AppX::IsAppXProcess())
            RETURN new AppXComClassFactory(rclsid);
        else
#endif
            RETURN new ComClassFactory(rclsid);
    }
};
//-------------------------------------------------------------------------
// Encapsulates data needed to instantiate WinRT runtime classes.
class WinRTClassFactory : public ClassFactoryBase
{
public:
    WinRTClassFactory(MethodTable *pClassMT)
        : ClassFactoryBase(pClassMT)
    {
        LIMITED_METHOD_CONTRACT;

        m_hClassName         = NULL;
        m_pDefaultItfMT      = NULL;
        m_pWinRTOverrideInfo = NULL;
        m_GCPressure         = RCW::GCPressureSize_WinRT_Base;
    }

    //-------------------------------------------------------------
    // Initialize this instance by parsing factory-related attributes.
    void Init();

    //-------------------------------------------------------------
    // Returns a factory method that matches the given signature.
    MethodDesc *FindFactoryMethod(PCCOR_SIGNATURE pSig, DWORD cSig, Module *pModule);

    //-------------------------------------------------------------
    // Returns a static interface method that matches the given signature.
    MethodDesc *FindStaticMethod(LPCUTF8 pszName, PCCOR_SIGNATURE pSig, DWORD cSig, Module *pModule);

    //-------------------------------------------------------------
    // Function to clean up
    void Cleanup();

    // If true, the class can be activated only using the composition pattern
    BOOL IsComposition()
    {
        LIMITED_METHOD_CONTRACT;
        return !m_pClassMT->IsSealed();
    }

    MethodTable *GetClass()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pClassMT;
    }

    HSTRING GetClassName()
    {
        LIMITED_METHOD_CONTRACT;
        return m_hClassName;
    }

    SArray<MethodTable *> *GetFactoryInterfaces()
    {
        LIMITED_METHOD_CONTRACT;
        return &m_factoryInterfaces;
    }

    SArray<MethodTable *> *GetStaticInterfaces()
    {
        LIMITED_METHOD_CONTRACT;
        return &m_staticInterfaces;
    }

    MethodTable *GetDefaultInterface()
    {
        LIMITED_METHOD_CONTRACT;
        return  m_pDefaultItfMT;
    }

    RCW::GCPressureSize GetGCPressure()
    {
        LIMITED_METHOD_CONTRACT;
        return m_GCPressure;
    }

    FORCEINLINE WinRTOverrideInfo *GetWinRTOverrideInfo ()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pWinRTOverrideInfo;
    }

    BOOL SetWinRTOverrideInfo (WinRTOverrideInfo *pWinRTOverrideInfo)
    {
        LIMITED_METHOD_CONTRACT;
        
        return (InterlockedCompareExchangeT(&m_pWinRTOverrideInfo, pWinRTOverrideInfo, NULL) == NULL);
    }
    
protected:
    MethodTable *GetTypeFromAttribute(IMDInternalImport *pImport, mdCustomAttribute tkAttribute);

    HSTRING m_hClassName;

    InlineSArray<MethodTable *, 1> m_factoryInterfaces;
    InlineSArray<MethodTable *, 1> m_staticInterfaces;

    MethodTable *m_pDefaultItfMT;                           // Default interface of the class

    WinRTOverrideInfo *m_pWinRTOverrideInfo;                // ToString/GetHashCode/GetValue override information

    RCW::GCPressureSize m_GCPressure;                       // GC pressure size associated with instances of this class
};
#endif // FEATURE_COMINTEROP_UNMANAGED_ACTIVATION

//-------------------------------------------------------------------------
// Encapsulates data needed to instantiate WinRT runtime classes implemented
// in managed code.
class WinRTManagedClassFactory : public WinRTClassFactory
{
public:
    WinRTManagedClassFactory(MethodTable *pClassMT)
        : WinRTClassFactory(pClassMT)
    {
        m_pCCWTemplate = NULL;
        LIMITED_METHOD_CONTRACT;
    }

    //-------------------------------------------------------------
    // Function to clean up
    void Cleanup();

    ComCallWrapperTemplate *GetComCallWrapperTemplate()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pCCWTemplate;
    }

    BOOL SetComCallWrapperTemplate(ComCallWrapperTemplate *pTemplate)
    {
        LIMITED_METHOD_CONTRACT;
        return (InterlockedCompareExchangeT(&m_pCCWTemplate, pTemplate, NULL) == NULL);
    }

    ComCallWrapperTemplate *GetOrCreateComCallWrapperTemplate(MethodTable *pFactoryMT);

protected:
    ComCallWrapperTemplate *m_pCCWTemplate; // CCW template for the factory object
};

FORCEINLINE void NewRCWHolderRelease(RCW* p)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (p)
    {
        GCX_COOP();
        
        p->DecoupleFromObject();
        p->Cleanup();
    }
};

class NewRCWHolder : public Wrapper<RCW*, NewRCWHolderDoNothing, NewRCWHolderRelease, NULL>
{
public:
    NewRCWHolder(RCW* p = NULL)
        : Wrapper<RCW*, NewRCWHolderDoNothing, NewRCWHolderRelease, NULL>(p)
    {
        WRAPPER_NO_CONTRACT;
    }

    FORCEINLINE void operator=(RCW* p)
    {
        WRAPPER_NO_CONTRACT;
        Wrapper<RCW*, NewRCWHolderDoNothing, NewRCWHolderRelease, NULL>::operator=(p);
    }
};

#ifndef DACCESS_COMPILE
class RCWHolder
{
public:
    RCWHolder(PTR_Thread pThread)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            SUPPORTS_DAC;
            PRECONDITION(CheckPointer(pThread));
        }
        CONTRACTL_END;

        m_pThread = pThread;
        m_pRCW = NULL;
        m_pSB = NULL;
        m_fValid = FALSE;
        m_fRCWInUse = FALSE;
    }

    ~RCWHolder()
    {
        CONTRACTL
        {
            NOTHROW;
            if (m_fRCWInUse) GC_TRIGGERS; else GC_NOTRIGGER;
            MODE_ANY;
            SUPPORTS_DAC;
        }
        CONTRACTL_END;

        if (m_fRCWInUse)
        {
            m_pRCW->DecrementUseCount();
        }
    }

    void Init(PTR_SyncBlock pSB)
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
            SUPPORTS_DAC;
            PRECONDITION(CheckPointer(pSB));
            PRECONDITION(m_pRCW == NULL);
            PRECONDITION(CheckPointer(m_pThread));
        }
        CONTRACTL_END;
        
        m_pSB = pSB;
        m_pRCW = m_pSB->GetInteropInfoNoCreate()->GetRCWAndIncrementUseCount();

        if (!m_pRCW)
        {
            COMPlusThrow(kInvalidComObjectException, IDS_EE_COM_OBJECT_NO_LONGER_HAS_WRAPPER);
        }
        m_fRCWInUse = TRUE;

        m_fValid = TRUE;
    }

    void Init(OBJECTREF pObject)
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_COOPERATIVE;
            PRECONDITION(m_pRCW == NULL);
            PRECONDITION(CheckPointer(m_pThread));
        }
        CONTRACTL_END;

        Init(pObject->GetSyncBlock());
    }

    // Like Init() but does not increment the use count on the RCW. To be used on perf-critical code paths.
    void InitFastCheck(PTR_SyncBlock pSB)
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
            SUPPORTS_DAC;
            PRECONDITION(CheckPointer(pSB));
            PRECONDITION(m_pRCW == NULL);
            PRECONDITION(CheckPointer(m_pThread));
        }
        CONTRACTL_END;

        m_pSB = pSB;
        m_pRCW = m_pSB->GetInteropInfoNoCreate()->GetRawRCW();

        if (!m_pRCW)
        {
            COMPlusThrow(kInvalidComObjectException, IDS_EE_COM_OBJECT_NO_LONGER_HAS_WRAPPER);
        }

        m_fValid = TRUE;
    }

    void InitNoCheck(PTR_SyncBlock pSB)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            SUPPORTS_DAC;
            PRECONDITION(CheckPointer(pSB));
            PRECONDITION(m_pRCW == NULL);
            PRECONDITION(CheckPointer(m_pThread));
            PRECONDITION(GetThread() == m_pThread);
        }
        CONTRACTL_END;
        
        m_pSB = pSB;
        m_pRCW = m_pSB->GetInteropInfoNoCreate()->GetRawRCW();
        m_fValid = TRUE;
    }

    void InitNoCheck(OBJECTREF pObject)
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_COOPERATIVE;
            PRECONDITION(m_pRCW == NULL);
            PRECONDITION(CheckPointer(m_pThread));
        }
        CONTRACTL_END;

        InitNoCheck((PTR_SyncBlock)pObject->GetSyncBlock());    
    }

    void InitNoCheck(RCW *pRCW)
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_COOPERATIVE;
            PRECONDITION(m_pRCW == NULL);
            PRECONDITION(CheckPointer(m_pThread));
            PRECONDITION(CheckPointer(pRCW));
        }
        CONTRACTL_END;

        InitNoCheck(pRCW->GetSyncBlock());
    }

    void UnInit()
    {
        CONTRACTL
        {
            if (m_fRCWInUse)
            {
                THROWS;
                GC_TRIGGERS;
            }
            else
            {
                NOTHROW;
                GC_NOTRIGGER;
            }
            MODE_ANY;
            SUPPORTS_DAC;
            PRECONDITION(CheckPointer(m_pThread));
            PRECONDITION(CheckPointer(m_pSB));
            PRECONDITION(GetThread() == m_pThread);
        }
        CONTRACTL_END;
        
        // Unregister this RCW on the thread
        if (m_fValid)
        {
            m_fValid = FALSE;
        }

        BOOL fThrowException = FALSE;
        if (m_fRCWInUse)
        {
            // Now's the perfect time to check the RCW again. If the SyncBlock doesn't point to
            // our RCW anymore, we know that we must have raced with an explicit release.
            if (m_pSB->GetInteropInfoNoCreate()->GetRawRCW() != m_pRCW)
            {
                fThrowException = TRUE;
            }

            m_pRCW->DecrementUseCount();
            m_fRCWInUse = FALSE;
        }

        m_pRCW = NULL;
        m_pSB = NULL;

        if (fThrowException)
        {
            // Since the object demonstrably had the RCW when we executed Init, we know for sure that
            // this must be a race. Use the same exception for compatibility but pass resource ID of
            // a slightly enhanced error message.
            COMPlusThrow(kInvalidComObjectException, IDS_EE_COM_OBJECT_RELEASE_RACE);
        }
    }
    
    PTR_RCW GetRawRCWUnsafe()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_pRCW;
    }

    BOOL IsNull()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return (m_pRCW == NULL) ? TRUE : FALSE;
    }

    inline PTR_RCW operator->()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            SUPPORTS_DAC;
            PRECONDITION(CheckPointer(m_pRCW));
        }
        CONTRACTL_END;
        
        return m_pRCW;
    }

private:
    PTR_RCW       m_pRCW;

    // Used for per-thread registration.
    PTR_SyncBlock m_pSB;
    PTR_Thread    m_pThread;

    // Used for de-registration
    BOOL        m_fValid;
    BOOL        m_fRCWInUse;
};
#endif // !DACCESS_COMPILE


//---------------------------------------------------------------------
// When the RCW is used for actual calls out to the COM object, we want to check for cleanup race
// when we're done with it, ideally at the point where the RCWHolder goes out of scope. But, since
// throwing exceptions from destructors is generally a bad idea, we use the RCWPROTECT_BEGIN
// RCWPROTECT_END brackets instead of the plain RCWHolder.
//---------------------------------------------------------------------
#define RCWPROTECT_BEGIN(pRCWHolder, arg)         \
    {                                             \
        pRCWHolder.Init(arg);

#define RCWPROTECT_END(pRCWHolder)                \
        pRCWHolder.UnInit();                      \
    }

//---------------------------------------------------------------------
// RCW cache, act as the manager for the RCWs
// uses a hash table to map IUnknown to the corresponding wrappers.
// There is one such cache per thread affinity domain.
//
// <TODO>@TODO context cwb: revisit.  One could have a cache per thread affinity
// domain, or one per context.  It depends on how we do the handshake between
// ole32 and runtime contexts.  For now, we only worry about apartments, so
// thread affinity domains are sufficient.</TODO>
//---------------------------------------------------------------------
class RCWCache
{
    friend class RCWRefCache;

public:
    class LockHolder : public CrstHolder
    {
    public:
        LockHolder(RCWCache *pCache)
            : CrstHolder(&pCache->m_lock)
        {
            CONTRACTL
            {
                NOTHROW;
                GC_NOTRIGGER;
                MODE_COOPERATIVE;       // The RCWCache lock must be taken
                                        // in coop mode.  It syncs RCW releases
                                        // with the GC.
                                        // This lock will *not* be taken by the GC
                                        // during collection.
            }
            CONTRACTL_END;
        }
    };

    
    RCWCache(AppDomain *pDomain);

    static RCWCache* GetRCWCache();
    static RCWCache* GetRCWCacheNoCreate();

#ifndef DACCESS_COMPILE
    // Insert wrapper into hash table.
    // Since lock is held, no need to report RCW use to thread.
    void InsertWrapper(RCWHolder* pRCW)
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_COOPERATIVE;
            PRECONDITION(CheckPointer(pRCW));
            PRECONDITION(CheckPointer(pRCW->GetRawRCWUnsafe()));
            PRECONDITION(LOCKHELD());
            PRECONDITION(LookupWrapperUnsafe(pRCW->GetRawRCWUnsafe()->m_pIdentity) == NULL);
        }
        CONTRACTL_END;

        m_HashMap.Add(pRCW->GetRawRCWUnsafe());
    }

    void RemoveWrapper(RCWHolder* pRCW)
    {
        WRAPPER_NO_CONTRACT;
        
        RemoveWrapper(pRCW->GetRawRCWUnsafe());
    }
#endif // DACCESS_COMPILE

    // Delete wrapper for a given IUnk from hash table
    void RemoveWrapper(RCW* pRCW)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(CheckPointer(pRCW));
        }
        CONTRACTL_END;
        
        // Note that the GC thread doesn't have to take the lock
        // since all other threads access in cooperative mode

        _ASSERTE_IMPL(LOCKHELD() && GetThread()->PreemptiveGCDisabled()
                 || Debug_IsLockedViaThreadSuspension());
       
        LPVOID pIdentity;
        pIdentity = pRCW->m_pIdentity;
        _ASSERTE(pIdentity != NULL);

        m_HashMap.Remove(pIdentity);
    }

    //  Lookup to see if we already have a wrapper else insert this wrapper
    //  return a valid wrapper that has been inserted into the cache
    BOOL FindOrInsertWrapper_NoLock(IUnknown* pIdentity, RCWHolder* pWrap, BOOL fAllowReinit);
    
    AppDomain* GetDomain()
    {
        CONTRACT (AppDomain*)
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            POSTCONDITION(CheckPointer(RETVAL));
        }
        CONTRACT_END;
        
        RETURN m_pDomain;
    }
    
    // Worker function called to release wrappers in the pCtxCookie context.
    // Zero indicates all wrappers.
    void ReleaseWrappersWorker(LPVOID pCtxCookie);

    // Worker function called to detach GC-unmarked wrappers from their
    // underlying COM pUnk identities to prevent resurrection.
    void DetachWrappersWorker();

#ifndef DACCESS_COMPILE
    
    // Lookup wrapper, lookup hash table for a wrapper for a given IUnk
    void LookupWrapper(LPVOID pUnk, RCWHolder* pRCW)
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_COOPERATIVE;
            PRECONDITION(CheckPointer(pUnk));
            PRECONDITION(LOCKHELD());
            //POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        }
        CONTRACTL_END;

        // We don't want the GC messing with the hash table underneath us.
        GCX_FORBID();
        
        RCW* pRawRCW = LookupWrapperUnsafe(pUnk);

        if (pRawRCW == NULL)
            return;

        // Assume that we already have a sync block for this object.
        pRCW->InitNoCheck(pRawRCW);
    }

    RCW* LookupWrapperUnsafe(LPVOID pUnk)
    {
        CONTRACT (RCW*)
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_COOPERATIVE;
            PRECONDITION(CheckPointer(pUnk));
            PRECONDITION(LOCKHELD());
            POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        }
        CONTRACT_END;
        
        // We don't want the GC messing with the hash table underneath us.
        GCX_FORBID();
        
        RETURN m_HashMap.Lookup(pUnk);
    }
        
#endif //DACCESS_COMPILE
   
#ifdef _DEBUG
    BOOL LOCKHELD()
    {
        WRAPPER_NO_CONTRACT;
        return m_lock.OwnedByCurrentThread();
    }
#endif

private :
    friend class COMInterfaceMarshaler;
    
    // Look up to see if we already have an valid wrapper in cache for this IUnk
    // DOES NOT hold a lock inside the function - locking in the caller side IS REQUIRED
    void FindWrapperInCache_NoLock(IUnknown* pIdentity, RCWHolder* pRCW);

private:
    class RCWCacheTraits : public DefaultSHashTraits<RCW *>
    {
    public:
        typedef LPVOID key_t;
        static RCW *Null()                         { LIMITED_METHOD_CONTRACT; return NULL; }
        static bool IsNull(RCW *e)                 { LIMITED_METHOD_CONTRACT; return (e == NULL); }
        static const LPVOID GetKey(RCW *e)         { LIMITED_METHOD_CONTRACT; return e->m_pIdentity; }
        static count_t Hash(LPVOID key_t)          { LIMITED_METHOD_CONTRACT; return (count_t)key_t; }
        static BOOL Equals(LPVOID lhs, LPVOID rhs) { LIMITED_METHOD_CONTRACT; return (lhs == rhs); }
        static RCW *Deleted()                      { LIMITED_METHOD_CONTRACT; return (RCW *)-1; }
        static bool IsDeleted(RCW *e)              { LIMITED_METHOD_CONTRACT; return e == (RCW *)-1; }
    };

    SHash<RCWCacheTraits> m_HashMap;
    
    // spin lock for fast synchronization
    Crst            m_lock;
    AppDomain*      m_pDomain;
};

struct ReleaseRCWList_Args
{
    RCW                *pHead;
    BOOL                ctxTried;
    BOOL                ctxBusy;
};

// RCWCleanupList represents a list of RCWs whose corresponding managed objects have been collected.
// These RCWs must be released, potentially involving transitioning into the right apartment/context.
// That is why the operation is deferred and done in chunks instead of individual RCWs so the
// transition overhead is minimized. This data structure is a two-dimensional linked list with
// individual RCWs grouped into buckets that share the same COM apartment/context.
//
// Adding RCWs into the cleanup list must not allocate memory or perform any similar operation that
// may fail. The only operation allowed to fail is the release itself (out of our control). Therefore
// the data structure uses only a single statically allocated instance of RCWCleanupList and the
// "links" are taken care of by the RCW structures themselves.
//
//         m_pFirstBucket        m_pNextCleanupBucket        m_pNextCleanupBucket
// RCWCleanupList ------> RCW_1a -------------------> RCW_2a -------------------> RCW_3a -->...--> NULL
//                          |                           |                           |
//                          | m_pNextRCW                | m_pNextRCW                | m_pNextRCW
//                          v                           v                           v
//                        RCW_1b                      RCW_2b                      RCW_3b
//                          |                           |                           |
//                          | m_pNextRCW                | m_pNextRCW                | m_pNextRCW
//                          v                           v                           v
//                        RCW_1c                      RCW_2c                      RCW_3c
//                          |                           |                           |
//                          v                           v                           v
//                         ...                         ...                         ...
//                          |                           |                           |
//                          v                           v                           v
//                         NULL                        NULL                        NULL
//
// In the picture above, RCW_1a, RCW_1b, RCW_1c, ... are in the same bucket, RCW_2a, RCW_2b, RCW_2c, ...
// are in another bucket etc. The supported operations are adding an RCW (see code:RCWCleanupList::AddWrapper)
// and removing entire buckets that meet given criteria (see code:RCWCleanupList::CleanupAllWrappers and
// code:RCWCleanupList::CleanupWrappersInCurrentCtxThread).

class RCWCleanupList
{
#ifdef DACCESS_COMPILE
    friend class ClrDataAccess;
#endif // DACCESS_COMPILE

public:   
    RCWCleanupList()
        : m_lock(CrstRCWCleanupList, CRST_UNSAFE_ANYMODE),
          m_pCurCleanupThread(NULL), m_doCleanupInContexts(FALSE),
          m_pFirstBucket(NULL)
    {
        WRAPPER_NO_CONTRACT;
    }
    
    ~RCWCleanupList()
    {
        WRAPPER_NO_CONTRACT;

        _ASSERTE(IsEmpty());
    }
    
    VOID AddWrapper(RCW* pRCW);
    VOID AddWrapper_NoLock(RCW *pRCW);
    VOID CleanupAllWrappers();
    VOID CleanupWrappersInCurrentCtxThread(BOOL fWait = TRUE, BOOL fManualCleanupRequested = FALSE, BOOL bIgnoreComObjectEagerCleanupSetting = FALSE);
    
    BOOL IsEmpty();

private:
    // These 2 functions are static so we can call them through the Context Callback mechanism.
    static HRESULT ReleaseRCWListInCorrectCtx(LPVOID pData);
    static VOID ReleaseRCWListRaw(RCW* pRCW);

#ifndef DACCESS_COMPILE
    // Utility class that maintains a list of buckets removed from the cleanup list.
    struct RemovedBuckets
    {
        RemovedBuckets()
            : m_pFirstBucket(NULL),
              m_pLastBucket(NULL)
        { }

        ~RemovedBuckets()
        {
            // we must always end up with an empty list, otherwise we leak RCWs
            _ASSERTE(m_pFirstBucket == NULL);
        }

        void Append(PTR_RCW pBucket)
        {
            LIMITED_METHOD_CONTRACT;

            if (m_pLastBucket == NULL)
            {
                // appending the first bucket
                _ASSERTE(m_pFirstBucket == NULL);
                m_pFirstBucket = pBucket;
            }
            else
            {
                // appending >first bucket
                m_pLastBucket->m_pNextCleanupBucket = pBucket;
            }

            pBucket->m_pNextCleanupBucket = NULL;
            m_pLastBucket = pBucket;
        }

        RCW *PopHead()
        {
            LIMITED_METHOD_CONTRACT;

            RCW *pRetVal = m_pFirstBucket;
            if (m_pFirstBucket != NULL)
                m_pFirstBucket = m_pFirstBucket->m_pNextCleanupBucket;

            return pRetVal;
        }

        RCW            *m_pFirstBucket;
        RCW            *m_pLastBucket;
    };
#endif // !DACCESS_COMPILE

    RCW                *m_pFirstBucket;
    Crst                m_lock;
    Thread*             m_pCurCleanupThread;

    // Fast check for whether threads should help cleanup wrappers in their contexts
    BOOL                m_doCleanupInContexts;
};

FORCEINLINE void CtxEntryHolderRelease(CtxEntry *p)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
    
    if (p != NULL)
    {
        p->Release();
    }
}

class CtxEntryHolder : public Wrapper<CtxEntry *, CtxEntryDoNothing, CtxEntryHolderRelease, NULL>
{
public:
    CtxEntryHolder(CtxEntry *p = NULL)
        : Wrapper<CtxEntry *, CtxEntryDoNothing, CtxEntryHolderRelease, NULL>(p)
    {
        WRAPPER_NO_CONTRACT;
    }
    
    FORCEINLINE void operator=(CtxEntry *p)
    {
        WRAPPER_NO_CONTRACT;
        
        Wrapper<CtxEntry *, CtxEntryDoNothing, CtxEntryHolderRelease, NULL>::operator=(p);
    }
   
};

#endif // _RUNTIMECALLABLEWRAPPER_H
