// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#define ___in       _SAL1_Source_(__in, (), _In_)
#define ___out      _SAL1_Source_(__out, (), _Out_)

#ifndef _countof
#define _countof(x) (sizeof(x)/sizeof(x[0]))
#endif

extern bool g_diagnostics;

#define TRACE(args...) \
        if (g_diagnostics) { \
            printf(args); \
        }

#include <winternl.h>
#include <winver.h>
#include <windows.h>
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
#include <dumpcommon.h>
typedef int T_CONTEXT;
#include <dacprivate.h>
#include <arrayholder.h>
#include <releaseholder.h>
#include <unistd.h>
#include <signal.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <sys/ptrace.h> 
#include <sys/user.h> 
#include <sys/wait.h>
#include <sys/procfs.h>
#include <dirent.h>
#include <fcntl.h>
#include <elf.h>
#include <link.h>
#define __STDC_FORMAT_MACROS
#include <inttypes.h>
#include <map>
#include <set>
#include <vector>
#include <array>
#include <string>
#include "datatarget.h"
#include "threadinfo.h"
#include "memoryregion.h"
#include "crashinfo.h"
#include "dumpwriter.h"
