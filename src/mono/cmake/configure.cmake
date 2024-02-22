#
# Configure checks
#

include(CheckCCompilerFlag)
include(CheckCSourceCompiles)
include(CheckIncludeFiles)
include(CheckStructHasMember)
include(CheckSymbolExists)
include(CheckTypeSize)

# Apple platforms like macOS/iOS allow targeting older operating system versions with a single SDK,
# the mere presence of a symbol in the SDK doesn't tell us whether the deployment target really supports it.
# The compiler raises a warning when using an unsupported API, turn that into an error so check_symbol_exists()
# can correctly identify whether the API is supported on the target.
check_c_compiler_flag("-Wunguarded-availability" "C_SUPPORTS_WUNGUARDED_AVAILABILITY")
if(C_SUPPORTS_WUNGUARDED_AVAILABILITY)
  set(CMAKE_REQUIRED_FLAGS "${CMAKE_REQUIRED_FLAGS} -Werror=unguarded-availability")
endif()

if(HOST_SOLARIS)
  set(CMAKE_REQUIRED_DEFINITIONS "${CMAKE_REQUIRED_DEFINITIONS} -DGC_SOLARIS_THREADS -DGC_SOLARIS_PTHREADS -D_REENTRANT -D_POSIX_PTHREAD_SEMANTICS -DUSE_MMAP -DUSE_MUNMAP -DHOST_SOLARIS -D__EXTENSIONS__ -D_XPG4_2")
endif()

if(HOST_HAIKU)
  set(CMAKE_REQUIRED_DEFINITIONS "${CMAKE_REQUIRED_DEFINITIONS} -D_REENTRANT -D_GNU_SOURCE -D_BSD_SOURCE -D_POSIX_PTHREAD_SEMANTICS")
endif()

if(HOST_WASI)
  set(CMAKE_REQUIRED_DEFINITIONS "${CMAKE_REQUIRED_DEFINITIONS} -D_WASI_EMULATED_PROCESS_CLOCKS -D_WASI_EMULATED_SIGNAL -D_WASI_EMULATED_MMAN")
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

if (NOT HOST_WIN32)
ac_check_headers (
  sys/types.h sys/stat.h sys/sockio.h sys/un.h sys/syscall.h sys/uio.h sys/param.h
  sys/prctl.h sys/socket.h sys/utsname.h sys/select.h sys/poll.h sys/wait.h sys/resource.h
  sys/ioctl.h sys/errno.h sys/mman.h sys/mount.h sys/time.h sys/random.h
  strings.h stdint.h unistd.h signal.h setjmp.h syslog.h netdb.h semaphore.h alloca.h ucontext.h pwd.h elf.h
  gnu/lib-names.h netinet/tcp.h netinet/in.h link.h arpa/inet.h unwind.h poll.h wchar.h
  android/legacy_signal_inlines.h execinfo.h pthread.h pthread_np.h net/if.h dirent.h
  CommonCrypto/CommonDigest.h dlfcn.h getopt.h pwd.h alloca.h
  /usr/include/malloc.h)

ac_check_funcs (
  sigaction clock_nanosleep backtrace_symbols mkstemp mmap
  dladdr sysconf getrlimit prctl
  sched_getaffinity sched_setaffinity lstat ftruncate
  openlog closelog atexit popen strerror_r
  poll mremap vsnprintf setpgid system
  fork localtime_r mkdtemp getrandom getentropy execvp strlcpy strtok_r
  vasprintf strndup getaddrinfo mach_absolute_time
  gethrtime read_real_time gethostbyname gethostbyname2
  getpid mktemp)

if (HOST_LINUX OR HOST_BROWSER OR HOST_WASI)
  # sysctl is deprecated on Linux and doesn't work on Browser
  set(HAVE_SYS_SYSCTL_H 0)
else ()
  check_include_files("sys/types.h;sys/sysctl.h" HAVE_SYS_SYSCTL_H)
endif()

if(NOT DISABLE_THREADS)
  find_package(Threads)
