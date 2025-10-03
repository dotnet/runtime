 
project (ComVisibleNative) 
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
set(SOURCES 
    ComVisibleNative.cpp 
) 
# Additional files to reference: 
# add the executable 
add_library (ComVisibleNative ${TEST_LIB_TYPE} ${SOURCES}) 
# add the install targets 
install (TARGETS ComVisibleNative DESTINATION bin) 
