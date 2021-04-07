# Compile options
add_definitions(-DLP64COMPATIBLE)
add_definitions(-DCORECLR)
add_definitions(-DPIC)

# C++ emits errors and warnings for c-string literal fed into char* parameter
# this is just to take care of the warnings
if (CMAKE_CXX_COMPILER_ID MATCHES "Clang")
  add_compile_options(-Wno-writable-strings)
elseif (CMAKE_CXX_COMPILER_ID STREQUAL "GNU")
  add_compile_options(-Wno-write-strings)
endif()

add_compile_options(-Wno-empty-body)

add_subdirectory(palsuite)


