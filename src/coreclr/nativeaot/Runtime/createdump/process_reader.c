// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Minimal NativeAOT-specific process reader.
// Reads memory regions, threads, and registers via /proc and ptrace.

#include "process_reader.h"

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <errno.h>
#include <unistd.h>
#include <fcntl.h>
#include <dirent.h>
#include <inttypes.h>
#include <sys/ptrace.h>
#include <sys/wait.h>
#include <sys/uio.h>

#define INITIAL_CAPACITY 64

// Dynamic array helpers

static bool MemRegionArrayAdd(MemRegionArray* array, const MemRegion* item)
{
    if (array->count >= array->capacity)
    {
        if (array->capacity > SIZE_MAX / sizeof(MemRegion) / 2)
        {
            return false;
        }
        size_t newCapacity = array->capacity == 0 ? INITIAL_CAPACITY : array->capacity * 2;
        MemRegion* newItems = (MemRegion*)realloc(array->items, newCapacity * sizeof(MemRegion));
        if (newItems == NULL)
        {
            return false;
        }
        array->items = newItems;
        array->capacity = newCapacity;
    }
    array->items[array->count++] = *item;
    return true;
}

static bool ThreadArrayAdd(ThreadArray* array, const ThreadData* item)
{
    if (array->count >= array->capacity)
    {
        if (array->capacity > SIZE_MAX / sizeof(ThreadData) / 2)
        {
            return false;
        }
        size_t newCapacity = array->capacity == 0 ? INITIAL_CAPACITY : array->capacity * 2;
        ThreadData* newItems = (ThreadData*)realloc(array->items, newCapacity * sizeof(ThreadData));
        if (newItems == NULL)
        {
            return false;
        }
        array->items = newItems;
        array->capacity = newCapacity;
    }
    array->items[array->count++] = *item;
    return true;
}

static bool AuxvArrayAdd(AuxvArray* array, const Elf64_auxv_t* item)
{
    if (array->count >= array->capacity)
    {
        if (array->capacity > SIZE_MAX / sizeof(Elf64_auxv_t) / 2)
        {
            return false;
        }
        size_t newCapacity = array->capacity == 0 ? INITIAL_CAPACITY : array->capacity * 2;
        Elf64_auxv_t* newItems = (Elf64_auxv_t*)realloc(array->items, newCapacity * sizeof(Elf64_auxv_t));
        if (newItems == NULL)
        {
            return false;
        }
        array->items = newItems;
        array->capacity = newCapacity;
    }
    array->items[array->count++] = *item;
    return true;
}

bool ProcessInfoInit(ProcessInfo* info, pid_t pid, int crashSignal, pid_t crashThread,
                     int signalCode, int signalErrno, uint64_t signalAddress)
{
    memset(info, 0, sizeof(ProcessInfo));

    long pageSize = sysconf(_SC_PAGESIZE);
    if (pageSize <= 0 || ((uint64_t)pageSize & ((uint64_t)pageSize - 1)) != 0)
    {
        fprintf(stderr, "[createdump] Invalid system page size: %ld\n", pageSize);
        return false;
    }

    info->pid = pid;
    info->pageSize = (uint64_t)pageSize;
    info->crashSignal = crashSignal;
    info->crashThread = crashThread;
    info->signalCode = signalCode;
    info->signalErrno = signalErrno;
    info->signalAddress = signalAddress;

    // Read the main executable path for dump filtering
    char exeLink[64];
    snprintf(exeLink, sizeof(exeLink), "/proc/%d/exe", pid);
    ssize_t len = readlink(exeLink, info->exePath, sizeof(info->exePath) - 1);
    if (len > 0)
    {
        info->exePath[len] = '\0';
    }

    return true;
}

void ProcessInfoCleanup(ProcessInfo* info)
{
    for (size_t i = 0; i < info->regions.count; i++)
    {
        free(info->regions.items[i].fileName);
    }
    free(info->regions.items);
    free(info->threads.items);
    free(info->auxv.items);
    memset(info, 0, sizeof(ProcessInfo));
}

static uint32_t ParsePermissions(const char* perms)
{
    uint32_t flags = 0;
    if (perms[0] == 'r') flags |= PF_R;
    if (perms[1] == 'w') flags |= PF_W;
    if (perms[2] == 'x') flags |= PF_X;
    if (perms[3] == 'p') flags |= MR_PRIVATE;
    return flags;
}

