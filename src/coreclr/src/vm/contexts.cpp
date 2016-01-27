// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// Contexts.CPP
//

// 
// Implementation for class Context
//


#include "common.h"

#ifdef FEATURE_REMOTING

#include "context.h"
#include "excep.h"
#include "field.h"
#include "remoting.h"
#include "perfcounters.h"
#include "specialstatics.h"
#include "appdomain.inl"

#ifdef FEATURE_COMINTEROP
#include "runtimecallablewrapper.h"
#endif // FEATURE_COMINTEROP

#ifndef DACCESS_COMPILE

#define CONTEXT_SIGNATURE   (0x2b585443)        // CTX+
#define CONTEXT_DESTROYED   (0x2d585443)        // CTX-

// Lock for safe operations
CrstStatic Context::s_ContextCrst;


Context::Context(AppDomain *pDomain)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pDomain));
    }
    CONTRACTL_END;
    
    m_pDomain = pDomain;
    m_Signature = CONTEXT_SIGNATURE;

    // This needs to be a LongWeakHandle since we want to be able
    // to run finalizers on Proxies while the Context itself
    // unreachable. When running the finalizer we will have to
    // transition into the context like a regular remote call.
    // If this is a short weak handle, it ceases being updated
    // as soon as the context is unreachable. By making it a strong
    // handle, it is updated till the context::finalize is run.

    m_ExposedObjectHandle = pDomain->CreateLongWeakHandle(NULL);

    // Set the pointers to the static data storage
    m_pUnsharedStaticData = NULL;
    m_pSharedStaticData = NULL;

    COUNTER_ONLY(GetPerfCounters().m_Context.cContexts++);
}

Context::~Context()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    
    BOOL fADUnloaded = m_pDomain->NoAccessToHandleTable();
    if (!fADUnloaded)
    {
        DestroyLongWeakHandle(m_ExposedObjectHandle);
    }

    m_pDomain = NULL;
    m_Signature = CONTEXT_DESTROYED;

    // Cleanup the static data storage
    if(m_pUnsharedStaticData)
    {
        for(WORD i = 0; i < m_pUnsharedStaticData->cElem; i++)
        {
            delete [] (BYTE *) (m_pUnsharedStaticData->dataPtr[i]);
        }
        delete [] m_pUnsharedStaticData;
        m_pUnsharedStaticData = NULL;
    }

    if(m_pSharedStaticData)
    {
        for(WORD i = 0; i < m_pSharedStaticData->cElem; i++)
        {
            delete [] (BYTE *) (m_pSharedStaticData->dataPtr[i]);
        }
        delete [] m_pSharedStaticData;
        m_pSharedStaticData = NULL;
    }

    // Destroy pinning handles associated with this context
    ObjectHandleList::NodeType* pHandleNode; 
    while ((pHandleNode = m_PinnedContextStatics.UnlinkHead() ) != NULL)
    {
        if (!fADUnloaded)
        {
            DestroyPinningHandle(pHandleNode->data);
        }
        delete pHandleNode;
    }

    COUNTER_ONLY(GetPerfCounters().m_Context.cContexts--);
}

Context* Context::CreateNewContext(AppDomain *pDomain)
{
    CONTRACT (Context*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pDomain));
    }
    CONTRACT_END;
    
    Context *p = new Context(pDomain);
    RETURN p;
}

void Context::Initialize()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Initialize the context critical section
    s_ContextCrst.Init(CrstContexts, (CrstFlags)(CRST_REENTRANCY|CRST_HOST_BREAKABLE));
}

BOOL Context::ValidateContext(Context *pCtx)
{  
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SO_TOLERANT;
        PRECONDITION(CheckPointer(pCtx));
    }
    CONTRACTL_END;  

    BOOL bRet = FALSE;
    
    EX_TRY
    {
        if (pCtx->m_Signature == CONTEXT_SIGNATURE)
            bRet = TRUE;
    }
    EX_CATCH
    {
        // Swallow exceptions - if not a valid ctx, just return false.
    }
    EX_END_CATCH(RethrowTerminalExceptions);
    
    return bRet;
}

