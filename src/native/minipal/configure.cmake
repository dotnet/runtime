include(CheckFunctionExists)
include(CheckIncludeFiles)
include(CheckSymbolExists)

check_include_files("windows.h;bcrypt.h" HAVE_BCRYPT_H)
check_include_files("sys/auxv.h;asm/hwcap.h" HAVE_AUXV_HWCAP_H)
check_include_files("asm/hwprobe.h" HAVE_HWPROBE_H)
check_include_files("crt_externs.h" HAVE_CRT_EXTERNS_H)

check_symbol_exists(sysctlbyname "sys/sysctl.h" HAVE_SYSCTLBYNAME)
check_symbol_exists(fsync "unistd.h" HAVE_FSYNC)

check_symbol_exists(arc4random_buf "stdlib.h" HAVE_ARC4RANDOM_BUF)
check_symbol_exists(O_CLOEXEC "fcntl.h" HAVE_O_CLOEXEC)
check_symbol_exists(CLOCK_MONOTONIC "time.h" HAVE_CLOCK_MONOTONIC)
check_symbol_exists(CLOCK_MONOTONIC_COARSE "time.h" HAVE_CLOCK_MONOTONIC_COARSE)
check_symbol_exists(clock_gettime_nsec_np "time.h" HAVE_CLOCK_GETTIME_NSEC_NP)

check_symbol_exists(getenv "stdlib.h" HAVE_GETENV)
check_symbol_exists(strcpy_s "string.h" HAVE_STRCPY_S)
check_symbol_exists(strncpy_s "string.h" HAVE_STRNCPY_S)
check_symbol_exists(strcat_s "string.h" HAVE_STRCAT_S)

if (HAVE_CRT_EXTERNS_H)
    check_c_source_compiles(
    "
    #include <crt_externs.h>
    int main(void) { char** e = *(_NSGetEnviron()); return 0; }
    "
    HAVE__NSGETENVIRON)
endif()

check_c_source_compiles(
"
#include <stdlib.h>
int main(void) { char** e = _environ; return 0; }
"
HAVE__ENVIRON)

check_c_source_compiles(
"
int main(void) { extern char **environ; char** e = environ; return 0; }
"
HAVE_ENVIRON)

if(CMAKE_C_BYTE_ORDER STREQUAL "BIG_ENDIAN")
    set(BIGENDIAN 1)
endif()

configure_file(${CMAKE_CURRENT_LIST_DIR}/minipalconfig.h.in ${CMAKE_CURRENT_BINARY_DIR}/minipalconfig.h)
