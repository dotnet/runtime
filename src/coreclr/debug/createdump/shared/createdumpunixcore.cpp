// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdumpcore.h"
#include "processreader.h"

#if defined(__arm__) || defined(__aarch64__) || defined(__loongarch64) || defined(__riscv)
long g_pageSize = 0;
#endif

bool GetDefaultDumpPath(char* buffer, size_t bufferSize)
{
    strncpy(buffer, DEFAULT_DUMP_PATH DEFAULT_DUMP_TEMPLATE, bufferSize);
    buffer[bufferSize - 1] = '\0';
    return true;
}

// Try reading the executable name from the /proc/<pid>/exe link. Prefer this name to the
// one reported by status if it is available because the status name is often truncated
void TryGetExecutableName(pid_t pid, char* name, size_t nameSize)
{
    char exePath[128];
    int written = snprintf(exePath, sizeof(exePath), "/proc/%d/exe", pid);
    if (written <= 0 || (size_t)written >= sizeof(exePath))
    {
        return;
    }

    char path[4096];
    ssize_t length = readlink(exePath, path, sizeof(path) - 1);
    if (length < 0)
    {
        return;
    }

    path[length] = '\0';

    const char* executableName = strrchr(path, '/');
    executableName = executableName != NULL ? executableName + 1 : path;

    snprintf(name, nameSize, "%s", executableName);
}

bool GetStatus(pid_t pid, pid_t* ppid, pid_t* tgid, char *name, size_t nameSize)
{
    char statusPath[128];
    int chars = snprintf(statusPath, sizeof(statusPath), "/proc/%d/status", pid);
    if (chars <= 0 || (size_t)chars >= sizeof(statusPath))
    {
        printf_error("snprintf failed building /proc/<pid>/status\n");
        return false;
    }

    FILE *statusFile = fopen(statusPath, "rb");
    if (statusFile == nullptr)
    {
        printf_error("GetStatus fopen(%s) FAILED %s (%d)\n", statusPath, strerror(errno), errno);
        return false;
    }

    *ppid = -1;

    char *line = NULL;
    size_t lineLen = 0;
    ssize_t read;
    while ((read = getline(&line, &lineLen, statusFile)) != -1)
    {
        if (strncmp("PPid:\t", line, 6) == 0)
        {
            *ppid = atoll(line + 6);
        }
        else if (strncmp("Tgid:\t", line, 6) == 0)
        {
            *tgid = atoll(line + 6);
        }
        else if (strncmp("Name:\t", line, 6) == 0)
        {
            if (name != NULL && nameSize > 0)
            {
                const char* source = line + 6;
                size_t length = strcspn(source, "\n");

                if (length >= nameSize)
                {
                    length = nameSize - 1;
                }

                memcpy(name, source, length);
                name[length] = '\0';
            }
        }
    }

    free(line);
    fclose(statusFile);
    return true;
}

bool ProcessInfo::Initialize()
{
    long pageSize = sysconf(_SC_PAGESIZE);
    if (pageSize <= 0 || ((uint64_t)pageSize & ((uint64_t)pageSize - 1)) != 0)
    {
        fprintf(stderr, "[createdump] Invalid system page size: %ld\n", pageSize);
        return false;
    }
#if defined(__arm__) || defined(__aarch64__) || defined(__loongarch64) || defined(__riscv)
    // it may have been initialized in the external createdump
    if (g_pageSize == 0)
    {
        g_pageSize = pageSize;
    }
#endif
    m_pageSize = (uint64_t)pageSize;
    char memPath[128];
    int chars = snprintf(memPath, sizeof(memPath), "/proc/%u/mem", m_pid);
    if (chars <= 0 || (size_t)chars >= sizeof(memPath))
    {
        printf_error("snprintf failed building /proc/<pid>/mem name\n");
        return false;
    }

    m_fdMemory = open(memPath, O_RDONLY);
    if (m_fdMemory == -1)
    {
        int err = errno;
        const char* message = "Problem accessing memory";
        if (err == EPERM || err == EACCES)
        {
            message = "The process or container does not have permissions or access";
        }
        else if (err == ENOENT)
        {
            message = "Invalid process id";
        }
        printf_error("%s: open(%s) FAILED %s (%d)\n", message, memPath, strerror(err), err);
        return false;
    }

    GetStatus(m_pid, &m_ppid, &m_tgid, m_exeName, sizeof(m_exeName));
    TryGetExecutableName(m_pid, m_exeName, sizeof(m_exeName));

    return true;
}

// verify later that it matches external createdump
void ProcessInfo::CleanupAndResumeProcess()
{
    for (const ThreadSnapshot& thread : m_threads)
    {
        pid_t tid = thread.Tid();
        if (ptrace(PTRACE_DETACH, tid, nullptr, nullptr) != -1)
        {
            int waitStatus;
            waitpid(tid, &waitStatus, __WALL);
        }
    }

    if (m_fdMemory != -1)
    {
        close(m_fdMemory);
        m_fdMemory = -1;
    }
    if (m_fdPagemap != -1)
    {
        close(m_fdPagemap);
        m_fdPagemap = -1;
    }

}

bool ValidateDumpOptions(const CreateDumpOptions* options)
{
    if (options->CrashReport && (options->AppModel == AppModelType::SingleFile || options->AppModel == AppModelType::NativeAOT))
    {
        printf_error("The app model does not support crash report generation\n");
        return false;
    }

    if (options->DumpType != DumpType::Full && options->AppModel == AppModelType::NativeAOT)
    {
        printf_error("The app model only supports full dump generation\n");
        return false;
    }

    return true;
}