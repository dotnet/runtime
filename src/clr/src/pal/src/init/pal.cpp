//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*++



Module Name:

    init/pal.cpp

Abstract:

    Implementation of PAL exported functions not part of the Win32 API.



--*/

#include "pal/thread.hpp"
#include "pal/synchobjects.hpp"
#include "pal/procobj.hpp"
#include "pal/cs.hpp"
#include "pal/file.hpp"
#include "pal/map.hpp"
#include "../objmgr/shmobjectmanager.hpp"
#include "pal/seh.hpp"
#include "pal/palinternal.h"
#include "pal/dbgmsg.h"
#include "pal/shmemory.h"
#include "pal/process.h"
#include "../thread/procprivate.hpp"
#include "pal/module.h"
#include "pal/virtual.h"
#include "pal/misc.h"
#include "pal/utils.h"
#include "pal/debug.h"
#include "pal/locale.h"
#include "pal/init.h"

#if HAVE_MACH_EXCEPTIONS
#include "../exception/machexception.h"
#endif

#include <stdlib.h>
#include <unistd.h>
#include <pwd.h>
#include <errno.h>
#include <sys/types.h>
#include <sys/param.h>
#include <sys/resource.h>
#include <sys/stat.h>
#include <limits.h>
#include <string.h>
#include <fcntl.h>

#if HAVE_POLL
#include <poll.h>
#else
#include "pal/fakepoll.h"
#endif  // HAVE_POLL

#if defined(__APPLE__)
#include <sys/sysctl.h>
int CacheLineSize;
#endif //__APPLE__

#ifdef __APPLE__
#include <mach-o/dyld.h>
#endif // __APPLE__

using namespace CorUnix;

//
// $$TODO The C++ compiler doesn't like pal/cruntime.h so duplicate the
// necessary prototype here
//

extern "C" BOOL CRTInitStdStreams( void );


SET_DEFAULT_DEBUG_CHANNEL(PAL);

Volatile<INT> init_count = 0;
Volatile<BOOL> shutdown_intent = 0;
static BOOL g_fThreadDataAvailable = FALSE;
static pthread_mutex_t init_critsec_mutex = PTHREAD_MUTEX_INITIALIZER;

/* critical section to protect access to init_count. This is allocated on the
   very first PAL_Initialize call, and is freed afterward. */
static PCRITICAL_SECTION init_critsec = NULL;

char g_szCoreCLRPath[MAX_PATH] = { 0 };

static int Initialize(int argc, const char *const argv[], DWORD flags);
static BOOL INIT_IncreaseDescriptorLimit(void);
static LPWSTR INIT_FormatCommandLine (CPalThread *pThread, int argc, const char * const *argv);
static LPWSTR INIT_FindEXEPath(CPalThread *pThread, LPCSTR exe_name);

#ifdef _DEBUG
extern void PROCDumpThreadList(void);
#endif

char g_ExePath[MAX_PATH] = { 0 };

#if defined(__APPLE__)
static bool RunningNatively()
{
    int ret = 0;
    size_t sz = sizeof(ret);
    if (sysctlbyname("sysctl.proc_native", &ret, &sz, NULL, 0) != 0)
    {
        // if the sysctl failed, we'll assume this OS does not support
        // binary translation - so we must be running natively.
        return true;
    }
    return ret != 0;
}
#endif // __APPLE__

/*++
Function:
  PAL_Initialize

Abstract:
  This function is the first function of the PAL to be called.
  Internal structure initialization is done here. It could be called
  several time by the same process, a reference count is kept.

Return:
  0 if successful
  -1 if it failed

--*/
int
PALAPI
PAL_Initialize(
    int argc,
    const char *const argv[])
{
    return Initialize(argc, argv, PAL_INITIALIZE_ALL);
}

/*++
Function:
  PAL_InitializeDLL

Abstract:
    Initializes the non-runtime DLLs/modules like the DAC and SOS.

Return:
  0 if successful
  -1 if it failed

--*/
int
PALAPI
PAL_InitializeDLL()
{
    return Initialize(0, NULL, PAL_INITIALIZE_DLL);
}

