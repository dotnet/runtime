// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
#include "pal/environ.h"
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
Volatile<LONG> g_coreclrInitialized = 0;
static BOOL g_fThreadDataAvailable = FALSE;
static pthread_mutex_t init_critsec_mutex = PTHREAD_MUTEX_INITIALIZER;

/* critical section to protect access to init_count. This is allocated on the
   very first PAL_Initialize call, and is freed afterward. */
static PCRITICAL_SECTION init_critsec = NULL;

static int Initialize(int argc, const char *const argv[], DWORD flags);
static BOOL INIT_IncreaseDescriptorLimit(void);
static LPWSTR INIT_FormatCommandLine (int argc, const char * const *argv);
static LPWSTR INIT_FindEXEPath(LPCSTR exe_name);

#ifdef _DEBUG
extern void PROCDumpThreadList(void);
#endif

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
    return Initialize(argc, argv, PAL_INITIALIZE);
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

    if (init_count == 0)
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
        if (FALSE == EnvironInitialize())
        {
            goto CLEANUP0;
        }

        // Initialize debug channel settings before anything else.
        // This depends on the environment, so it must come after
        // EnvironInitialize.
        if (FALSE == DBG_init_channels())
        {
            goto CLEANUP0;
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


        /* initialize the shared memory infrastructure */
        if (!SHMInitialize())
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
        // Initialize module manager
        //
        if (FALSE == LOADInitializeModules())
        {
            ERROR("Unable to initialize module manager\n");
            palError = ERROR_INTERNAL_ERROR;
            goto CLEANUP1b;
        }

        //
        // Initialize the object manager
        //

        pshmom = InternalNew<CSharedMemoryObjectManager>();
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
            InternalDelete(pshmom);
            goto CLEANUP1b;
        }

        g_pObjectManager = pshmom;

        //
        // Initialize the synchronization manager
        //
        g_pSynchronizationManager =
            CPalSynchMgrController::CreatePalSynchronizationManager();

        if (NULL == g_pSynchronizationManager)
        {
            palError = ERROR_NOT_ENOUGH_MEMORY;
            ERROR("Failure creating synchronization manager\n");
            goto CLEANUP1c;
        }
    }
    else
    {
        pThread = InternalGetCurrentThread();
    }

    palError = ERROR_GEN_FAILURE;

    if (argc > 0 && argv != NULL)
    {
        /* build the command line */
        command_line = INIT_FormatCommandLine(argc, argv);
        if (NULL == command_line)
        {
            ERROR("Error building command line\n");
            goto CLEANUP1d;
        }

        /* find out the application's full path */
        exe_path = INIT_FindEXEPath(argv[0]);
        if (NULL == exe_path)
        {
            ERROR("Unable to find exe path\n");
            goto CLEANUP1e;
        }

        if (NULL == command_line || NULL == exe_path)
        {
            ERROR("Failed to process command-line parameters!\n");
            goto CLEANUP2;
        }

        palError = InitializeProcessCommandLine(
            command_line,
            exe_path);
        
        if (NO_ERROR != palError)
        {
            ERROR("Unable to initialize command line\n");
            goto CLEANUP2;
        }

        // InitializeProcessCommandLine took ownership of this memory.
        command_line = NULL;

#ifdef PAL_PERF
        // Initialize the Profiling structure
        if(FALSE == PERFInitialize(command_line, exe_path)) 
        {
            ERROR("Performance profiling initial failed\n");
            goto CLEANUP2;
        }    
        PERFAllocThreadInfo();
#endif

        if (!LOADSetExeName(exe_path))
        {
            ERROR("Unable to set exe name\n");
            goto CLEANUP2;
        }

        // LOADSetExeName took ownership of this memory.
        exe_path = NULL;
    }

    if (init_count == 0)
    {
        //
        // Create the initial process and thread objects
        //
        palError = CreateInitialProcessAndThreadObjects(pThread);
        if (NO_ERROR != palError)
        {
            ERROR("Unable to create initial process and thread objects\n");
            goto CLEANUP2;
        }

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

        if (FALSE == TIMEInitialize())
        {
            ERROR("Unable to initialize TIME support\n");
            goto CLEANUP6;
        }

        /* Initialize the File mapping critical section. */
        if (FALSE == MAPInitialize())
        {
            ERROR("Unable to initialize file mapping support\n");
            goto CLEANUP6;
        }

        /* Initialize the Virtual* functions. */
        bool initializeExecutableMemoryAllocator = (flags & PAL_INITIALIZE_EXEC_ALLOCATOR) != 0;
        if (FALSE == VIRTUALInitialize(initializeExecutableMemoryAllocator))
        {
            ERROR("Unable to initialize virtual memory support\n");
            goto CLEANUP10;
        }

        if (flags & PAL_INITIALIZE_STD_HANDLES)
        {
            /* create file objects for standard handles */
            if (!FILEInitStdHandles())
            {
                ERROR("Unable to initialize standard file handles\n");
                goto CLEANUP13;
            }
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
    MAPCleanup();
CLEANUP6:
    SEHCleanup();
CLEANUP5:
    PROCCleanupInitialProcess();
CLEANUP2:
    InternalFree(exe_path);
CLEANUP1e:
    InternalFree(command_line);
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
    TLSCleanup();
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

Return:
  ERROR_SUCCESS if successful
  An error code, if it failed

--*/
PAL_ERROR
PALAPI
PAL_InitializeCoreCLR(const char *szExePath)
{    
    // Fake up a command line to call PAL initialization with.
    int result = Initialize(1, &szExePath, PAL_INITIALIZE_CORECLR);
    if (result != 0)
    {
        return GetLastError();
    }

    // Check for a repeated call (this is a no-op).
    if (InterlockedIncrement(&g_coreclrInitialized) > 1)
    {
        PAL_Enter(PAL_BoundaryTop);
        return ERROR_SUCCESS;
    }

    // Now that the PAL is initialized it's safe to call the initialization methods for the code that used to
    // be dynamically loaded libraries but is now statically linked into CoreCLR just like the PAL, i.e. the
    // PAL RT and mscorwks.
    if (!LOADInitializeCoreCLRModule())
    {
        return ERROR_DLL_INIT_FAILED;
    }

    if (!InitializeFlushProcessWriteBuffers())
    {
        return ERROR_GEN_FAILURE;
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
#if defined(__linux__)
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
  PAL_Shutdown

Abstract:
  This function shuts down the PAL WITHOUT exiting the current process.
--*/
void
PALAPI
PAL_Shutdown(
    void)
{
    TerminateCurrentProcessNoExit(FALSE /* bTerminateUnconditionally */);
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
PAL_TerminateEx(
    int exitCode)
{
    ENTRY_EXTERNAL("PAL_TerminateEx()\n");

    if (NULL == init_critsec)
    {
        /* note that these macros probably won't output anything, since the
        debug channels haven't been initialized yet */
        ASSERT("PAL_Initialize has never been called!\n");
        LOGEXIT("PAL_Terminate returns.\n");
    }

    // Declare the beginning of shutdown 
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
BOOL PALIsThreadDataInitialized()
{
    return g_fThreadDataAvailable;
}

/*++
Function:
  PALCommonCleanup

  Utility function to prepare for shutdown.

--*/
void 
PALCommonCleanup()
{
    static bool cleanupDone = false;

    // Declare the beginning of shutdown
    PALSetShutdownIntent();

    if (!cleanupDone)
    {
        cleanupDone = true;

        //
        // Let the synchronization manager know we're about to shutdown
        //
        CPalSynchMgrController::PrepareForShutdown();

#ifdef _DEBUG
        PROCDumpThreadList();
#endif
    }

    // Mark that the PAL is uninitialized
    init_count = 0;
}

BOOL PALIsShuttingDown()
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

void PALSetShutdownIntent()
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
static LPWSTR INIT_FormatCommandLine (int argc, const char * const *argv)
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
    command_line = reinterpret_cast<LPSTR>(InternalMalloc(length));

    if(!command_line)
    {
        ERROR("couldn't allocate memory for command line!\n");
        return NULL;
    }

    command_ptr=command_line;
    for(i=0; i<argc; i++)
    {
        /* double-quote at beginning of argument containing at least one space */
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
        InternalFree(command_line);
        return NULL;
    }

    retval = reinterpret_cast<LPWSTR>(InternalMalloc((sizeof(WCHAR)*i)));
    if(retval == NULL)
    {
        ERROR("can't allocate memory for Unicode command line!\n");
        InternalFree(command_line);
        return NULL;
    }

    if(!MultiByteToWideChar(CP_ACP, 0,command_line, i, retval, i))
    {
        ASSERT("MultiByteToWideChar failure\n");
        InternalFree(retval);
        retval = NULL;
    }
    else
        TRACE("Command line is %s\n", command_line);

    InternalFree(command_line);
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
static LPWSTR INIT_FindEXEPath(LPCSTR exe_name)
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
    if (strchr(exe_name, '/'))
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

            return_value = reinterpret_cast<LPWSTR>(InternalMalloc((return_size*sizeof(WCHAR))));
            if ( NULL == return_value )
            {
                ERROR("Not enough memory to create full path\n");
                return NULL;
            }
            else
            {
                if (!MultiByteToWideChar(CP_ACP, 0, real_path, -1, 
                                        return_value, return_size))
                {
                    ASSERT("MultiByteToWideChar failure\n");
                    InternalFree(return_value);
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

    env_path = EnvironGetenv("PATH");
    if (!env_path || *env_path=='\0')
    {
        WARN("$PATH isn't set.\n");
        if (env_path != NULL)
        {
            InternalFree(env_path);
        }

        goto last_resort;
    }

    exe_name_length=strlen(exe_name);

    cur_dir=env_path;

    while (cur_dir)
    {
        LPSTR full_path;
        struct stat theStats;

        /* skip all leading ':' */
        while (*cur_dir==':')
        {
            cur_dir++;
        }
        if (*cur_dir=='\0')
        {
            break;
        }

        /* cut string at next ':' */
        path_ptr = strchr(cur_dir, ':');
        if (path_ptr)
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
        full_path = reinterpret_cast<LPSTR>(InternalMalloc(iLength));
        if (!full_path)
        {
            ERROR("Not enough memory!\n");
            break;
        }
        
        if (strcpy_s(full_path, iLength, cur_dir) != SAFECRT_SUCCESS)
        {
            ERROR("strcpy_s failed!\n");
            InternalFree(full_path);
            InternalFree(env_path);
            return NULL;
        }

        if (need_slash)
        {
            if (strcat_s(full_path, iLength, "/") != SAFECRT_SUCCESS)
            {
                ERROR("strcat_s failed!\n");
                InternalFree(full_path);
                InternalFree(env_path);
                return NULL;
            }
        }

        if (strcat_s(full_path, iLength, exe_name) != SAFECRT_SUCCESS)
        {
            ERROR("strcat_s failed!\n");
            InternalFree(full_path);
            InternalFree(env_path);
            return NULL;
        }

        /* see if file exists AND is executable */
        if ( -1 != stat( full_path, &theStats ) )
        {
            if( UTIL_IsExecuteBitsSet( &theStats ) )
            {
                /* generate canonical path */
                if (!realpath(full_path, real_path))
                {
                    ERROR("realpath() failed!\n");
                    InternalFree(full_path);
                    InternalFree(env_path);
                    return NULL;
                }
                InternalFree(full_path);

                return_size = MultiByteToWideChar(CP_ACP,0,real_path,-1,NULL,0);
                if ( 0 == return_size )
                {
                    ASSERT("MultiByteToWideChar failure\n");
                    InternalFree(env_path);
                    return NULL;
                }

                return_value = reinterpret_cast<LPWSTR>(InternalMalloc((return_size*sizeof(WCHAR))));
                if ( NULL == return_value )
                {
                    ERROR("Not enough memory to create full path\n");
                    InternalFree(env_path);
                    return NULL;
                }

                if (!MultiByteToWideChar(CP_ACP, 0, real_path, -1, return_value,
                                    return_size))
                {
                    ASSERT("MultiByteToWideChar failure\n");
                    InternalFree(return_value);
                    return_value = NULL;
                }
                else
                {
                    TRACE("found %s in %s; real path is %s\n", exe_name,
                          cur_dir,real_path);
                }

                InternalFree(env_path);
                return return_value;
            }
        }

        /* file doesn't exist : keep searching */
        InternalFree(full_path);

        /* path_ptr is NULL if there's no ':' after this directory */
        cur_dir=path_ptr;
    }

    InternalFree(env_path);
    TRACE("No %s found in $PATH (%s)\n", exe_name, EnvironGetenv("PATH", FALSE));

last_resort:
    /* last resort : see if the executable is in the current directory. This is
       possible if it comes from a exec*() call. */
    if (0 == stat(exe_name,&theStats))
    {
        if ( UTIL_IsExecuteBitsSet( &theStats ) )
        {
            if (!realpath(exe_name, real_path))
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

            return_value = reinterpret_cast<LPWSTR>(InternalMalloc((return_size*sizeof(WCHAR))));
            if (NULL == return_value)
            {
                ERROR("Not enough memory to create full path\n");
                return NULL;
            }
            else
            {
                if (!MultiByteToWideChar(CP_ACP, 0, real_path, -1, 
                                        return_value, return_size))
                {
                    ASSERT("MultiByteToWideChar failure\n");
                    InternalFree(return_value);
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

    return_value = reinterpret_cast<LPWSTR>(InternalMalloc((return_size*sizeof(WCHAR))));
    if (NULL == return_value)
    {
        ERROR("Not enough memory to create full path\n");
        return NULL;
    }
    else
    {
        if (!MultiByteToWideChar(CP_ACP, 0, exec_path, -1, 
                                return_value, return_size))
        {
            ASSERT("MultiByteToWideChar failure\n");
            InternalFree(return_value);
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
