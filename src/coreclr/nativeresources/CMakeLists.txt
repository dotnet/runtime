project(nativeresourcestring)

add_library_clr(nativeresourcestring
  STATIC
  resourcestring.cpp
)

install (TARGETS nativeresourcestring DESTINATION lib)
