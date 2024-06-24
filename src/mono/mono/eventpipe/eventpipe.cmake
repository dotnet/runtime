include(${MONO_EVENTPIPE_SHIM_SOURCE_PATH}/gen-eventing.cmake)

if(ENABLE_PERFTRACING)
    set(MONO_EVENTPIPE_SHIM_SOURCES "")
    set(MONO_EVENTPIPE_SHIM_HEADERS "")

    set(MONO_DIAGNOSTIC_SERVER_SHIM_SOURCES "")
    set(MONO_DIAGNOSTIC_SERVER_SHIM_HEADERS "")

    list(APPEND MONO_EVENTPIPE_SHIM_SOURCES
        ep-rt-mono.c
        ep-rt-mono-runtime-provider.c
        ep-rt-mono-profiler-provider.c
    )

    list(APPEND MONO_DIAGNOSTIC_SERVER_SHIM_SOURCES
        ds-rt-mono.c
    )

    list(APPEND MONO_EVENTPIPE_SHIM_HEADERS
        ep-rt-config-mono.h
        ep-rt-mono.h
        ep-rt-types-mono.h
    )

    list(APPEND MONO_DIAGNOSTIC_SERVER_SHIM_HEADERS
        ds-rt-mono.h
        ds-rt-types-mono.h
    )

    set(mono_eventpipe_shim_sources_base "")

    set(mono_diagnostic_server_shim_sources_base "")

    list(APPEND mono_eventpipe_shim_sources_base
        ${MONO_EVENTPIPE_SHIM_SOURCES}
        ${MONO_EVENTPIPE_SHIM_HEADERS}
    )

    list(APPEND mono_diagnostic_server_shim_sources_base
        ${MONO_DIAGNOSTIC_SERVER_SHIM_SOURCES}
        ${MONO_DIAGNOSTIC_SERVER_SHIM_HEADERS}
    )

    addprefix(mono_eventpipe_shim_sources_base ${MONO_EVENTPIPE_SHIM_SOURCE_PATH} "${mono_eventpipe_shim_sources_base}")

    addprefix(mono_diagnostic_server_shim_sources_base ${MONO_EVENTPIPE_SHIM_SOURCE_PATH} "${mono_diagnostic_server_shim_sources_base}")

    set(eventpipe_sources ${mono_eventpipe_shim_sources_base} ${MONO_EVENTPIPE_GEN_HEADERS} ${MONO_EVENTPIPE_GEN_SOURCES})
    set(diagnostic_server_sources ${mono_diagnostic_server_shim_sources_base})

endif(ENABLE_PERFTRACING)
