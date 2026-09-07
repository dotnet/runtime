project (RuntimeHandlesNative) 
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
set(SOURCES RuntimeHandlesNative.cpp) 
add_library (RuntimeHandlesNative ${TEST_LIB_TYPE} ${SOURCES}) 
# add the install targets 
install (TARGETS RuntimeHandlesNative DESTINATION bin) 
