// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <platformdefines.h>

#include <atomic>
#include <signal.h>
#include <stdlib.h>
#include <string.h>

// Must match the runtime's INJECT_ACTIVATION_SIGNAL.
#ifdef SIGRTMIN
#define ACTIVATION_SIGNAL SIGRTMIN
#else
#define ACTIVATION_SIGNAL SIGUSR1
#endif

static std::atomic<int> s_handlerCalls;
static bool s_installedBeforeRuntime;

static void ActivationHandler(int, siginfo_t*, void*)
{
    s_handlerCalls++;
}

// When this library is preloaded into the child process, this runs before the runtime starts. It simulates
// another runtime (e.g. a NativeAOT library) that registered a handler for the shared activation signal first.
__attribute__((constructor)) static void InstallActivationHandler()
{
    if (getenv("DOTNET_TEST_126260_CHILD") == nullptr)
    {
        return;
    }

    struct sigaction previous;
    if (sigaction(ACTIVATION_SIGNAL, nullptr, &previous) != 0 || previous.sa_handler != SIG_DFL)
    {
        return;
    }

    struct sigaction action;
    memset(&action, 0, sizeof(action));
    action.sa_sigaction = ActivationHandler;
    action.sa_flags = SA_SIGINFO | SA_RESTART;
    sigemptyset(&action.sa_mask);
    s_installedBeforeRuntime = sigaction(ACTIVATION_SIGNAL, &action, nullptr) == 0;
}

extern "C" DLL_EXPORT int IsHandlerInstalledBeforeRuntime()
{
    return s_installedBeforeRuntime ? 1 : 0;
}

extern "C" DLL_EXPORT int GetHandlerCallCount()
{
    return s_handlerCalls.load();
}
