cmake_minimum_required (VERSION 2.6)
project (ExactSpellingNative)
include_directories(${INC_PLATFORM_DIR})
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")  
set(SOURCES ExactSpellingNative.cpp)

# add the executable
add_library (ExactSpellingNative SHARED ${SOURCES})
target_link_libraries(ExactSpellingNative ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS ExactSpellingNative DESTINATION bin)


