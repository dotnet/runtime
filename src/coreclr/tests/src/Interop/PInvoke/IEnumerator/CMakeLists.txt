cmake_minimum_required (VERSION 2.6)
project (IEnumeratorNative)
include_directories(${INC_PLATFORM_DIR})
set(SOURCES IEnumeratorNative.cpp )

# add the executable
add_library (IEnumeratorNative SHARED ${SOURCES})
target_link_libraries(IEnumeratorNative ${LINK_LIBRARIES_ADDITIONAL}) 

# add the install targets
install (TARGETS IEnumeratorNative DESTINATION bin)
