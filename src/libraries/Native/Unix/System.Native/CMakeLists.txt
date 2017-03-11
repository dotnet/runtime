project(System.Native)

set(NATIVE_SOURCES
    pal_console.cpp
    pal_errno.cpp
    pal_interfaceaddresses.cpp
    pal_io.cpp
    pal_maphardwaretype.cpp
    pal_memory.cpp
    pal_mount.cpp
    pal_networking.cpp
    pal_networkstatistics.cpp
    pal_process.cpp
    pal_random.cpp
    pal_runtimeinformation.cpp
    pal_runtimeextensions.cpp
    pal_string.cpp
    pal_tcpstate.cpp
    pal_time.cpp
    pal_uid.cpp
)

if (CMAKE_SYSTEM_NAME STREQUAL Linux)
    set(NATIVE_SOURCES ${NATIVE_SOURCES} pal_networkchange.cpp)

    if (!HAVE_LINUX_RTNETLINK_H)
        message(FATAL_ERROR "Could not find linux/rtnetlink.h")
    endif ()
endif ()

add_library(System.Native
    SHARED
    ${NATIVE_SOURCES}
    ${VERSION_FILE_PATH}
)


add_library(System.Native-Static
    STATIC
    ${NATIVE_SOURCES}
    ${VERSION_FILE_PATH}
)
SET_TARGET_PROPERTIES(System.Native-Static PROPERTIES PREFIX "")
SET_TARGET_PROPERTIES(System.Native-Static PROPERTIES OUTPUT_NAME System.Native CLEAN_DIRECT_OUTPUT 1)

if (CMAKE_SYSTEM_NAME STREQUAL Linux AND NOT CLR_CMAKE_PLATFORM_ANDROID)
    target_link_libraries(System.Native rt)
endif ()

install_library_and_symbols (System.Native)
install (TARGETS System.Native-Static DESTINATION .)
