include(FindPythonInterp)

set (GENERATE_SCRIPT ${CLR_DIR}/src/scripts/genDummyProvider.py)

set(GENERATE_COMMAND ${PYTHON_EXECUTABLE} ${GENERATE_SCRIPT} --man ${EVENT_MANIFEST} --intermediate ${CMAKE_CURRENT_BINARY_DIR})

execute_process(
  COMMAND ${GENERATE_COMMAND} --dry-run
  RESULT_VARIABLE GEN_DUMMY_RESULT
  OUTPUT_VARIABLE DUMMY_PROVIDER_OUTPUT
  ERROR_VARIABLE GEN_DUMMY_ERRORS
)

if (NOT GEN_DUMMY_RESULT EQUAL 0)
  message(FATAL_ERROR "Failed to generate dummy event provider: ${GEN_DUMMY_ERRORS}")
endif()

string(REPLACE "\n" ";" DUMMY_PROVIDER_OUTPUT ${DUMMY_PROVIDER_OUTPUT}) # turn the outputted list of files into a CMake list

set (DUMMY_PROVIDER_SOURCES "")
set (TRACEPOINT_PROVIDER_SOURCES "")
foreach(DUMMY_PROVIDER_FILE ${DUMMY_PROVIDER_OUTPUT})
  file(TO_CMAKE_PATH ${DUMMY_PROVIDER_FILE} DUMMY_PROVIDER_FILE)
  list(APPEND DUMMY_PROVIDER_SOURCES ${DUMMY_PROVIDER_FILE})
endforeach()

add_definitions(-DPAL_STDCPP_COMPAT=1)
include_directories(${COREPAL_SOURCE_DIR}/inc/rt)
include_directories(${CMAKE_CURRENT_BINARY_DIR}/dummy)

add_custom_command(OUTPUT ${DUMMY_PROVIDER_SOURCES}
  COMMAND ${GENERATE_COMMAND}
  DEPENDS ${EVENT_MANIFEST} ${GENERATE_SCRIPT})

add_library(eventprovider
    STATIC
    ${DUMMY_PROVIDER_SOURCES}
)

set_target_properties(eventprovider PROPERTIES LINKER_LANGUAGE CXX)

# Install the static eventprovider library
install(TARGETS eventprovider DESTINATION lib)
