cmake_minimum_required (VERSION 2.6)
project (MarshalBoolArrayNative)
include_directories(${INC_PLATFORM_DIR})
set(SOURCES MarshalBoolArrayNative.cpp)

# add the executable
add_library (MarshalBoolArrayNative SHARED ${SOURCES})
target_link_libraries(MarshalBoolArrayNative ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS MarshalBoolArrayNative DESTINATION bin)


