// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdarg.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>
#include <inttypes.h>
#include "createdump_options.h"

#ifdef HOST_UNIX
#include <assert.h>
#include <sys/ptrace.h>
#include <sys/uio.h>
#include <sys/user.h>
#include <elf.h>
#ifdef __APPLE__
#include <mach/mach.h>
#include <mach/mach_vm.h>
#endif
#endif

#ifndef __APPLE__
// typedef for our parsing of the auxv variables in /proc/pid/auxv.
#if TARGET_64BIT
typedef Elf64_auxv_t elf_aux_entry;
#define PRIx PRIx64
#define PRIu PRIu64
#define PRId PRId64
#define PRIA "016"
#define PRIxA PRIA PRIx
#else
typedef Elf32_auxv_t elf_aux_entry;
#define PRIx PRIx32
#define PRIu PRIu32
#define PRId PRId32
#define PRIA "08"
#define PRIxA PRIA PRIx
#endif

typedef __typeof__(((elf_aux_entry*) 0)->a_un.a_val) elf_aux_val_t;

// All interesting auvx entry types are AT_SYSINFO_EHDR and below
#define AT_MAX (AT_SYSINFO_EHDR + 1)

#endif // __APPLE__

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

#ifdef HOST_UNIX
extern void trace_printf(const char* format, ...) MINIPAL_ATTR_FORMAT_PRINTF(1, 2);
extern void trace_verbose_printf(const char* format, ...) MINIPAL_ATTR_FORMAT_PRINTF(1, 2);
#define TRACE(args...) trace_printf(args)
#define TRACE_VERBOSE(args...) trace_verbose_printf(args)
#else
#define TRACE(args, ...)
#define TRACE_VERBOSE(args, ...)
#endif

#include "memoryregion.h"
#include "threadsnapshot.h"
#include "processreader.h"

extern bool linkedCreateDump;
extern bool g_diagnostics;
extern bool g_diagnosticsVerbose;

void printf_status(const char* format, ...);
void printf_error(const char* format, ...);

bool CreateDump(const CreateDumpOptions& options);
bool GetDefaultDumpPath(char* buffer, size_t bufferSize);
bool FormatDumpName(char* name, size_t nameSize, const char* pattern, const char* exeName, int pid);
int ParseCreateDumpOptions(int argc, char* argv[], CreateDumpOptions* options);
bool GetStatus(pid_t pid, pid_t* ppid, pid_t* tgid, char *name, size_t nameSize);
bool AddSpecialDiagInfoRegion(DumpRegionStore* regionStore);
bool CreateDumpCore(const CreateDumpOptions* options, DumpRegionStore* regionStore);
