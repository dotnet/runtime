set(EVENT_MANIFEST ${VM_DIR}/ClrEtwAll.man)

# TODO: LTTNG is not supported on Android. We need a solution.
if(CLR_CMAKE_HOST_LINUX AND NOT CLR_CMAKE_TARGET_ANDROID)
  add_subdirectory(lttngprovider)
else()
  add_subdirectory(dummyprovider)
endif()
