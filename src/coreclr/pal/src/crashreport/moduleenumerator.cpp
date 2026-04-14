// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Crash report process-name lookup helper.
// Parses /proc/self/cmdline and /proc/self/maps using only open/read/close.
// No stdio, no sscanf, no heap allocation.

#include "moduleenumerator.h"
#include <unistd.h>
#include <fcntl.h>
#include <string.h>

typedef void (*ModuleCallback)(const char* filename, void* ctx);

struct ProcessNameCtx
{
    char* filename;
    int filenameLen;
    int found;
};

static
const char*
GetFilename(
    const char* path);

void
CopyFilename(
    char* filename,
    int filenameLen,
    const char* source);

static
void
ParseMapsLine(
    const char* line,
    ModuleCallback callback,
    void* ctx,
    char* lastModule,
    int lastModuleSize);

static
void
EnumerateModules(
    ModuleCallback callback,
    void* ctx);

static
void
ProcessNameCallback(
    const char* filename,
    void* ctx);

const char*
GetFilename(
    const char* path)
{
    const char* last = path;
    for (const char* p = path; *p; p++)
    {
        if (*p == '/')
            last = p + 1;
    }
    return last;
}

void
CopyFilename(
    char* filename,
    int filenameLen,
    const char* source)
{
    if (filename == NULL || filenameLen <= 0 || source == NULL)
    {
        return;
    }

    int len = 0;
    while (source[len] && len < filenameLen - 1)
    {
        filename[len] = source[len];
        len++;
    }
    filename[len] = '\0';
}

void
ParseMapsLine(
    const char* line,
    ModuleCallback callback,
    void* ctx,
    char* lastModule,
    int lastModuleSize)
{
    const char* p = line;
    while (*p != '\0' && *p != ' ')
    {
        p++;
    }

    if (*p != ' ')
    {
        return;
    }
    p++;

    const char* permissions = p;
    while (*p != '\0' && *p != ' ')
    {
        p++;
    }

    // We only care about named executable image mappings because they can
    // identify the current process image when /proc/self/cmdline is unavailable.
    if (*p != ' ' || (p - permissions <= 2) || permissions[2] != 'x')
    {
        return;
    }
    p++;

    int spacesToSkip = 3;
    while (*p != '\0' && spacesToSkip > 0)
    {
        if (*p == ' ')
        {
            while (*p == ' ')
            {
                p++;
            }
            spacesToSkip--;
        }
        else
        {
            p++;
        }
    }

    if (spacesToSkip != 0 || *p == '\0' || *p == '\n' || *p == '[')
    {
        return;
    }

    char pathname[256];
    int pathnameLen = 0;
    while (p[pathnameLen] != '\0' && p[pathnameLen] != '\n' && pathnameLen < sizeof(pathname) - 1)
    {
        pathname[pathnameLen] = p[pathnameLen];
        pathnameLen++;
    }
    pathname[pathnameLen] = '\0';

    if (strcmp(pathname, lastModule) == 0)
    {
        return;
    }

    CopyFilename(lastModule, lastModuleSize, pathname);
    callback(GetFilename(pathname), ctx);
}

void
EnumerateModules(
    ModuleCallback callback,
    void* ctx)
{
    // Walk the live executable mappings as a crash-time fallback for process
    // name resolution without depending on loader APIs or heap allocation.
    int fd = open("/proc/self/maps", O_RDONLY);
    if (fd == -1)
    {
        return;
    }

    char readBuf[4096];
    char lineBuf[512];
    int linePos = 0;
    char lastModule[256] = { 0 };
    ssize_t bytesRead = 0;

    while ((bytesRead = read(fd, readBuf, sizeof(readBuf))) > 0)
    {
        for (int i = 0; i < bytesRead; i++)
        {
            if (readBuf[i] == '\n')
            {
                lineBuf[linePos] = '\0';
                ParseMapsLine(lineBuf, callback, ctx, lastModule, sizeof(lastModule));
                linePos = 0;
            }
            else if (linePos < sizeof(lineBuf) - 1)
            {
                lineBuf[linePos++] = readBuf[i];
            }
        }
    }

    close(fd);
}

void
ProcessNameCallback(
    const char* filename,
    void* ctx)
{
    ProcessNameCtx* processName = (ProcessNameCtx*)ctx;
    if (processName->found || processName->filename == NULL || processName->filenameLen <= 0)
    {
        return;
    }

    CopyFilename(processName->filename, processName->filenameLen, filename);
    processName->found = processName->filename[0] != '\0';
}

int
CrashModulesTryGetProcessName(
    char* filename,
    int filenameLen)
{
    if (filename == NULL || filenameLen <= 0)
    {
        return 0;
    }

    filename[0] = '\0';

    int fd = open("/proc/self/cmdline", O_RDONLY);
    if (fd != -1)
    {
        char cmdline[256];
        ssize_t bytesRead = read(fd, cmdline, sizeof(cmdline) - 1);
        close(fd);

        if (bytesRead > 0)
        {
            cmdline[bytesRead] = '\0';
            CopyFilename(filename, filenameLen, GetFilename(cmdline));
            if (filename[0] != '\0')
            {
                return 1;
            }
        }
    }

    // Fall back to the first executable mapping if the kernel cmdline view is
    // unavailable or empty.
    ProcessNameCtx ctx = { filename, filenameLen, 0 };
    EnumerateModules(ProcessNameCallback, &ctx);
    return ctx.found;
}
