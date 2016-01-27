// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    machexception.cpp

Abstract:

    Abstraction over Mach messages used during exception handling.



--*/

#include "config.h"
#include "pal/dbgmsg.h"
#include "pal/thread.hpp"
#include "machmessage.h"

#if HAVE_MACH_EXCEPTIONS

// The vast majority of Mach calls we make in this module are critical: we cannot recover from failures of
// these methods (principally because we're handling hardware exceptions in the context of a single dedicated
// handler thread). The following macro encapsulates checking the return code from Mach methods (we always
// name this 'machret' for consistency) and emitting some useful data and aborting the process on failure.
#define MACH_CHECK(_msg) do {                                           \
        if (machret != KERN_SUCCESS)                                    \
        {                                                               \
            char _szError[1024];                                        \
            sprintf(_szError, "%s: %u: %s", __FUNCTION__, __LINE__, _msg); \
            mach_error(_szError, machret);                              \
            abort();                                                    \
        }                                                               \
    } while (false)

// This macro terminates the process with some useful debug info as above, but for the general failure points
// that have nothing to do with Mach.
#define FATAL_ERROR(_msg, ...) do {                                         \
        printf("%s: %u: " _msg "\n", __FUNCTION__, __LINE__, ## __VA_ARGS__); \
        abort();                                                        \
    } while (false)

#ifdef _DEBUG
// Assert macro that doesn't rely on the PAL.
#define MACHMESSAGE_ASSERT(_expr) do {                      \
        if (!(_expr))                                       \
            FATAL_ERROR("ASSERTION FAILURE: %s\n", #_expr); \
    } while (false)
#else // _DEBUG
#define MACHMESSAGE_ASSERT(_expr)
#endif // _DEBUG


// Construct an empty message. Use Receive() to form a message that can be inspected or SendSetThread(),
// ForwardNotification(), ReplyToNotification() or ForwardReply() to construct a message and sent it.
MachMessage::MachMessage()
{
    m_fPortsOwned = false;
    ResetMessage();
}

void MachMessage::InitializeFrom(const MachMessage& source)
{
    m_fPortsOwned = false;
    ResetMessage();

    memcpy(&m_rgMessageBuffer, &source.m_rgMessageBuffer, sizeof(m_rgMessageBuffer));
}

// Listen for the next message on the given port and initialize this class with the contents. The message type
// must match one of the MessageTypes indicated above (or the process will be aborted).
void MachMessage::Receive(mach_port_t hPort)
{
    kern_return_t machret;
    
    // Erase any stale data.
    ResetMessage();

    // Pull the next Mach message into the buffer.
    machret = mach_msg((mach_msg_header_t*)m_rgMessageBuffer,
                       MACH_RCV_MSG | MACH_RCV_LARGE | MACH_RCV_NOTIFY,
                       0,
                       kcbMaxMessageSize,
                       hPort,
                       MACH_MSG_TIMEOUT_NONE,
                       MACH_PORT_NULL);
    MACH_CHECK("mach_msg()");

    // Check it's one of the messages we're expecting.
    switch (m_pMessage->header.msgh_id)
    {
    case SET_THREAD_MESSAGE_ID:
    case EXCEPTION_RAISE_MESSAGE_ID:
    case EXCEPTION_RAISE_STATE_MESSAGE_ID:
    case EXCEPTION_RAISE_STATE_IDENTITY_MESSAGE_ID:
    case EXCEPTION_RAISE_REPLY_MESSAGE_ID:
    case EXCEPTION_RAISE_STATE_REPLY_MESSAGE_ID:
    case EXCEPTION_RAISE_STATE_IDENTITY_REPLY_MESSAGE_ID:
    case EXCEPTION_RAISE_64_MESSAGE_ID:
    case EXCEPTION_RAISE_STATE_64_MESSAGE_ID:
    case EXCEPTION_RAISE_STATE_IDENTITY_64_MESSAGE_ID:
    case EXCEPTION_RAISE_REPLY_64_MESSAGE_ID:
    case EXCEPTION_RAISE_STATE_REPLY_64_MESSAGE_ID:
    case EXCEPTION_RAISE_STATE_IDENTITY_REPLY_64_MESSAGE_ID:
        break;
    default:
        FATAL_ERROR("Unsupported message type: %u", m_pMessage->header.msgh_id);
    }
    
    m_fPortsOwned = true;
}

// Indicates whether the message is a request to set the context of a thread.
bool MachMessage::IsSetThreadRequest()
{
    return m_pMessage->header.msgh_id == SET_THREAD_MESSAGE_ID;
}

// Indicates whether the message is a notification of an exception.
bool MachMessage::IsExceptionNotification()
{
    switch (m_pMessage->header.msgh_id)
    {
    case EXCEPTION_RAISE_MESSAGE_ID:
    case EXCEPTION_RAISE_STATE_MESSAGE_ID:
    case EXCEPTION_RAISE_STATE_IDENTITY_MESSAGE_ID:
    case EXCEPTION_RAISE_64_MESSAGE_ID:
    case EXCEPTION_RAISE_STATE_64_MESSAGE_ID:
    case EXCEPTION_RAISE_STATE_IDENTITY_64_MESSAGE_ID:
        return true;
    default:
        return false;
    }
}

// Indicates whether the message is a reply to a notification of an exception.
bool MachMessage::IsExceptionReply()
{
    switch (m_pMessage->header.msgh_id)
    {
    case EXCEPTION_RAISE_REPLY_MESSAGE_ID:
    case EXCEPTION_RAISE_STATE_REPLY_MESSAGE_ID:
    case EXCEPTION_RAISE_STATE_IDENTITY_REPLY_MESSAGE_ID:
    case EXCEPTION_RAISE_REPLY_64_MESSAGE_ID:
    case EXCEPTION_RAISE_STATE_REPLY_64_MESSAGE_ID:
    case EXCEPTION_RAISE_STATE_IDENTITY_REPLY_64_MESSAGE_ID:
        return true;
    default:
        return false;
    }
}

// Returns the type code for a received message.
MachMessage::MessageType MachMessage::GetMessageType()
{
    return (MessageType)m_pMessage->header.msgh_id;
}

// Returns a textual form of the type of a received message. Useful for logging.
const char *MachMessage::GetMessageTypeName()
{
    switch (GetMessageType())
    {
    case SET_THREAD_MESSAGE_ID:
        return "SET_THREAD";
    case EXCEPTION_RAISE_MESSAGE_ID:
        return "EXCEPTION_RAISE";
    case EXCEPTION_RAISE_REPLY_MESSAGE_ID:
        return "EXCEPTION_RAISE_REPLY";
    case EXCEPTION_RAISE_STATE_MESSAGE_ID:
        return "EXCEPTION_RAISE_STATE";
    case EXCEPTION_RAISE_STATE_REPLY_MESSAGE_ID:
        return "EXCEPTION_RAISE_STATE_REPLY";
    case EXCEPTION_RAISE_STATE_IDENTITY_MESSAGE_ID:
        return "EXCEPTION_RAISE_STATE_IDENTITY";
    case EXCEPTION_RAISE_STATE_IDENTITY_REPLY_MESSAGE_ID:
        return "EXCEPTION_RAISE_STATE_IDENTITY_REPLY";
    case EXCEPTION_RAISE_64_MESSAGE_ID:
        return "EXCEPTION_RAISE_64";
    case EXCEPTION_RAISE_REPLY_64_MESSAGE_ID:
        return "EXCEPTION_RAISE_REPLY_64";
    case EXCEPTION_RAISE_STATE_64_MESSAGE_ID:
        return "EXCEPTION_RAISE_STATE_64";
    case EXCEPTION_RAISE_STATE_REPLY_64_MESSAGE_ID:
        return "EXCEPTION_RAISE_STATE_REPLY_64";
    case EXCEPTION_RAISE_STATE_IDENTITY_64_MESSAGE_ID:
        return "EXCEPTION_RAISE_STATE_IDENTITY_64";
    case EXCEPTION_RAISE_STATE_IDENTITY_REPLY_64_MESSAGE_ID:
        return "EXCEPTION_RAISE_STATE_IDENTITY_REPLY_64";
    case NOTIFY_SEND_ONCE_MESSAGE_ID:
        return "NOTIFY_SEND_ONCE";
    default:
        return "<unknown message type>";
    }
}

