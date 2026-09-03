// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Entry point for NativeAOT linked-in createdump functionality.
// This is invoked when a NativeAOT process re-executes itself with
// the GUID sentinel argument after a crash.
//
// Usage: <self> SENTINEL --name <path> [--full] [--diag] [--signal N]
//        [--crashthread N] [--code N] [--errno N] [--address N]
//        [--nativeaot] <pid>

#include "createdump_sentinel.h"
#include "process_reader.h"
#include "elf_dump_writer.h"

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include <time.h>
#include <sys/types.h>

#define DEFAULT_DUMP_PATH "/tmp/"
#define DEFAULT_DUMP_TEMPLATE "coredump.%p"

// Exported symbol so PalCreateDump.cpp can detect that this library is linked.
bool g_createdumpLinked = true;

// Format the dump path using the same substitutions as external createdump.
static bool FormatDumpPath(char* output, size_t outputSize, const char* pathTemplate, pid_t pid)
{
    if (outputSize == 0 || pathTemplate[0] == '|')
    {
        return false;
    }

    const char* src = pathTemplate;
    char* dst = output;
    char* dstEnd = output + outputSize - 1;

    while (*src != '\0' && dst < dstEnd)
    {
        if (*src != '%')
        {
            *dst++ = *src++;
            continue;
        }

        src++;
        if (*src == '\0')
        {
            break;
        }

        if (*src == '%')
        {
            *dst++ = '%';
            src++;
        }
        else if (*src == 'd' || *src == 'p')
        {
            size_t remaining = (size_t)(dstEnd - dst);
            int written = snprintf(dst, remaining + 1, "%d", pid);
            if (written < 0 || (size_t)written > remaining)
            {
                return false;
            }
            dst += written;
            src++;
        }
        else if (*src == 'e')
        {
            // Executable name - read from /proc
            char exePath[4096];
            char exeLink[64];
            snprintf(exeLink, sizeof(exeLink), "/proc/%d/exe", pid);
            ssize_t len = readlink(exeLink, exePath, sizeof(exePath) - 1);
            if (len > 0)
            {
                exePath[len] = '\0';
                // Get just the filename
                const char* name = strrchr(exePath, '/');
                name = name != NULL ? name + 1 : exePath;
                size_t remaining = (size_t)(dstEnd - dst);
                int written = snprintf(dst, remaining + 1, "%s", name);
                if (written < 0 || (size_t)written > remaining)
                {
                    return false;
                }
                dst += written;
            }
            else
            {
                return false;
            }
            src++;
        }
        else if (*src == 't')
        {
            size_t remaining = (size_t)(dstEnd - dst);
            int written = snprintf(dst, remaining + 1, "%ld", (long)time(NULL));
            if (written < 0 || (size_t)written > remaining)
            {
                return false;
            }
            dst += written;
            src++;
        }
        else if (*src == 'h')
        {
            char hostname[256];
            if (gethostname(hostname, sizeof(hostname)) != 0)
            {
                return false;
            }
            hostname[sizeof(hostname) - 1] = '\0';
            size_t remaining = (size_t)(dstEnd - dst);
            int written = snprintf(dst, remaining + 1, "%s", hostname);
            if (written < 0 || (size_t)written > remaining)
            {
                return false;
            }
            dst += written;
            src++;
        }
        else
        {
            fprintf(stderr, "[createdump] Invalid dump name format character '%c'\n", *src);
            return false;
        }
    }

    if (*src != '\0')
    {
        return false;
    }

    *dst = '\0';
    return true;
}

