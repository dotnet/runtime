if(CLR_CMAKE_HOST_WIN32)
  #use static crt
  add_definitions(-MT)
  add_definitions(-DHOST_WINDOWS)
endif(CLR_CMAKE_HOST_WIN32)

set(DEBUGSHIM_SOURCES
  debugshim.cpp
)

add_library_clr(debugshim STATIC ${DEBUGSHIM_SOURCES})
