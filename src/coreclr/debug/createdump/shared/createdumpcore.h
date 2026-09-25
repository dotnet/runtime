// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef CREATEDUMPCORE_H
#define CREATEDUMPCORE_H

extern bool g_diagnostics;
extern bool g_diagnosticsVerbose;

#ifdef __GNUC__
#define CREATEDUMP_FORMAT_PRINTF(fmt_pos, arg_pos) __attribute__ ((__format__(__printf__, fmt_pos, arg_pos)))
#else
#define CREATEDUMP_FORMAT_PRINTF(fmt_pos, arg_pos)
#endif

#ifdef HOST_UNIX
extern void trace_printf(const char* format, ...) CREATEDUMP_FORMAT_PRINTF(1, 2);
extern void trace_verbose_printf(const char* format, ...) CREATEDUMP_FORMAT_PRINTF(1, 2);
#define TRACE(args...) trace_printf(args)
#define TRACE_VERBOSE(args...) trace_verbose_printf(args)
#else
#define TRACE(args, ...)
#define TRACE_VERBOSE(args, ...)
#endif

#ifdef HOST_64BIT
#define PRIA "016"
#else
#define PRIA "08"
#endif

#include <stdio.h>
#include <stdarg.h>
#include <errno.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>
#include <inttypes.h>

extern FILE* g_logfile;
extern FILE* g_stdout;

#ifdef HOST_UNIX
#include <assert.h>
#include <unistd.h>
#include <signal.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <sys/ptrace.h>
#include <sys/user.h>
#include <sys/wait.h>
#ifndef __APPLE__
#include <sys/procfs.h>
#include <asm/ptrace.h>
#endif
#ifdef HAVE_PROCESS_VM_READV
#include <sys/uio.h>
#endif
#include <dirent.h>
#include <fcntl.h>
#ifdef __APPLE__
#include <ELF.h>
#else
#include <elf.h>
#include <link.h>
#endif
#endif

#ifndef MAX_LONGPATH
#define MAX_LONGPATH   1024
#endif

#ifdef HOST_WINDOWS
#define DEFAULT_DUMP_PATH "%TEMP%\\"
#define DEFAULT_DUMP_TEMPLATE "dump.%p.dmp"
#else
#define DEFAULT_DUMP_PATH "/tmp/"
#define DEFAULT_DUMP_TEMPLATE "coredump.%p"
#endif

enum class DumpType
{
    Mini,
    Heap,
    Triage,
    Full
};

enum class AppModelType
{
    Normal,
    SingleFile,
    NativeAOT
};

typedef struct
{
    const char* DumpPathTemplate;
    const char* LogFilePath;

    enum DumpType DumpType;
    enum AppModelType AppModel;

    bool CreateDump;
    bool CrashReport;
    bool Diagnostics;
    bool Verbose;

    int Pid;
    int CrashThread;
    int Signal;
    int SignalCode;
    int SignalErrno;
    uint64_t SignalAddress;
    uint64_t ExceptionRecord;
} CreateDumpOptions;

inline const char*
GetDumpTypeString(DumpType dumpType)
{
    switch (dumpType)
    {
        case DumpType::Mini:
            return "minidump";
        case DumpType::Heap:
            return "minidump with heap";
        case DumpType::Triage:
            return "triage minidump";
        case DumpType::Full:
            return "full dump";
        default:
            return "unknown";
    }
}

#ifdef HOST_UNIX
#ifdef __APPLE__
#include <mach/mach.h>
#include <mach/mach_vm.h>
#endif
#include "coreutils.h"
#include "memoryregion.h"
#include "specialdiaginfo.h"
#include "threadsnapshot.h"
#include "processinfo.h"
#endif

template <typename TRegions, typename TCombinedRegions, typename TInsert>
bool CombineMemoryRegions(const TRegions& regions, TCombinedRegions& combinedRegions, TInsert insert)
{
    TRACE("CombineMemoryRegions: STARTED\n");
    assert(!regions.empty());

    // MEMORY_REGION_FLAG_SHARED and MEMORY_REGION_FLAG_PRIVATE are internal flags that
    // don't affect the core dump so ignore them when comparing the flags.
    uint32_t flags = 0;
    uint64_t start = 0;
    uint64_t end = 0;
    bool hasRegion = false;

    for (const MemoryRegion& region : regions)
    {
        uint32_t regionFlags = region.Flags() & MEMORY_REGION_FLAG_PERMISSIONS_MASK;
        if (!hasRegion)
        {
            flags = regionFlags;
            start = region.StartAddress();
            end = region.EndAddress();
            hasRegion = true;
            continue;
        }

        // To combine a region it needs to be contiguous, same permissions and memory backed flag.
        if (end == region.StartAddress() && flags == regionFlags)
        {
            end = region.EndAddress();
        }
        else
        {
            if (!insert(MemoryRegion(flags, start, end)))
            {
                return false;
            }
            flags = regionFlags;
            start = region.StartAddress();
            end = region.EndAddress();
        }
    }

    assert(start != end);
    if (!insert(MemoryRegion(flags, start, end)))
    {
        return false;
    }

    TRACE("CombineMemoryRegions: FINISHED\n");

    if (g_diagnosticsVerbose)
    {
        TRACE("Final Memory Regions:\n");
        for (const MemoryRegion& region : combinedRegions)
        {
            region.Trace();
        }
    }

    return true;
}

void printf_status(const char* format, ...);
void printf_error(const char* format, ...);
void print_trace_timestamp();
void trace_prefix(const char* format, va_list args);

bool CreateDump(const CreateDumpOptions& options);
bool GetDefaultDumpPath(char* buffer, size_t bufferSize);
bool FormatDumpName(char* name, size_t nameSize, const char* pattern, const char* exeName, int pid);
int ParseCreateDumpOptions(int argc, char* argv[], CreateDumpOptions* options);
bool ValidateDumpOptions(const CreateDumpOptions* options);
bool GetStatus(pid_t pid, pid_t* ppid, pid_t* tgid, char *name, size_t nameSize);
bool AddSpecialDiagInfoRegion(DumpRegionStore& regionStore);
bool CreateDumpCore(const CreateDumpOptions* options, DumpRegionStore* regionStore);

#endif // CREATEDUMPCORE_H
