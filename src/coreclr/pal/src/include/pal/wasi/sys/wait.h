// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Stub sys/wait.h for WASI

#ifndef _WASI_SYS_WAIT_H
#define _WASI_SYS_WAIT_H

#include <sys/types.h>

#define WNOHANG    1
#define WUNTRACED  2

#define WIFEXITED(x)    (1)
#define WEXITSTATUS(x)  (0)
#define WIFSIGNALED(x)  (0)
#define WTERMSIG(x)     (0)
#define WIFSTOPPED(x)   (0)
#define WSTOPSIG(x)     (0)

static inline pid_t waitpid(pid_t pid, int *status, int options) { (void)pid; (void)status; (void)options; return -1; }
static inline pid_t wait(int *status) { (void)status; return -1; }

#endif // _WASI_SYS_WAIT_H
