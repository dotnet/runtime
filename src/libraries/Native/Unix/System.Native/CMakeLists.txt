project(System.Native C)

set(NATIVE_SOURCES
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

if (CLR_CMAKE_TARGET_IOS OR CLR_CMAKE_TARGET_TVOS)
    set(NATIVE_SOURCES ${NATIVE_SOURCES}
        pal_log.m
        pal_searchpath.m)
else ()
    set(NATIVE_SOURCES ${NATIVE_SOURCES} pal_console.c)
endif ()

if (CLR_CMAKE_TARGET_LINUX AND NOT CLR_CMAKE_TARGET_BROWSER)
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
    elseif (CLR_CMAKE_TARGET_FREEBSD)
        target_link_libraries(System.Native pthread)
        if (HAVE_INOTIFY)
            find_library(INOTIFY_LIBRARY inotify HINTS /usr/local/lib)
            target_link_libraries(System.Native ${INOTIFY_LIBRARY})
        endif ()
    elseif (CLR_CMAKE_TARGET_SUNOS)
        target_link_libraries(System.Native socket)
    endif ()
    install_with_stripped_symbols (System.Native PROGRAMS .)
endif ()

add_library(System.Native-Static
    STATIC
    ${NATIVE_SOURCES}
)

if (CLR_CMAKE_TARGET_IOS OR CLR_CMAKE_TARGET_TVOS)
    target_link_libraries(System.Native "-framework Foundation")
endif ()

set_target_properties(System.Native-Static PROPERTIES OUTPUT_NAME System.Native CLEAN_DIRECT_OUTPUT 1)

install (TARGETS System.Native-Static DESTINATION .)
