// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    debug.c

Abstract:

    Implementation of Win32 debugging API functions.

Revision History:



--*/

#ifndef BIT64
#undef _LARGEFILE64_SOURCE
#undef _FILE_OFFSET_BITS
#endif

#include "pal/dbgmsg.h"
SET_DEFAULT_DEBUG_CHANNEL(DEBUG); // some headers have code with asserts, so do this first

#include "pal/thread.hpp"
#include "pal/procobj.hpp"
#include "pal/file.hpp"

#include "pal/palinternal.h"
#include "pal/process.h"
#include "pal/context.h"
#include "pal/debug.h"
#include "pal/environ.h"
#include "pal/malloc.hpp"
#include "pal/module.h"
#include "pal/stackstring.hpp"
#include "pal/virtual.h"

#include <signal.h>
#include <unistd.h>
#if HAVE_PROCFS_CTL
#include <unistd.h>
#elif HAVE_TTRACE // HAVE_PROCFS_CTL
#include <sys/ttrace.h>
#else // HAVE_TTRACE
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

#if HAVE_MACH_EXCEPTIONS
#include "../exception/machexception.h"
#endif // HAVE_MACH_EXCEPTIONS

using namespace CorUnix;

extern "C" void DBG_DebugBreak_End();

#if HAVE_PROCFS_CTL
#define CTL_ATTACH      "attach"
#define CTL_DETACH      "detach"
#define CTL_WAIT        "wait"
#endif   // HAVE_PROCFS_CTL

/* ------------------- Constant definitions ----------------------------------*/

#if !HAVE_VM_READ && !HAVE_PROCFS_CTL
const BOOL DBG_ATTACH       = TRUE;
const BOOL DBG_DETACH       = FALSE;
#endif
static const char PAL_OUTPUTDEBUGSTRING[]    = "PAL_OUTPUTDEBUGSTRING";

#ifdef _DEBUG
#define ENABLE_RUN_ON_DEBUG_BREAK 1
#endif // _DEBUG

#ifdef ENABLE_RUN_ON_DEBUG_BREAK
static const char PAL_RUN_ON_DEBUG_BREAK[]   = "PAL_RUN_ON_DEBUG_BREAK";
#endif // ENABLE_RUN_ON_DEBUG_BREAK

/* ------------------- Static function prototypes ----------------------------*/

#if !HAVE_VM_READ && !HAVE_PROCFS_CTL && !HAVE_TTRACE
static int
DBGWriteProcMem_Int(DWORD processId, int *addr, int data);
static int
DBGWriteProcMem_IntWithMask(DWORD processId, int *addr, int data,
                            unsigned int mask);
#endif  // !HAVE_VM_READ && !HAVE_PROCFS_CTL && !HAVE_TTRACE

#if !HAVE_VM_READ && !HAVE_PROCFS_CTL

static BOOL 
DBGAttachProcess(CPalThread *pThread, HANDLE hProcess, DWORD dwProcessId);

static BOOL
DBGDetachProcess(CPalThread *pThread, HANDLE hProcess, DWORD dwProcessId);

static int
DBGSetProcessAttached(CPalThread *pThread, HANDLE hProcess, BOOL bAttach);

#endif // !HAVE_VM_READ && !HAVE_PROCFS_CTL

