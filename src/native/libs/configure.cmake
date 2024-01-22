include(CheckCCompilerFlag)
include(CheckCSourceCompiles)
include(CheckCSourceRuns)
include(CheckIncludeFiles)
include(CheckPrototypeDefinition)
include(CheckStructHasMember)
include(CheckSymbolExists)
include(CheckTypeSize)
include(CheckLibraryExists)
include(CheckFunctionExists)

if (CLR_CMAKE_TARGET_OSX)
    # Xcode's clang does not include /usr/local/include by default, but brew's does.
    # This ensures an even playing field.
    include_directories(SYSTEM /usr/local/include)
elseif (CLR_CMAKE_TARGET_FREEBSD)
    include_directories(SYSTEM ${CROSS_ROOTFS}/usr/local/include)
    set(CMAKE_REQUIRED_INCLUDES ${CROSS_ROOTFS}/usr/local/include)
elseif (CLR_CMAKE_TARGET_SUNOS)
    # requires /opt/tools when building in Global Zone (GZ)
    include_directories(SYSTEM /opt/local/include /opt/tools/include)
    set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -fstack-protector")
endif ()

if(CLR_CMAKE_USE_SYSTEM_LIBUNWIND)
    # This variable can be set and used by the coreclr and installer builds.
    # Libraries doesn't need it, but not using it makes the build fail.  So
    # just check and ignore the variable.
endif()

# We compile with -Werror, so we need to make sure these code fragments compile without warnings.
# Older CMake versions (3.8) do not assign the result of their tests, causing unused-value errors
# which are not distinguished from the test failing. So no error for that one.
# For clang-5.0 avoid errors like "unused variable 'err' [-Werror,-Wunused-variable]".
set(CMAKE_REQUIRED_FLAGS "${CMAKE_REQUIRED_FLAGS} -Werror -Wno-error=unused-value -Wno-error=unused-variable")
if (CMAKE_C_COMPILER_ID STREQUAL "Clang")
    set(CMAKE_REQUIRED_FLAGS "${CMAKE_REQUIRED_FLAGS} -Wno-error=builtin-requires-header")
endif()

# Apple platforms like macOS/iOS allow targeting older operating system versions with a single SDK,
# the mere presence of a symbol in the SDK doesn't tell us whether the deployment target really supports it.
# The compiler raises a warning when using an unsupported API, turn that into an error so check_symbol_exists()
# can correctly identify whether the API is supported on the target.
check_c_compiler_flag("-Wunguarded-availability" "C_SUPPORTS_WUNGUARDED_AVAILABILITY")
if(C_SUPPORTS_WUNGUARDED_AVAILABILITY)
  set(CMAKE_REQUIRED_FLAGS "${CMAKE_REQUIRED_FLAGS} -Wunguarded-availability")
endif()

# in_pktinfo: Find whether this struct exists
check_include_files(
    "sys/socket.h;linux/in.h"
    HAVE_LINUX_IN_H)

if (HAVE_LINUX_IN_H)
    set (SOCKET_INCLUDES linux/in.h)
else ()
    set (SOCKET_INCLUDES netinet/in.h)
endif ()

check_c_source_compiles(
    "
    #include <sys/socket.h>
    #include <${SOCKET_INCLUDES}>
    int main(void)
    {
        struct in_pktinfo pktinfo;
        return 0;
    }
    "
    HAVE_IN_PKTINFO)

check_c_source_compiles(
    "
    #include <sys/socket.h>
    #include <${SOCKET_INCLUDES}>
    int main(void)
    {
        struct ip_mreqn mreqn;
        return 0;
    }
    "
    HAVE_IP_MREQN)

# /in_pktinfo

check_c_source_compiles(
    "
    #include <sys/vfs.h>
    int main(void)
    {
        struct statfs s;
        return 0;
    }
    "
    HAVE_STATFS_VFS)

check_c_source_compiles(
    "
    #include <sys/mount.h>
    int main(void)
    {
        struct statfs s;
        return 0;
    }
    "
    HAVE_STATFS_MOUNT)

check_c_source_compiles(
    "
    #include <fcntl.h>
    int main(void)
    {
        struct flock64 l;
        return 0;
    }
    "
    HAVE_FLOCK64)

check_symbol_exists(
    O_CLOEXEC
    fcntl.h
    HAVE_O_CLOEXEC)

