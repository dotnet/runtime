// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++

Module Name:

    machmessage.h

Abstract:

    Abstraction over Mach messages used during exception handling.

--*/

#include <mach/mach.h>
#include <mach/mach_error.h>
#include <mach/thread_status.h>

using namespace CorUnix;

#if HAVE_MACH_EXCEPTIONS

#if defined(HOST_AMD64)
#define MACH_EH_TYPE(x) mach_##x
#else
#define MACH_EH_TYPE(x) x
#endif // defined(HOST_AMD64)

// The vast majority of Mach calls we make in this module are critical: we cannot recover from failures of
// these methods (principally because we're handling hardware exceptions in the context of a single dedicated
// handler thread). The following macro encapsulates checking the return code from Mach methods and emitting
// some useful data and aborting the process on failure.
#define CHECK_MACH(_msg, machret) do {                                      \
        if (machret != KERN_SUCCESS)                                        \
        {                                                                   \
            char _szError[1024];                                            \
            snprintf(_szError, _countof(_szError), "%s: %u: %s", __FUNCTION__, __LINE__, _msg);  \
            mach_error(_szError, machret);                                  \
            abort();                                                        \
        }                                                                   \
    } while (false)

// This macro terminates the process with some useful debug info as above, but for the general failure points
// that have nothing to do with Mach.
#define NONPAL_RETAIL_ASSERT(_msg, ...) do {                                    \
        fprintf(stdout, "%s: %u: " _msg "\n", __FUNCTION__, __LINE__, ## __VA_ARGS__);   \
        fflush(stdout);                                                         \
        abort();                                                                \
    } while (false)

#define NONPAL_RETAIL_ASSERTE(_expr) do {                    \
        if (!(_expr))                                        \
            NONPAL_RETAIL_ASSERT("ASSERT: %s\n", #_expr);    \
    } while (false)

#ifdef _DEBUG

#define NONPAL_TRACE_ENABLED EnvironGetenv("NONPAL_TRACING", /* copyValue */ false)

#define NONPAL_ASSERT(_msg, ...) NONPAL_RETAIL_ASSERT(_msg, __VA_ARGS__)

// Assert macro that doesn't rely on the PAL.
#define NONPAL_ASSERTE(_expr) do {                           \
        if (!(_expr))                                        \
            NONPAL_RETAIL_ASSERT("ASSERT: %s\n", #_expr);    \
    } while (false)

