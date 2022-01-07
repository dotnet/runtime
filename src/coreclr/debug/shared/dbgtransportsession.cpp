// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#include "dbgtransportsession.h"

#if (!defined(RIGHT_SIDE_COMPILE) && defined(FEATURE_DBGIPC_TRANSPORT_VM)) || (defined(RIGHT_SIDE_COMPILE) && defined(FEATURE_DBGIPC_TRANSPORT_DI))

// This is the entry type for the IPC event queue owned by the transport.
// Each entry contains the multiplexing type of the IPC event plus the
// IPC event itself.
struct DbgEventBufferEntry
{
public:
    IPCEventType     m_type;
    BYTE             m_event[CorDBIPC_BUFFER_SIZE];     // buffer for the IPC event
};

//
// Provides a robust and secure transport session between a debugger and a debuggee that are potentially on
// different machines.
//
// See DbgTransportSession.h for further detailed comments.
//

#ifndef RIGHT_SIDE_COMPILE
// The one and only transport instance for the left side. Allocated and initialized during EE startup (from
// Debugger::Startup() in debugger.cpp).
DbgTransportSession *g_pDbgTransport = NULL;

#include "ddmarshalutil.h"
#endif // !RIGHT_SIDE_COMPILE

// No real work done in the constructor. Use Init() instead.
DbgTransportSession::DbgTransportSession()
{
    m_ref = 1;
    m_eState = SS_Closed;
}

DbgTransportSession::~DbgTransportSession()
{
    DbgTransportLog(LC_Proxy, "DbgTransportSession::~DbgTransportSession() called");

    // No other threads are now using session resources. We're free to deallocate them as we wish (if they
    // were allocated in the first place).
    if (m_hTransportThread)
        CloseHandle(m_hTransportThread);
    if (m_rghEventReadyEvent[IPCET_OldStyle])
        CloseHandle(m_rghEventReadyEvent[IPCET_OldStyle]);
    if (m_rghEventReadyEvent[IPCET_DebugEvent])
        CloseHandle(m_rghEventReadyEvent[IPCET_DebugEvent]);
    if (m_pEventBuffers)
        delete [] m_pEventBuffers;

#ifdef RIGHT_SIDE_COMPILE
    if (m_hSessionOpenEvent)
        CloseHandle(m_hSessionOpenEvent);

    if (m_hProcessExited)
        CloseHandle(m_hProcessExited);
#endif // RIGHT_SIDE_COMPILE

    if (m_fInitStateLock)
        m_sStateLock.Destroy();
}

// Allocates initial resources (including starting the transport thread). The session will start in the
// SS_Opening state. That is, the RS will immediately start trying to Connect() a connection while the LS will
// perform an accept()/Accept() to wait for a connection request. The RS needs an IP address and port number
// to initiate connections. These should be given in host byte order. The LS, on the other hand, requires the
// addresses of a couple of runtime data structures to service certain debugger requests that may be delivered
// once the session is established.
#ifdef RIGHT_SIDE_COMPILE
HRESULT DbgTransportSession::Init(const ProcessDescriptor& pd, HANDLE hProcessExited)
#else // RIGHT_SIDE_COMPILE
HRESULT DbgTransportSession::Init(DebuggerIPCControlBlock *pDCB, AppDomainEnumerationIPCBlock *pADB)
#endif // RIGHT_SIDE_COMPILE
{
    _ASSERTE(m_eState == SS_Closed);

    // Start with a blank slate so that Shutdown() on a partially initialized instance will only do the
    // cleanup necessary.
    memset(this, 0, sizeof(*this));

    // Because of the above memset the embeded classes/structs need to be reinitialized especially
    // the two way pipe; it expects the in/out handles to be -1 instead of 0.
    m_ref = 1;
    m_pipe = TwoWayPipe();
    m_sStateLock = DbgTransportLock();

    // Initialize all per-session state variables.
    InitSessionState();

#ifdef RIGHT_SIDE_COMPILE
    // The RS randomly allocates a session ID which is sent to the LS in the SessionRequest message. In the
    // case of network errors during session formation this allows the LS to tell SessionRequest re-sends from
    // a new request from a different RS.
    HRESULT hr = CoCreateGuid(&m_sSessionID);
    if (FAILED(hr))
        return hr;
#endif // RIGHT_SIDE_COMPILE


#ifdef RIGHT_SIDE_COMPILE
    m_pd = pd;

    if (!DuplicateHandle(GetCurrentProcess(),
                         hProcessExited,
                         GetCurrentProcess(),
                         &m_hProcessExited,
                         0,      // ignored since we are going to pass DUPLICATE_SAME_ACCESS
                         FALSE,
                         DUPLICATE_SAME_ACCESS))
    {
        return HRESULT_FROM_GetLastError();
    }

    m_fDebuggerAttached = false;
#else // RIGHT_SIDE_COMPILE
    m_pDCB = pDCB;
    m_pADB = pADB;
#endif // RIGHT_SIDE_COMPILE

    m_sStateLock.Init();
    m_fInitStateLock = true;

#ifdef RIGHT_SIDE_COMPILE
    m_hSessionOpenEvent = WszCreateEvent(NULL, TRUE, FALSE, NULL); // Manual reset, not signalled
    if (m_hSessionOpenEvent == NULL)
        return E_OUTOFMEMORY;
#else // RIGHT_SIDE_COMPILE
    ProcessDescriptor pd = ProcessDescriptor::FromCurrentProcess();
    if (!m_pipe.CreateServer(pd)) {
        return E_OUTOFMEMORY;
    }
#endif // RIGHT_SIDE_COMPILE

    // Allocate some buffers to receive incoming events. The initial number is chosen arbitrarily, tune as
    // necessary. This array will need to grow if it fills with unread events (it takes our client a little
    // time to process each incoming receive). In general, however, one side will not send an unbounded stream
    // of events to the other without waiting for some kind of response. More usual are small bursts of events
    // to represent variable sized data (such as a stack trace).
    m_cEventBuffers = 10;
    m_pEventBuffers = (DbgEventBufferEntry *)new (nothrow) BYTE[m_cEventBuffers * sizeof(DbgEventBufferEntry)];
    if (m_pEventBuffers == NULL)
        return E_OUTOFMEMORY;

    m_rghEventReadyEvent[IPCET_OldStyle] = WszCreateEvent(NULL, FALSE, FALSE, NULL); // Auto reset, not signalled
    if (m_rghEventReadyEvent[IPCET_OldStyle] == NULL)
        return E_OUTOFMEMORY;

    m_rghEventReadyEvent[IPCET_DebugEvent] = WszCreateEvent(NULL, FALSE, FALSE, NULL); // Auto reset, not signalled
    if (m_rghEventReadyEvent[IPCET_DebugEvent] == NULL)
        return E_OUTOFMEMORY;

    // Start the transport thread which handles forming and re-forming connections, driving the session
    // state to SS_Open and receiving and initially processing all incoming traffic.
    AddRef();
    m_hTransportThread = CreateThread(NULL, 0, TransportWorkerStatic, this, 0, NULL);
    if (m_hTransportThread == NULL)
    {
        Release();
        return E_OUTOFMEMORY;
    }

    return S_OK;
}

// Drive the session to the SS_Closed state, which will deallocate all remaining transport resources
// (including terminating the transport thread). If this is the RS and the session state is SS_Open at the
// time of this call a graceful disconnect will be attempted (which tells the LS to go back to SS_Opening to
// look for a new RS rather than interpreting the disconnection as a temporary error and going into
// SS_Resync). On either side the session will no longer be functional after this call returns (though Init()
// may be called again to start over from the beginning).
void DbgTransportSession::Shutdown()
{
    DbgTransportLog(LC_Proxy, "DbgTransportSession::Shutdown() called");

    // The transport thread is allocated last in Init() (since it uses all the other resources that Init()
    // prepares). Don't do any transport related stuff unless this was allocated (which can happen if
    // Shutdown() is called after an Init() failure).

    if (m_hTransportThread)
    {
        // From SS_Open state try a graceful disconnect.
        if (m_eState == SS_Open)
        {
            DbgTransportLog(LC_Session, "Sending 'SessionClose'");
            DBG_TRANSPORT_INC_STAT(SentSessionClose);
            Message sMessage;
            sMessage.Init(MT_SessionClose);
            SendMessage(&sMessage, false);
        }

        // Must take the state lock to make a state transition.
        {
            TransportLockHolder sLockHolder(&m_sStateLock);

            // Remember previous state and transition to SS_Closed.
            SessionState ePreviousState = m_eState;
            m_eState = SS_Closed;

            if (ePreviousState != SS_Closed)
            {
                m_pipe.Disconnect();
            }

        } // Leave m_sStateLock

#ifdef RIGHT_SIDE_COMPILE
        // Signal the m_hSessionOpenEvent now to quickly error out any callers of WaitForSessionToOpen().
        SetEvent(m_hSessionOpenEvent);
#endif // RIGHT_SIDE_COMPILE
    }

    // The transport instance is no longer valid
    Release();
}

#ifndef RIGHT_SIDE_COMPILE

// Cleans up the named pipe connection so no tmp files are left behind. Does only
// the minimum and must be safe to call at any time. Called during PAL ExitProcess,
// TerminateProcess and for unhandled native exceptions and asserts.
void DbgTransportSession::AbortConnection()
{
    m_pipe.Disconnect();
}

// API used only by the LS to drive the transport into a state where it won't accept connections. This is used
// when no proxy is detected at startup but it's too late to shutdown all of the debugging system easily. It's
// mainly paranoia to increase the protection of your system when the proxy isn't started.
void DbgTransportSession::Neuter()
{
    // Simply set the session state to SS_Closed. The transport thread will switch itself off if it ever gets
    // a connection but the rest of the transport resources remain valid (so the debugger helper thread won't
    // AV on a deallocated handle, which might happen if we simply called Shutdown()).
    m_eState = SS_Closed;
}

#else // RIGHT_SIDE_COMPILE

// Used by debugger side (RS) to cleanup the target (LS) named pipes
// and semaphores when the debugger detects the debuggee process  exited.
void DbgTransportSession::CleanupTargetProcess()
{
    m_pipe.CleanupTargetProcess();
}

// On the RS it may be useful to wait and see if the session can reach the SS_Open state. If the target
// runtime has terminated for some reason then we'll never reach the open state. So the method below gives the
// RS a way to try and establish a connection for a reasonable amount of time and to time out otherwise. They
// could then call Shutdown on the session and report an error back to the rest of the debugger. The method
// returns true if the session opened within the time given (in milliseconds) and false otherwise.
bool DbgTransportSession::WaitForSessionToOpen(DWORD dwTimeout)
{
    DWORD dwRet = WaitForSingleObject(m_hSessionOpenEvent, dwTimeout);
    if (m_eState == SS_Closed)
        return false;

    if (dwRet == WAIT_TIMEOUT)
        DbgTransportLog(LC_Proxy, "DbgTransportSession::WaitForSessionToOpen(%u) timed out", dwTimeout);

    return dwRet == WAIT_OBJECT_0;
}

//---------------------------------------------------------------------------------------
//
// A valid ticket is returned if no other client is currently acting as the debugger.
// If the caller passes in a valid ticket, this function will return true without invalidating the ticket.
//
// Arguments:
//    pTicket - out parameter; set to a valid ticket if the client has successfully registered as the debugger
//
// Return Value:
//    Return true if the client has successfully registered as the debugger.
//

bool DbgTransportSession::UseAsDebugger(DebugTicket * pTicket)
{
    TransportLockHolder sLockHolder(&m_sStateLock);
    if (m_fDebuggerAttached)
    {
        if (pTicket->IsValid())
        {
            // The client already holds a valid ticket.
            return true;
        }
        else
        {
            // Another client of this session has already indicated that it's using this session to debug.
            _ASSERTE(!pTicket->IsValid());
            return false;
        }
    }
    else
    {
        m_fDebuggerAttached = true;
        pTicket->SetValid();
        return true;
    }
}

//---------------------------------------------------------------------------------------
//
// A valid ticket is required in order for this function to succeed.  After this function succeeds,
// another client can request to be the debugger.
//
// Arguments:
//    pTicket - the client's ticket; must be valid for this function to succeed
//
// Return Value:
//    Return true if the client has successfully unregistered as the debugger.
//    Return false if no client is currently acting as the debugger or if the client's ticket is invalid.
//

bool DbgTransportSession::StopUsingAsDebugger(DebugTicket * pTicket)
{
    TransportLockHolder sLockHolder(&m_sStateLock);
    if (m_fDebuggerAttached && pTicket->IsValid())
    {
        // The caller is indeed the owner of the debug ticket.
        m_fDebuggerAttached = false;
        pTicket->SetInvalid();
        return true;
    }
    else
    {
        return false;
    }
}
#endif // RIGHT_SIDE_COMPILE

// Sends a pre-initialized event to the other side.
HRESULT DbgTransportSession::SendEvent(DebuggerIPCEvent *pEvent)
{
    DbgTransportLog(LC_Events, "Sending '%s'", IPCENames::GetName(pEvent->type));
    DBG_TRANSPORT_INC_STAT(SentEvent);

    return SendEventWorker(pEvent, IPCET_OldStyle);
}

// Sends a pre-initialized event to the other side, but pretend that this is coming from the native pipeline.
// See code:IPCEventType for more information.
HRESULT DbgTransportSession::SendDebugEvent(DebuggerIPCEvent * pEvent)
{
    DbgTransportLog(LC_Events, "Sending '%s' as DEBUG_EVENT", IPCENames::GetName(pEvent->type));
    DBG_TRANSPORT_INC_STAT(SentEvent);

    return SendEventWorker(pEvent, IPCET_DebugEvent);
}

// Retrieves the auto-reset handle which is signalled by the session each time a new event is received from
// the other side.
HANDLE DbgTransportSession::GetIPCEventReadyEvent()
{
    return m_rghEventReadyEvent[IPCET_OldStyle];
}

// Retrieves the auto-reset handle which is signalled by the session each time a new event (disguised as a
// debug event) is received from the other side.
HANDLE DbgTransportSession::GetDebugEventReadyEvent()
{
    return m_rghEventReadyEvent[IPCET_DebugEvent];
}

