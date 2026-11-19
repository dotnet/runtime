// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <pal.h>
#include <unistd.h>
#include <fcntl.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <limits.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <pal_assert.h>
#include "processdescriptor.h"

#ifdef __APPLE__
#include <pwd.h>
#include <sys/sysctl.h>
#define APPLICATION_CONTAINER_BASE_PATH_SUFFIX "/Library/Group Containers/"
#endif

#ifdef __NetBSD__
#include <sys/cdefs.h>
#include <sys/param.h>
#include <sys/sysctl.h>
#include <kvm.h>
#endif

#ifdef __FreeBSD__
#include <sys/sysctl.h>
#include <sys/user.h>
#endif

#ifdef __HAIKU__
#include <OS.h>
#endif

#ifndef __ANDROID__
#define TEMP_DIRECTORY_PATH "/tmp/"
#else
// On Android, "/tmp/" doesn't exist; temporary files should go to
// /data/local/tmp/
#define TEMP_DIRECTORY_PATH "/data/local/tmp/"
#endif

ProcessDescriptor ProcessDescriptor::FromCurrentProcess()
{
    return Create(getpid(), nullptr);
}

/*++
 Function:
  GetProcessIdDisambiguationKey

  Get a numeric value that can be used to disambiguate between processes with the same PID,
  provided that one of them is still running. The numeric value can mean different things
  on different platforms, so it should not be used for any other purpose. Under the hood,
  it is implemented based on the creation time of the process.
--*/
bool GetProcessIdDisambiguationKey(DWORD processId, UINT64 *disambiguationKey)
{
    if (disambiguationKey == nullptr)
    {
        _ASSERTE(!"disambiguationKey argument cannot be null!");
        return false;
    }

    *disambiguationKey = 0;

#if defined(__APPLE__) || defined(__FreeBSD__)

    // On OS X, we return the process start time expressed in Unix time (the number of seconds
    // since the start of the Unix epoch).
    struct kinfo_proc info = {};
    size_t size = sizeof(info);
    int mib[4] = { CTL_KERN, KERN_PROC, KERN_PROC_PID, (int)processId };
    int ret = ::sysctl(mib, sizeof(mib)/sizeof(*mib), &info, &size, nullptr, 0);

    if (ret == 0)
    {
#if defined(__APPLE__)
        timeval procStartTime = info.kp_proc.p_starttime;
#else // __FreeBSD__
        timeval procStartTime = info.ki_start;
#endif
        long secondsSinceEpoch = procStartTime.tv_sec;

        *disambiguationKey = secondsSinceEpoch;
        return true;
    }
    else
    {
        _ASSERTE(!"Failed to get start time of a process.");
        return false;
    }

#elif defined(__NetBSD__)

    // On NetBSD, we return the process start time expressed in Unix time (the number of seconds
    // since the start of the Unix epoch).
    kvm_t *kd;
    int cnt;
    struct kinfo_proc2 *info;

    kd = kvm_open(nullptr, nullptr, nullptr, KVM_NO_FILES, "kvm_open");
    if (kd == nullptr)
    {
        _ASSERTE(!"Failed to get start time of a process.");
        return false;
    }

    info = kvm_getproc2(kd, KERN_PROC_PID, processId, sizeof(struct kinfo_proc2), &cnt);
    if (info == nullptr || cnt < 1)
    {
        kvm_close(kd);
        _ASSERTE(!"Failed to get start time of a process.");
        return false;
    }

    kvm_close(kd);

    long secondsSinceEpoch = info->p_ustart_sec;
    *disambiguationKey = secondsSinceEpoch;

    return true;

#elif defined(__HAIKU__)

    // On Haiku, we return the process start time expressed in microseconds since boot time.

    team_info info;

    if (get_team_info(processId, &info) == B_OK)
    {
        *disambiguationKey = info.start_time;
        return true;
    }
    else
    {
        return false;
    }

#elif HAVE_PROCFS_STAT

    // Here we read /proc/<pid>/stat file to get the start time for the process.
    // We return this value (which is expressed in jiffies since boot time).

    // Making something like: /proc/123/stat
    char statFileName[64];

    int chars = snprintf(statFileName, sizeof(statFileName), "/proc/%d/stat", processId);
    _ASSERTE(chars > 0 && chars <= (int)sizeof(statFileName));
    (void)chars;

    FILE *statFile = fopen(statFileName, "r");
    if (statFile == nullptr)
    {
        return false;
    }

    char *line = nullptr;
    size_t lineLen = 0;
    if (getline(&line, &lineLen, statFile) == -1)
    {
        free(line);
        fclose(statFile);
        return false;
    }

    unsigned long long starttime;

    // According to `man proc`, the second field in the stat file is the filename of the executable,
    // in parentheses. Tokenizing the stat file using spaces as separators breaks when that name
    // has spaces in it, so we start using sscanf after skipping everything up to and including the
    // last closing paren and the space after it.
    char *scanStartPosition = strrchr(line, ')') + 2;

    // All the format specifiers for the fields in the stat file are provided by 'man proc'.
    int sscanfRet = sscanf(scanStartPosition,
        "%*c %*d %*d %*d %*d %*d %*u %*u %*u %*u %*u %*u %*u %*d %*d %*d %*d %*d %*d %llu \n",
         &starttime);

    free(line);
    fclose(statFile);

    if (sscanfRet != 1)
    {
        _ASSERTE(!"Failed to parse stat file contents with sscanf.");
        return false;
    }

    *disambiguationKey = starttime;
    return true;

#else
    // If this is not OS X and we don't have /proc, we just return false.
    return false;
#endif
}

