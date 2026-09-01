project (PInvokeIntNative) 
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
set(SOURCES PInvokeIntNative.cpp) 
add_library (PInvokeIntNative ${TEST_LIB_TYPE} ${SOURCES}) 
# add the install targets 
install (TARGETS PInvokeIntNative DESTINATION bin) 
