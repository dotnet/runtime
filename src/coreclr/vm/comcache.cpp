// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



#include "common.h"

#include <crtwrap.h>
#include "comcache.h"
#include "runtimecallablewrapper.h"
#include <mtx.h>
#include "win32threadpool.h"

#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT
#include "olecontexthelpers.h"
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT


//================================================================
// Guid definitions.
const GUID IID_IEnterActivityWithNoLock = { 0xd7174f82, 0x36b8, 0x4aa8, { 0x80, 0x0a, 0xe9, 0x63, 0xab, 0x2d, 0xfa, 0xb9 } };
const GUID IID_IFuncEvalAbort           = { 0xde6844f6, 0x95ac, 0x4e83, { 0x90, 0x8d, 0x9b, 0x1b, 0xea, 0x2f, 0xe0, 0x8c } };

// sanity check., to find stress bug #82137
VOID IUnkEntry::CheckValidIUnkEntry()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (IsDisconnected())
    {
        COMPlusThrow(kInvalidComObjectException, IDS_EE_COM_OBJECT_NO_LONGER_HAS_WRAPPER);
    }
}

// Version that returns an HR instead of throwing.
HRESULT IUnkEntry::HRCheckValidIUnkEntry()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (IsDisconnected())
    {
        return COR_E_INVALIDCOMOBJECT;
    }

    return S_OK;
}

// Returns IErrorInfo corresponding to the exception injected by the debugger to abort a func eval,
// or NULL if there is no such exception.
static IErrorInfo *CheckForFuncEvalAbortNoThrow(HRESULT hr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // the managed exception thrown by the debugger is translated to EXCEPTION_COMPLUS by COM
    if (hr == EXCEPTION_COMPLUS)
    {
        GCX_PREEMP();

        // we recognize the ones thrown by the debugger by QI'ing the IErrorInfo for a special IID
        ReleaseHolder<IErrorInfo> pErrorInfo;
        if (SafeGetErrorInfo(&pErrorInfo) == S_OK)
        {
            ReleaseHolder<IUnknown> pUnk;
            if (SafeQueryInterface(pErrorInfo, IID_IFuncEvalAbort, &pUnk) == S_OK)
            {
                // QI succeeded, this is a func eval abort
                return pErrorInfo.Extract();
            }
            else
            {
                // QI failed, put the IErrorInfo back
                SetErrorInfo(0, pErrorInfo);
            }
        }
    }

    return NULL;
}

// Rethrows the exception injected by the debugger to abort a func eval, or does nothing if there is no such exception.
static void CheckForFuncEvalAbort(HRESULT hr)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    IErrorInfo *pErrorInfo = CheckForFuncEvalAbortNoThrow(hr);
    if (pErrorInfo != NULL)
    {
        // COMPlusThrowHR internally releases the pErrorInfo
        COMPlusThrowHR(hr, pErrorInfo);
    }
}

//+-------------------------------------------------------------------------
//
//  Function: STDAPI_(LPSTREAM) CreateMemStm(DWORD cb, BYTE** ppBuf))
//  Create a stream in the memory
//
STDAPI_(LPSTREAM) CreateMemStm(DWORD cb, BYTE** ppBuf)
{
    CONTRACT(LPSTREAM)
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
        INJECT_FAULT(CONTRACT_RETURN NULL);
        PRECONDITION(CheckPointer(ppBuf, NULL_OK));
        PRECONDITION(CheckPointer(ppBuf, NULL_OK));
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    LPSTREAM        pstm = NULL;

    BYTE* pMem = new(nothrow) BYTE[cb];
    if (pMem)
    {
        HRESULT hr = CInMemoryStream::CreateStreamOnMemory(pMem, cb, &pstm, TRUE);
        _ASSERTE(hr == S_OK || pstm == NULL);
    }

    if(ppBuf)
        *ppBuf = pMem;

    RETURN pstm;
}

//=====================================================================
// HRESULT wCoMarshalInterThreadInterfaceInStream
HRESULT wCoMarshalInterThreadInterfaceInStream(REFIID riid,
                                       LPUNKNOWN pUnk,
                                       LPSTREAM *ppStm)
{
#ifdef PLATFORM_CE
    return E_NOTIMPL;
#endif // !PLATFORM_CE

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pUnk));
        PRECONDITION(CheckPointer(ppStm));
    }
    CONTRACTL_END;

    HRESULT hr;
    LPSTREAM pStm = NULL;

    DWORD mshlFlgs = MSHLFLAGS_NORMAL;

    ULONG lSize;
    hr = CoGetMarshalSizeMax(&lSize, IID_IUnknown, pUnk, MSHCTX_INPROC, NULL, mshlFlgs);

    if (hr == S_OK)
    {
        // Create a stream
        pStm = CreateMemStm(lSize, NULL);

        if (pStm != NULL)
        {
            // Marshal the interface into the stream TABLE STRONG
            hr = CoMarshalInterface(pStm, riid, pUnk, MSHCTX_INPROC, NULL, mshlFlgs);
        }
        else
        {
            hr = E_OUTOFMEMORY;
        }
    }

    if (SUCCEEDED(hr))
    {
        // Reset the stream to the beginning
        LARGE_INTEGER li;
        LISet32(li, 0);
        ULARGE_INTEGER li2;
        pStm->Seek(li, STREAM_SEEK_SET, &li2);

        // Set the return value
        *ppStm = pStm;
    }
    else
    {
        // Cleanup if failure
        SafeReleasePreemp(pStm);
        *ppStm = NULL;
    }

    // Return the result
    return hr;
}

