// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++
Module Name:
    process.cpp

Abstract:
    Implementation of process object and functions related to processes.
--*/

#include "pal/dbgmsg.h"
SET_DEFAULT_DEBUG_CHANNEL(PROCESS); // some headers have code with asserts, so do this first

#include "pal/procobj.hpp"
#include "pal/thread.hpp"
#include "pal/handlemgr.hpp"
#include "pal/module.h"
#include "procprivate.hpp"
#include "pal/palinternal.h"
#include "pal/process.h"
#include "pal/init.h"
#include "pal/debug.h"
#include "pal/utils.h"
#include "pal/stackstring.hpp"
#include "pal/signal.hpp"


#include <generatedumpflags.h>
#include <clrconfignocache.h>
#include <minipal/time.h>

#include <errno.h>
#if HAVE_POLL
#include <poll.h>
#else
#include "pal/fakepoll.h"
#endif  // HAVE_POLL

#include <unistd.h>
#include <fcntl.h>
#include <sys/mman.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <signal.h>
#if HAVE_PRCTL_H
#include <sys/prctl.h>
#include <sys/syscall.h>
#endif
#if !defined(TARGET_WASI)
#include <sys/wait.h>
#endif
#include <sys/time.h>
#include <sys/resource.h>
#include <debugmacrosext.h>
#include <semaphore.h>
#include <stdint.h>
#include <dlfcn.h>
#include <limits.h>

#ifdef __linux__
#include <linux/membarrier.h>
#include <sys/syscall.h>
#define membarrier(...) syscall(__NR_membarrier, __VA_ARGS__)
#elif HAVE_SYS_MEMBARRIER_H
#include <sys/membarrier.h>
#ifdef TARGET_BROWSER
#define membarrier(cmd, flags, cpu_id) 0 // browser/wasm is currently single threaded
#endif
#endif

#ifdef __APPLE__
#include <pwd.h>
#include <sys/sysctl.h>
#include <mach/task.h>
#include <mach/vm_map.h>
extern "C"
{
#  include <mach/thread_state.h>
}

#define CHECK_MACH(_msg, machret) do {                                      \
        if (machret != KERN_SUCCESS)                                        \
        {                                                                   \
            char _szError[1024];                                            \
            snprintf(_szError, ARRAY_SIZE(_szError), "%s: %u: %s", __FUNCTION__, __LINE__, _msg);  \
            mach_error(_szError, machret);                                  \
            abort();                                                        \
        }                                                                   \
    } while (false)
#endif // __APPLE__

#ifdef __NetBSD__
#include <sys/cdefs.h>
#include <sys/param.h>
#include <sys/sysctl.h>
#include <kvm.h>
#endif

#ifdef __FreeBSD__
#include <sys/sysctl.h>
#include <sys/user.h>
#endif

#ifdef __HAIKU__
#include <image.h>
#include <OS.h>
#endif

extern char *g_szCoreCLRPath;
extern bool g_running_in_exe;

using namespace CorUnix;

// Thread ID of thread that has started the ExitProcess process
Volatile<LONG> terminator = 0;

static SIZE_T InterlockedCompareExchangeSizeT(SIZE_T volatile* destination, SIZE_T exchange, SIZE_T comparand)
{
#ifdef HOST_64BIT
    LONGLONG result = InterlockedCompareExchange64(
        reinterpret_cast<LONGLONG volatile*>(destination),
        *reinterpret_cast<LONGLONG*>(&exchange),
        *reinterpret_cast<LONGLONG*>(&comparand));
    return *reinterpret_cast<SIZE_T*>(&result);
#else
    LONG result = InterlockedCompareExchange(
        reinterpret_cast<LONG volatile*>(destination),
        *reinterpret_cast<LONG*>(&exchange),
        *reinterpret_cast<LONG*>(&comparand));
    return *reinterpret_cast<SIZE_T*>(&result);
#endif
}

// Id of thread generating a core dump
SIZE_T g_crashingThreadId = 0;

// Process ID of this process.
DWORD gPID = (DWORD) -1;

// Function to call during PAL/process shutdown/abort
Volatile<PSHUTDOWN_CALLBACK> g_shutdownCallback = nullptr;

// Function to call instead of exec'ing the createdump binary.  Used by single-file and native AOT hosts.
Volatile<PCREATEDUMP_CALLBACK> g_createdumpCallback = nullptr;
// Function to call to log the managed callstack for a signal. Used by Android since CoreCLR doesn't support CreateDump on Android.
Volatile<PLOGMANAGEDCALLSTACKFORSIGNAL_CALLBACK> g_logManagedCallstackForSignalCallback = nullptr;

// Crash dump generating program arguments. Initialized in PROCAbortInitialize().
#define MAX_ARGV_ENTRIES 32
const char* g_argvCreateDump[MAX_ARGV_ENTRIES] = { nullptr };

// Read from the fatal-signal path (PROCCreateCrashDumpIfEnabled) and written
// once during startup via PAL_SetInProcCrashReportCallback; use Volatile<>
// to match the publication ordering of g_logManagedCallstackForSignalCallback.
// PAL has no direct dependency on the in-proc crash reporter library; the
// reporter registers itself by installing this signal-safe callback. When
// no callback is registered, the fatal-signal path falls back to the
// out-of-proc createdump utility (where g_argvCreateDump has been populated
// via PAL_InitializeCoreCLR).
static Volatile<PINPROCCRASHREPORT_CALLBACK> g_inProcCrashReportCallback = nullptr;

