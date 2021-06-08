// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


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
#include "comsynchronizable.h"
#include "comthreadpool.h"
#include "marshalnative.h"

//
//The function is called from managed code to quicly check if a packet is available.
//This is a perf-critical function. Even helper method frames are not created. We fall
//back to the VM to do heavy weight operations like creating a new CP thread.
//
FCIMPL3(void, CheckVMForIOPacket, LPOVERLAPPED* lpOverlapped, DWORD* errorCode, DWORD* numBytes)
{
    FCALL_CONTRACT;

#ifndef TARGET_UNIX
    Thread *pThread = GetThread();
    size_t key=0;

    //Poll and wait if GC is in progress, to avoid blocking GC for too long.
    FC_GC_POLL();

    *lpOverlapped = ThreadpoolMgr::CompletionPortDispatchWorkWithinAppDomain(pThread, errorCode, numBytes, &key);
    if(*lpOverlapped == NULL)
    {
        return;
    }

    OVERLAPPEDDATAREF overlapped = ObjectToOVERLAPPEDDATAREF(OverlappedDataObject::GetOverlapped(*lpOverlapped));

    if (overlapped->m_callback == NULL)
    {
        //We're not initialized yet, go back to the Vm, and process the packet there.
        ThreadpoolMgr::StoreOverlappedInfoInThread(pThread, *errorCode, *numBytes, key, *lpOverlapped);

        *lpOverlapped = NULL;
        return;
    }
    else
    {
        if(!pThread->IsRealThreadPoolResetNeeded())
        {
            pThread->ResetManagedThreadObjectInCoopMode(ThreadNative::PRIORITY_NORMAL);
            pThread->InternalReset(TRUE, FALSE, FALSE);
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
    if (*lpOverlapped != NULL && ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context, ThreadPoolIODequeue))
        FireEtwThreadPoolIODequeue(*lpOverlapped, OverlappedDataObject::GetOverlapped(*lpOverlapped), GetClrInstanceId());

#else // !TARGET_UNIX
    *lpOverlapped = NULL;
#endif // !TARGET_UNIX

    return;
}
FCIMPLEND

FCIMPL1(LPOVERLAPPED, AllocateNativeOverlapped, OverlappedDataObject* overlappedUNSAFE)
{
    FCALL_CONTRACT;

    LPOVERLAPPED lpOverlapped;

    OVERLAPPEDDATAREF   overlapped   = ObjectToOVERLAPPEDDATAREF(overlappedUNSAFE);
    OBJECTREF       userObject = overlapped->m_userObject;

    HELPER_METHOD_FRAME_BEGIN_RET_ATTRIB_2(Frame::FRAME_ATTR_NONE, overlapped, userObject);

    if (g_pOverlappedDataClass == NULL)
    {
        g_pOverlappedDataClass = CoreLibBinder::GetClass(CLASS__OVERLAPPEDDATA);
        // We have optimization to avoid creating event if IO is in default domain.  This depends on default domain
        // can not be unloaded.
    }

    CONSISTENCY_CHECK(overlapped->GetMethodTable() == g_pOverlappedDataClass);

    if (userObject != NULL)
    {
        if (userObject->GetMethodTable() == g_pPredefinedArrayTypes[ELEMENT_TYPE_OBJECT].AsMethodTable())
        {
            BASEARRAYREF asArray = (BASEARRAYREF) userObject;
            OBJECTREF *pObj = (OBJECTREF*)(asArray->GetDataPtr());
            SIZE_T num = asArray->GetNumComponents();
            SIZE_T i;
            for (i = 0; i < num; i ++)
            {
                ValidatePinnedObject(pObj[i]);
            }
        }
        else
        {
            ValidatePinnedObject(userObject);
        }
    }

    NewHolder<NATIVEOVERLAPPED_AND_HANDLE> overlappedHolder(new NATIVEOVERLAPPED_AND_HANDLE());
    overlappedHolder->m_handle = GetAppDomain()->CreateTypedHandle(overlapped, HNDTYPE_ASYNCPINNED);
    lpOverlapped = &(overlappedHolder.Extract()->m_overlapped);

    lpOverlapped->Internal = 0;
    lpOverlapped->InternalHigh = 0;
    lpOverlapped->Offset = overlapped->m_offsetLow;
    lpOverlapped->OffsetHigh = overlapped->m_offsetHigh;
    lpOverlapped->hEvent = (HANDLE)overlapped->m_eventHandle;

    overlapped->m_pNativeOverlapped = lpOverlapped;

    HELPER_METHOD_FRAME_END();
    LOG((LF_INTEROP, LL_INFO10000, "In AllocNativeOperlapped thread 0x%x\n", GetThread()));

    if (ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context, ThreadPoolIODequeue))
        FireEtwThreadPoolIOPack(lpOverlapped, overlappedUNSAFE, GetClrInstanceId());

    return lpOverlapped;
}
FCIMPLEND

FCIMPL1(void, FreeNativeOverlapped, LPOVERLAPPED lpOverlapped)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_0();

    CONSISTENCY_CHECK(g_pOverlappedDataClass && (OverlappedDataObject::GetOverlapped(lpOverlapped)->GetMethodTable() == g_pOverlappedDataClass));

    DestroyAsyncPinningHandle(((NATIVEOVERLAPPED_AND_HANDLE*)lpOverlapped)->m_handle);
    delete lpOverlapped;

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
