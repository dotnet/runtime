#include "ds-rt-config.h"

#ifdef ENABLE_PERFTRACING

#include "ds-types.h"

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
#elif defined(TARGET_OSX) && !defined(TARGET_MACCAT)
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

#undef PORTABLE_RID_ARCH

#if defined(TARGET_AMD64)
#define PORTABLE_RID_ARCH "x64"
#elif defined(TARGET_ARM)
#define PORTABLE_RID_ARCH "arm"
#elif defined(TARGET_ARM64)
#define PORTABLE_RID_ARCH "arm64"
#elif defined(TARGET_ARMV6)
#define PORTABLE_RID_ARCH "armv6"
#elif defined(TARGET_LOONGARCH64)
#define PORTABLE_RID_ARCH "loongarch64"
#elif defined(TARGET_MIPS64)
#define PORTABLE_RID_ARCH "mips64"
#elif defined(TARGET_POWERPC64)
#define PORTABLE_RID_ARCH "ppc64le"
#elif defined(TARGET_RISCV64)
#define PORTABLE_RID_ARCH "riscv64"
#elif defined(TARGET_S390X)
#define PORTABLE_RID_ARCH "s390x"
#elif defined(TARGET_WASM)
#define PORTABLE_RID_ARCH "wasm"
#elif defined(TARGET_X86)
#define PORTABLE_RID_ARCH "x86"
#else
#error Unknown Architecture
#endif

const ep_char8_t* _ds_portable_rid_info = PORTABLE_RID_OS "-" PORTABLE_RID_ARCH;

#endif /* ENABLE_PERFTRACING */
