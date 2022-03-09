// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <assert.h>

// These are symbols that are never used at runtime, or at least don't need to do anything for prototype apps

int sem_init(int a, int b, int c) { return 0; }
int sem_destroy(int x) { return 0; }
int sem_post(int x) { return 0; }
int sem_wait(int x) { return 0; }
int sem_trywait(int x) { return 0; }
int sem_timedwait (int *sem, const struct timespec *abs_timeout) { assert(0); return 0; }

int __errno_location() { return 0; }

void mono_log_close_syslog () { assert(0); }
void mono_log_open_syslog (const char *a, void *b) { assert(0); }
void mono_log_write_syslog (const char *a, int b, int c, const char *d) { assert(0); }

void mono_runtime_setup_stat_profiler () { assert(0); }
int mono_thread_state_init_from_handle (int *tctx, int *info, void *sigctx) { assert(0); return 0; }

void syslog(int pri, const char *fmt, int ignored) { assert (0); }
