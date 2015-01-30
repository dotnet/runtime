//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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

    PAL_ERROR
    InternalOpenEvent(
        CPalThread *pThread,
        DWORD dwDesiredAccess,
        BOOL bInheritHandle,
        LPCWSTR lpName,
        HANDLE *phEvent
        );
        
}

#endif //PAL_EVENT_H_










