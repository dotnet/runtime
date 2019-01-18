 
cmake_minimum_required (VERSION 2.6) 
project (DecNative) 
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
set(SOURCES 
    DecNative.cpp 
) 
# Additional files to reference: 
# add the executable 
add_library (DecNative SHARED ${SOURCES}) 
target_link_libraries(DecNative ${LINK_LIBRARIES_ADDITIONAL}) 
# add the install targets 
install (TARGETS DecNative DESTINATION bin) 
