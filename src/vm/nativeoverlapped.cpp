// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


/*============================================================
**
** Header: COMNativeOverlapped.h
**
** Purpose: Native methods for allocating and freeing NativeOverlapped
**

** 
===========================================================*/
#include "common.h"
#include "fcall.h"
#include "nativeoverlapped.h"
#include "corhost.h"
#include "win32threadpool.h"
#include "mdaassistants.h"
#include "comsynchronizable.h"
#include "comthreadpool.h"

LONG OverlappedDataObject::s_CleanupRequestCount = 0;
BOOL OverlappedDataObject::s_CleanupInProgress = FALSE;
BOOL OverlappedDataObject::s_GCDetectsCleanup = FALSE;
BOOL OverlappedDataObject::s_CleanupFreeHandle = FALSE;

//
//The function is called from managed code to quicly check if a packet is available.
//This is a perf-critical function. Even helper method frames are not created. We fall
//back to the VM to do heavy weight operations like creating a new CP thread. 
//
FCIMPL3(void, CheckVMForIOPacket, LPOVERLAPPED* lpOverlapped, DWORD* errorCode, DWORD* numBytes)
{
    FCALL_CONTRACT;

#ifndef FEATURE_PAL       
    Thread *pThread = GetThread();
    DWORD adid = pThread->GetDomain()->GetId().m_dwId;
    size_t key=0;

    _ASSERTE(pThread);  

    //Poll and wait if GC is in progress, to avoid blocking GC for too long.    
    FC_GC_POLL();

    *lpOverlapped = ThreadpoolMgr::CompletionPortDispatchWorkWithinAppDomain(pThread, errorCode, numBytes, &key, adid);
    if(*lpOverlapped == NULL)
    {
        return;
    }

    OVERLAPPEDDATAREF overlapped = ObjectToOVERLAPPEDDATAREF(OverlappedDataObject::GetOverlapped(*lpOverlapped));

    _ASSERTE(overlapped->GetAppDomainId() == adid);
    _ASSERTE(CLRIoCompletionHosted() == FALSE);

    if(overlapped->m_iocb == NULL)
    {
        // no user delegate to callback
        _ASSERTE((overlapped->m_iocbHelper == NULL) || !"This is benign, but should be optimized");        

        if (g_pAsyncFileStream_AsyncResultClass)
        {
            SetAsyncResultProperties(overlapped, *errorCode, *numBytes);
        } 
        else 
        {
            //We're not initialized yet, go back to the Vm, and process the packet there.
            ThreadpoolMgr::StoreOverlappedInfoInThread(pThread, *errorCode, *numBytes, key, *lpOverlapped);
        }

        *lpOverlapped = NULL;
        return;
    }
    else
    {        
        if(!pThread->IsRealThreadPoolResetNeeded())
        {
            pThread->ResetManagedThreadObjectInCoopMode(ThreadNative::PRIORITY_NORMAL);
            pThread->InternalReset(FALSE, TRUE, FALSE, FALSE);  
            if(ThreadpoolMgr::ShouldGrowCompletionPortThreadpool(ThreadpoolMgr::CPThreadCounter.DangerousGetDirtyCounts()))
            {
                //We may have to create a CP thread, go back to the Vm, and process the packet there.
                ThreadpoolMgr::StoreOverlappedInfoInThread(pThread, *errorCode, *numBytes, key, *lpOverlapped);
                *lpOverlapped = NULL;              
            }
        }
        else
        {
            //A more complete reset is needed (due to change in priority etc), go back to the VM, 
            //and process the packet there.

            ThreadpoolMgr::StoreOverlappedInfoInThread(pThread, *errorCode, *numBytes, key, *lpOverlapped);
            *lpOverlapped = NULL;              
        }
    }

    // if this will be "dispatched" to the managed callback fire the IODequeue event:
    if (*lpOverlapped != NULL && ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context, ThreadPoolIODequeue))
        FireEtwThreadPoolIODequeue(*lpOverlapped, (BYTE*)(*lpOverlapped) - offsetof(OverlappedDataObject, Internal), GetClrInstanceId());

#else // !FEATURE_PAL
    *lpOverlapped = NULL;
#endif // !FEATURE_PAL

    return;     
} 
FCIMPLEND

