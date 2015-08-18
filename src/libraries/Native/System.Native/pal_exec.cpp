// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "../config.h"
#include "pal_exec.h"

#include <assert.h>
#include <errno.h>
#include <stdlib.h>
#include <unistd.h>

enum
{
    READ_END_OF_PIPE = 0,
    WRITE_END_OF_PIPE = 1,
};
    
static void CloseIfOpen(int fd)
{
    // Ignoring errors from close is a deliberate choice and we musn't
    // let close during cleanup on a failure path disturb errno.
    if (fd >= 0)
    {
        int priorErrno = errno;
        close(fd);
        errno = priorErrno;
    }
}

extern "C"
int32_t ForkAndExecProcess(
    const char* filename,
    char* const argv[],
    char* const envp[],
    const char* cwd,
    int32_t redirectStdin,
    int32_t redirectStdout,
    int32_t redirectStderr,
    int32_t* childPid,
    int32_t* stdinFd,
    int32_t* stdoutFd,
    int32_t* stderrFd)
{
    int success = true;
    int stdinFds[2] = { -1, -1 }, stdoutFds[2] = { -1, -1 }, stderrFds[2] = { -1, -1 };
    int processId = -1;

    // Validate arguments
    if (nullptr == filename || nullptr == argv || nullptr == envp ||
        nullptr == stdinFd || nullptr == stdoutFd || nullptr == stderrFd ||
        nullptr == childPid)
    {
        assert(!"null argument.");
        errno = EINVAL;
        success = false;
        goto done;
    }

    if ((redirectStdin  & ~1) != 0 ||
        (redirectStdout & ~1) != 0 ||
        (redirectStderr & ~1) != 0)
    {
        assert(!"Boolean redirect* inputs must be 0 or 1.");
        errno = EINVAL;
        success = false;
        goto done;
    }

    // Open pipes for any requests to redirect stdin/stdout/stderr
    if ((redirectStdin  && pipe(stdinFds)  != 0) ||
        (redirectStdout && pipe(stdoutFds) != 0) ||
        (redirectStderr && pipe(stderrFds) != 0))
    {
        assert(!"pipe() failed.");
        success = false;
        goto done;
    }

    // Fork the child process
    if ((processId = fork()) == -1)
    {
        assert(!"fork() failed.");
        success = false;
        goto done;
    }

    if (processId == 0) // processId == 0 if this is child process
    {
        // Close the child's copy of the parent end of any open pipes
        CloseIfOpen(stdinFds[WRITE_END_OF_PIPE]);
        CloseIfOpen(stdoutFds[READ_END_OF_PIPE]);
        CloseIfOpen(stderrFds[READ_END_OF_PIPE]);

        // For any redirections that should happen, dup the pipe descriptors onto stdin/out/err.
        // Then close out the old pipe descriptrs, which we no longer need.
        if ((redirectStdin  && dup2(stdinFds[READ_END_OF_PIPE],   STDIN_FILENO)  == -1) ||
            (redirectStdout && dup2(stdoutFds[WRITE_END_OF_PIPE], STDOUT_FILENO) == -1) ||
            (redirectStderr && dup2(stderrFds[WRITE_END_OF_PIPE], STDERR_FILENO) == -1))
        {
            _exit(errno != 0 ? errno : EXIT_FAILURE);
        }
        CloseIfOpen(stdinFds[READ_END_OF_PIPE]);
        CloseIfOpen(stdoutFds[WRITE_END_OF_PIPE]);
        CloseIfOpen(stderrFds[WRITE_END_OF_PIPE]);

        // Change to the designated working directory, if one was specified
        if (nullptr != cwd && chdir(cwd) == -1)
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
    CloseIfOpen(stdinFds[READ_END_OF_PIPE]);
    CloseIfOpen(stdoutFds[WRITE_END_OF_PIPE]);
    CloseIfOpen(stderrFds[WRITE_END_OF_PIPE]);

    // If we failed, close everything else and give back error values in all out arguments.
    if (!success)
    {
        CloseIfOpen(stdinFds[WRITE_END_OF_PIPE]);
        CloseIfOpen(stdoutFds[READ_END_OF_PIPE]);
        CloseIfOpen(stderrFds[READ_END_OF_PIPE]);

        *stdinFd  = -1;
        *stdoutFd = -1;
        *stderrFd = -1;
        *childPid = -1;
    }
    
    return success ? 0 : -1;
}