// Returns the destination port (i.e. the port we listened on to receive this message).
mach_port_t MachMessage::GetLocalPort()
{
    return m_pMessage->header.msgh_local_port;
}

// Returns the source port (the port sending the message) unless no reply is expected, in which case
// MACH_PORT_NULL is returned instead.
mach_port_t MachMessage::GetRemotePort()
{
    return m_pMessage->header.msgh_remote_port;
}

// Do the work of getting ports from the message.
//  * fCalculate -- calculate the thread port if the message did not contain it.
//  * fValidate  -- failfast if the message was not one expected to have a (calculable) thread port.
void MachMessage::GetPorts(bool fCalculate, bool fValidThread)
{
    switch (m_pMessage->header.msgh_id)
    {
    case SET_THREAD_MESSAGE_ID:
        m_hThread = m_pMessage->data.set_thread.thread;
        break;
    
    case EXCEPTION_RAISE_MESSAGE_ID:
        m_hThread = m_pMessage->data.raise.thread_port.name;
        m_hTask = m_pMessage->data.raise.task_port.name;
        break;

    case EXCEPTION_RAISE_64_MESSAGE_ID:
        m_hThread = m_pMessage->data.raise_64.thread_port.name;
        m_hTask = m_pMessage->data.raise_64.task_port.name;
        break;

    case EXCEPTION_RAISE_STATE_MESSAGE_ID:
        if (fCalculate && m_hThread == MACH_PORT_NULL)
        {
            // This is a tricky case since the message itself doesn't contain the target thread.
            m_hThread = GetThreadFromState(m_pMessage->data.raise_state.flavor,
                                           m_pMessage->data.raise_state.old_state);
        }
        break;

    case EXCEPTION_RAISE_STATE_64_MESSAGE_ID:
        if (fCalculate && m_hThread == MACH_PORT_NULL)
        {
            // This is a tricky case since the message itself doesn't contain the target thread.
            m_hThread = GetThreadFromState(m_pMessage->data.raise_state_64.flavor,
                                           m_pMessage->data.raise_state_64.old_state);
        }
        break;

    case EXCEPTION_RAISE_STATE_IDENTITY_MESSAGE_ID:
        m_hThread = m_pMessage->data.raise_state_identity.thread_port.name;
        m_hTask = m_pMessage->data.raise_state_identity.task_port.name;
        break;

    case EXCEPTION_RAISE_STATE_IDENTITY_64_MESSAGE_ID:
        m_hThread = m_pMessage->data.raise_state_identity_64.thread_port.name;
        m_hTask = m_pMessage->data.raise_state_identity_64.task_port.name;
        break;
        
    default:
        if (fValidThread)
        {
            FATAL_ERROR("Can only get thread from notification message.");
        }
        break;
    }
}

// Get the properties of a set thread request. Fills in the provided context structure with the context from
// the message and returns the target thread to which the context should be applied.
thread_act_t MachMessage::GetThreadContext(CONTEXT *pContext)
{
    if (m_pMessage->header.msgh_id != SET_THREAD_MESSAGE_ID)
        FATAL_ERROR("Unhandled message type for GetThreadContext(): %u", m_pMessage->header.msgh_id);

    memcpy(pContext, &m_pMessage->data.set_thread.new_context, sizeof(CONTEXT));
    m_hThread = m_pMessage->data.set_thread.thread;
    return m_hThread;
}

// Get the target thread for an exception notification message.
thread_act_t MachMessage::GetThread()
{
    GetPorts(true /* fCalculate */, true /* fValidThread */);
    return m_hThread;
}

// Get the exception type for an exception notification message.
exception_type_t MachMessage::GetException()
{
    switch (m_pMessage->header.msgh_id)
    {
    case EXCEPTION_RAISE_MESSAGE_ID:
        return m_pMessage->data.raise.exception;

    case EXCEPTION_RAISE_64_MESSAGE_ID:
        return m_pMessage->data.raise_64.exception;

    case EXCEPTION_RAISE_STATE_MESSAGE_ID:
        return m_pMessage->data.raise_state.exception;

    case EXCEPTION_RAISE_STATE_64_MESSAGE_ID:
        return m_pMessage->data.raise_state_64.exception;

    case EXCEPTION_RAISE_STATE_IDENTITY_MESSAGE_ID:
        return m_pMessage->data.raise_state_identity.exception;

    case EXCEPTION_RAISE_STATE_IDENTITY_64_MESSAGE_ID:
        return m_pMessage->data.raise_state_identity_64.exception;

    default:
        FATAL_ERROR("Can only get exception from notification message.");
    }
}

// Get the count of sub-codes for an exception notification message.
int MachMessage::GetExceptionCodeCount()
{
    switch (m_pMessage->header.msgh_id)
    {
    case EXCEPTION_RAISE_MESSAGE_ID:
        return m_pMessage->data.raise.code_count;

    case EXCEPTION_RAISE_64_MESSAGE_ID:
        return m_pMessage->data.raise_64.code_count;

    case EXCEPTION_RAISE_STATE_MESSAGE_ID:
        return m_pMessage->data.raise_state.code_count;

    case EXCEPTION_RAISE_STATE_64_MESSAGE_ID:
        return m_pMessage->data.raise_state_64.code_count;

    case EXCEPTION_RAISE_STATE_IDENTITY_MESSAGE_ID:
        return m_pMessage->data.raise_state_identity.code_count;

    case EXCEPTION_RAISE_STATE_IDENTITY_64_MESSAGE_ID:
        return m_pMessage->data.raise_state_identity_64.code_count;

    default:
        FATAL_ERROR("Can only get exception code count from notification message.");
    }
}

// Get the exception sub-code at the specified zero-based index for an exception notification message.
MACH_EH_TYPE(exception_data_type_t) MachMessage::GetExceptionCode(int iIndex)
{
    if (iIndex < 0 || iIndex >= GetExceptionCodeCount())
    {
        FATAL_ERROR("GetExceptionCode() index out of range.");
    }

    switch (m_pMessage->header.msgh_id)
    {
    case EXCEPTION_RAISE_MESSAGE_ID:
        return (int)m_pMessage->data.raise.code[iIndex];

    case EXCEPTION_RAISE_64_MESSAGE_ID:
        return m_pMessage->data.raise_64.code[iIndex];

    case EXCEPTION_RAISE_STATE_MESSAGE_ID:
        return (int)m_pMessage->data.raise_state.code[iIndex];

    case EXCEPTION_RAISE_STATE_64_MESSAGE_ID:
        return m_pMessage->data.raise_state_64.code[iIndex];

    case EXCEPTION_RAISE_STATE_IDENTITY_MESSAGE_ID:
        return (int)m_pMessage->data.raise_state_identity.code[iIndex];

    case EXCEPTION_RAISE_STATE_IDENTITY_64_MESSAGE_ID:
        return m_pMessage->data.raise_state_identity_64.code[iIndex];

    default:
        FATAL_ERROR("Can only get exception code from notification message.");
    }
}

