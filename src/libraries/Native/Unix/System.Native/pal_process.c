// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_config.h"
#include "pal_process.h"
#include "pal_io.h"
#include "pal_utilities.h"

#include <assert.h>
#include <errno.h>
#include <grp.h>
#include <limits.h>
#include <signal.h>
#include <stdlib.h>
#include <sys/resource.h>
#include <sys/types.h>
#include <sys/wait.h>
#include <syslog.h>
#include <unistd.h>
#if HAVE_CRT_EXTERNS_H
#include <crt_externs.h>
#endif
#if HAVE_PIPE2
#include <fcntl.h>
#endif
#include <pthread.h>

#if HAVE_SCHED_SETAFFINITY || HAVE_SCHED_GETAFFINITY
#include <sched.h>
#endif

// Validate that our Signals enum values are correct for the platform
c_static_assert(PAL_SIGKILL == SIGKILL);

// Validate that our SysLogPriority values are correct for the platform
c_static_assert(PAL_LOG_EMERG == LOG_EMERG);
c_static_assert(PAL_LOG_ALERT == LOG_ALERT);
c_static_assert(PAL_LOG_CRIT == LOG_CRIT);
c_static_assert(PAL_LOG_ERR == LOG_ERR);
c_static_assert(PAL_LOG_WARNING == LOG_WARNING);
c_static_assert(PAL_LOG_NOTICE == LOG_NOTICE);
c_static_assert(PAL_LOG_INFO == LOG_INFO);
c_static_assert(PAL_LOG_DEBUG == LOG_DEBUG);
c_static_assert(PAL_LOG_KERN == LOG_KERN);
c_static_assert(PAL_LOG_USER == LOG_USER);
c_static_assert(PAL_LOG_MAIL == LOG_MAIL);
c_static_assert(PAL_LOG_DAEMON == LOG_DAEMON);
c_static_assert(PAL_LOG_AUTH == LOG_AUTH);
c_static_assert(PAL_LOG_SYSLOG == LOG_SYSLOG);
c_static_assert(PAL_LOG_LPR == LOG_LPR);
c_static_assert(PAL_LOG_NEWS == LOG_NEWS);
c_static_assert(PAL_LOG_UUCP == LOG_UUCP);
c_static_assert(PAL_LOG_CRON == LOG_CRON);
c_static_assert(PAL_LOG_AUTHPRIV == LOG_AUTHPRIV);
c_static_assert(PAL_LOG_FTP == LOG_FTP);
c_static_assert(PAL_LOG_LOCAL0 == LOG_LOCAL0);
c_static_assert(PAL_LOG_LOCAL1 == LOG_LOCAL1);
c_static_assert(PAL_LOG_LOCAL2 == LOG_LOCAL2);
c_static_assert(PAL_LOG_LOCAL3 == LOG_LOCAL3);
c_static_assert(PAL_LOG_LOCAL4 == LOG_LOCAL4);
c_static_assert(PAL_LOG_LOCAL5 == LOG_LOCAL5);
c_static_assert(PAL_LOG_LOCAL6 == LOG_LOCAL6);
c_static_assert(PAL_LOG_LOCAL7 == LOG_LOCAL7);

// Validate that out PriorityWhich values are correct for the platform
c_static_assert(PAL_PRIO_PROCESS == (int)PRIO_PROCESS);
c_static_assert(PAL_PRIO_PGRP == (int)PRIO_PGRP);
c_static_assert(PAL_PRIO_USER == (int)PRIO_USER);

#if !HAVE_PIPE2
static pthread_mutex_t ProcessCreateLock = PTHREAD_MUTEX_INITIALIZER;
#endif

enum
{
    READ_END_OF_PIPE = 0,
    WRITE_END_OF_PIPE = 1,
};

static void CloseIfOpen(int fd)
{
    if (fd >= 0)
    {
        close(fd); // Ignoring errors from close is a deliberate choice
    }
}

static int Dup2WithInterruptedRetry(int oldfd, int newfd)
{
    int result;
    while (CheckInterrupted(result = dup2(oldfd, newfd)));
    return result;
}

