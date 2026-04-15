// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Crash report process-name lookup helper.
// Parses /proc/self/cmdline and /proc/self/maps using only open/read/close.
// No stdio, no sscanf, no heap allocation.

#include "moduleenumerator.h"

#include <unistd.h>
#include <fcntl.h>
#include <string.h>

typedef void (*ModuleCallback)(
    uint64_t startAddr,
    uint64_t endAddr,
    uint64_t fileOffset,
    const char* filename,
    void* ctx);

struct ProcessNameCtx
{
    char* filename;
    int filenameLen;
    int found;
};

static
const char*
ParseHex(
    const char* p,
    uint64_t* out);

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
    int lastModuleSize,
    bool deduplicate);

static
void
EnumerateModules(
    ModuleCallback callback,
    void* ctx,
    bool deduplicate);

static
void
ModuleLookupByAddress(
    uint64_t startAddr,
    uint64_t endAddr,
    uint64_t fileOffset,
    const char* filename,
    void* ctx);

static
void
ProcessNameCallback(
    uint64_t startAddr,
    uint64_t endAddr,
    uint64_t fileOffset,
    const char* filename,
    void* ctx);

const char*
ParseHex(
    const char* p,
    uint64_t* out)
{
    uint64_t value = 0;
    while (*p != '\0')
    {
        char c = *p;
        if (c >= '0' && c <= '9')
        {
            value = (value << 4) | static_cast<uint64_t>(c - '0');
        }
        else if (c >= 'a' && c <= 'f')
        {
            value = (value << 4) | static_cast<uint64_t>(c - 'a' + 10);
        }
        else if (c >= 'A' && c <= 'F')
        {
            value = (value << 4) | static_cast<uint64_t>(c - 'A' + 10);
        }
        else
        {
            break;
        }

        p++;
    }

    *out = value;
    return p;
}

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
    int lastModuleSize,
    bool deduplicate)
{
    uint64_t startAddr = 0;
    const char* p = ParseHex(line, &startAddr);
    if (*p != '-')
    {
        return;
    }
    p++;

    uint64_t endAddr = 0;
    p = ParseHex(p, &endAddr);
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
    if (*p != ' ')
    {
        return;
    }
    int executable = (p - permissions > 2) && permissions[2] == 'x';
    p++;

    while (*p == ' ')
    {
        p++;
    }

    uint64_t fileOffset = 0;
    p = ParseHex(p, &fileOffset);
    while (*p == ' ')
    {
        p++;
    }
    while (*p != '\0' && *p != ' ')
    {
        p++;
    }
    while (*p == ' ')
    {
        p++;
    }
    while (*p != '\0' && *p != ' ')
    {
        p++;
    }
    while (*p == ' ')
    {
        p++;
    }

    if (!executable || *p == '\0' || *p == '\n' || *p == '[')
    {
        return;
    }

    char pathname[256];
    int pathnameLen = 0;
    while (p[pathnameLen] != '\0' && p[pathnameLen] != '\n' && pathnameLen < static_cast<int>(sizeof(pathname)) - 1)
    {
        pathname[pathnameLen] = p[pathnameLen];
        pathnameLen++;
    }
    pathname[pathnameLen] = '\0';

    if (deduplicate && strcmp(pathname, lastModule) == 0)
    {
        return;
    }

    if (deduplicate)
    {
        CopyFilename(lastModule, lastModuleSize, pathname);
    }

    callback(startAddr, endAddr, fileOffset, GetFilename(pathname), ctx);
}

void
EnumerateModules(
    ModuleCallback callback,
    void* ctx,
    bool deduplicate)
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
                ParseMapsLine(lineBuf, callback, ctx, lastModule, sizeof(lastModule), deduplicate);
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
    uint64_t startAddr,
    uint64_t endAddr,
    uint64_t fileOffset,
    const char* filename,
    void* ctx)
{
    (void)startAddr;
    (void)endAddr;
    (void)fileOffset;

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
    EnumerateModules(ProcessNameCallback, &ctx, 1);
    return ctx.found;
}
