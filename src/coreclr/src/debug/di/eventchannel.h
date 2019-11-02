// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// EventChannel.h
//

//
// This file contains the old-style event channel interface.
//*****************************************************************************


#ifndef _EVENT_CHANNEL_H_
#define _EVENT_CHANNEL_H_

//---------------------------------------------------------------------------------------
//
// This is the abstract base class for the old-style "IPC" event channel.  (Despite the name, these events are
// no longer transmitted in an IPC shared memory block.)  The event channel owns the DebuggerIPCControlBlock.
//
// Assumptions:
//    This class is NOT thread-safe.  Caller is assumed to have taken the appropriate measures for
//    synchronization.
//
// Notes:
//    In Whidbey, both LS-to-RS and RS-to-LS communication are done by IPC shared memory block.  We allocate
//    a DebuggerIPCControlBlock (DCB) on the IPC shared memory block.  The DCB contains both a send buffer
//    and a receive buffer (from the perspective of the LS, e.g. the send buffer is for LS-to-RS communication).
//
//    In the new architecture, LS-to-RS communication is mostly done by raising an exception on the LS and
//    calling code:INativeEventPipeline::WaitForDebugEvent on the RS.  This communication is handled by
//    code:INativeEventPipeline.  RS-to-LS communication is mostly done by calling into the code:IDacDbiInterface,
//    which on Windows is just a structured way to do ReadProcessMemory().
//
//    There are still cases where we are sending IPC events in not-yet-DACized code.  There are two main
//    categories:
//
//    1) There are three types of events which the RS can send to the LS:
//       a) asynchronous: the RS can just send the event and continue
//       b) synchronous, but no reply: the RS must wait for an acknowledgement, but there is no reply
//       c) synchronous, reply required: the RS must wait for an acknowledgement before it can get the reply
//
//       For (c), the RS sends a synchronous IPC event to the LS and wait for a reply.  The reply is returned
//       in the same buffer space used to send the event, i.e. in the receive buffer.
//         - RS: code:CordbRCEventThread::SendIPCEvent
//         - LS: code:DebuggerRCThread::SendIPCReply
//
//    2) In the case where the information from the LS has a variable size (and so we are not sure if it will
//       fit in one event), the RS sends an asynchronous IPC event to the LS and wait for one or more
//       events from the LS.  The events from the LS are actually sent using the native pipeline.  This is
//       somewhat tricky because we need to make sure the event from the native pipeline is passed along to
//       the thread which is waiting for the IPC events from the LS.  (For more information, see how we use
//       code:CordbProcess::m_leftSideEventAvailable and code:CordbProcess::m_leftSideEventRead).  Currently,
//       the only place where we use send IPC events this way is in the inspection code used to check the
//       results from the DAC against the results from the IPC events.
//         - RS: code:Cordb::WaitForIPCEventFromProcess
//         - LS: code:DebuggerRCThread::SendIPCEvent
//
//    In a sense, you can think of the LS and the RS sharing 3 channels: one for debug events (see
//    code:INativeEventPipeline), one for DDI calls (see code:IDacDbiInterface),
//    and one for "IPC" events.  This is the interface for the "IPC" events.
//

class IEventChannel
{
public:

    //
    // Inititalize the event channel.
    //
    // Arguments:
    //    hTargetProc - the handle of the debuggee process
    //
    // Return Value:
    //    S_OK if successful
    //
    // Notes:
    //    For Mac debugging, the handle is not necessary.
    //

    virtual HRESULT Init(HANDLE hTargetProc) = 0;

    //
    // Called when the debugger is detaching.  Depending on the implementation, this may be necessary to
    // make sure the debuggee state is reset in case another debugger attaches to it.
    //
    // Notes:
    //    This is currently a nop on for Mac debugging.
    //

    virtual void Detach() = 0;

    //
    // Delete the event channel and clean up all the resources it owns.  This function can only be called once.
    //

    virtual void Delete() = 0;

    //
    // Update a single field with a value stored in the RS copy of the DCB. We can't update the entire LS DCB
    // because in some cases, the LS and RS are simultaneously initializing the DCB. If we initialize a field on
    // the RS and write back the whole thing, we may overwrite something the LS has initialized in the interim.
    //
    // Arguments:
    //    rsFieldAddr - the address of the field in the RS copy of the DCB that we want to write back to
    //                  the LS DCB. We use this to compute the offset of the field from the beginning of the
    //                  DCB and then add this offset to the starting address of the LS DCB to get the LS
    //                  address of the field we are updating
    //    size        - the size of the field we're updating.
    //
    // Return Value:
    //    S_OK if successful, otherwise whatever failure HR returned by the actual write operation
    //

    virtual HRESULT UpdateLeftSideDCBField(void * rsFieldAddr, SIZE_T size) = 0;

    //
    // Update the entire RS copy of the debugger control block by reading the LS copy. The RS copy is treated as
    // a throw-away temporary buffer, rather than a true cache. That is, we make no assumptions about the
    // validity of the information over time. Thus, before using any of the values, we need to update it. We
    // update everything for simplicity; any perf hit we take by doing this instead of updating the individual
    // fields we want at any given point isn't significant, particularly if we are updating multiple fields.
    //
    // Return Value:
    //    S_OK if successful, otherwise whatever failure HR returned by the actual read operation
    //

