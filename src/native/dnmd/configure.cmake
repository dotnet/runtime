# Compiler configurations

set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)
set(CMAKE_C_EXTENSIONS OFF)

set(CMAKE_CXX_STANDARD 14)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

#
# Configure CMake for platforms
#
if(WIN32)
  add_compile_definitions(BUILD_WINDOWS=1)
elseif(APPLE)
  add_compile_definitions(BUILD_MACOS=1)

  set(CMAKE_BUILD_WITH_INSTALL_NAME_DIR ON)
  set(CMAKE_INSTALL_NAME_DIR "@rpath")
  set(CMAKE_BUILD_WITH_INSTALL_RPATH TRUE)
  set(CMAKE_INSTALL_RPATH "@loader_path")
else()
  set(CMAKE_INSTALL_RPATH "$ORIGIN")
  add_compile_definitions(BUILD_UNIX=1)
endif()

#
# Agnostic compiler/platform settings
#
add_compile_definitions(__STDC_WANT_LIB_EXT1__=1) # https://en.cppreference.com/w/c/error#Bounds_checking

option(DNMD_ENABLE_PROFILING OFF)

if (DNMD_ENABLE_PROFILING AND MSVC)
    add_link_options(/PROFILE)
endif()