check_symbol_exists(
    F_DUPFD_CLOEXEC
    fcntl.h
    HAVE_F_DUPFD_CLOEXEC)

check_symbol_exists(
    F_DUPFD
    fcntl.h
    HAVE_F_DUPFD)

check_symbol_exists(
    F_FULLFSYNC
    fcntl.h
    HAVE_F_FULLFSYNC)

check_function_exists(
    getifaddrs
    HAVE_GETIFADDRS)

check_symbol_exists(
    fork
    unistd.h
    HAVE_FORK)

check_symbol_exists(
    lseek64
    unistd.h
    HAVE_LSEEK64)

check_symbol_exists(
    mmap64
    sys/mman.h
    HAVE_MMAP64)

check_symbol_exists(
    ftruncate64
    unistd.h
    HAVE_FTRUNCATE64)

check_symbol_exists(
    posix_fadvise64
    fcntl.h
    HAVE_POSIX_FADVISE64)

check_symbol_exists(
    stat64
    sys/stat.h
    HAVE_STAT64)

check_symbol_exists(
    vfork
    unistd.h
    HAVE_VFORK)

check_symbol_exists(
    pipe
    unistd.h
    HAVE_PIPE)

check_symbol_exists(
    pipe2
    unistd.h
    HAVE_PIPE2)

check_symbol_exists(
    getmntinfo
    sys/mount.h
    HAVE_MNTINFO)

check_symbol_exists(
    strcpy_s
    string.h
    HAVE_STRCPY_S)

check_symbol_exists(
    strlcat
    string.h
    HAVE_STRLCAT)

check_symbol_exists(
    posix_fadvise
    fcntl.h
    HAVE_POSIX_ADVISE)

check_symbol_exists(
    fallocate
    fcntl.h
    HAVE_FALLOCATE)

check_symbol_exists(
    preadv
    sys/uio.h
    HAVE_PREADV)

check_symbol_exists(
    pwritev
    sys/uio.h
    HAVE_PWRITEV)

check_symbol_exists(
    ioctl
    sys/ioctl.h
    HAVE_IOCTL)

check_symbol_exists(
    sched_getaffinity
    "sched.h"
    HAVE_SCHED_GETAFFINITY)

check_symbol_exists(
    sched_setaffinity
    "sched.h"
    HAVE_SCHED_SETAFFINITY)

check_symbol_exists(
    sched_getcpu
    "sched.h"
    HAVE_SCHED_GETCPU)

check_symbol_exists(
    pthread_setcancelstate
    "pthread.h"
    HAVE_PTHREAD_SETCANCELSTATE)

check_include_files(
    gnu/lib-names.h
    HAVE_GNU_LIBNAMES_H)

check_symbol_exists(
    TIOCGWINSZ
    "sys/ioctl.h"
    HAVE_TIOCGWINSZ)

check_symbol_exists(
    TIOCSWINSZ
    "sys/ioctl.h"
    HAVE_TIOCSWINSZ)

check_symbol_exists(
    tcgetattr
    termios.h
    HAVE_TCGETATTR)

check_symbol_exists(
    tcsetattr
    termios.h
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

check_symbol_exists(
    cfsetspeed
    termios.h
    HAVE_CFSETSPEED)

check_symbol_exists(
    cfmakeraw
    termios.h
    HAVE_CFMAKERAW)

check_struct_has_member(
    "struct utsname"
    domainname
    "sys/utsname.h"
    HAVE_UTSNAME_DOMAINNAME)

check_struct_has_member(
    "struct stat"
    st_birthtimespec
    "sys/types.h;sys/stat.h"
    HAVE_STAT_BIRTHTIME)

check_struct_has_member(
    "struct stat"
    st_flags
    "sys/types.h;sys/stat.h"
    HAVE_STAT_FLAGS)

check_symbol_exists(
    lchflags
    "sys/types.h;sys/stat.h"
    HAVE_LCHFLAGS)

check_struct_has_member(
    "struct stat"
    st_atimespec
    "sys/types.h;sys/stat.h"
    HAVE_STAT_TIMESPEC)

check_struct_has_member(
    "struct stat"
    st_atim
    "sys/types.h;sys/stat.h"
    HAVE_STAT_TIM)

check_struct_has_member(
    "struct stat"
    st_atimensec
    "sys/types.h;sys/stat.h"
    HAVE_STAT_NSEC)

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
    "struct statvfs"
    f_fstypename
    "sys/mount.h"
    HAVE_STATVFS_FSTYPENAME)

