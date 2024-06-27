include(CheckSymbolExists)
include(CheckIncludeFile)

check_include_file(
    sys/socket.h
    HAVE_SYS_SOCKET_H
)

check_symbol_exists(
    accept4
    sys/socket.h
    HAVE_ACCEPT4)

# Use TCP for EventPipe on mobile platforms
if (CLR_CMAKE_HOST_IOS OR CLR_CMAKE_HOST_TVOS OR CLR_CMAKE_HOST_ANDROID)
  set(FEATURE_PERFTRACING_PAL_TCP 1)
  set(FEATURE_PERFTRACING_DISABLE_DEFAULT_LISTEN_PORT 1)
endif()

configure_file(${CLR_SRC_NATIVE_DIR}/eventpipe/ep-shared-config.h.in ${CMAKE_CURRENT_BINARY_DIR}/ep-shared-config.h)

# Define the DiagnosticsServer and EventPipe as interface libraries.
# We must define them as interface libraries as each runtime builds the same set of files slightly differently.
# Defining it as an interface library allows us to specify common sources, include directories, dependencies, etc.
# in one place, but also allow each runtime to add any settings that are specific to that runtime.
# This includes, but is not limited to each runtime's implementation of the ds-rt.h and ep-rt.h contracts.

add_library(dn-diagnosticserver INTERFACE)

target_sources(dn-diagnosticserver INTERFACE
    ds-dump-protocol.c
    ds-eventpipe-protocol.c
    ds-ipc.c
    ds-portable-rid.c
    ds-process-protocol.c
    ds-profiler-protocol.c
    ds-protocol.c
    ds-server.c)

target_include_directories(dn-diagnosticserver INTERFACE ${CMAKE_CURRENT_BINARY_DIR})
target_link_libraries(dn-diagnosticserver INTERFACE dn-containers)

add_library(dn-diagnosticserver-pal INTERFACE)
target_include_directories(dn-diagnosticserver-pal INTERFACE ${CMAKE_CURRENT_BINARY_DIR})
target_link_libraries(dn-diagnosticserver-pal INTERFACE dn-containers)

if (FEATURE_PERFTRACING_PAL_TCP)
    target_sources(dn-diagnosticserver-pal INTERFACE
        ds-ipc-pal-socket.c
    )
elseif(HOST_WIN32 OR CLR_CMAKE_TARGET_WIN32)
    target_sources(dn-diagnosticserver-pal INTERFACE
        ds-ipc-pal-namedpipe.c
    )
else()
    target_sources(dn-diagnosticserver-pal INTERFACE
        ds-ipc-pal-socket.c
    )
endif (FEATURE_PERFTRACING_PAL_TCP)


add_library(dn-eventpipe INTERFACE)
target_sources(dn-eventpipe INTERFACE
    ep.c
    ep-block.c
    ep-buffer.c
    ep-buffer-manager.c
    ep-config.c
    ep-event.c
    ep-event-instance.c
    ep-event-payload.c
    ep-event-source.c
    ep-file.c
    ep-json-file.c
    ep-metadata-generator.c
    ep-provider.c
    ep-sample-profiler.c
    ep-session.c
    ep-session-provider.c
    ep-stack-contents.c
    ep-stream.c
    ep-string.c
    ep-thread.c)

target_include_directories(dn-eventpipe INTERFACE ${CMAKE_CURRENT_BINARY_DIR})
target_link_libraries(dn-eventpipe INTERFACE dn-containers)
