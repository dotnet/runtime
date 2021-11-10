include_directories(inc)

if(CLR_CMAKE_HOST_WIN32)
  add_subdirectory(coreshim)
endif(CLR_CMAKE_HOST_WIN32)

add_subdirectory(corerun)
