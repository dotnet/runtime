cmake_minimum_required (VERSION 2.6)
project (SimpleStructNative)
include_directories(${INC_PLATFORM_DIR})
set(SOURCES SimpleStructNative.cpp )

# add the executable
add_library (SimpleStructNative SHARED ${SOURCES})

# add the install targets
install (TARGETS SimpleStructNative DESTINATION bin)