include(configure.cmake)

set(SOURCES
    cpufeatures.c
    random.c
    time.c
    unicodedata.c
    utf8.c
)

# Provide an object library for scenarios where we ship static libraries
include_directories(${CMAKE_CURRENT_SOURCE_DIR}/../ ${CMAKE_CURRENT_BINARY_DIR})

add_library(minipal_objects OBJECT ${SOURCES})
set_target_properties(minipal_objects PROPERTIES CLR_CONTROL_FLOW_GUARD ON)

add_library(minipal_objects_no_lto OBJECT ${SOURCES})
set_target_properties(minipal_objects_no_lto PROPERTIES INTERPROCEDURAL_OPTIMIZATION OFF CLR_CONTROL_FLOW_GUARD ON)

# Provide a static library for our shared library and executable scenarios
# for easier usability.
add_library(minipal STATIC)
target_link_libraries(minipal PRIVATE minipal_objects)

add_library(minipal_sanitizer_support OBJECT
    sansupport.c)
set_target_properties(minipal_sanitizer_support PROPERTIES CLR_CONTROL_FLOW_GUARD ON)


add_subdirectory(com)
