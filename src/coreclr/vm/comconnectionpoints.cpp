// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
// File: ComConnectionPoints.cpp
//

// ===========================================================================
// Implementation of the classes used to expose connection points to COM.
// ===========================================================================


#include "common.h"

#include "comconnectionpoints.h"
#include "comcallablewrapper.h"

//------------------------------------------------------------------------------------------
//      Implementation of helper class used to expose connection points
//------------------------------------------------------------------------------------------

ConnectionPoint::ConnectionPoint(ComCallWrapper *pWrap, MethodTable *pEventMT)
: m_pOwnerWrap(pWrap)
, m_pTCEProviderMT(pWrap->GetSimpleWrapper()->GetMethodTable())
, m_pEventItfMT(pEventMT)
, m_Lock(CrstInterop)
, m_apEventMethods(NULL)
, m_NumEventMethods(0)
, m_cbRefCount(0)
, m_pLastInserted(NULL)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pWrap));
        PRECONDITION(CheckPointer(pEventMT));
    }
    CONTRACTL_END;

    // Retrieve the connection IID.
    pEventMT->GetGuid(&m_rConnectionIID, TRUE);

    // Set up the event methods.
    SetupEventMethods();
}

ConnectionPoint::~ConnectionPoint()
{
    WRAPPER_NO_CONTRACT;

    if (m_apEventMethods)
        delete []m_apEventMethods;
}

HRESULT __stdcall ConnectionPoint::QueryInterface(REFIID riid, void** ppv)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(ppv, NULL_OK));
    }
    CONTRACTL_END;

    // Verify the arguments.
    if (!ppv)
        return E_POINTER;

    // Initialize the out parameters.
    *ppv = NULL;

    SetupForComCallHR();

    if (riid == IID_IConnectionPoint)
    {
        *ppv = static_cast<IConnectionPoint*>(this);
    }
    else if (riid == IID_IUnknown)
    {
        *ppv = static_cast<IUnknown*>(this);
    }
    else
    {
        return E_NOINTERFACE;
    }

    ULONG cbRef = SafeAddRefPreemp((IUnknown*)*ppv);
    //@TODO(CLE) AddRef logging that doesn't use QI

    return S_OK;
}

ULONG __stdcall ConnectionPoint::AddRef()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    SetupForComCallHR();

    // The connection point objects share the CCW's ref count.
    return m_pOwnerWrap->AddRef();
}

ULONG __stdcall ConnectionPoint::Release()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    SetupForComCallHR();

    HRESULT hr = S_OK;
    ULONG cbRef = -1;

    BEGIN_EXTERNAL_ENTRYPOINT(&hr)
    {
        // The connection point objects share the CCW's ref count.
        cbRef = m_pOwnerWrap->Release();
    }
    END_EXTERNAL_ENTRYPOINT;

    return cbRef;
}

HRESULT __stdcall ConnectionPoint::GetConnectionInterface(IID *pIID)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pIID, NULL_OK));
    }
    CONTRACTL_END;

    // Verify the arguments.
    if (!pIID)
        return E_POINTER;

    // Initialize the out parameters.
    *pIID = GUID_NULL;

    SetupForComCallHR();

    *pIID = m_rConnectionIID;
    return S_OK;
}

HRESULT __stdcall ConnectionPoint::GetConnectionPointContainer(IConnectionPointContainer **ppCPC)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(ppCPC, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    // Verify the arguments.
    if (!ppCPC)
        return E_POINTER;

    // Initialize the out parameters.
    *ppCPC = NULL;

    SetupForComCallHR();

    BEGIN_EXTERNAL_ENTRYPOINT(&hr)
    {
        GCX_COOP_THREAD_EXISTS(GET_THREAD());

        *ppCPC = GetConnectionPointContainerWorker();
    }
    END_EXTERNAL_ENTRYPOINT;

    return hr;
}