bool ReadMemoryRegions(ProcessInfo* info)
{
    char path[64];
    snprintf(path, sizeof(path), "/proc/%d/maps", info->pid);

    FILE* fp = fopen(path, "r");
    if (fp == NULL)
    {
        fprintf(stderr, "[createdump] Failed to open %s: %s (%d)\n", path, strerror(errno), errno);
        return false;
    }

    char* line = NULL;
    size_t lineCapacity = 0;
    while (getline(&line, &lineCapacity, fp) != -1)
    {
        MemRegion region;
        memset(&region, 0, sizeof(region));

        uint64_t start, end, offset;
        char perms[5];
        unsigned int deviceMajor;
        unsigned int deviceMinor;
        uint64_t inode;
        int pathStart = 0;

        // Format: START-END PERMS OFFSET DEVMAJOR:DEVMINOR INODE PATHSTART
        int matched = sscanf(line, "%" SCNx64 "-%" SCNx64 " %4s %" SCNx64 " %x:%x %" SCNu64 " %n",
                             &start, &end, perms, &offset, &deviceMajor, &deviceMinor, &inode, &pathStart);

        if (matched < 7)
        {
            continue;
        }

        region.startAddress = start;
        region.endAddress = end;
        region.offset = offset;
        region.deviceMajor = deviceMajor;
        region.deviceMinor = deviceMinor;
        region.inode = inode;
        region.permissions = ParsePermissions(perms);

        if (pathStart > 0 && line[pathStart] != '\0')
        {
            // Copy file name, stripping trailing newline
            size_t len = strlen(line + pathStart);
            if (len > 0 && line[pathStart + len - 1] == '\n')
            {
                len--;
            }

            if (len > 0)
            {
                region.fileName = (char*)malloc(len + 1);
                if (region.fileName == NULL)
                {
                    free(line);
                    fclose(fp);
                    return false;
                }
                memcpy(region.fileName, line + pathStart, len);
                region.fileName[len] = '\0';
            }
        }

        if (!MemRegionArrayAdd(&info->regions, &region))
        {
            free(region.fileName);
            free(line);
            fclose(fp);
            return false;
        }
    }

    bool success = !ferror(fp);
    free(line);
    fclose(fp);
    return success;
}

bool EnumerateAndAttachThreads(ProcessInfo* info)
{
    char path[64];
    snprintf(path, sizeof(path), "/proc/%d/task", info->pid);

    DIR* dir = opendir(path);
    if (dir == NULL)
    {
        fprintf(stderr, "[createdump] Failed to open %s: %s (%d)\n", path, strerror(errno), errno);
        return false;
    }

    bool crashThreadAttached = false;
    while (true)
    {
        errno = 0;
        struct dirent* entry = readdir(dir);
        if (entry == NULL)
        {
            int readdirError = errno;
            closedir(dir);
            if (readdirError != 0)
            {
                fprintf(stderr, "[createdump] Failed to read %s: %s (%d)\n",
                        path, strerror(readdirError), readdirError);
                return false;
            }
            break;
        }

        pid_t tid = (pid_t)strtol(entry->d_name, NULL, 10);
        if (tid == 0)
        {
            continue;
        }

        long ptraceResult = ptrace(PTRACE_ATTACH, tid, NULL, NULL);
        if (ptraceResult != 0)
        {
            fprintf(stderr, "[createdump] ptrace(ATTACH, %d) failed: %s (%d)\n", tid, strerror(errno), errno);
            continue;
        }

        int waitStatus;
        int waitResult;
        do
        {
            waitResult = waitpid(tid, &waitStatus, __WALL);
        }
        while (waitResult == -1 && errno == EINTR);

        if (waitResult != tid)
        {
            fprintf(stderr, "[createdump] waitpid(%d) failed: %s (%d)\n", tid, strerror(errno), errno);
            ptrace(PTRACE_DETACH, tid, NULL, NULL);
            continue;
        }
        if (!WIFSTOPPED(waitStatus))
        {
            fprintf(stderr, "[createdump] Thread %d did not stop after ptrace attach (status %08x)\n",
                    tid, waitStatus);
            ptrace(PTRACE_DETACH, tid, NULL, NULL);
            continue;
        }

        ThreadData thread;
        memset(&thread, 0, sizeof(thread));
        thread.tid = tid;
        thread.isCrashThread = (tid == info->crashThread);

        if (!ThreadArrayAdd(&info->threads, &thread))
        {
            ptrace(PTRACE_DETACH, tid, NULL, NULL);
            closedir(dir);
            return false;
        }

        crashThreadAttached |= thread.isCrashThread;
    }

    if (info->crashThread != 0 && !crashThreadAttached)
    {
        fprintf(stderr, "[createdump] Failed to attach to crash thread %d\n", info->crashThread);
        return false;
    }

    return info->threads.count > 0;
}

