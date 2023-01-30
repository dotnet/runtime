# Compiler configurations

set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)
set(CMAKE_C_EXTENSIONS OFF)

set(CMAKE_CXX_STANDARD 14)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

if(MSVC)
  add_compile_options(/Zc:wchar_t-) # wchar_t is a built-in type.
  add_compile_options(/W4 /WX) # warning level 4 and warnings are errors.
  add_compile_options(/Zi) # enable debugging information.

  add_link_options(/DEBUG) # enable debugging information.
else()
  add_compile_options(-Wall -Werror) # All warnings and are errors.
  add_compile_options(-g) # enable debugging information.

  add_compile_options(-Wno-unknown-pragmas) # Ignore Win32 pragmas
  add_compile_options(-Wno-pragma-pack) # cor.h controls pack pragmas via headers.
endif()

# Define values for platform detection
if(WIN32)
  add_compile_definitions(BUILD_WINDOWS=1)
elseif(APPLE)
  add_compile_definitions(BUILD_MACOS=1)
else()
  add_compile_definitions(BUILD_UNIX=1)
endif()

add_compile_definitions(__STDC_WANT_LIB_EXT1__=1) # https://en.cppreference.com/w/c/error#Bounds_checking