// Fetch the thread state flavor from a notification or reply message (return THREAD_STATE_NONE for the
// messages that don't contain a thread state).
thread_state_flavor_t MachMessage::GetThreadStateFlavor()
{
    switch (m_pMessage->header.msgh_id)
    {
    case EXCEPTION_RAISE_MESSAGE_ID:
    case EXCEPTION_RAISE_REPLY_MESSAGE_ID:
    case EXCEPTION_RAISE_64_MESSAGE_ID:
    case EXCEPTION_RAISE_REPLY_64_MESSAGE_ID:
        return THREAD_STATE_NONE;

    case EXCEPTION_RAISE_STATE_MESSAGE_ID:
        return m_pMessage->data.raise_state.flavor;

    case EXCEPTION_RAISE_STATE_64_MESSAGE_ID:
        return m_pMessage->data.raise_state_64.flavor;

    case EXCEPTION_RAISE_STATE_IDENTITY_MESSAGE_ID:
        return m_pMessage->data.raise_state_identity.flavor;

    case EXCEPTION_RAISE_STATE_IDENTITY_64_MESSAGE_ID:
        return m_pMessage->data.raise_state_identity_64.flavor;

    case EXCEPTION_RAISE_STATE_REPLY_MESSAGE_ID:
        return m_pMessage->data.raise_state_reply.flavor;

    case EXCEPTION_RAISE_STATE_REPLY_64_MESSAGE_ID:
        return m_pMessage->data.raise_state_reply_64.flavor;

    case EXCEPTION_RAISE_STATE_IDENTITY_REPLY_MESSAGE_ID:
        return m_pMessage->data.raise_state_identity_reply.flavor;

    case EXCEPTION_RAISE_STATE_IDENTITY_REPLY_64_MESSAGE_ID:
        return m_pMessage->data.raise_state_identity_reply_64.flavor;

    default:
        FATAL_ERROR("Unsupported message type: %u", m_pMessage->header.msgh_id);
    }
}

// Get the thread state with the given flavor from the exception or exception reply message. If the message
// doesn't contain a thread state or the flavor of the state in the message doesn't match, the state will be
// fetched directly from the target thread instead (which can be computed implicitly for exception messages or
// passed explicitly for reply messages).
size_t MachMessage::GetThreadState(thread_state_flavor_t eFlavor, thread_state_t pState, thread_act_t hThread)
{
    kern_return_t machret;
    size_t cbState;

    switch (m_pMessage->header.msgh_id)
    {
    case EXCEPTION_RAISE_MESSAGE_ID:
    case EXCEPTION_RAISE_REPLY_MESSAGE_ID:
    case EXCEPTION_RAISE_64_MESSAGE_ID:
    case EXCEPTION_RAISE_REPLY_64_MESSAGE_ID:
        // No state in the message, fall through to get it directly from the thread.
        break;

    case EXCEPTION_RAISE_STATE_MESSAGE_ID:
    {
        // There's a state in the message, but we need to check that the flavor matches what the caller's
        // after (if not we'll fall through and get the correct flavor below).
        if (m_pMessage->data.raise_state.flavor == eFlavor)
        {
            cbState = m_pMessage->data.raise_state.old_state_count * sizeof(natural_t);
            memcpy(pState,
                   m_pMessage->data.raise_state.old_state,
                   cbState);
            return cbState;
        }
        break;
    }

    case EXCEPTION_RAISE_STATE_64_MESSAGE_ID:
    {
        // There's a state in the message, but we need to check that the flavor matches what the caller's
        // after (if not we'll fall through and get the correct flavor below).
        if (m_pMessage->data.raise_state_64.flavor == eFlavor)
        {
            cbState = m_pMessage->data.raise_state_64.old_state_count * sizeof(natural_t);
            memcpy(pState,
                   m_pMessage->data.raise_state_64.old_state,
                   cbState);
            return cbState;
        }
        break;
    }

    case EXCEPTION_RAISE_STATE_IDENTITY_MESSAGE_ID:
    {
        // There's a state in the message, but we need to check that the flavor matches what the caller's
        // after (if not we'll fall through and get the correct flavor below).
        if (m_pMessage->data.raise_state_identity.flavor == eFlavor)
        {
            cbState = m_pMessage->data.raise_state_identity.old_state_count * sizeof(natural_t);
            memcpy(pState,
                   m_pMessage->data.raise_state_identity.old_state,
                   cbState);
            return cbState;
        }
        break;
    }

    case EXCEPTION_RAISE_STATE_IDENTITY_64_MESSAGE_ID:
    {
        // There's a state in the message, but we need to check that the flavor matches what the caller's
        // after (if not we'll fall through and get the correct flavor below).
        if (m_pMessage->data.raise_state_identity_64.flavor == eFlavor)
        {
            cbState = m_pMessage->data.raise_state_identity_64.old_state_count * sizeof(natural_t);
            memcpy(pState,
                   m_pMessage->data.raise_state_identity_64.old_state,
                   cbState);
            return cbState;
        }
        break;
    }

    case EXCEPTION_RAISE_STATE_REPLY_MESSAGE_ID:
    {
        // There's a state in the message, but we need to check that the flavor matches what the caller's
        // after (if not we'll fall through and get the correct flavor below).
        if (m_pMessage->data.raise_state_reply.flavor == eFlavor)
        {
            cbState = m_pMessage->data.raise_state_reply.new_state_count * sizeof(natural_t);
            memcpy(pState,
                   m_pMessage->data.raise_state_reply.new_state,
                   cbState);
            return cbState;
        }
        break;
    }

    case EXCEPTION_RAISE_STATE_REPLY_64_MESSAGE_ID:
    {
        // There's a state in the message, but we need to check that the flavor matches what the caller's
        // after (if not we'll fall through and get the correct flavor below).
        if (m_pMessage->data.raise_state_reply_64.flavor == eFlavor)
        {
            cbState = m_pMessage->data.raise_state_reply_64.new_state_count * sizeof(natural_t);
            memcpy(pState,
                   m_pMessage->data.raise_state_reply_64.new_state,
                   cbState);
            return cbState;
        }
        break;
    }

    case EXCEPTION_RAISE_STATE_IDENTITY_REPLY_MESSAGE_ID:
    {
        // There's a state in the message, but we need to check that the flavor matches what the caller's
        // after (if not we'll fall through and get the correct flavor below).
        if (m_pMessage->data.raise_state_identity_reply.flavor == eFlavor)
        {
            cbState = m_pMessage->data.raise_state_identity_reply.new_state_count * sizeof(natural_t);
            memcpy(pState,
                   m_pMessage->data.raise_state_identity_reply.new_state,
                   cbState);
            return cbState;
        }
        break;
    }

    case EXCEPTION_RAISE_STATE_IDENTITY_REPLY_64_MESSAGE_ID:
    {
        // There's a state in the message, but we need to check that the flavor matches what the caller's
        // after (if not we'll fall through and get the correct flavor below).
        if (m_pMessage->data.raise_state_identity_reply_64.flavor == eFlavor)
        {
            cbState = m_pMessage->data.raise_state_identity_reply_64.new_state_count * sizeof(natural_t);
            memcpy(pState,
                   m_pMessage->data.raise_state_identity_reply_64.new_state,
                   cbState);
            return cbState;
        }
        break;
    }

    default:
        FATAL_ERROR("Unsupported message type for requesting thread state.");
    }

    // No state in the message or the flavor didn't match. Get the requested flavor of state directly from the
    // thread instead.
    mach_msg_type_number_t iStateCount = THREAD_STATE_MAX;
    machret = thread_get_state(hThread ? hThread : GetThread(), eFlavor, (thread_state_t)pState, &iStateCount);
    MACH_CHECK("thread_get_state()");

    return iStateCount * sizeof(natural_t);
}

