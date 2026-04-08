// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// WASI signal.h wrapper — provides full POSIX signal types needed by the
// CoreCLR PAL that WASI SDK hides behind __wasilibc_unmodified_upstream.

#ifndef _WASI_SIGNAL_WRAPPER_H
#define _WASI_SIGNAL_WRAPPER_H

#include_next <signal.h>
#include <sys/types.h>
#include <pthread.h>

#ifndef SA_SIGINFO

#define SA_SIGINFO  4
#define SA_RESTART  0x10000000
#define SA_ONSTACK  0x08000000
#define SA_NODEFER  0x40000000
#define SA_RESETHAND 0x80000000

#define SIG_BLOCK     0
#define SIG_UNBLOCK   1
#define SIG_SETMASK   2

#define FPE_INTDIV 1
#define FPE_INTOVF 2
#define FPE_FLTDIV 3
#define FPE_FLTOVF 4
#define FPE_FLTUND 5
#define FPE_FLTRES 6
#define FPE_FLTINV 7
#define FPE_FLTSUB 8

#define ILL_ILLOPC 1
#define ILL_ILLOPN 2
#define ILL_ILLADR 3
#define ILL_ILLTRP 4
#define ILL_PRVOPC 5
#define ILL_PRVREG 6
#define ILL_COPROC 7
#define ILL_BADSTK 8

#define SEGV_MAPERR 1
#define SEGV_ACCERR 2

#define BUS_ADRALN 1
#define BUS_ADRERR 2
#define BUS_OBJERR 3

#ifndef SS_DISABLE
#define SS_DISABLE 2
#define SS_ONSTACK 1
#endif

union sigval {
    int sival_int;
    void *sival_ptr;
};

typedef struct {
    int si_signo;
    int si_errno;
    int si_code;
    pid_t si_pid;
    uid_t si_uid;
    void *si_addr;
    int si_status;
    union sigval si_value;
} siginfo_t;

typedef struct sigaltstack {
    void *ss_sp;
    int ss_flags;
    size_t ss_size;
} stack_t;

struct sigaction {
    union {
        void (*sa_handler)(int);
        void (*sa_sigaction)(int, siginfo_t *, void *);
    } __sa_handler;
    sigset_t sa_mask;
    int sa_flags;
    void (*sa_restorer)(void);
};
#define sa_handler   __sa_handler.sa_handler
#define sa_sigaction __sa_handler.sa_sigaction

static inline int sigaction(int sig, const struct sigaction *act, struct sigaction *oact) { (void)sig; (void)act; (void)oact; return 0; }
static inline int sigemptyset(sigset_t *set) { if (set) *set = 0; return 0; }
static inline int sigfillset(sigset_t *set) { if (set) *set = ~(sigset_t)0; return 0; }
static inline int sigaddset(sigset_t *set, int sig) { if (set) *set |= (1UL << sig); return 0; }
static inline int sigdelset(sigset_t *set, int sig) { if (set) *set &= ~(1UL << sig); return 0; }
static inline int sigprocmask(int how, const sigset_t *set, sigset_t *oset) { (void)how; (void)set; (void)oset; return 0; }
static inline int pthread_sigmask(int how, const sigset_t *set, sigset_t *oset) { (void)how; (void)set; (void)oset; return 0; }
static inline int pthread_kill(pthread_t t, int sig) { (void)t; (void)sig; return -1; }
static inline int kill(pid_t pid, int sig) { (void)pid; (void)sig; return -1; }

#endif // SA_SIGINFO


// Additional signal codes not in the base set
#ifndef SI_USER
#define SI_USER 0
#define SI_KERNEL 128
#endif

#ifndef TRAP_BRKPT
#define TRAP_BRKPT 1
#define TRAP_TRACE 2
#endif
#endif // _WASI_SIGNAL_WRAPPER_H
