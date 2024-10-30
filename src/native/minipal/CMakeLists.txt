include(configure.cmake)

set(SOURCES
    cpufeatures.c
    random.c
    debugger.c
    time.c
    unicodedata.c
    utf8.c
)

# Provide an object library for scenarios where we ship static libraries
include_directories(${CLR_SRC_NATIVE_DIR} ${CMAKE_CURRENT_BINARY_DIR})

add_library(minipal_objects OBJECT ${SOURCES})

# Add a copy of the minipal static library with interprocedural optimization disabled
# for NativeAOT scenarios, where we ship static libraries that need to be able to be run with
# a variety of toolchain versions, not only the exact one we built with.
add_library(aotminipal STATIC ${SOURCES})
set_target_properties(aotminipal PROPERTIES INTERPROCEDURAL_OPTIMIZATION OFF)

# Provide a static library for our shared library and executable scenarios
# for easier usability.
add_library(minipal STATIC)
target_link_libraries(minipal PRIVATE minipal_objects)

add_library(minipal_sanitizer_support OBJECT
    sansupport.c)
# Exclude this target from the default build as we may not have sanitzer headers available
# in a non-sanitized build.
set_target_properties(minipal_sanitizer_support PROPERTIES EXCLUDE_FROM_ALL ON)
