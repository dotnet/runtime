include(${MONO_EVENTPIPE_SHIM_SOURCE_PATH}/gen-eventing.cmake)

# For feature detection to work correctly, this needs to be outside of the conditional.
set(EP_GENERATED_HEADER_PATH "${MONO_EVENTPIPE_GEN_INCLUDE_PATH}")
include(${SHARED_EVENTPIPE_SOURCE_PATH}configure.cmake)

if(ENABLE_PERFTRACING)

    if (FEATURE_PERFTRACING_PAL_TCP)
        add_definitions(-DENABLE_PERFTRACING_PAL_TCP)
    endif (FEATURE_PERFTRACING_PAL_TCP)

    if (FEATURE_PERFTRACING_DISABLE_PERFTRACING_LISTEN_PORTS)
        add_definitions(-DDISABLE_PERFTRACING_LISTEN_PORTS)
    endif (FEATURE_PERFTRACING_DISABLE_PERFTRACING_LISTEN_PORTS)

    if (FEATURE_PERFTRACING_DISABLE_DEFAULT_LISTEN_PORT)
        add_definitions(-DDISABLE_PERFTRACING_DEFAULT_LISTEN_PORT)
    endif (FEATURE_PERFTRACING_DISABLE_DEFAULT_LISTEN_PORT)

    if (FEATURE_PERFTRACING_DISABLE_CONNECT_PORTS)
        add_definitions(-DDISABLE_PERFTRACING_CONNECT_PORTS)
    endif (FEATURE_PERFTRACING_DISABLE_CONNECT_PORTS)

    include (${SHARED_EVENTPIPE_SOURCE_PATH}CMakeLists.txt)

    set(MONO_EVENTPIPE_SHIM_SOURCES "")
    set(MONO_EVENTPIPE_SHIM_HEADERS "")

    set(MONO_DIAGNOSTIC_SERVER_SHIM_SOURCES "")
    set(MONO_DIAGNOSTIC_SERVER_SHIM_HEADERS "")

    list(APPEND MONO_EVENTPIPE_SHIM_SOURCES
        ep-rt-mono.c
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

    set(shared_eventpipe_sources_base "")
    set(mono_eventpipe_shim_sources_base "")

    set(shared_diagnostic_server_sources_base "")
    set(mono_diagnostic_server_shim_sources_base "")

    list(APPEND shared_eventpipe_sources_base
        ${SHARED_EVENTPIPE_SOURCES}
        ${SHARED_EVENTPIPE_HEADERS}
    )

    list(APPEND shared_diagnostic_server_sources_base
        ${SHARED_DIAGNOSTIC_SERVER_SOURCES}
        ${SHARED_DIAGNOSTIC_SERVER_HEADERS}
        ${SHARED_DIAGNOSTIC_SERVER_PAL_SOURCES}
        ${SHARED_DIAGNOSTIC_SERVER_PAL_HEADERS}
    )

    list(APPEND mono_eventpipe_shim_sources_base
        ${MONO_EVENTPIPE_SHIM_SOURCES}
        ${MONO_EVENTPIPE_SHIM_HEADERS}
    )

    list(APPEND mono_diagnostic_server_shim_sources_base
        ${MONO_DIAGNOSTIC_SERVER_SHIM_SOURCES}
        ${MONO_DIAGNOSTIC_SERVER_SHIM_HEADERS}
    )

    addprefix(shared_eventpipe_sources_base ${SHARED_EVENTPIPE_SOURCE_PATH} "${shared_eventpipe_sources_base}")
    addprefix(mono_eventpipe_shim_sources_base ${MONO_EVENTPIPE_SHIM_SOURCE_PATH} "${mono_eventpipe_shim_sources_base}")

    addprefix(shared_diagnostic_server_sources_base ${SHARED_EVENTPIPE_SOURCE_PATH} "${shared_diagnostic_server_sources_base}")
    addprefix(mono_diagnostic_server_shim_sources_base ${MONO_EVENTPIPE_SHIM_SOURCE_PATH} "${mono_diagnostic_server_shim_sources_base}")

    set(eventpipe_sources ${shared_eventpipe_sources_base} ${SHARED_EVENTPIPE_CONFIG_HEADERS} ${mono_eventpipe_shim_sources_base} ${MONO_EVENTPIPE_GEN_HEADERS} ${MONO_EVENTPIPE_GEN_SOURCES})
    set(diagnostic_server_sources ${shared_diagnostic_server_sources_base} ${mono_diagnostic_server_shim_sources_base})

    set_source_files_properties(${SHARED_EVENTPIPE_SOURCE_PATH}/ep-sources.c PROPERTIES COMPILE_DEFINITIONS EP_FORCE_INCLUDE_SOURCE_FILES)
    set_source_files_properties(${SHARED_EVENTPIPE_SOURCE_PATH}/ds-sources.c PROPERTIES COMPILE_DEFINITIONS DS_FORCE_INCLUDE_SOURCE_FILES)

endif(ENABLE_PERFTRACING)
