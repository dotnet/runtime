cmake_minimum_required (VERSION 2.6)
project (MarshalBoolArrayNative)
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
set(SOURCES MarshalBoolArrayNative.cpp)

# add the executable
add_library (MarshalBoolArrayNative SHARED ${SOURCES})
target_link_libraries(MarshalBoolArrayNative ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS MarshalBoolArrayNative DESTINATION bin)