//
// Key used for associating CPalThread's with the underlying pthread
// (through pthread_setspecific)
//
pthread_key_t CorUnix::thObjKey;

static BOOL PROCEndProcess(UINT uExitCode, BOOL bTerminateUnconditionally);

static bool TryEnterCrashDumpGate(CrashDumpSerializeMode serializeMode);

static void ExitCrashDumpGate(CrashDumpSerializeMode serializeMode);

/*++
Function:
  GetCurrentProcess

See MSDN doc.
--*/
HANDLE
PALAPI
GetCurrentProcess(
          VOID)
{
    PERF_ENTRY(GetCurrentProcess);
    ENTRY("GetCurrentProcess()\n" );

    LOGEXIT("GetCurrentProcess returns HANDLE %p\n", hPseudoCurrentProcess);
    PERF_EXIT(GetCurrentProcess);

    /* return a pseudo handle */
    return hPseudoCurrentProcess;
}



/*++
Function:
  ExitProcess

See MSDN doc.
--*/
PAL_NORETURN
VOID
PALAPI
ExitProcess(
    IN UINT uExitCode)
{
    DWORD old_terminator;

    PERF_ENTRY_ONLY(ExitProcess);
    ENTRY("ExitProcess(uExitCode=0x%x)\n", uExitCode );

    old_terminator = InterlockedCompareExchange(&terminator, (DWORD)THREADSilentGetCurrentThreadId(), 0);

    if ((DWORD)THREADSilentGetCurrentThreadId() == old_terminator)
    {
        // This thread has already initiated termination. This can happen
        // in two ways:
        // 1) DllMain(DLL_PROCESS_DETACH) triggers a call to ExitProcess.
        // 2) PAL_exit() is called after the last PALTerminate().
        // If the PAL is still initialized, we go straight through to
        // PROCEndProcess. If it isn't, we simply exit.
        if (!PALIsInitialized())
        {
            exit(uExitCode);
            ASSERT("exit has returned\n");
        }
        else
        {
            WARN("thread re-called ExitProcess\n");
            PROCEndProcess(uExitCode, FALSE);
        }
    }
    else if (0 != old_terminator)
    {
        /* another thread has already initiated the termination process. we
           could just block on the PALInitLock critical section, but then
           PROCSuspendOtherThreads would hang... so sleep forever here, we're
           terminating anyway

           Update: [TODO] PROCSuspendOtherThreads has been removed. Can this
           code be changed? */
        WARN("termination already started from another thread; blocking.\n");
        while (true)
        {
            poll(NULL, 0, INFTIM);
        }
    }

    /* ExitProcess may be called even if PAL is not initialized.
       Verify if process structure exist
    */
    if (PALInitLock() && PALIsInitialized())
    {
        PROCEndProcess(uExitCode, FALSE);

        /* Should not get here, because we terminate the current process */
        ASSERT("PROCEndProcess has returned\n");
    }
    else
    {
        exit(uExitCode);

        /* Should not get here, because we terminate the current process */
        ASSERT("exit has returned\n");
    }

    /* this should never get executed */
    ASSERT("ExitProcess should not return!\n");
    while (true);
}

/*++
Function:
  TerminateProcess

Note:
  hProcess is a handle on the current process.

See MSDN doc.
--*/
BOOL
PALAPI
TerminateProcess(
    IN HANDLE hProcess,
    IN UINT uExitCode)
{
    BOOL ret;

    _ASSERTE(hProcess == hPseudoCurrentProcess);

    PERF_ENTRY(TerminateProcess);
    ENTRY("TerminateProcess(hProcess=%p, uExitCode=%u)\n",hProcess, uExitCode );

    ret = PROCEndProcess(uExitCode, TRUE);

    LOGEXIT("TerminateProcess returns BOOL %d\n", ret);
    PERF_EXIT(TerminateProcess);
    return ret;
}

/*++
Function:
  RaiseFailFastException

See MSDN doc.
--*/
VOID
PALAPI
DECLSPEC_NORETURN
RaiseFailFastException(
    IN PEXCEPTION_RECORD pExceptionRecord,
    IN PCONTEXT pContextRecord,
    IN DWORD dwFlags)
{
    PERF_ENTRY(RaiseFailFastException);
    ENTRY("RaiseFailFastException");

    TerminateCurrentProcessNoExit(TRUE);
    for (;;) PROCAbort();

    LOGEXIT("RaiseFailFastException");
    PERF_EXIT(RaiseFailFastException);
}

