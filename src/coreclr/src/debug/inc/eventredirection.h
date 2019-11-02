// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************

//
// NativePipeline.h
//
// define control block for redirecting events.
//*****************************************************************************

#ifndef _EVENTREDIRECTION_H
#define _EVENTREDIRECTION_H

//---------------------------------------------------------------------------------------
// Control block for redirecting events.
// Motivation here is that only 1 process can be the real OS debugger. So if we want a windbg
// attached to an ICorDebug debuggee, then that windbg is the real debugger and it forwards events
// to the mdbg process.
//
// Terminology:
//   Server: a windbg extension (StrikeRS) that is the real OS debugger, and it forwards native debug
//           events (just exceptions currently) to the client
//   Client: ICorDebug, which gets events via shimmed call to WaitForDebugEvent, etc.
//
// Control block lives in Client's process space. All handles are valid in client.
// Sever does Read/WriteProcessMemory
struct RedirectionBlock
{
    // Version of the control block. Initialized by client, verified by server.
    // Latest value is EVENT_REDIRECTION_CURRENT_VERSION
    DWORD m_versionCookie;

    //
    // Counters. After each WFDE/CDE pair, these counters should be in sync.
    //

    // increment after WFDE
    DWORD m_counterAvailable;
    DWORD m_counterConsumed;

    //
    // Data for WaitForDebugEvent. (Server writes; Client reads)
    //
    DWORD m_dwProcessId;
    DWORD m_dwThreadId;

    // Different sizes on different platforms
    EXCEPTION_RECORD m_record;
    BOOL m_dwFirstChance;

    //
    // Data for ContinueDebugEvent. (Client writes, server reads)
    //

    // Continuation status argument to ContinueDebugEvent
    DWORD m_ContinuationStatus;


    //
    // Coordination events. These are handles in client space; server duplicates out.
    //

    // Server signals when WFDE Data is ready.
    HANDLE m_hEventAvailable;

    // Server signals when CDE data is ready.
    HANDLE m_hEventConsumed;

    // Client signals before it deletes this block. This corresponds to client calling DebugActiveProcessStop.
    // Thus server can check if signalled to know if accessing this block (which lives in client space) is safe.
    // This is Synchronized because client only detaches if the debuggee is stopped, in which case the server
    // isn't in the middle of sending an event.
    HANDLE m_hDetachEvent;
};


// Current version.
#define EVENT_REDIRECTION_CURRENT_VERSION ((DWORD) 4)



#endif // _EVENTREDIRECTION_H

