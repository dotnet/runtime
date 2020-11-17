// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: nativeeventsource.h
// Abstract: This module implements native part of Event Source support in VM
//

//

//
// ============================================================================
#ifndef _NATIVEEVENTSOURCE_H_
#define _NATIVEEVENTSOURCE_H_

#if defined(FEATURE_EVENTSOURCE_XPLAT)
class XplatEventSourceLogger
{
public:
    static void QCALLTYPE LogEventSource(__in_z int eventID, __in_z LPCWSTR eventName, __in_z LPCWSTR eventSourceName, __in_z LPCWSTR payload);
    static BOOL QCALLTYPE IsEventSourceLoggingEnabled();
};

#endif //defined(FEATURE_EVENTSOURCE_XPLAT)
#endif //_NATIVEEVENTSOURCE_H_