bool ReadThreadRegisters(ProcessInfo* info)
{
    for (size_t i = 0; i < info->threads.count; i++)
    {
        ThreadData* thread = &info->threads.items[i];

        struct iovec gpVec = { &thread->gpRegs, sizeof(thread->gpRegs) };
        if (ptrace(PTRACE_GETREGSET, thread->tid, (void*)NT_PRSTATUS, &gpVec) != 0)
        {
            fprintf(stderr, "[createdump] ptrace(GETREGSET, NT_PRSTATUS, %d) failed: %s (%d)\n",
                    thread->tid, strerror(errno), errno);
            return false;
        }
        if (gpVec.iov_len != sizeof(thread->gpRegs))
        {
            fprintf(stderr, "[createdump] ptrace(GETREGSET, NT_PRSTATUS, %d) returned %zu bytes, expected %zu\n",
                thread->tid, gpVec.iov_len, sizeof(thread->gpRegs));
            return false;
        }

        // FP register failure is non-fatal
        struct iovec fpVec = { &thread->fpRegs, sizeof(thread->fpRegs) };
        if (ptrace(PTRACE_GETREGSET, thread->tid, (void*)NT_FPREGSET, &fpVec) != 0)
        {
            fprintf(stderr, "[createdump] ptrace(GETREGSET, NT_FPREGSET, %d) failed: %s (%d)\n",
                    thread->tid, strerror(errno), errno);

            continue;
        }
        if (fpVec.iov_len != sizeof(thread->fpRegs))
        {
            fprintf(stderr, "[createdump] ptrace(GETREGSET, NT_FPREGSET, %d) returned %zu bytes, expected %zu\n",
                thread->tid, fpVec.iov_len, sizeof(thread->fpRegs));
            continue;
        }
        thread->fpRegsSize = fpVec.iov_len;
    }

    return true;
}

bool ReadAuxv(ProcessInfo* info)
{
    char path[64];
    snprintf(path, sizeof(path), "/proc/%d/auxv", info->pid);

    int fd = open(path, O_RDONLY);
    if (fd == -1)
    {
        fprintf(stderr, "[createdump] Failed to open %s: %s (%d)\n", path, strerror(errno), errno);
        return false;
    }

    Elf64_auxv_t entry;
    while (read(fd, &entry, sizeof(entry)) == sizeof(entry))
    {
        if (!AuxvArrayAdd(&info->auxv, &entry))
        {
            close(fd);
            return false;
        }
        if (entry.a_type == AT_NULL)
        {
            break;
        }
    }

    close(fd);
    return true;
}

bool ReadProcessStatus(ProcessInfo* info)
{
    char path[64];
    snprintf(path, sizeof(path), "/proc/%d/status", info->pid);

    FILE* fp = fopen(path, "r");
    if (fp == NULL)
    {
        fprintf(stderr, "[createdump] Failed to open %s: %s (%d)\n", path, strerror(errno), errno);
        return false;
    }

    info->ppid = -1;

    char line[256];
    while (fgets(line, sizeof(line), fp) != NULL)
    {
        if (strncmp(line, "Name:", 5) == 0)
        {
            // Skip whitespace after "Name:"
            const char* name = line + 5;
            while (*name == ' ' || *name == '\t') name++;
            size_t len = strlen(name);
            if (len > 0 && name[len - 1] == '\n') len--;
            if (len >= sizeof(info->name)) len = sizeof(info->name) - 1;
            memcpy(info->name, name, len);
            info->name[len] = '\0';
        }
        else if (strncmp(line, "PPid:", 5) == 0)
        {
            info->ppid = (pid_t)strtol(line + 5, NULL, 10);
        }
    }

    fclose(fp);

    info->pgrp = getpgid(info->pid);
    if (info->pgrp == -1)
    {
        fprintf(stderr, "[createdump] getpgid(%d) failed: %s (%d)\n", info->pid, strerror(errno), errno);
        return false;
    }

    return true;
}

void DetachThreads(ProcessInfo* info)
{
    for (size_t i = 0; i < info->threads.count; i++)
    {
        ptrace(PTRACE_DETACH, info->threads.items[i].tid, NULL, NULL);
    }
}