// Initialize and send a request to set the register context of a particular thread.
void MachMessage::SendSetThread(mach_port_t hServerPort, thread_act_t hThread, CONTEXT *pContext)
{
    kern_return_t machret;

    // Set the message type.
    m_pMessage->header.msgh_id = SET_THREAD_MESSAGE_ID;

    // Initialize the fields that don't need any further input (this depends on the message type having been
    // set above).
    InitFixedFields();

    // Initialize type-specific fields.
    m_pMessage->data.set_thread.thread = hThread;
    memcpy(&m_pMessage->data.set_thread.new_context, pContext, sizeof(CONTEXT));

    // Initialize header fields.
    m_pMessage->header.msgh_bits = MACH_MSGH_BITS(MACH_MSG_TYPE_COPY_SEND, 0);
    m_pMessage->header.msgh_remote_port = hServerPort;      // Destination port
    m_pMessage->header.msgh_local_port = MACH_PORT_NULL;    // We expect no reply

    // Set the message header size field based on the contents of the message (call this function after all
    // other fields have been initialized).
    InitMessageSize();

    // Send the formatted message.
    machret = mach_msg((mach_msg_header_t*)m_pMessage,
                       MACH_SEND_MSG | MACH_MSG_OPTION_NONE,
                       m_pMessage->header.msgh_size,
                       0,
                       MACH_PORT_NULL,
                       MACH_MSG_TIMEOUT_NONE,
                       MACH_PORT_NULL);
    MACH_CHECK("mach_msg()");

    // Erase any stale data. (This may not finish executing; nothing is needed to be freed here.)
    ResetMessage();
}

// Initialize the message to represent a forwarded version of the given
// exception notification message and send that message to the chain-back handler previously registered for
// the exception type being notified. The new message takes account of the fact that the target handler may
// not have requested the same notification behavior or flavor as our handler. A new Mach port is created to
// receive the reply, and this port is returned to the caller. Clean up the message afterwards.
void MachMessage::ForwardNotification(CorUnix::MachExceptionHandler *pHandler, MachMessage *pNotification)
{
    kern_return_t machret;

    // Set the message type.
    m_pMessage->header.msgh_id = MapBehaviorToNotificationType(pHandler->m_behavior);

    // Initialize the fields that don't need any further input (this depends on the message type having been
    // set above).
    InitFixedFields();

    // Copy data from the incoming message. Use the getter and setter abstractions to simplify the act that
    // the two messages may be in different formats (e.g. RAISE vs RAISE_STATE). We silently drop data that is
    // not needed in the outgoing message and synthesize any required data that is not present in the incoming
    // message.
    SetThread(pNotification->GetThread());
    SetException(pNotification->GetException());

    int cCodes = pNotification->GetExceptionCodeCount();
    SetExceptionCodeCount(cCodes);
    for (int i = 0; i < cCodes; i++)
        SetExceptionCode(i, pNotification->GetExceptionCode(i));

    NONPAL_TRACE("ForwardNotification: handler thread flavor %04x\n", pHandler->m_flavor);

    // Don't bother fetching thread state unless the destination actually requires it.
    if (pHandler->m_flavor != THREAD_STATE_NONE)
    {
        thread_state_data_t sThreadState;
        size_t cbState = pNotification->GetThreadState(pHandler->m_flavor, (thread_state_t)&sThreadState);
        SetThreadState(pHandler->m_flavor, (thread_state_t)&sThreadState, cbState);
    }

    // Initialize header fields.
    m_pMessage->header.msgh_bits = MACH_MSGH_BITS(MACH_MSG_TYPE_COPY_SEND, MACH_MSG_TYPE_MAKE_SEND_ONCE);
    m_pMessage->header.msgh_remote_port = pHandler->m_handler;              // Forward to here
    m_pMessage->header.msgh_local_port = pNotification->GetLocalPort();     // The reply will come here

    // Set the message header size field based on the contents of the message (call this function after all
    // other fields have been initialized).
    InitMessageSize();

    // Send the formatted message.
    machret = mach_msg((mach_msg_header_t*)m_pMessage,
                       MACH_SEND_MSG | MACH_MSG_OPTION_NONE,
                       m_pMessage->header.msgh_size,
                       0,
                       MACH_PORT_NULL,
                       MACH_MSG_TIMEOUT_NONE,
                       MACH_PORT_NULL);
    MACH_CHECK("mach_msg()");

    // Erase any stale data.
    ResetMessage();
}

// Initialize the message to represent a reply to the given exception
// notification and send that reply back to the original sender of the notification. This is used when our
// handler handles the exception rather than forwarding it to a chain-back handler.
// Clean up the message afterwards.
void MachMessage::ReplyToNotification(MachMessage *pNotification, kern_return_t eResult)
{
    kern_return_t machret;

    // Set the message type.
    m_pMessage->header.msgh_id = MapNotificationToReplyType(pNotification->m_pMessage->header.msgh_id);

    // Initialize the fields that don't need any further input (this depends on the message type having been
    // set above).
    InitFixedFields();

    SetReturnCode(eResult);

    thread_state_flavor_t eNotificationFlavor = pNotification->GetThreadStateFlavor();
    if (eNotificationFlavor != THREAD_STATE_NONE)
    {
        // If the reply requires a thread state be sure to get it from the thread directly rather than the
        // notification message (handling the exception is likely to have changed the thread state).
        thread_state_data_t sThreadState;
        mach_msg_type_number_t iStateCount = THREAD_STATE_MAX;
        machret = thread_get_state(pNotification->GetThread(),
                                   eNotificationFlavor,
                                   (thread_state_t)&sThreadState,
                                   &iStateCount);
        MACH_CHECK("thread_get_state()");

        SetThreadState(eNotificationFlavor, (thread_state_t)&sThreadState, iStateCount * sizeof(natural_t));
    }

    // Initialize header fields.
    m_pMessage->header.msgh_bits = MACH_MSGH_BITS(MACH_MSG_TYPE_MOVE_SEND_ONCE, 0);
    m_pMessage->header.msgh_remote_port = pNotification->GetRemotePort(); // Reply goes back to sender
    m_pMessage->header.msgh_local_port = 0;                               // No reply to this expected

    // Set the message header size field based on the contents of the message (call this function after all
    // other fields have been initialized).
    InitMessageSize();

    // Send the formatted message.
    machret = mach_msg((mach_msg_header_t*)m_pMessage,
                       MACH_SEND_MSG | MACH_MSG_OPTION_NONE,
                       m_pMessage->header.msgh_size,
                       0,
                       MACH_PORT_NULL,
                       MACH_MSG_TIMEOUT_NONE,
                       MACH_PORT_NULL);
    MACH_CHECK("mach_msg()");

    // Erase any stale data.
    ResetMessage();
}

