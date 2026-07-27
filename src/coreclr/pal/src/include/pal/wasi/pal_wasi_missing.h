// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Minimal definitions for POSIX types/symbols that wasi-libc (wasi-sdk 33 /
// wasm32-wasip2) does not provide. Files include this explicitly inside an
// #if defined(TARGET_WASI) guard; this is not a #include_next-style shim
// and not a wholesale POSIX emulation — only what PAL headers reference.

#ifndef _PAL_WASI_MISSING_H_
#define _PAL_WASI_MISSING_H_

#if !defined(TARGET_WASI)
#error pal_wasi_missing.h must only be included on WASI targets
#endif

#include <sys/types.h>

// PAL headers reference siginfo_t* in prototypes (PROCAbort, signal_handler_worker,
// PROCCreateCrashDumpIfEnabled, …) even though the underlying source is
// excluded from the WASI build. wasi-libc only declares siginfo_t under
// __wasilibc_unmodified_upstream (off because WASI 0.2.8 has no sigaction).
#ifndef si_signo
typedef struct {
    int si_signo;
    int si_errno;
    int si_code;
    void *si_addr;
} siginfo_t;
#endif

// wasi-libc has no <pwd.h>; PAL init/pal.cpp includes it transitively but
// performs no lookup. Stub functions live in arch/wasm/stubs.cpp.
struct passwd {
    char   *pw_name;
    char   *pw_passwd;
    uid_t   pw_uid;
    gid_t   pw_gid;
    char   *pw_gecos;
    char   *pw_dir;
    char   *pw_shell;
};

// wasi-libc has dlopen/dlsym/dlerror but not dladdr/Dl_info. PAL call sites
// (dbgmsg.cpp, loader/module.cpp) already handle dladdr() failure gracefully.
typedef struct {
    const char *dli_fname;
    void       *dli_fbase;
    const char *dli_sname;
    void       *dli_saddr;
} Dl_info;

static inline int dladdr(const void *addr, Dl_info *info)
{
    (void)addr; (void)info;
    return 0;
}

// WASI has no user/group concept; PAL uses these only for st_uid/st_gid
// permission checks. Always-zero makes the owner-equals-process branches behave.
static inline uid_t getuid(void)  { return 0; }
static inline uid_t geteuid(void) { return 0; }
static inline gid_t getgid(void)  { return 0; }
static inline gid_t getegid(void) { return 0; }

#endif // _PAL_WASI_MISSING_H_
