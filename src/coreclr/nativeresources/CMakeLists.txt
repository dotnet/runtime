project(nativeresourcestring)

add_library_clr(nativeresourcestring
  STATIC
  resourcestring.cpp
)

install_clr (TARGETS nativeresourcestring DESTINATIONS lib)
