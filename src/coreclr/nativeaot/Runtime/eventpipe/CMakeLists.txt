project(Runtime)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(GENERATED_INCLUDE_DIR ${CMAKE_CURRENT_BINARY_DIR}/inc)

list(APPEND AOT_EVENTPIPE_DISABLED_SOURCES
  ${NATIVEAOT_RUNTIME_DIR}/DisabledEventPipeInterface.cpp
  ${NATIVEAOT_RUNTIME_DIR}/disabledeventpipeinternal.cpp
  ${NATIVEAOT_RUNTIME_DIR}/disabledeventtrace.cpp
  ${NATIVEAOT_RUNTIME_DIR}/disabledruntimeeventinternal.cpp
)

if(NOT CLR_DOTNET_HOST_PATH)
  # The eventing sources cannot be generated, so both libraries are the disabled implementation.
  set(EP_GENERATED_HEADER_PATH "${GENERATED_INCLUDE_DIR}")
  include(${CLR_SRC_NATIVE_DIR}/eventpipe/configure.cmake)
  include_directories(${EP_GENERATED_HEADER_PATH})

  add_custom_target(aot_eventing_headers)
  foreach(library eventpipe-enabled eventpipe-disabled)
    add_library(${library} STATIC ${AOT_EVENTPIPE_DISABLED_SOURCES})
    target_compile_definitions(${library} PRIVATE FEATURE_PERFTRACING FEATURE_EVENT_TRACE)
    install_static_library(${library} aotsdk nativeaot)
  endforeach()
  return()
endif()

add_event_data_target(aot_eventing_headers
  FLAVOR nativeaot
  OUTPUT_DIR ${GENERATED_INCLUDE_DIR}
  INCLUSION_LIST ${CMAKE_CURRENT_SOURCE_DIR}/gen-eventing-event-inc.lst
  OUTPUTS AOT_EVENT_DATA_OUTPUTS)

set(GEN_EVENTPIPE_PLAT_AGNOSTIC_SOURCES ${GENERATED_INCLUDE_DIR}/clretwallmain.cpp)
set(GEN_EVENTPIPE_PLAT_AGNOSTIC_DISABLED_SOURCES ${GENERATED_INCLUDE_DIR}/disabledclretwallmain.cpp)
set(GEN_EVENTPIPE_PROVIDER_SOURCES ${AOT_EVENT_DATA_OUTPUTS})
list(FILTER GEN_EVENTPIPE_PROVIDER_SOURCES INCLUDE REGEX "/eventpipe/[^/]+\\.cpp$")

set(EP_GENERATED_HEADER_PATH "${GENERATED_INCLUDE_DIR}")
include (${CLR_SRC_NATIVE_DIR}/eventpipe/configure.cmake)
include_directories(${EP_GENERATED_HEADER_PATH})

if (CLR_CMAKE_TARGET_WIN32)
  set(EP_ETW_GENERATED_HEADER_PATH "${GENERATED_INCLUDE_DIR}/etw")
  include_directories(${EP_ETW_GENERATED_HEADER_PATH})
endif()

set(AOT_EVENTPIPE_SHIM_DIR "${CMAKE_CURRENT_SOURCE_DIR}")

set (EVENTPIPE_SOURCES "")

list(APPEND AOT_EVENTPIPE_SHIM_SOURCES
  ${AOT_EVENTPIPE_SHIM_DIR}/ep-rt-aot.cpp
  ${AOT_EVENTPIPE_SHIM_DIR}/ds-rt-aot.cpp
)

list(APPEND AOT_EVENTPIPE_SHIM_HEADERS
  ${AOT_EVENTPIPE_SHIM_DIR}/ds-rt-aot.h
  ${AOT_EVENTPIPE_SHIM_DIR}/ds-rt-types-aot.h
  ${AOT_EVENTPIPE_SHIM_DIR}/ep-rt-aot.h
  ${AOT_EVENTPIPE_SHIM_DIR}/ep-rt-config-aot.h
  ${AOT_EVENTPIPE_SHIM_DIR}/ep-rt-types-aot.h
)


list(APPEND AOT_EVENTPIPE_MANAGED_TO_NATIVE_SOURCES
   ${NATIVEAOT_RUNTIME_DIR}/eventpipeinternal.cpp
   ${NATIVEAOT_RUNTIME_DIR}/EnabledEventPipeInterface.cpp
   ${NATIVEAOT_RUNTIME_DIR}/runtimeeventinternal.cpp
)