check_struct_has_member(
    "struct statvfs"
    f_basetype
    "sys/statvfs.h"
    HAVE_STATVFS_BASETYPE)

set(CMAKE_EXTRA_INCLUDE_FILES dirent.h)
check_type_size(
    "((struct dirent*)0)->d_name"
    DIRENT_NAME_SIZE)
set(CMAKE_EXTRA_INCLUDE_FILES)

# statfs: Find whether this struct exists
if (HAVE_STATFS_FSTYPENAME OR HAVE_STATVFS_FSTYPENAME)
    set (STATFS_INCLUDES sys/mount.h)
else ()
    set (STATFS_INCLUDES sys/statfs.h)
endif ()

set(CMAKE_EXTRA_INCLUDE_FILES ${STATFS_INCLUDES})

check_symbol_exists(
    "statfs"
    ${STATFS_INCLUDES}
    HAVE_STATFS)

check_symbol_exists(
    "getrlimit"
    "sys/resource.h"
    HAVE_GETRLIMIT)

check_symbol_exists(
    "setrlimit"
    "sys/resource.h"
    HAVE_SETRLIMIT)

check_type_size(
    "struct statfs"
    STATFS_SIZE
    BUILTIN_TYPES_ONLY)
set(CMAKE_EXTRA_INCLUDE_FILES) # reset CMAKE_EXTRA_INCLUDE_FILES
# /statfs

check_c_source_compiles(
    "
    #include <string.h>
    int main(void)
    {
        char buffer[1];
        char c = *strerror_r(0, buffer, 0);
        return 0;
    }
    "
    HAVE_GNU_STRERROR_R)

check_c_source_compiles(
    "
    #include <dirent.h>
    #include <stddef.h>
    int main(void)
    {
        DIR* dir = NULL;
        struct dirent* entry = NULL;
        struct dirent* result;
        readdir_r(dir, entry, &result);
        return 0;
    }
    "
    HAVE_READDIR_R)

check_c_source_compiles(
    "
    #include <sys/types.h>
    #include <sys/event.h>
    int main(void)
    {
        struct kevent event;
        void* data;
        EV_SET(&event, 0, EVFILT_READ, 0, 0, 0, data);
        return 0;
    }
    "
    KEVENT_HAS_VOID_UDATA)

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

# do not use sendfile() on iOS/tvOS, it causes SIGSYS at runtime on devices
if(NOT CLR_CMAKE_TARGET_IOS AND NOT CLR_CMAKE_TARGET_TVOS)
    check_c_source_compiles(
        "
        #include <sys/sendfile.h>
        int main(void) { int i = sendfile(0, 0, 0, 0); return 0; }
        "
        HAVE_SENDFILE_4)

    check_c_source_compiles(
        "
        #include <stdlib.h>
        #include <sys/types.h>
        #include <sys/socket.h>
        #include <sys/uio.h>
        int main(void) { int i = sendfile(0, 0, 0, NULL, NULL, 0); return 0; }
        "
        HAVE_SENDFILE_6)

    check_c_source_compiles(
        "
        #include <stdlib.h>
        #include <sys/types.h>
        #include <sys/socket.h>
        #include <sys/uio.h>
        int main(void) { int i = sendfile(0, 0, 0, 0, NULL, NULL, 0); return 0; }
        "
        HAVE_SENDFILE_7)
endif()

check_symbol_exists(
    fcopyfile
    copyfile.h
    HAVE_FCOPYFILE)

check_include_files(
     "sys/sockio.h"
     HAVE_SYS_SOCKIO_H)

check_include_files(
     "linux/ethtool.h"
     HAVE_ETHTOOL_H)

check_include_files(
     "sys/poll.h"
     HAVE_SYS_POLL_H)

check_include_files(
     "sys/proc_info.h"
     HAVE_SYS_PROCINFO_H)

check_symbol_exists(
    epoll_create1
    sys/epoll.h
    HAVE_EPOLL)

check_symbol_exists(
    accept4
    sys/socket.h
    HAVE_ACCEPT4)

set(PREVIOUS_CMAKE_REQUIRED_LIBRARIES ${CMAKE_REQUIRED_LIBRARIES})
if(CLR_CMAKE_TARGET_HAIKU)
    set(CMAKE_REQUIRED_LIBRARIES "bsd")
endif()

check_symbol_exists(
    kqueue
    "sys/types.h;sys/event.h"
    HAVE_KQUEUE)