/*++
Function:
  Initialize

Abstract:
  Common PAL initialization function.

Return:
  0 if successful
  -1 if it failed

--*/
int
Initialize(
    int argc,
    const char *const argv[],
    DWORD flags)
{
    PAL_ERROR palError = ERROR_GEN_FAILURE;
    CPalThread *pThread = NULL;
    CSharedMemoryObjectManager *pshmom = NULL;
    LPWSTR command_line = NULL;
    LPWSTR exe_path = NULL;
    int retval = -1;
    bool fFirstTimeInit = false;

    /* the first ENTRY within the first call to PAL_Initialize is a special
       case, since debug channels are not initialized yet. So in that case the
       ENTRY will be called after the DBG channels initialization */
    ENTRY_EXTERNAL("PAL_Initialize(argc = %d argv = %p)\n", argc, argv);

    /*Firstly initiate a lastError */
    SetLastError(ERROR_GEN_FAILURE);

#ifdef __APPLE__
    if (!RunningNatively())
    {
        SetLastError(ERROR_BAD_FORMAT);
        goto exit;
    }
#endif // __APPLE__

    CriticalSectionSubSysInitialize();

    if(NULL == init_critsec)
    {
        pthread_mutex_lock(&init_critsec_mutex); // prevents race condition of two threads 
                                                 // initializing the critical section.
        if(NULL == init_critsec)
        {
            static CRITICAL_SECTION temp_critsec;

            // Want this critical section to NOT be internal to avoid the use of unsafe region markers.
            InternalInitializeCriticalSectionAndSpinCount(&temp_critsec, 0, false);

            if(NULL != InterlockedCompareExchangePointer(&init_critsec, &temp_critsec, NULL))
            {
                // Another thread got in before us! shouldn't happen, if the PAL
                // isn't initialized there shouldn't be any other threads 
                WARN("Another thread initialized the critical section\n");
                InternalDeleteCriticalSection(&temp_critsec);
            }
        }
        pthread_mutex_unlock(&init_critsec_mutex);
    }

    InternalEnterCriticalSection(pThread, init_critsec); // here pThread is always NULL

    if(init_count==0)
    {
        // Set our pid.
        gPID = getpid();

        fFirstTimeInit = true;

        // Initialize the TLS lookaside cache
        if (FALSE == TLSInitialize())
        {
            goto done;
        }
    
        // Initialize the environment.
        if (FALSE == MiscInitialize())
        {
            goto done;
        }

        // Initialize debug channel settings before anything else.
        // This depends on the environment, so it must come after
        // MiscInitialize.
        if (FALSE == DBG_init_channels())
        {
            goto done;
        }

#if _DEBUG
        // Verify that our page size is what we think it is. If it's
        // different, we can't run.
        if (VIRTUAL_PAGE_SIZE != getpagesize())
        {
            ASSERT("VIRTUAL_PAGE_SIZE is incorrect for this system!\n"
                   "Change include/pal/virtual.h and clr/src/inc/stdmacros.h "
                   "to reflect the correct page size of %d.\n", getpagesize());
        }
#endif  // _DEBUG
    
        if (!INIT_IncreaseDescriptorLimit())
        {
            ERROR("Unable to increase the file descriptor limit!\n");
            // We can continue if this fails; we'll just have problems if
            // we use large numbers of threads or have many open files.
        }

#if !HAVE_COREFOUNDATION || ENABLE_DOWNLEVEL_FOR_NLS
        if( !CODEPAGEInit() )
        {
            ERROR( "Unable to initialize the locks or the codepage.\n" );
            goto done;
        }
#endif // !HAVE_COREFOUNDATION || ENABLE_DOWNLEVEL_FOR_NLS

        /* initialize the shared memory infrastructure */
        if(!SHMInitialize())
        {
            ERROR("Shared memory initialization failed!\n");
            goto CLEANUP0;
        }

        //
        // Initialize global process data
        //

        palError = InitializeProcessData();
        if (NO_ERROR != palError)
        {
            ERROR("Unable to initialize process data\n");
            goto CLEANUP1;
        }

#if HAVE_MACH_EXCEPTIONS
        // Mach exception port needs to be set up before the thread
        // data or threads are set up.
        if (!SEHInitializeMachExceptions())
        {
            ERROR("SEHInitializeMachExceptions failed!\n");
            palError = ERROR_GEN_FAILURE;
            goto CLEANUP1;
        }
#endif // HAVE_MACH_EXCEPTIONS

        //
        // Initialize global thread data
        //

        palError = InitializeGlobalThreadData();
        if (NO_ERROR != palError)
        {
            ERROR("Unable to initialize thread data\n");
            goto CLEANUP1;
        }

        //
        // Allocate the initial thread data
        //

        palError = CreateThreadData(&pThread);
        if (NO_ERROR != palError)
        {
            ERROR("Unable to create initial thread data\n");
            goto CLEANUP1a;
        }

        PROCAddThread(pThread, pThread);

        //
        // Initialize mutex and condition variable used to synchronize the ending threads count
        //

        palError = InitializeEndingThreadsData();
        if (NO_ERROR != palError)
        {
            ERROR("Unable to create ending threads data\n");
            goto CLEANUP1b;
        }

        //
        // It's now safe to access our thread data
        //

        g_fThreadDataAvailable = TRUE;

        //
        // Initialize the object manager
        //

        pshmom = InternalNew<CSharedMemoryObjectManager>(pThread);
        if (NULL == pshmom)
        {
            ERROR("Unable to allocate new object manager\n");
            palError = ERROR_OUTOFMEMORY;
            goto CLEANUP1b;
        }

        palError = pshmom->Initialize();
        if (NO_ERROR != palError)
        {
            ERROR("object manager initialization failed!\n");
            InternalDelete(pThread, pshmom);
            goto CLEANUP1b;
        }

        g_pObjectManager = pshmom;

        //
        // Initialize the synchronization manager
        //
        g_pSynchronizationManager =
            CPalSynchMgrController::CreatePalSynchronizationManager(pThread);

        palError = ERROR_GEN_FAILURE;

        if (NULL == g_pSynchronizationManager)
        {
            ERROR("Failure creating synchronization manager\n");
            goto CLEANUP1c;
        }

        if (argc > 0 && argv != NULL)
        {
            /* build the command line */
            command_line = INIT_FormatCommandLine(pThread, argc, argv);
            if (NULL == command_line)
            {
                ERROR("Error building command line\n");
                goto CLEANUP1d;
            }

            /* find out the application's full path */
            exe_path = INIT_FindEXEPath(pThread, argv[0]);
            if (NULL == exe_path)
            {
                ERROR("Unable to find exe path\n");
                goto CLEANUP1e;
            }

            if (!WideCharToMultiByte(CP_ACP, 0, exe_path, -1, g_ExePath,
                sizeof(g_ExePath), NULL, NULL))
            {
                ERROR("Failed to store process executable path\n");
                goto CLEANUP2;
            }

            if (NULL == command_line || NULL == exe_path)
            {
                ERROR("Failed to process command-line parameters!\n");
                goto CLEANUP2;
            }

#ifdef PAL_PERF
            // Initialize the Profiling structure
            if(FALSE == PERFInitialize(command_line, exe_path)) 
            {
                ERROR("Performance profiling initial failed\n");
                goto done;
            }    
            PERFAllocThreadInfo();
#endif
        }

        //
        // Create the initial process and thread objects
        //

        palError = CreateInitialProcessAndThreadObjects(
            pThread,
            command_line,
            exe_path
            );
        
        if (NO_ERROR != palError)
        {
            ERROR("Unable to create initial process and thread objects\n");
            goto CLEANUP4;
        }
        // CreateInitialProcessAndThreadObjects took ownership of this memory.
        command_line = NULL;

        if (flags & PAL_INITIALIZE_SYNC_THREAD)
        {
            //
            // Tell the synchronization manager to start its worker thread
            //
            palError = CPalSynchMgrController::StartWorker(pThread);
            if (NO_ERROR != palError)
            {
                ERROR("Synch manager failed to start worker thread\n");
                goto CLEANUP5;
            }
        }

        palError = ERROR_GEN_FAILURE;

        /* initialize structured exception handling stuff (signals, etc) */
        if (FALSE == SEHInitialize(pThread, flags))
        {
            ERROR("Unable to initialize SEH support\n");
            goto CLEANUP5;
        }

        /* Initialize the File mapping critical section. */
        if (FALSE == MAPInitialize())
        {
            ERROR("Unable to initialize file mapping support\n");
            goto CLEANUP6;
        }

        /* initialize module manager */
        if (FALSE == LOADInitializeModules(exe_path))
        {
            ERROR("Unable to initialize module manager\n");
            palError = GetLastError();
            goto CLEANUP8;
        }
         
        /* Initialize the Virtual* functions. */
        if (FALSE == VIRTUALInitialize())
        {
            ERROR("Unable to initialize virtual memory support\n");
            goto CLEANUP10;
        }

        /* create file objects for standard handles */
        if(!FILEInitStdHandles())
        {
            ERROR("Unable to initialize standard file handles\n");
            goto CLEANUP13;
        }

        if (FALSE == CRTInitStdStreams())
        {
            ERROR("Unable to initialize CRT standard streams\n");
            goto CLEANUP15;
        }

        TRACE("First-time PAL initialization complete.\n");
        init_count++;        

        /* Set LastError to a non-good value - functions within the
           PAL startup may set lasterror to a nonzero value. */
        SetLastError(NO_ERROR);
        
        retval = 0;
    }
    else
    {
        init_count++;

        // Behave the same wrt entering the PAL independent of whether this
        // is the first call to PAL_Initialize or not.  The first call implied
        // PAL_Enter by virtue of creating the CPalThread for the current
        // thread, and its starting state is to be in the PAL.
        (void)PAL_Enter(PAL_BoundaryTop);

        TRACE("Initialization count increases to %d\n", init_count.Load());

        SetLastError(NO_ERROR);
        retval = 0;
    }
    goto done;

    /* No cleanup required for CRTInitStdStreams */ 
CLEANUP15:
    FILECleanupStdHandles();
CLEANUP13:
    VIRTUALCleanup();
CLEANUP10:
    LOADFreeModules(TRUE);
CLEANUP8:
    MAPCleanup();
CLEANUP6:
    SEHCleanup(flags);
CLEANUP5:
    PROCCleanupInitialProcess();
CLEANUP4:
    FMTMSG_FormatMessageCleanUp();
CLEANUP2:
    InternalFree(pThread, exe_path);
CLEANUP1e:
    if (command_line != NULL)
    {
        InternalFree(pThread, command_line);
    }
CLEANUP1d:
    // Cleanup synchronization manager
CLEANUP1c:
    // Cleanup object manager
CLEANUP1b:
    // Cleanup initial thread data
CLEANUP1a:
    // Cleanup global process data
CLEANUP1:
    SHMCleanup();
CLEANUP0:
#if !HAVE_COREFOUNDATION
    CODEPAGECleanup();
#endif // !HAVE_COREFOUNDATION
    ERROR("PAL_Initialize failed\n");
    SetLastError(palError);
done:
#ifdef PAL_PERF 
    if( retval == 0)
    {
         PERFEnableProcessProfile();
         PERFEnableThreadProfile(FALSE);
         PERFCalibrate("Overhead of PERF entry/exit");
    }
#endif

    InternalLeaveCriticalSection(pThread, init_critsec);

    if (fFirstTimeInit && 0 == retval)
    {
        _ASSERTE(NULL != pThread);
        _ASSERTE(pThread->suspensionInfo.IsSuspensionStateSafe());
    }

    if (retval != 0 && GetLastError() == ERROR_SUCCESS)
    {
        ASSERT("returning failure, but last error not set\n");
    }

#ifdef __APPLE__
exit :
#endif // __APPLE__
    LOGEXIT("PAL_Initialize returns int %d\n", retval);
    return retval;
}

