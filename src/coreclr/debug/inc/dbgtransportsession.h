// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#ifndef __DBG_TRANSPORT_SESSION_INCLUDED
#define __DBG_TRANSPORT_SESSION_INCLUDED

#ifndef RIGHT_SIDE_COMPILE
#include <utilcode.h>
#include <crst.h>

#endif // !RIGHT_SIDE_COMPILE

#if defined(FEATURE_DBGIPC_TRANSPORT_VM) || defined(FEATURE_DBGIPC_TRANSPORT_DI)

#include <twowaypipe.h>

/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
 DbgTransportSession was originally designed around cross-machine debugging via sockets and it is supposed to
 handle network interruptions. Right now we use pipes (see TwoWaypipe) and don't expect to have connection issues.
 But there seem to be no good reason to try hard to get rid of existing working protocol even if it's a bit
 cautious about connection quality. So please KEEP IN MIND THAT SOME COMMENTS REFERRING TO NETWORK AND SOCKETS
 CAN BE OUTDATED.
 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */

//
// Provides a robust and secure transport session between a debugger and a debuggee that are potentially on
// different machines.
//
// The following terminology is used for the wire protocol. The smallest meaningful entity written to or read
// from the connection is a "message". This consists of one or maybe two "blocks" where a block is a
// contiguous region of memory in the host machine. The first block is always a "message header" which is
// fixed size (allowing the receiver to know how many bytes to read off the stream oriented connection) and
// has type codes and other fields which the receiver can use to determine if another block is part of the
// message (and if so, exactly how large that block is). Many management messages consist only of a message
// header block, while operations such as sending a debugger event structure involve a message header followed
// by a block containing the actual event structure.
//
// Message acknowledgement (sometimes abbreviated to ack) refers to a system of marking all messages with an
// ID and noting and reporting which IDs we've seen from our peer. We piggy back the highest seen ID on all
// outgoing messages and this is used by the infrastructure to communicate the fact that a sender can release
// its copy of an outbound message since it successfully made it across the communications channel and won't
// need to be resent in the case of a network failure.
//
// This file uses the debugger conventions for naming the two endpoints of the session: the left side or LS is
// the side with the runtime while the right side (RS) is the side with the debugger.
//

// The structure of this file necessitates a certain number of forward references (particularly in the
// comments). If you see a term you don't understand please do a search for it further down the file, where
// hopefully you will find a detailed definition (and if not, please add one).

struct DebuggerIPCEvent;
struct DbgEventBufferEntry;

// Some simple ad-hoc debug only transport logging. This output is too chatty for an existng CLR logging
// channel (and we've run out of bits for an additional channel) and is likely to be of limited use to anyone
// besides the transport developer (and even then only occasionally).
//
// To enable use 'set|export COMPlus_DbgTransportLog=X' where X is 1 for RS logging, 2 for LS logging and 3
// for both (default is disabled). Use 'set|export COMPlus_DbgTransportLogClass=X' where X is the hex
// representation of one or more DbgTransportLogClass flags defined below (default is all classes enabled).
// For instance, 'set COMPlus_DbgTransportLogClass=f' will enable only message send and receive logging (for
// all message types).
enum DbgTransportLogEnable
{
    LE_None         = 0x00000000,
    LE_LeftSide     = 0x00000001,
    LE_RightSide    = 0x00000002,
    LE_Unknown      = 0xffffffff,
};

enum DbgTransportLogClass
{
    LC_None         = 0x00000000,
    LC_Events       = 0x00000001,   // Sending and receiving debugger events
    LC_Session      = 0x00000002,   // Sending and receiving session messages
    LC_Requests     = 0x00000004,   // Sending requests such as MT_GetDCB and receiving replies
    LC_EventAcks    = 0x00000008,   // Sending and receiving debugger event acks (DEPRECATED)
    LC_NetErrors    = 0x00000010,   // Network errors
    LC_FaultInject  = 0x00000020,   // Artificially injected network faults
    LC_Proxy        = 0x00000040,   // Proxy interactions
    LC_All          = 0xffffffff,
    LC_Always       = 0xffffffff,   // Always log, regardless of class setting
};

// Status codes that can be returned by various APIs that indicate some conditions of the error that a caller
// might usefully pass on to a user (environmental factors that the user might have some control over).
enum ConnStatus
{
    SCS_Success,                // The request succeeded
    SCS_OutOfMemory,            // The request failed due to a low memory situation
    SCS_InvalidConfiguration,   // Initialize() failed because the debugger settings were not configured or
                                // have become corrupt
    SCS_UnknownTarget,          // Connect() failed because the remote machine at the given address could not
                                // be found
    SCS_NoListener,             // Connect() failed because the remote machine was not listening for requests
                                // on the given port (most likely the remote machine is not configured for
                                // debugging)
    SCS_NetworkFailure,         // Connect() failed due to miscellaneous network error
    SCS_MismatchedCerts,        // Connect()/Accept() failed because the remote party was using a different
                                // cert
};


