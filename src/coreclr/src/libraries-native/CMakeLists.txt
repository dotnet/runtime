set(CMAKE_INCLUDE_CURRENT_DIR ON)
set(GLOBALIZATION_NATIVE_DIR ${CLR_REPO_ROOT_DIR}/src/libraries/Native/Unix/System.Globalization.Native)

# Suppress exporting of the PAL APIs
add_definitions(-DPALEXPORT=)

if(CLR_CMAKE_HOST_UNIX)
  include_directories("${CLR_REPO_ROOT_DIR}/src/libraries/Native/Unix/Common")
  include_directories("${GLOBALIZATION_NATIVE_DIR}")

  add_subdirectory(${GLOBALIZATION_NATIVE_DIR} System.Globalization.Native)
endif()

add_library(libraries-native
    STATIC
    entrypoints.c
)
