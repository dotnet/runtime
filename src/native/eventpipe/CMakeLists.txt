# Define the DiagnosticsServer and EventPipe as interface libraries.
# We must define them as interface libraries as each runtime builds the same set of files slightly differently.
# Defining it as an interface library allows us to specify common sources, include directories, dependencies, etc.
# in one place, but also allow each runtime to add any settings that are specific to that runtime.
# This includes, but is not limited to each runtime's implementation of the ds-rt.h and ep-rt.h contracts.

if (NOT DEFINED SHARED_EVENTPIPE_CONFIG_HEADER_PATH)
    message(FATAL_ERROR "Required configuration SHARED_EVENTPIPE_CONFIG_HEADER_PATH not set.")
endif (NOT DEFINED SHARED_EVENTPIPE_CONFIG_HEADER_PATH)

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
target_include_directories(dn-diagnosticserver INTERFACE ${SHARED_EVENTPIPE_CONFIG_HEADER_PATH})
target_link_libraries(dn-diagnosticserver INTERFACE dn-containers)

add_library(dn-diagnosticserver-pal INTERFACE)
target_include_directories(dn-diagnosticserver-pal INTERFACE ${CMAKE_CURRENT_BINARY_DIR})
target_include_directories(dn-diagnosticserver-pal INTERFACE ${SHARED_EVENTPIPE_CONFIG_HEADER_PATH})
target_link_libraries(dn-diagnosticserver-pal INTERFACE dn-containers)

if (FEATURE_PERFTRACING_PAL_WS)
    target_sources(dn-diagnosticserver-pal INTERFACE
        ds-ipc-pal-websocket.c
    )
elseif (FEATURE_PERFTRACING_PAL_TCP)
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
endif (FEATURE_PERFTRACING_PAL_WS)


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
target_include_directories(dn-eventpipe INTERFACE ${SHARED_EVENTPIPE_CONFIG_HEADER_PATH})
target_link_libraries(dn-eventpipe INTERFACE dn-containers)