// Multiple clients can use a single DbgTransportSession, but only one can act as the debugger.
// A valid DebugTicket is given to the client who is acting as the debugger.
struct DebugTicket
{
friend class DbgTransportSession;

public:
    DebugTicket() { m_fValid = false; };

    bool IsValid() { return m_fValid; };

protected:
    void SetValid()   { m_fValid = true;  };
    void SetInvalid() { m_fValid = false; };

private:
    // Tickets can't be copied around. Hide these definitions so as to enforce that.
    // We still need the Copy ctor so that it can be passed in as a parameter.
    void operator=(DebugTicket & other);

    bool m_fValid;
};

#ifdef RIGHT_SIDE_COMPILE
#define DBG_TRANSPORT_LOG_THIS_SIDE LE_RightSide
#define DBG_TRANSPORT_LOG_PREFIX    "RS"
#else // RIGHT_SIDE_COMPILE
#define DBG_TRANSPORT_LOG_THIS_SIDE LE_LeftSide
#define DBG_TRANSPORT_LOG_PREFIX    "LS"
#endif // RIGHT_SIDE_COMPILE

// Method used to log an interesting event (of the given class). The message given will have any additional
// arguments inserted following 'printf' formatiing conventions and will be automatically prepended with a
// LS/RS indicator and suffixed with a newline.
inline void DbgTransportLog(DbgTransportLogClass eClass, const char *szFormat, ...)
{
#ifdef _DEBUG
    static DWORD s_dwLoggingEnabled = LE_Unknown;
    static DWORD s_dwLoggingClass = LC_All;

    if (s_dwLoggingEnabled == LE_Unknown)
    {
        s_dwLoggingEnabled = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_DbgTransportLog);
        s_dwLoggingClass = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_DbgTransportLogClass);
    }

    if ((s_dwLoggingEnabled & DBG_TRANSPORT_LOG_THIS_SIDE) &&
        ((s_dwLoggingClass & eClass) || eClass == LC_Always))
    {
        char    szOutput[256];
        va_list args;

        va_start(args, szFormat);
        vsprintf_s(szOutput, sizeof(szOutput), szFormat, args);
        va_end(args);

        printf("%s  %04x: %s\n", DBG_TRANSPORT_LOG_PREFIX, GetCurrentThreadId(), szOutput);
        fflush(stdout);

        char szDebugOutput[512];
        sprintf_s(szDebugOutput, sizeof(szDebugOutput), "%s: %s\n", DBG_TRANSPORT_LOG_PREFIX, szOutput);
        OutputDebugStringA(szDebugOutput);
    }
#endif // _DEBUG
}

#ifdef _DEBUG
//
// Debug-only network fault injection (in order to help test the robust session code). Control is via a single
// DWORD read from the environment (COMPlus_DbgTransportFaultInject). This DWORD is treated as a set of bit
// fields as follows:
//
//    +-------+-------+-------+----------------+-----------+
//    |  Side |   Op  | State |    Reserved    | Frequency |
//    +-------+-------+-------+----------------+-----------+
//     31<->28 27<->24 23<->20 19<----------->8 7<------->0
//
// The 'Side' field indicates whether the left or right side (or both) should have faults injected. See
// DbgTransportFaultSide below for values.
//
// The 'Op' field indicates which connection methods should simulate failures. See DbgTransportFaultOp.
//
// The 'State' field indicates the session states in which faults will be injected. See
// DbgTransportFaultState. Note that introducing too many failures into the Opening and Opening_NC states will
// cause the debugger to timeout and fail.
//
// The 'Reserved' field has no current meaning and should be left as zero.
//
// The 'Frequency' field indicates a percentage failure rate. Valid values are between 0 and 99, values beyond
// this range will be clamped to 99.
//
// For example:
//
//  export COMPlus_DbgTransportFaultInject=1ff00001
//  --> Fail all network operations on the left side 1% of the time
//
//  export COMPlus_DbgTransportFaultInject=34200063
//  --> Fail Send() calls on both sides while the session is Open 99% of the time
//

#define DBG_TRANSPORT_FAULT_RATE_MASK 0x000000ff

// Whether to inject faults to the left, right or both sides.
enum DbgTransportFaultSide
{
    FS_Left     = 0x10000000,
    FS_Right    = 0x20000000,
};

// Network operations which are candidates for fault injection.
enum DbgTransportFaultOp
{
    FO_Connect  = 0x01000000,
    FO_Accept   = 0x02000000,
    FO_Send     = 0x04000000,
    FO_Receive  = 0x08000000,
};

// Session states into which faults should be injected.
enum DbgTransportFaultState
{
    FS_Opening  = 0x00100000,   // Opening and Opening_NC
    FS_Open     = 0x00200000,
    FS_Resync   = 0x00400000,   // Resync and Resync_NC
};

