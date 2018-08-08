
cmake_minimum_required (VERSION 2.6) 
project (ReversePInvokeNative) 
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
include_directories(${INC_PLATFORM_DIR}) 
set(SOURCES 
    ExpStructAsParamNative.cpp 
) 
# add the executable 
add_library (ReversePInvokeNative SHARED ${SOURCES})
target_link_libraries(ReversePInvokeNative ${LINK_LIBRARIES_ADDITIONAL})  
# add the install targets 
install (TARGETS ReversePInvokeNative DESTINATION bin) 