set(CMAKE_REQUIRED_LIBRARIES ${PREVIOUS_CMAKE_REQUIRED_LIBRARIES})

check_symbol_exists(
    disconnectx
    "sys/socket.h"
    HAVE_DISCONNECTX)

set(PREVIOUS_CMAKE_REQUIRED_FLAGS ${CMAKE_REQUIRED_FLAGS})
set(CMAKE_REQUIRED_FLAGS "-Werror -Wsign-conversion")
check_c_source_compiles(
     "
     #include <stddef.h>
     #include <sys/types.h>
     #include <netdb.h>

     int main(void)
     {
        const struct sockaddr *addr;
        socklen_t addrlen = 0;
        char *host = NULL;
        socklen_t hostlen = 0;
        char *serv = NULL;
        socklen_t servlen = 0;
        int flags = 0;
        int result = getnameinfo(addr, addrlen, host, hostlen, serv, servlen, flags);
        return 0;
     }
     "
     HAVE_GETNAMEINFO_SIGNED_FLAGS)
set(CMAKE_REQUIRED_FLAGS ${PREVIOUS_CMAKE_REQUIRED_FLAGS})

set(HAVE_SUPPORT_FOR_DUAL_MODE_IPV4_PACKET_INFO 0)

if (CLR_CMAKE_TARGET_LINUX)
    if (NOT CLR_CMAKE_TARGET_ANDROID)
        set(CMAKE_REQUIRED_LIBRARIES rt)
    endif ()

    set(HAVE_SUPPORT_FOR_DUAL_MODE_IPV4_PACKET_INFO 1)
endif ()

check_symbol_exists(
    malloc_size
    malloc/malloc.h
    HAVE_MALLOC_SIZE)
check_symbol_exists(
    malloc_usable_size
    malloc.h
    HAVE_MALLOC_USABLE_SIZE)
check_symbol_exists(
    malloc_usable_size
    malloc_np.h
    HAVE_MALLOC_USABLE_SIZE_NP)
check_symbol_exists(
    posix_memalign
    stdlib.h
    HAVE_POSIX_MEMALIGN)

if(CLR_CMAKE_TARGET_IOS)
    # Manually set results from check_c_source_runs() since it's not possible to actually run it during CMake configure checking
    unset(HAVE_SHM_OPEN_THAT_WORKS_WELL_ENOUGH_WITH_MMAP)
    unset(HAVE_ALIGNED_ALLOC)   # only exists on iOS 13+
    set(HAVE_CLOCK_MONOTONIC 1)
    set(HAVE_CLOCK_REALTIME 1)
    unset(HAVE_FORK) # exists but blocked by kernel
elseif(CLR_CMAKE_TARGET_MACCATALYST)
    # Manually set results from check_c_source_runs() since it's not possible to actually run it during CMake configure checking
    unset(HAVE_SHM_OPEN_THAT_WORKS_WELL_ENOUGH_WITH_MMAP)
    unset(HAVE_ALIGNED_ALLOC)   # only exists on iOS 13+
    set(HAVE_CLOCK_MONOTONIC 1)
    set(HAVE_CLOCK_REALTIME 1)
    unset(HAVE_FORK) # exists but blocked by kernel
elseif(CLR_CMAKE_TARGET_TVOS)
    # Manually set results from check_c_source_runs() since it's not possible to actually run it during CMake configure checking
    unset(HAVE_SHM_OPEN_THAT_WORKS_WELL_ENOUGH_WITH_MMAP)
    unset(HAVE_ALIGNED_ALLOC)   # only exists on iOS 13+
    set(HAVE_CLOCK_MONOTONIC 1)
    set(HAVE_CLOCK_REALTIME 1)
    unset(HAVE_FORK) # exists but blocked by kernel
elseif(CLR_CMAKE_TARGET_ANDROID)
    # Manually set results from check_c_source_runs() since it's not possible to actually run it during CMake configure checking
    unset(HAVE_SHM_OPEN_THAT_WORKS_WELL_ENOUGH_WITH_MMAP)
    unset(HAVE_ALIGNED_ALLOC) # only exists on newer Android
    set(HAVE_CLOCK_MONOTONIC 1)
    set(HAVE_CLOCK_REALTIME 1)
elseif(CLR_CMAKE_TARGET_BROWSER OR CLR_CMAKE_TARGET_WASI)
    set(HAVE_FORK 0)
