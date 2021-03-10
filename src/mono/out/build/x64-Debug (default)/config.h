#ifndef __MONO_CONFIG_H__
#define __MONO_CONFIG_H__

#ifdef _MSC_VER

// FIXME This is all questionable but the logs are flooded and nothing else is fixing them.
#pragma warning(disable:4018) // signed/unsigned mismatch
#pragma warning(disable:4090) // const problem
#pragma warning(disable:4146) // unary minus operator applied to unsigned type, result still unsigned
#pragma warning(disable:4244) // integer conversion, possible loss of data
#pragma warning(disable:4267) // integer conversion, possible loss of data

// promote warnings to errors
#pragma warning(  error:4013) // function undefined; assuming extern returning int
#pragma warning(  error:4022) // call and prototype disagree
#pragma warning(  error:4047) // differs in level of indirection
#pragma warning(  error:4098) // void return returns a value
#pragma warning(  error:4113) // call and prototype disagree
#pragma warning(  error:4172) // returning address of local variable or temporary
#pragma warning(  error:4197) // top-level volatile in cast is ignored
#pragma warning(  error:4273) // inconsistent dll linkage
#pragma warning(  error:4293) // shift count negative or too big, undefined behavior
#pragma warning(  error:4312) // 'type cast': conversion from 'MonoNativeThreadId' to 'gpointer' of greater size
#pragma warning(  error:4715) // 'keyword' not all control paths return a value

#include <SDKDDKVer.h>

#if _WIN32_WINNT < 0x0601
#error "Mono requires Windows 7 or later."
#endif /* _WIN32_WINNT < 0x0601 */

#ifndef HAVE_WINAPI_FAMILY_SUPPORT

#define HAVE_WINAPI_FAMILY_SUPPORT

/* WIN API Family support */
#include <winapifamily.h>

#if WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)
	#define HAVE_CLASSIC_WINAPI_SUPPORT 1
	#define HAVE_UWP_WINAPI_SUPPORT 0
#elif WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_APP)
	#define HAVE_CLASSIC_WINAPI_SUPPORT 0
	#define HAVE_UWP_WINAPI_SUPPORT 1
#else
	#define HAVE_CLASSIC_WINAPI_SUPPORT 0
	#define HAVE_UWP_WINAPI_SUPPORT 0
#ifndef HAVE_EXTERN_DEFINED_WINAPI_SUPPORT
	#error Unsupported WINAPI family
#endif
#endif

#endif
#endif

/* Define to the full name of this package. */
/* #undef PACKAGE_NAME */

/* Define to the one symbol short name of this package. */
/* #undef PACKAGE_TARNAME */

/* Define to the version of this package. */
/* #undef PACKAGE_VERSION */

/* Define to the full name and version of this package. */
/* #undef PACKAGE_STRING */

/* Define to the address where bug reports for this package should be sent. */
/* #undef PACKAGE_BUGREPORT */

/* Define to the home page for this package. */
/* #undef PACKAGE_URL */

/* Version of the corlib-runtime interface */
#define MONO_CORLIB_VERSION "1A5E0066-58DC-428A-B21C-0AD6CDAE2789"

/* Disables the IO portability layer */
#define DISABLE_PORTABILITY 1

/* This platform does not support symlinks */
#define HOST_NO_SYMLINKS 1

/* pthread is a pointer */
/* #undef PTHREAD_POINTER_ID */

/* Targeting the Android platform */
/* #undef HOST_ANDROID */

/* ... */
/* #undef TARGET_ANDROID */

/* ... */
/* #undef USE_MACH_SEMA */

/* Targeting the Fuchsia platform */
/* #undef HOST_FUCHSIA */

/* Targeting the AIX and PASE platforms */
/* #undef HOST_AIX */

/* Host Platform is Win32 */
#define HOST_WIN32 1

/* Target Platform is Win32 */
#define TARGET_WIN32 1

/* Host Platform is Darwin */
/* #undef HOST_DARWIN */

/* Host Platform is iOS */
/* #undef HOST_IOS */

/* Host Platform is tvOS */
/* #undef HOST_TVOS */

/* Use classic Windows API support */
#define HAVE_CLASSIC_WINAPI_SUPPORT 1

/* Don't use UWP Windows API support */
/* #undef HAVE_UWP_WINAPI_SUPPORT */

/* Define to 1 if you have the ANSI C header files. */
/* #undef STDC_HEADERS */

/* Define to 1 if you have the <sys/types.h> header file. */
#define HAVE_SYS_TYPES_H 1

