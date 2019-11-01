cmake_minimum_required (VERSION 2.6) 
project (ReversePInvokePassingByRefNative) 
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
include_directories("..") 
set(SOURCES 
    ReversePInvokePassingByRefNative.cpp 
) 
# Additional files to reference: 
# add the executable 
add_library (ReversePInvokePassingByRefNative SHARED ${SOURCES}) 
target_link_libraries(ReversePInvokePassingByRefNative ${LINK_LIBRARIES_ADDITIONAL}) 
# add the install targets 
install (TARGETS ReversePInvokePassingByRefNative DESTINATION bin) 