static ssize_t WriteSize(int fd, const void* buffer, size_t count)
{
    ssize_t rv = 0;
    while (count > 0)
    {
        ssize_t result = 0;
        while (CheckInterrupted(result = write(fd, buffer, count)));
        if (result > 0)
        {
            rv += result;
            buffer = (const uint8_t*)buffer + result;
            count -= (size_t)result;
        }
        else
        {
            return -1;
        }
    }
    return rv;
}

static ssize_t ReadSize(int fd, void* buffer, size_t count)
{
    ssize_t rv = 0;
    while (count > 0)
    {
        ssize_t result = 0;
        while (CheckInterrupted(result = read(fd, buffer, count)));
        if (result > 0)
        {
            rv += result;
            buffer = (uint8_t*)buffer + result;
            count -= (size_t)result;
        }
        else
        {
            return -1;
        }
    }
    return rv;
}

__attribute__((noreturn))
static void ExitChild(int pipeToParent, int error)
{
    if (pipeToParent != -1)
    {
        WriteSize(pipeToParent, &error, sizeof(error));
    }
    _exit(error != 0 ? error : EXIT_FAILURE);
}

static int compare_groups(const void * a, const void * b)
{
    // Cast to signed because we need a signed return value.
    // It's okay to changed signedness (groups are uint), we just need an order.
    return *(const int32_t*)a - *(const int32_t*)b;
}

static int SetGroups(uint32_t* userGroups, int32_t userGroupsLength, uint32_t* processGroups)
{
#ifdef __linux__
    size_t platformGroupsLength = Int32ToSizeT(userGroupsLength);
#else // BSD
    int platformGroupsLength = userGroupsLength;
#endif
    int rv = setgroups(platformGroupsLength, userGroups);

    // We fall back to using the current process' groups, if they are a subset of the user groups.
    // We do this to support a user setting UserName to himself but not having setgroups permissions.
    // And for dealing with platforms with low NGROUP_MAX (e.g. 16 on OSX).
    if (rv == -1 && ((errno == EPERM) ||
                     (errno == EINVAL && userGroupsLength > NGROUPS_MAX)))
    {
        int processGroupsLength = getgroups(userGroupsLength, processGroups);
        if (processGroupsLength >= 0)
        {
            if (userGroupsLength == 0)
            {
                // calling setgroups with zero size returns number of groups.
                rv = processGroupsLength == 0 ? 0 : -1;
            }
            else
            {
                rv = 0;
                // sort the groups so we can efficiently search them.
                qsort(userGroups, (size_t)userGroupsLength, sizeof(uint32_t), compare_groups);
                for (int i = 0; i < processGroupsLength; i++)
                {
                    bool isUserGroup = NULL != bsearch(&processGroups[i], userGroups, (size_t)userGroupsLength, sizeof(uint32_t), compare_groups);
                    if (!isUserGroup)
                    {
                        rv = -1;
                        break;
                    }
                }
            }
        }
    }

    // Truncate on platforms with a low NGROUPS_MAX.
    if (rv == -1 && (errno == EINVAL && userGroupsLength > NGROUPS_MAX))
    {
        platformGroupsLength = NGROUPS_MAX;
        rv = setgroups(platformGroupsLength, userGroups);
    }

    return rv;
}