// Re-initializes this data structure (to the same state as default construction, containing no message).
void MachMessage::ResetMessage()
{
    // Clean up ports if we own them.
    if (m_fPortsOwned)
    {
        kern_return_t machret;
        
        GetPorts(false /* fCalculate */, false /* fValidThread */);
        if (m_hThread != MACH_PORT_NULL)
        {
            machret = mach_port_deallocate(mach_task_self(), m_hThread);
            MACH_CHECK("mach_port_deallocate(m_hThread)");
        }
        
        if (m_hTask != MACH_PORT_NULL)
        {
            machret = mach_port_deallocate(mach_task_self(), m_hTask);
            MACH_CHECK("mach_port_deallocate(m_hTask)");
        }
    }

#ifdef _DEBUG
    memset(this, 0xcc, sizeof(*this));
#endif

    m_pMessage = (mach_message_t*)m_rgMessageBuffer;
    m_hThread = MACH_PORT_NULL;
    m_hTask = MACH_PORT_NULL;
    m_fPortsOwned = false;
}

// Initialize those fields of a message that are invariant. This method expects that the msgh_id field has
// been filled in prior to the call so it can determine which non-header fields to initialize.
void MachMessage::InitFixedFields()
{
    switch (m_pMessage->header.msgh_id)
    {
    case SET_THREAD_MESSAGE_ID:
        break;

    case EXCEPTION_RAISE_MESSAGE_ID:
        m_pMessage->data.raise.msgh_body.msgh_descriptor_count = 0;
        m_pMessage->data.raise.ndr = NDR_record;
        m_pMessage->data.raise.task_port.name = mach_task_self();
        m_pMessage->data.raise.task_port.pad1 = 0;
        m_pMessage->data.raise.task_port.pad2 = 0;
        m_pMessage->data.raise.task_port.disposition = MACH_MSG_TYPE_COPY_SEND;
        m_pMessage->data.raise.task_port.type = MACH_MSG_PORT_DESCRIPTOR;
        m_hTask = mach_task_self();
        break;

    case EXCEPTION_RAISE_64_MESSAGE_ID:
        m_pMessage->data.raise_64.msgh_body.msgh_descriptor_count = 0;
        m_pMessage->data.raise_64.ndr = NDR_record;
        m_pMessage->data.raise_64.task_port.name = mach_task_self();
        m_pMessage->data.raise_64.task_port.pad1 = 0;
        m_pMessage->data.raise_64.task_port.pad2 = 0;
        m_pMessage->data.raise_64.task_port.disposition = MACH_MSG_TYPE_COPY_SEND;
        m_pMessage->data.raise_64.task_port.type = MACH_MSG_PORT_DESCRIPTOR;
        m_hTask = mach_task_self();
        break;

    case EXCEPTION_RAISE_STATE_MESSAGE_ID:
        m_pMessage->data.raise_state.ndr = NDR_record;
        break;

    case EXCEPTION_RAISE_STATE_64_MESSAGE_ID:
        m_pMessage->data.raise_state_64.ndr = NDR_record;
        break;

    case EXCEPTION_RAISE_STATE_IDENTITY_MESSAGE_ID:
        m_pMessage->data.raise_state_identity.msgh_body.msgh_descriptor_count = 0;
        m_pMessage->data.raise_state_identity.ndr = NDR_record;
        m_pMessage->data.raise_state_identity.task_port.name = mach_task_self();
        m_pMessage->data.raise_state_identity.task_port.pad1 = 0;
        m_pMessage->data.raise_state_identity.task_port.pad2 = 0;
        m_pMessage->data.raise_state_identity.task_port.disposition = MACH_MSG_TYPE_COPY_SEND;
        m_pMessage->data.raise_state_identity.task_port.type = MACH_MSG_PORT_DESCRIPTOR;
        m_hTask = mach_task_self();
        break;

    case EXCEPTION_RAISE_STATE_IDENTITY_64_MESSAGE_ID:
        m_pMessage->data.raise_state_identity_64.msgh_body.msgh_descriptor_count = 0;
        m_pMessage->data.raise_state_identity_64.ndr = NDR_record;
        m_pMessage->data.raise_state_identity_64.task_port.name = mach_task_self();
        m_pMessage->data.raise_state_identity_64.task_port.pad1 = 0;
        m_pMessage->data.raise_state_identity_64.task_port.pad2 = 0;
        m_pMessage->data.raise_state_identity_64.task_port.disposition = MACH_MSG_TYPE_COPY_SEND;
        m_pMessage->data.raise_state_identity_64.task_port.type = MACH_MSG_PORT_DESCRIPTOR;
        m_hTask = mach_task_self();
        break;

    case EXCEPTION_RAISE_REPLY_MESSAGE_ID:
        m_pMessage->data.raise_reply.ndr = NDR_record;
        break;

    case EXCEPTION_RAISE_REPLY_64_MESSAGE_ID:
        m_pMessage->data.raise_reply_64.ndr = NDR_record;
        break;

    case EXCEPTION_RAISE_STATE_REPLY_MESSAGE_ID:
        m_pMessage->data.raise_state_reply.ndr = NDR_record;
        break;

    case EXCEPTION_RAISE_STATE_REPLY_64_MESSAGE_ID:
        m_pMessage->data.raise_state_reply_64.ndr = NDR_record;
        break;

    case EXCEPTION_RAISE_STATE_IDENTITY_REPLY_MESSAGE_ID:
        m_pMessage->data.raise_state_identity_reply.ndr = NDR_record;
        break;

    case EXCEPTION_RAISE_STATE_IDENTITY_REPLY_64_MESSAGE_ID:
        m_pMessage->data.raise_state_identity_reply_64.ndr = NDR_record;
        break;

    default:
        FATAL_ERROR("Unhandled message type: %u", m_pMessage->header.msgh_id);
    }

    m_pMessage->header.msgh_reserved = 0;
    
    if (m_hTask)
    {
        kern_return_t machret;
        // Addref the task, because the receiver will expect it to own it. (or, if we
        // free it unsent, we'll expect to deallocate it).
        machret = mach_port_mod_refs(mach_task_self(), m_hTask, MACH_PORT_RIGHT_SEND, 1);
    }
}