/*++
Function:
  PROCEndProcess

  Called from TerminateProcess and ExitProcess. This does the work of
  TerminateProcess, but also takes a flag that determines whether we
  shut down unconditionally. If the flag is set, the PAL will do very
  little extra work before exiting. Most importantly, it won't shut
  down any DLLs that are loaded.

  Only terminating current process is supported.

--*/
static BOOL PROCEndProcess(UINT uExitCode, BOOL bTerminateUnconditionally)
{
    BOOL ret = FALSE;

    // WARN/ERROR before starting the termination process and/or leaving the PAL.
    if (bTerminateUnconditionally)
    {
        WARN("exit code 0x%x ignored for terminate.\n", uExitCode);
    }
    else if ((uExitCode & 0xff) != uExitCode)
    {
        // TODO: Convert uExitCodes into sysexits(3)?
        ERROR("exit() only supports the lower 8-bits of an exit code. "
            "status will only see error 0x%x instead of 0x%x.\n", uExitCode & 0xff, uExitCode);
    }

    TerminateCurrentProcessNoExit(bTerminateUnconditionally);

    LOGEXIT("PROCEndProcess will not return\n");

    if (bTerminateUnconditionally)
    {
        // abort() has the semantics that
        // (1) it doesn't run atexit handlers
        // (2) can invoke CrashReporter or produce a coredump, which is appropriate for TerminateProcess calls
        // TerminationRequestHandlingRoutine in synchmanager.cpp sets the exit code to this special value. The
        // Watson analyzer needs to know that the process was terminated with a SIGTERM.
        PROCAbort(uExitCode == (128 + SIGTERM) ? SIGTERM : SIGABRT);
    }
    else
    {
        exit(uExitCode);
    }

    ASSERT(FALSE); // we shouldn't get here

    return ret;
}

/*++
Function:
  PAL_SetShutdownCallback

Abstract:
  Sets a callback that is executed when the PAL is shut down because of
  ExitProcess, TerminateProcess or PAL_Shutdown but not PAL_Terminate/Ex.

  NOTE: Currently only one callback can be set at a time.
--*/
PALIMPORT
VOID
PALAPI
PAL_SetShutdownCallback(
    IN PSHUTDOWN_CALLBACK callback)
{
    _ASSERTE(g_shutdownCallback == nullptr);
    g_shutdownCallback = callback;
}

/*++
Function:
  PAL_SetCreateDumpCallback

Abstract:
  Sets a callback that is executed when create dump is launched to create a crash dump.

  NOTE: Currently only one callback can be set at a time.
--*/
PALIMPORT
VOID
PALAPI
PAL_SetCreateDumpCallback(
    IN PCREATEDUMP_CALLBACK callback)
{
    _ASSERTE(g_createdumpCallback == nullptr);
    g_createdumpCallback = callback;
}
/*++
Function:
  PAL_SetLogManagedCallstackForSignalCallback

Abstract:
  Sets a callback that is executed when a signal is received to log the managed callstack.
  Used by Android CoreCLR since CreateDump is not supported on Android.

  NOTE: Currently only one callback can be set at a time.
--*/
PALIMPORT
VOID
PALAPI
PAL_SetLogManagedCallstackForSignalCallback(
    IN PLOGMANAGEDCALLSTACKFORSIGNAL_CALLBACK callback)
{
    _ASSERTE(g_logManagedCallstackForSignalCallback == nullptr);
    g_logManagedCallstackForSignalCallback = callback;
}

/*++
Function:
  PAL_SetInProcCrashReportCallback

Abstract:
  Sets a callback that is invoked from the fatal-signal path to let the host
  emit an in-proc crash report (used by Android CoreCLR in place of
  out-of-proc createdump).

  NOTE: Currently only one callback can be set at a time.
--*/
VOID
PALAPI
PAL_SetInProcCrashReportCallback(
    IN PINPROCCRASHREPORT_CALLBACK callback)
{
    _ASSERTE(g_inProcCrashReportCallback == nullptr);
    g_inProcCrashReportCallback = callback;
}

/*++
Function
  PROCNotifyProcessShutdown

  Calls the abort handler to do any shutdown cleanup. Call be called
  from the unhandled native exception handler.

(no return value)
--*/
VOID
PROCNotifyProcessShutdown(bool isExecutingOnAltStack)
{
    // Call back into the coreclr to clean up the debugger transport pipes
    PSHUTDOWN_CALLBACK callback = InterlockedExchangePointer(&g_shutdownCallback, NULL);
    if (callback != NULL)
    {
        callback(isExecutingOnAltStack);
    }
}

/*++
Function
  PROCNotifyProcessShutdownDestructor

  Called at process exit, invokes process shutdown notification

(no return value)
--*/
__attribute__((destructor))
VOID
PROCNotifyProcessShutdownDestructor()
{
    PROCNotifyProcessShutdown(/* isExecutingOnAltStack */ false);
}

/*++
Function:
    PROCFormatInt

    Helper function to format an ULONG32 as a string.

--*/
char*
PROCFormatInt(ULONG32 value)
{
    char* buffer = (char*)malloc(128);
    if (buffer != nullptr)
    {
        if (snprintf(buffer, 128, "%d", value) < 0)
        {
            free(buffer);
            buffer = nullptr;
        }
    }
    return buffer;
}

/*++
Function:
    PROCFormatInt64

    Helper function to format an ULONG64 as a string.

--*/
char*
PROCFormatInt64(ULONG64 value)
{
    char* buffer = (char*)malloc(128);
    if (buffer != nullptr)
    {
        if (snprintf(buffer, 128, "%lld", (long long)value) < 0)
        {
            free(buffer);
            buffer = nullptr;
        }
    }
    return buffer;
}

