// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    event.cpp

Abstract:

    Implementation of event synchronization object as described in 
    the WIN32 API

Revision History:



--*/

#include "pal/event.hpp"
#include "pal/thread.hpp"
#include "pal/dbgmsg.h"

using namespace CorUnix;

/* ------------------- Definitions ------------------------------*/
SET_DEFAULT_DEBUG_CHANNEL(SYNC);

CObjectType CorUnix::otManualResetEvent(
                otiManualResetEvent,
                NULL,   // No cleanup routine
                NULL,   // No initialization routine
                0,      // No immutable data
                0,      // No process local data
                0,      // No shared data
                EVENT_ALL_ACCESS, // Currently ignored (no Win32 security)
                CObjectType::SecuritySupported,
                CObjectType::SecurityInfoNotPersisted,
                CObjectType::UnnamedObject,
                CObjectType::LocalDuplicationOnly,
                CObjectType::WaitableObject,
                CObjectType::ObjectCanBeUnsignaled,
                CObjectType::ThreadReleaseHasNoSideEffects,
                CObjectType::NoOwner
                );

CObjectType CorUnix::otAutoResetEvent(
                otiAutoResetEvent,
                NULL,   // No cleanup routine
                NULL,   // No initialization routine
                0,      // No immutable data
                0,      // No process local data
                0,      // No shared data
                EVENT_ALL_ACCESS, // Currently ignored (no Win32 security)
                CObjectType::SecuritySupported,
                CObjectType::SecurityInfoNotPersisted,
                CObjectType::UnnamedObject,
                CObjectType::LocalDuplicationOnly,
                CObjectType::WaitableObject,
                CObjectType::ObjectCanBeUnsignaled,
                CObjectType::ThreadReleaseAltersSignalCount,
                CObjectType::NoOwner
                );

PalObjectTypeId rgEventIds[] = {otiManualResetEvent, otiAutoResetEvent};
CAllowedObjectTypes aotEvent(rgEventIds, sizeof(rgEventIds)/sizeof(rgEventIds[0]));

/*++
Function:
  CreateEventA

Note:
  lpEventAttributes currentely ignored:
  -- Win32 object security not supported
  -- handles to event objects are not inheritable

Parameters:
  See MSDN doc.
--*/

HANDLE
PALAPI
CreateEventA(
         IN LPSECURITY_ATTRIBUTES lpEventAttributes,
         IN BOOL bManualReset,
         IN BOOL bInitialState,
         IN LPCSTR lpName)
{
    HANDLE hEvent = NULL;
    CPalThread *pthr = NULL;
    PAL_ERROR palError;

    PERF_ENTRY(CreateEventA);
    ENTRY("CreateEventA(lpEventAttr=%p, bManualReset=%d, bInitialState=%d, lpName=%p (%s)\n",
          lpEventAttributes, bManualReset, bInitialState, lpName, lpName?lpName:"NULL");

    pthr = InternalGetCurrentThread();
    
    if (lpName != nullptr)
    {
        ASSERT("lpName: Cross-process named objects are not supported in PAL");
        palError = ERROR_NOT_SUPPORTED;
    }
    else
    {
        palError = InternalCreateEvent(
            pthr,
            lpEventAttributes,
            bManualReset,
            bInitialState,
            NULL,
            &hEvent
            );
    }

    //
    // We always need to set last error, even on success:
    // we need to protect ourselves from the situation
    // where last error is set to ERROR_ALREADY_EXISTS on
    // entry to the function
    //

    pthr->SetLastError(palError);
    
    LOGEXIT("CreateEventA returns HANDLE %p\n", hEvent);
    PERF_EXIT(CreateEventA);
    return hEvent;
}


/*++
Function:
  CreateEventW

Note:
  lpEventAttributes currentely ignored:
  -- Win32 object security not supported
  -- handles to event objects are not inheritable

Parameters:  
  See MSDN doc.
--*/

