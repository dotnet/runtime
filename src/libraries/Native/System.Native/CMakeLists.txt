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
    pal_runtimeinformation.cpp
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
)

if (CMAKE_SYSTEM_NAME STREQUAL Linux)
    target_link_libraries(System.Native rt)
endif ()

install (TARGETS System.Native DESTINATION .)
