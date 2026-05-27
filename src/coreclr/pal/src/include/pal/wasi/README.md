# WASI POSIX shim headers

This directory contains a set of header wrappers that augment the WASI sysroot
with POSIX declarations and inline stub implementations required by the
CoreCLR PAL and adjacent components (minipal, debug, hosts).

## Why this exists

Unlike Linux, macOS, FreeBSD, OpenBSD, Android, or Haiku, the WASI sysroot
is missing entire POSIX subsystems:

| Subsystem | Status in WASI |
|-----------|---------------|
| Signals (`<signal.h>`, `sigaction`, `siginfo_t`, `sigaltstack`) | Most declarations hidden behind `__wasilibc_unmodified_upstream` |
| `<ucontext.h>` | Not provided |
| `<pthread.h>` | Partial — `pthread_setschedparam`, `pthread_getschedparam`, `pthread_exit` declarations missing |
| `<sys/wait.h>` | Not provided |
| `<sys/vfs.h>` (statfs) | Not provided |
| `<sys/resource.h>` (rusage) | Partial |
| `<dlfcn.h>` (dynamic loading) | Not provided |
| `<link.h>` (link map) | Not provided |
| `<pwd.h>` (user database) | Not provided |

The PAL and adjacent code consume these headers from dozens of translation
units. Two patterns were considered:

### Alternative 1 — `HAVE_*` configure checks (the dominant PAL pattern)

Existing platforms (Haiku, FreeBSD, OpenBSD, Apple) handle missing pieces by:

1. Probing for the missing symbol in `pal/src/configure.cmake` with
   `check_include_files` / `check_function_exists`.
2. Wrapping each call site in `#if HAVE_<NAME>`.
3. Adding inline stubs to the shared `.cpp` file behind `#ifdef <PLATFORM>`.

This works well when the gap is small (e.g., Haiku is missing a handful of
symbols). It scales poorly for WASI because the gap spans *most of POSIX*
across *many* PAL files. Applying it here would produce hundreds of
`#if HAVE_*` blocks scattered across `pal/src/{thread,exception,signal,
synchmgr,init,debug,loader,map,misc}/`.

### Alternative 2 — Sysroot shim headers (the approach taken)

A single directory of `#include_next` wrappers layers the missing
declarations and link-time-stubbed inline functions on top of the WASI
sysroot. Each shim is small (10–100 lines) and lives next to its peers,
making the platform delta inspectable in one place.

The shim approach is similar to how Apple's `sys/sysctl.h` or Linux's
`linux/elf.h` are sometimes wrapped by build systems that need to add
missing typedefs.

## How the shims are wired into the build

`src/coreclr/CMakeLists.txt` adds the shim directory via

    include_directories(BEFORE SYSTEM ...pal/src/include/pal/wasi)

inside the `if(CLR_CMAKE_TARGET_WASI)` block. The `BEFORE SYSTEM` placement
ensures the wrapper resolves before the WASI sysroot's own `<signal.h>`,
allowing each wrapper to do `#include_next <signal.h>` to chain to the
underlying header and then add missing declarations on top.

The scope is intentionally the entire CoreCLR build, not just PAL.
`minipal/Unix/doublemapping.cpp` and `debug/dbgutil/` also include
`<link.h>`, `<dlfcn.h>`, and `<sys/resource.h>`. Limiting the scope to PAL
would require either narrowing the consumer set (large refactor) or
duplicating the include-directory glue in every subdirectory that needs it.

## Link-time stubs

Header declarations alone are not enough — when the PAL references
`pthread_setschedparam` (declared in the shim) the linker still needs a
definition. Trivial stubs live in two files in `pal/src/arch/wasm/`:

- `wasm-stubs.cpp` — stubs shared by all WASM targets (browser-wasm and
  WASI), including `DBG_DebugBreak`, the unwinder (`unw_*`) stubs, and
  `pthread_setschedparam`.
- `wasi-stubs.cpp` — stubs only needed on WASI, where browser-wasm gets
  real implementations from Emscripten (e.g., `shm_open`, `RaiseException`,
  `pthread_getschedparam`).

Heavier shims like the `posix_memalign`-backed arena allocator that
replaces `mmap` for anonymous mappings live next to them in
`mmap-wasi.c`.

## When to remove a shim

A shim should be removed (and the symbol absorbed back into the shared PAL)
when one of the following becomes true:

1. The upstream WASI sysroot starts providing the missing declaration.
   Track [WebAssembly/wasi-libc](https://github.com/WebAssembly/wasi-libc)
   for additions.
2. The number of call sites in CoreCLR shrinks to a handful, at which point
   `HAVE_*` configure checks become cheaper than a wrapper header.
3. The symbol becomes completely unused in the WASI build, in which case
   the shim can be deleted outright.
