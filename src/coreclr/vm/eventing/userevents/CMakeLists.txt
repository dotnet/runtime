include(FindPython)
set (GENERATE_SCRIPT ${CLR_DIR}/scripts/genUserEvents.py)

set(GENERATE_COMMAND ${Python_EXECUTABLE} ${GENERATE_SCRIPT} --man ${EVENT_MANIFEST} --exc ${EVENT_EXCLUSIONS} --intermediate ${CMAKE_CURRENT_BINARY_DIR} ${NONEXTERN_ARG})

execute_process(
  COMMAND ${GENERATE_COMMAND} --dry-run
  RESULT_VARIABLE GEN_USEREVENTS_RESULT
  OUTPUT_VARIABLE USEREVENTS_PROVIDER_OUTPUT
  ERROR_VARIABLE GEN_USEREVENTS_ERRORS
)

if (NOT GEN_USEREVENTS_RESULT EQUAL 0)
  message(FATAL_ERROR "Failed to generate user_events provider: ${GEN_USEREVENTS_ERRORS}")
endif()

include(${CLR_SRC_NATIVE_DIR}/external/LinuxTracepoints.cmake)

include_directories(${COREPAL_SOURCE_DIR}/inc/rt)

set (USEREVENTS_HELPER_SOURCES
     user_events.cpp)

# TODO: keep in sync with providers in ClrEtwAll.man
set (USEREVENTS_PROVIDER_SOURCES
     userevents/dotnetruntime.cpp
     userevents/dotnetruntimeprivate.cpp
     userevents/dotnetruntimerundown.cpp
     userevents/dotnetruntimestress.cpp)

set (CORECLR_USEREVENTS_SHIM_SOURCE_PATH "${CORECLR_USEREVENTS_SHIM_DIR}")

addprefix(USEREVENTS_PROVIDER_SOURCES ${CMAKE_CURRENT_BINARY_DIR} "${USEREVENTS_PROVIDER_SOURCES}")

add_custom_command(OUTPUT ${USEREVENTS_PROVIDER_SOURCES}
  COMMAND ${GENERATE_COMMAND}
  DEPENDS ${EVENT_MANIFEST} ${GENERATE_SCRIPT})

set_source_files_properties(
  ${USEREVENTS_PROVIDER_SOURCES}
  PROPERTIES GENERATED TRUE)

add_custom_target(
  generated_userevents_headers
  DEPENDS ${USEREVENTS_HEADERS})

add_library_clr(usereventsprovider
  OBJECT
  ${USEREVENTS_HELPER_SOURCES}
  ${USEREVENTS_PROVIDER_SOURCES}
  ${LinuxTracepoints_sources}
)

set_target_properties(usereventsprovider PROPERTIES LINKER_LANGUAGE CXX)
add_dependencies(usereventsprovider eventing_headers)
