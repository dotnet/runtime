// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#define ___in       _SAL1_Source_(__in, (), _In_)
#define ___out      _SAL1_Source_(__out, (), _Out_)

extern bool g_diagnostics;
extern bool g_diagnosticsVerbose;

#ifdef HOST_UNIX
extern bool g_checkForSingleFile;
extern void trace_printf(const char* format, ...);
extern void trace_verbose_printf(const char* format, ...);
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

#ifdef HOST_UNIX
#include "config.h"
#endif

#include <windows.h>
#include <winternl.h>
#include <winver.h>
#include <stdlib.h>
#include <stdint.h>
#include <stddef.h>
#include <string.h>
#include <corhdr.h>
#include <cor.h>
#include <corsym.h>
#include <clrdata.h>
#include <xclrdata.h>
#include <corerror.h>
#include <cordebug.h>
#include <xcordebug.h>
#include <mscoree.h>
typedef int T_CONTEXT;
#include <dacprivate.h>
#include <arrayholder.h>
#include <releaseholder.h>
#ifdef HOST_UNIX
#include <dumpcommon.h>
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
#include <dlfcn.h>
#include <cxxabi.h>
#ifdef __APPLE__
#include <ELF.h>
#else
#include <elf.h>
#include <link.h>
#endif
#define __STDC_FORMAT_MACROS
#include <inttypes.h>
#else
#include <dbghelp.h>
#endif
#include <map>
#include <set>
#include <vector>
#include <array>
#include <string>
#ifdef HOST_UNIX
#ifdef __APPLE__
#include <mach/mach.h>
#include <mach/mach_vm.h>
#endif
#include "moduleinfo.h"
#include "datatarget.h"
#include "stackframe.h"
#include "threadinfo.h"
#include "memoryregion.h"
#include "crashinfo.h"
#include "crashreportwriter.h"
#include "dumpwriter.h"
#include "runtimeinfo.h"
#endif

#ifndef MAX_LONGPATH
#define MAX_LONGPATH   1024
#endif

extern bool FormatDumpName(std::string& name, const char* pattern, const char* exename, int pid);
extern bool CreateDump(const char* dumpPathTemplate, int pid, const char* dumpType, MINIDUMP_TYPE minidumpType, bool createDump, bool crashReport, int crashThread, int signal);

extern std::string GetLastErrorString();
extern void printf_status(const char* format, ...);
extern void printf_error(const char* format, ...);

// Keep in sync with the definitions in dbgutil.cpp and daccess.h
#define DACCESS_TABLE_SYMBOL "g_dacTable"
