project(System.Native C)

set(NATIVE_SOURCES
    pal_console.c
    pal_errno.c
    pal_interfaceaddresses.c
    pal_io.c
    pal_maphardwaretype.c
    pal_memory.c
    pal_mount.c
    pal_networking.c
    pal_networkstatistics.c
    pal_process.c
    pal_random.c
    pal_runtimeinformation.c
    pal_runtimeextensions.c
    pal_signal.c
    pal_string.c
    pal_tcpstate.c
    pal_time.c
    pal_uid.c
    pal_datetime.c
    pal_sysctl.c
)

if (CLR_CMAKE_TARGET_LINUX)
    set(NATIVE_SOURCES ${NATIVE_SOURCES} pal_networkchange.c)

    if (!HAVE_LINUX_RTNETLINK_H)
        message(FATAL_ERROR "Could not find linux/rtnetlink.h")
    endif ()
elseif (CLR_CMAKE_TARGET_FREEBSD)
    set(NATIVE_SOURCES ${NATIVE_SOURCES} pal_networkchange.c)

    if (!HAVE_RT_MSGHDR)
        message(FATAL_ERROR "Could not find net/route.h")
    endif ()
endif ()

if (GEN_SHARED_LIB)
    add_library(System.Native
        SHARED
        ${NATIVE_SOURCES}
        ${VERSION_FILE_PATH}
    )
    if (CLR_CMAKE_TARGET_LINUX AND NOT CLR_CMAKE_TARGET_ANDROID)
        target_link_libraries(System.Native rt)
    endif ()

    if (CLR_CMAKE_TARGET_FREEBSD)
        target_link_libraries(System.Native pthread)
    if (HAVE_INOTIFY)
        find_library(INOTIFY_LIBRARY inotify HINTS /usr/local/lib)
        target_link_libraries(System.Native ${INOTIFY_LIBRARY})
    endif ()
    endif ()
    install_library_and_symbols (System.Native)
endif ()

add_library(System.Native-Static
    STATIC
    ${NATIVE_SOURCES}
)

# Disable the "lib" prefix and override default name
set_target_properties(System.Native-Static PROPERTIES PREFIX "")
set_target_properties(System.Native-Static PROPERTIES OUTPUT_NAME System.Native CLEAN_DIRECT_OUTPUT 1)

install (TARGETS System.Native-Static DESTINATION .)
