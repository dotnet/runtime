cmake_minimum_required(VERSION 3.16)
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
set(CMAKE_OBJC_STANDARD 99)
set(CMAKE_OBJC_STANDARD_REQUIRED TRUE)

# Define OBJC_TESTS for all of the tests so any typedefs that conflict with Objective-C
# are not defined the same way for these tests.
add_compile_definitions(OBJC_TESTS)

enable_language(OBJCXX)

set(SOURCES
    autorelease.mm
)

set_source_files_properties(autorelease.m PROPERTIES COMPILE_FLAGS -fno-objc-arc)

include(CMakeFindFrameworks)
find_library(FOUNDATION Foundation REQUIRED)

add_library(ObjectiveC SHARED ${SOURCES})
target_link_libraries(ObjectiveC ${FOUNDATION})
