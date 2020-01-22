 
project (MarshalArrayLPArrayNative) 
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")
include_directories("../..")
set(SOURCES 
    MarshalArrayLPArrayNative.cpp 
) 

# add the executable 
add_library (MarshalArrayLPArrayNative SHARED ${SOURCES})
target_link_libraries(MarshalArrayLPArrayNative ${LINK_LIBRARIES_ADDITIONAL})  
# add the install targets 
install (TARGETS MarshalArrayLPArrayNative DESTINATION bin) 
