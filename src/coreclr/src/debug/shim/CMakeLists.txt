if(WIN32)
  #use static crt
  add_definitions(-MT)
  add_definitions(-DHOST_IS_WINDOWS_OS)
endif(WIN32)

set(DEBUGSHIM_SOURCES
  debugshim.cpp
)

add_library_clr(debugshim STATIC ${DEBUGSHIM_SOURCES})