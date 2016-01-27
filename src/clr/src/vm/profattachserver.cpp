// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 
// ProfAttachServer.cpp
// 

// 
// Implementation of ProfilingAPIAttachServer, which is instantiated on the stack of the
// AttachThread running in the target profilee (server end of the pipe) to receive and
// carry out requests that are sent by the trigger (client end of the pipe).
// 
// Most of the contracts in this file follow the lead of default contracts throughout the
// CLR (triggers, throws, etc.) and many are marked as CAN_TAKE_LOCK, as event logging
// happens all over the place, and that loads resource strings, which takes locks. Some
// notes:
//     * MODE_PREEMPTIVE also allows for GetThread() == NULL, which will be the case for
//         most of these functions most of the time (as most are called on the
//         AttachThread).
//     * NOTHROW is used at the root of the AttachThread (to protect AttachThread from
//         unhandled exceptions which would tear down the entire process), and at the
//         root of the AttachProfiler() API (to protect trigger processes from unhandled
//         exceptions).
//         

// ======================================================================================

#include "common.h"

#ifdef FEATURE_PROFAPI_ATTACH_DETACH 

#include "profilinghelper.h"
#include "profilinghelper.inl"
#include "profattach.h"
#include "profattach.inl"
#include "profattachserver.h"
#include "profattachserver.inl"


// ----------------------------------------------------------------------------
// Implementation of RequestMessageVerifier; a helper to verify incoming messages to the
// target profilee.
// 

// ----------------------------------------------------------------------------
// RequestMessageVerifier::Verify
//
// Description: 
//    Verifies self-consistency of a request message expressed as a byte array from
//    the pipe.  This also calls the appropriate helper to check consistency of the
//    derived request message type, based on the kind of request this is.
//
// Return Value:
//    S_OK or CORPROF_E_UNRECOGNIZED_PIPE_MSG_FORMAT
//

HRESULT RequestMessageVerifier::Verify()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    // In the beginning, the message is not yet verified
    _ASSERTE(!m_fVerified);

    HRESULT hr = CORPROF_E_UNRECOGNIZED_PIPE_MSG_FORMAT;

    // First, do we have something big enough to fit in a BaseRequestMessage?
    if (m_cbRequestMessage < sizeof(BaseRequestMessage))
    {
        return CORPROF_E_UNRECOGNIZED_PIPE_MSG_FORMAT;
    }

    // Yes, but do the fields lie?
    const BaseRequestMessage * pUnverifiedBaseRequestMessage 
        = (const BaseRequestMessage *) m_pbRequestMessage;
    
    // Does the struct claim a size different than the entire message?
    if (pUnverifiedBaseRequestMessage->m_cbMessage != m_cbRequestMessage)
    {
        return CORPROF_E_UNRECOGNIZED_PIPE_MSG_FORMAT;
    }

    // Check for an unknown type, or a known type but with invalid subclass fields
    switch(pUnverifiedBaseRequestMessage->m_requestMessageType)
    {
    default:
        // Unknown message type
        hr = CORPROF_E_UNRECOGNIZED_PIPE_MSG_FORMAT;
        break;

    case kMsgGetVersion:
        hr = VerifyGetVersionRequestMessage();
        break;

    case kMsgAttach:
        hr = VerifyAttachRequestMessage();
        break;
    }

    // For debug builds, remember whether we successfully verified the message
    INDEBUG(m_fVerified = SUCCEEDED(hr));
    return hr;
}

// ----------------------------------------------------------------------------
// RequestMessageVerifier::VerifyGetVersionRequestMessage
// 
// Description:
//    Once a BaseRequestMessage has been verified as self-consistent, and is of type
//    kMsgGetVersion, this helper is called to verify consistency as a Get Version
//    message
//    
// Return Value:
//    S_OK or CORPROF_E_UNRECOGNIZED_PIPE_MSG_FORMAT
//    
// Assumptions:
//    * Verify() calls this, but only after it has verified base type
//        

HRESULT RequestMessageVerifier::VerifyGetVersionRequestMessage()
{
    LIMITED_METHOD_CONTRACT;

    const BaseRequestMessage * pBaseRequestMessage = 
        (const BaseRequestMessage *) m_pbRequestMessage;

    // Not much to verify here, since the get version request message is simply a
    // BaseRequestMessage (no subtype)

    // Not allowed to call this unless you checked the m_requestMessageType first!
    _ASSERTE(pBaseRequestMessage->m_requestMessageType == kMsgGetVersion);

    if (pBaseRequestMessage->m_cbMessage != sizeof(BaseRequestMessage))
    {
        return CORPROF_E_UNRECOGNIZED_PIPE_MSG_FORMAT;
    }

    return S_OK;
}

// ----------------------------------------------------------------------------
// RequestMessageVerifier::VerifyAttachRequestMessage
//
// Description: 
//    Once a BaseRequestMessage has been verified as self-consistent, and is of type
//    kMsgAttach, this helper is called to verify consistency of derived type
//    AttachRequestMessage
//
// Return Value:
//    S_OK or CORPROF_E_UNRECOGNIZED_PIPE_MSG_FORMAT
//
// Assumptions:
//    * Verify() calls this, but only after it has verified base type
//