/* Define to 1 if you have the <sys/stat.h> header file. */
#define HAVE_SYS_STAT_H 1

/* Define to 1 if you have the <strings.h> header file. */
/* #undef HAVE_STRINGS_H */

/* Define to 1 if you have the <stdint.h> header file. */
#define HAVE_STDINT_H 1

/* Define to 1 if you have the <unistd.h> header file. */
/* #undef HAVE_UNISTD_H */

/* Define to 1 if `major', `minor', and `makedev' are declared in <mkdev.h>.
   */
/* #undef MAJOR_IN_MKDEV */

/* Define to 1 if `major', `minor', and `makedev' are declared in
   <sysmacros.h>. */
/* #undef MAJOR_IN_SYSMACROS */

/* Define to 1 if you have the <sys/filio.h> header file. */
/* #undef HAVE_SYS_FILIO_H */

/* Define to 1 if you have the <sys/sockio.h> header file. */
/* #undef HAVE_SYS_SOCKIO_H */

/* Define to 1 if you have the <netdb.h> header file. */
/* #undef HAVE_NETDB_H */

/* Define to 1 if you have the <utime.h> header file. */
/* #undef HAVE_UTIME_H */

/* Define to 1 if you have the <sys/utime.h> header file. */
#define HAVE_SYS_UTIME_H 1

/* Define to 1 if you have the <semaphore.h> header file. */
/* #undef HAVE_SEMAPHORE_H */

/* Define to 1 if you have the <sys/un.h> header file. */
/* #undef HAVE_SYS_UN_H */

/* Define to 1 if you have the <sys/syscall.h> header file. */
/* #undef HAVE_SYS_SYSCALL_H */

/* Define to 1 if you have the <sys/uio.h> header file. */
/* #undef HAVE_SYS_UIO_H */

/* Define to 1 if you have the <sys/param.h> header file. */
/* #undef HAVE_SYS_PARAM_H */

/* Define to 1 if you have the <sys/sysctl.h> header file. */
/* #undef HAVE_SYS_SYSCTL_H */

/* Define to 1 if you have the <libproc.h> header file. */
/* #undef HAVE_LIBPROC_H */

/* Define to 1 if you have the <sys/prctl.h> header file. */
/* #undef HAVE_SYS_PRCTL_H */

/* Define to 1 if you have the <gnu/lib-names.h> header file. */
/* #undef HAVE_GNU_LIB_NAMES_H */

/* Define to 1 if you have the <sys/socket.h> header file. */
/* #undef HAVE_SYS_SOCKET_H */

/* Define to 1 if you have the <sys/utsname.h> header file. */
/* #undef HAVE_SYS_UTSNAME_H */

/* Define to 1 if you have the <alloca.h> header file. */
/* #undef HAVE_ALLOCA_H */

/* Define to 1 if you have the <ucontext.h> header file. */
/* #undef HAVE_UCONTEXT_H */

/* Define to 1 if you have the <pwd.h> header file. */
/* #undef HAVE_PWD_H */

/* Define to 1 if you have the <sys/select.h> header file. */
/* #undef HAVE_SYS_SELECT_H */

/* Define to 1 if you have the <netinet/tcp.h> header file. */
/* #undef HAVE_NETINET_TCP_H */

/* Define to 1 if you have the <netinet/in.h> header file. */
/* #undef HAVE_NETINET_IN_H */

/* Define to 1 if you have the <link.h> header file. */
/* #undef HAVE_LINK_H */

/* Define to 1 if you have the <arpa/inet.h> header file. */
/* #undef HAVE_ARPA_INET_H */

/* Define to 1 if you have the <unwind.h> header file. */
/* #undef HAVE_UNWIND_H */

/* Define to 1 if you have the <sys/user.h> header file. */
/* #undef HAVE_SYS_USER_H */

/* Use static ICU */
/* #undef STATIC_ICU */

/* Use OS-provided zlib */
/* #undef HAVE_SYS_ZLIB */

/* Define to 1 if you have the <poll.h> header file. */
/* #undef HAVE_POLL_H */

/* Define to 1 if you have the <sys/poll.h> header file. */
/* #undef HAVE_SYS_POLL_H */

/* Define to 1 if you have the <sys/wait.h> header file. */
/* #undef HAVE_SYS_WAIT_H */

/* Define to 1 if you have the <wchar.h> header file. */
#define HAVE_WCHAR_H 1

/* Define to 1 if you have the <linux/magic.h> header file. */
/* #undef HAVE_LINUX_MAGIC_H */

