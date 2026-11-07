// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "eventpipeadapter.h"
#include "eventpipeinternal.h"

#include <minipal/guid.h>

#ifdef FEATURE_PERFTRACING

extern "C" UINT64 QCALLTYPE EventPipeInternal_Enable(
    LPCWSTR,
    EventPipeSerializationFormat,
    UINT32,
    LPCVOID,
    UINT32,
    QCallExceptionStatus *qcallError)
{
    QCALL_CONTRACT_NO_GC_TRANSITION;
    *qcallError = 0;
    return 0;
}

extern "C" void QCALLTYPE EventPipeInternal_Disable(UINT64, QCallExceptionStatus *qcallError)
{
    QCALL_CONTRACT_NO_GC_TRANSITION;
    *qcallError = 0;
}

extern "C" BOOL QCALLTYPE EventPipeInternal_GetSessionInfo(
    UINT64,
    EventPipeSessionInfo *,
    QCallExceptionStatus *qcallError)
{
    QCALL_CONTRACT_NO_GC_TRANSITION;
    *qcallError = 0;
    return FALSE;
}

extern "C" INT_PTR QCALLTYPE EventPipeInternal_CreateProvider(
    LPCWSTR,
    EventPipeCallback,
    void *,
    QCallExceptionStatus *qcallError)
{
    QCALL_CONTRACT_NO_GC_TRANSITION;
    *qcallError = 0;
    return 0;
}

extern "C" INT_PTR QCALLTYPE EventPipeInternal_DefineEvent(
    INT_PTR,
    UINT32,
    int64_t,
    UINT32,
    UINT32,
    void *,
    UINT32,
    QCallExceptionStatus *qcallError)
{
    QCALL_CONTRACT_NO_GC_TRANSITION;
    *qcallError = 0;
    return 0;
}

extern "C" INT_PTR QCALLTYPE EventPipeInternal_GetProvider(LPCWSTR, QCallExceptionStatus *qcallError)
{
    QCALL_CONTRACT_NO_GC_TRANSITION;
    *qcallError = 0;
    return 0;
}

extern "C" void QCALLTYPE EventPipeInternal_DeleteProvider(INT_PTR, QCallExceptionStatus *qcallError)
{
    QCALL_CONTRACT_NO_GC_TRANSITION;
    *qcallError = 0;
}

extern "C" int QCALLTYPE EventPipeInternal_EventActivityIdControl(
    uint32_t controlCode,
    GUID *activityId,
    QCallExceptionStatus *qcallError)
{
    QCALL_CONTRACT_NO_GC_TRANSITION;
    *qcallError = 0;

    Thread *thread = GetThreadNULLOk();
    if (thread == nullptr || activityId == nullptr)
    {
        return 1;
    }

    GUID currentActivityId;
    switch (static_cast<ActivityControlCode>(controlCode))
    {
        case ActivityControlCode::EVENT_ACTIVITY_CONTROL_GET_ID:
            *activityId = *thread->GetActivityId();
            break;

        case ActivityControlCode::EVENT_ACTIVITY_CONTROL_SET_ID:
            thread->SetActivityId(activityId);
            break;

        case ActivityControlCode::EVENT_ACTIVITY_CONTROL_CREATE_ID:
            minipal_guid_v4_create(activityId);
            break;

        case ActivityControlCode::EVENT_ACTIVITY_CONTROL_GET_SET_ID:
            currentActivityId = *thread->GetActivityId();
            thread->SetActivityId(activityId);
            *activityId = currentActivityId;
            break;

        case ActivityControlCode::EVENT_ACTIVITY_CONTROL_CREATE_SET_ID:
            *activityId = *thread->GetActivityId();
            minipal_guid_v4_create(&currentActivityId);
            thread->SetActivityId(&currentActivityId);
            break;

        default:
            return 1;
    }

    return 0;
}

extern "C" void QCALLTYPE EventPipeInternal_WriteEventData(
    INT_PTR,
    EventData *,
    UINT32,
    LPCGUID,
    LPCGUID,
    QCallExceptionStatus *qcallError)
{
    QCALL_CONTRACT_NO_GC_TRANSITION;
    *qcallError = 0;
}

extern "C" BOOL QCALLTYPE EventPipeInternal_GetNextEvent(
    UINT64,
    EventPipeEventInstanceData *,
    QCallExceptionStatus *qcallError)
{
    QCALL_CONTRACT_NO_GC_TRANSITION;
    *qcallError = 0;
    return FALSE;
}

extern "C" BOOL QCALLTYPE EventPipeInternal_SignalSession(UINT64, QCallExceptionStatus *qcallError)
{
    QCALL_CONTRACT_NO_GC_TRANSITION;
    *qcallError = 0;
    return FALSE;
}

extern "C" BOOL QCALLTYPE EventPipeInternal_WaitForSessionSignal(
    UINT64,
    INT32,
    QCallExceptionStatus *qcallError)
{
    QCALL_CONTRACT_NO_GC_TRANSITION;
    *qcallError = 0;
    return FALSE;
}

#if defined(TARGET_WASI) && defined(PERFTRACING_DISABLE_THREADS)
extern "C" CLR_BOOL QCALLTYPE EventPipeInternal_WasiRunJobs(QCallExceptionStatus *qcallError)
{
    QCALL_CONTRACT_NO_GC_TRANSITION;
    *qcallError = 0;
    return FALSE;
}
#endif // TARGET_WASI && PERFTRACING_DISABLE_THREADS

#endif // FEATURE_PERFTRACING
