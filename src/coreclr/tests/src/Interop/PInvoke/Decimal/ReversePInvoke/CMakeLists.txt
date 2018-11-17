#VCXPROJ 
cmake_minimum_required (VERSION 2.6) 
project (RevNative) 
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
include_directories(${INC_PLATFORM_DIR}) 
set(SOURCES 
    RevNative.cpp 
) 
# add the executable 
add_library (RevNative SHARED ${SOURCES}) 
target_link_libraries(RevNative ${LINK_LIBRARIES_ADDITIONAL}) 
# add the install targets 
install (TARGETS RevNative DESTINATION bin) 
