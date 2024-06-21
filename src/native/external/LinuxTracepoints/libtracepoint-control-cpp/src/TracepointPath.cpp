// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#include <tracepoint/TracepointPath.h>

#include <errno.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <assert.h>

#include <fcntl.h>
#include <pthread.h>
#include <unistd.h>
#include <sys/stat.h>

using namespace std::string_view_literals;

static constexpr unsigned TRACING_DIR_MAX = 256;

static int
IsSpaceChar(char ch)
{
    return ch == ' ' || ch == '\t';
}

static int
IsNonSpaceChar(char ch)
{
    return ch != '\0' && !IsSpaceChar(ch);
}

static int
GetFailureErrno(void)
{
    int err = errno;
    assert(err > 0);
    if (err <= 0)
    {
        err = ENOENT;
    }

    return err;
}

static _Ret_z_ char const*
UpdateTracingDirectory(char const** pStaticTracingDir) noexcept
{
    static pthread_mutex_t staticTracingDirMutex = PTHREAD_MUTEX_INITIALIZER;
    pthread_mutex_lock(&staticTracingDirMutex);

    auto tracingDir = __atomic_load_n(pStaticTracingDir, __ATOMIC_RELAXED); // CONSUME semantics.
    if (tracingDir == nullptr)
    {
        static char staticTracingDirBuffer[TRACING_DIR_MAX + 1];

#define SYS_KERNEL_TRACING "/sys/kernel/tracing"

        struct stat eventsStat = {};
        if (!stat(SYS_KERNEL_TRACING "/events", &eventsStat) && S_ISDIR(eventsStat.st_mode))
        {
            memcpy(staticTracingDirBuffer, SYS_KERNEL_TRACING, sizeof(SYS_KERNEL_TRACING));
        }
        else
        {
            auto const mountsFile = fopen("/proc/mounts", "r");
            if (mountsFile != nullptr)
            {
                for (;;)
                {
                    char line[4097];
                    if (!fgets(line, sizeof(line), mountsFile))
                    {
                        break;
                    }

                    // line is "deviceName mountPoint fileSystem otherStuff..."

                    size_t linePos = 0;

                    // deviceName
                    while (IsNonSpaceChar(line[linePos]))
                    {
                        linePos += 1;
                    }

                    // whitespace
                    while (IsSpaceChar(line[linePos]))
                    {
                        linePos += 1;
                    }

                    // mountPoint
                    auto const mountPointBegin = linePos;
                    while (IsNonSpaceChar(line[linePos]))
                    {
                        linePos += 1;
                    }
                    auto const mountPointEnd = linePos;

                    // whitespace
                    while (IsSpaceChar(line[linePos]))
                    {
                        linePos += 1;
                    }

                    // fileSystem
                    auto const fileSystemBegin = linePos;
                    while (IsNonSpaceChar(line[linePos]))
                    {
                        linePos += 1;
                    }
                    auto const fileSystemEnd = linePos;

                    if (!IsSpaceChar(line[linePos]))
                    {
                        // Ignore line if no whitespace after fileSystem.
                        continue;
                    }

                    std::string_view const fileSystem(line + fileSystemBegin, fileSystemEnd - fileSystemBegin);
                    std::string_view pathSuffix;
                    bool keepLooking;
                    if (fileSystem == "tracefs"sv)
                    {
                        // "tracefsMountPoint"
                        pathSuffix = ""sv;
                        keepLooking = false; // prefer "tracefs" over "debugfs".
                    }
                    else if (staticTracingDirBuffer[0] == 0 &&
                        fileSystem == "debugfs"sv)
                    {
                        // "debugfsMountPoint/tracing"
                        pathSuffix = "/tracing"sv;
                        keepLooking = true; // prefer "tracefs" over "debugfs".
                    }
                    else
                    {
                        continue;
                    }

                    auto const mountPointLen = mountPointEnd - mountPointBegin;
                    auto const pathLen = mountPointLen + pathSuffix.size() + 1; // includes NUL
                    if (pathLen > sizeof(staticTracingDirBuffer))
                    {
                        continue;
                    }

                    // path = mountpoint + suffix, e.g. "/sys/kernel/tracing\0"
                    memcpy(staticTracingDirBuffer, line + mountPointBegin, mountPointLen);
                    memcpy(staticTracingDirBuffer + mountPointLen, pathSuffix.data(), pathSuffix.size() + 1); // includes NUL

                    if (!keepLooking)
                    {
                        break;
                    }
                }

                fclose(mountsFile);
            }
        }

        tracingDir = staticTracingDirBuffer;
        __atomic_store_n(pStaticTracingDir, tracingDir, __ATOMIC_RELEASE); // CONSUME semantics.
    }

    pthread_mutex_unlock(&staticTracingDirMutex);
    return tracingDir;
}

