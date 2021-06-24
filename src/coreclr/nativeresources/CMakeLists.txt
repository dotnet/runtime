project(nativeresourcestring)

add_library_clr(nativeresourcestring
  STATIC
  resourcestring.cpp
)

install_clr (TARGETS nativeresourcestring DESTINATIONS lib)

set(CLR_CMAKE_ENABLE_ASAN OFF)

add_library_clr(nativeresourcestring_nosanitizer
  STATIC
  resourcestring.cpp
)

install_clr (TARGETS nativeresourcestring_nosanitizer DESTINATIONS lib)