#ifdef RIGHT_SIDE_COMPILE
#define DBG_TRANSPORT_FAULT_THIS_SIDE FS_Right
#else // RIGHT_SIDE_COMPILE
#define DBG_TRANSPORT_FAULT_THIS_SIDE FS_Left
#endif // RIGHT_SIDE_COMPILE

// Macro to determine whether a fault should be injected for the given operation.
#define DBG_TRANSPORT_SHOULD_INJECT_FAULT(_op) DbgTransportShouldInjectFault(FO_##_op, #_op)

#else // _DEBUG
#define DBG_TRANSPORT_SHOULD_INJECT_FAULT(_op) false
#endif // _DEBUG

// The PAL doesn't define htons (host-to-network-short) and friends. So provide our own versions here.
// winsock2.h defines BIGENDIAN to 0x0000 and LITTLEENDIAN to 0x0001, so we need to be careful with the
// #ifdef.
#if BIGENDIAN > 0
#define DBGIPC_HTONS(x) (x)
#define DBGIPC_NTOHS(x) (x)
#define DBGIPC_HTONL(x) (x)
#define DBGIPC_NTOHL(x) (x)
#else
inline UINT16 DBGIPC_HTONS(UINT16 x)
{
    return (x >> 8) | (x << 8);
}
#define DBGIPC_NTOHS(x) DBGIPC_HTONS(x)
inline UINT32 DBGIPC_HTONL(UINT32 x)
{
    return  (x >> 24) |
            ((x >> 8) & 0x0000FF00L) |
            ((x & 0x0000FF00L) << 8) |
            (x << 24);
}
#define DBGIPC_NTOHL(x) DBGIPC_HTONL(x)

#endif

// Lock abstraction (we can't use the same lock implementation on LS and RS since we really want a Crst on the
// LS and this isn't available in the RS environment).
class DbgTransportLock
{
public:
    void Init();
    void Destroy();
    void Enter();
    void Leave();

private:
#ifdef RIGHT_SIDE_COMPILE
    CRITICAL_SECTION    m_sLock;
#else // RIGHT_SIDE_COMPILE
    CrstExplicitInit    m_sLock;
#endif // RIGHT_SIDE_COMPILE
};

// The transport has only one queue for IPC events, but each IPC event can be marked as one of two types.
// The transport will signal the handle corresponding to the type of each IPC event.  (See
// code:DbgTransportSession::GetIPCEventReadyEvent and code:DbgTransportSession::GetDebugEventReadyEvent.)
// This is effectively a basic multiplexing scheme.  The old-style IPC event are for all RS-to-LS IPC events
// and for all LS-to-RS replies.  The other type is for LS-to-RS IPC events transported over the native
// pipeline.  For more information, see the comments for the interface code:IEventChannel.
enum IPCEventType
{
   IPCET_OldStyle,
   IPCET_DebugEvent,
   IPCET_Max,
};

// The class that encapsulates all the state for a single session on either the right or left side. The left
// side supports only one instance of this class for a given runtime. The right side can support several (all
// connected to different LS instances of course).
class DbgTransportSession
{
public:
    // No real work done in the constructor. Use Init() instead.
    DbgTransportSession();

    // Cleanup what is allocated/created in Init()
    ~DbgTransportSession();

    // Allocates initial resources (including starting the transport thread). The session will start in the
    // SS_Opening state. That is, the RS will immediately start trying to Connect() a connection while the
    // LS will perform an Accept() to wait for a connection request. The RS needs an IP address and port
    // number to initiate connections. These should be given in host byte order. The LS, on the other hand,
    // requires the addresses of a couple of runtime data structures to service certain debugger requests that
    // may be delivered once the session is established.
#ifdef RIGHT_SIDE_COMPILE
    HRESULT Init(const ProcessDescriptor& pd, HANDLE hProcessExited);
#else
    HRESULT Init(DebuggerIPCControlBlock * pDCB, AppDomainEnumerationIPCBlock * pADB);
#endif // RIGHT_SIDE_COMPILE

    // Drive the session to the SS_Closed state, which will deallocate all remaining transport resources
    // (including terminating the transport thread). If this is the RS and the session state is SS_Open at the
    // time of this call a graceful disconnect will be attempted (which tells the LS to go back to SS_Opening
    // to look for a new RS rather than interpreting the disconnection as a temporary error and going into
    // SS_Resync). On either side the session will no longer be functional after this call returns (though
    // Init() may be called again to start over from the beginning).
    void Shutdown();

#ifdef RIGHT_SIDE_COMPILE
    // Used by debugger side (RS) to cleanup the target (LS) named pipes
    // and semaphores when the debugger detects the debuggee process  exited.
    void CleanupTargetProcess();
#else
    // Cleans up the named pipe connection so no tmp files are left behind. Does only
    // the minimum and must be safe to call at any time. Called during PAL ExitProcess,
    // TerminateProcess and for unhandled native exceptions and asserts.
    void AbortConnection();
#endif // RIGHT_SIDE_COMPILE

