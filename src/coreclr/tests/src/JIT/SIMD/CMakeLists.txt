cmake_minimum_required (VERSION 2.6)
project (Vector3TestNative)
include_directories(${INC_PLATFORM_DIR})
set(SOURCES Vector3TestNative.cpp )

# add the executable
add_library (Vector3TestNative SHARED ${SOURCES})

# add the install targets
install (TARGETS Vector3TestNative DESTINATION bin)
