// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// File: RemoteEventChannel.cpp
// 

//
// Implements the old-style event channel between two remote processes.
//*****************************************************************************

#include "stdafx.h"
#include "eventchannel.h"

#include "dbgtransportsession.h"
#include "dbgtransportmanager.h"


//---------------------------------------------------------------------------------------
// Class serves as a connector to win32 native-debugging API.
class RemoteEventChannel : public IEventChannel
{
public:
    RemoteEventChannel(DebuggerIPCControlBlock * pDCBBuffer,
                       DbgTransportTarget *      pProxy, 
                       DbgTransportSession *     pTransport);

    virtual ~RemoteEventChannel() {}

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
    DebuggerIPCControlBlock * m_pDCBBuffer;     // local buffer for the DCB on the RS
    DbgTransportTarget *      m_pProxy;         // connection to the debugger proxy
    DbgTransportSession *     m_pTransport;     // connection to the debuggee process

    // The next two fields are used for storing an IPC event from the native pipeline 
    // for the IPC event channel.
    BYTE m_rgbLeftSideEventBuffer[CorDBIPC_BUFFER_SIZE];
    BOOL m_fLeftSideEventAvailable;
};

// Allocate and return an old-style event channel object for this target platform.
HRESULT NewEventChannelForThisPlatform(CORDB_ADDRESS pLeftSideDCB, 
                                       ICorDebugMutableDataTarget * pMutableDataTarget,
                                       const ProcessDescriptor * pProcessDescriptor,
                                       MachineInfo machineInfo,
                                       IEventChannel ** ppEventChannel)
{
    // @dbgtodo  Mac - Consider moving all of the transport logic to one place.
    // Perhaps add a new function on DbgTransportManager.
    HandleHolder hDummy;
    HRESULT hr = E_FAIL;

    RemoteEventChannel *      pEventChannel = NULL;
    DebuggerIPCControlBlock * pDCBBuffer    = NULL;

    DbgTransportTarget *   pProxy     = g_pDbgTransportTarget;
    DbgTransportSession *  pTransport = NULL;

    hr = pProxy->GetTransportForProcess(pProcessDescriptor, &pTransport, &hDummy);
    if (FAILED(hr))
    {
        goto Label_Exit;
    }

    if (!pTransport->WaitForSessionToOpen(10000))
    {
        hr = CORDBG_E_TIMEOUT;
        goto Label_Exit;
    }

    pDCBBuffer = new (nothrow) DebuggerIPCControlBlock;
    if (pDCBBuffer == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto Label_Exit;
    }

    pEventChannel = new (nothrow) RemoteEventChannel(pDCBBuffer, pProxy, pTransport);
    if (pEventChannel == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto Label_Exit;
    }

    _ASSERTE(SUCCEEDED(hr));
    *ppEventChannel = pEventChannel;

Label_Exit:
    if (FAILED(hr))
    {
        if (pEventChannel != NULL)
        {
            // The IEventChannel has ownership of the proxy and the transport, 
            // so we don't need to clean them up here.
            delete pEventChannel;
        }
        else
        {
            if (pTransport != NULL)
            {
                pProxy->ReleaseTransport(pTransport);
            }
            if (pDCBBuffer != NULL)
            {
                delete pDCBBuffer;
            }
        }
    }
    return hr;
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

RemoteEventChannel::RemoteEventChannel(DebuggerIPCControlBlock * pDCBBuffer,
                                       DbgTransportTarget *      pProxy, 
                                       DbgTransportSession *     pTransport)
{
    m_pDCBBuffer = pDCBBuffer;
    m_pProxy = pProxy;
    m_pTransport = pTransport;
    m_fLeftSideEventAvailable = FALSE;
}

// Inititalize the event channel.
//
// virtual
HRESULT RemoteEventChannel::Init(HANDLE hTargetProc)
{
    return S_OK;
}

// Called when the debugger is detaching.
//
// virtual
void RemoteEventChannel::Detach()
{
    // This is a nop for Mac debugging because we don't use RSEA/RSER.
    return;
}

// Delete the event channel and clean up all the resources it owns.  This function can only be called once.
// 
// virtual
void RemoteEventChannel::Delete()
{
    if (m_pDCBBuffer != NULL)
    {
        delete m_pDCBBuffer;
        m_pDCBBuffer = NULL;
    }

    if (m_pTransport != NULL)
    {
        m_pProxy->ReleaseTransport(m_pTransport);
    }

    delete this;
}

// Update a single field with a value stored in the RS copy of the DCB.
//
// virtual 
HRESULT RemoteEventChannel::UpdateLeftSideDCBField(void * rsFieldAddr, SIZE_T size)
{
    _ASSERTE(m_pDCBBuffer != NULL);

    // Ask the transport to update the LS DCB.
    return m_pTransport->SetDCB(m_pDCBBuffer);
}

// Update the entire RS copy of the debugger control block by reading the LS copy.
//
// virtual 
HRESULT RemoteEventChannel::UpdateRightSideDCB()
{
    _ASSERTE(m_pDCBBuffer != NULL);

    // Ask the transport to read the DCB from the Ls.
    return m_pTransport->GetDCB(m_pDCBBuffer);
}

// Get the pointer to the RS DCB.
//
// virtual 
DebuggerIPCControlBlock * RemoteEventChannel::GetDCB()
{
    return m_pDCBBuffer;
}

// Check whether we need to wait for an acknowledgement from the LS after sending an IPC event.
//
// virtual 
BOOL RemoteEventChannel::NeedToWaitForAck(DebuggerIPCEvent * pEvent)
{
    // There are three cases to consider when sending an event over the transport:
    //
    // 1) asynchronous
    //      - the LS can just send the event and continue
    //
    // 2) synchronous, but no reply
    //      - This is different than Windows.  We don't wait for an acknowledgement.  
    //        Needless to say this is a semantical difference, but none of our code actually expects 
    //        this type of IPC events to be synchronized.
    //
    // 3) synchronous, reply required: 
    //      - This is the only case we need to wait for an acknowledgement in the Mac debugging case.
    return (!pEvent->asyncSend && pEvent->replyRequired);
}

// Get a handle to wait on after sending an IPC event to the LS.  The caller should call NeedToWaitForAck()
//
// virtual 
HANDLE RemoteEventChannel::GetRightSideEventAckHandle()
{
    // Delegate to the transport which does the real work.
    return m_pTransport->GetIPCEventReadyEvent();
}

// Clean up the state if the wait for an acknowledgement is unsuccessful.
//
// virtual 
void RemoteEventChannel::ClearEventForLeftSide()
{
    // This is a nop for Mac debugging because we don't use RSEA/RSER.
    return;
}

// Send an IPC event to the LS.
//
// virtual 
HRESULT RemoteEventChannel::SendEventToLeftSide(DebuggerIPCEvent * pEvent, SIZE_T eventSize)
{
    _ASSERTE(eventSize <= CorDBIPC_BUFFER_SIZE);

    // Delegate to the transport.  The event size is ignored.
    return m_pTransport->SendEvent(pEvent);
}

// Get the reply from the LS for a previously sent IPC event.
//
// virtual 
HRESULT RemoteEventChannel::GetReplyFromLeftSide(DebuggerIPCEvent * pReplyEvent, SIZE_T eventSize)
{
    // Delegate to the transport.
    m_pTransport->GetNextEvent(pReplyEvent, (DWORD)eventSize);
    return S_OK;
}

// Save an IPC event from the LS.  
// Used for transferring an IPC event from the native pipeline to the IPC event channel.
//
// virtual
HRESULT RemoteEventChannel::SaveEventFromLeftSide(DebuggerIPCEvent * pEventFromLeftSide)
{
    if (m_fLeftSideEventAvailable)
    {
        // We should only be saving one event at a time.
        return E_FAIL;
    }
    else
    {
        memcpy(m_rgbLeftSideEventBuffer, reinterpret_cast<BYTE *>(pEventFromLeftSide), CorDBIPC_BUFFER_SIZE);
        m_fLeftSideEventAvailable = TRUE;
        return S_OK;
    }
}

// Get a saved IPC event from the LS.  
// Used for transferring an IPC event from the native pipeline to the IPC event channel.
//
// virtual
HRESULT RemoteEventChannel::GetEventFromLeftSide(DebuggerIPCEvent * pLocalManagedEvent)
{
    if (m_fLeftSideEventAvailable)
    {
        memcpy(reinterpret_cast<BYTE *>(pLocalManagedEvent), m_rgbLeftSideEventBuffer, CorDBIPC_BUFFER_SIZE);
        m_fLeftSideEventAvailable = FALSE;
        return S_OK;
    }
    else
    {
        // We have not saved any event.
        return E_FAIL;
    }
}
