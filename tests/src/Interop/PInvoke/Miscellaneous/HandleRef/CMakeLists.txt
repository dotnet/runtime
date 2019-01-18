 
cmake_minimum_required (VERSION 2.6) 
project (HandleRefNative) 
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
set(SOURCES 
    HandleRefNative.cpp 
) 
# Additional files to reference: 
#    HandleRefNative.def 
# add the executable 
add_library (HandleRefNative SHARED ${SOURCES}) 
# add the install targets 
install (TARGETS HandleRefNative DESTINATION bin) 
