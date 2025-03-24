include(CheckFunctionExists)
include(CheckIncludeFiles)
include(CheckSymbolExists)

check_include_files("windows.h" HAVE_WINDOWS_H)
check_include_files("windows.h;bcrypt.h" HAVE_BCRYPT_H)
check_include_files("sys/auxv.h;asm/hwcap.h" HAVE_AUXV_HWCAP_H)
check_include_files("asm/hwprobe.h" HAVE_HWPROBE_H)

check_function_exists(sysctlbyname HAVE_SYSCTLBYNAME)
check_function_exists(fsync HAVE_FSYNC)

check_symbol_exists(arc4random_buf "stdlib.h" HAVE_ARC4RANDOM_BUF)
check_symbol_exists(O_CLOEXEC fcntl.h HAVE_O_CLOEXEC)
check_symbol_exists(clock_gettime_nsec_np time.h HAVE_CLOCK_GETTIME_NSEC_NP)

if(CMAKE_C_BYTE_ORDER STREQUAL "BIG_ENDIAN")
    set(BIGENDIAN 1)
endif()

configure_file(${CMAKE_CURRENT_LIST_DIR}/minipalconfig.h.in ${CMAKE_CURRENT_BINARY_DIR}/minipalconfig.h)
