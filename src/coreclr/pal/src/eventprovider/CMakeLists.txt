set(EVENT_MANIFEST ${VM_DIR}/ClrEtwAll.man)

# TODO: https://github.com/dotnet/runtime/issues/111491
if(CLR_CMAKE_HOST_LINUX AND NOT CLR_CMAKE_HOST_ANDROID)
  add_subdirectory(lttngprovider)
else()
  add_subdirectory(dummyprovider)
endif()