HRESULT __stdcall ConnectionPoint::Advise(IUnknown *pUnk, DWORD *pdwCookie)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pUnk, NULL_OK));
        PRECONDITION(CheckPointer(pdwCookie, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    // Verify the arguments.
    if (!pUnk || !pdwCookie)
        return E_POINTER;

    // Initialize the out parameters.
    *pdwCookie = NULL;

    SetupForComCallHR();

    BEGIN_EXTERNAL_ENTRYPOINT(&hr)
    {
        GCX_COOP_THREAD_EXISTS(GET_THREAD());

        AdviseWorker(pUnk, pdwCookie);
    }
    END_EXTERNAL_ENTRYPOINT;

    return hr;
}

HRESULT __stdcall ConnectionPoint::Unadvise(DWORD dwCookie)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    // Verify the arguments.
    if (dwCookie == 0)
        return CONNECT_E_NOCONNECTION;

    SetupForComCallHR();

    BEGIN_EXTERNAL_ENTRYPOINT(&hr)
    {
        GCX_COOP_THREAD_EXISTS(GET_THREAD());

        UnadviseWorker(dwCookie);
    }
    END_EXTERNAL_ENTRYPOINT;

    return hr;
}

HRESULT __stdcall ConnectionPoint::EnumConnections(IEnumConnections **ppEnum)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(ppEnum, NULL_OK));
    }
    CONTRACTL_END;

    // Verify the arguments.
    if (!ppEnum)
        return E_POINTER;

    // Initialize the out parameters.
    *ppEnum = NULL;

    SetupForComCallHR();

    ConnectionEnum *pConEnum = new(nothrow) ConnectionEnum(this);
    if (!pConEnum)
        return E_OUTOFMEMORY;

    // Retrieve the IEnumConnections interface. This cannot fail.
    HRESULT hr = SafeQueryInterfacePreemp((IUnknown*)pConEnum, IID_IEnumConnections, (IUnknown**)ppEnum);
    LogInteropQI((IUnknown*)pConEnum, IID_IEnumConnections, hr, "ConnectionPoint::EnumConnections: QIing for IID_IEnumConnections");
    _ASSERTE(hr == S_OK);

    return hr;
}

IConnectionPointContainer *ConnectionPoint::GetConnectionPointContainerWorker()
{
    CONTRACT(IConnectionPointContainer*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    // Retrieve the IConnectionPointContainer from the owner wrapper.
    RETURN (IConnectionPointContainer*)
        ComCallWrapper::GetComIPFromCCW(m_pOwnerWrap, IID_IConnectionPointContainer, NULL);
}

void ConnectionPoint::AdviseWorker(IUnknown *pUnk, DWORD *pdwCookie)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pUnk));
        PRECONDITION(CheckPointer(pdwCookie));
    }
    CONTRACTL_END;

    SafeComHolder<IUnknown> pEventItf = NULL;
    HRESULT hr;

    // Make sure we have a pointer to the interface and not to another IUnknown.
    hr = SafeQueryInterface(pUnk, m_rConnectionIID, &pEventItf );
    LogInteropQI(pUnk, m_rConnectionIID, hr, "ConnectionPoint::AdviseWorker: QIing for correct interface");

    if (FAILED(hr) || !pEventItf)
        COMPlusThrowHR(CONNECT_E_CANNOTCONNECT);

    COMOBJECTREF pEventItfObj = NULL;
    OBJECTREF pTCEProviderObj = NULL;

    GCPROTECT_BEGIN(pEventItfObj)
    GCPROTECT_BEGIN(pTCEProviderObj)
    {
        // Create a COM+ object ref to wrap the event interface.
        GetObjectRefFromComIP((OBJECTREF*)&pEventItfObj, pUnk, NULL);
        IfNullThrow(pEventItfObj);

        // Get the TCE provider COM+ object from the wrapper
        pTCEProviderObj = m_pOwnerWrap->GetObjectRef();

        for (int cEventMethod = 0; cEventMethod < m_NumEventMethods; cEventMethod++)
        {
            // If the managed object supports the event that call the AddEventX method.
            if (m_apEventMethods[cEventMethod].m_pEventMethod)
                InvokeProviderMethod( pTCEProviderObj, (OBJECTREF) pEventItfObj, m_apEventMethods[cEventMethod].m_pAddMethod, m_apEventMethods[cEventMethod].m_pEventMethod );
        }

        // Allocate the object handle and the connection cookie.
        OBJECTHANDLEHolder phndEventItfObj = GetAppDomain()->CreateHandle((OBJECTREF)pEventItfObj);
        ConnectionCookieHolder pConCookie = ConnectionCookie::CreateConnectionCookie(phndEventItfObj);

        // pConCookie owns the handle now and will destroy it on exception
        phndEventItfObj.SuppressRelease();

        // Add the connection cookie to the list.
        InsertWithLock(pConCookie);

        // Everything went ok so hand back the cookie id.
        *pdwCookie = pConCookie->m_id;

        pConCookie.SuppressRelease();
    }
    GCPROTECT_END();
    GCPROTECT_END();
}

