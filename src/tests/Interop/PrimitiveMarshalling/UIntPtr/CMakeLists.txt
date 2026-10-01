project (UIntPtrNative)
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")
set(SOURCES UIntPtrNative.cpp )

# add the executable
add_library (UIntPtrNative ${TEST_LIB_TYPE} ${SOURCES})
target_link_libraries(UIntPtrNative PRIVATE ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS UIntPtrNative DESTINATION bin)
