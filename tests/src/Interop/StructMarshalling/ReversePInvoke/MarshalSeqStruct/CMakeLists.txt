cmake_minimum_required (VERSION 2.6) 
project (SeqPInvokeNative) 
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
include_directories(${INC_PLATFORM_DIR}) 
set(SOURCES 
    SeqStructDelRevPInvokeNative.cpp 
) 
add_library (SeqPInvokeNative SHARED ${SOURCES}) 
target_link_libraries(SeqPInvokeNative ${LINK_LIBRARIES_ADDITIONAL}) 
# add the install targets 
install (TARGETS SeqPInvokeNative DESTINATION bin) 
