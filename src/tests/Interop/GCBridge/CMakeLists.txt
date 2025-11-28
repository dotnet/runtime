project (GCBridgeNative)
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")
set(SOURCES GCBridgeNative.cpp )

# add the executable
add_library (GCBridgeNative SHARED ${SOURCES})
target_link_libraries(GCBridgeNative PRIVATE ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS GCBridgeNative DESTINATION bin)
