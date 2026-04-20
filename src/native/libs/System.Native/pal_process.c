// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
#include <fcntl.h>

#if defined(__linux__)
#if !defined(HAVE_CLOSE_RANGE)
#include <sys/syscall.h>
#if !defined(__NR_close_range)
// close_range was added in Linux 5.9. The syscall number is 436 for all
// architectures using the generic syscall table (asm-generic/unistd.h),
// which covers aarch64, riscv, s390x, ppc64le, and others. The exception
// is alpha, which has its own syscall table and uses 546 instead.
# if defined(__alpha__)
#  define __NR_close_range 546
# else
#  define __NR_close_range 436
# endif
#endif // !defined(__NR_close_range)
#endif // !defined(HAVE_CLOSE_RANGE)
#endif // defined(__linux__)
#if (HAVE_CLOSE_RANGE || defined(__NR_close_range)) && !defined(CLOSE_RANGE_CLOEXEC)
#define CLOSE_RANGE_CLOEXEC (1U << 2)
#endif
#include <pthread.h>

#if HAVE_SCHED_SETAFFINITY || HAVE_SCHED_GETAFFINITY
#include <sched.h>
#endif

#ifdef __APPLE__
#include <mach-o/dyld.h>
#include <spawn.h>
#endif

#ifdef __FreeBSD__
#include <sys/types.h>
#include <sys/param.h>
#include <sys/sysctl.h>
#endif

#include <minipal/getexepath.h>

// Validate that our SysLogPriority values are correct for the platform
c_static_assert(PAL_LOG_EMERG == LOG_EMERG);
c_static_assert(PAL_LOG_ALERT == LOG_ALERT);
c_static_assert(PAL_LOG_CRIT == LOG_CRIT);
c_static_assert(PAL_LOG_ERR == LOG_ERR);
c_static_assert(PAL_LOG_WARNING == LOG_WARNING);
c_static_assert(PAL_LOG_NOTICE == LOG_NOTICE);
c_static_assert(PAL_LOG_INFO == LOG_INFO);
c_static_assert(PAL_LOG_DEBUG == LOG_DEBUG);

// Validate that out PriorityWhich values are correct for the platform
c_static_assert(PAL_PRIO_PROCESS == (int)PRIO_PROCESS);
c_static_assert(PAL_PRIO_PGRP == (int)PRIO_PGRP);
c_static_assert(PAL_PRIO_USER == (int)PRIO_USER);

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
#if defined(__linux__) || defined(TARGET_WASM)
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

typedef void (*VoidIntFn)(int);

static
VoidIntFn
handler_from_sigaction (struct sigaction *sa)
{
    if (((unsigned int)sa->sa_flags) & SA_SIGINFO)
    {
        // work around -Wcast-function-type
        void (*tmp)(void) = (void (*)(void))sa->sa_sigaction;
        return (void (*)(int))tmp;
    }
    else
    {
        return sa->sa_handler;
    }
}

#if HAVE_FDWALK
// Callback used with fdwalk() on Illumos/Solaris to set FD_CLOEXEC on all file descriptors >= 3.
static int SetCloexecForFd(void* context, int fd)
{
    (void)context;
    if (fd >= 3)
    {
        int flags = fcntl(fd, F_GETFD);
        if (flags != -1 && (flags & FD_CLOEXEC) == 0)
        {
            fcntl(fd, F_SETFD, flags | FD_CLOEXEC);
        }
    }
    return 0;
}
#endif

// Used when OS-specific bulk methods (close_range, fdwalk) are unavailable or fail.
// Always sets FD_CLOEXEC rather than closing fds directly. Closing fds directly would close
// waitForChildToExecPipe[WRITE_END_OF_PIPE] (which has O_CLOEXEC), making it impossible to
// report exec() failures back to the parent when execve() fails after RestrictHandleInheritance.
static void SetCloexecForAllFdsFallback(void)
{
    struct rlimit rl;
    int maxFd;
    if (getrlimit(RLIMIT_NOFILE, &rl) == 0 && rl.rlim_cur != RLIM_INFINITY)
    {
        maxFd = (int)rl.rlim_cur;
    }
    else
    {
        maxFd = (int)sysconf(_SC_OPEN_MAX);
        if (maxFd <= 0)
        {
            maxFd = 65536; // reasonable upper bound
        }
    }

    // Fallback: iterate over all file descriptors and set FD_CLOEXEC on each open one >= 3.
    for (int fd = 3; fd < maxFd; fd++)
    {
        int flags = fcntl(fd, F_GETFD);
        if (flags == -1)
        {
            continue; // fd not open
        }

        if ((flags & FD_CLOEXEC) == 0)
        {
            fcntl(fd, F_SETFD, flags | FD_CLOEXEC);
        }
    }
}

