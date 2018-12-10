// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


/*============================================================
**
** COMWaitHandle.cpp
**
** Purpose: Native methods on System.WaitHandle
**
**
===========================================================*/
#include "common.h"
#include "object.h"
#include "field.h"
#include "excep.h"
#include "comwaithandle.h"


//-----------------------------------------------------------------------------
// ObjArrayHolder : ideal for holding a managed array of items.  Will run 
// the ACQUIRE method sequentially on each item.  Assume the ACQUIRE method 
// may possibly fail.  If it does, only release the ones we've acquired.
// Note: If a GC occurs during the ACQUIRE or RELEASE methods, you'll have to
// explicitly gc protect the objectref.
//-----------------------------------------------------------------------------
template <typename TYPE, void (*ACQUIRE)(TYPE), void (*RELEASEF)(TYPE)>
class ObjArrayHolder
{

public:
    ObjArrayHolder() {
        LIMITED_METHOD_CONTRACT;
        m_numAcquired = 0;
        m_pValues = NULL;
    }

    // Assuming ACQUIRE can throw an exception, we must put this logic 
    // somewhere outside of the constructor.  In C++, the destructor won't be
    // run if the constructor didn't complete.
    void Initialize(const unsigned int numElements, PTRARRAYREF* pValues) {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(m_numAcquired == 0);
        m_numElements = numElements;
        m_pValues = pValues;
        for (unsigned int i=0; i<m_numElements; i++) {
            TYPE value = (TYPE) (*m_pValues)->GetAt(i);
            ACQUIRE(value);
            m_numAcquired++;
        }
    }
        
    ~ObjArrayHolder() {
        WRAPPER_NO_CONTRACT;

        GCX_COOP();
        for (unsigned int i=0; i<m_numAcquired; i++) {
            TYPE value = (TYPE) (*m_pValues)->GetAt(i);
            RELEASEF(value);
        }
    }

private:
    unsigned int m_numElements;
    unsigned int m_numAcquired;
    PTRARRAYREF* m_pValues;

    FORCEINLINE ObjArrayHolder<TYPE, ACQUIRE, RELEASEF> &operator=(const ObjArrayHolder<TYPE, ACQUIRE, RELEASEF> &holder)
    {
        _ASSERTE(!"No assignment allowed");
        return NULL;
    }

    FORCEINLINE ObjArrayHolder(const ObjArrayHolder<TYPE, ACQUIRE, RELEASEF> &holder)
    {
        _ASSERTE(!"No copy construction allowed");
    }
};

void AcquireSafeHandleFromWaitHandle(WAITHANDLEREF wh)
{
    CONTRACTL {
        THROWS;    
        GC_TRIGGERS;    
        SO_INTOLERANT;    
        MODE_COOPERATIVE;
        PRECONDITION(wh != NULL);
    } CONTRACTL_END;

    SAFEHANDLEREF sh = wh->GetSafeHandle();
    if (sh == NULL)
        COMPlusThrow(kObjectDisposedException);
    sh->AddRef();
}

void ReleaseSafeHandleFromWaitHandle(WAITHANDLEREF wh)
{
    CONTRACTL {
        THROWS;    
        GC_TRIGGERS;    
        SO_TOLERANT;    
        MODE_COOPERATIVE;
        PRECONDITION(wh != NULL);
    } CONTRACTL_END;
    
    SAFEHANDLEREF sh = wh->GetSafeHandle();
    _ASSERTE(sh);
    sh->Release();
}

typedef ObjArrayHolder<WAITHANDLEREF, AcquireSafeHandleFromWaitHandle, ReleaseSafeHandleFromWaitHandle> WaitHandleArrayHolder;

INT64 AdditionalWait(INT64 sPauseTime, INT64 sTime, INT64 expDuration)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(g_PauseTime >= sPauseTime);

    INT64 pauseTime = g_PauseTime - sPauseTime;
    // No pause was used inbetween this handle
    if(pauseTime <= 0)
        return 0;

    INT64 actDuration = CLRGetTickCount64() - sTime;

    // In case the CLR is paused inbetween a wait, this method calculates how much 
    // the wait has to be adjusted to account for the CLR Freeze. Essentially all
    // pause duration has to be considered as "time that never existed".
    //
    // Two cases exists, consider that 10 sec wait is issued 
    // Case 1: All pauses happened before the wait completes. Hence just the 
    // pause time needs to be added back at the end of wait
    // 0           3                   8       10
    // |-----------|###################|------>
    //                 5-sec pause    
    //             ....................>
    //                                            Additional 5 sec wait
    //                                        |=========================> 
    //
    // Case 2: Pauses ended after the wait completes. 
    // 3 second of wait was left as the pause started at 7 so need to add that back
    // 0                           7           10
    // |---------------------------|###########>
    //                                 5-sec pause   12
    //                             ...................>
    //                                            Additional 3 sec wait
    //                                                |==================> 
    //
    // Both cases can be expressed in the same calculation
    // pauseTime:   sum of all pauses that were triggered after the timer was started
    // expDuration: expected duration of the wait (without any pauses) 10 in the example
    // actDuration: time when the wait finished. Since the CLR is frozen during pause it's
    //              max of timeout or pause-end. In case-1 it's 10, in case-2 it's 12
    INT64 additional = expDuration - (actDuration - pauseTime);
    if(additional < 0)
        additional = 0;

    return additional;
}

