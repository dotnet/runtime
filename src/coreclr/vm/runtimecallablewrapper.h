// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

class Object;
class ComCallWrapper;
class Thread;

#define GC_PRESSURE_PROCESS_LOCAL 3456
#define GC_PRESSURE_MACHINE_LOCAL 4004
#define GC_PRESSURE_REMOTE 4824

enum {INTERFACE_ENTRY_CACHE_SIZE = 8};

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
        // unused               = 0x01,
        // unused               = 0x02,
        // unused               = 0x04,
        CF_NeedUniqueObject     = 0x08, // always create a new RCW/object even if we have one cached already
        // unused               = 0x10,
        // unused               = 0x20,
    };

    static CreationFlags CreationFlagsFromObjForComIPFlags(ObjFromComIP::flags flags);

    // List of RCW instances that have been freed since the last RCW cleanup.
    static SLIST_HEADER s_RCWStandbyList;

    // Simple read-only iterator for all cached interface pointers.
    class CachedInterfaceEntryIterator
    {
        PTR_RCW   m_pRCW;
        int       m_InlineCacheIndex;

    public:
        inline CachedInterfaceEntryIterator(PTR_RCW pRCW)
        {
            LIMITED_METHOD_CONTRACT;
            m_pRCW = pRCW;
            m_InlineCacheIndex = -1;
        }

        // Move to the next item returning TRUE if an item exists or FALSE if we've run off the end
        inline bool Next()
        {
            LIMITED_METHOD_CONTRACT;

            if (m_InlineCacheIndex >= INTERFACE_ENTRY_CACHE_SIZE)
                return FALSE;
    
            // stop incrementing m_InlineCacheIndex once we reach INTERFACE_ENTRY_CACHE_SIZE
            if (++m_InlineCacheIndex < INTERFACE_ENTRY_CACHE_SIZE)
                return TRUE;

            return FALSE;
        }

        inline InterfaceEntry *GetEntry()
        {
            LIMITED_METHOD_CONTRACT;

            _ASSERTE_MSG(m_InlineCacheIndex >= 0, "Iterator starts before the first element, you need to call Next");
            if (m_InlineCacheIndex >= INTERFACE_ENTRY_CACHE_SIZE)
                return NULL;

            return &m_pRCW->m_aInterfaceEntries[m_InlineCacheIndex];
        }

        inline LPVOID GetCtxCookie()
        {
            LIMITED_METHOD_CONTRACT;

            _ASSERTE_MSG(m_InlineCacheIndex >= 0, "Iterator starts before the first element, you need to call Next");
            if (m_InlineCacheIndex >= INTERFACE_ENTRY_CACHE_SIZE)
                return NULL;

            return m_pRCW->GetWrapperCtxCookie();
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
        GCPressureSize_COUNT        = 4
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

    // Performs QI for the given interface, optionally instantiating it with the given generic args.
    HRESULT CallQueryInterface(MethodTable *pMT, Instantiation inst, IID *piid, IUnknown **ppUnk);

    //-----------------------------------------------------------------
    // Retrieve correct COM IP for the current apartment.
    // use the cache /update the cache
    IUnknown* GetComIPForMethodTableFromCache(MethodTable * pMT);

    // helpers to get to IUnknown, IDispatch interfaces
    // Returns an addref'd pointer - caller must Release
    IUnknown*  GetWellKnownInterface(REFIID riid);

    IUnknown*  GetIUnknown();
    IUnknown*  GetIUnknown_NoAddRef();
    IDispatch* GetIDispatch();

    ULONG GetRefCount()
    {
        return m_cbRefCount;
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
                (!bIInspectableOnly))
            {
                TADDR taUnk = (TADDR)(it.GetEntry()->m_pUnknown.Load());
                if (taUnk != NULL)
                {
                    rgItfPtrs->Append(taUnk);
                }
            }
        }
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

    //
    // This COM object aggregates FTM?
    //
    bool IsFreeThreaded()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return (m_Flags.m_MarshalingType == MarshalingType_FreeThreaded) ;
    }

    //
    // This COM object implements INoMarshal?
    //
    bool IsMarshalingInhibited()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return (m_Flags.m_MarshalingType == MarshalingType_Inhibit) ;
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
    // Returns true iff pItfMT is a "standard managed" interface, such as
    // IEnumerator, and the RCW supports the interface through classic COM
    // interop mechanisms.
    bool SupportsMngStdInterface(MethodTable *pItfMT);

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

            static_assert((1 << 3) >= GCPressureSize_COUNT, "m_GCPressure needs a bigger data type");
            DWORD       m_GCPressure:3;            // index into s_rGCPressureTable

            // Reserve 2 bits for marshaling behavior
            DWORD       m_MarshalingType:2;        // MarshalingBehavior of the COM object.

            DWORD       m_Detached:1;              // set if the RCW was found dead during GC
        };
    }
    m_Flags;

    static_assert(sizeof(RCWFlags) == 4, "Flags don't fit in 4 bytes, there's too many of them");

    // GC pressure sizes in bytes
    static const int s_rGCPressureTable[GCPressureSize_COUNT];

    // Tracks concurrent access to this RCW to prevent using RCW instances that have already been released
    LONG                m_cbUseCount;

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

