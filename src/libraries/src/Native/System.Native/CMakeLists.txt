project(System.Native)

set(NATIVE_SOURCES
    pal_errno.cpp
    pal_io.cpp
    pal_mount.cpp
    pal_networking.cpp
    pal_process.cpp
    pal_string.cpp
    pal_time.cpp
    pal_uid.cpp
)

add_library(System.Native
    SHARED
    ${NATIVE_SOURCES}
)

if (CMAKE_SYSTEM_NAME STREQUAL Linux)
    target_link_libraries(System.Native rt)
    add_definitions(-DPLATFORM_SUPPORTS_MULTIPLE_CONNECT_ATTEMPTS)
    add_definitions(-DPLATFORM_SUPPORTS_DUAL_MODE_IPV4_PACKET_INFO)
elseif (CMAKE_SYSTEM_NAME STREQUAL Darwin)
    add_definitions(-DHAVE_THREAD_SAFE_GETHOSTBYNAME_AND_GETHOSTBYADDR)
endif ()

install (TARGETS System.Native DESTINATION .)
