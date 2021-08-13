include_directories(inc)

if(CLR_CMAKE_HOST_WIN32)
  add_subdirectory(coreshim)
else(CLR_CMAKE_HOST_WIN32)
  add_definitions(-D_FILE_OFFSET_BITS=64)
endif(CLR_CMAKE_HOST_WIN32)

add_subdirectory(corerun)
