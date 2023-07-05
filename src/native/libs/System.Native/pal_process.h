// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_compiler.h"
#include "pal_types.h"
#include <stdio.h>
#include <string.h>

/**
 * Used by System.Diagnostics.Process.Start to fork/exec a new process.
 *
 * This function takes the place of directly using fork and execve from managed code,
 * in order to avoid executing managed code in the child process in the window between
 * fork and execve, which is not safe.
 *
 * As would have been the case with fork/execve, a return value of 0 is success and -1
 * is failure; if failure, error information is provided in errno.
 */
PALEXPORT int32_t SystemNative_ForkAndExecProcess(
                   const char* filename,   // filename argument to execve
                   char* const argv[],     // argv argument to execve
                   char* const envp[],     // envp argument to execve
                   const char* cwd,        // path passed to chdir in child process
                   int32_t redirectStdin,  // whether to redirect standard input from the parent
                   int32_t redirectStdout, // whether to redirect standard output to the parent
                   int32_t redirectStderr, // whether to redirect standard error to the parent
                   int32_t setCredentials, // whether to set the userId and groupId for the child process
                   uint32_t userId,        // the user id under which the child process should run
                   uint32_t groupId,       // the group id under which the child process should run
                   uint32_t* groups,       // the groups under which the child process should run
                   int32_t groupsLength,   // the length of groups
                   int32_t* childPid,      // [out] the child process' id
                   int32_t* stdinFd,       // [out] if redirectStdin, the parent's fd for the child's stdin
                   int32_t* stdoutFd,      // [out] if redirectStdout, the parent's fd for the child's stdout
                   int32_t* stderrFd);     // [out] if redirectStderr, the parent's fd for the child's stderr

/************
 * The values below in the header are fixed and correct for managed callers to use forever.
 * We must never change them. The implementation must either static_assert that they are equal
 * to the native equivalent OR convert them appropriately.
 */

/**
 * These values differ from OS to OS, so make a constant contract.
 * These values apply for the current process only
 */
typedef enum
{
    PAL_RLIMIT_CPU = 0,     // CPU limit in seconds
    PAL_RLIMIT_FSIZE = 1,   // Largest file that can be created, in bytes
    PAL_RLIMIT_DATA = 2,    // Maximum size of data segment, in bytes
    PAL_RLIMIT_STACK = 3,   // Maximum size of stack segment, in bytes
    PAL_RLIMIT_CORE = 4,    // Largest core file that can be created, in bytes
    PAL_RLIMIT_AS = 5,      // Address space limit
    PAL_RLIMIT_RSS = 6,     // Largest resident set size, in bytes
    PAL_RLIMIT_MEMLOCK = 7, // Locked-in-memory address space
    PAL_RLIMIT_NPROC = 8,   // Number of processes
    PAL_RLIMIT_NOFILE = 9,  // Number of open files
} RLimitResources;

typedef enum
{
    PAL_NONE = 0,
    PAL_SIGKILL = 9, /* kill the specified process */
    PAL_SIGSTOP = 19,
} Signals;

/**
 * Constants for passing to the first parameter of syslog.
 * These are a combination of flags where the lower bits are
 * the priority and the higher bits are the facility. The lower
 * bits cannot be OR'd together; they must be OR'd with the higher bits.
 *
 * These values keep their original definition and are taken from syslog.h
 */
typedef enum
{
    // Priorities
    PAL_LOG_EMERG = 0,   /* system is unusable */
    PAL_LOG_ALERT = 1,   /* action must be taken immediately */
    PAL_LOG_CRIT = 2,    /* critical conditions */
    PAL_LOG_ERR = 3,     /* error conditions */
    PAL_LOG_WARNING = 4, /* warning conditions */
    PAL_LOG_NOTICE = 5,  /* normal but significant condition */
    PAL_LOG_INFO = 6,    /* informational */
    PAL_LOG_DEBUG = 7,   /* debug-level messages */
} SysLogPriority;

/**
 * Constants to pass into pathconf.
 *
 * Note - these differ per OS so these values are the PAL-specific
 *        values; they must be converted to the correct platform
 *        values before passing to pathconf.
 */
typedef enum
{
    PAL_PC_LINK_MAX = 1,
    PAL_PC_MAX_CANON = 2,
    PAL_PC_MAX_INPUT = 3,
    PAL_PC_NAME_MAX = 4,
    PAL_PC_PATH_MAX = 5,
    PAL_PC_PIPE_BUF = 6,
    PAL_PC_CHOWN_RESTRICTED = 7,
    PAL_PC_NO_TRUNC = 8,
    PAL_PC_VDISABLE = 9,
} PathConfName;

/**
 * Constants for passing to GetPriority and SetPriority.
 */
