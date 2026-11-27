// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++
Module Name:
    debug.c

Abstract:
    Implementation of Win32 debugging API functions.
--*/

#include "pal/dbgmsg.h"
SET_DEFAULT_DEBUG_CHANNEL(DEBUG); // some headers have code with asserts, so do this first

#include "pal/thread.hpp"
#include "pal/procobj.hpp"

#include "pal/palinternal.h"
#include "pal/process.h"
#include "pal/context.h"
#include "pal/debug.h"
#include "pal/module.h"
#include "pal/stackstring.hpp"
#include "pal/utils.h"
#include <minipal/environ.h>

#include <signal.h>
#include <unistd.h>
#include <fcntl.h>
#if HAVE_PROCFS_CTL
#include <unistd.h>
#elif defined(HAVE_TTRACE) // HAVE_PROCFS_CTL
#include <sys/ttrace.h>
#elif HAVE_SYS_PTRACE_H
#include <sys/ptrace.h>
#endif  // HAVE_PROCFS_CTL
#if HAVE_VM_READ
#include <mach/mach.h>
#endif  // HAVE_VM_READ
#include <errno.h>
#include <sys/types.h>
#include <sys/wait.h>

#if HAVE_PROCFS_H
#include <procfs.h>
#endif // HAVE_PROCFS_H

#ifdef __APPLE__
#include <mach/mach.h>
#if defined(TARGET_OSX)
#include <mach/mach_vm.h>
#endif
#endif // __APPLE__

#if HAVE_MACH_EXCEPTIONS
#include "../exception/machexception.h"
#endif // HAVE_MACH_EXCEPTIONS

using namespace CorUnix;

extern "C" void DBG_DebugBreak_End();

/* ------------------- Constant definitions ----------------------------------*/

#if !HAVE_VM_READ && !HAVE_PROCFS_CTL
const BOOL DBG_ATTACH       = TRUE;
const BOOL DBG_DETACH       = FALSE;
#endif

#if defined(_DEBUG) && !defined(TARGET_IOS) && !defined(TARGET_TVOS)
#define ENABLE_RUN_ON_DEBUG_BREAK 1
#endif // _DEBUG

#ifdef ENABLE_RUN_ON_DEBUG_BREAK
static const char PAL_RUN_ON_DEBUG_BREAK[]   = "PAL_RUN_ON_DEBUG_BREAK";
#endif // ENABLE_RUN_ON_DEBUG_BREAK