HRESULT RequestMessageVerifier::VerifyAttachRequestMessage()
{
    LIMITED_METHOD_CONTRACT;

    const BaseRequestMessage * pBaseRequestMessage = 
        (const BaseRequestMessage *) m_pbRequestMessage;

    // Not allowed to call this unless you checked the m_requestMessageType first!
    _ASSERTE(pBaseRequestMessage->m_requestMessageType == kMsgAttach);

    // Enough memory to cast to AttachRequestMessage?
    if (pBaseRequestMessage->m_cbMessage < sizeof(AttachRequestMessage))
    {
        return CORPROF_E_UNRECOGNIZED_PIPE_MSG_FORMAT;
    }

    AttachRequestMessage * pUnverifiedAttachRequestMessage = 
        (AttachRequestMessage *) pBaseRequestMessage;

    // Is client data properly contained inside message?  Use 64-bit arithmetic to
    // detect overflow
    UINT64 ui64TotalMsgLength = (UINT64) pUnverifiedAttachRequestMessage->m_cbMessage;
    UINT64 ui64ClientDataStartOffset = (UINT64) pUnverifiedAttachRequestMessage->m_dwClientDataStartOffset;
    UINT64 ui64ClientDataLength = (UINT64) pUnverifiedAttachRequestMessage->m_cbClientDataLength;

    // Client data must occur AFTER struct
    if (ui64ClientDataStartOffset < sizeof(AttachRequestMessage))
    {
        return CORPROF_E_UNRECOGNIZED_PIPE_MSG_FORMAT;
    }

    // Client data should be wholly contained inside the message
    if (ui64ClientDataStartOffset + ui64ClientDataLength > ui64TotalMsgLength)
    {
        return CORPROF_E_UNRECOGNIZED_PIPE_MSG_FORMAT;
    }

    // m_wszProfilerPath must be a NULL-terminated string.
    if (wmemchr(pUnverifiedAttachRequestMessage->m_wszProfilerPath, 
                W('\0'), 
                _countof(pUnverifiedAttachRequestMessage->m_wszProfilerPath)) == NULL)
    {
        return CORPROF_E_UNRECOGNIZED_PIPE_MSG_FORMAT;
    }

    return S_OK;
}


// ----------------------------------------------------------------------------
// RequestMessageVerifier::GetBaseRequestMessage
//
// Description: 
//    After you've called code:RequestMessageVerifier::Verify, this function will hand
//    you a pointer to the verified request message.  (If you call this before verifying
//    the message, it'll assert.)
//
// Return Value:
//    Pointer to the verified message
//
// Assumptions:
//    * Call code:RequestMessageVerifier::Verify first!
//

const BaseRequestMessage * RequestMessageVerifier::GetBaseRequestMessage()
{
    LIMITED_METHOD_CONTRACT;

    // Not allowed to ask for the message unless it's been successfully verified!
    _ASSERTE(m_fVerified);
    
    return (const BaseRequestMessage *) m_pbRequestMessage;
}


//---------------------------------------------------------------------------------------
// #ConnectedPipeHolder
//
// Simple holder that ensures a connected pipe disconnects its client when the scope is
// over. User of the class is responsible for creating the pipe and connecting the pipe,
// before using this holder. The user of the class is responsible for closing the pipe
// after this holder goes away.
// 

// ----------------------------------------------------------------------------
// AcquireConnectedPipe
//
// Description: 
//    Used for ConnectedPipeHolder when acquiring a pipe HANDLE.  Does nothing but
//    assert that the handle is valid.
//
// Arguments:
//    * hConnectedPipe - HANDLE being acquired
//

void AcquireConnectedPipe(HANDLE hConnectedPipe)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(IsValidHandle(hConnectedPipe));
}

// ----------------------------------------------------------------------------
// ReleaseConnectedPipe
// 
// Description:
//    Used for ConnectedPipeHolder when releasing a pipe HANDLE. Disconnects the pipe
//    from its client, but leaves the pipe open and ready for the next client connection.
//    
// Arguments:
//    * hConnectedPipe - HANDLE to pipe being disconnected
//        

void ReleaseConnectedPipe(HANDLE hConnectedPipe)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(IsValidHandle(hConnectedPipe));

    LOG((
        LF_CORPROF, 
        LL_ERROR, 
        "**PROF: Disconnecting pipe from current client.\n"));
    
    if (!DisconnectNamedPipe(hConnectedPipe)) 
    {
        LOG((
            LF_CORPROF, 
            LL_ERROR, 
            "**PROF: DisconnectNamedPipe failed with %d.\n", 
            GetLastError()));
    }
}

// See code:#ConnectedPipeHolder
typedef Wrapper<HANDLE, AcquireConnectedPipe, ReleaseConnectedPipe, 
                (UINT_PTR) INVALID_HANDLE_VALUE> ConnectedPipeHolder;


// ----------------------------------------------------------------------------
// Implementation of ProfilingAPIAttachServer: the primary class that handles the server
// end of the pipe by receiving trigger requests, carrying them out, and then sending
// responses back to the trigger (client end of pipe).
// 
// This is the meat. Savor its juices.


// ----------------------------------------------------------------------------
// ProfilingAPIAttachServer::ExecutePipeRequests
// 
// Description:
//    The AttachThread is responsible for performing attach and detach operations. This
//    function comprises the main loop for the "attach" operations. Creates the pipe
//    server, and repeatedly connects to clients (i.e., trigger processes calling
//    AttachProfiler() API), services them, and disconnects them. Once client connections
//    stop arriving for a while (default is 5 minutes), the loop ends, the pipe server is
//    destroyed, and this function returns. (Note: the exception is when running in
//    code:ProfilingAPIAttachDetach::kAlwaysOn mode. In that case, this function loops
//    forever over all clients, without timing out and returning if it takes a long time
//    for the next connection request to come in.)
//    
// Return Value:
//    Any success code implies one client successfully attached a profiler, else, error
//    HRESULT indicating the last error encountered with a client.
//    