//================================================================
// Struct passed in to DoCallback.
struct CtxEntryEnterContextCallbackData
{
    PFNCTXCALLBACK m_pUserCallbackFunc;
    LPVOID         m_pUserData;
    LPVOID         m_pCtxCookie;
    HRESULT        m_UserCallbackHR;
};

//================================================================
// Static members.
CtxEntryCache* CtxEntryCache::s_pCtxEntryCache = NULL;

CtxEntryCache::CtxEntryCache()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_Lock.Init(LOCK_COMCTXENTRYCACHE);
    LockOwner lock = {&m_Lock, IsOwnerOfSpinLock};
}

CtxEntryCache::~CtxEntryCache()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    for (SHash<CtxEntryCacheTraits>::Iterator it = m_CtxEntryHash.Begin(); it != m_CtxEntryHash.End(); it++)
    {
        CtxEntry *pCtxEntry = (CtxEntry *)*it;
        _ASSERTE(pCtxEntry);
        LPVOID CtxCookie = pCtxEntry->GetCtxCookie();
        m_CtxEntryHash.Remove(CtxCookie);

        LOG((LF_INTEROP, LL_INFO100, "Leaked CtxEntry %8.8x with CtxCookie %8.8x, ref count %d\n", pCtxEntry, pCtxEntry->GetCtxCookie(), pCtxEntry->m_dwRefCount));
        pCtxEntry->m_dwRefCount = 0;
        delete pCtxEntry;
    }
}


void CtxEntryCache::Init()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;

        // This should never be called more than once.
        PRECONDITION(NULL == s_pCtxEntryCache);
    }
    CONTRACTL_END;

    // Allocate the one and only instance of the context entry cache.
    s_pCtxEntryCache = new CtxEntryCache();
}

CtxEntryCache* CtxEntryCache::GetCtxEntryCache()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(s_pCtxEntryCache));
    }
    CONTRACTL_END;

    return s_pCtxEntryCache;
}

CtxEntry* CtxEntryCache::CreateCtxEntry(LPVOID pCtxCookie, Thread * pSTAThread)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    CtxEntry * pCtxEntry = NULL;

    // If we don't already have a context entry for the context cookie,
    // we need to create one.
    NewHolder<CtxEntry> pNewCtxEntry = new CtxEntry(pCtxCookie, pSTAThread);
    // tiggers GC, can't happen when we hold spin lock
    pNewCtxEntry->Init();

    {
        TAKE_SPINLOCK_AND_DONOT_TRIGGER_GC(&m_Lock);

        // double check for race
        pCtxEntry = m_CtxEntryHash.Lookup(pCtxCookie);
        if (pCtxEntry == NULL)
        {
            // We successfully allocated and initialized the entry.
            m_CtxEntryHash.Add(pNewCtxEntry);
            pCtxEntry = pNewCtxEntry.Extract();
        }
        // We must have an entry now; we need to addref it before
        // we leave the lock.
        pCtxEntry->AddRef ();
    }

    return pCtxEntry;
}

CtxEntry* CtxEntryCache::FindCtxEntry(LPVOID pCtxCookie, Thread *pThread)
{
    CtxEntry *pCtxEntry = NULL;
    Thread *pSTAThread = NULL;

    CONTRACT (CtxEntry*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pCtxCookie));
        POSTCONDITION(CheckPointer(RETVAL));
        POSTCONDITION(pCtxCookie == pCtxEntry->GetCtxCookie());
        POSTCONDITION(pSTAThread == pCtxEntry->GetSTAThread());
    }
    CONTRACT_END;

    // Find our STA (if any)
    if (pThread->GetApartment() == Thread::AS_InSTA)
    {
        // We are in an STA thread.  But we may be in a NA context, so do an extra
        // check for that case.
        BOOL fNAContext;

        // try the simple cache on Thread first
        if (pCtxCookie != pThread->GetLastSTACtxCookie(&fNAContext))
        {
            APTTYPE type;
            fNAContext = (SUCCEEDED(GetCurrentApartmentTypeNT5((IObjectContext *)pCtxCookie, &type)) && type == APTTYPE_NA);
            pThread->SetLastSTACtxCookie(pCtxCookie, fNAContext);
        }

        if (!fNAContext)
            pSTAThread = pThread;
    }

    ASSERT (GetThreadNULLOk ());
    BOOL bFound = FALSE;

    ACQUIRE_SPINLOCK_NO_HOLDER(&m_Lock);
    {
        // Try to find a context entry for the context cookie.
        pCtxEntry = m_CtxEntryHash.Lookup(pCtxCookie);
        if (pCtxEntry)
        {
            // We must have an entry now; we need to addref it before
            // we leave the lock.
            pCtxEntry->AddRef ();
            bFound = TRUE;
        }
    }
    RELEASE_SPINLOCK_NO_HOLDER(&m_Lock);

    if (!bFound)
    {
        pCtxEntry = CreateCtxEntry(pCtxCookie, pSTAThread);
    }

    // Returned the found or allocated entry.
    RETURN pCtxEntry;
}


