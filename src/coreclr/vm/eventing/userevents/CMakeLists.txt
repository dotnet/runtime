include(FindPython)
set (GENERATE_SCRIPT ${CLR_DIR}/scripts/genUserEvents.py)

set(GENERATE_COMMAND ${Python_EXECUTABLE} ${GENERATE_SCRIPT} --man ${EVENT_MANIFEST} --intermediate ${CMAKE_CURRENT_BINARY_DIR})

execute_process(
  COMMAND ${GENERATE_COMMAND} --dry-run
  RESULT_VARIABLE GEN_USEREVENTS_RESULT
  OUTPUT_VARIABLE USEREVENTS_PROVIDER_OUTPUT
  ERROR_VARIABLE GEN_USEREVENTS_ERRORS
)

if (NOT GEN_USEREVENTS_RESULT EQUAL 0)
  message(FATAL_ERROR "Failed to generate user_events provider: ${GEN_USEREVENTS_ERRORS}")
endif()

add_definitions(-DPAL_STDCPP_COMPAT=1)
include_directories(${COREPAL_SOURCE_DIR}/inc/rt)
# include_directories(${CMAKE_CURRENT_BINARY_DIR}/lttng)

# TODO: keep in sync with providers in ClrEtwAll.man
set (USEREVENTS_PROVIDER_SOURCES
     userevents.cpp
     dotnetruntime.cpp
     dotnetruntimemonoprofiler.cpp
     dotnetruntimerundown.cpp
     dotnetruntimestress.cpp)

add_custom_command(OUTPUT ${USEREVENTS_PROVIDER_SOURCES}
  COMMAND ${GENERATE_COMMAND}
  DEPENDS ${EVENT_MANIFEST} ${GENERATE_SCRIPT})

set_source_files_properties(
  ${USEREVENTS_PROVIDER_SOURCES}
  PROPERTIES GENERATED TRUE)

add_custom_target(
  generated_userevents_headers
  DEPENDS ${USEREVENTS_HEADERS})

add_library(usereventsprovider
  STATIC
  ${USEREVENTS_PROVIDER_SOURCES}
)

add_dependencies(usereventsprovider generated_eventing_headers)

# Install the static usereventsprovider library
install_clr(TARGETS usereventsprovider DESTINATIONS lib COMPONENT runtime)
