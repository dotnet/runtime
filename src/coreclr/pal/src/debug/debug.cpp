// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    debug.c

Abstract:

    Implementation of Win32 debugging API functions.

Revision History:



--*/


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
#include "pal/utils.h"

#include <signal.h>
#include <unistd.h>
#if HAVE_PROCFS_CTL
#include <unistd.h>
#elif defined(HAVE_TTRACE) // HAVE_PROCFS_CTL
#include <sys/ttrace.h>
#else // defined(HAVE_TTRACE)
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
#include <mach/mach_vm.h>
#endif // __APPLE__

#if HAVE_MACH_EXCEPTIONS
#include "../exception/machexception.h"
#endif // HAVE_MACH_EXCEPTIONS

using namespace CorUnix;

extern "C" void DBG_DebugBreak_End();
extern size_t OffsetWithinPage(off_t addr);

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

        if (snprintf (exe_buf, sizeof (CHAR) * (dwexe_buf + 1), EXE_TEXT "%ls", (wchar_t*)exe_module.lib_name) <= 0)
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
        fprintf (stderr, "Failed to execute command: '%s'\n", command_string);
        free(command_string);
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

/*++
Function:
  PAL_OpenProcessMemory

Abstract
  Creates the handle for PAL_ReadProcessMemory.

Parameter
  processId : process id to read memory
  pHandle : returns a platform specific handle or UINT32_MAX if failed

Return
  true successful, false invalid process id or not supported.
--*/
BOOL
PALAPI
PAL_OpenProcessMemory(
    IN DWORD processId,
    OUT DWORD* pHandle
)
{
    ENTRY("PAL_OpenProcessMemory(pid=%d)\n", processId);
    _ASSERTE(pHandle != nullptr);
    *pHandle = UINT32_MAX;
#ifdef __APPLE__
    mach_port_name_t port;
    kern_return_t result = ::task_for_pid(mach_task_self(), (int)processId, &port);
    if (result != KERN_SUCCESS)
    {
        ERROR("task_for_pid(%d) FAILED %x %s\n", processId, result, mach_error_string(result));
        LOGEXIT("PAL_OpenProcessMemory FALSE\n");
        return FALSE;
    }
    *pHandle = port;
#else
    char memPath[128];
    _snprintf_s(memPath, sizeof(memPath), sizeof(memPath), "/proc/%lu/mem", processId);

    int fd = open(memPath, O_RDONLY);
    if (fd == -1)
    {
        ERROR("open(%s) FAILED %d (%s)\n", memPath, errno, strerror(errno));
        LOGEXIT("PAL_OpenProcessMemory FALSE\n");
        return FALSE;
    }
    *pHandle = fd;
#endif
    LOGEXIT("PAL_OpenProcessMemory TRUE\n");
    return TRUE;
}

/*++
Function:
  PAL_CloseProcessMemory

Abstract
  Closes the PAL_OpenProcessMemory handle.

Parameter
  handle : from PAL_OpenProcessMemory

Return
  none
--*/
VOID
PALAPI
PAL_CloseProcessMemory(
    IN DWORD handle
)
{
    ENTRY("PAL_CloseProcessMemory(handle=%x)\n", handle);
    if (handle != UINT32_MAX)
    {
#ifdef __APPLE__
        kern_return_t result = ::mach_port_deallocate(mach_task_self(), (mach_port_name_t)handle);
        if (result != KERN_SUCCESS)
        {
            ERROR("mach_port_deallocate FAILED %x %s\n", result, mach_error_string(result));
        }
#else
        close(handle);
#endif
    }
    LOGEXIT("PAL_CloseProcessMemory\n");
}