FCIMPL4(INT32, WaitHandleNative::CorWaitOneNative, SafeHandle* safeWaitHandleUNSAFE, INT32 timeout, CLR_BOOL hasThreadAffinity, CLR_BOOL exitContext)
{
    FCALL_CONTRACT;

    INT32 retVal = 0;
    SAFEHANDLEREF sh(safeWaitHandleUNSAFE);
    HELPER_METHOD_FRAME_BEGIN_RET_1(sh);

    _ASSERTE(sh != NULL);

    Thread* pThread = GET_THREAD();

    DWORD res = (DWORD) -1;

    SafeHandleHolder shh(&sh);
    // Note that SafeHandle is a GC object, and RequestCallback and 
    // DoAppropriateWait work on an array of handles.  Don't pass the address
    // of the handle field - that's a GC hole.  Instead, pass this temp
    // array.
    HANDLE handles[1];
    handles[0] = sh->GetHandle();
    {
        // Support for pause/resume (FXFREEZE)
        while(true)
        {
            INT64 sPauseTime = g_PauseTime;
            INT64 sTime = CLRGetTickCount64();
            res = pThread->DoAppropriateWait(1,handles,TRUE,timeout, WaitMode_Alertable /*alertable*/);
            if(res != WAIT_TIMEOUT)
                break;
            timeout = (INT32)AdditionalWait(sPauseTime, sTime, timeout);
            if(timeout == 0)
                break;
        }
    }

    retVal = res;


    HELPER_METHOD_FRAME_END();
    return retVal;
}
FCIMPLEND

FCIMPL4(INT32, WaitHandleNative::CorWaitMultipleNative, Object* waitObjectsUNSAFE, INT32 timeout, CLR_BOOL exitContext, CLR_BOOL waitForAll)
{
    FCALL_CONTRACT;

    INT32 retVal = 0;
    OBJECTREF waitObjects = (OBJECTREF) waitObjectsUNSAFE;
    HELPER_METHOD_FRAME_BEGIN_RET_1(waitObjects);

    _ASSERTE(waitObjects);

    Thread* pThread = GET_THREAD();

    PTRARRAYREF pWaitObjects = (PTRARRAYREF)waitObjects;  // array of objects on which to wait
    int numWaiters = pWaitObjects->GetNumComponents();

#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT
    // There are some issues with wait-all from an STA thread
    // - https://github.com/dotnet/coreclr/issues/17787#issuecomment-385117537
    if (waitForAll && numWaiters > 1 && pThread->GetApartment() == Thread::AS_InSTA)
    {
        COMPlusThrow(kNotSupportedException, W("NotSupported_WaitAllSTAThread"));
    }
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT

    WaitHandleArrayHolder arrayHolder;
    arrayHolder.Initialize(numWaiters, (PTRARRAYREF*) &waitObjects);
    
    pWaitObjects = (PTRARRAYREF)waitObjects;  // array of objects on which to wait
    HANDLE* internalHandles = (HANDLE*) _alloca(numWaiters*sizeof(HANDLE));
    for (int i=0;i<numWaiters;i++)
    {
        WAITHANDLEREF waitObject = (WAITHANDLEREF) pWaitObjects->m_Array[i];
        _ASSERTE(waitObject != NULL);

        //If the size of the array is 1 and m_handle is INVALID_HANDLE then WaitForMultipleObjectsEx will
        //   return ERROR_INVALID_HANDLE but DoAppropriateWait will convert to WAIT_OBJECT_0.  i.e Success,
        //   this behavior seems wrong but someone explicitly coded that condition so it must have been for a reason.        
        internalHandles[i] = waitObject->m_handle;

    }

    DWORD res = (DWORD) -1;
    {
        // Support for pause/resume (FXFREEZE)
        while(true)
        {
            INT64 sPauseTime = g_PauseTime;
            INT64 sTime = CLRGetTickCount64();
            res = pThread->DoAppropriateWait(numWaiters, internalHandles, waitForAll, timeout, WaitMode_Alertable /*alertable*/);
            if(res != WAIT_TIMEOUT)
                break;
            timeout = (INT32)AdditionalWait(sPauseTime, sTime, timeout);
            if(timeout == 0)
                break;
        }
    }


    retVal = res;

    HELPER_METHOD_FRAME_END();
    return retVal;
}
FCIMPLEND

FCIMPL5(INT32, WaitHandleNative::CorSignalAndWaitOneNative, SafeHandle* safeWaitHandleSignalUNSAFE,SafeHandle* safeWaitHandleWaitUNSAFE, INT32 timeout, CLR_BOOL hasThreadAffinity, CLR_BOOL exitContext)
{
    FCALL_CONTRACT;

    INT32 retVal = 0;
    SAFEHANDLEREF shSignal(safeWaitHandleSignalUNSAFE);
    SAFEHANDLEREF shWait(safeWaitHandleWaitUNSAFE);

    HELPER_METHOD_FRAME_BEGIN_RET_2(shSignal,shWait);

    if(shSignal == NULL || shWait == NULL)
        COMPlusThrow(kObjectDisposedException);    

    _ASSERTE(safeWaitHandleSignalUNSAFE != NULL);
    _ASSERTE( safeWaitHandleWaitUNSAFE != NULL);


    Thread* pThread = GET_THREAD();

#ifdef FEATURE_COMINTEROP
    if (pThread->GetApartment() == Thread::AS_InSTA) {
        COMPlusThrow(kNotSupportedException, W("NotSupported_SignalAndWaitSTAThread"));  //<TODO> Change this message
    }
#endif

    DWORD res = (DWORD) -1;

    SafeHandleHolder shhSignal(&shSignal);
    SafeHandleHolder shhWait(&shWait);
    // Don't pass the address of the handle field 
    // - that's a GC hole.  Instead, pass this temp array.
    HANDLE handles[2];
    handles[0] = shSignal->GetHandle();
    handles[1] = shWait->GetHandle();
    {
        res = pThread->DoSignalAndWait(handles,timeout,TRUE /*alertable*/);
    }


    retVal = res;

    HELPER_METHOD_FRAME_END();
    return retVal;
}
FCIMPLEND
