cmake_minimum_required (VERSION 2.6)
project (DllFile.Probe)
set(SOURCES DllFile.Probe.cpp )
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 

# add the executable
add_library (DllFile.Probe SHARED ${SOURCES})
target_link_libraries(DllFile.Probe ${LINK_LIBRARIES_ADDITIONAL}) 

# add the install targets
install (TARGETS DllFile.Probe DESTINATION bin)