HRESULT ProfilingAPIAttachServer::ExecutePipeRequests()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    HRESULT hr;
    AttachStatus attachStatusOverall = kNoAttachRequested;

    // First create the pipe server.  If this fails, all is lost
    hr = CreateAttachPipe();
    if (FAILED(hr))
    {
        LOG((
            LF_CORPROF, 
            LL_ERROR, 
            "**PROF: Failed trying to create attach pipe server. hr=0x%x.\n",
            hr));
        ProfilingAPIUtility::LogProfError(IDS_E_PROF_ATTACHTHREAD_INIT, hr);
        return hr;
    }

    // Thank you, CreateAttachPipe()!
    _ASSERTE(IsValidHandle(m_hPipeServer));

    // Now loop until there are no more clients to service. Remember if any of the
    // clients got a profiler to attach, so we can return the appropriate HRESULT.
    // 
    // Note that we intentionally keep on looping even after a profiler is attached, just
    // in case there are any extra client requests coming in (e.g., user launched a
    // couple triggers simultanously, or a single trigger retried AttachProfiler API a
    // couple times). Once client connections stop coming in for a while,
    // ServiceOneClient will fail with a timeout, and we'll break out of the loop.
    // 
    // Also note that, in kAlwaysOn mode, we loop forever until the thread naturally dies
    // during app shutdown

    while (SUCCEEDED(hr) || 
            (ProfilingAPIAttachDetach::GetAttachThreadingMode() == 
                ProfilingAPIAttachDetach::kAlwaysOn))
    {
        AttachStatus attachStatusForThisClient = kNoAttachRequested;

        hr = ServiceOneClient(&attachStatusForThisClient);
        
        // #AttachStatusOrder
        // Here's where the order of the AttachStatus enum is important. Any given client
        // must have an attach status "better" than the current overall attach status,
        // for us to want the overall attach status to change (to match the client's
        // status). See code:ProfilingAPIAttachServer::AttachStatus
        if ((int) attachStatusForThisClient > (int) attachStatusOverall)
        {
            attachStatusOverall = attachStatusForThisClient;
        }
    }

    // We reach this point only when we're in kOnDemand mode, and a failure is causing
    // us to destroy the pipe (usually the failure is simply a timeout waiting for the
    // next client to come along).
    _ASSERTE(FAILED(hr) &&
            (ProfilingAPIAttachDetach::GetAttachThreadingMode() == 
                ProfilingAPIAttachDetach::kOnDemand));

    // This switch statement can forgive, but it will never forget. We went through all
    // the trouble of making an AttachThread and a pipe, and now we're destroying them.
    // If no one even asked to attach a profiler in the meantime, this switch notes that
    // in the event log. Conversely, if at least some client successfully attached a
    // profiler, return S_OK.
    
    switch(attachStatusOverall)
    {
    default:
        _ASSERTE(!"Unknown AttachStatus value!");
        return E_UNEXPECTED;

    case kNoAttachRequested:
        // All this time, and no one even asked for an attach? Wack. Log and return the
        // last error we got
        _ASSERTE(FAILED(hr));
        ProfilingAPIUtility::LogProfError(IDS_E_PROF_NO_ATTACH_REQ, hr);
        return hr;

    case kAttachFailed:
        // Someone tried to attach and failed.  Event was already logged at that time
        _ASSERTE(FAILED(hr));
        return hr;

    case kAttachSucceeded:
        // At least one of the clients managed to get a profiler successfully attached
        // (info event was logged at that time), so all is well
        return S_OK;
    }
}


// ----------------------------------------------------------------------------
// ProfilingAPIAttachServer::CreateAttachPipe
//
// Description: 
//    Creates a new pipe server, that is not yet connected to a client
//
// Return Value:
//    HRESULT indicating success or failure
//

HRESULT ProfilingAPIAttachServer::CreateAttachPipe()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    HRESULT hr;
    SECURITY_ATTRIBUTES *psa = NULL;
    SECURITY_ATTRIBUTES sa;

    // Only assign security attributes for non-app container scenario
    // We are assuming the default for app container scenario is good enough
    if (!ProfilingAPIAttachDetach::IsAppContainerProcess(GetCurrentProcess()))
    {
        hr = ProfilingAPIAttachDetach::InitSecurityAttributes(&sa, sizeof(sa));
        if (FAILED(hr))
        {
            return hr;
        }

        psa = &sa;
    }

    StackSString attachPipeName;
    hr = ProfilingAPIAttachDetach::GetAttachPipeName(::GetCurrentProcess(), &attachPipeName);
    if (FAILED(hr))
    {
        return hr;
    }

    m_hPipeServer = CreateNamedPipeW(
        attachPipeName.GetUnicode(),
        PIPE_ACCESS_DUPLEX |                // server and client read/write to pipe
            FILE_FLAG_OVERLAPPED,           // server may read asynchronously & use a timeout
        PIPE_TYPE_MESSAGE |                 // pipe data written as stream of messages
            PIPE_READMODE_MESSAGE,          // pipe data read as stream of messages
        1,                                  // Only one instance of the pipe is allowed
        sizeof(GetVersionResponseMessage),  // Hint of typical response size (GetVersion is the biggest)
        sizeof(AttachRequestMessage) +
            0x100,                          // Hint of typical request size (attach requests are the
                                            // biggest, plus figure 0x100 for client data)
        1000,                               // nDefaultTimeOut: unused.  Clients will always
                                            //     specify their own timeout when waiting
                                            //     for the pipe to appear
        psa                                 // lpSecurityAttributes
        );
    if (m_hPipeServer == INVALID_HANDLE_VALUE)
    {
        return HRESULT_FROM_GetLastError();
    }

    _ASSERTE(IsValidHandle(m_hPipeServer));

    LOG((
        LF_CORPROF, 
        LL_INFO10, 
        "**PROF: Successfully created attach pipe server.  Name: '%S'.\n", 
        attachPipeName.GetUnicode()));

    return S_OK;
}