int32_t SystemNative_ForkAndExecProcess(const char* filename,
                                      char* const argv[],
                                      char* const envp[],
                                      const char* cwd,
                                      int32_t redirectStdin,
                                      int32_t redirectStdout,
                                      int32_t redirectStderr,
                                      int32_t setCredentials,
                                      uint32_t userId,
                                      uint32_t groupId,
                                      uint32_t* groups,
                                      int32_t groupsLength,
                                      int32_t* childPid,
                                      int32_t* stdinFd,
                                      int32_t* stdoutFd,
                                      int32_t* stderrFd)
{
#if !HAVE_PIPE2
    bool haveProcessCreateLock = false;
#endif
    bool success = true;
    int stdinFds[2] = {-1, -1}, stdoutFds[2] = {-1, -1}, stderrFds[2] = {-1, -1}, waitForChildToExecPipe[2] = {-1, -1};
    pid_t processId = -1;
    uint32_t* getGroupsBuffer = NULL;
    int thread_cancel_state;
    sigset_t signal_set;
    sigset_t old_signal_set;

    // None of this code can be canceled without leaking handles, so just don't allow it
    pthread_setcancelstate(PTHREAD_CANCEL_DISABLE, &thread_cancel_state);

    // Validate arguments
    if (NULL == filename || NULL == argv || NULL == envp || NULL == stdinFd || NULL == stdoutFd ||
        NULL == stderrFd || NULL == childPid || (groupsLength > 0 && groups == NULL))
    {
        assert(false && "null argument.");
        errno = EINVAL;
        success = false;
        goto done;
    }

    if ((redirectStdin & ~1) != 0 || (redirectStdout & ~1) != 0 || (redirectStderr & ~1) != 0 || (setCredentials & ~1) != 0)
    {
        assert(false && "Boolean redirect* inputs must be 0 or 1.");
        errno = EINVAL;
        success = false;
        goto done;
    }

    if (setCredentials && groupsLength > 0)
    {
        getGroupsBuffer = malloc(sizeof(uint32_t) * Int32ToSizeT(groupsLength));
        if (getGroupsBuffer == NULL)
        {
            success = false;
            goto done;
        }
    }

    // Make sure we can find and access the executable. exec will do this, of course, but at that point it's already
    // in the child process, at which point it'll translate to the child process' exit code rather than to failing
    // the Start itself.  There's a race condition here, in that this could change prior to exec's checks, but there's
    // little we can do about that. There are also more rigorous checks exec does, such as validating the executable
    // format of the target; such errors will emerge via the child process' exit code.
    if (access(filename, X_OK) != 0)
    {
        success = false;
        goto done;
    }

#if !HAVE_PIPE2
    // We do not have pipe2(); take the lock to emulate it race free.
    // If another process were to be launched between the pipe creation and the fcntl call to set CLOEXEC on it, that
    // file descriptor will be inherited into the other child process, eventually causing a deadlock either in the loop
    // below that waits for that pipe to be closed or in StreamReader.ReadToEnd() in the calling code.
    if (pthread_mutex_lock(&ProcessCreateLock) != 0)
    {
        // This check is pretty much just checking for trashed memory.
        success = false;
        goto done;
    }
    haveProcessCreateLock = true;
#endif

    // Open pipes for any requests to redirect stdin/stdout/stderr and set the
    // close-on-exec flag to the pipe file descriptors.
    if ((redirectStdin  && SystemNative_Pipe(stdinFds,  PAL_O_CLOEXEC) != 0) ||
        (redirectStdout && SystemNative_Pipe(stdoutFds, PAL_O_CLOEXEC) != 0) ||
        (redirectStderr && SystemNative_Pipe(stderrFds, PAL_O_CLOEXEC) != 0))
    {
        success = false;
        goto done;
    }

    // We create a pipe purely for the benefit of knowing when the child process has called exec.
    // We can use that to block waiting on the pipe to be closed, which lets us block the parent
    // from returning until the child process is actually transitioned to the target program.  This
    // avoids problems where the parent process uses members of Process, like ProcessName, when the
    // Process is still the clone of this one. This is a best-effort attempt, so ignore any errors.
    // If the child fails to exec we use the pipe to pass the errno to the parent process.
#if HAVE_PIPE2
    pipe2(waitForChildToExecPipe, O_CLOEXEC);
#else
    SystemNative_Pipe(waitForChildToExecPipe, PAL_O_CLOEXEC);
#endif

    // The fork child must not be signalled until it calls exec(): our signal handlers do not
    // handle being raised in the child process correctly
    sigfillset(&signal_set);
    pthread_sigmask(SIG_SETMASK, &signal_set, &old_signal_set);

#if HAVE_VFORK && !(defined(__APPLE__)) // We don't trust vfork on OS X right now.
    // This platform has vfork(). vfork() is either a synonym for fork or provides shared memory
    // semantics. For a one gigabyte process, the expected performance gain of using shared memory
    // vfork() rather than fork() is 99.5% merely due to avoiding page faults as the kernel does not
    // need to set all writable pages in the parent process to copy-on-write because the child process
    // is allowed to write to the parent process memory pages.

    // The thing to remember about shared memory vfork() is the documentation is way out of date.
    // It does the following things:
    // * creates a new process in the memory space of the calling process.
    // * blocks the calling thread (not process!) in an uninterruptable sleep
    // * sets up the process records so the following happen:
    //   + execve() replaces the memory space in the child and unblocks the parent
    //   + process exit by any means unblocks the parent
    //   + ptrace() makes a security demand against the parent process
    //   + accessing the terminal with read() or write() fail in system-dependent ways
    // Due to lack of documentation, setting signal handlers in the vfork() child is a bad idea. We don't
    // do this, but it's worth pointing out.

    // All platforms that provide shared memory vfork() check the parent process's context when
    // ptrace() is used on the child, thus making setuid() safe to use after vfork(). The fabled vfork()
    // security hole is the other way around; if a multithreaded host were to execute setuid()
    // on another thread while a vfork() child is still pending, bad things are possible; however we
    // do not do that.

    if ((processId = vfork()) == 0) // processId == 0 if this is child process
#else
    if ((processId = fork()) == 0) // processId == 0 if this is child process
#endif
    {
        // It turns out that child processes depend on their sigmask being set to something sane rather than mask all.
        // On the other hand, we have to mask all to avoid our own signal handlers running in the child process, writing
        // to the pipe, and waking up the handling thread in the parent process. This also avoids third-party code getting
        // equally confused.
        // Remove all signals, then restore signal mask.
        // Since we are in a vfork() child, the only safe signal values are SIG_DFL and SIG_IGN.  See man 3 libthr on BSD.
        // "The implementation interposes the user-installed signal(3) handlers....to pospone signal delivery to threads
        // which entered (libthr-internal) critical sections..."  We want to pass SIG_DFL anyway.
        sigset_t junk_signal_set;
        struct sigaction sa_default;
        struct sigaction sa_old;
        memset(&sa_default, 0, sizeof(sa_default)); // On some architectures, sa_mask is a struct so assigning zero to it doesn't compile
        sa_default.sa_handler = SIG_DFL;
        for (int sig = 1; sig < NSIG; ++sig)
        {
            if (sig == SIGKILL || sig == SIGSTOP)
            {
                continue;
            }
            if (!sigaction(sig, NULL, &sa_old))
            {
                void (*oldhandler)(int) = (sa_old.sa_flags & SA_SIGINFO) ? (void (*)(int))sa_old.sa_sigaction : sa_old.sa_handler;
                if (oldhandler != SIG_IGN && oldhandler != SIG_DFL)
                {
                    // It has a custom handler, put the default handler back.
                    // We check first to preserve flags on default handlers.
                    sigaction(sig, &sa_default, NULL);
                }
            }
        }
        pthread_sigmask(SIG_SETMASK, &old_signal_set, &junk_signal_set); // Not all architectures allow NULL here

        // For any redirections that should happen, dup the pipe descriptors onto stdin/out/err.
        // We don't need to explicitly close out the old pipe descriptors as they will be closed on the 'execve' call.
        if ((redirectStdin && Dup2WithInterruptedRetry(stdinFds[READ_END_OF_PIPE], STDIN_FILENO) == -1) ||
            (redirectStdout && Dup2WithInterruptedRetry(stdoutFds[WRITE_END_OF_PIPE], STDOUT_FILENO) == -1) ||
            (redirectStderr && Dup2WithInterruptedRetry(stderrFds[WRITE_END_OF_PIPE], STDERR_FILENO) == -1))
        {
            ExitChild(waitForChildToExecPipe[WRITE_END_OF_PIPE], errno);
        }

        if (setCredentials)
        {
            if (SetGroups(groups, groupsLength, getGroupsBuffer) == -1 ||
                setgid(groupId) == -1 ||
                setuid(userId) == -1)
            {
                ExitChild(waitForChildToExecPipe[WRITE_END_OF_PIPE], errno);
            }
        }

        // Change to the designated working directory, if one was specified
        if (NULL != cwd)
        {
            int result;
            while (CheckInterrupted(result = chdir(cwd)));
            if (result == -1)
            {
                ExitChild(waitForChildToExecPipe[WRITE_END_OF_PIPE], errno);
            }
        }

        // Finally, execute the new process.  execve will not return if it's successful.
        execve(filename, argv, envp);
        ExitChild(waitForChildToExecPipe[WRITE_END_OF_PIPE], errno); // execve failed
    }

    // Restore signal mask in the parent process immediately after fork() or vfork() call
    pthread_sigmask(SIG_SETMASK, &old_signal_set, &signal_set);

    if (processId < 0)
    {
        // failed
        success = false;
        goto done;
    }

    // This is the parent process. processId == pid of the child
    *childPid = processId;
    *stdinFd = stdinFds[WRITE_END_OF_PIPE];
    *stdoutFd = stdoutFds[READ_END_OF_PIPE];
    *stderrFd = stderrFds[READ_END_OF_PIPE];

done:;
#if !HAVE_PIPE2
    if (haveProcessCreateLock)
    {
        pthread_mutex_unlock(&ProcessCreateLock);
    }
#endif

    int priorErrno = errno;

    // Regardless of success or failure, close the parent's copy of the child's end of
    // any opened pipes.  The parent doesn't need them anymore.
    CloseIfOpen(stdinFds[READ_END_OF_PIPE]);
    CloseIfOpen(stdoutFds[WRITE_END_OF_PIPE]);
    CloseIfOpen(stderrFds[WRITE_END_OF_PIPE]);

    // Also close the write end of the exec waiting pipe, and wait for the pipe to be closed
    // by trying to read from it (the read will wake up when the pipe is closed and broken).
    // Ignore any errors... this is a best-effort attempt.
    CloseIfOpen(waitForChildToExecPipe[WRITE_END_OF_PIPE]);
    if (waitForChildToExecPipe[READ_END_OF_PIPE] != -1)
    {
        int childError;
        if (success)
        {
            ssize_t result = ReadSize(waitForChildToExecPipe[READ_END_OF_PIPE], &childError, sizeof(childError));
            if (result == sizeof(childError))
            {
                success = false;
                priorErrno = childError;
            }
        }
        CloseIfOpen(waitForChildToExecPipe[READ_END_OF_PIPE]);
    }

    // If we failed, close everything else and give back error values in all out arguments.
    if (!success)
    {
        CloseIfOpen(stdinFds[WRITE_END_OF_PIPE]);
        CloseIfOpen(stdoutFds[READ_END_OF_PIPE]);
        CloseIfOpen(stderrFds[READ_END_OF_PIPE]);

        // Reap child
        if (processId > 0)
        {
            int status;
            waitpid(processId, &status, 0);
        }

        *stdinFd = -1;
        *stdoutFd = -1;
        *stderrFd = -1;
        *childPid = -1;

        errno = priorErrno;
    }

    // Restore thread cancel state
    pthread_setcancelstate(thread_cancel_state, &thread_cancel_state);
  
    free(getGroupsBuffer);

    return success ? 0 : -1;
}