/* Define to 1 if you have the <android/legacy_signal_inlines.h> header file.
   */
/* #undef HAVE_ANDROID_LEGACY_SIGNAL_INLINES_H */

/* Define to 1 if you have the <android/ndk-version.h> header file. */
/* #undef HAVE_ANDROID_NDK_VERSION_H */

/* Whether Android NDK unified headers are used */
/* #undef ANDROID_UNIFIED_HEADERS */

/* The size of `void *', as computed by sizeof. */
#define SIZEOF_VOID_P 8

/* The size of `long', as computed by sizeof. */
#define SIZEOF_LONG 4

/* The size of `int', as computed by sizeof. */
#define SIZEOF_INT 4

/* The size of `long long', as computed by sizeof. */
#define SIZEOF_LONG_LONG 8

/* Xen-specific behaviour */
/* #undef MONO_XEN_OPT */

/* Reduce runtime requirements (and capabilities) */
/* #undef MONO_SMALL_CONFIG */

/* Make jemalloc assert for mono */
/* #undef MONO_JEMALLOC_ASSERT */

/* Make jemalloc default for mono */
/* #undef MONO_JEMALLOC_DEFAULT */

/* Enable jemalloc usage for mono */
/* #undef MONO_JEMALLOC_ENABLED */

/* Do not include names of unmanaged functions in the crash dump */
/* #undef MONO_PRIVATE_CRASHES */

/* Do not create structured crash files during unmanaged crashes */
/* #undef DISABLE_STRUCTURED_CRASH */

/* String of disabled features */
#define DISABLED_FEATURES ""

/* Disable AOT Compiler */
/* #undef DISABLE_AOT */

/* Disable runtime debugging support */
/* #undef DISABLE_DEBUG */

/* Disable reflection emit support */
/* #undef DISABLE_REFLECTION_EMIT */

/* Disable support debug logging */
/* #undef DISABLE_LOGGING */

/* Disable COM support */
#define DISABLE_COM 1

/* Disable advanced SSA JIT optimizations */
/* #undef DISABLE_SSA */

/* Disable the JIT, only full-aot mode or interpreter will be supported by the
   runtime. */
/* #undef DISABLE_JIT */

/* Disable the interpreter. */
/* #undef DISABLE_INTERPRETER */

/* Some VES is available at runtime */
#define ENABLE_ILGEN 1

/* Disable non-blittable marshalling */
/* #undef DISABLE_NONBLITTABLE */

/* Disable SIMD intrinsics related optimizations. */
/* #undef DISABLE_SIMD */

/* Disable Soft Debugger Agent. */
/* #undef DISABLE_DEBUGGER_AGENT */

/* Disable Performance Counters. */
#define DISABLE_PERFCOUNTERS 1

/* Disable shared perfcounters. */
/* #undef DISABLE_SHARED_PERFCOUNTERS */

/* Disable support code for the LLDB plugin. */
/* #undef DISABLE_LLDB */

/* Disable support for .mdb symbol files. */
#define DISABLE_MDB 1

/* Disable assertion messages. */
/* #undef DISABLE_ASSERT_MESSAGES */

/* Disable runtime cleanup. */
#define DISABLE_CLEANUP 1

/* Disable concurrent gc support in SGEN. */
/* #undef DISABLE_SGEN_MAJOR_MARKSWEEP_CONC */

/* Disable minor=split support in SGEN. */
/* #undef DISABLE_SGEN_SPLIT_NURSERY */

/* Disable gc bridge support in SGEN. */
/* #undef DISABLE_SGEN_GC_BRIDGE */

/* Disable debug helpers in SGEN. */
/* #undef DISABLE_SGEN_DEBUG_HELPERS */

/* Disable sockets */
/* #undef DISABLE_SOCKETS */

/* Disables use of DllMaps in MonoVM */
#define DISABLE_DLLMAP 1

/* Disable Threads */
/* #undef DISABLE_THREADS */

/* GC description */
/* #undef DEFAULT_GC_NAME */

/* No GC support. */
/* #undef HAVE_NULL_GC */

/* Length of zero length arrays */
#define MONO_ZERO_LEN_ARRAY 1

/* Define to 1 if you have the `sigaction' function. */
/* #undef HAVE_SIGACTION */

/* Define to 1 if you have the `kill' function. */
/* #undef HAVE_KILL */

/* CLOCK_MONOTONIC */
/* #undef HAVE_CLOCK_MONOTONIC */

