if(ENABLE_PERFTRACING)
    message(FATAL_ERROR "EventPipe is not supported on Mono; the eventing sources it needs are no longer generated.")
endif()

add_custom_target(${MONO_DIAGNOSTICS_TRACING_COMPONENT_NAME}-gen-headers)
add_custom_target(${MONO_DIAGNOSTICS_TRACING_COMPONENT_NAME}-gen-sources)
