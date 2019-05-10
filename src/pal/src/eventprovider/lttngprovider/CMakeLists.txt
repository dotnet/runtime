cmake_minimum_required(VERSION 2.8.12)

include(FindPythonInterp)
set (GENERATE_SCRIPT ${CLR_DIR}/src/scripts/genLttngProvider.py)

set(GENERATE_COMMAND ${PYTHON_EXECUTABLE} ${GENERATE_SCRIPT} --man ${EVENT_MANIFEST} --intermediate ${CMAKE_CURRENT_BINARY_DIR})

execute_process(
  COMMAND ${GENERATE_COMMAND} --dry-run
  RESULT_VARIABLE GEN_LTTNG_RESULT
  OUTPUT_VARIABLE LTTNG_PROVIDER_OUTPUT
  ERROR_VARIABLE GEN_LTTNG_ERRORS
)

if (NOT GEN_LTTNG_RESULT EQUAL 0)
  message(FATAL_ERROR "Failed to generate Lttng provider: ${GEN_LTTNG_ERRORS}")
endif()

string(REPLACE "\n" ";" LTTNG_PROVIDER_OUTPUT ${LTTNG_PROVIDER_OUTPUT}) # turn the outputted list of files into a CMake list

set (LTTNG_PROVIDER_SOURCES "")
set (TRACEPOINT_PROVIDER_SOURCES "")
set (LTTNG_HEADERS "")
foreach(LTTNG_PROVIDER_FILE ${LTTNG_PROVIDER_OUTPUT})
  file(TO_CMAKE_PATH ${LTTNG_PROVIDER_FILE} LTTNG_PROVIDER_FILE)
  if ("${LTTNG_PROVIDER_FILE}" MATCHES "/lttng/traceptprov[a-z]+.cpp$")
    list(APPEND TRACEPOINT_PROVIDER_SOURCES ${LTTNG_PROVIDER_FILE})
  elseif("${LTTNG_PROVIDER_FILE}" MATCHES "/lttng/eventprov[a-z]+.cpp$")
    list(APPEND LTTNG_PROVIDER_SOURCES ${LTTNG_PROVIDER_FILE})
  else()
    list(APPEND LTTNG_HEADERS ${LTTNG_PROVIDER_FILE})
  endif()
endforeach()

add_definitions(-DPAL_STDCPP_COMPAT=1)
include_directories(${COREPAL_SOURCE_DIR}/inc/rt)
include_directories(${CMAKE_CURRENT_BINARY_DIR}/lttng)

add_custom_command(OUTPUT ${LTTNG_PROVIDER_SOURCES} ${TRACEPOINT_PROVIDER_SOURCES} ${LTTNG_HEADERS}
  COMMAND ${GENERATE_COMMAND}
  DEPENDS ${EVENT_MANIFEST} ${GENERATE_SCRIPT})

add_library(eventprovider
    STATIC
    ${LTTNG_PROVIDER_SOURCES}
    eventproviderhelpers.cpp
)

add_compile_options(-fPIC)

add_library(coreclrtraceptprovider
    SHARED
    ${TRACEPOINT_PROVIDER_SOURCES}
)

find_library(LTTNG NAMES lttng-ust)

target_link_libraries(coreclrtraceptprovider
                      ${LTTNG}
)

set_target_properties(coreclrtraceptprovider PROPERTIES LINKER_LANGUAGE CXX)

# Install the static eventprovider library
_install(TARGETS eventprovider DESTINATION lib)
# Install the static coreclrtraceptprovider library
install_clr(coreclrtraceptprovider)
