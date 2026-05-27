// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// WASI unistd.h wrapper — adds missing POSIX functions.

#ifndef _WASI_UNISTD_WRAPPER_H
#define _WASI_UNISTD_WRAPPER_H

#include_next <unistd.h>

#ifdef __wasi__
static inline uid_t geteuid(void) { return 0; }
static inline gid_t getegid(void) { return 0; }
static inline pid_t getsid(pid_t pid) { (void)pid; return 0; }
static inline pid_t fork(void) { return -1; }
static inline int execv(const char *path, char *const argv[]) { (void)path; (void)argv; return -1; }
static inline int execve(const char *path, char *const argv[], char *const envp[]) { (void)path; (void)argv; (void)envp; return -1; }
static inline int pipe(int pipefd[2]) { (void)pipefd; return -1; }

#ifndef PIPE_BUF
#define PIPE_BUF 4096
#endif

#ifndef SCHED_OTHER
#define SCHED_OTHER 0
#endif
#endif // __wasi__

#endif // _WASI_UNISTD_WRAPPER_H