// Initialize the size field of the message header (msgh_size) based on the message type and other fields.
// This should be called after all other fields have been initialized.
void MachMessage::InitMessageSize()
{
    // Note that in particular the kernel is very particular about the size of messages with embedded thread
    // states. The size of the message must reflect the exact size of the state flavor contained, not the
    // maximum size of a thread state that the message format implies.

    switch (m_pMessage->header.msgh_id)
    {
    case SET_THREAD_MESSAGE_ID:
        m_pMessage->header.msgh_size = sizeof(mach_msg_header_t) + sizeof(set_thread_request_t);
        break;

    case EXCEPTION_RAISE_MESSAGE_ID:
        m_pMessage->header.msgh_size = sizeof(mach_msg_header_t) + sizeof(exception_raise_notification_t);
        break;

    case EXCEPTION_RAISE_64_MESSAGE_ID:
        m_pMessage->header.msgh_size = sizeof(mach_msg_header_t) + sizeof(exception_raise_notification_64_t);
        break;

    case EXCEPTION_RAISE_STATE_MESSAGE_ID:
        m_pMessage->header.msgh_size = sizeof(mach_msg_header_t) +
            offsetof(exception_raise_state_notification_t, old_state) +
            (m_pMessage->data.raise_state.old_state_count * sizeof(natural_t));
        break;

    case EXCEPTION_RAISE_STATE_64_MESSAGE_ID:
        m_pMessage->header.msgh_size = sizeof(mach_msg_header_t) +
            offsetof(exception_raise_state_notification_64_t, old_state) +
            (m_pMessage->data.raise_state_64.old_state_count * sizeof(natural_t));
        break;

    case EXCEPTION_RAISE_STATE_IDENTITY_MESSAGE_ID:
        m_pMessage->header.msgh_size = sizeof(mach_msg_header_t) +
            offsetof(exception_raise_state_identity_notification_t, old_state) +
            (m_pMessage->data.raise_state_identity.old_state_count * sizeof(natural_t));
        break;

    case EXCEPTION_RAISE_STATE_IDENTITY_64_MESSAGE_ID:
        m_pMessage->header.msgh_size = sizeof(mach_msg_header_t) +
            offsetof(exception_raise_state_identity_notification_64_t, old_state) +
            (m_pMessage->data.raise_state_identity_64.old_state_count * sizeof(natural_t));
        break;

    case EXCEPTION_RAISE_REPLY_MESSAGE_ID:
        m_pMessage->header.msgh_size = sizeof(mach_msg_header_t) + sizeof(exception_raise_reply_t);
        break;

    case EXCEPTION_RAISE_REPLY_64_MESSAGE_ID:
        m_pMessage->header.msgh_size = sizeof(mach_msg_header_t) + sizeof(exception_raise_reply_64_t);
        break;

    case EXCEPTION_RAISE_STATE_REPLY_MESSAGE_ID:
        m_pMessage->header.msgh_size = sizeof(mach_msg_header_t) +
            offsetof(exception_raise_state_reply_t, new_state) +
            (m_pMessage->data.raise_state_reply.new_state_count * sizeof(natural_t));
        break;

    case EXCEPTION_RAISE_STATE_REPLY_64_MESSAGE_ID:
        m_pMessage->header.msgh_size = sizeof(mach_msg_header_t) +
            offsetof(exception_raise_state_reply_64_t, new_state) +
            (m_pMessage->data.raise_state_reply_64.new_state_count * sizeof(natural_t));
        break;

    case EXCEPTION_RAISE_STATE_IDENTITY_REPLY_MESSAGE_ID:
        m_pMessage->header.msgh_size = sizeof(mach_msg_header_t) +
            offsetof(exception_raise_state_identity_reply_t, new_state) +
            (m_pMessage->data.raise_state_identity_reply.new_state_count * sizeof(natural_t));
        break;

    case EXCEPTION_RAISE_STATE_IDENTITY_REPLY_64_MESSAGE_ID:
        m_pMessage->header.msgh_size = sizeof(mach_msg_header_t) +
            offsetof(exception_raise_state_identity_reply_64_t, new_state) +
            (m_pMessage->data.raise_state_identity_reply_64.new_state_count * sizeof(natural_t));
        break;

    default:
        FATAL_ERROR("Unhandled message type: %u", m_pMessage->header.msgh_id);
    }
}

// Given a thread's register context, locate and return the Mach port representing that thread. Only the
// x86_THREAD_STATE and x86_THREAD_STATE32 state flavors are supported for 32-bit.
thread_act_t MachMessage::GetThreadFromState(thread_state_flavor_t eFlavor, thread_state_t pState)
{
    SIZE_T targetSP;

    // Determine SP from the state provided based on its flavor (this algorithm only works with SP, so
    // flavors that don't report this register can't be used). However, hosts that use RAISE_STATE and a
    // flavor of state that don't contain SP should be very, very rare indeed (it's hard to imagine many
    // useful exception handlers that receive neither the exception thread or the general registers of that
    // thread).
    switch (eFlavor)
    {
    case x86_THREAD_STATE32:
#ifdef _X86_
        targetSP = ((x86_thread_state32_t*)pState)->esp;
#elif defined(_AMD64_)
        targetSP = ((x86_thread_state32_t*)pState)->__esp;
#else
#error Unexpected architecture.
#endif
        break;

    case x86_THREAD_STATE:
#ifdef _X86_
        targetSP = ((x86_thread_state_t*)pState)->uts.ts32.esp;
#elif defined(_AMD64_)
        targetSP = ((x86_thread_state_t*)pState)->uts.ts64.__rsp;
#else
#error Unexpected architecture.
#endif
        break;

#ifdef _AMD64_
    case x86_THREAD_STATE64:
        targetSP = ((x86_thread_state64_t*)pState)->__rsp;
        break;
#endif // _AMD64_
        
    default:
        FATAL_ERROR("Unhandled thread state flavor: %u", eFlavor);
    }

    // Capture the list of threads in the current task. Obviously this changes asynchronously to us, but that
    // doesn't matter since we know the thread we're after is suspended in the kernel and can't go anywhere.
    mach_msg_type_number_t cThreads;
    thread_act_t *pThreads;
    kern_return_t machret = task_threads(mach_task_self(),
                                         &pThreads,
                                         &cThreads);
    MACH_CHECK("task_threads()");

    // Iterate through each of the threads in the list.
    for (mach_msg_type_number_t i = 0; i < cThreads; i++)
    {
        // Get the general register state of each thread.
#ifdef _X86_        
        x86_thread_state32_t sThreadState;
        const thread_state_flavor_t sThreadStateFlavor = x86_THREAD_STATE32;
#elif defined(_AMD64_)
        x86_thread_state64_t sThreadState;
        const thread_state_flavor_t sThreadStateFlavor = x86_THREAD_STATE64;
#else
#error Unexpected architecture.
#endif
        mach_msg_type_number_t cThreadState = sizeof(sThreadState) / sizeof(natural_t);
        if (thread_get_state(pThreads[i],
                             sThreadStateFlavor,
                             (thread_state_t)&sThreadState,
                             &cThreadState) == KERN_SUCCESS)
        {
            // If a thread has the same SP as our target it should be the same thread (otherwise we have two
            // threads sharing the same stack which is very bad). Conversely the thread we're looking for is
            // suspended in the kernel so its SP should not change. We should always be able to find an exact
            // match as a result.
#ifdef _X86_
            if (sThreadState.esp == targetSP)
#elif defined(_AMD64_)
            if (sThreadState.__rsp == targetSP)
#else
#error Unexpected architecture.
#endif
            {
                thread_act_t hThread = pThreads[i];
                
                // Increment the refcount; the thread is a "send" right.
                machret = mach_port_mod_refs(mach_task_self(), hThread, MACH_PORT_RIGHT_SEND, 1);
                MACH_CHECK("mach_port_mod_refs()");

                // Deallocate the thread list now we're done with it.
                machret = vm_deallocate(mach_task_self(),
                                        (vm_address_t)pThreads,
                                        cThreads * sizeof(thread_act_t));
                MACH_CHECK("vm_deallocate()");

                // Return the thread we found.
                return hThread;
            }
        }
    }

    // If we got here no thread matched. That shouldn't be possible.
    FATAL_ERROR("Failed to locate thread from state.");
}

// Transform a exception handler behavior type into the corresponding Mach message ID for the notification.
mach_msg_id_t MachMessage::MapBehaviorToNotificationType(exception_behavior_t eBehavior)
{
    switch ((uint)eBehavior)
    {
    case EXCEPTION_DEFAULT:
        return EXCEPTION_RAISE_MESSAGE_ID;
    case EXCEPTION_STATE:
        return EXCEPTION_RAISE_STATE_MESSAGE_ID;
    case EXCEPTION_STATE_IDENTITY:
        return EXCEPTION_RAISE_STATE_IDENTITY_MESSAGE_ID;
    case MACH_EXCEPTION_CODES|EXCEPTION_DEFAULT:
        return EXCEPTION_RAISE_64_MESSAGE_ID;
    case MACH_EXCEPTION_CODES|EXCEPTION_STATE:
        return EXCEPTION_RAISE_STATE_64_MESSAGE_ID;
    case MACH_EXCEPTION_CODES|EXCEPTION_STATE_IDENTITY:
        return EXCEPTION_RAISE_STATE_IDENTITY_64_MESSAGE_ID;
    default:
        FATAL_ERROR("Unsupported exception behavior type: %u", eBehavior);
    }
}

