
add_library(monoapi_utils INTERFACE)

set(utils_public_headers_base
    mono-logger.h
    mono-error.h
    mono-forward.h
    mono-publib.h
    mono-jemalloc.h
    mono-dl-fallback.h
    mono-private-unstable.h
    mono-counters.h)
addprefix(utils_public_headers ./mono/utils "${utils_public_headers_base}")

target_sources(monoapi_utils INTERFACE ${utils_public_headers})

target_include_directories(monoapi_utils INTERFACE .)

set_target_properties(monoapi_utils PROPERTIES PUBLIC_HEADER "${utils_public_headers}")

add_library(monoapi_metadata INTERFACE)

set(metadata_public_headers_base
	appdomain.h
	assembly.h
	attrdefs.h
	blob.h
	class.h
	debug-helpers.h
	debug-mono-symfile.h
	environment.h
	exception.h
	image.h
	loader.h
	metadata.h
	mono-config.h
	mono-debug.h
	mono-gc.h
	mono-private-unstable.h
	object.h
	object-forward.h
	opcodes.h
	profiler.h
	profiler-events.h
	reflection.h
	row-indexes.h
	sgen-bridge.h
	threads.h
	tokentype.h
	verify.h)
addprefix(metadata_public_headers ./mono/metadata/ "${metadata_public_headers_base}")

set_target_properties(monoapi_metadata PROPERTIES PUBLIC_HEADER "${metadata_public_headers}")

add_library(monoapi_mini INTERFACE)

set(mini_public_headers_base
  jit.h
  mono-private-unstable.h)
addprefix(mini_public_headers ./mono/mini "${mini_public_headers_base}")

set_target_properties(monoapi_mini PROPERTIES PUBLIC_HEADER "${mini_public_headers}")

add_library(monoapi INTERFACE)
target_link_libraries(monoapi INTERFACE monoapi_utils monoapi_metadata monoapi_mini)

if(INSTALL_MONO_API)
  install(TARGETS monoapi_utils PUBLIC_HEADER DESTINATION ${CMAKE_INSTALL_INCLUDEDIR}/mono-2.0/mono/utils)
  install(TARGETS monoapi_metadata PUBLIC_HEADER DESTINATION ${CMAKE_INSTALL_INCLUDEDIR}/mono-2.0/mono/metadata)
  install(TARGETS monoapi_mini PUBLIC_HEADER DESTINATION ${CMAKE_INSTALL_INCLUDEDIR}/mono-2.0/mono/jit)
endif()
