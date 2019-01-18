cmake_minimum_required (VERSION 2.6)
project (LPTSTRTestNative)
set(SOURCES LPTSTRTestNative.cpp )
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")

# add the executable
add_library (LPTSTRTestNative SHARED ${SOURCES})
target_link_libraries(LPTSTRTestNative ${LINK_LIBRARIES_ADDITIONAL}) 

# add the install targets
install (TARGETS LPTSTRTestNative DESTINATION bin)
