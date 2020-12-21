project (UTF8TestNative)
set(SOURCES UTF8TestNative.cpp )
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")

# add the executable
add_library (UTF8TestNative SHARED ${SOURCES})
target_link_libraries(UTF8TestNative ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS UTF8TestNative DESTINATION bin)
