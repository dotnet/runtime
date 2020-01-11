project (MarshalArrayByValNative)
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
set(SOURCES MarshalArrayByValNative.cpp)

# add the executable
add_library (MarshalArrayByValNative SHARED ${SOURCES})
target_link_libraries(MarshalArrayByValNative ${LINK_LIBRARIES_ADDITIONAL}) 

# add the install targets
install (TARGETS MarshalArrayByValNative DESTINATION bin)