void CtxEntryCache::TryDeleteCtxEntry(LPVOID pCtxCookie)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pCtxCookie));
    }
    CONTRACTL_END;

    BOOL bDelete = FALSE;
    CtxEntry *pCtxEntry = NULL;

    {
        TAKE_SPINLOCK_AND_DONOT_TRIGGER_GC(&m_Lock);

        // Try to find a context entry for the context cookie.
        pCtxEntry = m_CtxEntryHash.Lookup(pCtxCookie);
        if (pCtxEntry)
        {
            // If the ref count of the context entry is still 0, then we can
            // remove the ctx entry and delete it.
            if (pCtxEntry->m_dwRefCount == 0)
            {
                // First remove the context entry from the list.
                m_CtxEntryHash.Remove(pCtxCookie);

                // We need to unlock the context entry cache before we delete the
                // context entry since this can cause release to be called on
                // an IP which can cause us to re-enter the runtime thus causing a
                // deadlock.
                // We can now safely delete the context entry.
                bDelete = TRUE;
            }
        }
    }

    if (bDelete)
    {
        delete pCtxEntry;
    }
}

//================================================================
// Get the RCW associated with this IUnkEntry
// We assert inside Init that this IUnkEntry is indeed within a RCW
RCW *IUnkEntry::GetRCW()
{
    LIMITED_METHOD_CONTRACT;

    return (RCW *) (((LPBYTE) this) - offsetof(RCW, m_UnkEntry));
}

//================================================================
// Initialize the entry
void IUnkEntry::Init(
    IUnknown *pUnk,
    BOOL bIsFreeThreaded,
    Thread *pThread
    DEBUGARG(RCW *pRCW)
    )
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pUnk));
        INDEBUG(PRECONDITION(CheckPointer(pRCW));)
    }
    CONTRACTL_END;

    // Make sure this IUnkEntry is part of a RCW so that we can get back to the RCW through offset
    // if we have to
    _ASSERTE(((LPBYTE)pRCW) + offsetof(RCW, m_UnkEntry) == (LPBYTE) this);

    // Find our context cookie
    LPVOID pCtxCookie = GetCurrentCtxCookie();
    _ASSERTE(pCtxCookie);

    // Set up IUnkEntry's state.
    if (bIsFreeThreaded)
        m_pCtxEntry = NULL;
    else
        m_pCtxEntry = CtxEntryCache::GetCtxEntryCache()->FindCtxEntry(pCtxCookie, pThread);

    m_pUnknown = pUnk;
    m_pCtxCookie = pCtxCookie;
    m_pStream = NULL;

    // Sanity check this IUnkEntry.
    CheckValidIUnkEntry();
}

//================================================================
// Release the interface pointer held by the IUnkEntry.
VOID IUnkEntry::ReleaseInterface(RCW *pRCW)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    if (g_fProcessDetach)
    {
        // The Release call is unsafe if the process is going away (calls into
        // DLLs we don't know are even mapped).
        LogInteropLeak(this);
    }
    else
    {
        // now release the IUnknown that we hold
        if ((m_pUnknown != 0) && (m_pUnknown != (IUnknown *)0xBADF00D))
        {
            ULONG cbRef = SafeReleasePreemp(m_pUnknown, pRCW);
            LogInteropRelease(m_pUnknown, cbRef, "IUnkEntry::Free: Releasing the held ref");
        }

        // mark the entry as dead
        m_pUnknown = (IUnknown *)0xBADF00D;
    }
}

//================================================================
// Free the IUnknown entry. ReleaseInterface must have been called.
VOID IUnkEntry::Free()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(g_fProcessDetach || m_pUnknown == (IUnknown *)0xBADF00D);
    }
    CONTRACTL_END;

    // Log the de-allocation of the IUnknown entry.
    LOG((LF_INTEROP, LL_INFO10000, "IUnkEntry::Free called for context 0x%08X, to release entry with m_pUnknown %p, on thread %p\n", m_pCtxCookie, m_pUnknown, GetThreadNULLOk()));

    if (g_fProcessDetach)
    {
        IStream *pOldStream = m_pStream;
        if (InterlockedExchangeT(&m_pStream, NULL) == pOldStream)
            SafeReleasePreemp(pOldStream);
    }
    else
    {
        IStream *pStream = m_pStream;
        m_pStream = NULL;

        // This will release the stream, object in the stream and the memory on which the stream was created
        if (pStream)
            SafeReleaseStream(pStream);

    }

    // Release the ref count we have on the CtxEntry.
    CtxEntry *pEntry = GetCtxEntry();
    if (pEntry)
    {
        pEntry->Release();
        m_pCtxEntry = NULL;
    }
}

