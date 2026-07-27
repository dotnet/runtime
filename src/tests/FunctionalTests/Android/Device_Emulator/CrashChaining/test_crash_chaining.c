// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <setjmp.h>
#include <signal.h>
#include <string.h>
#include <android/log.h>

#define LOG_ERROR(fmt, ...) __android_log_print(ANDROID_LOG_ERROR, "DOTNET", fmt, ##__VA_ARGS__)

int test_crash_chaining(void);

/*
 * Test for crash chaining: verify that mono_handle_native_crash does not
 * reset SIGABRT to SIG_DFL when crash_chaining is enabled.
 *
 * Strategy:
 *   1. Install pre-Mono SIGSEGV and SIGABRT handlers before mono_jit_init.
 *   2. Trigger a SIGSEGV from a non-JIT native function.
 *   3. Mono chains to our SIGSEGV handler (because signal_chaining is enabled).
 *   4. Our handler checks that SIGABRT was restored to the pre-Mono handler.
 *
 * Returns 0 on success, non-zero on failure.
 */
static volatile sig_atomic_t g_test_crash_chain_result = -1;
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
}

/**
 * Pre-Mono SIGSEGV handler installed before mono_jit_init. Mono's
 * signal chaining saves this handler and calls it via mono_chain_signal
 * when a native crash occurs.
 *
 * After Mono's crash diagnostics (mono_handle_native_crash) run, this
 * handler verifies that SIGABRT was restored to the pre-Mono handler.
 * Without the fix, mono_handle_native_crash unconditionally resets
 * SIGABRT to SIG_DFL. With the fix, SIGABRT is restored to the
 * pre-Mono handler (test_sigabrt_handler).
 */
static void
test_pre_mono_sigsegv_handler(int signum, siginfo_t *info, void *context)
{
    (void)signum;
    (void)info;
    (void)context;

    // By the time Mono chains to us, mono_handle_native_crash has
    // already run. Check if SIGABRT was restored to our pre-Mono handler.
    struct sigaction current_abrt;
    sigaction(SIGABRT, NULL, &current_abrt);

    g_test_crash_chain_result = (current_abrt.sa_handler == test_sigabrt_handler) ? 0 : 1;

    siglongjmp(g_test_jmpbuf, 1);
}

__attribute__((constructor))
static void install_pre_mono_handler(void)
{
    struct sigaction sa;
    memset(&sa, 0, sizeof(sa));
    sa.sa_sigaction = test_pre_mono_sigsegv_handler;
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