/* CLOCK_MONOTONIC_COARSE */
/* #undef HAVE_CLOCK_MONOTONIC_COARSE */

/* clockid_t */
/* #undef HAVE_CLOCKID_T */

/* mach_absolute_time */
/* #undef HAVE_MACH_ABSOLUTE_TIME */

/* gethrtime */
/* #undef HAVE_GETHRTIME */

/* read_real_time */
/* #undef HAVE_READ_REAL_TIME */

/* Define to 1 if you have the `clock_nanosleep' function. */
/* #undef HAVE_CLOCK_NANOSLEEP */

/* Does dlsym require leading underscore. */
/* #undef MONO_DL_NEED_USCORE */

/* Define to 1 if you have the <execinfo.h> header file. */
/* #undef HAVE_EXECINFO_H */

/* Define to 1 if you have the <sys/auxv.h> header file. */
/* #undef HAVE_SYS_AUXV_H */

/* Define to 1 if you have the <sys/resource.h> header file. */
/* #undef HAVE_SYS_RESOURCE_H */

/* kqueue */
/* #undef HAVE_KQUEUE */

/* Define to 1 if you have the `backtrace_symbols' function. */
/* #undef HAVE_BACKTRACE_SYMBOLS */

/* Define to 1 if you have the `mkstemp' function. */
/* #undef HAVE_MKSTEMP */

/* Define to 1 if you have the `mmap' function. */
/* #undef HAVE_MMAP */

/* Define to 1 if you have the `madvise' function. */
/* #undef HAVE_MADVISE */

/* Define to 1 if you have the `getrusage' function. */
/* #undef HAVE_GETRUSAGE */

/* Define to 1 if you have the `dladdr' function. */
/* #undef HAVE_DLADDR */

/* Define to 1 if you have the `sysconf' function. */
/* #undef HAVE_SYSCONF */

/* Define to 1 if you have the `getrlimit' function. */
/* #undef HAVE_GETRLIMIT */

/* Define to 1 if you have the `prctl' function. */
/* #undef HAVE_PRCTL */

/* Define to 1 if you have the `nl_langinfo' function. */
/* #undef HAVE_NL_LANGINFO */

/* sched_getaffinity */
/* #undef HAVE_SCHED_GETAFFINITY */

/* sched_setaffinity */
/* #undef HAVE_SCHED_SETAFFINITY */

/* Define to 1 if you have the `sched_getcpu' function. */
/* #undef HAVE_SCHED_GETCPU */

/* Define to 1 if you have the `getpwuid_r' function. */
/* #undef HAVE_GETPWUID_R */

/* Define to 1 if you have the `readlink' function. */
/* #undef HAVE_READLINK */

/* Define to 1 if you have the `chmod' function. */
#define HAVE_CHMOD 1

/* Define to 1 if you have the `lstat' function. */
/* #undef HAVE_LSTAT */

/* Define to 1 if you have the `getdtablesize' function. */
/* #undef HAVE_GETDTABLESIZE */

/* Define to 1 if you have the `ftruncate' function. */
/* #undef HAVE_FTRUNCATE */

/* Define to 1 if you have the `msync' function. */
/* #undef HAVE_MSYNC */

/* Define to 1 if you have the `getpeername' function. */
/* #undef HAVE_GETPEERNAME */

/* Define to 1 if you have the `utime' function. */
#define HAVE_UTIME 1

/* Define to 1 if you have the `utimes' function. */
/* #undef HAVE_UTIMES */

/* Define to 1 if you have the `openlog' function. */
/* #undef HAVE_OPENLOG */

/* Define to 1 if you have the `closelog' function. */
/* #undef HAVE_CLOSELOG */

/* Define to 1 if you have the `atexit' function. */
#define HAVE_ATEXIT 1

/* Define to 1 if you have the `popen' function. */
/* #undef HAVE_POPEN */

/* Define to 1 if you have the `strerror_r' function. */
/* #undef HAVE_STRERROR_R */

/* Define to 1 if strerror_r returns char *. */
/* #undef STRERROR_R_CHAR_P */

/* Have GLIBC_BEFORE_2_3_4_SCHED_SETAFFINITY */
/* #undef GLIBC_BEFORE_2_3_4_SCHED_SETAFFINITY */

/* GLIBC has CPU_COUNT macro in sched.h */
/* #undef GLIBC_HAS_CPU_COUNT */

/* Have large file support */
/* #undef HAVE_LARGE_FILE_SUPPORT */

/* Have getaddrinfo */
#define HAVE_GETADDRINFO 1

