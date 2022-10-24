# Compiler configurations

set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)
set(CMAKE_C_EXTENSIONS OFF)

set(CMAKE_CXX_STANDARD 11)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

if(MSVC)
    add_compile_options(/Zc:wchar_t-) # wchar_t is a built-in type.
    add_compile_options(/W4 /WX) # warning level 4 and all warnings as errors.
else()
    add_compile_options(-Wall -Werror) # warning level 3 and all warnings as errors.
endif()