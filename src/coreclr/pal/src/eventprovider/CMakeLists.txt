set(EVENT_MANIFEST ${VM_DIR}/ClrEtwAll.man)

if(CLR_CMAKE_HOST_LINUX AND NOT CLR_CMAKE_HOST_ANDROID)
  add_subdirectory(lttngprovider)
else()
  add_subdirectory(dummyprovider)
endif()
