#ifndef _MSC_VER
#include "cygconfig.h"
#else

/* The architecture this is running on */
#if defined(_M_IA64)
#define MONO_ARCHITECTURE "ia64"
#elif defined(_M_AMD64)
#define MONO_ARCHITECTURE "amd64"
#elif defined(_M_IX86)
#define MONO_ARCHITECTURE "x86"
#else
#error Unknown architecture
#endif

#ifndef WINVER
#define WINVER 0x0A00
#endif

#include <SDKDDKVer.h>

#if _WIN32_WINNT < 0x0600
#error "Mono requires Windows Vista or later"
#endif /* _WIN32_WINNT < 0x0600 */

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
#ifndef HAVE_EXTERN_DEFINED_WINAPI_SUPPORT
	#error Unsupported WINAPI family
#endif
#else
	#define HAVE_CLASSIC_WINAPI_SUPPORT 0
	#define HAVE_UWP_WINAPI_SUPPORT 0
#ifndef HAVE_EXTERN_DEFINED_WINAPI_SUPPORT
	#error Unsupported WINAPI family
#endif
#endif

#endif

/*
 * Features that are not required in the Windows port
 */
#define DISABLE_PORTABILITY 1

/* Windows does not have symlinks */
#define HOST_NO_SYMLINKS 1

/* String of disabled features */
#define DISABLED_FEATURES "none"

/* Disable AOT support */
/* #undef DISABLE_AOT */

/* Disable COM support */
/* #undef DISABLE_COM */

/* Disable runtime debugging support */
/* #undef DISABLE_DEBUG */

/* Disable System.Decimal support */
/* #undef DISABLE_DECIMAL */

/* Disable generics support */
/* #undef DISABLE_GENERICS */

/* Disable support for huge assemblies */
/* #undef DISABLE_LARGE_CODE */

/* Disable support debug logging */
/* #undef DISABLE_LOGGING */

/* Disable P/Invoke support */
/* #undef DISABLE_PINVOKE */

/* Disable default profiler support */
/* #undef DISABLE_PROFILER */

/* Disable reflection emit support */
/* #undef DISABLE_REFLECTION_EMIT */

/* Disable advanced SSA JIT optimizations */
/* #undef DISABLE_SSA */

/* Disable interpreter */
/* #undef DISABLE_INTERPRETER */

/* Enable DTrace probes */
/* #undef ENABLE_DTRACE */

/* Has the 'aintl' function */
/* #undef HAVE_AINTL */

/* Supports C99 array initialization */
/* #undef HAVE_ARRAY_ELEM_INIT */

/* Define to 1 if you have the <attr/xattr.h> header file. */
/* #undef HAVE_ATTR_XATTR_H */

/* Define to 1 if you have the `backtrace_symbols' function. */
/* #undef HAVE_BACKTRACE_SYMBOLS */

/* Define to 1 if the system has the type `blkcnt_t'. */
/* #undef HAVE_BLKCNT_T */

/* Define to 1 if the system has the type `blksize_t'. */
/* #undef HAVE_BLKSIZE_T */

/* Have Boehm GC */
/* #define HAVE_BOEHM_GC 1 */

/* Define to 1 if you have the <checklist.h> header file. */
/* #undef HAVE_CHECKLIST_H */

/* Define to 1 if you have the <complex.h> header file. */
#define HAVE_COMPLEX_H 1

/* Define to 1 if you have the `system' function. */
#if HAVE_WINAPI_FAMILY_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)
#define HAVE_SYSTEM 1
#endif


/* Have /dev/random */
#define HAVE_CRYPT_RNG 1

/* Define to 1 if you have the <curses.h> header file. */
/* #undef HAVE_CURSES_H */

/* Define to 1 if you have the declaration of `InterlockedAdd',
   and to 0 if you don't. */
#define HAVE_DECL_INTERLOCKEDADD 1

/* Define to 1 if you have the declaration of `InterlockedAdd64',
   and to 0 if you don't. */
#define HAVE_DECL_INTERLOCKEDADD64 1

/* Define to 1 if you have the declaration of `InterlockedCompareExchange64',
   and to 0 if you don't. */
#define HAVE_DECL_INTERLOCKEDCOMPAREEXCHANGE64 1

/* Define to 1 if you have the declaration of `InterlockedDecrement64',
   and to 0 if you don't. */
#define HAVE_DECL_INTERLOCKEDDECREMENT64 1

/* Define to 1 if you have the declaration of `InterlockedExchange64',
   and to 0 if you don't. */
#define HAVE_DECL_INTERLOCKEDEXCHANGE64 1

