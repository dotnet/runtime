// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_config.h"
#include "pal_process.h"
#include "pal_io.h"
#include "pal_utilities.h"

#include <assert.h>
#include <errno.h>
#include <limits.h>
#include <signal.h>
#include <stdlib.h>
#include <sys/resource.h>
#include <sys/types.h>
#include <unistd.h>

#include <minipal/getexepath.h>

int32_t SystemNative_ForkAndExecProcess(const char* filename,
                                      char* const argv[],
                                      char* const envp[],
                                      const char* cwd,
                                      int32_t redirectStdin,
                                      int32_t redirectStdout,
                                      int32_t redirectStderr,
                                      int32_t setCredentials,
                                      uint32_t userId,
                                      uint32_t groupId,
                                      uint32_t* groups,
                                      int32_t groupsLength,
                                      int32_t* childPid,
                                      int32_t* stdinFd,
                                      int32_t* stdoutFd,
                                      int32_t* stderrFd)
{
    (void)filename; // unused
    (void)argv; // unused
    (void)envp; // unused
    (void)cwd; // unused
    (void)redirectStdin; // unused
    (void)redirectStdout; // unused
    (void)redirectStderr; // unused
    (void)setCredentials; // unused
    (void)userId; // unused
    (void)groupId; // unused
    (void)groups; // unused
    (void)groupsLength; // unused
    (void)childPid; // unused
    (void)stdinFd; // unused
    (void)stdoutFd; // unused
    (void)stderrFd; // unused
    return -1;
}

int32_t SystemNative_GetRLimit(RLimitResources resourceType, RLimit* limits)
{
    assert(limits != NULL);
    int result = -1;
    memset(limits, 0, sizeof(RLimit));
    (void)resourceType; // unused
    return result;
}

int32_t SystemNative_SetRLimit(RLimitResources resourceType, const RLimit* limits)
{
    assert(limits != NULL);
    (void)resourceType; // unused
    (void)limits; // unused
    return -1;
}

int32_t SystemNative_Kill(int32_t pid, int32_t signal)
{
    return -1;
}

int32_t SystemNative_GetPid(void)
{
    return -1;
}

int32_t SystemNative_GetSid(int32_t pid)
{
    return -1;
}

__attribute__((noreturn))
void SystemNative_SysLog(SysLogPriority priority, const char* message, const char* arg1)
{
    assert_msg(false, "Not supported on WASI", 0);
    (void)priority; // unused
    (void)message; // unused
    (void)arg1; // unused
}

int32_t SystemNative_WaitIdAnyExitedNoHangNoWait(void)
{
    return -1;
}

int32_t SystemNative_WaitPidExitedNoHang(int32_t pid, int32_t* exitCode)
{
    (void)pid; // unused
    (void)exitCode; // unused
    return -1;
}

int64_t SystemNative_PathConf(const char* path, PathConfName name)
{
    (void)path; // unused
    (void)name; // unused
    return -1;
}

int32_t SystemNative_GetPriority(PriorityWhich which, int32_t who)
{
    (void)which; // unused
    (void)who; // unused
    return -1;
}

int32_t SystemNative_SetPriority(PriorityWhich which, int32_t who, int32_t nice)
{
    (void)which; // unused
    (void)who; // unused
    (void)nice; // unused
    return -1;
}

char* SystemNative_GetCwd(char* buffer, int32_t bufferSize)
{
    assert(bufferSize >= 0);

    if (bufferSize < 0)
    {
        errno = EINVAL;
        return NULL;
    }

    return getcwd(buffer, Int32ToSizeT(bufferSize));
}

int32_t SystemNative_SchedSetAffinity(int32_t pid, intptr_t* mask)
{
    (void)pid;
    (void)mask;
    errno = ENOTSUP;
    return -1;
}

int32_t SystemNative_SchedGetAffinity(int32_t pid, intptr_t* mask)
{
    (void)pid;
    (void)mask;
    errno = ENOTSUP;
    return -1;
}

char* SystemNative_GetProcessPath(void)
{
    return minipal_getexepath();
}