// if the object we are creating is a proxy to another appdomain, want to create the wrapper for the
// new object in the appdomain of the proxy target
Context* Context::GetExecutionContext(OBJECTREF pObj)
{
    CONTRACT (Context*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(pObj != NULL);
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;
    
    Context *pContext = NULL;
    if (pObj->IsTransparentProxy()) 
        pContext = CRemotingServices::GetServerContextForProxy(pObj);
    if (pContext == NULL)
        pContext = GetAppDomain()->GetDefaultContext();

    RETURN pContext;
}

OBJECTREF Context::GetExposedObject()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;
    
    if (ObjectFromHandle(m_ExposedObjectHandle) == NULL)
    {
        // This call should fault in the managed context for the thread
        MethodDescCallSite getCurrentContext(METHOD__THREAD__GET_CURRENT_CONTEXT);
        CONTEXTBASEREF ctx = (CONTEXTBASEREF) getCurrentContext.Call_RetOBJECTREF((ARG_SLOT*)NULL);

        GCPROTECT_BEGIN(ctx);
        {
            // Take a lock to make sure that only one thread creates the object.
            // This locking may be too severe!
            CrstHolder ch(&s_ContextCrst);

            // Check to see if another thread has not already created the exposed object.
            if (ObjectFromHandle(m_ExposedObjectHandle) == NULL)
            {
                // Keep a weak reference to the exposed object.
                StoreObjectInHandle(m_ExposedObjectHandle, (OBJECTREF) ctx);

                ctx->SetInternalContext(this);
            }
        }
        GCPROTECT_END();

    }
    return ObjectFromHandle(m_ExposedObjectHandle);
}

void Context::SetExposedObject(OBJECTREF exposed)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(exposed != NULL);
        PRECONDITION(ObjectFromHandle(m_ExposedObjectHandle) == NULL);
    }
    CONTRACTL_END;
    
    StoreObjectInHandle(m_ExposedObjectHandle, exposed);
}

// This is called by EE to transition into a context(possibly in
// another appdomain) and execute the method Context::ExecuteCallBack
// with the private data provided to this method
void Context::RequestCallBack(ADID appDomainID, Context* targetCtxID, void* privateData)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(targetCtxID));
        PRECONDITION(CheckPointer(privateData));
        PRECONDITION(ValidateContext((Context*)targetCtxID));
    }
    CONTRACTL_END;
    // a warning: don't touch targetCtxID until you verified appDomainID, 
    // unless the latter is CURRENT_APPDOMAIN_ID

    // Get the current context of the thread. This is assumed as
    // the context where the request originated
    Context *pCurrCtx;
    pCurrCtx = GetCurrentContext();

    // Check that the target context is not the same (presumably the caller has checked for it).
    _ASSERTE(pCurrCtx != targetCtxID);

    // Check if we might be going to a context in another appDomain.
    ADID targetDomainID;

    if (appDomainID == CURRENT_APPDOMAIN_ID)
    {
        targetDomainID = (ADID)0;
        _ASSERTE(targetCtxID->GetDomain()==::GetAppDomain());
    }
    else
    {
        targetDomainID=appDomainID;
#ifdef _DEBUG        
        AppDomainFromIDHolder ad(appDomainID, FALSE);
        if (!ad.IsUnloaded())
            _ASSERTE(targetCtxID->GetDomain()->GetId()==appDomainID);
#endif
    }

    // we need to be co-operative mode for jitting
    GCX_COOP();

    MethodDescCallSite callback(METHOD__CONTEXT__CALLBACK);

    ARG_SLOT args[3];
    args[0] = PtrToArgSlot(targetCtxID);
    args[1] = PtrToArgSlot(privateData);
    args[2] = (ARG_SLOT) (size_t)targetDomainID.m_dwId;

    callback.Call(args);
}

/*** Definitions of callback executions for the various callbacks that are known to EE  ***/

// Callback for waits on waithandle
void Context::ExecuteWaitCallback(WaitArgs* waitArgs)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(waitArgs));
    }
    CONTRACTL_END;

    Thread* pCurThread = GetThread();
    _ASSERTE(pCurThread != NULL);
    
    // DoAppropriateWait switches to preemptive GC before entering the wait
    *(waitArgs->pResult) = pCurThread->DoAppropriateWait( waitArgs->numWaiters,
                                                          waitArgs->waitHandles,
                                                          waitArgs->waitAll,
                                                          waitArgs->millis,
                                                          waitArgs->alertable?WaitMode_Alertable:WaitMode_None);
}

// Callback for monitor wait on objects
void Context::ExecuteMonitorWaitCallback(MonitorWaitArgs* waitArgs)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(waitArgs));
    }
    CONTRACTL_END;
    
    Thread* pCurThread = GetThread();
    _ASSERTE(pCurThread != NULL);
    
    GCX_PREEMP();
    
    *(waitArgs->pResult) = pCurThread->Block(waitArgs->millis,
                                             waitArgs->syncState);
}