void ConnectionPoint::UnadviseWorker(DWORD dwCookie)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    COMOBJECTREF pEventItfObj = NULL;
    OBJECTREF pTCEProviderObj = NULL;

    GCPROTECT_BEGIN(pEventItfObj)
    GCPROTECT_BEGIN(pTCEProviderObj)
    {
        // The cookie is actually a connection cookie.
        ConnectionCookieHolder pConCookie = FindWithLock(dwCookie);

        // Retrieve the COM+ object from the cookie which in fact is the object handle.
        pEventItfObj = (COMOBJECTREF) ObjectFromHandle(pConCookie->m_hndEventProvObj);
        if (!pEventItfObj)
            COMPlusThrowHR(E_INVALIDARG);

        // Get the object from the wrapper
        pTCEProviderObj = m_pOwnerWrap->GetObjectRef();

        for (int cEventMethod = 0; cEventMethod < m_NumEventMethods; cEventMethod++)
        {
            // If the managed object supports the event that call the RemoveEventX method.
            if (m_apEventMethods[cEventMethod].m_pEventMethod)
            {
                InvokeProviderMethod(pTCEProviderObj, (OBJECTREF) pEventItfObj, m_apEventMethods[cEventMethod].m_pRemoveMethod, m_apEventMethods[cEventMethod].m_pEventMethod);
            }
        }

        // Remove the connection cookie from the list.
        FindAndRemoveWithLock(pConCookie);
    }
    GCPROTECT_END();
    GCPROTECT_END();
}

void ConnectionPoint::SetupEventMethods()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;

    // Remember the number of not supported events.
    int cNonSupportedEvents = 0;

    // Retrieve the total number of event methods present on the source interface.
    int cMaxNumEventMethods = m_pEventItfMT->GetNumMethods();

    // If there are no methods then there is nothing to do.
    if (cMaxNumEventMethods == 0)
        return;

    // Allocate the event method tables.
    NewArrayHolder<EventMethodInfo> EventMethodInfos = new EventMethodInfo[cMaxNumEventMethods];

    // Find all the real event methods needed to be able to advise on the current connection point.
    int NumEventMethods = 0;
    for (int cEventMethod = 0; cEventMethod < cMaxNumEventMethods; cEventMethod++)
    {
        // Retrieve the method descriptor for the current method on the event interface.
        MethodDesc *pEventMethodDesc = m_pEventItfMT->GetMethodDescForSlot(cEventMethod);
        if (!pEventMethodDesc)
            continue;

        // Store the event method on the source interface.
        EventMethodInfos[NumEventMethods].m_pEventMethod = pEventMethodDesc;

        // Retrieve and store the add and remove methods for the event.
        EventMethodInfos[NumEventMethods].m_pAddMethod = FindProviderMethodDesc(pEventMethodDesc, EventAdd);
        EventMethodInfos[NumEventMethods].m_pRemoveMethod = FindProviderMethodDesc(pEventMethodDesc, EventRemove);

        // Make sure we have found both the add and the remove methods.
        if (!EventMethodInfos[NumEventMethods].m_pAddMethod || !EventMethodInfos[NumEventMethods].m_pRemoveMethod)
        {
            cNonSupportedEvents++;
            continue;
        }

        // Increment the real number of event methods on the source interface.
        NumEventMethods++;
    }

    // If the interface has methods and the object does not support any then we
    // fail the connection.
    if ((NumEventMethods == 0) && (cNonSupportedEvents > 0))
        COMPlusThrowHR(CONNECT_E_NOCONNECTION);

    // Now that the struct is totally setup, we'll set the members.
    m_NumEventMethods = NumEventMethods;
    m_apEventMethods = EventMethodInfos;
    EventMethodInfos.SuppressRelease();
}

