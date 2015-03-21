//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

/*++

Module Name:

    corefx.cpp

Abstract:

    Implementation of PAL APIs meant to be consumed by CoreFX libraries

--*/

#include "pal/palinternal.h"
#include "pal/dbgmsg.h"
#include "pal/module.h"
#include <pal_corefx.h>

#include <sys/types.h>
#include <sys/stat.h>
#include <errno.h>
#include <unistd.h>
#include <dlfcn.h>

#ifdef __APPLE__
#include <mach-o/dyld.h>
#endif // __APPLE__

SET_DEFAULT_DEBUG_CHANNEL(MISC);

/*++
Function:
  EnsureOpenSslInitialized

  Used by cryptographic libraries in CoreFX to initialize
  threading support in OpenSSL.

  --*/

static const char * const libcryptoName = "libcrypto" PAL_SHLIB_SUFFIX;

static void* g_OpenSslLib;
static pthread_mutex_t g_OpenSslInitLock = PTHREAD_MUTEX_INITIALIZER;
static pthread_mutex_t *g_OpenSslLocks;

#define CRYPTO_LOCK 1
typedef void(*locking_function)(int mode, int n, char* file, int line);
typedef int(*CRYPTO_num_locks)(void);
typedef void(*CRYPTO_set_locking_callback)(locking_function callback);

static void LockingCallback(int mode, int n, char* file, int line)
{
    int result;
    if (mode & CRYPTO_LOCK)
    {
        result = pthread_mutex_lock(&g_OpenSslLocks[n]);
    }
    else
    {
        result = pthread_mutex_unlock(&g_OpenSslLocks[n]);
    }

    if (result != 0)
    {
        ASSERT("LockingCallback(%d, %d, %s, %d) failed with error %d \n",
            mode, n, file, line, result);
    }
}

int
PALAPI
EnsureOpenSslInitialized()
{
    int ret = 0;
    int numLocks;
    CRYPTO_num_locks numLocksFunc;
    CRYPTO_set_locking_callback setCallbackFunc;
    int locksInitialized = 0;

    PERF_ENTRY(EnsureOpenSslInitialized);
    ENTRY("EnsureOpenSslInitialized()\n");

    pthread_mutex_lock(&g_OpenSslInitLock);

    if (g_OpenSslLocks != NULL)
    {
        // Already initialized; nothing more to do.
        goto done;
    }

    // Open the libcrypto library
    g_OpenSslLib = dlopen(libcryptoName, RTLD_NOW);
    if (g_OpenSslLib == NULL)
    {
        // CoreCLR does not require libcrypto as a dependency,
        // even though various libraries might.
        ret = 1;
        goto done;
    }

    // Get the functions we need from OpenSSL
    numLocksFunc = (CRYPTO_num_locks) dlsym(g_OpenSslLib, "CRYPTO_num_locks");
    setCallbackFunc = (CRYPTO_set_locking_callback) dlsym(g_OpenSslLib, "CRYPTO_set_locking_callback");
    if (numLocksFunc == NULL || setCallbackFunc == NULL)
    {
        ASSERT("Unable to find CRYPTO_num_locks or CRYPTO_set_locking_callback\n");
        ret = 2;
        goto done;
    }

    // Determine how many locks are needed
    numLocks = numLocksFunc();
    if (numLocks <= 0)
    {
        ASSERT("CRYPTO_num_locks returned invalid value: %d\n", numLocks);
        ret = 3;
        goto done;
    }

    // Create the locks array
    g_OpenSslLocks = (pthread_mutex_t*) PAL_malloc(sizeof(pthread_mutex_t) * numLocks);
    if (g_OpenSslLocks == NULL)
    {
        ASSERT("PAL_malloc failed\n");
        ret = 4;
        goto done;
    }

    // Initialize each of the locks
    for (locksInitialized = 0; locksInitialized < numLocks; locksInitialized++)
    {
        if (pthread_mutex_init(&g_OpenSslLocks[locksInitialized], NULL) != 0)
        {
            ASSERT("pthread_mutex_init failed\n");
            ret = 5;
            goto done;
        }
    }

    // Initialize the callback
    setCallbackFunc((locking_function) LockingCallback);

done:
    if (ret != 0)
    {
        // Cleanup on failure

        if (g_OpenSslLocks != NULL)
        {
            for (int i = locksInitialized - 1; i >= 0; i--)
            {
                if (pthread_mutex_destroy(&g_OpenSslLocks[i]) != 0)
                {
                    ASSERT("Unable to pthread_mutex_destroy while cleaning up\n");
                }
            }
            PAL_free(g_OpenSslLocks);
            g_OpenSslLocks = NULL;
        }

        if (g_OpenSslLib != NULL)
        {
            if (dlclose(g_OpenSslLib) != 0)
            {
                ASSERT("Unable to close OpenSSL with dlerror \"%s\" \n", dlerror());
            }
            g_OpenSslLib = NULL;
        }
    }

    pthread_mutex_unlock(&g_OpenSslInitLock);

    // If successful, keep OpenSSL library open and initialized

    LOGEXIT("EnsureOpenSslInitialized returns %u\n", ret);
    PERF_EXIT(EnsureOpenSslInitialized);
    return ret;
}

