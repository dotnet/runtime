#
# Configure checks
#

include(CheckTypeSize)
include(CheckStructHasMember)
include(CheckSymbolExists)
include(CheckCCompilerFlag)

# Apple platforms like macOS/iOS allow targeting older operating system versions with a single SDK,
# the mere presence of a symbol in the SDK doesn't tell us whether the deployment target really supports it.
# The compiler raises a warning when using an unsupported API, turn that into an error so check_symbol_exists()
# can correctly identify whether the API is supported on the target.
check_c_compiler_flag("-Wunguarded-availability" "C_SUPPORTS_WUNGUARDED_AVAILABILITY")
if(C_SUPPORTS_WUNGUARDED_AVAILABILITY)
  set(CMAKE_REQUIRED_FLAGS "${CMAKE_REQUIRED_FLAGS} -Werror=unguarded-availability")
endif()

function(ac_check_headers)
  foreach(arg ${ARGN})
	check_include_file ("${arg}" FOUND_${arg})
	string(TOUPPER "${arg}" var1)
	string(REPLACE "/" "_" var2 ${var1})
	string(REPLACE "." "_" var3 ${var2})
	string(REPLACE "-" "_" var4 ${var3})
	if (FOUND_${arg})
	  set(HAVE_${var4} 1 PARENT_SCOPE)
	endif()
  endforeach(arg)
endfunction()

function(ac_check_funcs)
  foreach(arg ${ARGN})
	check_function_exists ("${arg}" FOUND_${arg})
	string(TOUPPER "${arg}" var1)
	string(REPLACE "/" "_" var2 ${var1})
	string(REPLACE "." "_" var3 ${var2})
	if (FOUND_${arg})
	  set(HAVE_${var3} 1 PARENT_SCOPE)
	endif()
  endforeach(arg)
endfunction()

function(ac_check_type type suffix includes)
  set(CMAKE_EXTRA_INCLUDE_FILES ${includes})
  check_type_size(${type} AC_CHECK_TYPE_${suffix})
  if (AC_CHECK_TYPE_${suffix})
	string(TOUPPER "${type}" var1)
	string(REPLACE "/" "_" var2 ${var1})
	string(REPLACE "." "_" var3 ${var2})
	string(REPLACE " " "_" var4 ${var3})
	set(HAVE_${var4} 1 PARENT_SCOPE)
  endif()
  set(CMAKE_EXTRA_INCLUDE_FILES)
endfunction()

ac_check_headers (
  sys/types.h sys/stat.h sys/filio.h sys/sockio.h sys/utime.h sys/un.h sys/syscall.h sys/uio.h sys/param.h sys/sysctl.h
  sys/prctl.h sys/socket.h sys/utsname.h sys/select.h sys/user.h sys/poll.h sys/wait.h sts/auxv.h sys/resource.h
  sys/ioctl.h sys/errno.h sys/sendfile.h sys/statvfs.h sys/statfs.h sys/mman.h sys/mount.h sys/time.h sys/random.h
  strings.h stdint.h unistd.h netdb.h utime.h semaphore.h libproc.h alloca.h ucontext.h pwd.h
  gnu/lib-names.h netinet/tcp.h netinet/in.h link.h arpa/inet.h unwind.h poll.h wchar.h linux/magic.h
  android/legacy_signal_inlines.h android/ndk-version.h execinfo.h pthread.h pthread_np.h net/if.h dirent.h
  CommonCrypto/CommonDigest.h dlfcn.h getopt.h pwd.h iconv.h alloca.h
  /usr/include/malloc.h)

ac_check_funcs (
  sigaction kill clock_nanosleep kqueue backtrace_symbols mkstemp mmap
  madvise getrusage dladdr sysconf getrlimit prctl nl_langinfo
  sched_getaffinity sched_setaffinity getpwuid_r readlink chmod lstat getdtablesize ftruncate msync
  getpeername utime utimes openlog closelog atexit popen strerror_r inet_pton inet_aton
  shm_open poll getfsstat mremap posix_fadvise vsnprintf sendfile statfs statvfs setpgid system
  fork execv execve waitpid localtime_r mkdtemp getrandom execvp strlcpy stpcpy strtok_r rewinddir
  vasprintf strndup getpwuid_r getprotobyname getprotobyname_r getaddrinfo mach_absolute_time
  gethrtime read_real_time gethostbyname gethostbyname2 getnameinfo getifaddrs
  access inet_ntop Qp2getifaddrs)

if(NOT HOST_DARWIN)
  # getentropy was introduced in macOS 10.12 / iOS 10.0
  ac_check_funcs (getentropy)
endif()

find_package(Threads)
# Needed to find pthread_ symbols
set(CMAKE_REQUIRED_LIBRARIES "${CMAKE_REQUIRED_LIBRARIES} ${CMAKE_THREAD_LIBS_INIT}")

