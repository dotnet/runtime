// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: LocalEventChannel.cpp
//

//
// Implements the old-style event channel between two processes on a local Windows machine.
//*****************************************************************************

#include "stdafx.h"
#include "eventchannel.h"


//---------------------------------------------------------------------------------------
//
// This is the implementation of the event channel for the normal case, where both the debugger and the
// debuggee are on the same Windows machine.  See code:IEventChannel for more information.
//

class LocalEventChannel : public IEventChannel
{
public:
    LocalEventChannel(CORDB_ADDRESS pLeftSideDCB,
                      DebuggerIPCControlBlock * pDCBBuffer,
                      ICorDebugMutableDataTarget * pMutableDataTarget);

    // Inititalize the event channel.
    virtual HRESULT Init(HANDLE hTargetProc);

    // Called when the debugger is detaching.
    virtual void Detach();

    // Delete the event channel and clean up all the resources it owns.  This function can only be called once.
    virtual void Delete();

    // Check whether we need to wait for an acknowledgement from the LS after sending an IPC event.
    virtual BOOL NeedToWaitForAck(DebuggerIPCEvent * pEvent);

    // Get a handle to wait on after sending an IPC event to the LS.  The caller should call NeedToWaitForAck()
    virtual WaitEvent *GetRightSideEventAckHandle();

    // Clean up the state if the wait for an acknowledgement is unsuccessful.
    virtual void   ClearEventForLeftSide();

    // Send an IPC event to the LS.
    virtual HRESULT SendEventToLeftSide(DebuggerIPCEvent * pEvent, SIZE_T eventSize);

    // Get the reply from the LS for a previously sent IPC event.
    virtual HRESULT GetReplyFromLeftSide(DebuggerIPCEvent * pReplyEvent, SIZE_T eventSize);

    // Save an IPC event from the LS.
    // Used for transferring an IPC event from the native pipeline to the IPC event channel.
    virtual HRESULT SaveEventFromLeftSide(DebuggerIPCEvent * pEventFromLeftSide);

    // Get a saved IPC event from the LS.
    // Used for transferring an IPC event from the native pipeline to the IPC event channel.
    virtual HRESULT GetEventFromLeftSide(DebuggerIPCEvent * pLocalManagedEvent);

private:
    // Get a target buffer representing the area of the DebuggerIPCControlBlock on the helper thread that
    // holds information received from the LS as the result of an IPC event.
    TargetBuffer RemoteReceiveBuffer(SIZE_T size);

    // Get a target buffer representing the area of the DebuggerIPCControlBlock on the helper thread that
    // holds information sent to the LS with an IPC event.
    TargetBuffer RemoteSendBuffer(SIZE_T size);

    // duplicate a remote handle into the local process
    HRESULT DuplicateHandleToLocalProcess(HANDLE * pLocalHandle, RemoteHANDLE * pRemoteHandle);

    // used to signal the to the LS that an event is available
    HANDLE        m_rightSideEventAvailable;

    // used by the LS to signal that the event is read
    HANDLE        m_rightSideEventRead;
    WaitEvent *m_rightSideEventReadWaitHandle;

    // handle of the debuggee process
    HANDLE        m_hTargetProc;

};

// Allocate and return an old-style event channel object for this target platform.
HRESULT NewEventChannelForThisPlatform(CORDB_ADDRESS pLeftSideDCB,
                                       ICorDebugMutableDataTarget * pMutableDataTarget,
                                       const ProcessDescriptor * pProcessDescriptor,
                                       MachineInfo machineInfo,
                                       IEventChannel ** ppEventChannel)
{
    _ASSERTE(ppEventChannel != NULL);

    LocalEventChannel *       pEventChannel = NULL;
    DebuggerIPCControlBlock * pDCBBuffer    = NULL;

    pDCBBuffer = new (nothrow) DebuggerIPCControlBlock{};
    if (pDCBBuffer == NULL)
    {
        return E_OUTOFMEMORY;
    }

    pEventChannel = new (nothrow) LocalEventChannel(pLeftSideDCB, pDCBBuffer, pMutableDataTarget);
    if (pEventChannel == NULL)
    {
        delete pDCBBuffer;
        return E_OUTOFMEMORY;
    }

    *ppEventChannel = pEventChannel;
    return S_OK;
}

//-----------------------------------------------------------------------------
//
// This is the constructor.
//
// Arguments:
//    pLeftSideDCB       - target address of the DCB on the LS
//    pDCBBuffer         - local buffer for storing the DCB on the RS; the memory is owned by this class
//    pMutableDataTarget - data target for reading from and writing to the target process's address space
//

LocalEventChannel::LocalEventChannel(CORDB_ADDRESS pLeftSideDCB,
                                     DebuggerIPCControlBlock * pDCBBuffer,
                                     ICorDebugMutableDataTarget * pMutableDataTarget)
{
    m_pLeftSideDCB = pLeftSideDCB;
    m_pDCBBuffer   = pDCBBuffer;

    m_rightSideEventAvailable = NULL;
    m_rightSideEventRead      = NULL;
    m_rightSideEventReadWaitHandle = nullptr;

    m_pMutableDataTarget.Assign(pMutableDataTarget);
}

