project(monoapi)

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


add_library(monoapi INTERFACE)
target_link_libraries(monoapi INTERFACE monoapi_utils)

if(NOT DISABLE_LIBS)
  install(TARGETS monoapi_utils PUBLIC_HEADER DESTINATION ${CMAKE_INSTALL_INCLUDEDIR}/mono-2.0/utils)
endif()