MethodDesc *ConnectionPoint::FindProviderMethodDesc( MethodDesc *pEventMethodDesc, EnumEventMethods Method )
{
    CONTRACT (MethodDesc*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pEventMethodDesc));
        PRECONDITION(Method == EventAdd || Method == EventRemove);
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END

    // Retrieve the event method.
    MethodDesc *pProvMethodDesc =
        MemberLoader::FindEventMethod(m_pTCEProviderMT, pEventMethodDesc->GetName(), Method, MemberLoader::FM_IgnoreCase);
    if (!pProvMethodDesc)
        RETURN NULL;

    // Validate that the signature of the delegate is the expected signature.
    MetaSig Sig(pProvMethodDesc);
    if (Sig.NextArg() != ELEMENT_TYPE_CLASS)
        RETURN NULL;

    // <TODO>@TODO: this ignores the type of failure - try GetLastTypeHandleThrowing()</TODO>
    TypeHandle DelegateType = Sig.GetLastTypeHandleNT();
    if (DelegateType.IsNull())
        RETURN NULL;

    PCCOR_SIGNATURE pEventMethSig;
    DWORD cEventMethSig;
    pEventMethodDesc->GetSig(&pEventMethSig, &cEventMethSig);
    MethodDesc *pInvokeMD = MemberLoader::FindMethod(DelegateType.GetMethodTable(),
        "Invoke",
        pEventMethSig,
        cEventMethSig,
        pEventMethodDesc->GetModule());

    if (!pInvokeMD)
        RETURN NULL;

    // The requested method exists and has the appropriate signature.
    RETURN pProvMethodDesc;
}

void ConnectionPoint::InvokeProviderMethod( OBJECTREF pProvider, OBJECTREF pSubscriber, MethodDesc *pProvMethodDesc, MethodDesc *pEventMethodDesc )
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pProvMethodDesc));
        PRECONDITION(CheckPointer(pEventMethodDesc));
    }
    CONTRACTL_END;

    GCPROTECT_BEGIN (pSubscriber);
    GCPROTECT_BEGIN (pProvider);
    {
        // Create a method signature to extract the type of the delegate.
        MetaSig MethodSig( pProvMethodDesc);
        _ASSERTE( 1 == MethodSig.NumFixedArgs() );

        // Go to the first argument.
        CorElementType ArgType = MethodSig.NextArg();
        _ASSERTE( ELEMENT_TYPE_CLASS == ArgType );

        // Retrieve the EE class representing the argument.
        MethodTable *pDelegateCls = MethodSig.GetLastTypeHandleThrowing().GetMethodTable();

        // Make sure we activate the assembly containing the target method desc
        pEventMethodDesc->EnsureActive();

        // Allocate an object based on the method table of the delegate class.
        OBJECTREF pDelegate = pDelegateCls->Allocate();

        GCPROTECT_BEGIN( pDelegate );
        {
            // Initialize the delegate using the arguments structure.
            // <TODO>Generics: ensure we get the right MethodDesc here and in similar places</TODO>
            // Accept both void (object, native int) and void (object, native uint)
            MethodDesc *pDlgCtorMD = MemberLoader::FindConstructor(pDelegateCls, &gsig_IM_Obj_IntPtr_RetVoid);
            if (pDlgCtorMD == NULL)
                pDlgCtorMD = MemberLoader::FindConstructor(pDelegateCls, &gsig_IM_Obj_UIntPtr_RetVoid);

            // The loader is responsible for only accepting well-formed delegate classes.
            _ASSERTE(pDlgCtorMD);

            MethodDescCallSite dlgCtor(pDlgCtorMD);

            ARG_SLOT CtorArgs[3] = { ObjToArgSlot(pDelegate),
                                     ObjToArgSlot(pSubscriber),
                                     (ARG_SLOT)pEventMethodDesc->GetMultiCallableAddrOfCode()
                                   };
            dlgCtor.Call(CtorArgs);

            MethodDescCallSite prov(pProvMethodDesc, &pProvider);

            // Do the actual invocation of the method method.
            ARG_SLOT Args[2] = { ObjToArgSlot( pProvider ), ObjToArgSlot( pDelegate ) };
            prov.Call(Args);
        }
        GCPROTECT_END();
    }
    GCPROTECT_END();
    GCPROTECT_END();
}

