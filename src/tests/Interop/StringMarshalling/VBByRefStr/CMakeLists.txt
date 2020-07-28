project (VBByRefStrNative)
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
set(SOURCES VBByRefStrNative.cpp )

# add the executable
add_library (VBByRefStrNative SHARED ${SOURCES})
target_link_libraries(VBByRefStrNative ${LINK_LIBRARIES_ADDITIONAL}) 

# add the install targets
install (TARGETS VBByRefStrNative DESTINATION bin)
