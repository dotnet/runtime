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
#include "pal/file.hpp"
#include "pal/handlemgr.hpp"
#include "pal/module.h"
#include "procprivate.hpp"
#include "pal/palinternal.h"
#include "pal/process.h"
#include "pal/init.h"
#include "pal/critsect.h"
#include "pal/debug.h"
#include "pal/utils.h"
#include "pal/environ.h"
#include "pal/virtual.h"
#include "pal/stackstring.hpp"
#include "pal/signal.hpp"

#include <clrconfignocache.h>

#include <errno.h>
#if HAVE_POLL
#include <poll.h>
#else
#include "pal/fakepoll.h"
#endif  // HAVE_POLL

#include <unistd.h>
#include <sys/mman.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <signal.h>
#if HAVE_PRCTL_H
#include <sys/prctl.h>
#include <sys/syscall.h>
#endif
#include <sys/wait.h>
#include <sys/time.h>
#include <sys/resource.h>
#include <debugmacrosext.h>
#include <semaphore.h>
#include <stdint.h>
#include <dlfcn.h>
#include <limits.h>
#include <vector>

#ifdef __linux__
#include <sys/syscall.h> // __NR_membarrier
// Ensure __NR_membarrier is defined for portable builds.
# if !defined(__NR_membarrier)
#  if defined(__amd64__)
#   define __NR_membarrier  324
#  elif defined(__i386__)
#   define __NR_membarrier  375
#  elif defined(__arm__)
#   define __NR_membarrier  389
#  elif defined(__aarch64__)
#   define __NR_membarrier  283
#  elif defined(__loongarch64)
#   define __NR_membarrier  283
#  else
#   error Unknown architecture
#  endif
# endif
#endif

#ifdef __APPLE__
#include <libproc.h>
#include <sys/sysctl.h>
#include <sys/posix_sem.h>
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

extern char *g_szCoreCLRPath;
extern bool g_running_in_exe;

using namespace CorUnix;

CObjectType CorUnix::otProcess(
                otiProcess,
                NULL,   // No cleanup routine
                NULL,   // No initialization routine
                0,      // No immutable data
                NULL,   // No immutable data copy routine
                NULL,   // No immutable data cleanup routine
                sizeof(CProcProcessLocalData),
                NULL,   // No process local data cleanup routine
                0,      // No shared data
                PROCESS_ALL_ACCESS,
                CObjectType::SecuritySupported,
                CObjectType::SecurityInfoNotPersisted,
                CObjectType::UnnamedObject,
                CObjectType::CrossProcessDuplicationAllowed,
                CObjectType::WaitableObject,
                CObjectType::SingleTransitionObject,
                CObjectType::ThreadReleaseHasNoSideEffects,
                CObjectType::NoOwner
                );

//
// Helper membarrier function
//
#ifdef __NR_membarrier
# define membarrier(...)  syscall(__NR_membarrier, __VA_ARGS__)
#else
# define membarrier(...)  -ENOSYS
#endif

enum membarrier_cmd
{
    MEMBARRIER_CMD_QUERY                                 = 0,
    MEMBARRIER_CMD_GLOBAL                                = (1 << 0),
    MEMBARRIER_CMD_GLOBAL_EXPEDITED                      = (1 << 1),
    MEMBARRIER_CMD_REGISTER_GLOBAL_EXPEDITED             = (1 << 2),
    MEMBARRIER_CMD_PRIVATE_EXPEDITED                     = (1 << 3),
    MEMBARRIER_CMD_REGISTER_PRIVATE_EXPEDITED            = (1 << 4),
    MEMBARRIER_CMD_PRIVATE_EXPEDITED_SYNC_CORE           = (1 << 5),
    MEMBARRIER_CMD_REGISTER_PRIVATE_EXPEDITED_SYNC_CORE  = (1 << 6)
};

//
// Tracks if the OS supports FlushProcessWriteBuffers using membarrier
//
static int s_flushUsingMemBarrier = 0;

//
// Helper memory page used by the FlushProcessWriteBuffers
//
static int* s_helperPage = 0;

//
// Mutex to make the FlushProcessWriteBuffersMutex thread safe
//
pthread_mutex_t flushProcessWriteBuffersMutex;

CAllowedObjectTypes aotProcess(otiProcess);

//
// The representative IPalObject for this process
//
IPalObject* CorUnix::g_pobjProcess;

//
// Critical section that protects process data (e.g., the
// list of active threads)/
//
CRITICAL_SECTION g_csProcess;

//
// List and count of active threads
//
CPalThread* CorUnix::pGThreadList;
DWORD g_dwThreadCount;

//
// The command line and app name for the process
//
LPWSTR g_lpwstrCmdLine = NULL;
LPWSTR g_lpwstrAppDir = NULL;

// Thread ID of thread that has started the ExitProcess process
Volatile<LONG> terminator = 0;

// Process and session ID of this process.
DWORD gPID = (DWORD) -1;
DWORD gSID = (DWORD) -1;

// Application group ID for this process
#ifdef __APPLE__
LPCSTR gApplicationGroupId = nullptr;
int gApplicationGroupIdLength = 0;
#endif // __APPLE__
PathCharString* gSharedFilesPath = nullptr;

// The lowest common supported semaphore length, including null character
// NetBSD-7.99.25: 15 characters
// MacOSX 10.11: 31 -- Core 1.0 RC2 compatibility
#if defined(__NetBSD__)
#define CLR_SEM_MAX_NAMELEN 15
#elif defined(__APPLE__)
#define CLR_SEM_MAX_NAMELEN PSEMNAMLEN
#elif defined(NAME_MAX)
#define CLR_SEM_MAX_NAMELEN (NAME_MAX - 4)
#else
// On Solaris, MAXNAMLEN is 512, which is higher than MAX_PATH defined by pal.h
#define CLR_SEM_MAX_NAMELEN MAX_PATH
#endif

static_assert_no_msg(CLR_SEM_MAX_NAMELEN <= MAX_PATH);

// Function to call during PAL/process shutdown/abort
Volatile<PSHUTDOWN_CALLBACK> g_shutdownCallback = nullptr;

// Crash dump generating program arguments. Initialized in PROCAbortInitialize().
std::vector<const char*> g_argvCreateDump;

//
// Key used for associating CPalThread's with the underlying pthread
// (through pthread_setspecific)
//
pthread_key_t CorUnix::thObjKey;

static WCHAR W16_WHITESPACE[]= {0x0020, 0x0009, 0x000D, 0};
static WCHAR W16_WHITESPACE_DQUOTE[]= {0x0020, 0x0009, 0x000D, '"', 0};

enum FILETYPE
{
    FILE_ERROR,/*ERROR*/
    FILE_UNIX, /*Unix Executable*/
    FILE_DIR   /*Directory*/
};

#pragma pack(push,1)
// When creating the semaphore name on Mac running in a sandbox, We reference this structure as a byte array
// in order to encode its data into a string. Its important to make sure there is no padding between the fields
// and also at the end of the buffer. Hence, this structure is defined inside a pack(1)
struct UnambiguousProcessDescriptor
{
    UnambiguousProcessDescriptor()
    {
    }

    UnambiguousProcessDescriptor(DWORD processId, UINT64 disambiguationKey)
    {
        Init(processId, disambiguationKey);
    }

    void Init(DWORD processId, UINT64 disambiguationKey)
    {
        m_processId = processId;
        m_disambiguationKey = disambiguationKey;
    }
    UINT64 m_disambiguationKey;
    DWORD m_processId;
};
#pragma pack(pop)

static
DWORD
StartupHelperThread(
    LPVOID p);

static
BOOL
GetProcessIdDisambiguationKey(
    IN DWORD processId,
    OUT UINT64 *disambiguationKey);

PAL_ERROR
PROCGetProcessStatus(
    CPalThread *pThread,
    HANDLE hProcess,
    PROCESS_STATE *pps,
    DWORD *pdwExitCode);

static
void
CreateSemaphoreName(
    char semName[CLR_SEM_MAX_NAMELEN],
    LPCSTR semaphoreName,
    const UnambiguousProcessDescriptor& unambiguousProcessDescriptor,
    LPCSTR applicationGroupId);

static BOOL getFileName(LPCWSTR lpApplicationName, LPWSTR lpCommandLine, PathCharString& lpFileName);
static char ** buildArgv(LPCWSTR lpCommandLine, PathCharString& lpAppPath, UINT *pnArg);
static BOOL getPath(PathCharString& lpFileName, PathCharString& lpPathFileName);
static int checkFileType(LPCSTR lpFileName);
static BOOL PROCEndProcess(HANDLE hProcess, UINT uExitCode, BOOL bTerminateUnconditionally);

/*++
Function:
  GetCurrentProcessId

See MSDN doc.
--*/
DWORD
PALAPI
GetCurrentProcessId(
            VOID)
{
    PERF_ENTRY(GetCurrentProcessId);
    ENTRY("GetCurrentProcessId()\n" );

    LOGEXIT("GetCurrentProcessId returns DWORD %#x\n", gPID);
    PERF_EXIT(GetCurrentProcessId);
    return gPID;
}


/*++
Function:
  GetCurrentSessionId

See MSDN doc.
--*/
DWORD
PALAPI
GetCurrentSessionId(
            VOID)
{
    PERF_ENTRY(GetCurrentSessionId);
    ENTRY("GetCurrentSessionId()\n" );

    LOGEXIT("GetCurrentSessionId returns DWORD %#x\n", gSID);
    PERF_EXIT(GetCurrentSessionId);
    return gSID;
}


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
  CreateProcessW

Note:
  Only Standard handles need to be inherited.
  Security attributes parameters are not used.

See MSDN doc.
--*/
BOOL
PALAPI
CreateProcessW(
           IN LPCWSTR lpApplicationName,
           IN LPWSTR lpCommandLine,
           IN LPSECURITY_ATTRIBUTES lpProcessAttributes,
           IN LPSECURITY_ATTRIBUTES lpThreadAttributes,
           IN BOOL bInheritHandles,
           IN DWORD dwCreationFlags,
           IN LPVOID lpEnvironment,
           IN LPCWSTR lpCurrentDirectory,
           IN LPSTARTUPINFOW lpStartupInfo,
           OUT LPPROCESS_INFORMATION lpProcessInformation)
{
    PAL_ERROR palError = NO_ERROR;
    CPalThread *pThread;

    PERF_ENTRY(CreateProcessW);
    ENTRY("CreateProcessW(lpAppName=%p (%S), lpCmdLine=%p (%S), lpProcessAttr=%p,"
           "lpThreadAttr=%p, bInherit=%d, dwFlags=%#x, lpEnv=%p,"
           "lpCurrentDir=%p (%S), lpStartupInfo=%p, lpProcessInfo=%p)\n",
           lpApplicationName?lpApplicationName:W16_NULLSTRING,
           lpApplicationName?lpApplicationName:W16_NULLSTRING,
           lpCommandLine?lpCommandLine:W16_NULLSTRING,
           lpCommandLine?lpCommandLine:W16_NULLSTRING,lpProcessAttributes,
           lpThreadAttributes, bInheritHandles, dwCreationFlags,lpEnvironment,
           lpCurrentDirectory?lpCurrentDirectory:W16_NULLSTRING,
           lpCurrentDirectory?lpCurrentDirectory:W16_NULLSTRING,
           lpStartupInfo, lpProcessInformation);

    pThread = InternalGetCurrentThread();

    palError = InternalCreateProcess(
        pThread,
        lpApplicationName,
        lpCommandLine,
        lpProcessAttributes,
        lpThreadAttributes,
        dwCreationFlags,
        lpEnvironment,
        lpCurrentDirectory,
        lpStartupInfo,
        lpProcessInformation
        );

    if (NO_ERROR != palError)
    {
        pThread->SetLastError(palError);
    }

    LOGEXIT("CreateProcessW returns BOOL %d\n", NO_ERROR == palError);
    PERF_EXIT(CreateProcessW);

    return NO_ERROR == palError;
}

PAL_ERROR
PrepareStandardHandle(
    CPalThread *pThread,
    HANDLE hFile,
    IPalObject **ppobjFile,
    int *piFd
    )
{
    PAL_ERROR palError = NO_ERROR;
    IPalObject *pobjFile = NULL;
    IDataLock *pDataLock = NULL;
    CFileProcessLocalData *pLocalData = NULL;
    int iError = 0;

    palError = g_pObjectManager->ReferenceObjectByHandle(
        pThread,
        hFile,
        &aotFile,
        &pobjFile
        );

    if (NO_ERROR != palError)
    {
        ERROR("Bad handle passed through CreateProcess\n");
        goto PrepareStandardHandleExit;
    }

    palError = pobjFile->GetProcessLocalData(
        pThread,
        ReadLock,
        &pDataLock,
        reinterpret_cast<void **>(&pLocalData)
        );

    if (NO_ERROR != palError)
    {
        ASSERT("Unable to access file data\n");
        goto PrepareStandardHandleExit;
    }

    //
    // The passed in file needs to be inheritable
    //

    if (!pLocalData->inheritable)
    {
        ERROR("Non-inheritable handle passed through CreateProcess\n");
        palError = ERROR_INVALID_HANDLE;
        goto PrepareStandardHandleExit;
    }

    iError = fcntl(pLocalData->unix_fd, F_SETFD, 0);
    if (-1 == iError)
    {
        ERROR("Unable to remove close-on-exec for file (errno %i)\n", errno);
        palError = ERROR_INVALID_HANDLE;
        goto PrepareStandardHandleExit;
    }

    *piFd = pLocalData->unix_fd;
    pDataLock->ReleaseLock(pThread, FALSE);
    pDataLock = NULL;

    //
    // Transfer pobjFile reference to out parameter
    //

    *ppobjFile = pobjFile;
    pobjFile = NULL;

PrepareStandardHandleExit:

    if (NULL != pDataLock)
    {
        pDataLock->ReleaseLock(pThread, FALSE);
    }

    if (NULL != pobjFile)
    {
        pobjFile->ReleaseReference(pThread);
    }

    return palError;
}

