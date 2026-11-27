
project (Char_BestFitMappingNative)
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")
set(SOURCES
    BestFitMappingNative.cpp
)
add_library (Char_BestFitMappingNative ${TEST_LIB_TYPE} ${SOURCES})
target_link_libraries(Char_BestFitMappingNative PRIVATE ${LINK_LIBRARIES_ADDITIONAL})
# add the install targets
install (TARGETS Char_BestFitMappingNative DESTINATION bin)
