cmake_minimum_required (VERSION 2.6)
project (IUnknownNative)
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
set(SOURCES IUnknownNative.cpp)

# add the executable
add_library (IUnknownNative SHARED ${SOURCES})
target_link_libraries(IUnknownNative ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS IUnknownNative DESTINATION bin)