static void RestrictHandleInheritance(int32_t* inheritedFds, int32_t inheritedFdCount)
{
    // FDs 0-2 are stdin/stdout/stderr; this method must be called AFTER the dup2 calls.
    // We always set FD_CLOEXEC rather than closing fds directly. This is critical because
    // waitForChildToExecPipe[WRITE_END_OF_PIPE] (which has O_CLOEXEC) must remain open
    // until execve() is called so that exec failures can be reported back to the parent.
    // Using closefrom() or close_range() with flag 0 (direct close) would destroy this pipe.

#if HAVE_CLOSE_RANGE
    // On systems where close_range() is available as a function (FreeBSD 12.2+, Linux glibc >= 2.34).
    if (close_range(3, UINT_MAX, CLOSE_RANGE_CLOEXEC) != 0)
    {
        SetCloexecForAllFdsFallback();
    }
#elif defined(__NR_close_range)
    // On Linux with older glibc that doesn't expose close_range() as a function,
    // use the raw syscall number if the kernel supports it (kernel >= 5.9).
    if (syscall(__NR_close_range, 3, UINT_MAX, CLOSE_RANGE_CLOEXEC) != 0)
    {
        SetCloexecForAllFdsFallback();
    }
#elif HAVE_FDWALK
    // On Illumos/Solaris, use fdwalk() to set FD_CLOEXEC on all open fds >= 3.
    if (fdwalk(SetCloexecForFd, NULL) != 0)
    {
        SetCloexecForAllFdsFallback();
    }
#else
    SetCloexecForAllFdsFallback();
#endif

    // Remove CLOEXEC from user-provided inherited file descriptors so they survive execve.
    for (int i = 0; i < inheritedFdCount; i++)
    {
        int fd = inheritedFds[i];
        if (fd >= 3) // skip std io (already handled by dup)
        {
            int flags = fcntl(fd, F_GETFD);
            if (flags != -1)
            {
                fcntl(fd, F_SETFD, flags & ~FD_CLOEXEC);
            }
        }
    }
}