FCIMPL1(void*, AllocateNativeOverlapped, OverlappedDataObject* overlappedUNSAFE)
{
    FCALL_CONTRACT;

    OVERLAPPEDDATAREF   overlapped   = ObjectToOVERLAPPEDDATAREF(overlappedUNSAFE);
    OBJECTREF       userObject = overlapped->m_userObject;

    HELPER_METHOD_FRAME_BEGIN_RET_ATTRIB_2(Frame::FRAME_ATTR_NONE, overlapped, userObject);

    AsyncPinningHandleHolder handle;

    if (g_pOverlappedDataClass == NULL)
    {
        g_pOverlappedDataClass = MscorlibBinder::GetClass(CLASS__OVERLAPPEDDATA);
        // We have optimization to avoid creating event if IO is in default domain.  This depends on default domain 
        // can not be unloaded.
        _ASSERTE(IsSingleAppDomain() || !SystemDomain::System()->DefaultDomain()->CanUnload());
        _ASSERTE(SystemDomain::System()->DefaultDomain()->GetId().m_dwId == DefaultADID);
    }

    CONSISTENCY_CHECK(overlapped->GetMethodTable() == g_pOverlappedDataClass);

    overlapped->m_AppDomainId = GetAppDomain()->GetId().m_dwId;

    if (userObject != NULL)
    {
        if (overlapped->m_isArray == 1)
        {
            BASEARRAYREF asArray = (BASEARRAYREF) userObject;
            OBJECTREF *pObj = (OBJECTREF*)(asArray->GetDataPtr());
            SIZE_T num = asArray->GetNumComponents();
            SIZE_T i;
            for (i = 0; i < num; i ++)
            {
                GCHandleValidatePinnedObject(pObj[i]);
            }
            for (i = 0; i < num; i ++)
            {
                asArray = (BASEARRAYREF) userObject;
                AddMTForPinHandle(pObj[i]);
            }
        }
        else
        {
            GCHandleValidatePinnedObject(userObject);
            AddMTForPinHandle(userObject);
        }
        
    }

    handle = GetAppDomain()->CreateTypedHandle(overlapped, HNDTYPE_ASYNCPINNED);

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
    // CoreCLR does not have IO completion hosted
    if (CLRIoCompletionHosted()) 
    {
        _ASSERTE(CorHost2::GetHostIoCompletionManager());
        HRESULT hr;
        BEGIN_SO_TOLERANT_CODE_CALLING_HOST(GetThread());
        hr = CorHost2::GetHostIoCompletionManager()->InitializeHostOverlapped(&overlapped->Internal);
        END_SO_TOLERANT_CODE_CALLING_HOST;
        if (FAILED(hr)) 
        {
            COMPlusThrowHR(hr);
        }
    }
#endif // FEATURE_INCLUDE_ALL_INTERFACES

    handle.SuppressRelease();
    overlapped->m_pinSelf = handle;

    HELPER_METHOD_FRAME_END();
    LOG((LF_INTEROP, LL_INFO10000, "In AllocNativeOperlapped thread 0x%x\n", GetThread()));

    if (ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context, ThreadPoolIODequeue))
        FireEtwThreadPoolIOPack(&overlapped->Internal, overlappedUNSAFE, GetClrInstanceId());

    return &overlapped->Internal;
}
FCIMPLEND

FCIMPL1(void, FreeNativeOverlapped, LPOVERLAPPED lpOverlapped)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_0();

    OVERLAPPEDDATAREF overlapped = ObjectToOVERLAPPEDDATAREF(OverlappedDataObject::GetOverlapped(lpOverlapped));
    CONSISTENCY_CHECK(g_pOverlappedDataClass && (overlapped->GetMethodTable() == g_pOverlappedDataClass));

    // We don't want to call HasCompleted in the default domain, because we don't have 
    // overlapped handle support.
    if ((!overlapped->HasCompleted ()))
    {
#ifdef MDA_SUPPORTED
        MdaOverlappedFreeError *pFreeError = MDA_GET_ASSISTANT(OverlappedFreeError);
        if (pFreeError)
        {
            pFreeError->ReportError((LPVOID) OVERLAPPEDDATAREFToObject(overlapped)); 

            // If we entered ReportError then our overlapped OBJECTREF became technically invalid,
            // since a gc can be triggered. That causes an assert from FreeAsyncPinHandles() below.
            // (I say technically because the object is pinned and won't really move)
            overlapped = ObjectToOVERLAPPEDDATAREF(OverlappedDataObject::GetOverlapped(lpOverlapped));
        }        
#endif // MDA_SUPPORTED
    }

    overlapped->FreeAsyncPinHandles();
    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

FCIMPL1(OverlappedDataObject*, GetOverlappedFromNative, LPOVERLAPPED lpOverlapped)
{
    FCALL_CONTRACT;

    CONSISTENCY_CHECK(g_pOverlappedDataClass && (OverlappedDataObject::GetOverlapped(lpOverlapped)->GetMethodTable() == g_pOverlappedDataClass));

    return OverlappedDataObject::GetOverlapped(lpOverlapped);
}
FCIMPLEND

