project(Runtime)

include(FindPython)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(EVENT_MANIFEST ${VM_DIR}/ClrEtwAll.man)
set(EVENT_EXCLUSIONS ${VM_DIR}/ClrEtwAllMeta.lst)
set(EVENT_INCLUSION_FILE ${CMAKE_CURRENT_SOURCE_DIR}/gen-eventing-event-inc.lst)
set(GENERATED_INCLUDE_DIR ${CMAKE_CURRENT_BINARY_DIR}/inc)
set(NONEXTERN_ARG "--nonextern")
set(NOXPLATHEADER_ARG "--noxplatheader")

# Platform agnostic source generation, clretwallmain.h/cpp, disabledclretwallmain.cpp, clreventpipewriteevents.h, etmdummy.h
set (GEN_EVENTPIPE_PLAT_AGNOSTIC_SOURCES ${GENERATED_INCLUDE_DIR}/clretwallmain.cpp)

set (GEN_EVENTPIPE_PLAT_AGNOSTIC_DISABLED_SOURCES ${GENERATED_INCLUDE_DIR}/disabledclretwallmain.cpp)

set(GENERATE_PLAT_AGNOSTIC_SCRIPT ${CLR_DIR}/scripts/genEventing.py)

set (EventingHeaders
  ${GENERATED_INCLUDE_DIR}/etmdummy.h
  ${GENERATED_INCLUDE_DIR}/clretwallmain.h
  ${GENERATED_INCLUDE_DIR}/clreventpipewriteevents.h
  ${GENERATED_INCLUDE_DIR}/clrproviders.h
  ${GENERATED_INCLUDE_DIR}/disabledclretwallmain.cpp
  ${GENERATED_INCLUDE_DIR}/clretwallmain.cpp
)

add_custom_command(
  OUTPUT ${CMAKE_CURRENT_BINARY_DIR}/${CMAKE_CFG_INTDIR}/aot_eventing_headers.timestamp
  COMMAND ${Python_EXECUTABLE} ${GENERATE_PLAT_AGNOSTIC_SCRIPT} --man ${EVENT_MANIFEST} --incdir ${GENERATED_INCLUDE_DIR} --inc ${EVENT_INCLUSION_FILE} --dummy ${GENERATED_INCLUDE_DIR}/etmdummy.h  --runtimeflavor nativeaot ${NONEXTERN_ARG} ${NOXPLATHEADER_ARG}
  COMMAND ${CMAKE_COMMAND} -E touch ${CMAKE_CURRENT_BINARY_DIR}/${CMAKE_CFG_INTDIR}/aot_eventing_headers.timestamp
  DEPENDS ${EVENT_MANIFEST} ${GENERATE_PLAT_AGNOSTIC_SCRIPT}
  VERBATIM
)

set_source_files_properties(${EventingHeaders} PROPERTIES GENERATED TRUE)

add_custom_target(aot_eventing_headers DEPENDS ${CMAKE_CURRENT_BINARY_DIR}/${CMAKE_CFG_INTDIR}/aot_eventing_headers.timestamp)

if(CLR_CMAKE_TARGET_WIN32)

  set(ETW_PROVIDER_SCRIPT ${CLR_DIR}/scripts/genEtwProvider.py)

  set (ETW_PROVIDER_OUTPUTS
    ${GENERATED_INCLUDE_DIR}/etw/ClrEtwAll.h
    ${GENERATED_INCLUDE_DIR}/etw/ClrEtwAll.rc
    ${GENERATED_INCLUDE_DIR}/etw/etwmacros.h
    ${GENERATED_INCLUDE_DIR}/etw/ClrEtwAll_MSG00001.bin
    ${GENERATED_INCLUDE_DIR}/etw/ClrEtwAllTEMP.bin
    ${GENERATED_INCLUDE_DIR}/clrxplatevents.h
  )

  set_source_files_properties(${ETW_PROVIDER_OUTPUTS} PROPERTIES GENERATED TRUE)

  add_custom_command(
    OUTPUT ${CMAKE_CURRENT_BINARY_DIR}/${CMAKE_CFG_INTDIR}/eventprovider.timestamp
    COMMAND ${Python_EXECUTABLE} ${ETW_PROVIDER_SCRIPT} --man ${EVENT_MANIFEST} --exc ${EVENT_EXCLUSIONS} --intermediate ${GENERATED_INCLUDE_DIR}
    COMMAND ${CMAKE_COMMAND} -E touch ${CMAKE_CURRENT_BINARY_DIR}/${CMAKE_CFG_INTDIR}/eventprovider.timestamp
    DEPENDS ${EVENT_MANIFEST} ${EVENT_EXCLUSIONS} ${ETW_PROVIDER_SCRIPT}
  )

  add_custom_target(aot_etw_headers DEPENDS ${CMAKE_CURRENT_BINARY_DIR}/${CMAKE_CFG_INTDIR}/eventprovider.timestamp)

