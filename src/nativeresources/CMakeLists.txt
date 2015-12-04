project(nativeresourcestring)

add_compile_options(-fPIC)

add_library(nativeresourcestring
  STATIC
  resourcestring.cpp
)

