set(CMAKE_INCLUDE_CURRENT_DIR ON)
set(GLOBALIZATION_NATIVE_DIR ${CLR_REPO_ROOT_DIR}/src/libraries/Native/AnyOS/System.Globalization.Native)

# Suppress exporting of the PAL APIs
add_definitions(-DPALEXPORT=)

include_directories("${GLOBALIZATION_NATIVE_DIR}")
include_directories("${CLR_REPO_ROOT_DIR}/src/libraries/Native/AnyOS/Common")

add_subdirectory(${GLOBALIZATION_NATIVE_DIR} System.Globalization.Native)

if(CLR_CMAKE_HOST_UNIX)
    include_directories("${CLR_REPO_ROOT_DIR}/src/libraries/Native/Unix/Common")
endif()

add_library(libraries-native
    STATIC
    entrypoints.c
)
