set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(GEN_EVENTPIPE_SOURCES ${CORECLR_EVENT_DATA_OUTPUTS})
list(FILTER GEN_EVENTPIPE_SOURCES INCLUDE REGEX "/eventpipe/[^/]+\\.cpp$")

list(APPEND CORECLR_EVENTPIPE_SHIM_SOURCES
  ep-rt-coreclr.cpp
  ep-rt-coreclr-wasm-sampling.cpp
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
target_compile_definitions(eventpipe_gen_objs PRIVATE FEATURE_PERFTRACING)

add_library_clr(eventpipe_objs OBJECT ${CORECLR_EVENTPIPE_SHIM_SOURCES} ${CORECLR_EVENTPIPE_SHIM_HEADERS})
set_target_properties(eventpipe_objs PROPERTIES
    LINKER_LANGUAGE CXX
    UNITY_BUILD ON
    UNITY_BUILD_BATCH_SIZE 0
)
add_dependencies(eventpipe_objs eventing_headers)
target_link_libraries(eventpipe_objs PRIVATE dn-diagnosticserver dn-eventpipe)
target_compile_definitions(eventpipe_objs PRIVATE FEATURE_PERFTRACING)
if (FEATURE_PERFTRACING_DISABLE_THREADS)
  target_compile_definitions(eventpipe_objs PRIVATE PERFTRACING_DISABLE_THREADS)
endif()

# The CoreCLR EventPipe runtime is C++, but the EventPipe and DiagnosticServer sources are C.
# Forcibly override them to build as C++.
get_property(diagnosticserver_sources TARGET dn-diagnosticserver PROPERTY INTERFACE_SOURCES)
get_property(eventpipe_sources TARGET dn-eventpipe PROPERTY INTERFACE_SOURCES)
set_source_files_properties(${diagnosticserver_sources} ${eventpipe_sources} PROPERTIES LANGUAGE CXX)

# Build EventPipe and DiagnosticServer as unity-builds.
set (SHARED_EVENTPIPE_SOURCE_PATH "${CLR_SRC_NATIVE_DIR}/eventpipe")

add_library(eventpipe INTERFACE)
target_sources(eventpipe INTERFACE $<TARGET_OBJECTS:eventpipe_gen_objs> $<TARGET_OBJECTS:eventpipe_objs> $<TARGET_OBJECTS:dn-containers>)
