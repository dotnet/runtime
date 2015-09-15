// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma once

#include "pal_types.h"
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
extern "C" int32_t
ForkAndExecProcess(const char* filename,   // filename argument to execve
                   char* const argv[],     // argv argument to execve
                   char* const envp[],     // envp argument to execve
                   const char* cwd,        // path passed to chdir in child process
                   int32_t redirectStdin,  // whether to redirect standard input from the parent
                   int32_t redirectStdout, // whether to redirect standard output to the parent
                   int32_t redirectStderr, // whether to redirect standard error to the parent
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
enum RLimitResources : int32_t
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
};

enum Signals : int32_t
{
    PAL_SIGKILL = 9, /* kill the specified process */
};

/**
 * Constants for passing to waitpid determining how waitpid behaves
 */
enum WaitPidOptions : int32_t
{
    PAL_WNOHANG = 1,   /* don't block waiting */
    PAL_WUNTRACED = 2, /* report status of stopped children */
};

/**
 * Constants for passing to the first parameter of syslog.
 * These are a combination of flags where the lower bits are
 * the priority and the higher bits are the facility. The lower
 * bits cannot be OR'd together; they must be OR'd with the higer bits.
 *
 * These values keep their original definition and are taken from syslog.h
 */
enum SysLogPriority : int32_t
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
    // Facilities
    PAL_LOG_KERN = (0 << 3),      /* kernel messages */
    PAL_LOG_USER = (1 << 3),      /* random user-level messages */
    PAL_LOG_MAIL = (2 << 3),      /* mail system */
    PAL_LOG_DAEMON = (3 << 3),    /* system daemons */
    PAL_LOG_AUTH = (4 << 3),      /* authorization messages */
    PAL_LOG_SYSLOG = (5 << 3),    /* messages generated internally by syslogd */
    PAL_LOG_LPR = (6 << 3),       /* line printer subsystem */
    PAL_LOG_NEWS = (7 << 3),      /* network news subsystem */
    PAL_LOG_UUCP = (8 << 3),      /* UUCP subsystem */
    PAL_LOG_CRON = (9 << 3),      /* clock daemon */
    PAL_LOG_AUTHPRIV = (10 << 3), /* authorization messages (private) */
    PAL_LOG_FTP = (11 << 3),      /* ftp daemon */
    // Between FTP and Local is reserved for system use
    PAL_LOG_LOCAL0 = (16 << 3), /* reserved for local use */
    PAL_LOG_LOCAL1 = (17 << 3), /* reserved for local use */
    PAL_LOG_LOCAL2 = (18 << 3), /* reserved for local use */
    PAL_LOG_LOCAL3 = (19 << 3), /* reserved for local use */
    PAL_LOG_LOCAL4 = (20 << 3), /* reserved for local use */
    PAL_LOG_LOCAL5 = (21 << 3), /* reserved for local use */
    PAL_LOG_LOCAL6 = (22 << 3), /* reserved for local use */
    PAL_LOG_LOCAL7 = (23 << 3), /* reserved for local use */
};

/**
 * Constants to pass into pathconf.
 *
 * Note - these differ per OS so these values are the PAL-specific
 *        values; they must be converted to the correct platform
 *        values before passing to pathconf.
 */
enum PathConfName : int32_t
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
};

/**
 * Constants for passing to GetPriority and SetPriority.
 */
enum PriorityWhich : int32_t
{
    PAL_PRIO_PROCESS = 0,
    PAL_PRIO_PGRP = 1,
    PAL_PRIO_USER = 2,
};

/**
 * The current and maximum resource values for the current process.
 * These values are depict the resource according to the above enum.
 */
struct RLimit
{
    uint64_t CurrentLimit;
    uint64_t MaximumLimit;
};

/**
 * Get the current limit for the specified resource of the current process.
 * Returns 0 on success; returns -1 on failure and errno is set to the error reason.
 */
extern "C" int32_t GetRLimit(RLimitResources resourceType, RLimit* limits);

/**
 * Set the soft and hard limits for the specified resource.
 * Only a super-user can increase hard limits for the current process.
 * Returns 0 on success; returns -1 on failure and errno is set to the error reason.
 */
extern "C" int32_t SetRLimit(RLimitResources resourceType, const RLimit* limits);

/**
 * Kill the specified process (or process group) identified by the supplied pid; the
 * process or process group will be killed by the specified signal.
 * Returns 0 on success; on failure, -1 is returned and errno is set
 */
extern "C" int32_t Kill(int32_t pid, int32_t signal);

/**
 * Returns the Process ID of the current executing process.
 * This call should never fail
 */
extern "C" int32_t GetPid();

/**
 * Returns the sessions ID of the specified process; if 0 is passed in, returns the
 * session ID of the current process.
 * Returns a session ID on success; otherwise, returns -1 and sets errno.
 */
extern "C" int32_t GetSid(int32_t pid);

/**
 * Write a message to the system logger, which in turn writes the message to the system console, log files, etc.
 * See man 3 syslog for more info
 */
extern "C" void SysLog(SysLogPriority priority, const char* message, const char* arg1);

/**
 * Waits for child process(s) or gathers resource utilization information about child processes
 *
 * The return value from WaitPid can very greatly.
 * 1) returns the process id of a terminating or stopped child process
 * 2) if no children are waiting, -1 is returned and errno is set to ECHILD
 * 3) if WNOHANG is specified and there are no stopped or exited children, 0 is returned
 * 4) on error, -1 is returned and errno is set
 */
extern "C" int32_t WaitPid(int32_t pid, int32_t* status, WaitPidOptions options);

/**
 * The four functions below are wrappers around the platform-specific macros of the same name.
 */
extern "C" int32_t WExitStatus(int32_t status);

extern "C" int32_t WIfExited(int32_t status);

extern "C" int32_t WIfSignaled(int32_t status);

extern "C" int32_t WTermSig(int32_t status);

/**
 * Gets the configurable limit or variable for system path or file descriptor options.
 *
 * Returns the requested variable value on success; if the variable does not have a limit, -1 is returned and errno
 * is not set; otherwise, -1 is returned and errno is set.
 */
extern "C" int64_t PathConf(const char* path, PathConfName name);

/**
 * Gets the current (or default, on failure) Maximum Path allowed by the system.
 *
 * This is called out explicitly, rather than using PathConf, since the default value changes depending on the platform.
 */
extern "C" int64_t GetMaximumPath();

/**
 * Gets the priority (nice value) of a certain execution group.
 *
 * Returns the nice value (from -20 to 20) of the group on success; otherwise, returns -1. Unfortunately, -1 is also a
 * valid nice value, meaning we can't use that value to determine valid output or not. Errno is set on failure so 
 * we need to reset errno before a call and check the value if we get -1.
 */
extern "C" int32_t GetPriority(PriorityWhich which, int32_t who);

/**
 * Sets the priority (nice value) of a certain execution group.
 *
 * Returns 0 on success; otherwise, -1 and errno is set.
 */
extern "C" int32_t SetPriority(PriorityWhich which, int32_t who, int32_t nice);

/**
 * Gets the current working directory of the currently executing process.
 */
extern "C" char* GetCwd(char* buffer, int32_t bufferSize);