/*++
Function:
  PAL_InitializeCoreCLR

Abstract:
  A replacement for PAL_Initialize when loading CoreCLR. Instead of taking a command line (which CoreCLR
  instances aren't given anyway) the path into which the CoreCLR is installed is supplied instead. This is
  cached so that PAL_GetPALDirectoryW can return it later.

  This routine also makes sure the psuedo dynamic libraries PALRT and mscorwks have their initialization
  methods called.

  Which PAL (if any) we're executing in the context of is a function of the return code and the fStayInPAL
  argument. If an error is returned then the PAL context is that of the caller (i.e. this call doesn't switch
  into the context of the PAL being initialized). Otherwise (on success) the context is remains in that of the
  new PAL if and only if fStayInPAL is TRUE.

Return:
  ERROR_SUCCESS if successful
  An error code, if it failed

--*/
PAL_ERROR
PALAPI
PAL_InitializeCoreCLR(
    const char *szExePath,
    const char *szCoreCLRPath,
    BOOL fStayInPAL)
{    
    // Check for a repeated call (this is a no-op).
    if (g_szCoreCLRPath[0] != '\0')
    {
        if (fStayInPAL)
        {
            PAL_Enter(PAL_BoundaryTop);
        }
        return ERROR_SUCCESS;
    }

    // Make sure it's an absolute path.
    if (szCoreCLRPath[0] != '/')
    {
        return ERROR_INVALID_PARAMETER;
    }
    
    // Check we can handle the length of the installation directory.
    size_t cchCoreCLRPath = strlen(szCoreCLRPath);
    if (cchCoreCLRPath >= sizeof(g_szCoreCLRPath))
    {
        ASSERT("CoreCLR installation path is too long");
        return ERROR_BAD_PATHNAME;
    }

    // Stash a copy of the CoreCLR installation path in a global variable.
    // Make sure it's terminated with a slash.
    if (strcpy_s(g_szCoreCLRPath, sizeof(g_szCoreCLRPath), szCoreCLRPath) != SAFECRT_SUCCESS)
    {
        ASSERT("strcpy_s failed!");
        return ERROR_FILENAME_EXCED_RANGE;
    }

#ifdef __APPLE__    // Fake up a command line to call PAL_Initialize with.
    const char *argv[] = { "CoreCLR" };
    int result = PAL_Initialize(1, argv);
#else // __APPLE__
    // Fake up a command line to call PAL_Initialize with.
    int result = PAL_Initialize(1, &szExePath);
#endif // __APPLE__
    if (result != 0)
        return GetLastError();

    // Now that the PAL is initialized it's safe to call the initialization methods for the code that used to
    // be dynamically loaded libraries but is now statically linked into CoreCLR just like the PAL, i.e. the
    // PAL RT and mscorwks.
    if (!LOADInitCoreCLRModules())
    {
        return ERROR_DLL_INIT_FAILED;
    }
    
    if (!fStayInPAL)
    {
        PAL_Leave(PAL_BoundaryTop);
    }
    return ERROR_SUCCESS;
}

