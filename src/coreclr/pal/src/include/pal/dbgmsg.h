// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    include/pal/dbgmsg.h

Abstract:
    Header file for Debug Message utilities. Output macros, type definitions,
    extern variables. See overview section below for usage details.

--*/

/*
Overview of Debug Message utilities

Use debug channels to selectively output information to the console.

Available macros :

    - SET_DEFAULT_DEBUG_CHANNEL

    This defines the channel to use with the macros TRACE, ERROR, etc
    Use this macro once at the beginning of your source file.
    (impl. details : this declares a constant static variable defdbgchan and
    sets it to the appropriate channel)

    usage : SET_DEFAULT_DEBUG_CHANNEL(somechannel);

    - TRACE, ENTRY, WARN, ERROR, DBGOUT

    Use this to output debug messages to the default debug channel (set with
    SET_DEFAULT_DEBUG_CHANNEL). Messages will only be output if the channel is
    active for the specified level.

    usage : TRACE("printf format string", params...);

    - TRACE_, ENTRY_, WARN_, ERROR_, DBGOUT_

    Use this to autput debug messages to a channel other than the default.

    usage : TRACE_(someotherchannel)("printf format string",params...);
                 ^                ^^                                ^
    don't forget the double set of parentheses!

Available channels :
    PAL     : PAL-specific functionalities (PAL_Initialize, etc.)
    LOADER  : Loading API (LoadLibrary, etc); loader application
    HANDLE  : Handle manager (CloseHandle, etc.)
    SHMEM   : Shared Memory functions (for IPC)
    PROCESS : Process related APIs
    THREAD  : Threading mechanism
    EXCEPT  : Structured Exception Handling functions
    CRT     : PAL implementation of the C Runtime Library functions
    UNICODE : Unicode support API
    ARCH    : platform-dependent stuff
    SYNC    : Management of synchronization objects
    FILE    : File I/O API
    VIRTUAL : Virtual memory and File mapping
    MEM     : Memory management (except Virtual* stuff)
    SOCKET  : WINSOCK implementation
    DEBUG   : Debugging API (ReadProcessMemory, etc.)
    LOCALE  : Locale support API
    MISC    : what doesn't fit anywhere else.
    MUTEX   : Mutex management functions
    CRITSEC : Critical section API
    POLL    : ?
    CRYPT   : Cryptographic functions
    SHFOLDER: Shared (well-known) folder functions
    SXS     : Side-by-side PALs (if supported)

    Note : Most channels correspond to subdirectories $(PALROOT)
    Note 2 : DON'T write TRACE("PAL") or TRACE(DCI_PAL), write TRACE(PAL)

Available debug levels :
    ENTRY : use this at the beginning of a function to print parameters.
    TRACE : use this to output informational messages.
    WARN  : use this to report non-critical problems.
    ERROR : use this to report critical problems.

    DBGOUT: same as TRACE, but does not output line headers (thread ID, etc)

Format specifiers :
    These trace functions currently use the native fprintf() to output data.
    All standard printf format specifiers should therefore work, while Microsoft
    extensions will not.
    There is one special case to consider : wide strings and wide characters.
    Microsoft's extensions to printf include the specifiers %S and %C for
    printing strings and characters of wchar_t. In the C99 standard,
    the specifiers %ls and %ls serve the same purpose. However, Windows defines
    wchar_t as a 16bit int, which is NOT guaranteed to match implementations
    on other platforms. glibc on a x86 defines wchar_t as a 32bit int.
    For this reason, %S and %C should be used in TRACE functions to output
    Windows wide strings (of type wchar_t or WCHAR). To output wide-strings
    in a platforms native format (litterals L"string" or variables of type
    wchar_native), the specifiers %ls and %lc should be used instead.

Using Debug channels at Run Time
    To tell the PAL which debug channels should be open and which should be
    closed, set the environment variable PAL_DBG_CHANNELS according to the
    following syntax :
    [+|-]<channel>.<level>[: ...]
    + opens a channel, - closes it;
    <channel> must be one of PAL, FILE, (etc), or the wildcard "all"
    <level> must be TRACE, ENTRY, WARN, ERROR or "all"

    Examples (for bash):

    export PAL_DBG_CHANNELS="+PAL.TRACE:-FILE.ERROR"
    export PAL_DBG_CHANNELS="+all.ENTRY"
    export PAL_DBG_CHANNELS="-all.all"

    To explicitly redirect the output of debug messages to a file (instead of
    relying on the shell's > and |), set the environment variable
    PAL_API_TRACING to the name of the file to write to. It can also be set to
    "stdout" or "stderr". If PAL_API_TRACING is not set, output will go to
    stderr.

    ASSERT() messages cannot be controlled with PAL_DBG_CHANNELS; they can be
    globally disabled (in debug builds) by setting the environment variable
    PAL_DISABLE_ASSERTS to 1. In release builds, they will always be disabled

    The environment variable "PAL_API_LEVELS" determines how many levels of
    nesting will be allowed in ENTRY calls; if not set, the default is 1; a
    value of 0 will allow infinite nesting, but will not indent the output

    It is possible to disable/enable all channels during the execution of a
    process; this involves using a debugger to modify a variable within the
    address space of the running process. the variable is named
    'dbg_master_switch'; if set to zero, all debug chanels will be closed; if
    set to nonzero, channels will be open or closed based on PAL_DBG_CHANNELS

    Notes :
    If _ENABLE_DEBUG_MESSAGES_ was not defined at build-time, no debug messages
    will be generated.
    If _ENABLE_DEBUG_MESSAGES_ was defined, all debug levels will be enabled,
    but all channels will be closed by default

    Another configure option is --enable-appendtraces
    Normally, if the file specified by PAL_API_TRACING exists, its content will
    be overwritten when a PAL process starts using it. If --enable-appendtraces
    is used, debug output will be appended at the end of the file instead.



 */

