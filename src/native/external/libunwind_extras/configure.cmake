include(CheckCSourceCompiles)
include(CheckIncludeFiles)
include(CheckFunctionExists)

if(MSVC)
    # Our posix abstraction layer will provide these headers
    set(HAVE_ELF_H 1)
    set(HAVE_ENDIAN_H 1)

    # MSVC compiler is currently missing C11 stdalign.h header
    # Fake it until support is added. Place fakes under a msvc-shim
    # subdir that is added to the include path only for libunwind's own
    # compile (see libunwind_extras/CMakeLists.txt) and not for libunwind
    # consumers (e.g. CoreCLR PAL when cross-compiling for Android), which
    # would otherwise risk shadowing the real headers from the target
    # sysroot.
    check_include_files(stdalign.h HAVE_STDALIGN_H)
    if (NOT HAVE_STDALIGN_H)
        configure_file(${CLR_SRC_NATIVE_DIR}/external/libunwind/include/remote/win/fakestdalign.h.in ${CMAKE_CURRENT_BINARY_DIR}/include/msvc-shim/stdalign.h COPYONLY)
    endif (NOT HAVE_STDALIGN_H)

    # MSVC compiler is currently missing C11 stdatomic.h header
    check_c_source_compiles("#include <stdatomic.h> void main() { _Atomic int a; }" HAVE_STDATOMIC_H)
    if (NOT HAVE_STDATOMIC_H)
        configure_file(${CLR_SRC_NATIVE_DIR}/external/libunwind/include/remote/win/fakestdatomic.h.in ${CMAKE_CURRENT_BINARY_DIR}/include/msvc-shim/stdatomic.h COPYONLY)
    endif (NOT HAVE_STDATOMIC_H)

    # MSVC compiler is currently missing C11 _Thread_local
    check_c_source_compiles("void main() { _Thread_local int a; }"  HAVE_THREAD_LOCAL)
    if (NOT HAVE_THREAD_LOCAL)
        add_definitions(-D_Thread_local=)
    endif (NOT HAVE_THREAD_LOCAL)
else(MSVC)
    check_include_files(elf.h HAVE_ELF_H)
    check_include_files(sys/elf.h HAVE_SYS_ELF_H)

    check_include_files(endian.h HAVE_ENDIAN_H)
    check_include_files(sys/endian.h HAVE_SYS_ENDIAN_H)
endif(MSVC)

check_include_files(link.h HAVE_LINK_H)
check_include_files(sys/link.h HAVE_SYS_LINK_H)

check_function_exists(pipe2 HAVE_PIPE2)

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