/*++
Function
  PROCBuildCreateDumpCommandLine

Abstract
  Builds the createdump command line from the arguments.

Return
  TRUE - succeeds, FALSE - fails

--*/
BOOL
PROCBuildCreateDumpCommandLine(
    const char* argv[],
    char** pprogram,
    const char* dumpName,
    const char* logFileName,
    INT dumpType,
    ULONG32 flags)
{
    if (g_szCoreCLRPath == nullptr)
    {
        return FALSE;
    }
    const char* DumpGeneratorName = "createdump";
    int programLen = strlen(g_szCoreCLRPath) + strlen(DumpGeneratorName) + 1;
    char* program = *pprogram = (char*)malloc(programLen);
    if (program == nullptr)
    {
        return FALSE;
    }
    // createdump lives next to libcoreclr.
    const char *last = strrchr(g_szCoreCLRPath, '/');
    size_t directoryLen = last != nullptr ? (size_t)(last - g_szCoreCLRPath + 1) : 0;
    memcpy(program, g_szCoreCLRPath, directoryLen);
    strcpy(program + directoryLen, DumpGeneratorName);

    int argc = 0;
    argv[argc++] = program;

    if (dumpName != nullptr)
    {
        argv[argc++] = "--name";
        argv[argc++] = dumpName;
    }

    switch (dumpType)
    {
        case DumpTypeNormal:
            argv[argc++] = "--normal";
            break;
        case DumpTypeWithHeap:
            argv[argc++] = "--withheap";
            break;
        case DumpTypeTriage:
            argv[argc++] = "--triage";
            break;
        case DumpTypeFull:
            argv[argc++] = "--full";
            break;
        default:
            break;
    }

    if (flags & GenerateDumpFlagsLoggingEnabled)
    {
        argv[argc++] = "--diag";
    }

    if (flags & GenerateDumpFlagsVerboseLoggingEnabled)
    {
        argv[argc++] = "--verbose";
    }

    if (flags & GenerateDumpFlagsCrashReportEnabled)
    {
        argv[argc++] = "--crashreport";
    }

    if (flags & GenerateDumpFlagsCrashReportOnlyEnabled)
    {
        argv[argc++] = "--crashreportonly";
    }

    if (g_running_in_exe)
    {
        argv[argc++] = "--singlefile";
    }

    if (logFileName != nullptr)
    {
        argv[argc++] = "--logtofile";
        argv[argc++] = logFileName;
    }

    argv[argc] = nullptr;
    _ASSERTE(argc < MAX_ARGV_ENTRIES);

    return TRUE;
}

