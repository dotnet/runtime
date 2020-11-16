if(ENABLE_PERFTRACING)

	include (../../../common/src/eventpipe/CMakeLists.txt)

	set(EVENTPIPE_MONO_SHIM_SOURCES "")
	set(EVENTPIPE_MONO_SHIM_HEADERS "")

	list(APPEND EVENTPIPE_MONO_SHIM_SOURCES
		ds-rt-mono.c
		ep-rt-mono.c
	)

	list(APPEND EVENTPIPE_MONO_SHIM_HEADERS
		ds-rt-mono.h
		ds-rt-types-mono.h
		ep-rt-config-mono.h
		ep-rt-mono.h
		ep-rt-types-mono.h
	)

	set(shared_eventpipe_sources_base "")
	set(eventpipe_mono_shim_sources_base "")

	list(APPEND shared_eventpipe_sources_base
		${SHARED_EVENTPIPE_SOURCES}
		${SHARED_EVENTPIPE_HEADERS}
		${SHARED_DIAGNOSTIC_SERVER_SOURCES}
		${SHARED_DIAGNOSTIC_SERVER_HEADERS}
	)

	list(APPEND eventpipe_mono_shim_sources_base
		${EVENTPIPE_MONO_SHIM_SOURCES}
		${EVENTPIPE_MONO_SHIM_HEADERS}
	)

	addprefix(shared_eventpipe_sources_base ../../../common/src/eventpipe "${shared_eventpipe_sources_base}")
	addprefix(eventpipe_mono_shim_sources_base ../eventpipe "${eventpipe_mono_shim_sources_base}")

	set(eventpipe_sources ${shared_eventpipe_sources_base} ${eventpipe_mono_shim_sources_base})

endif(ENABLE_PERFTRACING)
