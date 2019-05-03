cmake_minimum_required (VERSION 2.6) 
project (NativeDateTime) 
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
set(SOURCES 
    NativeDateTime.cpp 
)
# add the executable 
add_library (NativeDateTime SHARED ${SOURCES}) 
target_link_libraries(NativeDateTime ${LINK_LIBRARIES_ADDITIONAL}) 
# add the install targets 
install (TARGETS NativeDateTime DESTINATION bin) 