// Entry point called from bootstrap when GUID sentinel is detected.
int nativeaot_createdump_main(int argc, const char* argv[])
{
    const char* dumpPathTemplate = NULL;
    bool diagnostics = false;
    bool fullDump = false;
    int crashSignal = 0;
    pid_t crashThread = 0;
    int signalCode = 0;
    int signalErrno = 0;
    uint64_t signalAddress = 0;
    pid_t pid = 0;

    // Parse arguments. argv[0] is the program name.
    for (int i = 1; i < argc; i++)
    {
        if (strcmp(argv[i], "--name") == 0 && i + 1 < argc)
        {
            dumpPathTemplate = argv[++i];
        }
        else if (strcmp(argv[i], "--full") == 0)
        {
            fullDump = true;
        }
        else if (strcmp(argv[i], "--diag") == 0)
        {
            diagnostics = true;
        }
        else if (strcmp(argv[i], "--verbose") == 0)
        {
            diagnostics = true;
        }
        else if (strcmp(argv[i], "--signal") == 0 && i + 1 < argc)
        {
            crashSignal = (int)strtol(argv[++i], NULL, 10);
        }
        else if (strcmp(argv[i], "--crashthread") == 0 && i + 1 < argc)
        {
            crashThread = (pid_t)strtol(argv[++i], NULL, 10);
        }
        else if (strcmp(argv[i], "--code") == 0 && i + 1 < argc)
        {
            signalCode = (int)strtol(argv[++i], NULL, 10);
        }
        else if (strcmp(argv[i], "--errno") == 0 && i + 1 < argc)
        {
            signalErrno = (int)strtol(argv[++i], NULL, 10);
        }
        else if (strcmp(argv[i], "--address") == 0 && i + 1 < argc)
        {
            signalAddress = (uint64_t)strtoull(argv[++i], NULL, 10);
        }
        else if (strcmp(argv[i], "--nativeaot") == 0)
        {
            // NativeAOT mode is the only mode we support; accept and ignore
        }
        else if (strcmp(argv[i], "--withheap") == 0)
        {
            // Heap mode is the default linked-createdump mode.
        }
        else if (strcmp(argv[i], "--normal") == 0 ||
                 strcmp(argv[i], "--triage") == 0 ||
                 strcmp(argv[i], "--crashreport") == 0 ||
                 strcmp(argv[i], "--crashreportonly") == 0 ||
                 strcmp(argv[i], "--logtofile") == 0)
        {
            fprintf(stderr, "[createdump] Option '%s' is not supported by linked createdump\n", argv[i]);
            return 1;
        }
        else if (strcmp(argv[i], "--exception-record") == 0 && i + 1 < argc)
        {
            // TODO: add support for exception record
            i++;
        }
        else
        {
            // Assume it's the PID
            pid = (pid_t)strtol(argv[i], NULL, 10);
        }
    }

    if (pid <= 0)
    {
        fprintf(stderr, "[createdump] No target PID specified\n");
        return 1;
    }

    pid_t parentPid = getppid();
    if (pid != parentPid)
    {
        fprintf(stderr, "[createdump] Target PID %d is not the parent process %d\n", pid, parentPid);
        return 1;
    }

    // Build dump path from template
    char dumpPath[4096];
    if (dumpPathTemplate == NULL)
    {
        dumpPathTemplate = DEFAULT_DUMP_PATH DEFAULT_DUMP_TEMPLATE;
    }

    if (!FormatDumpPath(dumpPath, sizeof(dumpPath), dumpPathTemplate, pid))
    {
        fprintf(stderr, "[createdump] Failed to format dump path\n");
        return 1;
    }

    if (diagnostics)
    {
        fprintf(stderr, "[createdump] Generating core dump for process %d\n", pid);
        fprintf(stderr, "[createdump] Dump path: %s\n", dumpPath);
    }

    // Initialize process info
    ProcessInfo processInfo;
    if (!ProcessInfoInit(&processInfo, pid, crashSignal, crashThread,
                         signalCode, signalErrno, signalAddress))
    {
        fprintf(stderr, "[createdump] Failed to initialize process info\n");
        return 1;
    }

    int exitCode = 1;

    // Read process status
    if (!ReadProcessStatus(&processInfo))
    {
        fprintf(stderr, "[createdump] Failed to read process status\n");
        goto cleanup;
    }

    if (diagnostics)
    {
        fprintf(stderr, "[createdump] Process name: %s, ppid: %d\n", processInfo.name, processInfo.ppid);
    }

    // Enumerate and attach to threads
    if (!EnumerateAndAttachThreads(&processInfo))
    {
        fprintf(stderr, "[createdump] Failed to enumerate threads\n");
        goto detach;
    }

    if (diagnostics)
    {
        fprintf(stderr, "[createdump] Found %zu threads\n", processInfo.threads.count);
    }

    // Read thread registers
    if (!ReadThreadRegisters(&processInfo))
    {
        fprintf(stderr, "[createdump] Failed to read thread registers\n");
        goto detach;
    }

    // Read memory regions
    if (!ReadMemoryRegions(&processInfo))
    {
        fprintf(stderr, "[createdump] Failed to read memory regions\n");
        goto detach;
    }

    if (diagnostics)
    {
        fprintf(stderr, "[createdump] Found %zu memory regions\n", processInfo.regions.count);
    }

    // Read auxiliary vector
    if (!ReadAuxv(&processInfo))
    {
        fprintf(stderr, "[createdump] Failed to read auxv\n");
        goto detach;
    }

    // Write the ELF core dump
    if (WriteElfCoreDump(dumpPath, &processInfo, fullDump))
    {
        fprintf(stderr, "[createdump] Dump successfully written to %s\n", dumpPath);
        exitCode = 0;
    }
    else
    {
        fprintf(stderr, "[createdump] Failed to write dump\n");
    }

detach:
    DetachThreads(&processInfo);

cleanup:
    ProcessInfoCleanup(&processInfo);
    return exitCode;
}
