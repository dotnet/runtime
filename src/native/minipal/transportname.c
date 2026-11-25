// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <minipal/transportname.h>
#include <assert.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>

#ifdef __APPLE__
#include <pwd.h>
#include <sys/sysctl.h>
#define APPLICATION_CONTAINER_BASE_PATH_SUFFIX "/Library/Group Containers/"
#endif

#ifdef __NetBSD__
#include <sys/param.h>
#include <sys/sysctl.h>
#include <kvm.h>
#endif

#ifdef __FreeBSD__
#include <sys/types.h>
#include <sys/sysctl.h>
#include <sys/user.h>
#endif

#ifdef __HAIKU__
#include <OS.h>
#endif

#ifdef __ANDROID__
// On Android, "/tmp/" doesn't exist; temporary files go to /data/local/tmp/.
#define TEMP_DIRECTORY_PATH "/data/local/tmp/"
#else
#define TEMP_DIRECTORY_PATH "/tmp/"
#endif

// The directory limit the debugger and diagnostics tools have always used (MAX_PATH).
#define MAX_TRANSPORT_DIRECTORY_LENGTH 260

bool minipal_get_process_id_disambiguation_key(uint32_t pid, uint64_t* key)
{
    assert(key != NULL);
    *key = 0;

#if defined(__APPLE__) || defined(__FreeBSD__)
    // The process start time in seconds since the Unix epoch.
    struct kinfo_proc info;
    memset(&info, 0, sizeof(info));
    size_t size = sizeof(info);
    int mib[4] = { CTL_KERN, KERN_PROC, KERN_PROC_PID, (int)pid };
    if (sysctl(mib, sizeof(mib) / sizeof(*mib), &info, &size, NULL, 0) != 0)
    {
        assert(!"Failed to get start time of a process.");
        return false;
    }

#if defined(__APPLE__)
    *key = (uint64_t)info.kp_proc.p_starttime.tv_sec;
#else
    *key = (uint64_t)info.ki_start.tv_sec;
#endif
    return true;

#elif defined(__NetBSD__)
    // The process start time in seconds since the Unix epoch.
    kvm_t* kd = kvm_open(NULL, NULL, NULL, KVM_NO_FILES, "kvm_open");
    if (kd == NULL)
    {
        assert(!"Failed to get start time of a process.");
        return false;
    }

    int cnt;
    struct kinfo_proc2* info = kvm_getproc2(kd, KERN_PROC_PID, (int)pid, sizeof(struct kinfo_proc2), &cnt);
    if (info == NULL || cnt < 1)
    {
        kvm_close(kd);
        assert(!"Failed to get start time of a process.");
        return false;
    }

    *key = (uint64_t)info->p_ustart_sec;
    kvm_close(kd);
    return true;

#elif defined(__HAIKU__)
    // The process start time in microseconds since boot.
    team_info info;
    if (get_team_info((team_id)pid, &info) != B_OK)
    {
        return false;
    }

    *key = (uint64_t)info.start_time;
    return true;

#elif defined(__OpenBSD__) || defined(__EMSCRIPTEN__) || defined(__wasi__)
    // No /proc.
    (void)pid;
    return false;

#else
    // The process start time in jiffies since boot, from /proc/<pid>/stat. Platforms without it fail to open the file.
    char statFileName[64];
    snprintf(statFileName, sizeof(statFileName), "/proc/%u/stat", pid);

    FILE* statFile = fopen(statFileName, "r");
    if (statFile == NULL)
    {
        return false;
    }

    char* line = NULL;
    size_t lineLen = 0;
    if (getline(&line, &lineLen, statFile) == -1)
    {
        free(line);
        fclose(statFile);
        return false;
    }

    // The second field is the executable name in parentheses, which can contain spaces,
    // so start scanning after the last closing paren and the space after it.
    // The field formats are documented in 'man proc'.
    unsigned long long starttime;
    char* closingParen = strrchr(line, ')');
    int scanned = closingParen == NULL ? 0 : sscanf(closingParen + 2,
        "%*c %*d %*d %*d %*d %*d %*u %*u %*u %*u %*u %*u %*u %*d %*d %*d %*d %*d %*d %llu \n",
        &starttime);

    free(line);
    fclose(statFile);

    if (scanned != 1)
    {
        assert(!"Failed to parse stat file contents with sscanf.");
        return false;
    }

    *key = starttime;
    return true;
#endif
}

bool minipal_get_transport_name(char* name, size_t nameLength, const char* prefix, uint32_t pid, const char* applicationGroupId, const char* suffix)
{
    assert(name != NULL && nameLength > 0);
    *name = '\0';

    // On failure the key is 0. Anyone else making the name is expected to fail too and use 0.
    uint64_t disambiguationKey;
    minipal_get_process_id_disambiguation_key(pid, &disambiguationKey);

    char directory[MAX_TRANSPORT_DIRECTORY_LENGTH];
    int directoryLength;
#ifdef __APPLE__
    if (applicationGroupId != NULL)
    {
        if (strlen(applicationGroupId) > MINIPAL_MAX_APPLICATION_GROUP_ID_LENGTH)
        {
            return false;
        }

        // In a sandbox, IPC files must be in the application group container, /Users/<login>/Library/Group Containers/<group>/,
        // because $TMPDIR is unique to each process.
        directoryLength = snprintf(directory, sizeof(directory), "%s%s%s/",
                                   getpwuid(getuid())->pw_dir, APPLICATION_CONTAINER_BASE_PATH_SUFFIX, applicationGroupId);
    }
    else
#else
    (void)applicationGroupId;
#endif
    {
        // Must match the out-of-process clients that compute the same names (Path.GetTempPath() rules).
        const char* tmpDir = getenv("TMPDIR");
        if (tmpDir == NULL || tmpDir[0] == '\0')
        {
            tmpDir = TEMP_DIRECTORY_PATH;
        }

        const char* slash = (tmpDir[strlen(tmpDir) - 1] == '/') ? "" : "/";
        directoryLength = snprintf(directory, sizeof(directory), "%s%s", tmpDir, slash);
    }

    if (directoryLength < 0 || (size_t)directoryLength >= sizeof(directory) || (size_t)directoryLength >= nameLength)
    {
        return false;
    }

    int chars = snprintf(name, nameLength, "%s%s-%d-%llu-%s",
                         directory, prefix, (int)pid, (unsigned long long)disambiguationKey, suffix);
    if (chars < 0 || (size_t)chars >= nameLength)
    {
        *name = '\0';
        return false;
    }

    return true;
}
