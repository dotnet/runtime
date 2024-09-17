include(configure.cmake)

# Provide an object library for scenarios where we ship static libraries
add_library(minipal_objects OBJECT
cpufeatures.c
random.c
time.c
unicodedata.c
utf8.c)

add_library(minipal_objects.GuardCF OBJECT
    cpufeatures.c
    random.c
    time.c
    unicodedata.c
    utf8.c)

target_include_directories(minipal_objects PRIVATE ${CLR_SRC_NATIVE_DIR} ${CMAKE_CURRENT_BINARY_DIR})
target_include_directories(minipal_objects.GuardCF PRIVATE ${CLR_SRC_NATIVE_DIR} ${CMAKE_CURRENT_BINARY_DIR})
set_target_properties(minipal_objects.GuardCF PROPERTIES CLR_CONTROL_FLOW_GUARD ON)

# Provide a static library for our shared library and executable scenarios
# for easier usability.
add_library(minipal STATIC)
target_link_libraries(minipal PRIVATE minipal_objects.GuardCF)
set_target_properties(minipal PROPERTIES CLR_CONTROL_FLOW_GUARD ON)

add_library(minipal_sanitizer_support OBJECT
    sansupport.c)
set_target_properties(minipal_sanitizer_support PROPERTIES CLR_CONTROL_FLOW_GUARD ON)
