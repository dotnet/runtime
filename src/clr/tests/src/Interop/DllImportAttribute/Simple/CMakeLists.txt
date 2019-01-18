cmake_minimum_required (VERSION 2.6)
project (DllFileProbe)
set(SOURCES DllFileProbe.cpp )
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 

# add the executable
add_library (DllFileProbe SHARED ${SOURCES})
target_link_libraries(DllFileProbe ${LINK_LIBRARIES_ADDITIONAL}) 

# add the install targets
install (TARGETS DllFileProbe DESTINATION bin)