// 04 CLASS_IS_HINT                   04 ITF_MARSHAL_CLASS_IS_HINT
// 08 UNIQUE_OBJECT                                                   08 CF_NeedUniqueObject
//                                    08 ITF_MARSHAL_DISP_ITF
//                                    10 ITF_MARSHAL_USE_BASIC_ITF
inline RCW::CreationFlags RCW::CreationFlagsFromObjForComIPFlags(ObjFromComIP::flags dwFlags)
{
    LIMITED_METHOD_CONTRACT;

    static_assert_no_msg(CF_NeedUniqueObject     == ObjFromComIP::UNIQUE_OBJECT);

    RCW::CreationFlags result = (RCW::CreationFlags)(dwFlags &
                                        (ObjFromComIP::UNIQUE_OBJECT));
    return result;
}

#ifdef FEATURE_COMINTEROP_UNMANAGED_ACTIVATION

class ComClassFactory;

class ClassFactoryBase
{
public:
    //-------------------------------------------------------------
    // Function to clean up
    virtual void Cleanup() = 0;

    ComClassFactory *AsComClassFactory()
    {
        LIMITED_METHOD_CONTRACT;
        return (ComClassFactory *)this;
    }
protected:
    ClassFactoryBase(MethodTable *pClassMT = NULL)
        : m_pClassMT(pClassMT)
    {
        LIMITED_METHOD_CONTRACT;
    }

    MethodTable *m_pClassMT;
};

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
public:
    // We have two types of ComClassFactory:
    // 1. We build for reflection purpose.  We should not clean up.
    // 2. We build for IClassFactory.  We should clean up.
    //-----------------------------------------------------------
    // constructor
    ComClassFactory(REFCLSID rclsid)
    {
        WRAPPER_NO_CONTRACT;

        m_wszServer = NULL;

        // Default to unmanaged version.
        m_bManagedVersion = FALSE;
        m_rclsid = rclsid;
    }

    //---------------------------------------------------------
    // Mark this instance as Managed Version, so we will not do clean up.
    void SetManagedVersion()
    {
        LIMITED_METHOD_CONTRACT;
        m_bManagedVersion = TRUE;
    }

    //--------------------------------------------------------------
    // Init the ComClassFactory
    void Init(__in_opt PCWSTR wszServer, MethodTable* pClassMT);

    //-------------------------------------------------------------
    // create instance, calls IClassFactory::CreateInstance
    OBJECTREF CreateInstance(MethodTable* pMTClass, BOOL ForManaged = FALSE);

    //-------------------------------------------------------------
    // Function to clean up
    void Cleanup();

protected :
    //-------------------------------------------------------------
    // Create instance. Overridable from child classes
    virtual IUnknown *CreateInstanceInternal(IUnknown *pOuter, BOOL *pfDidContainment);
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
    CLSID           m_rclsid;       // CLSID
    PCWSTR          m_wszServer;   // server name

private:
    BOOL            m_bManagedVersion;
};
#endif // FEATURE_COMINTEROP_UNMANAGED_ACTIVATION

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
        static count_t Hash(LPVOID key_t)          { LIMITED_METHOD_CONTRACT; return (count_t)(size_t) key_t; }
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
        : m_pFirstBucket(NULL), m_lock(CrstRCWCleanupList, CRST_UNSAFE_ANYMODE),
          m_pCurCleanupThread(NULL), m_doCleanupInContexts(FALSE)         
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
