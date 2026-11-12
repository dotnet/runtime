add_event_data_target(eventing_headers
  FLAVOR coreclr
  OUTPUT_DIR ${GENERATED_INCLUDE_DIR}
  OUTPUTS CORECLR_EVENT_DATA_OUTPUTS)

add_subdirectory(eventpipe)

if(FEATURE_EVENTSOURCE_XPLAT)
  add_subdirectory(lttng)
endif()
