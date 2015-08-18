// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma once

#include <stdint.h>

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