// Copies the last event received from the other side into the provided buffer. This should only be called
// (once) after the event returned from GetIPCEEventReadyEvent()/GetDebugEventReadyEvent() has been signalled.
void DbgTransportSession::GetNextEvent(DebuggerIPCEvent *pEvent, DWORD cbEvent)
{
    _ASSERTE(cbEvent <= CorDBIPC_BUFFER_SIZE);

    // Must acquire the state lock to synchronize us wrt to the transport thread (clients already guarantee
    // they serialize calls to this and waiting on m_rghEventReadyEvent).
    TransportLockHolder sLockHolder(&m_sStateLock);

    // There must be at least one valid event waiting (this call does not block).
    _ASSERTE(m_cValidEventBuffers);

    // Copy the first valid event into the client's buffer.
    memcpy(pEvent, &m_pEventBuffers[m_idxEventBufferHead].m_event, cbEvent);

    // Move the index of the head of the valid list forward (which may in fact move it back to the start of
    // the array since the list is circular). This reduces the number of valid entries by one. Note that these
    // two adjustments do not affect the tail of the list in any way. In the limit case the head will end up
    // pointing to the same event as the tail (and m_cValidEventBuffers will be zero).
    m_idxEventBufferHead = (m_idxEventBufferHead + 1) % m_cEventBuffers;
    m_cValidEventBuffers--;
    _ASSERTE(((m_idxEventBufferHead + m_cValidEventBuffers) % m_cEventBuffers) == m_idxEventBufferTail);

    // If there's at least one more valid event we can signal event ready now.
    if (m_cValidEventBuffers)
    {
        SetEvent(m_rghEventReadyEvent[m_pEventBuffers[m_idxEventBufferHead].m_type]);
    }
}



void MarshalDCBTransportToDCB(DebuggerIPCControlBlockTransport* pIn, DebuggerIPCControlBlock* pOut)
{
    pOut->m_DCBSize =                         pIn->m_DCBSize;
    pOut->m_verMajor =                        pIn->m_verMajor;
    pOut->m_verMinor =                        pIn->m_verMinor;
    pOut->m_checkedBuild =                    pIn->m_checkedBuild;
    pOut->m_bHostingInFiber =                 pIn->m_bHostingInFiber;
    pOut->padding2 =                          pIn->padding2;
    pOut->padding3 =                          pIn->padding3;

    pOut->m_leftSideProtocolCurrent =         pIn->m_leftSideProtocolCurrent;
    pOut->m_leftSideProtocolMinSupported =    pIn->m_leftSideProtocolMinSupported;

    pOut->m_rightSideProtocolCurrent =        pIn->m_rightSideProtocolCurrent;
    pOut->m_rightSideProtocolMinSupported =   pIn->m_rightSideProtocolMinSupported;

    pOut->m_errorHR =                         pIn->m_errorHR;
    pOut->m_errorCode =                       pIn->m_errorCode;

#if defined(TARGET_64BIT)
    pOut->padding4 =                          pIn->padding4;
#endif // TARGET_64BIT


    //
    //pOut->m_rightSideEventAvailable
    //pOut->m_rightSideEventRead
    //pOut->m_paddingObsoleteLSEA
    //pOut->m_paddingObsoleteLSER
    //pOut->m_rightSideProcessHandle
    //pOut->m_leftSideUnmanagedWaitEvent

    pOut->m_realHelperThreadId =             pIn->m_realHelperThreadId;
    pOut->m_helperThreadId =                 pIn->m_helperThreadId;
    pOut->m_temporaryHelperThreadId =        pIn->m_temporaryHelperThreadId;
    pOut->m_CanaryThreadId =                 pIn->m_CanaryThreadId;
    pOut->m_pRuntimeOffsets =                pIn->m_pRuntimeOffsets;
    pOut->m_helperThreadStartAddr =          pIn->m_helperThreadStartAddr;
    pOut->m_helperRemoteStartAddr =          pIn->m_helperRemoteStartAddr;
    pOut->m_specialThreadList =              pIn->m_specialThreadList;

    //
    //pOut->m_receiveBuffer
    //pOut->m_sendBuffer

    pOut->m_specialThreadListLength =        pIn->m_specialThreadListLength;
    pOut->m_shutdownBegun =                  pIn->m_shutdownBegun;
    pOut->m_rightSideIsWin32Debugger =       pIn->m_rightSideIsWin32Debugger;
    pOut->m_specialThreadListDirty =         pIn->m_specialThreadListDirty;

    pOut->m_rightSideShouldCreateHelperThread = pIn->m_rightSideShouldCreateHelperThread;

}

void MarshalDCBToDCBTransport(DebuggerIPCControlBlock* pIn, DebuggerIPCControlBlockTransport* pOut)
{
    pOut->m_DCBSize =                         pIn->m_DCBSize;
    pOut->m_verMajor =                        pIn->m_verMajor;
    pOut->m_verMinor =                        pIn->m_verMinor;
    pOut->m_checkedBuild =                    pIn->m_checkedBuild;
    pOut->m_bHostingInFiber =                 pIn->m_bHostingInFiber;
    pOut->padding2 =                          pIn->padding2;
    pOut->padding3 =                          pIn->padding3;

    pOut->m_leftSideProtocolCurrent =         pIn->m_leftSideProtocolCurrent;
    pOut->m_leftSideProtocolMinSupported =    pIn->m_leftSideProtocolMinSupported;

    pOut->m_rightSideProtocolCurrent =        pIn->m_rightSideProtocolCurrent;
    pOut->m_rightSideProtocolMinSupported =   pIn->m_rightSideProtocolMinSupported;

    pOut->m_errorHR =                         pIn->m_errorHR;
    pOut->m_errorCode =                       pIn->m_errorCode;

#if defined(TARGET_64BIT)
    pOut->padding4 =                          pIn->padding4;
#endif // TARGET_64BIT

    pOut->m_realHelperThreadId =             pIn->m_realHelperThreadId;
    pOut->m_helperThreadId =                 pIn->m_helperThreadId;
    pOut->m_temporaryHelperThreadId =        pIn->m_temporaryHelperThreadId;
    pOut->m_CanaryThreadId =                 pIn->m_CanaryThreadId;
    pOut->m_pRuntimeOffsets =                pIn->m_pRuntimeOffsets;
    pOut->m_helperThreadStartAddr =          pIn->m_helperThreadStartAddr;
    pOut->m_helperRemoteStartAddr =          pIn->m_helperRemoteStartAddr;
    pOut->m_specialThreadList =              pIn->m_specialThreadList;

    pOut->m_specialThreadListLength =        pIn->m_specialThreadListLength;
    pOut->m_shutdownBegun =                  pIn->m_shutdownBegun;
    pOut->m_rightSideIsWin32Debugger =       pIn->m_rightSideIsWin32Debugger;
    pOut->m_specialThreadListDirty =         pIn->m_specialThreadListDirty;

    pOut->m_rightSideShouldCreateHelperThread = pIn->m_rightSideShouldCreateHelperThread;
}



#ifdef RIGHT_SIDE_COMPILE
// Read and write memory on the LS from the RS.
HRESULT DbgTransportSession::ReadMemory(PBYTE pbRemoteAddress, PBYTE pbBuffer, SIZE_T cbBuffer)
{
    DbgTransportLog(LC_Requests, "Sending 'ReadMemory(0x%08X, %u)'", pbRemoteAddress, cbBuffer);
    DBG_TRANSPORT_INC_STAT(SentReadMemory);

    Message sMessage;
    sMessage.Init(MT_ReadMemory, NULL, 0, pbBuffer, (DWORD)cbBuffer);
    sMessage.m_sHeader.TypeSpecificData.MemoryAccess.m_pbLeftSideBuffer = pbRemoteAddress;
    sMessage.m_sHeader.TypeSpecificData.MemoryAccess.m_cbLeftSideBuffer = (DWORD)cbBuffer;

    HRESULT hr = SendRequestMessageAndWait(&sMessage);
    if (FAILED(hr))
        return hr;

    // If we reached here the send was successful but the actual memory operation may not have been (due to
    // unmapped memory or page protections etc.). So the final result comes back to us in the reply.
    return sMessage.m_sHeader.TypeSpecificData.MemoryAccess.m_hrResult;
}

HRESULT DbgTransportSession::WriteMemory(PBYTE pbRemoteAddress, PBYTE pbBuffer, SIZE_T cbBuffer)
{
    DbgTransportLog(LC_Requests, "Sending 'WriteMemory(0x%08X, %u)'", pbRemoteAddress, cbBuffer);
    DBG_TRANSPORT_INC_STAT(SentWriteMemory);

    Message sMessage;
    sMessage.Init(MT_WriteMemory, pbBuffer, (DWORD)cbBuffer);
    sMessage.m_sHeader.TypeSpecificData.MemoryAccess.m_pbLeftSideBuffer = pbRemoteAddress;
    sMessage.m_sHeader.TypeSpecificData.MemoryAccess.m_cbLeftSideBuffer = (DWORD)cbBuffer;

    HRESULT hr = SendRequestMessageAndWait(&sMessage);
    if (FAILED(hr))
        return hr;

    // If we reached here the send was successful but the actual memory operation may not have been (due to
    // unmapped memory or page protections etc.). So the final result comes back to us in the reply.
    return sMessage.m_sHeader.TypeSpecificData.MemoryAccess.m_hrResult;
}

HRESULT DbgTransportSession::VirtualUnwind(DWORD threadId, ULONG32 contextSize, PBYTE context)
{
    DbgTransportLog(LC_Requests, "Sending 'VirtualUnwind'");
    DBG_TRANSPORT_INC_STAT(SentVirtualUnwind);

    Message sMessage;
    sMessage.Init(MT_VirtualUnwind, context, contextSize, context, contextSize);
    return SendRequestMessageAndWait(&sMessage);
}

// Read and write the debugger control block on the LS from the RS.
HRESULT DbgTransportSession::GetDCB(DebuggerIPCControlBlock *pDCB)
{
    DbgTransportLog(LC_Requests, "Sending 'GetDCB'");
    DBG_TRANSPORT_INC_STAT(SentGetDCB);

    Message sMessage;
    DebuggerIPCControlBlockTransport dcbt;
    sMessage.Init(MT_GetDCB, NULL, 0, (PBYTE)&dcbt, sizeof(DebuggerIPCControlBlockTransport));
    HRESULT ret = SendRequestMessageAndWait(&sMessage);

    MarshalDCBTransportToDCB(&dcbt, pDCB);
    return ret;
}

HRESULT DbgTransportSession::SetDCB(DebuggerIPCControlBlock *pDCB)
{
    DbgTransportLog(LC_Requests, "Sending 'SetDCB'");
    DBG_TRANSPORT_INC_STAT(SentSetDCB);

    DebuggerIPCControlBlockTransport dcbt;
    MarshalDCBToDCBTransport(pDCB, &dcbt);

    Message sMessage;
    sMessage.Init(MT_SetDCB, (PBYTE)&dcbt, sizeof(DebuggerIPCControlBlockTransport));
    return SendRequestMessageAndWait(&sMessage);

}

// Read the AppDomain control block on the LS from the RS.
HRESULT DbgTransportSession::GetAppDomainCB(AppDomainEnumerationIPCBlock *pADB)
{
    DbgTransportLog(LC_Requests, "Sending 'GetAppDomainCB'");
    DBG_TRANSPORT_INC_STAT(SentGetAppDomainCB);

    Message sMessage;
    sMessage.Init(MT_GetAppDomainCB, NULL, 0, (PBYTE)pADB, sizeof(AppDomainEnumerationIPCBlock));
    return SendRequestMessageAndWait(&sMessage);
}

#endif // RIGHT_SIDE_COMPILE

// Worker function for code:DbgTransportSession::SendEvent and code:DbgTransportSession::SendDebugEvent.
HRESULT DbgTransportSession::SendEventWorker(DebuggerIPCEvent * pEvent, IPCEventType type)
{
    DWORD cbEvent = GetEventSize(pEvent);
    _ASSERTE(cbEvent <= CorDBIPC_BUFFER_SIZE);

    Message sMessage;
    sMessage.Init(MT_Event, (PBYTE)pEvent, cbEvent);

    // Store the event type in the header as well, it's sometimes useful for debugging.
    sMessage.m_sHeader.TypeSpecificData.Event.m_eIPCEventType = type;
    sMessage.m_sHeader.TypeSpecificData.Event.m_eType = pEvent->type;

    return SendMessage(&sMessage, false);
}