// Callback for signalandwait on waithandles
void Context::ExecuteSignalAndWaitCallback(SignalAndWaitArgs* signalAndWaitArgs)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(signalAndWaitArgs));
    }
    CONTRACTL_END;

    Thread* pCurThread = GetThread();
    _ASSERTE(pCurThread != NULL);
    
    // DoAppropriateWait switches to preemptive GC before entering the wait
    *(signalAndWaitArgs->pResult) = pCurThread->DoSignalAndWait( signalAndWaitArgs->waitHandles,
                                                          signalAndWaitArgs->millis,
                                                          signalAndWaitArgs->alertable);
}

//+----------------------------------------------------------------------------
//
//  Method:     Context::GetStaticFieldAddress   private
//
//  Synopsis:   Get the address of the field relative to the current context.
//              If an address has not been assigned yet then create one.
//

//
//+----------------------------------------------------------------------------
LPVOID Context::GetStaticFieldAddress(FieldDesc *pFD)
{
    CONTRACT (LPVOID)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pFD));
        PRECONDITION(!s_ContextCrst.OwnedByCurrentThread());
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    LPVOID          pvAddress       = NULL;
    Context*        pCtx            = NULL;
    // for static field the MethodTable is exact even for generic classes
    MethodTable*    pMT             = pFD->GetEnclosingMethodTable();
    BOOL            fIsShared       = pMT->IsDomainNeutral();
    DWORD           dwClassOffset   = pMT->GetContextStaticsOffset();
    DWORD           currElem        = 0;
    STATIC_DATA*    pData;
    
    // NOTE: if you change this method, you must also change
    // GetStaticFieldAddrNoCreate below.

    if (dwClassOffset == (DWORD)-1)
    {
        dwClassOffset = pMT->AllocateContextStaticsOffset();
    }

    // Retrieve the current context
    pCtx = GetCurrentContext();
    _ASSERTE(NULL != pCtx);

    // Acquire the context lock before accessing the static data pointer
    {
        CrstHolder ch(&s_ContextCrst);

        if(!fIsShared)
            pData = pCtx->m_pUnsharedStaticData;
        else
            pData = pCtx->m_pSharedStaticData;

        if(NULL != pData)
            currElem = pData->cElem;

        // Check whether we have allocated space for storing a pointer to
        // this class' context static store
        if(dwClassOffset >= currElem)
        {
            // Allocate space for storing pointers
            DWORD dwNewElem = (currElem == 0 ? 4 : currElem*2);
            
            // Ensure that we grow to a size larger than the index we intend to use
            while (dwNewElem <= dwClassOffset)
                dwNewElem = 2*dwNewElem;

            STATIC_DATA *pNew = (STATIC_DATA *)new BYTE[sizeof(STATIC_DATA) + dwNewElem*sizeof(LPVOID)];

            // Set the new count.
            pNew->cElem = dwNewElem;
                        
            if(NULL != pData)
            {
                // Copy the old data into the new data
                memcpy(&pNew->dataPtr[0], &pData->dataPtr[0], currElem*sizeof(LPVOID));

                // Delete the old data
                delete [] (BYTE*) pData;
            }

            // Zero init any new elements.
            ZeroMemory(&pNew->dataPtr[currElem], (dwNewElem - currElem)* sizeof(LPVOID));

            // Update the locals
            pData = pNew;

            // Reset the pointers in the context object to point to the
            // new memory
            if(!fIsShared)
                pCtx->m_pUnsharedStaticData = pData;
            else
                pCtx->m_pSharedStaticData = pData;
        }

        _ASSERTE(NULL != pData);

        // Check whether we have to allocate space for
        // the context local statics of this class
        if(NULL == pData->dataPtr[dwClassOffset])
        {
            DWORD dwSize = pMT->GetContextStaticsSize();

            // Allocate memory for context static fields
            LPBYTE pFields = new BYTE[dwSize];

            // Initialize the memory allocated for the fields
            ZeroMemory(pFields, dwSize);

            pData->dataPtr[dwClassOffset] = pFields;
        }

        _ASSERTE(NULL != pData->dataPtr[dwClassOffset]);

        pvAddress = (LPVOID)((LPBYTE)pData->dataPtr[dwClassOffset] + pFD->GetOffset());
        
        // For object and value class fields we have to allocate storage in the
        // __StaticContainer class in the managed heap
        if(pFD->IsObjRef() || pFD->IsByValue())
        {
            // in this case *pvAddress == bucket|index
            int *pSlot = (int*)pvAddress;
            pvAddress = NULL;
            pCtx->GetStaticFieldAddressSpecial(pFD, pMT, pSlot, &pvAddress);

            if (pFD->IsByValue())
            {
                _ASSERTE(pvAddress != NULL);
                pvAddress = (*((OBJECTREF*)pvAddress))->GetData();
            }
            // ************************************************
            // ************** WARNING *************************
            // Do not provoke GC from here to the point JIT gets
            // pvAddress back
            // ************************************************
            _ASSERTE(*pSlot > 0);
        }
    }

    RETURN pvAddress;
}