/* Define to 1 if you have the declaration of `InterlockedIncrement64',
   and to 0 if you don't. */
#define HAVE_DECL_INTERLOCKEDINCREMENT64 1

/* Define to 1 if you have the declaration of `__readfsdword',
   and to 0 if you don't. */
#define HAVE_DECL___READFSDWORD 1

/* Define to 1 if you have the <dirent.h> header file. */
/* #define HAVE_DIRENT_H 1 */

/* Define to 1 if you have the <dlfcn.h> header file. */
/* #undef HAVE_DLFCN_H */

/* dlopen-based dynamic loader available */
/* #undef HAVE_DL_LOADER */

/* Define to 1 if you have the <elf.h> header file. */
/* #undef HAVE_ELF_H */

/* epoll supported */
/* #undef HAVE_EPOLL */

/* Define to 1 if you have the `epoll_ctl' function. */
/* #undef HAVE_EPOLL_CTL */

/* Define to 1 if you have the <execinfo.h> header file. */
/* #undef HAVE_EXECINFO_H */

/* Define to 1 if you have the `fgetgrent' function. */
/* #undef HAVE_FGETGRENT */

/* Define to 1 if you have the `fgetpwent' function. */
/* #undef HAVE_FGETPWENT */

/* Define to 1 if you have the `finite' function. */
/* #undef HAVE_FINITE */

/* Define to 1 if you have the <fstab.h> header file. */
/* #undef HAVE_FSTAB_H */

/* Define to 1 if you have the `fstatfs' function. */
/* #undef HAVE_FSTATFS */

/* Define to 1 if you have the `fstatvfs' function. */
/* #undef HAVE_FSTATVFS */

/* Define to 1 if you have the `getaddrinfo' function. */
#define HAVE_GETADDRINFO 1

/* Define to 1 if you have the `getnameinfo' function. */
#define HAVE_GETNAMEINFO 1

/* Define to 1 if you have the `getprotobyname' function. */
#define HAVE_GETPROTOBYNAME 1

/* Define to 1 if you have the `getdomainname' function. */
/* #undef HAVE_GETDOMAINNAME */

/* Define to 1 if you have the `getfsstat' function. */
/* #undef HAVE_GETFSSTAT */

/* Define to 1 if you have the `getgrgid_r' function. */
/* #undef HAVE_GETGRGID_R */

/* Define to 1 if you have the `getgrnam_r' function. */
/* #undef HAVE_GETGRNAM_R */

/* Have gethostbyname2_r */
/* #undef HAVE_GETHOSTBYNAME2_R */

/* Define to 1 if you have the `getpriority' function. */
/* #undef HAVE_GETPRIORITY */

/* Define to 1 if you have the `GetProcessId' function. */
#define HAVE_GETPROCESSID 1

/* Define to 1 if you have the `getpwnam_r' function. */
/* #undef HAVE_GETPWNAM_R */

/* Define to 1 if you have the `getpwuid_r' function. */
/* #undef HAVE_GETPWUID_R */

/* Define to 1 if you have the `getresuid' function. */
/* #undef HAVE_GETRESUID */

/* Define to 1 if you have the `getrusage' function. */
/* #undef HAVE_GETRUSAGE */

/* Define to 1 if you have the <grp.h> header file. */
/* #undef HAVE_GRP_H */

/* Define to 1 if you have the <ieeefp.h> header file. */
/* #undef HAVE_IEEEFP_H */

/* Define to 1 if you have the `inet_aton' function. */
/* #undef HAVE_INET_ATON */

/* Define to 1 if you have the `inet_pton' function. */
#define HAVE_INET_PTON 1

/* Define to 1 if you have the <inttypes.h> header file. */
#define HAVE_INTTYPES_H 1

/* Have IPV6_PKTINFO */
/* #undef HAVE_IPV6_PKTINFO */

/* Have IP_DONTFRAGMENT */
/* #undef HAVE_IP_DONTFRAGMENT */

/* Have IP_MTU_DISCOVER */
/* #undef HAVE_IP_MTU_DISCOVER */

/* Have IP_PKTINFO */
/* #undef HAVE_IP_PKTINFO */

/* Define to 1 if you have the `isfinite' function. */
/* #undef HAVE_ISFINITE */

/* isinf available */
#define HAVE_ISINF 1

/* Define to 1 if you have the `kqueue' function. */
/* #undef HAVE_KQUEUE */

/* Have __thread keyword */
/* #undef HAVE_KW_THREAD */

/* Have large file support */
/* #undef HAVE_LARGE_FILE_SUPPORT */

/* Define to 1 if you have the `unwind' library (-lunwind). */
/* #undef HAVE_LIBUNWIND */