// Sends a pre-formatted message (including the data block, if any). The fWaitsForReply indicates whether the
// caller is going to block until some sort of reply message is received (for instance an event that must be
// ack'd or a request such as MT_GetDCB that needs a reply). SendMessage() uses this to determine whether it
// needs to buffer the message before placing it on the send queue (since it may need to resend the message
// after a transitory network failure).
HRESULT DbgTransportSession::SendMessage(Message *pMessage, bool fWaitsForReply)
{
    // Serialize the whole operation under the state lock. In particular we need to make allocating the
    // message ID atomic wrt placing the message on the connection (to ensure our IDs are seen in order by the
    // other side). We also need to hold the lock while manipulating the send queue (to prevent corruption)
    // and while determining whether to send immediately or not depending on the session state (to avoid
    // posting a send on a closed and possibly recycled socket).
    {
        TransportLockHolder sLockHolder(&m_sStateLock);

        // Perform any last updates to the header or data block here since we might be about to encrypt them.

        // Give this message a unique ID (useful both to track which messages need to be resent on a network
        // failure and to match replies to the original message).
        pMessage->m_sHeader.m_dwId = m_dwNextMessageId++;

        // Use this message send to piggyback an acknowledgement of the last message we processed from the
        // other side (this will allow the other side to discard one or more buffered messages from its send
        // queue).
        pMessage->m_sHeader.m_dwLastSeenId = m_dwLastMessageIdSeen;

        // Check the session state.
        if (m_eState == SS_Closed)
        {
            // SS_Closed is bad news, we'll never recover from that so error the send immediately.
            return E_ABORT;
        }

        // If the caller isn't waiting around for a reply we must make a copy of the message to place on the
        // send queue.
        pMessage->m_pOrigMessage = pMessage;
        Message *pMessageCopy = NULL;
        PBYTE pDataBlockCopy = NULL;
        if (!fWaitsForReply)
        {
            // Allocate a new message (includes an embedded message header).
            pMessageCopy = new (nothrow) Message();
            if (pMessageCopy == NULL)
                return E_OUTOFMEMORY;

            // Allocate a new data block if one is being used.
            if (pMessage->m_pbDataBlock)
            {
                pDataBlockCopy = new (nothrow) BYTE[pMessage->m_cbDataBlock];
                if (pDataBlockCopy == NULL)
                {
                    delete pMessageCopy;
                    return E_OUTOFMEMORY;
                }
            }

            // Copy the message descriptor over.
            memcpy(pMessageCopy, pMessage, sizeof(Message));

            // And the data block if applicable.
            if (pDataBlockCopy)
                memcpy(pDataBlockCopy, pMessage->m_pbDataBlock, pMessage->m_cbDataBlock);

            // The message copy still points to the wrong data block (if there is one).
            pMessageCopy->m_pbDataBlock = pDataBlockCopy;

            // Point the copy back to the original message.
            pMessageCopy->m_pOrigMessage = pMessage;

            // From now on we'll use the copy.
            pMessage = pMessageCopy;
        }

        // If the state is SS_Open we can send the message now.
        if (m_eState == SS_Open)
        {
            // Send the message header block followed by the data block if it's provided. Any network error will
            // be reported internally by SendBlock and result in a transition to the SS_Resync_NC state (and an
            // eventual resend of the data).
            if (SendBlock((PBYTE)&pMessage->m_sHeader, sizeof(MessageHeader)) && pMessage->m_pbDataBlock)
                SendBlock(pMessage->m_pbDataBlock, pMessage->m_cbDataBlock);
        }

        // Don't queue session management messages. We always recreate these if we need to re-send them.
        if (pMessage->m_sHeader.m_eType > MT_SessionClose)
        {
            // Regardless of session state we always queue the message for at least as long as it takes us to
            // be sure the other side has received the message.
            if (m_pSendQueueLast == NULL)
            {
                // Queue is currently empty.
                m_pSendQueueFirst = pMessage;
                m_pSendQueueLast = pMessage;
                pMessage->m_pNext = NULL;
            }
            else
            {
                // Place on end of queue.
                m_pSendQueueLast->m_pNext = pMessage;
                m_pSendQueueLast = pMessage;
                pMessage->m_pNext = NULL;
            }
        }
        else
        {
            if (pMessageCopy)
                delete pMessageCopy;
            if (pDataBlockCopy)
                delete [] pDataBlockCopy;
        }

        // If the state wasn't open there's nothing more to be done. The state will eventually transition to
        // either SS_Open (in which case the transport thread will send all pending messages for us at the
        // transition point) or SS_Closed (where the transport thread will drain the queue and discard each
        // message, setting m_fAborted if necessary).

    } // Leave m_sStateLock

    return S_OK;
}

// Helper method for sending messages requiring a reply (such as MT_GetDCB) and waiting on the result.
HRESULT DbgTransportSession::SendRequestMessageAndWait(Message *pMessage)
{
    // Allocate event to wait for reply on.
    pMessage->m_hReplyEvent = WszCreateEvent(NULL, FALSE, FALSE, NULL); // Auto-reset, not signalled
    if (pMessage->m_hReplyEvent == NULL)
        return E_OUTOFMEMORY;

    // Duplicate the handle to the event.  It's necessary to have two handles to the same event because
    // both this thread and the message pumping thread may be trying to access the handle at the same
    // time (e.g. closing the handle).  So we make a duplicate handle.  This thread is responsible for
    // closing hReplyEvent (the local variable) whereas the message pumping thread is responsible for
    // closing the handle on the message.
    HANDLE hReplyEvent = NULL;
    if (!DuplicateHandle(GetCurrentProcess(),
                         pMessage->m_hReplyEvent,
                         GetCurrentProcess(),
                         &hReplyEvent,
                         0,      // ignored since we are going to pass DUPLICATE_SAME_ACCESS
                         FALSE,
                         DUPLICATE_SAME_ACCESS))
    {
        return HRESULT_FROM_GetLastError();
    }

    // Send the request.
    HRESULT hr = SendMessage(pMessage, true);
    if (FAILED(hr))
    {
        // In this case, we need to close both handles since the message is never put into the send queue.
        // This thread is the only one who has access to the message.
        CloseHandle(pMessage->m_hReplyEvent);
        CloseHandle(hReplyEvent);
        return hr;
    }

    // At this point, the message pumping thread may receive the reply any time.  It may even receive the
    // reply message even before we wait on the event.  Keep this in mind.

    // Wait for a reply (by the time this event is signalled the message header will have been overwritten by
    // the reply and any output buffer provided will have been filled in).
#if defined(RIGHT_SIDE_COMPILE)
    HANDLE rgEvents[] = { hReplyEvent, m_hProcessExited };
#else  // !RIGHT_SIDE_COMPILE
    HANDLE rgEvents[] = { hReplyEvent };
#endif // RIGHT_SIDE_COMPILE

    DWORD dwResult = WaitForMultipleObjectsEx(sizeof(rgEvents)/sizeof(rgEvents[0]), rgEvents, FALSE, INFINITE, FALSE);

    if (dwResult == WAIT_OBJECT_0)
    {
        // This is the normal case.  The message pumping thread receives a reply from the debuggee process.
        // It signals the event to wake up this thread.
        CloseHandle(hReplyEvent);

        // Check whether the session aborted us due to a Shutdown().
        if (pMessage->m_fAborted)
            return E_ABORT;
    }
#if defined(RIGHT_SIDE_COMPILE)
    else if (dwResult == (WAIT_OBJECT_0 + 1))
    {
        // This is the complicated case.  This thread wakes up because the debuggee process is terminated.
        // At the same time, the message pumping thread may be in the process of handling the reply message.
        // We need to be careful here because there is a race condition.

        // Remove the original message from the send queue.  This is because in the case of a blocking message,
        // the message can be allocated on the stack.  Thus, the message becomes invalid when we return from
        // this function.  The message pumping thread may have beaten this thread to it.  That's ok since
        // RemoveMessageFromSendQueue() takes the state lock.
        Message * pOriginalMessage = RemoveMessageFromSendQueue(pMessage->m_sHeader.m_dwId);
        _ASSERTE((pOriginalMessage == NULL) || (pOriginalMessage == pMessage));

        // If the message pumping thread has beaten this thread to removing the original message, then this
        // thread must wait until the message pumping thread is done with the message before returning.
        // Otherwise, the message may become invalid when the message pumping thread is accessing it.
        // Fortunately, in this case, we know the message pumping thread is going to signal the event.
        if (pOriginalMessage == NULL)
        {
            WaitForSingleObject(hReplyEvent, INFINITE);
        }

        CloseHandle(hReplyEvent);
        return CORDBG_E_PROCESS_TERMINATED;
    }
#endif // RIGHT_SIDE_COMPILE
    else
    {
        // Should never get here.
        CloseHandle(hReplyEvent);
        UNREACHABLE();
    }

    return S_OK;
}

// Sends a single contiguous buffer of host memory over the connection. The caller is responsible for holding
// the state lock and ensuring the session state is SS_Open. Returns false if the send failed (the error will
// have already caused the recovery logic to kick in, so handling it is not required, the boolean is just
// returned so that any further blocks in the message are not sent).
bool DbgTransportSession::SendBlock(PBYTE pbBuffer, DWORD cbBuffer)
{
    _ASSERTE(m_eState == SS_Opening || m_eState == SS_Resync || m_eState == SS_Open);
    _ASSERTE(m_pipe.GetState() == TwoWayPipe::ServerConnected || m_pipe.GetState() == TwoWayPipe::ClientConnected);
    _ASSERTE(cbBuffer > 0);

    DBG_TRANSPORT_INC_STAT(SentBlocks);
    DBG_TRANSPORT_ADD_STAT(SentBytes, cbBuffer);

    //DbgTransportLog(LC_Proxy, "SendBlock(%08X, %u)", pbBuffer, cbBuffer);
    bool fSuccess;
    if (DBG_TRANSPORT_SHOULD_INJECT_FAULT(Send))
        fSuccess = false;
    else
        fSuccess = ((DWORD)m_pipe.Write(pbBuffer, cbBuffer) == cbBuffer);

    if (!fSuccess)
    {
        DbgTransportLog(LC_NetErrors, "Network error on Send()");
        DBG_TRANSPORT_INC_STAT(SendErrors);
        HandleNetworkError(true);
        return false;
    }

    return true;
}

// Receives a single contiguous buffer of host memory over the connection. No state lock needs to be held
// (receives are serialized by the fact they're only performed on the transport thread). Returns false if a
// network error is encountered (which will automatically transition the session into the correct retry
// state).
bool DbgTransportSession::ReceiveBlock(PBYTE pbBuffer, DWORD cbBuffer)
{
    _ASSERTE(m_pipe.GetState() == TwoWayPipe::ServerConnected || m_pipe.GetState() == TwoWayPipe::ClientConnected);
    _ASSERTE(cbBuffer > 0);

    DBG_TRANSPORT_INC_STAT(ReceivedBlocks);
    DBG_TRANSPORT_ADD_STAT(ReceivedBytes, cbBuffer);

    //DbgTransportLog(LC_Proxy, "ReceiveBlock(%08X, %u)", pbBuffer, cbBuffer);

    bool fSuccess;
    if (DBG_TRANSPORT_SHOULD_INJECT_FAULT(Receive))
        fSuccess = false;
    else
        fSuccess = ((DWORD)m_pipe.Read(pbBuffer, cbBuffer) == cbBuffer);

    if (!fSuccess)
    {
        DbgTransportLog(LC_NetErrors, "Network error on Receive()");
        DBG_TRANSPORT_INC_STAT(ReceiveErrors);
        HandleNetworkError(false);
        return false;
    }

    return true;
}

// Called upon encountering a network error (e.g. an error from Send() or Receive()). This handles pushing the
// session state into SS_Resync_NC or SS_Opening_NC in order to start the recovery process.
void DbgTransportSession::HandleNetworkError(bool fCallerHoldsStateLock)
{
    _ASSERTE(m_eState == SS_Open || m_eState == SS_Opening || m_eState == SS_Resync || !fCallerHoldsStateLock);

    // Check the easy cases first which don't require us to take the lock (because we don't transition the
    // state). These are the SS_Closed state (a network error doesn't matter when we're closing down the
    // session anyway) and the SS_*_NC states (which indicate someone else beat us to it, closed the
    // connection and has started recovery).
    if (m_eState == SS_Closed ||
        m_eState == SS_Opening_NC ||
        m_eState == SS_Resync_NC)
        return;

    // We need the state lock to perform a state transition.
    if (!fCallerHoldsStateLock)
        m_sStateLock.Enter();

    switch (m_eState)
    {
    case SS_Closed:
    case SS_Opening_NC:
    case SS_Resync_NC:
        // Still need to cope with the no-op states handled above since we could have transitioned into them
        // before we took the lock.
        break;

    case SS_Opening:
        // All work to transition SS_Opening to SS_Open is performed by the transport thread, so we know we're
        // on that thread. Consequently it's just enough to set the state to SS_Opening_NC and the thread will
        // notice the change when the SendMessage() or ReceiveBlock() call completes.
        m_eState = SS_Opening_NC;
        break;

    case SS_Resync:
        // Likewise, all the work to transition SS_Resync to SS_Open is performed by the transport thread, so
        // we know we're on that thread.
        m_eState = SS_Resync_NC;
        break;

    case SS_Open:
        // The state change to SS_Resync_NC will prompt the transport thread (which might be this thread) that
        // it should discard the current connection and reform a new one. It will also cause sends to be
        // queued instead of sent. In case we're not the transport thread and instead it is currently stuck in
        // a Receive (I don't entirely trust the connection to immediately fail these on a network problem)
        // we'll call CancelReceive() to abort the operation. The transport thread itself will handle the
        // actual Destroy() (having one thread do this management greatly simplifies things).
        m_eState = SS_Resync_NC;
        m_pipe.Disconnect();
        break;

    default:
        _ASSERTE(!"Unknown session state");
    }

    if (!fCallerHoldsStateLock)
        m_sStateLock.Leave();
}

// Scan the send queue and discard any messages which have been processed by the other side according to the
// specified ID). Messages waiting on a reply message (e.g. MT_GetDCB) will be retained until that reply is
// processed. FlushSendQueue will take the state lock.
void DbgTransportSession::FlushSendQueue(DWORD dwLastProcessedId)
{
    // Must access the send queue under the state lock.
    TransportLockHolder sLockHolder(&m_sStateLock);

    // Note that message headers (and data blocks) may be encrypted. Use the cached fields in the Message
    // structure to compare message IDs and types.

    Message *pMsg = m_pSendQueueFirst;
    Message *pLastMsg = NULL;
    while (pMsg)
    {
        if (pMsg->m_sHeader.m_dwId <= dwLastProcessedId)
        {
            // Message has been seen and processed by other side.
            // Check if we can discard it (i.e. it's not waiting on a reply message that needs the original
            // request to hang around).
#ifdef RIGHT_SIDE_COMPILE
            MessageType eType = pMsg->m_sHeader.m_eType;
            if (eType != MT_ReadMemory &&
                eType != MT_WriteMemory &&
                eType != MT_VirtualUnwind &&
                eType != MT_GetDCB &&
                eType != MT_SetDCB &&
                eType != MT_GetAppDomainCB)
#endif // RIGHT_SIDE_COMPILE
            {
#ifdef RIGHT_SIDE_COMPILE
                _ASSERTE(eType == MT_Event);
#endif // RIGHT_SIDE_COMPILE

                // We can discard this message.

                // Unlink it from the queue.
                if (pLastMsg == NULL)
                    m_pSendQueueFirst = pMsg->m_pNext;
                else
                    pLastMsg->m_pNext = pMsg->m_pNext;
                if (m_pSendQueueLast == pMsg)
                    m_pSendQueueLast = pLastMsg;

                Message *pDiscardMsg = pMsg;
                pMsg = pMsg->m_pNext;

                // If the message is a copy deallocate it (and the data block associated with it).
                if (pDiscardMsg->m_pOrigMessage != pDiscardMsg)
                {
                    if (pDiscardMsg->m_pbDataBlock)
                        delete [] pDiscardMsg->m_pbDataBlock;
                    delete pDiscardMsg;
                }

                continue;
            }
        }

        pLastMsg = pMsg;
        pMsg = pMsg->m_pNext;
    }
}