FILE* SystemNative_POpen(const char* command, const char* type)
{
    assert(command != NULL);
    assert(type != NULL);
    return popen(command, type);
}

int32_t SystemNative_PClose(FILE* stream)
{
    assert(stream != NULL);
    return pclose(stream);
}

// Each platform type has it's own RLIMIT values but the same name, so we need
// to convert our standard types into the platform specific ones.
static int32_t ConvertRLimitResourcesPalToPlatform(RLimitResources value)
{
    switch (value)
    {
        case PAL_RLIMIT_CPU:
            return RLIMIT_CPU;
        case PAL_RLIMIT_FSIZE:
            return RLIMIT_FSIZE;
        case PAL_RLIMIT_DATA:
            return RLIMIT_DATA;
        case PAL_RLIMIT_STACK:
            return RLIMIT_STACK;
        case PAL_RLIMIT_CORE:
            return RLIMIT_CORE;
        case PAL_RLIMIT_AS:
            return RLIMIT_AS;
        case PAL_RLIMIT_RSS:
            return RLIMIT_RSS;
        case PAL_RLIMIT_MEMLOCK:
            return RLIMIT_MEMLOCK;
        case PAL_RLIMIT_NPROC:
            return RLIMIT_NPROC;
        case PAL_RLIMIT_NOFILE:
            return RLIMIT_NOFILE;
    }

    assert_msg(false, "Unknown RLIMIT value", (int)value);
    return -1;
}

