if(WIN32)
  #use static crt
  add_definitions(-MT) 
  add_definitions(-DHOST_IS_WINDOWS_OS)
endif(WIN32)

if(CLR_CMAKE_PLATFORM_UNIX)
  add_compile_options(-fPIC)
endif(CLR_CMAKE_PLATFORM_UNIX)

set(DEBUGSHIM_SOURCES
  debugshim.cpp
)

add_library_clr(debugshim STATIC ${DEBUGSHIM_SOURCES})