ac_check_funcs(
  pthread_getname_np pthread_setname_np pthread_cond_timedwait_relative_np pthread_kill
  pthread_attr_setstacksize pthread_get_stackaddr_np
)

check_symbol_exists(pthread_mutexattr_setprotocol "pthread.h" HAVE_DECL_PTHREAD_MUTEXATTR_SETPROTOCOL)
check_symbol_exists(CLOCK_MONOTONIC "time.h" HAVE_CLOCK_MONOTONIC)
check_symbol_exists(CLOCK_MONOTONIC_COARSE "time.h" HAVE_CLOCK_MONOTONIC_COARSE)
check_symbol_exists(sys_signame "signal.h" HAVE_SYSSIGNAME)
check_symbol_exists(pthread_jit_write_protect_np "pthread.h" HAVE_PTHREAD_JIT_WRITE_PROTECT_NP)

ac_check_type("struct sockaddr_in6" sockaddr_in6 "netinet/in.h")
ac_check_type("struct timeval" timeval "sys/time.h;sys/types.h;utime.h")
ac_check_type("socklen_t" socklen_t "sys/types.h;sys/socket.h")
ac_check_type("struct ip_mreqn" ip_mreqn "netinet/in.h")
ac_check_type("struct ip_mreq" ip_mreq "netinet/in.h")
ac_check_type("clockid_t" clockid_t "sys/types.h")

check_struct_has_member("struct kinfo_proc" kp_proc "sys/types.h;sys/param.h;sys/sysctl.h;sys/proc.h" HAVE_STRUCT_KINFO_PROC_KP_PROC)
check_struct_has_member("struct sockaddr_in" sin_len "netinet/in.h" HAVE_SOCKADDR_IN_SIN_LEN)
check_struct_has_member("struct sockaddr_in6" sin6_len "netinet/in.h" HAVE_SOCKADDR_IN6_SIN_LEN)
check_struct_has_member("struct stat" st_atim "sys/types.h;sys/stat.h;unistd.h" HAVE_STRUCT_STAT_ST_ATIM)
check_struct_has_member("struct stat" st_atimespec "sys/types.h;sys/stat.h;unistd.h" HAVE_STRUCT_STAT_ST_ATIMESPEC)

check_type_size("int" SIZEOF_INT)
check_type_size("void*" SIZEOF_VOID_P)
check_type_size("long" SIZEOF_LONG)
check_type_size("long long" SIZEOF_LONG_LONG)
check_type_size("size_t" SIZEOF_SIZE_T)

# ICONV
set(ICONV_LIB)
find_library(LIBICONV_FOUND iconv)
if(NOT LIBICONV_FOUND STREQUAL "LIBICONV_FOUND-NOTFOUND")
  set(ICONV_LIB "iconv")
endif()

file(WRITE ${CMAKE_BINARY_DIR}${CMAKE_FILES_DIRECTORY}/CMakeTmp/test.c
  "#include <sched.h>\n"
  "void main () { CPU_COUNT((void *) 0); }\n"
)
try_compile(GLIBC_HAS_CPU_COUNT ${CMAKE_BINARY_DIR}/CMakeTmp SOURCES "${CMAKE_BINARY_DIR}${CMAKE_FILES_DIRECTORY}/CMakeTmp/test.c"
    COMPILE_DEFINITIONS "-D_GNU_SOURCE")


if(HOST_WIN32)
  # checking for this doesn't work for some reason, hardcode result
  set(HAVE_WINTERNL_H 1)
  set(HAVE_CRYPT_RNG 1)
  set(HAVE_GETADDRINFO 1)
  set(HAVE_GETNAMEINFO 1)
  set(HAVE_GETPROTOBYNAME 1)
  set(HAVE_INET_NTOP 1)
  set(HAVE_INET_PTON 1)
  set(HAVE_STRUCT_SOCKADDR_IN6 1)
  set(HAVE_STRTOK_R 1)
  set(HAVE_EXECVP 0)
endif()

if(HOST_IOS)
  set(HAVE_SYSTEM 0)
  set(HAVE_GETPWUID_R 0)
  set(HAVE_SYS_USER_H 0)
  set(HAVE_GETENTROPY 0)
  if(HOST_TVOS)
    set(HAVE_PTHREAD_KILL 0)
    set(HAVE_KILL 0)
    set(HAVE_SIGACTION 0)
    set(HAVE_FORK 0)
    set(HAVE_EXECV 0)
    set(HAVE_EXECVE 0)
    set(HAVE_EXECVP 0)
  endif()
elseif(HOST_MACCAT)
  set(HAVE_SYSTEM 0)
endif()

if(HOST_BROWSER)
  set(HAVE_FORK 0)
endif()