/* Define to 1 if you have the <linux/rtc.h> header file. */
/* #undef HAVE_LINUX_RTC_H */

/* Define to 1 if you have the `lutimes' function. */
/* #undef HAVE_LUTIMES */

/* Define to 1 if you have the `madvise' function. */
/* #undef HAVE_MADVISE */

/* Define to 1 if you have the <memory.h> header file. */
#define HAVE_MEMORY_H 1

/* Define to 1 if you have the `mkstemp' function. */
/* #undef HAVE_MKSTEMP */

/* Define to 1 if you have the `mmap' function. */
/* #undef HAVE_MMAP */

/* The GC can move objects. */
/* #undef HAVE_MOVING_COLLECTOR */

/* Define to 1 if you have the `mremap' function. */
/* #undef HAVE_MREMAP */

/* Have MSG_NOSIGNAL */
/* #undef HAVE_MSG_NOSIGNAL */

/* Define to 1 if you have the <netdb.h> header file. */
/* #undef HAVE_NETDB_H */

/* Define to 1 if you have the <net/if.h> header file. */
/* #undef HAVE_NET_IF_H */

/* No GC support. */
/* #undef HAVE_NULL_GC */

/* Define to 1 if you have the `poll' function. */
/* #undef HAVE_POLL */

/* Define to 1 if you have the <poll.h> header file. */
/* #undef HAVE_POLL_H */

/* Define to 1 if you have the `posix_fadvise' function. */
/* #undef HAVE_POSIX_FADVISE */

/* Define to 1 if you have the `posix_fallocate' function. */
/* #undef HAVE_POSIX_FALLOCATE */

/* Define to 1 if you have the `posix_madvise' function. */
/* #undef HAVE_POSIX_MADVISE */

/* Define to 1 if you have the `pthread_attr_getstack' function. */
/* #undef HAVE_PTHREAD_ATTR_GETSTACK */

/* Define to 1 if you have the `pthread_attr_get_np' function. */
/* #undef HAVE_PTHREAD_ATTR_GET_NP */

/* Define to 1 if you have the `pthread_attr_setstacksize' function. */
/* #undef HAVE_PTHREAD_ATTR_SETSTACKSIZE */

/* Define to 1 if you have the `pthread_getattr_np' function. */
/* #undef HAVE_PTHREAD_GETATTR_NP */

/* Define to 1 if you have the `pthread_get_stackaddr_np' function. */
/* #undef HAVE_PTHREAD_GET_STACKADDR_NP */

/* Define to 1 if you have the `pthread_get_stacksize_np' function. */
/* #undef HAVE_PTHREAD_GET_STACKSIZE_NP */

/* Define to 1 if you have the <pthread.h> header file. */
/* #undef HAVE_PTHREAD_H */

/* Define to 1 if you have the `pthread_mutex_timedlock' function. */
/* #undef HAVE_PTHREAD_MUTEX_TIMEDLOCK */

/* Define to 1 if you have the `remap_file_pages' function. */
/* #undef HAVE_REMAP_FILE_PAGES */

/* Define to 1 if you have the `sched_setaffinity' function. */
/* #undef HAVE_SCHED_SETAFFINITY */

/* Define to 1 if you have the <semaphore.h> header file. */
/* #undef HAVE_SEMAPHORE_H */

/* Define to 1 if you have the `sendfile' function. */
/* #undef HAVE_SENDFILE */

/* Define to 1 if you have the `setdomainname' function. */
/* #undef HAVE_SETDOMAINNAME */

/* Define to 1 if you have the `sethostid' function. */
/* #undef HAVE_SETHOSTID */

/* Define to 1 if you have the `setpriority' function. */
/* #undef HAVE_SETPRIORITY */

/* Define to 1 if you have the `setresuid' function. */
/* #undef HAVE_SETRESUID */

/* Using the simple generational GC. */
/* #undef HAVE_SGEN_GC */

 /* Have signal */
#define HAVE_SIGNAL 1

 /* Define to 1 if you have the <signal.h> header file. */
#define HAVE_SIGNAL_H 1

/* Have signbit */
/* #undef HAVE_SIGNBIT */

/* Can get interface list */
/* #undef HAVE_SIOCGIFCONF */

/* sockaddr_in6 has sin6_len */
/* #undef HAVE_SOCKADDR_IN6_SIN_LEN */

/* sockaddr_in has sin_len */
/* #undef HAVE_SOCKADDR_IN_SIN_LEN */

/* Have socklen_t */
/* #undef HAVE_SOCKLEN_T */

/* Have SOL_IP */
/* #undef HAVE_SOL_IP */

