include(CheckFunctionExists)
include(CheckStructHasMember)
include(CheckCXXSourceCompiles)

#CMake does not include /usr/local/include into the include search path
#thus add it manually. This is required on FreeBSD.
include_directories(/usr/local/include)

check_function_exists(
    stat64
    HAVE_STAT64)

check_function_exists(
    pipe2
    HAVE_PIPE2)

check_function_exists(
    getmntinfo
    HAVE_MNTINFO)

check_function_exists(
    strcpy_s
    HAVE_STRCPY_S)

check_function_exists(
    strlcpy
    HAVE_STRLCPY)

check_struct_has_member(
    "struct stat"
    st_birthtime
    "sys/types.h;sys/stat.h"
    HAVE_STAT_BIRTHTIME)

check_struct_has_member(
    "struct dirent"
    d_namlen
    "dirent.h"
    HAVE_DIRENT_NAME_LEN)

check_struct_has_member(
    "struct statfs"
    f_fstypename
    "sys/mount.h"
    HAVE_STATFS_FSTYPENAME)

check_cxx_source_compiles(
    "
    #include <string.h>
    int main() { char* c = strerror_r(0, 0, 0); }
    "
    HAVE_GNU_STRERROR_R)

configure_file(
    ${CMAKE_CURRENT_SOURCE_DIR}/config.h.in
    ${CMAKE_CURRENT_BINARY_DIR}/config.h)