/*++
Function:
  PROCLaunchCreateDump

  Launches the out-of-proc createdump utility for the process. Does not
  serialize; callers that need serialization should go through PROCCreateCrashDump.

Return:
  TRUE - succeeds, FALSE - fails
--*/
static BOOL
PROCLaunchCreateDump(
    const char* argv[],
    LPSTR errorMessageBuffer,
    INT cbErrorMessageBuffer)
{
#if defined(TARGET_IOS) || defined(TARGET_TVOS) || defined(TARGET_WASM)
    return FALSE;
#else
    _ASSERTE(argv[0] != nullptr);
    _ASSERTE(errorMessageBuffer == nullptr || cbErrorMessageBuffer > 0);

    int pipe_descs[4];
    if (pipe(pipe_descs) == -1 || pipe(pipe_descs + 2) == -1)
    {
        if (errorMessageBuffer != nullptr)
        {
            snprintf(errorMessageBuffer, cbErrorMessageBuffer, "Problem launching createdump: pipe() FAILED %s (%d)\n", strerror(errno), errno);
        }
        return false;
    }

    // from parent (write) to child (read), used to signal prctl(PR_SET_PTRACER, childpid) is done
    int child_read_pipe = pipe_descs[0];
    int parent_write_pipe = pipe_descs[1];
    // from child (write) to parent (read), used to capture createdump's stderr
    int parent_read_pipe = pipe_descs[2];
    int child_write_pipe = pipe_descs[3];

    // Fork the core dump child process.
    pid_t childpid = fork();

    // If error, write an error to trace log and abort
    if (childpid == -1)
    {
        if (errorMessageBuffer != nullptr)
        {
            snprintf(errorMessageBuffer, cbErrorMessageBuffer, "Problem launching createdump: fork() FAILED %s (%d)\n", strerror(errno), errno);
        }
        for (int i = 0; i < 4; i++)
        {
            close(pipe_descs[i]);
        }
        return false;
    }
    else if (childpid == 0)
    {
        int callbackResult = 0;

        close(parent_read_pipe);
        close(parent_write_pipe);

        // Wait for prctl(PR_SET_PTRACER, childpid) in parent
        char buffer;
        int bytesRead;
        while((bytesRead = read(child_read_pipe, &buffer, 1)) < 0 && errno == EINTR);
        close(child_read_pipe);

        if (bytesRead != 1)
        {
            fprintf(stderr, "Problem reading from createdump child_read_pipe: %s (%d)\n", strerror(errno), errno);
            close(child_write_pipe);
            _exit(EXIT_FAILURE);
        }

        // Only dup the child's stderr if there is error buffer
        if (errorMessageBuffer != nullptr)
        {
            dup2(child_write_pipe, STDERR_FILENO);
        }
        if (g_createdumpCallback != nullptr)
        {
            // Remove the signal handlers inherited from the runtime process
            SEHCleanupSignals(true /* isChildProcess */);

            // Call the statically linked createdump code
            int argc = 0;
            while (argv[argc] != nullptr && argc < MAX_ARGV_ENTRIES)
            {
                argc++;
            }
            callbackResult = g_createdumpCallback(argc, argv);
            // Set the shutdown callback to nullptr and exit
            // If we don't exit, the child's execution will continue into the diagnostic server behavior
            // which causes all sorts of problems.
            g_shutdownCallback = nullptr;
            exit(callbackResult);
        }
        else
        {
            // Execute the createdump program
            if (execv(argv[0], (char**)argv) == -1)
            {
                fprintf(stderr, "Problem launching createdump (may not have execute permissions): execve(%s) FAILED %s (%d)\n", argv[0], strerror(errno), errno);
                _exit(EXIT_FAILURE);
            }
        }
    }
    else
    {
        close(child_read_pipe);
        close(child_write_pipe);
#if HAVE_PRCTL_H && HAVE_PR_SET_PTRACER
        // Gives the child process permission to use /proc/<pid>/mem and ptrace
        if (prctl(PR_SET_PTRACER, childpid, 0, 0, 0) == -1)
        {
            // Ignore any error because on some CentOS and OpenSUSE distros, it isn't
            // supported but createdump works just fine.
            ERROR("PROCCreateCrashDump: prctl() FAILED %s (%d)\n", strerror(errno), errno);
        }
#endif // HAVE_PRCTL_H && HAVE_PR_SET_PTRACER
        // Signal child that prctl(PR_SET_PTRACER, childpid) is done
        int bytesWritten;
        while((bytesWritten = write(parent_write_pipe, "S", 1)) < 0 && errno == EINTR);
        close(parent_write_pipe);

        if (bytesWritten != 1)
        {
            fprintf(stderr, "Problem writing to createdump parent_write_pipe: %s (%d)\n", strerror(errno), errno);
            close(parent_read_pipe);
            if (errorMessageBuffer != nullptr)
            {
                errorMessageBuffer[0] = 0;
            }
            return false;
        }

        // Read createdump's stderr messages (if any)
        if (errorMessageBuffer != nullptr)
        {
            // Read createdump's stderr
            int bytesRead = 0;
            int count = 0;
            while ((count = read(parent_read_pipe, errorMessageBuffer + bytesRead, cbErrorMessageBuffer - bytesRead)) > 0)
            {
                bytesRead += count;
            }
            errorMessageBuffer[bytesRead] = 0;
            if (bytesRead > 0)
            {
                fputs(errorMessageBuffer, stderr);
            }
        }
        close(parent_read_pipe);

        // Parent waits until the child process is done
        int wstatus = 0;
        int result = waitpid(childpid, &wstatus, 0);
        if (result != childpid)
        {
            fprintf(stderr, "Problem waiting for createdump: waitpid() FAILED result %d wstatus %08x errno %s (%d)\n",
                result, wstatus, strerror(errno), errno);
            return false;
        }
        else
        {
#ifdef _DEBUG
            fprintf(stderr, "waitpid() returned successfully (wstatus %08x) WEXITSTATUS %x WTERMSIG %x\n", wstatus, WEXITSTATUS(wstatus), WTERMSIG(wstatus));
#endif
            return !WIFEXITED(wstatus) || WEXITSTATUS(wstatus) == 0;
        }
    }
    return true;
#endif // !TARGET_IOS && !TARGET_TVOS && !TARGET_WASM
}

/*++
Function:
  PROCCreateCrashDump

  Creates a crash dump of the process via the out-of-proc createdump utility,
  serializing concurrent crash diagnostics per serializeMode. Can be
  called from the unhandled native exception handler.

Return:
  TRUE  - succeeds.
  FALSE - fails, another thread already owns the gate under
          CrashDumpSerialize_NoWait, or the owning thread re-enters the gate.
          Under CrashDumpSerialize_WaitInfinite a losing non-owner thread blocks
          indefinitely in TryEnterCrashDumpGate instead of returning.
--*/
static BOOL
PROCCreateCrashDump(
    const char* argv[],
    LPSTR errorMessageBuffer,
    INT cbErrorMessageBuffer,
    CrashDumpSerializeMode serializeMode)
{
    if (!TryEnterCrashDumpGate(serializeMode))
    {
        return FALSE;
    }

    BOOL result = PROCLaunchCreateDump(argv, errorMessageBuffer, cbErrorMessageBuffer);

    ExitCrashDumpGate(serializeMode);
    return result;
}

