cmake_minimum_required (VERSION 2.6)
project (BestFitMappingNative)
set(SOURCES BestFitMappingNative.cpp )
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 

# add the executable
add_library (BestFitMappingNative SHARED ${SOURCES})
target_link_libraries(BestFitMappingNative ${LINK_LIBRARIES_ADDITIONAL}) 

# add the install targets
install (TARGETS BestFitMappingNative DESTINATION bin)
