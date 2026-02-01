# CMakeLists.txt for jitshared - shared allocator infrastructure
# This directory contains allocator code shared between the JIT and interpreter.

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(JITSHARED_SOURCES
    arenaallocator.cpp
)

set(JITSHARED_HEADERS
    allocconfig.h
    arenaallocator.h
    compallocator.h
    memstats.h
)

# Create object library for shared allocator code
add_library_clr(jitshared_objects OBJECT ${JITSHARED_SOURCES})

target_include_directories(jitshared_objects
    PUBLIC
    ${CMAKE_CURRENT_SOURCE_DIR}
)