/*++
Function
  PROCAbortInitialize()

Abstract
  Initialize the process abort crash dump program file path and
  name. Doing all of this ahead of time so nothing is allocated
  or copied in PROCAbort/signal handler.

Return
  TRUE - succeeds, FALSE - fails

--*/
BOOL
PROCAbortInitialize()
{
    CLRConfigNoCache enabledCfg = CLRConfigNoCache::Get("DbgEnableMiniDump", /*noprefix*/ false, &getenv);

    DWORD enabled = 0;
    if (enabledCfg.IsSet() && enabledCfg.TryAsInteger(10, enabled) && enabled)
    {
        CLRConfigNoCache dmpNameCfg = CLRConfigNoCache::Get("DbgMiniDumpName", /*noprefix*/ false, &getenv);
        const char* dumpName = dmpNameCfg.IsSet() ? dmpNameCfg.AsString() : nullptr;

        CLRConfigNoCache dmpLogToFileCfg = CLRConfigNoCache::Get("CreateDumpLogToFile", /*noprefix*/ false, &getenv);
        const char* logFilePath = dmpLogToFileCfg.IsSet() ? dmpLogToFileCfg.AsString() : nullptr;

        CLRConfigNoCache dmpTypeCfg = CLRConfigNoCache::Get("DbgMiniDumpType", /*noprefix*/ false, &getenv);
        DWORD dumpType = DumpTypeUnknown;
        if (dmpTypeCfg.IsSet())
        {
            (void)dmpTypeCfg.TryAsInteger(10, dumpType);
            if (dumpType <= DumpTypeUnknown || dumpType > DumpTypeMax)
            {
                dumpType = DumpTypeUnknown;
            }
        }

        ULONG32 flags = GenerateDumpFlagsNone;
        CLRConfigNoCache createDumpDiag = CLRConfigNoCache::Get("CreateDumpDiagnostics", /*noprefix*/ false, &getenv);
        DWORD val = 0;
        if (createDumpDiag.IsSet() && createDumpDiag.TryAsInteger(10, val) && val == 1)
        {
            flags |= GenerateDumpFlagsLoggingEnabled;
        }
        CLRConfigNoCache createDumpVerboseDiag = CLRConfigNoCache::Get("CreateDumpVerboseDiagnostics", /*noprefix*/ false, &getenv);
        val = 0;
        if (createDumpVerboseDiag.IsSet() && createDumpVerboseDiag.TryAsInteger(10, val) && val == 1)
        {
            flags |= GenerateDumpFlagsVerboseLoggingEnabled;
        }
        CLRConfigNoCache enabledReportCfg = CLRConfigNoCache::Get("EnableCrashReport", /*noprefix*/ false, &getenv);
        val = 0;
        if (enabledReportCfg.IsSet() && enabledReportCfg.TryAsInteger(10, val) && val == 1)
        {
            flags |= GenerateDumpFlagsCrashReportEnabled;
        }
        CLRConfigNoCache enabledReportOnlyCfg = CLRConfigNoCache::Get("EnableCrashReportOnly", /*noprefix*/ false, &getenv);
        val = 0;
        if (enabledReportOnlyCfg.IsSet() && enabledReportOnlyCfg.TryAsInteger(10, val) && val == 1)
        {
            flags |= GenerateDumpFlagsCrashReportOnlyEnabled;
        }

        char* program = nullptr;
        if (!PROCBuildCreateDumpCommandLine(g_argvCreateDump, &program, dumpName, logFilePath, dumpType, flags))
        {
            return FALSE;
        }
    }
    return TRUE;
}

/*++
Function:
  PAL_GenerateCoreDump

Abstract:
  Public entry point to create a crash dump of the process.

Parameters:
    dumpName
    dumpType:
        Normal = 1,
        WithHeap = 2,
        Triage = 3,
        Full = 4
    flags
        See enum

Return:
    TRUE success
    FALSE failed
--*/
BOOL
PALAPI
PAL_GenerateCoreDump(
    LPCSTR dumpName,
    INT dumpType,
    ULONG32 flags,
    LPSTR errorMessageBuffer,
    INT cbErrorMessageBuffer)
{
    const char* argvCreateDump[MAX_ARGV_ENTRIES] = { nullptr };

    if (dumpType <= DumpTypeUnknown || dumpType > DumpTypeMax)
    {
        return FALSE;
    }
    if (dumpName != nullptr && dumpName[0] == '\0')
    {
        dumpName = nullptr;
    }
    char* program = nullptr;
    BOOL result = PROCBuildCreateDumpCommandLine(argvCreateDump, &program, dumpName, nullptr, dumpType, flags);
    if (result)
    {
        result = PROCCreateCrashDump(argvCreateDump, errorMessageBuffer, cbErrorMessageBuffer, CrashDumpSerialize_None);
    }
    free(program);
    return result;
}

// Helper function to prevent compiler from optimizing away a variable
__attribute__((noinline,NOOPT_ATTRIBUTE))
static void DoNotOptimize(const void* p)
{
    // This function takes the address of a variable to ensure
    // it's preserved and available in crash dumps
    (void)p;
}

static LPCWSTR GetSignalName(int signal)
{
    switch (signal)
    {
        case SIGSEGV: return W("SIGSEGV");
        case SIGBUS:  return W("SIGBUS");
        case SIGFPE:  return W("SIGFPE");
        case SIGILL:  return W("SIGILL");
        case SIGABRT: return W("SIGABRT");
        case SIGTRAP: return W("SIGTRAP");
        case SIGTERM: return W("SIGTERM");
        default:      return W("Unknown signal");
    }
}

/*++
Function:
  PROCLogManagedCallstackForSignal

  Invokes the registered callback to log the managed callstack for a signal.
  Used by Android since CreateDump is not supported there.

Parameters:
  signal - POSIX signal number

(no return value)
--*/
VOID
PROCLogManagedCallstackForSignal(int signal)
{
    if (g_logManagedCallstackForSignalCallback != nullptr)
    {
        LPCWSTR signalName = GetSignalName(signal);
        g_logManagedCallstackForSignalCallback(signalName);
    }
}

