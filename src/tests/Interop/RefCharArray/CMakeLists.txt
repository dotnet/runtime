project (RefCharArrayNative)
set(SOURCES RefCharArrayNative.cpp )
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")

# add the executable
add_library (RefCharArrayNative ${TEST_LIB_TYPE} ${SOURCES})
target_link_libraries(RefCharArrayNative PRIVATE ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS RefCharArrayNative DESTINATION bin)
