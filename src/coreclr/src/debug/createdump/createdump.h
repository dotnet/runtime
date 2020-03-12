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

#include "config.h"

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
#ifdef HAVE_PROCESS_VM_READV
#include <sys/uio.h>
#endif
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

#ifdef TARGET_ANDROID
// copied from .tools/android-rootfs/android-ndk-r21/sysroot/usr/include/linux/elfcore.h
// (after running eng/common/cross/build-android-rootfs.sh), because we should not use
// Linux headers in Android build, and if we do it clashes with sys/procfs.h (included above).

struct elf_prstatus {
  struct elf_siginfo pr_info;
  short pr_cursig;
  unsigned long pr_sigpend;
  unsigned long pr_sighold;
  pid_t pr_pid;
  pid_t pr_ppid;
  pid_t pr_pgrp;
  pid_t pr_sid;
  struct timeval pr_utime;
  struct timeval pr_stime;
  struct timeval pr_cutime;
  struct timeval pr_cstime;
  elf_gregset_t pr_reg;
  int pr_fpvalid;
};

struct elf_prpsinfo {
  char pr_state;
  char pr_sname;
  char pr_zomb;
  char pr_nice;
  unsigned long pr_flag;
  __kernel_uid_t pr_uid;
  __kernel_gid_t pr_gid;
  pid_t pr_pid, pr_ppid, pr_pgrp, pr_sid;
  char pr_fname[16];
  char pr_psargs[ELF_PRARGSZ];
};

typedef struct elf_prstatus prstatus_t;
typedef struct elf_prpsinfo prpsinfo_t;
#endif

#include "dumpwriter.h"