    LONG AddRef()
    {
        LONG ref = InterlockedIncrement(&m_ref);
        return ref;
    }

    LONG Release()
    {
        _ASSERTE(m_ref > 0);
        LONG ref = InterlockedDecrement(&m_ref);
        if (ref == 0)
        {
            delete this;
        }
        return ref;
    }

#ifndef RIGHT_SIDE_COMPILE
    // API used only by the LS to drive the transport into a state where it won't accept connections. This is
    // used when no proxy is detected at startup but it's too late to shutdown all of the debugging system
    // easily. It's mainly paranoia to increase the protection of your system when the proxy isn't started.
    void Neuter();
#endif // !RIGHT_SIDE_COMPILE

#ifdef RIGHT_SIDE_COMPILE
    // On the RS it may be useful to wait and see if the session can reach the SS_Open state. If the target
    // runtime has terminated for some reason then we'll never reach the open state. So the method below gives
    // the RS a way to try and establish a connection for a reasonable amount of time and to time out
    // otherwise. They could then call Shutdown on the session and report an error back to the rest of the
    // debugger. The method returns true if the session opened within the time given (in milliseconds) and
    // false otherwise.
    bool WaitForSessionToOpen(DWORD dwTimeout);

    // A valid ticket is returned if no other client is currently acting as the debugger.
    bool UseAsDebugger(DebugTicket * pTicket);

    // A valid ticket is required in order for this function to succeed.  After this function succeeds,
    // another client can request to be the debugger.
    bool StopUsingAsDebugger(DebugTicket * pTicket);
#endif // RIGHT_SIDE_COMPILE

    // Sends a pre-initialized event to the other side.
    HRESULT SendEvent(DebuggerIPCEvent * pEvent);
    HRESULT SendDebugEvent(DebuggerIPCEvent * pEvent);

    // Retrieves the auto-reset handle which is signalled by the session each time a new event is received
    // from the other side.
    HANDLE GetIPCEventReadyEvent();
    HANDLE GetDebugEventReadyEvent();

    // Copies the last event received from the other side into the provided buffer. This should only be called
    // (once) after the event returned from GetIPCEventReadyEvent()/GetDebugEventReadyEvent() has been signalled.
    void GetNextEvent(DebuggerIPCEvent *pEvent, DWORD cbEvent);

#ifdef RIGHT_SIDE_COMPILE
    // Read and write memory on the LS from the RS.
    HRESULT ReadMemory(PBYTE pbRemoteAddress, PBYTE pbBuffer, SIZE_T cbBuffer);
    HRESULT WriteMemory(PBYTE pbRemoteAddress, PBYTE pbBuffer, SIZE_T cbBuffer);
    HRESULT VirtualUnwind(DWORD threadId, ULONG32 contextSize, PBYTE context);

    // Read and write the debugger control block on the LS from the RS.
    HRESULT GetDCB(DebuggerIPCControlBlock *pDCB);
    HRESULT SetDCB(DebuggerIPCControlBlock *pDCB);

    // Read the AppDomain control block on the LS from the RS.
    HRESULT GetAppDomainCB(AppDomainEnumerationIPCBlock *pADB);

#endif // RIGHT_SIDE_COMPILE

private:

    // Highest protocol version supported by this side of the session. See the
    // m_dwMajorVersion/m_dwMinorVersion fields for a detailed explanation and the actual version being used
    // by the session (if it is formed).
    static const DWORD kCurrentMajorVersion = 2;
    static const DWORD kCurrentMinorVersion = 0;

    // Session states. These determine which action is taken on a SendMessage (message is sent, queued or an
    // error is raised) and which incoming messages are valid.
    enum SessionState
    {
        SS_Closed,      // No session and no attempt is being made to form one
        SS_Opening_NC,  // Session is being formed but no connection is established yet
        SS_Opening,     // Session is being formed, the low level connection is in place
        SS_Open,        // Session is fully formed and normal transport messages can be sent and received
        SS_Resync_NC,   // A low level connection error is occurred and we're attempting to re-form the link
        SS_Resync,      // We're trying to resynchronize high level state over the new connection
    };

    // Types of messages that can be sent over the transport connection.
    enum MessageType
    {
        // Session management operations. These must come first and MT_SessionClose must be last in the group.
        MT_SessionRequest,  // RS -> LS  : Request a new session be formed (optionally pass encrypted data key)
        MT_SessionAccept,   // LS -> RS  : Accept new session
        MT_SessionReject,   // LS -> RS  : Reject new session, give reason
        MT_SessionResync,   // RS <-> LS : Resync broken connection by informing other side which messages must be resent
        MT_SessionClose,    // RS -> LS  : Gracefully terminate a session

        // Debugger events.
        MT_Event,           // RS <-> LS : A debugger event is being sent as the data block of the message

