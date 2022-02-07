set(EVENT_MANIFEST ${VM_DIR}/ClrEtwAll.man)

if(CLR_CMAKE_HOST_LINUX)
  add_subdirectory(lttngprovider)
else()
  add_subdirectory(dummyprovider)
endif()