/* Have gethostbyname2 */
/* #undef HAVE_GETHOSTBYNAME2 */

/* Have gethostbyname */
/* #undef HAVE_GETHOSTBYNAME */

/* Have getprotobyname */
#define HAVE_GETPROTOBYNAME 1

/* Have getprotobyname_r */
/* #undef HAVE_GETPROTOBYNAME_R */

/* Have getnameinfo */
#define HAVE_GETNAMEINFO 1

/* Have inet_ntop */
#define HAVE_INET_NTOP 1

/* Have inet_pton */
#define HAVE_INET_PTON 1

/* Define to 1 if you have the `inet_aton' function. */
/* #undef HAVE_INET_ATON */

/* Define to 1 if you have the <pthread.h> header file. */
/* #undef HAVE_PTHREAD_H */

/* Define to 1 if you have the <pthread_np.h> header file. */
/* #undef HAVE_PTHREAD_NP_H */

/* Define to 1 if you have the `pthread_mutex_timedlock' function. */
/* #undef HAVE_PTHREAD_MUTEX_TIMEDLOCK */

/* Define to 1 if you have the `pthread_getattr_np' function. */
/* #undef HAVE_PTHREAD_GETATTR_NP */

/* Define to 1 if you have the `pthread_attr_get_np' function. */
/* #undef HAVE_PTHREAD_ATTR_GET_NP */

/* Define to 1 if you have the `pthread_getname_np' function. */
/* #undef HAVE_PTHREAD_GETNAME_NP */

/* Define to 1 if you have the `pthread_setname_np' function. */
/* #undef HAVE_PTHREAD_SETNAME_NP */

/* Define to 1 if you have the `pthread_cond_timedwait_relative_np' function.
   */
/* #undef HAVE_PTHREAD_COND_TIMEDWAIT_RELATIVE_NP */

/* Define to 1 if you have the `pthread_kill' function. */
/* #undef HAVE_PTHREAD_KILL */

/* Define to 1 if you have the `pthread_attr_setstacksize' function. */
/* #undef HAVE_PTHREAD_ATTR_SETSTACKSIZE */

/* Define to 1 if you have the `pthread_attr_getstack' function. */
/* #undef HAVE_PTHREAD_ATTR_GETSTACK */

/* Define to 1 if you have the `pthread_attr_getstacksize' function. */
/* #undef HAVE_PTHREAD_ATTR_GETSTACKSIZE */

/* Define to 1 if you have the `pthread_get_stacksize_np' function. */
/* #undef HAVE_PTHREAD_GET_STACKSIZE_NP */

/* Define to 1 if you have the `pthread_get_stackaddr_np' function. */
/* #undef HAVE_PTHREAD_GET_STACKADDR_NP */

/* Define to 1 if you have the declaration of `pthread_mutexattr_setprotocol',
   and to 0 if you don't. */
/* #undef HAVE_DECL_PTHREAD_MUTEXATTR_SETPROTOCOL */

/* Have a working sigaltstack */
/* #undef HAVE_WORKING_SIGALTSTACK */

/* Define to 1 if you have the `shm_open' function. */
/* #undef HAVE_SHM_OPEN */

/* Define to 1 if you have the `poll' function. */
/* #undef HAVE_POLL */

/* epoll_create1 */
/* #undef HAVE_EPOLL */

/* Define to 1 if you have the <sys/ioctl.h> header file. */
/* #undef HAVE_SYS_IOCTL_H */

/* Define to 1 if you have the <net/if.h> header file. */
/* #undef HAVE_NET_IF_H */

/* Can get interface list */
/* #undef HAVE_SIOCGIFCONF */

/* sockaddr_in has sin_len */
/* #undef HAVE_SOCKADDR_IN_SIN_LEN */

/* sockaddr_in6 has sin6_len */
/* #undef HAVE_SOCKADDR_IN6_SIN_LEN */

/* Have getifaddrs */
/* #undef HAVE_GETIFADDRS */

/* Have access */
#define HAVE_ACCESS 1

/* Define to 1 if you have the <sys/errno.h> header file. */
/* #undef HAVE_SYS_ERRNO_H */

/* Define to 1 if you have the <sys/sendfile.h> header file. */
/* #undef HAVE_SYS_SENDFILE_H */

/* Define to 1 if you have the <sys/statvfs.h> header file. */
/* #undef HAVE_SYS_STATVFS_H */

/* Define to 1 if you have the <sys/statfs.h> header file. */
/* #undef HAVE_SYS_STATFS_H */

