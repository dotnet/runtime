include(CheckSymbolExists)

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

add_library(dn-diagnosticserver INTERFACE)

target_sources(dn-diagnosticserver INTERFACE
    ds-sources.c
    ds-dump-protocol.c
    ds-eventpipe-protocol.c
    ds-ipc.c
    ds-process-protocol.c
    ds-profiler-protocol.c
    ds-protocol.c
    ds-server.c)

target_include_directories(dn-diagnosticserver INTERFACE ${CMAKE_CURRENT_BINARY_DIR})

add_library(dn-diagnosticserver-pal INTERFACE)
target_include_directories(dn-diagnosticserver-pal INTERFACE ${CMAKE_CURRENT_BINARY_DIR})

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
    ep-sources.c
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
