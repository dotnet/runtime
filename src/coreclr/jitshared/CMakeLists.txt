# CMakeLists.txt for jitshared - shared allocator infrastructure
# This directory contains allocator code shared between the JIT and interpreter.

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(JITSHARED_SOURCES
    histogram.cpp
)

set(JITSHARED_HEADERS
    arenaallocator.h
    compallocator.h
    dumpable.h
    histogram.h
    jitshared.h
    memstats.h
)

# Create object library for shared allocator code
add_library_clr(jitshared OBJECT ${JITSHARED_SOURCES} ${JITSHARED_HEADERS})

target_include_directories(jitshared
    PUBLIC
    ${CMAKE_CURRENT_SOURCE_DIR}
)
