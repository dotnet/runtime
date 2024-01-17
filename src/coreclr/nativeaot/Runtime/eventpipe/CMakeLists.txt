project(Runtime)

set(EP_GENERATED_HEADER_PATH "${GENERATED_INCLUDE_DIR}")
include (${CLR_SRC_NATIVE_DIR}/eventpipe/configure.cmake)
include_directories(${EP_GENERATED_HEADER_PATH})

set(CMAKE_INCLUDE_CURRENT_DIR ON)
set(AOT_EVENTPIPE_SHIM_DIR "${CMAKE_CURRENT_SOURCE_DIR}")

set (CONTAINER_SOURCES "")
set (CONTAINER_HEADERS "")
set (MINIPAL_SOURCES "")
set (EVENTPIPE_SOURCES "")
set (EVENTPIPE_HEADERS "")
set (GEN_EVENTPIPE_SOURCES "")

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
  if (CMAKE_VERSION VERSION_GREATER 3.11 OR CMAKE_VERSION VERSION_EQUAL 3.11)
    set_source_files_properties(${CONTAINER_SOURCES} PROPERTIES COMPILE_OPTIONS -xc++)
    set_source_files_properties(${EVENTPIPE_SOURCES} PROPERTIES COMPILE_OPTIONS -xc++)
  else(CMAKE_VERSION VERSION_GREATER 3.11 OR CMAKE_VERSION VERSION_EQUAL 3.11)
    add_compile_options(-xc++)
  endif()
endif(CLR_CMAKE_HOST_UNIX)

if (WIN32)
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

list(APPEND GEN_EVENTPIPE_SOURCES
  ${AOT_EVENTPIPE_SHIM_DIR}/clretwallmain.cpp
  ${AOT_EVENTPIPE_SHIM_DIR}/dotnetruntime.cpp
)

list(APPEND AOT_EVENTPIPE_MANAGED_TO_NATIVE_SOURCES
   ${RUNTIME_DIR}/eventpipeinternal.cpp
   ${RUNTIME_DIR}/eventpipeadapter.h
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
endif()

list(APPEND EVENTPIPE_SOURCES
  ${AOT_EVENTPIPE_SHIM_SOURCES}
  ${AOT_EVENTPIPE_SHIM_HEADERS}
  ${AOT_EVENTPIPE_MANAGED_TO_NATIVE_SOURCES}
  ${AOT_EVENTTRACE_SOURCES}
  ${SHARED_EVENTPIPE_CONFIG_HEADERS}
  ${GEN_EVENTPIPE_SOURCES}
  ${CONTAINER_SOURCES}
  ${CONTAINER_HEADERS}
  ${MINIPAL_SOURCES}
)

list(APPEND AOT_EVENTPIPE_DISABLED_SOURCES
  ${RUNTIME_DIR}/DisabledEventPipeInterface.cpp
  ${RUNTIME_DIR}/disabledeventpipeinternal.cpp
  ${RUNTIME_DIR}/disabledeventtrace.cpp
  ${RUNTIME_DIR}/disabledruntimeeventinternal.cpp
  ${AOT_EVENTPIPE_SHIM_DIR}/disabledclretwallmain.cpp
)

add_library(eventpipe-enabled STATIC ${EVENTPIPE_SOURCES})
add_library(eventpipe-disabled STATIC ${AOT_EVENTPIPE_DISABLED_SOURCES})

if (CLR_CMAKE_TARGET_WIN32)
  add_library(eventpipe-enabled.GuardCF STATIC ${EVENTPIPE_SOURCES})
  add_library(eventpipe-disabled.GuardCF STATIC ${AOT_EVENTPIPE_DISABLED_SOURCES})
  target_compile_options(eventpipe-enabled.GuardCF PRIVATE $<$<OR:$<COMPILE_LANGUAGE:C>,$<COMPILE_LANGUAGE:CXX>>:/guard:cf>)
  target_compile_options(eventpipe-disabled.GuardCF PRIVATE $<$<OR:$<COMPILE_LANGUAGE:C>,$<COMPILE_LANGUAGE:CXX>>:/guard:cf>)
endif (CLR_CMAKE_TARGET_WIN32)

install_static_library(eventpipe-enabled aotsdk nativeaot)
install_static_library(eventpipe-disabled aotsdk nativeaot)
if (CLR_CMAKE_TARGET_WIN32)
  install_static_library(eventpipe-enabled.GuardCF aotsdk nativeaot)
  install_static_library(eventpipe-disabled.GuardCF aotsdk nativeaot)
endif (CLR_CMAKE_TARGET_WIN32)
