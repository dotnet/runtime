#include "ds-rt-config.h"

#ifdef ENABLE_PERFTRACING

// Option to include all internal source files into ds-sources.c.
#ifdef DS_INCLUDE_SOURCE_FILES
#ifndef DS_FORCE_INCLUDE_SOURCE_FILES
#define DS_FORCE_INCLUDE_SOURCE_FILES
#endif
#include "ds-server.c"
#include "ds-eventpipe-protocol.c"
#include "ds-dump-protocol.c"
#include "ds-ipc.c"
#include "ds-process-protocol.c"
#include "ds-profiler-protocol.c"
#include "ds-protocol.c"
#endif

#undef PORTABLE_RID_OS

#if defined(TARGET_BROWSER)
#define PORTABLE_RID_OS "browser"
#elif defined(TARGET_UNIX)

#if defined(TARGET_ANDROID)
#define PORTABLE_RID_OS "linux-bionic"
#elif defined(TARGET_LINUX_MUSL)
#define PORTABLE_RID_OS "linux-musl"
#elif defined(TARGET_LINUX)
#define PORTABLE_RID_OS "linux"
#elif defined(TARGET_OSX)
#define PORTABLE_RID_OS "osx"
#else
#define PORTABLE_RID_OS "unix"
#endif

#elif defined(TARGET_WASI)
#define PORTABLE_RID_OS "wasi"
#elif defined(TARGET_WINDOWS)
#define PORTABLE_RID_OS "win"
#else
#error Unknown OS
#endif

#define QUOTE_MACRO_HELPER(x)       #x
#define QUOTE_MACRO(x)              QUOTE_MACRO_HELPER(x)

const ep_char8_t* _ds_portable_rid_info = PORTABLE_RID_OS "-" QUOTE_MACRO(ARCH_TARGET_NAME);

#endif /* ENABLE_PERFTRACING */

extern const char quiet_linker_empty_file_warning_diagnostics_sources;
const char quiet_linker_empty_file_warning_diagnostics_sources = 0;
