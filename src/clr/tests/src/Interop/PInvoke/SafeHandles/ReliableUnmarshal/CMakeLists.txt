cmake_minimum_required (VERSION 2.6) 
project (ReliableUnmarshal) 
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
set(SOURCES 
  ReliableUnmarshalNative.cpp 
)
add_library (ReliableUnmarshalNative SHARED ${SOURCES}) 
install (TARGETS ReliableUnmarshalNative DESTINATION bin) 