//+----------------------------------------------------------------------------
//
//  Method:     Context::GetStaticFieldAddressSpecial private
//
//  Synopsis:   Allocate an entry in the __StaticContainer class in the
//              managed heap for static objects and value classes
//

//
//+----------------------------------------------------------------------------

// NOTE: At one point we used to allocate these in the long lived handle table
// which is per-appdomain. However, that causes them to get rooted and not
// cleaned up until the appdomain gets unloaded. This is not very desirable
// since a context static object may hold a reference to the context itself or
// to a proxy in the context causing a whole lot of garbage to float around.
// Now (2/13/01) these are allocated from a managed structure rooted in each
// managed context.

void Context::GetStaticFieldAddressSpecial(FieldDesc *pFD, MethodTable *pMT, int *pSlot, LPVOID *ppvAddress)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pFD));
        PRECONDITION(CheckPointer(pMT));
        PRECONDITION(CheckPointer(pSlot));
        PRECONDITION(CheckPointer(ppvAddress));
    }
    CONTRACTL_END;
    
    OBJECTREF *pObjRef = NULL;
    BOOL bNewSlot = (*pSlot == 0);
    
    if (bNewSlot)
    {
        // ! this line will trigger a GC, don't move it down
        // ! without protecting the args[] and other OBJECTREFS
        OBJECTREF orThis = GetExposedObject();;
        GCPROTECT_BEGIN(orThis);
        
        MethodDescCallSite reserveSlot(METHOD__CONTEXT__RESERVE_SLOT, &orThis);

        // We need to assign a location for this static field.
        // Call the managed helper
        ARG_SLOT args[1] =
        {
            ObjToArgSlot(orThis)
        };

        // The managed ReserveSlot methods counts on this!
        _ASSERTE(s_ContextCrst.OwnedByCurrentThread());
        _ASSERTE(args[0] != 0);

        *pSlot = reserveSlot.Call_RetI4(args);

        _ASSERTE(*pSlot>0);

        GCPROTECT_END();


        // to a boxed version of the value class.This allows the standard GC
        // algorithm to take care of internal pointers in the value class.
        if (pFD->IsByValue())
        {
            // Extract the type of the field
            TypeHandle  th = pFD->GetFieldTypeHandleThrowing();

            OBJECTHANDLE oh;            
            OBJECTREF obj = MethodTable::AllocateStaticBox(th.GetMethodTable(), pMT->HasFixedAddressVTStatics(), &oh);
            pObjRef = (OBJECTREF*)CalculateAddressForManagedStatic(*pSlot);

            if (oh != NULL)
            {            
                ObjectHandleList::NodeType* pNewNode = new ObjectHandleList::NodeType(oh);                
                m_PinnedContextStatics.LinkHead(pNewNode);
            }

            SetObjectReference( pObjRef, obj, GetAppDomain() );
        }
        else
        {
            pObjRef = (OBJECTREF*)CalculateAddressForManagedStatic(*pSlot);
        }
    }
    else
    {
        // If the field already has a location assigned we go through here
        pObjRef = (OBJECTREF*)CalculateAddressForManagedStatic(*pSlot);
    }

    *(ULONG_PTR *)ppvAddress =  (ULONG_PTR)pObjRef;
}

