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

#undef PORTABLE_RID_ARCH

#if defined(TARGET_X86)
#define PORTABLE_RID_ARCH "x86"
#elif defined(TARGET_AMD64)
#define PORTABLE_RID_ARCH "x64";
#elif defined(TARGET_ARMV6)
#define PORTABLE_RID_ARCH "arm32";
#elif defined(TARGET_ARM)
#define PORTABLE_RID_ARCH "arm32";
#elif defined(TARGET_ARM64)
#define PORTABLE_RID_ARCH "arm64";
#elif defined(TARGET_S390X)
#define PORTABLE_RID_ARCH "s390x";
#elif defined(TARGET_LOONGARCH64)
#define PORTABLE_RID_ARCH "loongarch64";
#elif defined(TARGET_POWERPC64)
#define PORTABLE_RID_ARCH "ppc64le";
#endif

#undef PORTABLE_RID_OS

#if defined(TARGET_UNIX)

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

#elif defined(TARGET_WINDOWS)
#define PORTABLE_RID_OS "win"
#endif

#if defined(PORTABLE_RID_OS) && defined(PORTABLE_RID_ARCH)
const ep_char8_t* _ds_portable_rid_info = PORTABLE_RID_OS "-" PORTABLE_RID_ARCH;
#else
const ep_char8_t* _ds_portable_rid_info = "unknown";
#endif

#endif /* ENABLE_PERFTRACING */

extern const char quiet_linker_empty_file_warning_diagnostics_sources;
const char quiet_linker_empty_file_warning_diagnostics_sources = 0;
