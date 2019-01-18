 
cmake_minimum_required (VERSION 2.6) 
project (LPStr_BestFitMappingNative) 
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
set(SOURCES 
    BestFitMappingNative.cpp 
) 

add_library (LPStr_BestFitMappingNative SHARED ${SOURCES}) 
target_link_libraries(LPStr_BestFitMappingNative ${LINK_LIBRARIES_ADDITIONAL}) 
# add the install targets 
install (TARGETS LPStr_BestFitMappingNative DESTINATION bin) 
