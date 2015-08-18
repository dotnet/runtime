project(System.Native)

set(NATIVE_SOURCES
    pal_errno.cpp
    pal_exec.cpp
    pal_stat.cpp
)

add_library(System.Native
    SHARED
    ${NATIVE_SOURCES}
)

install (TARGETS System.Native DESTINATION .)