// ----------------------------------------------------------------------------
// ProfilingAPIAttachServer::ServiceOneClient
// 
// Description:
//    Awaits a connection from a client, receives the client's requests, executes and
//    responds to those requests, and then disconnects the client on error or once a
//    profiler has been attached as a result. If any blocking operation takes too long,
//    this will disconnect the client as well.
//    
// Arguments:
//    * pAttachStatusForClient - [out] enum indicating whether an attach request was
//        received and processed successfully. NOTE: This out param is always set
//        properly, even if this function returns an error.
//        
// Return Value:
//    * error HRESULT: something bad happened with the pipe itself (e.g., couldn't
//        connect to a new client due to timeout or something worse). When in kOnDemand
//        mode, an error return from this function indicates the entire AttachThread
//        should go away.
//    * S_OK: Pipe is fine and connected to at least one client. That connection may or
//        may not have resulted in successful communication or a profiler attach. But in
//        any case, the pipe is still intact, and the caller should connect with the next
//        client.
//        
// Notes:
//    * A failure event will be logged for any kind of user-actionable failure that
//        occurs in this function or callees.
//    * A failure event is NOT logged for a NON-actionable failure such as failure in
//        communicating a response message back to the trigger (client). See comment at
//        top of code:ProfilingAPIAttachServer::WriteResponseToPipe

HRESULT ProfilingAPIAttachServer::ServiceOneClient(
    AttachStatus * pAttachStatusForClient)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    _ASSERTE(IsValidHandle(m_hPipeServer));
    _ASSERTE(pAttachStatusForClient != NULL);

    HRESULT hr = E_UNEXPECTED;
    *pAttachStatusForClient = kNoAttachRequested;

    // What is the max timeout for each blocking wait for the trigger? Examples of
    // blocking waits: wait for a pipe client to show up, or for the client to send a
    // request, or for the pipe to transfer our response to the client.
    // 
    // If any blocking operation takes longer than this, the current function will
    // timeout.
    //     * While in kOnDemand mode, a timeout waiting for a client to connect will
    //         cause the AttachThread to give up, go away, and the app reverts to
    //         non-attach performance characteristics. The Global Attach Event will need
    //         to be signaled again by a trigger process (via AttachProfiler API) before
    //         a new AttachThread gets created and tries again.
    //     * Once a client is connected, timeouts from this function simply cause that
    //         client to be disconnected, and this function will be called again to wait
    //         (with timeout!) for the next client to connect.
    m_dwMillisecondsMaxPerWait = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_ProfAPIMaxWaitForTriggerMs);

    hr = ConnectToClient();
    if (FAILED(hr))
    {
        if (hr != HRESULT_FROM_WIN32(ERROR_TIMEOUT))
        {
            // Any error other than timeout is unexpected and should be logged. Timeouts,
            // however, are expected, as eventually clients will stop trying to connect
            // to the pipe, so no need to log that.
            ProfilingAPIUtility::LogProfError(IDS_E_PROF_CONNECT_TO_TRIGGER, hr);
        }
        return hr;
    }

    LOG((
        LF_CORPROF, 
        LL_INFO10, 
        "**PROF: Pipe server is now connected to a new client.\n"));

    // This forces a call to DisconnectNamedPipe before we return. That kicks the current
    // client off of the pipe, and leaves the pipe available for the next client
    // connection.
    ConnectedPipeHolder connectedPipeHolder(m_hPipeServer);

    // Keep executing requests from this client until it asks for (and we attempt) an
    // attach.  Whether the attach succeeds or fails, that's the end of this client, and
    // we'll fall out of the loop and return.
    while (*pAttachStatusForClient == kNoAttachRequested)
    {
        hr = ServiceOneRequest(pAttachStatusForClient);
        if (FAILED(hr))
        {
            // Low-level error on the pipe itself indicating that we should disconnect
            // from this client, and try connecting to a new one. Typical errors you
            // might see here:
            // * HRESULT_FROM_WIN32(ERROR_BROKEN_PIPE)
            //     * Someone killed the trigger process (or it timed out) before an
            //         attach could be requested.
            // * HRESULT_FROM_WIN32(ERROR_TIMEOUT)
            // * HRESULT_FROM_WIN32(ERROR_SEM_TIMEOUT)
            //     * Client's taking too long to send a request
            //         
            // Since a failure here indicates a problem with this particular client, and
            // not a global problem with the pipe, just convert to S_OK and return so we
            // disconnect this client, and the caller knows to try connecting to another
            // client. Note that ServiceOneRequest() has already reported any actionable
            // problem into the event log.
            return S_OK;
        }
    }

    // A trigger finally managed to request an attach (success of the attach may be
    // found in pAttachStatusForClient).  So we can return to disconnect this client and
    // poll for the next client.
    return S_OK;
}


