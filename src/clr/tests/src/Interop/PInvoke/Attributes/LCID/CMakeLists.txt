cmake_minimum_required (VERSION 2.6) 
project (LCIDNative) 
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
include_directories(${INC_PLATFORM_DIR}) 
set(SOURCES 
    LCIDNative.cpp 
) 
add_library (LCIDNative SHARED ${SOURCES}) 
target_link_libraries(LCIDNative ${LINK_LIBRARIES_ADDITIONAL}) 
# add the install targets 
install (TARGETS LCIDNative DESTINATION bin) 