void ConnectionPoint::InsertWithLock(ConnectionCookie* pConCookie)
{
    WRAPPER_NO_CONTRACT;

    LockHolder lh(this);

    bool fDone = false;

    //
    // handle special cases
    //

    if (NULL == m_pLastInserted)
    {
        //
        // Special case 1:  List is empty.
        //
        CONSISTENCY_CHECK(NULL == m_ConnectionList.GetHead());

        pConCookie->m_id = 1;
        m_ConnectionList.InsertHead(pConCookie);
        fDone = true;
    }

    if (!fDone && ((NULL != m_pLastInserted->m_Link.m_pNext) || (idUpperLimit == m_pLastInserted->m_id)))
    {
        //
        // Special case 2:  Last inserted is somewhere in the middle of the list or we last
        //                  inserted the max token id (we've wrapped around) and ID 1 is not
        //                  taken.
        //
        CONSISTENCY_CHECK(NULL != m_ConnectionList.GetHead());

        if (1 != m_ConnectionList.GetHead()->m_id)
        {
            // if ID 1 is not taken, we can just insert there.
            pConCookie->m_id = 1;
            m_ConnectionList.InsertHead(pConCookie);
            fDone = true;
        }
    }

    //
    // General cases
    //
    if (!fDone)
    {
        ConnectionCookie* pLocationToStartSearchForInsertPoint = NULL;
        ConnectionCookie* pInsertionPoint = NULL;

        if (NULL == m_pLastInserted->m_Link.m_pNext)
        {
            if (idUpperLimit == m_pLastInserted->m_id)
            {
                CONSISTENCY_CHECK(1 == m_ConnectionList.GetHead()->m_id);   // should be handled by special case #2

                // we need to wrap around
                // scan from head for first hole, insert there
                pLocationToStartSearchForInsertPoint = m_ConnectionList.GetHead();
            }
            else
            {
                // Most common case: insert at tail, incrementing ID
                pInsertionPoint = m_pLastInserted;
                pLocationToStartSearchForInsertPoint = NULL;    // don't do any searching, just append
            }
        }
        else
        {
            // scan from m_pLastInserted for first hole, insert there
            pLocationToStartSearchForInsertPoint = m_pLastInserted;
        }

        if (NULL != pLocationToStartSearchForInsertPoint)
        {
            // Starting from pLocationToStartSearchForInsertPoint, scan list to find a
            // discontinuity in the IDs.  Insert there.

            ConnectionCookie* pCurrentNode = pLocationToStartSearchForInsertPoint;

            //
            // limit case is where we've wrapped around the whole list and found the initial start point again.
            //
            while (true)
            {
                if (NULL == pCurrentNode->m_Link.m_pNext)
                {
                    if (pCurrentNode->m_id < idUpperLimit)
                    {
                        // if we reach the end of the list and we have free IDs, let's use them.
                        break;
                    }
                    pCurrentNode = m_ConnectionList.GetHead();
                }
                else
                {
                    ConnectionCookie* pNext = CONTAINING_RECORD(pCurrentNode->m_Link.m_pNext, ConnectionCookie, m_Link);
                    if ((pCurrentNode->m_id + 1) < pNext->m_id)
                    {
                        break;
                    }
                    pCurrentNode = pNext;
                }

                if (pCurrentNode == pLocationToStartSearchForInsertPoint)
                {
                    // we came back to the node where we started which means that there's no gap and
                    // all IDs up to idUpperLimit are taken
                    EX_THROW(HRException, (CONNECT_E_ADVISELIMIT));
                }
            }

            pInsertionPoint = pCurrentNode;
        }


        CONSISTENCY_CHECK(NULL != pInsertionPoint);
        CONSISTENCY_CHECK(idUpperLimit != pInsertionPoint->m_id);

#ifdef _DEBUG
        ConnectionCookie* pNextCookieNode = CONTAINING_RECORD(pInsertionPoint->m_Link.m_pNext, ConnectionCookie, m_Link);
        DWORD idNew = pInsertionPoint->m_id + 1;
        CONSISTENCY_CHECK(NULL == pNextCookieNode ||
            ((pInsertionPoint->m_id < idNew) &&
                                     (idNew < pNextCookieNode->m_id)));
#endif // _DEBUG

        pConCookie->m_id = pInsertionPoint->m_id + 1;
        pInsertionPoint->m_Link.InsertAfter(&pConCookie->m_Link);
    }

    m_pLastInserted = pConCookie;
}

