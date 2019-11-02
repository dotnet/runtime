// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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



    // Update a single field with a value stored in the RS copy of the DCB.
    virtual HRESULT UpdateLeftSideDCBField(void *rsFieldAddr, SIZE_T size);

    // Update the entire RS copy of the debugger control block by reading the LS copy.
    virtual HRESULT UpdateRightSideDCB();

    // Get the pointer to the RS DCB.
    virtual DebuggerIPCControlBlock * GetDCB();



    // Check whether we need to wait for an acknowledgement from the LS after sending an IPC event.
    virtual BOOL NeedToWaitForAck(DebuggerIPCEvent * pEvent);

    // Get a handle to wait on after sending an IPC event to the LS.  The caller should call NeedToWaitForAck()
    virtual HANDLE GetRightSideEventAckHandle();

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

    // write memory to the LS using the data target
    HRESULT SafeWriteBuffer(TargetBuffer tb, const BYTE * pLocalBuffer);

    // read memory from the LS using the data target
    HRESULT SafeReadBuffer(TargetBuffer tb, BYTE * pLocalBuffer);

    // duplicate a remote handle into the local process
    HRESULT DuplicateHandleToLocalProcess(HANDLE * pLocalHandle, RemoteHANDLE * pRemoteHandle);

    // target address of the DCB on the LS
    CORDB_ADDRESS m_pLeftSideDCB;

    // used to signal the to the LS that an event is available
    HANDLE        m_rightSideEventAvailable;

    // used by the LS to signal that the event is read
    HANDLE        m_rightSideEventRead;

    // handle of the debuggee process
    HANDLE        m_hTargetProc;

    // local buffer for the DCB on the RS
    DebuggerIPCControlBlock * m_pDCBBuffer;

    // data target used for cross-process memory reads and writes
    RSExtSmartPtr<ICorDebugMutableDataTarget> m_pMutableDataTarget;
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

    pDCBBuffer = new (nothrow) DebuggerIPCControlBlock;
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
        m_pDCBBuffer->m_rightSideProcessHandle.DuplicateToRemoteProcess(m_hTargetProc, GetCurrentProcess());
    if (!fSuccess)
    {
        return HRESULT_FROM_GetLastError();
    }

    IfFailRet(UpdateLeftSideDCBField(&(m_pDCBBuffer->m_rightSideProcessHandle),
                                     sizeof(m_pDCBBuffer->m_rightSideProcessHandle)));

    // Dup RSEA and RSER into this process if we don't already have them.
    // On Launch, we don't have them yet, but on attach we do.
    IfFailRet(DuplicateHandleToLocalProcess(&m_rightSideEventAvailable,
                                            &m_pDCBBuffer->m_rightSideEventAvailable));
    IfFailRet(DuplicateHandleToLocalProcess(&m_rightSideEventRead,
                                            &m_pDCBBuffer->m_rightSideEventRead));

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
        m_pDCBBuffer->m_rightSideProcessHandle.CloseInRemoteProcess(m_hTargetProc);
        UpdateLeftSideDCBField(&(m_pDCBBuffer->m_rightSideProcessHandle), sizeof(m_pDCBBuffer->m_rightSideProcessHandle));
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

// Update a single field with a value stored in the RS copy of the DCB.
//
// virtual
HRESULT LocalEventChannel::UpdateLeftSideDCBField(void * rsFieldAddr, SIZE_T size)
{
    _ASSERTE(m_pDCBBuffer != NULL);
    _ASSERTE(m_pLeftSideDCB  != NULL);

    BYTE * pbRSFieldAddr = reinterpret_cast<BYTE *>(rsFieldAddr);
    CORDB_ADDRESS lsFieldAddr = m_pLeftSideDCB + (pbRSFieldAddr - reinterpret_cast<BYTE *>(m_pDCBBuffer));
    return SafeWriteBuffer(TargetBuffer(lsFieldAddr, (ULONG)size), const_cast<const BYTE *>(pbRSFieldAddr));
}

// Update the entire RS copy of the debugger control block by reading the LS copy.
//
// virtual
HRESULT LocalEventChannel::UpdateRightSideDCB()
{
    _ASSERTE(m_pDCBBuffer != NULL);
    _ASSERTE(m_pLeftSideDCB  != NULL);

    return SafeReadBuffer(TargetBuffer(m_pLeftSideDCB, sizeof(DebuggerIPCControlBlock)),
                          reinterpret_cast<BYTE *>(m_pDCBBuffer));
}

// Get the pointer to the RS DCB.
//
// virtual
DebuggerIPCControlBlock * LocalEventChannel::GetDCB()
{
    return m_pDCBBuffer;
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
HANDLE LocalEventChannel::GetRightSideEventAckHandle()
{
    return m_rightSideEventRead;
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
    return TargetBuffer(m_pLeftSideDCB + offsetof(DebuggerIPCControlBlock, m_receiveBuffer), (ULONG)size);
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
    return TargetBuffer(m_pLeftSideDCB + offsetof(DebuggerIPCControlBlock, m_sendBuffer), (ULONG)size);
}

//-----------------------------------------------------------------------------
//
// Write memory to the LS using the data target.
//
// Arguments:
//    tb           - target address and size to be written to
//    pLocalBuffer - data to write
//
// Return Value:
//    S_OK if successful
//

HRESULT LocalEventChannel::SafeWriteBuffer(TargetBuffer tb, const BYTE * pLocalBuffer)
{
    return m_pMutableDataTarget->WriteVirtual(tb.pAddress, pLocalBuffer, tb.cbSize);
}

//-----------------------------------------------------------------------------
//
// Read memory from the LS using the data target.
//
// Arguments:
//    tb           - target address and size to be read from
//    pLocalBuffer - buffer for storing the data read from the LS
//
// Return Value:
//    S_OK if the entire specified range is read successful
//

HRESULT LocalEventChannel::SafeReadBuffer(TargetBuffer tb, BYTE * pLocalBuffer)
{
    ULONG32 cbRead;
    HRESULT hr = m_pMutableDataTarget->ReadVirtual(tb.pAddress, pLocalBuffer, tb.cbSize, &cbRead);
    if (FAILED(hr))
    {
        return CORDBG_E_READVIRTUAL_FAILURE;
    }

    if (cbRead != tb.cbSize)
    {
        return HRESULT_FROM_WIN32(ERROR_PARTIAL_COPY);
    }

    return S_OK;
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