// ----------------------------------------------------------------------------
// ProfilingAPIAttachServer::ConnectToClient
//
// Description: 
//    Waits until a client connects to the pipe server, or until timeout.
//
// Return Value:
//    HRESULT indicating success or failure.
//

HRESULT ProfilingAPIAttachServer::ConnectToClient()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    _ASSERTE(IsValidHandle(m_hPipeServer));

    HRESULT hr;
    BOOL fRet;
    DWORD dwErr;
    DWORD cbReceived;
    ProfilingAPIAttachDetach::OverlappedResultHolder overlapped;
    hr = overlapped.Initialize();
    if (FAILED(hr))
    {
        return hr;
    }

    // Start an overlapped connection for this pipe instance. 
    fRet = ConnectNamedPipe(m_hPipeServer, overlapped);
    if (fRet)
    {
        // No need to wait, pipe connected already
        return S_OK;
    }

    dwErr = GetLastError();
    if (dwErr == ERROR_PIPE_CONNECTED)
    {
        // In true Windows style, a "failure" with ERROR_PIPE_CONNECTED is
        // actually a success case: a client tried to connect before we (the
        // server) called ConnectNamedPipe, so that we're now connected
        // just fine
        return S_OK;
    }

    if (dwErr != ERROR_IO_PENDING)
    {
        // An error we cannot recover from
        LOG((
            LF_CORPROF, 
            LL_ERROR, 
            "**PROF: ConnectNamedPipe failed.  hr=0x%x.\n",
            HRESULT_FROM_WIN32(dwErr)));
        return HRESULT_FROM_WIN32(dwErr);
    }

    // Typical case: ERROR_IO_PENDING. ConnectNamedPipe is waiting (in overlapped mode)
    // for a client to connect. Block until this happens (or we timeout)

    hr = overlapped.Wait(

        // How long we wait for the next client to show up depends on our threading mode
        (ProfilingAPIAttachDetach::GetAttachThreadingMode() == 
            ProfilingAPIAttachDetach::kAlwaysOn) ?  

            // In always-on mode, we're willing to wait forever until the next client
            // shows up.
            INFINITE :

            // In on-demand mode, we want the AttachThread to exit if there aren't
            // any new clients in a reasonable amount of time.
            m_dwMillisecondsMaxPerWait,
                    
        m_hPipeServer,
        &cbReceived);
    if (FAILED(hr))
    {
        LOG((
            LF_CORPROF, 
            LL_ERROR, 
            "**PROF: Waiting for overlapped result for ConnectNamedPipe failed.  hr=0x%x.\n",
            hr));
        return hr;
    }

    return S_OK;
}


// ----------------------------------------------------------------------------
// ProfilingAPIAttachServer::ServiceOneRequest
// 
// Description:
//    Receives, executes, and responds to a single request from a single client.
//    
// Arguments:
//    * pAttachStatus - [out] enum indicating whether an attach request was received and
//        processed successfully. NOTE: This out param is always set properly, even if
//        this function returns an error.
//                 
// Return Value:
//    * S_OK: Request was received. It may or may not have been processed successfully.
//        Any processing failure would be due to a high level problem, like an unknown
//        request format, or a CLR problem in handling the request ("can't attach
//        profiler cuz profiler already loaded").  In any case, the caller may leave the
//        pipe connection to this client open, as the connection is valid.
//    * error: Low-level error (e.g., OS pipe failure or timeout) trying to receive the
//        request or send a response. Such an error is generally unexpected and will
//        cause the caller to close the connection to the client (though the pipe will
//        remain up for the next client to try connecting).
//        
// Notes:
//    * A failure event will be logged for any kind of user-actionable failure that
//        occurs in this function or callees.
//    * A failure event is NOT logged for a NON-actionable failure such as failure in
//        communicating a response message back to the trigger (client). See comment at
//        top of code:ProfilingAPIAttachServer::WriteResponseToPipe
//        