ConnectionCookie* ConnectionPoint::FindWithLock(DWORD idOfCookie)
{
    WRAPPER_NO_CONTRACT;

    ConnectionCookie* pCurrentNode;

    {
        LockHolder lh(this);

        pCurrentNode = m_ConnectionList.GetHead();

        while (pCurrentNode && (pCurrentNode->m_id != idOfCookie))
        {
            pCurrentNode = CONTAINING_RECORD(pCurrentNode->m_Link.m_pNext, ConnectionCookie, m_Link);
        }
    }

    if (NULL == pCurrentNode)
    {
        EX_THROW(HRException, (CONNECT_E_NOCONNECTION));
    }

    return pCurrentNode;
}


void ConnectionPoint::FindAndRemoveWithLock(ConnectionCookie* pConCookie)
{
    WRAPPER_NO_CONTRACT;

    LockHolder lh(this);

    m_ConnectionList.FindAndRemove(pConCookie);

    if (pConCookie == m_pLastInserted)
    {
        m_pLastInserted = m_ConnectionList.GetHead();
    }
}

ConnectionPointEnum::ConnectionPointEnum(ComCallWrapper *pOwnerWrap, CQuickArray<ConnectionPoint*> *pCPList)
: m_pOwnerWrap(pOwnerWrap)
, m_pCPList(pCPList)
, m_CurrPos(0)
, m_cbRefCount(0)
, m_Lock(CrstInterop)
{
    WRAPPER_NO_CONTRACT;

    m_pOwnerWrap->AddRef();
}

ConnectionPointEnum::~ConnectionPointEnum()
{
    WRAPPER_NO_CONTRACT;

    if (m_pOwnerWrap)
        m_pOwnerWrap->Release();
}

HRESULT __stdcall ConnectionPointEnum::QueryInterface(REFIID riid, void** ppv)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(ppv, NULL_OK));
    }
    CONTRACTL_END;

    // Verify the arguments.
    if (!ppv)
        return E_POINTER;

    // Initialize the out parameters.
    *ppv = NULL;

    SetupForComCallHR();

    if (riid == IID_IEnumConnectionPoints)
    {
        *ppv = static_cast<IEnumConnectionPoints*>(this);
    }
    else if (riid == IID_IUnknown)
    {
        *ppv = static_cast<IUnknown*>(this);
    }
    else
    {
        return E_NOINTERFACE;
    }

    ULONG cbRef = SafeAddRefPreemp((IUnknown*)*ppv);
    //@TODO(CLE) AddRef logging that doesn't use QI

    return S_OK;
}

ULONG __stdcall ConnectionPointEnum::AddRef()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    SetupForComCallHR();

    LONG i = InterlockedIncrement((LONG*)&m_cbRefCount );
    return i;
}

ULONG __stdcall ConnectionPointEnum::Release()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    SetupForComCallHR();

    HRESULT hr = S_OK;
    ULONG cbRef = -1;

    BEGIN_EXTERNAL_ENTRYPOINT(&hr)
    {
        cbRef = InterlockedDecrement((LONG*)&m_cbRefCount );
        _ASSERTE(cbRef >=0);
        if (cbRef == 0)
            delete this;
    }
    END_EXTERNAL_ENTRYPOINT;

    return cbRef;
}