//================================================================
// Get IUnknown for the current context from IUnkEntry
IUnknown* IUnkEntry::GetIUnknownForCurrContext(bool fNoAddRef)
{
    CONTRACT (IUnknown*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL, (fNoAddRef ? NULL_OK : NULL_NOT_OK)));
    }
    CONTRACT_END;

    IUnknown* pUnk = NULL;
    LPVOID pCtxCookie = GetCurrentCtxCookie();
    _ASSERTE(pCtxCookie);

    CheckValidIUnkEntry();

    if (m_pCtxCookie == pCtxCookie || IsFreeThreaded())
    {
        pUnk = GetRawIUnknown_NoAddRef();

        if (!fNoAddRef)
        {
            RCW_VTABLEPTR(GetRCW());
            ULONG cbRef = SafeAddRef(pUnk);
            LogInteropAddRef(pUnk, cbRef, "IUnkEntry::GetIUnknownForCurrContext: Addref pUnk, passing ref to caller");
        }
    }

    if (pUnk == NULL && !fNoAddRef)
        pUnk = UnmarshalIUnknownForCurrContext();

    RETURN pUnk;
}

//================================================================
// Unmarshal IUnknown for the current context from IUnkEntry
IUnknown* IUnkEntry::UnmarshalIUnknownForCurrContext()
{
    CONTRACT (IUnknown*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(!IsFreeThreaded());
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    HRESULT     hrCDH               = S_OK;
    IUnknown*   pUnk                = NULL;
    BOOL        fRetry              = TRUE;
    BOOL        fUnmarshalFailed    = FALSE;
    BOOL        fCallHelper         = FALSE;

    CheckValidIUnkEntry();

    _ASSERTE(GetCtxEntry() != NULL);


    if(IsMarshalingInhibited() && (m_pCtxCookie != GetCurrentCtxCookie()))
    {
        // We want to use an interface in a different context but it can't be marshalled.
        LOG((LF_INTEROP, LL_INFO100, "IUnkEntry::GetIUnknownForCurrContext failed as the COM object has inhibited marshaling"));
        COMPlusThrow(kInvalidCastException, IDS_EE_CANNOTCAST_NOMARSHAL);
    }

    // Make sure we are in preemptive GC mode before we call out to COM.
    GCX_PREEMP();

    // Need to synchronize
    while (fRetry)
    {
        // Marshal the interface to the stream if it hasn't been done yet.
        if (m_pStream == NULL)
        {
            // If context transition failed, we'll return a failure HRESULT
            // Otherwise, we return S_OK but m_pStream will stay being NULL
            hrCDH = MarshalIUnknownToStreamCallback(this);
            CheckForFuncEvalAbort(hrCDH);
        }

        if (TryUpdateEntry())
        {
            // If the interface is not marshalable or if we failed to
            // enter the context, then we don't have any choice but to
            // use the raw IP.
            if (m_pStream == NULL)
            {
                // We retrieved an IP so stop retrying.
                fRetry = FALSE;

                // Give out this IUnknown we are holding
                pUnk = GetRawIUnknown_NoAddRef();

                RCW_VTABLEPTR(GetRCW());
                ULONG cbRef = SafeAddRefPreemp(pUnk);

                LogInteropAddRef(pUnk, cbRef, "UnmarshalIUnknownForCurrContext handing out raw IUnknown");
            }
            else
            {
                // we got control for this entry
                // GetInterface for the current context
                HRESULT hr;
                hr = CoUnmarshalInterface(m_pStream, IID_IUnknown, (void **)&pUnk);

                // If the objref in the stream times out, we need to go an marshal into the
                // stream once again.
                if (FAILED(hr))
                {
                    _ASSERTE(m_pStream);

                    CheckForFuncEvalAbort(hr);

                    // This should release the stream, object in the stream and the memory on which the stream was created
                    SafeReleaseStream(m_pStream);
                    m_pStream = NULL;

                    // If unmarshal failed twice, then bail out.
                    if (fUnmarshalFailed)
                    {
                        fRetry = FALSE;

                        // Handing out m_pUnknown in this case would be incorrect. We should fix other places that are doing the same thing in Dev10
                        // To minimize code changes, throwing E_NOINTERFACE instead
                        COMPlusThrowHR(E_NOINTERFACE);
                    }

                    // Remember we failed to unmarshal.
                    fUnmarshalFailed = TRUE;
                }
                else
                {
                    // Reset the stream to the beginning
                    LARGE_INTEGER li;
                    LISet32(li, 0);
                    ULARGE_INTEGER li2;
                    m_pStream->Seek(li, STREAM_SEEK_SET, &li2);

                    // Marshal the interface into the stream with appropriate flags
                    hr = CoMarshalInterface(m_pStream,
                        IID_IUnknown, pUnk, MSHCTX_INPROC, NULL, MSHLFLAGS_NORMAL);

                    if (FAILED(hr))
                    {
                        CheckForFuncEvalAbort(hr);

                        // The proxy is no longer valid. This sometimes manifests itself by
                        // a failure during re-marshaling it to the stream. When this happens,
                        // we need to release the pUnk we extracted and the stream and try to
                        // re-create the stream. We don't want to release the stream data since
                        // we already extracted the proxy from the stream and released it.
                        RCW_VTABLEPTR(GetRCW());
                        SafeReleasePreemp(pUnk);

                        SafeReleasePreemp(m_pStream);
                        m_pStream = NULL;
                    }
                    else
                    {
                        // Reset the stream to the beginning
                        LISet32(li, 0);
                        m_pStream->Seek(li, STREAM_SEEK_SET, &li2);

                        // We managed to unmarshal the IP from the stream, stop retrying.
                        fRetry = FALSE;
                    }
                }
            }

            // Done with the entry.
            EndUpdateEntry();
        }
        else
        {
            //================================================================
            // We can potentially collide with the COM+ activity lock so spawn off
            // another call that does its stream marshalling on the stack without
            // the need to do locking.
            fCallHelper = TRUE;
            fRetry = FALSE;
        }
    }

    if (fCallHelper)
    {
        // If we hit a collision earlier, spawn off helper that repeats this operation without locking.
        pUnk = UnmarshalIUnknownForCurrContextHelper();
    }

    RETURN pUnk;
}

//================================================================
// Release the stream. This will force UnmarshalIUnknownForCurrContext to transition
// into the context that owns the IP and re-marshal it to the stream.
void IUnkEntry::ReleaseStream()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // This should release the stream, object in the stream and the memory on which the stream was created
    if (m_pStream)
    {
        SafeReleaseStream(m_pStream);
        m_pStream = NULL;
    }
}