#ifdef RIGHT_SIDE_COMPILE
// Perform processing required to complete a request (such as MT_GetDCB) once a reply comes in. This includes
// reading data from the connection into the output buffer, removing the original message from the send queue
// and signalling the completion event. Returns true if no network error was encountered.
bool DbgTransportSession::ProcessReply(MessageHeader *pHeader)
{
    // Locate original message on the send queue.
    Message *pMsg = RemoveMessageFromSendQueue(pHeader->m_dwReplyId);

    // This can happen if the thread blocked waiting for the replyl message has waken up because the debuggee
    // process has terminated.  See code:DbgTransportSession::SendRequestMessageAndWait() for more info.
    if (pMsg == NULL)
    {
        return true;
    }

    // If there is a reply block but the caller hasn't specified a reply buffer.
    // This combination is not used any more.
    _ASSERTE(! ((pHeader->m_cbDataBlock != (DWORD)0) && (pMsg->m_pbReplyBlock == (PBYTE)NULL)) );

    // If there was an output buffer provided then we copy the data block in the reply into it (perhaps
    // decrypting it first). If the reply header indicates there is no data block then presumably the request
    // failed (which should be indicated in the TypeSpecificData of the reply, ala MT_ReadMemory).
    if (pMsg->m_pbReplyBlock && pHeader->m_cbDataBlock)
    {
        _ASSERTE(pHeader->m_cbDataBlock == pMsg->m_cbReplyBlock);
        if (!ReceiveBlock(pMsg->m_pbReplyBlock, pMsg->m_cbReplyBlock))
        {
            // Whoops. We hit an error trying to read the reply data. We need to push the original message
            // back on the queue and await a retry. Since this message must have been seen by the other side
            // we don't need to put it on the queue in order (it will never be resent). Easiest just to put it
            // on the head.
            {
                TransportLockHolder sLockHolder(&m_sStateLock);
                pMsg->m_pNext = m_pSendQueueFirst;
                m_pSendQueueFirst = pMsg;
                if (m_pSendQueueLast == NULL)
                    m_pSendQueueLast = pMsg;
                return false;
            } // Leave m_sStateLock
        }
    }

    // Copy TypeSpecificData from the reply back into the original message (it can contain additional status).
    // Be careful to update the real original message (the version on the queue will be a copy if we're using
    // a secure session).
    pMsg->m_pOrigMessage->m_sHeader.TypeSpecificData = pHeader->TypeSpecificData;

    // **** IMPORTANT NOTE ****
    // We're about to cause a side-effect visible to our client. From here on out (until we update the
    // session's idea of the last incoming message we processed back in the transport thread's main loop) we
    // must avoid any failures. If we fail before the update the other side will re-send the message which is
    // bad if we've already processed it. See the comment near the start of the SS_Open message dispatch logic
    // for more details.
    // **** IMPORTANT NOTE ****

    // Signal the completion event.
    SignalReplyEvent(pMsg);

    return true;
}

//---------------------------------------------------------------------------------------
//
// Upon receiving a reply message, signal the event on the message to wake up the thread waiting for
// the reply message and close the handle to the event.
//
// Arguments:
//    pMessage - the reply message to be processed
//

void DbgTransportSession::SignalReplyEvent(Message * pMessage)
{
    // Make a local copy of the event handle.  As soon as we signal the event, the thread blocked waiting on
    // the reply may wake up and trash the message.  See code:DbgTransportSession::SendRequestMessageAndWait()
    // for more info.
    HANDLE hReplyEvent = pMessage->m_hReplyEvent;
    _ASSERTE(hReplyEvent != NULL);

    SetEvent(hReplyEvent);
    CloseHandle(hReplyEvent);
}

//---------------------------------------------------------------------------------------
//
// Given a message ID, find the matching message in the send queue.  If there is no match, return NULL.
// If there is a match, remove the message from the send queue and return it.
//
// Arguments:
//    dwMessageId - the ID of the message to retrieve
//
// Return Value:
//    NULL if the specified message cannot be found.
//    Otherwise return the specified message with the side effect that it's also removed from the send queue.
//
// Notes:
//    The caller is NOT responsible for taking the state lock.  This function will do that.
//

DbgTransportSession::Message * DbgTransportSession::RemoveMessageFromSendQueue(DWORD dwMessageId)
{
    // Locate original message on the send queue.
    Message *pMsg = NULL;
    {
        TransportLockHolder sLockHolder(&m_sStateLock);

        pMsg = m_pSendQueueFirst;
        Message *pLastMsg = NULL;

        while (pMsg)
        {
            if (dwMessageId == pMsg->m_sHeader.m_dwId)
            {
                // Found the original message that this is a reply to. Unlink it.
                if (pLastMsg == NULL)
                    m_pSendQueueFirst = pMsg->m_pNext;
                else
                    pLastMsg->m_pNext = pMsg->m_pNext;

                if (m_pSendQueueLast == pMsg)
                    m_pSendQueueLast = pLastMsg;
                break;
            }

            pLastMsg = pMsg;
            pMsg = pMsg->m_pNext;
        }
    } // Leave m_sStateLock

    // could be NULL
    return pMsg;
}
#endif

#ifndef RIGHT_SIDE_COMPILE

// Check read and optionally write memory access to the specified range of bytes. Used to check
// ReadProcessMemory and WriteProcessMemory requests.
HRESULT DbgTransportSession::CheckBufferAccess(_In_reads_(cbBuffer) PBYTE pbBuffer, DWORD cbBuffer, bool fWriteAccess)
{
    // check for integer overflow
    if ((pbBuffer + cbBuffer) < pbBuffer)
    {
        return HRESULT_FROM_WIN32(ERROR_ARITHMETIC_OVERFLOW);
    }

    // VirtualQuery doesn't know much about memory allocated outside of PAL's VirtualAlloc
    // that's why on Unix we can't rely on in to detect invalid memory reads
#ifndef TARGET_UNIX
    do
    {
        // Find the attributes of the largest set of pages with common attributes starting from our base address.
        MEMORY_BASIC_INFORMATION sMemInfo;
        VirtualQuery(pbBuffer, &sMemInfo, sizeof(sMemInfo));

        DbgTransportLog(LC_Proxy, "CBA(%08X,%08X): State:%08X Protect:%08X BA:%08X RS:%08X",
                        pbBuffer, cbBuffer, sMemInfo.State, sMemInfo.Protect, sMemInfo.BaseAddress, sMemInfo.RegionSize);

        // The memory must be committed (i.e. have physical pages or backing store).
        if (sMemInfo.State != MEM_COMMIT)
            return HRESULT_FROM_WIN32(ERROR_INVALID_ADDRESS);

        // Check for compatible page protections. Lower byte of Protect has these (upper bytes have options we're
        // not interested in, cache modes and the like.
        DWORD dwProtect = sMemInfo.Protect & 0xff;

        if (fWriteAccess &&
            ((dwProtect & (PAGE_EXECUTE_READWRITE | PAGE_EXECUTE_WRITECOPY | PAGE_READWRITE | PAGE_WRITECOPY)) == 0))
            return HRESULT_FROM_WIN32(ERROR_NOACCESS);
        else if (!fWriteAccess &&
            ((dwProtect & (PAGE_EXECUTE_READ | PAGE_EXECUTE_READWRITE | PAGE_EXECUTE_WRITECOPY | PAGE_READONLY | PAGE_READWRITE | PAGE_WRITECOPY)) == 0))
            return HRESULT_FROM_WIN32(ERROR_NOACCESS);

        // If the requested range is bigger than the region we have queried,
        // we need to continue on to check the next region.
        if ((pbBuffer + cbBuffer) > ((PBYTE)sMemInfo.BaseAddress + sMemInfo.RegionSize))
        {
            PBYTE pbRegionEnd = reinterpret_cast<PBYTE>(sMemInfo.BaseAddress) + sMemInfo.RegionSize;
            cbBuffer = (DWORD)((pbBuffer + cbBuffer) - pbRegionEnd);
            pbBuffer = pbRegionEnd;
        }
        else
        {
            // We are done.  Set cbBuffer to 0 to exit this loop.
            cbBuffer = 0;
        }
    }
    while (cbBuffer > 0);
#else
    if (!PAL_ProbeMemory(pbBuffer, cbBuffer, fWriteAccess))
    {
        return HRESULT_FROM_WIN32(ERROR_INVALID_ADDRESS);
    }
#endif

    // The specified region has passed all of our checks.
    return S_OK;
}

#endif // !RIGHT_SIDE_COMPILE

// Initialize all session state to correct starting values. Used during Init() and on the LS when we
// gracefully close one session and prepare for another.
void DbgTransportSession::InitSessionState()
{
    DBG_TRANSPORT_INC_STAT(Sessions);

    m_dwMajorVersion = kCurrentMajorVersion;
    m_dwMinorVersion = kCurrentMinorVersion;

    memset(&m_sSessionID, 0, sizeof(m_sSessionID));

    m_pSendQueueFirst = NULL;
    m_pSendQueueLast = NULL;

    m_dwNextMessageId = 1;
    m_dwLastMessageIdSeen = 0;

    m_eState = SS_Opening_NC;

    m_cValidEventBuffers = 0;
    m_idxEventBufferHead = 0;
    m_idxEventBufferTail = 0;
}

// The entry point of the transport worker thread. This one's static, so we immediately dispatch to an
// instance method version defined below for convenience in the implementation.
DWORD WINAPI DbgTransportSession::TransportWorkerStatic(LPVOID pvContext)
{
    ((DbgTransportSession*)pvContext)->TransportWorker();

    // Nobody looks at this result, the choice of 0 is arbitrary.
    return 0;
}

// Macros used to simplify error and state transition handling within the transport worker loop. Errors are
// classified as either transient or critical. Transient errors (typically those from network operations)
// result in the connection being closed and rebuilt: we should eventually recover from them. Critical errors
// are those that cause a transition to the SS_Closed state, which the session never recovers from. These are
// normally due to protocol errors where we want to shut the transport down in case they are of malicious
// origin.
#define HANDLE_TRANSIENT_ERROR() do {           \
    HandleNetworkError(false);                  \
    m_pipe.Disconnect();                        \
    goto ResetConnection;                       \
} while (false)