HANDLE
PALAPI
CreateEventW(
         IN LPSECURITY_ATTRIBUTES lpEventAttributes,
         IN BOOL bManualReset,
         IN BOOL bInitialState,
         IN LPCWSTR lpName)
{
    HANDLE hEvent = NULL;
    PAL_ERROR palError;
    CPalThread *pthr = NULL;

    PERF_ENTRY(CreateEventW);
    ENTRY("CreateEventW(lpEventAttr=%p, bManualReset=%d, "
          "bInitialState=%d, lpName=%p (%S)\n", lpEventAttributes, bManualReset, 
           bInitialState, lpName, lpName?lpName:W16_NULLSTRING);

    pthr = InternalGetCurrentThread();

    palError = InternalCreateEvent(
        pthr,
        lpEventAttributes, 
        bManualReset,
        bInitialState,
        lpName,
        &hEvent
        );

    //
    // We always need to set last error, even on success:
    // we need to protect ourselves from the situation
    // where last error is set to ERROR_ALREADY_EXISTS on
    // entry to the function
    //

    pthr->SetLastError(palError);

    LOGEXIT("CreateEventW returns HANDLE %p\n", hEvent);
    PERF_EXIT(CreateEventW);
    return hEvent;
}

/*++
Function:
  CreateEventExW

Note:
  lpEventAttributes and dwDesiredAccess are currently ignored:
  -- Win32 object security not supported
  -- handles to event objects are not inheritable
  -- Access rights are not supported

Parameters:  
  See MSDN doc.
--*/

HANDLE
PALAPI
CreateEventExW(
    IN LPSECURITY_ATTRIBUTES lpEventAttributes,
    IN LPCWSTR lpName,
    IN DWORD dwFlags,
    IN DWORD dwDesiredAccess)
{
    return
        CreateEventW(
            lpEventAttributes,
            (dwFlags & CREATE_EVENT_MANUAL_RESET) != 0,
            (dwFlags & CREATE_EVENT_INITIAL_SET) != 0,
            lpName);
}

/*++
Function:
  InternalCreateEvent

Note:
  lpEventAttributes currentely ignored:
  -- Win32 object security not supported
  -- handles to event objects are not inheritable

Parameters:
  pthr -- thread data for calling thread
  phEvent -- on success, receives the allocated event handle

  See MSDN docs on CreateEvent for all other parameters
--*/

PAL_ERROR
CorUnix::InternalCreateEvent(
    CPalThread *pthr,
    LPSECURITY_ATTRIBUTES lpEventAttributes,
    BOOL bManualReset,
    BOOL bInitialState,
    LPCWSTR lpName,
    HANDLE *phEvent
    )
{
    CObjectAttributes oa(lpName, lpEventAttributes);
    PAL_ERROR palError = NO_ERROR;
    IPalObject *pobjEvent = NULL;
    IPalObject *pobjRegisteredEvent = NULL;

    _ASSERTE(NULL != pthr);
    _ASSERTE(NULL != phEvent);

    ENTRY("InternalCreateEvent(pthr=%p, lpEventAttributes=%p, bManualReset=%i, "
        "bInitialState=%i, lpName=%p, phEvent=%p)\n",
        pthr,
        lpEventAttributes,
        bManualReset,
        bInitialState,
        lpName,
        phEvent
        );

    if (lpName != nullptr)
    {
        ASSERT("lpName: Cross-process named objects are not supported in PAL");
        palError = ERROR_NOT_SUPPORTED;
        goto InternalCreateEventExit;
    }

    palError = g_pObjectManager->AllocateObject(
        pthr,
        bManualReset ? &otManualResetEvent : &otAutoResetEvent,
        &oa,
        &pobjEvent
        );

    if (NO_ERROR != palError)
    {
        goto InternalCreateEventExit;
    }

    if (bInitialState)
    {
        ISynchStateController *pssc;

        palError = pobjEvent->GetSynchStateController(
            pthr,
            &pssc
            );

        if (NO_ERROR == palError)
        {
            palError = pssc->SetSignalCount(1);
            pssc->ReleaseController();
        }

        if (NO_ERROR != palError)
        {
            ASSERT("Unable to set new event state (%d)\n", palError);
            goto InternalCreateEventExit;
        }
    }

    palError = g_pObjectManager->RegisterObject(
        pthr,
        pobjEvent,
        &aotEvent, 
        EVENT_ALL_ACCESS, // Currently ignored (no Win32 security)
        phEvent,
        &pobjRegisteredEvent
        );

    //
    // pobjEvent is invalidated by the call to RegisterObject, so NULL it
    // out here to ensure that we don't try to release a reference on
    // it down the line.
    //
    
    pobjEvent = NULL;

InternalCreateEventExit:

    if (NULL != pobjEvent)
    {
        pobjEvent->ReleaseReference(pthr);
    }

    if (NULL != pobjRegisteredEvent)
    {
        pobjRegisteredEvent->ReleaseReference(pthr);
    }

    LOGEXIT("InternalCreateEvent returns %i\n", palError);

    return palError;
}


