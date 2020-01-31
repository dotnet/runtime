set(EXCLUDE_VERSION_HEADER_FILE ON)
set(CMAKE_INCLUDE_CURRENT_DIR ON)

if(CLR_CMAKE_HOST_UNIX AND NOT DEFINED CLR_CROSS_COMPONENTS_BUILD)
    set(BUILD_GLOBALIZATION_SHARED_LIBRARY OFF)
    include_directories("../../../libraries/Native/Unix/Common")

    add_subdirectory(../../../libraries/Native/Unix/System.Globalization.Native System.Globalization.Native)
endif()
