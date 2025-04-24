include(configure.cmake)

set(SOURCES
    cpufeatures.c
    guid.c
    random.c
    debugger.c
    strings.c
    time.c
    unicodedata.c
    utf8.c
    xoshiro128pp.c
    log.c
)

# Provide an object library for scenarios where we ship static libraries
include_directories(${CLR_SRC_NATIVE_DIR} ${CMAKE_CURRENT_BINARY_DIR})

add_library(minipal_objects OBJECT ${SOURCES})

if (WIN32)
    target_link_libraries(minipal_objects PUBLIC bcrypt)
endif()

# Add a copy of the minipal object library with interprocedural optimization disabled
# for NativeAOT scenarios, where we ship static libraries that need to be able to be run with
# a variety of toolchain versions, not only the exact one we built with.
add_library(aotminipal STATIC ${SOURCES})
set_target_properties(aotminipal PROPERTIES INTERPROCEDURAL_OPTIMIZATION OFF)

if (WIN32)
    target_link_libraries(aotminipal PUBLIC bcrypt)
endif()

# Provide a static library for our shared library and executable scenarios
# for easier usability.
add_library(minipal STATIC)
target_link_libraries(minipal PRIVATE minipal_objects)

if(CLR_CMAKE_HOST_ANDROID)
  target_link_libraries(minipal PRIVATE log)
endif(CLR_CMAKE_HOST_ANDROID)

add_library(minipal_sanitizer_support OBJECT
    sansupport.c)
# Exclude this target from the default build as we may not have sanitzer headers available
# in a non-sanitized build.
set_target_properties(minipal_sanitizer_support PROPERTIES EXCLUDE_FROM_ALL ON)
