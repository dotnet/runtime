set(EVENT_MANIFEST ${VM_DIR}/ClrEtwAll.man)

if(FEATURE_EVENTSOURCE_XPLAT)
  add_subdirectory(lttngprovider)
else()
  add_subdirectory(dummyprovider)
endif()