/*++
Function:
  PAL_ReadProcessMemory

Abstract
  Reads process memory. 

Parameter
  handle : from PAL_OpenProcessMemory
  address : address of memory to read
  buffer : buffer to read memory to
  size : number of bytes to read
  numberOfBytesRead: number of bytes read (optional)

Return
  true read memory is successful, false if not.
--*/
BOOL
PALAPI
PAL_ReadProcessMemory(
    IN DWORD handle,
    IN ULONG64 address,
    IN LPVOID buffer,
    IN SIZE_T size,
    OUT SIZE_T* numberOfBytesRead)
{
    ENTRY("PAL_ReadProcessMemory(handle=%x, address=%p buffer=%p size=%d)\n", handle, (void*)address, buffer, size);
    _ASSERTE(handle != 0);
    _ASSERTE(numberOfBytesRead != nullptr);
    BOOL result = TRUE;
    size_t read = 0;
#ifdef __APPLE__
    vm_map_t task = (vm_map_t)handle;

    // vm_read_overwrite usually requires that the address be page-aligned
    // and the size be a multiple of the page size.  We can't differentiate
    // between the cases in which that's required and those in which it
    // isn't, so we do it all the time.
    const size_t pageSize = GetVirtualPageSize();
    vm_address_t addressAligned = ALIGN_DOWN(address, pageSize);
    ssize_t offset = OffsetWithinPage(address);
    ssize_t bytesLeft = size;

    char *data = (char*)malloc(pageSize);
    if (data != nullptr)
    {
        while (bytesLeft > 0)
        {
            vm_size_t bytesRead = pageSize;
            kern_return_t result = ::vm_read_overwrite(task, addressAligned, pageSize, (vm_address_t)data, &bytesRead);
            if (result != KERN_SUCCESS || bytesRead != pageSize)
            {
                TRACE("PAL_ReadProcessMemory(%p %d): vm_read_overwrite failed bytesLeft %d bytesRead %d from %p: %x %s\n",
                    (void*)address, size, bytesLeft, bytesRead, (void*)addressAligned, result, mach_error_string(result));
                break;
            }
            ssize_t bytesToCopy = pageSize - offset;
            if (bytesToCopy > bytesLeft)
            {
                bytesToCopy = bytesLeft;
            }
            memcpy((LPSTR)buffer + read, data + offset, bytesToCopy);
            addressAligned = addressAligned + pageSize;
            read += bytesToCopy;
            bytesLeft -= bytesToCopy;
            offset = 0;
        }
        result = size == 0 || read > 0;
    }
    else
    {
        ERROR("malloc(%d) FAILED\n", pageSize);
        result = FALSE;
    }

    if (data != nullptr)
    {
        free(data);
    }
#else
    read = pread(handle, buffer, size, address);
    if (read == (size_t)-1)
    {
        result = FALSE;
    }
#endif
    *numberOfBytesRead = read;
    LOGEXIT("PAL_ReadProcessMemory result=%d bytes read=%d\n", result, read);
    return result;
}

/*++
Function:
  PAL_ProbeMemory

Abstract

Parameter
  pBuffer : address of memory to validate
  cbBuffer : size of memory region to validate
  fWriteAccess : if true, validate writable access, else just readable.

Return
  true if memory is valid, false if not.
--*/
BOOL
PALAPI
PAL_ProbeMemory(
    PVOID pBuffer,
    DWORD cbBuffer,
    BOOL fWriteAccess)
{
    int fds[2];
    int flags;

    if (pipe(fds) != 0)
    {
        ASSERT("pipe failed: errno is %d (%s)\n", errno, strerror(errno));
        return FALSE;
    }

    flags = fcntl(fds[0], F_GETFL, 0);
    fcntl(fds[0], F_SETFL, flags | O_NONBLOCK);
    
    flags = fcntl(fds[1], F_GETFL, 0);
    fcntl(fds[1], F_SETFL, flags | O_NONBLOCK);

    PVOID pEnd = (PBYTE)pBuffer + cbBuffer;
    BOOL result = TRUE;

    // Validate the first byte in the buffer, then validate the first byte on each page after that.
    while (pBuffer < pEnd)
    {
        int written = write(fds[1], pBuffer, 1);
        if (written == -1)
        {
            if (errno != EFAULT)
            {
                ASSERT("write failed: errno is %d (%s)\n", errno, strerror(errno));
            }
            result = FALSE;
            break;
        }
        else
        {
            if (fWriteAccess)
            {
                int rd = read(fds[0], pBuffer, 1);
                if (rd == -1)
                {
                    if (errno != EFAULT)
                    {
                        ASSERT("read failed: errno is %d (%s)\n", errno, strerror(errno));
                    }
                    result = FALSE;
                    break;
                }
            }
        }

        // Round to the beginning of the next page
        pBuffer = PVOID(ALIGN_DOWN((SIZE_T)pBuffer, GetVirtualPageSize()) + GetVirtualPageSize());
    }

    close(fds[0]);
    close(fds[1]);

    return result;
}

} // extern "C"
