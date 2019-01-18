cmake_minimum_required (VERSION 2.6)
project (NativeLibrary)
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
set(SOURCES NativeLibrary.cpp)

# add the executable
add_library (NativeLibrary SHARED ${SOURCES})
target_link_libraries(NativeLibrary ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS NativeLibrary DESTINATION bin)


