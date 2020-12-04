project (SizeConstNative)
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
set(SOURCES SizeConstNative.cpp)

# add the executable
add_library (SizeConstNative SHARED ${SOURCES})
target_link_libraries(SizeConstNative ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS SizeConstNative DESTINATION bin)


