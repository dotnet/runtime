project(System.Native C)

if (NOT CLR_CMAKE_TARGET_IOS AND NOT CLR_CMAKE_TARGET_TVOS)
    add_definitions(-DHAS_CONSOLE_SIGNALS)
endif ()

include_directories("${CLR_SRC_NATIVE_DIR}/common")

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
    pal_threading.c
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
    set(NATIVE_SOURCES ${NATIVE_SOURCES}
        pal_searchpath.c
        pal_console.c
        pal_log.c)
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

include(${CMAKE_CURRENT_LIST_DIR}/extra_libs.cmake)

set(NATIVE_LIBS_EXTRA)
append_extra_system_libs(NATIVE_LIBS_EXTRA)

if (GEN_SHARED_LIB)
    add_library(System.Native
        SHARED
        ${NATIVE_SOURCES}
        ${VERSION_FILE_PATH}
    )

    target_link_libraries(System.Native
        ${NATIVE_LIBS_EXTRA}
    )

    install_with_stripped_symbols (System.Native PROGRAMS .)
endif ()

add_library(System.Native-Static
    STATIC
    ${NATIVE_SOURCES}
    entrypoints.c
)

set_target_properties(System.Native-Static PROPERTIES OUTPUT_NAME System.Native CLEAN_DIRECT_OUTPUT 1)

install (TARGETS System.Native-Static DESTINATION ${STATIC_LIB_DESTINATION})