_Ret_z_ char const*
tracepoint_control::GetTracingDirectory() noexcept
{
    static char const* staticTracingDir = nullptr;

    auto tracingDir = __atomic_load_n(&staticTracingDir, __ATOMIC_RELAXED); // CONSUME semantics.
    if (tracingDir == nullptr)
    {
        tracingDir = UpdateTracingDirectory(&staticTracingDir);
    }

    return tracingDir;
}

static _Success_(return >= 0) int
UpdateUserEventsDataFile(int* pStaticFile) noexcept
{
    int newFileOrError;

    if (auto const tracingDir = tracepoint_control::GetTracingDirectory();
        tracingDir[0] == 0)
    {
        // Unable to find the "/.../tracing" directory.
        newFileOrError = -ENOTSUP;
    }
    else
    {
#define USER_EVENTS_DATA "/user_events_data"
        char fileName[TRACING_DIR_MAX + sizeof(USER_EVENTS_DATA)];
        auto const cchTracingDir = strlen(tracingDir);
        assert(cchTracingDir <= TRACING_DIR_MAX);
        memcpy(fileName, tracingDir, cchTracingDir);
        memcpy(fileName + cchTracingDir, USER_EVENTS_DATA, sizeof(USER_EVENTS_DATA));

        newFileOrError = open(fileName, O_WRONLY);
        if (0 > newFileOrError)
        {
            newFileOrError = -GetFailureErrno();
        }
    }

    int oldFileOrError = -EAGAIN;
    for (;;)
    {
        if (__atomic_compare_exchange_n(
            pStaticFile,
            &oldFileOrError,
            newFileOrError,
            0,
            __ATOMIC_RELAXED,
            __ATOMIC_RELAXED))
        {
            // The cmpxchg set *pStaticFile = newFileOrError.
            return newFileOrError;
        }

        // The cmpxchg set oldFileOrError = *pStaticFile.

        if (oldFileOrError >= 0 || newFileOrError < 0)
        {
            // Prefer the existing contents of pStaticFile.
            if (newFileOrError >= 0)
            {
                close(newFileOrError);
            }

            return oldFileOrError;
        }
    }
}

_Success_(return >= 0) int
tracepoint_control::GetUserEventsDataFile() noexcept
{
    static int staticFileOrError = -EAGAIN; // Intentionally leaked.
    int fileOrError = __atomic_load_n(&staticFileOrError, __ATOMIC_RELAXED);
    return fileOrError != -EAGAIN
        ? fileOrError
        : UpdateUserEventsDataFile(&staticFileOrError);
}

_Success_(return == 0) int
tracepoint_control::AppendTracingFile(
    std::vector<char>& dest,
    _In_z_ char const* fileName) noexcept
{
    int error;
    auto const destOldSize = dest.size();

    if (auto const file = fopen(fileName, "r");
        file == nullptr)
    {
        error = errno;
    }
    else
    {
        // Usually a special file, so don't try to seek or stat to get file size.
        try
        {
            auto pos = destOldSize;
            for (;;)
            {
                dest.resize(dest.size() + 512);

                auto const readSize = fread(dest.data() + pos, 1, dest.size() - pos, file);
                pos += readSize;

                if (pos != dest.size())
                {
                    if (feof(file))
                    {
                        dest.resize(pos);
                        error = 0;
                        break;
                    }
                    else if (ferror(file))
                    {
                        dest.resize(destOldSize);
                        error = EIO;
                        break;
                    }
                }
            }
        }
        catch (...)
        {
            dest.resize(destOldSize);
            error = ENOMEM;
        }

        fclose(file);
    }

    return error;
}

_Success_(return == 0) int
tracepoint_control::AppendTracingFormatFile(
    std::vector<char>& dest,
    std::string_view systemName,
    std::string_view eventName) noexcept
{
    int error;

    if (systemName.empty() ||
        systemName.find_first_of("/.\0"sv) != systemName.npos ||
        eventName.empty() ||
        eventName.find_first_of("/.\0"sv) != eventName.npos)
    {
        // Invalid systemName or eventName parameter.
        error = EINVAL;
    }
    else if (
        auto const tracingDir = GetTracingDirectory();
        tracingDir[0] == 0)
    {
        // Unable to find the "/.../tracing" directory.
        error = ENOTSUP;
    }
    else
    {
        char fileName[TRACING_DIR_MAX + 256];
        unsigned const pathLen = snprintf(fileName, sizeof(fileName), "%s/events/%.*s/%.*s/format",
            tracingDir,
            (unsigned)systemName.size(), systemName.data(),
            (unsigned)eventName.size(), eventName.data());
        if (pathLen >= sizeof(fileName))
        {
            // tracingDirectory + systemName + eventName too long.
            error = E2BIG;
        }
        else
        {
            error = AppendTracingFile(dest, fileName);
        }
    }

    return error;
}
