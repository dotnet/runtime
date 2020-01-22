set(CMAKE_INCLUDE_CURRENT_DIR ON)
#include_directories(BEFORE "../vm/${ARCH_SOURCES_DIR}")
#include_directories(BEFORE "../vm")
#include_directories(BEFORE "inc")

set(INTEROP_COMMON_SOURCES
)

set(INTEROP_COMMON_HEADERS
    interoplib.h
    platform.h
)

set(INTEROP_SOURCES
    ${INTEROP_COMMON_SOURCES}
)

set(INTEROP_HEADERS
    ${INTEROP_COMMON_HEADERS}
)

if (WIN32)
    list(APPEND INTEROP_SOURCES
        ${INTEROP_HEADERS}
        comwrappers.cpp
        comwrappers.h
        referencetrackertypes.h)
endif(WIN32)

convert_to_absolute_path(INTEROP_SOURCES ${INTEROP_SOURCES})

add_library_clr(interop
    STATIC
    ${INTEROP_SOURCES}
)
