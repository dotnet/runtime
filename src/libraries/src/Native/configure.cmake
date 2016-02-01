include(CheckCXXSourceCompiles)
include(CheckCXXSourceRuns)
include(CheckFunctionExists)
include(CheckIncludeFiles)
include(CheckPrototypeDefinition)
include(CheckStructHasMember)
include(CheckSymbolExists)
include(CheckTypeSize)

#CMake does not include /usr/local/include into the include search path
#thus add it manually. This is required on FreeBSD.
include_directories(SYSTEM /usr/local/include)

# in_pktinfo: Find whether this struct exists
check_include_files(
    linux/in.h
    HAVE_LINUX_IN_H)

if (HAVE_LINUX_IN_H)
    set (SOCKET_INCLUDES ${SOCKET_INCLUDES} linux/in.h)
else ()
    set (SOCKET_INCLUDES ${SOCKET_INCLUDES} netinet/in.h)
endif ()

set(CMAKE_EXTRA_INCLUDE_FILES ${SOCKET_INCLUDES})
check_type_size(
    "struct in_pktinfo"
    HAVE_IN_PKTINFO
    BUILTIN_TYPES_ONLY)
set(CMAKE_EXTRA_INCLUDE_FILES) # reset CMAKE_EXTRA_INCLUDE_FILES
# /in_pktinfo

check_include_files(
    alloca.h
    HAVE_ALLOCA_H)

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

check_function_exists(
    ioctl
    HAVE_IOCTL)

check_function_exists(
    sched_getaffinity
    HAVE_SCHED_GETAFFINITY)

check_function_exists(
    sched_setaffinity
    HAVE_SCHED_SETAFFINITY)

check_symbol_exists(
    TIOCGWINSZ
    "sys/ioctl.h"
    HAVE_TIOCGWINSZ)

check_function_exists(
    tcgetattr
    HAVE_TCGETATTR)

check_function_exists(
    tcsetattr
    HAVE_TCSETATTR)

check_symbol_exists(
    ECHO
    "termios.h"
    HAVE_ECHO)

check_symbol_exists(
    ICANON
    "termios.h"
    HAVE_ICANON)

check_symbol_exists(
    TCSANOW
    "termios.h"
    HAVE_TCSANOW)

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

check_struct_has_member(
    "struct in6_addr"
    __in6_u
    "netdb.h"
    HAVE_IN6_U)

check_cxx_source_compiles(
    "
    #include <string.h>
    int main() { char* c = strerror_r(0, 0, 0); }
    "
    HAVE_GNU_STRERROR_R)

check_struct_has_member(
    "struct fd_set"
    fds_bits
    "sys/select.h"
    HAVE_FDS_BITS)

check_struct_has_member(
    "struct fd_set"
    __fds_bits
    "sys/select.h"
    HAVE_PRIVATE_FDS_BITS)

check_function_exists(
    epoll_create1
    HAVE_EPOLL)

check_function_exists(
    kqueue
    HAVE_KQUEUE)

check_function_exists(
    gethostbyname_r
    HAVE_GETHOSTBYNAME_R)

check_function_exists(
    gethostbyaddr_r
    HAVE_GETHOSTBYADDR_R)

set(HAVE_SUPPORT_FOR_MULTIPLE_CONNECT_ATTEMPTS 0)
set(HAVE_SUPPORT_FOR_DUAL_MODE_IPV4_PACKET_INFO 0)
set(HAVE_THREAD_SAFE_GETHOSTBYNAME_AND_GETHOSTBYADDR 0)

if (CMAKE_SYSTEM_NAME STREQUAL Linux)
    set(CMAKE_REQUIRED_LIBRARIES rt)
    set(HAVE_SUPPORT_FOR_MULTIPLE_CONNECT_ATTEMPTS 1)
    set(HAVE_SUPPORT_FOR_DUAL_MODE_IPV4_PACKET_INFO 1)
elseif (CMAKE_SYSTEM_NAME STREQUAL Darwin)
    set(HAVE_THREAD_SAFE_GETHOSTBYNAME_AND_GETHOSTBYADDR 1)