/*++
Function:
ForkAndExecProcess

Used by System.Diagnostics.Process.Start to fork/exec a new process.

This function takes the place of directly using fork and execve from managed code,
in order to avoid executing managed code in the child process in the window between
fork and execve, which is not safe.

As would have been the case with fork/execve, a return value of 0 is success and -1
is failure; if failure, error information is provided in errno.

--*/

#define READ_END_OF_PIPE  0
#define WRITE_END_OF_PIPE 1

static void closeIfOpen(int fd)
{
    if (fd >= 0)
        close(fd);
}

int
PALAPI
ForkAndExecProcess(
           const char* filename, // filename argument to execve
           char* const argv[],   // argv argument to execve
           char* const envp[],   // envp argument to execve
           const char* cwd,      // path passed to chdir in child process
           int redirectStdin,    // whether to redirect standard input from the parent
           int redirectStdout,   // whether to redirect standard output to the parent
           int redirectStderr,   // whether to redirect standard error to the parent
           int* childPid,        // the child process' id
           int* stdinFd,         // if bRedirectStdin, the parent's fd for the child's stdin
           int* stdoutFd,        // if bRedirectStdout, the parent's fd for the child's stdout
           int* stderrFd)        // if bRedirectStderr, the parent's fd for the child's stderr
{
    int success = TRUE;
    int stdinFds[2] = { -1, -1 }, stdoutFds[2] = { -1, -1 }, stderrFds[2] = { -1, -1 };
    int processId = -1;

    PERF_ENTRY(ForkAndExecProcess);
    ENTRY("ForkAndExecProcess(filename=%p (%s), argv=%p, envp=%p, cwd=%p (%s), "
           "redirectStdin=%d, redirectStdout=%d, redirectStderr=%d, "
           "childPid=%p, stdinFd=%p, stdoutFd=%p, stderrFd=%p)\n",
           filename, filename ? filename : "NULL",
           argv, envp,
           cwd, cwd ? cwd : "NULL",
           redirectStdin, redirectStdout, redirectStderr,
           childPid, stdinFd, stdoutFd, stderrFd);

    // Validate arguments
    if (NULL == filename || NULL == argv || NULL == envp ||
        NULL == stdinFd || NULL == stdoutFd || NULL == stderrFd ||
        NULL == childPid)
    {
        ASSERT("%s should not be NULL\n",
            filename == NULL ? "filename" :
            argv == NULL ? "argv" :
            envp == NULL ? "envp" :
            stdinFd == NULL ? "stdinFd" :
            stdoutFd == NULL ? "stdoutFd" :
            stderrFd == NULL ? "stderrFd" :
            "childPid");
        errno = EINVAL;
        success = FALSE;
        goto done;
    }
    if ((redirectStdin  & ~1) != 0 ||
        (redirectStdout & ~1) != 0 ||
        (redirectStderr & ~1) != 0)
    {
        ASSERT("Boolean redirect* inputs must be 0 or 1. "
               "redirectStdin=%d redirectStdout=%d redirectStderr=%d ",
               redirectStdin, redirectStdout, redirectStderr);
        errno = EINVAL;
        success = FALSE;
        goto done;
    }

    // Open pipes for any requests to redirect stdin/stdout/stderr
    if ((redirectStdin  && pipe(stdinFds)  != 0) ||
        (redirectStdout && pipe(stdoutFds) != 0) ||
        (redirectStderr && pipe(stderrFds) != 0))
    {
        ASSERT("pipe() failed with error %d (%s)\n", errno, strerror(errno));
        success = FALSE;
        goto done;
    }

    // Fork the child process
    if ((processId = fork()) == -1)
    {
        ASSERT("fork() failed with error %d (%s)\n", errno, strerror(errno));
        success = FALSE;
        goto done;
    }

    /* From the time the child process (processId == 0) begins running from fork to when
     * it reaches execve, the child process must not touch anything in the PAL.  Doing so
     * is not safe. The parent process (processId >= 0) may continue to use the PAL.
     */

    if (processId == 0) // processId == 0 if this is child process
    {
        // Close the parent end of any open pipes
        closeIfOpen(stdinFds[WRITE_END_OF_PIPE]);
        closeIfOpen(stdoutFds[READ_END_OF_PIPE]);
        closeIfOpen(stderrFds[READ_END_OF_PIPE]);

        // For any redirections that should happen, dup the pipe descriptors onto stdin/out/err.
        // Then close out the old pipe descriptrs, which we no longer need.
        if ((redirectStdin  && dup2(stdinFds[READ_END_OF_PIPE],   STDIN_FILENO)  == -1) ||
            (redirectStdout && dup2(stdoutFds[WRITE_END_OF_PIPE], STDOUT_FILENO) == -1) ||
            (redirectStderr && dup2(stderrFds[WRITE_END_OF_PIPE], STDERR_FILENO) == -1))
        {
            _exit(errno != 0 ? errno : EXIT_FAILURE);
        }
        closeIfOpen(stdinFds[READ_END_OF_PIPE]);
        closeIfOpen(stdoutFds[WRITE_END_OF_PIPE]);
        closeIfOpen(stderrFds[WRITE_END_OF_PIPE]);

        // Change to the designated working directory, if one was specified
        if (NULL != cwd && chdir(cwd) == -1)
        {
            _exit(errno != 0 ? errno : EXIT_FAILURE);
        }

        // Finally, execute the new process.  execve will not return if it's successful.
        execve(filename, (char**)argv, (char**)envp);
        _exit(errno != 0 ? errno : EXIT_FAILURE); // execve failed
    }

    // This is the parent process. processId == pid of the child
    *childPid = processId;
    *stdinFd = stdinFds[WRITE_END_OF_PIPE];
    *stdoutFd = stdoutFds[READ_END_OF_PIPE];
    *stderrFd = stderrFds[READ_END_OF_PIPE];

done:
    // Regardless of success or failure, close the parent's copy of the child's end of
    // any opened pipes.  The parent doesn't need them anymore.
    closeIfOpen(stdinFds[READ_END_OF_PIPE]);
    closeIfOpen(stdoutFds[WRITE_END_OF_PIPE]);
    closeIfOpen(stderrFds[WRITE_END_OF_PIPE]);

    // If we failed, close everything else and give back error values in all out arguments.
    if (!success)
    {
        closeIfOpen(stdinFds[WRITE_END_OF_PIPE]);
        closeIfOpen(stdoutFds[READ_END_OF_PIPE]);
        closeIfOpen(stderrFds[READ_END_OF_PIPE]);

        *stdinFd  = -1;
        *stdoutFd = -1;
        *stderrFd = -1;
        *childPid = -1;
    }

    LOGEXIT("ForkAndExecProcess returns BOOL %d with error %d\n", success, success ? 0 : errno);
    PERF_EXIT(ForkAndExecProcess);

    return success ? 0 : -1;
}