/*++
Function:
  SetEvent

See MSDN doc.
--*/

BOOL
PALAPI
SetEvent(
     IN HANDLE hEvent)
{
    PAL_ERROR palError = NO_ERROR;
    CPalThread *pthr = NULL;

    PERF_ENTRY(SetEvent);
    ENTRY("SetEvent(hEvent=%p)\n", hEvent);

    pthr = InternalGetCurrentThread();
    
    palError = InternalSetEvent(pthr, hEvent, TRUE);

    if (NO_ERROR != palError)
    {
        pthr->SetLastError(palError);
    }
    
    LOGEXIT("SetEvent returns BOOL %d\n", (NO_ERROR == palError));
    PERF_EXIT(SetEvent);
    return (NO_ERROR == palError);
}


/*++
Function:
  ResetEvent

See MSDN doc.
--*/

BOOL
PALAPI
ResetEvent(
       IN HANDLE hEvent)
{
    PAL_ERROR palError = NO_ERROR;
    CPalThread *pthr = NULL;

    PERF_ENTRY(ResetEvent);
    ENTRY("ResetEvent(hEvent=%p)\n", hEvent);

    pthr = InternalGetCurrentThread();

    palError = InternalSetEvent(pthr, hEvent, FALSE);

    if (NO_ERROR != palError)
    {
        pthr->SetLastError(palError);
    }
    
    LOGEXIT("ResetEvent returns BOOL %d\n", (NO_ERROR == palError));
    PERF_EXIT(ResetEvent);
    return (NO_ERROR == palError);
}

/*++
Function:
  InternalCreateEvent

Parameters:
  pthr -- thread data for calling thread
  hEvent -- handle to the event to set
  fSetEvent -- if TRUE, set the event; if FALSE, reset it
--*/

PAL_ERROR
CorUnix::InternalSetEvent(
    CPalThread *pthr,
    HANDLE hEvent,
    BOOL fSetEvent
    )
{
    PAL_ERROR palError = NO_ERROR;
    IPalObject *pobjEvent = NULL;
    ISynchStateController *pssc = NULL;

    _ASSERTE(NULL != pthr);

    ENTRY("InternalSetEvent(pthr=%p, hEvent=%p, fSetEvent=%i\n",
        pthr,
        hEvent,
        fSetEvent
        );

    palError = g_pObjectManager->ReferenceObjectByHandle(
        pthr,
        hEvent,
        &aotEvent,
        0, // Should be EVENT_MODIFY_STATE; currently ignored (no Win32 security)
        &pobjEvent
        );

    if (NO_ERROR != palError)
    {
        ERROR("Unable to obtain object for handle %p (error %d)!\n", hEvent, palError);
        goto InternalSetEventExit;
    }

    palError = pobjEvent->GetSynchStateController(
        pthr,
        &pssc
        );

    if (NO_ERROR != palError)
    {
        ASSERT("Error %d obtaining synch state controller\n", palError);
        goto InternalSetEventExit;
    }

    palError = pssc->SetSignalCount(fSetEvent ? 1 : 0);

    if (NO_ERROR != palError)
    {
        ASSERT("Error %d setting event state\n", palError);
        goto InternalSetEventExit;
    }

InternalSetEventExit:

    if (NULL != pssc)
    {
        pssc->ReleaseController();
    }

    if (NULL != pobjEvent)
    {
        pobjEvent->ReleaseReference(pthr);
    }

    LOGEXIT("InternalSetEvent returns %d\n", palError);

    return palError;
}

