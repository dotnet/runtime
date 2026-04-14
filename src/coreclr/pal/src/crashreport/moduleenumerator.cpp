// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Crash report module lookup helpers.
// Parses /proc/self/cmdline and /proc/self/maps using only open/read/close.
// No stdio, no sscanf, no heap allocation.

#include "moduleenumerator.h"

#include <unistd.h>
#include <fcntl.h>
#include <string.h>

#ifdef __linux__
#include <elf.h>

#if UINTPTR_MAX > UINT32_MAX
typedef Elf64_Ehdr ElfNative_Ehdr;
typedef Elf64_Phdr ElfNative_Phdr;
#else
typedef Elf32_Ehdr ElfNative_Ehdr;
typedef Elf32_Phdr ElfNative_Phdr;
#endif
#endif

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

struct LookupAddressCtx
{
    uint64_t address;
    uint64_t baseAddress;
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

static
uint64_t
ComputeImageBase(
    uint64_t startAddr,
    uint64_t endAddr,
    uint64_t fileOffset);

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

uint64_t
ComputeImageBase(
    uint64_t startAddr,
    uint64_t endAddr,
    uint64_t fileOffset)
{
#ifndef __linux__
    (void)endAddr;
    (void)fileOffset;
    return startAddr;
#else
    if (fileOffset > startAddr)
    {
        return startAddr;
    }

    uint64_t mappedFileBase = startAddr - fileOffset;
    if (mappedFileBase > endAddr || sizeof(ElfNative_Ehdr) > endAddr - mappedFileBase)
    {
        return startAddr;
    }

    const ElfNative_Ehdr* ehdr = reinterpret_cast<const ElfNative_Ehdr*>(static_cast<uintptr_t>(mappedFileBase));
    if (ehdr->e_ident[EI_MAG0] != ELFMAG0 ||
        ehdr->e_ident[EI_MAG1] != ELFMAG1 ||
        ehdr->e_ident[EI_MAG2] != ELFMAG2 ||
        ehdr->e_ident[EI_MAG3] != ELFMAG3)
    {
        return mappedFileBase;
    }

    if (ehdr->e_phentsize != sizeof(ElfNative_Phdr))
    {
        return mappedFileBase;
    }

    uint64_t availableBytes = endAddr - mappedFileBase;
    uint64_t phdrBytes = static_cast<uint64_t>(ehdr->e_phnum) * sizeof(ElfNative_Phdr);
    if (ehdr->e_phoff > availableBytes || phdrBytes > availableBytes - ehdr->e_phoff)
    {
        return mappedFileBase;
    }

    const ElfNative_Phdr* phdrs = reinterpret_cast<const ElfNative_Phdr*>(static_cast<uintptr_t>(mappedFileBase + ehdr->e_phoff));
    for (int i = 0; i < ehdr->e_phnum; i++)
    {
        if (phdrs[i].p_type == PT_LOAD && phdrs[i].p_offset == 0)
        {
            return mappedFileBase - phdrs[i].p_vaddr;
        }
    }

    return mappedFileBase;
#endif
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
ModuleLookupByAddress(
    uint64_t startAddr,
    uint64_t endAddr,
    uint64_t fileOffset,
    const char* filename,
    void* ctx)
{
    LookupAddressCtx* lookup = reinterpret_cast<LookupAddressCtx*>(ctx);
    if (lookup->found || lookup->address < startAddr || lookup->address >= endAddr)
    {
        return;
    }

    lookup->baseAddress = ComputeImageBase(startAddr, endAddr, fileOffset);
    lookup->found = 1;

    if (lookup->filename != NULL && lookup->filenameLen > 0)
    {
        CopyFilename(lookup->filename, lookup->filenameLen, filename);
    }
}

int
CrashModulesTryLookupModuleForAddress(
    uint64_t address,
    uint64_t* baseAddress,
    char* filename,
    int filenameLen)
{
    LookupAddressCtx ctx = { address, 0, filename, filenameLen, 0 };
    if (filename != NULL && filenameLen > 0)
    {
        filename[0] = '\0';
    }

    EnumerateModules(ModuleLookupByAddress, &ctx, 0);

    if (ctx.found && baseAddress != NULL)
    {
        *baseAddress = ctx.baseAddress;
    }

    return ctx.found;
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
