cmake_minimum_required (VERSION 2.6)
set(SOURCES LPStrTestNative.cpp )
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")

# add the executable
add_library (LPStrTestNative SHARED ${SOURCES})
target_link_libraries(LPStrTestNative ${LINK_LIBRARIES_ADDITIONAL}) 

# add the install targets
install (TARGETS LPStrTestNative DESTINATION bin)
