if(ENABLE_PERFTRACING OR FEATURE_PERFTRACING)
    set (SHARED_EVENTPIPE_SOURCES "")
    set (SHARED_EVENTPIPE_HEADERS "")
    set (SHARED_DIAGNOSTIC_SERVER_SOURCES "")
    set (SHARED_DIAGNOSTIC_SERVER_HEADERS "")

    list(APPEND SHARED_EVENTPIPE_SOURCES
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
        ep-thread.c
    )

    list(APPEND SHARED_EVENTPIPE_HEADERS
        ep.h
        ep-block.h
        ep-buffer.h
        ep-buffer-manager.h
        ep-config.h
        ep-config-internals.h
        ep-event.h
        ep-event-instance.h
        ep-event-payload.h
        ep-event-source.h
        ep-file.h
        ep-getter-setter.h
        ep-ipc-pal-types.h
        ep-ipc-stream.h
        ep-json-file.h
        ep-metadata-generator.h
        ep-provider.h
        ep-provider-internals.h
        ep-rt.h
        ep-rt-config.h
        ep-rt-types.h
        ep-sample-profiler.h
        ep-session.h
        ep-session-provider.h
        ep-stack-contents.h
        ep-stream.h
        ep-thread.h
        ep-types.h
    )

    list(APPEND SHARED_DIAGNOSTIC_SERVER_SOURCES
        ds.c
        ds-dump-protocol.c
        ds-eventpipe-protocol.c
        ds-ipc.c
        ds-process-protocol.c
        ds-profiler-protocol.c
        ds-protocol.c
        ds-server.c
    )

    list(APPEND SHARED_DIAGNOSTIC_SERVER_HEADERS
        ds-dump-protocol.h
        ds-eventpipe-protocol.h
        ds-getter-setter.h
        ds-ipc.h
        ds-ipc-pal.h
        ds-ipc-pal-types.h
        ds-process-protocol.h
        ds-profiler-protocol.h
        ds-protocol.h
        ds-rt.h
        ds-rt-config.h
        ds-rt-types.h
        ds-server.h
        ds-types.h
    )

    if(HOST_WIN32 OR CLR_CMAKE_TARGET_WIN32)
        list(APPEND SHARED_DIAGNOSTIC_SERVER_PAL_SOURCES
            ds-ipc-win32.c
        )
        list(APPEND SHARED_DIAGNOSTIC_SERVER_PAL_HEADERS
            ds-ipc-win32.h
        )
    else(HOST_WIN32 OR CLR_CMAKE_TARGET_WIN32)
        list(APPEND SHARED_DIAGNOSTIC_SERVER_PAL_SOURCES
            ds-ipc-posix.c
        )
        list(APPEND SHARED_DIAGNOSTIC_SERVER_PAL_HEADERS
            ds-ipc-posix.h
        )
    endif(HOST_WIN32 OR CLR_CMAKE_TARGET_WIN32)

endif(ENABLE_PERFTRACING OR FEATURE_PERFTRACING)
