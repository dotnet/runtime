# This is a custom file written for .NET Core's build system


include(CheckCSourceCompiles)
include(CheckIncludeFiles)

if(CLR_CMAKE_HOST_WIN32)
    # Our posix abstraction layer will provide these headers
    set(HAVE_ELF_H 1)
    set(HAVE_ENDIAN_H 1)

    # MSVC compiler is currently missing C11 stdalign.h header
    # Fake it until support is added
    check_include_files(stdalign.h HAVE_STDALIGN_H)
    if (NOT HAVE_STDALIGN_H)
        configure_file(include/win/fakestdalign.h.in ${CMAKE_CURRENT_BINARY_DIR}/include/stdalign.h COPYONLY)
    endif (NOT HAVE_STDALIGN_H)

    # MSVC compiler is currently missing C11 stdatomic.h header
    check_c_source_compiles("#include <stdatomic.h> void main() { _Atomic int a; }" HAVE_STDATOMIC_H)
    if (NOT HAVE_STDATOMIC_H)
        configure_file(include/win/fakestdatomic.h.in ${CMAKE_CURRENT_BINARY_DIR}/include/stdatomic.h COPYONLY)
    endif (NOT HAVE_STDATOMIC_H)

    # MSVC compiler is currently missing C11 _Thread_local
    check_c_source_compiles("void main() { _Thread_local int a; }"  HAVE_THREAD_LOCAL)
    if (NOT HAVE_THREAD_LOCAL)
        add_definitions(-D_Thread_local=)
    endif (NOT HAVE_THREAD_LOCAL)
else(CLR_CMAKE_HOST_WIN32)
    check_include_files(elf.h HAVE_ELF_H)
    check_include_files(sys/elf.h HAVE_SYS_ELF_H)

    check_include_files(endian.h HAVE_ENDIAN_H)
    check_include_files(sys/endian.h HAVE_SYS_ENDIAN_H)
endif(CLR_CMAKE_HOST_WIN32)

check_include_files(link.h HAVE_LINK_H)
check_include_files(sys/link.h HAVE_SYS_LINK_H)

check_include_files(atomic_ops.h HAVE_ATOMIC_OPS_H)

check_c_source_compiles("
int main(int argc, char **argv)
{
    __sync_bool_compare_and_swap((int *)0, 0, 1);
    __sync_fetch_and_add((int *)0, 1);

    return 0;
}" HAVE_SYNC_ATOMICS)


check_c_source_compiles("
int main(int argc, char **argv)
{
    __builtin_unreachable();

    return 0;
}" HAVE__BUILTIN_UNREACHABLE)

check_c_source_compiles("
#include <stdalign.h>

int main(void)
{
    alignas(128) char result = 0;

    return result;
}" HAVE_STDALIGN_ALIGNAS)

configure_file(${CMAKE_CURRENT_LIST_DIR}/config.h.in ${CMAKE_CURRENT_BINARY_DIR}/include/config.h)
add_definitions(-DHAVE_CONFIG_H=1)

configure_file(include/libunwind-common.h.in ${CMAKE_CURRENT_BINARY_DIR}/include/libunwind-common.h)
configure_file(include/libunwind.h.in ${CMAKE_CURRENT_BINARY_DIR}/include/libunwind.h)
configure_file(include/tdep/libunwind_i.h.in ${CMAKE_CURRENT_BINARY_DIR}/include/tdep/libunwind_i.h)