if (FEATURE_EVENT_TRACE)
  list(APPEND AOT_EVENTTRACE_SOURCES
    ${NATIVEAOT_RUNTIME_DIR}/eventtrace.cpp
    ${NATIVEAOT_RUNTIME_DIR}/eventtrace_bulktype.cpp
    ${NATIVEAOT_RUNTIME_DIR}/eventtrace_gcheap.cpp
    ${NATIVEAOT_RUNTIME_DIR}/profheapwalkhelper.cpp
  )

  if(CLR_CMAKE_TARGET_WIN32)
    set_source_files_properties(${GEN_EVENTPIPE_PROVIDER_SOURCES} PROPERTIES COMPILE_FLAGS "/FI\"${NATIVEAOT_RUNTIME_DIR}/eventpipe/NativeaotEventPipeSupport.h\"")
    set_source_files_properties(${GEN_EVENTPIPE_PLAT_AGNOSTIC_SOURCES} PROPERTIES COMPILE_FLAGS "/FI\"${NATIVEAOT_RUNTIME_DIR}/eventpipe/NativeaotEventPipeSupport.h\"")
    set_source_files_properties(${AOT_EVENTTRACE_SOURCES} PROPERTIES COMPILE_FLAGS "/FI\"${NATIVEAOT_RUNTIME_DIR}/eventpipe/NativeaotEventPipeSupport.h\"")
  endif()
endif()

# The NativeAOT EventPipe runtime is C++, but the EventPipe and DiagnosticServer sources are C.
# Forcibly override them to build as C++.
get_property(diagnosticserver_sources TARGET dn-diagnosticserver PROPERTY INTERFACE_SOURCES)
get_property(diagnosticserver_pal_sources TARGET dn-diagnosticserver-pal PROPERTY INTERFACE_SOURCES)
get_property(eventpipe_sources TARGET dn-eventpipe PROPERTY INTERFACE_SOURCES)
set_source_files_properties(${diagnosticserver_sources} ${diagnosticserver_pal_sources} ${eventpipe_sources} PROPERTIES LANGUAGE CXX)

add_library(eventpipe-shared-objects OBJECT)
target_link_libraries(eventpipe-shared-objects PRIVATE dn-diagnosticserver dn-eventpipe dn-diagnosticserver-pal)
# Build EventPipe and DiagnosticServer as unity-builds for better inlining.
set_target_properties(eventpipe-shared-objects PROPERTIES
  UNITY_BUILD ON
  UNITY_BUILD_BATCH_SIZE 0
  COMPILE_PDB_NAME "eventpipe-shared-objects"
  COMPILE_PDB_OUTPUT_DIRECTORY "${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>"
)
if (CLR_CMAKE_TARGET_WIN32)
    target_compile_options(eventpipe-shared-objects PRIVATE
        "/FI${NATIVEAOT_RUNTIME_DIR}/eventpipe/NativeaotEventPipeSupport.h")

    # Install the compile PDB for the eventpipe unity builds.
    install(FILES "${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/eventpipe-shared-objects.pdb" DESTINATION aotsdk COMPONENT nativeaot)
    # For the container library, we need to produce a compile PDB and install it
    set_target_properties(dn-containers-no-lto PROPERTIES
        COMPILE_PDB_NAME "dn-containers"
        COMPILE_PDB_OUTPUT_DIRECTORY "${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>")
    install(FILES "${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/dn-containers.pdb" DESTINATION aotsdk COMPONENT nativeaot)
endif()

list(APPEND EVENTPIPE_SOURCES
  ${AOT_EVENTPIPE_SHIM_SOURCES}
  ${AOT_EVENTPIPE_MANAGED_TO_NATIVE_SOURCES}
  ${AOT_EVENTTRACE_SOURCES}
  ${GEN_EVENTPIPE_PROVIDER_SOURCES}
  ${GEN_EVENTPIPE_PLAT_AGNOSTIC_SOURCES}
)

list(APPEND AOT_EVENTPIPE_DISABLED_SOURCES ${GEN_EVENTPIPE_PLAT_AGNOSTIC_DISABLED_SOURCES})

add_library(eventpipe-enabled STATIC ${EVENTPIPE_SOURCES})

target_link_libraries(eventpipe-enabled PRIVATE eventpipe-shared-objects dn-containers-no-lto)
add_dependencies(eventpipe-enabled aot_eventing_headers)

add_library(eventpipe-disabled STATIC ${AOT_EVENTPIPE_DISABLED_SOURCES})
add_dependencies(eventpipe-disabled aot_eventing_headers)

install_static_library(eventpipe-enabled aotsdk nativeaot)
install_static_library(eventpipe-disabled aotsdk nativeaot)
