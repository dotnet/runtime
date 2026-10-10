// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdumpcore.h"

bool GetProcessInfo(pid_t pid, pid_t* ppid, pid_t* tgid, char *name, size_t nameSize);

bool ProcessInfo::Initialize()
{
#ifdef CREATEDUMP_RUNTIME_PAGE_SIZE
    // g_pageSize may have been initialized in the external createdump
    if (g_pageSize == 0)
    {
        long pageSize = sysconf(_SC_PAGESIZE);
        if (pageSize <= 0 || ((uint64_t)pageSize & ((uint64_t)pageSize - 1)) != 0)
        {
            fprintf(stderr, "[createdump] Invalid system page size: %ld\n", pageSize);
            return false;
        }
        g_pageSize = pageSize;
    }
#endif

    char memPath[128];
    int chars = snprintf(memPath, sizeof(memPath), "/proc/%u/mem", m_pid);
    if (chars <= 0 || (size_t)chars >= sizeof(memPath))
    {
        printf_error("snprintf failed building /proc/<pid>/mem name\n");
        return false;
    }

    m_fdMem = open(memPath, O_RDONLY);
    if (m_fdMem == -1)
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

#ifndef __APPLE__
    const char* disablePagemapUse = getenv("DOTNET_DbgDisablePagemapUse");
    if (disablePagemapUse == nullptr)
    {
        disablePagemapUse = getenv("COMPlus_DbgDisablePagemapUse");
    }
    char* end = nullptr;
    errno = 0;
    unsigned long value = disablePagemapUse != nullptr ? strtoul(disablePagemapUse, &end, 10) : 1;
    if (disablePagemapUse != nullptr && end != disablePagemapUse && errno != ERANGE && value == 0)
    {
        TRACE("DbgDisablePagemapUse detected - pagemap file checking is enabled\n");
        char pagemapPath[128];
        chars = snprintf(pagemapPath, sizeof(pagemapPath), "/proc/%u/pagemap", m_pid);
        if (chars <= 0 || (size_t)chars >= sizeof(pagemapPath))
        {
            printf_error("snprintf failed building /proc/<pid>/pagemap name\n");
            CleanupAndResumeProcess();
            return false;
        }
        m_fdPagemap = open(pagemapPath, O_RDONLY);
        if (m_fdPagemap == -1)
        {
            TRACE("open(%s) FAILED %d (%s), will fallback to dumping all memory regions without checking if they are committed\n", pagemapPath, errno, strerror(errno));
        }
    }
    else
    {
        m_fdPagemap = -1;
    }
#endif

    if (!GetProcessInfo(m_pid, &m_ppid, &m_tgid, m_exeName, sizeof(m_exeName)))
    {
        CleanupAndResumeProcess();
        return false;
    }

    m_canUseProcVmReadSyscall = true;
    return true;
}

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

    if (m_fdMem != -1)
    {
        close(m_fdMem);
        m_fdMem = -1;
    }
    if (m_fdPagemap != -1)
    {
        close(m_fdPagemap);
        m_fdPagemap = -1;
    }
}

//
// Get the process or thread status
//
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

    char *line = nullptr;
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
            if (name != nullptr && nameSize > 0)
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

bool GetProcessInfo(pid_t pid, pid_t* ppid, pid_t* tgid, char *name, size_t nameSize)
{
    if (!GetStatus(pid, ppid, tgid, name, nameSize))
    {
        return false;
    }

    // Try reading the executable name from the /proc/<pid>/exe link. Prefer this name to the
    // one reported by status if it is available because the status name is often truncated
    char exePath[128];
    int chars = snprintf(exePath, sizeof(exePath), "/proc/%d/exe", pid);
    if (chars > 0 && (size_t)chars < sizeof(exePath))
    {
        char buf[4096];
        ssize_t nbytes = readlink(exePath, buf, sizeof(buf) - 1);
        if (nbytes != -1)
        {
            buf[nbytes] = '\0';
            const char* executableName = strrchr(buf, '/');
            executableName = executableName != nullptr ? executableName + 1 : buf;
            snprintf(name, nameSize, "%s", executableName);
        }
    }

    return true;
}
