set(CMAKE_INCLUDE_CURRENT_DIR ON)

if(CLR_CMAKE_HOST_UNIX AND NOT CLR_CROSS_COMPONENTS_BUILD)
    add_subdirectory(${CLR_REPO_ROOT_DIR}/src/libraries/Native/Unix/System.Globalization.Native System.Globalization.Native)
endif()
