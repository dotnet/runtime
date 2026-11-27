project (MarshalBoolArrayNative)
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")
set(SOURCES MarshalBoolArrayNative.cpp)

# add the executable
add_library (MarshalBoolArrayNative ${TEST_LIB_TYPE} ${SOURCES})
target_link_libraries(MarshalBoolArrayNative PRIVATE ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS MarshalBoolArrayNative DESTINATION bin)


