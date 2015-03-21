//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================
**
** Source:  
**
** Source : test1.c
**
** Purpose: Test for ForkAndExecProcess function
**
**
**=========================================================*/

#include <palsuite.h>
#include <pal_corefx.h>
#include "string.h"

extern char** environ;

int __cdecl main(int argc, char *argv[]) {

    int childPid = -1, childStdinFd = -1, childStdoutFd = -1, childStderrFd = -1;
    FILE *childStdin = NULL, *childStdout = NULL, *childStderr = NULL;
    char* childArgv[3] = { argv[0], "child", NULL };
    int c = 0;

    // Initialize the PAL and return FAILURE if this fails
    if ((PAL_Initialize(argc, argv)) != 0)
    {
        return FAIL;
    }

    // If this is the child process, it'll have an argument.
    if (argc > 1)
    {
        // This is the child.  Receive 'a' from the parent,
        // then send back 'b' on stdout and 'c' on stderr.
        if ((c = getc(stdin)) == EOF ||
            c != 'a' ||
            fputc('b', stdout) == EOF ||
            fflush(stdout) != 0 ||
            fputc('c', stderr) == EOF ||
            fflush(stdout) != 0)
        {
            Fail("Error: Child process failed");
        }
        goto done;
    }

    // Now fork/exec the child process, with the same executable but an extra argument
    if (ForkAndExecProcess(argv[0], childArgv, environ, NULL,
                           1, 1, 1,
                           &childPid, &childStdinFd, &childStdoutFd, &childStderrFd) != 0)
    {
        Fail("Error: ForkAndExecProces failed with errno %d (%s)\n", errno, strerror(errno));
    }
    if (childPid < 0 || childStdinFd < 0 || childStdoutFd < 0 || childStderrFd < 0)
    {
        Fail("Error: ForkAndExecProcess returned childpid=%d, stdinFd=%d, stdoutFd=%d, stderrFd=%d", 
            childPid, childStdinFd, childStdoutFd, childStderrFd);
    }

    // Open files for the child's redirected stdin, stdout, and stderr
    if ((childStdin = _fdopen(childStdinFd, "w")) == NULL ||
        (childStdout = _fdopen(childStdoutFd, "r")) == NULL ||
        (childStderr = _fdopen(childStderrFd, "r")) == NULL)
    {
        Fail("Error: Opening FILE* for stdin, stdout, or stderr resulted in errno %d (%s)", 
            errno, strerror(errno));
    }

    // Send 'a' to the child
    if (fputc('a', childStdin) == EOF ||
        fflush(childStdin) != 0)
    {
        Fail("Writing to the child process failed with errno %d (%s)", errno, strerror(errno));
    }

    // Then receive 'b' from the child's stdout, then 'c' from stderr
    if ((c = getc(childStdout)) != 'b')
    {
        Fail("Received '%c' from child's stdout; expected 'b'", c);
    }
    if ((c = getc(childStderr)) != 'c')
    {
        Fail("Received '%c' from child's stderr; expected 'c'", c);
    }

done:
    PAL_Terminate();
    return PASS;
}

