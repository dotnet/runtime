include(CheckFunctionExists)
include(CheckIncludeFiles)
include(CheckSymbolExists)

check_include_files("windows.h;bcrypt.h" HAVE_BCRYPT_H)
check_include_files("sys/auxv.h;asm/hwcap.h" HAVE_AUXV_HWCAP_H)
check_include_files("asm/hwprobe.h" HAVE_HWPROBE_H)
check_include_files(crt_externs.h HAVE_CRT_EXTERNS_H)

check_function_exists(sysctlbyname HAVE_SYSCTLBYNAME)
check_function_exists(fsync HAVE_FSYNC)

check_symbol_exists(arc4random_buf "stdlib.h" HAVE_ARC4RANDOM_BUF)
check_symbol_exists(O_CLOEXEC fcntl.h HAVE_O_CLOEXEC)
check_symbol_exists(CLOCK_MONOTONIC time.h HAVE_CLOCK_MONOTONIC)
check_symbol_exists(CLOCK_MONOTONIC_COARSE time.h HAVE_CLOCK_MONOTONIC_COARSE)
check_symbol_exists(clock_gettime_nsec_np time.h HAVE_CLOCK_GETTIME_NSEC_NP)

check_function_exists(sprintf_s HAVE_SPRINTF_S)
check_function_exists(strncasecmp HAVE_STRNCASECMP)
check_function_exists(strcpy_s HAVE_STRCPY_S)
check_function_exists(strncpy_s HAVE_STRNCPY_S)
check_function_exists(strcat_s HAVE_STRCAT_S)
check_function_exists(getenv_s HAVE_GETENV_S)

if (HAVE_CRT_EXTERNS_H)
    check_c_source_compiles(
    "
    #include <crt_externs.h>
    int main(void) { char** e = *(_NSGetEnviron()); return 0; }
    "
    HAVE__NSGETENVIRON)
endif()

if(CMAKE_C_BYTE_ORDER STREQUAL "BIG_ENDIAN")
    set(BIGENDIAN 1)
endif()

configure_file(${CMAKE_CURRENT_LIST_DIR}/minipalconfig.h.in ${CMAKE_CURRENT_BINARY_DIR}/minipalconfig.h)