// Indicates if the COM component being wrapped by the IUnkEntry aggregates the FTM
bool IUnkEntry::IsFreeThreaded()
{
    LIMITED_METHOD_CONTRACT;
    return GetRCW()->IsFreeThreaded();
}

// Indicates if the COM component being wrapped by the IUnkEntry implements INoMashal.
bool IUnkEntry::IsMarshalingInhibited()
{
    LIMITED_METHOD_CONTRACT;
    return GetRCW()->IsMarshalingInhibited();
}

// Helper function to marshal the IUnknown pointer to the stream.
static HRESULT MarshalIUnknownToStreamHelper(IUnknown * pUnknown, IStream ** ppStream)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    IStream *pStream = NULL;

    GCX_PREEMP();

    // ensure we register this cookie
    HRESULT hr = wCoMarshalInterThreadInterfaceInStream(IID_IUnknown, pUnknown, &pStream);

    if ((hr == REGDB_E_IIDNOTREG) ||
        (hr == E_FAIL) ||
        (hr == E_NOINTERFACE) ||
        (hr == E_INVALIDARG) ||
        (hr == E_UNEXPECTED))
    {
        // Interface is not marshallable.
        pStream = NULL;
        hr      = S_OK;
    }

    *ppStream = pStream;

    return hr;
}

//================================================================
struct StreamMarshalData
{
    IUnkEntry * m_pUnkEntry;
    IStream * m_pStream;
};
// Fix for if the lock is held
HRESULT IUnkEntry::MarshalIUnknownToStreamCallback2(LPVOID pData)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pData));
        PRECONDITION(g_fProcessDetach == FALSE);
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    StreamMarshalData *psmd = reinterpret_cast<StreamMarshalData *>(pData);

    // This should never be called during process detach.

    hr = psmd->m_pUnkEntry->HRCheckValidIUnkEntry();
    if (hr != S_OK)
    {
        // Interface not marshallable
        // We'll know marshaling failed because m_pStream == NULL
        return S_OK;
    }

    LPVOID pCurrentCtxCookie = GetCurrentCtxCookie();
    _ASSERTE(pCurrentCtxCookie);

    if (pCurrentCtxCookie == psmd->m_pUnkEntry->m_pCtxCookie)
    {
        // We are in the right context marshal the IUnknown to the
        // stream directly.
        hr = MarshalIUnknownToStreamHelper(psmd->m_pUnkEntry->m_pUnknown, &psmd->m_pStream);
    }
    else
    {
        // Transition into the context to marshal the IUnknown to
        // the stream.
        _ASSERTE(psmd->m_pUnkEntry->GetCtxEntry() != NULL);
        hr = psmd->m_pUnkEntry->GetCtxEntry()->EnterContext(MarshalIUnknownToStreamCallback2, psmd);
    }

    return hr;
}

