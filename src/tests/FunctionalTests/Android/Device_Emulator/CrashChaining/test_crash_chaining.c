// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <setjmp.h>
#include <signal.h>
#include <string.h>
#include <android/log.h>

#define LOG_ERROR(fmt, ...) __android_log_print(ANDROID_LOG_ERROR, "DOTNET", fmt, ##__VA_ARGS__)

int test_crash_chaining(void);

/*
 * Test for crash chaining: verify that a secondary signal raised from a
 * chained crash handler reaches the handler installed before the runtime.
 *
 * Strategy:
 *   1. Install SIGSEGV and SIGABRT handlers before the runtime initializes.
 *   2. Trigger a SIGSEGV from a non-JIT native function.
 *   3. The runtime chains to our SIGSEGV handler.
 *   4. Our handler raises SIGABRT and verifies that the pre-runtime handler ran.
 *
 * Returns 0 on success, non-zero on failure.
 */
static volatile sig_atomic_t g_test_crash_chain_result = -1;
static volatile sig_atomic_t g_sigabrt_handler_called = 0;
static sigjmp_buf g_test_jmpbuf;

__attribute__((noinline))
static void do_test_crash(void)
{
    volatile int *ptr = 0;
    *ptr = 42;
}

static void
test_sigabrt_handler(int signum)
{
    (void)signum;
    g_sigabrt_handler_called = 1;
}

/**
 * SIGSEGV handler installed before the runtime. The runtime saves this
 * handler and invokes it when a native crash cannot be handled.
 */
static void
test_pre_runtime_sigsegv_handler(int signum, siginfo_t *info, void *context)
{
    (void)signum;
    (void)info;
    (void)context;

    // By the time the runtime chains to us, its crash diagnostics have already run.
    // Raise SIGABRT to verify that its previously installed handler is still chained.
    g_sigabrt_handler_called = 0;
    raise(SIGABRT);
    g_test_crash_chain_result = g_sigabrt_handler_called ? 0 : 1;

    siglongjmp(g_test_jmpbuf, 1);
}

__attribute__((constructor))
static void install_pre_runtime_handler(void)
{
    struct sigaction sa;
    memset(&sa, 0, sizeof(sa));
    sa.sa_sigaction = test_pre_runtime_sigsegv_handler;
    sa.sa_flags = SA_SIGINFO;
    sigemptyset(&sa.sa_mask);
    sigaction(SIGSEGV, &sa, NULL);

    struct sigaction sa_abrt;
    memset(&sa_abrt, 0, sizeof(sa_abrt));
    sa_abrt.sa_handler = test_sigabrt_handler;
    sigemptyset(&sa_abrt.sa_mask);
    sigaction(SIGABRT, &sa_abrt, NULL);
}

int
test_crash_chaining(void)
{
    g_test_crash_chain_result = -1;
    if (sigsetjmp(g_test_jmpbuf, 1) == 0) {
        do_test_crash();
    }

    if (g_test_crash_chain_result == -1) {
        LOG_ERROR("test_crash_chaining: handler was not called");
        return 3;
    }

    return g_test_crash_chain_result;
}