/*++
Function:
PAL_IsDebuggerPresent

Abstract:
This function should be used to determine if a debugger is attached to the process.
--*/
PALIMPORT
BOOL
PALAPI
PAL_IsDebuggerPresent()
{
#if defined(__LINUX__)
    BOOL debugger_present = FALSE;
    char buf[2048];

    int status_fd = open("/proc/self/status", O_RDONLY);
    if (status_fd == -1)
    {
        return FALSE;
    }
    ssize_t num_read = read(status_fd, buf, sizeof(buf) - 1);

    if (num_read > 0)
    {
        static const char TracerPid[] = "TracerPid:";
        char *tracer_pid;

        buf[num_read] = '\0';
        tracer_pid = strstr(buf, TracerPid);
        if (tracer_pid)
        {
            debugger_present = !!atoi(tracer_pid + sizeof(TracerPid) - 1);
        }
    }

    return debugger_present;
#elif defined(__APPLE__)
    struct kinfo_proc info = {};
    size_t size = sizeof(info);
    int mib[4] = { CTL_KERN, KERN_PROC, KERN_PROC_PID, getpid() };
    int ret = sysctl(mib, sizeof(mib)/sizeof(*mib), &info, &size, NULL, 0);

    if (ret == 0)
        return ((info.kp_proc.p_flag & P_TRACED) != 0);

    return FALSE;
#else
    return FALSE;
#endif
}

/*++
Function:
  PAL_EntryPoint

Abstract:
  This function should be used to wrap code that uses PAL library on thread that was not created by PAL.
--*/
PALIMPORT
DWORD_PTR
PALAPI
PAL_EntryPoint(
    IN LPTHREAD_START_ROUTINE lpStartAddress,
    IN LPVOID lpParameter)
{
    CPalThread *pThread;
    DWORD_PTR retval = (DWORD) -1;

    ENTRY("PAL_EntryPoint(lpStartAddress=%p, lpParameter=%p)\n", lpStartAddress, lpParameter);

    pThread = InternalGetCurrentThread();
    if (NULL == pThread)
    {
        /* This function works only for thread that called PAL_Initialize for now. */
        ERROR( "Unable to get the thread object.\n" );
        goto done;
    }

    retval = (*lpStartAddress)(lpParameter);

done:
    LOGEXIT("PAL_EntryPoint returns int %d\n", retval);
    return retval;
}

