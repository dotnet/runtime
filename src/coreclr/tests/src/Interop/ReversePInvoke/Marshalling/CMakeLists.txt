cmake_minimum_required (VERSION 2.6)
project (MarshalBoolArrayDLL)
include_directories(${INC_PLATFORM_DIR})
set(SOURCES MarshalBoolArrayDLL.cpp MarshalBoolArrayDLL.def)

# add the executable
add_library (MarshalBoolArrayDLL SHARED ${SOURCES})

# add the install targets
install (TARGETS MarshalBoolArrayDLL DESTINATION bin)


