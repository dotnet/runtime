
project (ReversePInvokeNative) 
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
set(SOURCES 
    ExpStructAsParamNative.cpp 
) 
# add the executable 
add_library (ReversePInvokeNative SHARED ${SOURCES})
target_link_libraries(ReversePInvokeNative ${LINK_LIBRARIES_ADDITIONAL})  
# add the install targets 
install (TARGETS ReversePInvokeNative DESTINATION bin) 
