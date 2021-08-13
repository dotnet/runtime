// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  exceptionsxs.c (exception_handling\pal_sxs\test1)
**
** Purpose: Test to make sure the PAL_EXCEPT block is executed
**          after an exception occurs in the PAL_TRY block with
**          multiple PALs in the process.
**
**
**===================================================================*/

#include <stdio.h>
#include <signal.h>
#include <errno.h>
#include <sys/ucontext.h>
#include <sys/utsname.h>
#include <unistd.h>

enum
{
    PASS = 0,
    FAIL = 1
};

extern "C" int InitializeDllTest1();
extern "C" int InitializeDllTest2();
extern "C" int DllTest1();
extern "C" int DllTest2();

bool bSignal = false;
bool bCatch = false;
bool bHandler = false;

void sigsegv_handler(int code, siginfo_t *siginfo, void *context)
{
    printf("pal_sxs test1: signal handler called\n");
    bHandler = true;                                // Mark that the signal handler was executed

    if (!bSignal)
    {
        printf("ERROR: executed signal handler NOT from try/catch\n");
        _exit(FAIL);
    }

    // Validate that the faulting address is correct; the contents of "p" (0x33000).
    if (siginfo->si_addr != (void *)0x33000)
    {
        printf("ERROR: signal handler faulting address != 0x33000\n");
        _exit(FAIL);
    }

    // Unmask signal so we can receive it again
    sigset_t signal_set;
    sigemptyset(&signal_set);
    sigaddset(&signal_set, SIGSEGV);
    if (-1 == sigprocmask(SIG_UNBLOCK, &signal_set, NULL))
    {
        printf("ERROR: sigprocmask failed; error is %d\n", errno);
        _exit(FAIL);
    } 

    printf("Signal chaining PASSED\n");
    _exit(PASS);
}

int main(int argc, char *argv[])
{
    struct sigaction newAction;
    struct sigaction oldAction;
    newAction.sa_flags = SA_SIGINFO | SA_RESTART;
    newAction.sa_handler = NULL;
    newAction.sa_sigaction = sigsegv_handler;
    sigemptyset(&newAction.sa_mask);

    if (-1 == sigaction(SIGSEGV, &newAction, &oldAction))
    {
        printf("ERROR: sigaction failed; error is %d\n", errno);
        return FAIL;
    }

    printf("PAL_SXS test1 SIGSEGV handler %p\n", oldAction.sa_sigaction);

    if (0 != InitializeDllTest1())
    {
        return FAIL;
    }

    if (0 != InitializeDllTest2())
    {
        return FAIL;
    }

    // Test catching exceptions in other PAL instances
    DllTest2();
    DllTest1();
    DllTest2();

    if (bHandler)
    {
        printf("ERROR: signal handler called by PAL sxs tests\n");
        return FAIL;
    }

    printf("Starting PAL_SXS test1 signal chaining\n");

    bSignal = true;

    volatile int* p = (volatile int *)0x33000; // Invalid pointer
    *p = 3;                                 // Causes an access violation exception

    printf("ERROR: code was executed after the access violation.\n");
    return FAIL;
}