        // Misc management operations.
        MT_ReadMemory,      // RS <-> LS : RS wants to read LS memory block (or LS is replying to such a request)
        MT_WriteMemory,     // RS <-> LS : RS wants to write LS memory block (or LS is replying to such a request)
        MT_VirtualUnwind,   // RS <-> LS : RS wants to LS unwind a stack frame (or LS is replying to such a request)
        MT_GetDCB,          // RS <-> LS : RS wants to read LS DCB (or LS is replying to such a request)
        MT_SetDCB,          // RS <-> LS : RS wants to write LS DCB (or LS is replying to such a request)
        MT_GetAppDomainCB,  // RS <-> LS : RS wants to read LS AppDomainCB (or LS is replying to such a request)
    };

    // Reasons the LS can give for rejecting a session. These codes should *not* be changed other than by
    // adding reasons to keep versioning possible.
    enum RejectReason
    {
        RR_IncompatibleVersion,     // LS doesn't support the major version asked for in the request.
        RR_AlreadyAttached,         // LS already has another session open (LS only supports one session at a time)
    };

    // Struct that defines the format of a message header block sent on the connection. Note that the size of
    // this structure and the location/size of the m_eType field must *never* change to allow our versioning
    // protocol to work properly (in particular any LS must be able to interpret at least the type and version
    // number of an MT_SessionRequest and reply with a MT_SessionReject that any RS can interpret the type and
    // version of). To help with this there is a padding field at the end for future expansion (this should be
    // initialized to zero and not accessed in any other manner).
    struct MessageHeader
    {
        Portable<MessageType>   m_eType;        // Type of message this is
        Portable<DWORD>         m_cbDataBlock;  // Size of data block that immediately follows this header (can be zero)
        Portable<DWORD>         m_dwId;         // Message ID assigned by the sender of this message
        Portable<DWORD>         m_dwReplyId;    // Message ID that this is a reply to (used by messages such as MT_GetDCB)
        Portable<DWORD>         m_dwLastSeenId; // Message ID last seen by sender (receiver can discard up to here from send queue)
        Portable<DWORD>         m_dwReserved;   // Reserved for future expansion (must be initialized to zero and
                                                // never read)

        // The rest of the header varies depending on the message type (keep the maximum size of this union
        // small since all messages will pay the overhead, large message type specific data should go in the
        // following data block).
        union
        {
            // Used by MT_SessionRequest / MT_SessionAccept.
            struct
            {
                Portable<DWORD>         m_dwMajorVersion;   // Protocol version requested/accepted
                Portable<DWORD>         m_dwMinorVersion;
            } VersionInfo;

            // Used by MT_SessionReject.
            struct
            {
                Portable<RejectReason>  m_eReason;          // Reason for rejection.
                Portable<DWORD>         m_dwMajorVersion;   // Highest protocol version the LS supports
                Portable<DWORD>         m_dwMinorVersion;
            } SessionReject;

            // Used by MT_ReadMemory and MT_WriteMemory.
            struct
            {
                Portable<PBYTE>         m_pbLeftSideBuffer; // Address of memory to read/write on the LS
                Portable<DWORD>         m_cbLeftSideBuffer; // Size in bytes of memory to read/write
                Portable<HRESULT>       m_hrResult;         // Result from LS (access can fail due to unmapped memory etc.)
            } MemoryAccess;

            // Used by MT_Event.
            struct
            {
                Portable<IPCEventType>  m_eIPCEventType;    // multiplexing type of this IPC event
                Portable<DWORD>         m_eType;            // Event type (useful for debugging)
            } Event;

        } TypeSpecificData;

        BYTE                    m_sMustBeZero[8];   // Set this to zero when initializing and never read the contents
    };

    // Struct defining the format of the data block sent with a SessionRequest.
    struct SessionRequestData
    {
        GUID            m_sSessionID;   // Unique session ID. Treated as byte blob so no endian-ness
    };

    // Struct used to track a message that is being (or will soon be) sent but has not yet been acknowledged.
    // These are usually found queued on the send queue.
    struct Message
    {
        Message        *m_pNext;         // Next message in the queue
        MessageHeader   m_sHeader;       // Inline message header
        PBYTE           m_pbDataBlock;   // Pointer to optional message data block (or NULL)
        DWORD           m_cbDataBlock;   // Count of bytes in above block if it's non-NULL
        HANDLE          m_hReplyEvent;   // Optional event to signal if this message is replied to (or NULL)
        PBYTE           m_pbReplyBlock;  // Optional buffer to place data block from reply into (or NULL)
        DWORD           m_cbReplyBlock;  // Size in bytes of the above buffer if it is non-NULL
        Message        *m_pOrigMessage;  // Used when we need to find the original message from a copy
        bool            m_fAborted;      // True if this send was aborted due to session shutdown