else()
    check_symbol_exists(
        aligned_alloc
        stdlib.h
        HAVE_ALIGNED_ALLOC)

    check_c_source_runs(
        "
        #include <sys/mman.h>
        #include <fcntl.h>
        #include <unistd.h>

        int main(void)
        {
            int fd = shm_open(\"/corefx_configure_shm_open\", O_CREAT | O_RDWR, 0777);
            if (fd == -1)
                return -1;

            shm_unlink(\"/corefx_configure_shm_open\");

            // NOTE: PROT_EXEC and MAP_PRIVATE don't work well with shm_open
            //       on at least the current version of Mac OS X

            if (mmap(NULL, 1, PROT_EXEC, MAP_PRIVATE, fd, 0) == MAP_FAILED)
                return -1;

            return 0;
        }
        "
        HAVE_SHM_OPEN_THAT_WORKS_WELL_ENOUGH_WITH_MMAP)

    check_c_source_runs(
        "
        #include <stdlib.h>
        #include <time.h>
        #include <sys/time.h>
        int main(void)
        {
            int ret;
            struct timespec ts;
            ret = clock_gettime(CLOCK_MONOTONIC, &ts);
            exit(ret);
            return 0;
        }
        "
        HAVE_CLOCK_MONOTONIC)

    check_c_source_runs(
        "
        #include <stdlib.h>
        #include <time.h>
        #include <sys/time.h>
        int main(void)
        {
            int ret;
            struct timespec ts;
            ret = clock_gettime(CLOCK_REALTIME, &ts);
            exit(ret);
            return 0;
        }
        "
        HAVE_CLOCK_REALTIME)
endif()

check_symbol_exists(
    clock_gettime_nsec_np
    time.h
    HAVE_CLOCK_GETTIME_NSEC_NP)

check_library_exists(pthread pthread_create "" HAVE_LIBPTHREAD)
check_library_exists(c pthread_create "" HAVE_PTHREAD_IN_LIBC)

if (HAVE_LIBPTHREAD)
  set(PTHREAD_LIBRARY pthread)
elseif (HAVE_PTHREAD_IN_LIBC)
  set(PTHREAD_LIBRARY c)
endif()

if (NOT CLR_CMAKE_TARGET_WASI)
    check_library_exists(${PTHREAD_LIBRARY} pthread_condattr_setclock "" HAVE_PTHREAD_CONDATTR_SETCLOCK)
endif()

check_symbol_exists(
    futimes
    sys/time.h
    HAVE_FUTIMES)

check_symbol_exists(
    futimens
    sys/stat.h
    HAVE_FUTIMENS)

check_symbol_exists(
    fchmod
    sys/stat.h
    HAVE_FCHMOD)

check_symbol_exists(
    chmod
    sys/stat.h
    HAVE_CHMOD)

check_symbol_exists(
    utimensat
    sys/stat.h
    HAVE_UTIMENSAT)

check_symbol_exists(
    lutimes
    sys/time.h
    HAVE_LUTIMES)

set (PREVIOUS_CMAKE_REQUIRED_FLAGS ${CMAKE_REQUIRED_FLAGS})
set (CMAKE_REQUIRED_FLAGS "-Werror -Wsign-conversion")

check_c_source_compiles(
    "
    #include <stddef.h>
    #include <sys/socket.h>

    int main(void)
    {
        int fd = -1;
        struct sockaddr* addr = NULL;
        socklen_t addrLen = 0;

        int err = bind(fd, addr, addrLen);
        return 0;
    }
    "
    BIND_ADDRLEN_UNSIGNED)

check_c_source_compiles(
    "
    #include <netinet/in.h>
    #include <netinet/tcp.h>

    int main(void)
    {
        struct ipv6_mreq opt;
        unsigned int index = 0;
        opt.ipv6mr_interface = index;
        return 0;
    }
    "
    IPV6MR_INTERFACE_UNSIGNED)

check_include_files(
    "sys/inotify.h"
    HAVE_SYS_INOTIFY_H)

check_c_source_compiles(
    "
    #include <sys/inotify.h>

    int main(void)
    {
        intptr_t fd;
        uint32_t wd;
        return inotify_rm_watch(fd, wd);
    }
    "
    INOTIFY_RM_WATCH_WD_UNSIGNED)

set (CMAKE_REQUIRED_FLAGS ${PREVIOUS_CMAKE_REQUIRED_FLAGS})

check_prototype_definition(
    getpriority
    "int getpriority(int which, int who)"
    0
    "sys/resource.h"
    PRIORITY_REQUIRES_INT_WHO)

check_prototype_definition(
    kevent
    "int kevent(int kg, const struct kevent* chagelist, int nchanges, struct kevent* eventlist, int nevents, const struct timespec* timeout)"
    0
    "sys/types.h;sys/event.h"
    KEVENT_REQUIRES_INT_PARAMS)

check_prototype_definition(
    statfs
    "int statfs(const char *path, struct statfs *buf)"
    0
    ${STATFS_INCLUDES}
    HAVE_NON_LEGACY_STATFS)

check_prototype_definition(
    ioctl
    "int ioctl(int fd, int request, ...)"
    0
    "sys/ioctl.h"
    HAVE_IOCTL_WITH_INT_REQUEST)

check_c_source_compiles(
    "
    #include <stdlib.h>
    #include <unistd.h>
    #include <string.h>

    int main(void)
    {
        return mkstemps(\"abc\", 3);
    }
    "
    HAVE_MKSTEMPS)

check_c_source_compiles(
    "
    #include <stdlib.h>
    #include <unistd.h>
    #include <string.h>

    int main(void)
    {
        return mkstemp(\"abc\");
    }
    "
    HAVE_MKSTEMP)

if (NOT HAVE_MKSTEMPS AND NOT HAVE_MKSTEMP AND NOT CLR_CMAKE_TARGET_WASI)
    message(FATAL_ERROR "Cannot find mkstemps nor mkstemp on this platform.")
endif()

check_c_source_compiles(
    "
    #include <sys/types.h>
    #include <sys/socketvar.h>
    #include <sys/queue.h>
    #include <netinet/in.h>
    #include <netinet/ip.h>
    #include <netinet/tcp.h>
    #include <netinet/tcp_var.h>
    int main(void) { return 0; }
    "
    HAVE_NETINET_TCP_VAR_H)

check_c_source_compiles(
    "
    #include <sys/types.h>
    #include <sys/socketvar.h>
    #include <sys/queue.h>
    #include <netinet/in.h>
    #include <netinet/ip.h>
    #include <netinet/ip_var.h>
    #include <netinet/udp.h>
    #include <netinet/udp_var.h>
    int main(void) { return 0; }
    "
    HAVE_NETINET_UDP_VAR_H)

check_c_source_compiles(
    "
    #include <sys/types.h>
    #include <sys/socketvar.h>
    #include <sys/queue.h>
    #include <netinet/in.h>
    #include <netinet/ip.h>
    #include <netinet/ip_var.h>
    int main(void) { return 0; }
    "
    HAVE_NETINET_IP_VAR_H)

check_c_source_compiles(
    "
    #include <sys/types.h>
    #include <sys/socketvar.h>
    #include <sys/queue.h>
    #include <netinet/in.h>
    #include <netinet/ip.h>
    #include <netinet/ip_icmp.h>
    #include <netinet/icmp_var.h>
    int main(void) { return 0; }
    "
    HAVE_NETINET_ICMP_VAR_H)

check_include_files(
    sys/cdefs.h
    HAVE_SYS_CDEFS_H)

if (HAVE_SYS_CDEFS_H)
    set(CMAKE_REQUIRED_DEFINITIONS "-DHAVE_SYS_CDEFS_H")
endif()

# If sys/cdefs is not included on Android, this check will fail because
# __BEGIN_DECLS is not defined
check_c_source_compiles(
    "
#ifdef HAVE_SYS_CDEFS_H
    #include <sys/cdefs.h>
#endif
    #include <netinet/tcp.h>
    int main(void) { int x = TCP_ESTABLISHED; return x; }
    "
    HAVE_TCP_H_TCPSTATE_ENUM)

set(CMAKE_REQUIRED_DEFINITIONS)

check_symbol_exists(
    TCPS_ESTABLISHED
    "netinet/tcp_fsm.h"
    HAVE_TCP_FSM_H)

check_symbol_exists(
    getgrouplist
    "unistd.h;grp.h"
    HAVE_GETGROUPLIST)

check_include_files(
    "syslog.h"
    HAVE_SYSLOG_H)

check_include_files(
    "termios.h"
    HAVE_TERMIOS_H)

check_include_files(
    "dlfcn.h"
    HAVE_DLFCN_H)

check_include_files(
    "sys/statvfs.h"
    HAVE_SYS_STATVFS_H)

check_include_files(
    "net/if.h"
    HAVE_NET_IF_H)

check_include_files(
    "pthread.h"
    HAVE_PTHREAD_H)

check_include_files(
    "sys/statfs.h"
    HAVE_SYS_STATFS_H)

if(CLR_CMAKE_TARGET_MACCATALYST OR CLR_CMAKE_TARGET_IOS OR CLR_CMAKE_TARGET_TVOS)
    set(HAVE_IOS_NET_ROUTE_H 1)
    set(HAVE_IOS_NET_IFMEDIA_H 1)
    set(HAVE_IOS_NETINET_TCPFSM_H 1)
    set(HAVE_IOS_NETINET_IP_VAR_H 1)
    set(HAVE_IOS_NETINET_ICMP_VAR_H 1)
    set(HAVE_IOS_NETINET_UDP_VAR_H 1)
    set(CMAKE_EXTRA_INCLUDE_FILES
        sys/types.h
        "${CMAKE_CURRENT_SOURCE_DIR}/System.Native/ios/net/route.h"
    )
else()
    set(CMAKE_EXTRA_INCLUDE_FILES sys/types.h net/if.h net/route.h)
endif()

check_type_size(
    "struct rt_msghdr"
     HAVE_RT_MSGHDR
     BUILTIN_TYPES_ONLY)
check_type_size(
    "struct rt_msghdr2"
     HAVE_RT_MSGHDR2
     BUILTIN_TYPES_ONLY)
set(CMAKE_EXTRA_INCLUDE_FILES) # reset CMAKE_EXTRA_INCLUDE_FILES

set(CMAKE_EXTRA_INCLUDE_FILES net/if.h)
check_type_size(
    "struct if_msghdr2"
     HAVE_IF_MSGHDR2
     BUILTIN_TYPES_ONLY)
set(CMAKE_EXTRA_INCLUDE_FILES) # reset CMAKE_EXTRA_INCLUDE_FILES

if (CLR_CMAKE_TARGET_LINUX)
    # sysctl is deprecated on Linux
    set(HAVE_SYS_SYSCTL_H 0)
else ()
    check_include_files(
        "sys/types.h;sys/sysctl.h"
        HAVE_SYS_SYSCTL_H)
endif()

check_include_files(
    "sys/ioctl.h"
    HAVE_SYS_IOCTL_H)

check_include_files(
    "sys/filio.h"
    HAVE_SYS_FILIO_H)

check_include_files(
    "sys/types.h;netpacket/packet.h"
    HAVE_NETPACKET_PACKET_H)

check_include_files(
    "net/if_arp.h"
    HAVE_NET_IF_ARP_H)

check_include_files(
    "sys/mntent.h"
    HAVE_SYS_MNTENT_H)

check_include_files(
    "mntent.h"
    HAVE_MNTENT_H)

check_include_files(
    "stdint.h;net/if_media.h"
    HAVE_NET_IFMEDIA_H)

check_include_files(
    linux/rtnetlink.h
    HAVE_LINUX_RTNETLINK_H)

check_include_files(
    linux/can.h
    HAVE_LINUX_CAN_H)

check_include_files(
    IOKit/serial/ioss.h
    HAVE_IOSS_H)

check_symbol_exists(
    getpeereid
    unistd.h
    HAVE_GETPEEREID)

check_symbol_exists(
    getdomainname
    unistd.h
    HAVE_GETDOMAINNAME)

check_symbol_exists(
    uname
    sys/utsname.h
    HAVE_UNAME)

# getdomainname on OSX takes an 'int' instead of a 'size_t'
# check if compiling with 'size_t' would cause a warning
set (PREVIOUS_CMAKE_REQUIRED_FLAGS ${CMAKE_REQUIRED_FLAGS})
set (CMAKE_REQUIRED_FLAGS "-Werror -Weverything")
check_c_source_compiles(
    "
    #include <unistd.h>
    int main(void)
    {
        size_t namelen = 20;
        char name[20];
        int dummy = getdomainname(name, namelen);
        (void)dummy;
        return 0;
    }
    "
    HAVE_GETDOMAINNAME_SIZET)
set (CMAKE_REQUIRED_FLAGS ${PREVIOUS_CMAKE_REQUIRED_FLAGS})

set (PREVIOUS_CMAKE_REQUIRED_LIBRARIES ${CMAKE_REQUIRED_LIBRARIES})
if (HAVE_SYS_INOTIFY_H AND CLR_CMAKE_TARGET_FREEBSD)
    set (CMAKE_REQUIRED_LIBRARIES "-linotify -L${CROSS_ROOTFS}/usr/local/lib")
endif()

check_symbol_exists(
    inotify_init
    sys/inotify.h
    HAVE_INOTIFY_INIT)

check_symbol_exists(
    inotify_add_watch
    sys/inotify.h
    HAVE_INOTIFY_ADD_WATCH)

check_symbol_exists(
    inotify_rm_watch
    sys/inotify.h
    HAVE_INOTIFY_RM_WATCH)
set (CMAKE_REQUIRED_LIBRARIES ${PREVIOUS_CMAKE_REQUIRED_LIBRARIES})

set (HAVE_INOTIFY 0)
if (HAVE_INOTIFY_INIT AND HAVE_INOTIFY_ADD_WATCH AND HAVE_INOTIFY_RM_WATCH)
    set (HAVE_INOTIFY 1)
elseif (CLR_CMAKE_TARGET_LINUX AND NOT CLR_CMAKE_TARGET_BROWSER AND NOT CLR_CMAKE_TARGET_WASI)
    message(FATAL_ERROR "Cannot find inotify functions on a Linux platform.")
endif()

option(HeimdalGssApi "use heimdal implementation of GssApi" OFF)

if (HeimdalGssApi)
   check_include_files(
       gssapi/gssapi.h
       HAVE_HEIMDAL_HEADERS)
endif()

check_include_files(
    GSS/GSS.h
    HAVE_GSSFW_HEADERS)

if (HAVE_GSSFW_HEADERS)
    check_symbol_exists(
        GSS_SPNEGO_MECHANISM
        "GSS/GSS.h"
        HAVE_GSS_SPNEGO_MECHANISM)
else ()
    check_symbol_exists(
        GSS_SPNEGO_MECHANISM
        "gssapi/gssapi.h"
        HAVE_GSS_SPNEGO_MECHANISM)
endif ()

check_symbol_exists(getauxval sys/auxv.h HAVE_GETAUXVAL)
check_include_files(crt_externs.h HAVE_CRT_EXTERNS_H)

if (HAVE_CRT_EXTERNS_H)
    check_c_source_compiles(
    "
    #include <crt_externs.h>
    int main(void) { char** e = *(_NSGetEnviron()); return 0; }
    "
    HAVE_NSGETENVIRON)
endif()

set (CMAKE_REQUIRED_LIBRARIES)

check_c_source_compiles(
    "
    #include <sys/inotify.h>
    int main(void)
    {
        uint32_t mask = IN_EXCL_UNLINK;
        return 0;
    }
    "
    HAVE_IN_EXCL_UNLINK)

check_c_source_compiles(
    "
    #include <netinet/tcp.h>
    int main(void)
    {
        int x = TCP_KEEPALIVE;
        return x;
    }
    "
    HAVE_TCP_H_TCP_KEEPALIVE)

check_c_source_compiles(
    "
    #include <unistd.h>
    int main(void)
    {
        size_t result;
        (void)__builtin_mul_overflow(0, 0, &result);
    }
    "
    HAVE_BUILTIN_MUL_OVERFLOW)

check_symbol_exists(
    makedev
    sys/file.h
    HAVE_MAKEDEV_FILEH)

check_symbol_exists(
    makedev
    sys/sysmacros.h
    HAVE_MAKEDEV_SYSMACROSH)

if (NOT HAVE_MAKEDEV_FILEH AND NOT HAVE_MAKEDEV_SYSMACROSH AND NOT CLR_CMAKE_TARGET_WASI AND NOT CLR_CMAKE_TARGET_HAIKU)
  message(FATAL_ERROR "Cannot find the makedev function on this platform.")
endif()

check_symbol_exists(
    getgrgid_r
    grp.h
    HAVE_GETGRGID_R)

check_c_source_compiles(
    "
    #include <asm/termbits.h>
    #include <sys/ioctl.h>

    int main(void)
    {
        struct termios2 t;
        return 0;
    }
    "
    HAVE_TERMIOS2)

configure_file(
    ${CMAKE_CURRENT_SOURCE_DIR}/Common/pal_config.h.in
    ${CMAKE_CURRENT_BINARY_DIR}/Common/pal_config.h)