HRESULT ProfilingAPIAttachServer::ServiceOneRequest(AttachStatus * pAttachStatus)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    _ASSERTE(IsValidHandle(m_hPipeServer));
    _ASSERTE(pAttachStatus != NULL);

    HRESULT hr;
    DWORD cbRequestMessageRead;
    *pAttachStatus = kNoAttachRequested;

    // Reading from the pipe is a 3-step process.
    // 
    // * 1. Read into a 0-sized buffer. This causes us to block (with timeout) until the
    //     message is in the pipe and ready to be analyzed. Since the buffer is 0-sized,
    //     the message is not actually read out of the pipe yet.
    // * 2. Now that we know the message is available, peek into the pipe to extract the
    //     size of the message
    // * 3. Now that we know the size, allocate a sufficient buffer, and repeat step 1,
    //     but with the appropriately sized buffer. This time the data is emptied out of
    //     the pipe.

    // Step 1: Read request once w/ 0-sized buffer so we know when the message is ready;
    // at that point we can ask how long the message is
    hr = ReadRequestFromPipe(
        NULL,                   // Request buffer
        0,                      // Size of request buffer
        &cbRequestMessageRead);
    if (FAILED(hr) && (hr != HRESULT_FROM_WIN32(ERROR_MORE_DATA)))
    {
        ProfilingAPIUtility::LogProfError(IDS_E_PROF_PIPE_RCV, hr);
        return hr;
    }

    // Step 2: Message is ready.  How big is it?
    DWORD cbRequestMessage;
    if (!PeekNamedPipe(
        m_hPipeServer,
        NULL,                   // Request buffer (0-size for now)
        0,                      // Size of request buffer
        NULL,                   // lpBytesRead (NULL cuz message shan't be read)
        NULL,                   // lpTotalBytesAvail (NULL cuz don't care)
        &cbRequestMessage))
    {
        ProfilingAPIUtility::LogProfError(IDS_E_PROF_PIPE_RCV, hr);
        return hr;
    }

    // 0-sized requests are invalid.  Something wrong with the pipe?
    if (cbRequestMessage == 0)
    {
        hr = E_UNEXPECTED;
        ProfilingAPIUtility::LogProfError(IDS_E_PROF_PIPE_RCV, hr);
        return hr;
    }

    // Step 3: message is ready and we know the size.  Make the buffer, and read it in.
    
    NewHolder<BYTE> pbRequestMessage(new (nothrow) BYTE[cbRequestMessage]);
    if (pbRequestMessage == NULL)
    {
        hr = E_OUTOFMEMORY;
        ProfilingAPIUtility::LogProfError(IDS_E_PROF_PIPE_RCV, hr);
        return hr;
    }

    hr = ReadRequestFromPipe(
        pbRequestMessage, 
        cbRequestMessage,
        &cbRequestMessageRead);
    if (FAILED(hr))
    {
        ProfilingAPIUtility::LogProfError(IDS_E_PROF_PIPE_RCV, hr);
        return hr;
    }

    if (cbRequestMessage != cbRequestMessageRead)
    {
        // Somehow we read a different number of bytes than we were told was in the pipe
        // buffer.  Pipe having problems?
        hr = E_UNEXPECTED;
        ProfilingAPIUtility::LogProfError(IDS_E_PROF_PIPE_RCV, hr);
        return hr;
    }

    // Request successfully read! Now figure out what the request is, carry it out, and
    // send a response. This function will report to the event log any user-actionable
    // error.
    return InterpretAndExecuteRequestMessage(pbRequestMessage, cbRequestMessage, pAttachStatus);
}

// ----------------------------------------------------------------------------
// ProfilingAPIAttachServer::ReadRequestFromPipe
//
// Description: 
//    Performs a ReadFile with timeout on the pipe server to read the client's request
//    message.
//
// Arguments:
//    * pvRequestBuffer - [out] Buffer into which the request will be placed
//    * cbRequestBuffer - [in] Size, in bytes, of the request buffer
//    * pcbActualRequest - [out] Actual number of bytes placed into the request buffer.
//
// Return Value:
//    HRESULT indicating success or failure
//
// Assumptions:
//    * m_hPipeServer must be connected to a client.
//
// Notes:
//    * The [out] parameters may be written to even if this function fails.  But their
//        contents should be ignored by the caller in this case.
//

HRESULT ProfilingAPIAttachServer::ReadRequestFromPipe(
    LPVOID pvRequestBuffer,
    DWORD cbRequestBuffer,
    DWORD * pcbActualRequest)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    _ASSERTE(IsValidHandle(m_hPipeServer));

    // NULL buffer implies zero size!
    _ASSERTE((pvRequestBuffer != NULL) || (cbRequestBuffer == 0));

    _ASSERTE(pcbActualRequest != NULL);

    HRESULT hr;
    DWORD dwErr;
    ProfilingAPIAttachDetach::OverlappedResultHolder overlapped;
    hr = overlapped.Initialize();
    if (FAILED(hr))
    {
        return hr;
    }
    
    if (ReadFile(
        m_hPipeServer,
        pvRequestBuffer,
        cbRequestBuffer,
        pcbActualRequest,
        overlapped))
    {
        // Quick read, no waiting
        return S_OK;
    }

    dwErr = GetLastError();
    if (dwErr != ERROR_IO_PENDING)
    {
        LOG((
            LF_CORPROF, 
            LL_ERROR, 
            "**PROF: ReadFile on the pipe failed.  hr=0x%x.\n",
            HRESULT_FROM_WIN32(dwErr)));
        return HRESULT_FROM_WIN32(dwErr);
    }

    // Typical case=ERROR_IO_PENDING: gotta wait until request comes in (or we timeout)

    hr = overlapped.Wait(
        m_dwMillisecondsMaxPerWait,
        m_hPipeServer,
        pcbActualRequest);
    if (FAILED(hr))
    {
        LOG((
            LF_CORPROF, 
            LL_ERROR, 
            "**PROF: Waiting for overlapped result for ReadFile on the pipe failed.  hr=0x%x.\n",
            hr));
        return hr;
    }

    return S_OK;
}


// ----------------------------------------------------------------------------
// ProfilingAPIAttachServer::InterpretAndExecuteRequestMessage
// 
// Description:
//    Takes an unverified stream of bytes read from the pipe, and then verifies the bytes
//    as a self-consistent message and executes the request (either get version or
//    attach). Once the request has been executed, a response is sent back across the
//    pipe.
//    
// Arguments:
//    * pbRequestMessage - [in] Bytes read from pipe
//    * cbRequestMessage - [in] Count of bytes read from pipe
//    * pAttachStatus - [out] (see comment header for
//        code:ProfilingAPIAttachServer::ServiceOneRequest)
//        
// Return Value:
//    HRESULT indicating success or failure with low-level reading / writing operations
//    on the pipe that indicate whether the caller should abandon this client connection.
//    Higher-level failures (e.g., bogus request messages, or failure performing the
//    actual attach) do not cause an error to be returned from this function. Caller may
//    use pAttachStatus to determine whether this request resulted in a successful
//    profiler attach.
//    
// Notes:
//    * This (or callee) will log an event on actionable failures. (Failure to send a
//        response back to the trigger is not considered actionable. See comment at top
//        of code:ProfilingAPIAttachServer::WriteResponseToPipe.)
//        

