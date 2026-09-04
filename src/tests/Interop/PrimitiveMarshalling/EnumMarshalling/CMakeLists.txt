project (MarshalEnumNative)
set(SOURCES MarshalEnumNative.cpp )
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")

# add the executable
add_library (MarshalEnumNative ${TEST_LIB_TYPE} ${SOURCES})
target_link_libraries(MarshalEnumNative PRIVATE ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS MarshalEnumNative DESTINATION bin)