/*++
Function:
  PALCommonCleanup

Utility function to free any resource used by the PAL. 

Parameters :
    step: selects the desired cleanup step
    full_cleanup:  FALSE: cleanup only what's needed and leave the rest 
                          to the OS process cleanup
                   TRUE:  full cleanup 
--*/
void 
PALCommonCleanup(PALCLEANUP_STEP step, BOOL full_cleanup)
{
    CPalThread *pThread = InternalGetCurrentThread();
    static int step_done[PALCLEANUP_STEP_INVALID] = { 0 };

    switch (step)
    {
    case PALCLEANUP_ALL_STEPS:
    case PALCLEANUP_STEP_ONE:
        /* Note: in order to work correctly, this step should be executed with 
           init_count > 0
         */
        if (!step_done[PALCLEANUP_STEP_ONE])
        {
            step_done[PALCLEANUP_STEP_ONE] = 1;

            PALSetShutdownIntent();

            //
            // Let the synchronization manager know we're about to shutdown
            //

            CPalSynchMgrController::PrepareForShutdown();

#ifdef _DEBUG
            PROCDumpThreadList();
#endif

            TRACE("About to suspend every other thread\n");

            /* prevent other threads from acquiring signaled objects */
            PROCCondemnOtherThreads();
            /* prevent other threads from using services we're shutting down */
            PROCSuspendOtherThreads();

            TRACE("Every other thread suspended until exit\n");
        }

        /* Fall down for PALCLEANUP_ALL_STEPS */
        if (PALCLEANUP_ALL_STEPS != step)
            break;

    case PALCLEANUP_STEP_TWO:
        if (!step_done[PALCLEANUP_STEP_TWO])
        {
            step_done[PALCLEANUP_STEP_TWO] = 1;

            /* LOADFreeeModules needs to be called before unitializing the rest
               of the PAL since it could result in calling DllMain for loaded
               libraries. For the user DllMain, all PAL APIs should still be
               functional. */
            LOADFreeModules(FALSE);

#ifdef PAL_PERF
            PERFDisableProcessProfile();
            PERFDisableThreadProfile(FALSE);
            PERFTerminate();  
#endif

            if (full_cleanup)
            {
                /* close primary handles of standard file objects */
                FILECleanupStdHandles();
                /* This unloads the palrt so, during its unloading, they
                   can call any number of APIs, so we have to be active for it to work. */
                FMTMSG_FormatMessageCleanUp();
                VIRTUALCleanup();
                /* SEH requires information from the process structure to work;
                   LOADFreeModules requires SEH to be functional when calling DllMain.
                   Therefore SEHCleanup must go between LOADFreeModules and
                   PROCCleanupInitialProcess */
                SEHCleanup(PAL_INITIALIZE_ALL);
                PROCCleanupInitialProcess();
            }

            // Object manager shutdown may cause all CPalThread objects
            // to be deleted. Since the CPalThread of the shutdown thread
            // needs to be available for reference by the thread suspension unsafe
            // operations, the reference of CPalThread is incremented here
            // to keep it alive until PAL finishes cleanup.
            pThread->AddThreadReference();

            //
            // Shutdown object manager -- this needs to happen before the
            // synch manager shutdown since it will call into the synch
            // manager to free object synch data
            // 
            static_cast<CSharedMemoryObjectManager*>(g_pObjectManager)->Shutdown(pThread);

            //
            // Final synch manager shutdown
            //
            CPalSynchMgrController::Shutdown(pThread, full_cleanup);

            if (full_cleanup)
            {
                /* It needs to be done after stopping the handle manager, because
                   the cleanup will delete the critical section which is used
                   when closing the handle of a file mapping */
                MAPCleanup();
                // MutexCleanup();

                MiscCleanup();

#if !HAVE_COREFOUNDATION
                CODEPAGECleanup();
#endif // !HAVE_COREFOUNDATION
                TLSCleanup();
            }

            // The thread object will no longer be available after the shutdown thread
            // releases the thread reference.
            g_fThreadDataAvailable = FALSE;
            pThread->ReleaseThreadReference();
            pthread_setspecific(thObjKey, NULL); // Make sure any TLS entry is removed.

            // Since thread object is no longer available here,
            // the code path from here should stop using any functions
            // that reference thread object.
            SHMCleanup();

            TRACE("PAL Terminated.\n");
        }
        break;

    default:
        ASSERT("Unknown final cleanup step %d", step);
        break;
    }
}

/*++
Function:
  PAL_Terminate

Abstract:
  This function is the called when a thread has finished using the PAL
  library. It shuts down PAL and exits the current process.
--*/
void
PALAPI
PAL_Terminate(
          void)
{
    PAL_TerminateEx(0);
}

/*++
Function:
PAL_TerminateEx

Abstract:
This function is the called when a thread has finished using the PAL
library. It shuts down PAL and exits the current process with
the specified exit code.
--*/
void
PALAPI
PAL_TerminateEx(int exitCode)
{
    ENTRY_EXTERNAL("PAL_TerminateEx()\n");

    if (NULL == init_critsec)
    {
        /* note that these macros probably won't output anything, since the
        debug channels haven't been initialized yet */
        ASSERT("PAL_Initialize has never been called!\n");
        LOGEXIT("PAL_Terminate returns.\n");
    }

    PALSetShutdownIntent();

    LOGEXIT("PAL_TerminateEx is exiting the current process.\n");
    exit(exitCode);
}

/*++
Function:
  PAL_InitializeDebug

Abstract:
  This function is the called when cordbg attaches to the process.
--*/
void
PALAPI
PAL_InitializeDebug(
          void)
{
    PERF_ENTRY(PAL_InitializeDebug);
    ENTRY("PAL_InitializeDebug()\n");
#if HAVE_MACH_EXCEPTIONS
    MachExceptionInitializeDebug();
#endif
    LOGEXIT("PAL_InitializeDebug returns\n");
    PERF_EXIT(PAL_InitializeDebug);
}

