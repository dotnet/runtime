#VCXPROJ 
cmake_minimum_required (VERSION 2.6) 
project (MarshalArrayLPArrayNative) 
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")
include_directories("../..")
include_directories(${INC_PLATFORM_DIR}) 
set(SOURCES 
    MarshalArrayLPArrayNative.cpp 
) 

# add the executable 
add_library (MarshalArrayLPArrayNative SHARED ${SOURCES})
target_link_libraries(MarshalArrayLPArrayNative ${LINK_LIBRARIES_ADDITIONAL})  
# add the install targets 
install (TARGETS MarshalArrayLPArrayNative DESTINATION bin) 