// Transform a Mach message ID for an exception notification into the corresponding ID for the reply.
mach_msg_id_t MachMessage::MapNotificationToReplyType(mach_msg_id_t eNotificationType)
{
    switch (eNotificationType)
    {
    case EXCEPTION_RAISE_MESSAGE_ID:
        return EXCEPTION_RAISE_REPLY_MESSAGE_ID;
    case EXCEPTION_RAISE_STATE_MESSAGE_ID:
        return EXCEPTION_RAISE_STATE_REPLY_MESSAGE_ID;
    case EXCEPTION_RAISE_STATE_IDENTITY_MESSAGE_ID:
        return EXCEPTION_RAISE_STATE_IDENTITY_REPLY_MESSAGE_ID;
    case EXCEPTION_RAISE_64_MESSAGE_ID:
        return EXCEPTION_RAISE_REPLY_64_MESSAGE_ID;
    case EXCEPTION_RAISE_STATE_64_MESSAGE_ID:
        return EXCEPTION_RAISE_STATE_REPLY_64_MESSAGE_ID;
    case EXCEPTION_RAISE_STATE_IDENTITY_64_MESSAGE_ID:
        return EXCEPTION_RAISE_STATE_IDENTITY_REPLY_64_MESSAGE_ID;
    default:
        FATAL_ERROR("Unsupported message type: %u", eNotificationType);
    }
}

// Fetch the return code from a reply type message.
kern_return_t MachMessage::GetReturnCode()
{
    switch (m_pMessage->header.msgh_id)
    {
    case EXCEPTION_RAISE_REPLY_MESSAGE_ID:
        return m_pMessage->data.raise_reply.ret;

    case EXCEPTION_RAISE_REPLY_64_MESSAGE_ID:
        return m_pMessage->data.raise_reply_64.ret;

    case EXCEPTION_RAISE_STATE_REPLY_MESSAGE_ID:
        return m_pMessage->data.raise_state_reply.ret;

    case EXCEPTION_RAISE_STATE_REPLY_64_MESSAGE_ID:
        return m_pMessage->data.raise_state_reply_64.ret;

    case EXCEPTION_RAISE_STATE_IDENTITY_REPLY_MESSAGE_ID:
        return m_pMessage->data.raise_state_identity_reply.ret;

    case EXCEPTION_RAISE_STATE_IDENTITY_REPLY_64_MESSAGE_ID:
        return m_pMessage->data.raise_state_identity_reply_64.ret;

    default:
        FATAL_ERROR("Unsupported message type: %u", m_pMessage->header.msgh_id);
    }
}

// Set faulting thread in an exception notification message.
void MachMessage::SetThread(thread_act_t hThread)
{
    bool fSet;

    switch (m_pMessage->header.msgh_id)
    {
    case EXCEPTION_RAISE_MESSAGE_ID:
        m_pMessage->data.raise.thread_port.name = hThread;
        m_pMessage->data.raise.thread_port.pad1 = 0;
        m_pMessage->data.raise.thread_port.pad2 = 0;
        m_pMessage->data.raise.thread_port.disposition = MACH_MSG_TYPE_COPY_SEND;
        m_pMessage->data.raise.thread_port.type = MACH_MSG_PORT_DESCRIPTOR;
        fSet = true;
        break;

    case EXCEPTION_RAISE_64_MESSAGE_ID:
        m_pMessage->data.raise_64.thread_port.name = hThread;
        m_pMessage->data.raise_64.thread_port.pad1 = 0;
        m_pMessage->data.raise_64.thread_port.pad2 = 0;
        m_pMessage->data.raise_64.thread_port.disposition = MACH_MSG_TYPE_COPY_SEND;
        m_pMessage->data.raise_64.thread_port.type = MACH_MSG_PORT_DESCRIPTOR;
        fSet = true;
        break;

    case EXCEPTION_RAISE_STATE_MESSAGE_ID:
    case EXCEPTION_RAISE_STATE_64_MESSAGE_ID:
        // No thread field in RAISE_STATE messages.
        fSet = false;
        break;

    case EXCEPTION_RAISE_STATE_IDENTITY_MESSAGE_ID:
        m_pMessage->data.raise_state_identity.thread_port.name = hThread;
        m_pMessage->data.raise_state_identity.thread_port.pad1 = 0;
        m_pMessage->data.raise_state_identity.thread_port.pad2 = 0;
        m_pMessage->data.raise_state_identity.thread_port.disposition = MACH_MSG_TYPE_COPY_SEND;
        m_pMessage->data.raise_state_identity.thread_port.type = MACH_MSG_PORT_DESCRIPTOR;
        fSet = true;
        break;

    case EXCEPTION_RAISE_STATE_IDENTITY_64_MESSAGE_ID:
        m_pMessage->data.raise_state_identity_64.thread_port.name = hThread;
        m_pMessage->data.raise_state_identity_64.thread_port.pad1 = 0;
        m_pMessage->data.raise_state_identity_64.thread_port.pad2 = 0;
        m_pMessage->data.raise_state_identity_64.thread_port.disposition = MACH_MSG_TYPE_COPY_SEND;
        m_pMessage->data.raise_state_identity_64.thread_port.type = MACH_MSG_PORT_DESCRIPTOR;
        fSet = true;
        break;

    default:
        FATAL_ERROR("Unsupported message type: %u", m_pMessage->header.msgh_id);
        fSet = false;
    }
    
    if (fSet)
    {
        // Addref the thread port.
        kern_return_t machret;
        machret = mach_port_mod_refs(mach_task_self(), hThread, MACH_PORT_RIGHT_SEND, 1);
    }
}

// Set exception type in an exception notification message.
void MachMessage::SetException(exception_type_t eException)
{
    switch (m_pMessage->header.msgh_id)
    {
    case EXCEPTION_RAISE_MESSAGE_ID:
        m_pMessage->data.raise.exception = eException;
        break;

    case EXCEPTION_RAISE_64_MESSAGE_ID:
        m_pMessage->data.raise_64.exception = eException;
        break;

    case EXCEPTION_RAISE_STATE_MESSAGE_ID:
        m_pMessage->data.raise_state.exception = eException;
        break;

    case EXCEPTION_RAISE_STATE_64_MESSAGE_ID:
        m_pMessage->data.raise_state_64.exception = eException;
        break;

    case EXCEPTION_RAISE_STATE_IDENTITY_MESSAGE_ID:
        m_pMessage->data.raise_state_identity.exception = eException;
        break;

    case EXCEPTION_RAISE_STATE_IDENTITY_64_MESSAGE_ID:
        m_pMessage->data.raise_state_identity_64.exception = eException;
        break;

    default:
        FATAL_ERROR("Unsupported message type: %u", m_pMessage->header.msgh_id);
    }
}

