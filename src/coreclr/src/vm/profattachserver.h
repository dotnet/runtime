// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 
// ProfAttachServer.h
// 

// 
// Definitions of ProfilingAPIAttachServer and helpers, which are used by the
// AttachThread running in the target profilee (server end of the pipe) to receive and
// carry out requests that are sent by the trigger (client end of the pipe).
// 

// ======================================================================================

#ifndef __PROF_ATTACH_SERVER_H__
#define __PROF_ATTACH_SERVER_H__


//---------------------------------------------------------------------------------------
// Helper to verify any messages received by the target profilee, before the target
// profilee is allowed to trust any of the message contents.

class RequestMessageVerifier
{
public:
    RequestMessageVerifier(LPCBYTE pbRequestMessage, DWORD cbRequestMessage);
    HRESULT Verify();
    const BaseRequestMessage * GetBaseRequestMessage();

protected:
    LPCBYTE m_pbRequestMessage;
    DWORD m_cbRequestMessage;
    INDEBUG(BOOL m_fVerified);

    HRESULT VerifyGetVersionRequestMessage();
    HRESULT VerifyAttachRequestMessage();
};

//---------------------------------------------------------------------------------------
// Here's the beef.  All the pipe server stuff running on the AttachThread is housed in
// this class.

class ProfilingAPIAttachServer
{
public:
    ProfilingAPIAttachServer();
    ~ProfilingAPIAttachServer();
    
    HRESULT ExecutePipeRequests();

protected:
    //---------------------------------------------------------------------------------------
    // Notes whether an attach was requested, and whether the request was serviced
    // successfully. Primarily used to aggregate status across multiple trigger processes
    // that connect over the pipe, so we know what we've logged to the event log.
    //
    // Notes:
    //     * The order is important! Overall attach status may change only in ascending
    //         order of the values of this enum. See
    //         code:ProfilingAPIAttachDetach::ExecutePipeRequests#AttachStatusOrder
    enum AttachStatus
    {
        // Default, and worst of all: No one requested a profiler attach
        kNoAttachRequested  = 0,
        
        // Slightly better: someone figured out how to ask for an attach, but it failed.
        kAttachFailed       = 1,
        
        // Bestest of all: someone requested an attach, and it worked
        kAttachSucceeded    = 2,
    };

    // Server end of the pipe created by the current process (which is the target
    // profilee).
    HandleHolder m_hPipeServer;

    // Most blocking operations on the server end of the pipe (i.e., this process), use
    // this as the timeout.  The exception is waiting for new connections when in
    // code:ProfilingAPIAttachDetach::kAlwaysOn mode (which waits with INFINITE timeout).
    DWORD m_dwMillisecondsMaxPerWait;

    HRESULT CreateAttachPipe();
    HRESULT ServiceOneClient(AttachStatus * pAttachStatusForClient);
    HRESULT ConnectToClient();
    HRESULT ServiceOneRequest(
        AttachStatus * pAttachStatus);
    HRESULT ReadRequestFromPipe(
        LPVOID pvRequestBuffer,
        DWORD cbRequestBuffer,
        DWORD * pcbActualRequest);
    HRESULT InterpretAndExecuteRequestMessage(
        LPCBYTE pbRequestMessage, 
        DWORD cbRequestMessage,
        AttachStatus * pAttachStatus);
    HRESULT WriteResponseToPipeNoBufferSizeCheck(
        LPVOID pvResponse,
        DWORD cbResponse,
        DWORD * pcbWritten);
    HRESULT WriteResponseToPipe(
        LPVOID pvResponse,
        DWORD cbResponse);
    HRESULT ExecuteGetVersionRequestMessage();
    HRESULT ExecuteAttachRequestMessage(
        const AttachRequestMessage * pAttachRequestMessage,
        AttachStatus * pAttachStatus);
};

#endif // __PROF_ATTACH_SERVER_H__