#ifndef _PAL_DBGMSG_H_
#define _PAL_DBGMSG_H_

#include "pal/palinternal.h"
#include "config.h"
#include "pal/perftrace.h"
#include "pal/debug.h"
#include "pal/thread.hpp"

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

/* Channel identifiers */
typedef enum
{
    DCI_PAL,
    DCI_LOADER,
    DCI_HANDLE,
    DCI_SHMEM,
    DCI_PROCESS,
    DCI_THREAD,
    DCI_EXCEPT,
    DCI_CRT,
    DCI_UNICODE,
    DCI_ARCH,
    DCI_SYNC,
    DCI_FILE,
    DCI_VIRTUAL,
    DCI_MEM,
    DCI_SOCKET,
    DCI_DEBUG,
    DCI_LOCALE,
    DCI_MISC,
    DCI_MUTEX,
    DCI_CRITSEC,
    DCI_POLL,
    DCI_CRYPT,
    DCI_SHFOLDER,
    DCI_SXS,
    DCI_NUMA,
    // Please make sure to update dbg_channel_names when adding entries here.

    // Do not remove this line, as it indicates the end of the list
    DCI_LAST
} DBG_CHANNEL_ID;

/* Level identifiers */
typedef enum
{
    DLI_ENTRY,
    DLI_TRACE,
    DLI_WARN,
    DLI_ERROR,
    DLI_ASSERT,
    DLI_EXIT,

    DLI_LAST
} DBG_LEVEL_ID;


/* extern variables */

// Change W16_NULLSTRING to external variable to avoid multiple warnings showing up in prefast
extern LPCWSTR W16_NULLSTRING;

extern DWORD dbg_channel_flags[DCI_LAST];
extern BOOL g_Dbg_asserts_enabled;

/* we must use stdio functions directly rather that rely on PAL functions for
  output, because those functions do tracing and we need to avoid recursion */
extern FILE *output_file;

/* master switch for debug channel enablement, to be modified by debugger */
extern Volatile<BOOL> dbg_master_switch ;


/* conditionnal compilation for other debug messages */
#if !_ENABLE_DEBUG_MESSAGES_

/* compile out these trace levels; see the definition of NOTRACE */
#define TRACE     NOTRACE
#define TRACE_(x) NOTRACE
#define WARN      NOTRACE
#define WARN_(x)  NOTRACE
#define ENTRY_EXTERNAL NOTRACE
#define ENTRY     NOTRACE
#define ENTRY_(x) NOTRACE
#define LOGEXIT   NOTRACE
#define LOGEXIT_(x) NOTRACE
#define DBGOUT     NOTRACE
#define DBGOUT_(x) NOTRACE
#define ERROR     NOTRACE
#define ERROR_(x) NOTRACE
#define DBG_PRINTF(level, channel, bHeader) NOTRACE