void OverlappedDataObject::FreeAsyncPinHandles()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    // This cannot throw or return error, and cannot force SO because it is called
    // from CCLRIoCompletionManager::OnComplete which probes.
    CONTRACT_VIOLATION(SOToleranceViolation);

    CONSISTENCY_CHECK(g_pOverlappedDataClass && (this->GetMethodTable() == g_pOverlappedDataClass));

    _ASSERTE(GetThread() != NULL);

    if (m_pinSelf)
    {
        OBJECTHANDLE h = m_pinSelf;
        if (FastInterlockCompareExchangePointer(&m_pinSelf, static_cast<OBJECTHANDLE>(NULL), h) == h)
        {
            DestroyAsyncPinningHandle(h);
        }
    }

    EventHandle = 0;
}


void OverlappedDataObject::StartCleanup()
{
    CONTRACTL
    {
        NOTHROW;
        if (GetThread()) {MODE_COOPERATIVE;} else {DISABLED(MODE_COOPERATIVE);}
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    if (s_CleanupRequestCount == 0)
    {
        return;
    }

    LONG curCount = s_CleanupRequestCount;
    if (FastInterlockExchange((LONG*)&s_CleanupInProgress, TRUE) == FALSE)
    {
        {
            BOOL HasJob = Ref_HandleAsyncPinHandles();
            if (!HasJob)
            {
                s_CleanupInProgress = FALSE;
                FastInterlockExchangeAdd (&s_CleanupRequestCount, -curCount);
                return;
            }
        }

        if (!ThreadpoolMgr::DrainCompletionPortQueue())
        {
            s_CleanupInProgress = FALSE;
        }
        else
        {
            FastInterlockExchangeAdd (&s_CleanupRequestCount, -curCount);
        }
    }
}


void OverlappedDataObject::FinishCleanup(bool wasDrained)
{
    WRAPPER_NO_CONTRACT;

    if (wasDrained)
    {
        GCX_COOP();

        s_CleanupFreeHandle = TRUE;
        Ref_HandleAsyncPinHandles();
        s_CleanupFreeHandle = FALSE;

        s_CleanupInProgress = FALSE;
        if (s_CleanupRequestCount > 0)
        {
            StartCleanup();
        }
    }
    else
    {
        s_CleanupInProgress = FALSE;
    }
}


void OverlappedDataObject::HandleAsyncPinHandle()
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE (s_CleanupInProgress);
    if (m_toBeCleaned || !ThreadpoolMgr::IsCompletionPortInitialized())
    {
        OBJECTHANDLE h = m_pinSelf;
        if (h)
        {
            if (FastInterlockCompareExchangePointer(&m_pinSelf, (OBJECTHANDLE)NULL, h) == h)
            {
                DestroyAsyncPinningHandle(h);
            }
        }
    }
    else if (!s_CleanupFreeHandle)
    {
        m_toBeCleaned = 1;
    }
}


// A hash table to track size of objects that may be moved to default domain
typedef EEHashTable<size_t, EEPtrHashTableHelper<size_t>, FALSE> EEHashTableOfMT;
EEHashTableOfMT *s_pPinHandleTable;

CrstStatic s_PinHandleTableCrst;

void InitializePinHandleTable()
{
    WRAPPER_NO_CONTRACT;

    s_PinHandleTableCrst.Init(CrstPinHandle);
    LockOwner lock = {&s_PinHandleTableCrst, IsOwnerOfCrst};
    s_pPinHandleTable = new EEHashTableOfMT();
    s_pPinHandleTable->Init(10, &lock);
}

