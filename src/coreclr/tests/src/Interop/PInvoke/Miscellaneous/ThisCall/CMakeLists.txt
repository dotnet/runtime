 
cmake_minimum_required (VERSION 2.6) 
project (ThisCallNative) 
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
set(SOURCES 
   ThisCallNative.cpp 
) 
add_library (ThisCallNative SHARED ${SOURCES})
install (TARGETS ThisCallNative DESTINATION bin) 