// Debug-only output with printf-style formatting.
#define NONPAL_TRACE(_format, ...) do {                                              \
        if (NONPAL_TRACE_ENABLED) { fprintf(stdout, "NONPAL_TRACE: " _format, ## __VA_ARGS__); fflush(stdout); }  \
    } while (false)

#else // _DEBUG

#define NONPAL_TRACE_ENABLED false
#define NONPAL_ASSERT(_msg, ...)
#define NONPAL_ASSERTE(_expr)
#define NONPAL_TRACE(_format, ...)

#endif // _DEBUG

class MachMessage;

// Contains all the exception and thread state information needed to forward the exception.
struct MachExceptionInfo
{
    exception_type_t ExceptionType;
    mach_msg_type_number_t SubcodeCount;
    MACH_EH_TYPE(exception_data_type_t) Subcodes[2];
#if defined(HOST_AMD64)
    x86_thread_state_t ThreadState;
    x86_float_state_t FloatState;
    x86_debug_state_t DebugState;
#elif defined(HOST_ARM64)
    arm_thread_state64_t ThreadState;
    arm_neon_state64_t FloatState;
    arm_debug_state64_t DebugState;
#else
#error Unexpected architecture
#endif
    MachExceptionInfo(mach_port_t thread, MachMessage& message);
    void RestoreState(mach_port_t thread);
};

// Abstraction of a subset of Mach message types. Provides accessors that hide the subtle differences in the
// message layout of similar message types.
class MachMessage
{
public:
    // The message types handled by this class. The values are the actual type codes set in the Mach message
    // header.
    enum MessageType
    {
        SET_THREAD_MESSAGE_ID = 1,
        FORWARD_EXCEPTION_MESSAGE_ID = 2,
        NOTIFY_SEND_ONCE_MESSAGE_ID = 71,
        EXCEPTION_RAISE_MESSAGE_ID = 2401,
        EXCEPTION_RAISE_STATE_MESSAGE_ID = 2402,
        EXCEPTION_RAISE_STATE_IDENTITY_MESSAGE_ID = 2403,
        EXCEPTION_RAISE_64_MESSAGE_ID = 2405,
        EXCEPTION_RAISE_STATE_64_MESSAGE_ID = 2406,
        EXCEPTION_RAISE_STATE_IDENTITY_64_MESSAGE_ID = 2407,
        EXCEPTION_RAISE_REPLY_MESSAGE_ID = 2501,
        EXCEPTION_RAISE_STATE_REPLY_MESSAGE_ID = 2502,
        EXCEPTION_RAISE_STATE_IDENTITY_REPLY_MESSAGE_ID = 2503,
        EXCEPTION_RAISE_REPLY_64_MESSAGE_ID = 2505,
        EXCEPTION_RAISE_STATE_REPLY_64_MESSAGE_ID = 2506,
        EXCEPTION_RAISE_STATE_IDENTITY_REPLY_64_MESSAGE_ID = 2507
    };

    // Construct an empty message. Use Receive() to form a message that can be inspected or SendSetThread(),
    // ForwardNotification() or ReplyToNotification() to construct a message and sent it.
    MachMessage();

    // Listen for the next message on the given port and initialize this class with the contents. The message
    // type must match one of the MessageTypes indicated above (or the process will be aborted).
    void Receive(mach_port_t hPort);

    // Indicate whether a received message belongs to a particular semantic class.
    bool IsSetThreadRequest();          // Message is a request to set the context of a particular thread
    bool IsForwardExceptionRequest();   // Message is a request to forward the exception
    bool IsSendOnceDestroyedNotify();   // Message is a notification that a send-once message was destroyed by the receiver
    bool IsExceptionNotification();     // Message is a notification of an exception
    bool IsExceptionReply();            // Message is a reply to the notification of an exception

    // Get properties of a received message header.
    MessageType GetMessageType();       // The message type
    const char *GetMessageTypeName();   // An ASCII representation of the message type for logging purposes
    mach_port_t GetLocalPort();         // The destination port the message was sent to
    mach_port_t GetRemotePort();        // The source port the message came from (if a reply is expected)

    // Get the properties of a set thread request. Fills in the provided context structure with the context
    // from the message and returns the target thread to which the context should be applied.
    thread_act_t GetThreadContext(CONTEXT *pContext);

    // Returns the pal thread instance for the forward exception message
    CPalThread *GetPalThread();

    // Returns the exception info from the forward exception message
    MachExceptionInfo *GetExceptionInfo();

    // Get properties of the type-specific portion of the message. The following properties are supported by
    // exception notification messages only.
    thread_act_t GetThread();           // Get the faulting thread
    exception_type_t GetException();    // Get the exception type (e.g. EXC_BAD_ACCESS)
    int GetExceptionCodeCount();        // Get the number of exception sub-codes
    MACH_EH_TYPE(exception_data_type_t) GetExceptionCode(int iIndex);   // Get the exception sub-code at the given index

    // Fetch the thread state flavor from a notification or reply message (return THREAD_STATE_NONE for the
    // messages that don't contain a thread state).
    thread_state_flavor_t GetThreadStateFlavor();

    // Get the thread state with the given flavor from the exception or exception reply message. If the
    // message doesn't contain a thread state or the flavor of the state in the message doesn't match, the
    // state will be fetched directly from the target thread instead (which can be computed implicitly for
    // exception messages or passed explicitly for reply messages).
    mach_msg_type_number_t GetThreadState(thread_state_flavor_t eFlavor, thread_state_t pState, thread_act_t thread = NULL);

    // Fetch the return code from a reply type message.
    kern_return_t GetReturnCode();

    // Initialize and send a request to set the register context of a particular thread.
    void SendSetThread(mach_port_t hServerPort, CONTEXT *pContext);

    // Initialize and send a request to forward the exception message to the notification thread
    void SendForwardException(mach_port_t hServerPort, MachExceptionInfo *pExceptionInfo, CPalThread *ppalThread);

    // Initialize the message (overwriting any previous content) to represent a forwarded version of the given
    // exception notification message and send that message to the chain-back handler previously registered
    // for the exception type being notified. The new message takes account of the fact that the target
    // handler may not have requested the same notification behavior or flavor as our handler.
    void ForwardNotification(MachExceptionHandler *pHandler, MachMessage& message);

    // Initialize the message (overwriting any previous content) to represent a reply to the given exception
    // notification and send that reply back to the original sender of the notification. This is used when our
    // handler handles the exception rather than forwarding it to a chain-back handler.
    void ReplyToNotification(MachMessage& message, kern_return_t eResult);

private:
    // The maximum size in bytes of any Mach message we can send or receive. Calculating an exact size for
    // this is non trivial (basically because of the security trailers that Mach appends) but the current
    // value has proven to be more than enough so far.
    static const size_t kcbMaxMessageSize = 1500;

    // The following are structures describing the formats of the Mach messages we understand.

    // Request to set the register context on a particular thread.
    // SET_THREAD_MESSAGE_ID
    struct set_thread_request_t
    {
        thread_act_t thread;
        CONTEXT new_context;
    };

    // Request to forward the exception notification
    // FORWARD_EXCEPTION_MESSAGE_ID
    struct forward_exception_request_t
    {
        thread_act_t thread;
        CPalThread *ppalThread;
        MachExceptionInfo exception_info;
    };

#pragma pack(4)

    // EXCEPTION_RAISE_MESSAGE_ID
    struct exception_raise_notification_t
    {
        mach_msg_body_t msgh_body;
        mach_msg_port_descriptor_t thread_port;
        mach_msg_port_descriptor_t task_port;
        NDR_record_t ndr;
        exception_type_t exception;
        mach_msg_type_number_t code_count;
        exception_data_type_t code[2];
    };

    // EXCEPTION_RAISE_REPLY_MESSAGE_ID
    struct exception_raise_reply_t
    {
        NDR_record_t ndr;
        kern_return_t ret;
    };

    // EXCEPTION_RAISE_64_MESSAGE_ID
    struct exception_raise_notification_64_t
    {
        mach_msg_body_t msgh_body;
        mach_msg_port_descriptor_t thread_port;
        mach_msg_port_descriptor_t task_port;
        NDR_record_t ndr;
        exception_type_t exception;
        mach_msg_type_number_t code_count;
        mach_exception_data_type_t code[2];
    };

    // EXCEPTION_RAISE_REPLY_64_MESSAGE_ID
    struct exception_raise_reply_64_t
    {
        NDR_record_t ndr;
        kern_return_t ret;
    };

    // EXCEPTION_RAISE_STATE_MESSAGE_ID
    struct exception_raise_state_notification_t
    {
        NDR_record_t ndr;
        exception_type_t exception;
        mach_msg_type_number_t code_count;
        exception_data_type_t code[2];
        thread_state_flavor_t flavor;
        mach_msg_type_number_t old_state_count;
        natural_t old_state[THREAD_STATE_MAX];
    };

    // EXCEPTION_RAISE_STATE_REPLY_MESSAGE_ID
    struct exception_raise_state_reply_t
    {
        NDR_record_t ndr;
        kern_return_t ret;
        thread_state_flavor_t flavor;
        mach_msg_type_number_t new_state_count;
        natural_t new_state[THREAD_STATE_MAX];
    };

    // EXCEPTION_RAISE_STATE_64_MESSAGE_ID
    struct exception_raise_state_notification_64_t
    {
        NDR_record_t ndr;
        exception_type_t exception;
        mach_msg_type_number_t code_count;
        mach_exception_data_type_t code[2];
        thread_state_flavor_t flavor;
        mach_msg_type_number_t old_state_count;
        natural_t old_state[THREAD_STATE_MAX];
    };

    // EXCEPTION_RAISE_STATE_REPLY_64_MESSAGE_ID
    struct exception_raise_state_reply_64_t
    {
        NDR_record_t ndr;
        kern_return_t ret;
        thread_state_flavor_t flavor;
        mach_msg_type_number_t new_state_count;
        natural_t new_state[THREAD_STATE_MAX];
    };

    // EXCEPTION_RAISE_STATE_IDENTITY_MESSAGE_ID
    struct exception_raise_state_identity_notification_t
    {
        mach_msg_body_t msgh_body;
        mach_msg_port_descriptor_t thread_port;
        mach_msg_port_descriptor_t task_port;
        NDR_record_t ndr;
        exception_type_t exception;
        mach_msg_type_number_t code_count;
        exception_data_type_t code[2];
        thread_state_flavor_t flavor;
        mach_msg_type_number_t old_state_count;
        natural_t old_state[THREAD_STATE_MAX];
    };

    // EXCEPTION_RAISE_STATE_IDENTITY_REPLY_MESSAGE_ID
    struct exception_raise_state_identity_reply_t
    {
        NDR_record_t ndr;
        kern_return_t ret;
        thread_state_flavor_t flavor;
        mach_msg_type_number_t new_state_count;
        natural_t new_state[THREAD_STATE_MAX];
    };

    // EXCEPTION_RAISE_STATE_IDENTITY_64_MESSAGE_ID
    struct exception_raise_state_identity_notification_64_t
    {
        mach_msg_body_t msgh_body;
        mach_msg_port_descriptor_t thread_port;
        mach_msg_port_descriptor_t task_port;
        NDR_record_t ndr;
        exception_type_t exception;
        mach_msg_type_number_t code_count;
        mach_exception_data_type_t code[2];
        thread_state_flavor_t flavor;
        mach_msg_type_number_t old_state_count;
        natural_t old_state[THREAD_STATE_MAX];
    };

    // EXCEPTION_RAISE_STATE_IDENTITY_REPLY_64_MESSAGE_ID
    struct exception_raise_state_identity_reply_64_t
    {
        NDR_record_t ndr;
        kern_return_t ret;
        thread_state_flavor_t flavor;
        mach_msg_type_number_t new_state_count;
        natural_t new_state[THREAD_STATE_MAX];
    };

#pragma pack()

    // All the above messages are sent with a standard Mach header prepended. This structure unifies the
    // message formats.
    struct mach_message_t
    {
        mach_msg_header_t       header;
        union
        {
            set_thread_request_t                                set_thread;
            forward_exception_request_t                         forward_exception;
            exception_raise_notification_t                      raise;
            exception_raise_state_notification_t                raise_state;
            exception_raise_state_identity_notification_t       raise_state_identity;
            exception_raise_notification_64_t                   raise_64;
            exception_raise_state_notification_64_t             raise_state_64;
            exception_raise_state_identity_notification_64_t    raise_state_identity_64;
            exception_raise_reply_t                             raise_reply;
            exception_raise_state_reply_t                       raise_state_reply;
            exception_raise_state_identity_reply_t              raise_state_identity_reply;
            exception_raise_reply_64_t                          raise_reply_64;
            exception_raise_state_reply_64_t                    raise_state_reply_64;
            exception_raise_state_identity_reply_64_t           raise_state_identity_reply_64;
        } data;
    } __attribute__((packed));;

    // Re-initializes this data structure (to the same state as default construction, containing no message).
    void ResetMessage();

    // Initialize those fields of a message that are invariant. This method expects that the msgh_id field has
    // been filled in prior to the call so it can determine which non-header fields to initialize.
    void InitFixedFields();

    // Initialize the size field of the message header (msgh_size) based on the message type and other fields.
    // This should be called after all other fields have been initialized.
    void InitMessageSize();

    // Do the work of getting ports from the message.
    //  * fCalculate -- calculate the thread port if the message did not contain it.
    //  * fValidate  -- failfast if the message was not one expected to have a (calculable) thread port.
    void GetPorts(bool fCalculate, bool fValidThread);

    // Given a thread's register context, locate and return the Mach port representing that thread. Only the
    // x86_THREAD_STATE and x86_THREAD_STATE32 state flavors are supported.
    thread_act_t GetThreadFromState(thread_state_flavor_t eFlavor, thread_state_t pState);

    // Transform an exception handler behavior type into the corresponding Mach message ID for the
    // notification.
    mach_msg_id_t MapBehaviorToNotificationType(exception_behavior_t eBehavior);

    // Transform a Mach message ID for an exception notification into the corresponding ID for the reply.
    mach_msg_id_t MapNotificationToReplyType(mach_msg_id_t eNotificationType);

    // The following methods initialize fields on the message prior to transmission. Each is valid for either
    // notification, replies or both. If a particular setter is defined for replies, say, then it will be a
    // no-op for any replies which don't contain that field. This makes transforming between notifications and
    // replies of different types simpler (we can copy a super-set of all fields between the two, but only
    // those operations that make sense will do any work).

    // Defined for notifications:
    void SetThread(thread_act_t thread);
    void SetException(exception_type_t eException);
    void SetExceptionCodeCount(int cCodes);
    void SetExceptionCode(int iIndex, MACH_EH_TYPE(exception_data_type_t) iCode);

    // Defined for replies:
    void SetReturnCode(kern_return_t eReturnCode);

    // Defined for both notifications and replies.
    void SetThreadState(thread_state_flavor_t eFlavor, thread_state_t pState, mach_msg_type_number_t count);

    // Maximally sized buffer for the message to be received into or transmitted out of this class.
    unsigned char   m_rgMessageBuffer[kcbMaxMessageSize];

    // Initialized by ResetMessage() to point to the buffer above. Gives a typed view of the encapsulated Mach
    // message.
    mach_message_t *m_pMessage;

    // Cached value of GetThread() or MACH_PORT_NULL if that has not been computed yet.
    thread_act_t    m_hThread;

    // Cached value of the task port or MACH_PORT_NULL if the message doesn't have one.
    mach_port_t     m_hTask;

    // Considered whether we are responsible for the deallocation of the ports in
    // this message. It is true for messages we receive, and false for messages we send.
    bool m_fPortsOwned;
};

#endif // HAVE_MACH_EXCEPTIONS
