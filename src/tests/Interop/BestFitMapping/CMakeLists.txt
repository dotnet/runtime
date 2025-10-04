project (BestFitMappingNative)
set(SOURCES BestFitMappingNative.cpp )
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")

# add the executable
add_library (BestFitMappingNative ${TEST_LIB_TYPE} ${SOURCES})
target_link_libraries(BestFitMappingNative PRIVATE ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS BestFitMappingNative DESTINATION bin)