/*++
Function:
  PALIsThreadDataInitialized

Returns TRUE if startup has reached a point where thread data is available
--*/
BOOL
PALIsThreadDataInitialized()
{
    return g_fThreadDataAvailable;
}

/*++
Function:
  PALShutdown

  sets the PAL's initialization count to zero, so that PALIsInitialized will 
  return FALSE. called by PROCCleanupProcess to tell some functions that the
  PAL isn't fully functional, and that they should use an alternate code path
  
(no parameters, no retun vale)
--*/
void
PALShutdown(
          void)
{
    init_count = 0;
}

BOOL
PALIsShuttingDown()
{
    /* ROTORTODO: This function may be used to provide a reader/writer-like
       mechanism (or a ref counting one) to prevent PAL APIs that need to access 
       PAL runtime data, from working when PAL is shutting down. Each of those API 
       should acquire a read access while executing. The shutting down code would
       acquire a write lock, i.e. suspending any new incoming reader, and waiting 
       for the current readers to be done. That would allow us to get rid of the
       dangerous suspend-all-other-threads at shutdown time */
    return shutdown_intent;
}

void
PALSetShutdownIntent()
{
    /* ROTORTODO: See comment in PALIsShuttingDown */
    shutdown_intent = TRUE;
}

/*++
Function:
  PALInitLock

Take the initializaiton critical section (init_critsec). necessary to serialize 
TerminateProcess along with PAL_Terminate and PAL_Initialize

(no parameters)

Return value :
    TRUE if critical section existed (and was acquired)
    FALSE if critical section doens't exist yet
--*/
BOOL PALInitLock(void)
{
    if(!init_critsec)
    {
        return FALSE;
    }
    
    CPalThread * pThread = 
        (PALIsThreadDataInitialized() ? InternalGetCurrentThread() : NULL);
    
    InternalEnterCriticalSection(pThread, init_critsec);
    return TRUE;
}

/*++
Function:
  PALInitUnlock

Release the initialization critical section (init_critsec). 

(no parameters, no return value)
--*/
void PALInitUnlock(void)
{
    if(!init_critsec)
    {
        return;
    }

    CPalThread * pThread = 
        (PALIsThreadDataInitialized() ? InternalGetCurrentThread() : NULL);

    InternalLeaveCriticalSection(pThread, init_critsec);
}

/* Internal functions *********************************************************/

/*++
Function:
    INIT_IncreaseDescriptorLimit [internal]

Abstract:
    Calls setrlimit(2) to increase the maximum number of file descriptors
    this process can open.

Return value:
    TRUE if the call to setrlimit succeeded; FALSE otherwise.
--*/
static BOOL INIT_IncreaseDescriptorLimit(void)
{
    struct rlimit rlp;
    int result;
    
    result = getrlimit(RLIMIT_NOFILE, &rlp);
    if (result != 0)
    {
        return FALSE;
    }
    // Set our soft limit for file descriptors to be the same
    // as the max limit.
    rlp.rlim_cur = rlp.rlim_max;
    result = setrlimit(RLIMIT_NOFILE, &rlp);
    if (result != 0)
    {
        return FALSE;
    }

    return TRUE;
}

/*++
Function:
    INIT_FormatCommandLine [Internal]

Abstract:
    This function converts an array of arguments (argv) into a Unicode
    command-line for use by GetCommandLineW

Parameters :
    int argc : number of arguments in argv
    char **argv : argument list in an array of NULL-terminated strings

Return value :
    pointer to Unicode command line. This is a buffer allocated with malloc;
    caller is responsible for freeing it with free()

Note : not all peculiarities of Windows command-line processing are supported; 

-what is supported :
    -arguments with white-space must be double quoted (we'll just double-quote
     all arguments to simplify things)
    -some characters must be escaped with \ : particularly, the double-quote,
     to avoid confusion with the double-quotes at the start and end of
     arguments, and \ itself, to avoid confusion with escape sequences.
-what is not supported:    
    -under Windows, \\ is interpreted as an escaped \ ONLY if it's followed by
     an escaped double-quote \". \\\" is passed to argv as \", but \\a is
     passed to argv as \\a... there may be other similar cases
    -there may be other characters which must be escaped 
--*/
static LPWSTR INIT_FormatCommandLine (CPalThread *pThread, int argc, const char * const *argv)
{
    LPWSTR retval;
    LPSTR command_line=NULL, command_ptr;
    LPCSTR arg_ptr;
    INT length, i,j;
    BOOL bQuoted = FALSE;

    /* list of characters that need no be escaped with \ when building the
       command line. currently " and \ */
    LPCSTR ESCAPE_CHARS="\"\\";

    /* allocate temporary memory for the string. Play it safe :
       double the length of each argument (in case they're composed
       exclusively of escaped characters), and add 3 (for the double-quotes
       and separating space). This is temporary anyway, we return a LPWSTR */
    length=0;
    for(i=0; i<argc; i++)
    {
        TRACE("argument %d is %s\n", i, argv[i]);
        length+=3;
        length+=strlen(argv[i])*2;
    }
    command_line = reinterpret_cast<LPSTR>(InternalMalloc(pThread, length));

    if(!command_line)
    {
        ERROR("couldn't allocate memory for command line!\n");
        return NULL;
    }

    command_ptr=command_line;
    for(i=0; i<argc; i++)
    {
        /* double-quote at beginning of argument containing at leat one space */
        for(j = 0; (argv[i][j] != 0) && (!isspace((unsigned char) argv[i][j])); j++);

        if (argv[i][j] != 0)
        {
            *command_ptr++='"';
            bQuoted = TRUE;
        }
        /* process the argument one character at a time */
        for(arg_ptr=argv[i]; *arg_ptr; arg_ptr++)
        {
            /* if character needs to be escaped, prepend a \ to it. */
            if( strchr(ESCAPE_CHARS,*arg_ptr))
            {
                *command_ptr++='\\';
            }

            /* now we can copy the actual character over. */
            *command_ptr++=*arg_ptr;
        }
        /* double-quote at end of argument; space to separate arguments */
        if (bQuoted == TRUE)
        {
            *command_ptr++='"';
            bQuoted = FALSE;
        }
        *command_ptr++=' ';
    }
    /* replace the last space with a NULL terminator */
    command_ptr--;
    *command_ptr='\0';

    /* convert to Unicode */
    i = MultiByteToWideChar(CP_ACP, 0,command_line, -1, NULL, 0);
    if (i == 0)
    {
        ASSERT("MultiByteToWideChar failure\n");
        InternalFree(pThread, command_line);
        return NULL;
    }

    retval = reinterpret_cast<LPWSTR>(InternalMalloc(pThread, (sizeof(WCHAR)*i)));
    if(retval == NULL)
    {
        ERROR("can't allocate memory for Unicode command line!\n");
        InternalFree(pThread, command_line);
        return NULL;
    }

    if(!MultiByteToWideChar(CP_ACP, 0,command_line, i, retval, i))
    {
        ASSERT("MultiByteToWideChar failure\n");
        InternalFree(pThread, retval);
        retval = NULL;
    }
    else
        TRACE("Command line is %s\n", command_line);

    InternalFree(pThread, command_line);
    return retval;
}

