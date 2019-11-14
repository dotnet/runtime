 
cmake_minimum_required (VERSION 2.6) 
project (ComVisibleNative) 
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
set(SOURCES 
    ComVisibleNative.cpp 
) 
# Additional files to reference: 
# add the executable 
add_library (ComVisibleNative SHARED ${SOURCES}) 
# add the install targets 
install (TARGETS ComVisibleNative DESTINATION bin) 