//================================================================
// Unmarshal IUnknown for the current context if the lock is held
IUnknown* IUnkEntry::UnmarshalIUnknownForCurrContextHelper()
{
    CONTRACT (IUnknown*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(!IsFreeThreaded());
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    HRESULT hrCDH = S_OK;
    IUnknown * pUnk = NULL;
    SafeComHolder<IStream> spStream;

    CheckValidIUnkEntry();

    // Make sure we are in preemptive GC mode before we call out to COM.
    GCX_PREEMP();

    // Marshal the interface to the stream. Any call to this function
    // would be from another apartment so marshalling is needed.
    StreamMarshalData smd = {this, NULL};

    // If context transition failed, we'll return a failure HRESULT
    // Otherwise, we return S_OK but m_pStream will stay being NULL
    hrCDH = MarshalIUnknownToStreamCallback2(&smd);

    spStream = smd.m_pStream;
    smd.m_pStream = NULL;

    CheckForFuncEvalAbort(hrCDH);

    // If the interface is not marshalable or if we failed to
    // enter the context, then we don't have any choice but to
    // use the raw IP.
    if (spStream == NULL)
    {
        // Give out this IUnknown we are holding
        pUnk = GetRawIUnknown_NoAddRef();

        RCW_VTABLEPTR(GetRCW());
        ULONG cbRef = SafeAddRefPreemp(pUnk);

        LogInteropAddRef(pUnk, cbRef, "UnmarshalIUnknownForCurrContext handing out raw IUnknown");
    }
    else
    {
        // we got control for this entry
        // GetInterface for the current context
        HRESULT hr;
        hr = CoUnmarshalInterface(spStream, IID_IUnknown, reinterpret_cast<void**>(&pUnk));
        spStream.Release();

        if (FAILED(hr))
        {
            CheckForFuncEvalAbort(hr);

            // Give out this IUnknown we are holding
            pUnk = GetRawIUnknown_NoAddRef();

            RCW_VTABLEPTR(GetRCW());
            ULONG cbRef = SafeAddRefPreemp(pUnk);

            LogInteropAddRef(pUnk, cbRef, "UnmarshalIUnknownForCurrContext handing out raw IUnknown");
        }
    }

    RETURN pUnk;
}

//================================================================
// Callback called to marshal the IUnknown into a stream lazily.
HRESULT IUnkEntry::MarshalIUnknownToStreamCallback(LPVOID pData)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pData));
        PRECONDITION(g_fProcessDetach == FALSE);
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    IUnkEntry *pUnkEntry = (IUnkEntry *)pData;

    // This should never be called during process detach.

    hr = pUnkEntry->HRCheckValidIUnkEntry();
    if (hr != S_OK)
    {
        // Interface not marshallable
        // We'll know marshaling failed because m_pStream == NULL
        return S_OK;
    }

    LPVOID pCurrentCtxCookie = GetCurrentCtxCookie();
    _ASSERTE(pCurrentCtxCookie);

    if (pCurrentCtxCookie == pUnkEntry->m_pCtxCookie)
    {
        // We are in the right context marshal the IUnknown to the
        // stream directly.
        hr = pUnkEntry->MarshalIUnknownToStream();
    }
    else
    {
        _ASSERTE(pUnkEntry->GetCtxEntry() != NULL);

        // Transition into the context to marshal the IUnknown to
        // the stream.
        hr = pUnkEntry->GetCtxEntry()->EnterContext(MarshalIUnknownToStreamCallback, pUnkEntry);
    }

    return hr;
}

//================================================================
// Helper function to determine if a COM component aggregates the
// FTM.
bool IUnkEntry::IsComponentFreeThreaded(IUnknown *pUnk)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pUnk));
    }
    CONTRACTL_END;

    // First see if the object implements the IAgileObject marker interface
    SafeComHolderPreemp<IAgileObject> pAgileObject;
    HRESULT hr = SafeQueryInterfacePreemp(pUnk, IID_IAgileObject, (IUnknown**)&pAgileObject);
    LogInteropQI(pUnk, IID_IAgileObject, hr, "IUnkEntry::IsComponentFreeThreaded: QI for IAgileObject");

    if (SUCCEEDED(hr))
    {
        return true;
    }
    else
    {
        SafeComHolderPreemp<IMarshal> pMarshal = NULL;

        // If not, then we can try to determine if the component aggregates the FTM via IMarshal.
        hr = SafeQueryInterfacePreemp(pUnk, IID_IMarshal, (IUnknown **)&pMarshal);
        LogInteropQI(pUnk, IID_IMarshal, hr, "IUnkEntry::IsComponentFreeThreaded: QI for IMarshal");
        if (SUCCEEDED(hr))
        {
            CLSID clsid;

            // The COM component implements IMarshal so we now check to see if the un-marshal class
            // for this IMarshal is the FTM's un-marshaler.
            hr = pMarshal->GetUnmarshalClass(IID_IUnknown, NULL, MSHCTX_INPROC, NULL, MSHLFLAGS_NORMAL, &clsid);
            if (SUCCEEDED(hr) && clsid == CLSID_InProcFreeMarshaler)
            {
                // The un-marshaler is indeed the unmarshaler for the FTM so this object
                // is free threaded.
                return true;
            }
        }
    }

    return false;
}

