set(CMAKE_INCLUDE_CURRENT_DIR ON)
set(CORECLR_BUILD ON)

if(CLR_CMAKE_HOST_UNIX AND NOT DEFINED CLR_CROSS_COMPONENTS_BUILD)
    add_subdirectory(../../../libraries/Native/Unix/System.Globalization.Native System.Globalization.Native)
endif()
