include(CheckFunctionExists)
include(CheckIncludeFiles)
include(CheckLibraryExists)
include(CheckCSourceCompiles)
include(CheckSymbolExists)

check_include_files("windows.h;bcrypt.h" HAVE_BCRYPT_H)
check_include_files("sys/auxv.h;asm/hwcap.h" HAVE_AUXV_HWCAP_H)
check_include_files("asm/hwprobe.h" HAVE_HWPROBE_H)
check_include_files("sys/resource.h" HAVE_RESOURCE_H)

check_function_exists(sysctlbyname HAVE_SYSCTLBYNAME)
check_function_exists(fsync HAVE_FSYNC)

check_symbol_exists(elf_aux_info "sys/auxv.h" HAVE_ELF_AUX_INFO)
check_symbol_exists(arc4random_buf "stdlib.h" HAVE_ARC4RANDOM_BUF)
check_symbol_exists(getauxval "sys/auxv.h" HAVE_GETAUXVAL)
check_symbol_exists(getrandom "sys/random.h" HAVE_GETRANDOM)
check_symbol_exists(getentropy "unistd.h" HAVE_GETENTROPY)
check_symbol_exists(O_CLOEXEC fcntl.h HAVE_O_CLOEXEC)
check_symbol_exists(CLOCK_MONOTONIC_COARSE time.h HAVE_CLOCK_MONOTONIC_COARSE)
check_symbol_exists(clock_gettime_nsec_np time.h HAVE_CLOCK_GETTIME_NSEC_NP)
if(CLR_CMAKE_HOST_UNIX)
    check_library_exists(pthread pthread_create "" HAVE_LIBPTHREAD)
    check_library_exists(c pthread_create "" HAVE_PTHREAD_IN_LIBC)
    if(HAVE_LIBPTHREAD)
        set(PTHREAD_LIBRARY pthread)
    elseif(HAVE_PTHREAD_IN_LIBC)
        set(PTHREAD_LIBRARY c)
    endif()
    if(PTHREAD_LIBRARY)
        set(PREVIOUS_CMAKE_REQUIRED_LIBRARIES ${CMAKE_REQUIRED_LIBRARIES})
        list(APPEND CMAKE_REQUIRED_LIBRARIES ${PTHREAD_LIBRARY})
        check_c_source_compiles("
            #if defined(__linux__) && !defined(_GNU_SOURCE)
            #define _GNU_SOURCE
            #endif
            #include <pthread.h>
            int main(void)
            {
                pthread_rwlockattr_t attributes;
                if (pthread_rwlockattr_init(&attributes) != 0)
                    return 1;

                int result = pthread_rwlockattr_setkind_np(&attributes, PTHREAD_RWLOCK_PREFER_WRITER_NONRECURSIVE_NP);
                pthread_rwlockattr_destroy(&attributes);
                return result;
            }"
            HAVE_PTHREAD_RWLOCK_PREFER_WRITER_NONRECURSIVE_NP)
        set(CMAKE_REQUIRED_LIBRARIES ${PREVIOUS_CMAKE_REQUIRED_LIBRARIES})

        check_library_exists(${PTHREAD_LIBRARY} pthread_condattr_setclock "" HAVE_PTHREAD_CONDATTR_SETCLOCK)
    endif()
endif()

if(CMAKE_C_BYTE_ORDER STREQUAL "BIG_ENDIAN")
    set(BIGENDIAN 1)
endif()

configure_file(${CMAKE_CURRENT_LIST_DIR}/minipalconfig.h.in ${CMAKE_CURRENT_BINARY_DIR}/minipalconfig.h)
