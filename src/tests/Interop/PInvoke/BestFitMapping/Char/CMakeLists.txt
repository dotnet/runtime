 
project (Char_BestFitMappingNative) 
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
set(SOURCES 
    BestFitMappingNative.cpp 
) 
add_library (Char_BestFitMappingNative SHARED ${SOURCES}) 
target_link_libraries(Char_BestFitMappingNative ${LINK_LIBRARIES_ADDITIONAL}) 
# add the install targets 
install (TARGETS Char_BestFitMappingNative DESTINATION bin) 