endif()
# Needed to find pthread_ symbols
set(CMAKE_REQUIRED_LIBRARIES "${CMAKE_REQUIRED_LIBRARIES} ${CMAKE_THREAD_LIBS_INIT}")

ac_check_funcs(
  pthread_getname_np pthread_setname_np pthread_cond_timedwait_relative_np pthread_kill
  pthread_attr_setstacksize pthread_get_stackaddr_np
)

check_function_exists(clock_gettime HAVE_CLOCK_GETTIME)

check_symbol_exists(madvise "sys/mman.h" HAVE_MADVISE)
check_symbol_exists(pthread_mutexattr_setprotocol "pthread.h" HAVE_DECL_PTHREAD_MUTEXATTR_SETPROTOCOL)
check_symbol_exists(CLOCK_MONOTONIC "time.h" HAVE_CLOCK_MONOTONIC)
check_symbol_exists(CLOCK_MONOTONIC_COARSE "time.h" HAVE_CLOCK_MONOTONIC_COARSE)

check_symbol_exists(sys_signame "signal.h" HAVE_SYSSIGNAME)
check_symbol_exists(pthread_jit_write_protect_np "pthread.h" HAVE_PTHREAD_JIT_WRITE_PROTECT_NP)
check_symbol_exists(getauxval sys/auxv.h HAVE_GETAUXVAL)

ac_check_type("struct sockaddr_in6" sockaddr_in6 "netinet/in.h")
ac_check_type("socklen_t" socklen_t "sys/types.h;sys/socket.h")
ac_check_type("struct ip_mreqn" ip_mreqn "netinet/in.h")
ac_check_type("struct ip_mreq" ip_mreq "netinet/in.h")
ac_check_type("clockid_t" clockid_t "sys/types.h")

check_struct_has_member("struct sockaddr_in" sin_len "netinet/in.h" HAVE_SOCKADDR_IN_SIN_LEN)
check_struct_has_member("struct sockaddr_in6" sin6_len "netinet/in.h" HAVE_SOCKADDR_IN6_SIN_LEN)

if (HOST_DARWIN)
  check_struct_has_member("struct objc_super" super_class "objc/runtime.h;objc/message.h" HAVE_OBJC_SUPER_SUPER_CLASS)
endif()

if (HOST_LINUX)
  set(CMAKE_REQUIRED_DEFINITIONS -D_GNU_SOURCE)
endif()

check_symbol_exists(CPU_COUNT "sched.h" HAVE_GNU_CPU_COUNT)

check_c_source_compiles(
  "
  #include <string.h>
  int main(void)
  {
    char buffer[1];
    char c = *strerror_r(0, buffer, 0);
    return 0;
  }
  "
  HAVE_GNU_STRERROR_R)

if (HOST_LINUX OR HOST_ANDROID)
  set(CMAKE_REQUIRED_DEFINITIONS)
endif()

