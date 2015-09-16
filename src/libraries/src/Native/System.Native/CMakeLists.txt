project(System.Native)

set(NATIVE_SOURCES
    pal_errno.cpp
    pal_io.cpp
    pal_mount.cpp
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
endif ()

install (TARGETS System.Native DESTINATION .)