        // Common initialization for messages.
        void Init(MessageType   eType,
                  PBYTE         pbBufferIn = NULL,
                  DWORD         cbBufferIn = 0,
                  PBYTE         pbBufferOut = NULL,
                  DWORD         cbBufferOut = 0)
        {
            memset(this, 0, sizeof(*this));
            m_sHeader.m_eType = eType;
            m_sHeader.m_cbDataBlock = cbBufferIn;
            m_pbDataBlock = pbBufferIn;
            m_cbDataBlock = cbBufferIn;
            m_pbReplyBlock = pbBufferOut;
            m_cbReplyBlock = cbBufferOut;
        }
    };

    // Holder class used to take a transport lock in a given scope and automatically release it once that
    // scope is exited.
    class TransportLockHolder
    {
    public:
        TransportLockHolder(DbgTransportLock *pLock)
        {
            m_pLock = pLock;
            m_pLock->Enter();
        }

        ~TransportLockHolder()
        {
            m_pLock->Leave();
        }

    private:
        DbgTransportLock   *m_pLock;
    };

#ifdef _DEBUG
    // Store statistics for various session activities that will be useful for performance analysis and tracking
    // down bugs.
    struct DbgStats
    {
        // Message type counts for sends.
        LONG        m_cSentSessionRequest;
        LONG        m_cSentSessionAccept;
        LONG        m_cSentSessionReject;
        LONG        m_cSentSessionResync;
        LONG        m_cSentSessionClose;
        LONG        m_cSentEvent;
        LONG        m_cSentReadMemory;
        LONG        m_cSentWriteMemory;
        LONG        m_cSentVirtualUnwind;
        LONG        m_cSentGetDCB;
        LONG        m_cSentSetDCB;
        LONG        m_cSentGetAppDomainCB;
        LONG        m_cSentDDMessage;

        // Message type counts for receives.
        LONG        m_cReceivedSessionRequest;
        LONG        m_cReceivedSessionAccept;
        LONG        m_cReceivedSessionReject;
        LONG        m_cReceivedSessionResync;
        LONG        m_cReceivedSessionClose;
        LONG        m_cReceivedEvent;
        LONG        m_cReceivedReadMemory;
        LONG        m_cReceivedWriteMemory;
        LONG        m_cReceivedVirtualUnwind;
        LONG        m_cReceivedGetDCB;
        LONG        m_cReceivedSetDCB;
        LONG        m_cReceivedGetAppDomainCB;
        LONG        m_cReceivedDDMessage;

        // Low level block counts.
        LONG        m_cSentBlocks;
        LONG        m_cReceivedBlocks;

        // Byte count summaries.
        LONGLONG    m_cbSentBytes;
        LONGLONG    m_cbReceivedBytes;

        // Errors and recovery
        LONG        m_cSendErrors;
        LONG        m_cReceiveErrors;
        LONG        m_cMiscErrors;
        LONG        m_cConnections;
        LONG        m_cResends;

        // Session counts.
        LONG        m_cSessions;
    };

    DbgStats        m_sStats;

    // Macros to update the statistics. The increment version is thread safe, but the add version is assumed to be
    // externally serialized since the 64-bit Interlocked operations are not available on all platforms and these
    // stats are used for send and receive byte counts which are updated at locations that are serialized anyway.
#define DBG_TRANSPORT_INC_STAT(_name) InterlockedIncrement(&m_sStats.m_c##_name)
#define DBG_TRANSPORT_ADD_STAT(_name, _amount) m_sStats.m_cb##_name += (_amount)

#else // _DEBUG

#define DBG_TRANSPORT_INC_STAT(_name)
#define DBG_TRANSPORT_ADD_STAT(_name, _amount)

#endif // _DEBUG

    // Reference count
    LONG m_ref;

    // Some flags used to record how far we got in Init() (used for cleanup in Shutdown()).
    bool m_fInitStateLock;
#ifndef RIGHT_SIDE_COMPILE
    bool m_fInitWSA;
#endif // !RIGHT_SIDE_COMPILE

    // Protocol version. This consists of two parts. The major version is incremented on incompatible protocol
    // updates. That is, a session between left and right sides that cannot use a protocol with the exact same
    // major version cannot be formed. The minor version number is incremented on compatible protocol updates.
    // These are usually associated with optional extensions to the protocol (e.g. a V1.2 endpoint might set
    // previously unused fields in a message header to indicate some optional hint about the message that a
    // V1.1 client won't notice at all).
    //
    // The right side has a hard-coded version number it sends in the SessionRequest message. The left side
    // must support the same major version or reply with a SessionReject message containing the highest
    // version it does support. For this reason the format of a SessionReject message can never change at all.
    // On a SessionAccept the left side sends back the version number and can choose to lower the minor
    // version to the highest it knows about. This gives the right side a hint as to the capabilities of the
    // left side (though it must be prepared to interact with a left side with any minor version number).
    //
    // If necessary (and the SessionReject message sent by an incompatible left side indicates a major version
    // the right side can also support), the right side can re-attempt a SessionRequest with a lower major
    // version.
    DWORD           m_dwMajorVersion;
    DWORD           m_dwMinorVersion;