endif()

# EventPipe provider source, currently dotnetruntime provider and helper file
set(GENERATE_EVENTPIPE_SCRIPT ${CLR_DIR}/scripts/genEventPipe.py)
set(GENERATE_COMMAND ${Python_EXECUTABLE} ${GENERATE_EVENTPIPE_SCRIPT} --man ${EVENT_MANIFEST} --exc ${EVENT_EXCLUSIONS} --inc ${EVENT_INCLUSION_FILE} --intermediate ${CMAKE_CURRENT_BINARY_DIR} --runtimeflavor nativeaot ${NONEXTERN_ARG})

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

set (GEN_EVENTPIPE_PROVIDER_SOURCES "")
foreach(GEN_EVENTPIPE_SOURCE_PATH ${GEN_EVENTPIPE_SOURCE_PATHS})
  file(TO_CMAKE_PATH ${GEN_EVENTPIPE_SOURCE_PATH} GEN_EVENTPIPE_SOURCE)
  list(APPEND GEN_EVENTPIPE_PROVIDER_SOURCES ${GEN_EVENTPIPE_SOURCE})
endforeach()

# Generate the provider source files via python auto-gen
add_custom_command(OUTPUT ${GEN_EVENTPIPE_PROVIDER_SOURCES}
  COMMAND ${GENERATE_COMMAND}
  DEPENDS ${GENERATE_EVENTPIPE_SCRIPT} ${EVENT_MANIFEST} ${EVENT_EXCLUSIONS})

set_source_files_properties(${GEN_EVENTPIPE_PROVIDER_SOURCES} PROPERTIES GENERATED TRUE)

set(EP_GENERATED_HEADER_PATH "${GENERATED_INCLUDE_DIR}")
include (${CLR_SRC_NATIVE_DIR}/eventpipe/configure.cmake)
include_directories(${EP_GENERATED_HEADER_PATH})

if (CLR_CMAKE_TARGET_WIN32)
  set(EP_ETW_GENERATED_HEADER_PATH "${GENERATED_INCLUDE_DIR}/etw")
  include_directories(${EP_ETW_GENERATED_HEADER_PATH})
endif()

set(AOT_EVENTPIPE_SHIM_DIR "${CMAKE_CURRENT_SOURCE_DIR}")

set (CONTAINER_SOURCES "")
set (CONTAINER_HEADERS "")
set (MINIPAL_SOURCES "")
set (EVENTPIPE_SOURCES "")
set (EVENTPIPE_HEADERS "")

# Use TCP for EventPipe on mobile platforms
if (CLR_CMAKE_HOST_IOS OR CLR_CMAKE_HOST_TVOS OR CLR_CMAKE_HOST_ANDROID)
  set(FEATURE_PERFTRACING_PAL_TCP 1)
  set(FEATURE_PERFTRACING_DISABLE_DEFAULT_LISTEN_PORT 1)
endif()

set (SHARED_CONTAINERS_SOURCE_PATH "${CLR_SRC_NATIVE_DIR}/containers")
set (SHARED_EVENTPIPE_SOURCE_PATH "${CLR_SRC_NATIVE_DIR}/eventpipe")
set (SHARED_MINIPAL_SOURCE_PATH "${CLR_SRC_NATIVE_DIR}/minipal")
include (${SHARED_EVENTPIPE_SOURCE_PATH}/eventpipe.cmake)
include (${SHARED_CONTAINERS_SOURCE_PATH}/containers.cmake)