#define LIMIT_MAX(T) _Generic(((T)0), \
  unsigned int: UINT_MAX,             \
  unsigned long: ULONG_MAX,           \
  long: LONG_MAX,                     \
  unsigned long long: ULLONG_MAX)

// Because RLIM_INFINITY is different per-platform, use the max value of a uint64 (which is RLIM_INFINITY on Ubuntu)
// to signify RLIM_INIFINITY; on OS X, where RLIM_INFINITY is slightly lower, we'll translate it to the correct value
// here.
static rlim_t ConvertFromManagedRLimitInfinityToPalIfNecessary(uint64_t value)
{
    // rlim_t type can vary per platform, so we also treat anything outside its range as infinite.
    if (value == UINT64_MAX || value > LIMIT_MAX(rlim_t))
        return RLIM_INFINITY;

    return (rlim_t)value;
}

// Because RLIM_INFINITY is different per-platform, use the max value of a uint64 (which is RLIM_INFINITY on Ubuntu)
// to signify RLIM_INIFINITY; on OS X, where RLIM_INFINITY is slightly lower, we'll translate it to the correct value
// here.
static uint64_t ConvertFromNativeRLimitInfinityToManagedIfNecessary(rlim_t value)
{
    if (value == RLIM_INFINITY)
        return UINT64_MAX;

    assert(value >= 0);
    return (uint64_t)value;
}

