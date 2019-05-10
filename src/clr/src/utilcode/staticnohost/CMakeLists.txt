add_definitions(-DSELF_NO_HOST)

if(WIN32)
     add_definitions(-D_CRTIMP=) # use static version of crt
     add_precompiled_header(stdafx.h ../stdafx.cpp UTILCODE_STATICNOHOST_SOURCES)
endif(WIN32)

add_library_clr(utilcodestaticnohost STATIC ${UTILCODE_STATICNOHOST_SOURCES})

if(CLR_CMAKE_PLATFORM_UNIX)
  target_link_libraries(utilcodestaticnohost  nativeresourcestring)
endif(CLR_CMAKE_PLATFORM_UNIX)

add_dependencies(utilcodestaticnohost ${UTILCODE_DEPENDENCIES})
