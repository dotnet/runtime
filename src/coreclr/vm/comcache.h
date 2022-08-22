// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ComCache.h
//

//
// Classes/Structures used to represent and store info on COM interfaces and contexts.


#ifndef _H_COMCACHE
#define _H_COMCACHE

#ifndef FEATURE_COMINTEROP
#error FEATURE_COMINTEROP is required for this file
#endif // FEATURE_COMINTEROP

// COM context callback
typedef HRESULT ( __stdcall __RPC_FAR *PFNCTXCALLBACK)(void __RPC_FAR *pParam);

#include <ctxtcall.h>

//================================================================
// Forward declarations.
class CtxEntryCache;
class CtxEntry;
class Thread;

//================================================================
// OLE32 helpers.
HRESULT             wCoMarshalInterThreadInterfaceInStream(REFIID riid, LPUNKNOWN pUnk, LPSTREAM* ppStm);
STDAPI_(LPSTREAM)   CreateMemStm(DWORD cb, BYTE** ppBuf);


typedef DPTR(CtxEntry) PTR_CtxEntry;

//==============================================================
// An entry representing a COM+ 1.0 context or an appartment.
class CtxEntry
{
    // The CtxEntryCache needs to be able to see the internals
    // of the CtxEntry.
    friend CtxEntryCache;

    // NewHolder<CtxEntry> needs to be able to call the destructor of CtxEntry.
    // DISABLE Warning C4396, the inline specifier cannot be used when a friend declaration refers to a specialization of a function template
#pragma warning(push)		// store original warning levels
#pragma warning(disable: 4396)
    friend void Delete<CtxEntry>(CtxEntry *);
#pragma warning(pop)		// restore original warning levels


private:
    // Disallow creation and deletion of the CtxEntries.
    CtxEntry(LPVOID pCtxCookie, Thread* pSTAThread);
    ~CtxEntry();

    // Initialization method called from the CtxEntryCache.
    VOID Init();

public:
    // Add a reference to the CtxEntry.
    DWORD AddRef();

    // Release a reference to the CtxEntry.
    DWORD Release();

    // Function to enter the context. The specified callback function will
    // be called from within the context.
    HRESULT EnterContext(PFNCTXCALLBACK pCallbackFunc, LPVOID pData);

    // Accessor for the context cookie.
    LPVOID GetCtxCookie()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pCtxCookie;
    }

    // Accessor for the STA thread.
    Thread* GetSTAThread()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pSTAThread;
    }

private:
    // Callback function called by DoCallback.
    static HRESULT __stdcall EnterContextCallback(ComCallData* pData);

    LPVOID          m_pCtxCookie;           // The OPAQUE context cookie.
    IUnknown*       m_pObjCtx;              // The object context interface.
    DWORD           m_dwRefCount;           // The ref count.
    Thread*         m_pSTAThread;           // STA thread associated with the context, if any
};

//==============================================================
// IUnkEntry: represent a single COM component
struct IUnkEntry
{
    // The context entry needs to be a friend to be able to call InitSpecial.
    friend CtxEntry;
    // RCW need to access IUnkEntry
    friend RCW;

#ifdef _DEBUG
    // Does not throw if m_pUnknown is no longer valid, debug only.
    IUnknown *GetRawIUnknown_NoAddRef_NoThrow()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(m_pUnknown != NULL && m_pUnknown != (IUnknown*)0xBADF00D);

        return m_pUnknown;
    }
#endif // _DEBUG

    IUnknown *GetRawIUnknown_NoAddRef()
    {
        CONTRACTL
        {
            THROWS;
            MODE_ANY;
        }
        CONTRACTL_END;

        IUnknown *pUnk = m_pUnknown;
#ifndef DACCESS_COMPILE
        if (pUnk == (IUnknown *)0xBADF00D)
        {
            // All callers of this method had checked the pUnk before so this must be a race.
            COMPlusThrow(kInvalidComObjectException, IDS_EE_COM_OBJECT_RELEASE_RACE);
        }
#endif // !DACCESS_COMPILE

        return pUnk;
    }

    LPVOID GetCtxCookie()
    {
        LIMITED_METHOD_CONTRACT;

        return m_pCtxCookie;
    }

    // Is the RCW disconnected from its COM object?
    inline bool IsDisconnected()
    {
        LIMITED_METHOD_CONTRACT;
        return (m_pUnknown == (IUnknown*)0xBADF00D ||
           (GetCtxEntry() != NULL && m_pCtxCookie != GetCtxEntry()->GetCtxCookie()));
    }


