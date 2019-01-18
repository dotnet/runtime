 
cmake_minimum_required (VERSION 2.6) 
project (PInvoke_Delegate_AsField) 
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
set(SOURCES 
    AsFieldNative.cpp 
) 
# add the executable 
add_library (PInvoke_Delegate_AsField SHARED ${SOURCES}) 
target_link_libraries(PInvoke_Delegate_AsField ${LINK_LIBRARIES_ADDITIONAL}) 
# add the install targets 
install (TARGETS PInvoke_Delegate_AsField DESTINATION bin) 
