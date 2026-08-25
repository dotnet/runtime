project (SafeArrayNative)
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")
set(SOURCES SafeArrayNative.cpp RecordNative.cpp)

# add the executable
add_library (SafeArrayNative ${TEST_LIB_TYPE} ${SOURCES})
target_link_libraries(SafeArrayNative PRIVATE ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS SafeArrayNative DESTINATION bin)