HRESULT ProfilingAPIAttachServer::InterpretAndExecuteRequestMessage(
    LPCBYTE pbRequestMessage, 
    DWORD cbRequestMessage,
    AttachStatus * pAttachStatus)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;

        // This causes events to be logged, which loads resource strings,
        // which takes locks.
        CAN_TAKE_LOCK;

        MODE_PREEMPTIVE;
    } 
    CONTRACTL_END;

    _ASSERTE(pbRequestMessage != NULL);
    _ASSERTE(pAttachStatus != NULL);

    HRESULT hr;

    *pAttachStatus = kNoAttachRequested;
    
    // Message bytes have not been verified, so none of the contents (such as sizes or
    // offsets) may be trusted until they're all verified.
    RequestMessageVerifier messageVerifier(pbRequestMessage, cbRequestMessage);
    hr = messageVerifier.Verify();
    if (FAILED(hr))
    {
        // Bogus request message.  Log to event log
        ProfilingAPIUtility::LogProfError(IDS_E_PROF_INVALID_MSG);
        
        // And send complaint back to trigger
        BaseResponseMessage responseMsg(hr);
        return WriteResponseToPipe(&responseMsg, sizeof(responseMsg));
    }

    // Yay! Message is valid
    const BaseRequestMessage * pBaseRequestMessage = messageVerifier.GetBaseRequestMessage();

    // Execute request based on its type
    switch(pBaseRequestMessage->m_requestMessageType)
    {
    default:
        // RequestMessageVerifier should have verified no unexpected request message
        // types slipped through.
        _ASSERTE(!"Unexpected m_requestMessageType");
        return E_UNEXPECTED;

    case kMsgGetVersion:
        return ExecuteGetVersionRequestMessage();

    case kMsgAttach:
        return ExecuteAttachRequestMessage(
            (const AttachRequestMessage *) pBaseRequestMessage,
            pAttachStatus);
    }
}


// ----------------------------------------------------------------------------
// ProfilingAPIAttachServer::ExecuteAttachRequestMessage
// 
// Description:
//    Once an attach request message has been verified as self-consistent (see
//    code:RequestMessageVerifier), call this function to actually attach the profiler
//    using data from the message
//    
// Arguments:
//    * pAttachRequestMessage - [in] An already-verified attach request message that was
//        received from trigger.
//    * pAttachStatus - [out] (see comment header for
//        code:ProfilingAPIAttachServer::ServiceOneRequest)
//        
// Return Value:
//    HRESULT indicating success or failure in sending response over the pipe back to the
//    trigger. Note that a failure to perform the attach does not necessarily cause a
//    failure HRESULT to be returned by this function (only low-level pipe problems will
//    cause this function to fail). A failure performing the attach is noted in
//    pAttachStatus.
//    
// Notes:
//    * This (or a callee) will log an event on failure or success of performing the
//        attach. However, once the attach is complete (failed or succeeded), no event
//        will be logged if there is a communication error sending the response back to
//        the trigger. (See comment at top of
//        code:ProfilingAPIAttachServer::WriteResponseToPipe)
//        

HRESULT ProfilingAPIAttachServer::ExecuteAttachRequestMessage(
    const AttachRequestMessage * pAttachRequestMessage,
    AttachStatus * pAttachStatus)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;

        // This causes events to be logged, which loads resource strings,
        // which takes locks.
        CAN_TAKE_LOCK;

        MODE_PREEMPTIVE;
    } 
    CONTRACTL_END;

    _ASSERTE(pAttachRequestMessage != NULL);
    _ASSERTE(pAttachStatus != NULL);

    // Start off pessimistic
    *pAttachStatus = kAttachFailed;

    if (g_profControlBlock.curProfStatus.Get() != kProfStatusNone)
    {
        // Sorry, profiler's already here.
        // 
        // Note: It might appear that there's a race here (i.e.,
        // g_profControlBlock.curProfStatus.Get() == kProfStatusNone so we try to load the
        // profiler, but another profiler is already getting loaded somehow, and
        // g_profControlBlock.curProfStatus.Get() just hasn't been updated yet. So we end
        // up loading two profilers at once.) But there is actually no race here for a
        // couple reasons:
        // * 1. Startup load of profiler occurs before the pipe is even constructed. So
        //     we won't get an attach request while a startup load is in progress
        // * 2. Pipe requests are serialized. OS handles this for us because:
        //     * a. Only one instance of the attach pipe is allowed at a time, because
        //         our call to CreateNamedPipeW specifies only 1 instance is allowed, and
        //     * b. Within that single pipe instance, messages are processed serially,
        //         from the single AttachThread that successfully created the pipe in the
        //         first place.
        ProfilingAPIUtility::LogProfError(IDS_E_PROF_PROFILER_ALREADY_ACTIVE);
        
        _ASSERTE(*pAttachStatus == kAttachFailed);

        // Inform trigger that attach cannot happen now
        AttachResponseMessage responseMsg(CORPROF_E_PROFILER_ALREADY_ACTIVE);
        return WriteResponseToPipe(&responseMsg, sizeof(responseMsg));
    }

    // If the client sends us a V2 message, retrieve the time out value
    // In theory both client and server should be both on v4.5+ but I'm assigning a default value 
    // just in case
    DWORD dwConcurrentGCWaitTimeoutInMs = INFINITE;
    if (AttachRequestMessageV2::CanCastTo(pAttachRequestMessage))
        dwConcurrentGCWaitTimeoutInMs = 
            static_cast<const AttachRequestMessageV2 *>(pAttachRequestMessage)->m_dwConcurrentGCWaitTimeoutInMs;    
    
    // LoadProfilerForAttach & callees ensure an event is logged on error.
    HRESULT hrAttach = ProfilingAPIUtility::LoadProfilerForAttach(
    
        // Profiler's CLSID
        &(pAttachRequestMessage->m_clsidProfiler),

        // wszProfilerDLL
        pAttachRequestMessage->m_wszProfilerPath,

        // Client data ptr
        (pAttachRequestMessage->m_cbClientDataLength == 0) ?
            // No client data: use NULL
            NULL :
            // Else, follow offset to find client data
            (LPVOID) (((LPBYTE) pAttachRequestMessage) +
                pAttachRequestMessage->m_dwClientDataStartOffset),

        // Client data size
        pAttachRequestMessage->m_cbClientDataLength,

        // Time out for wait operation on current gc that is in progress
        dwConcurrentGCWaitTimeoutInMs);

    // Inform caller if attach succeeded
    if (SUCCEEDED(hrAttach))
    {
        *pAttachStatus = kAttachSucceeded;
    }
    else
    {
        _ASSERTE(*pAttachStatus == kAttachFailed);
    }

    // Inform trigger about how the attach went
    AttachResponseMessage responseMsg(hrAttach);
    return WriteResponseToPipe(&responseMsg, sizeof(responseMsg));
}


