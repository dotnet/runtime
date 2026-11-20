project (MutualRecurThdTLSNative)
include(${CLR_ENG_NATIVE_DIR}/ijw/IJW.cmake)

add_library(MutualRecurThdTLSNative ${TEST_LIB_TYPE} MutualRecurThd-TLS.cpp)
target_link_libraries(MutualRecurThdTLSNative PRIVATE ${LINK_LIBRARIES_ADDITIONAL})
add_ijw_msbuild_project_properties(MutualRecurThdTLSNative ijwhost)
install(TARGETS MutualRecurThdTLSNative DESTINATION bin)