/* Define to 1 if you have the <sys/mman.h> header file. */
/* #undef HAVE_SYS_MMAN_H */

/* Define to 1 if you have the <sys/mount.h> header file. */
/* #undef HAVE_SYS_MOUNT_H */

/* Define to 1 if you have the `getfsstat' function. */
/* #undef HAVE_GETFSSTAT */

/* Define to 1 if you have the `mremap' function. */
/* #undef HAVE_MREMAP */

/* Define to 1 if you have the `posix_fadvise' function. */
/* #undef HAVE_POSIX_FADVISE */

/* Define to 1 if you have the `vsnprintf' function. */
/* #undef HAVE_VSNPRINTF */

/* Define to 1 if you have the `sendfile' function. */
/* #undef HAVE_SENDFILE */

/* struct statfs */
/* #undef HAVE_STATFS */

/* Define to 1 if you have the `statvfs' function. */
/* #undef HAVE_STATVFS */

/* Define to 1 if you have the `setpgid' function. */
/* #undef HAVE_SETPGID */

/* Define to 1 if you have the `system' function. */
#ifdef _MSC_VER
#if HAVE_WINAPI_FAMILY_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)
#define HAVE_SYSTEM 1
#endif
#else
#define HAVE_SYSTEM 1
#endif

/* Define to 1 if you have the `fork' function. */
/* #undef HAVE_FORK */

/* Define to 1 if you have the `execv' function. */
#define HAVE_EXECV 1

/* Define to 1 if you have the `execve' function. */
#define HAVE_EXECVE 1

/* Define to 1 if you have the `waitpid' function. */
/* #undef HAVE_WAITPID */

/* Define to 1 if you have the `localtime_r' function. */
/* #undef HAVE_LOCALTIME_R */

/* Define to 1 if you have the `mkdtemp' function. */
/* #undef HAVE_MKDTEMP */

/* The size of `size_t', as computed by sizeof. */
#define SIZEOF_SIZE_T 8

/* Define to 1 if the system has the type `struct sockaddr'. */
/* #undef HAVE_STRUCT_SOCKADDR */

/* Define to 1 if the system has the type `struct sockaddr_in'. */
/* #undef HAVE_STRUCT_SOCKADDR_IN */

/* Define to 1 if the system has the type `struct sockaddr_in6'. */
#define HAVE_STRUCT_SOCKADDR_IN6 1

/* Define to 1 if the system has the type `struct stat'. */
/* #undef HAVE_STRUCT_STAT */

/* Define to 1 if the system has the type `struct timeval'. */
/* #undef HAVE_STRUCT_TIMEVAL */

/* Define to 1 if `st_atim' is a member of `struct stat'. */
/* #undef HAVE_STRUCT_STAT_ST_ATIM */

/* Define to 1 if `st_atimespec' is a member of `struct stat'. */
/* #undef HAVE_STRUCT_STAT_ST_ATIMESPEC */

/* Define to 1 if `kp_proc' is a member of `struct kinfo_proc'. */
/* #undef HAVE_STRUCT_KINFO_PROC_KP_PROC */

/* Define to 1 if you have the <sys/time.h> header file. */
/* #undef HAVE_SYS_TIME_H */

/* Define to 1 if you have the <dirent.h> header file. */
/* #undef HAVE_DIRENT_H */

/* Define to 1 if you have the <CommonCrypto/CommonDigest.h> header file. */
/* #undef HAVE_COMMONCRYPTO_COMMONDIGEST_H */

/* Define to 1 if you have the <sys/random.h> header file. */
/* #undef HAVE_SYS_RANDOM_H */

/* Define to 1 if you have the `getrandom' function. */
/* #undef HAVE_GETRANDOM */

/* Define to 1 if you have the `getentropy' function. */
/* #undef HAVE_GETENTROPY */

/* Qp2getifaddrs */
/* #undef HAVE_QP2GETIFADDRS */

/* Define to 1 if you have the `strlcpy' function. */
/* #undef HAVE_STRLCPY */

/* Define to 1 if you have the <winternl.h> header file. */
#define HAVE_WINTERNL_H 1

/* Have socklen_t */
/* #undef HAVE_SOCKLEN_T */

/* Define to 1 if you have the `execvp' function. */
/* #undef HAVE_EXECVP */

/* Name of /dev/random */
#define NAME_DEV_RANDOM ""

/* Enable the allocation and indexing of arrays greater than Int32.MaxValue */
/* #undef MONO_BIG_ARRAYS */