void GetTransportName(
    const unsigned int maxTransportNameLength,
    char *name,
    const char *prefix,
    DWORD id,
    const char *applicationGroupId,
    const char *suffix)
{
    *name = '\0';
    UINT64 disambiguationKey = 0;
    char directory[MAX_PATH];
    int directoryLength;
    bool ret = GetProcessIdDisambiguationKey(id, &disambiguationKey);

    // If GetProcessIdDisambiguationKey failed for some reason, it should set the value
    // to 0. We expect that anyone else making the pipe name will also fail and thus will
    // also try to use 0 as the value.
    _ASSERTE(ret || disambiguationKey == 0);
#ifdef __APPLE__
    if (nullptr != applicationGroupId)
    {
        // Verify the length of the application group ID
        if (strlen(applicationGroupId) > MAX_APPLICATION_GROUP_ID_LENGTH)
        {
            return;
        }

        // In sandbox, all IPC files (locks, pipes) should be written to the application group
        // container. $TMPDIR will be unique for each process and cannot
        // be used for IPC between two different processes.
        // The application group container folder is defined as:
        // /user/{loginname}/Library/Group Containers/{AppGroupId}/
        directoryLength = snprintf(directory, sizeof(directory), "%s%s%s/",
                                   getpwuid(getuid())->pw_dir, APPLICATION_CONTAINER_BASE_PATH_SUFFIX, applicationGroupId);
    }
    else
#endif // __APPLE__
    {
        // Must match the out-of-process clients that compute the same names (Path.GetTempPath() rules).
        const char* tmpDir = getenv("TMPDIR");
        if (tmpDir == nullptr || tmpDir[0] == '\0')
        {
            tmpDir = TEMP_DIRECTORY_PATH;
        }

        const char* slash = (tmpDir[strlen(tmpDir) - 1] == '/') ? "" : "/";
        directoryLength = snprintf(directory, sizeof(directory), "%s%s", tmpDir, slash);
    }

    // Verify the size of the path won't exceed maximum allowed size
    if (directoryLength < 0 || (size_t)directoryLength >= sizeof(directory) || (unsigned int)directoryLength >= maxTransportNameLength)
    {
        return;
    }

    int chars = snprintf(name, maxTransportNameLength, "%s%s-%d-%llu-%s",
                         directory, prefix, (int)id, (unsigned long long)disambiguationKey, suffix);
    if (chars < 0 || (unsigned int)chars >= maxTransportNameLength)
    {
        *name = '\0';
    }
}

void GetTransportPipeName(
    char *name,
    DWORD id,
    const char *applicationGroupId,
    const char *suffix)
{
    GetTransportName(
        MAX_DEBUGGER_TRANSPORT_PIPE_NAME_LENGTH,
        name,
        "clr-debug-pipe",
        id,
        applicationGroupId,
        suffix);
}
