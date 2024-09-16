include(configure.cmake)

# Provide an object library for scenarios where we ship static libraries
add_library(minipal_objects OBJECT
    cpufeatures.c
    random.c
    time.c
    unicodedata.c
    utf8.c)

target_include_directories(minipal_objects PRIVATE ${CLR_SRC_NATIVE_DIR} ${CMAKE_CURRENT_BINARY_DIR})

# Provide a static library for our shared library and executable scenarios
# for easier usability.
add_library(minipal STATIC)
target_link_libraries(minipal PRIVATE minipal_objects)

add_library(minipal_sanitizer_support OBJECT
    sansupport.c)