list(APPEND MINIPAL_SOURCES
  utf8.c
)

if(CLR_CMAKE_HOST_WIN32)
  list(APPEND SHARED_DIAGNOSTIC_SERVER_SOURCES
    ds-ipc-pal-namedpipe.c
  )

  list(APPEND SHARED_DIAGNOSTIC_SERVER_HEADERS
    ds-ipc-pal-namedpipe.h
  )
endif(CLR_CMAKE_HOST_WIN32)

if(CLR_CMAKE_HOST_UNIX)
  list(APPEND SHARED_DIAGNOSTIC_SERVER_SOURCES
    ds-ipc-pal-socket.c
  )

  list(APPEND SHARED_DIAGNOSTIC_SERVER_HEADERS
    ds-ipc-pal-socket.h
  )

  include(${CLR_SRC_NATIVE_DIR}/minipal/configure.cmake)
  list(APPEND MINIPAL_SOURCES
    random.c
  )

endif(CLR_CMAKE_HOST_UNIX)

list(APPEND EVENTPIPE_SOURCES
  ${SHARED_EVENTPIPE_SOURCES}
  ${SHARED_DIAGNOSTIC_SERVER_SOURCES}
)

list(APPEND EVENTPIPE_HEADERS
  ${SHARED_EVENTPIPE_HEADERS}
  ${SHARED_DIAGNOSTIC_SERVER_HEADERS}
)

addprefix(CONTAINER_SOURCES ${SHARED_CONTAINERS_SOURCE_PATH} "${SHARED_CONTAINER_SOURCES}")
addprefix(CONTAINER_HEADERS ${SHARED_CONTAINERS_SOURCE_PATH} "${SHARED_CONTAINER_HEADERS}")
addprefix(MINIPAL_SOURCES ${SHARED_MINIPAL_SOURCE_PATH} "${MINIPAL_SOURCES}")

addprefix(EVENTPIPE_SOURCES ${SHARED_EVENTPIPE_SOURCE_PATH} "${EVENTPIPE_SOURCES}")
addprefix(EVENTPIPE_HEADERS ${SHARED_EVENTPIPE_SOURCE_PATH} "${EVENTPIPE_HEADERS}")

set_source_files_properties(${SHARED_EVENTPIPE_SOURCE_PATH}/ep-sources.c PROPERTIES COMPILE_DEFINITIONS EP_FORCE_INCLUDE_SOURCE_FILES)
set_source_files_properties(${SHARED_EVENTPIPE_SOURCE_PATH}/ds-sources.c PROPERTIES COMPILE_DEFINITIONS DS_FORCE_INCLUDE_SOURCE_FILES)

set_source_files_properties(${CONTAINER_SOURCES} PROPERTIES LANGUAGE CXX)
set_source_files_properties(${EVENTPIPE_SOURCES} PROPERTIES LANGUAGE CXX)

if(CLR_CMAKE_HOST_UNIX)
    set_source_files_properties(${CONTAINER_SOURCES} PROPERTIES COMPILE_OPTIONS -xc++)
    set_source_files_properties(${EVENTPIPE_SOURCES} PROPERTIES COMPILE_OPTIONS -xc++)
endif(CLR_CMAKE_HOST_UNIX)

if (CLR_CMAKE_TARGET_WIN32)
    set_source_files_properties(${EVENTPIPE_SOURCES} PROPERTIES COMPILE_FLAGS "/FI\"${RUNTIME_DIR}/eventpipe/NativeaotEventPipeSupport.h\"")
endif()

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
   ${RUNTIME_DIR}/eventpipeinternal.cpp
   ${RUNTIME_DIR}/EnabledEventPipeInterface.cpp
   ${RUNTIME_DIR}/runtimeeventinternal.cpp
)

