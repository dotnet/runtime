project(nativeresourcestring)

add_compile_options(-fPIC)

add_library_clr(nativeresourcestring
  STATIC
  resourcestring.cpp
)