#define CHECK_STACK_ALIGN

#define SET_DEFAULT_DEBUG_CHANNEL(x)
#define DBG_ENABLED(level, channel)

#else /* _ENABLE_DEBUG_MESSAGES_ */

/* output macros */

#define SET_DEFAULT_DEBUG_CHANNEL(x) \
    static const DBG_CHANNEL_ID defdbgchan = DCI_##x

/* Is debug output enabled for the given level and channel? */
#define DBG_ENABLED(level, channel) (output_file &&                     \
                                     dbg_master_switch &&               \
                                     (dbg_channel_flags[channel] & (1 << (level))))
#define TRACE \
    DBG_PRINTF(DLI_TRACE,defdbgchan,TRUE)

#define TRACE_(x) \
    DBG_PRINTF(DLI_TRACE,DCI_##x,TRUE)

#define WARN \
    DBG_PRINTF(DLI_WARN,defdbgchan,TRUE)

#define WARN_(x) \
    DBG_PRINTF(DLI_WARN,DCI_##x,TRUE)

#if _DEBUG && defined(__APPLE__)
bool DBG_ShouldCheckStackAlignment();
#define CHECK_STACK_ALIGN   if (DBG_ShouldCheckStackAlignment()) DBG_CheckStackAlignment()
#else
#define CHECK_STACK_ALIGN
#endif

#define ENTRY_EXTERNAL \
    CHECK_STACK_ALIGN; \
    DBG_PRINTF(DLI_ENTRY, defdbgchan,TRUE)

#define ENTRY \
    CHECK_STACK_ALIGN; \
    DBG_PRINTF(DLI_ENTRY, defdbgchan,TRUE)

#define ENTRY_(x) \
    CHECK_STACK_ALIGN; \
    DBG_PRINTF(DLI_ENTRY, DCI_##x,TRUE)

#define LOGEXIT \
    DBG_PRINTF(DLI_EXIT, defdbgchan,TRUE)

#define LOGEXIT_(x) \
    DBG_PRINTF(DLI_EXIT, DCI_##x,TRUE)

#define DBGOUT \
    DBG_PRINTF(DLI_TRACE,defdbgchan,FALSE)

#define DBGOUT_(x) \
    DBG_PRINTF(DLI_TRACE,DCI_##x,FALSE)

/*Added this  code here to stop error messages
 *from appearing in retail build*/
#define ERROR \
    DBG_PRINTF(DLI_ERROR,defdbgchan,TRUE)

#define ERROR_(x) \
    DBG_PRINTF(DLI_ERROR,DCI_##x,TRUE)

#define DBG_PRINTF(level, channel, bHeader) \
{\
    if( DBG_ENABLED(level, channel) ) {         \
        DBG_CHANNEL_ID __chanid=channel;\
        DBG_LEVEL_ID __levid=level;\
        BOOL __bHeader = bHeader;\
        DBG_PRINTF2

#define DBG_PRINTF2(...)\
      DBG_printf(__chanid,__levid,__bHeader,__FUNCTION__,__FILE__,__LINE__,__VA_ARGS__);\
    }\
}

#endif /* _ENABLE_DEBUG_MESSAGES_ */

/* define NOTRACE as nothing; this will absorb the variable-argument list used
   in tracing macros */
#define NOTRACE(...)

#if !defined(_DEBUG)

#define ASSERT(...)
#define _ASSERT(expr)
#define _ASSERTE(expr)
#define _ASSERT_MSG(...)

#else /* defined(_DEBUG) */

inline void ANALYZER_NORETURN AssertBreak()
{
    if(g_Dbg_asserts_enabled)
    {
        DebugBreak();
    }
}

#define ASSERT(...)                                                     \
{                                                                       \
    if (output_file && dbg_master_switch)                               \
    {                                                                   \
        DBG_printf(defdbgchan,DLI_ASSERT,TRUE,__FUNCTION__,__FILE__,__LINE__,__VA_ARGS__); \
    }                                                                   \
    AssertBreak();                                                     \
}

#define _ASSERT(expr) do { if (!(expr)) { ASSERT(""); } } while(0)
#define _ASSERTE(expr) do { if (!(expr)) { ASSERT("Expression: " #expr "\n"); } } while(0)
#define _ASSERT_MSG(expr, ...) \
    do { \
        if (!(expr)) \
        { \
            ASSERT("Expression: " #expr ", Description: " __VA_ARGS__); \
        } \
    } while(0)

#endif /* !_DEBUG */

/* Function declarations */

/*++
Function :
    DBG_init_channels

    Initialize debug channel information based on environment settings
    Call this only once at startup.

    (no parameters, no return value)
--*/
BOOL DBG_init_channels(void);

/*++
Function :
    DBG_close_channels

    Close the output file for debug messages.

    (no parameters, no return value)
--*/
void DBG_close_channels(void);

/*++
Function :
    DBG_preprintf

    Internal function for debug channels; don't use.
    This function outputs the header information for debug messages (channel,
    level, etc).

Parameters :
    DBG_CHANNEL_ID channel : debug channel to use
    DBG_LEVEL_ID level : debug message level
    BOOL bHeader : whether or not to output message header (thread id, etc)
    LPSTR file : current file
    INT line : line number

Return Value :
    TRUE if there's an output file, FALSE otherwise. this is so that
    DBG_printf_plain doesn't get called unnecessarily.

Notes :
    This function is only used with compilers that don't support
    variable-argument macros. It enters a critical section, which is left in
    DBG_printf_plain.
--*/
BOOL DBG_preprintf(DBG_CHANNEL_ID channel, DBG_LEVEL_ID level, BOOL bHeader,
                   LPSTR file, INT line);

/*++
Function :
    DBG_printf

    Internal function for debug channels; don't use.
    This function outputs a complete debug message, without function name.

Parameters :
    DBG_CHANNEL_ID channel : debug channel to use
    DBG_LEVEL_ID level : debug message level
    BOOL bHeader : whether or not to output message header (thread id, etc)
    LPCSTR function : current function
    LPCSTR file : current file
    INT line : line number
    LPCSTR format, ... : standard printf parameter list.

Return Value :
    always 1.

Notes :
    This function requires that the compiler support the C99 flavor of
    variable-argument macros, and that they support the __FUNCTION__
    pseudo-macro.

--*/
#if __GNUC__ && CHECK_TRACE_SPECIFIERS
/* if requested, use an __attribute__ feature to ask gcc to check that format
   specifiers match their parameters */
int DBG_printf(DBG_CHANNEL_ID channel, DBG_LEVEL_ID level, BOOL bHeader,
               LPCSTR function, LPCSTR file, INT line, LPCSTR format, ...)
               __attribute__ ((format (printf,7, 8)));
#else
int DBG_printf(DBG_CHANNEL_ID channel, DBG_LEVEL_ID level, BOOL bHeader,
               LPCSTR function, LPCSTR file, INT line, LPCSTR format, ...);
#endif

/*++
Function :
    DBG_printf_plain

    Internal function for debug channels; don't use.
    This function output the user-specified part of a debug-message.

Parameters :
    LPSTR format, ... : standard printf parameter list.

Return value :
    always 1.

Notes :
    This function is only used with compilers that don't support
    variable-argument macros. It will leave the critical section entered in
    DBG_preprintf.

--*/
int DBG_printf_plain(LPSTR format, ...);

/*++
Function :
    DBG_change_entrylevel

    retrieve current ENTRY nesting level and [optionnally] modify it

Parameters :
    int new_level : value to which the nesting level must be set, or -1

Return value :
    nesting level at the time the function was called

Notes:
if new_level is -1, the nesting level will not be modified
--*/
int DBG_change_entrylevel(int new_level);

#ifdef __APPLE__
/*++
Function :
    PAL_DisplayDialog

    Display a simple modal dialog with an alert icon and a single OK button. Caller supplies the title of the
    dialog and the main text. The dialog is displayed only if the COMPlus_EnableAssertDialog environment
    variable is set to the value "1".

--*/
void PAL_DisplayDialog(const char *szTitle, const char *szText);

/*++
Function :
    PAL_DisplayDialogFormatted

    As above but takes a printf-style format string and insertion values to form the main text.

--*/
void PAL_DisplayDialogFormatted(const char *szTitle, const char *szTextFormat, ...);
#else // __APPLE__
#define PAL_DisplayDialog(_szTitle, _szText)
#define PAL_DisplayDialogFormatted(_szTitle, _szTextFormat, args...)
#endif // __APPLE__

#ifdef __cplusplus
}
#endif // __cplusplus

#endif /* _PAL_DBGMSG_H_ */


