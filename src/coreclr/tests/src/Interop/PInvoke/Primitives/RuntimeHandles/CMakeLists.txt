cmake_minimum_required (VERSION 2.6) 
project (RuntimeHandlesNative) 
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
set(SOURCES RuntimeHandlesNative.cpp) 
add_library (RuntimeHandlesNative SHARED ${SOURCES}) 
# add the install targets 
install (TARGETS RuntimeHandlesNative DESTINATION bin) 