/* Enable DTrace probes */
/* #undef ENABLE_DTRACE */

/* AOT cross offsets file */
/* #undef MONO_OFFSETS_FILE */

/* Enable the LLVM back end */
/* #undef ENABLE_LLVM */

/* Runtime support code for llvm enabled */
/* #undef ENABLE_LLVM_RUNTIME */

/* 64 bit mode with 4 byte longs and pointers */
/* #undef MONO_ARCH_ILP32 */

/* The runtime is compiled for cross-compiling mode */
/* #undef MONO_CROSS_COMPILE */

/* ... */
/* #undef TARGET_WASM */

/* The JIT/AOT targets WatchOS */
/* #undef TARGET_WATCHOS */

/* ... */
/* #undef TARGET_PS3 */

/* ... */
/* #undef __mono_ppc64__ */

/* ... */
/* #undef TARGET_XBOX360 */

/* ... */
/* #undef TARGET_PS4 */

/* ... */
/* #undef DISABLE_HW_TRAPS */

/* Target is RISC-V */
/* #undef TARGET_RISCV */

/* Target is 32-bit RISC-V */
/* #undef TARGET_RISCV32 */

/* Target is 64-bit RISC-V */
/* #undef TARGET_RISCV64 */

/* ... */
/* #undef TARGET_X86 */

/* ... */
#define TARGET_AMD64 1

/* ... */
/* #undef TARGET_ARM */

/* ... */
/* #undef TARGET_ARM64 */

/* ... */
/* #undef TARGET_POWERPC */

/* ... */
/* #undef TARGET_POWERPC64 */

/* ... */
/* #undef TARGET_S390X */

/* ... */
/* #undef TARGET_MIPS */

/* ... */
/* #undef TARGET_SPARC */

/* ... */
/* #undef TARGET_SPARC64 */

/* ... */
/* #undef HOST_WASM */

/* ... */
/* #undef HOST_X86 */

/* ... */
#define HOST_AMD64 1

/* ... */
/* #undef HOST_ARM */

/* ... */
/* #undef HOST_ARM64 */

/* ... */
/* #undef HOST_POWERPC */

/* ... */
/* #undef HOST_POWERPC64 */

/* ... */
/* #undef HOST_S390X */

/* ... */
/* #undef HOST_MIPS */

/* ... */
/* #undef HOST_SPARC */

/* ... */
/* #undef HOST_SPARC64 */

/* Host is RISC-V */
/* #undef HOST_RISCV */

/* Host is 32-bit RISC-V */
/* #undef HOST_RISCV32 */

/* Host is 64-bit RISC-V */
/* #undef HOST_RISCV64 */

/* ... */
/* #undef USE_GCC_ATOMIC_OPS */

/* The JIT/AOT targets iOS */
/* #undef TARGET_IOS */

/* The JIT/AOT targets OSX */
/* #undef TARGET_OSX */

/* The JIT/AOT targets Apple platforms */
/* #undef TARGET_MACH */

/* byte order of target */
#define TARGET_BYTE_ORDER G_LITTLE_ENDIAN

/* wordsize of target */
#define TARGET_SIZEOF_VOID_P 8

/* size of target machine integer registers */
#define SIZEOF_REGISTER 8

/* Support for the visibility ("hidden") attribute */
/* #undef HAVE_VISIBILITY_HIDDEN */

/* Support for the deprecated attribute */
/* #undef HAVE_DEPRECATED */

/* Moving collector */
#define HAVE_MOVING_COLLECTOR 1

/* Defaults to concurrent GC */
#define HAVE_CONC_GC_AS_DEFAULT 1

/* Define to 1 if you have the `stpcpy' function. */
/* #undef HAVE_STPCPY */

/* Define to 1 if you have the `strtok_r' function. */
#define HAVE_STRTOK_R 1

/* Define to 1 if you have the `rewinddir' function. */
/* #undef HAVE_REWINDDIR */

/* Define to 1 if you have the `vasprintf' function. */
/* #undef HAVE_VASPRINTF */

/* Overridable allocator support enabled */
/* #undef ENABLE_OVERRIDABLE_ALLOCATORS */

/* Define to 1 if you have the `strndup' function. */
/* #undef HAVE_STRNDUP */

/* Define to 1 if you have the <getopt.h> header file. */
/* #undef HAVE_GETOPT_H */

/* Define to 1 if you have the <iconv.h> header file. */
/* #undef HAVE_ICONV_H */