//================================================================
// Helper function to marshal the IUnknown pointer to the stream.
HRESULT IUnkEntry::MarshalIUnknownToStream()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;

        // This must always be called in the right context.
        PRECONDITION(m_pCtxCookie == GetCurrentCtxCookie());
    }
    CONTRACTL_END;

    IStream *pStream = NULL;

    GCX_PREEMP();

    HRESULT hr = S_OK;

    // ensure we register this cookie
    IUnknown *pUnk = m_pUnknown;
    if (pUnk == (IUnknown *)0xBADF00D)
    {
        hr = COR_E_INVALIDCOMOBJECT;
    }
    else
    {
        hr = wCoMarshalInterThreadInterfaceInStream(IID_IUnknown, pUnk, &pStream);

        if ((hr == REGDB_E_IIDNOTREG) ||
            (hr == E_FAIL) ||
            (hr == E_NOINTERFACE) ||
            (hr == E_INVALIDARG) ||
            (hr == E_UNEXPECTED))
        {
            // Interface is not marshallable.
            pStream = NULL;
            hr      = S_OK;
        }
    }

    // Try to set the stream in the IUnkEntry. If another thread already set it,
    // then we need to release the stream we just set up.
    if (InterlockedCompareExchangeT(&m_pStream, pStream, NULL) != NULL)
        SafeReleaseStream(pStream);

    return hr;
}


// Method to try to start updating the entry.
bool IUnkEntry::TryUpdateEntry()
{
    WRAPPER_NO_CONTRACT;

    CtxEntry *pOldEntry = m_pCtxEntry;
    if (((DWORD_PTR)pOldEntry & 1) == 0)
    {
        CtxEntry *pNewEntry = (CtxEntry *)((DWORD_PTR)pOldEntry | 1);
        return (InterlockedExchangeT(&m_pCtxEntry, pNewEntry) == pOldEntry);
    }
    return false;
}

// Method to end updating the entry.
VOID IUnkEntry::EndUpdateEntry()
{
    LIMITED_METHOD_CONTRACT;

    CtxEntry *pOldEntry = m_pCtxEntry;

    // we should hold the lock
    _ASSERTE(((DWORD_PTR)pOldEntry & 1) == 1);

    CtxEntry *pNewEntry = (CtxEntry *)((DWORD_PTR)pOldEntry & ~1);

    // and it's us who resets the bit
    VERIFY(InterlockedExchangeT(&m_pCtxEntry, pNewEntry) == pOldEntry);
}


// Initialize the entry, returns true on success (i.e. the entry was free).
bool InterfaceEntry::Init(MethodTable* pMT, IUnknown* pUnk)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // It is important the fields be set in this order.
    if (InterlockedCompareExchangeT(&m_pUnknown, pUnk, NULL) == NULL)
    {
        m_pMT = (IE_METHODTABLE_PTR)pMT;
        return true;
    }
    return false;
}

// Helper to determine if the entry is free.
BOOL InterfaceEntry::IsFree()
{
    LIMITED_METHOD_CONTRACT;
    return m_pUnknown.Load() == NULL;
}

void InterfaceEntry::Free()
{
    LIMITED_METHOD_CONTRACT;

    // We use the m_pUnknown field to synchronize access to the entry so that's the only
    // one we need to reset. After all, the set of interfaces that the object is known to
    // support is one of the most important debugging cues so let's keep m_pMT intact.
    m_pUnknown.Store(NULL);
}

//================================================================
// Constructor for the context entry.
CtxEntry::CtxEntry(LPVOID pCtxCookie, Thread *pSTAThread)
: m_pCtxCookie(pCtxCookie)
, m_pObjCtx(NULL)
, m_dwRefCount(0)
, m_pSTAThread(pSTAThread)
{
    WRAPPER_NO_CONTRACT;
}

//================================================================
// Destructor for the context entry.
CtxEntry::~CtxEntry()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(m_dwRefCount == 0);
    }
    CONTRACTL_END;

    // If the context is a valid context then release it.
    if (m_pObjCtx && !g_fProcessDetach)
    {
        SafeRelease(m_pObjCtx);
        m_pObjCtx = NULL;
    }

    // Set the context cookie to 0xBADF00D to indicate the current context
    // has been deleted.
    m_pCtxCookie = (LPVOID)0xBADF00D;
}

//================================================================
// Initialization method for the context entry.
VOID CtxEntry::Init()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());

        // Make sure COM has been started
        PRECONDITION(g_fComStarted == TRUE);
    }
    CONTRACTL_END;

    // Retrieve the IObjectContext.
    HRESULT hr = GetCurrentObjCtx(&m_pObjCtx);

    // In case the call to GetCurrentObjCtx fails (which should never really happen)
    // we will throw an exception.
    if (FAILED(hr))
        COMPlusThrowHR(hr);
}


// Add a reference to the CtxEntry.
DWORD CtxEntry::AddRef()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    ULONG cbRef = InterlockedIncrement((LONG*)&m_dwRefCount);
    LOG((LF_INTEROP, LL_INFO100, "CtxEntry::Addref %8.8x with %d\n", this, cbRef));
    return cbRef;
}


//================================================================
// Method to decrement the ref count of the context entry.
DWORD CtxEntry::Release()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(m_dwRefCount > 0);
    }
    CONTRACTL_END;

    LPVOID pCtxCookie = m_pCtxCookie;

    LONG cbRef = InterlockedDecrement((LONG*)&m_dwRefCount);
    LOG((LF_INTEROP, LL_INFO100, "CtxEntry::Release %8.8x with %d\n", this, cbRef));

    // If the ref count falls to 0, try and delete the ctx entry.
    // This might not end up deleting it if another thread tries to
    // retrieve this ctx entry at the same time this one tries
    // to delete it.
    if (cbRef == 0)
        CtxEntryCache::GetCtxEntryCache()->TryDeleteCtxEntry(pCtxCookie);

    // WARNING: The this pointer cannot be used at this point.
    return cbRef;
}

