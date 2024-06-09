include(CheckFunctionExists)
include(CheckIncludeFiles)
include(CheckSymbolExists)

check_include_files("sys/auxv.h;asm/hwcap.h" HAVE_AUXV_HWCAP_H)
check_function_exists(sysctlbyname HAVE_SYSCTLBYNAME)

check_symbol_exists(arc4random_buf "stdlib.h" HAVE_ARC4RANDOM_BUF)
check_symbol_exists(O_CLOEXEC fcntl.h HAVE_O_CLOEXEC)

check_symbol_exists(
    clock_gettime_nsec_np
    time.h
    HAVE_CLOCK_GETTIME_NSEC_NP)

configure_file(${CMAKE_CURRENT_LIST_DIR}/minipalconfig.h.in ${CMAKE_CURRENT_BINARY_DIR}/minipalconfig.h)