/*++
Function:
  TryEnterCrashDumpGate

  Serializes so that one thread generates a crash dump or in-proc crash report at a time.
  The first thread to arrive wins the gate; concurrent threads wait (or not) depending on the
  mode. See CrashDumpSerializeMode.

Return:
  TRUE  - the calling thread owns the gate.
  FALSE - the calling thread does not own the gate. Returned for
          CrashDumpSerialize_NoWait and for a reentrant call by the owning
          thread; a losing non-owner CrashDumpSerialize_WaitInfinite thread
          blocks indefinitely instead and never returns.
--*/
static bool
TryEnterCrashDumpGate(CrashDumpSerializeMode serializeMode)
{
    if (serializeMode == CrashDumpSerialize_None)
    {
        // No serialization requested.
        return true;
    }

    SIZE_T currentThreadId = THREADSilentGetCurrentThreadId();
    SIZE_T previousThreadId = InterlockedCompareExchangeSizeT(&g_crashingThreadId, currentThreadId, 0);
    if (previousThreadId == 0)
    {
        // Won the gate.
        return true;
    }

    // Lost the gate, or this is a reentrant call on the owning thread. A
    // WaitInfinite contender (other than the owner) waits indefinitely; the owner
    // and CrashDumpSerialize_NoWait fall through and return immediately.
    if (previousThreadId != currentThreadId && serializeMode == CrashDumpSerialize_WaitInfinite)
    {
        // The winner generates diagnostics and should terminate the process.
        // Wait here until that happens.
        while (true)
        {
#if HAVE_POLL
            poll(NULL, 0, INFTIM);
#else
            pause();
#endif
        }
    }

    return false;
}

/*++
Function:
  ExitCrashDumpGate

  Releases the gate acquired by TryEnterCrashDumpGate. Only the owning thread
  releases the gate; a call from any other thread is a no-op.
--*/
static void
ExitCrashDumpGate(CrashDumpSerializeMode serializeMode)
{
    // CrashDumpSerialize_None never acquired the gate, and
    // CrashDumpSerialize_WaitInfinite intentionally leaves it held.
    if (serializeMode == CrashDumpSerialize_None || serializeMode == CrashDumpSerialize_WaitInfinite)
    {
        return;
    }

    // Re-arm the gate so a later crash can generate diagnostics again, but only
    // if this thread still owns it.
    SIZE_T currentThreadId = THREADSilentGetCurrentThreadId();
    InterlockedCompareExchangeSizeT(&g_crashingThreadId, 0, currentThreadId);
}

/*++
Function:
  PROCCreateInProcCrashReport

  Invokes the host-registered in-proc crash report callback, serializing
  concurrent crash diagnostics per serializeMode through the shared gate.
  Can be called from the unhandled native exception handler.

Parameters:
  signal - POSIX signal number
  siginfo - POSIX signal info or nullptr
  context - signal context or nullptr
  serializeMode - how to serialize concurrent crash diagnostics

(no return value)
--*/
static VOID
PROCCreateInProcCrashReport(int signal, siginfo_t* siginfo, void* context, CrashDumpSerializeMode serializeMode)
{
    if (!TryEnterCrashDumpGate(serializeMode))
    {
        return;
    }

    // The host emits its report from this signal frame.
    PINPROCCRASHREPORT_CALLBACK callback = g_inProcCrashReportCallback;
    if (callback != nullptr)
    {
        callback(signal, siginfo, context);
    }

    ExitCrashDumpGate(serializeMode);
}

/*++
Function:
  PROCCreateCrashDumpIfEnabled

  Creates crash dump of the process (if enabled). Can be
  called from the unhandled native exception handler.

Parameters:
  signal - POSIX signal number
  siginfo - POSIX signal info or nullptr
  context - signal context or nullptr
  serializeMode - how to serialize concurrent crash diagnostics

(no return value)
--*/
VOID
PROCCreateCrashDumpIfEnabled(int signal, siginfo_t* siginfo, void* context, CrashDumpSerializeMode serializeMode)
{
    // Preserve context pointer to prevent optimization
    DoNotOptimize(&context);

    // If a host registered an in-proc crash report callback, it owns crash
    // reporting for this process: the host emits its report from this signal
    // frame and createdump is not launched.
    if (g_inProcCrashReportCallback != nullptr)
    {
        // The in-proc crash reporter runs only on terminal crash paths. Non-terminal
        // paths that may continue execution after signal handling (e.g. SIGTERM, which
        // uses CrashDumpSerialize_None) are skipped, because the reporter does not
        // support recurrent invocations.
        if (serializeMode != CrashDumpSerialize_None)
        {
            PROCCreateInProcCrashReport(signal, siginfo, context, serializeMode);
        }
        return;
    }

    // Otherwise fall back to launching the out-of-proc createdump utility
    // and wait until it completes.
    if (g_argvCreateDump[0] != nullptr)
    {
        const char* argv[MAX_ARGV_ENTRIES];
        char* signalArg = nullptr;
        char* crashThreadArg = nullptr;
        char* signalCodeArg = nullptr;
        char* signalErrnoArg = nullptr;
        char* signalAddressArg = nullptr;

        // Copy the createdump argv
        int argc = 0;
        for (; argc < MAX_ARGV_ENTRIES && g_argvCreateDump[argc] != nullptr; argc++)
        {
            argv[argc] = g_argvCreateDump[argc];
        }

        if (signal != 0)
        {
            // Add the signal number to the command line
            signalArg = PROCFormatInt(signal);
            if (signalArg != nullptr)
            {
                argv[argc++] = "--signal";
                argv[argc++] = signalArg;
            }

            // Add the current thread id to the command line. This function is always called on the crashing thread.
            crashThreadArg = PROCFormatInt(THREADSilentGetCurrentThreadId());
            if (crashThreadArg != nullptr)
            {
                argv[argc++] = "--crashthread";
                argv[argc++] = crashThreadArg;
            }

            if (siginfo != nullptr)
            {
                signalCodeArg = PROCFormatInt(siginfo->si_code);
                if (signalCodeArg != nullptr)
                {
                    argv[argc++] = "--code";
                    argv[argc++] = signalCodeArg;
                }
                signalErrnoArg = PROCFormatInt(siginfo->si_errno);
                if (signalErrnoArg != nullptr)
                {
                    argv[argc++] = "--errno";
                    argv[argc++] = signalErrnoArg;
                }
                signalAddressArg = PROCFormatInt64((ULONG64)siginfo->si_addr);
                if (signalAddressArg != nullptr)
                {
                    argv[argc++] = "--address";
                    argv[argc++] = signalAddressArg;
                }
            }
        }

        argv[argc] = nullptr;
        _ASSERTE(argc < MAX_ARGV_ENTRIES);

        PROCCreateCrashDump(argv, nullptr, 0, serializeMode);

        free(signalArg);
        free(crashThreadArg);
        free(signalCodeArg);
        free(signalErrnoArg);
        free(signalAddressArg);
    }
}