check_c_source_compiles(
  "
  int main(void)
  {
    static __thread int foo __attribute__((tls_model(\"initial-exec\")));
    return 0;
  }
  "
  HAVE_TLS_MODEL_ATTR)

if (TARGET_RISCV32 OR TARGET_RISCV64)
  check_c_source_compiles(
    "
    int main(void)
    {
      #ifdef __riscv_float_abi_double
      #error \"double\"
      #endif
      return 0;
    }
    "
    riscv_fpabi_result)

    # check if the compile succeeded (-> not double)
    if(riscv_fpabi_result EQUAL 0)
      check_c_source_compiles(
        "
        int main(void)
        {
          #ifdef __riscv_float_abi_single
          #error \"single\"
          #endif
          return 0;
        }
        "
        riscv_fpabi_result)

        # check if the compile succeeded (-> not single)
        if(riscv_fpabi_result EQUAL 0)
          check_c_source_compiles(
            "
            int main(void)
            {
              #ifdef __riscv_float_abi_soft
              #error \"soft\"
              #endif
              return 0;
            }
            "
            riscv_fpabi_result)

            # check if the compile succeeded (-> not soft)
            if(riscv_fpabi_result EQUAL 0)
              message(FATAL_ERROR "Unable to detect RISC-V floating point abi.")
            else()
              set(RISCV_FPABI_SOFT 1)
            endif()
        else()
            set(RISCV_FPABI_SINGLE 1)
        endif()
    else()
        set(RISCV_FPABI_DOUBLE 1)
    endif()
endif()
endif()

check_type_size("int" SIZEOF_INT)
check_type_size("void*" SIZEOF_VOID_P)
check_type_size("long" SIZEOF_LONG)
check_type_size("long long" SIZEOF_LONG_LONG)
check_type_size("size_t" SIZEOF_SIZE_T)

#
# Override checks
#

if(HOST_WIN32)
  # Dynamic lookup using ac_check_headers/ac_check_functions is extremly slow on Windows, espacially on msbuild.
  # Since majority of the checks above will fail on Windows host, we can just directly define the available static
  # API surface.
  set(HAVE_ATEXIT 1)
  set(HAVE_GETADDRINFO 1)
  set(HAVE_GETPEERNAME 1)
  set(HAVE_GETHOSTBYNAME 1)
  set(HAVE_SETJMP_H 1)
  set(HAVE_SIGNAL_H 1)
  set(HAVE_STDINT_H 1)
  set(HAVE_STRTOK_R 1)
  set(HAVE_STRUCT_SOCKADDR_IN6 1)
  set(HAVE_SYS_STAT_H 1)
  set(HAVE_SYS_TYPES_H 1)
  set(HAVE_SYS_UTIME_H 1)
  set(HAVE_SYSTEM 1)
  set(HAVE_WCHAR_H 1)
  set(HAVE_WINTERNL_H 1)
elseif(HOST_IOS OR HOST_TVOS OR HOST_MACCAT)
  set(HAVE_SYSTEM 0)
  # getentropy isn't allowed in the AppStore: https://github.com/rust-lang/rust/issues/102643
  set(HAVE_GETENTROPY 0)
  if(HOST_TVOS)
    set(HAVE_PTHREAD_KILL 0)
    set(HAVE_SIGACTION 0)
    set(HAVE_FORK 0)
    set(HAVE_EXECVP 0)
  endif()
elseif(HOST_BROWSER)
  set(HAVE_FORK 0)
  # wasm does have strtok_r even though cmake fails to find it
  set(HAVE_STRTOK_R 1)
elseif(HOST_SOLARIS)
  set(HAVE_NETINET_TCP_H 1)
  set(HAVE_GETADDRINFO 1)
elseif(HOST_WASI)
  # Redirected to errno.h
  set(SYS_ERRNO_H 0)
  # Some headers exist, but don't compile (wasi sdk 12.0)
  set(HAVE_SYS_SOCKET_H 0)
  set(HAVE_SYS_UN_H 0)
  set(HAVE_NETINET_TCP_H 0)
  set(HAVE_ARPA_INET_H 0)
  set(HAVE_MKDTEMP 0)
  set(HAVE_FORK 0)
  # wasm does have strtok_r even though cmake fails to find it
  set(HAVE_STRTOK_R 1)
  set(HAVE_GETRLIMIT 0)
  set(HAVE_MKSTEMP 0)
  set(HAVE_BACKTRACE_SYMBOLS 0)
  set(HAVE_GETPID 0)
  set(HAVE_MACH_ABSOLUTE_TIME 0)
  set(HAVE_GETHRTIME 0)
  set(HAVE_READ_REAL_TIME 0)
  set(HAVE_SCHED_GETAFFINITY 0)
  set(HAVE_SCHED_SETAFFINITY 0)
  set(HAVE_GETADDRINFO 0)
  set(HAVE_GETHOSTBYNAME 0)
  set(HAVE_GETHOSTBYNAME2 0)
  set(HAVE_EXECVP 0)
  set(HAVE_MMAP 1)
endif()