    // Session ID randomly allocated by the right side and sent over in the SessionRequest message. This
    // serves to disambiguate a re-send of the SessionRequest due to a network error versus a SessionRequest
    // from a different debugger.
    GUID            m_sSessionID;

    // Lock used to synchronize sending messages and updating the session state. This ensures message bytes
    // don't become interleaved on the transport connection, the send queue is updated consistently across
    // multiple threads and that we never attempt to use a connection that is being deallocated on another
    // thread due to a state change. Receives don't need this since they're performed only on the transport
    // thread (which is also the only thread allowed to deallocate the connection).
    DbgTransportLock m_sStateLock;

    // Queue of messages that have been sent over the connection but not acknowledged yet or are waiting to be
    // sent (because another message is using the connection or we're in a SessionResync state). You must hold
    // m_sStateLock in order to access this queue.
    Message        *m_pSendQueueFirst;
    Message        *m_pSendQueueLast;

    // Message IDs. These are monotonically increasing numbers starting from 0 that are used to stamp each
    // non-session management message sent on this session. If a low-level network error occurs and we must
    // abandon and re-form the underlying transport connection the left and right sides send SessionResync
    // messages with the ID of the last message they received (and processed). This allows us to determine
    // which messages we still have in our send queue must be re-sent over the new transport connection.
    // Allocate a new message ID by post incrementing m_dwNextMessageId under the state lock.
    DWORD           m_dwNextMessageId;      // Next ID we'll give to a message we're sending
    DWORD           m_dwLastMessageIdSeen;  // Last ID we saw in an incoming, fully received message

    // The current session state. This is updated atomically under m_sStateLock.
    SessionState    m_eState;

#ifdef RIGHT_SIDE_COMPILE
    // Manual reset event that is signalled whenever the session state is SS_Open or SS_Closed (after waiting
    // on this event the caller should check to see which state it was).
    HANDLE          m_hSessionOpenEvent;
#endif // RIGHT_SIDE_COMPILE

    // Thread responsible for initial Connect()/Accept() on a low level transport connection and
    // subsequently for all message reception on that connection. Any error will cause the thread to reset
    // back into the Connect()/Accept() phase (along with the resulting session state change).
    HANDLE          m_hTransportThread;

    TwoWayPipe      m_pipe;

#ifdef RIGHT_SIDE_COMPILE
    // On the RS the transport thread needs to know the IP address and port number to Connect() to.
    ProcessDescriptor m_pd;                  // Descriptor of a process we're talking to.

    HANDLE            m_hProcessExited;       // event which will be signaled when the debuggee is terminated

    bool              m_fDebuggerAttached;
#endif

    // Debugger event handling. To improve performance we allow the debugger to send as many events as it
    // likes without acknowledgement from its peer. While not strictly adhering to the semantic provided by
    // the shared memory buffer transport (where the buffer could not be written again until the receiver had
    // explicitly released it) it turns out that no debugging code relies on this. In particular, the most
    // common scenario where this makes sense is the left side sending large scale update events (such as the
    // groups of appdomain create, module load etc. events sent during an attach). Here the right hand side
    // queues the events for later processing and releases the buffers right away.
    // We gain performance since its no longer necessary to send (or wait on) event acknowledgment messages.
    // This lowers both network bandwidth and latency (especially when one side is trying to send a continuous
    // stream of events).
    // From the transport standpoint this design mainly impacts event receipt. We maintain a dynamically sized
    // pool of event receipt buffers (the size is determined by the maximum number of unread events we've seen
    // at any one time). The buffer is a circular array: clients read from the buffer at head index which is
    // followed by some number of valid buffers (wrapping around to the start of the array if necessary). New
    // events are added after these (and grow the array if the tail would touch the head otherwise).
    DbgEventBufferEntry * m_pEventBuffers;                  // Pointer to array of incoming debugger events
    DWORD           m_cEventBuffers;                        // Size of the array above (in events)
    DWORD           m_cValidEventBuffers;                   // Number of events that actually contain data
    DWORD           m_idxEventBufferHead;                   // Index of the first valid event
    DWORD           m_idxEventBufferTail;                   // Index of the first invalid event
    HANDLE          m_rghEventReadyEvent[IPCET_Max];        // The event signalled when a new event arrives

#ifndef RIGHT_SIDE_COMPILE
    // The LS requires the addresses of a couple of runtime data structures in order to service MT_GetDCB etc.
    // These are provided by the runtime at initialization time.
    DebuggerIPCControlBlock *m_pDCB;
    AppDomainEnumerationIPCBlock *m_pADB;
#endif // !RIGHT_SIDE_COMPILE

    HRESULT SendEventWorker(DebuggerIPCEvent * pEvent, IPCEventType type);