#define HANDLE_CRITICAL_ERROR() do {            \
    m_eState = SS_Closed;                       \
    goto Shutdown;                              \
} while (false)

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
void DbgTransportSession::TransportWorker()
{
    _ASSERTE(m_eState == SS_Opening_NC);

    // Loop until shutdown. Each loop iteration involves forming a connection (or waiting for one to form)
    // followed by processing incoming messages on that connection until there's a failure (either here of
    // from a send on another thread) or the session shuts down. The connection is then closed and discarded
    // and we either go round the loop again (to recover our previous session state) or exit the method as
    // part of shutdown.
  ResetConnection:
    while (m_eState != SS_Closed)
    {
        _ASSERTE(m_eState == SS_Opening_NC || m_eState == SS_Resync_NC || m_eState == SS_Closed);

        DbgTransportLog(LC_Proxy, "Forming new connection");

#ifdef RIGHT_SIDE_COMPILE
        // The session is definitely not open at this point.
        ResetEvent(m_hSessionOpenEvent);

        // On the right side we initiate the connection via Connect(). A failure is dealt with by waiting a
        // little while and retrying (the LS may take a little while to set up). If there's nobody listening
        // the debugger will eventually get bored waiting for us and shutdown the session, which will
        // terminate this loop.
        ConnStatus eStatus;
        if (DBG_TRANSPORT_SHOULD_INJECT_FAULT(Connect))
            eStatus = SCS_NetworkFailure;
        else
        {
            if (m_pipe.Connect(m_pd))
            {
                eStatus = SCS_Success;
            }
            else
            {
                //not really sure that this is the real failure
                //TODO: we probably need to analyse GetErrorCode() here
                eStatus = SCS_NoListener;
            }
        }

        if (eStatus != SCS_Success)
        {
            DbgTransportLog(LC_Proxy, "AllocateConnection() failed with %u\n", eStatus);
            DBG_TRANSPORT_INC_STAT(MiscErrors);
            _ASSERTE(m_pipe.GetState() != TwoWayPipe::ClientConnected);
            Sleep(1000);
            continue;
        }
#else // RIGHT_SIDE_COMPILE
        ConnStatus eStatus;
        if (DBG_TRANSPORT_SHOULD_INJECT_FAULT(Accept))
            eStatus = SCS_NetworkFailure;
        else
        {
            ProcessDescriptor pd = ProcessDescriptor::FromCurrentProcess();
            if ((m_pipe.GetState() == TwoWayPipe::Created || m_pipe.CreateServer(pd)) &&
                 m_pipe.WaitForConnection())
            {
                eStatus = SCS_Success;
            }
            else
            {
                //not really sure that this is the real failure
                //TODO: we probably need to analyse GetErrorCode() here
                eStatus = SCS_NoListener;
            }
        }

        if (eStatus != SCS_Success)
        {
            DbgTransportLog(LC_Proxy, "Accept() failed with %u\n", eStatus);
            DBG_TRANSPORT_INC_STAT(MiscErrors);
            _ASSERTE(m_pipe.GetState() != TwoWayPipe::ServerConnected);
            Sleep(1000);
            continue;
        }

        // Note that when resynching a session we may let in a connection from a different debugger. That's
        // OK, we'll reject its SessionRequest message in due course and drop the connection.
#endif // RIGHT_SIDE_COMPILE

        DBG_TRANSPORT_INC_STAT(Connections);

        // We now have a connection. Transition to the next state (either SS_Opening or SS_Resync). The
        // primary purpose of this state transition is to let other threads know that this thread might now be
        // blocked on a Receive() on the newly formed connection (important if they want to transition the state
        // to SS_Closed).
        {
            TransportLockHolder sLockHolder(&m_sStateLock);

            if (m_eState == SS_Closed)
                break;
            else if (m_eState == SS_Opening_NC)
                m_eState = SS_Opening;
            else if (m_eState == SS_Resync_NC)
                m_eState = SS_Resync;
            else
                _ASSERTE(!"Bad session state");
        } // Leave m_sStateLock


        // Now we have a connection in place. Start reading messages and processing them. Which messages are
        // valid depends on whether we're in SS_Opening or SS_Resync (the state can change at any time
        // asynchronously to us to either SS_Closed or SS_Resync_NC but we're guaranteed the connection stays
        // valid (though not necessarily useful) until we notice this state change and Destroy() it ourself).
        // We check the state after each network operation.

        // During the SS_Opening and SS_Resync states we're guarantee to be the only thread posting sends, so
        // we can break the rules and use SendBlock without acquiring the state lock. (We use SendBlock a lot
        // during these phases because we're using simple Session* messages which don't require the extra
        // processing SendMessage gives us such as encryption or placement on the send queue).

        MessageHeader   sSendHeader;
        MessageHeader   sReceiveHeader;

        memset(&sSendHeader, 0, sizeof(MessageHeader));

        if (m_eState == SS_Opening)
        {
#ifdef RIGHT_SIDE_COMPILE
            // The right side actually starts things off by sending a SessionRequest message.

            SessionRequestData sDataBlock;

            sSendHeader.m_eType = MT_SessionRequest;
            sSendHeader.TypeSpecificData.VersionInfo.m_dwMajorVersion = kCurrentMajorVersion;
            sSendHeader.TypeSpecificData.VersionInfo.m_dwMinorVersion = kCurrentMinorVersion;

            // The start of the data block always contains a session ID. This is a GUID randomly generated at
            // Init() time.
            sSendHeader.m_cbDataBlock = sizeof(SessionRequestData);
            memcpy(&sDataBlock.m_sSessionID, &m_sSessionID, sizeof(m_sSessionID));

            // Send the header block followed by the data block. For failures during SS_Opening we just close
            // the connection and retry from the beginning (the failing send will already have caused a
            // transition into SS_Opening_NC. No need to use the same resend logic that SS_Resync does, since
            // no user messages have been sent and we can simply recreate the SessionRequest.
            DbgTransportLog(LC_Session, "Sending 'SessionRequest'");
            DBG_TRANSPORT_INC_STAT(SentSessionRequest);
            if (!SendBlock((PBYTE)&sSendHeader, sizeof(MessageHeader)) ||
                !SendBlock((PBYTE)&sDataBlock, sSendHeader.m_cbDataBlock))
                HANDLE_TRANSIENT_ERROR();

            // Wait for a reply.
            if (!ReceiveBlock((PBYTE)&sReceiveHeader, sizeof(MessageHeader)))
                HANDLE_TRANSIENT_ERROR();

            DbgTransportLogMessageReceived(&sReceiveHeader);

            // This should be either a SessionAccept or SessionReject. Any other message type will be treated
            // as a SessionReject (i.e. an unrecoverable failure that will leave the session in SS_Closed
            // permanently).
            if (sReceiveHeader.m_eType != MT_SessionAccept)
            {
                _ASSERTE(!"Unexpected response to SessionRequest");
                HANDLE_CRITICAL_ERROR();
            }

            // Validate the SessionAccept.
            if (sReceiveHeader.TypeSpecificData.VersionInfo.m_dwMajorVersion != kCurrentMajorVersion ||
                sReceiveHeader.m_cbDataBlock != (DWORD)0)
            {
                _ASSERTE(!"Malformed SessionAccept received");
                HANDLE_CRITICAL_ERROR();
            }

            // The LS might have negotiated the minor protocol version down.
            m_dwMinorVersion = sReceiveHeader.TypeSpecificData.VersionInfo.m_dwMinorVersion;
#else // RIGHT_SIDE_COMPILE

            // On the left side we wait for a SessionRequest first.
            if (!ReceiveBlock((PBYTE)&sReceiveHeader, sizeof(MessageHeader)))
                HANDLE_TRANSIENT_ERROR();

            DbgTransportLogMessageReceived(&sReceiveHeader);

            if (sReceiveHeader.m_eType != MT_SessionRequest)
            {
                _ASSERTE(!"Unexpected message type");
                HANDLE_CRITICAL_ERROR();
            }

            // Validate the SessionRequest.
            if (sReceiveHeader.TypeSpecificData.VersionInfo.m_dwMajorVersion != kCurrentMajorVersion ||
                sReceiveHeader.m_cbDataBlock != (DWORD)sizeof(SessionRequestData))
            {
                // Send a SessionReject message with the reason for rejection.
                sSendHeader.m_eType = MT_SessionReject;
                sSendHeader.TypeSpecificData.SessionReject.m_eReason = RR_IncompatibleVersion;
                sSendHeader.TypeSpecificData.SessionReject.m_dwMajorVersion = kCurrentMajorVersion;
                sSendHeader.TypeSpecificData.SessionReject.m_dwMinorVersion = kCurrentMinorVersion;

                DbgTransportLog(LC_Session, "Sending 'SessionReject(RR_IncompatibleVersion)'");
                DBG_TRANSPORT_INC_STAT(SentSessionReject);

                SendBlock((PBYTE)&sSendHeader, sizeof(MessageHeader));

                // Go back into the opening state rather than closed because we want to give the RS a chance
                // to correct the problem and try again.
                HANDLE_TRANSIENT_ERROR();
            }

            // Read the data block.
            SessionRequestData sDataBlock;
            if (!ReceiveBlock((PBYTE)&sDataBlock, sizeof(SessionRequestData)))
                HANDLE_TRANSIENT_ERROR();

            // If the RS only understands a lower minor protocol version than us then remember that fact.
            if (sReceiveHeader.TypeSpecificData.VersionInfo.m_dwMinorVersion < m_dwMinorVersion)
                m_dwMinorVersion = sReceiveHeader.TypeSpecificData.VersionInfo.m_dwMinorVersion;

            // Send a SessionAccept message back.
            sSendHeader.m_eType = MT_SessionAccept;
            sSendHeader.m_cbDataBlock = 0;
            sSendHeader.TypeSpecificData.VersionInfo.m_dwMajorVersion = kCurrentMajorVersion;
            sSendHeader.TypeSpecificData.VersionInfo.m_dwMinorVersion = m_dwMinorVersion;

            DbgTransportLog(LC_Session, "Sending 'SessionAccept'");
            DBG_TRANSPORT_INC_STAT(SentSessionAccept);

            if (!SendBlock((PBYTE)&sSendHeader, sizeof(MessageHeader)))
                HANDLE_TRANSIENT_ERROR();
#endif // RIGHT_SIDE_COMPILE

            // Everything pans out, we have a session formed. But we must send messages that queued up
            // before transitioning the state to open (otherwise a racing send could sneak in ahead).

            // Must access the send queue under the state lock.
            {
                TransportLockHolder sLockHolder(&m_sStateLock);
                Message *pMsg = m_pSendQueueFirst;
                while (pMsg)
                {
                    if (SendBlock((PBYTE)&pMsg->m_sHeader, sizeof(MessageHeader)) && pMsg->m_pbDataBlock)
                        SendBlock(pMsg->m_pbDataBlock, pMsg->m_cbDataBlock);
                    pMsg = pMsg->m_pNext;
                }

                // Check none of the sends failed.
                if (m_eState != SS_Opening)
                {
                    m_pipe.Disconnect();
                    continue;
                }
            } // Leave m_sStateLock

            // Finally we can transition to SS_Open.
            {
                TransportLockHolder sLockHolder(&m_sStateLock);
                if (m_eState == SS_Closed)
                    break;
                else if (m_eState == SS_Opening)
                    m_eState = SS_Open;
                else
                    _ASSERTE(!"Bad session state");
            } // Leave m_sStateLock

#ifdef RIGHT_SIDE_COMPILE
            // Signal any WaitForSessionToOpen() waiters that we've gotten to SS_Open.
            SetEvent(m_hSessionOpenEvent);
#endif // RIGHT_SIDE_COMPILE

            // We're ready to begin receiving normal incoming messages now.
        }
        else
        {
            // The SS_Resync case. Send a message indicating the last message we saw from the other side and
            // wait for a similar message to arrive for us.

            sSendHeader.m_eType = MT_SessionResync;
            sSendHeader.m_dwLastSeenId = m_dwLastMessageIdSeen;

            DbgTransportLog(LC_Session, "Sending 'SessionResync'");
            DBG_TRANSPORT_INC_STAT(SentSessionResync);

            if (!SendBlock((PBYTE)&sSendHeader, sizeof(MessageHeader)))
                HANDLE_TRANSIENT_ERROR();

            if (!ReceiveBlock((PBYTE)&sReceiveHeader, sizeof(MessageHeader)))
                HANDLE_TRANSIENT_ERROR();

#ifndef RIGHT_SIDE_COMPILE
            if (sReceiveHeader.m_eType == MT_SessionRequest)
            {
                DbgTransportLogMessageReceived(&sReceiveHeader);

                // This SessionRequest could be from a different debugger. In this case we should send a
                // SessionReject to let them know we're not available and close the connection so we can
                // re-listen for the original debugger.
                // Or it could be the original debugger re-sending the SessionRequest because the connection
                // died as we sent the SessionAccept.
                // We distinguish the two cases by looking at the session ID in the request.
                bool fRequestResend = false;

                // Only read the data block if it matches our expectations of its size.
                if (sReceiveHeader.m_cbDataBlock == (DWORD)sizeof(SessionRequestData))
                {
                    SessionRequestData sDataBlock;
                    if (!ReceiveBlock((PBYTE)&sDataBlock, sizeof(SessionRequestData)))
                        HANDLE_TRANSIENT_ERROR();

                    // Check the session ID for a match.
                    if (memcmp(&sDataBlock.m_sSessionID, &m_sSessionID, sizeof(m_sSessionID)) == 0)
                        // OK, everything checks out and this is a valid re-send of a SessionRequest.
                        fRequestResend = true;
                }

                if (fRequestResend)
                {
                    // The RS never got our SessionAccept. We must resend it.
                    memset(&sSendHeader, 0, sizeof(MessageHeader));
                    sSendHeader.m_eType = MT_SessionAccept;
                    sSendHeader.m_cbDataBlock = 0;
                    sSendHeader.TypeSpecificData.VersionInfo.m_dwMajorVersion = kCurrentMajorVersion;
                    sSendHeader.TypeSpecificData.VersionInfo.m_dwMinorVersion = m_dwMinorVersion;

                    DbgTransportLog(LC_Session, "Sending 'SessionAccept'");
                    DBG_TRANSPORT_INC_STAT(SentSessionAccept);

                    if (!SendBlock((PBYTE)&sSendHeader, sizeof(MessageHeader)))
                        HANDLE_TRANSIENT_ERROR();

                    // Now simply reset the connection. The RS should get the SessionAccept and transition to
                    // SS_Open then detect the connection loss and transition to SS_Resync_NC, which will
                    // finally sync the two sides.
                    HANDLE_TRANSIENT_ERROR();
                }
                else
                {
                    // This is the case where we must reject the request.
                    memset(&sSendHeader, 0, sizeof(MessageHeader));
                    sSendHeader.m_eType = MT_SessionReject;
                    sSendHeader.TypeSpecificData.SessionReject.m_eReason = RR_AlreadyAttached;
                    sSendHeader.TypeSpecificData.SessionReject.m_dwMajorVersion = kCurrentMajorVersion;
                    sSendHeader.TypeSpecificData.SessionReject.m_dwMinorVersion = kCurrentMinorVersion;

                    DbgTransportLog(LC_Session, "Sending 'SessionReject(RR_AlreadyAttached)'");
                    DBG_TRANSPORT_INC_STAT(SentSessionReject);

                    SendBlock((PBYTE)&sSendHeader, sizeof(MessageHeader));

                    HANDLE_TRANSIENT_ERROR();
                }
            }
#endif // !RIGHT_SIDE_COMPILE

            DbgTransportLogMessageReceived(&sReceiveHeader);

            // Handle all other invalid message types by shutting down (it may be an attempt to subvert the
            // protocol).
            if (sReceiveHeader.m_eType != MT_SessionResync)
            {
                _ASSERTE(!"Unexpected message type during SS_Resync");
                HANDLE_CRITICAL_ERROR();
            }

            // We've got our resync message. Go through the send queue and resend any messages that haven't
            // been processed by the other side. Those that have been processed can be discarded (unless
            // they're waiting for another form of higher level acknowledgement, such as a reply message).

            // Discard unneeded messages first.
            FlushSendQueue(sReceiveHeader.m_dwLastSeenId);

            // Must access the send queue under the state lock.
            {
                TransportLockHolder sLockHolder(&m_sStateLock);

                Message *pMsg = m_pSendQueueFirst;
                while (pMsg)
                {
                    if (pMsg->m_sHeader.m_dwId > sReceiveHeader.m_dwLastSeenId)
                    {
                        // The other side never saw this message, re-send it.
                        DBG_TRANSPORT_INC_STAT(Resends);
                        if (SendBlock((PBYTE)&pMsg->m_sHeader, sizeof(MessageHeader)) && pMsg->m_pbDataBlock)
                            SendBlock(pMsg->m_pbDataBlock, pMsg->m_cbDataBlock);
                    }
                    pMsg = pMsg->m_pNext;
                }

                // Finished processing queued sends. We can transition to the SS_Open state now as long as there
                // wasn't a send failure or an asynchronous Shutdown().
                if (m_eState == SS_Resync)
                    m_eState = SS_Open;
                else if (m_eState == SS_Closed)
                    break;
                else if (m_eState == SS_Resync_NC)
                {
                    m_pipe.Disconnect();
                    continue;
                }
                else
                    _ASSERTE(!"Bad session state");
            } // Leave m_sStateLock
        }

        // Once we get here we should be in SS_Open (can't assert this because Shutdown() can throw the state
        // into SS_Closed and we've just released SendMessage() calls on other threads that can transition us
        // into SS_Resync).

        // We now loop receiving messages and processing them until the state changes.
        while (m_eState == SS_Open)
        {
#ifndef RIGHT_SIDE_COMPILE
            // temporary data block used in DCB messages
            DebuggerIPCControlBlockTransport dcbt;

            // temporary virtual stack unwind context buffer
            CONTEXT frameContext;
#endif

            // Read a message header block.
            if (!ReceiveBlock((PBYTE)&sReceiveHeader, sizeof(MessageHeader)))
                HANDLE_TRANSIENT_ERROR();

            // Since we care about security here, perform some additional validation checks that make it
            // harder for a malicious sender to attack with random message data.
            if (sReceiveHeader.m_eType > MT_GetAppDomainCB ||
                (sReceiveHeader.m_dwId <= m_dwLastMessageIdSeen &&
                 sReceiveHeader.m_dwId != (DWORD)0) ||
                (sReceiveHeader.m_dwReplyId >= m_dwNextMessageId &&
                 sReceiveHeader.m_dwReplyId != (DWORD)0) ||
                (sReceiveHeader.m_dwLastSeenId >= m_dwNextMessageId &&
                 sReceiveHeader.m_dwLastSeenId != (DWORD)0))
            {
                _ASSERTE(!"Incoming message header looks bogus");
                HANDLE_CRITICAL_ERROR();
            }

            DbgTransportLogMessageReceived(&sReceiveHeader);

            // Flush any entries in our send queue for messages that the other side has just confirmed
            // processed with this message.
            FlushSendQueue(sReceiveHeader.m_dwLastSeenId);

#ifndef RIGHT_SIDE_COMPILE
            // State variables to track whether this message needs a reply and if so whether it consists of a
            // header only or a header and an optional data block.
            bool    fReplyRequired = false;
            PBYTE   pbOptReplyData = NULL;
            DWORD   cbOptReplyData = 0;
            HRESULT hr             = E_FAIL;

            // if you change the lifetime of resultBuffer, make sure you change pbOptReplyData to match.
            // In some cases pbOptReplyData will point at the memory held alive in resultBuffer
            WriteBuffer resultBuffer;
            ReadBuffer  receiveBuffer;

#endif // RIGHT_SIDE_COMPILE

            // Dispatch based on message type.
            //
            // **** IMPORTANT NOTE ****
            //
            // We must be very careful wrt to updating m_dwLastMessageIdSeen here. If we update it too soon
            // (we haven't finished receiving the entire message, for instance) then the other side won't
            // re-send the message on failure and we'll lose it. If we update it too late we might have
            // reported the message to our caller or produced any other side-effect we can't take back such as
            // sending a reply and then hit an error and reset the connection before we had a chance to record
            // the message as seen. In this case the other side will re-send the original message and we'll
            // repeat our actions, which is also very bad.
            //
            // So we must be very disciplined here.
            //
            // First we must read the message in its entirety (i.e. receive the data block if there is one)
            // without causing any side-effects. This ensures that any failure at this point will be handled
            // correctly (by the other side re-sending us the same message).
            //
            // Then we process the message. At this point we are committed. The processing must always
            // succeed, or have no side-effect (that we care about) or we must have an additional scheme to
            // handle resynchronization in the event of failure. This ensures that we don't have the tricky
            // situation where we can't cope with a re-send of the message (because we've started processing
            // it) but can't report a failure to the other side (because we don't know how).
            //
            // Finally we must ensure that there is no error path between the completion of processing and
            // updating the m_dwLastMessageIdSeen field. This ensures we don't accidently get re-sent a
            // message we've processed completely (it's really just a sub-case of the rule above, but it's
            // worth pointing out explicitly since it can be a subtle problem).
            //
            // Request messages (such as MT_GetDCB) are an interesting case in point here. They all require a
            // reply and we can fail on the reply because we run out of system resources. This breaks the
            // second rule above (we fail halfway through processing). We should really preallocate enough
            // resources to send the reply before we begin processing of it but for now we don't since (a) the
            // SendMessage system isn't currently set up to make this easy and (b) we happen to know that all
            // the request types are effectively idempotent (even ReadMemory and WriteMemory since the RS is
            // holding the LS still while it does these). So instead we must carefully distinguish the case
            // where SendMessage fails without possibility of message transmission (e.g. out of memory) and
            // those where it fails for a transient network failure (where it will re-send the reply on
            // resync). This is easy enough to do since SendMessage returns a failure hresult for the first
            // case and success (and a state transition) for the second. In the first case we don't update
            // m_dwLastMessageIdSeen and instead wait for the request to be resent. In the second we make the
            // update because we know the reply will get through eventually.
            //
            // **** IMPORTANT NOTE ****
            switch (sReceiveHeader.m_eType)
            {
            case MT_SessionRequest:
            case MT_SessionAccept:
            case MT_SessionReject:
            case MT_SessionResync:
                // Illegal messages at this time, fail the transport entirely.
                m_eState = SS_Closed;
                break;

            case MT_SessionClose:
                // Close is legal on the LS and transitions to the SS_Opening_NC state. It's illegal on the RS
                // and should shutdown the transport.
#ifdef RIGHT_SIDE_COMPILE
                m_eState = SS_Closed;
                break;
#else // RIGHT_SIDE_COMPILE
                // We need to do some state cleanup here, since when we reform a connection (if ever, it will
                // be with a new session).
                {
                    TransportLockHolder sLockHolder(&m_sStateLock);

                    // Check we're still in a good state before a clean restart.
                    if (m_eState != SS_Open)
                    {
                        m_eState = SS_Closed;
                        break;
                    }

                    m_pipe.Disconnect();

                    // We could add code to drain the send queue here (like we have for SS_Closed at the end of
                    // this method) but I'm pretty sure we can only get a graceful session close with no
                    // outstanding sends. So just assert the queue is empty instead. If the assert fires and it's
                    // not due to an issue we can add the logic here).
                    _ASSERTE(m_pSendQueueFirst == NULL);
                    _ASSERTE(m_pSendQueueLast == NULL);

                    // This will reset all session specific state and transition us to SS_Opening_NC.
                    InitSessionState();
                } // Leave m_sStateLock

                goto ResetConnection;
#endif // RIGHT_SIDE_COMPILE

            case MT_Event:
            {
                // Incoming debugger event.

                if (sReceiveHeader.m_cbDataBlock > CorDBIPC_BUFFER_SIZE)
                {
                    _ASSERTE(!"Oversized Event");
                    HANDLE_CRITICAL_ERROR();
                }

                // See if our array of buffered events has filled up. If so we'll need to re-allocate the
                // array to expand it.
                if (m_cValidEventBuffers == m_cEventBuffers)
                {
                    // Allocate a larger array.
                    DWORD cNewEntries = m_cEventBuffers + 4;
                    DbgEventBufferEntry * pNewBuffers = (DbgEventBufferEntry *)new (nothrow) BYTE[cNewEntries * sizeof(DbgEventBufferEntry)];
                    if (pNewBuffers == NULL)
                        HANDLE_TRANSIENT_ERROR();

                    // We must take the lock to swap the new array in. Although this thread is the only one
                    // that can expand the array, a client thread may be in GetNextEvent() reading from the
                    // old version.
                    {
                        TransportLockHolder sLockHolder(&m_sStateLock);

                        // When we copy old array contents over we place the head of the list at the start of
                        // the new array for simplicity. If the head happened to be at the start of the old
                        // array anyway, this is even simpler.
                        if (m_idxEventBufferHead == 0)
                            memcpy(pNewBuffers, m_pEventBuffers, m_cEventBuffers * sizeof(DbgEventBufferEntry));
                        else
                        {
                            // Otherwise we need to perform the copy in two segments: first we copy the head
                            // of the list (starts at a non-zero index and runs to the end of the old array)
                            // into the start of the new array.
                            DWORD cHeadEntries = m_cEventBuffers - m_idxEventBufferHead;

                            memcpy(pNewBuffers,
                                   &m_pEventBuffers[m_idxEventBufferHead],
                                   cHeadEntries * sizeof(DbgEventBufferEntry));

                            // Then we copy the remaining portion from the beginning of the old array upto to
                            // the index of the head.
                            memcpy(&pNewBuffers[cHeadEntries],
                                   m_pEventBuffers,
                                   m_idxEventBufferHead * sizeof(DbgEventBufferEntry));
                        }

                        // Delete the old array.
                        delete [] m_pEventBuffers;

                        // Swap the new array in.
                        m_pEventBuffers = pNewBuffers;
                        m_cEventBuffers = cNewEntries;

                        // The new array now has the head at index zero and the tail at the start of the
                        // new entries.
                        m_idxEventBufferHead = 0;
                        m_idxEventBufferTail = m_cValidEventBuffers;
                    }
                }

                // We have at least one free buffer at this point (no threading issues, the only thread that
                // can add entries is this one).

                // Receive event data into the tail buffer (we want to do this without holding the state lock
                // and can do so safely since this is the only thread that can receive data and clients can do
                // nothing that impacts the location of the tail of the buffer list).
                if (!ReceiveBlock((PBYTE)&m_pEventBuffers[m_idxEventBufferTail].m_event, sReceiveHeader.m_cbDataBlock))
                    HANDLE_TRANSIENT_ERROR();

                {
                    m_pEventBuffers[m_idxEventBufferTail].m_type = sReceiveHeader.TypeSpecificData.Event.m_eIPCEventType;

                    // We must take the lock to update the count of valid entries though, since clients can
                    // touch this field as well.
                    TransportLockHolder sLockHolder(&m_sStateLock);

                    m_cValidEventBuffers++;
                    DWORD idxCurrentEvent = m_idxEventBufferTail;

                    // Update tail of the list (strictly speaking this needn't be done under the lock, but the
                    // code in GetNextEvent() does read it for an assert.
                    m_idxEventBufferTail = (m_idxEventBufferTail + 1) % m_cEventBuffers;

                    // If we just added the first valid event then wake up the client so they can call
                    // GetNextEvent().
                    if (m_cValidEventBuffers == 1)
                        SetEvent(m_rghEventReadyEvent[m_pEventBuffers[idxCurrentEvent].m_type]);
                }
            }
            break;

            case MT_ReadMemory:
#ifdef RIGHT_SIDE_COMPILE
                if (!ProcessReply(&sReceiveHeader))
                    HANDLE_TRANSIENT_ERROR();
#else // RIGHT_SIDE_COMPILE
                // The RS wants to read our memory. First check the range requested is both committed and
                // readable. If that succeeds we simply set the optional reply block to match the request region
                // (i.e. we send the memory directly).
                fReplyRequired = true;

                hr = CheckBufferAccess(sReceiveHeader.TypeSpecificData.MemoryAccess.m_pbLeftSideBuffer,
                                       sReceiveHeader.TypeSpecificData.MemoryAccess.m_cbLeftSideBuffer,
                                       false);
                sReceiveHeader.TypeSpecificData.MemoryAccess.m_hrResult = hr;
                if (SUCCEEDED(hr))
                {
                    pbOptReplyData = sReceiveHeader.TypeSpecificData.MemoryAccess.m_pbLeftSideBuffer;
                    cbOptReplyData = sReceiveHeader.TypeSpecificData.MemoryAccess.m_cbLeftSideBuffer;
                }
#endif // RIGHT_SIDE_COMPILE
                break;

            case MT_WriteMemory:
#ifdef RIGHT_SIDE_COMPILE
                if (!ProcessReply(&sReceiveHeader))
                    HANDLE_TRANSIENT_ERROR();
#else // RIGHT_SIDE_COMPILE
                // The RS wants to write our memory.
                if (sReceiveHeader.m_cbDataBlock != sReceiveHeader.TypeSpecificData.MemoryAccess.m_cbLeftSideBuffer)
                {
                    _ASSERTE(!"Inconsistent WriteMemory request");
                    HANDLE_CRITICAL_ERROR();
                }

                fReplyRequired = true;

                // Check the range requested is both committed and writeable. If that succeeds we simply read
                // the next incoming block into the destination buffer.
                hr = CheckBufferAccess(sReceiveHeader.TypeSpecificData.MemoryAccess.m_pbLeftSideBuffer,
                                       sReceiveHeader.TypeSpecificData.MemoryAccess.m_cbLeftSideBuffer,
                                       true);
                if (SUCCEEDED(hr))
                {
                    if (!ReceiveBlock(sReceiveHeader.TypeSpecificData.MemoryAccess.m_pbLeftSideBuffer,
                                      sReceiveHeader.TypeSpecificData.MemoryAccess.m_cbLeftSideBuffer))
                        HANDLE_TRANSIENT_ERROR();
                }
                else
                {
                    sReceiveHeader.TypeSpecificData.MemoryAccess.m_hrResult = hr;

                    // We might be failing the write attempt but we still need to read the update data to
                    // drain it from the connection or we'll become unsynchronized (i.e. we'll treat the start
                    // of the write data as the next message header). So read and discard the data into a
                    // dummy buffer.
                    BYTE    rgDummy[256];
                    DWORD   cbBytesToRead = sReceiveHeader.TypeSpecificData.MemoryAccess.m_cbLeftSideBuffer;
                    while (cbBytesToRead)
                    {
                        DWORD cbTransfer = min(cbBytesToRead, sizeof(rgDummy));
                        if (!ReceiveBlock(rgDummy, cbTransfer))
                            HANDLE_TRANSIENT_ERROR();
                        cbBytesToRead -= cbTransfer;
                    }
                }
#endif // RIGHT_SIDE_COMPILE
                break;

            case MT_VirtualUnwind:
#ifdef RIGHT_SIDE_COMPILE
                if (!ProcessReply(&sReceiveHeader))
                    HANDLE_TRANSIENT_ERROR();
#else // RIGHT_SIDE_COMPILE
                if (sReceiveHeader.m_cbDataBlock != (DWORD)sizeof(frameContext))
                {
                    _ASSERTE(!"Inconsistent VirtualUnwind request");
                    HANDLE_CRITICAL_ERROR();
                }

                if (!ReceiveBlock((PBYTE)&frameContext, sizeof(frameContext)))
                {
                    HANDLE_TRANSIENT_ERROR();
                }

                if (!PAL_VirtualUnwind(&frameContext, NULL))
                {
                    HANDLE_TRANSIENT_ERROR();
                }

                fReplyRequired = true;
                pbOptReplyData = (PBYTE)&frameContext;
                cbOptReplyData = sizeof(frameContext);
#endif // RIGHT_SIDE_COMPILE
                break;

            case MT_GetDCB:
#ifdef RIGHT_SIDE_COMPILE
                if (!ProcessReply(&sReceiveHeader))
                    HANDLE_TRANSIENT_ERROR();
#else // RIGHT_SIDE_COMPILE
                fReplyRequired = true;
                MarshalDCBToDCBTransport(m_pDCB, &dcbt);
                pbOptReplyData = (PBYTE)&dcbt;
                cbOptReplyData = sizeof(DebuggerIPCControlBlockTransport);
#endif // RIGHT_SIDE_COMPILE
                break;

            case MT_SetDCB:
#ifdef RIGHT_SIDE_COMPILE
                if (!ProcessReply(&sReceiveHeader))
                    HANDLE_TRANSIENT_ERROR();
#else // RIGHT_SIDE_COMPILE
                if (sReceiveHeader.m_cbDataBlock != (DWORD)sizeof(DebuggerIPCControlBlockTransport))
                {
                    _ASSERTE(!"Inconsistent SetDCB request");
                    HANDLE_CRITICAL_ERROR();
                }

                fReplyRequired = true;

                if (!ReceiveBlock((PBYTE)&dcbt, sizeof(DebuggerIPCControlBlockTransport)))
                    HANDLE_TRANSIENT_ERROR();

                MarshalDCBTransportToDCB(&dcbt, m_pDCB);
#endif // RIGHT_SIDE_COMPILE
                break;

            case MT_GetAppDomainCB:
#ifdef RIGHT_SIDE_COMPILE
                if (!ProcessReply(&sReceiveHeader))
                    HANDLE_TRANSIENT_ERROR();
#else // RIGHT_SIDE_COMPILE
                fReplyRequired = true;
                pbOptReplyData = (PBYTE)m_pADB;
                cbOptReplyData = sizeof(AppDomainEnumerationIPCBlock);
#endif // RIGHT_SIDE_COMPILE
                break;

            default:
                _ASSERTE(!"Unknown message type");
                HANDLE_CRITICAL_ERROR();
            }

#ifndef RIGHT_SIDE_COMPILE
            // On the left side we may need to send a reply back.
            if (fReplyRequired)
            {
                Message sReply;
                sReply.Init(sReceiveHeader.m_eType, pbOptReplyData, cbOptReplyData);
                sReply.m_sHeader.m_dwReplyId = sReceiveHeader.m_dwId;
                sReply.m_sHeader.TypeSpecificData = sReceiveHeader.TypeSpecificData;

#ifdef _DEBUG
                DbgTransportLog(LC_Requests, "Sending '%s' reply", MessageName(sReceiveHeader.m_eType));
#endif // _DEBUG

                // We must be careful with the failure mode of SendMessage here to avoid the same request
                // being processed too many or too few times. See the comment above starting with 'IMPORTANT
                // NOTE' for more details. The upshot is that on SendMessage hresult failures (which indicate
                // the message will never be sent), we don't update m_dwLastMessageIdSeen and simply wait for
                // the request to be made again. When we get success, however, we must be careful to ensure
                // that m_dwLastMessageIdSeen gets updated even if a network error is reported. Otherwise on
                // the resync we'll both reprocess the request and re-send the original reply which is very
                // very bad.
                hr = SendMessage(&sReply, false);

                if (FAILED(hr))
                    HANDLE_TRANSIENT_ERROR(); // Message will never be sent, other side will retry

                // SendMessage doesn't report network errors (it simply queues the send and changes the
                // session state). So check for a network error here specifically so we can get started on the
                // resync. We must update m_dwLastMessageIdSeen first though, or the other side will retry the
                // request.
                if (m_eState != SS_Open)
                {
                    _ASSERTE(sReceiveHeader.m_dwId > m_dwLastMessageIdSeen);
                    m_dwLastMessageIdSeen = sReceiveHeader.m_dwId;
                    HANDLE_TRANSIENT_ERROR();
                }
            }
#endif // !RIGHT_SIDE_COMPILE

            if (sReceiveHeader.m_dwId != (DWORD)0)
            {
                // We've now completed processing on the incoming message. Remember we've processed up to this
                // message ID so that on a resync the other side doesn't send it to us again.
                _ASSERTE(sReceiveHeader.m_dwId > m_dwLastMessageIdSeen);
                m_dwLastMessageIdSeen = sReceiveHeader.m_dwId;
            }
        }
    }

  Shutdown:

    _ASSERTE(m_eState == SS_Closed);

#ifdef RIGHT_SIDE_COMPILE
    // The session is definitely not open at this point.
    ResetEvent(m_hSessionOpenEvent);
#endif // RIGHT_SIDE_COMPILE

    // Close the connection if we haven't done so already.
    m_pipe.Disconnect();

    // Drain any remaining entries in the send queue (aborting them when they need completions).
    {
        TransportLockHolder sLockHolder(&m_sStateLock);

        Message *pMsg;
        while ((pMsg = m_pSendQueueFirst) != NULL)
        {
            // Remove message from the queue.
            m_pSendQueueFirst = pMsg->m_pNext;

            // Determine whether the message needs to be deleted by us before we signal any completion (because
            // once we signal the completion pMsg might become invalid immediately if it's not a copy).
            bool fMustDelete = pMsg->m_pOrigMessage != pMsg;

            // If there's a waiter (i.e. we don't own the message) it know that the operation didn't really
            // complete, it was aborted.
            if (!fMustDelete)
                pMsg->m_pOrigMessage->m_fAborted = true;

            // Determine how to complete the message.
            switch (pMsg->m_sHeader.m_eType)
            {
            case MT_SessionRequest:
            case MT_SessionAccept:
            case MT_SessionReject:
            case MT_SessionResync:
            case MT_SessionClose:
                _ASSERTE(!"Session management messages should not be on send queue");
                break;

            case MT_Event:
                break;

#ifdef RIGHT_SIDE_COMPILE
            case MT_ReadMemory:
            case MT_WriteMemory:
            case MT_VirtualUnwind:
            case MT_GetDCB:
            case MT_SetDCB:
            case MT_GetAppDomainCB:
                // On the RS these are the original requests. Signal the completion event.
                SignalReplyEvent(pMsg);
                break;
#else // RIGHT_SIDE_COMPILE
            case MT_ReadMemory:
            case MT_WriteMemory:
            case MT_VirtualUnwind:
            case MT_GetDCB:
            case MT_SetDCB:
            case MT_GetAppDomainCB:
                // On the LS these are replies to the original request. Nobody's waiting on these.
                break;
#endif // RIGHT_SIDE_COMPILE

            default:
                _ASSERTE(!"Unknown message type");
            }

            // If the message was a copy, deallocate the resources now.
            if (fMustDelete)
            {
                if (pMsg->m_pbDataBlock)
                    delete [] pMsg->m_pbDataBlock;
                delete pMsg;
            }
        }
    } // Leave m_sStateLock

    // Now release all the resources allocated for the transport now that the
    // worker thread isn't using them anymore.
    Release();
}