// This is called by the managed context constructor
FCIMPL2(void, Context::SetupInternalContext, ContextBaseObject* pThisUNSAFE, CLR_BOOL bDefault)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(pThisUNSAFE != NULL);
        PRECONDITION(pThisUNSAFE->m_internalContext == NULL);
    }
    CONTRACTL_END;

    CONTEXTBASEREF pThis = (CONTEXTBASEREF) pThisUNSAFE;
    HELPER_METHOD_FRAME_BEGIN_1(pThis);

    Context *pCtx;
    
    if (bDefault)
    {
        // We have to hook this up with the internal default
        // context for the current appDomain
        pCtx = GetThread()->GetDomain()->GetDefaultContext();
    }
    else
    {
        // Create the unmanaged backing context object
        pCtx = Context::CreateNewContext(GetThread()->GetDomain());
    }

    // Set the managed & unmanaged objects to point at each other.
    pThis->SetInternalContext(pCtx);
    pCtx->SetExposedObject((OBJECTREF)pThis);

    // Set the AppDomain field in the Managed context object
    pThis->SetExposedDomain(GetThread()->GetDomain()->GetExposedObject());

    if(bDefault)
        ((APPDOMAINREF)GetThread()->GetDomain()->GetExposedObject())->SetDefaultContext(pThis);

    COUNTER_ONLY(GetPerfCounters().m_Context.cContexts++);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

// This is called by the managed context finalizer
FCIMPL1(void, Context::CleanupInternalContext, ContextBaseObject* pThisUNSAFE)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(pThisUNSAFE != NULL);
    }
    CONTRACTL_END;

    CONTEXTBASEREF pThis = (CONTEXTBASEREF) pThisUNSAFE;
    HELPER_METHOD_FRAME_BEGIN_1(pThis);

    CONTEXTBASEREF refCtx = pThis;

    Context *pCtx = refCtx->m_internalContext;
    _ASSERTE(pCtx != NULL);
    
    if (ValidateContext(pCtx))
    {
        LOG((LF_APPDOMAIN, LL_INFO1000, "Context::CleanupInternalContext: %8.8x, %8.8x\n", OBJECTREFToObject(refCtx), pCtx));
        Context::FreeContext(pCtx);
    }

    COUNTER_ONLY(GetPerfCounters().m_Context.cContexts--);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND


// This is where a call back request made by EE in Context::RequestCallBack
// actually gets "executed".
// At this point we have done a real context transition from the threads
// context when RequestCallBack was called to the destination context.
FCIMPL1(void, Context::ExecuteCallBack, LPVOID privateData)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(privateData));
    }
    CONTRACTL_END;

    HELPER_METHOD_FRAME_BEGIN_0();

    switch (((CallBackInfo*) privateData)->callbackId)
    {
        case Wait_callback:
        {
            WaitArgs* waitArgs;
            waitArgs = (WaitArgs*) ((CallBackInfo*) privateData)->callbackData;
            ExecuteWaitCallback(waitArgs);
            break;
        }

        case MonitorWait_callback:
        {
            MonitorWaitArgs* waitArgs;
            waitArgs = (MonitorWaitArgs*) ((CallBackInfo*) privateData)->callbackData;
            ExecuteMonitorWaitCallback(waitArgs);
            break;
        }

        case ADTransition_callback:
        {
            ADCallBackArgs* pCallArgs = (ADCallBackArgs*)(((CallBackInfo*) privateData)->callbackData);
            pCallArgs->pTarget(pCallArgs->pArguments);
            break;
        }

        case SignalAndWait_callback:
        {
            SignalAndWaitArgs* signalAndWaitArgs;
            signalAndWaitArgs = (SignalAndWaitArgs*)((CallBackInfo*)privateData)->callbackData;
            ExecuteSignalAndWaitCallback(signalAndWaitArgs);
            break;
        }
        // Add other callback types here
        default:
            _ASSERTE(!"Invalid callback type");
            break;
    }

    // This is EE's entry point to do whatever it wanted to do in
    // the targetContext. This will return back into the managed
    // world and transition back into the original context.

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND



#ifdef ENABLE_PERF_COUNTERS

FCIMPL0(LPVOID, GetPrivateContextsPerfCountersEx)
{
    FCALL_CONTRACT;

    return (LPVOID)GetPrivateContextsPerfCounters();
}
FCIMPLEND


#else
FCIMPL0(LPVOID, GetPrivateContextsPerfCountersEx)
{
    FCALL_CONTRACT;

    return NULL;
}
FCIMPLEND

#endif // ENABLE_PERF_COUNTERS

#endif // DACCESS_COMPILE

// This will NOT create the exposed object if there isn't one!
OBJECTREF Context::GetExposedObjectRaw()
{
    WRAPPER_NO_CONTRACT;
    
    return ObjectFromHandle(m_ExposedObjectHandle);
}