#if HAVE_STAT64 && !(defined(__APPLE__) && defined(_AMD64_))
typedef struct stat64 stat_native;
#elif HAVE_STAT
typedef struct stat stat_native;
#else
#error need an alias for stat_native to stat struct for platform
#endif

void CopyStatNativeToFileInfo(struct fileinfo* dst, const stat_native* src)
{
    dst->flags = FILEINFO_FLAGS_NONE;
    dst->mode = src->st_mode;
    dst->uid = src->st_uid;
    dst->gid = src->st_gid;
    dst->size = src->st_size;
    dst->atime = src->st_atime;
    dst->mtime = src->st_mtime;
    dst->ctime = src->st_ctime;

    #if HAVE_STAT_BIRTHTIME
    dst->btime = src->st_birthtime;
    dst->flags |= FILEINFO_FLAGS_HAS_BTIME;
    #endif
}

int
PALAPI
GetFileInformationFromPath(
    const char* path,
    struct fileinfo* buf)
{
    PERF_ENTRY(GetFileInformationFromPath);
    ENTRY("GetFileInformationFromPath(path=%p (%s), buf=%p\n",
          path, path ? path : "NULL",
          buf);

    int success = FALSE;
    stat_native result;
    int ret;

    #if HAVE_STAT64 && !(defined(__APPLE__) && defined(_AMD64_))
    ret = stat64(path, &result);
    #elif HAVE_STAT
    ret = stat(path, &result);
    #else
    #error need implemetation of stat/stat64
    #endif

    if (ret == 0)
    {
        CopyStatNativeToFileInfo(buf, &result);
        success = TRUE;
    }

    LOGEXIT("GetFileInformationFromPath returns BOOL %d with error %d\n", success, success ? 0 : errno);
    PERF_EXIT(GetFileInformationFromPath);

    return success ? 0 : -1;
}

int
PALAPI
GetFileInformationFromFd(
    int fd,
    struct fileinfo* buf)
{
    PERF_ENTRY(GetFileInformationFromFd);
    ENTRY("GetFileInformationFromFd(fd=%d, buf=%p\n",
          fd, buf);

    int success = FALSE;
    stat_native result;
    int ret;

    #if HAVE_STAT64 && !(defined(__APPLE__) && defined(_AMD64_))
    ret = fstat64(fd, &result);
    #elif HAVE_STAT
    ret = fstat(fd, &result);
    #else
    #error need implemetation of fstat/fstat64
    #endif

    if (ret == 0)
    {
        CopyStatNativeToFileInfo(buf, &result);
        success = TRUE;
    }

    LOGEXIT("GetFileInformationFromFd returns BOOL %d with error %d\n", success, success ? 0 : errno);
    PERF_EXIT(GetFileInformationFromFd);

    return success ? 0 : -1;
}
