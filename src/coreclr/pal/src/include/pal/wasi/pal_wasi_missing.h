// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Minimal definitions for POSIX types that wasi-libc (wasi-sdk 33 /
// wasm32-wasip2 / WASI 0.2.8) does not provide in its default profile.
//
// This is a deliberately narrow scaffolding header. Each entry is tagged
// with a "REPLACE-WHEN" condition; delete the entry when that condition
// becomes true and recompile.
//
// Anti-goals:
//   * This is NOT a `#include_next`-style transparent shim. Files that need
//     these types include this header explicitly inside an `#if
//     defined(TARGET_WASI)` guard.
//   * This is NOT a wholesale POSIX emulation. We only add what PAL headers
//     reference at compile time.
//

#ifndef _PAL_WASI_MISSING_H_
#define _PAL_WASI_MISSING_H_

#if !defined(TARGET_WASI)
#error pal_wasi_missing.h must only be included on WASI targets
#endif

#include <sys/types.h>

// siginfo_t
//
// wasi-libc's <signal.h> declares siginfo_t only under
// __wasilibc_unmodified_upstream (a build-time switch that wasi-libc keeps
// off because WASI 0.2.8 has no sigaction delivery). PAL headers reference
// `siginfo_t*` in function prototypes (PROCAbort, signal_handler_worker,
// PROCCreateCrashDumpIfEnabled, ExecuteHandlerOnCustomStack, …) even though
// the underlying source files (exception/signal.cpp) are excluded from the
// WASI build.
//
// REPLACE-WHEN: wasi-libc exposes siginfo_t in its default profile, OR PAL
// stops referencing siginfo_t in headers that the WASI build still compiles.
#ifndef si_signo
typedef struct {
    int si_signo;
    int si_errno;
    int si_code;
    void *si_addr;
} siginfo_t;
#endif

// struct passwd
//
// wasi-libc has no <pwd.h>. PAL init/pal.cpp includes <pwd.h> transitively;
// no actual lookup is performed on WASI. Provide the structure shape so the
// include compiles. Stub functions (getpwuid_r) live in
// arch/wasm/stubs.cpp.
//
// REPLACE-WHEN: wasi-libc adds <pwd.h>, OR PAL stops including <pwd.h>
// transitively on WASI.
struct passwd {
    char   *pw_name;
    char   *pw_passwd;
    uid_t   pw_uid;
    gid_t   pw_gid;
    char   *pw_gecos;
    char   *pw_dir;
    char   *pw_shell;
};

// Dl_info / dladdr
//
// wasi-libc declares dlopen/dlsym/dlerror in <dlfcn.h> (returning errors at
// runtime), but does NOT declare Dl_info or dladdr. PAL uses dladdr() in two
// patterns: best-effort symbol/module identification for diagnostics
// (misc/dbgmsg.cpp), and as part of GetProcAddress (loader/module.cpp).
// Provide a struct and a stub dladdr() that always reports "not found" — the
// existing PAL call sites already handle that case gracefully.
//
// REPLACE-WHEN: wasi-libc adds dladdr() (or PAL gains an abstraction we can
// pass through).
typedef struct {
    const char *dli_fname;
    void       *dli_fbase;
    const char *dli_sname;
    void       *dli_saddr;
} Dl_info;

static inline int dladdr(const void *addr, Dl_info *info)
{
    (void)addr; (void)info;
    return 0;  // failure — callers must handle this path
}

// getuid / geteuid / getgid / getegid
//
// wasi-libc has no concept of users/groups, so these functions are absent.
// PAL uses them only to compare file st_uid/st_gid for permission checks
// (misc/utils.cpp). On WASI we are always "user 0" — return 0 so the existing
// owner-equals-process branches behave consistently.
//
// REPLACE-WHEN: WASI gains a user/process-identity surface.
static inline uid_t getuid(void)  { return 0; }
static inline uid_t geteuid(void) { return 0; }
static inline gid_t getgid(void)  { return 0; }
static inline gid_t getegid(void) { return 0; }

#endif // _PAL_WASI_MISSING_H_