PTR_Object Context::GetExposedObjectRawUnchecked()
{
    LIMITED_METHOD_CONTRACT;

    return *PTR_PTR_Object(m_ExposedObjectHandle);
}

PTR_PTR_Object Context::GetExposedObjectRawUncheckedPtr()
{
    LIMITED_METHOD_CONTRACT;

    return PTR_PTR_Object(m_ExposedObjectHandle);
}

//+----------------------------------------------------------------------------
//
//  Method:     Context::GetStaticFieldAddrNoCreate   private
//
//  Synopsis:   Get the address of the field relative to the context given a thread.
//              If an address has not been assigned, return NULL.
//              No creating is allowed.
//

//
//+----------------------------------------------------------------------------
PTR_VOID Context::GetStaticFieldAddrNoCreate(FieldDesc *pFD)
{
    CONTRACT (PTR_VOID)
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pFD));
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        SUPPORTS_DAC;
    }
    CONTRACT_END;
    
    PTR_VOID        pvAddress       = NULL;
    // for static field the MethodTable is exact even for generic classes
    MethodTable*    pMT             = pFD->GetEnclosingMethodTable();
    BOOL            fIsShared       = pMT->IsDomainNeutral();
    DWORD           dwClassOffset   = pMT->GetContextStaticsOffset();
    DWORD           currElem        = 0;
    STATIC_DATA*    pData;

    if (dwClassOffset == (DWORD)-1)
        RETURN NULL;
    
    if(!fIsShared)
        pData = m_pUnsharedStaticData;
    else
        pData = m_pSharedStaticData;

    if (NULL == pData)
        RETURN NULL;
    
    currElem = pData->cElem;

    // Check whether we have allocated space for storing a pointer to
    // this class' context static store
    if(dwClassOffset >= currElem || pData->dataPtr[dwClassOffset] == NULL)
        RETURN NULL;

    _ASSERTE(pData->dataPtr[dwClassOffset] != NULL);
    
    // We have allocated static storage for this data
    // Just return the address by getting the offset into the data
    pvAddress = PTR_VOID(dac_cast<PTR_BYTE>(pData->dataPtr[dwClassOffset]) + pFD->GetOffset());

    if(pFD->IsObjRef() || pFD->IsByValue())
    {
        if (*dac_cast<PTR_BYTE>(pvAddress) == NULL)
        {
            pvAddress = NULL;
            LOG((LF_SYNC, LL_ALWAYS, "dbgr: pvAddress = NULL"));
        }
        else
        {
            pvAddress = CalculateAddressForManagedStatic(*(PTR_int(pvAddress)));
            LOG((LF_SYNC, LL_ALWAYS, "dbgr: pvAddress = %lx", pvAddress));
            if (pFD->IsByValue())
            {
                _ASSERTE(pvAddress != NULL);
                pvAddress = (*(PTR_OBJECTREF(pvAddress)))->GetData();
            }
        }
    }

    RETURN pvAddress;
}


// This is used for context relative statics that are object refs
// These are stored in a structure in the managed context. The first
// time over an index and a bucket are determined and subsequently
// remembered in the location for the field in the per-context-per-class
// data structure.
// Here we map back from the index to the address of the object ref.
PTR_VOID Context::CalculateAddressForManagedStatic(int slot)
{
    CONTRACT (PTR_VOID)
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(this));
        POSTCONDITION(CheckPointer(RETVAL));
        SUPPORTS_DAC;
    }
    CONTRACT_END;
   
    PTR_OBJECTREF pObjRef;
    int bucket = (slot>>16);
    int index = (0x0000ffff&slot);
    
    // Now determine the address of the static field
    PTRARRAYREF bucketRef = NULL;

    bucketRef = ((CONTEXTBASEREF)GetExposedObjectRaw())->GetContextStaticsHolder();
    
    // walk the chain to our bucket
    while (bucket--)
        bucketRef = (PTRARRAYREF) bucketRef->GetAt(0);

    // Index 0 is used to point to the next bucket!
    _ASSERTE(index > 0);
    pObjRef = PTR_OBJECTREF(bucketRef->GetDataPtr())+index;

    RETURN (PTR_VOID(pObjRef));
}

#endif // FEATURE_REMOTING

#ifdef DACCESS_COMPILE

void
Context::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;
    DAC_ENUM_DTHIS();

    if (m_pDomain.IsValid())
    {
        m_pDomain->EnumMemoryRegions(flags, true);
    }
}
#endif // #ifdef DACCESS_COMPILE
