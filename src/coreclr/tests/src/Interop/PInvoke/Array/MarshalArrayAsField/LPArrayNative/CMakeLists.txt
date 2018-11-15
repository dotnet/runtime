#VCXPROJ 
cmake_minimum_required (VERSION 2.6) 
project (MarshalArrayByValArrayNative) 
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
include_directories("../..")
include_directories(${INC_PLATFORM_DIR})
set(SOURCES 
    MarshalArrayByValArrayNative.cpp 
) 

# add the executable 
add_library (MarshalArrayByValArrayNative SHARED ${SOURCES}) 
target_link_libraries(MarshalArrayByValArrayNative ${LINK_LIBRARIES_ADDITIONAL}) 
# add the install targets 
install (TARGETS MarshalArrayByValArrayNative DESTINATION bin) 