endif ()

check_cxx_source_runs(
    "
    #include <stdlib.h>
    #include <time.h>
    #include <sys/time.h>
    int main()
    {
        int ret; 
        struct timespec ts;
        ret = clock_gettime(CLOCK_MONOTONIC, &ts);
        exit(ret);
    }
    " 
    HAVE_CLOCK_MONOTONIC)

check_function_exists(
    mach_absolute_time
    HAVE_MACH_ABSOLUTE_TIME)

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

check_prototype_definition(
    getpriority
    "int getpriority(int which, int who)"
    "0"
    "sys/resource.h"
    PRIORITY_REQUIRES_INT_WHO)

check_cxx_source_compiles(
    "
    #include <sys/types.h>
    #include <sys/socketvar.h>
    #include <netinet/ip.h>
    #include <netinet/tcp.h>
    #include <netinet/tcp_var.h>
    int main() { return 0; }
    "
    HAVE_TCP_VAR_H
)

check_cxx_source_compiles(
    "
    #include <netinet/tcp.h>
    int main() { int x = TCP_ESTABLISHED; return x; }
    "
    HAVE_TCP_H_TCPSTATE_ENUM
)

check_symbol_exists(
    TCPS_ESTABLISHED
    "netinet/tcp_fsm.h"
    HAVE_TCP_FSM_H
)

check_cxx_source_compiles(
    "
    #include <sys/types.h>
    #include <net/route.h>
    int main() { rt_msghdr* hdr; return 0; }
    "
    HAVE_RT_MSGHDR
)

check_include_files(
    linux/rtnetlink.h
    HAVE_LINUX_RTNETLINK_H)

# getdomainname on OSX takes an 'int' instead of a 'size_t'
# check if compiling with 'size_t' would cause a warning
set (PREVIOUS_CMAKE_REQUIRED_FLAGS ${CMAKE_REQUIRED_FLAGS})
set (CMAKE_REQUIRED_FLAGS "-Werror -Weverything")
check_cxx_source_compiles(
    "
    #include <unistd.h>
    int main() { size_t namelen = 20; char name[20]; getdomainname(name, namelen); }
    "
    HAVE_GETDOMAINNAME_SIZET
)
set (CMAKE_REQUIRED_FLAGS ${PREVIOUS_CMAKE_REQUIRED_FLAGS})

check_function_exists(
    inotify_init
    HAVE_INOTIFY_INIT)

check_function_exists(
    inotify_add_watch
    HAVE_INOTIFY_ADD_WATCH)

check_function_exists(
    inotify_rm_watch
    HAVE_INOTIFY_RM_WATCH)

set (HAVE_INOTIFY 0)
if (HAVE_INOTIFY_INIT AND HAVE_INOTIFY_ADD_WATCH AND HAVE_INOTIFY_RM_WATCH)
	set (HAVE_INOTIFY 1)
elseif (CMAKE_SYSTEM_NAME STREQUAL Linux)
	message(FATAL_ERROR "Cannot find inotify functions on a Linux platform.")
endif()

check_cxx_source_compiles(
    "
    #include <curl/multi.h>
    int main() { int i = CURLM_ADDED_ALREADY; }
    "
    HAVE_CURLM_ADDED_ALREADY)

check_cxx_source_compiles(
    "
    #include <curl/multi.h>
    int main() { int i = CURL_HTTP_VERSION_2_0; }
    "
    HAVE_CURL_HTTP_VERSION_2_0)

check_cxx_source_compiles(
    "
    #include <curl/multi.h>
    int main() { int i = CURLPIPE_MULTIPLEX; }
    "
    HAVE_CURLPIPE_MULTIPLEX)

check_symbol_exists(
    OPEN_MAX
    "sys/syslimits.h"
    HAVE_OPEN_MAX)

set (CMAKE_REQUIRED_LIBRARIES)

configure_file(
    ${CMAKE_CURRENT_SOURCE_DIR}/Common/pal_config.h.in
    ${CMAKE_CURRENT_BINARY_DIR}/Common/pal_config.h)
