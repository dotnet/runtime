cmake_minimum_required (VERSION 2.6)

if(NOT (CLR_CMAKE_TARGET_ARCH STREQUAL arm64))

project (ForeignThreadExceptionsNative)

include_directories(${INC_PLATFORM_DIR})

set(SOURCES ForeignThreadExceptionsNative.cpp)

# add the executable
add_library (ForeignThreadExceptionsNative SHARED ${SOURCES})

# add the install targets
install (TARGETS ForeignThreadExceptionsNative DESTINATION bin)

endif()