// Inititalize the event channel.
//
// virtual
HRESULT LocalEventChannel::Init(HANDLE hTargetProc)
{
    HRESULT hr = E_FAIL;

    m_hTargetProc = hTargetProc;

    // Duplicate the handle of the RS process (i.e. the debugger) to the LS process's address space.
    BOOL fSuccess =
        m_pDCBBuffer->m_dcb2.m_rightSideProcessHandle.DuplicateToRemoteProcess(m_hTargetProc, GetCurrentProcess());
    if (!fSuccess)
    {
        return HRESULT_FROM_GetLastError();
    }

    IfFailRet(UpdateLeftSideDCBField(&(m_pDCBBuffer->m_dcb2.m_rightSideProcessHandle),
                                     sizeof(m_pDCBBuffer->m_dcb2.m_rightSideProcessHandle)));

    // Dup RSEA and RSER into this process if we don't already have them.
    // On Launch, we don't have them yet, but on attach we do.
    IfFailRet(DuplicateHandleToLocalProcess(&m_rightSideEventAvailable,
                                            &m_pDCBBuffer->m_dcb2.m_rightSideEventAvailable));
    IfFailRet(DuplicateHandleToLocalProcess(&m_rightSideEventRead,
                                            &m_pDCBBuffer->m_dcb2.m_rightSideEventRead));

    m_rightSideEventReadWaitHandle = new (nothrow) WaitEvent(m_rightSideEventRead);
    if (m_rightSideEventReadWaitHandle == nullptr)
    {
        return E_OUTOFMEMORY;
    }
    if (!m_rightSideEventReadWaitHandle->IsValid())
    {
        HRESULT hr = HRESULT_FROM_GetLastError();
        delete m_rightSideEventReadWaitHandle;
        m_rightSideEventReadWaitHandle = nullptr;
        return hr;
    }

    return S_OK;
}

// Called when the debugger is detaching.
//
// virtual
void LocalEventChannel::Detach()
{
    // This averts a race condition wherein we'll detach, then reattach,
    // and find these events in the still-signalled state.
    if (m_rightSideEventAvailable != NULL)
    {
        ResetEvent(m_rightSideEventAvailable);
    }
    if (m_rightSideEventRead != NULL)
    {
        ResetEvent(m_rightSideEventRead);
    }
}

// Delete the event channel and clean up all the resources it owns.  This function can only be called once.
//
// virtual
void LocalEventChannel::Delete()
{
    if (m_hTargetProc != NULL)
    {
        m_pDCBBuffer->m_dcb2.m_rightSideProcessHandle.CloseInRemoteProcess(m_hTargetProc);
        UpdateLeftSideDCBField(&(m_pDCBBuffer->m_dcb2.m_rightSideProcessHandle), sizeof(m_pDCBBuffer->m_dcb2.m_rightSideProcessHandle));
        m_hTargetProc = NULL;
    }

    if (m_rightSideEventAvailable != NULL)
    {
        CloseHandle(m_rightSideEventAvailable);
        m_rightSideEventAvailable = NULL;
    }

    if (m_rightSideEventRead!= NULL)
    {
        CloseHandle(m_rightSideEventRead);
        m_rightSideEventRead = NULL;
    }

    delete m_rightSideEventReadWaitHandle;
    m_rightSideEventReadWaitHandle = nullptr;

    if (m_pDCBBuffer != NULL)
    {
        delete m_pDCBBuffer;
        m_pDCBBuffer = NULL;
    }

    if (m_pMutableDataTarget != NULL)
    {
        m_pMutableDataTarget.Clear();
    }

    delete this;
}

// Check whether we need to wait for an acknowledgement from the LS after sending an IPC event.
//
// virtual
BOOL LocalEventChannel::NeedToWaitForAck(DebuggerIPCEvent * pEvent)
{
    // On Windows, we need to wait for acknowledgement for every synchronous event.
    return !pEvent->asyncSend;
}

// Get a handle to wait on after sending an IPC event to the LS.  The caller should call NeedToWaitForAck()
//
// virtual
WaitEvent *LocalEventChannel::GetRightSideEventAckHandle()
{
    return m_rightSideEventReadWaitHandle;
}

// Clean up the state if the wait for an acknowledgement is unsuccessful.
//
// virtual
void LocalEventChannel::ClearEventForLeftSide()
{
    ResetEvent(m_rightSideEventAvailable);
}

