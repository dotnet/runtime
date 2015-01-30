//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#ifndef __DBG_PROXY_INCLUDED
#define __DBG_PROXY_INCLUDED

//
// This header is used when managed debugging is performed over a network connection rather than via a shared
// IPC memory block. It describes the format of the communication between a debugger or debuggee and the proxy
// service that sits in the middle between the two.
//
// The proxy has its own process on the same machine as the debuggee. It is involved at the start of debugging
// (where it launches processes, enumerates and detects runtime startups for the debugger) and at the end
// (where it handles process termination commands from the debugger or sends process termination events back).
// The interaction with the debuggee is limited to a single message exchange where the debuggee registers its
// runtime with the proxy and the proxy responds with whether or not a debugger is currently waiting to
// attach.
//
// The communication transport is SSL over TCP/IP.
//
// Note that this header uses very basic data types only as it is included from both Win32/PAL code and native
// Mac code.
//

#include <dbgportable.h>

// Constants describing the different platforms the debugger proxy can provide service for.
enum DbgTargetPlatform
{
    DTP_Unknown,    // Not set yet
    DTP_MacPPC,     // Macintosh, PPC 32-bit
    DTP_MacX86,     // Macintosh, X86 32-bit
    DTP_WinARM,     // Windows, ARM 32-bit
    DTP_MacX86_64,  // Macintosh, X86 64-bit
};

// The proxy uses its own ID to track both processes and CoreCLR runtimes within those processes. Both come
// from the same namespace (i.e. a given PRUID uniquely identifies either a process or a runtime).
typedef unsigned int PRUID;

// Codes for the different types of messages the proxy can send or receive. Each message is tagged with one
// of these codes.
enum DbgProxyMessageType
{
    // Messages sent to the proxy from the debugger.
    DPMT_GetSystemInfo,     // Query the remote system type and proxy version, must be first message sent
    DPMT_EnumProcesses,     // List processes and runtimes on the target machine
    DPMT_LaunchProcess,     // Run the given command line on the target machine
    DPMT_EarlyAttach,       // Notify the proxy that the next runtime started on a process is to be debugged
    DPMT_TerminateProcess,  // Terminate the specified process

    // Messages sent to the debugger from the proxy. Some are responses to requests from above and some are
    // asynchronous events.
    DPMT_SystemInfo,        // Reply to GetSystemInfo
    DPMT_ProcessList,       // Reply to EnumProcesses
    DPMT_ProcessLaunched,   // Reply to LaunchProcess, indicates success status and PID/PRUID of new process
    DPMT_RuntimeStarted,    // Event delivered every time a runtime registers for startup with the proxy
    DPMT_ProcessTerminated, // Event delivered every time a process terminates
    DPMT_EarlyAttachDone,   // Reply to EarlyAttach

    // The only message sent from the debuggee to the proxy. The debuggee expects a RuntimeRegistered in
    // reply.
    DPMT_RegisterRuntime,   // Tell the proxy a new runtime has started and provide useful info (port etc.)

    // The only message sent from the proxy to the debugee.
    DPMT_RuntimeRegistered, // Acknowledge registration, inform debuggee whether it should wait for debugger attach
};

// Status code returned in ProcessLaunched messages. Indicates whether the process creation succeeded or not.
enum DbgProxyLaunchResult
{
    DPLR_Success,           // The process was launched successfully
    DPLR_OutOfMemory,       // The request failed due to lack of resources
    DPLR_Denied,            // The request failed for security reasons
    DPLR_NotFound,          // The specified application cannot be found.
    DPLR_UnspecifiedError,  // The request failed for unspecified reasons (catchall)
};

// The current protocol used by the proxy. Major versions increment for incompatible protocol updates, minor
// versions for any other changes. Proxies (or their clients) will discard messages with a major protocol they
// don't handle, but must handle any minor version within a supported major protocol version.
#define kCurrentMajorVersion    2
#define kCurrentMinorVersion    0

