include_directories(${INC_PLATFORM_DIR})

add_library(PgoSchemaNative ${TEST_LIB_TYPE} PgoSchemaNative.cpp)
target_include_directories(PgoSchemaNative PRIVATE ${CLR_REPO_ROOT_DIR}/src/coreclr/inc)
target_compile_definitions(PgoSchemaNative PRIVATE FEATURE_PGO)

install(TARGETS PgoSchemaNative DESTINATION bin)