if (FEATURE_EVENT_TRACE)
  list(APPEND AOT_EVENTTRACE_SOURCES
    ${RUNTIME_DIR}/eventtrace.cpp
    ${RUNTIME_DIR}/profheapwalkhelper.cpp
  )

  # These are carry-overs from .NET Native and only included for ETW currently
  #   bulktype : directly emits via ETW with EventWrite
  #   gcheap : GCHeapDump, GCHeapSurvivalAndMovement - not prioritizing for nativeaot yet
  if (FEATURE_ETW)
    list(APPEND AOT_EVENTTRACE_SOURCES
      ${RUNTIME_DIR}/eventtrace_bulktype.cpp
      ${RUNTIME_DIR}/eventtrace_gcheap.cpp
    )
  endif()

  if(CLR_CMAKE_TARGET_WIN32)
    set_source_files_properties(${GEN_EVENTPIPE_PROVIDER_SOURCES} PROPERTIES COMPILE_FLAGS "/FI\"${RUNTIME_DIR}/eventpipe/NativeaotEventPipeSupport.h\"")
    set_source_files_properties(${GEN_EVENTPIPE_PLAT_AGNOSTIC_SOURCES} PROPERTIES COMPILE_FLAGS "/FI\"${RUNTIME_DIR}/eventpipe/NativeaotEventPipeSupport.h\"")
    set_source_files_properties(${AOT_EVENTTRACE_SOURCES} PROPERTIES COMPILE_FLAGS "/FI\"${RUNTIME_DIR}/eventpipe/NativeaotEventPipeSupport.h\"")
  endif()
endif()



list(APPEND EVENTPIPE_SOURCES
  ${AOT_EVENTPIPE_SHIM_SOURCES}
  ${AOT_EVENTPIPE_SHIM_HEADERS}
  ${AOT_EVENTPIPE_MANAGED_TO_NATIVE_SOURCES}
  ${AOT_EVENTTRACE_SOURCES}
  ${SHARED_EVENTPIPE_CONFIG_HEADERS}
  ${GEN_EVENTPIPE_PROVIDER_SOURCES}
  ${GEN_EVENTPIPE_PLAT_AGNOSTIC_SOURCES}
  ${CONTAINER_SOURCES}
  ${CONTAINER_HEADERS}
  ${MINIPAL_SOURCES}
)

list(APPEND AOT_EVENTPIPE_DISABLED_SOURCES
  ${RUNTIME_DIR}/DisabledEventPipeInterface.cpp
  ${RUNTIME_DIR}/disabledeventpipeinternal.cpp
  ${RUNTIME_DIR}/disabledeventtrace.cpp
  ${RUNTIME_DIR}/disabledruntimeeventinternal.cpp
  ${GEN_EVENTPIPE_PLAT_AGNOSTIC_DISABLED_SOURCES}
)

add_library(eventpipe-enabled STATIC ${EVENTPIPE_SOURCES})
add_dependencies(eventpipe-enabled aot_eventing_headers)
if(CLR_CMAKE_TARGET_WIN32)
  add_dependencies(eventpipe-enabled aot_etw_headers)
endif()

add_library(eventpipe-disabled STATIC ${AOT_EVENTPIPE_DISABLED_SOURCES})
add_dependencies(eventpipe-disabled aot_eventing_headers)

if (CLR_CMAKE_TARGET_WIN32)
  add_library(eventpipe-enabled.GuardCF STATIC ${EVENTPIPE_SOURCES})
  add_dependencies(eventpipe-enabled.GuardCF aot_eventing_headers)
  add_dependencies(eventpipe-enabled.GuardCF aot_etw_headers)
  add_library(eventpipe-disabled.GuardCF STATIC ${AOT_EVENTPIPE_DISABLED_SOURCES})
  add_dependencies(eventpipe-disabled.GuardCF aot_eventing_headers)
  set_target_properties(eventpipe-enabled.GuardCF PROPERTIES CLR_CONTROL_FLOW_GUARD ON)
  set_target_properties(eventpipe-disabled.GuardCF PROPERTIES CLR_CONTROL_FLOW_GUARD ON)
endif (CLR_CMAKE_TARGET_WIN32)

install_static_library(eventpipe-enabled aotsdk nativeaot)
install_static_library(eventpipe-disabled aotsdk nativeaot)
if (CLR_CMAKE_TARGET_WIN32)
  install_static_library(eventpipe-enabled.GuardCF aotsdk nativeaot)
  install_static_library(eventpipe-disabled.GuardCF aotsdk nativeaot)
endif (CLR_CMAKE_TARGET_WIN32)