// To enable a more flexible protocol we define the concept of an attribute block. This is a variable sized
// record consisting of a header containing the overall length followed by zero or more records. Each record
// has a tag (an enum value) and a variable sized binary blob value. The attribute block does not assign any
// semantic to this value. Each record is padded to ensure that the next record (or the overall size of the
// attribute block) is aligned on a 4 byte boundary. Value sizes are limited to 16-bit values (i.e. <= 65535
// bytes).

// Valid tag values for attribute blocks. More may be defined in the future with minimal impact on current
// clients. Only one instance of a given tag should be present in a single block (the behavior is undefined
// otherwise).
enum DbgAttributeTag
{
    DAT_CommandLine,        // Value is a UTF8 string containing a shell command and its arguments (whitespace separated)
    DAT_DefaultDirectory,   // Value is a UTF8 string containing the default directory to lauch a process under
    DAT_Environment         // Value is a sequence of UTF8 strings containing an environment block
};

// The header present at the start of every attribute block.
struct DbgAttributeBlockHeader
{
    unsigned int    m_cbBlockSize;  // Count of bytes in the block (including this header)
};

// Helper class used to calculate the size of and then format an attribute block. First tell it the sizes of
// all the values that will be stored, then ask for the total space needed, allocate a buffer large enough and
// go back and fill in all the details.
class DbgAttributeBlockWriter
{
public:
    DbgAttributeBlockWriter()
    {
        m_cbBuffer = sizeof(DbgAttributeBlockHeader);
        m_pbBuffer = NULL;
        m_pbNextEntry = NULL;
    }

    // Plan for adding a specific string value to the block.
    void ScheduleStringValue(LPCSTR szValue)
    {
        ScheduleValue((unsigned int)(strlen(szValue) + 1));
    }

    // Plan for adding a specific binary blob value to the block.
    void ScheduleValue(unsigned int cbValue)
    {
        _PASSERT(cbValue <= 0xffff);
        _PASSERT(m_pbBuffer == NULL);

        m_cbBuffer += sizeof(unsigned short);   // Space for the record tag
        m_cbBuffer += sizeof(unsigned short);   // Space for the value length
        m_cbBuffer += (cbValue + 3) & ~3;       // Account for value size rounded up to nearest multiple of 4
    }

    // Get the total size of buffer required to hold a block with the values planned so far.
    unsigned int GetRequiredBufferSize()
    {
        return m_cbBuffer;
    }

    // Tell the writer where the buffer space for the block has been allocated and prepare to start
    // initializing records.
    void BeginFormatting(__inout_bcount(m_cbBuffer) char *pbBuffer)
    {
        _PASSERT(m_pbBuffer == NULL);
        m_pbBuffer = pbBuffer;
        m_pbNextEntry = m_pbBuffer;

        // Write the block header (just contains the total size of the block).
        *(unsigned int*)m_pbNextEntry = m_cbBuffer;
        m_pbNextEntry += sizeof(unsigned int);
    }

    // Add a string value to the block.
    void AddStringValue(DbgAttributeTag eTag, LPCSTR szValue)
    {
        AddValue(eTag, szValue, (unsigned int)(strlen(szValue) + 1));
    }

    // Add a binary blob value to the block.
    void AddValue(DbgAttributeTag eTag, __in_bcount(cbValue) const char *pbValue, unsigned int cbValue)
    {
        _PASSERT(cbValue <= 0xffff);
        _PASSERT(m_pbBuffer != NULL);
        _PASSERT(((unsigned int)(size_t)m_pbNextEntry & 3) == 0);

        // Write the record tag.
        *(unsigned short*)m_pbNextEntry = eTag;
        m_pbNextEntry += sizeof(unsigned short);

        // Write the value size.
        *(unsigned short*)m_pbNextEntry = cbValue;
        m_pbNextEntry += sizeof(unsigned short);

        // Copy the value over.
        memcpy(m_pbNextEntry, pbValue, cbValue);
        m_pbNextEntry += cbValue;

        // Calculate whether we need any padding to make the record a multiple of 4 bytes long.
        unsigned int cbPadding = 3 - ((cbValue + 3) & 3);
        if (cbPadding)
        {
            // We need padding, fill with zeroes.
            memset(m_pbNextEntry, 0, cbPadding);
            m_pbNextEntry += cbPadding;
        }

        _PASSERT(m_pbNextEntry <= (m_pbBuffer + m_cbBuffer));
    }

private:
    unsigned int    m_cbBuffer;     // The bytes needed to represent the block so far
    char           *m_pbBuffer;     // The buffer allocated by the caller to hold the block (NULL during planning)
    char           *m_pbNextEntry;  // Current location as we format the block (NULL during planning)
};

