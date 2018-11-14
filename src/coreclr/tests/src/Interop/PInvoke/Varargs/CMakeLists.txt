cmake_minimum_required (VERSION 2.6)
project (VarargsNative)
include_directories(${INC_PLATFORM_DIR})
set(SOURCES VarargsNative.cpp )

# add the executable
add_library (VarargsNative SHARED ${SOURCES})
target_link_libraries(VarargsNative ${LINK_LIBRARIES_ADDITIONAL}) 

# add the install targets
install (TARGETS VarargsNative DESTINATION bin)
