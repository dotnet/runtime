// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "gctoclreventsink.h"

GCToCLREventSink g_gcToClrEventSink;

void GCToCLREventSink::FireDynamicEvent(const char* eventName, void* payload, uint32_t payloadSize)
{
    LIMITED_METHOD_CONTRACT;

    const size_t EventNameMaxSize = 255;

    WCHAR wideEventName[EventNameMaxSize];
    if (MultiByteToWideChar(CP_ACP, 0, eventName, -1, wideEventName, EventNameMaxSize) == 0)
    {
        return;
    }

    FireEtwGCDynamicEvent(wideEventName, payloadSize, (const BYTE*)payload, GetClrInstanceId());
}