HRESULT __stdcall ConnectionPointEnum::Next(ULONG cConnections, IConnectionPoint **ppCP, ULONG *pcFetched)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(ppCP, NULL_OK));
        PRECONDITION(CheckPointer(pcFetched, NULL_OK));
    }
    CONTRACTL_END;

    // Verify the arguments.
    if (NULL == ppCP)
        return E_POINTER;

    // Initialize the out parameters.
    if (pcFetched)
        *pcFetched = 0;

    SetupForComCallHR();

    UINT cFetched;

    // Acquire the lock before we start traversing the connection point list.
    {
        LockHolder lh(this);

        for (cFetched = 0; cFetched < cConnections && m_CurrPos < m_pCPList->Size(); cFetched++, m_CurrPos++)
        {
            ppCP[cFetched] = (*m_pCPList)[m_CurrPos];
            SafeAddRefPreemp(ppCP[cFetched]);
        }

        if (pcFetched)
            *pcFetched = cFetched;
    }

    return cFetched == cConnections ? S_OK : S_FALSE;
}

HRESULT __stdcall ConnectionPointEnum::Skip(ULONG cConnections)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    SetupForComCallHR();

    // Acquire the lock before we start traversing the connection point list.
    {
        LockHolder lh(this);

        if(m_CurrPos + cConnections <= m_pCPList->Size())
        {
            // There are enough connection points left in the list to allow
            // us to skip the required number.
            m_CurrPos += cConnections;
            return S_OK;
        }
        else
        {
            // There aren't enough connection points left so set the current
            // position to the end of the list and return S_FALSE to indicate
            // we couldn't skip the requested number.
            m_CurrPos = (UINT)m_pCPList->Size();
            return S_FALSE;
        }
    }
}

HRESULT __stdcall ConnectionPointEnum::Reset()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    SetupForComCallHR();

    // Acquire the lock before we start traversing the connection point list.
    {
        LockHolder lh(this);
        m_CurrPos = 0;
    }

    return S_OK;
}

HRESULT __stdcall ConnectionPointEnum::Clone(IEnumConnectionPoints **ppEnum)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(ppEnum, NULL_OK));
    }
    CONTRACTL_END;

    // Verify the arguments.
    if (!ppEnum)
        return E_POINTER;

    // Initialize the out parameters.
    *ppEnum = NULL;

    SetupForComCallHR();

    ConnectionPointEnum *pCPEnum;
    {
        CONTRACT_VIOLATION(ThrowsViolation);  //ConnectionPointEnum throws
        pCPEnum = new(nothrow) ConnectionPointEnum(m_pOwnerWrap, m_pCPList);
    }
    if (!pCPEnum)
        return E_OUTOFMEMORY;

    HRESULT hr = SafeQueryInterfacePreemp(pCPEnum, IID_IEnumConnectionPoints, (IUnknown**)ppEnum);
    LogInteropQI(pCPEnum, IID_IEnumConnectionPoints, hr, "ConnectionPointEnum::Clone: QIing for IID_IEnumConnectionPoints");

    return hr;
}

ConnectionEnum::ConnectionEnum(ConnectionPoint *pConnectionPoint)
: m_pConnectionPoint(pConnectionPoint)
, m_CurrCookie(pConnectionPoint->GetCookieList()->GetHead())
, m_cbRefCount(0)
{
    CONTRACTL
    {
        NOTHROW;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    ULONG cbRef = SafeAddRefPreemp(m_pConnectionPoint);
    //@TODO(CLE) AddRef logging that doesn't use QI
}

ConnectionEnum::~ConnectionEnum()
{
    CONTRACTL
    {
        NOTHROW;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    ULONG cbRef = SafeReleasePreemp(m_pConnectionPoint);
    LogInteropRelease(m_pConnectionPoint, cbRef, "ConnectionEnum::~ConnectionEnum: Releasing the connection point object");
}

HRESULT __stdcall ConnectionEnum::QueryInterface(REFIID riid, void** ppv)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(ppv, NULL_OK));
    }
    CONTRACTL_END;

    // Verify the arguments.
    if (!ppv)
        return E_POINTER;

    // Initialize the out parameters.
    *ppv = NULL;

    SetupForComCallHR();

    if (riid == IID_IEnumConnections)
    {
        *ppv = static_cast<IEnumConnections*>(this);
    }
    else if (riid == IID_IUnknown)
    {
        *ppv = static_cast<IUnknown*>(this);
    }
    else
    {
        return E_NOINTERFACE;
    }

    ULONG cbRef = SafeAddRefPreemp((IUnknown*)*ppv);
    //@TODO(CLE) AddRef logging that doesn't use QI

    return S_OK;
}