int32_t SystemNative_ForkAndExecProcess(const char* filename,
                                      char* const argv[],
                                      char* const envp[],
                                      const char* cwd,
                                      int32_t setCredentials,
                                      uint32_t userId,
                                      uint32_t groupId,
                                      uint32_t* groups,
                                      int32_t groupsLength,
                                      int32_t* childPid,
                                      int32_t stdinFd,
                                      int32_t stdoutFd,
                                      int32_t stderrFd,
                                      int32_t* inheritedFds,
                                      int32_t inheritedFdCount,
                                      int32_t startDetached)
{
#if HAVE_FORK || defined(TARGET_OSX) || defined(TARGET_MACCATALYST)
    assert(NULL != filename && NULL != argv && NULL != envp && NULL != childPid &&
            (groupsLength == 0 || groups != NULL) && "null argument.");

    *childPid = -1;

    // Make sure we can find and access the executable. exec will do this, of course, but at that point it's already
    // in the child process, at which point it'll translate to the child process' exit code rather than to failing
    // the Start itself.  There's a race condition here, in that this could change prior to exec's checks, but there's
    // little we can do about that. There are also more rigorous checks exec does, such as validating the executable
    // format of the target; such errors will emerge via the child process' exit code.
    if (access(filename, X_OK) != 0)
    {
        return -1;
    }
#endif

#if defined(TARGET_OSX) || defined(TARGET_MACCATALYST)
#if !HAVE_FORK
    // On MacCatalyst, fork(2) exists in the SDK but is blocked by the kernel at runtime (EPERM).
    // setuid/setgid-based credential changes require fork.
    if (setCredentials)
    {
        errno = ENOTSUP;
        return -1;
    }
#endif

#if !HAVE_POSIX_SPAWN_FILE_ACTIONS_ADDCHDIR_NP
    // posix_spawn_file_actions_addchdir_np is not available on all Apple platforms (e.g. MacCatalyst).
    if (cwd != NULL)
    {
        errno = ENOTSUP;
        return -1;
    }
#endif
    // Use posix_spawn on macOS/MacCatalyst when credentials don't need to be set,
    // since posix_spawn does not support setuid/setgid.
    if (!setCredentials)
    {
        pid_t spawnedPid;
        posix_spawn_file_actions_t file_actions;
        posix_spawnattr_t attr;
        int result;

        if ((result = posix_spawnattr_init(&attr)) != 0)
        {
            errno = result;
            return -1;
        }

        // Build sigdefault set: only reset signals that have custom handlers,
        // preserving SIG_IGN and SIG_DFL handlers (matching fork path behavior).
        sigset_t sigdefault_set;
        sigemptyset(&sigdefault_set);
        for (int sig = 1; sig < NSIG; ++sig)
        {
            if (sig == SIGKILL || sig == SIGSTOP)
            {
                continue;
            }

            struct sigaction sa_old;
            if (!sigaction(sig, NULL, &sa_old))
            {
                void (*oldhandler)(int) = handler_from_sigaction(&sa_old);
                if (oldhandler != SIG_IGN && oldhandler != SIG_DFL)
                {
                    sigaddset(&sigdefault_set, sig);
                }
            }
        }

        // pthread_sigmask follows POSIX thread conventions: it returns an error number but does not set errno
        sigset_t current_mask;
        result = pthread_sigmask(SIG_SETMASK, NULL, &current_mask);
        if (result != 0)
        {
            posix_spawnattr_destroy(&attr);
            errno = result;
            return -1;
        }

        // POSIX_SPAWN_SETSIGDEF to reset signal handlers to default
        // POSIX_SPAWN_SETSIGMASK to set the child's signal mask
        short flags = POSIX_SPAWN_SETSIGDEF | POSIX_SPAWN_SETSIGMASK;

        // When inheritedFdCount >= 0, use POSIX_SPAWN_CLOEXEC_DEFAULT to close all FDs by default,
        // then use posix_spawn_file_actions_addinherit_np to explicitly keep the specified FDs open.
        if (inheritedFdCount >= 0)
        {
            flags |= POSIX_SPAWN_CLOEXEC_DEFAULT;
        }

        // When startDetached is set, create a new session so the child is detached from the parent.
        if (startDetached)
        {
            flags |= POSIX_SPAWN_SETSID;
        }

        if ((result = posix_spawnattr_setflags(&attr, flags)) != 0
            || (result = posix_spawnattr_setsigdefault(&attr, &sigdefault_set)) != 0
            || (result = posix_spawnattr_setsigmask(&attr, &current_mask)) != 0 // Set the child's signal mask to match the parent's current mask
            || (result = posix_spawn_file_actions_init(&file_actions)) != 0)
        {
            int saved_errno = result;
            posix_spawnattr_destroy(&attr);
            errno = saved_errno;
            return -1;
        }

        // Redirect stdin/stdout/stderr
        if ((stdinFd != -1 && (result = posix_spawn_file_actions_adddup2(&file_actions, stdinFd, STDIN_FILENO)) != 0)
            || (stdoutFd != -1 && (result = posix_spawn_file_actions_adddup2(&file_actions, stdoutFd, STDOUT_FILENO)) != 0)
            || (stderrFd != -1 && (result = posix_spawn_file_actions_adddup2(&file_actions, stderrFd, STDERR_FILENO)) != 0)
#if HAVE_POSIX_SPAWN_FILE_ACTIONS_ADDCHDIR_NP
            || (cwd != NULL && (result = posix_spawn_file_actions_addchdir_np(&file_actions, cwd)) != 0) // Change working directory if specified
#endif
            )
        {
            int saved_errno = result;
            posix_spawn_file_actions_destroy(&file_actions);
            posix_spawnattr_destroy(&attr);
            errno = saved_errno;
            return -1;
        }

        // When handle count restriction is active, explicitly mark the user-provided FDs as inherited
        if (inheritedFdCount > 0)
        {
            for (int i = 0; i < inheritedFdCount; i++)
            {
                int fd = inheritedFds[i];
                if (fd != STDIN_FILENO && fd != STDOUT_FILENO && fd != STDERR_FILENO)
                {
                    if ((result = posix_spawn_file_actions_addinherit_np(&file_actions, fd)) != 0)
                    {
                        int saved_errno = result;
                        posix_spawn_file_actions_destroy(&file_actions);
                        posix_spawnattr_destroy(&attr);
                        errno = saved_errno;
                        return -1;
                    }
                }
            }
        }

        // Spawn the process
        result = posix_spawn(&spawnedPid, filename, &file_actions, &attr, argv, envp);

        posix_spawn_file_actions_destroy(&file_actions);
        posix_spawnattr_destroy(&attr);

        if (result != 0)
        {
            errno = result;
            return -1;
        }

        *childPid = spawnedPid;
        return 0;
    }
#endif

#if HAVE_FORK
    bool success = true;
    int waitForChildToExecPipe[2] = {-1, -1};
    pid_t processId = -1;
    uint32_t* getGroupsBuffer = NULL;
    sigset_t signal_set;
    sigset_t old_signal_set;

#if HAVE_PTHREAD_SETCANCELSTATE
    int thread_cancel_state;

    // None of this code can be canceled without leaking handles, so just don't allow it
    pthread_setcancelstate(PTHREAD_CANCEL_DISABLE, &thread_cancel_state);
#endif

    if (setCredentials && groupsLength > 0)
    {
        getGroupsBuffer = (uint32_t*)(malloc(sizeof(uint32_t) * Int32ToSizeT(groupsLength)));
        if (getGroupsBuffer == NULL)
        {
            success = false;
            goto done;
        }
    }

    // We create a pipe purely for the benefit of knowing when the child process has called exec.
    // We can use that to block waiting on the pipe to be closed, which lets us block the parent
    // from returning until the child process is actually transitioned to the target program.  This
    // avoids problems where the parent process uses members of Process, like ProcessName, when the
    // Process is still the clone of this one. This is a best-effort attempt, so ignore any errors.
    // If the child fails to exec we use the pipe to pass the errno to the parent process.
#if HAVE_PIPE2
    (void)! pipe2(waitForChildToExecPipe, O_CLOEXEC);
#else
    (void)! SystemNative_Pipe(waitForChildToExecPipe, PAL_O_CLOEXEC);
#endif

    // The fork child must not be signalled until it calls exec(): our signal handlers do not
    // handle being raised in the child process correctly
    sigfillset(&signal_set);
    pthread_sigmask(SIG_SETMASK, &signal_set, &old_signal_set);

// vfork on OS X is deprecated
// On Android, signal handlers between parent and child processes are shared with vfork, so when we reset
// the signal handlers during child startup, we end up incorrectly clearing also the ones for the parent.
#if HAVE_VFORK && !defined(__APPLE__) && !defined(TARGET_ANDROID)

    // This platform has vfork(). vfork() is either a synonym for fork or provides shared memory
    // semantics. For a one gigabyte process, the expected performance gain of using shared memory
    // vfork() rather than fork() is 99.5% merely due to avoiding page faults as the kernel does not
    // need to set all writable pages in the parent process to copy-on-write because the child process
    // is allowed to write to the parent process memory pages.

    // The thing to remember about shared memory vfork() is the documentation is way out of date.
    // It does the following things:
    // * creates a new process in the memory space of the calling process.
    // * blocks the calling thread (not process!) in an uninterruptible sleep
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

#if defined (__GLIBC__)
    if ((processId = vfork()) == 0) // processId == 0 if this is child process
#else
    // musl libc has an undocumented failure mode around setuid(); we must exclude it.
    if (setCredentials)
    {
        processId = fork();
    }
    else
    {
        processId = vfork();
    }
    if (processId == 0)
#endif

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
        // "The implementation interposes the user-installed signal(3) handlers....to postpone signal delivery to threads
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
                void (*oldhandler)(int) = handler_from_sigaction (&sa_old);
                if (oldhandler != SIG_IGN && oldhandler != SIG_DFL)
                {
                    // It has a custom handler, put the default handler back.
                    // We check first to preserve flags on default handlers.
                    sigaction(sig, &sa_default, NULL);
                }
            }
        }
        pthread_sigmask(SIG_SETMASK, &old_signal_set, &junk_signal_set); // Not all architectures allow NULL here

        // Map stdin/out/err for the new process to the provided fds.
        // They are not closed on exec because dup2 clears CLOEXEC.
        if ((stdinFd != -1 && stdinFd != STDIN_FILENO && Dup2WithInterruptedRetry(stdinFd, STDIN_FILENO) == -1) ||
            (stdoutFd != -1 && stdoutFd != STDOUT_FILENO && Dup2WithInterruptedRetry(stdoutFd, STDOUT_FILENO) == -1) ||
            (stderrFd != -1 && stderrFd != STDERR_FILENO && Dup2WithInterruptedRetry(stderrFd, STDERR_FILENO) == -1))
        {
            ExitChild(waitForChildToExecPipe[WRITE_END_OF_PIPE], errno);
        }

        // Start the child in a new session when startDetached is set, making it independent
        // of the parent's process group and terminal.
        if (startDetached && setsid() == -1)
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

        if (inheritedFdCount >= 0)
        {
            RestrictHandleInheritance(inheritedFds, inheritedFdCount);
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

done:;

    int priorErrno = errno;

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

    // If we failed, give back error values in all out arguments.
    if (!success)
    {
        // Reap child
        if (processId > 0)
        {
            int status;
            waitpid(processId, &status, 0);
        }

        *childPid = -1;

        errno = priorErrno;
    }

#if HAVE_PTHREAD_SETCANCELSTATE
    // Restore thread cancel state
    pthread_setcancelstate(thread_cancel_state, &thread_cancel_state);
#endif

    free(getGroupsBuffer);

    return success ? 0 : -1;
#else
    // ignore unused parameters
    (void)filename;
    (void)argv;
    (void)envp;
    (void)cwd;
    (void)setCredentials;
    (void)userId;
    (void)groupId;
    (void)groups;
    (void)groupsLength;
    (void)childPid;
    (void)stdinFd;
    (void)stdoutFd;
    (void)stderrFd;
    (void)inheritedFds;
    (void)inheritedFdCount;
    (void)startDetached;
    return -1;
#endif
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
#ifdef RLIMIT_AS
            return RLIMIT_AS;
#elif defined(RLIMIT_VMEM)
            return RLIMIT_VMEM;
#endif
#ifdef RLIMIT_RSS
        case PAL_RLIMIT_RSS:
            return RLIMIT_RSS;
#elif defined(RLIMIT_VMEM)
        case PAL_RLIMIT_RSS:
            return RLIMIT_VMEM;
#endif
#ifdef RLIMIT_MEMLOCK
        case PAL_RLIMIT_MEMLOCK:
            return RLIMIT_MEMLOCK;
#elif defined(RLIMIT_VMEM)
        case PAL_RLIMIT_MEMLOCK:
            return RLIMIT_VMEM;
#endif
#ifdef RLIMIT_NPROC
        case PAL_RLIMIT_NPROC:
            return RLIMIT_NPROC;
#endif
        case PAL_RLIMIT_NOFILE:
            return RLIMIT_NOFILE;
#if !defined(RLIMIT_RSS) || !(defined(RLIMIT_MEMLOCK) || defined(RLIMIT_VMEM)) || !defined(RLIMIT_NPROC)
        default:
            break;
#endif
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

#if defined(__USE_GNU) && !defined(__cplusplus) && !defined(TARGET_ANDROID)
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

int32_t SystemNative_GetPid(void)
{
    return getpid();
}

int32_t SystemNative_GetSid(int32_t pid)
{
    return getsid(pid);
}

void SystemNative_SysLog(SysLogPriority priority, const char* message, const char* arg1)
{
    syslog((int)(LOG_USER | priority), message, arg1);
}

int32_t SystemNative_WaitIdAnyExitedNoHangNoWait(void)
{
    siginfo_t siginfo;
    memset(&siginfo, 0, sizeof(siginfo));
    int32_t result;
    while (CheckInterrupted(result = waitid(P_ALL, 0, &siginfo, WEXITED | WNOHANG | WNOWAIT)));
    if (result == 0)
    {
        // When there are no waitable children and WNOHANG is specified,
        // waitid may return zero with si_pid unchanged.
        assert(siginfo.si_pid == 0 ||        // no waitable child
               siginfo.si_signo == SIGCHLD); // waitable child

        result = siginfo.si_pid;
    }
    else if (errno == ECHILD)
    {
        // The calling process has no existing unwaited-for child processes.
        result = 0;
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
#else
int32_t SystemNative_SchedSetAffinity(int32_t pid, intptr_t* mask)
{
    (void)pid;
    (void)mask;
    errno = ENOTSUP;
    return -1;
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
#else
int32_t SystemNative_SchedGetAffinity(int32_t pid, intptr_t* mask)
{
    (void)pid;
    (void)mask;
    errno = ENOTSUP;
    return -1;
}
#endif

char* SystemNative_GetProcessPath(void)
{
    return minipal_getexepath();
}