/* Have SOL_IPV6 */
/* #undef HAVE_SOL_IPV6 */

/* Have SOL_TCP */
/* #undef HAVE_SOL_TCP */

/* Define to 1 if you have the `statfs' function. */
/* #undef HAVE_STATFS */

/* Define to 1 if you have the `statvfs' function. */
/* #undef HAVE_STATVFS */

/* Define to 1 if you have the <stdint.h> header file. */
/* #define HAVE_STDINT_H 1 */

/* Define to 1 if you have the <stdlib.h> header file. */
#define HAVE_STDLIB_H 1

/* Define to 1 if you have the `stime' function. */
/* #undef HAVE_STIME */

/* Define to 1 if you have the `strerror_r' function. */
/* #undef HAVE_STRERROR_R */

/* Define to 1 if you have the <strings.h> header file. */
#define HAVE_STRINGS_H 1

/* Define to 1 if you have the <string.h> header file. */
#define HAVE_STRING_H 1

/* Define to 1 if `d_off' is member of `struct dirent'. */
/* #undef HAVE_STRUCT_DIRENT_D_OFF */

/* Define to 1 if `d_reclen' is member of `struct dirent'. */
/* #undef HAVE_STRUCT_DIRENT_D_RECLEN */

/* Define to 1 if `d_type' is member of `struct dirent'. */
/* #undef HAVE_STRUCT_DIRENT_D_TYPE */

/* Have struct ip_mreq */
#define HAVE_STRUCT_IP_MREQ 1

/* Have struct ip_mreqn */
/* #undef HAVE_STRUCT_IP_MREQN */

/* Define to 1 if the system has the type `struct pollfd'. */
/* #undef HAVE_STRUCT_POLLFD */

/* Define to 1 if the system has the type `struct stat'. */
/* #undef HAVE_STRUCT_STAT */

/* Define to 1 if the system has the type `struct timeval'. */
/* #undef HAVE_STRUCT_TIMEVAL */

/* Define to 1 if the system has the type `struct timezone'. */
/* #undef HAVE_STRUCT_TIMEZONE */

/* Define to 1 if the system has the type `struct utimbuf'. */
/* #undef HAVE_STRUCT_UTIMBUF */

/* Define to 1 if the system has the type `suseconds_t'. */
/* #undef HAVE_SUSECONDS_T */

/* Define to 1 if you have the <syslog.h> header file. */
/* #undef HAVE_SYSLOG_H */

/* Define to 1 if you have the <sys/epoll.h> header file. */
/* #undef HAVE_SYS_EPOLL_H */

/* Define to 1 if you have the <sys/extattr.h> header file. */
/* #undef HAVE_SYS_EXTATTR_H */

/* Define to 1 if you have the <sys/filio.h> header file. */
/* #undef HAVE_SYS_FILIO_H */

/* Define to 1 if you have the <sys/ioctl.h> header file. */
/* #undef HAVE_SYS_IOCTL_H */

/* Define to 1 if you have the <sys/mkdev.h> header file. */
/* #undef HAVE_SYS_MKDEV_H */

/* Define to 1 if you have the <sys/mman.h> header file. */
/* #undef HAVE_SYS_MMAN_H */

/* Define to 1 if you have the <sys/param.h> header file. */
/* #undef HAVE_SYS_PARAM_H */

/* Define to 1 if you have the <sys/poll.h> header file. */
/* #undef HAVE_SYS_POLL_H */

/* Define to 1 if you have the <sys/sdt.h> header file. */
/* #undef HAVE_SYS_SDT_H */

/* Define to 1 if you have the <sys/sendfile.h> header file. */
/* #undef HAVE_SYS_SENDFILE_H */

/* Define to 1 if you have the <sys/sockio.h> header file. */
/* #undef HAVE_SYS_SOCKIO_H */

/* Define to 1 if you have the <sys/statvfs.h> header file. */
/* #undef HAVE_SYS_STATVFS_H */

/* Define to 1 if you have the <sys/stat.h> header file. */
#define HAVE_SYS_STAT_H 1

/* Define to 1 if you have the <sys/syscall.h> header file. */
/* #undef HAVE_SYS_SYSCALL_H */

/* Define to 1 if you have the <sys/time.h> header file. */
/* #undef HAVE_SYS_TIME_H */

/* Define to 1 if you have the <sys/types.h> header file. */
#define HAVE_SYS_TYPES_H 1

/* Define to 1 if you have the <sys/un.h> header file. */
/* #undef HAVE_SYS_UN_H */

/* Define to 1 if you have the <sys/utime.h> header file. */
#define HAVE_SYS_UTIME_H 1

