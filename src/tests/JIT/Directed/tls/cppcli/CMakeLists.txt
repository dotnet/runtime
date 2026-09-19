project (TestTLSNative)
include(${CLR_ENG_NATIVE_DIR}/ijw/IJW.cmake)

add_library(TestTLSNative ${TEST_LIB_TYPE} TestTLSNative.cpp)
target_link_libraries(TestTLSNative PRIVATE ${LINK_LIBRARIES_ADDITIONAL})
add_ijw_msbuild_project_properties(TestTLSNative ijwhost)
install(TARGETS TestTLSNative DESTINATION bin)
