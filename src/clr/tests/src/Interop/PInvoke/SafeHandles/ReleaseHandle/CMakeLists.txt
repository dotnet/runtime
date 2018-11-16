#VCXPROJ 
cmake_minimum_required (VERSION 2.6) 
project (PInvoke_SafeHandle_ReleaseHandle) 
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
include_directories(${INC_PLATFORM_DIR}) 
set(SOURCES 
    ReleaseHandleNative.cpp 
) 
# Additional files to reference: 
#    ReleaseHandleNative.def 
# add the executable 
add_library (PInvoke_SafeHandle_ReleaseHandle SHARED ${SOURCES}) 
# add the install targets 
install (TARGETS PInvoke_SafeHandle_ReleaseHandle DESTINATION bin) 
