
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Work In Progress to add native events to EventPipe
// shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
// @TODO: Audit native events in NativeAOT Runtime

BOOL EventPipeEventEnabledGCStart_V2(void);
ULONG EventPipeWriteEventGCStart_V2(
    const unsigned int  Count,
    const unsigned int  Depth,
    const unsigned int  Reason,
    const unsigned int  Type,
    const unsigned short  ClrInstanceID,
    const unsigned __int64  ClientSequenceNumber,
    const GUID * ActivityId,// = nullptr,
    const GUID * RelatedActivityId// = nullptr
);
BOOL EventPipeEventEnabledGCRestartEEEnd_V1(void);
ULONG EventPipeWriteEventGCRestartEEEnd_V1(
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledGCRestartEEBegin_V1(void);
ULONG EventPipeWriteEventGCRestartEEBegin_V1(
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledGCSuspendEEEnd_V1(void);
ULONG EventPipeWriteEventGCSuspendEEEnd_V1(
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
BOOL EventPipeEventEnabledGCSuspendEEBegin_V1(void);
ULONG EventPipeWriteEventGCSuspendEEBegin_V1(
    const unsigned int  Reason,
    const unsigned int  Count,
    const unsigned short  ClrInstanceID,
    const GUID * ActivityId = nullptr,
    const GUID * RelatedActivityId = nullptr
);
