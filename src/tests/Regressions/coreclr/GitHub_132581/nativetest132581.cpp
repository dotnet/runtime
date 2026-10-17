// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <platformdefines.h>

#include <errno.h>
#include <signal.h>
#include <stdlib.h>
#include <string.h>
#include <sys/wait.h>
#include <unistd.h>

static void SignalHandler(int, siginfo_t*, void*)
{
}

extern "C" DLL_EXPORT int InstallSignalHandlerAndExec(const char* executable, const char* managedAssembly)
{
    struct sigaction action;
    memset(&action, 0, sizeof(action));
    action.sa_sigaction = SignalHandler;
    action.sa_flags = SA_SIGINFO | SA_RESTART;
    sigemptyset(&action.sa_mask);

    if (sigaction(SIGUSR1, &action, nullptr) != 0)
    {
        return errno;
    }

    if (setenv("DOTNET_TEST_132581_CHILD", "1", 1) != 0)
    {
        return errno;
    }

    char* arguments[4];
    int index = 0;
    arguments[index++] = const_cast<char*>(executable);
    if (managedAssembly[0] != '\0')
    {
        arguments[index++] = const_cast<char*>(managedAssembly);
    }
    arguments[index++] = const_cast<char*>("--child");
    arguments[index] = nullptr;

    execv(executable, arguments);
    return errno;
}

extern "C" DLL_EXPORT int SendSignalFromChildProcess()
{
    pid_t child = fork();
    if (child == -1)
    {
        return errno;
    }

    if (child == 0)
    {
        int result = kill(getppid(), SIGUSR1);
        _exit(result == 0 ? 0 : 1);
    }

    int status;
    pid_t result;
    do
    {
        result = waitpid(child, &status, 0);
    }
    while ((result == -1) && (errno == EINTR));

    return (result == child) && WIFEXITED(status) && (WEXITSTATUS(status) == 0) ? 0 : -1;
}
