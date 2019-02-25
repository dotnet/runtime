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

FCIMPL2(INT32, WaitHandleNative::CorWaitOneNative, HANDLE handle, INT32 timeout)
{
    FCALL_CONTRACT;

    INT32 retVal = 0;
    HELPER_METHOD_FRAME_BEGIN_RET_0();

    _ASSERTE(handle != 0);
    _ASSERTE(handle != INVALID_HANDLE_VALUE);

    Thread* pThread = GET_THREAD();

    DWORD res = (DWORD) -1;

    // Support for pause/resume (FXFREEZE)
    while(true)
    {
        INT64 sPauseTime = g_PauseTime;
        INT64 sTime = CLRGetTickCount64();
        res = pThread->DoAppropriateWait(1, &handle, TRUE, timeout, (WaitMode)(WaitMode_Alertable | WaitMode_IgnoreSyncCtx));
        if(res != WAIT_TIMEOUT)
            break;
        timeout = (INT32)AdditionalWait(sPauseTime, sTime, timeout);
        if(timeout == 0)
            break;
    }

    retVal = res;

    HELPER_METHOD_FRAME_END();
    return retVal;
}
FCIMPLEND

FCIMPL4(INT32, WaitHandleNative::CorWaitMultipleNative, HANDLE *handleArray, INT32 numHandles, CLR_BOOL waitForAll, INT32 timeout)
{
    FCALL_CONTRACT;

    INT32 ret = 0;
    HELPER_METHOD_FRAME_BEGIN_RET_0();

    Thread * pThread = GET_THREAD();

#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT
    // There are some issues with wait-all from an STA thread
    // - https://github.com/dotnet/coreclr/issues/17787#issuecomment-385117537
    if (waitForAll && numHandles > 1 && pThread->GetApartment() == Thread::AS_InSTA)
    {
        COMPlusThrow(kNotSupportedException, W("NotSupported_WaitAllSTAThread"));
    }
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT

    DWORD res = (DWORD) -1;
    {
        // Support for pause/resume (FXFREEZE)
        while(true)
        {
            INT64 sPauseTime = g_PauseTime;
            INT64 sTime = CLRGetTickCount64();
            res = pThread->DoAppropriateWait(numHandles, handleArray, waitForAll, timeout, (WaitMode)(WaitMode_Alertable | WaitMode_IgnoreSyncCtx));
            if(res != WAIT_TIMEOUT)
                break;
            timeout = (INT32)AdditionalWait(sPauseTime, sTime, timeout);
            if(timeout == 0)
                break;
        }
    }

    ret = res;
    
    HELPER_METHOD_FRAME_END();
    return ret;
}
FCIMPLEND

FCIMPL3(INT32, WaitHandleNative::CorSignalAndWaitOneNative, HANDLE waitHandleSignalUNSAFE, HANDLE waitHandleWaitUNSAFE, INT32 timeout)
{
    FCALL_CONTRACT;

    INT32 retVal = 0;

    HELPER_METHOD_FRAME_BEGIN_RET_0();

    _ASSERTE(waitHandleSignalUNSAFE != 0);
    _ASSERTE(waitHandleWaitUNSAFE != 0);

    Thread* pThread = GET_THREAD();

#ifdef FEATURE_COMINTEROP
    if (pThread->GetApartment() == Thread::AS_InSTA) {
        COMPlusThrow(kNotSupportedException, W("NotSupported_SignalAndWaitSTAThread"));  //<TODO> Change this message
    }
#endif

    DWORD res = (DWORD) -1;

    HANDLE handles[2];
    handles[0] = waitHandleSignalUNSAFE;
    handles[1] = waitHandleWaitUNSAFE;
    {
        res = pThread->DoSignalAndWait(handles, timeout, TRUE /*alertable*/);
    }

    retVal = res;

    HELPER_METHOD_FRAME_END();
    return retVal;
}
FCIMPLEND