/*++
Function:
  INIT_FindEXEPath

Abstract:
    Determine the full, canonical path of the current executable by searching
    $PATH.

Parameters:
    LPCSTR exe_name : file to search for

Return:
    pointer to buffer containing the full path. This buffer must be released
    by the caller using free()

Notes :
    this function assumes that "exe_name" is in Unix style (no \)

Notes 2:
    This doesn't handle the case of directories with the desired name
    (and directories are usually executable...)
--*/
static LPWSTR INIT_FindEXEPath(CPalThread *pThread, LPCSTR exe_name)
{
#ifndef __APPLE__
    CHAR real_path[PATH_MAX+1];
    LPSTR env_path;
    LPSTR path_ptr;
    LPSTR cur_dir;
    INT exe_name_length;
    BOOL need_slash;
    LPWSTR return_value;
    INT return_size;
    struct stat theStats;

    /* if a path is specified, only search there */
    if(strchr(exe_name, '/'))
    {
        if ( -1 == stat( exe_name, &theStats ) )
        {
            ERROR( "The file does not exist\n" );
            return NULL;
        }

        if ( UTIL_IsExecuteBitsSet( &theStats ) )
        {
            if(!realpath(exe_name, real_path))
            {
                ERROR("realpath() failed!\n");
                return NULL;
            }

            return_size=MultiByteToWideChar(CP_ACP,0,real_path,-1,NULL,0);
            if ( 0 == return_size )
            {
                ASSERT("MultiByteToWideChar failure\n");
                return NULL;
            }

            return_value = reinterpret_cast<LPWSTR>(InternalMalloc(pThread, (return_size*sizeof(WCHAR))));
            if ( NULL == return_value )
            {
                ERROR("Not enough memory to create full path\n");
                return NULL;
            }
            else
            {
                if(!MultiByteToWideChar(CP_ACP, 0, real_path, -1, 
                                        return_value, return_size))
                {
                    ASSERT("MultiByteToWideChar failure\n");
                    InternalFree(pThread, return_value);
                    return_value = NULL;
                }
                else
                {
                    TRACE("full path to executable is %s\n", real_path);
                }
            }
            return return_value;
        }
    }

    /* no path was specified : search $PATH */

    env_path=MiscGetenv("PATH");
    if(!env_path || *env_path=='\0')
    {
        WARN("$PATH isn't set.\n");
        goto last_resort;
    }

    /* get our own copy of env_path so we can modify it */
    env_path=InternalStrdup(pThread, env_path);
    if(!env_path)
    {
        ERROR("Not enough memory to copy $PATH!\n");
        return NULL;
    }

    exe_name_length=strlen(exe_name);

    cur_dir=env_path;

    while(cur_dir)
    {
        LPSTR full_path;
        struct stat theStats;

        /* skip all leading ':' */
        while(*cur_dir==':')
        {
            cur_dir++;
        }
        if(*cur_dir=='\0')
        {
            break;
        }

        /* cut string at next ':' */
        path_ptr=strchr(cur_dir, ':');
        if(path_ptr)
        {
            /* check if we need to add a '/' between the path and filename */
            need_slash=(*(path_ptr-1))!='/';

            /* NULL_terminate path element */
            *path_ptr++='\0';
        }
        else
        {
            /* check if we need to add a '/' between the path and filename */
            need_slash=(cur_dir[strlen(cur_dir)-1])!='/';
        }

        TRACE("looking for %s in %s\n", exe_name, cur_dir);

        /* build tentative full file name */
        int iLength = (strlen(cur_dir)+exe_name_length+2);
        full_path = reinterpret_cast<LPSTR>(InternalMalloc(pThread, iLength));
        if(!full_path)
        {
            ERROR("Not enough memory!\n");
            break;
        }
        
        if (strcpy_s(full_path, iLength, cur_dir) != SAFECRT_SUCCESS)
        {
            ERROR("strcpy_s failed!\n");
            InternalFree(pThread, full_path);
            InternalFree(pThread, env_path);
            return NULL;
        }

        if(need_slash)
        {
            if (strcat_s(full_path, iLength, "/") != SAFECRT_SUCCESS)
            {
                ERROR("strcat_s failed!\n");
                InternalFree(pThread, full_path);
                InternalFree(pThread, env_path);
                return NULL;
            }
        }

        if (strcat_s(full_path, iLength, exe_name) != SAFECRT_SUCCESS)
        {
            ERROR("strcat_s failed!\n");
            InternalFree(pThread, full_path);
            InternalFree(pThread, env_path);
            return NULL;
        }

        /* see if file exists AND is executable */
        if ( -1 != stat( full_path, &theStats ) )
        {
            if( UTIL_IsExecuteBitsSet( &theStats ) )
            {
                /* generate canonical path */
                if(!realpath(full_path, real_path))
                {
                    ERROR("realpath() failed!\n");
                    InternalFree(pThread, full_path);
                    InternalFree(pThread, env_path);
                    return NULL;
                }
                InternalFree(pThread, full_path);
    
                return_size = MultiByteToWideChar(CP_ACP,0,real_path,-1,NULL,0);
                if ( 0 == return_size )
                {
                    ASSERT("MultiByteToWideChar failure\n");
                    InternalFree(pThread, env_path);
                    return NULL;
                }

                return_value = reinterpret_cast<LPWSTR>(InternalMalloc(pThread, (return_size*sizeof(WCHAR))));
                if ( NULL == return_value )
                {
                    ERROR("Not enough memory to create full path\n");
                    InternalFree(pThread, env_path);
                    return NULL;
                }

                if(!MultiByteToWideChar(CP_ACP, 0, real_path, -1, return_value,
                                    return_size))
                {
                    ASSERT("MultiByteToWideChar failure\n");
                    InternalFree(pThread, return_value);
                    return_value = NULL;
                }
                else
                {
                    TRACE("found %s in %s; real path is %s\n", exe_name,
                          cur_dir,real_path);
                }
                InternalFree(pThread, env_path);
                return return_value;
            }
        }
        /* file doesn't exist : keep searching */
        InternalFree(pThread, full_path);

        /* path_ptr is NULL if there's no ':' after this directory */
        cur_dir=path_ptr;
    }
    InternalFree(pThread, env_path);
    TRACE("No %s found in $PATH (%s)\n", exe_name, MiscGetenv("PATH"));

last_resort:
    /* last resort : see if the executable is in the current directory. This is
       possible if it comes from a exec*() call. */
    if(0 == stat(exe_name,&theStats))
    {
        if ( UTIL_IsExecuteBitsSet( &theStats ) )
        {
            if(!realpath(exe_name, real_path))
            {
                ERROR("realpath() failed!\n");
                return NULL;
            }

            return_size = MultiByteToWideChar(CP_ACP,0,real_path,-1,NULL,0);
            if (0 == return_size)
            {
                ASSERT("MultiByteToWideChar failure\n");
                return NULL;
            }

            return_value = reinterpret_cast<LPWSTR>(InternalMalloc(pThread, (return_size*sizeof(WCHAR))));
            if (NULL == return_value)
            {
                ERROR("Not enough memory to create full path\n");
                return NULL;
            }
            else
            {
                if(!MultiByteToWideChar(CP_ACP, 0, real_path, -1, 
                                        return_value, return_size))
                {
                    ASSERT("MultiByteToWideChar failure\n");
                    InternalFree(pThread, return_value);
                    return_value = NULL;
                }
                else
                {
                    TRACE("full path to executable is %s\n", real_path);
                }
            }
            return return_value;
        }
        else
        {
            ERROR("found %s in current directory, but it isn't executable!\n",
                  exe_name);
        }                                                                   
    }
    else
    {
        TRACE("last resort failed : executable %s is not in the current "
              "directory\n",exe_name);
    }
    ERROR("executable %s not found anywhere!\n", exe_name);
    return NULL;
#else // !__APPLE__
    // On the Mac we can just directly ask the OS for the executable path.

    CHAR exec_path[PATH_MAX+1];
    LPWSTR return_value;
    INT return_size;

    uint32_t bufsize = sizeof(exec_path);
    if (_NSGetExecutablePath(exec_path, &bufsize))
    {
        ASSERT("_NSGetExecutablePath failure\n");
        return NULL;
    }

    return_size = MultiByteToWideChar(CP_ACP,0,exec_path,-1,NULL,0);
    if (0 == return_size)
    {
        ASSERT("MultiByteToWideChar failure\n");
        return NULL;
    }

    return_value = reinterpret_cast<LPWSTR>(InternalMalloc(pThread, (return_size*sizeof(WCHAR))));
    if (NULL == return_value)
    {
        ERROR("Not enough memory to create full path\n");
        return NULL;
    }
    else
    {
        if(!MultiByteToWideChar(CP_ACP, 0, exec_path, -1, 
                                return_value, return_size))
        {
            ASSERT("MultiByteToWideChar failure\n");
            InternalFree(pThread, return_value);
            return_value = NULL;
        }
        else
        {
            TRACE("full path to executable is %s\n", exec_path);
        }
    }

    return return_value;
#endif // !__APPLE__
}
