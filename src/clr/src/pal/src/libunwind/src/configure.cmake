include(CheckCXXSourceCompiles)
include(CheckIncludeFiles)

check_include_files(elf.h HAVE_ELF_H)
check_include_files(sys/elf.h HAVE_SYS_ELF_H)

check_include_files(endian.h HAVE_ENDIAN_H)
check_include_files(sys/endian.h HAVE_SYS_ENDIAN_H)

check_include_files(link.h HAVE_LINK_H)
check_include_files(sys/link.h HAVE_SYS_LINK_H)

check_include_files(atomic_ops.h HAVE_ATOMIC_OPS_H)

check_cxx_source_compiles("
int main(int argc, char **argv)
{
    __sync_bool_compare_and_swap((int *)0, 0, 1);
    __sync_fetch_and_add((int *)0, 1);

    return 0;
}" HAVE_SYNC_ATOMICS)


check_cxx_source_compiles("
int main(int argc, char **argv)
{
    __builtin_unreachable();

    return 0;
}" HAVE__BUILTIN_UNREACHABLE)

configure_file(${CMAKE_CURRENT_SOURCE_DIR}/config.h.in ${CMAKE_CURRENT_BINARY_DIR}/config.h)