static void ConvertFromRLimitManagedToPal(const RLimit* pal, struct rlimit* native)
{
    native->rlim_cur = ConvertFromManagedRLimitInfinityToPalIfNecessary(pal->CurrentLimit);
    native->rlim_max = ConvertFromManagedRLimitInfinityToPalIfNecessary(pal->MaximumLimit);
}

static void ConvertFromPalRLimitToManaged(const struct rlimit* native, RLimit* pal)
{
    pal->CurrentLimit = ConvertFromNativeRLimitInfinityToManagedIfNecessary(native->rlim_cur);
    pal->MaximumLimit = ConvertFromNativeRLimitInfinityToManagedIfNecessary(native->rlim_max);
}

#if defined __USE_GNU && !defined __cplusplus
typedef __rlimit_resource_t rlimitResource;
typedef __priority_which_t priorityWhich;
#else
typedef int rlimitResource;
typedef int priorityWhich;
#endif

int32_t SystemNative_GetRLimit(RLimitResources resourceType, RLimit* limits)
{
    assert(limits != NULL);

    int32_t platformLimit = ConvertRLimitResourcesPalToPlatform(resourceType);
    struct rlimit internalLimit;
    int result = getrlimit((rlimitResource)platformLimit, &internalLimit);
    if (result == 0)
    {
        ConvertFromPalRLimitToManaged(&internalLimit, limits);
    }
    else
    {
        memset(limits, 0, sizeof(RLimit));
    }

    return result;
}

int32_t SystemNative_SetRLimit(RLimitResources resourceType, const RLimit* limits)
{
    assert(limits != NULL);

    int32_t platformLimit = ConvertRLimitResourcesPalToPlatform(resourceType);
    struct rlimit internalLimit;
    ConvertFromRLimitManagedToPal(limits, &internalLimit);
    return setrlimit((rlimitResource)platformLimit, &internalLimit);
}

int32_t SystemNative_Kill(int32_t pid, int32_t signal)
{
    return kill(pid, signal);
}

int32_t SystemNative_GetPid()
{
    return getpid();
}

int32_t SystemNative_GetSid(int32_t pid)
{
    return getsid(pid);
}

void SystemNative_SysLog(SysLogPriority priority, const char* message, const char* arg1)
{
    syslog((int)priority, message, arg1);
}

int32_t SystemNative_WaitIdAnyExitedNoHangNoWait()
{
    siginfo_t siginfo;
    int32_t result;
    while (CheckInterrupted(result = waitid(P_ALL, 0, &siginfo, WEXITED | WNOHANG | WNOWAIT)));
    if (result == -1 && errno == ECHILD)
    {
        // The calling process has no existing unwaited-for child processes.
        result = 0;
    }
    else if (result == 0 && siginfo.si_signo == SIGCHLD)
    {
        result = siginfo.si_pid;
    }
    return result;
}