/* Define to 1 if you have the `iconv' library (-liconv). */
/* #undef HAVE_LIBICONV */

/* Icall symbol map enabled */
/* #undef ENABLE_ICALL_SYMBOL_MAP */

/* Icall export enabled */
/* #undef ENABLE_ICALL_EXPORT */

/* Icall tables disabled */
/* #undef DISABLE_ICALL_TABLES */

/* QCalls disabled */
/* #undef DISABLE_QCALLS */

/* Have __thread keyword */
#define MONO_KEYWORD_THREAD __declspec (thread)

/* tls_model available */
/* #undef HAVE_TLS_MODEL_ATTR */

/* ARM v5 */
/* #undef HAVE_ARMV5 */

/* ARM v6 */
/* #undef HAVE_ARMV6 */

/* ARM v7 */
/* #undef HAVE_ARMV7 */

/* RISC-V FPABI is double-precision */
/* #undef RISCV_FPABI_DOUBLE */

/* RISC-V FPABI is single-precision */
/* #undef RISCV_FPABI_SINGLE */

/* RISC-V FPABI is soft float */
/* #undef RISCV_FPABI_SOFT */

/* Use malloc for each single mempool allocation */
/* #undef USE_MALLOC_FOR_MEMPOOLS */

/* Enable lazy gc thread creation by the embedding host. */
/* #undef LAZY_GC_THREAD_CREATION */

/* Enable cooperative stop-the-world garbage collection. */
/* #undef ENABLE_COOP_SUSPEND */

/* Enable hybrid suspend for GC stop-the-world */
/* #undef ENABLE_HYBRID_SUSPEND */

/* Enable feature experiments */
/* #undef ENABLE_EXPERIMENTS */

/* Enable experiment 'null' */
/* #undef ENABLE_EXPERIMENT_null */

/* Enable experiment 'Tiered Compilation' */
/* #undef ENABLE_EXPERIMENT_TIERED */

/* Enable checked build */
#define ENABLE_CHECKED_BUILD 1

/* Enable GC checked build */
/* #undef ENABLE_CHECKED_BUILD_GC */

/* Enable metadata checked build */
/* #undef ENABLE_CHECKED_BUILD_METADATA */

/* Enable thread checked build */
/* #undef ENABLE_CHECKED_BUILD_THREAD */

/* Enable private types checked build */
#define ENABLE_CHECKED_BUILD_PRIVATE_TYPES 1

/* Enable private types checked build */
/* #undef ENABLE_CHECKED_BUILD_CRASH_REPORTING */

/* Enable EventPipe library support */
#define ENABLE_PERFTRACING 1

/* Define to 1 if you have /usr/include/malloc.h. */
/* #undef HAVE_USR_INCLUDE_MALLOC_H */

/* The architecture this is running on */
#define MONO_ARCHITECTURE "amd64"

/* Disable banned functions from being used by the runtime */
#define MONO_INSIDE_RUNTIME 1

/* Version number of package */
#define VERSION "6.0.0.0"

/* Full version number of package */
#define FULL_VERSION 

/* Define to 1 if you have the <dlfcn.h> header file. */
/* #undef HAVE_DLFCN_H */

/* Disable crash reporting subsystem */
#define DISABLE_CRASH_REPORTING 1

/* Enable lazy gc thread creation by the embedding host */
/* #undef LAZY_GC_THREAD_CREATION */

/* Enable additional checks */
#define ENABLE_CHECKED_BUILD 1

/* Enable compile time checking that getter functions are used */
#define ENABLE_CHECKED_BUILD_PRIVATE_TYPES 1

/* Enable runtime GC Safe / Unsafe mode assertion checks (must set env var MONO_CHECK_MODE=gc) */
/* #undef ENABLE_CHECKED_BUILD_GC */

/* Enable runtime history of per-thread coop state transitions (must set env var MONO_CHECK_MODE=thread) */
/* #undef ENABLE_CHECKED_BUILD_THREAD */

/* Enable runtime checks of mempool references between metadata images (must set env var MONO_CHECK_MODE=metadata) */
/* #undef ENABLE_CHECKED_BUILD_METADATA */

/* Enable runtime support for metadata updates */
/* #undef ENABLE_METADATA_UPDATE */

#if defined(ENABLE_LLVM) && defined(HOST_WIN32) && defined(TARGET_WIN32) && (!defined(TARGET_AMD64) || !defined(_MSC_VER))
#error LLVM for host=Windows and target=Windows is only supported on x64 MSVC build.
#endif

#endif