//================================================================
// Method to transition into the context and call the callback
// from within the context.
HRESULT CtxEntry::EnterContext(PFNCTXCALLBACK pCallbackFunc, LPVOID pData)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;

        PRECONDITION(CheckPointer(pCallbackFunc));
        PRECONDITION(CheckPointer(pData));
        // This should not be called if the this context is the current context.
        PRECONDITION(m_pCtxCookie != GetCurrentCtxCookie());
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    // If we are in process detach, we cannot safely try to enter another context
    // since we don't know if OLE32 is still loaded.
    if (g_fProcessDetach)
    {
        LOG((LF_INTEROP, LL_INFO100, "Entering into context 0x08%x has failed since we are in process detach\n", m_pCtxCookie));
        return RPC_E_DISCONNECTED;
    }

    // Make sure we are in preemptive GC mode before we call out to COM.
    GCX_PREEMP();

    // Prepare the information struct passed into the callback.
    CtxEntryEnterContextCallbackData CallbackInfo;
    CallbackInfo.m_pUserCallbackFunc = pCallbackFunc;
    CallbackInfo.m_pUserData = pData;
    CallbackInfo.m_pCtxCookie = m_pCtxCookie;
    CallbackInfo.m_UserCallbackHR = E_FAIL;

    // Retrieve the IContextCallback interface from the IObjectContext.
    SafeComHolderPreemp<IContextCallback> pCallback;
    hr = SafeQueryInterfacePreemp(m_pObjCtx, IID_IContextCallback, (IUnknown**)&pCallback);
    LogInteropQI(m_pObjCtx, IID_IContextCallback, hr, "QI for IID_IContextCallback");
    _ASSERTE(SUCCEEDED(hr) && pCallback);

    // Setup the callback data structure with the callback Args
    ComCallData callBackData;
    callBackData.dwDispid = 0;
    callBackData.dwReserved = 0;
    callBackData.pUserDefined = &CallbackInfo;

    EX_TRY
    {
        hr = ((IContextCallback*)pCallback)->ContextCallback(EnterContextCallback, &callBackData, IID_IEnterActivityWithNoLock, 2, NULL);
    }
    EX_CATCH
    {
        hr = GET_EXCEPTION()->GetHR();
    }
    EX_END_CATCH(SwallowAllExceptions);

    if (FAILED(hr))
    {
        // If the transition failed because of an aborted func eval, simply propagate
        // the HRESULT/IErrorInfo back to the caller as we cannot throw here.
        SafeComHolder<IErrorInfo> pErrorInfo = CheckForFuncEvalAbortNoThrow(hr);
        if (pErrorInfo != NULL)
        {
            LOG((LF_INTEROP, LL_INFO100, "Entering into context 0x08X has failed since the debugger is blocking it\n", m_pCtxCookie));

            // put the IErrorInfo back
            SetErrorInfo(0, pErrorInfo);
        }
        else
        {
            // The context is disconnected so we cannot transition into it.
            LOG((LF_INTEROP, LL_INFO100, "Entering into context 0x08X has failed since the context has disconnected\n", m_pCtxCookie));
        }
    }

    return hr;
}


//================================================================
// Callback function called by DoCallback.
HRESULT __stdcall CtxEntry::EnterContextCallback(ComCallData* pComCallData)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pComCallData));
    }
    CONTRACTL_END;

    // Retrieve the callback data.
    CtxEntryEnterContextCallbackData *pData = (CtxEntryEnterContextCallbackData*)pComCallData->pUserDefined;


    Thread *pThread = GetThreadNULLOk();

    // Make sure the thread has been set before we call the user callback function.
    if (!pThread)
    {
        // huh! we are in the middle of shutdown
        // and there is no way we can add a new thread
        // so let us just return RPC_E_DISCONNECTED
        // look at the pCallBack->DoCallback above
        // to see why we are returning this SCODE
        if(g_fEEShutDown)
            return RPC_E_DISCONNECTED;

        // Otherwise, we need to create a managed thread object for this new thread
        else
        {
            HRESULT hr;
            pThread = SetupThreadNoThrow(&hr);
            if (pThread == NULL)
                return hr;
        }
    }

    // at this point we should be in the right context on NT4,
    // if not then it is possible that the actual apartment state for this
    // thread has changed and we have stale info in our thread or the CtxEntry
    LPVOID pCtxCookie = GetCurrentCtxCookie();
    _ASSERTE(pCtxCookie);
    if (pData->m_pCtxCookie != pCtxCookie)
        return RPC_E_DISCONNECTED;

    // Call the user callback function and store the return value the
    // callback data.
    pData->m_UserCallbackHR = pData->m_pUserCallbackFunc(pData->m_pUserData);

    // Return S_OK to indicate the context transition was successful.
    return S_OK;
}
