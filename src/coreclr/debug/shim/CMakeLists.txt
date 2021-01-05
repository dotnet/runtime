if(CLR_CMAKE_HOST_WIN32)
  #use static crt
  set(CMAKE_MSVC_RUNTIME_LIBRARY MultiThreaded)
  add_definitions(-DHOST_WINDOWS)
endif(CLR_CMAKE_HOST_WIN32)

set(DEBUGSHIM_SOURCES
  debugshim.cpp
)

add_library_clr(debugshim STATIC ${DEBUGSHIM_SOURCES})
