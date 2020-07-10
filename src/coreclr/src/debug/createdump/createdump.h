// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#define ___in       _SAL1_Source_(__in, (), _In_)
#define ___out      _SAL1_Source_(__out, (), _Out_)

#ifndef _countof
#define _countof(x) (sizeof(x)/sizeof(x[0]))
#endif

extern void trace_printf(const char* format, ...);
extern bool g_diagnostics;

#ifdef HOST_UNIX
#define TRACE(args...) trace_printf(args)
#define TRACE_VERBOSE(args...)
#else
#define TRACE(args, ...)
#define TRACE_VERBOSE(args, ...)
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
#include "mac.h"
#endif
#include "datatarget.h"
#include "threadinfo.h"
#include "memoryregion.h"
#include "crashinfo.h"
#include "dumpwriter.h"
#endif

#ifndef MAX_LONGPATH
#define MAX_LONGPATH   1024
#endif

bool CreateDump(const char* dumpPathTemplate, int pid, MINIDUMP_TYPE minidumpType);