    virtual HRESULT UpdateRightSideDCB() = 0;

    //
    // Get the pointer to the RS DCB.  The LS copy isn't updated until UpdateLeftSideDCBField() is called.
    // Note that the DCB is owned by the event channel.
    //
    // Return Value:
    //    Return a pointer to the RS DCB.  The memory is owned by the event channel.
    //

    virtual DebuggerIPCControlBlock * GetDCB() = 0;

    //
    // Check whether we need to wait for an acknowledgement from the LS after sending an IPC event.
    // If so, wait for GetRightSideEventAckHandle().
    //
    // Arguments:
    //    pEvent - the IPC event which has just been sent to the LS
    //
    // Return Value:
    //    TRUE if an acknowledgement is required (see the comment for this class for more information)
    //

    virtual BOOL NeedToWaitForAck(DebuggerIPCEvent * pEvent) = 0;

    //
    // Get a handle to wait on after sending an IPC event to the LS.  The caller should call NeedToWaitForAck()
    // first to see if it is necessary to wait for an acknowledgement.
    //
    // Return Value:
    //    a handle to a Win32 event which will be signaled when the LS acknowledges the receipt of the IPC event
    //
    // Assumptions:
    //    NeedToWaitForAck() returns true after sending an IPC event to the LS
    //

    virtual HANDLE GetRightSideEventAckHandle() = 0;

    //
    // After sending an event to the LS and determining that we need to wait for the LS's acknowledgement,
    // if any failure occurs, the LS may not have reset the Win32 event which is signaled when an event is
    // available on the RS (i.e. what's called the Right-Side-Event-Available (RSEA) event).  This function
    // should be called if any failure occurs to make sure our state is consistent.
    //

    virtual void   ClearEventForLeftSide() = 0;

    //
    // Send an IPC event to the LS.  The caller should call NeedToWaitForAck() to check if it needs to wait
    // for an acknowledgement, and wait on GetRightSideEventAckHandle() if necessary.
    //
    // Arguments:
    //    pEvent    - the IPC event to be sent over to the LS
    //    eventSize - the size of the IPC event; cannot be bigger than CorDBIPC_BUFFER_SIZE
    //
    // Return Value:
    //    S_OK if successful
    //
    // Notes:
    //    This function returns a failure HR for recoverable errors.  It throws on unrecoverable errors.
    //

    virtual HRESULT SendEventToLeftSide(DebuggerIPCEvent * pEvent, SIZE_T eventSize) = 0;

    //
    // Get the reply from the LS for a previously sent IPC event.  The caller must have waited on
    // GetRightSdieEventAckHandle().
    //
    // Arguments:
    //    pReplyEvent - buffer for the replyl event
    //    eventSize   - size of the buffer
    //
    // Return Value:
    //    S_OK if successful
    //

    virtual HRESULT GetReplyFromLeftSide(DebuggerIPCEvent * pReplyEvent, SIZE_T eventSize) = 0;

    //
    // This function and GetEventFromLeftSide() are for the second category of IPC events described in the
    // class header above, i.e. for events which take more than one IPC event to reply.  The event actually
    // doesn't come from the IPC channel.  Instead, it comes from the native pipeline.  We need to save the
    // event from the native pipeline and then wake up the thread which is waiting for this event.  Then the
    // thread can call GetEventFromLeftSide() to receive this event.
    //
    // Arguments:
    //    pEventFromLeftSide - IPC event from the LS
    //
    // Return Value:
    //    S_OK if successful, E_FAIL if an event has already been saved
    //
    // Assumptions:
    //    At any given time there should only be one event saved.  The caller is responsible for the
    //    synchronization.
    //

    virtual HRESULT SaveEventFromLeftSide(DebuggerIPCEvent * pEventFromLeftSide) = 0;

    //
    // See the function header for SaveEventFromLeftSide.
    //
    // Arguments:
    //    pLocalManagedEvent - buffer to be filled with the IPC event from the LS
    //
    // Return Value:
    //    S_OK if successful
    //
    // Assumptions:
    //    At any given time there should only be one event saved.  The caller is responsible for the
    //    synchronization.
    //

    virtual HRESULT GetEventFromLeftSide(DebuggerIPCEvent * pLocalManagedEvent) = 0;
};

//-----------------------------------------------------------------------------
//
// Allocate and return an old-style event channel object for this target platform.
//
// Arguments:
//    pLeftSideDCB       - target address of the DCB on the LS
//    pMutableDataTarget - data target for reading from and writing to the target process's address space
//    dwProcessId        - used for Mac debugging; specifies the target process ID
//    machineInfo        - used for Mac debugging; specifies the machine and the port number of the proxy
//    ppEventChannel     - out parament; returns the newly created event channel
//
// Return Value:
//    S_OK if successful
//

HRESULT NewEventChannelForThisPlatform(CORDB_ADDRESS pLeftSideDCB,
                                       ICorDebugMutableDataTarget * pMutableDataTarget,
                                       const ProcessDescriptor * pProcessDescriptor,
                                       MachineInfo machineInfo,
                                       IEventChannel ** ppEventChannel);

#endif // _EVENT_CHANNEL_H_
