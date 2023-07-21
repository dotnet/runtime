include(CheckFunctionExists)
include(CheckIncludeFiles)

check_include_files("sys/auxv.h;asm/hwcap.h" HAVE_AUXV_HWCAP_H)
check_function_exists(sysctlbyname HAVE_SYSCTLBYNAME)

configure_file(${CMAKE_CURRENT_LIST_DIR}/minipalconfig.h.in ${CMAKE_CURRENT_BINARY_DIR}/minipalconfig.h)
