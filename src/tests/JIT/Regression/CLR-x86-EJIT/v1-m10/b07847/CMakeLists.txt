if (CLR_CMAKE_HOST_WIN32)
    project (B07847Native)
    include(${CLR_ENG_NATIVE_DIR}/ijw/IJW.cmake)

    add_library(B07847Native ${TEST_LIB_TYPE} b07847.cpp)
    target_link_libraries(B07847Native PRIVATE ${LINK_LIBRARIES_ADDITIONAL})
    add_ijw_msbuild_project_properties(B07847Native ijwhost)
    install(TARGETS B07847Native DESTINATION bin)
endif()
