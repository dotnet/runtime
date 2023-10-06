include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")
set(SOURCES MarshalStructArrayNative.cpp)

# add the executable
add_library (MarshalStructArrayNative SHARED ${SOURCES})
target_link_libraries(MarshalStructArrayNative PRIVATE ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS MarshalStructArrayNative  DESTINATION bin)
