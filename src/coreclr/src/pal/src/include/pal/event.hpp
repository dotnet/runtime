// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    event.hpp

Abstract:

    Event object structure definition.



--*/

#ifndef _PAL_EVENT_H_
#define _PAL_EVENT_H_

#include "corunix.hpp"

namespace CorUnix
{
    extern CObjectType otManualResetEvent;
    extern CObjectType otAutoResetEvent;

    PAL_ERROR
    InternalCreateEvent(
        CPalThread *pThread,
        LPSECURITY_ATTRIBUTES lpEventAttributes,
        BOOL bManualReset,
        BOOL bInitialState,
        LPCWSTR lpName,
        HANDLE *phEvent
        );

    PAL_ERROR
    InternalSetEvent(
        CPalThread *pThread,
        HANDLE hEvent,
        BOOL fSetEvent
        );

}

#endif //PAL_EVENT_H_