// TODO: Implementation of OpenEventA() doesn't exist, do we need it? More generally, do we need the A versions at all?

/*++
Function:
  OpenEventW

Note:
  dwDesiredAccess is currently ignored (no Win32 object security support)
  bInheritHandle is currently ignored (handles to events are not inheritable)

Parameters:  
  See MSDN doc.
--*/

HANDLE
PALAPI
OpenEventW(
       IN DWORD dwDesiredAccess,
       IN BOOL bInheritHandle,
       IN LPCWSTR lpName)
{
    HANDLE hEvent = NULL;
    PAL_ERROR palError = NO_ERROR;
    CPalThread *pthr = NULL;

    PERF_ENTRY(OpenEventW);
    ENTRY("OpenEventW(dwDesiredAccess=%#x, bInheritHandle=%d, lpName=%p (%S))\n", 
          dwDesiredAccess, bInheritHandle, lpName, lpName?lpName:W16_NULLSTRING);

    pthr = InternalGetCurrentThread();

    /* validate parameters */
    if (lpName == nullptr)
    {
        ERROR("name is NULL\n");
        palError = ERROR_INVALID_PARAMETER;
        goto OpenEventWExit;            
    }
    else
    {
        ASSERT("lpName: Cross-process named objects are not supported in PAL");
        palError = ERROR_NOT_SUPPORTED;
    }

OpenEventWExit:

    if (NO_ERROR != palError)
    {
        pthr->SetLastError(palError);
    }

    LOGEXIT("OpenEventW returns HANDLE %p\n", hEvent);
    PERF_EXIT(OpenEventW);

    return hEvent;
}

/*++
Function:
  InternalOpenEvent

Note:
  dwDesiredAccess is currently ignored (no Win32 object security support)
  bInheritHandle is currently ignored (handles to events are not inheritable)

Parameters:
  pthr -- thread data for calling thread
  phEvent -- on success, receives the allocated event handle
  
  See MSDN docs on OpenEvent for all other parameters.
--*/

PAL_ERROR
CorUnix::InternalOpenEvent(
    CPalThread *pthr,
    DWORD dwDesiredAccess,
    BOOL bInheritHandle,
    LPCWSTR lpName,
    HANDLE *phEvent
    )
{
    PAL_ERROR palError = NO_ERROR;
    IPalObject *pobjEvent = NULL;
    CPalString sObjectName(lpName);

    _ASSERTE(NULL != pthr);
    _ASSERTE(NULL != lpName);
    _ASSERTE(NULL != phEvent);

    ENTRY("InternalOpenEvent(pthr=%p, dwDesiredAccess=%#x, bInheritHandle=%d, "
        "lpName=%p, phEvent=%p)\n",
        pthr,
        dwDesiredAccess,
        bInheritHandle,
        lpName,
        phEvent
        );

    palError = g_pObjectManager->LocateObject(
        pthr,
        &sObjectName,
        &aotEvent,
        &pobjEvent
        );

    if (NO_ERROR != palError)
    {
        goto InternalOpenEventExit;
    }

    palError = g_pObjectManager->ObtainHandleForObject(
        pthr,
        pobjEvent,
        dwDesiredAccess,
        bInheritHandle,
        NULL,
        phEvent
        );

    if (NO_ERROR != palError)
    {
        goto InternalOpenEventExit;
    }

InternalOpenEventExit:

    if (NULL != pobjEvent)
    {
        pobjEvent->ReleaseReference(pthr);
    }

    LOGEXIT("InternalOpenEvent returns %d\n", palError);
    
    return palError;
}