// We can not fail due to OOM when we move an object to default domain during AD unload.
// If we may need a dummy MethodTable later, we allocate the MethodTable here.
void AddMTForPinHandle(OBJECTREF obj)
{
    CONTRACTL
    {
        THROWS;
        WRAPPER(GC_TRIGGERS);
    }
    CONTRACTL_END;

    if (obj == NULL)
    {
        return;
    }

    _ASSERTE (g_pOverlappedDataClass != NULL);

    SSIZE_T size = 0;
    MethodTable *pMT = obj->GetMethodTable();

    if (pMT->GetLoaderModule()->IsSystem())
    {
        return;
    }

    if (pMT->IsArray())
    {
#ifdef _DEBUG
        BASEARRAYREF asArray = (BASEARRAYREF) obj;
        TypeHandle th = asArray->GetArrayElementTypeHandle();
        _ASSERTE (!th.IsTypeDesc());
        MethodTable *pElemMT = th.AsMethodTable();
        _ASSERTE (pElemMT->IsValueType() && pElemMT->IsBlittable());
        _ASSERTE (!pElemMT->GetLoaderModule()->IsSystem());
#endif

        // Create an ArrayMethodTable that has the same element size
        // Use negative number for arrays of structs - it assumes that 
        // the maximum type base size is less than 2GB.
        size = - (SSIZE_T)pMT->GetComponentSize();
        _ASSERTE(size < 0);
    }
    else
    {
        size = pMT->GetBaseSize();
        _ASSERTE(size >= 0);
    }

    HashDatum data;
    if (s_pPinHandleTable->GetValue(size, &data) == FALSE)
    {
        CrstHolder csh(&s_PinHandleTableCrst);
        if (s_pPinHandleTable->GetValue(size, &data) == FALSE)
        {
            // We do not need to include GCDescr here, since this
            // methodtable does not contain pointers.
            BYTE *buffer = new BYTE[sizeof(MethodTable)];
            memset (buffer, 0, sizeof(MethodTable));
            MethodTable *pNewMT = (MethodTable *)buffer;
            NewArrayHolder<BYTE> pMTHolder(buffer);
            pNewMT->SetIsAsyncPinType();
            if (size >= 0)
            {
                pNewMT->SetBaseSize(static_cast<DWORD>(size));
            }
            else
            {
                pNewMT->SetBaseSize(ObjSizeOf (ArrayBase));
                pNewMT->SetComponentSize(static_cast<WORD>(-size));
            }
            s_pPinHandleTable->InsertValue(size, (HashDatum)pNewMT);
            pMTHolder.SuppressRelease();
        }
    }
}

// We need to ensure that the MethodTable of an object is valid in default domain when the object
// is move to default domain duing AD unload.
void BashMTForPinnedObject(OBJECTREF obj)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
    }
    CONTRACTL_END;

    if (obj == NULL)
    {
        return;
    }

    ADIndex adIndx = obj->GetAppDomainIndex();
    ADIndex defaultAdIndx = SystemDomain::System()->DefaultDomain()->GetIndex();
    if (adIndx.m_dwIndex != 0 && adIndx != defaultAdIndx)
    {
        obj->GetHeader()->ResetAppDomainIndexNoFailure(defaultAdIndx);
    }
    SSIZE_T size = 0;
    MethodTable *pMT = obj->GetMethodTable();

    if (pMT == g_pOverlappedDataClass)
    {
        // Managed Overlapped
        OVERLAPPEDDATAREF overlapped = (OVERLAPPEDDATAREF)(obj);
        overlapped->m_asyncResult = NULL;
        overlapped->m_iocb = NULL;
        overlapped->m_iocbHelper = NULL;
        overlapped->m_overlapped = NULL;

        if (overlapped->m_userObject != NULL)
        {
            if (overlapped->m_isArray == 1)
            {
                BASEARRAYREF asArray = (BASEARRAYREF) (overlapped->m_userObject);
                OBJECTREF *pObj = (OBJECTREF*)asArray->GetDataPtr (TRUE);
                SIZE_T num = asArray->GetNumComponents();
                for (SIZE_T i = 0; i < num; i ++)
                {
                    BashMTForPinnedObject(pObj[i]);
                }
            }
            else
            {
                BashMTForPinnedObject(overlapped->m_userObject);
            }
        }
        STRESS_LOG1 (LF_APPDOMAIN | LF_GC, LL_INFO100, "OverlappedData %p:MT is bashed\n", OBJECTREFToObject (overlapped));
        return;
    }

    if (pMT->GetLoaderModule()->IsSystem())
    {
        return;
    }

    if (pMT->IsArray())
    {
#ifdef _DEBUG
        BASEARRAYREF asArray = (BASEARRAYREF) obj;
        TypeHandle th = asArray->GetArrayElementTypeHandle();
        _ASSERTE (!th.IsTypeDesc());
        MethodTable *pElemMT = th.AsMethodTable();
        _ASSERTE (pElemMT->IsValueType() && pElemMT->IsBlittable());
        _ASSERTE (!pElemMT->GetLoaderModule()->IsSystem());
#endif

        // Create an ArrayMethodTable that has the same element size
        size = - (SSIZE_T)pMT->GetComponentSize();
    }
    else 
    {
        _ASSERTE (pMT->IsBlittable());
        size = pMT->GetBaseSize();
    }
    
    HashDatum data = NULL;
    BOOL fRet;
    fRet = s_pPinHandleTable->GetValue(size, &data);
    _ASSERTE(fRet);
    PREFIX_ASSUME(data != NULL);
    obj->SetMethodTable((MethodTable*)data);
}
