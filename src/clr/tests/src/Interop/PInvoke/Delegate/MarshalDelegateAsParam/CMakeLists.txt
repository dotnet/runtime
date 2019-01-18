 
cmake_minimum_required (VERSION 2.6) 
project (PInvoke_Delegate_AsParam) 
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
set(SOURCES 
    AsParamNative.cpp 
) 

# add the executable 
add_library (PInvoke_Delegate_AsParam SHARED ${SOURCES})
target_link_libraries(PInvoke_Delegate_AsParam ${LINK_LIBRARIES_ADDITIONAL}) 
# add the install targets 
install (TARGETS PInvoke_Delegate_AsParam DESTINATION bin) 
