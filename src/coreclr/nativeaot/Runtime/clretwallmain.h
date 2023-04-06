
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Work In Progress to add native events to EventPipe
// shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
// @TODO: Audit native events in NativeAOT Runtime

#include "clreventpipewriteevents.h"
#include "etwevents.h"

inline BOOL EventEnabledDestroyGCHandle(void) {return EventPipeEventEnabledDestroyGCHandle();}

inline ULONG FireEtwDestroyGCHandle(
    void*  HandleID,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventDestroyGCHandle(HandleID,ClrInstanceID,ActivityId,RelatedActivityId);
#ifndef TARGET_UNIX
    status &= FireEtXplatDestroyGCHandle(HandleID,ClrInstanceID);
#endif
    return status;
}

inline BOOL EventEnabledGCStart_V2(void) {return EventPipeEventEnabledGCStart_V2();}

inline ULONG FireEtwGCStart_V2(
    const unsigned int  Count,
    const unsigned int  Depth,
    const unsigned int  Reason,
    const unsigned int  Type,
    const unsigned short  ClrInstanceID,
    const unsigned __int64  ClientSequenceNumber,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventGCStart_V2(Count,Depth,Reason,Type,ClrInstanceID,ClientSequenceNumber,ActivityId,RelatedActivityId);
#ifndef TARGET_UNIX
    status &= FireEtXplatGCStart_V2(Count,Depth,Reason,Type,ClrInstanceID,ClientSequenceNumber);
#endif
    return status;
}


inline BOOL EventEnabledGCRestartEEEnd_V1(void) {return EventPipeEventEnabledGCRestartEEEnd_V1();}

inline ULONG FireEtwGCRestartEEEnd_V1(
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventGCRestartEEEnd_V1(ClrInstanceID,ActivityId,RelatedActivityId);
#ifndef TARGET_UNIX
    status &= FireEtXplatGCRestartEEEnd_V1(ClrInstanceID);
#endif
    return status;
}

inline BOOL EventEnabledGCRestartEEBegin_V1(void) {return EventPipeEventEnabledGCRestartEEBegin_V1();}

inline ULONG FireEtwGCRestartEEBegin_V1(
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventGCRestartEEBegin_V1(ClrInstanceID,ActivityId,RelatedActivityId);
#ifndef TARGET_UNIX
    status &= FireEtXplatGCRestartEEBegin_V1(ClrInstanceID);
#endif
    return status;
}

inline BOOL EventEnabledGCSuspendEEEnd_V1(void) {return EventPipeEventEnabledGCSuspendEEEnd_V1();}

inline ULONG FireEtwGCSuspendEEEnd_V1(
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventGCSuspendEEEnd_V1(ClrInstanceID,ActivityId,RelatedActivityId);
#ifndef TARGET_UNIX
    status &= FireEtXPlatGCSuspendEEEnd_V1(ClrInstanceID);
#endif
    return status;
}

inline BOOL EventEnabledGCSuspendEEBegin_V1(void) {return EventPipeEventEnabledGCSuspendEEBegin_V1();}

inline ULONG FireEtwGCSuspendEEBegin_V1(
    const unsigned int  Reason,
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
)
{
    ULONG status = EventPipeWriteEventGCSuspendEEBegin_V1(Reason,Count,ClrInstanceID,ActivityId,RelatedActivityId);
#ifndef TARGET_UNIX
    status &= FireEtXplatGCSuspendEEBegin_V1(Reason,Count,ClrInstanceID);
#endif
    return status;
}