extern "C" {

/*++
Function:
  FlushInstructionCache

The FlushInstructionCache function flushes the instruction cache for
the specified process.

Remarks

This is a no-op for x86 architectures where the instruction and data
caches are coherent in hardware. For non-X86 architectures, this call
usually maps to a kernel API to flush the D-caches on all processors.

--*/
BOOL
PALAPI
FlushInstructionCache(
        IN HANDLE hProcess,
        IN LPCVOID lpBaseAddress,
        IN SIZE_T dwSize)
{
    BOOL Ret;

    PERF_ENTRY(FlushInstructionCache);
    ENTRY("FlushInstructionCache (hProcess=%p, lpBaseAddress=%p dwSize=%d)\
          \n", hProcess, lpBaseAddress, dwSize);

    if (lpBaseAddress != NULL)
    {
        Ret = DBG_FlushInstructionCache(lpBaseAddress, dwSize);
    }
    else
    {
        Ret = TRUE;
    }

    LOGEXIT("FlushInstructionCache returns BOOL %d\n", Ret);
    PERF_EXIT(FlushInstructionCache);
    return Ret;
}


/*++
Function:
  OutputDebugStringA

See MSDN doc.
--*/
VOID
PALAPI
OutputDebugStringA(
        IN LPCSTR lpOutputString)
{
    PERF_ENTRY(OutputDebugStringA);
    ENTRY("OutputDebugStringA (lpOutputString=%p (%s))\n",
          lpOutputString ? lpOutputString : "NULL",
          lpOutputString ? lpOutputString : "NULL");

    // As we don't support debug events, we are going to output the debug string
    // to stderr instead of generating OUT_DEBUG_STRING_EVENT. It's safe to tell
    // EnvironGetenv not to make a copy of the value here since we only want to
    // check whether it exists, not actually use it.
    if ((lpOutputString != NULL) &&
        (NULL != EnvironGetenv(PAL_OUTPUTDEBUGSTRING, /* copyValue */ FALSE)))
    {
        fprintf(stderr, "%s", lpOutputString);
    }

    LOGEXIT("OutputDebugStringA returns\n");
    PERF_EXIT(OutputDebugStringA);
}

/*++
Function:
  OutputDebugStringW

See MSDN doc.
--*/
VOID
PALAPI
OutputDebugStringW(
        IN LPCWSTR lpOutputString)
{
    CHAR *lpOutputStringA;
    int strLen;

    PERF_ENTRY(OutputDebugStringW);
    ENTRY("OutputDebugStringW (lpOutputString=%p (%S))\n",
          lpOutputString ? lpOutputString: W16_NULLSTRING,
          lpOutputString ? lpOutputString: W16_NULLSTRING);
    
    if (lpOutputString == NULL) 
    {
        OutputDebugStringA("");
        goto EXIT;
    }

    if ((strLen = WideCharToMultiByte(CP_ACP, 0, lpOutputString, -1, NULL, 0, 
                                      NULL, NULL)) 
        == 0)
    {
        ASSERT("failed to get wide chars length\n");
        SetLastError(ERROR_INTERNAL_ERROR);
        goto EXIT;
    }

    /* strLen includes the null terminator */
    if ((lpOutputStringA = (LPSTR) InternalMalloc((strLen * sizeof(CHAR)))) == NULL)
    {
        ERROR("Insufficient memory available !\n");
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto EXIT;
    }

    if(! WideCharToMultiByte(CP_ACP, 0, lpOutputString, -1, 
                             lpOutputStringA, strLen, NULL, NULL)) 
    {
        ASSERT("failed to convert wide chars to multibytes\n");
        SetLastError(ERROR_INTERNAL_ERROR);
        free(lpOutputStringA);
        goto EXIT;
    }
    
    OutputDebugStringA(lpOutputStringA);
    free(lpOutputStringA);

EXIT:
    LOGEXIT("OutputDebugStringW returns\n");
    PERF_EXIT(OutputDebugStringW);
}

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

static
int
run_debug_command (const char *command)
{
    int pid;
    Volatile<int> spin = 1;

    if (!command) {
        return 1;
    }

    printf("Spawning command: %s\n", command);
    
    pid = fork();
    if (pid == -1) {
        return -1;
    }
    if (pid == 0) {
        const char *argv[4] = { "sh", "-c", command, 0 };
        execv("/bin/sh", (char **)argv);
        exit(127);
    }

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

#define PID_TEXT "PAL_EXE_PID="
#define EXE_TEXT "PAL_EXE_NAME="

static
int
DebugBreakCommand()
{
#ifdef ENABLE_RUN_ON_DEBUG_BREAK
    extern MODSTRUCT exe_module;

    char *command_string = EnvironGetenv(PAL_RUN_ON_DEBUG_BREAK);
    if (command_string)
    {
        char pid_buf[sizeof (PID_TEXT) + 32];
        PathCharString exe_bufString;
        int libNameLength = 10;

        if (exe_module.lib_name != NULL)
        {
            libNameLength = PAL_wcslen(exe_module.lib_name);
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

        if (snprintf (exe_buf, sizeof (CHAR) * (dwexe_buf + 1), EXE_TEXT "%ls", (wchar_t *)exe_module.lib_name) <= 0)
        {
            goto FAILED;
        }

        exe_bufString.CloseBuffer(dwexe_buf);
        /* strictly speaking, we might want to only set these environment
           variables in the child process, but if we do that we can't check
           for errors. putenv/setenv can fail when out of memory */

        if (!EnvironPutenv (pid_buf, FALSE) || !EnvironPutenv (exe_buf, FALSE))
        {
            goto FAILED;
        }

        if (run_debug_command (command_string))
        {
            goto FAILED;
        }

        free(command_string);
        return 1;
    }

    return 0;

FAILED:
    if (command_string)
    {
        free(command_string);
    }

    fprintf (stderr, "Failed to execute command: '%s'\n", command_string);
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
    return (addr >= (void *)DBG_DebugBreak) && (addr <= (void *)DBG_DebugBreak_End);
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
        0, // THREAD_GET_CONTEXT
        &pTargetThread,
        &pobjThread
        );

    if (NO_ERROR == palError)
    {
        if (!pTargetThread->IsDummy())
        {
            ret = CONTEXT_GetThreadContext(
                GetCurrentProcessId(),
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
        0, // THREAD_SET_CONTEXT
        &pTargetThread,
        &pobjThread
        );

    if (NO_ERROR == palError)
    {
        if (!pTargetThread->IsDummy())
        {
            ret = CONTEXT_SetThreadContext(
                GetCurrentProcessId(),
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

#if !HAVE_VM_READ && !HAVE_PROCFS_CTL && !HAVE_TTRACE
/*++
Function:
  DBGWriteProcMem_Int

Abstract
  write one int to a process memory address

Parameter
  processId : process handle
  addr : memory address where the int should be written
  data : int to be written in addr

Return
  Return 1 if it succeeds, or 0 if it's fails
--*/
static
int
DBGWriteProcMem_Int(IN DWORD processId, 
                    IN int *addr,
                    IN int data)
{
    if (PAL_PTRACE( PAL_PT_WRITE_D, processId, addr, data ) == -1)
    {
        if (errno == EFAULT) 
        {
            ERROR("ptrace(PT_WRITE_D, pid:%d caddr_t:%p data:%x) failed "
                  "errno:%d (%s)\n", processId, addr, data, errno, strerror(errno));
            SetLastError(ERROR_INVALID_ADDRESS);
        }
        else
        {
            ASSERT("ptrace(PT_WRITE_D, pid:%d caddr_t:%p data:%x) failed "
                  "errno:%d (%s)\n", processId, addr, data, errno, strerror(errno));
            SetLastError(ERROR_INTERNAL_ERROR);
        }
        return 0;
    }

    return 1;
}

/*++
Function:
  DBGWriteProcMem_IntWithMask

Abstract
  write one int to a process memory address space using mask

Parameter
  processId : process ID
  addr : memory address where the int should be written
  data : int to be written in addr
  mask : the mask used to write only a parts of data

Return
  Return 1 if it succeeds, or 0 if it's fails
--*/
static
int
DBGWriteProcMem_IntWithMask(IN DWORD processId,
                            IN int *addr,
                            IN int data,
                            IN unsigned int mask )
{
    int readInt;

    if (mask != ~0)
    {
        errno = 0;
        if (((readInt = PAL_PTRACE( PAL_PT_READ_D, processId, addr, 0 )) == -1)
             && errno)
        {
            if (errno == EFAULT) 
            {
                ERROR("ptrace(PT_READ_D, pid:%d, caddr_t:%p, 0) failed "
                      "errno:%d (%s)\n", processId, addr, errno, strerror(errno));
                SetLastError(ERROR_INVALID_ADDRESS);
            }
            else
            {
                ASSERT("ptrace(PT_READ_D, pid:%d, caddr_t:%p, 0) failed "
                      "errno:%d (%s)\n", processId, addr, errno, strerror(errno));
                SetLastError(ERROR_INTERNAL_ERROR);
            }

            return 0;
        }
        data = (data & mask) | (readInt & ~mask);
    }    
    return DBGWriteProcMem_Int(processId, addr, data);
}
#endif  // !HAVE_VM_READ && !HAVE_PROCFS_CTL && !HAVE_TTRACE

#if !HAVE_VM_READ && !HAVE_PROCFS_CTL

/*++
Function:
  DBGAttachProcess

Abstract  
  
  Attach the indicated process to the current process. 
  
  if the indicated process is already attached by the current process, then 
  increment the number of attachment pending. if ot, attach it to the current 
  process (with PT_ATTACH).

Parameter
  hProcess : handle to process to attach to
  processId : process ID to attach
Return
  Return true if it succeeds, or false if it's fails
--*/
static
BOOL 
DBGAttachProcess(
    CPalThread *pThread,
    HANDLE hProcess,
    DWORD processId
    )
{
    int attchmentCount;
    int savedErrno;
#if HAVE_PROCFS_CTL
    int fd = -1;
    char ctlPath[1024];
#endif  // HAVE_PROCFS_CTL

    attchmentCount = 
        DBGSetProcessAttached(pThread, hProcess, DBG_ATTACH);

    if (attchmentCount == -1)
    {
        /* Failed to set the process as attached */
        goto EXIT;
    }
    
    if (attchmentCount == 1)
    {
#if HAVE_PROCFS_CTL
        struct timespec waitTime;

        // FreeBSD has some trouble when a series of attach/detach sequences
        // occurs too close together.  When this happens, we'll be able to
        // attach to the process, but waiting for the process to stop
        // (either via writing "wait" to /proc/<pid>/ctl or via waitpid)
        // will hang.  If we pause for a very short amount of time before
        // trying to attach, we don't run into this situation.
        waitTime.tv_sec = 0;
        waitTime.tv_nsec = 50000000;
        nanosleep(&waitTime, NULL);
        
        sprintf_s(ctlPath, sizeof(ctlPath), "/proc/%d/ctl", processId);
        fd = InternalOpen(ctlPath, O_WRONLY);
        if (fd == -1)
        {
            ERROR("Failed to open %s: errno is %d (%s)\n", ctlPath,
                  errno, strerror(errno));
            goto DETACH1;
        }
        
        if (write(fd, CTL_ATTACH, sizeof(CTL_ATTACH)) < (int)sizeof(CTL_ATTACH))
        {
            ERROR("Failed to attach to %s: errno is %d (%s)\n", ctlPath,
                  errno, strerror(errno));
            close(fd);
            goto DETACH1;
        }
        
        if (write(fd, CTL_WAIT, sizeof(CTL_WAIT)) < (int)sizeof(CTL_WAIT))
        {
            ERROR("Failed to wait for %s: errno is %d (%s)\n", ctlPath,
                  errno, strerror(errno));
            goto DETACH2;
        }
        
        close(fd);
#elif HAVE_TTRACE
        if (ttrace(TT_PROC_ATTACH, processId, 0, TT_DETACH_ON_EXIT, TT_VERSION, 0) == -1)
        {
            if (errno != ESRCH)
            {                
                ASSERT("ttrace(TT_PROC_ATTACH, pid:%d) failed errno:%d (%s)\n",
                     processId, errno, strerror(errno));
            }
            goto DETACH1;
        }
#else   // HAVE_TTRACE
        if (PAL_PTRACE( PAL_PT_ATTACH, processId, 0, 0 ) == -1)
        {
            if (errno != ESRCH)
            {                
                ASSERT("ptrace(PT_ATTACH, pid:%d) failed errno:%d (%s)\n",
                     processId, errno, strerror(errno));
            }
            goto DETACH1;
        }
                    
        if (waitpid(processId, NULL, WUNTRACED) == -1)
        {
            if (errno != ESRCH)
            {
                ASSERT("waitpid(pid:%d, NULL, WUNTRACED) failed.errno:%d"
                       " (%s)\n", processId, errno, strerror(errno));
            }
            goto DETACH2;
        }
#endif  // HAVE_PROCFS_CTL
    }
    
    return TRUE;

#if HAVE_PROCFS_CTL
DETACH2:
    if (write(fd, CTL_DETACH, sizeof(CTL_DETACH)) < (int)sizeof(CTL_DETACH))
    {
        ASSERT("Failed to detach from %s: errno is %d (%s)\n", ctlPath,
               errno, strerror(errno));
    }
    close(fd);
#elif !HAVE_TTRACE
DETACH2:
    if (PAL_PTRACE(PAL_PT_DETACH, processId, 0, 0) == -1)
    {
        ASSERT("ptrace(PT_DETACH, pid:%d) failed. errno:%d (%s)\n", processId, 
              errno, strerror(errno));
    }
#endif  // HAVE_PROCFS_CTL

DETACH1:
    savedErrno = errno;
    DBGSetProcessAttached(pThread, hProcess, DBG_DETACH);
    errno = savedErrno;
EXIT:
    if (errno == ESRCH || errno == ENOENT || errno == EBADF)
    {
        ERROR("Invalid process ID:%d\n", processId);
        SetLastError(ERROR_INVALID_PARAMETER);
    }
    else
    {
        SetLastError(ERROR_INTERNAL_ERROR);
    }
    return FALSE;
}

/*++
Function:
  DBGDetachProcess

Abstract
  Detach the indicated process from the current process.
  
  if the indicated process is already attached by the current process, then 
  decrement the number of attachment pending and detach it from the current 
  process (with PT_DETACH) if there's no more attachment left. 
  
Parameter
  hProcess : process handle
  processId : process ID

Return
  Return true if it succeeds, or true if it's fails
--*/
static
BOOL
DBGDetachProcess(
    CPalThread *pThread,
    HANDLE hProcess,
    DWORD processId
    )
{     
    int nbAttachLeft;
#if HAVE_PROCFS_CTL
    int fd = -1;
    char ctlPath[1024];
#endif  // HAVE_PROCFS_CTL

    nbAttachLeft = DBGSetProcessAttached(pThread, hProcess, DBG_DETACH);    

    if (nbAttachLeft == -1)
    {
        /* Failed to set the process as detached */
        return FALSE;
    }
    
    /* check if there's no more attachment left on processId */
    if (nbAttachLeft == 0)
    {
#if HAVE_PROCFS_CTL
        sprintf(ctlPath, sizeof(ctlPath), "/proc/%d/ctl", processId);
        fd = InternalOpen(pThread, ctlPath, O_WRONLY);
        if (fd == -1)
        {
            if (errno == ENOENT)
            {
                ERROR("Invalid process ID: %d\n", processId);
                SetLastError(ERROR_INVALID_PARAMETER);
            }
            else
            {
                ERROR("Failed to open %s: errno is %d (%s)\n", ctlPath,
                      errno, strerror(errno));
                SetLastError(ERROR_INTERNAL_ERROR);
            }
            return FALSE;
        }
        
        if (write(fd, CTL_DETACH, sizeof(CTL_DETACH)) < (int)sizeof(CTL_DETACH))
        {
            ERROR("Failed to detach from %s: errno is %d (%s)\n", ctlPath,
                  errno, strerror(errno));
            close(fd);
            return FALSE;
        }
        close(fd);

#elif HAVE_TTRACE  
        if (ttrace(TT_PROC_DETACH, processId, 0, 0, 0, 0) == -1)
        {
            if (errno == ESRCH)
            {
                ERROR("Invalid process ID: %d\n", processId);
                SetLastError(ERROR_INVALID_PARAMETER);
            }
            else
            {
                ASSERT("ttrace(TT_PROC_DETACH, pid:%d) failed. errno:%d (%s)\n", 
                      processId, errno, strerror(errno));
                SetLastError(ERROR_INTERNAL_ERROR);
            }
            return FALSE;
        }
#else   // HAVE_TTRACE
        if (PAL_PTRACE(PAL_PT_DETACH, processId, 1, 0) == -1)
        {            
            if (errno == ESRCH)
            {
                ERROR("Invalid process ID: %d\n", processId);
                SetLastError(ERROR_INVALID_PARAMETER);
            }
            else
            {
                ASSERT("ptrace(PT_DETACH, pid:%d) failed. errno:%d (%s)\n", 
                      processId, errno, strerror(errno));
                SetLastError(ERROR_INTERNAL_ERROR);
            }
            return FALSE;
        }
#endif  // HAVE_PROCFS_CTL

#if !HAVE_TTRACE
        if (kill(processId, SIGCONT) == -1)
        {
            ERROR("Failed to continue the detached process:%d errno:%d (%s)\n",
                  processId, errno, strerror(errno));
            return FALSE;
        }
#endif  // !HAVE_TTRACE        
    }
    return TRUE;
}

/*++
Function:
  DBGSetProcessAttached

Abstract
  saves the current process Id in the attached process structure

Parameter
  hProcess : process handle
  bAttach : true (false) to set the process as attached (as detached)
Return
 returns the number of attachment left on attachedProcId, or -1 if it fails
--*/
static int
DBGSetProcessAttached(
    CPalThread *pThread,
    HANDLE hProcess,
    BOOL  bAttach
    )
{
    PAL_ERROR palError = NO_ERROR;
    IPalObject *pobjProcess = NULL;
    IDataLock *pDataLock = NULL;
    CProcProcessLocalData *pLocalData = NULL;
    int ret = -1;
    CAllowedObjectTypes aotProcess(otiProcess);

    palError = g_pObjectManager->ReferenceObjectByHandle(
        pThread,
        hProcess,
        &aotProcess,
        0,
        &pobjProcess
        );

    if (NO_ERROR != palError)
    {
        goto DBGSetProcessAttachedExit;
    }

    palError = pobjProcess->GetProcessLocalData(
        pThread,
        WriteLock,
        &pDataLock,
        reinterpret_cast<void **>(&pLocalData)
        );

    if (NO_ERROR != palError)
    {
        goto DBGSetProcessAttachedExit;
    }

    if (bAttach)
    {
        pLocalData->lAttachCount += 1;
    }
    else
    {
        pLocalData->lAttachCount -= 1;

        if (pLocalData->lAttachCount < 0)
        {
            ASSERT("pLocalData->lAttachCount < 0 check for extra DBGDetachProcess calls\n");
            palError = ERROR_INTERNAL_ERROR;
            goto DBGSetProcessAttachedExit;
        }
    }

    ret = pLocalData->lAttachCount;
    
DBGSetProcessAttachedExit:

    if (NULL != pDataLock)
    {
        pDataLock->ReleaseLock(pThread, TRUE);
    }

    if (NULL != pobjProcess)
    {
        pobjProcess->ReleaseReference(pThread);
    }
    
    return ret;
}

#endif // !HAVE_VM_READ && !HAVE_PROCFS_CTL

/*++
Function:
  PAL_CreateExecWatchpoint

Abstract
  Creates an OS exec watchpoint for the specified instruction
  and thread. This function should only be called on architectures
  that do not support a hardware single-step mode (e.g., SPARC).

Parameter
  hThread : the thread for which the watchpoint is to apply
  pvInstruction : the instruction on which the watchpoint is to be set

Return
  A Win32 error code
--*/

DWORD
PAL_CreateExecWatchpoint(
    HANDLE hThread,
    PVOID pvInstruction
    )
{
    PERF_ENTRY(PAL_CreateExecWatchpoint);
    ENTRY("PAL_CreateExecWatchpoint (hThread=%p, pvInstruction=%p)\n", hThread, pvInstruction);

    DWORD dwError = ERROR_NOT_SUPPORTED;

#if HAVE_PRWATCH_T

    CPalThread *pThread = NULL;
    CPalThread *pTargetThread = NULL;
    IPalObject *pobjThread = NULL;
    int fd = -1;
    char ctlPath[50];

    struct
    {
        long ctlCode;
        prwatch_t prwatch;
    } ctlStruct;

    //
    // We must never set a watchpoint on an instruction that enters a syscall;
    // if such a request comes in we succeed it w/o actually creating the
    // watchpoint. This mirrors the behavior of setting the single-step flag
    // in a thread context when the thread is w/in a system service -- the
    // flag is ignored and will not be present when the thread returns
    // to user mode.
    //

#if defined(_SPARC_)
    if (*(DWORD*)pvInstruction == 0x91d02008) // ta 8
    {
        TRACE("Watchpoint requested on sysenter instruction -- ignoring");
        dwError = ERROR_SUCCESS;
        goto PAL_CreateExecWatchpointExit;        
    }
#else
#error Need syscall instruction for this platform
#endif // _SPARC_

    pThread = InternalGetCurrentThread();

    dwError = InternalGetThreadDataFromHandle(
        pThread,
        hThread,
        0, // THREAD_SET_CONTEXT
        &pTargetThread,
        &pobjThread
        );

    if (NO_ERROR != dwError)
    {
        goto PAL_CreateExecWatchpointExit;
    }

    snprintf(ctlPath, sizeof(ctlPath), "/proc/%u/lwp/%u/lwpctl", getpid(), pTargetThread->GetLwpId());

    fd = InternalOpen(pThread, ctlPath, O_WRONLY);
    if (-1 == fd)
    {
        ERROR("Failed to open %s\n", ctlPath);
        dwError = ERROR_INVALID_ACCESS;
        goto PAL_CreateExecWatchpointExit;
    }

    ctlStruct.ctlCode = PCWATCH;
    ctlStruct.prwatch.pr_vaddr = (uintptr_t) pvInstruction;
    ctlStruct.prwatch.pr_size = sizeof(DWORD);
    ctlStruct.prwatch.pr_wflags = WA_EXEC | WA_TRAPAFTER;

    if (write(fd, (void*) &ctlStruct, sizeof(ctlStruct)) != sizeof(ctlStruct))
    {
        ERROR("Failure writing control structure (errno = %u)\n", errno);
        dwError = ERROR_INTERNAL_ERROR;
        goto PAL_CreateExecWatchpointExit;
    }

    dwError = ERROR_SUCCESS;
    
PAL_CreateExecWatchpointExit:

    if (NULL != pobjThread)
    {
        pobjThread->ReleaseReference(pThread);
    }

    if (-1 != fd)
    {
        close(fd);
    }

#endif // HAVE_PRWATCH_T     
    
    LOGEXIT("PAL_CreateExecWatchpoint returns ret:%d\n", dwError);
    PERF_EXIT(PAL_CreateExecWatchpoint);
    return dwError;
}

/*++
Function:
  PAL_DeleteExecWatchpoint

Abstract
  Deletes an OS exec watchpoint for the specified instruction
  and thread. This function should only be called on architectures
  that do not support a hardware single-step mode (e.g., SPARC).

Parameter
  hThread : the thread to remove the watchpoint from
  pvInstruction : the instruction for which the watchpoint is to be removed

Return
  A Win32 error code. Attempting to delete a watchpoint that does not exist
  may or may not result in an error, depending on the behavior of the
  underlying operating system.
--*/

DWORD
PAL_DeleteExecWatchpoint(
    HANDLE hThread,
    PVOID pvInstruction
    )
{
    PERF_ENTRY(PAL_DeleteExecWatchpoint);
    ENTRY("PAL_DeleteExecWatchpoint (hThread=%p, pvInstruction=%p)\n", hThread, pvInstruction);

    DWORD dwError = ERROR_NOT_SUPPORTED;

#if HAVE_PRWATCH_T

    CPalThread *pThread = NULL;
    CPalThread *pTargetThread = NULL;
    IPalObject *pobjThread = NULL;
    int fd = -1;
    char ctlPath[50];

    struct
    {
        long ctlCode;
        prwatch_t prwatch;
    } ctlStruct;


    pThread = InternalGetCurrentThread();

    dwError = InternalGetThreadDataFromHandle(
        pThread,
        hThread,
        0, // THREAD_SET_CONTEXT
        &pTargetThread,
        &pobjThread
        );

    if (NO_ERROR != dwError)
    {
        goto PAL_DeleteExecWatchpointExit;
    }

    snprintf(ctlPath, sizeof(ctlPath), "/proc/%u/lwp/%u/lwpctl", getpid(), pTargetThread->GetLwpId());

    fd = InternalOpen(pThread, ctlPath, O_WRONLY);
    if (-1 == fd)
    {
        ERROR("Failed to open %s\n", ctlPath);
        dwError = ERROR_INVALID_ACCESS;
        goto PAL_DeleteExecWatchpointExit;
    }

    ctlStruct.ctlCode = PCWATCH;
    ctlStruct.prwatch.pr_vaddr = (uintptr_t) pvInstruction;
    ctlStruct.prwatch.pr_size = sizeof(DWORD);
    ctlStruct.prwatch.pr_wflags = 0;

    if (write(fd, (void*) &ctlStruct, sizeof(ctlStruct)) != sizeof(ctlStruct))
    {
        ERROR("Failure writing control structure (errno = %u)\n", errno);
        dwError = ERROR_INTERNAL_ERROR;
        goto PAL_DeleteExecWatchpointExit;
    }

    dwError = ERROR_SUCCESS;
    
PAL_DeleteExecWatchpointExit:

    if (NULL != pobjThread)
    {
        pobjThread->ReleaseReference(pThread);
    }

    if (-1 != fd)
    {
        close(fd);
    }

#endif // HAVE_PRWATCH_T    
    
    LOGEXIT("PAL_DeleteExecWatchpoint returns ret:%d\n", dwError);
    PERF_EXIT(PAL_DeleteExecWatchpoint);
    return dwError;
}

// We want to enable hardware exception handling for ReadProcessMemory
// and WriteProcessMemory in all cases since it is acceptable if they
// hit AVs, so redefine HardwareExceptionHolder for these two functions
// (here to the end of the file).
#undef HardwareExceptionHolder
#define HardwareExceptionHolder CatchHardwareExceptionHolder __catchHardwareException;

/*++
Function:
  ReadProcessMemory

See MSDN doc.
--*/
BOOL
PALAPI
ReadProcessMemory(
           IN HANDLE hProcess,
           IN LPCVOID lpBaseAddress,
           IN LPVOID lpBuffer,
           IN SIZE_T nSize,
           OUT SIZE_T * lpNumberOfBytesRead
           )
{
    CPalThread *pThread;
    DWORD processId;
    Volatile<BOOL> ret = FALSE;
    Volatile<SIZE_T> numberOfBytesRead = 0;
#if HAVE_VM_READ
    kern_return_t result;
    vm_map_t task;
    LONG_PTR bytesToRead;
#elif HAVE_PROCFS_CTL
    int fd = -1;
    char memPath[64];
    off_t offset;
#elif !HAVE_TTRACE
    SIZE_T nbInts;
    int* ptrInt;
    int* lpTmpBuffer;
#endif
#if !HAVE_PROCFS_CTL && !HAVE_TTRACE
    int* lpBaseAddressAligned;
    SIZE_T offset;
#endif  // !HAVE_PROCFS_CTL && !HAVE_TTRACE

    PERF_ENTRY(ReadProcessMemory);
    ENTRY("ReadProcessMemory (hProcess=%p,lpBaseAddress=%p, lpBuffer=%p, "
          "nSize=%u, lpNumberOfBytesRead=%p)\n",hProcess,lpBaseAddress,
          lpBuffer, (unsigned int)nSize, lpNumberOfBytesRead);

    pThread = InternalGetCurrentThread();
    
    if (!(processId = PROCGetProcessIDFromHandle(hProcess)))
    {
        ERROR("Invalid process handler hProcess:%p.",hProcess);
        SetLastError(ERROR_INVALID_HANDLE);
        goto EXIT;
    }
    
    // Check if the read request is for the current process. 
    // We don't need ptrace in that case.
    if (GetCurrentProcessId() == processId) 
    {
        TRACE("We are in the same process, so ptrace is not needed\n");
        
        struct Param
        {
            LPCVOID lpBaseAddress;
            LPVOID lpBuffer;
            SIZE_T nSize;
            SIZE_T numberOfBytesRead;
            BOOL ret;
        } param;
        param.lpBaseAddress = lpBaseAddress;
        param.lpBuffer = lpBuffer;
        param.nSize = nSize;
        param.numberOfBytesRead = numberOfBytesRead;
        param.ret = ret;

        PAL_TRY(Param *, pParam, &param)
        {
            SIZE_T i;
            
            // Seg fault in memcpy can't be caught
            // so we simulate the memcpy here

            for (i = 0; i<pParam->nSize; i++)
            {
                *((char*)(pParam->lpBuffer)+i) = *((char*)(pParam->lpBaseAddress)+i);
            }

            pParam->numberOfBytesRead = pParam->nSize;
            pParam->ret = TRUE;
        }
        PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
        {
            SetLastError(ERROR_ACCESS_DENIED);
        }
        PAL_ENDTRY

        numberOfBytesRead = param.numberOfBytesRead;
        ret = param.ret;
        goto EXIT;
    }

#if HAVE_VM_READ
    result = task_for_pid(mach_task_self(), processId, &task);
    if (result != KERN_SUCCESS)
    {
        ERROR("No Mach task for pid %d: %d\n", processId, ret.Load());
        SetLastError(ERROR_INVALID_HANDLE);
        goto EXIT;
    }
    // vm_read_overwrite usually requires that the address be page-aligned
    // and the size be a multiple of the page size.  We can't differentiate
    // between the cases in which that's required and those in which it
    // isn't, so we do it all the time.
    lpBaseAddressAligned = (int*)((SIZE_T) lpBaseAddress & ~VIRTUAL_PAGE_MASK);
    offset = ((SIZE_T) lpBaseAddress & VIRTUAL_PAGE_MASK);
    char *data;
    data = (char*)alloca(VIRTUAL_PAGE_SIZE);
    while (nSize > 0)
    {
        vm_size_t bytesRead;
        
        bytesToRead = VIRTUAL_PAGE_SIZE - offset;
        if (bytesToRead > (LONG_PTR)nSize)
        {
            bytesToRead = nSize;
        }
        bytesRead = VIRTUAL_PAGE_SIZE;
        result = vm_read_overwrite(task, (vm_address_t) lpBaseAddressAligned,
                                   VIRTUAL_PAGE_SIZE, (vm_address_t) data, &bytesRead);
        if (result != KERN_SUCCESS || bytesRead != VIRTUAL_PAGE_SIZE)
        {
            ERROR("vm_read_overwrite failed for %d bytes from %p in %d: %d\n",
                  VIRTUAL_PAGE_SIZE, (char *) lpBaseAddressAligned, task, result);
            if (result <= KERN_RETURN_MAX)
            {
                SetLastError(ERROR_INVALID_ACCESS);
            }
            else
            {
                SetLastError(ERROR_INTERNAL_ERROR);
            }
            goto EXIT;
        }
        memcpy((LPSTR)lpBuffer + numberOfBytesRead, data + offset, bytesToRead);
        numberOfBytesRead.Store(numberOfBytesRead.Load() + bytesToRead);
        lpBaseAddressAligned = (int*)((char*)lpBaseAddressAligned + VIRTUAL_PAGE_SIZE);
        nSize -= bytesToRead;
        offset = 0;
    }
    ret = TRUE;
#else   // HAVE_VM_READ
#if HAVE_PROCFS_CTL
    snprintf(memPath, sizeof(memPath), "/proc/%u/%s", processId, PROCFS_MEM_NAME);
    fd = InternalOpen(memPath, O_RDONLY);
    if (fd == -1)
    {
        ERROR("Failed to open %s\n", memPath);
        SetLastError(ERROR_INVALID_ACCESS);
        goto PROCFSCLEANUP;
    }

    //
    // off_t may be greater in size than void*, so first cast to
    // an unsigned type to ensure that no sign extension takes place
    //

    offset = (off_t) (UINT_PTR) lpBaseAddress;

    if (lseek(fd, offset, SEEK_SET) == -1)
    {
        ERROR("Failed to seek to base address\n");
        SetLastError(ERROR_INVALID_ACCESS);
        goto PROCFSCLEANUP;
    }
    
    numberOfBytesRead = read(fd, lpBuffer, nSize);
    ret = TRUE;

#else   // HAVE_PROCFS_CTL
    // Attach the process before calling ttrace/ptrace otherwise it fails.
    if (DBGAttachProcess(pThread, hProcess, processId))
    {
#if HAVE_TTRACE
        if (ttrace(TT_PROC_RDDATA, processId, 0, (__uint64_t)lpBaseAddress, (__uint64_t)nSize, (__uint64_t)lpBuffer) == -1)
        {
            if (errno == EFAULT) 
            {
                ERROR("ttrace(TT_PROC_RDDATA, pid:%d, 0, addr:%p, data:%d, addr2:%d) failed"
                      " errno=%d (%s)\n", processId, lpBaseAddress, (int)nSize, lpBuffer,
                      errno, strerror(errno));
                
                SetLastError(ERROR_ACCESS_DENIED);
            }
            else
            {
                ASSERT("ttrace(TT_PROC_RDDATA, pid:%d, 0, addr:%p, data:%d, addr2:%d) failed"
                      " errno=%d (%s)\n", processId, lpBaseAddress, (int)nSize, lpBuffer,
                      errno, strerror(errno));
                SetLastError(ERROR_INTERNAL_ERROR);
            }

            goto CLEANUP1;
        }

        numberOfBytesRead = nSize;
        ret = TRUE;
        
#else   // HAVE_TTRACE

        offset = (SIZE_T)lpBaseAddress % sizeof(int);
        lpBaseAddressAligned =  (int*)((char*)lpBaseAddress - offset);
        nbInts = (nSize + offset)/sizeof(int) + 
                 ((nSize + offset)%sizeof(int) ? 1:0);
        
        /* before transferring any data to lpBuffer we should make sure that all 
           data is accessible for read. so we need to use a temp buffer for that.*/
        if (!(lpTmpBuffer = (int*)InternalMalloc((nbInts * sizeof(int)))))
        {
            ERROR("Insufficient memory available !\n");
            SetLastError(ERROR_NOT_ENOUGH_MEMORY);
            goto CLEANUP1;
        }
        
        for (ptrInt = lpTmpBuffer; nbInts; ptrInt++,
            lpBaseAddressAligned++, nbInts--)
        {
            errno = 0;
            *ptrInt =
                PAL_PTRACE(PAL_PT_READ_D, processId, lpBaseAddressAligned, 0);
            if (*ptrInt == -1 && errno) 
            {
                if (errno == EFAULT) 
                {
                    ERROR("ptrace(PT_READ_D, pid:%d, addr:%p, data:0) failed"
                          " errno=%d (%s)\n", processId, lpBaseAddressAligned,
                          errno, strerror(errno));
                    
                    SetLastError(ptrInt == lpTmpBuffer ? ERROR_ACCESS_DENIED : 
                                                         ERROR_PARTIAL_COPY);
                }
                else
                {
                    ASSERT("ptrace(PT_READ_D, pid:%d, addr:%p, data:0) failed"
                          " errno=%d (%s)\n", processId, lpBaseAddressAligned,
                          errno, strerror(errno));
                    SetLastError(ERROR_INTERNAL_ERROR);
                }
                
                goto CLEANUP2;
            }
        }
        
        /* transfer data from temp buffer to lpBuffer */
        memcpy( (char *)lpBuffer, ((char*)lpTmpBuffer) + offset, nSize);
        numberOfBytesRead = nSize;
        ret = TRUE;
#endif // HAVE_TTRACE        
    }
    else
    {
        /* Failed to attach processId */
        goto EXIT;    
    }
#endif  // HAVE_PROCFS_CTL

#if HAVE_PROCFS_CTL
PROCFSCLEANUP:
    if (fd != -1)
    {
        close(fd);
    }    
#elif !HAVE_TTRACE
CLEANUP2:
    if (lpTmpBuffer) 
    {
        free(lpTmpBuffer);
    }
#endif  // !HAVE_TTRACE

#if !HAVE_PROCFS_CTL
CLEANUP1:
    if (!DBGDetachProcess(pThread, hProcess, processId))
    {
        /* Failed to detach processId */
        ret = FALSE;
    }
#endif  // HAVE_PROCFS_CTL
#endif  // HAVE_VM_READ

EXIT:
    if (lpNumberOfBytesRead)
    {
        *lpNumberOfBytesRead = numberOfBytesRead;
    }
    LOGEXIT("ReadProcessMemory returns BOOL %d\n", ret.Load());
    PERF_EXIT(ReadProcessMemory);
    return ret;
}

/*++
Function:
  WriteProcessMemory

See MSDN doc.
--*/
BOOL
PALAPI
WriteProcessMemory(
           IN HANDLE hProcess,
           IN LPVOID lpBaseAddress,
           IN LPCVOID lpBuffer,
           IN SIZE_T nSize,
           OUT SIZE_T * lpNumberOfBytesWritten
           )

{
    CPalThread *pThread;
    DWORD processId;
    Volatile<BOOL> ret = FALSE;
    Volatile<SIZE_T> numberOfBytesWritten = 0;
#if HAVE_VM_READ
    kern_return_t result;
    vm_map_t task;
#elif HAVE_PROCFS_CTL
    int fd = -1;
    char memPath[64];
    LONG_PTR bytesWritten;
    off_t offset;
#elif !HAVE_TTRACE
    SIZE_T FirstIntOffset;
    SIZE_T LastIntOffset;
    unsigned int FirstIntMask;
    unsigned int LastIntMask;
    SIZE_T nbInts;
    int *lpTmpBuffer = 0, *lpInt;
    int* lpBaseAddressAligned;
#endif

    PERF_ENTRY(WriteProcessMemory);
    ENTRY("WriteProcessMemory (hProcess=%p,lpBaseAddress=%p, lpBuffer=%p, "
           "nSize=%u, lpNumberOfBytesWritten=%p)\n",
           hProcess,lpBaseAddress, lpBuffer, (unsigned int)nSize, lpNumberOfBytesWritten); 

    pThread = InternalGetCurrentThread();
    
    if (!(nSize && (processId = PROCGetProcessIDFromHandle(hProcess))))
    {
        ERROR("Invalid nSize:%u number or invalid process handler "
              "hProcess:%p\n", (unsigned int)nSize, hProcess);
        SetLastError(ERROR_INVALID_PARAMETER);
        goto EXIT;
    }
    
    // Check if the write request is for the current process.
    // In that case we don't need ptrace.
    if (GetCurrentProcessId() == processId) 
    {
        TRACE("We are in the same process so we don't need ptrace\n");
        
        struct Param
        {
            LPVOID lpBaseAddress;
            LPCVOID lpBuffer;
            SIZE_T nSize;
            SIZE_T numberOfBytesWritten;
            BOOL ret;
        } param;
        param.lpBaseAddress = lpBaseAddress;
        param.lpBuffer = lpBuffer;
        param.nSize = nSize;
        param.numberOfBytesWritten = numberOfBytesWritten;
        param.ret = ret;

        PAL_TRY(Param *, pParam, &param)
        {
            SIZE_T i;
            
            // Seg fault in memcpy can't be caught
            // so we simulate the memcpy here

            for (i = 0; i<pParam->nSize; i++)
            {
                *((char*)(pParam->lpBaseAddress)+i) = *((char*)(pParam->lpBuffer)+i);
            }

            pParam->numberOfBytesWritten = pParam->nSize;
            pParam->ret = TRUE;
        } 
        PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
        {
            SetLastError(ERROR_ACCESS_DENIED);
        }
        PAL_ENDTRY

        numberOfBytesWritten = param.numberOfBytesWritten;
        ret = param.ret;
        goto EXIT;        
    }

#if HAVE_VM_READ
    result = task_for_pid(mach_task_self(), processId, &task);
    if (result != KERN_SUCCESS)
    {
        ERROR("No Mach task for pid %d: %d\n", processId, ret.Load());
        SetLastError(ERROR_INVALID_HANDLE);
        goto EXIT;
    }
    result = vm_write(task, (vm_address_t) lpBaseAddress, 
                      (vm_address_t) lpBuffer, nSize);
    if (result != KERN_SUCCESS)
    {
        ERROR("vm_write failed for %d bytes from %p in %d: %d\n",
              (int)nSize, lpBaseAddress, task, result);
        if (result <= KERN_RETURN_MAX)
        {
            SetLastError(ERROR_ACCESS_DENIED);
        }
        else
        {
            SetLastError(ERROR_INTERNAL_ERROR);
        }
        goto EXIT;
    }
    numberOfBytesWritten = nSize;
    ret = TRUE;
#else   // HAVE_VM_READ
#if HAVE_PROCFS_CTL
    snprintf(memPath, sizeof(memPath), "/proc/%u/%s", processId, PROCFS_MEM_NAME);
    fd = InternalOpen(memPath, O_WRONLY);
    if (fd == -1)
    {
        ERROR("Failed to open %s\n", memPath);
        SetLastError(ERROR_INVALID_ACCESS);
        goto PROCFSCLEANUP;
    }

    //
    // off_t may be greater in size than void*, so first cast to
    // an unsigned type to ensure that no sign extension takes place
    //

    offset = (off_t) (UINT_PTR) lpBaseAddress;

    if (lseek(fd, offset, SEEK_SET) == -1)
    {
        ERROR("Failed to seek to base address\n");
        SetLastError(ERROR_INVALID_ACCESS);
        goto PROCFSCLEANUP;
    }
    
    bytesWritten = write(fd, lpBuffer, nSize);
    if (bytesWritten < 0)
    {
        ERROR("Failed to write to %s\n", memPath);
        SetLastError(ERROR_INVALID_ACCESS);
        goto PROCFSCLEANUP;
    }

    numberOfBytesWritten = bytesWritten;
    ret = TRUE;

#else   // HAVE_PROCFS_CTL
    /* Attach the process before calling ptrace otherwise it fails */
    if (DBGAttachProcess(pThread, hProcess, processId))
    {
#if HAVE_TTRACE
        if (ttrace(TT_PROC_WRDATA, processId, 0, (__uint64_t)lpBaseAddress, (__uint64_t)nSize, (__uint64_t)lpBuffer) == -1)
        {
            if (errno == EFAULT) 
            {
                ERROR("ttrace(TT_PROC_WRDATA, pid:%d, addr:%p, data:%d, addr2:%d) failed"
                      " errno=%d (%s)\n", processId, lpBaseAddress, nSize, lpBuffer,
                      errno, strerror(errno));
                
                SetLastError(ERROR_ACCESS_DENIED);
            }
            else
            {
                ASSERT("ttrace(TT_PROC_WRDATA, pid:%d, addr:%p, data:%d, addr2:%d) failed"
                      " errno=%d (%s)\n", processId, lpBaseAddress, nSize, lpBuffer,
                      errno, strerror(errno));
                SetLastError(ERROR_INTERNAL_ERROR);
            }

            goto CLEANUP1;
        }

        numberOfBytesWritten = nSize;
        ret = TRUE;
        
#else   // HAVE_TTRACE

        FirstIntOffset = (SIZE_T)lpBaseAddress % sizeof(int);    
        FirstIntMask = -1;
        FirstIntMask <<= (FirstIntOffset * 8);
        
        nbInts = (nSize + FirstIntOffset) / sizeof(int) + 
                 (((nSize + FirstIntOffset)%sizeof(int)) ? 1:0);
        lpBaseAddressAligned = (int*)((char*)lpBaseAddress - FirstIntOffset);
        
        if ((lpTmpBuffer = (int*)InternalMalloc((nbInts * sizeof(int)))) == NULL)
        {
            ERROR("Insufficient memory available !\n");
            SetLastError(ERROR_NOT_ENOUGH_MEMORY);
            goto CLEANUP1;
        }

        memcpy((char *)lpTmpBuffer + FirstIntOffset, (char *)lpBuffer, nSize);
        lpInt = lpTmpBuffer;

        LastIntOffset = (nSize + FirstIntOffset) % sizeof(int);
        LastIntMask = -1;
        LastIntMask >>= ((sizeof(int) - LastIntOffset) * 8);

        if (nbInts == 1)
        {
            if (DBGWriteProcMem_IntWithMask(processId, lpBaseAddressAligned, 
                                            *lpInt,
                                            LastIntMask & FirstIntMask)
                  == 0)
            {
                goto CLEANUP2;
            }
            numberOfBytesWritten = nSize;
            ret = TRUE;
            goto CLEANUP2;
        }

        if (DBGWriteProcMem_IntWithMask(processId,
                                        lpBaseAddressAligned++,
                                        *lpInt++, FirstIntMask) 
            == 0)
        {
            goto CLEANUP2;
        }

        while (--nbInts > 1)
        {      
          if (DBGWriteProcMem_Int(processId, lpBaseAddressAligned++,
                                  *lpInt++) == 0)
          {
              goto CLEANUP2;
          }
        }
        
        if (DBGWriteProcMem_IntWithMask(processId, lpBaseAddressAligned,
                                        *lpInt, LastIntMask ) == 0)
        {
            goto CLEANUP2;
        }

        numberOfBytesWritten = nSize;
        ret = TRUE;
#endif  // HAVE_TTRACE
    }
    else
    {
        /* Failed to attach processId */
        goto EXIT;
    }
#endif // HAVE_PROCFS_CTL

#if HAVE_PROCFS_CTL
PROCFSCLEANUP:
    if (fd != -1)
    {
        close(fd);
    }
#elif !HAVE_TTRACE
CLEANUP2:
    if (lpTmpBuffer) 
    {
        free(lpTmpBuffer);
    }
#endif  // !HAVE_TTRACE

#if !HAVE_PROCFS_CTL
CLEANUP1:
    if (!DBGDetachProcess(pThread, hProcess, processId))
    {
        /* Failed to detach processId */
        ret = FALSE;
    }
#endif  // !HAVE_PROCFS_CTL
#endif  // HAVE_VM_READ

EXIT:
    if (lpNumberOfBytesWritten)
    {
        *lpNumberOfBytesWritten = numberOfBytesWritten;
    }

    LOGEXIT("WriteProcessMemory returns BOOL %d\n", ret.Load());
    PERF_EXIT(WriteProcessMemory);
    return ret;
}

} // extern "C"