// Set exception sub-code count in an exception notification message.
void MachMessage::SetExceptionCodeCount(int cCodes)
{
    switch (m_pMessage->header.msgh_id)
    {
    case EXCEPTION_RAISE_MESSAGE_ID:
        m_pMessage->data.raise.code_count = cCodes;
        break;

    case EXCEPTION_RAISE_64_MESSAGE_ID:
        m_pMessage->data.raise_64.code_count = cCodes;
        break;

    case EXCEPTION_RAISE_STATE_MESSAGE_ID:
        m_pMessage->data.raise_state.code_count = cCodes;
        break;

    case EXCEPTION_RAISE_STATE_64_MESSAGE_ID:
        m_pMessage->data.raise_state_64.code_count = cCodes;
        break;

    case EXCEPTION_RAISE_STATE_IDENTITY_MESSAGE_ID:
        m_pMessage->data.raise_state_identity.code_count = cCodes;
        break;

    case EXCEPTION_RAISE_STATE_IDENTITY_64_MESSAGE_ID:
        m_pMessage->data.raise_state_identity_64.code_count = cCodes;
        break;

    default:
        FATAL_ERROR("Unsupported message type: %u", m_pMessage->header.msgh_id);
    }
}

// Set exception sub-code in an exception notification message.
void MachMessage::SetExceptionCode(int iIndex, MACH_EH_TYPE(exception_data_type_t) iCode)
{
    if (iIndex < 0 || iIndex > 1)
        FATAL_ERROR("Exception code index out of range");

    // Note that although the 64-bit message variants support 64-bit exception sub-codes the CoreCLR only
    // supports 32-bit processes. We should never see the upper 32-bits containing a non-zero value therefore.

    switch (m_pMessage->header.msgh_id)
    {
    case EXCEPTION_RAISE_MESSAGE_ID:
        m_pMessage->data.raise.code[iIndex] = (int)iCode;
        break;

    case EXCEPTION_RAISE_64_MESSAGE_ID:
        m_pMessage->data.raise_64.code[iIndex] = iCode;
        break;

    case EXCEPTION_RAISE_STATE_MESSAGE_ID:
        m_pMessage->data.raise_state.code[iIndex] = (int)iCode;
        break;

    case EXCEPTION_RAISE_STATE_64_MESSAGE_ID:
        m_pMessage->data.raise_state_64.code[iIndex] = iCode;
        break;

    case EXCEPTION_RAISE_STATE_IDENTITY_MESSAGE_ID:
        m_pMessage->data.raise_state_identity.code[iIndex] = (int)iCode;
        break;

    case EXCEPTION_RAISE_STATE_IDENTITY_64_MESSAGE_ID:
        m_pMessage->data.raise_state_identity_64.code[iIndex] = iCode;
        break;

    default:
        FATAL_ERROR("Unsupported message type: %u", m_pMessage->header.msgh_id);
    }
}

// Set return code in a reply message.
void MachMessage::SetReturnCode(kern_return_t eReturnCode)
{
    switch (m_pMessage->header.msgh_id)
    {
    case EXCEPTION_RAISE_REPLY_MESSAGE_ID:
        m_pMessage->data.raise_reply.ret = eReturnCode;
        break;

    case EXCEPTION_RAISE_REPLY_64_MESSAGE_ID:
        m_pMessage->data.raise_reply_64.ret = eReturnCode;
        break;

    case EXCEPTION_RAISE_STATE_REPLY_MESSAGE_ID:
        m_pMessage->data.raise_state_reply.ret = eReturnCode;
        break;

    case EXCEPTION_RAISE_STATE_REPLY_64_MESSAGE_ID:
        m_pMessage->data.raise_state_reply_64.ret = eReturnCode;
        break;

    case EXCEPTION_RAISE_STATE_IDENTITY_REPLY_MESSAGE_ID:
        m_pMessage->data.raise_state_identity_reply.ret = eReturnCode;
        break;

    case EXCEPTION_RAISE_STATE_IDENTITY_REPLY_64_MESSAGE_ID:
        m_pMessage->data.raise_state_identity_reply_64.ret = eReturnCode;
        break;

    default:
        FATAL_ERROR("Unsupported message type: %u", m_pMessage->header.msgh_id);
    }
}

// Set faulting thread register state in an exception notification or reply message.
void MachMessage::SetThreadState(thread_state_flavor_t eFlavor, thread_state_t pState, size_t cbState)
{
    switch (m_pMessage->header.msgh_id)
    {
    case EXCEPTION_RAISE_MESSAGE_ID:
    case EXCEPTION_RAISE_REPLY_MESSAGE_ID:
    case EXCEPTION_RAISE_64_MESSAGE_ID:
    case EXCEPTION_RAISE_REPLY_64_MESSAGE_ID:
        // No thread state in RAISE or RAISE_REPLY messages.
        break;

    case EXCEPTION_RAISE_STATE_MESSAGE_ID:
        m_pMessage->data.raise_state.flavor = eFlavor;
        m_pMessage->data.raise_state.old_state_count = cbState / sizeof(natural_t);
        memcpy(m_pMessage->data.raise_state.old_state, pState, cbState);
        break;

    case EXCEPTION_RAISE_STATE_64_MESSAGE_ID:
        m_pMessage->data.raise_state_64.flavor = eFlavor;
        m_pMessage->data.raise_state_64.old_state_count = cbState / sizeof(natural_t);
        memcpy(m_pMessage->data.raise_state_64.old_state, pState, cbState);
        break;

    case EXCEPTION_RAISE_STATE_IDENTITY_MESSAGE_ID:
        m_pMessage->data.raise_state_identity.flavor = eFlavor;
        m_pMessage->data.raise_state_identity.old_state_count = cbState / sizeof(natural_t);
        memcpy(m_pMessage->data.raise_state_identity.old_state, pState, cbState);
        break;

    case EXCEPTION_RAISE_STATE_IDENTITY_64_MESSAGE_ID:
        m_pMessage->data.raise_state_identity_64.flavor = eFlavor;
        m_pMessage->data.raise_state_identity_64.old_state_count = cbState / sizeof(natural_t);
        memcpy(m_pMessage->data.raise_state_identity_64.old_state, pState, cbState);
        break;

    case EXCEPTION_RAISE_STATE_REPLY_MESSAGE_ID:
        m_pMessage->data.raise_state_reply.flavor = eFlavor;
        m_pMessage->data.raise_state_reply.new_state_count = cbState / sizeof(natural_t);
        memcpy(m_pMessage->data.raise_state_reply.new_state, pState, cbState);
        break;

    case EXCEPTION_RAISE_STATE_REPLY_64_MESSAGE_ID:
        m_pMessage->data.raise_state_reply_64.flavor = eFlavor;
        m_pMessage->data.raise_state_reply_64.new_state_count = cbState / sizeof(natural_t);
        memcpy(m_pMessage->data.raise_state_reply_64.new_state, pState, cbState);
        break;

    case EXCEPTION_RAISE_STATE_IDENTITY_REPLY_MESSAGE_ID:
        m_pMessage->data.raise_state_identity_reply.flavor = eFlavor;
        m_pMessage->data.raise_state_identity_reply.new_state_count = cbState / sizeof(natural_t);
        memcpy(m_pMessage->data.raise_state_identity_reply.new_state, pState, cbState);
        break;

    case EXCEPTION_RAISE_STATE_IDENTITY_REPLY_64_MESSAGE_ID:
        m_pMessage->data.raise_state_identity_reply_64.flavor = eFlavor;
        m_pMessage->data.raise_state_identity_reply_64.new_state_count = cbState / sizeof(natural_t);
        memcpy(m_pMessage->data.raise_state_identity_reply_64.new_state, pState, cbState);
        break;

    default:
        FATAL_ERROR("Unsupported message type: %u", m_pMessage->header.msgh_id);
    }
}

#endif // HAVE_MACH_EXCEPTIONS
