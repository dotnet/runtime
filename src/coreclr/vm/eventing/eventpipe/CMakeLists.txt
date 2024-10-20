include(FindPython)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(GENERATE_SCRIPT ${CLR_DIR}/scripts/genEventPipe.py)
set(GENERATE_COMMAND ${Python_EXECUTABLE} ${GENERATE_SCRIPT} --man ${EVENT_MANIFEST} --exc ${EVENT_EXCLUSIONS} --intermediate ${CMAKE_CURRENT_BINARY_DIR} ${NONEXTERN_ARG})

execute_process(
  COMMAND ${GENERATE_COMMAND} --dry-run
  RESULT_VARIABLE GEN_EVENTPIPE_RESULT
  OUTPUT_VARIABLE GEN_EVENTPIPE_SOURCE_PATHS
  ERROR_VARIABLE GEN_EVENTPIPE_ERRORS
)

if (NOT GEN_EVENTPIPE_RESULT EQUAL 0)
  message(FATAL_ERROR "Failed to generate EventPipe: ${GEN_EVENTPIPE_ERRORS}")
endif()

string(REPLACE "\n" ";" GEN_EVENTPIPE_SOURCE_PATHS ${GEN_EVENTPIPE_SOURCE_PATHS}) # turn the outputted list of files into a CMake list

set (GEN_EVENTPIPE_SOURCES "")
foreach(GEN_EVENTPIPE_SOURCE_PATH ${GEN_EVENTPIPE_SOURCE_PATHS})
  file(TO_CMAKE_PATH ${GEN_EVENTPIPE_SOURCE_PATH} GEN_EVENTPIPE_SOURCE)
  list(APPEND GEN_EVENTPIPE_SOURCES ${GEN_EVENTPIPE_SOURCE})
endforeach()

add_custom_command(OUTPUT ${GEN_EVENTPIPE_SOURCES}
  COMMAND ${GENERATE_COMMAND}
  DEPENDS ${GENERATE_SCRIPT} ${EVENT_MANIFEST} ${EVENT_EXCLUSIONS})

list(APPEND CORECLR_EVENTPIPE_SHIM_SOURCES
  ep-rt-coreclr.cpp
)

list(APPEND CORECLR_EVENTPIPE_SHIM_HEADERS
  ds-rt-coreclr.h
  ds-rt-types-coreclr.h
  ep-rt-coreclr.h
  ep-rt-config-coreclr.h
  ep-rt-types-coreclr.h
)

add_library_clr(eventpipe_gen_objs OBJECT ${GEN_EVENTPIPE_SOURCES})
target_precompile_headers(eventpipe_gen_objs PRIVATE [["common.h"]])
set_target_properties(eventpipe_gen_objs PROPERTIES LINKER_LANGUAGE CXX)
add_dependencies(eventpipe_gen_objs eventing_headers)

add_library_clr(eventpipe_objs OBJECT ${CORECLR_EVENTPIPE_SHIM_SOURCES} ${CORECLR_EVENTPIPE_SHIM_HEADERS})
set_target_properties(eventpipe_objs PROPERTIES
    LINKER_LANGUAGE CXX
    UNITY_BUILD ON
    UNITY_BUILD_BATCH_SIZE 0
)
add_dependencies(eventpipe_objs eventing_headers)
target_link_libraries(eventpipe_objs PRIVATE dn-diagnosticserver dn-eventpipe)

# The CoreCLR EventPipe runtime is C++, but the EventPipe and DiagnosticServer sources are C.
# Forcibly override them to build as C++.
get_property(diagnosticserver_sources TARGET dn-diagnosticserver PROPERTY INTERFACE_SOURCES)
get_property(eventpipe_sources TARGET dn-eventpipe PROPERTY INTERFACE_SOURCES)
set_source_files_properties(${diagnosticserver_sources} ${eventpipe_sources} PROPERTIES LANGUAGE CXX)

# Build EventPipe and DiagnosticServer as unity-builds.
set (SHARED_EVENTPIPE_SOURCE_PATH "${CLR_SRC_NATIVE_DIR}/eventpipe")

add_library(eventpipe INTERFACE)
target_sources(eventpipe INTERFACE $<TARGET_OBJECTS:eventpipe_gen_objs> $<TARGET_OBJECTS:eventpipe_objs> $<TARGET_OBJECTS:dn-containers>)