// ----------------------------------------------------------------------------
// ProfilingAPIAttachServer::ExecuteGetVersionRequestMessage
// 
// Description:
//    Composes a response message to the "GetVersion" request message. Response contains
//    the version of the profilee (server), and the minimum allowable version of a
//    trigger (client) we're willing to talk to.
//    
// Return Value:
//    HRESULT Indicating success or failure.
//    
// Notes:
//    * Composing the response cannot fail, and we are not logging communcation failures
//        in sending response messages (see comment at top of
//        code:ProfilingAPIAttachServer::WriteResponseToPipe), so no event will be logged
//        by this function or callees.
//        

HRESULT ProfilingAPIAttachServer::ExecuteGetVersionRequestMessage()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    GetVersionResponseMessage responseMsg(
        // S_OK means we successfully carried out the "GetVersion" request
        S_OK,
        
        // This is the version of the target profilee app
        ProfilingAPIAttachDetach::kCurrentProcessVersion,
        
        // This is the oldest trigger that we allow communicating with
        ProfilingAPIAttachDetach::kMinimumAllowableTriggerVersion);
    
    return WriteResponseToPipe(&responseMsg, sizeof(responseMsg));
}

// ----------------------------------------------------------------------------
// ProfilingAPIAttachServer::WriteResponseToPipeNoBufferSizeCheck
//
// Description: 
//    Performs a WriteFile with timeout on the pipe server to write the specified
//    response back to the client.  This is an internal helper used by
//    code:ProfilingAPIAttachServer::WriteResponseToPipe
//
// Arguments:
//    * pvResponse - [in] Buffer containing the response to be sent to the client
//    * cbResponse - [in] Size, in bytes, of the response to send.
//    * pcbWritten - [out] Actual number of bytes sent to client
//
// Return Value:
//    HRESULT indicating success or failure
//
// Assumptions:
//    * m_hPipeServer must be connected to a client.
//
// Notes:
//    * The [out] parameter may be written to even if this function fails.  But its
//        contents should be ignored by the caller in this case.
//

HRESULT ProfilingAPIAttachServer::WriteResponseToPipeNoBufferSizeCheck(
    LPVOID pvResponse,
    DWORD cbResponse,
    DWORD * pcbWritten)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    _ASSERTE(IsValidHandle(m_hPipeServer));
    _ASSERTE(pvResponse != NULL);
    _ASSERTE(pcbWritten != NULL);

    HRESULT hr;
    DWORD dwErr;
    ProfilingAPIAttachDetach::OverlappedResultHolder overlapped;
    hr = overlapped.Initialize();
    if (FAILED(hr))
    {
        return hr;
    }
    
    if (WriteFile( 
        m_hPipeServer,
        pvResponse,
        cbResponse,
        pcbWritten,
        overlapped))
    {
        // Quick write, no waiting
        return S_OK;
    }

    dwErr = GetLastError();
    if (dwErr != ERROR_IO_PENDING)
    {
        LOG((
            LF_CORPROF, 
            LL_ERROR, 
            "**PROF: WriteFile on the pipe failed.  hr=0x%x.\n",
            HRESULT_FROM_WIN32(dwErr)));
        return HRESULT_FROM_WIN32(dwErr);
    }

    // Typical case=ERROR_IO_PENDING: gotta wait until response is sent (or we timeout)

    hr = overlapped.Wait(
        m_dwMillisecondsMaxPerWait,
        m_hPipeServer,
        pcbWritten);
    if (FAILED(hr))
    {
        LOG((
            LF_CORPROF, 
            LL_ERROR, 
            "**PROF: Waiting for overlapped result for WriteFile on the pipe failed.  hr=0x%x.\n",
            hr));
        return hr;
    }

    return S_OK;
}

#endif //FEATURE_PROFAPI_ATTACH_DETACH 
