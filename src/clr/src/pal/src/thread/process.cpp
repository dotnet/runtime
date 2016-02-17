// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    process.cpp

Abstract:

    Implementation of process object and functions related to processes.



--*/

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
#include "pal/dbgmsg.h"
#include "pal/utils.h"
#include "pal/misc.h"
#include "pal/virtual.h"
#include "pal/stackstring.hpp"

#include <errno.h>
#if HAVE_POLL
#include <poll.h>
#else
#include "pal/fakepoll.h"
#endif  // HAVE_POLL

#include <sys/mman.h>
#include <sys/types.h>
#include <signal.h>
#include <sys/wait.h>
#include <sys/time.h>
#include <sys/resource.h>
#include <debugmacrosext.h>
#include <semaphore.h>

using namespace CorUnix;

SET_DEFAULT_DEBUG_CHANNEL(PROCESS);

CObjectType CorUnix::otProcess(
                otiProcess,
                NULL,
                NULL,
                0,
                sizeof(CProcProcessLocalData),
                0,
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

static
DWORD
PALAPI
StartupHelperThread(
    LPVOID p);

//
// Helper memory page used by the FlushProcessWriteBuffers
//
static int s_helperPage[VIRTUAL_PAGE_SIZE / sizeof(int)] __attribute__((aligned(VIRTUAL_PAGE_SIZE)));

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

// Process ID of this process.
DWORD gPID = (DWORD) -1;

// Function to call during PAL/process shutdown/abort
Volatile<PSHUTDOWN_CALLBACK> g_shutdownCallback = nullptr;

//
// Key used for associating CPalThread's with the underlying pthread
// (through pthread_setspecific)
//
pthread_key_t CorUnix::thObjKey;

#define PROCESS_PELOADER_FILENAME  "clix"

static WCHAR W16_WHITESPACE[]= {0x0020, 0x0009, 0x000D, 0};
static WCHAR W16_WHITESPACE_DQUOTE[]= {0x0020, 0x0009, 0x000D, '"', 0};

enum FILETYPE
{
    FILE_ERROR,/*ERROR*/
    FILE_PE,   /*PE/COFF file*/
    FILE_UNIX, /*Unix Executable*/
    FILE_DIR   /*Directory*/
};

PAL_ERROR
PROCGetProcessStatus(
    CPalThread *pThread,
    HANDLE hProcess,
    PROCESS_STATE *pps,
    DWORD *pdwExitCode
    );

static BOOL getFileName(LPCWSTR lpApplicationName, LPWSTR lpCommandLine,
                        char *lpFileName);
static char ** buildArgv(LPCWSTR lpCommandLine, LPSTR lpAppPath,
                         UINT *pnArg, BOOL prependLoader);
static BOOL getPath(LPCSTR lpFileName, UINT iLen, LPSTR  lpPathFileName);
static int checkFileType(char *lpFileName);
static BOOL PROCEndProcess(HANDLE hProcess, UINT uExitCode,
                           BOOL bTerminateUnconditionally);

ProcessModules *GetProcessModulesFromHandle(IN HANDLE hProcess, OUT LPDWORD lpCount);
ProcessModules *CreateProcessModules(IN DWORD dwProcessId, OUT LPDWORD lpCount);
void DestroyProcessModules(IN ProcessModules *listHead);

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
  CreateProcessA

Note:
  Only Standard handles need to be inherited.
  Security attributes parameters are not used.

See MSDN doc.
--*/
BOOL
PALAPI
CreateProcessA(
           IN LPCSTR lpApplicationName,
           IN LPSTR lpCommandLine,
           IN LPSECURITY_ATTRIBUTES lpProcessAttributes,
           IN LPSECURITY_ATTRIBUTES lpThreadAttributes,
           IN BOOL bInheritHandles,
           IN DWORD dwCreationFlags,
           IN LPVOID lpEnvironment,
           IN LPCSTR lpCurrentDirectory,
           IN LPSTARTUPINFOA lpStartupInfo,
           OUT LPPROCESS_INFORMATION lpProcessInformation)
{
    PAL_ERROR palError = NO_ERROR;
    CPalThread *pThread;
    STARTUPINFOW StartupInfoW;
    LPWSTR CommandLineW = NULL;
    LPWSTR ApplicationNameW = NULL;
    LPWSTR CurrentDirectoryW = NULL;

    int n;

    PERF_ENTRY(CreateProcessA);
    ENTRY("CreateProcessA(lpAppName=%p (%s), lpCmdLine=%p (%s), lpProcessAttr=%p, "
          "lpThreadAttr=%p, bInherit=%d, dwFlags=%#x, lpEnv=%p, "
          "lpCurrentDir=%p (%s), lpStartupInfo=%p, lpProcessInfo=%p)\n",
           lpApplicationName?lpApplicationName:"NULL",
           lpApplicationName?lpApplicationName:"NULL",
           lpCommandLine?lpCommandLine:"NULL",
           lpCommandLine?lpCommandLine:"NULL",
           lpProcessAttributes, lpThreadAttributes, bInheritHandles,
           dwCreationFlags, lpEnvironment,
           lpCurrentDirectory?lpCurrentDirectory:"NULL",
           lpCurrentDirectory?lpCurrentDirectory:"NULL",
           lpStartupInfo, lpProcessInformation);

    pThread = InternalGetCurrentThread();

    if(lpStartupInfo == NULL)
    {
        ASSERT("lpStartupInfo is NULL!\n");
        palError = ERROR_INVALID_PARAMETER;
        goto done;
    }

    /* convert parameters to Unicode */

    if(lpApplicationName)
    {
        n = MultiByteToWideChar(CP_ACP, 0, lpApplicationName, -1, NULL, 0);
        if(0 == n)
        {
            ASSERT("MultiByteToWideChar failed!\n");
            palError = ERROR_INTERNAL_ERROR;
            goto done;
        }
        ApplicationNameW = (LPWSTR)InternalMalloc(sizeof(WCHAR)*n);
        if(!ApplicationNameW)
        {
            ERROR("malloc() failed!\n");
            palError = ERROR_NOT_ENOUGH_MEMORY;
            goto done;
        }
        MultiByteToWideChar(CP_ACP, 0, lpApplicationName, -1, ApplicationNameW,
                            n);
    }

    if(lpCommandLine)
    {
        n = MultiByteToWideChar(CP_ACP, 0, lpCommandLine, -1, NULL, 0);
        if(0 == n)
        {
            ASSERT("MultiByteToWideChar failed!\n");
            palError = ERROR_INTERNAL_ERROR;
            goto done;
        }
        CommandLineW = (LPWSTR)InternalMalloc(sizeof(WCHAR)*n);
        if(!CommandLineW)
        {
            ERROR("malloc() failed!\n");
            palError = ERROR_NOT_ENOUGH_MEMORY;
            goto done;
        }
        MultiByteToWideChar(CP_ACP, 0, lpCommandLine, -1, CommandLineW, n);
    }

    if(lpCurrentDirectory)
    {
        n = MultiByteToWideChar(CP_ACP, 0, lpCurrentDirectory, -1, NULL, 0);
        if(0 == n)
        {
            ASSERT("MultiByteToWideChar failed!\n");
            palError = ERROR_INTERNAL_ERROR;
            goto done;
        }
        CurrentDirectoryW = (LPWSTR)InternalMalloc(sizeof(WCHAR)*n);
        if(!CurrentDirectoryW)
        {
            ERROR("malloc() failed!\n");
            palError = ERROR_NOT_ENOUGH_MEMORY;
            goto done;
        }
        MultiByteToWideChar(CP_ACP, 0, lpCurrentDirectory, -1,
                            CurrentDirectoryW, n);
    }

    // lpEnvironment should remain ansi on the call to CreateProcessW

    StartupInfoW.cb = sizeof StartupInfoW;
    StartupInfoW.dwFlags = lpStartupInfo->dwFlags;
    StartupInfoW.hStdError = lpStartupInfo->hStdError;
    StartupInfoW.hStdInput = lpStartupInfo->hStdInput;
    StartupInfoW.hStdOutput = lpStartupInfo->hStdOutput;
    /* all other members are PAL_Undefined, we can ignore them */

    palError  = InternalCreateProcess(
        pThread,
        ApplicationNameW,
        CommandLineW,
        lpProcessAttributes,
        lpThreadAttributes,
        bInheritHandles,
        dwCreationFlags,
        lpEnvironment,
        CurrentDirectoryW,
        &StartupInfoW,
        lpProcessInformation
        );
done:
    InternalFree(ApplicationNameW);
    InternalFree(CommandLineW);
    InternalFree(CurrentDirectoryW);

    if (NO_ERROR != palError)
    {
        pThread->SetLastError(palError);
    }

    LOGEXIT("CreateProcessA returns BOOL %d\n", NO_ERROR == palError);
    PERF_EXIT(CreateProcessA);
    return NO_ERROR == palError;
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
        bInheritHandles,
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
        0,
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
    BOOL bInheritHandles,
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
    char * lpFileName;
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

    lpFileName = lpFileNamePS.OpenStringBuffer(MAX_LONGPATH-1);
    if (NULL == lpFileName)
    {
        palError = ERROR_NOT_ENOUGH_MEMORY;
        goto InternalCreateProcessExit;
    }
    if (!getFileName(lpApplicationName, lpCommandLine, lpFileName))
    {
        ERROR("Can't find executable!\n");
        palError = ERROR_FILE_NOT_FOUND;
        goto InternalCreateProcessExit;
    }
    
    lpFileNamePS.CloseBuffer(MAX_LONGPATH-1);
    /* check type of file */
    iRet = checkFileType(lpFileName);

    switch (iRet)
    {
        case FILE_ERROR: /* file not found, or not an executable */
            WARN ("File is not valid (%s)", lpFileName);
            palError = ERROR_FILE_NOT_FOUND;
            goto InternalCreateProcessExit;

        case FILE_PE: /* PE/COFF file */
            /*Get the path name where the PAL DLL was loaded from
             * I am using MAX_LONGPATH - (strlen(PROCESS_PELOADER_FILENAME)+1)
             * as the length as I have to append the file name at the end */
            if ( PAL_GetPALDirectoryA( lpFileName,
                                      (MAX_LONGPATH - (strlen(PROCESS_PELOADER_FILENAME)+1))))
            {
                if ((strcat_s(lpFileName, lpFileNamePS.GetSizeOf(), "/") != SAFECRT_SUCCESS) ||
                    (strcat_s(lpFileName, lpFileNamePS.GetSizeOf(), PROCESS_PELOADER_FILENAME) != SAFECRT_SUCCESS))
                {
                    ERROR("strcpy_s/strcat_s failed!\n");
                    palError = ERROR_INTERNAL_ERROR;
                    goto InternalCreateProcessExit;
                }
            }
            else
            {
                ASSERT("PAL_GetPALDirectoryA failed to return the"
                       "pal installation directory \n");
                palError = ERROR_INTERNAL_ERROR;
                goto InternalCreateProcessExit;
            }

            break;

        case FILE_UNIX: /* Unix binary file */
            break;  /* nothing to do */

        case FILE_DIR:/*Directory*/
            WARN ("File is a Directory (%s)", lpFileName);
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
    lppArgv = buildArgv(lpCommandLine, lpFileName, &nArg, iRet==1);

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
        PROCESS_ALL_ACCESS,
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
            execve(lpFileName, lppArgv, EnvironmentArray);
        }
        else
        {
            execve(lpFileName, lppArgv, palEnvironment);
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
        InternalFree(EnvironmentArray);
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
        InternalFree(*lppArgv);
        InternalFree(lppArgv);
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
    for (;;);
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

        // exit() runs atexit handlers possibly registered by foreign code.
        // The right thing to do here is to leave the PAL.  If our client
        // registered our own PAL_Terminate with atexit(), the latter will
        // explicitly re-enter us.
        PAL_Leave(PAL_BoundaryBottom);
        
        if (bTerminateUnconditionally)
        {
            // abort() has the semantics that 
            // (1) it doesn't run atexit handlers
            // (2) can invoke CrashReporter or produce a coredump,
            // which is appropriate for TerminateProcess calls
            abort();
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

#define RuntimeStartupSemaphoreName "/RuntimeStartupEvent%08x"
#define RuntimeContinueSemaphoreName "/RuntimeContinueEvent%08x"

static bool IsCoreClrModule(const char* pModulePath)
{
    // Strip off everything up to and including the last slash in the path to get name
    const char* pModuleName = pModulePath;
    while (strchr(pModuleName, '/') != NULL)
    {
        pModuleName = strchr(pModuleName, '/');
        pModuleName++; // pass the slash
    }

    return _stricmp(pModuleName, MAKEDLLNAME_A("coreclr")) == 0;
}

class PAL_RuntimeStartupHelper
{
    LONG m_ref;
    bool m_canceled;
    DWORD m_processId;
    PPAL_STARTUP_CALLBACK m_callback;
    PVOID m_parameter;
    DWORD m_threadId;
    HANDLE m_threadHandle;

    // Debugger waits on this semaphore and the runtime signals it on startup.
    sem_t *m_startupSem;

    // Debuggee waits on this semaphore and the debugger signals it after the callback returns.
    sem_t *m_continueSem;

public:
    PAL_RuntimeStartupHelper(DWORD dwProcessId, PPAL_STARTUP_CALLBACK pfnCallback, PVOID parameter) :
        m_ref(1),
        m_canceled(false),
        m_processId(dwProcessId),
        m_callback(pfnCallback),
        m_parameter(parameter),
        m_threadId(0),
        m_threadHandle(NULL),
        m_startupSem(SEM_FAILED),
        m_continueSem(SEM_FAILED)
    {
    }

    ~PAL_RuntimeStartupHelper()
    {
        if (m_startupSem != SEM_FAILED)
        {
            char startupSemName[NAME_MAX - 4];
            sprintf_s(startupSemName, sizeof(startupSemName), RuntimeStartupSemaphoreName, m_processId);

            sem_close(m_startupSem);
            sem_unlink(startupSemName);
        }
        if (m_continueSem != SEM_FAILED)
        {
            char continueSemName[NAME_MAX - 4];
            sprintf_s(continueSemName, sizeof(continueSemName), RuntimeContinueSemaphoreName, m_processId);

            sem_close(m_continueSem);
            sem_unlink(continueSemName);
        }
        if (m_threadHandle != NULL)
        {
            CloseHandle(m_threadHandle);
        }
    }

    LONG AddRef()
    {
        LONG ref = InterlockedIncrement(&m_ref);    
        return ref;
    }

    LONG Release()
    {
        LONG ref = InterlockedDecrement(&m_ref);
        if (ref == 0)
        {
            delete this;
        }
        return ref;
    }

    PAL_ERROR Register()
    {
        CPalThread *pThread = InternalGetCurrentThread();
        char startupSemName[NAME_MAX - 4];
        char continueSemName[NAME_MAX - 4];
        PAL_ERROR pe = NO_ERROR;

        sprintf_s(startupSemName, sizeof(startupSemName), RuntimeStartupSemaphoreName, m_processId);
        sprintf_s(continueSemName, sizeof(continueSemName), RuntimeContinueSemaphoreName, m_processId);

        TRACE("PAL_RuntimeStartupHelper.Register startup '%s' continue '%s'\n", startupSemName, continueSemName);

        // Create the continue semaphore first so we don't race with PAL_NotifyRuntimeStarted. This open will fail if another 
        // debugger is trying to attach to this process because the name will already exist.
        m_continueSem = sem_open(continueSemName, O_CREAT | O_EXCL | O_RDWR, S_IRWXU, 0);
        if (m_continueSem == SEM_FAILED)
        {
            TRACE("sem_open(continue) failed: errno is %d (%s)\n", errno, strerror(errno));
            pe = ERROR_INVALID_PARAMETER;
            goto exit;
        }

        // Create the debuggee startup semaphore so the runtime (debuggee) knows to wait for a debugger 
        // connection.
        m_startupSem = sem_open(startupSemName, O_CREAT | O_EXCL | O_RDWR, S_IRWXU, 0);
        if (m_startupSem == SEM_FAILED)
        {
            TRACE("sem_open(startup) failed: errno is %d (%s)\n", errno, strerror(errno));
            pe = ERROR_INVALID_PARAMETER;
            goto exit;
        }

        // Add a reference for the thread handler
        AddRef();

        pe = InternalCreateThread(
            pThread,
            NULL,
            0,
            ::StartupHelperThread,
            this,
            0,
            UserCreatedThread,
            &m_threadId,
            &m_threadHandle);

        if (NO_ERROR != pe)
        {
            TRACE("InternalCreateThread failed %d\n", pe);
            Release();
            goto exit;
        }

    exit:
        return pe;
    }

    void Unregister()
    {
        m_canceled = true;

        // Tell the runtime to continue
        if (sem_post(m_continueSem) != 0)
        {
            ASSERT("sem_post(continueSem) failed: errno is %d (%s)\n", errno, strerror(errno));
        }

        // Tell the worker thread to continue
        if (sem_post(m_startupSem) != 0)
        {
            ASSERT("sem_post(startupSem) failed: errno is %d (%s)\n", errno, strerror(errno));
        }

        // Don't need to wait for the worker thread if unregister called on it
        if (m_threadId != (DWORD)THREADSilentGetCurrentThreadId())
        {
            // Wait for work thread to exit
            if (WaitForSingleObject(m_threadHandle, INFINITE) != WAIT_OBJECT_0)
            {
                ASSERT("WaitForSingleObject\n");
            }
        }
    }

    PAL_ERROR InvokeStartupCallback(bool *pCoreClrExists)
    {
        PAL_ERROR pe = NO_ERROR;

        *pCoreClrExists = false;

        // Enumerate all the modules in the process and invoke the callback 
        // for the coreclr module if found.
        DWORD count;
        ProcessModules *listHead = CreateProcessModules(m_processId, &count);
        if (listHead == NULL)
        {
            TRACE("CreateProcessModules failed for pid %d\n", m_processId);
            pe = ERROR_INVALID_PARAMETER;
            goto exit;
        }

        for (ProcessModules *entry = listHead; entry != NULL; entry = entry->Next)
        {
            if (IsCoreClrModule(entry->Name))
            {
                *pCoreClrExists = true;

                PAL_CPP_TRY
                {
                    TRACE("InvokeStartupCallback executing callback %s\n", entry->Name);
                    m_callback(entry->Name, entry->BaseAddress, m_parameter);
                }
                PAL_CPP_CATCH_ALL
                {
                }
                PAL_CPP_ENDTRY

                // Currently only the first coreclr module in a process is supported
                break;
            }
        }

    exit:
        if (*pCoreClrExists)
        {
            // Wake up all the runtimes
            if (sem_post(m_continueSem) != 0)
            {
                ASSERT("sem_post(continueSem) failed: errno is %d (%s)\n", errno, strerror(errno));
            }
        }

        if (listHead != NULL)
        {
            DestroyProcessModules(listHead);
        }
        return pe;
    }

    void StartupHelperThread()
    {
        bool coreclrExists = false;

        PAL_ERROR pe = InvokeStartupCallback(&coreclrExists);
        if (pe == NO_ERROR)
        {
            if (!coreclrExists && !m_canceled)
            {
                // Wait until the coreclr runtime (debuggee) starts up
                if (sem_wait(m_startupSem) == 0)
                {
                    if (!m_canceled)
                    {
                        pe = InvokeStartupCallback(&coreclrExists);
                        if (pe == NO_ERROR)
                        {
                            // We should always find a coreclr module
                            _ASSERTE(coreclrExists);
                        }
                    }
                }
                else 
                {
                    TRACE("sem_wait(startup) failed: errno is %d (%s)\n", errno, strerror(errno));
                    pe = ERROR_INVALID_HANDLE;
                }
            }
        }

        if (pe != NO_ERROR)
        {
            SetLastError(pe);
            m_callback(NULL, NULL, m_parameter);
        }
    }
};

static
DWORD 
PALAPI
StartupHelperThread(LPVOID p)
{
    TRACE("PAL's StartupHelperThread starting\n");

    PAL_RuntimeStartupHelper *helper = (PAL_RuntimeStartupHelper *)p;
    helper->StartupHelperThread();
    helper->Release();
    return 0;
}

/*++
    PAL_RegisterForRuntimeStartup

Parameters:
    dwProcessId - process id of runtime process
    pfnCallback - function to callback for coreclr module found
    parameter - data to pass to callback
    ppUnregisterToken - pointer to put PAL_UnregisterForRuntimeStartup token.

Return value:
    PAL_ERROR

Note:
    If the modulePath or hModule is NULL when the callback is invoked, an error occured
    and GetLastError() will return the Win32 error code.

    The callback is always invoked on a separate thread and this API returns immediately.

    Only the first coreclr module is currently supported.

--*/
DWORD
PALAPI
PAL_RegisterForRuntimeStartup(
    IN DWORD dwProcessId,
    IN PPAL_STARTUP_CALLBACK pfnCallback,
    IN PVOID parameter,
    OUT PVOID *ppUnregisterToken)
{
    _ASSERTE(pfnCallback != NULL);
    _ASSERTE(ppUnregisterToken != NULL);

    PAL_RuntimeStartupHelper *helper = new PAL_RuntimeStartupHelper(dwProcessId, pfnCallback, parameter);

    // Create the debuggee startup semaphore so the runtime (debuggee) knows to wait for 
    // a debugger connection.
    PAL_ERROR pe = helper->Register();
    if (NO_ERROR != pe)
    {
        helper->Release();
        helper = NULL;
    }

    *ppUnregisterToken = helper;
    return pe;
}

/*++
    PAL_UnregisterForRuntimeStartup

    Stops/cancels startup notification. This API can be called in the startup callback. Otherwise,
    it will block until the callback thread finishes and no more callbacks will be initiated after
    this API returns.

Parameters:
    dwUnregisterToken - token from PAL_RegisterForRuntimeStartup or NULL.

Return value:
    PAL_ERROR
--*/
DWORD
PALAPI
PAL_UnregisterForRuntimeStartup(
    IN PVOID pUnregisterToken)
{
    if (pUnregisterToken != NULL)
    {
        PAL_RuntimeStartupHelper *helper = (PAL_RuntimeStartupHelper *)pUnregisterToken;
        helper->Unregister();
        helper->Release();
    }
    return NO_ERROR;
}

/*++
    PAL_NotifyRuntimeStarted

    Signals the debugger waiting for runtime startup notification to continue and
    waits until the debugger signals us to continue.

Parameters:
    None

Return value:
    TRUE - succeeded, FALSE - failed
--*/
BOOL
PALAPI
PAL_NotifyRuntimeStarted()
{
    char szStartupSemName[NAME_MAX - 4];
    char szContinueSemName[NAME_MAX - 4];
    sem_t *startupSem = SEM_FAILED;
    sem_t *continueSem = SEM_FAILED;
    BOOL result = TRUE;

    sprintf_s(szStartupSemName, sizeof(szStartupSemName), RuntimeStartupSemaphoreName, gPID);
    sprintf_s(szContinueSemName, sizeof(szContinueSemName), RuntimeContinueSemaphoreName, gPID);

    TRACE("PAL_NotifyRuntimeStarted opening startup '%s' continue '%s'\n", szStartupSemName, szContinueSemName);

    // Open the debugger startup semaphore. If it doesn't exists, then we do nothing and
    // the function is successful.
    startupSem = sem_open(szStartupSemName, O_RDWR);
    if (startupSem == SEM_FAILED)
    {
        TRACE("sem_open(%s) failed: %d (%s)\n", szStartupSemName, errno, strerror(errno));
        goto exit;
    }

    // Open the debuggee continue semaphore. If we can open the startup sem and not this one
    // something is seriously wrong.
    continueSem = sem_open(szContinueSemName, O_RDWR);
    if (continueSem == SEM_FAILED)
    {
        ASSERT("sem_open(%s) failed: %d (%s)\n", szContinueSemName, errno, strerror(errno));
        result = FALSE;
        goto exit;
    }

    // Wake up the debugger waiting for startup
    if (sem_post(startupSem) != 0)
    {
        ASSERT("sem_post(startupSem) failed: errno is %d (%s)\n", errno, strerror(errno));
        result = FALSE;
        goto exit;
    }

    // Now wait until the debugger notification is finished
    if (sem_wait(continueSem) != 0)
    {
        ASSERT("sem_wait(continueSem) failed: errno is %d (%s)\n", errno, strerror(errno));
        result = FALSE;
        goto exit;
    }

exit:
    if (startupSem != SEM_FAILED)
    {
        sem_close(startupSem);
    }
    if (continueSem != SEM_FAILED)
    {
        sem_close(continueSem);
    }
    return result;
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
    __int64 calcTime;
    const __int64 SECS_TO_NS = 1000000000; /* 10^9 */
    const __int64 USECS_TO_NS = 1000;      /* 10^3 */


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

    if (lpUserTime)
    {
        /* Get the time of user mode execution, in 100s of nanoseconds */
        calcTime = (__int64)resUsage.ru_utime.tv_sec * SECS_TO_NS;
        calcTime += (__int64)resUsage.ru_utime.tv_usec * USECS_TO_NS;
        calcTime /= 100; /* Produce the time in 100s of ns */
        /* Assign the time into lpUserTime */
        lpUserTime->dwLowDateTime = (DWORD)calcTime;
        lpUserTime->dwHighDateTime = (DWORD)(calcTime >> 32);
    }

    if (lpKernelTime)
    {
        /* Get the time of kernel mode execution, in 100s of nanoseconds */
        calcTime = (__int64)resUsage.ru_stime.tv_sec * SECS_TO_NS;
        calcTime += (__int64)resUsage.ru_stime.tv_usec * USECS_TO_NS;
        calcTime /= 100; /* Produce the time in 100s of ns */
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
        dwDesiredAccess,
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
Function:
  EnumProcessModules

Abstract
  Returns a process's module list

Return
  TRUE if it succeeded, FALSE otherwise

Notes
  This API is tricky because the module handles are never closed/freed so there can't be any 
  allocations for the module handle or name strings, etc. The "handles" are actually the base 
  addresses of the modules. The module handles should only be used by GetModuleFileNameExW 
  below. 
--*/
BOOL
PALAPI
EnumProcessModules(
    IN HANDLE hProcess,
    OUT HMODULE *lphModule,
    IN DWORD cb,
    OUT LPDWORD lpcbNeeded)
{
    PERF_ENTRY(EnumProcessModules);
    ENTRY("EnumProcessModules(hProcess=0x%08x, cb=%d)\n", hProcess, cb);

    BOOL result = TRUE;
    DWORD count = 0;
    ProcessModules *listHead = GetProcessModulesFromHandle(hProcess, &count);
    if (listHead != NULL)
    {
        for (ProcessModules *entry = listHead; entry != NULL; entry = entry->Next)
        {
            if (cb <= 0)
            {
                break;
            }
            cb -= sizeof(HMODULE);
            *lphModule = (HMODULE)entry->BaseAddress;
            lphModule++;
        }
    }
    else
    {
        result = FALSE;
    }

    if (lpcbNeeded)
    {
        // This return value isn't exactly up to spec because it should return the actual
        // number of modules in the process even if "cb" isn't big enough but for our use
        // it works just fine.
        (*lpcbNeeded) = count * sizeof(HMODULE);
    }

    LOGEXIT("EnumProcessModules returns %d\n", result);
    PERF_EXIT(EnumProcessModules);
    return result;
}

/*++
Function:
  GetModuleFileNameExW

  Used only with module handles returned from EnumProcessModule (for dbgshim). 

--*/
DWORD
PALAPI
GetModuleFileNameExW(
    IN HANDLE hProcess,
    IN HMODULE hModule,
    OUT LPWSTR lpFilename,
    IN DWORD nSize
)
{
    DWORD result = 0;
    DWORD count = 0;

    ProcessModules *listHead = GetProcessModulesFromHandle(hProcess, &count);
    if (listHead != NULL)
    {
        for (ProcessModules *entry = listHead; entry != NULL; entry = entry->Next)
        {
            if ((HMODULE)entry->BaseAddress == hModule)
            {
                // Convert CHAR string into WCHAR string
                result = MultiByteToWideChar(CP_ACP, 0, entry->Name, -1, lpFilename, nSize);
                break;
            }
        }
    }

    return result;
}

/*++
Function:
 GetProcessModulesFromHandle

Abstract
  Returns a process's module list

Return
  ProcessModules * list

--*/
ProcessModules *
GetProcessModulesFromHandle(
    IN HANDLE hProcess,
    OUT LPDWORD lpCount)
{
    CPalThread* pThread = InternalGetCurrentThread();
    CProcProcessLocalData *pLocalData = NULL;
    ProcessModules *listHead = NULL;
    IPalObject *pobjProcess = NULL;
    IDataLock *pDataLock = NULL;
    PAL_ERROR palError = NO_ERROR;
    DWORD dwProcessId = 0;
    DWORD count = 0;

    _ASSERTE(lpCount != NULL);

    if (hPseudoCurrentProcess == hProcess)
    {
        pobjProcess = g_pobjProcess;
    }
    else
    {
        CAllowedObjectTypes aotProcess(otiProcess);

        palError = g_pObjectManager->ReferenceObjectByHandle(
            pThread,
            hProcess,
            &aotProcess,
            0,
            &pobjProcess);

        if (NO_ERROR != palError)
        {
            pThread->SetLastError(ERROR_INVALID_HANDLE);
            goto exit;
        }
    }

    palError = pobjProcess->GetProcessLocalData(
        pThread,
        WriteLock,
        &pDataLock,
        reinterpret_cast<void **>(&pLocalData));

    _ASSERTE(NO_ERROR == palError);

    dwProcessId = pLocalData->dwProcessId;
    listHead = pLocalData->pProcessModules;
    count = pLocalData->cProcessModules;

    // If the module list hasn't been created yet, create it now
    if (listHead == NULL)
    {
        listHead = CreateProcessModules(dwProcessId, &count);
        if (listHead == NULL)
        {
            pThread->SetLastError(ERROR_INVALID_PARAMETER);
            goto exit;
        }

        if (pLocalData != NULL)
        {
            pLocalData->pProcessModules = listHead;
            pLocalData->cProcessModules = count;
        }
    }

exit:
    if (NULL != pDataLock)
    {
        pDataLock->ReleaseLock(pThread, TRUE);
    }
    if (NULL != pobjProcess)
    {
        pobjProcess->ReleaseReference(pThread);
    }

    *lpCount = count;
    return listHead;
}

/*++
Function:
  CreateProcessModules

Abstract
  Returns a process's module list

Return
  ProcessModules * list

--*/
ProcessModules *
CreateProcessModules(
    IN DWORD dwProcessId,
    OUT LPDWORD lpCount)
{
    ProcessModules *listHead = NULL;
    _ASSERTE(lpCount != NULL);

#if defined(__APPLE__)

    // For OSx, the "vmmap" command outputs something similar to the /proc/*/maps file so popen the
    // command and read the relevant lines:
    //
    // ...
    // ==== regions for process 347  (non-writable and writable regions are interleaved)
    // REGION TYPE                      START - END             [ VSIZE] PRT/MAX SHRMOD  REGION DETAIL
    // __TEXT                 000000010446d000-0000000104475000 [   32K] r-x/rwx SM=COW  /Users/mikem/coreclr/bin/Product/OSx.x64.Debug/corerun
    // __DATA                 0000000104475000-0000000104476000 [    4K] rw-/rwx SM=PRV  /Users/mikem/coreclr/bin/Product/OSx.x64.Debug/corerun
    // __LINKEDIT             0000000104476000-000000010447a000 [   16K] r--/rwx SM=COW  /Users/mikem/coreclr/bin/Product/OSx.x64.Debug/corerun
    // Kernel Alloc Once      000000010447a000-000000010447b000 [    4K] rw-/rwx SM=PRV
    // MALLOC (admin)         000000010447b000-000000010447c000 [    4K] r--/rwx SM=ZER
    // ...
    // MALLOC (admin)         00000001044ab000-00000001044ac000 [    4K] r--/rwx SM=PRV
    // __TEXT                 00000001044ac000-0000000104c84000 [ 8032K] r-x/rwx SM=COW  /Users/mikem/coreclr/bin/Product/OSx.x64.Debug/libcoreclr.dylib
    // __TEXT                 0000000104c84000-0000000104c85000 [    4K] rwx/rwx SM=PRV  /Users/mikem/coreclr/bin/Product/OSx.x64.Debug/libcoreclr.dylib
    // __TEXT                 0000000104c85000-000000010513b000 [ 4824K] r-x/rwx SM=COW  /Users/mikem/coreclr/bin/Product/OSx.x64.Debug/libcoreclr.dylib
    // __TEXT                 000000010513b000-000000010513c000 [    4K] rwx/rwx SM=PRV  /Users/mikem/coreclr/bin/Product/OSx.x64.Debug/libcoreclr.dylib
    // __TEXT                 000000010513c000-000000010516f000 [  204K] r-x/rwx SM=COW  /Users/mikem/coreclr/bin/Product/OSx.x64.Debug/libcoreclr.dylib
    // __DATA                 000000010516f000-00000001051ce000 [  380K] rw-/rwx SM=COW  /Users/mikem/coreclr/bin/Product/OSx.x64.Debug/libcoreclr.dylib
    // __DATA                 00000001051ce000-00000001051fa000 [  176K] rw-/rwx SM=PRV  /Users/mikem/coreclr/bin/Product/OSx.x64.Debug/libcoreclr.dylib
    // __LINKEDIT             00000001051fa000-0000000105bac000 [ 9928K] r--/rwx SM=COW  /Users/mikem/coreclr/bin/Product/OSx.x64.Debug/libcoreclr.dylib
    // VM_ALLOCATE            0000000105bac000-0000000105bad000 [    4K] r--/rw- SM=SHM
    // MALLOC (admin)         0000000105bad000-0000000105bae000 [    4K] r--/rwx SM=ZER
    // MALLOC                 0000000105bae000-0000000105baf000 [    4K] rw-/rwx SM=ZER
    char *line = NULL;
    size_t lineLen = 0;
    int count = 0;
    ssize_t read;

    char vmmapCommand[100];
    int chars = snprintf(vmmapCommand, sizeof(vmmapCommand), "/usr/bin/vmmap -interleaved %d -wide", dwProcessId);
    _ASSERTE(chars > 0 && chars <= sizeof(vmmapCommand));

    FILE *vmmapFile = popen(vmmapCommand, "r");
    if (vmmapFile == NULL)
    {
        goto exit;
    }

    // Reading maps file line by line
    while ((read = getline(&line, &lineLen, vmmapFile)) != -1)
    {
        void *startAddress, *endAddress;
        char moduleName[PATH_MAX];
        int size;

        if (sscanf(line, "__TEXT %p-%p [ %dK] %*[-/rwxsp] SM=%*[A-Z] %s\n", &startAddress, &endAddress, &size, moduleName) == 4)
        {
            bool dup = false;
            for (ProcessModules *entry = listHead; entry != NULL; entry = entry->Next)
            {
                if (strcmp(moduleName, entry->Name) == 0)
                {
                    dup = true;
                    break;
                }
            }

            if (!dup)
            {
                int cbModuleName = strlen(moduleName) + 1;
                ProcessModules *entry = (ProcessModules *)InternalMalloc(sizeof(ProcessModules) + cbModuleName);
                if (entry == NULL)
                {
                    DestroyProcessModules(listHead);
                    listHead = NULL;
                    count = 0;
                    break;
                }
                strcpy_s(entry->Name, cbModuleName, moduleName);
                entry->BaseAddress = startAddress;
                entry->Next = listHead;
                listHead = entry;
                count++;
            }
        }
    }

    *lpCount = count;

    free(line); // We didn't allocate line, but as per contract of getline we should free it
    pclose(vmmapFile);

#elif defined(HAVE_PROCFS_CTL)

    // Here we read /proc/<pid>/maps file in order to parse it and figure out what it says 
    // about a library we are looking for. This file looks something like this:
    //
    // [address]      [perms] [offset] [dev] [inode]     [pathname] - HEADER is not preset in an actual file
    //
    // 35b1800000-35b1820000 r-xp 00000000 08:02 135522  /usr/lib64/ld-2.15.so
    // 35b1a1f000-35b1a20000 r--p 0001f000 08:02 135522  /usr/lib64/ld-2.15.so
    // 35b1a20000-35b1a21000 rw-p 00020000 08:02 135522  /usr/lib64/ld-2.15.so
    // 35b1a21000-35b1a22000 rw-p 00000000 00:00 0       [heap]
    // 35b1c00000-35b1dac000 r-xp 00000000 08:02 135870  /usr/lib64/libc-2.15.so
    // 35b1dac000-35b1fac000 ---p 001ac000 08:02 135870  /usr/lib64/libc-2.15.so
    // 35b1fac000-35b1fb0000 r--p 001ac000 08:02 135870  /usr/lib64/libc-2.15.so
    // 35b1fb0000-35b1fb2000 rw-p 001b0000 08:02 135870  /usr/lib64/libc-2.15.so

    // Making something like: /proc/123/maps
    char mapFileName[100]; 
    char *line = NULL;
    size_t lineLen = 0;
    int count = 0;
    ssize_t read;

    INDEBUG(int chars = )
    snprintf(mapFileName, sizeof(mapFileName), "/proc/%d/maps", dwProcessId);
    _ASSERTE(chars > 0 && chars <= sizeof(mapFileName));

    FILE *mapsFile = fopen(mapFileName, "r");
    if (mapsFile == NULL) 
    {
        goto exit;
    }

    // Reading maps file line by line 
    while ((read = getline(&line, &lineLen, mapsFile)) != -1) 
    {
        void *startAddress, *endAddress, *offset;
        int devHi, devLo, inode;
        char moduleName[PATH_MAX];

        if (sscanf(line, "%p-%p %*[-rwxsp] %p %x:%x %d %s\n", &startAddress, &endAddress, &offset, &devHi, &devLo, &inode, moduleName) == 7)
        {
            if (inode != 0)
            {
                bool dup = false;
                for (ProcessModules *entry = listHead; entry != NULL; entry = entry->Next)
                {
                    if (strcmp(moduleName, entry->Name) == 0)
                    {
                        dup = true;
                        break;
                    }
                }

                if (!dup)
                {
                    int cbModuleName = strlen(moduleName) + 1;
                    ProcessModules *entry = (ProcessModules *)InternalMalloc(sizeof(ProcessModules) + cbModuleName);
                    if (entry == NULL)
                    {
                        DestroyProcessModules(listHead);
                        listHead = NULL;
                        count = 0;
                        break;
                    }
                    strcpy_s(entry->Name, cbModuleName, moduleName);
                    entry->BaseAddress = startAddress;
                    entry->Next = listHead;
                    listHead = entry;
                    count++;
                }
            }
        }
    }

    *lpCount = count;

    free(line); // We didn't allocate line, but as per contract of getline we should free it
    fclose(mapsFile);
#else
    _ASSERTE(!"Not implemented on this platform");
#endif
exit:
    return listHead;
}

/*++
Function:
    DestroyProcessModules

Abstract
  Cleans up the process module table.

Return
  None

--*/
void
DestroyProcessModules(IN ProcessModules *listHead)
{
    for (ProcessModules *entry = listHead; entry != NULL; )
    {
        ProcessModules *next = entry->Next;
        InternalFree(entry);
        entry = next;
    }
}

/*++
Function:
  PROCNotifyProcessShutdown
  
  Calls the abort handler to do any shutdown cleanup. Call be called 
  from the unhandled native exception handler.

(no return value)
--*/
__attribute__((destructor)) 
void PROCNotifyProcessShutdown()
{
    TRACE("PROCNotifyProcessShutdown %p\n", g_shutdownCallback.RawValue());

    PSHUTDOWN_CALLBACK callback = InterlockedExchangePointer(&g_shutdownCallback, NULL);
    if (callback != NULL)
    {
        callback();
    }
}

/*++
Function:
  PROCAbort()

  Aborts the process after calling the shutdown cleanup handler. This function
  should be called instead of calling abort() directly.
  
  Does not return
--*/
PAL_NORETURN
void
PROCAbort()
{
    PROCNotifyProcessShutdown();
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
BOOL InitializeFlushProcessWriteBuffers()
{
    // Verify that the s_helperPage is really aligned to the VIRTUAL_PAGE_SIZE
    _ASSERTE((((SIZE_T)s_helperPage) & (VIRTUAL_PAGE_SIZE - 1)) == 0);

    // Locking the page ensures that it stays in memory during the two mprotect
    // calls in the FlushProcessWriteBuffers below. If the page was unmapped between
    // those calls, they would not have the expected effect of generating IPI.
    int status = mlock(s_helperPage, VIRTUAL_PAGE_SIZE);

    if (status != 0)
    {
        return FALSE;
    }

    status = pthread_mutex_init(&flushProcessWriteBuffersMutex, NULL);
    if (status != 0)
    {
        munlock(s_helperPage, VIRTUAL_PAGE_SIZE);
    }

    return status == 0;
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
    int status = pthread_mutex_lock(&flushProcessWriteBuffersMutex);
    FATAL_ASSERT(status == 0, "Failed to lock the flushProcessWriteBuffersMutex lock");

    // Changing a helper memory page protection from read / write to no access 
    // causes the OS to issue IPI to flush TLBs on all processors. This also
    // results in flushing the processor buffers.
    status = mprotect(s_helperPage, VIRTUAL_PAGE_SIZE, PROT_READ | PROT_WRITE);
    FATAL_ASSERT(status == 0, "Failed to change helper page protection to read / write");

    // Ensure that the page is dirty before we change the protection so that
    // we prevent the OS from skipping the global TLB flush.
    InterlockedIncrement(s_helperPage);

    status = mprotect(s_helperPage, VIRTUAL_PAGE_SIZE, PROT_NONE);
    FATAL_ASSERT(status == 0, "Failed to change helper page protection to no access");

    status = pthread_mutex_unlock(&flushProcessWriteBuffersMutex);
    FATAL_ASSERT(status == 0, "Failed to unlock the flushProcessWriteBuffersMutex lock");
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
        0,
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
        INT n = lstrlenW(lpwstrFullPath) + 1;

        int iLen = n;
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
            InternalFree(initial_dir);
            palError = ERROR_INTERNAL_ERROR;
            goto exit;
        }

        lpwstr[0] = '/';

        InternalFree(g_lpwstrAppDir);
        g_lpwstrAppDir = initial_dir;
    }

    InternalFree(g_lpwstrCmdLine);
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
        PROCESS_ALL_ACCESS,
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
    InternalFree(g_lpwstrAppDir);
    
    /* Free the stored command line */
    InternalFree(g_lpwstrCmdLine);

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
void
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
void
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
    void)
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
void
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
        0,
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
            if(EINTR == errno)
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
       char *lpPathFileName)
{
    LPWSTR lpEnd;
    WCHAR wcEnd;
    char * lpFileName;
    PathCharString lpFileNamePS;
    char *lpTemp;

    if (lpApplicationName)
    {
        int path_size = MAX_LONGPATH;
        lpTemp = lpPathFileName;
        /* if only a file name is specified, prefix it with "./" */
        if ((*lpApplicationName != '.') && (*lpApplicationName != '/') &&
            (*lpApplicationName != '\\'))
        {
            if (strcpy_s(lpPathFileName, MAX_LONGPATH, "./") != SAFECRT_SUCCESS)
            {
                ERROR("strcpy_s failed!\n");
                return FALSE;
            }

            lpTemp+=2;
            path_size -= 2;
       }

        /* Convert to ASCII */
        if (!WideCharToMultiByte(CP_ACP, 0, lpApplicationName, -1,
                                 lpTemp, path_size, NULL, NULL))
        {
            ASSERT("WideCharToMultiByte failure\n");
            return FALSE;
        }

        /* Replace '\' by '/' */
        FILEDosToUnixPathA(lpPathFileName);

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

        lpFileNamePS.CloseBuffer(size);
        /* restore last character */
        *lpEnd = wcEnd;

        /* Replace '\' by '/' */
        FILEDosToUnixPathA(lpFileName);

        if (!getPath(lpFileName, MAX_LONGPATH, lpPathFileName))
        {
            /* file is not in the path */
            return FALSE;
        }
    }
    return TRUE;
}

/*++
Functions: VAL16 & VAL32
   Byte swapping functions for reading in little endian format files
--*/
#ifdef BIGENDIAN

static inline USHORT    VAL16(USHORT x)
{
    return ( ((x & 0xFF00) >> 8) | ((x & 0x00FF) << 8) );
}
static inline ULONG   VAL32(DWORD x)
{
    return( ((x & 0xFF000000L) >> 24) |
            ((x & 0x00FF0000L) >>  8) |
            ((x & 0x0000FF00L) <<  8) |
            ((x & 0x000000FFL) << 24) );
}
#else   // BIGENDIAN
// For little-endian machines, do nothing
static __inline USHORT  VAL16(unsigned short x) { return x; }
static __inline DWORD   VAL32(DWORD x){ return x; }
#endif  // BIGENDIAN

static const DWORD IMAGE_DOS_SIGNATURE = 0x5A4D;
static const DWORD IMAGE_NT_SIGNATURE  = 0x00004550;
static const DWORD IMAGE_SIZEOF_NT_OPTIONAL32_HEADER     = 224;
static const DWORD IMAGE_NT_OPTIONAL_HDR32_MAGIC         = 0x10b;
static const DWORD IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR  = 14;

typedef struct _IMAGE_DATA_DIRECTORY {
    DWORD   VirtualAddress;
    DWORD   Size;
} IMAGE_DATA_DIRECTORY, *PIMAGE_DATA_DIRECTORY;

typedef struct _IMAGE_OPTIONAL_HEADER {
    //
    // Standard fields.
    //

    WORD    Magic;
    BYTE    MajorLinkerVersion;
    BYTE    MinorLinkerVersion;
    DWORD   SizeOfCode;
    DWORD   SizeOfInitializedData;
    DWORD   SizeOfUninitializedData;
    DWORD   AddressOfEntryPoint;
    DWORD   BaseOfCode;
    DWORD   BaseOfData;

    //
    // NT additional fields.
    //

    DWORD   ImageBase;
    DWORD   SectionAlignment;
    DWORD   FileAlignment;
    WORD    MajorOperatingSystemVersion;
    WORD    MinorOperatingSystemVersion;
    WORD    MajorImageVersion;
    WORD    MinorImageVersion;
    WORD    MajorSubsystemVersion;
    WORD    MinorSubsystemVersion;
    DWORD   Win32VersionValue;
    DWORD   SizeOfImage;
    DWORD   SizeOfHeaders;
    DWORD   CheckSum;
    WORD    Subsystem;
    WORD    DllCharacteristics;
    DWORD   SizeOfStackReserve;
    DWORD   SizeOfStackCommit;
    DWORD   SizeOfHeapReserve;
    DWORD   SizeOfHeapCommit;
    DWORD   LoaderFlags;
    DWORD   NumberOfRvaAndSizes;
    IMAGE_DATA_DIRECTORY DataDirectory[16];
} IMAGE_OPTIONAL_HEADER32, *PIMAGE_OPTIONAL_HEADER32;

typedef struct _IMAGE_FILE_HEADER {
    WORD    Machine;
    WORD    NumberOfSections;
    DWORD   TimeDateStamp;
    DWORD   PointerToSymbolTable;
    DWORD   NumberOfSymbols;
    WORD    SizeOfOptionalHeader;
    WORD    Characteristics;
} IMAGE_FILE_HEADER, *PIMAGE_FILE_HEADER;

typedef struct _IMAGE_NT_HEADERS {
    DWORD Signature;
    IMAGE_FILE_HEADER FileHeader;
    IMAGE_OPTIONAL_HEADER32 OptionalHeader;
} IMAGE_NT_HEADERS32, *PIMAGE_NT_HEADERS32;

typedef struct _IMAGE_DOS_HEADER {      /* DOS .EXE header*/
    WORD   e_magic;                     /* Magic number*/
    WORD   e_cblp;                      /* Bytes on last page of file*/
    WORD   e_cp;                        /* Pages in file*/
    WORD   e_crlc;                      /* Relocations*/
    WORD   e_cparhdr;                   /* Size of header in paragraphs*/
    WORD   e_minalloc;                  /* Minimum extra paragraphs needed*/
    WORD   e_maxalloc;                  /* Maximum extra paragraphs needed*/
    WORD   e_ss;                        /* Initial (relative) SS value*/
    WORD   e_sp;                        /* Initial SP value*/
    WORD   e_csum;                      /* Checksum*/
    WORD   e_ip;                        /* Initial IP value*/
    WORD   e_cs;                        /* Initial (relative) CS value*/
    WORD   e_lfarlc;                    /* File address of relocation table*/
    WORD   e_ovno;                      /* Overlay number*/
    WORD   e_res[4];                    /* Reserved words*/
    WORD   e_oemid;                     /* OEM identifier (for e_oeminfo)*/
    WORD   e_oeminfo;                   /* OEM information; e_oemid specific*/
    WORD   e_res2[10];                  /* Reserved words*/
    LONG   e_lfanew;                    /* File address of new exe header*/
  } IMAGE_DOS_HEADER, *PIMAGE_DOS_HEADER;


/*++
Function:
  isManagedExecutable

Determines if the passed in file is a managed executable

--*/
static
int
isManagedExecutable(LPSTR lpFileName)
{
    HANDLE hFile = INVALID_HANDLE_VALUE;
    DWORD cbRead;
    IMAGE_DOS_HEADER        dosheader;
    IMAGE_NT_HEADERS32      NtHeaders; 
    BOOL ret = 0;

    /* then check if it is a PE/COFF file */ 
    if((hFile = CreateFileA(lpFileName, GENERIC_READ, FILE_SHARE_READ, NULL,
                            OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN,
                            NULL)) == INVALID_HANDLE_VALUE)
    {        
          goto isManagedExecutableExit;
    }
      
    /* Open the file and read the IMAGE_DOS_HEADER structure */ 
    if(!ReadFile(hFile, &dosheader, sizeof(IMAGE_DOS_HEADER), &cbRead, NULL) || cbRead != sizeof(IMAGE_DOS_HEADER) )
      goto isManagedExecutableExit;
       
    /* check the DOS headers */
    if ( (dosheader.e_magic != VAL16(IMAGE_DOS_SIGNATURE)) || (VAL32(dosheader.e_lfanew) <= 0) ) 
      goto isManagedExecutableExit;         
 
    /* Advance the file pointer to File address of new exe header */
    if( SetFilePointer(hFile, VAL32(dosheader.e_lfanew), NULL, FILE_BEGIN) == 0xffffffff)
      goto isManagedExecutableExit;
            
    if( !ReadFile(hFile, &NtHeaders , sizeof(IMAGE_NT_HEADERS32), &cbRead, NULL) || cbRead != sizeof(IMAGE_NT_HEADERS32) )
      goto isManagedExecutableExit;
   
    /* check the NT headers */   
    if ((NtHeaders.Signature != VAL32(IMAGE_NT_SIGNATURE)) ||
        (NtHeaders.FileHeader.SizeOfOptionalHeader != VAL16(IMAGE_SIZEOF_NT_OPTIONAL32_HEADER)) ||
        (NtHeaders.OptionalHeader.Magic != VAL16(IMAGE_NT_OPTIONAL_HDR32_MAGIC)))
        goto isManagedExecutableExit;
     
    /* Check that the virtual address of IMAGE_DIRECTORY_ENTRY_COMHEADER is non-null */
    if ( NtHeaders.OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR].VirtualAddress == 0 )
        goto isManagedExecutableExit;
  
    /* The file is a managed executable */
    ret =  1;
     
 isManagedExecutableExit:
    /* Close the file handle if we opened it */
    if ( hFile != INVALID_HANDLE_VALUE )
      CloseHandle(hFile);

    return ret;
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
    FILE_PE: managed PE/COFF file
    FILE_ERROR: Error
--*/
static
int
checkFileType( char *lpFileName)
{ 
    struct stat stat_data;

    /* check if the file exist */
    if ( access(lpFileName, F_OK) != 0 )
    {
        return FILE_ERROR;
    }
    
    if( isManagedExecutable(lpFileName) )
    {
        return FILE_PE;
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
    IN  prependLoader:  If True first argument should be the PE loader

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
      LPSTR lpAppPath,
      UINT *pnArg,
      BOOL prependLoader)
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
    int iLength = (iWlen + strlen(PROCESS_PELOADER_FILENAME) + strlen(lpAppPath) + 2);
    lpAsciiCmdLine = (char *) InternalMalloc(iLength);

    if (lpAsciiCmdLine == NULL)
    {
        ERROR("Unable to allocate memory\n");
        return NULL;
    }

    pChar = lpAsciiCmdLine;

    /* Prepend the PE loader, if it's required */
    if (prependLoader)
    {
        if ((strcpy_s(lpAsciiCmdLine, iLength,  PROCESS_PELOADER_FILENAME) != SAFECRT_SUCCESS) ||
            (strcat_s(lpAsciiCmdLine, iLength, " ") != SAFECRT_SUCCESS))
        {
            ERROR("strcpy_s/strcat_s failed!\n");
            return NULL;
        }

        pChar = lpAsciiCmdLine + strlen (lpAsciiCmdLine);

    }
    else
    {
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
    }

    /* Convert to ASCII */
    if (lpCommandLine)
    {
        if (!WideCharToMultiByte(CP_ACP, 0, lpCommandLine, -1,
                                 pChar, iWlen+1, NULL, NULL))
        {
            ASSERT("Unable to convert to a multibyte string\n");
            InternalFree(lpAsciiCmdLine);
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
        InternalFree(lpAsciiCmdLine);
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
    IN  iLen: length of lpPathFileName buffer
    OUT lpPathFileName: returned string containing the path and the filename

Return:
    TRUE if found
    FALSE otherwise
--*/
static
BOOL
getPath(
      LPCSTR lpFileName,
      UINT iLen,
      LPSTR  lpPathFileName)
{
    LPSTR lpPath;
    LPSTR lpNext;
    LPSTR lpCurrent;
    LPWSTR lpwstr;
    INT n;
    INT nextLen;
    INT slashLen;
    CPalThread *pThread = NULL;

    /* if a path is specified, only look there */
    if(strchr(lpFileName, '/'))
    {
        if (access (lpFileName, F_OK) == 0)
        {
            if (strcpy_s(lpPathFileName, iLen, lpFileName) != SAFECRT_SUCCESS)
            {
                TRACE("strcpy_s failed!\n");
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
        n = WideCharToMultiByte(CP_ACP, 0, lpwstr, -1, lpPathFileName, iLen,
            NULL, NULL);
        if (n == 0)
        {
            ASSERT("WideCharToMultiByte failure!\n");
            return FALSE;
        }

        n += strlen(lpFileName) + 2;
        if (n > (INT)iLen)
        {
            ERROR("Buffer too small for full path!\n");
            return FALSE;
        }

        if ((strcat_s(lpPathFileName, iLen, "/") != SAFECRT_SUCCESS) ||
            (strcat_s(lpPathFileName, iLen, lpFileName) != SAFECRT_SUCCESS))
        {
            ERROR("strcat_s failed!\n");
            return FALSE;
        }

        if (access(lpPathFileName, F_OK) == 0)
        {
            TRACE("found %s in application directory (%s)\n", lpFileName, lpPathFileName);
            return TRUE;
        }
    }

    /* then try the current directory */
    if ((strcpy_s(lpPathFileName, iLen, "./") != SAFECRT_SUCCESS) ||
        (strcat_s(lpPathFileName, iLen, lpFileName) != SAFECRT_SUCCESS))
    {
        ERROR("strcpy_s/strcat_s failed!\n");
        return FALSE;
    }

    if (access (lpPathFileName, R_OK) == 0)
    {
        TRACE("found %s in current directory.\n", lpFileName);
        return TRUE;
    }

    pThread = InternalGetCurrentThread();
    /* Then try to look in the path */
    int iLen2 = strlen(MiscGetenv("PATH"))+1;
    lpPath = (LPSTR) InternalMalloc(iLen2);

    if (!lpPath)
    {
        ERROR("couldn't allocate memory for $PATH\n");
        return FALSE;
    }

    if (strcpy_s(lpPath, iLen2, MiscGetenv("PATH")) != SAFECRT_SUCCESS)
    {
        ERROR("strcpy_s failed!");
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

        /* verify if the path fit in the OUT parameter */
        if (slashLen + nextLen + strlen (lpFileName) >= iLen)
        {
            InternalFree(lpPath);
            ERROR("buffer too small for full path\n");
            return FALSE;
        }

        strcpy_s (lpPathFileName, iLen, lpNext);

        /* append a '/' if there's no '/' at the end of the path */
        if ( slashLen == 1 )
        {
            strcat_s (lpPathFileName, iLen, "/");
        }
        
        strcat_s (lpPathFileName, iLen, lpFileName);

        if (access (lpPathFileName, F_OK) == 0)
        {
            TRACE("Found %s in $PATH element %s\n", lpFileName, lpNext);
            InternalFree(lpPath);
            return TRUE;
        }

        lpNext = lpCurrent;  /* search in the next directory */
    }

    InternalFree(lpPath);
    TRACE("File %s not found in $PATH\n", lpFileName);
    return FALSE;
}

/*++
Function:
    ~CProcProcessLocalData

Process data destructor
--*/
CorUnix::CProcProcessLocalData::~CProcProcessLocalData()
{
    if (pProcessModules != NULL)
    {
        DestroyProcessModules(pProcessModules);
    }
}
        