int32_t SystemNative_WaitPidExitedNoHang(int32_t pid, int32_t* exitCode)
{
    assert(exitCode != NULL);

    int32_t result;
    int status;
    while (CheckInterrupted(result = waitpid(pid, &status, WNOHANG)));
    if (result > 0)
    {
        if (WIFEXITED(status))
        {
            // the child terminated normally.
            *exitCode = WEXITSTATUS(status);
        }
        else if (WIFSIGNALED(status))
        {
            // child process was terminated by a signal.
            *exitCode = 128 + WTERMSIG(status);
        }
        else
        {
            assert(false);
        }
    }
    return result;
}

int64_t SystemNative_PathConf(const char* path, PathConfName name)
{
    int32_t confValue = -1;
    switch (name)
    {
        case PAL_PC_LINK_MAX:
            confValue = _PC_LINK_MAX;
            break;
        case PAL_PC_MAX_CANON:
            confValue = _PC_MAX_CANON;
            break;
        case PAL_PC_MAX_INPUT:
            confValue = _PC_MAX_INPUT;
            break;
        case PAL_PC_NAME_MAX:
            confValue = _PC_NAME_MAX;
            break;
        case PAL_PC_PATH_MAX:
            confValue = _PC_PATH_MAX;
            break;
        case PAL_PC_PIPE_BUF:
            confValue = _PC_PIPE_BUF;
            break;
        case PAL_PC_CHOWN_RESTRICTED:
            confValue = _PC_CHOWN_RESTRICTED;
            break;
        case PAL_PC_NO_TRUNC:
            confValue = _PC_NO_TRUNC;
            break;
        case PAL_PC_VDISABLE:
            confValue = _PC_VDISABLE;
            break;
    }

    if (confValue == -1)
    {
        assert_msg(false, "Unknown PathConfName", (int)name);
        errno = EINVAL;
        return -1;
    }

    return pathconf(path, confValue);
}

int32_t SystemNative_GetPriority(PriorityWhich which, int32_t who)
{
    // GetPriority uses errno 0 to show success to make sure we don't have a stale value
    errno = 0;
#if PRIORITY_REQUIRES_INT_WHO
    return getpriority((priorityWhich)which, who);
#else
    return getpriority((priorityWhich)which, (id_t)who);
#endif
}

int32_t SystemNative_SetPriority(PriorityWhich which, int32_t who, int32_t nice)
{
#if PRIORITY_REQUIRES_INT_WHO
    return setpriority((priorityWhich)which, who, nice);
#else
    return setpriority((priorityWhich)which, (id_t)who, nice);
#endif
}

char* SystemNative_GetCwd(char* buffer, int32_t bufferSize)
{
    assert(bufferSize >= 0);

    if (bufferSize < 0)
    {
        errno = EINVAL;
        return NULL;
    }

    return getcwd(buffer, Int32ToSizeT(bufferSize));
}

#if HAVE_SCHED_SETAFFINITY
int32_t SystemNative_SchedSetAffinity(int32_t pid, intptr_t* mask)
{
    assert(mask != NULL);

    int maxCpu = sizeof(intptr_t) * 8;
    assert(maxCpu <= CPU_SETSIZE);

    cpu_set_t set;
    CPU_ZERO(&set);

    intptr_t bits = *mask; 
    for (int cpu = 0; cpu < maxCpu; cpu++)
    {
        if ((bits & (((intptr_t)1u) << cpu)) != 0)
        {
            CPU_SET(cpu, &set);
        }
    }
 
    return sched_setaffinity(pid, sizeof(cpu_set_t), &set);
}
#endif

#if HAVE_SCHED_GETAFFINITY
int32_t SystemNative_SchedGetAffinity(int32_t pid, intptr_t* mask)
{
    assert(mask != NULL);

    cpu_set_t set;
    int32_t result = sched_getaffinity(pid, sizeof(cpu_set_t), &set);
    if (result == 0)
    {
        int maxCpu = sizeof(intptr_t) * 8;
        assert(maxCpu <= CPU_SETSIZE);

        intptr_t bits = 0;
        for (int cpu = 0; cpu < maxCpu; cpu++)
        {
            if (CPU_ISSET(cpu, &set))
            {
                bits |= ((intptr_t)1) << cpu;
            }
        }

        *mask = bits;
    }
    else
    {
        *mask = 0;
    }

    return result;
}
#endif
