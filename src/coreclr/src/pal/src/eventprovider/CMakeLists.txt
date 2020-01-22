set(EVENT_MANIFEST ${VM_DIR}/ClrEtwAll.man)

if(CLR_CMAKE_HOST_LINUX OR CLR_CMAKE_HOST_FREEBSD)
  add_subdirectory(lttngprovider)
else()
  add_subdirectory(dummyprovider)
endif()
