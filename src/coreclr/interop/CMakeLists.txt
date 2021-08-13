set(CMAKE_INCLUDE_CURRENT_DIR ON)
include_directories(BEFORE inc)

set(INTEROP_COMMON_SOURCES
    interoplib.cpp
)

set(INTEROP_COMMON_HEADERS
    inc/interoplibimports.h
    inc/interoplib.h
    platform.h
)

set(INTEROP_SOURCES
    ${INTEROP_COMMON_SOURCES}
)

set(INTEROP_HEADERS
    ${INTEROP_COMMON_HEADERS}
)

if (FEATURE_COMWRAPPERS)
    list(APPEND INTEROP_SOURCES
        ${INTEROP_HEADERS}
        comwrappers.cpp
        comwrappers.hpp
        trackerobjectmanager.cpp
        referencetrackertypes.hpp)
endif(FEATURE_COMWRAPPERS)

convert_to_absolute_path(INTEROP_SOURCES ${INTEROP_SOURCES})

add_library_clr(interop_obj
    OBJECT
    ${INTEROP_SOURCES}
)

add_library(interop INTERFACE)
target_sources(interop INTERFACE $<TARGET_OBJECTS:interop_obj>)