// Send an IPC event to the LS.
//
// virtual
HRESULT LocalEventChannel::SendEventToLeftSide(DebuggerIPCEvent * pEvent, SIZE_T eventSize)
{
    _ASSERTE(eventSize <= CorDBIPC_BUFFER_SIZE);

    HRESULT hr       = E_FAIL;
    BOOL    fSuccess = FALSE;

    // Copy the event into the shared memory segment.
    hr = SafeWriteBuffer(RemoteReceiveBuffer(eventSize), reinterpret_cast<BYTE *>(pEvent));
    if (FAILED(hr))
    {
        return hr;
    }

    // Do some safety-checks for sending an Async-Event.
#if defined(_DEBUG)
    {
        // We can only send 1 event from RS-->LS at a time.
        // For non-async events, this is obviously enforced. (since the events are blocking & serialized)
        // If this is an AsyncSend, then our caller was responsible for making sure it
        // was safe to send.
        // There should be no other IPC event in the pipeline. This, both RSEA & RSER
        // should be non-signaled. check that now.
        // It's ok if these fail - we detect that below.
        int res2 = ::WaitForSingleObject(m_rightSideEventAvailable, 0);
        CONSISTENCY_CHECK_MSGF(res2 != WAIT_OBJECT_0, ("RSEA:%d", res2));

        int res3 = ::WaitForSingleObject(m_rightSideEventRead, 0);
        CONSISTENCY_CHECK_MSGF(res3 != WAIT_OBJECT_0, ("RSER:%d", res3));
    }
#endif // _DEBUG

    // Tell the runtime controller there is an event ready.
    STRESS_LOG0(LF_CORDB, LL_INFO1000, "Set RSEA\n");
    fSuccess = SetEvent(m_rightSideEventAvailable);

    if (!fSuccess)
    {
        ThrowHR(HRESULT_FROM_GetLastError());
    }

    return S_OK;
}

// Get the reply from the LS for a previously sent IPC event.
//
// virtual
HRESULT LocalEventChannel::GetReplyFromLeftSide(DebuggerIPCEvent * pReplyEvent, SIZE_T eventSize)
{
    // Simply read the IPC event reply directly from the receive buffer on the LS.
    return SafeReadBuffer(RemoteReceiveBuffer(eventSize), reinterpret_cast<BYTE *>(pReplyEvent));
}

// Save an IPC event from the LS.
// Used for transferring an IPC event from the native pipeline to the IPC event channel.
//
// virtual
HRESULT LocalEventChannel::SaveEventFromLeftSide(DebuggerIPCEvent * pEventFromLeftSide)
{
    // On Windows, when a thread raises a debug event through the native pipeline, the process is suspended.
    // Thus, the LS IPC event will still be in the send buffer in the debuggee's address space.
    // Since there is no chance the send buffer can be altered, we don't need to save the event.
    // We can simply read from it.
    return S_OK;
}

// Get a saved IPC event from the LS.
// Used for transferring an IPC event from the native pipeline to the IPC event channel.
//
// virtual
HRESULT LocalEventChannel::GetEventFromLeftSide(DebuggerIPCEvent * pLocalManagedEvent)
{
    // See code:LocalEventChannel::SaveEventFromLeftSide.
    // Make sure we are reading form the send buffer, not the receive buffer.
    return SafeReadBuffer(RemoteSendBuffer(CorDBIPC_BUFFER_SIZE), reinterpret_cast<BYTE *>(pLocalManagedEvent));
}

//-----------------------------------------------------------------------------
//
// Get a target buffer representing the area of the DebuggerIPCControlBlock on the helper thread that
// holds information received from the LS as the result of an IPC event.
//
// Arguments:
//    size - size of the receive buffer
//
// Return Value:
//    a TargetBuffer representing the receive buffer on the LS
//

TargetBuffer LocalEventChannel::RemoteReceiveBuffer(SIZE_T size)
{
    return TargetBuffer(m_pLeftSideDCB + offsetof(DebuggerIPCControlBlock_2, m_receiveBuffer), (ULONG)size);
}

//-----------------------------------------------------------------------------
//
// Get a target buffer representing the area of the DebuggerIPCControlBlock on the helper thread that
// holds information sent to the LS with an IPC event.
//
// Arguments:
//    size - size of the send buffer
//
// Return Value:
//    a TargetBuffer representing the send buffer on the LS
//

TargetBuffer LocalEventChannel::RemoteSendBuffer(SIZE_T size)
{
    return TargetBuffer(m_pLeftSideDCB + offsetof(DebuggerIPCControlBlock_2, m_sendBuffer), (ULONG)size);
}

//-----------------------------------------------------------------------------
//
// Duplicate a remote handle into the local process.
//
// Arguments:
//    pLocalHandle  - out parameter; return the duplicated handle
//    pRemoteHandle - remote handle to be duplicated
//
// Return Value:
//    S_OK if successful
//
// Notes:
//    nop if pLocalHandle is already initialized
//

HRESULT LocalEventChannel::DuplicateHandleToLocalProcess(HANDLE * pLocalHandle, RemoteHANDLE * pRemoteHandle)
{
    // Dup RSEA and RSER into this process if we don't already have them.
    // On Launch, we don't have them yet, but on attach we do.
    if (*pLocalHandle == NULL)
    {
        BOOL fSuccess = pRemoteHandle->DuplicateToLocalProcess(m_hTargetProc, pLocalHandle);
        if (!fSuccess)
        {
            return HRESULT_FROM_GetLastError();
        }
    }
    return S_OK;
}