// Given a fully initialized debugger event structure, return the size of the structure in bytes (this is not
// trivial since DebuggerIPCEvent contains a large union member which can cause the portion containing
// significant data to vary wildy from event to event).
DWORD DbgTransportSession::GetEventSize(DebuggerIPCEvent *pEvent)
{
    DWORD cbBaseSize = offsetof(DebuggerIPCEvent, LeftSideStartupData);
    DWORD cbAdditionalSize = 0;

    switch (pEvent->type & DB_IPCE_TYPE_MASK)
    {
    case DB_IPCE_SYNC_COMPLETE:
    case DB_IPCE_THREAD_ATTACH:
    case DB_IPCE_THREAD_DETACH:
    case DB_IPCE_USER_BREAKPOINT:
    case DB_IPCE_EXIT_APP_DOMAIN:
    case DB_IPCE_SET_DEBUG_STATE_RESULT:
    case DB_IPCE_FUNC_EVAL_ABORT_RESULT:
    case DB_IPCE_CONTROL_C_EVENT:
    case DB_IPCE_FUNC_EVAL_CLEANUP_RESULT:
    case DB_IPCE_SET_METHOD_JMC_STATUS_RESULT:
    case DB_IPCE_SET_MODULE_JMC_STATUS_RESULT:
    case DB_IPCE_FUNC_EVAL_RUDE_ABORT_RESULT:
    case DB_IPCE_INTERCEPT_EXCEPTION_RESULT:
    case DB_IPCE_INTERCEPT_EXCEPTION_COMPLETE:
    case DB_IPCE_CREATE_PROCESS:
    case DB_IPCE_SET_NGEN_COMPILER_FLAGS_RESULT:
    case DB_IPCE_LEFTSIDE_STARTUP:
    case DB_IPCE_ASYNC_BREAK:
    case DB_IPCE_CONTINUE:
    case DB_IPCE_ATTACHING:
    case DB_IPCE_GET_NGEN_COMPILER_FLAGS:
    case DB_IPCE_DETACH_FROM_PROCESS:
    case DB_IPCE_CONTROL_C_EVENT_RESULT:
    case DB_IPCE_BEFORE_GARBAGE_COLLECTION:
    case DB_IPCE_AFTER_GARBAGE_COLLECTION:
        cbAdditionalSize = 0;
        break;
    case DB_IPCE_DATA_BREAKPOINT:
        cbAdditionalSize = sizeof(pEvent->DataBreakpointData);
        break;

    case DB_IPCE_BREAKPOINT:
        cbAdditionalSize = sizeof(pEvent->BreakpointData);
        break;

    case DB_IPCE_LOAD_MODULE:
        cbAdditionalSize = sizeof(pEvent->LoadModuleData);
        break;

    case DB_IPCE_UNLOAD_MODULE:
        cbAdditionalSize = sizeof(pEvent->UnloadModuleData);
        break;

    case DB_IPCE_LOAD_CLASS:
        cbAdditionalSize = sizeof(pEvent->LoadClass);
        break;

    case DB_IPCE_UNLOAD_CLASS:
        cbAdditionalSize = sizeof(pEvent->UnloadClass);
        break;

    case DB_IPCE_EXCEPTION:
        cbAdditionalSize = sizeof(pEvent->Exception);
        break;

    case DB_IPCE_BREAKPOINT_ADD_RESULT:
        cbAdditionalSize = sizeof(pEvent->BreakpointData);
        break;

    case DB_IPCE_STEP_RESULT:
        cbAdditionalSize = sizeof(pEvent->StepData);
        if (pEvent->StepData.rangeCount)
            cbAdditionalSize += (pEvent->StepData.rangeCount - 1) * sizeof(COR_DEBUG_STEP_RANGE);
        break;

    case DB_IPCE_STEP_COMPLETE:
        cbAdditionalSize = sizeof(pEvent->StepData);
        break;

    case DB_IPCE_GET_BUFFER_RESULT:
        cbAdditionalSize = sizeof(pEvent->GetBufferResult);
        break;

    case DB_IPCE_RELEASE_BUFFER_RESULT:
        cbAdditionalSize = sizeof(pEvent->ReleaseBufferResult);
        break;

    case DB_IPCE_ENC_ADD_FIELD:
        cbAdditionalSize = sizeof(pEvent->EnCUpdate);
        break;

    case DB_IPCE_APPLY_CHANGES_RESULT:
        cbAdditionalSize = sizeof(pEvent->ApplyChangesResult);
        break;

    case DB_IPCE_FIRST_LOG_MESSAGE:
        cbAdditionalSize = sizeof(pEvent->FirstLogMessage);
        break;

    case DB_IPCE_LOGSWITCH_SET_MESSAGE:
        cbAdditionalSize = sizeof(pEvent->LogSwitchSettingMessage);
        break;

    case DB_IPCE_CREATE_APP_DOMAIN:
        cbAdditionalSize = sizeof(pEvent->AppDomainData);
        break;

    case DB_IPCE_LOAD_ASSEMBLY:
        cbAdditionalSize = sizeof(pEvent->AssemblyData);
        break;

    case DB_IPCE_UNLOAD_ASSEMBLY:
        cbAdditionalSize = sizeof(pEvent->AssemblyData);
        break;

    case DB_IPCE_FUNC_EVAL_SETUP_RESULT:
        cbAdditionalSize = sizeof(pEvent->FuncEvalSetupComplete);
        break;

    case DB_IPCE_FUNC_EVAL_COMPLETE:
        cbAdditionalSize = sizeof(pEvent->FuncEvalComplete);
        break;

    case DB_IPCE_SET_REFERENCE_RESULT:
        cbAdditionalSize = sizeof(pEvent->SetReference);
        break;

    case DB_IPCE_NAME_CHANGE:
        cbAdditionalSize = sizeof(pEvent->NameChange);
        break;

    case DB_IPCE_UPDATE_MODULE_SYMS:
        cbAdditionalSize = sizeof(pEvent->UpdateModuleSymsData);
        break;

    case DB_IPCE_ENC_REMAP:
        cbAdditionalSize = sizeof(pEvent->EnCRemap);
        break;

    case DB_IPCE_SET_VALUE_CLASS_RESULT:
        cbAdditionalSize = sizeof(pEvent->SetValueClass);
        break;

    case DB_IPCE_BREAKPOINT_SET_ERROR:
        cbAdditionalSize = sizeof(pEvent->BreakpointSetErrorData);
        break;

    case DB_IPCE_ENC_UPDATE_FUNCTION:
        cbAdditionalSize = sizeof(pEvent->EnCUpdate);
        break;

    case DB_IPCE_GET_METHOD_JMC_STATUS_RESULT:
        cbAdditionalSize = sizeof(pEvent->SetJMCFunctionStatus);
        break;

    case DB_IPCE_GET_THREAD_FOR_TASKID_RESULT:
        cbAdditionalSize = sizeof(pEvent->GetThreadForTaskIdResult);
        break;

    case DB_IPCE_CREATE_CONNECTION:
        cbAdditionalSize = sizeof(pEvent->CreateConnection);
        break;

    case DB_IPCE_DESTROY_CONNECTION:
        cbAdditionalSize = sizeof(pEvent->ConnectionChange);
        break;

    case DB_IPCE_CHANGE_CONNECTION:
        cbAdditionalSize = sizeof(pEvent->ConnectionChange);
        break;

    case DB_IPCE_EXCEPTION_CALLBACK2:
        cbAdditionalSize = sizeof(pEvent->ExceptionCallback2);
        break;

    case DB_IPCE_EXCEPTION_UNWIND:
        cbAdditionalSize = sizeof(pEvent->ExceptionUnwind);
        break;

    case DB_IPCE_CREATE_HANDLE_RESULT:
        cbAdditionalSize = sizeof(pEvent->CreateHandleResult);
        break;

    case DB_IPCE_ENC_REMAP_COMPLETE:
        cbAdditionalSize = sizeof(pEvent->EnCRemapComplete);
        break;

    case DB_IPCE_ENC_ADD_FUNCTION:
        cbAdditionalSize = sizeof(pEvent->EnCUpdate);
        break;

    case DB_IPCE_GET_NGEN_COMPILER_FLAGS_RESULT:
        cbAdditionalSize = sizeof(pEvent->JitDebugInfo);
        break;

    case DB_IPCE_MDA_NOTIFICATION:
        cbAdditionalSize = sizeof(pEvent->MDANotification);
        break;

    case DB_IPCE_GET_GCHANDLE_INFO_RESULT:
        cbAdditionalSize = sizeof(pEvent->GetGCHandleInfoResult);
        break;

    case DB_IPCE_SET_IP:
        cbAdditionalSize = sizeof(pEvent->SetIP);
        break;

    case DB_IPCE_BREAKPOINT_ADD:
        cbAdditionalSize = sizeof(pEvent->BreakpointData);
        break;

    case DB_IPCE_BREAKPOINT_REMOVE:
        cbAdditionalSize = sizeof(pEvent->BreakpointData);
        break;

    case DB_IPCE_STEP_CANCEL:
        cbAdditionalSize = sizeof(pEvent->StepData);
        break;

    case DB_IPCE_STEP:
        cbAdditionalSize = sizeof(pEvent->StepData);
        if (pEvent->StepData.rangeCount)
            cbAdditionalSize += (pEvent->StepData.rangeCount - 1) * sizeof(COR_DEBUG_STEP_RANGE);
        break;

    case DB_IPCE_STEP_OUT:
        cbAdditionalSize = sizeof(pEvent->StepData);
        break;

    case DB_IPCE_GET_BUFFER:
        cbAdditionalSize = sizeof(pEvent->GetBuffer);
        break;

    case DB_IPCE_RELEASE_BUFFER:
        cbAdditionalSize = sizeof(pEvent->ReleaseBuffer);
        break;

    case DB_IPCE_SET_CLASS_LOAD_FLAG:
        cbAdditionalSize = sizeof(pEvent->SetClassLoad);
        break;

    case DB_IPCE_APPLY_CHANGES:
        cbAdditionalSize = sizeof(pEvent->ApplyChanges);
        break;

    case DB_IPCE_SET_NGEN_COMPILER_FLAGS:
        cbAdditionalSize = sizeof(pEvent->JitDebugInfo);
        break;

    case DB_IPCE_IS_TRANSITION_STUB:
        cbAdditionalSize = sizeof(pEvent->IsTransitionStub);
        break;

    case DB_IPCE_IS_TRANSITION_STUB_RESULT:
        cbAdditionalSize = sizeof(pEvent->IsTransitionStubResult);
        break;

    case DB_IPCE_MODIFY_LOGSWITCH:
        cbAdditionalSize = sizeof(pEvent->LogSwitchSettingMessage);
        break;

    case DB_IPCE_ENABLE_LOG_MESSAGES:
        cbAdditionalSize = sizeof(pEvent->LogSwitchSettingMessage);
        break;

    case DB_IPCE_FUNC_EVAL:
        cbAdditionalSize = sizeof(pEvent->FuncEval);
        break;

    case DB_IPCE_SET_REFERENCE:
        cbAdditionalSize = sizeof(pEvent->SetReference);
        break;

    case DB_IPCE_FUNC_EVAL_ABORT:
        cbAdditionalSize = sizeof(pEvent->FuncEvalAbort);
        break;

    case DB_IPCE_FUNC_EVAL_CLEANUP:
        cbAdditionalSize = sizeof(pEvent->FuncEvalCleanup);
        break;

    case DB_IPCE_SET_ALL_DEBUG_STATE:
        cbAdditionalSize = sizeof(pEvent->SetAllDebugState);
        break;

    case DB_IPCE_SET_VALUE_CLASS:
        cbAdditionalSize = sizeof(pEvent->SetValueClass);
        break;

    case DB_IPCE_SET_METHOD_JMC_STATUS:
        cbAdditionalSize = sizeof(pEvent->SetJMCFunctionStatus);
        break;

    case DB_IPCE_GET_METHOD_JMC_STATUS:
        cbAdditionalSize = sizeof(pEvent->SetJMCFunctionStatus);
        break;

    case DB_IPCE_SET_MODULE_JMC_STATUS:
        cbAdditionalSize = sizeof(pEvent->SetJMCFunctionStatus);
        break;

    case DB_IPCE_GET_THREAD_FOR_TASKID:
        cbAdditionalSize = sizeof(pEvent->GetThreadForTaskId);
        break;

    case DB_IPCE_FUNC_EVAL_RUDE_ABORT:
        cbAdditionalSize = sizeof(pEvent->FuncEvalRudeAbort);
        break;

    case DB_IPCE_CREATE_HANDLE:
        cbAdditionalSize = sizeof(pEvent->CreateHandle);
        break;

    case DB_IPCE_DISPOSE_HANDLE:
        cbAdditionalSize = sizeof(pEvent->DisposeHandle);
        break;

    case DB_IPCE_INTERCEPT_EXCEPTION:
        cbAdditionalSize = sizeof(pEvent->InterceptException);
        break;

    case DB_IPCE_GET_GCHANDLE_INFO:
        cbAdditionalSize = sizeof(pEvent->GetGCHandleInfo);
        break;

    case DB_IPCE_CUSTOM_NOTIFICATION:
        cbAdditionalSize = sizeof(pEvent->CustomNotification);
        break;

    default:
        printf("Unknown debugger event type: 0x%x\n", (pEvent->type & DB_IPCE_TYPE_MASK));
        _ASSERTE(!"Unknown debugger event type");
    }

    return cbBaseSize + cbAdditionalSize;
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

#ifdef _DEBUG
// Debug helper which returns the name associated with a MessageType.
const char *DbgTransportSession::MessageName(MessageType eType)
{
    switch (eType)
    {
    case MT_SessionRequest:
        return "SessionRequest";
    case MT_SessionAccept:
        return "SessionAccept";
    case MT_SessionReject:
        return "SessionReject";
    case MT_SessionResync:
        return "SessionResync";
    case MT_SessionClose:
        return "SessionClose";
    case MT_Event:
        return "Event";
    case MT_ReadMemory:
        return "ReadMemory";
    case MT_WriteMemory:
        return "WriteMemory";
    case MT_VirtualUnwind:
        return "VirtualUnwind";
    case MT_GetDCB:
        return "GetDCB";
    case MT_SetDCB:
        return "SetDCB";
    case MT_GetAppDomainCB:
        return "GetAppDomainCB";
    default:
        _ASSERTE(!"Unknown message type");
        return NULL;
    }
}

// Debug logging helper which logs an incoming message of any type (as long as logging for that message
// class is currently enabled).
void DbgTransportSession::DbgTransportLogMessageReceived(MessageHeader *pHeader)
{
    switch (pHeader->m_eType)
    {
    case MT_SessionRequest:
        DbgTransportLog(LC_Session, "Received 'SessionRequest'");
        DBG_TRANSPORT_INC_STAT(ReceivedSessionRequest);
        return;
    case MT_SessionAccept:
        DbgTransportLog(LC_Session,  "Received 'SessionAccept'");
        DBG_TRANSPORT_INC_STAT(ReceivedSessionAccept);
        return;
    case MT_SessionReject:
        DbgTransportLog(LC_Session,  "Received 'SessionReject'");
        DBG_TRANSPORT_INC_STAT(ReceivedSessionReject);
        return;
    case MT_SessionResync:
        DbgTransportLog(LC_Session,  "Received 'SessionResync'");
        DBG_TRANSPORT_INC_STAT(ReceivedSessionResync);
        return;
    case MT_SessionClose:
        DbgTransportLog(LC_Session,  "Received 'SessionClose'");
        DBG_TRANSPORT_INC_STAT(ReceivedSessionClose);
        return;
    case MT_Event:
        DbgTransportLog(LC_Events,  "Received '%s'",
                        IPCENames::GetName((DebuggerIPCEventType)(DWORD)pHeader->TypeSpecificData.Event.m_eType));
        DBG_TRANSPORT_INC_STAT(ReceivedEvent);
        return;
#ifdef RIGHT_SIDE_COMPILE
    case MT_ReadMemory:
        DbgTransportLog(LC_Requests,  "Received 'ReadMemory(0x%08X, %u)' reply",
                        (PBYTE)pHeader->TypeSpecificData.MemoryAccess.m_pbLeftSideBuffer,
                        (DWORD)pHeader->TypeSpecificData.MemoryAccess.m_cbLeftSideBuffer);
        DBG_TRANSPORT_INC_STAT(ReceivedReadMemory);
        return;
    case MT_WriteMemory:
        DbgTransportLog(LC_Requests,  "Received 'WriteMemory(0x%08X, %u)' reply",
                        (PBYTE)pHeader->TypeSpecificData.MemoryAccess.m_pbLeftSideBuffer,
                        (DWORD)pHeader->TypeSpecificData.MemoryAccess.m_cbLeftSideBuffer);
        DBG_TRANSPORT_INC_STAT(ReceivedWriteMemory);
        return;
    case MT_VirtualUnwind:
        DbgTransportLog(LC_Requests,  "Received 'VirtualUnwind' reply");
        DBG_TRANSPORT_INC_STAT(ReceivedVirtualUnwind);
        return;
    case MT_GetDCB:
        DbgTransportLog(LC_Requests,  "Received 'GetDCB' reply");
        DBG_TRANSPORT_INC_STAT(ReceivedGetDCB);
        return;
    case MT_SetDCB:
        DbgTransportLog(LC_Requests,  "Received 'SetDCB' reply");
        DBG_TRANSPORT_INC_STAT(ReceivedSetDCB);
        return;
    case MT_GetAppDomainCB:
        DbgTransportLog(LC_Requests,  "Received 'GetAppDomainCB' reply");
        DBG_TRANSPORT_INC_STAT(ReceivedGetAppDomainCB);
        return;
#else // RIGHT_SIDE_COMPILE
    case MT_ReadMemory:
        DbgTransportLog(LC_Requests,  "Received 'ReadMemory(0x%08X, %u)'",
                        (PBYTE)pHeader->TypeSpecificData.MemoryAccess.m_pbLeftSideBuffer,
                        (DWORD)pHeader->TypeSpecificData.MemoryAccess.m_cbLeftSideBuffer);
        DBG_TRANSPORT_INC_STAT(ReceivedReadMemory);
        return;
    case MT_WriteMemory:
        DbgTransportLog(LC_Requests,  "Received 'WriteMemory(0x%08X, %u)'",
                        (PBYTE)pHeader->TypeSpecificData.MemoryAccess.m_pbLeftSideBuffer,
                        (DWORD)pHeader->TypeSpecificData.MemoryAccess.m_cbLeftSideBuffer);
        DBG_TRANSPORT_INC_STAT(ReceivedWriteMemory);
        return;
    case MT_VirtualUnwind:
        DbgTransportLog(LC_Requests,  "Received 'VirtualUnwind'");
        DBG_TRANSPORT_INC_STAT(ReceivedVirtualUnwind);
        return;
    case MT_GetDCB:
        DbgTransportLog(LC_Requests,  "Received 'GetDCB'");
        DBG_TRANSPORT_INC_STAT(ReceivedGetDCB);
        return;
    case MT_SetDCB:
        DbgTransportLog(LC_Requests,  "Received 'SetDCB'");
        DBG_TRANSPORT_INC_STAT(ReceivedSetDCB);
        return;
    case MT_GetAppDomainCB:
        DbgTransportLog(LC_Requests,  "Received 'GetAppDomainCB'");
        DBG_TRANSPORT_INC_STAT(ReceivedGetAppDomainCB);
        return;
#endif // RIGHT_SIDE_COMPILE
    default:
        _ASSERTE(!"Unknown message type");
        return;
    }
}

static CLRRandom s_faultInjectionRandom;

// Helper method used by the DBG_TRANSPORT_SHOULD_INJECT_FAULT macro.
bool DbgTransportSession::DbgTransportShouldInjectFault(DbgTransportFaultOp eOp, const char *szOpName)
{
    static DWORD s_dwFaultInjection = 0xffffffff;

    // Init the fault injection system if that hasn't already happened.
    if (s_dwFaultInjection == 0xffffffff)
    {
        s_dwFaultInjection = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_DbgTransportFaultInject);

        // Try for repeatable failures here by always initializing the random seed to a fixed value. But use
        // different seeds for the left and right sides or they'll end up in lock step. The
        // DBG_TRANSPORT_FAULT_THIS_SIDE macro is a convenient integer value that differs on each side.
        s_faultInjectionRandom.Init(DBG_TRANSPORT_FAULT_THIS_SIDE);

        // Clamp failure rate to a permissable value.
        if ((s_dwFaultInjection & DBG_TRANSPORT_FAULT_RATE_MASK) > 99)
            s_dwFaultInjection = (s_dwFaultInjection & ~DBG_TRANSPORT_FAULT_RATE_MASK) | 99;
    }

    // Map current session state into the bitmask format used for fault injection control.
    DWORD dwState = 0;
    switch (m_eState)
    {
    case SS_Opening_NC:
    case SS_Opening:
        dwState = FS_Opening;
        break;
    case SS_Resync_NC:
    case SS_Resync:
        dwState = FS_Resync;
        break;
    case SS_Open:
        dwState = FS_Open;
        break;
    case SS_Closed:
        break;
    default:
        _ASSERTE(!"Bad session state");
    }

    if ((s_dwFaultInjection & DBG_TRANSPORT_FAULT_THIS_SIDE) &&
        (s_dwFaultInjection & eOp) &&
        (s_dwFaultInjection & dwState))
    {
        // We're faulting this side, op and state. Roll the dice and see if this particular call should fail.
        DWORD dwChance = s_faultInjectionRandom.Next(100);
        if (dwChance < (s_dwFaultInjection & DBG_TRANSPORT_FAULT_RATE_MASK))
        {
            DbgTransportLog(LC_FaultInject, "Injected fault for %s operation", szOpName);
#if defined(FEATURE_CORESYSTEM)
        // not supported
#else
            WSASetLastError(WSAEFAULT);
#endif // defined(FEATURE_CORESYSTEM)
            return true;
        }
    }

    return false;
}
#endif // _DEBUG

