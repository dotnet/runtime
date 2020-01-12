project (SeqPInvokeNative) 
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
set(SOURCES 
    SeqStructDelRevPInvokeNative.cpp 
) 
add_library (SeqPInvokeNative SHARED ${SOURCES}) 
target_link_libraries(SeqPInvokeNative ${LINK_LIBRARIES_ADDITIONAL}) 
# add the install targets 
install (TARGETS SeqPInvokeNative DESTINATION bin) 