// Helper class to interpret an initialized attribute block.
class DbgAttributeBlockReader
{
public:
    DbgAttributeBlockReader(__in_bcount(*(unsigned int*)pbBuffer) char *pbBuffer)
    {
        m_pbBuffer = pbBuffer;
    }

    // Get the total size of the block in bytes.
    unsigned int GetBlockSize()
    {
        _PASSERT(*(unsigned int*)m_pbBuffer >= sizeof(unsigned int));

        return *(unsigned int*)m_pbBuffer;
    }

    // Locate a value with the given tag. If none exists NULL is returned. Otherwise a pointer to the start of
    // the value in the block is returned (along with, optionally, the length of the value).
    char *FindValue(DbgAttributeTag eTag, unsigned int *pcbValue = NULL)
    {
        unsigned int    cbBuffer = GetBlockSize() - sizeof(unsigned int);
        char           *pbEntry = m_pbBuffer + sizeof(unsigned int);

        // Step through the block while there are still records to parse.
        while (cbBuffer)
        {
            // Read the tag from the record and step over it.
            DbgAttributeTag eEntryTag = (DbgAttributeTag)*(unsigned short*)pbEntry;
            pbEntry += sizeof(unsigned short);

            // Read the value size from the record and step over it.
            unsigned int cbValue = *(unsigned short*)pbEntry;
            pbEntry += sizeof(unsigned short);

            // If the tags match then return the pointer (which is currently at the start of the value in the
            // record).
            if (eEntryTag == eTag)
            {
                if (pcbValue)
                    *pcbValue = cbValue;
                return pbEntry;
            }

            // Calculate the size of any padding this record has.
            unsigned int cbPadding = 3 - ((cbValue + 3) & 3);

            // Step over the value and padding to the start of the next record.
            pbEntry += cbValue + cbPadding;

            _PASSERT(cbBuffer >= (sizeof(unsigned short) + sizeof(unsigned short) + cbValue + cbPadding));
            cbBuffer -= sizeof(unsigned short) + sizeof(unsigned short) + cbValue + cbPadding;
        }

        // If we get here we ran out of records, so no record with the given tag is present.
        if (pcbValue)
            *pcbValue = 0;
        return NULL;
    }

private:
    char           *m_pbBuffer;
};

// Maximum number of bytes of process command line that will be returned in a DbgProxyProcessInfo.
#define kMaxCommandLine 256

// Structure reported in ProcessList messages that describes a single process on the target machine.
struct DbgProxyProcessInfo
{
    Portable<unsigned>  m_uiPID;        // Host system PID of the process
    Portable<PRUID>     m_pruidProcess; // Proxy allocated PRUID for the process
    char                m_szCommandLine[kMaxCommandLine]; // Command and arguments the process is running
};

// Structure reported in ProcessList messages that describes a single CoreCLR runtime on the target machine.
struct DbgProxyRuntimeInfo
{
    Portable<PRUID>     m_pruidProcess; // PRUID of the host process
    Portable<PRUID>     m_pruidRuntime; // PRUID assigned by the proxy to this runtime
    Portable<unsigned short> m_usPort;  // Port number the runtime listens for debuggers on (in network byte order)
};

// This structure describes the message header common to every proxy message.
struct DbgProxyMessageHeader
{
    Portable<DbgProxyMessageType> m_eType;  // Describes the type of this message
    Portable<unsigned>  m_uiRequestID;      // Unique ID that links request/reply message pair
    Portable<unsigned>  m_uiMagic;          // Redundant field used to make random data attacks harder,
                                            // contains ~(m_eType + m_uiRequestID)
    Portable<unsigned>  m_uiReserved;       // Reserved for future expansion (must be initialized to zero and
                                            // never read)

