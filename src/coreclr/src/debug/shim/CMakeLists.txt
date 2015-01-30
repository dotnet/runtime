add_definitions(-DHOST_IS_WINDOWS_OS)

if(WIN32)
  #use static crt
  add_definitions(-MT) 
endif(WIN32)

set(DEBUGSHIM_SOURCES
  debugshim.cpp
)


add_library(debugshim STATIC ${DEBUGSHIM_SOURCES})