// Lock abstraction code (hides difference in lock implementation between left and right side).
#ifdef RIGHT_SIDE_COMPILE

// On the right side we use a CRITICAL_SECTION.

void DbgTransportLock::Init()
{
    InitializeCriticalSection(&m_sLock);
}

void DbgTransportLock::Destroy()
{
    DeleteCriticalSection(&m_sLock);
}

void DbgTransportLock::Enter()
{
    EnterCriticalSection(&m_sLock);
}

void DbgTransportLock::Leave()
{
    LeaveCriticalSection(&m_sLock);
}
#else // RIGHT_SIDE_COMPILE

// On the left side we use a Crst.

void DbgTransportLock::Init()
{
    m_sLock.Init(CrstDbgTransport, (CrstFlags)(CRST_UNSAFE_ANYMODE | CRST_DEBUGGER_THREAD | CRST_TAKEN_DURING_SHUTDOWN));
}

void DbgTransportLock::Destroy()
{
}

void DbgTransportLock::Enter()
{
    m_sLock.Enter();
}

void DbgTransportLock::Leave()
{
    m_sLock.Leave();
}
#endif // RIGHT_SIDE_COMPILE

#endif // (!defined(RIGHT_SIDE_COMPILE) && defined(FEATURE_DBGIPC_TRANSPORT_VM)) || (defined(RIGHT_SIDE_COMPILE) && defined(FEATURE_DBGIPC_TRANSPORT_DI))