extern "C" {

#ifdef ENABLE_RUN_ON_DEBUG_BREAK
/*
   When DebugBreak() is called, if PAL_RUN_ON_DEBUG_BREAK is set,
   DebugBreak() will execute whatever command is in there.

   PAL_RUN_ON_DEBUG_BREAK must be no longer than 255 characters.

   This command string inherits the current process's environment,
   with two additions:
      PAL_EXE_PID  - the process ID of the current process
      PAL_EXE_NAME - the name of the executable of the current process

   When DebugBreak() runs this string, it periodically polls the child process
   and blocks until it finishes. If you use this mechanism to start a
   debugger, you can break this poll loop by setting the "spin" variable in
   run_debug_command()'s frame to 0, and then the parent process can
   continue.

   suggested values for PAL_RUN_ON_DEBUG_BREAK:
     to halt the process for later inspection:
       'echo stopping $PAL_EXE_PID; kill -STOP $PAL_EXE_PID; sleep 10'

     to print out the stack trace:
       'pstack $PAL_EXE_PID'

     to invoke the gdb debugger on the process:
       'set -x; gdb $PAL_EXE_NAME $PAL_EXE_PID'

     to invoke the ddd debugger on the process (requires X11):
       'set -x; ddd $PAL_EXE_NAME $PAL_EXE_PID'
*/

#define PID_TEXT "PAL_EXE_PID="
#define EXE_TEXT "PAL_EXE_NAME="

static
int
run_debug_command (const char *command, char *pid_entry, char *exe_entry)
{
    int pid;
    Volatile<int> spin = 1;

    if (!command) {
        return 1;
    }

    char **environment = minipal_environ_get();
    size_t count = 0;
    while (environment[count] != NULL)
    {
        count++;
    }

    char **child_environment = (char **)malloc((count + 3) * sizeof(char *));
    if (child_environment == NULL)
    {
        return -1;
    }

    size_t child_count = 0;
    child_environment[child_count++] = pid_entry;
    child_environment[child_count++] = exe_entry;
    for (size_t i = 0; i < count; i++)
    {
        if (strncmp(environment[i], PID_TEXT, strlen(PID_TEXT)) != 0 &&
            strncmp(environment[i], EXE_TEXT, strlen(EXE_TEXT)) != 0)
        {
            child_environment[child_count++] = environment[i];
        }
    }
    child_environment[child_count] = NULL;

    printf("Spawning command: %s\n", command);

    pid = fork();
    if (pid == -1) {
        free(child_environment);
        return -1;
    }
    if (pid == 0) {
        const char *argv[4] = { "sh", "-c", command, 0 };
        execve("/bin/sh", (char **)argv, child_environment);
        exit(127);
    }

    free(child_environment);

    /* We continue either when the spawned process has stopped, or when
       an attached debugger sets spin to 0 */
    while (spin != 0) {
        int status = 0;
        int ret = waitpid(pid, &status, WNOHANG);
        if (ret == 0) {
            int i;
            /* I tried to use sleep for this, and that works everywhere except
               FreeBSD. The problem on FreeBSD is that if the process gets a
               signal while blocked in sleep(), gdb is confused by the stack */
            for (i = 0; i < 1000000; i++)
                ;
        }
        else if (ret == -1) {
            if (errno != EINTR) {
                return -1;
            }
        }
        else if (WIFEXITED(status)) {
            return WEXITSTATUS(status);
        }
        else {
            fprintf (stderr, "unexpected return from waitpid\n");
            return -1;
        }
    };
    return 0;
}
#endif // ENABLE_RUN_ON_DEBUG_BREAK

static
int
DebugBreakCommand()
{
#ifdef ENABLE_RUN_ON_DEBUG_BREAK
    extern MODSTRUCT exe_module;

    const char *command_string = getenv(PAL_RUN_ON_DEBUG_BREAK);
    if (command_string)
    {
        char pid_buf[sizeof (PID_TEXT) + 32];
        PathCharString exe_bufString;
        int libNameLength = 10;

        if (exe_module.lib_name != NULL)
        {
            libNameLength = minipal_u16_strlen((const CHAR16_T*)exe_module.lib_name);
        }

        SIZE_T dwexe_buf = strlen(EXE_TEXT) + libNameLength + 1;
        CHAR * exe_buf = exe_bufString.OpenStringBuffer(dwexe_buf);

        if (NULL == exe_buf)
        {
            goto FAILED;
        }

        if (snprintf (pid_buf, sizeof (pid_buf), PID_TEXT "%d", getpid()) <= 0)
        {
            goto FAILED;
        }

        if (snprintf (exe_buf, sizeof (CHAR) * (dwexe_buf + 1), EXE_TEXT "%ls", (wchar_t*)exe_module.lib_name) <= 0)
        {
            goto FAILED;
        }

        exe_bufString.CloseBuffer(dwexe_buf);

        if (run_debug_command (command_string, pid_buf, exe_buf))
        {
            goto FAILED;
        }

        return 1;
    }

    return 0;

FAILED:
    if (command_string)
    {
        fprintf (stderr, "Failed to execute command: '%s'\n", command_string);
    }

    return -1;
#else // ENABLE_RUN_ON_DEBUG_BREAK
    return 0;
#endif // ENABLE_RUN_ON_DEBUG_BREAK
}

/*++
Function:
  DebugBreak

See MSDN doc.
--*/
VOID
PALAPI
DebugBreak(
       VOID)
{
    PERF_ENTRY(DebugBreak);
    ENTRY("DebugBreak()\n");

    if (DebugBreakCommand() <= 0) {
        // either didn't do anything, or failed
        TRACE("Calling DBG_DebugBreak\n");
        DBG_DebugBreak();
    }

    LOGEXIT("DebugBreak returns\n");
    PERF_EXIT(DebugBreak);
}

/*++
Function:
  IsInDebugBreak(addr)

  Returns true if the address is in DBG_DebugBreak.

--*/
BOOL
IsInDebugBreak(void *addr)
{
#ifdef TARGET_WASM
    _ASSERT("IsInDebugBreak not implemented on wasm");
    return false;
#else
    return (addr >= (void *)DBG_DebugBreak) && (addr <= (void *)DBG_DebugBreak_End);
#endif
}

/*++
Function:
  GetThreadContext

See MSDN doc.
--*/
BOOL
PALAPI
GetThreadContext(
           IN HANDLE hThread,
           IN OUT LPCONTEXT lpContext)
{
    PAL_ERROR palError;
    CPalThread *pThread;
    CPalThread *pTargetThread;
    IPalObject *pobjThread = NULL;
    BOOL ret = FALSE;

    PERF_ENTRY(GetThreadContext);
    ENTRY("GetThreadContext (hThread=%p, lpContext=%p)\n",hThread,lpContext);

    pThread = InternalGetCurrentThread();

    palError = InternalGetThreadDataFromHandle(
        pThread,
        hThread,
        &pTargetThread,
        &pobjThread
        );

    if (NO_ERROR == palError)
    {
        if (!pTargetThread->IsDummy())
        {
            ret = CONTEXT_GetThreadContext(
                gPID,
                pTargetThread->GetPThreadSelf(),
                lpContext
                );
        }
        else
        {
            ASSERT("Dummy thread handle passed to GetThreadContext\n");
            pThread->SetLastError(ERROR_INVALID_HANDLE);
        }
    }
    else
    {
        pThread->SetLastError(palError);
    }

    if (NULL != pobjThread)
    {
        pobjThread->ReleaseReference(pThread);
    }

    LOGEXIT("GetThreadContext returns ret:%d\n", ret);
    PERF_EXIT(GetThreadContext);
    return ret;
}

/*++
Function:
  SetThreadContext

See MSDN doc.
--*/
BOOL
PALAPI
SetThreadContext(
           IN HANDLE hThread,
           IN CONST CONTEXT *lpContext)
{
    PAL_ERROR palError;
    CPalThread *pThread;
    CPalThread *pTargetThread;
    IPalObject *pobjThread = NULL;
    BOOL ret = FALSE;

    PERF_ENTRY(SetThreadContext);
    ENTRY("SetThreadContext (hThread=%p, lpContext=%p)\n",hThread,lpContext);

    pThread = InternalGetCurrentThread();

    palError = InternalGetThreadDataFromHandle(
        pThread,
        hThread,
        &pTargetThread,
        &pobjThread
        );

    if (NO_ERROR == palError)
    {
        if (!pTargetThread->IsDummy())
        {
            ret = CONTEXT_SetThreadContext(
                gPID,
                pTargetThread->GetPThreadSelf(),
                lpContext
                );
        }
        else
        {
            ASSERT("Dummy thread handle passed to SetThreadContext\n");
            pThread->SetLastError(ERROR_INVALID_HANDLE);
        }
    }
    else
    {
        pThread->SetLastError(palError);
    }

    if (NULL != pobjThread)
    {
        pobjThread->ReleaseReference(pThread);
    }

    return ret;
}



} // extern "C"