/*++
Function:
  PROCAbort()

  Aborts the process after calling the shutdown cleanup handler. This function
  should be called instead of calling abort() directly.

Parameters:
  signal - POSIX signal number
  context - signal context or nullptr

  Does not return
--*/
#if !defined(HOST_ARM)
PAL_NORETURN
#endif
VOID
PROCAbort(int signal, siginfo_t* siginfo, void* context)
{
    // Do any shutdown cleanup before aborting or creating a core dump
    PROCNotifyProcessShutdown();

    PROCCreateCrashDumpIfEnabled(signal, siginfo, context, CrashDumpSerialize_WaitInfinite);

    // Restore the signals that abort() relies on to end the process. See SEHCleanupSignals.
    SEHCleanupSignals(false /* isChildProcess */);

    // Abort the process after waiting for the core dump to complete
    abort();
}

/*++
Function:
  CreateInitialProcessAndThreadObjects

Abstract
  Creates the IPalObjects that represent the current process
  and the initial thread

Parameter
  pThread - the initial thread

Return
  PAL_ERROR
--*/

PAL_ERROR
CorUnix::CreateInitialProcessAndThreadObjects(
    CPalThread *pThread
    )
{
    PAL_ERROR palError = NO_ERROR;
    HANDLE hThread;

    //
    // Create initial thread object
    //

    palError = CreateThreadObject(pThread, pThread, &hThread);
    if (NO_ERROR != palError)
    {
        goto CreateInitialProcessAndThreadObjectsExit;
    }

    //
    // This handle isn't needed
    //

    (void) g_pObjectManager->RevokeHandle(pThread, hThread);

CreateInitialProcessAndThreadObjectsExit:

    return palError;
}


/*++
Function:
  TerminateCurrentProcessNoExit

Abstract:
    Terminate current Process, but leave the caller alive

Parameters:
    BOOL bTerminateUnconditionally - If this is set, the PAL will exit as
    quickly as possible. In particular, it will not unload DLLs.

Return value :
    No return

Note:
  This function is used in ExitThread and TerminateProcess

--*/
VOID
CorUnix::TerminateCurrentProcessNoExit(BOOL bTerminateUnconditionally)
{
    BOOL locked;
    DWORD old_terminator;

    old_terminator = InterlockedCompareExchange(&terminator, (DWORD)THREADSilentGetCurrentThreadId(), 0);

    if (0 != old_terminator && (DWORD)THREADSilentGetCurrentThreadId() != old_terminator)
    {
        /* another thread has already initiated the termination process. we
           could just block on the PALInitLock critical section, but then
           PROCSuspendOtherThreads would hang... so sleep forever here, we're
           terminating anyway

           Update: [TODO] PROCSuspendOtherThreads has been removed. Can this
           code be changed? */

        /* note that if *this* thread has already started the termination
           process, we want to proceed. the only way this can happen is if a
           call to DllMain (from ExitProcess) brought us here (because DllMain
           called ExitProcess, or TerminateProcess, or ExitThread);
           TerminateProcess won't call DllMain, so there's no danger to get
           caught in an infinite loop */
        WARN("termination already started from another thread; blocking.\n");
        while (true)
        {
            poll(NULL, 0, INFTIM);
        }
    }

    /* Try to lock the initialization count to prevent multiple threads from
       terminating/initializing the PAL simultaneously */

    /* note : it's also important to take this lock before the process lock,
       because Init/Shutdown take the init lock, and the functions they call
       may take the process lock. We must do it in the same order to avoid
       deadlocks */

    locked = PALInitLock();
    if(locked && PALIsInitialized())
    {
        PROCNotifyProcessShutdown();
    }
}