/* Define to 1 if you have the <sys/vfstab.h> header file. */
/* #undef HAVE_SYS_VFSTAB_H */

/* Define to 1 if you have the <sys/wait.h> header file. */
/* #undef HAVE_SYS_WAIT_H */

/* Define to 1 if you have the <sys/xattr.h> header file. */
/* #undef HAVE_SYS_XATTR_H */

/* Define to 1 if you have the <termios.h> header file. */
/* #undef HAVE_TERMIOS_H */

/* Define to 1 if you have the <term.h> header file. */
/* #undef HAVE_TERM_H */

/* Have timezone variable */
/* #undef HAVE_TIMEZONE */

/* tld_model available */
/* #undef HAVE_TLS_MODEL_ATTR */

/* Have tm_gmtoff */
/* #undef HAVE_TM_GMTOFF */

/* Define to 1 if you have the `trunc' function. */
#define HAVE_TRUNC 1

/* Define to 1 if you have the `ttyname_r' function. */
/* #undef HAVE_TTYNAME_R */

/* Define to 1 if you have the <unistd.h> header file. */
/* #define HAVE_UNISTD_H 1 */

/* Define to 1 if you have the <utime.h> header file. */
/* #define HAVE_UTIME_H 1 */

/* Define to 1 if you have the <valgrind/memcheck.h> header file. */
/* #undef HAVE_VALGRIND_MEMCHECK_H */

/* Support for the visibility ("hidden") attribute */
/* #define HAVE_VISIBILITY_HIDDEN 1 */

/* Define to 1 if you have the `vsnprintf' function. */
/* #undef HAVE_VSNPRINTF */

/* Define to 1 if you have the <wchar.h> header file. */
#define HAVE_WCHAR_H 1

/* Define to 1 if you have IPv6 support. */
#define HAVE_STRUCT_SOCKADDR_IN6 1

/* Defined as strtok_s in eglib-config.hw */
#define HAVE_STRTOK_R 1

/* Have a working sigaltstack */
/* #undef HAVE_WORKING_SIGALTSTACK */

/* The GC needs write barriers. */
/* #undef HAVE_WRITE_BARRIERS */

/* Have system zlib */
/* #define HAVE_ZLIB 1 */

/* Architecture uses registers for Parameters */
/* #undef MONO_ARCH_REGPARMS */

/* Enable the allocation and indexing of arrays greater than Int32.MaxValue */
/* #undef MONO_BIG_ARRAYS */

/* Xen-specific behaviour */
/* #define MONO_XEN_OPT 1 */

/* Length of zero length arrays */
#define MONO_ZERO_LEN_ARRAY 1

/* Name of /dev/random */
#define NAME_DEV_RANDOM ""

/* Define if Unix sockets cannot be created in an anonymous namespace */
/* #undef NEED_LINK_UNLINK */

/* Name of package */
#define PACKAGE "mono"

/* Define to the address where bug reports for this package should be sent. */
#define PACKAGE_BUGREPORT "Hans_Boehm@hp.com"

/* Define to the full name of this package. */
#define PACKAGE_NAME "libgc-mono"

/* Define to the full name and version of this package. */
#define PACKAGE_STRING "libgc-mono 6.6"

/* Define to the one symbol short name of this package. */
#define PACKAGE_TARNAME "libgc-mono"

/* Define to the version of this package. */
#define PACKAGE_VERSION "6.6"

/* Platform is Win32 */
#define HOST_WIN32 1
#define TARGET_WIN32 1

#ifdef _WIN64
#define TARGET_AMD64 1
#else
#define TARGET_X86 1
#endif

/* pthread_t is a pointer */
/* #undef PTHREAD_POINTER_ID */

/* The size of `size_t', as computed by sizeof. */
/* #undef SIZEOF_SIZE_T */

/* The size of a `void *', as computed by sizeof. */
#ifdef _WIN64
#define SIZEOF_VOID_P 8
#else
#define SIZEOF_VOID_P 4
#endif

#define SIZEOF_REGISTER SIZEOF_VOID_P

/* Define to 1 if you have the ANSI C header files. */
#define STDC_HEADERS 1

/* Use included libgc */
/* #define USE_INCLUDED_LIBGC 1 */

#define DEFAULT_GC_NAME "Included Boehm (with typed GC)"

/* ... */
/* #undef USE_MACH_SEMA */

/* Use mono_mutex_t */
/* #undef USE_MONO_MUTEX */

/* Version number of package */
#define VERSION "#MONO_VERSION#"

/* Version of the corlib-runtime interface */
#define MONO_CORLIB_VERSION #MONO_CORLIB_VERSION#

#endif
