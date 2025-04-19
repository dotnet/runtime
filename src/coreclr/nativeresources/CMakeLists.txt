project(nativeresourcestring)

add_library_clr(nativeresourcestring
  STATIC
  resourcestring.cpp
)

install_clr (TARGETS nativeresourcestring DESTINATIONS lib)

if(CLR_CMAKE_HOST_ANDROID)
    install_clr(TARGETS nativeresourcestring DESTINATIONS . sharedFramework)
endif()
