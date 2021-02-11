set(SOURCES LPTStrTestNative.cpp )
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")

# add the executable
add_library (LPTStrTestNative SHARED ${SOURCES})
target_link_libraries(LPTStrTestNative ${LINK_LIBRARIES_ADDITIONAL}) 

# add the install targets
install (TARGETS LPTStrTestNative DESTINATION bin)