private :
    // Initialize the entry, returns true if we are in an STA.
    // We assert inside Init that this IUnkEntry is indeed within a RCW
    void Init(IUnknown* pUnk, BOOL bIsFreeThreaded, Thread *pThread DEBUGARG(RCW *pRCW));

    // Release the interface pointer held by the IUnkEntry.
    VOID ReleaseInterface(RCW *pRCW);

    // Free the IUnknown entry. ReleaseInterface must have been called.
    VOID Free();

    // Get the RCW associated with this IUnkEntry
    // We assert inside Init that this IUnkEntry is indeed within a RCW
    RCW *GetRCW();

    // Get IUnknown for the current context from IUnkEntry
    IUnknown* GetIUnknownForCurrContext(bool fNoAddRef);

    // Unmarshal IUnknown for the current context from IUnkEntry
    IUnknown* UnmarshalIUnknownForCurrContext();

    // Release the stream. This will force UnmarshalIUnknownForCurrContext to transition
    // into the context that owns the IP and re-marshal it to the stream.
    void ReleaseStream();

    // Indicates if the COM component being wrapped by the IUnkEntry aggregates the FTM
    inline bool IsFreeThreaded();

    // Indicates if the COM component being wrapped by the IUnkEntry implements INoMashal.
    inline bool IsMarshalingInhibited();

    VOID CheckValidIUnkEntry();

    HRESULT HRCheckValidIUnkEntry();

    // Unmarshal IUnknown for the current context if the lock is held
    IUnknown* UnmarshalIUnknownForCurrContextHelper();

    // Fix for if the lock is held that works on a stack allocated stream
    // instead of the member variable stream
    static HRESULT MarshalIUnknownToStreamCallback2(LPVOID pData);

    // Callback called to marshal the IUnknown into a stream lazily.
    static HRESULT MarshalIUnknownToStreamCallback(LPVOID pData);

    // Helper function called from MarshalIUnknownToStreamCallback.
    HRESULT MarshalIUnknownToStream();

    // Method to try and start updating the entry.
    bool TryUpdateEntry();

    // Method to end updating the entry.
    VOID EndUpdateEntry();

    // Helper function to determine if a COM component aggregates the FTM.
    static bool IsComponentFreeThreaded(IUnknown *pUnk);

    inline PTR_CtxEntry GetCtxEntry()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        PTR_CtxEntry pCtxEntry = dac_cast<PTR_CtxEntry>(dac_cast<TADDR>(m_pCtxEntry) & ~1);

        return pCtxEntry;
    }

    // Context cookie at the point where we acquired the interface pointer
    LPVOID          m_pCtxCookie;

    // Context entry representing the context where we acquired the interface pointer.
    // We use the lowest bit for synchronization and we rely on the fact that the
    // context itself (the rest of the bits) does not change throughout the lifetime
    // of this object.
    PTR_CtxEntry    m_pCtxEntry;

    // IUnknown interface
    IUnknown*       m_pUnknown;

    // IStream used for marshalling
    IStream*        m_pStream;
};

// Don't use this directly as the methodtable could have been released
//  by an AD Unload.
typedef MethodTable* IE_METHODTABLE_PTR;

//==============================================================
// Interface Entry represents a single COM IP
struct InterfaceEntry
{
    // Initialize the entry, returns true on success (i.e. the entry was free).
    bool Init(MethodTable* pMT, IUnknown* pUnk);

    // Helper to determine if the entry is free.
    BOOL IsFree();

    // Mark the entry as free.
    void Free();

    // Member of the entry. These must be volatile so the compiler
    // will not try and optimize reads and writes to them.
    Volatile<IE_METHODTABLE_PTR> m_pMT;                  // Interface asked for
    Volatile<IUnknown*>          m_pUnknown;             // Result of query
};

class CtxEntryCacheTraits : public DefaultSHashTraits<CtxEntry *>
{
public:
    typedef LPVOID key_t;
    static CtxEntry *Null()                     { LIMITED_METHOD_CONTRACT; return NULL; }
    static bool IsNull(CtxEntry *e)             { LIMITED_METHOD_CONTRACT; return (e == NULL); }
    static const LPVOID GetKey(CtxEntry *e)     { LIMITED_METHOD_CONTRACT; return e->GetCtxCookie(); }
    static count_t Hash(LPVOID key_t)           { LIMITED_METHOD_CONTRACT; return (count_t)(size_t) key_t; }
    static BOOL Equals(LPVOID lhs, LPVOID rhs)  { LIMITED_METHOD_CONTRACT; return (lhs == rhs); }
    static CtxEntry *Deleted()                  { LIMITED_METHOD_CONTRACT; return (CtxEntry *)-1; }
    static bool IsDeleted(CtxEntry *e)          { LIMITED_METHOD_CONTRACT; return e == (CtxEntry *)-1; }
};

//==============================================================
// The cache of context entries.
class CtxEntryCache
{
    // The CtxEntry needs to be able to call some of the private
    // method of the CtxEntryCache.
    friend CtxEntry;

private:
    // Disallow creation and deletion of the CtxEntryCache.
    CtxEntryCache();
    ~CtxEntryCache();

public:
    // Static initialization routine for the CtxEntryCache.
    static VOID Init();

    // Static accessor for the one and only instance of the CtxEntryCache.
    static CtxEntryCache *GetCtxEntryCache();

    // Method to retrieve/create a CtxEntry for the specified context cookie.
    CtxEntry *FindCtxEntry(LPVOID pCtxCookie, Thread *pSTAThread);

private:
    CtxEntry * CreateCtxEntry(LPVOID pCtxCookie, Thread * pSTAThread);

    // Helper function called from the CtxEntry.
    void TryDeleteCtxEntry(LPVOID pCtxCookie);

    SHash<CtxEntryCacheTraits>  m_CtxEntryHash;

    // spin lock for fast synchronization
    SpinLock                m_Lock;

    // The one and only instance for the context entry cache.
    static CtxEntryCache*   s_pCtxEntryCache;
};

#endif