typedef enum
{
    PAL_PRIO_PROCESS = 0,
    PAL_PRIO_PGRP = 1,
    PAL_PRIO_USER = 2,
} PriorityWhich;

/**
 * The current and maximum resource values for the current process.
 * These values are depict the resource according to the above enum.
 */
typedef struct
{
    uint64_t CurrentLimit;
    uint64_t MaximumLimit;
} RLimit;

/**
 * The native struct is dependent on the size of a numeric type
 * so make it the largest possible value here and then we will
 * copy to native as necessary
 */
typedef struct
{
    uint64_t Bits[16]; // __CPU_SETSIZE / (8 * sizeof(int64_t))
} CpuSetBits;

/**
 * Get the current limit for the specified resource of the current process.
 * Returns 0 on success; returns -1 on failure and errno is set to the error reason.
 */
PALEXPORT int32_t SystemNative_GetRLimit(RLimitResources resourceType, RLimit* limits);

/**
 * Set the soft and hard limits for the specified resource.
 * Only a super-user can increase hard limits for the current process.
 * Returns 0 on success; returns -1 on failure and errno is set to the error reason.
 */
PALEXPORT int32_t SystemNative_SetRLimit(RLimitResources resourceType, const RLimit* limits);

/**
 * Kill the specified process (or process group) identified by the supplied pid; the
 * process or process group will be killed by the specified signal.
 * Returns 0 on success; on failure, -1 is returned and errno is set
 */
PALEXPORT int32_t SystemNative_Kill(int32_t pid, int32_t signal);

/**
 * Returns the Process ID of the current executing process.
 * This call should never fail
 */
PALEXPORT int32_t SystemNative_GetPid(void);

/**
 * Returns the sessions ID of the specified process; if 0 is passed in, returns the
 * session ID of the current process.
 * Returns a session ID on success; otherwise, returns -1 and sets errno.
 */
PALEXPORT int32_t SystemNative_GetSid(int32_t pid);

/**
 * Write a message to the system logger, which in turn writes the message to the system console, log files, etc.
 * See man 3 syslog for more info
 */
PALEXPORT void SystemNative_SysLog(SysLogPriority priority, const char* message, const char* arg1);

/**
 * Returns the pid of a terminated child without reaping it.
 *
 * 1) returns the process id of a terminated child process
 * 2) if no children are terminated, 0 is returned
 * 3) on error, -1 is returned
 */
PALEXPORT int32_t SystemNative_WaitIdAnyExitedNoHangNoWait(void);

/**
 * Reaps a terminated child.
 *
 * 1) when a child is reaped, its process id is returned
 * 2) if pid is not a child or there are no unwaited-for children, -1 is returned (errno=ECHILD)
 * 3) if the child has not yet terminated, 0 is returned
 * 4) on error, -1 is returned.
 */
PALEXPORT int32_t SystemNative_WaitPidExitedNoHang(int32_t pid, int32_t* exitCode);

/**
 * Gets the configurable limit or variable for system path or file descriptor options.
 *
 * Returns the requested variable value on success; if the variable does not have a limit, -1 is returned and errno
 * is not set; otherwise, -1 is returned and errno is set.
 */
PALEXPORT int64_t SystemNative_PathConf(const char* path, PathConfName name);

/**
 * Gets the priority (nice value) of a certain execution group.
 *
 * Returns the nice value (from -20 to 20) of the group on success; otherwise, returns -1. Unfortunately, -1 is also a
 * valid nice value, meaning we can't use that value to determine valid output or not. Errno is set on failure so
 * we need to reset errno before a call and check the value if we get -1.
 */
PALEXPORT int32_t SystemNative_GetPriority(PriorityWhich which, int32_t who);

/**
 * Sets the priority (nice value) of a certain execution group.
 *
 * Returns 0 on success; otherwise, -1 and errno is set.
 */
PALEXPORT int32_t SystemNative_SetPriority(PriorityWhich which, int32_t who, int32_t nice);

/**
 * Gets the current working directory of the currently executing process.
 */
PALEXPORT char* SystemNative_GetCwd(char* buffer, int32_t bufferSize);

/**
 * Sets the CPU affinity mask for a specified thread (or the current thread if 0).
 *
 * Returns 0 on success; otherwise, -1 is returned and errno is set
 */
PALEXPORT int32_t SystemNative_SchedSetAffinity(int32_t pid, intptr_t* mask);

/**
 * Gets the affinity mask of the specified thread (or the current thread if 0).
 *
 * Returns 0 on success; otherwise, -1 is returned and errno is set.
 */
PALEXPORT int32_t SystemNative_SchedGetAffinity(int32_t pid, intptr_t* mask);

/**
 * Returns the path of the executable that started the currently executing process,
 * resolving symbolic links. The caller is responsible for releasing the buffer.
 */
PALEXPORT char* SystemNative_GetProcessPath(void);
