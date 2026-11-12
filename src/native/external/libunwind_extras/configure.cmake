include(CheckCSourceCompiles)
include(CheckIncludeFiles)
include(CheckSymbolExists)
include(CMakePushCheckState)

check_include_files(elf.h HAVE_ELF_H)
check_include_files(sys/elf.h HAVE_SYS_ELF_H)

check_include_files(endian.h HAVE_ENDIAN_H)
check_include_files(sys/endian.h HAVE_SYS_ENDIAN_H)

check_include_files(link.h HAVE_LINK_H)
check_include_files(sys/link.h HAVE_SYS_LINK_H)

cmake_push_check_state()
list(APPEND CMAKE_REQUIRED_DEFINITIONS -D_GNU_SOURCE)
check_symbol_exists(pipe2 unistd.h HAVE_PIPE2)
cmake_pop_check_state()

check_c_source_compiles("
int main(int argc, char **argv)
{
    __builtin_unreachable();

    return 0;
}" HAVE__BUILTIN_UNREACHABLE)

configure_file(${CMAKE_CURRENT_LIST_DIR}/config.h.in ${CMAKE_CURRENT_BINARY_DIR}/include/config.h)
add_definitions(-DHAVE_CONFIG_H=1)

configure_file(${CLR_SRC_NATIVE_DIR}/external/libunwind/include/libunwind-common.h.in ${CMAKE_CURRENT_BINARY_DIR}/include/libunwind-common.h)
configure_file(${CLR_SRC_NATIVE_DIR}/external/libunwind/include/libunwind.h.in ${CMAKE_CURRENT_BINARY_DIR}/include/libunwind.h)
configure_file(${CLR_SRC_NATIVE_DIR}/external/libunwind/include/tdep/libunwind_i.h.in ${CMAKE_CURRENT_BINARY_DIR}/include/tdep/libunwind_i.h)
