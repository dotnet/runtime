project(nativeresourcestring)

if (CLR_CMAKE_HOST_ANDROID)
  add_library_clr(nativeresourcestring_objects
    OBJECT
    resourcestring.cpp
  )
endif(CLR_CMAKE_HOST_ANDROID)

add_library_clr(nativeresourcestring
  STATIC
  resourcestring.cpp
)

install_clr (TARGETS nativeresourcestring DESTINATIONS lib)
