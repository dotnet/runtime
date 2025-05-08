project(nativeresourcestring)

add_library_clr(nativeresourcestring_objects
  OBJECT
  resourcestring.cpp
)

add_library_clr(nativeresourcestring
  STATIC
  $<TARGET_OBJECTS:nativeresourcestring_objects>
)

install_clr (TARGETS nativeresourcestring DESTINATIONS lib)