PAL_ERROR
CorUnix::InternalCreateProcess(
    CPalThread *pThread,
    LPCWSTR lpApplicationName,
    LPWSTR lpCommandLine,
    LPSECURITY_ATTRIBUTES lpProcessAttributes,
    LPSECURITY_ATTRIBUTES lpThreadAttributes,
    DWORD dwCreationFlags,
    LPVOID lpEnvironment,
    LPCWSTR lpCurrentDirectory,
    LPSTARTUPINFOW lpStartupInfo,
    LPPROCESS_INFORMATION lpProcessInformation
    )
{
    PAL_ERROR palError = NO_ERROR;
    IPalObject *pobjProcess = NULL;
    IPalObject *pobjProcessRegistered = NULL;
    IDataLock *pLocalDataLock = NULL;
    CProcProcessLocalData *pLocalData;
    IDataLock *pSharedDataLock = NULL;
    CPalThread *pDummyThread = NULL;
    HANDLE hDummyThread = NULL;
    HANDLE hProcess = NULL;
    CObjectAttributes oa(NULL, lpProcessAttributes);

    IPalObject *pobjFileIn = NULL;
    int iFdIn = -1;
    IPalObject *pobjFileOut = NULL;
    int iFdOut = -1;
    IPalObject *pobjFileErr = NULL;
    int iFdErr = -1;

    pid_t processId;
    PathCharString lpFileNamePS;
    char **lppArgv = NULL;
    UINT nArg;
    int  iRet;
    char **EnvironmentArray=NULL;
    int child_blocking_pipe = -1;
    int parent_blocking_pipe = -1;

    /* Validate parameters */

    /* note : specs indicate lpApplicationName should always
       be NULL; however support for it is already implemented. Leaving the code
       in, specs can change; but rejecting non-NULL for now to conform to the
       spec. */
    if( NULL != lpApplicationName )
    {
        ASSERT("lpApplicationName should be NULL, but is %S instead\n",
               lpApplicationName);
        palError = ERROR_INVALID_PARAMETER;
        goto InternalCreateProcessExit;
    }

    if (0 != (dwCreationFlags & ~(CREATE_SUSPENDED|CREATE_NEW_CONSOLE)))
    {
        ASSERT("Unexpected creation flags (%#x)\n", dwCreationFlags);
        palError = ERROR_INVALID_PARAMETER;
        goto InternalCreateProcessExit;
    }

    /* Security attributes parameters are ignored */
    if (lpProcessAttributes != NULL &&
        (lpProcessAttributes->lpSecurityDescriptor != NULL ||
         lpProcessAttributes->bInheritHandle != TRUE))
    {
        ASSERT("lpProcessAttributes is invalid, parameter ignored (%p)\n",
               lpProcessAttributes);
        palError = ERROR_INVALID_PARAMETER;
        goto InternalCreateProcessExit;
    }

    if (lpThreadAttributes != NULL)
    {
        ASSERT("lpThreadAttributes parameter must be NULL (%p)\n",
               lpThreadAttributes);
        palError = ERROR_INVALID_PARAMETER;
        goto InternalCreateProcessExit;
    }

    /* note : Win32 crashes in this case */
    if(NULL == lpStartupInfo)
    {
        ERROR("lpStartupInfo is NULL\n");
        palError = ERROR_INVALID_PARAMETER;
        goto InternalCreateProcessExit;
    }

    /* Validate lpStartupInfo.cb field */
    if (lpStartupInfo->cb < sizeof(STARTUPINFOW))
    {
        ASSERT("lpStartupInfo parameter structure size is invalid (%u)\n",
              lpStartupInfo->cb);
        palError = ERROR_INVALID_PARAMETER;
        goto InternalCreateProcessExit;
    }

    /* lpStartupInfo should be either zero or STARTF_USESTDHANDLES */
    if (lpStartupInfo->dwFlags & ~STARTF_USESTDHANDLES)
    {
        ASSERT("lpStartupInfo parameter invalid flags (%#x)\n",
              lpStartupInfo->dwFlags);
        palError = ERROR_INVALID_PARAMETER;
        goto InternalCreateProcessExit;
    }

    /* validate given standard handles if we have any */
    if (lpStartupInfo->dwFlags & STARTF_USESTDHANDLES)
    {
        palError = PrepareStandardHandle(
            pThread,
            lpStartupInfo->hStdInput,
            &pobjFileIn,
            &iFdIn
            );

        if (NO_ERROR != palError)
        {
            goto InternalCreateProcessExit;
        }

        palError = PrepareStandardHandle(
            pThread,
            lpStartupInfo->hStdOutput,
            &pobjFileOut,
            &iFdOut
            );

        if (NO_ERROR != palError)
        {
            goto InternalCreateProcessExit;
        }

        palError = PrepareStandardHandle(
            pThread,
            lpStartupInfo->hStdError,
            &pobjFileErr,
            &iFdErr
            );

        if (NO_ERROR != palError)
        {
            goto InternalCreateProcessExit;
        }
    }

    if (!getFileName(lpApplicationName, lpCommandLine, lpFileNamePS))
    {
        ERROR("Can't find executable!\n");
        palError = ERROR_FILE_NOT_FOUND;
        goto InternalCreateProcessExit;
    }

    /* check type of file */
    iRet = checkFileType(lpFileNamePS);

    switch (iRet)
    {
        case FILE_ERROR: /* file not found, or not an executable */
            WARN ("File is not valid (%s)", lpFileNamePS.GetString());
            palError = ERROR_FILE_NOT_FOUND;
            goto InternalCreateProcessExit;

        case FILE_UNIX: /* Unix binary file */
            break;  /* nothing to do */

        case FILE_DIR:/*Directory*/
            WARN ("File is a Directory (%s)", lpFileNamePS.GetString());
            palError = ERROR_ACCESS_DENIED;
            goto InternalCreateProcessExit;
            break;

        default: /* not supposed to get here */
            ASSERT ("Invalid return type from checkFileType");
            palError = ERROR_FILE_NOT_FOUND;
            goto InternalCreateProcessExit;
    }

    /* build Argument list, lppArgv is allocated in buildArgv function and
       requires to be freed */
    lppArgv = buildArgv(lpCommandLine, lpFileNamePS, &nArg);

    /* set the Environment variable */
    if (lpEnvironment != NULL)
    {
        unsigned i;
        // Since CREATE_UNICODE_ENVIRONMENT isn't supported we know the string is ansi
        unsigned EnvironmentEntries = 0;
        // Convert the environment block to array of strings
        // Count the number of entries
        // Is it a string that contains null terminated string, the end is delimited
        // by two null in a row.
        for (i = 0; ((char *)lpEnvironment)[i]!='\0'; i++)
        {
            EnvironmentEntries ++;
            for (;((char *)lpEnvironment)[i]!='\0'; i++)
            {
            }
        }
        EnvironmentEntries++;
        EnvironmentArray = (char **)InternalMalloc(EnvironmentEntries * sizeof(char *));

        EnvironmentEntries = 0;
        // Convert the environment block to array of strings
        // Count the number of entries
        // Is it a string that contains null terminated string, the end is delimited
        // by two null in a row.
        for (i = 0; ((char *)lpEnvironment)[i]!='\0'; i++)
        {
            EnvironmentArray[EnvironmentEntries] = &((char *)lpEnvironment)[i];
            EnvironmentEntries ++;
            for (;((char *)lpEnvironment)[i]!='\0'; i++)
            {
            }
        }
        EnvironmentArray[EnvironmentEntries] = NULL;
    }

    //
    // Allocate and register the process object for the new process
    //

    palError = g_pObjectManager->AllocateObject(
        pThread,
        &otProcess,
        &oa,
        &pobjProcess
        );

    if (NO_ERROR != palError)
    {
        ERROR("Unable to allocate object for new proccess\n");
        goto InternalCreateProcessExit;
    }

    palError = g_pObjectManager->RegisterObject(
        pThread,
        pobjProcess,
        &aotProcess,
        &hProcess,
        &pobjProcessRegistered
        );

    //
    // pobjProcess is invalidated by the above call, so
    // NULL it out here
    //

    pobjProcess = NULL;

    if (NO_ERROR != palError)
    {
        ERROR("Unable to register new process object\n");
        goto InternalCreateProcessExit;
    }

    //
    // Create a new "dummy" thread object
    //

    palError = InternalCreateDummyThread(
        pThread,
        lpThreadAttributes,
        &pDummyThread,
        &hDummyThread
        );

    if (dwCreationFlags & CREATE_SUSPENDED)
    {
        int pipe_descs[2];

        if (-1 == pipe(pipe_descs))
        {
            ERROR("pipe() failed! error is %d (%s)\n", errno, strerror(errno));
            palError = ERROR_NOT_ENOUGH_MEMORY;
            goto InternalCreateProcessExit;
        }

        /* [0] is read end, [1] is write end */
        pDummyThread->suspensionInfo.SetBlockingPipe(pipe_descs[1]);
        parent_blocking_pipe = pipe_descs[1];
        child_blocking_pipe = pipe_descs[0];
    }

    palError = pobjProcessRegistered->GetProcessLocalData(
        pThread,
        WriteLock,
        &pLocalDataLock,
        reinterpret_cast<void **>(&pLocalData)
        );

    if (NO_ERROR != palError)
    {
        ASSERT("Unable to obtain local data for new process object\n");
        goto InternalCreateProcessExit;
    }


    /* fork the new process */
    processId = fork();

    if (processId == -1)
    {
        ASSERT("Unable to create a new process with fork()\n");
        if (-1 != child_blocking_pipe)
        {
            close(child_blocking_pipe);
            close(parent_blocking_pipe);
        }

        palError = ERROR_INTERNAL_ERROR;
        goto InternalCreateProcessExit;
    }

    /* From the time the child process begins running, to when it reaches execve,
    the child process is not a real PAL process and does not own any PAL
    resources, although it has access to the PAL resources of its parent process.
    Thus, while the child process is in this window, it is dangerous for it to affect
    its parent's PAL resources. As a consequence, no PAL code should be used
    in this window; all code should make unix calls. Note the use of _exit
    instead of exit to avoid calling PAL_Terminate and the lack of TRACE's and
    ASSERT's. */

    if (processId == 0)  /* child process */
    {
        // At this point, the PAL should be considered uninitialized for this child process.

        // Don't want to enter the init_critsec here since we're trying to avoid
        // calling PAL functions. Furthermore, nothing should be changing
        // the init_count in the child process at this point since this is the only
        // thread executing.
        init_count = 0;

        sigset_t sm;

        //
        // Clear out the signal mask for the new process.
        //

        sigemptyset(&sm);
        iRet = sigprocmask(SIG_SETMASK, &sm, NULL);
        if (iRet != 0)
        {
            _exit(EXIT_FAILURE);
        }

        if (dwCreationFlags & CREATE_SUSPENDED)
        {
            BYTE resume_code = 0;
            ssize_t read_ret;

            /* close the write end of the pipe, the child doesn't need it */
            close(parent_blocking_pipe);

            read_again:
            /* block until ResumeThread writes something to the pipe */
            read_ret = read(child_blocking_pipe, &resume_code, sizeof(resume_code));
            if (sizeof(resume_code) != read_ret)
            {
                if (read_ret == -1 && EINTR == errno)
                {
                    goto read_again;
                }
                else
                {
                    /* note : read might return 0 (and return EAGAIN) if the other
                       end of the pipe gets closed - for example because the parent
                       process dies (very) abruptly */
                    _exit(EXIT_FAILURE);
                }
            }
            if (WAKEUPCODE != resume_code)
            {
                // resume_code should always equal WAKEUPCODE.
                _exit(EXIT_FAILURE);
            }

            close(child_blocking_pipe);
        }

        /* Set the current directory */
        if (lpCurrentDirectory)
        {
            SetCurrentDirectoryW(lpCurrentDirectory);
        }

        /* Set the standard handles to the incoming values */
        if (lpStartupInfo->dwFlags & STARTF_USESTDHANDLES)
        {
            /* For each handle, we need to duplicate the incoming unix
               fd to the corresponding standard one.  The API that I use,
               dup2, will copy the source to the destination, automatically
               closing the existing destination, in an atomic way */
            if (dup2(iFdIn, STDIN_FILENO) == -1)
            {
                // Didn't duplicate standard in.
                _exit(EXIT_FAILURE);
            }

            if (dup2(iFdOut, STDOUT_FILENO) == -1)
            {
                // Didn't duplicate standard out.
                _exit(EXIT_FAILURE);
            }

            if (dup2(iFdErr, STDERR_FILENO) == -1)
            {
                // Didn't duplicate standard error.
                _exit(EXIT_FAILURE);
            }

            /* now close the original FDs, we don't need them anymore */
            close(iFdIn);
            close(iFdOut);
            close(iFdErr);
        }

        /* execute the new process */

        if (EnvironmentArray)
        {
            execve(lpFileNamePS, lppArgv, EnvironmentArray);
        }
        else
        {
            execve(lpFileNamePS, lppArgv, palEnvironment);
        }

        /* if we get here, it means the execve function call failed so just exit */
        _exit(EXIT_FAILURE);
    }

    /* parent process */

    /* close the read end of the pipe, the parent doesn't need it */
    close(child_blocking_pipe);

    /* Set the process ID */
    pLocalData->dwProcessId = processId;
    pLocalDataLock->ReleaseLock(pThread, TRUE);
    pLocalDataLock = NULL;

    //
    // Release file handle info; we don't need them anymore. Note that
    // this must happen after we've released the data locks, as
    // otherwise a deadlock could result.
    //

    if (lpStartupInfo->dwFlags & STARTF_USESTDHANDLES)
    {
        pobjFileIn->ReleaseReference(pThread);
        pobjFileIn = NULL;
        pobjFileOut->ReleaseReference(pThread);
        pobjFileOut = NULL;
        pobjFileErr->ReleaseReference(pThread);
        pobjFileErr = NULL;
    }

    /* fill PROCESS_INFORMATION strucutre */
    lpProcessInformation->hProcess = hProcess;
    lpProcessInformation->hThread = hDummyThread;
    lpProcessInformation->dwProcessId = processId;
    lpProcessInformation->dwThreadId_PAL_Undefined = 0;


    TRACE("New process created: id=%#x\n", processId);

InternalCreateProcessExit:

    if (NULL != pLocalDataLock)
    {
        pLocalDataLock->ReleaseLock(pThread, FALSE);
    }

    if (NULL != pSharedDataLock)
    {
        pSharedDataLock->ReleaseLock(pThread, FALSE);
    }

    if (NULL != pobjProcess)
    {
        pobjProcess->ReleaseReference(pThread);
    }

    if (NULL != pobjProcessRegistered)
    {
        pobjProcessRegistered->ReleaseReference(pThread);
    }

    if (NO_ERROR != palError)
    {
        if (NULL != hProcess)
        {
            g_pObjectManager->RevokeHandle(pThread, hProcess);
        }

        if (NULL != hDummyThread)
        {
            g_pObjectManager->RevokeHandle(pThread, hDummyThread);
        }
    }

    if (EnvironmentArray)
    {
        free(EnvironmentArray);
    }

    /* if we still have the file structures at this point, it means we
       encountered an error sometime between when we acquired them and when we
       fork()ed. We not only have to release them, we have to give them back
       their close-on-exec flag */
    if (NULL != pobjFileIn)
    {
        if(-1 == fcntl(iFdIn, F_SETFD, 1))
        {
            WARN("couldn't restore close-on-exec flag to stdin descriptor! "
                 "errno is %d (%s)\n", errno, strerror(errno));
        }
        pobjFileIn->ReleaseReference(pThread);
    }

    if (NULL != pobjFileOut)
    {
        if(-1 == fcntl(iFdOut, F_SETFD, 1))
        {
            WARN("couldn't restore close-on-exec flag to stdout descriptor! "
                 "errno is %d (%s)\n", errno, strerror(errno));
        }
        pobjFileOut->ReleaseReference(pThread);
    }

    if (NULL != pobjFileErr)
    {
        if(-1 == fcntl(iFdErr, F_SETFD, 1))
        {
            WARN("couldn't restore close-on-exec flag to stderr descriptor! "
                 "errno is %d (%s)\n", errno, strerror(errno));
        }
        pobjFileErr->ReleaseReference(pThread);
    }

    /* free allocated memory */
    if (lppArgv)
    {
        free(*lppArgv);
        free(lppArgv);
    }

    return palError;
}


