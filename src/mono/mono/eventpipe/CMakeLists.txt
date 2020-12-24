if(ENABLE_PERFTRACING)

    include (${SHARED_EVENTPIPE_SOURCE_PATH}CMakeLists.txt)

    set(MONO_EVENTPIPE_SHIM_SOURCES "")
    set(MONO_EVENTPIPE_SHIM_HEADERS "")

    list(APPEND MONO_EVENTPIPE_SHIM_SOURCES
        ds-rt-mono.c
        ep-rt-mono.c
    )

    list(APPEND MONO_EVENTPIPE_SHIM_HEADERS
        ds-rt-mono.h
        ds-rt-types-mono.h
        ep-rt-config-mono.h
        ep-rt-mono.h
        ep-rt-types-mono.h
    )

    set(shared_eventpipe_sources_base "")
    set(mono_eventpipe_shim_sources_base "")

    list(APPEND shared_eventpipe_sources_base
        ${SHARED_EVENTPIPE_SOURCES}
        ${SHARED_EVENTPIPE_HEADERS}
        ${SHARED_DIAGNOSTIC_SERVER_SOURCES}
        ${SHARED_DIAGNOSTIC_SERVER_HEADERS}
        ${SHARED_DIAGNOSTIC_SERVER_PAL_SOURCES}
        ${SHARED_DIAGNOSTIC_SERVER_PAL_HEADERS}
    )

    list(APPEND mono_eventpipe_shim_sources_base
        ${MONO_EVENTPIPE_SHIM_SOURCES}
        ${MONO_EVENTPIPE_SHIM_HEADERS}
    )

    addprefix(shared_eventpipe_sources_base ${SHARED_EVENTPIPE_SOURCE_PATH} "${shared_eventpipe_sources_base}")
    addprefix(mono_eventpipe_shim_sources_base ${MONO_EVENTPIPE_SHIM_SOURCE_PATH} "${mono_eventpipe_shim_sources_base}")

    set(eventpipe_sources ${shared_eventpipe_sources_base} ${mono_eventpipe_shim_sources_base})

endif(ENABLE_PERFTRACING)