    // Some message types require little or no additional data. For such types the extra fields are defined
    // inline in the header in the union below. More substantial data is sent as a seperate buffer.
    union _VariantData
    {
        struct _GetSystemInfo
        {
            Portable<unsigned>  m_uiMajorVersion;   // Major protocol being employed by the debugger
            Portable<unsigned>  m_uiMinorVersion;   // Minor version of the protocol above
        } GetSystemInfo;

        struct _LaunchProcess
        {
            Portable<unsigned>  m_cbAttributeBlock; // Count of bytes in following attribute block
        } LaunchProcess;

        struct _EarlyAttach
        {
            Portable<PRUID>     m_pruidProcess;     // PRUID of process whose next runtime will be debugged
        } EarlyAttach;

        struct _TerminateProcess
        {
            Portable<PRUID>     m_pruidProcess;     // PRUID of process to terminate
        } TerminateProcess;

        struct _SystemInfo
        {
            Portable<unsigned>  m_uiMajorVersion;   // Major protocol being employed by the proxy
            Portable<unsigned>  m_uiMinorVersion;   // Minor version of the protocol above
            Portable<DbgTargetPlatform> m_ePlatform;// Platform code of target machine
        } SystemInfo;

        struct _ProcessList
        {
            Portable<unsigned>  m_uiProcessRecords; // Total number of process records following
            Portable<unsigned>  m_uiRuntimeRecords; // Total number of runtime records following
        } ProcessList;

        struct _ProcessLaunched
        {
            Portable<DbgProxyLaunchResult> m_eResult;// Whether the launch succeeded and if not, why not
            Portable<unsigned>  m_uiPID;            // PID of new process
            Portable<PRUID>     m_pruidProcess;     // PRUID of new process
        } ProcessLaunched;

        struct _ProcessTerminated
        {
            Portable<PRUID>     m_pruidProcess;     // PRUID of process which has exited
        } ProcessTerminated;

        struct _EarlyAttachDone
        {
            Portable<bool>      m_fProcessExited;   // True if the early attach was on a process which terminated
        } EarlyAttachDone;

        struct _RuntimeRegistered
        {
            Portable<unsigned>  m_uiMajorVersion;   // Major protocol being employed by the proxy
            Portable<unsigned>  m_uiMinorVersion;   // Minor version of the protocol above
            Portable<bool>      m_fWaitForDebuggerAttach; // True if the debuggee should suspend startup and wait
                                                      // for debugger attach
        } RuntimeRegistered;
    } VariantData;
};

// Calculate the expected value of the m_uiMagic field given a message header.
#define DBGPROXY_MAGIC_VALUE(_header) (~(((unsigned int)(_header)->m_eType) + (_header)->m_uiRequestID))

// Some messages require more data than will comfortably fit in a message header. Define aggregate structures
// for these.

struct DbgProxyLaunchProcessMessage
{
    DbgProxyMessageHeader   m_sHeader;
    DbgAttributeBlockHeader m_sLaunchAttributes;                // Variable sized attribute block with launch
                                                                // arguments
};

struct DbgProxyRuntimeStartedMessage
{
    DbgProxyMessageHeader   m_sHeader;
    DbgProxyProcessInfo     m_sProcessInfo;     // In case the process is new as well
    DbgProxyRuntimeInfo     m_sRuntimeInfo;
};

struct DbgProxyRegisterRuntimeMessage
{
    DbgProxyMessageHeader   m_sHeader;
    Portable<unsigned>      m_uiMajorVersion;   // Major protocol being employed by the debuggee
    Portable<unsigned>      m_uiMinorVersion;   // Minor version of the protocol above
    Portable<unsigned>      m_uiPID;            // PID of process hosting this runtime
    Portable<unsigned short> m_usPort;          // Port that runtime waits for debugger on (host byte order)
};

#endif // !__DBG_PROXY_INCLUDED
