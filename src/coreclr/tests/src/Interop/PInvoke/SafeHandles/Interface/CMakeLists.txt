#VCXPROJ 
cmake_minimum_required (VERSION 2.6) 
project (PInvoke_SafeHandle_MarshalAs_Interface) 
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")
include_directories("..")
include_directories(${INC_PLATFORM_DIR}) 
set(SOURCES 
    InterfaceNative.cpp 
) 
# Additional files to reference: 
#    InterfaceNative.def 
# add the executable 
add_library (PInvoke_SafeHandle_MarshalAs_Interface SHARED ${SOURCES}) 
# add the install targets 
install (TARGETS PInvoke_SafeHandle_MarshalAs_Interface DESTINATION bin) 
