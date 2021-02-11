project (MarshalStructAsParam)
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
set(SOURCES MarshalStructAsParamDLL.cpp)

# add the executable
add_library (MarshalStructAsParam SHARED ${SOURCES})
target_link_libraries(MarshalStructAsParam ${LINK_LIBRARIES_ADDITIONAL}) 

# add the install targets
install (TARGETS MarshalStructAsParam DESTINATION bin)