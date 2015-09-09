include(CheckFunctionExists)
include(CheckStructHasMember)
include(CheckCXXSourceCompiles)
include(CheckCXXSourceRuns)

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

check_function_exists(
    posix_fadvise
    HAVE_POSIX_ADVISE)

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


if (CMAKE_SYSTEM_NAME STREQUAL Linux)
    set (CMAKE_REQUIRED_LIBRARIES rt)
endif ()

check_cxx_source_runs(
    "
    #include <sys/mman.h>
    #include <fcntl.h>
    #include <unistd.h>

    int main()
    {
        int fd = shm_open(\"/corefx_configure_shm_open\", O_CREAT | O_RDWR, 0777);
        if (fd == -1)
            return -1;

        shm_unlink(\"/corefx_configure_shm_open\");

        // NOTE: PROT_EXEC and MAP_PRIVATE don't work well with shm_open
        //       on at least the current version of Mac OS X

        if (mmap(nullptr, 1, PROT_EXEC, MAP_PRIVATE, fd, 0) == MAP_FAILED)
            return -1;

        return 0;
    }
    "
    HAVE_SHM_OPEN_THAT_WORKS_WELL_ENOUGH_WITH_MMAP)

set (CMAKE_REQUIRED_LIBRARIES)

configure_file(
    ${CMAKE_CURRENT_SOURCE_DIR}/Common/pal_config.h.in
    ${CMAKE_CURRENT_BINARY_DIR}/Common/pal_config.h)
