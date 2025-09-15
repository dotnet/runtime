include_directories(inc)

if(CLR_CMAKE_HOST_WIN32)
  add_subdirectory(coreshim)
endif(CLR_CMAKE_HOST_WIN32)

if (CLR_CMAKE_TARGET_ARCH_WASM)
  add_subdirectory(corewasmrun)
endif()

add_subdirectory(corerun)