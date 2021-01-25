project (NativeLibrary)
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")
set(SOURCES NativeLibrary.cpp)

# add the shared library
add_library (NativeLibrary SHARED ${SOURCES})
target_link_libraries(NativeLibrary ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS NativeLibrary DESTINATION bin)
