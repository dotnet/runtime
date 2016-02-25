cmake_minimum_required (VERSION 2.6)
project (MarshalBoolArrayNative)
include_directories(${INC_PLATFORM_DIR})
set(SOURCES MarshalBoolArrayNative.cpp)

# add the executable
add_library (MarshalBoolArrayNative SHARED ${SOURCES})

# add the install targets
install (TARGETS MarshalBoolArrayNative DESTINATION bin)


