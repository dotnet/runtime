 
cmake_minimum_required (VERSION 2.6) 
project (PInvoke_SafeHandle) 
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
set(SOURCES 
    SafeHandleNative.cpp 
) 
# Additional files to reference: 
#    StructDefs.h 
#    SafeHandleNative.def 
# add the executable 
add_library (PInvoke_SafeHandle SHARED ${SOURCES}) 
# add the install targets 
install (TARGETS PInvoke_SafeHandle DESTINATION bin) 