ULONG __stdcall ConnectionEnum::AddRef()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    SetupForComCallHR();

    LONG i = InterlockedIncrement((LONG*)&m_cbRefCount);
    return i;
}

ULONG __stdcall ConnectionEnum::Release()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    SetupForComCallHR();

    LONG i = InterlockedDecrement((LONG*)&m_cbRefCount);
    _ASSERTE(i >=0);
    if (i == 0)
        delete this;

    return i;
}

HRESULT __stdcall ConnectionEnum::Next(ULONG cConnections, CONNECTDATA* rgcd, ULONG *pcFetched)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(rgcd, NULL_OK));
        PRECONDITION(CheckPointer(pcFetched, NULL_OK));
    }
    CONTRACTL_END;

    // Verify the arguments.
    if (NULL == rgcd)
        return E_POINTER;

    // Initialize the out parameters.
    if (pcFetched)
        *pcFetched = 0;

    SetupForComCallHR();

    HRESULT hr = S_OK;
    UINT cFetched;
    CONNECTIONCOOKIELIST *pConnectionList = m_pConnectionPoint->GetCookieList();

    // Acquire the connection point's lock before we start traversing the connection list.
    {
        ConnectionPoint::LockHolder lh(m_pConnectionPoint);

        {
            // Switch to cooperative GC mode before we manipulate OBJCETREF's.
            GCX_COOP();

            for (cFetched = 0; cFetched < cConnections && m_CurrCookie; cFetched++)
            {
                {
                    CONTRACT_VIOLATION(ThrowsViolation);
                    rgcd[cFetched].pUnk = GetComIPFromObjectRef((OBJECTREF*)m_CurrCookie->m_hndEventProvObj, ComIpType_Unknown, NULL);
                    rgcd[cFetched].dwCookie = m_CurrCookie->m_id;
                }
                m_CurrCookie = pConnectionList->GetNext(m_CurrCookie);
            }
        }

        // Leave the lock now that we are done traversing the list.
    }

    // Set the count of fetched connections if the caller desires it.
    if (pcFetched)
        *pcFetched = cFetched;

    return cFetched == cConnections ? S_OK : S_FALSE;
}

HRESULT __stdcall ConnectionEnum::Skip(ULONG cConnections)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    SetupForComCallHR();

    HRESULT hr = S_FALSE;
    CONNECTIONCOOKIELIST *pConnectionList = m_pConnectionPoint->GetCookieList();

    {
        ConnectionPoint::LockHolder lh(m_pConnectionPoint);

        // Try and skip the requested number of connections.
        while (m_CurrCookie && cConnections)
        {
            m_CurrCookie = pConnectionList->GetNext(m_CurrCookie);
            cConnections--;
        }
        // Leave the lock now that we are done traversing the list.
    }

    // Check to see if we succeeded.
    return cConnections == 0 ? S_OK : S_FALSE;
}

HRESULT __stdcall ConnectionEnum::Reset()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    SetupForComCallHR();

    // Set the current cookie back to the head of the list. We must acquire the
    // connection point lock before we touch the list.
    ConnectionPoint::LockHolder lh(m_pConnectionPoint);

    m_CurrCookie = m_pConnectionPoint->GetCookieList()->GetHead();

    return S_OK;
}

HRESULT __stdcall ConnectionEnum::Clone(IEnumConnections **ppEnum)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(ppEnum, NULL_OK));
    }
    CONTRACTL_END;

    // Verify the arguments.
    if (!ppEnum)
        return E_POINTER;

    // Initialize the out parameters.
    *ppEnum = NULL;

    SetupForComCallHR();

    ConnectionEnum *pConEnum = new(nothrow) ConnectionEnum(m_pConnectionPoint);
    if (!pConEnum)
        return E_OUTOFMEMORY;

    HRESULT hr = SafeQueryInterfacePreemp(pConEnum, IID_IEnumConnections, (IUnknown**)ppEnum);
    LogInteropQI(pConEnum, IID_IEnumConnections, hr, "ConnectionEnum::Clone: QIing for IID_IEnumConnections");

    return hr;
}
