if (CLR_CMAKE_HOST_WIN32)
    project (B03689Native)
    include(${CLR_ENG_NATIVE_DIR}/ijw/IJW.cmake)

    add_library(B03689Native ${TEST_LIB_TYPE} b03689.cpp)
    target_link_libraries(B03689Native PRIVATE ${LINK_LIBRARIES_ADDITIONAL})
    add_ijw_msbuild_project_properties(B03689Native ijwhost)
    install(TARGETS B03689Native DESTINATION bin)
endif()
