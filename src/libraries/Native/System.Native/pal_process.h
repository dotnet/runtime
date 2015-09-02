// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma once

#include <stdint.h>
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
extern "C"
int32_t ForkAndExecProcess(
    const char* filename,    // filename argument to execve
    char* const argv[],      // argv argument to execve
    char* const envp[],      // envp argument to execve
    const char* cwd,         // path passed to chdir in child process
    int32_t redirectStdin,    // whether to redirect standard input from the parent
    int32_t redirectStdout,   // whether to redirect standard output to the parent
    int32_t redirectStderr,   // whether to redirect standard error to the parent
    int32_t* childPid,        // [out] the child process' id
    int32_t* stdinFd,         // [out] if redirectStdin, the parent's fd for the child's stdin
    int32_t* stdoutFd,        // [out] if redirectStdout, the parent's fd for the child's stdout
    int32_t* stderrFd);       // [out] if redirectStderr, the parent's fd for the child's stderr


/**
 * These values differ from OS to OS, so make a constant contract.
 * These values apply for the current process only
 */
enum class RLimitResources : int32_t
{
    PAL_RLIMIT_CPU          = 0,        // CPU limit in seconds
    PAL_RLIMIT_FSIZE        = 1,        // Largest file that can be created, in bytes
    PAL_RLIMIT_DATA         = 2,        // Maximum size of data segment, in bytes
    PAL_RLIMIT_STACK        = 3,        // Maximum size of stack segment, in bytes
    PAL_RLIMIT_CORE         = 4,        // Largest core file that can be created, in bytes
    PAL_RLIMIT_AS           = 5,        // Address space limit
    PAL_RLIMIT_RSS          = 6,        // Largest resident set size, in bytes
    PAL_RLIMIT_MEMLOCK      = 7,        // Locked-in-memory address space
    PAL_RLIMIT_NPROC        = 8,        // Number of processes
    PAL_RLIMIT_NOFILE       = 9,        // Number of open files
};

enum class Signals : int32_t
{
    PAL_None = 0,       /* error check and don't send signal */
    PAL_SIGKILL = 9,    /* kill the specified process */
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
extern "C"
int32_t GetRLimit(
    RLimitResources     resourceType, 
    RLimit*             limits);

/**
 * Set the soft and hard limits for the specified resource. 
 * Only a super-user can increase hard limits for the current process.
 * Returns 0 on success; returns -1 on failure and errno is set to the error reason.
 */
extern "C"
int32_t SetRLimit(
    RLimitResources resourceType,
    const RLimit*   limits);

/**
 * Kill the specified process (or process group) identified by the supplied pid; the 
 * process or process group will be killed by the specified signal.
 * Returns 0 on success; on failure, -1 is returned and errno is set
 */
extern "C"
int32_t Kill(int32_t pid, int32_t signal);

/**
 * Returns the Process ID of the current executing process. 
 * This call should never fail
 */
extern "C"
int32_t GetPid();

/**
 * Returns the sessions ID of the specified process; if 0 is passed in, returns the 
 * session ID of the current process.
 * Returns a session ID on success; otherwise, returns -1 and sets errno.
 */
extern "C"
int32_t GetSid(int32_t pid);
