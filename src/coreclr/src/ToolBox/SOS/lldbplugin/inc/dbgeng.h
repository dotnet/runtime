// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//----------------------------------------------------------------------------
//
// Debugger engine interface subset implemented with LLDB APIs.
//
//----------------------------------------------------------------------------

#ifndef __DBGENG_H__
#define __DBGENG_H__

#include <cstdarg>

#ifdef __cplusplus
extern "C" {
#endif

//----------------------------------------------------------------------------
// IDebugControl2
//----------------------------------------------------------------------------

class IDebugControl2
{
public:
    // Checks for a user interrupt, such a Ctrl-C
    // or stop button.
    // This method is reentrant.
    virtual HRESULT GetInterrupt(
        void) = 0;

    // Sends output through clients
    // output callbacks if the mask is allowed
    // by the current output control mask and
    // according to the output distribution
    // settings.
    virtual HRESULT Output(
        ULONG mask,
        PCSTR format, 
        ...) = 0;

    virtual HRESULT OutputVaList(
        ULONG mask,
        PCSTR format,
        va_list args) = 0;

    // The following methods allow direct control
    // over the distribution of the given output
    // for situations where something other than
    // the default is desired.  These methods require
    // extra work in the engine so they should
    // only be used when necessary.
    virtual HRESULT ControlledOutput(
        ULONG outputControl,
        ULONG mask,
        PCSTR format,
        ...) = 0;

    virtual HRESULT ControlledOutputVaList(
        ULONG outputControl,
        ULONG mask,
        PCSTR format,
        va_list args) = 0;

    // Returns information about the debuggee such
    // as user vs. kernel, dump vs. live, etc.
    virtual HRESULT GetDebuggeeType(
        PULONG debugClass,
        PULONG qualifier) = 0;

    // Returns the page size for the currently executing
    // processor context.  The page size may vary between
    // processor types.
    virtual HRESULT GetPageSize(
        PULONG size) = 0;

    // Returns the type of processor used in the
    // current processor context.
    virtual HRESULT GetExecutingProcessorType(
        PULONG type) = 0;

    // Executes the given command string.
    // If the string has multiple commands
    // Execute will not return until all
    // of them have been executed.  If this
    // requires waiting for the debuggee to
    // execute an internal wait will be done
    // so Execute can take an arbitrary amount
    // of time.
    virtual HRESULT Execute(
        ULONG outputControl,
        PCSTR command,
        ULONG flags) = 0;

    // Retrieves information about the last event that occurred.
    // EventType is one of the event callback mask bits.
    // ExtraInformation contains additional event-specific
    // information.  Not all events have additional information.
    virtual HRESULT GetLastEventInformation(
        PULONG type,
        PULONG processId,
        PULONG threadId,
        PVOID extraInformation,
        ULONG extraInformationSize,
        PULONG extraInformationUsed,
        PSTR description,
        ULONG descriptionSize,
        PULONG descriptionUsed) = 0;
};

typedef class IDebugControl2* PDEBUG_CONTROL2;

// Output mask bits.
// Normal output.
#define DEBUG_OUTPUT_NORMAL            0x00000001
// Error output.
#define DEBUG_OUTPUT_ERROR             0x00000002
// Warnings.
#define DEBUG_OUTPUT_WARNING           0x00000004
// Additional output.
#define DEBUG_OUTPUT_VERBOSE           0x00000008
// Prompt output.
#define DEBUG_OUTPUT_PROMPT            0x00000010
// Register dump before prompt.
#define DEBUG_OUTPUT_PROMPT_REGISTERS  0x00000020
// Warnings specific to extension operation.
#define DEBUG_OUTPUT_EXTENSION_WARNING 0x00000040
// Debuggee debug output, such as from OutputDebugString.
#define DEBUG_OUTPUT_DEBUGGEE          0x00000080
// Debuggee-generated prompt, such as from DbgPrompt.
#define DEBUG_OUTPUT_DEBUGGEE_PROMPT   0x00000100
// Symbol messages, such as for !sym noisy.
#define DEBUG_OUTPUT_SYMBOLS           0x00000200

// Execute and ExecuteCommandFile flags.
// These flags only apply to the command
// text itself; output from the executed
// command is controlled by the output
// control parameter.
// Default execution.  Command is logged
// but not output.
#define DEBUG_EXECUTE_DEFAULT    0x00000000
// Echo commands during execution.  In
// ExecuteCommandFile also echoes the prompt
// for each line of the file.
#define DEBUG_EXECUTE_ECHO       0x00000001
// Do not log or output commands during execution.
// Overridden by DEBUG_EXECUTE_ECHO.
#define DEBUG_EXECUTE_NOT_LOGGED 0x00000002
// If this flag is not set an empty string
// to Execute will repeat the last Execute
// string.
#define DEBUG_EXECUTE_NO_REPEAT  0x00000004

// Classes of debuggee.  Each class
// has different qualifiers for specific
// kinds of debuggees.
#define DEBUG_CLASS_UNINITIALIZED 0
#define DEBUG_CLASS_KERNEL        1
#define DEBUG_CLASS_USER_WINDOWS  2
#define DEBUG_CLASS_IMAGE_FILE    3

#define IMAGE_FILE_MACHINE_I386              0x014c  // Intel 386.
#define IMAGE_FILE_MACHINE_AMD64             0x8664  // AMD64 (K8)

// Execution status codes used for waiting,
// for returning current status and for
// event method return values.
#define DEBUG_STATUS_NO_CHANGE           0
#define DEBUG_STATUS_GO                  1
#define DEBUG_STATUS_GO_HANDLED          2
#define DEBUG_STATUS_GO_NOT_HANDLED      3
#define DEBUG_STATUS_STEP_OVER           4
#define DEBUG_STATUS_STEP_INTO           5
#define DEBUG_STATUS_BREAK               6
#define DEBUG_STATUS_NO_DEBUGGEE         7
#define DEBUG_STATUS_STEP_BRANCH         8
#define DEBUG_STATUS_IGNORE_EVENT        9
#define DEBUG_STATUS_RESTART_REQUESTED   10
#define DEBUG_STATUS_REVERSE_GO          11
#define DEBUG_STATUS_REVERSE_STEP_BRANCH 12
#define DEBUG_STATUS_REVERSE_STEP_OVER   13
#define DEBUG_STATUS_REVERSE_STEP_INTO   14
#define DEBUG_STATUS_OUT_OF_SYNC         15
#define DEBUG_STATUS_WAIT_INPUT          16
#define DEBUG_STATUS_TIMEOUT             17

#define DEBUG_STATUS_MASK                0x1f

#define DEBUG_EVENT_EXCEPTION            0x00000002

#ifdef DEFINE_EXCEPTION_RECORD

#define EXCEPTION_MAXIMUM_PARAMETERS     15

// This copy of the "64" bit record has been modified
// by removing the alignment field to make it the same
// as the _EXCEPTION_RECORD used in the pal defined in 
// pal.h.
typedef struct _EXCEPTION_RECORD64 {
    DWORD ExceptionCode;
    DWORD ExceptionFlags;
    DWORD64 ExceptionRecord;
    DWORD64 ExceptionAddress;
    DWORD NumberParameters;
    DWORD64 ExceptionInformation[EXCEPTION_MAXIMUM_PARAMETERS];
} EXCEPTION_RECORD64, *PEXCEPTION_RECORD64;

#endif // DEFINE_EXCEPTION_RECORD

typedef struct _DEBUG_LAST_EVENT_INFO_EXCEPTION
{
    EXCEPTION_RECORD64 ExceptionRecord;
    ULONG FirstChance;
} DEBUG_LAST_EVENT_INFO_EXCEPTION, *PDEBUG_LAST_EVENT_INFO_EXCEPTION;

//----------------------------------------------------------------------------
// IDebugDataSpaces
//----------------------------------------------------------------------------

struct IDebugDataSpaces
{
public:
    virtual HRESULT ReadVirtual(
        ULONG64 offset,
        PVOID buffer,
        ULONG bufferSize,
        PULONG bytesRead) = 0;

    virtual HRESULT WriteVirtual(
        ULONG64 offset,
        PVOID buffer,
        ULONG bufferSize,
        PULONG bytesWritten) = 0;
};

typedef IDebugDataSpaces* PDEBUG_DATA_SPACES;

//----------------------------------------------------------------------------
// IDebugSymbols
//----------------------------------------------------------------------------

//
// Information about a module.
//

// Flags.
#define DEBUG_MODULE_LOADED            0x00000000
#define DEBUG_MODULE_UNLOADED          0x00000001
#define DEBUG_MODULE_USER_MODE         0x00000002
#define DEBUG_MODULE_EXPLICIT          0x00000008
#define DEBUG_MODULE_SECONDARY         0x00000010
#define DEBUG_MODULE_SYNTHETIC         0x00000020
#define DEBUG_MODULE_SYM_BAD_CHECKSUM  0x00010000

// Symbol types.
#define DEBUG_SYMTYPE_NONE     0
#define DEBUG_SYMTYPE_COFF     1
#define DEBUG_SYMTYPE_CODEVIEW 2
#define DEBUG_SYMTYPE_PDB      3
#define DEBUG_SYMTYPE_EXPORT   4
#define DEBUG_SYMTYPE_DEFERRED 5
#define DEBUG_SYMTYPE_SYM      6
#define DEBUG_SYMTYPE_DIA      7

typedef struct _DEBUG_MODULE_PARAMETERS
{
    ULONG64 Base;
    ULONG Size;
    ULONG TimeDateStamp;
    ULONG Checksum;
    ULONG Flags;
    ULONG SymbolType;
    ULONG ImageNameSize;
    ULONG ModuleNameSize;
    ULONG LoadedImageNameSize;
    ULONG SymbolFileNameSize;
    ULONG MappedImageNameSize;
    ULONG64 Reserved[2];
} DEBUG_MODULE_PARAMETERS, *PDEBUG_MODULE_PARAMETERS;

// A special value marking an offset that should not
// be treated as a valid offset.  This is only used
// in special situations where it is unlikely that
// this value would be a valid offset.
#define DEBUG_INVALID_OFFSET ((ULONG64)-1)

// General unspecified ID constant.
#define DEBUG_ANY_ID 0xffffffff

class IDebugSymbols
{
public:
    // Controls the symbol options used during
    // symbol operations.
    // Uses the same flags as dbghelps SymSetOptions.
    virtual HRESULT GetSymbolOptions(
        PULONG options) = 0;

    virtual HRESULT GetNameByOffset(
        ULONG64 offset,
        PSTR nameBuffer,
        ULONG nameBufferSize,
        PULONG nameSize,
        PULONG64 displacement) = 0;

    // Enumerates the engines list of modules
    // loaded for the current process.  This may
    // or may not match the system module list
    // for the process.  Reload can be used to
    // synchronize the engines list with the system
    // if necessary.
    // Some sessions also track recently unloaded
    // code modules for help in analyzing failures
    // where an attempt is made to call unloaded code.
    // These modules are indexed after the loaded
    // modules.
    virtual HRESULT GetNumberModules(
        PULONG loaded,
        PULONG unloaded) = 0;

    virtual HRESULT GetModuleByIndex(
        ULONG index,
        PULONG64 base) = 0;

    // The module name may not be unique.
    // This method returns the first match.
    virtual HRESULT GetModuleByModuleName(
        PCSTR name,
        ULONG startIndex,
        PULONG index,
        PULONG64 base) = 0;

    // Offset can be any offset within
    // the module extent.  Extents may
    // not be unique when including unloaded
    // drivers.  This method returns the
    // first match.
    virtual HRESULT GetModuleByOffset(
        ULONG64 offset,
        ULONG startIndex,
        PULONG index,
        PULONG64 base) = 0;

    // If Index is DEBUG_ANY_ID the base address
    // is used to look up the module instead.
    virtual HRESULT GetModuleNames(
        ULONG index,
        ULONG64 base,
        PSTR imageNameBuffer,
        ULONG imageNameBufferSize,
        PULONG imageNameSize,
        PSTR moduleNameBuffer,
        ULONG moduleNameBufferSize,
        PULONG moduleNameSize,
        PSTR loadedImageNameBuffer,
        ULONG loadedImageNameBufferSize,
        PULONG loadedImageNameSize) = 0;
};

typedef class IDebugSymbols* PDEBUG_SYMBOLS;

//----------------------------------------------------------------------------
// IDebugSystemObjects
//----------------------------------------------------------------------------

class IDebugSystemObjects 
{
public:
    virtual HRESULT GetCurrentProcessId(
        PULONG id) = 0;

    // Controls implicit thread used by the
    // debug engine.  The debuggers current
    // thread is just a piece of data held
    // by the debugger for calls which use
    // thread-specific information.  In those
    // calls the debuggers current thread is used.
    // The debuggers current thread is not related
    // to any system thread attribute.
    // IDs for threads are small integer IDs
    // maintained by the engine.  They are not
    // related to system thread IDs.
    virtual HRESULT GetCurrentThreadId(
        PULONG id) = 0;

    virtual HRESULT SetCurrentThreadId(
        ULONG id) = 0;

    // Returns the system unique ID for the current thread.
    // Not currently supported when kernel debugging.
    virtual HRESULT GetCurrentThreadSystemId(
        PULONG sysId) = 0;

    // Looks up a debugger thread ID for the given
    // system thread ID.
    // Currently when kernel debugging this will fail
    // if the thread is not executing on a processor.
    virtual HRESULT GetThreadIdBySystemId(
        ULONG sysId,
        PULONG id) = 0;

    // This is a special sos/lldb function used to implement the ICLRDataTarget interface and
    // not actually part of dbgeng's IDebugSystemObjects interface.
    virtual HRESULT GetThreadContextById(
        /* [in] */ ULONG32 threadID,
        /* [in] */ ULONG32 contextFlags,
        /* [in] */ ULONG32 contextSize,
        /* [out, size_is(contextSize)] */ PBYTE context) = 0;
};

typedef class IDebugSystemObjects* PDEBUG_SYSTEM_OBJECTS;

//----------------------------------------------------------------------------
// IDebugRegister
//----------------------------------------------------------------------------

class IDebugRegister
{
public:
    // This is the combination of dbgeng's GetIndexByName and GetValue and not
    // actually part of the dbgeng's IDebugRegister interface.
    virtual HRESULT GetValueByName(
        PCSTR name,
        PDWORD_PTR value) = 0;

    // Abstracted pieces of processor information.
    // The mapping of these values to architectural
    // registers is architecture-specific and their
    // interpretation and existence may vary.  They
    // are intended to be directly compatible with
    // calls which take this information, such as
    // stack walking.
    virtual HRESULT GetInstructionOffset(
        PULONG64 offset) = 0;

    virtual HRESULT GetStackOffset(
        PULONG64 offset) = 0;

    virtual HRESULT GetFrameOffset(
        PULONG64 offset) = 0;
};

typedef class IDebugRegister* PDEBUG_REGISTERS;

//----------------------------------------------------------------------------
// IDebugClient
//----------------------------------------------------------------------------

class IDebugClient;
typedef HRESULT (*PFN_EXCEPTION_CALLBACK)(IDebugClient* client);

class IDebugClient : IDebugControl2, IDebugDataSpaces, IDebugSymbols, IDebugSystemObjects, IDebugRegister
{
public:
    // Returns the coreclr module directory found by lldb plugin 
    // in the target process.
    virtual PCSTR GetCoreClrDirectory() = 0;

    // Evaluates a lldb expression into a value.
    virtual DWORD_PTR GetExpression(
        /* [in] */ PCSTR exp) = 0;

    // Unwind one native stack frame given a thread and register context
    virtual HRESULT VirtualUnwind(
        /* [in] */ DWORD threadID,
        /* [in] */ ULONG32 contextSize,
        /* [in, out, size_is(contextSize)] */ PBYTE context) = 0;

    // Set an exception throw callback
    virtual HRESULT SetExceptionCallback(
        /* [in] */ PFN_EXCEPTION_CALLBACK callback) = 0;

    // Clear the exception throw callback
    virtual HRESULT ClearExceptionCallback() = 0;
};

typedef class IDebugClient* PDEBUG_CLIENT;

#ifdef __cplusplus
};
#endif

#endif // #ifndef __DBGENG_H__