/*++
Function:
  GetExitCodeProcess

See MSDN doc.
--*/
BOOL
PALAPI
GetExitCodeProcess(
    IN HANDLE hProcess,
    IN LPDWORD lpExitCode)
{
    CPalThread *pThread;
    PAL_ERROR palError = NO_ERROR;
    DWORD dwExitCode;
    PROCESS_STATE ps;

    PERF_ENTRY(GetExitCodeProcess);
    ENTRY("GetExitCodeProcess(hProcess = %p, lpExitCode = %p)\n",
          hProcess, lpExitCode);

    pThread = InternalGetCurrentThread();

    if(NULL == lpExitCode)
    {
        WARN("Got NULL lpExitCode\n");
        palError = ERROR_INVALID_PARAMETER;
        goto done;
    }

    palError = PROCGetProcessStatus(
        pThread,
        hProcess,
        &ps,
        &dwExitCode
        );

    if (NO_ERROR != palError)
    {
        ASSERT("Couldn't get process status information!\n");
        goto done;
    }

    if( PS_DONE == ps )
    {
        *lpExitCode = dwExitCode;
    }
    else
    {
        *lpExitCode = STILL_ACTIVE;
    }

done:

    if (NO_ERROR != palError)
    {
        pThread->SetLastError(palError);
    }

    LOGEXIT("GetExitCodeProcess returns BOOL %d\n", NO_ERROR == palError);
    PERF_EXIT(GetExitCodeProcess);

    return NO_ERROR == palError;
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

    old_terminator = InterlockedCompareExchange(&terminator, GetCurrentThreadId(), 0);

    if (GetCurrentThreadId() == old_terminator)
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
            PROCEndProcess(GetCurrentProcess(), uExitCode, FALSE);
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
        poll(NULL, 0, INFTIM);
    }

    /* ExitProcess may be called even if PAL is not initialized.
       Verify if process structure exist
    */
    if (PALInitLock() && PALIsInitialized())
    {
        PROCEndProcess(GetCurrentProcess(), uExitCode, FALSE);

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

    PERF_ENTRY(TerminateProcess);
    ENTRY("TerminateProcess(hProcess=%p, uExitCode=%u)\n",hProcess, uExitCode );

    ret = PROCEndProcess(hProcess, uExitCode, TRUE);

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
RaiseFailFastException(
    IN PEXCEPTION_RECORD pExceptionRecord,
    IN PCONTEXT pContextRecord,
    IN DWORD dwFlags)
{
    PERF_ENTRY(RaiseFailFastException);
    ENTRY("RaiseFailFastException");

    TerminateCurrentProcessNoExit(TRUE);
    PROCAbort();

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

--*/
static BOOL PROCEndProcess(HANDLE hProcess, UINT uExitCode, BOOL bTerminateUnconditionally)
{
    DWORD dwProcessId;
    BOOL ret = FALSE;

    dwProcessId = PROCGetProcessIDFromHandle(hProcess);
    if (dwProcessId == 0)
    {
        SetLastError(ERROR_INVALID_HANDLE);
    }
    else if(dwProcessId != GetCurrentProcessId())
    {
        if (uExitCode != 0)
            WARN("exit code 0x%x ignored for external process.\n", uExitCode);

        if (kill(dwProcessId, SIGKILL) == 0)
        {
            ret = TRUE;
        }
        else
        {
            switch (errno) {
            case ESRCH:
                SetLastError(ERROR_INVALID_HANDLE);
                break;
            case EPERM:
                SetLastError(ERROR_ACCESS_DENIED);
                break;
            default:
                // Unexpected failure.
                ASSERT(FALSE);
                SetLastError(ERROR_INTERNAL_ERROR);
                break;
            }
        }
    }
    else
    {
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
    }

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

// Build the semaphore names using the PID and a value that can be used for distinguishing
// between processes with the same PID (which ran at different times). This is to avoid
// cases where a prior process with the same PID exited abnormally without having a chance
// to clean up its semaphore.
// Note to anyone modifying these names in the future: Semaphore names on OS X are limited
// to SEM_NAME_LEN characters, including null. SEM_NAME_LEN is 31 (at least on OS X 10.11).
// NetBSD limits semaphore names to 15 characters, including null (at least up to 7.99.25).
// Keep 31 length for Core 1.0 RC2 compatibility
#if defined(__NetBSD__)
static const char* RuntimeSemaphoreNameFormat = "/clr%s%08llx";
#else
static const char* RuntimeSemaphoreNameFormat = "/clr%s%08x%016llx";
#endif

static const char* RuntimeStartupSemaphoreName = "st";
static const char* RuntimeContinueSemaphoreName = "co";

#if defined(__NetBSD__)
static uint64_t HashSemaphoreName(uint64_t a, uint64_t b)
{
    return (a ^ b) & 0xffffffff;
}
#else
#define HashSemaphoreName(a,b) a,b
#endif

static const char *const TwoWayNamedPipePrefix = "clr-debug-pipe";
static const char* IpcNameFormat = "%s-%d-%llu-%s";

/*++
    PAL_NotifyRuntimeStarted

    Signals the debugger waiting for runtime startup notification to continue and
    waits until the debugger signals us to continue.

Parameters:
    None

Return value:
    TRUE - successfully launched by debugger, FALSE - not launched or some failure in the handshake
--*/
BOOL
PALAPI
PAL_NotifyRuntimeStarted()
{
    char startupSemName[CLR_SEM_MAX_NAMELEN];
    char continueSemName[CLR_SEM_MAX_NAMELEN];
    sem_t *startupSem = SEM_FAILED;
    sem_t *continueSem = SEM_FAILED;
    BOOL launched = FALSE;

    UINT64 processIdDisambiguationKey = 0;
    BOOL ret = GetProcessIdDisambiguationKey(gPID, &processIdDisambiguationKey);

    // If GetProcessIdDisambiguationKey failed for some reason, it should set the value
    // to 0. We expect that anyone else making the semaphore name will also fail and thus
    // will also try to use 0 as the value.
    _ASSERTE(ret == TRUE || processIdDisambiguationKey == 0);

    UnambiguousProcessDescriptor unambiguousProcessDescriptor(gPID, processIdDisambiguationKey);
    LPCSTR applicationGroupId = PAL_GetApplicationGroupId();
    CreateSemaphoreName(startupSemName, RuntimeStartupSemaphoreName, unambiguousProcessDescriptor, applicationGroupId);
    CreateSemaphoreName(continueSemName, RuntimeContinueSemaphoreName, unambiguousProcessDescriptor, applicationGroupId);

    TRACE("PAL_NotifyRuntimeStarted opening continue '%s' startup '%s'\n", continueSemName, startupSemName);

    // Open the debugger startup semaphore. If it doesn't exists, then we do nothing and return
    startupSem = sem_open(startupSemName, 0);
    if (startupSem == SEM_FAILED)
    {
        TRACE("sem_open(%s) failed: %d (%s)\n", startupSemName, errno, strerror(errno));
        goto exit;
    }

    continueSem = sem_open(continueSemName, 0);
    if (continueSem == SEM_FAILED)
    {
        ASSERT("sem_open(%s) failed: %d (%s)\n", continueSemName, errno, strerror(errno));
        goto exit;
    }

    // Wake up the debugger waiting for startup
    if (sem_post(startupSem) != 0)
    {
        ASSERT("sem_post(startupSem) failed: errno is %d (%s)\n", errno, strerror(errno));
        goto exit;
    }

    // Now wait until the debugger's runtime startup notification is finished
    while (sem_wait(continueSem) != 0)
    {
        if (EINTR == errno)
        {
            TRACE("sem_wait() failed with EINTR; re-waiting");
            continue;
        }
        ASSERT("sem_wait(continueSem) failed: errno is %d (%s)\n", errno, strerror(errno));
        goto exit;
    }

    // Returns that the runtime was successfully launched for debugging
    launched = TRUE;

exit:
    if (startupSem != SEM_FAILED)
    {
        sem_close(startupSem);
    }
    if (continueSem != SEM_FAILED)
    {
        sem_close(continueSem);
    }
    return launched;
}

LPCSTR
PALAPI
PAL_GetApplicationGroupId()
{
#ifdef __APPLE__
    return gApplicationGroupId;
#else
    return nullptr;
#endif
}

#ifdef __APPLE__

// We use 7bits from each byte, so this computes the extra size we need to encode a given byte count
constexpr int GetExtraEncodedAreaSize(UINT rawByteCount)
{
    return (rawByteCount+6)/7;
}
const int SEMAPHORE_ENCODED_NAME_EXTRA_LENGTH = GetExtraEncodedAreaSize(sizeof(UnambiguousProcessDescriptor));
const int SEMAPHORE_ENCODED_NAME_LENGTH =
    sizeof(UnambiguousProcessDescriptor) + /* For process ID + disambiguationKey */
    SEMAPHORE_ENCODED_NAME_EXTRA_LENGTH; /* For base 255 extra encoding space */

static_assert_no_msg(MAX_APPLICATION_GROUP_ID_LENGTH
    + 1 /* For / */
    + 2 /* For ST/CO name prefix */
    + SEMAPHORE_ENCODED_NAME_LENGTH /* For encoded name string */
    + 1 /* For null terminator */
    <= CLR_SEM_MAX_NAMELEN);

// In Apple we are limited by the length of the semaphore name. However, the characters which can be used in the
// name can be anything between 1 and 255 (since 0 will terminate the string). Thus, we encode each byte b in
// unambiguousProcessDescriptor as b ? b : 1, and mark an additional bit indicating if b is 0 or not. We use 7 bits
// out of each extra byte so 1 bit will always be '1'. This will ensure that our extra bytes are never 0 which are
// invalid characters. Thus we need an extra byte for each 7 input bytes. Hence, only extra 2 bytes for the name string.
void EncodeSemaphoreName(char *encodedSemName, const UnambiguousProcessDescriptor& unambiguousProcessDescriptor)
{
    const unsigned char *buffer = (const unsigned char *)&unambiguousProcessDescriptor;
    char *extraEncodingBits = encodedSemName + sizeof(UnambiguousProcessDescriptor);

    // Reset the extra encoding bit area
    for (int i=0; i<SEMAPHORE_ENCODED_NAME_EXTRA_LENGTH; i++)
    {
        extraEncodingBits[i] = 0x80;
    }

    // Encode each byte in unambiguousProcessDescriptor
    for (int i=0; i<sizeof(UnambiguousProcessDescriptor); i++)
    {
        unsigned char b = buffer[i];
        encodedSemName[i] = b ? b : 1;
        extraEncodingBits[i/7] |= (b ? 0 : 1) << (i%7);
    }
}
#endif

void CreateSemaphoreName(char semName[CLR_SEM_MAX_NAMELEN], LPCSTR semaphoreName, const UnambiguousProcessDescriptor& unambiguousProcessDescriptor, LPCSTR applicationGroupId)
{
    int length = 0;

#ifdef __APPLE__
    if (applicationGroupId != nullptr)
    {
        // We assume here that applicationGroupId has been already tested for length and is less than MAX_APPLICATION_GROUP_ID_LENGTH
        length = sprintf_s(semName, CLR_SEM_MAX_NAMELEN, "%s/%s", applicationGroupId, semaphoreName);
        _ASSERTE(length > 0 && length < CLR_SEM_MAX_NAMELEN);

        EncodeSemaphoreName(semName+length, unambiguousProcessDescriptor);
        length += SEMAPHORE_ENCODED_NAME_LENGTH;
        semName[length] = 0;
    }
    else
#endif // __APPLE__
    {
        length = sprintf_s(
            semName,
            CLR_SEM_MAX_NAMELEN,
            RuntimeSemaphoreNameFormat,
            semaphoreName,
            HashSemaphoreName(unambiguousProcessDescriptor.m_processId, unambiguousProcessDescriptor.m_disambiguationKey));
    }

    _ASSERTE(length > 0 && length < CLR_SEM_MAX_NAMELEN );
}

/*++
 Function:
  GetProcessIdDisambiguationKey

  Get a numeric value that can be used to disambiguate between processes with the same PID,
  provided that one of them is still running. The numeric value can mean different things
  on different platforms, so it should not be used for any other purpose. Under the hood,
  it is implemented based on the creation time of the process.
--*/
BOOL
GetProcessIdDisambiguationKey(DWORD processId, UINT64 *disambiguationKey)
{
    if (disambiguationKey == nullptr)
    {
        _ASSERTE(!"disambiguationKey argument cannot be null!");
        return FALSE;
    }

    *disambiguationKey = 0;

#if defined(__APPLE__) || defined(__FreeBSD__)

    // On OS X, we return the process start time expressed in Unix time (the number of seconds
    // since the start of the Unix epoch).
    struct kinfo_proc info = {};
    size_t size = sizeof(info);
    int mib[4] = { CTL_KERN, KERN_PROC, KERN_PROC_PID, processId };
    int ret = ::sysctl(mib, sizeof(mib)/sizeof(*mib), &info, &size, nullptr, 0);

    if (ret == 0)
    {
#if defined(__APPLE__)
        timeval procStartTime = info.kp_proc.p_starttime;
#else // __FreeBSD__
        timeval procStartTime = info.ki_start;
#endif
        long secondsSinceEpoch = procStartTime.tv_sec;

        *disambiguationKey = secondsSinceEpoch;
        return TRUE;
    }
    else
    {
        _ASSERTE(!"Failed to get start time of a process.");
        return FALSE;
    }

#elif defined(__NetBSD__)

    // On NetBSD, we return the process start time expressed in Unix time (the number of seconds
    // since the start of the Unix epoch).
    kvm_t *kd;
    int cnt;
    struct kinfo_proc2 *info;

    kd = kvm_open(nullptr, nullptr, nullptr, KVM_NO_FILES, "kvm_open");
    if (kd == nullptr)
    {
        _ASSERTE(!"Failed to get start time of a process.");
        return FALSE;
    }

    info = kvm_getproc2(kd, KERN_PROC_PID, processId, sizeof(struct kinfo_proc2), &cnt);
    if (info == nullptr || cnt < 1)
    {
        kvm_close(kd);
        _ASSERTE(!"Failed to get start time of a process.");
        return FALSE;
    }

    kvm_close(kd);

    long secondsSinceEpoch = info->p_ustart_sec;
    *disambiguationKey = secondsSinceEpoch;

    return TRUE;

#elif HAVE_PROCFS_STAT

    // Here we read /proc/<pid>/stat file to get the start time for the process.
    // We return this value (which is expressed in jiffies since boot time).

    // Making something like: /proc/123/stat
    char statFileName[64];

    INDEBUG(int chars = )
    snprintf(statFileName, sizeof(statFileName), "/proc/%d/stat", processId);
    _ASSERTE(chars > 0 && chars <= (int)sizeof(statFileName));

    FILE *statFile = fopen(statFileName, "r");
    if (statFile == nullptr)
    {
        TRACE("GetProcessIdDisambiguationKey: fopen() FAILED");
        SetLastError(ERROR_INVALID_HANDLE);
        return FALSE;
    }

    char *line = nullptr;
    size_t lineLen = 0;
    if (getline(&line, &lineLen, statFile) == -1)
    {
        TRACE("GetProcessIdDisambiguationKey: getline() FAILED");
        SetLastError(ERROR_INVALID_HANDLE);
        return FALSE;
    }

    unsigned long long starttime;

    // According to `man proc`, the second field in the stat file is the filename of the executable,
    // in parentheses. Tokenizing the stat file using spaces as separators breaks when that name
    // has spaces in it, so we start using sscanf_s after skipping everything up to and including the
    // last closing paren and the space after it.
    char *scanStartPosition = strrchr(line, ')') + 2;

    // All the format specifiers for the fields in the stat file are provided by 'man proc'.
    int sscanfRet = sscanf_s(scanStartPosition,
        "%*c %*d %*d %*d %*d %*d %*u %*u %*u %*u %*u %*u %*u %*d %*d %*d %*d %*d %*d %llu \n",
         &starttime);

    if (sscanfRet != 1)
    {
        _ASSERTE(!"Failed to parse stat file contents with sscanf_s.");
        return FALSE;
    }

    free(line);
    fclose(statFile);

    *disambiguationKey = starttime;
    return TRUE;

#else
    // If this is not OS X and we don't have /proc, we just return FALSE.
    WARN("GetProcessIdDisambiguationKey was called but is not implemented on this platform!");
    return FALSE;
#endif
}

/*++
 Function:
  PAL_GetTransportName

  Builds the transport IPC names from the process id.
--*/
VOID
PALAPI
PAL_GetTransportName(
    const unsigned int MAX_TRANSPORT_NAME_LENGTH,
    OUT char *name,
    IN const char *prefix,
    IN DWORD id,
    IN const char *applicationGroupId,
    IN const char *suffix)
{
    *name = '\0';
    DWORD dwRetVal = 0;
    UINT64 disambiguationKey = 0;
    PathCharString formatBufferString;
    BOOL ret = GetProcessIdDisambiguationKey(id, &disambiguationKey);
    char *formatBuffer = formatBufferString.OpenStringBuffer(MAX_TRANSPORT_NAME_LENGTH-1);
    if (formatBuffer == nullptr)
    {
        ERROR("Out Of Memory");
        return;
    }

    // If GetProcessIdDisambiguationKey failed for some reason, it should set the value
    // to 0. We expect that anyone else making the pipe name will also fail and thus will
    // also try to use 0 as the value.
    _ASSERTE(ret == TRUE || disambiguationKey == 0);
#ifdef __APPLE__
    if (nullptr != applicationGroupId)
    {
        // Verify the length of the application group ID
        int applicationGroupIdLength = strlen(applicationGroupId);
        if (applicationGroupIdLength > MAX_APPLICATION_GROUP_ID_LENGTH)
        {
            ERROR("The length of applicationGroupId is larger than MAX_APPLICATION_GROUP_ID_LENGTH");
            return;
        }

        // In sandbox, all IPC files (locks, pipes) should be written to the application group
        // container. The path returned by GetTempPathA will be unique for each process and cannot
        // be used for IPC between two different processes
        if (!GetApplicationContainerFolder(formatBufferString, applicationGroupId, applicationGroupIdLength))
        {
            ERROR("Out Of Memory");
            return;
        }

        // Verify the size of the path won't exceed maximum allowed size
        if (formatBufferString.GetCount() >= MAX_TRANSPORT_NAME_LENGTH)
        {
            ERROR("GetApplicationContainerFolder returned a path that was larger than MAX_TRANSPORT_NAME_LENGTH");
            return;
        }
    }
    else
#endif // __APPLE__
    {
        // Get a temp file location
        dwRetVal = ::GetTempPathA(MAX_TRANSPORT_NAME_LENGTH, formatBuffer);
        if (dwRetVal == 0)
        {
            ERROR("GetTempPath failed (0x%08x)", ::GetLastError());
            return;
        }
        if (dwRetVal > MAX_TRANSPORT_NAME_LENGTH)
        {
            ERROR("GetTempPath returned a path that was larger than MAX_TRANSPORT_NAME_LENGTH");
            return;
        }
    }

    if (strncat_s(formatBuffer, MAX_TRANSPORT_NAME_LENGTH, IpcNameFormat, strlen(IpcNameFormat)) == STRUNCATE)
    {
        ERROR("TransportPipeName was larger than MAX_TRANSPORT_NAME_LENGTH");
        return;
    }

    int chars = snprintf(name, MAX_TRANSPORT_NAME_LENGTH, formatBuffer, prefix, id, disambiguationKey, suffix);
    _ASSERTE(chars > 0 && (unsigned int)chars < MAX_TRANSPORT_NAME_LENGTH);
}

/*++
 Function:
  PAL_GetTransportPipeName

  Builds the transport pipe names from the process id.
--*/
VOID
PALAPI
PAL_GetTransportPipeName(
    OUT char *name,
    IN DWORD id,
    IN const char *applicationGroupId,
    IN const char *suffix)
{
    PAL_GetTransportName(
        MAX_DEBUGGER_TRANSPORT_PIPE_NAME_LENGTH,
        name,
        TwoWayNamedPipePrefix,
        id,
        applicationGroupId,
        suffix);
}

/*++
Function:
  GetProcessTimes

See MSDN doc.
--*/
BOOL
PALAPI
GetProcessTimes(
        IN HANDLE hProcess,
        OUT LPFILETIME lpCreationTime,
        OUT LPFILETIME lpExitTime,
        OUT LPFILETIME lpKernelTime,
        OUT LPFILETIME lpUserTime)
{
    BOOL retval = FALSE;
    struct rusage resUsage;
    UINT64 calcTime;
    const UINT64 SECS_TO_100NS = 10000000ULL;  // 10^7
    const UINT64 USECS_TO_100NS = 10ULL;       // 10
    const UINT64 EPOCH_DIFF = 11644473600ULL;  // number of seconds from 1 Jan. 1601 00:00 to 1 Jan 1970 00:00 UTC

    PERF_ENTRY(GetProcessTimes);
    ENTRY("GetProcessTimes(hProcess=%p, lpExitTime=%p, lpKernelTime=%p,"
          "lpUserTime=%p)\n",
          hProcess, lpCreationTime, lpExitTime, lpKernelTime, lpUserTime );

    /* Make sure hProcess is the current process, this is the only supported
       case */
    if(PROCGetProcessIDFromHandle(hProcess)!=GetCurrentProcessId())
    {
        ASSERT("GetProcessTimes() does not work on a process other than the "
              "current process.\n");
        SetLastError(ERROR_INVALID_HANDLE);
        goto GetProcessTimesExit;
    }

    /* First, we need to actually retrieve the relevant statistics from the
       OS */
    if (getrusage (RUSAGE_SELF, &resUsage) == -1)
    {
        ASSERT("Unable to get resource usage information for the current "
              "process\n");
        SetLastError(ERROR_INTERNAL_ERROR);
        goto GetProcessTimesExit;
    }

    TRACE ("getrusage User: %ld sec,%ld microsec. Kernel: %ld sec,%ld"
           " microsec\n",
           resUsage.ru_utime.tv_sec, resUsage.ru_utime.tv_usec,
           resUsage.ru_stime.tv_sec, resUsage.ru_stime.tv_usec);

    if (lpCreationTime)
    {
        // The IBC profile data uses this, instead of the actual
        // process creation time we just return the current time

        struct timeval tv;
        if (gettimeofday(&tv, NULL) == -1)
        {
            ASSERT("gettimeofday() failed; errno is %d (%s)\n", errno, strerror(errno));

            // Assign zero to lpCreationTime
            lpCreationTime->dwLowDateTime = 0;
            lpCreationTime->dwHighDateTime = 0;
        }
        else
        {
            calcTime = EPOCH_DIFF;
            calcTime += (UINT64)tv.tv_sec;
            calcTime *= SECS_TO_100NS;
            calcTime += ((UINT64)tv.tv_usec * USECS_TO_100NS);

            // Assign the time into lpCreationTime
            lpCreationTime->dwLowDateTime = (DWORD)calcTime;
            lpCreationTime->dwHighDateTime = (DWORD)(calcTime >> 32);
        }
    }

    if (lpExitTime)
    {
        // Assign zero to lpExitTime
        lpExitTime->dwLowDateTime = 0;
        lpExitTime->dwHighDateTime = 0;
    }

    if (lpUserTime)
    {
        /* Get the time of user mode execution, in 100s of nanoseconds */
        calcTime = (UINT64)resUsage.ru_utime.tv_sec * SECS_TO_100NS;
        calcTime += (UINT64)resUsage.ru_utime.tv_usec * USECS_TO_100NS;

        /* Assign the time into lpUserTime */
        lpUserTime->dwLowDateTime = (DWORD)calcTime;
        lpUserTime->dwHighDateTime = (DWORD)(calcTime >> 32);
    }

    if (lpKernelTime)
    {
        /* Get the time of kernel mode execution, in 100s of nanoseconds */
        calcTime = (UINT64)resUsage.ru_stime.tv_sec * SECS_TO_100NS;
        calcTime += (UINT64)resUsage.ru_stime.tv_usec * USECS_TO_100NS;

        /* Assign the time into lpUserTime */
        lpKernelTime->dwLowDateTime = (DWORD)calcTime;
        lpKernelTime->dwHighDateTime = (DWORD)(calcTime >> 32);
    }

    retval = TRUE;


GetProcessTimesExit:
    LOGEXIT("GetProcessTimes returns BOOL %d\n", retval);
    PERF_EXIT(GetProcessTimes);
    return (retval);
}

#define FILETIME_TO_ULONGLONG(f) \
    (((ULONGLONG)(f).dwHighDateTime << 32) | ((ULONGLONG)(f).dwLowDateTime))

/*++
Function:
  PAL_GetCPUBusyTime

The main purpose of this function is to compute the overall CPU utilization
for the CLR thread pool to regulate the number of I/O completion port
worker threads.
Since there is no consistent API on Unix to get the CPU utilization
from a user process, getrusage and gettimeofday are used to
compute the current process's CPU utilization instead.
This function emulates the ThreadpoolMgr::GetCPUBusyTime_NT function in
win32threadpool.cpp of the CLR.

See MSDN doc for GetSystemTimes.
--*/
INT
PALAPI
PAL_GetCPUBusyTime(
    IN OUT PAL_IOCP_CPU_INFORMATION *lpPrevCPUInfo)
{
    ULONGLONG nLastRecordedCurrentTime = 0;
    ULONGLONG nLastRecordedUserTime = 0;
    ULONGLONG nLastRecordedKernelTime = 0;
    ULONGLONG nKernelTime = 0;
    ULONGLONG nUserTime = 0;
    ULONGLONG nCurrentTime = 0;
    ULONGLONG nCpuBusyTime = 0;
    ULONGLONG nCpuTotalTime = 0;
    DWORD nReading = 0;
    struct rusage resUsage;
    struct timeval tv;
    static DWORD dwNumberOfProcessors = 0;

    if (dwNumberOfProcessors <= 0)
    {
        SYSTEM_INFO SystemInfo;
        GetSystemInfo(&SystemInfo);
        dwNumberOfProcessors = SystemInfo.dwNumberOfProcessors;
        if (dwNumberOfProcessors <= 0)
        {
            return 0;
        }

        UINT cpuLimit;
        if (PAL_GetCpuLimit(&cpuLimit) && cpuLimit < dwNumberOfProcessors)
        {
            dwNumberOfProcessors = cpuLimit;
        }
    }

    if (getrusage(RUSAGE_SELF, &resUsage) == -1)
    {
        ASSERT("getrusage() failed; errno is %d (%s)\n", errno, strerror(errno));
        return 0;
    }
    else
    {
        nKernelTime = (ULONGLONG)resUsage.ru_stime.tv_sec*tccSecondsTo100NanoSeconds +
            resUsage.ru_stime.tv_usec*tccMicroSecondsTo100NanoSeconds;
        nUserTime = (ULONGLONG)resUsage.ru_utime.tv_sec*tccSecondsTo100NanoSeconds +
            resUsage.ru_utime.tv_usec*tccMicroSecondsTo100NanoSeconds;
    }

    if (gettimeofday(&tv, NULL) == -1)
    {
        ASSERT("gettimeofday() failed; errno is %d (%s)\n", errno, strerror(errno));
        return 0;
    }
    else
    {
        nCurrentTime = (ULONGLONG)tv.tv_sec*tccSecondsTo100NanoSeconds +
            tv.tv_usec*tccMicroSecondsTo100NanoSeconds;
    }

    nLastRecordedCurrentTime = FILETIME_TO_ULONGLONG(lpPrevCPUInfo->LastRecordedTime.ftLastRecordedCurrentTime);
    nLastRecordedUserTime = FILETIME_TO_ULONGLONG(lpPrevCPUInfo->ftLastRecordedUserTime);
    nLastRecordedKernelTime = FILETIME_TO_ULONGLONG(lpPrevCPUInfo->ftLastRecordedKernelTime);

    if (nCurrentTime > nLastRecordedCurrentTime)
    {
        nCpuTotalTime = (nCurrentTime - nLastRecordedCurrentTime);
#if HAVE_THREAD_SELF || HAVE__LWP_SELF || HAVE_VM_READ
        // For systems that run multiple threads of a process on multiple processors,
        // the accumulated userTime and kernelTime of this process may exceed
        // the elapsed time. In this case, the cpuTotalTime needs to be adjusted
        // according to number of processors so that the cpu utilization
        // will not be greater than 100.
        nCpuTotalTime *= dwNumberOfProcessors;
#endif // HAVE_THREAD_SELF || HAVE__LWP_SELF || HAVE_VM_READ
    }

    if (nUserTime >= nLastRecordedUserTime &&
        nKernelTime >= nLastRecordedKernelTime)
    {
        nCpuBusyTime =
            (nUserTime - nLastRecordedUserTime)+
            (nKernelTime - nLastRecordedKernelTime);
    }

    if (nCpuTotalTime > 0 && nCpuBusyTime > 0)
    {
        nReading = (DWORD)((nCpuBusyTime*100)/nCpuTotalTime);
        TRACE("PAL_GetCPUBusyTime: nCurrentTime=%lld, nKernelTime=%lld, nUserTime=%lld, nReading=%d\n",
            nCurrentTime, nKernelTime, nUserTime, nReading);
    }

    if (nReading > 100)
    {
        ERROR("cpu utilization(%d) > 100\n", nReading);
    }

    lpPrevCPUInfo->LastRecordedTime.ftLastRecordedCurrentTime.dwLowDateTime = (DWORD)nCurrentTime;
    lpPrevCPUInfo->LastRecordedTime.ftLastRecordedCurrentTime.dwHighDateTime = (DWORD)(nCurrentTime >> 32);

    lpPrevCPUInfo->ftLastRecordedUserTime.dwLowDateTime = (DWORD)nUserTime;
    lpPrevCPUInfo->ftLastRecordedUserTime.dwHighDateTime = (DWORD)(nUserTime >> 32);

    lpPrevCPUInfo->ftLastRecordedKernelTime.dwLowDateTime = (DWORD)nKernelTime;
    lpPrevCPUInfo->ftLastRecordedKernelTime.dwHighDateTime = (DWORD)(nKernelTime >> 32);

    return (DWORD)nReading;
}

/*++
Function:
  GetCommandLineW

See MSDN doc.
--*/
LPWSTR
PALAPI
GetCommandLineW(
    VOID)
{
    PERF_ENTRY(GetCommandLineW);
    ENTRY("GetCommandLineW()\n");

    LPWSTR lpwstr = g_lpwstrCmdLine ? g_lpwstrCmdLine : (LPWSTR)W("");

    LOGEXIT("GetCommandLineW returns LPWSTR %p (%S)\n",
          g_lpwstrCmdLine,
          lpwstr);
    PERF_EXIT(GetCommandLineW);

    return lpwstr;
}

/*++
Function:
  OpenProcess

See MSDN doc.

Notes :
dwDesiredAccess is ignored (all supported operations will be allowed)
bInheritHandle is ignored (no inheritance)
--*/
HANDLE
PALAPI
OpenProcess(
        DWORD dwDesiredAccess,
        BOOL bInheritHandle,
        DWORD dwProcessId)
{
    PAL_ERROR palError;
    CPalThread *pThread;
    IPalObject *pobjProcess = NULL;
    IPalObject *pobjProcessRegistered = NULL;
    IDataLock *pDataLock;
    CProcProcessLocalData *pLocalData;
    CObjectAttributes oa;
    HANDLE hProcess = NULL;

    PERF_ENTRY(OpenProcess);
    ENTRY("OpenProcess(dwDesiredAccess=0x%08x, bInheritHandle=%d, "
          "dwProcessId = 0x%08x)\n",
          dwDesiredAccess, bInheritHandle, dwProcessId );

    pThread = InternalGetCurrentThread();

    if (0 == dwProcessId)
    {
        palError = ERROR_INVALID_PARAMETER;
        goto OpenProcessExit;
    }

    palError = g_pObjectManager->AllocateObject(
        pThread,
        &otProcess,
        &oa,
        &pobjProcess
        );

    if (NO_ERROR != palError)
    {
        goto OpenProcessExit;
    }

    palError = pobjProcess->GetProcessLocalData(
        pThread,
        WriteLock,
        &pDataLock,
        reinterpret_cast<void **>(&pLocalData)
        );

    if (NO_ERROR != palError)
    {
        goto OpenProcessExit;
    }

    pLocalData->dwProcessId = dwProcessId;
    pDataLock->ReleaseLock(pThread, TRUE);

    palError = g_pObjectManager->RegisterObject(
        pThread,
        pobjProcess,
        &aotProcess,
        &hProcess,
        &pobjProcessRegistered
        );

    //
    // pobjProcess was invalidated by the above call, so NULL
    // it out here
    //

    pobjProcess = NULL;

    //
    // TODO: check to see if the process actually exists?
    //

OpenProcessExit:

    if (NULL != pobjProcess)
    {
        pobjProcess->ReleaseReference(pThread);
    }

    if (NULL != pobjProcessRegistered)
    {
        pobjProcessRegistered->ReleaseReference(pThread);
    }

    if (NO_ERROR != palError)
    {
        pThread->SetLastError(palError);
    }

    LOGEXIT("OpenProcess returns HANDLE %p\n", hProcess);
    PERF_EXIT(OpenProcess);
    return hProcess;
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
    char* buffer = (char*)InternalMalloc(128);
    if (buffer != nullptr)
    {
        if (sprintf_s(buffer, 128, "%d", value) == -1)
        {
            free(buffer);
            buffer = nullptr;
        }
    }
    return buffer;
}

static const INT UndefinedDumpType = 0;

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
    std::vector<const char*>& argv,
    char** pprogram,
    char** ppidarg,
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
    char* program = *pprogram = (char*)InternalMalloc(programLen);
    if (program == nullptr)
    {
        return FALSE;
    }
    if (strcpy_s(program, programLen, g_szCoreCLRPath) != SAFECRT_SUCCESS)
    {
        return FALSE;
    }
    char *last = strrchr(program, '/');
    if (last != nullptr)
    {
        *(last + 1) = '\0';
    }
    else
    {
        program[0] = '\0';
    }
    if (strcat_s(program, programLen, DumpGeneratorName) != SAFECRT_SUCCESS)
    {
        return FALSE;
    }
    *ppidarg = PROCFormatInt(gPID);
    if (*ppidarg == nullptr)
    {
        return FALSE;
    }
    argv.push_back(program);

    if (dumpName != nullptr)
    {
        argv.push_back("--name");
        argv.push_back(dumpName);
    }

    switch (dumpType)
    {
        case 1: argv.push_back("--normal");
            break;
        case 2: argv.push_back("--withheap");
            break;
        case 3: argv.push_back("--triage");
            break;
        case 4: argv.push_back("--full");
            break;
        case UndefinedDumpType:
        default:
            break;
    }

    if (flags & GenerateDumpFlagsLoggingEnabled)
    {
        argv.push_back("--diag");
    }

    if (flags & GenerateDumpFlagsVerboseLoggingEnabled)
    {
        argv.push_back("--verbose");
    }

    if (flags & GenerateDumpFlagsCrashReportEnabled)
    {
        argv.push_back("--crashreport");
    }

    if (g_running_in_exe)
    {
        argv.push_back("--singlefile");
    }

    if (logFileName != nullptr)
    {
        argv.push_back("--logtofile");
        argv.push_back(logFileName);
    }

    argv.push_back(*ppidarg);
    argv.push_back(nullptr);

    return TRUE;
}

/*++
Function:
  PROCCreateCrashDump

  Creates crash dump of the process. Can be called from the
  unhandled native exception handler.

(no return value)
--*/
BOOL
PROCCreateCrashDump(std::vector<const char*>& argv)
{
    // Fork the core dump child process.
    pid_t childpid = fork();

    // If error, write an error to trace log and abort
    if (childpid == -1)
    {
        ERROR("PROCCreateCrashDump: fork() FAILED %d (%s)\n", errno, strerror(errno));
        return false;
    }
    else if (childpid == 0)
    {
        // Child process
        if (execve(argv[0], (char**)argv.data(), palEnvironment) == -1)
        {
            ERROR("PROCCreateCrashDump: execve() FAILED %d (%s)\n", errno, strerror(errno));
            return false;
        }
    }
    else
    {
#if HAVE_PRCTL_H && HAVE_PR_SET_PTRACER
        // Gives the child process permission to use /proc/<pid>/mem and ptrace
        if (prctl(PR_SET_PTRACER, childpid, 0, 0, 0) == -1)
        {
            // Ignore any error because on some CentOS and OpenSUSE distros, it isn't
            // supported but createdump works just fine.
            ERROR("PROCCreateCrashDump: prctl() FAILED %d (%s)\n", errno, strerror(errno));
        }
#endif // HAVE_PRCTL_H && HAVE_PR_SET_PTRACER
        // Parent waits until the child process is done
        int wstatus = 0;
        int result = waitpid(childpid, &wstatus, 0);
        if (result != childpid)
        {
            ERROR("PROCCreateCrashDump: waitpid() FAILED result %d wstatus %d errno %d (%s)\n",
                result, wstatus, errno, strerror(errno));
            return false;
        }
        return !WIFEXITED(wstatus) || WEXITSTATUS(wstatus) == 0;
    }
    return true;
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
        DWORD dumpType = UndefinedDumpType;
        if (dmpTypeCfg.IsSet())
        {
            (void)dmpTypeCfg.TryAsInteger(10, dumpType);
            if (dumpType < 1 || dumpType > 4)
            {
                dumpType = UndefinedDumpType;
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
        CLRConfigNoCache enabldReportCfg = CLRConfigNoCache::Get("EnableCrashReport", /*noprefix*/ false, &getenv);
        val = 0;
        if (enabldReportCfg.IsSet() && enabldReportCfg.TryAsInteger(10, val) && val == 1)
        {
            flags |= GenerateDumpFlagsCrashReportEnabled;
        }

        char* program = nullptr;
        char* pidarg = nullptr;
        if (!PROCBuildCreateDumpCommandLine(g_argvCreateDump, &program, &pidarg, dumpName, logFilePath, dumpType, flags))
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
PAL_GenerateCoreDump(
    LPCSTR dumpName,
    INT dumpType,
    ULONG32 flags)
{
    std::vector<const char*> argvCreateDump;

    if (dumpType < 1 || dumpType > 4)
    {
        return FALSE;
    }
    if (dumpName != nullptr && dumpName[0] == '\0')
    {
        dumpName = nullptr;
    }
    char* program = nullptr;
    char* pidarg = nullptr;
    BOOL result = PROCBuildCreateDumpCommandLine(argvCreateDump, &program, &pidarg, dumpName, nullptr, dumpType, flags);
    if (result)
    {
        result = PROCCreateCrashDump(argvCreateDump);
    }
    free(program);
    free(pidarg);
    return result;
}

/*++
Function:
  PROCCreateCrashDumpIfEnabled

  Creates crash dump of the process (if enabled). Can be
  called from the unhandled native exception handler.

Parameters:
  signal - POSIX signal number

(no return value)
--*/
VOID
PROCCreateCrashDumpIfEnabled(int signal)
{
    // If enabled, launch the create minidump utility and wait until it completes
    if (!g_argvCreateDump.empty())
    {
        std::vector<const char*> argv(g_argvCreateDump);
        char* signalArg = nullptr;
        char* crashThreadArg = nullptr;

        if (signal != 0)
        {
            // Remove the terminating nullptr
            argv.pop_back();

            // Add the Windows exception code to the command line
            signalArg = PROCFormatInt(signal);
            if (signalArg != nullptr)
            {
                argv.push_back("--signal");
                argv.push_back(signalArg);
            }

            // Add the current thread id to the command line. This function is always called on the crashing thread.
            crashThreadArg = PROCFormatInt(THREADSilentGetCurrentThreadId());
            if (crashThreadArg != nullptr)
            {
                argv.push_back("--crashthread");
                argv.push_back(crashThreadArg);
            }
            argv.push_back(nullptr);
        }

        PROCCreateCrashDump(argv);

        free(signalArg);
        free(crashThreadArg);
    }
}

/*++
Function:
  PROCAbort()

  Aborts the process after calling the shutdown cleanup handler. This function
  should be called instead of calling abort() directly.

Parameters:
  signal - POSIX signal number

  Does not return
--*/
PAL_NORETURN
VOID
PROCAbort(int signal)
{
    // Do any shutdown cleanup before aborting or creating a core dump
    PROCNotifyProcessShutdown();

    PROCCreateCrashDumpIfEnabled(signal);

    // Restore the SIGABORT handler to prevent recursion
    SEHCleanupAbort();

    // Abort the process after waiting for the core dump to complete
    abort();
}

/*++
Function:
  InitializeFlushProcessWriteBuffers

Abstract
  This function initializes data structures needed for the FlushProcessWriteBuffers
Return
  TRUE if it succeeded, FALSE otherwise
--*/
BOOL
InitializeFlushProcessWriteBuffers()
{
    _ASSERTE(s_helperPage == 0);
    _ASSERTE(s_flushUsingMemBarrier == 0);

    // Starting with Linux kernel 4.14, process memory barriers can be generated
    // using MEMBARRIER_CMD_PRIVATE_EXPEDITED.
    int mask = membarrier(MEMBARRIER_CMD_QUERY, 0);
    if (mask >= 0 &&
        mask & MEMBARRIER_CMD_PRIVATE_EXPEDITED)
    {
        // Register intent to use the private expedited command.
        if (membarrier(MEMBARRIER_CMD_REGISTER_PRIVATE_EXPEDITED, 0) == 0)
        {
            s_flushUsingMemBarrier = TRUE;
            return TRUE;
        }
    }

#ifdef TARGET_OSX
    return TRUE;
#else
    s_helperPage = static_cast<int*>(mmap(0, GetVirtualPageSize(), PROT_READ | PROT_WRITE, MAP_ANONYMOUS | MAP_PRIVATE, -1, 0));

    if(s_helperPage == MAP_FAILED)
    {
        return FALSE;
    }

    // Verify that the s_helperPage is really aligned to the GetVirtualPageSize()
    _ASSERTE((((SIZE_T)s_helperPage) & (GetVirtualPageSize() - 1)) == 0);

    // Locking the page ensures that it stays in memory during the two mprotect
    // calls in the FlushProcessWriteBuffers below. If the page was unmapped between
    // those calls, they would not have the expected effect of generating IPI.
    int status = mlock(s_helperPage, GetVirtualPageSize());

    if (status != 0)
    {
        return FALSE;
    }

    status = pthread_mutex_init(&flushProcessWriteBuffersMutex, NULL);
    if (status != 0)
    {
        munlock(s_helperPage, GetVirtualPageSize());
    }

    return status == 0;
#endif // TARGET_OSX
}

#define FATAL_ASSERT(e, msg) \
    do \
    { \
        if (!(e)) \
        { \
            fprintf(stderr, "FATAL ERROR: " msg); \
            PROCAbort(); \
        } \
    } \
    while(0)

/*++
Function:
  FlushProcessWriteBuffers

See MSDN doc.
--*/
VOID
PALAPI
FlushProcessWriteBuffers()
{
    if (s_flushUsingMemBarrier)
    {
        int status = membarrier(MEMBARRIER_CMD_PRIVATE_EXPEDITED, 0);
        FATAL_ASSERT(status == 0, "Failed to flush using membarrier");
    }
    else if (s_helperPage != 0)
    {
        int status = pthread_mutex_lock(&flushProcessWriteBuffersMutex);
        FATAL_ASSERT(status == 0, "Failed to lock the flushProcessWriteBuffersMutex lock");

        // Changing a helper memory page protection from read / write to no access
        // causes the OS to issue IPI to flush TLBs on all processors. This also
        // results in flushing the processor buffers.
        status = mprotect(s_helperPage, GetVirtualPageSize(), PROT_READ | PROT_WRITE);
        FATAL_ASSERT(status == 0, "Failed to change helper page protection to read / write");

        // Ensure that the page is dirty before we change the protection so that
        // we prevent the OS from skipping the global TLB flush.
        InterlockedIncrement(s_helperPage);

        status = mprotect(s_helperPage, GetVirtualPageSize(), PROT_NONE);
        FATAL_ASSERT(status == 0, "Failed to change helper page protection to no access");

        status = pthread_mutex_unlock(&flushProcessWriteBuffersMutex);
        FATAL_ASSERT(status == 0, "Failed to unlock the flushProcessWriteBuffersMutex lock");
    }
#ifdef TARGET_OSX
    else
    {
        mach_msg_type_number_t cThreads;
        thread_act_t *pThreads;
        kern_return_t machret = task_threads(mach_task_self(), &pThreads, &cThreads);
        CHECK_MACH("task_threads()", machret);

        uintptr_t sp;
        uintptr_t registerValues[128];

        // Iterate through each of the threads in the list.
        for (mach_msg_type_number_t i = 0; i < cThreads; i++)
        {
            // Request the threads pointer values to force the thread to emit a memory barrier
            size_t registers = 128;
            machret = thread_get_register_pointer_values(pThreads[i], &sp, &registers, registerValues);
            if (machret == KERN_INSUFFICIENT_BUFFER_SIZE)
            {
                CHECK_MACH("thread_get_register_pointer_values()", machret);
            }
        }
        // Deallocate the thread list now we're done with it.
        machret = vm_deallocate(mach_task_self(), (vm_address_t)pThreads, cThreads * sizeof(thread_act_t));
        CHECK_MACH("vm_deallocate()", machret);
    }
#endif // TARGET_OSX
}

/*++
Function:
  PROCGetProcessIDFromHandle

Abstract
  Return the process ID from a process handle

Parameter
  hProcess:  process handle

Return
  Return the process ID, or 0 if it's not a valid handle
--*/
DWORD
PROCGetProcessIDFromHandle(
        HANDLE hProcess)
{
    PAL_ERROR palError;
    IPalObject *pobjProcess = NULL;
    CPalThread *pThread = InternalGetCurrentThread();

    DWORD dwProcessId = 0;

    if (hPseudoCurrentProcess == hProcess)
    {
        dwProcessId = gPID;
        goto PROCGetProcessIDFromHandleExit;
    }


    palError = g_pObjectManager->ReferenceObjectByHandle(
        pThread,
        hProcess,
        &aotProcess,
        &pobjProcess
        );

    if (NO_ERROR == palError)
    {
        IDataLock *pDataLock;
        CProcProcessLocalData *pLocalData;

        palError = pobjProcess->GetProcessLocalData(
            pThread,
            ReadLock,
            &pDataLock,
            reinterpret_cast<void **>(&pLocalData)
            );

        if (NO_ERROR == palError)
        {
            dwProcessId = pLocalData->dwProcessId;
            pDataLock->ReleaseLock(pThread, FALSE);
        }

        pobjProcess->ReleaseReference(pThread);
    }

PROCGetProcessIDFromHandleExit:

    return dwProcessId;
}

PAL_ERROR
CorUnix::InitializeProcessData(
    void
    )
{
    PAL_ERROR palError = NO_ERROR;
    bool fLockInitialized = FALSE;

    pGThreadList = NULL;
    g_dwThreadCount = 0;

    InternalInitializeCriticalSection(&g_csProcess);
    fLockInitialized = TRUE;

    if (NO_ERROR != palError)
    {
        if (fLockInitialized)
        {
            InternalDeleteCriticalSection(&g_csProcess);
        }
    }

    return palError;
}

/*++
Function
    InitializeProcessCommandLine

Abstract
    Initializes (or re-initializes) the saved command line and exe path.

Parameter
    lpwstrCmdLine
    lpwstrFullPath

Return
    PAL_ERROR

Notes
    This function takes ownership of lpwstrCmdLine, but not of lpwstrFullPath
--*/

PAL_ERROR
CorUnix::InitializeProcessCommandLine(
    LPWSTR lpwstrCmdLine,
    LPWSTR lpwstrFullPath
)
{
    PAL_ERROR palError = NO_ERROR;
    LPWSTR initial_dir = NULL;

    //
    // Save the command line and initial directory
    //

    if (lpwstrFullPath)
    {
        LPWSTR lpwstr = PAL_wcsrchr(lpwstrFullPath, '/');
        lpwstr[0] = '\0';
        size_t n = PAL_wcslen(lpwstrFullPath) + 1;

        size_t iLen = n;
        initial_dir = reinterpret_cast<LPWSTR>(InternalMalloc(iLen*sizeof(WCHAR)));
        if (NULL == initial_dir)
        {
            ERROR("malloc() failed! (initial_dir) \n");
            palError = ERROR_NOT_ENOUGH_MEMORY;
            goto exit;
        }

        if (wcscpy_s(initial_dir, iLen, lpwstrFullPath) != SAFECRT_SUCCESS)
        {
            ERROR("wcscpy_s failed!\n");
            free(initial_dir);
            palError = ERROR_INTERNAL_ERROR;
            goto exit;
        }

        lpwstr[0] = '/';

        free(g_lpwstrAppDir);
        g_lpwstrAppDir = initial_dir;
    }

    free(g_lpwstrCmdLine);
    g_lpwstrCmdLine = lpwstrCmdLine;

exit:
    return palError;
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
    IPalObject *pobjProcess = NULL;
    IDataLock *pDataLock;
    CProcProcessLocalData *pLocalData;
    CObjectAttributes oa;
    HANDLE hProcess;

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

    //
    // Create and initialize process object
    //

    palError = g_pObjectManager->AllocateObject(
        pThread,
        &otProcess,
        &oa,
        &pobjProcess
        );

    if (NO_ERROR != palError)
    {
        ERROR("Unable to allocate process object");
        goto CreateInitialProcessAndThreadObjectsExit;
    }

    palError = pobjProcess->GetProcessLocalData(
        pThread,
        WriteLock,
        &pDataLock,
        reinterpret_cast<void **>(&pLocalData)
        );

    if (NO_ERROR != palError)
    {
        ASSERT("Unable to access local data");
        goto CreateInitialProcessAndThreadObjectsExit;
    }

    pLocalData->dwProcessId = gPID;
    pLocalData->ps = PS_RUNNING;
    pDataLock->ReleaseLock(pThread, TRUE);

    palError = g_pObjectManager->RegisterObject(
        pThread,
        pobjProcess,
        &aotProcess,
        &hProcess,
        &g_pobjProcess
        );

    //
    // pobjProcess is invalidated by the call to RegisterObject, so
    // NULL it out here to prevent it from being released later
    //

    pobjProcess = NULL;

    if (NO_ERROR != palError)
    {
        ASSERT("Failure registering process object");
        goto CreateInitialProcessAndThreadObjectsExit;
    }

    //
    // There's no need to keep this handle around, so revoke
    // it now
    //

    g_pObjectManager->RevokeHandle(pThread, hProcess);

CreateInitialProcessAndThreadObjectsExit:

    if (NULL != pobjProcess)
    {
        pobjProcess->ReleaseReference(pThread);
    }

    return palError;
}


/*++
Function:
  PROCCleanupInitialProcess

Abstract
  Cleanup all the structures for the initial process.

Parameter
  VOID

Return
  VOID

--*/
VOID
PROCCleanupInitialProcess(VOID)
{
    CPalThread *pThread = InternalGetCurrentThread();

    InternalEnterCriticalSection(pThread, &g_csProcess);

    /* Free the application directory */
    free(g_lpwstrAppDir);

    /* Free the stored command line */
    free(g_lpwstrCmdLine);

    InternalLeaveCriticalSection(pThread, &g_csProcess);

    //
    // Object manager shutdown will handle freeing the underlying
    // thread and process data
    //

}

/*++
Function:
  PROCAddThread

Abstract
  Add a thread to the thread list of the current process

Parameter
  pThread:   Thread object

--*/
VOID
CorUnix::PROCAddThread(
    CPalThread *pCurrentThread,
    CPalThread *pTargetThread
    )
{
    /* protect the access of the thread list with critical section for
       mutithreading access */
    InternalEnterCriticalSection(pCurrentThread, &g_csProcess);

    pTargetThread->SetNext(pGThreadList);
    pGThreadList = pTargetThread;
    g_dwThreadCount += 1;

    TRACE("Thread 0x%p (id %#x) added to the process thread list\n",
          pTargetThread, pTargetThread->GetThreadId());

    InternalLeaveCriticalSection(pCurrentThread, &g_csProcess);
}


/*++
Function:
  PROCRemoveThread

Abstract
  Remove a thread form the thread list of the current process

Parameter
  CPalThread *pThread : thread object to remove

(no return value)
--*/
VOID
CorUnix::PROCRemoveThread(
    CPalThread *pCurrentThread,
    CPalThread *pTargetThread
    )
{
    CPalThread *curThread, *prevThread;

    /* protect the access of the thread list with critical section for
       mutithreading access */
    InternalEnterCriticalSection(pCurrentThread, &g_csProcess);

    curThread = pGThreadList;

    /* if thread list is empty */
    if (curThread == NULL)
    {
        ASSERT("Thread list is empty.\n");
        goto EXIT;
    }

    /* do we remove the first thread? */
    if (curThread == pTargetThread)
    {
        pGThreadList =  curThread->GetNext();
        TRACE("Thread 0x%p (id %#x) removed from the process thread list\n",
            pTargetThread, pTargetThread->GetThreadId());
        goto EXIT;
    }

    prevThread = curThread;
    curThread = curThread->GetNext();
    /* find the thread to remove */
    while (curThread != NULL)
    {
        if (curThread == pTargetThread)
        {
            /* found, fix the chain list */
            prevThread->SetNext(curThread->GetNext());
            g_dwThreadCount -= 1;
            TRACE("Thread %p removed from the process thread list\n", pTargetThread);
            goto EXIT;
        }

        prevThread = curThread;
        curThread = curThread->GetNext();
    }

    WARN("Thread %p not removed (it wasn't found in the list)\n", pTargetThread);

EXIT:
    InternalLeaveCriticalSection(pCurrentThread, &g_csProcess);
}


/*++
Function:
  PROCGetNumberOfThreads

Abstract
  Return the number of threads in the thread list.

Parameter
  void

Return
  the number of threads.
--*/
INT
CorUnix::PROCGetNumberOfThreads(
    VOID)
{
    return g_dwThreadCount;
}


/*++
Function:
  PROCProcessLock

Abstract
  Enter the critical section associated to the current process

Parameter
  void

Return
  void
--*/
VOID
PROCProcessLock(
    VOID)
{
    CPalThread * pThread =
        (PALIsThreadDataInitialized() ? InternalGetCurrentThread() : NULL);

    InternalEnterCriticalSection(pThread, &g_csProcess);
}


/*++
Function:
  PROCProcessUnlock

Abstract
  Leave the critical section associated to the current process

Parameter
  void

Return
  void
--*/
VOID
PROCProcessUnlock(
    VOID)
{
    CPalThread * pThread =
        (PALIsThreadDataInitialized() ? InternalGetCurrentThread() : NULL);

    InternalLeaveCriticalSection(pThread, &g_csProcess);
}

#if USE_SYSV_SEMAPHORES
/*++
Function:
  PROCCleanupThreadSemIds

Abstract
  Cleanup SysV semaphore ids for all threads

(no parameters, no return value)
--*/
VOID
PROCCleanupThreadSemIds(void)
{
    //
    // When using SysV semaphores, the semaphore ids used by PAL threads must be removed
    // so they can be used again.
    //

    PROCProcessLock();

    CPalThread *pTargetThread = pGThreadList;
    while (NULL != pTargetThread)
    {
        pTargetThread->suspensionInfo.DestroySemaphoreIds();
        pTargetThread = pTargetThread->GetNext();
    }

    PROCProcessUnlock();

}
#endif // USE_SYSV_SEMAPHORES

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

    old_terminator = InterlockedCompareExchange(&terminator, GetCurrentThreadId(), 0);

    if (0 != old_terminator && GetCurrentThreadId() != old_terminator)
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
        poll(NULL, 0, INFTIM);
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
        PALCommonCleanup();
    }
}

/*++
Function:
    PROCGetProcessStatus

Abstract:
    Retrieve process state information (state & exit code).

Parameters:
    DWORD process_id : PID of process to retrieve state for
    PROCESS_STATE *state : state of process (starting, running, done)
    DWORD *exit_code : exit code of process (from ExitProcess, etc.)

Return value :
    TRUE on success
--*/
PAL_ERROR
PROCGetProcessStatus(
    CPalThread *pThread,
    HANDLE hProcess,
    PROCESS_STATE *pps,
    DWORD *pdwExitCode
    )
{
    PAL_ERROR palError = NO_ERROR;
    IPalObject *pobjProcess = NULL;
    IDataLock *pDataLock;
    CProcProcessLocalData *pLocalData;
    pid_t wait_retval;
    int status;

    //
    // First, check if we already know the status of this process. This will be
    // the case if this function has already been called for the same process.
    //

    palError = g_pObjectManager->ReferenceObjectByHandle(
        pThread,
        hProcess,
        &aotProcess,
        &pobjProcess
        );

    if (NO_ERROR != palError)
    {
        goto PROCGetProcessStatusExit;
    }

    palError = pobjProcess->GetProcessLocalData(
        pThread,
        WriteLock,
        &pDataLock,
        reinterpret_cast<void **>(&pLocalData)
        );

    if (PS_DONE == pLocalData->ps)
    {
        TRACE("We already called waitpid() on process ID %#x; process has "
              "terminated, exit code is %d\n",
              pLocalData->dwProcessId, pLocalData->dwExitCode);

        *pps = pLocalData->ps;
        *pdwExitCode = pLocalData->dwExitCode;

        pDataLock->ReleaseLock(pThread, FALSE);

        goto PROCGetProcessStatusExit;
    }

    /* By using waitpid(), we can even retrieve the exit code of a non-PAL
       process. However, note that waitpid() can only provide the low 8 bits
       of the exit code. This is all that is required for the PAL spec. */
    TRACE("Looking for status of process; trying wait()");

    while(1)
    {
        /* try to get state of process, using non-blocking call */
        wait_retval = waitpid(pLocalData->dwProcessId, &status, WNOHANG);

        if ( wait_retval == (pid_t) pLocalData->dwProcessId )
        {
            /* success; get the exit code */
            if ( WIFEXITED( status ) )
            {
                *pdwExitCode = WEXITSTATUS(status);
                TRACE("Exit code was %d\n", *pdwExitCode);
            }
            else
            {
                WARN("process terminated without exiting; can't get exit "
                     "code. faking it.\n");
                *pdwExitCode = EXIT_FAILURE;
            }
            *pps = PS_DONE;
        }
        else if (0 == wait_retval)
        {
            // The process is still running.
            TRACE("Process %#x is still active.\n", pLocalData->dwProcessId);
            *pps = PS_RUNNING;
            *pdwExitCode = 0;
        }
        else if (-1 == wait_retval)
        {
            // This might happen if waitpid() had already been called, but
            // this shouldn't happen - we call waitpid once, store the
            // result, and use that afterwards.
            // One legitimate cause of failure is EINTR; if this happens we
            // have to try again. A second legitimate cause is ECHILD, which
            // happens if we're trying to retrieve the status of a currently-
            // running process that isn't a child of this process.
            if (EINTR == errno)
            {
                TRACE("waitpid() failed with EINTR; re-waiting");
                continue;
            }
            else if (ECHILD == errno)
            {
                TRACE("waitpid() failed with ECHILD; calling kill instead");
                if (kill(pLocalData->dwProcessId, 0) != 0)
                {
                    if(ESRCH == errno)
                    {
                        WARN("kill() failed with ESRCH, i.e. target "
                             "process exited and it wasn't a child, "
                             "so can't get the exit code, assuming  "
                             "it was 0.\n");
                        *pdwExitCode = 0;
                    }
                    else
                    {
                        ERROR("kill(pid, 0) failed; errno is %d (%s)\n",
                              errno, strerror(errno));
                        *pdwExitCode = EXIT_FAILURE;
                    }
                    *pps = PS_DONE;
                }
                else
                {
                    *pps = PS_RUNNING;
                    *pdwExitCode = 0;
                }
            }
            else
            {
                // Ignoring unexpected waitpid errno and assuming that
                // the process is still running
                ERROR("waitpid(pid=%u) failed with unexpected errno=%d (%s)\n",
                      pLocalData->dwProcessId, errno, strerror(errno));
                *pps = PS_RUNNING;
                *pdwExitCode = 0;
            }
        }
        else
        {
            ASSERT("waitpid returned unexpected value %d\n",wait_retval);
            *pdwExitCode = EXIT_FAILURE;
            *pps = PS_DONE;
        }
        // Break out of the loop in all cases except EINTR.
        break;
    }

    // Save the exit code for future reference (waitpid will only work once).
    if(PS_DONE == *pps)
    {
        pLocalData->ps = PS_DONE;
        pLocalData->dwExitCode = *pdwExitCode;
    }

    TRACE( "State of process 0x%08x : %d (exit code %d)\n",
           pLocalData->dwProcessId, *pps, *pdwExitCode );

    pDataLock->ReleaseLock(pThread, TRUE);

PROCGetProcessStatusExit:

    if (NULL != pobjProcess)
    {
        pobjProcess->ReleaseReference(pThread);
    }

    return palError;
}

#ifdef __APPLE__
bool GetApplicationContainerFolder(PathCharString& buffer, const char *applicationGroupId, int applicationGroupIdLength)
{
    const char *homeDir = getpwuid(getuid())->pw_dir;
    int homeDirLength = strlen(homeDir);

    // The application group container folder is defined as:
    // /user/{loginname}/Library/Group Containers/{AppGroupId}/
    return buffer.Set(homeDir, homeDirLength)
        && buffer.Append(APPLICATION_CONTAINER_BASE_PATH_SUFFIX)
        && buffer.Append(applicationGroupId, applicationGroupIdLength)
        && buffer.Append('/');
}
#endif // __APPLE__

#ifdef _DEBUG
void PROCDumpThreadList()
{
    CPalThread *pThread;

    PROCProcessLock();

    TRACE ("Threads:{\n");

    pThread = pGThreadList;
    while (NULL != pThread)
    {
        TRACE ("    {pThr=0x%p tid=%#x lwpid=%#x state=%d finsusp=%d}\n",
               pThread, (int)pThread->GetThreadId(), (int)pThread->GetLwpId(),
               (int)pThread->synchronizationInfo.GetThreadState(),
               (int)pThread->suspensionInfo.GetSuspendedForShutdown());

        pThread = pThread->GetNext();
    }
    TRACE ("Threads:}\n");

    PROCProcessUnlock();
}
#endif

/* Internal function definitions **********************************************/

/*++
Function:
  getFileName

Abstract:
    Helper function for CreateProcessW, it retrieves the executable filename
    from the application name, and the command line.

Parameters:
    IN  lpApplicationName:  first parameter from CreateProcessW (an unicode string)
    IN  lpCommandLine: second parameter from CreateProcessW (an unicode string)
    OUT lpFileName: file to be executed (the new process)

Return:
    TRUE: if the file name is retrieved
    FALSE: otherwise

--*/
static
BOOL
getFileName(
       LPCWSTR lpApplicationName,
       LPWSTR lpCommandLine,
       PathCharString& lpPathFileName)
{
    LPWSTR lpEnd;
    WCHAR wcEnd;
    char * lpFileName;
    PathCharString lpFileNamePS;
    char *lpTemp;

    if (lpApplicationName)
    {
        int length = WideCharToMultiByte(CP_ACP, 0, lpApplicationName, -1,
                                            NULL, 0, NULL, NULL);

        /* if only a file name is specified, prefix it with "./" */
        if ((*lpApplicationName != '.') && (*lpApplicationName != '/') &&
            (*lpApplicationName != '\\'))
        {
            length += 2;
            lpTemp = lpPathFileName.OpenStringBuffer(length);

            if (strcpy_s(lpTemp, length, "./") != SAFECRT_SUCCESS)
            {
                ERROR("strcpy_s failed!\n");
                return FALSE;
            }
            lpTemp+=2;

       }
       else
       {
            lpTemp = lpPathFileName.OpenStringBuffer(length);
       }

        /* Convert to ASCII */
        length = WideCharToMultiByte(CP_ACP, 0, lpApplicationName, -1,
                                     lpTemp, length, NULL, NULL);
        if (length == 0)
        {
            lpPathFileName.CloseBuffer(0);
            ASSERT("WideCharToMultiByte failure\n");
            return FALSE;
        }

        lpPathFileName.CloseBuffer(length -1);

        /* Replace '\' by '/' */
        FILEDosToUnixPathA(lpTemp);

        return TRUE;
    }
    else
    {
        /* use the Command line */

        /* filename should be the first token of the command line */

        /* first skip all leading whitespace */
        lpCommandLine = UTIL_inverse_wcspbrk(lpCommandLine,W16_WHITESPACE);
        if(NULL == lpCommandLine)
        {
            ERROR("CommandLine contains only whitespace!\n");
            return FALSE;
        }

        /* check if it is starting with a quote (") character */
        if (*lpCommandLine == 0x0022)
        {
            lpCommandLine++; /* skip the quote */

            /* file name ends with another quote */
            lpEnd = PAL_wcschr(lpCommandLine+1, 0x0022);

            /* if no quotes found, set lpEnd to the end of the Command line */
            if (lpEnd == NULL)
                lpEnd = lpCommandLine + PAL_wcslen(lpCommandLine);
        }
        else
        {
            /* filename is end out by a whitespace */
            lpEnd = PAL_wcspbrk(lpCommandLine, W16_WHITESPACE);

            /* if no whitespace found, set lpEnd to end of the Command line */
            if (lpEnd == NULL)
            {
                lpEnd = lpCommandLine + PAL_wcslen(lpCommandLine);
            }
        }

        if (lpEnd == lpCommandLine)
        {
            ERROR("application name and command line are both empty!\n");
            return FALSE;
        }

        /* replace the last character by a null */
        wcEnd = *lpEnd;
        *lpEnd = 0x0000;

        /* Convert to ASCII */
        int size = 0;
        int length = (PAL_wcslen(lpCommandLine)+1) * sizeof(WCHAR);
        lpFileName = lpFileNamePS.OpenStringBuffer(length);
        if (NULL == lpFileName)
        {
            ERROR("Not Enough Memory!\n");
            return FALSE;
        }
        if (!(size = WideCharToMultiByte(CP_ACP, 0, lpCommandLine, -1,
                                 lpFileName, length, NULL, NULL)))
        {
            ASSERT("WideCharToMultiByte failure\n");
            return FALSE;
        }

        lpFileNamePS.CloseBuffer(size - 1);
        /* restore last character */
        *lpEnd = wcEnd;

        /* Replace '\' by '/' */
        FILEDosToUnixPathA(lpFileName);
        if (!getPath(lpFileNamePS, lpPathFileName))
        {
            /* file is not in the path */
            return FALSE;
        }
    }
    return TRUE;
}

/*++
Function:
    checkFileType

Abstract:
    Return the type of the file.

Parameters:
    IN  lpFileName:  file name

Return:
    FILE_DIR: Directory
    FILE_UNIX: Unix executable file
    FILE_ERROR: Error
--*/
static
int
checkFileType( LPCSTR lpFileName)
{
    struct stat stat_data;

    /* check if the file exist */
    if ( access(lpFileName, F_OK) != 0 )
    {
        return FILE_ERROR;
    }

    /* if it's not a PE/COFF file, check if it is executable */
    if ( -1 != stat( lpFileName, &stat_data ) )
    {
        if((stat_data.st_mode & S_IFMT) == S_IFDIR )
        {
            /*The given file is a directory*/
            return FILE_DIR;
        }
        if ( UTIL_IsExecuteBitsSet( &stat_data ) )
        {
            return FILE_UNIX;
        }
        else
        {
            return FILE_ERROR;
        }
    }
    return FILE_ERROR;

}


/*++
Function:
  buildArgv

Abstract:
    Helper function for CreateProcessW, it builds the array of argument in
    a format than can be passed to execve function.lppArgv is allocated
    in this function and must be freed by the caller.

Parameters:
    IN  lpCommandLine: second parameter from CreateProcessW (an unicode string)
    IN  lpAppPath: cannonical name of the application to launched
    OUT lppArgv: array of arguments to be passed to the new process

Return:
    the number of arguments

note: this doesn't yet match precisely the behavior of Windows, but should be
sufficient.
what's here:
1) stripping nonquoted whitespace
2) handling of quoted parameters and quoted "parts" of parameters, removal of
   doublequotes (<aaaa"b bbb b"ccc> becomes <aaaab bbb bccc>)
3) \" as an escaped doublequote, both within doublequoted sequences and out
what's known missing :
1) \\ as an escaped backslash, but only if the string of '\'
   is followed by a " (escaped or not)
2) "alternate" escape sequence : double-doublequote within a double-quoted
    argument (<"aaa a""aa aaa">) expands to a single-doublequote(<aaa a"aa aaa>)
note that there may be other special cases
--*/
static
char **
buildArgv(
      LPCWSTR lpCommandLine,
      PathCharString& lpAppPath,
      UINT *pnArg)
{
    CPalThread *pThread = NULL;
    UINT iWlen;
    char *lpAsciiCmdLine;
    char *pChar;
    char **lppArgv;
    char **lppTemp;
    UINT i,j;

    *pnArg = 0;

    iWlen = WideCharToMultiByte(CP_ACP,0,lpCommandLine,-1,NULL,0,NULL,NULL);

    if(0 == iWlen)
    {
        ASSERT("Can't determine length of command line\n");
        return NULL;
    }

    pThread = InternalGetCurrentThread();
    /* make sure to allocate enough space, up for the worst case scenario */
    int iLength = (iWlen + lpAppPath.GetCount() + 2);
    lpAsciiCmdLine = (char *) InternalMalloc(iLength);

    if (lpAsciiCmdLine == NULL)
    {
        ERROR("Unable to allocate memory\n");
        return NULL;
    }

    pChar = lpAsciiCmdLine;

    /* put the cannonical name of the application as the first parameter */
    if ((strcpy_s(lpAsciiCmdLine, iLength, "\"") != SAFECRT_SUCCESS) ||
        (strcat_s(lpAsciiCmdLine, iLength, lpAppPath) != SAFECRT_SUCCESS) ||
        (strcat_s(lpAsciiCmdLine, iLength,  "\"") != SAFECRT_SUCCESS) ||
        (strcat_s(lpAsciiCmdLine, iLength, " ") != SAFECRT_SUCCESS))
    {
        ERROR("strcpy_s/strcat_s failed!\n");
        return NULL;
    }

    pChar = lpAsciiCmdLine + strlen (lpAsciiCmdLine);

    /* let's skip the first argument in the command line */

    /* strip leading whitespace; function returns NULL if there's only
        whitespace, so the if statement below will work correctly */
    lpCommandLine = UTIL_inverse_wcspbrk((LPWSTR)lpCommandLine, W16_WHITESPACE);

    if (lpCommandLine)
    {
        LPCWSTR stringstart = lpCommandLine;

        do
        {
            /* find first whitespace or dquote character */
            lpCommandLine = PAL_wcspbrk(lpCommandLine,W16_WHITESPACE_DQUOTE);
            if(NULL == lpCommandLine)
            {
                /* no whitespace or dquote found : first arg is only arg */
                break;
            }
            else if('"' == *lpCommandLine)
            {
                /* got a dquote; skip over it if it's escaped; make sure we
                    don't try to look before the first character in the
                    string */
                if(lpCommandLine > stringstart && '\\' == lpCommandLine[-1])
                {
                    lpCommandLine++;
                    continue;
                }

                /* found beginning of dquoted sequence, run to the end */
                /* don't stop if we hit an escaped dquote */
                lpCommandLine++;
                while( *lpCommandLine )
                {
                    lpCommandLine = PAL_wcschr(lpCommandLine, '"');
                    if(NULL == lpCommandLine)
                    {
                        /* no ending dquote, arg runs to end of string */
                        break;
                    }
                    if('\\' != lpCommandLine[-1])
                    {
                        /* dquote is not escaped, dquoted sequence is over*/
                        break;
                    }
                    lpCommandLine++;
                }
                if(NULL == lpCommandLine || '\0' == *lpCommandLine)
                {
                    /* no terminating dquote */
                    break;
                }

                /* step over dquote, keep looking for end of arg */
                lpCommandLine++;
            }
            else
            {
                /* found whitespace : end of arg. */
                lpCommandLine++;
                break;
            }
        }while(lpCommandLine);
    }

    /* Convert to ASCII */
    if (lpCommandLine)
    {
        if (!WideCharToMultiByte(CP_ACP, 0, lpCommandLine, -1,
                                 pChar, iWlen+1, NULL, NULL))
        {
            ASSERT("Unable to convert to a multibyte string\n");
            free(lpAsciiCmdLine);
            return NULL;
        }
    }

    pChar = lpAsciiCmdLine;

    /* loops through all the arguments, to find out how many arguments there
       are; while looping replace whitespace by \0 */

    /* skip leading whitespace (and replace by '\0') */
    /* note : there shouldn't be any, command starts either with PE loader name
       or computed application path, but this won't hurt */
    while (*pChar)
    {
        if (!isspace((unsigned char) *pChar))
        {
           break;
        }
        WARN("unexpected whitespace in command line!\n");
        *pChar++ = '\0';
    }

    while (*pChar)
    {
        (*pnArg)++;

        /* find end of current arg */
        while(*pChar && !isspace((unsigned char) *pChar))
        {
            if('"' == *pChar)
            {
                /* skip over dquote if it's escaped; make sure we don't try to
                   look before the start of the string for the \ */
                if(pChar > lpAsciiCmdLine && '\\' == pChar[-1])
                {
                    pChar++;
                    continue;
                }

                /* found leading dquote : look for ending dquote */
                pChar++;
                while (*pChar)
                {
                    pChar = strchr(pChar,'"');
                    if(NULL == pChar)
                    {
                        /* no ending dquote found : argument extends to the end
                           of the string*/
                        break;
                    }
                    if('\\' != pChar[-1])
                    {
                        /* found a dquote, and it's not escaped : quoted
                           sequence is over*/
                        break;
                    }
                    /* found a dquote, but it was escaped : skip over it, keep
                       looking */
                    pChar++;
                }
                if(NULL == pChar || '\0' == *pChar)
                {
                    /* reached the end of the string : we're done */
                    break;
                }
            }
            pChar++;
        }
        if(NULL == pChar)
        {
            /* reached the end of the string : we're done */
            break;
        }
        /* reached end of arg; replace trailing whitespace by '\0', to split
           arguments into separate strings */
        while (isspace((unsigned char) *pChar))
        {
            *pChar++ = '\0';
        }
    }

    /* allocate lppargv according to the number of arguments
       in the command line */
    lppArgv = (char **) InternalMalloc((((*pnArg)+1) * sizeof(char *)));

    if (lppArgv == NULL)
    {
        free(lpAsciiCmdLine);
        return NULL;
    }

    lppTemp = lppArgv;

    /* at this point all parameters are separated by NULL
       we need to fill the array of arguments; we must also remove all dquotes
       from arguments (new process shouldn't see them) */
    for (i = *pnArg, pChar = lpAsciiCmdLine; i; i--)
    {
        /* skip NULLs */
        while (!*pChar)
        {
            pChar++;
        }

        *lppTemp = pChar;

        /* go to the next parameter, removing dquotes as we go along */
        j = 0;
        while (*pChar)
        {
            /* copy character if it's not a dquote */
            if('"' != *pChar)
            {
                /* if it's the \ of an escaped dquote, skip over it, we'll
                   copy the " instead */
                if( '\\' == pChar[0] && '"' == pChar[1] )
                {
                    pChar++;
                }
                (*lppTemp)[j++] = *pChar;
            }
            pChar++;
        }
        /* re-NULL terminate the argument */
        (*lppTemp)[j] = '\0';

        lppTemp++;
    }

    *lppTemp = NULL;

    return lppArgv;
}


/*++
Function:
  getPath

Abstract:
    Helper function for CreateProcessW, it looks in the path environment
    variable to find where the process to executed is.

Parameters:
    IN  lpFileName: file name to search in the path
    OUT lpPathFileName: returned string containing the path and the filename

Return:
    TRUE if found
    FALSE otherwise
--*/
static
BOOL
getPath(
      PathCharString& lpFileNameString,
      PathCharString& lpPathFileName)
{
    LPSTR lpPath;
    LPSTR lpNext;
    LPSTR lpCurrent;
    LPWSTR lpwstr;
    INT n;
    INT nextLen;
    INT slashLen;
    CPalThread *pThread = NULL;
    LPCSTR lpFileName = lpFileNameString.GetString();

    /* if a path is specified, only look there */
    if(strchr(lpFileName, '/'))
    {
        if (access (lpFileName, F_OK) == 0)
        {
            if (!lpPathFileName.Set(lpFileNameString))
            {
                TRACE("Set of StackString failed!\n");
                return FALSE;
            }

            TRACE("file %s exists\n", lpFileName);
            return TRUE;
        }
        else
        {
            TRACE("file %s doesn't exist.\n", lpFileName);
            return FALSE;
        }
    }

    /* first look in directory from which the application loaded */
    lpwstr = g_lpwstrAppDir;

    if (lpwstr)
    {
        /* convert path to multibyte, check buffer size */
        n = WideCharToMultiByte(CP_ACP, 0, lpwstr, -1, NULL, 0,
            NULL, NULL);

        if (!lpPathFileName.Reserve(n + lpFileNameString.GetCount() + 1 ))
        {
            ERROR("StackString Reserve failed!\n");
            return FALSE;
        }

        lpPath = lpPathFileName.OpenStringBuffer(n);

        n = WideCharToMultiByte(CP_ACP, 0, lpwstr, -1, lpPath, n,
            NULL, NULL);

        if (n == 0)
        {
            lpPathFileName.CloseBuffer(0);
            ASSERT("WideCharToMultiByte failure!\n");
            return FALSE;
        }

        lpPathFileName.CloseBuffer(n - 1);

        lpPathFileName.Append("/", 1);
        lpPathFileName.Append(lpFileNameString);

        if (access(lpPathFileName, F_OK) == 0)
        {
            TRACE("found %s in application directory (%s)\n", lpFileName, lpPathFileName.GetString());
            return TRUE;
        }
    }

    /* then try the current directory */
    if (!lpPathFileName.Reserve(lpFileNameString.GetCount()  + 2))
    {
        ERROR("StackString Reserve failed!\n");
        return FALSE;
    }

    lpPathFileName.Set("./", 2);
    lpPathFileName.Append(lpFileNameString);

    if (access (lpPathFileName, R_OK) == 0)
    {
        TRACE("found %s in current directory.\n", lpFileName);
        return TRUE;
    }

    pThread = InternalGetCurrentThread();

    /* Then try to look in the path */
    lpPath = EnvironGetenv("PATH");

    if (!lpPath)
    {
        ERROR("EnvironGetenv returned NULL for $PATH\n");
        return FALSE;
    }

    lpNext = lpPath;

    /* search in every path directory */
    TRACE("looking for file %s in $PATH (%s)\n", lpFileName, lpPath);
    while (lpNext)
    {
        /* skip all leading ':' */
        while(*lpNext==':')
        {
            lpNext++;
        }

        /* search for ':' */
        lpCurrent = strchr(lpNext, ':');
        if (lpCurrent)
        {
            *lpCurrent++ = '\0';
        }

        nextLen = strlen(lpNext);
        slashLen = (lpNext[nextLen-1] == '/') ? 0:1;

        if (!lpPathFileName.Reserve(nextLen + lpFileNameString.GetCount() + 1))
        {
            free(lpPath);
            ERROR("StackString ran out of memory for full path\n");
            return FALSE;
        }

        lpPathFileName.Set(lpNext, nextLen);

        if( slashLen == 1)
        {
            /* append a '/' if there's no '/' at the end of the path */
            lpPathFileName.Append("/", 1);
        }

        lpPathFileName.Append(lpFileNameString);

        if ( access (lpPathFileName, F_OK) == 0)
        {
            TRACE("Found %s in $PATH element %s\n", lpFileName, lpNext);
            free(lpPath);
            return TRUE;
        }

        lpNext = lpCurrent;  /* search in the next directory */
    }

    free(lpPath);
    TRACE("File %s not found in $PATH\n", lpFileName);
    return FALSE;
}