    // Sends a pre-formatted message (including the data block, if any). The fWaitsForReply indicates whether
    // the caller is going to block until some sort of reply message is received (for instance an event that
    // must be ack'd or a request such as MT_GetDCB that needs a reply). SendMessage() uses this to determine
    // whether it needs to buffer the message before placing it on the send queue (since it may need to resend
    // the message after a transitory network failure).
    HRESULT SendMessage(Message *pMessage, bool fWaitsForReply);

    // Helper method for sending messages requiring a reply (such as MT_GetDCB) and waiting on the result.
    HRESULT SendRequestMessageAndWait(Message *pMessage);

    // Sends a single contiguous buffer of host memory over the connection. The caller is responsible for
    // holding the state lock and ensuring the session state is SS_Open. Returns false if the send failed (the
    // error will have already caused the recovery logic to kick in, so handling it is not required, the
    // boolean is just returned so that any further blocks in the message are not sent).
    bool SendBlock(PBYTE pbBuffer, DWORD cbBuffer);

    // Receives a single contiguous buffer of host memory over the connection. No state lock needs to be
    // held (receives are serialized by the fact they're only performed on the transport thread). Returns
    // false if a network error is encountered (which will automatically transition the session into the
    // correct retry state).
    bool ReceiveBlock(PBYTE pbBuffer, DWORD cbBuffer);

    // Called upon encountering a network error (e.g. an error from Send() or Receive()). This handles pushing
    // the session state into SS_Resync_NC in order to start the recovery process.
    void HandleNetworkError(bool fCallerHoldsStateLock);

    // Scan the send queue and discard any messages which have been processed by the other side according to
    // the specified ID). Messages waiting on a reply message (e.g. MT_GetDCB) will be retained until that
    // reply is processed. FlushSendQueue will take the state lock.
    void FlushSendQueue(DWORD dwLastProcessedId);

#ifdef RIGHT_SIDE_COMPILE
    // Perform processing required to complete a request (such as MT_GetDCB) once a reply comes in. This
    // includes reading data from the connection into the output buffer, removing the original message from
    // the send queue and signalling the completion event. Returns true if no network error was encountered.
    bool ProcessReply(MessageHeader *pHeader);

    // Upon receiving a reply message, signal the event on the message to wake up the thread waiting for
    // the reply message and close the handle to the event.
    void SignalReplyEvent(Message * pMessage);

    // Given a message ID, find the matching message in the send queue.  If there is no match, return NULL.
    // If there is a match, remove the message from the send queue and return it.
    Message * RemoveMessageFromSendQueue(DWORD dwMessageId);
#endif

#ifndef RIGHT_SIDE_COMPILE
    // Check read and optionally write memory access to the specified range of bytes. Used to check
    // ReadProcessMemory and WriteProcessMemory requests.
    HRESULT CheckBufferAccess(PBYTE pbBuffer, DWORD cbBuffer, bool fWriteAccess);
#endif // !RIGHT_SIDE_COMPILE

    // Initialize all session state to correct starting values. Used during Init() and on the LS when we
    // gracefully close one session and prepare for another.
    void InitSessionState();

    // The entry point of the transport worker thread. This one's static, so we immediately dispatch to an
    // instance method version defined below for convenience in the implementation.
    static DWORD WINAPI TransportWorkerStatic(LPVOID pvContext);
    void TransportWorker();

    // Given a fully initialized debugger event structure, return the size of the structure in bytes (this is
    // not trivial since DebuggerIPCEvent contains a large union member which can cause the portion containing
    // significant data to vary wildy from event to event).
    DWORD GetEventSize(DebuggerIPCEvent *pEvent);

#ifdef _DEBUG
    // Debug helper which returns the name associated with a MessageType.
    const char *MessageName(MessageType eType);

    // Debug logging helper which logs an incoming message of any type (as long as logging for that message
    // class is currently enabled).
    void DbgTransportLogMessageReceived(MessageHeader *pHeader);

    // Helper method used by the DBG_TRANSPORT_SHOULD_INJECT_FAULT macro.
    bool DbgTransportShouldInjectFault(DbgTransportFaultOp eOp, const char *szOpName);
#else // _DEBUG
#define DbgTransportLogMessageReceived(x)
#endif // _DEBUG
};

#ifndef RIGHT_SIDE_COMPILE
// The one and only transport instance for the left side. Allocated and initialized during EE startup (from
// Debugger::Startup() in debugger.cpp).
extern DbgTransportSession *g_pDbgTransport;
#endif // !RIGHT_SIDE_COMPILE

#define DBG_GET_LAST_WSA_ERROR() WSAGetLastError()

#endif // defined(FEATURE_DBGIPC_TRANSPORT_VM) || defined(FEATURE_DBGIPC_TRANSPORT_DI)

#endif // __DBG_TRANSPORT_SESSION_INCLUDED
