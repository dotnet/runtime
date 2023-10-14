set(MONO_COMPONENT_PATH "${CMAKE_CURRENT_SOURCE_DIR}/../component")
set(SHARED_CONTAINERS_SOURCE_PATH "${CLR_SRC_NATIVE_DIR}/containers/")
set(SHARED_EVENTPIPE_SOURCE_PATH "${CLR_SRC_NATIVE_DIR}/eventpipe/")
set(MONO_EVENTPIPE_SHIM_SOURCE_PATH "${CMAKE_CURRENT_SOURCE_DIR}/../eventpipe/")
set(MONO_EVENTPIPE_GEN_INCLUDE_PATH "${CMAKE_CURRENT_BINARY_DIR}/eventpipe")

set(MONO_HOT_RELOAD_COMPONENT_NAME "hot_reload")
set(MONO_DIAGNOSTICS_TRACING_COMPONENT_NAME "diagnostics_tracing")
set(MONO_DEBUGGER_COMPONENT_NAME "debugger")
set(MONO_MARSHAL_ILGEN_COMPONENT_NAME "marshal-ilgen")

# a list of every component.
set(components "")
# a list of components needed by the AOT compiler
set(components_for_aot "")

# the sources for each individiable component define a new
# component_name-sources list for each component, and a
# component_name-stub-sources list for the component stub.

# debugger
list(APPEND components
  ${MONO_DEBUGGER_COMPONENT_NAME}
)
set(${MONO_DEBUGGER_COMPONENT_NAME}-sources
  ${MONO_COMPONENT_PATH}/debugger.c
  ${MONO_COMPONENT_PATH}/debugger.h
  ${MONO_COMPONENT_PATH}/debugger-networking.c
  ${MONO_COMPONENT_PATH}/debugger-networking.h
  ${MONO_COMPONENT_PATH}/debugger-poll.c
  ${MONO_COMPONENT_PATH}/debugger-poll.h
  ${MONO_COMPONENT_PATH}/debugger-agent.c
  ${MONO_COMPONENT_PATH}/debugger-agent.h
  ${MONO_COMPONENT_PATH}/debugger-engine.c
  ${MONO_COMPONENT_PATH}/debugger-engine.h
  ${MONO_COMPONENT_PATH}/debugger-state-machine.h
  ${MONO_COMPONENT_PATH}/debugger-state-machine.c
  ${MONO_COMPONENT_PATH}/mini-wasm-debugger.c
  ${MONO_COMPONENT_PATH}/mini-wasi-debugger.c
  ${MONO_COMPONENT_PATH}/debugger-protocol.h
  ${MONO_COMPONENT_PATH}/debugger-protocol.c
  )
set(${MONO_DEBUGGER_COMPONENT_NAME}-stub-sources
  ${MONO_COMPONENT_PATH}/debugger-stub.c
  )

# hot_reload
list(APPEND components
  ${MONO_HOT_RELOAD_COMPONENT_NAME}
)
set(${MONO_HOT_RELOAD_COMPONENT_NAME}-sources
  ${MONO_COMPONENT_PATH}/hot_reload.c
  ${MONO_COMPONENT_PATH}/hot_reload-internals.h
  ${MONO_COMPONENT_PATH}/hot_reload.h
  )
set(${MONO_HOT_RELOAD_COMPONENT_NAME}-stub-sources
  ${MONO_COMPONENT_PATH}/hot_reload-stub.c
  )

# diagnostics_tracing (event_pipe/diagnostics_server)
list(APPEND components
  ${MONO_DIAGNOSTICS_TRACING_COMPONENT_NAME}
)

include(${MONO_EVENTPIPE_SHIM_SOURCE_PATH}/eventpipe.cmake)

include_directories(
  ${MONO_EVENTPIPE_SHIM_SOURCE_PATH}
  ${MONO_EVENTPIPE_GEN_INCLUDE_PATH}
)

set(${MONO_DIAGNOSTICS_TRACING_COMPONENT_NAME}-sources
  ${container_sources}
  ${eventpipe_sources}
  ${diagnostic_server_sources}
  ${MONO_COMPONENT_PATH}/event_pipe.c
  ${MONO_COMPONENT_PATH}/event_pipe.h
  ${MONO_COMPONENT_PATH}/event_pipe-wasm.h
  ${MONO_COMPONENT_PATH}/diagnostics_server.c
  ${MONO_COMPONENT_PATH}/diagnostics_server.h
  )
set(${MONO_DIAGNOSTICS_TRACING_COMPONENT_NAME}-stub-sources
  ${MONO_COMPONENT_PATH}/event_pipe-stub.c
  ${MONO_COMPONENT_PATH}/diagnostics_server-stub.c
  )
set(${MONO_DIAGNOSTICS_TRACING_COMPONENT_NAME}-dependencies
  ${MONO_DIAGNOSTICS_TRACING_COMPONENT_NAME}-gen-headers
  ${MONO_DIAGNOSTICS_TRACING_COMPONENT_NAME}-gen-sources
)

# marshal-ilgen
list(APPEND components
  ${MONO_MARSHAL_ILGEN_COMPONENT_NAME}
)
list(APPEND components_for_aot
  ${MONO_MARSHAL_ILGEN_COMPONENT_NAME}
)

set(${MONO_MARSHAL_ILGEN_COMPONENT_NAME}-sources
  ${MONO_COMPONENT_PATH}/marshal-ilgen.c
  ${MONO_COMPONENT_PATH}/marshal-ilgen.h
)

# For every component not build into the AOT compiler, build the stub instead
set(stubs_for_aot "")
foreach (component IN LISTS components)
  if (NOT (component IN_LIST components_for_aot))
    list(APPEND stubs_for_aot "${component}")
  endif()
endforeach()


set(${MONO_MARSHAL_ILGEN_COMPONENT_NAME}-stub-sources
  ${MONO_COMPONENT_PATH}/marshal-ilgen-stub.c
)

if (AOT_COMPONENTS)
  set(components_to_build  ${components_for_aot})
  set(stubs_to_build ${stubs_for_aot})
else()
  set(components_to_build  ${components})
  set(stubs_to_build ${components})
endif()

# from here down, all the components are treated in the same way

#define a library for each component and component stub
function(define_component_libs)
  # NOTE: keep library naming pattern in sync with RuntimeComponentManifest.targets
  if (AOT_COMPONENTS OR NOT DISABLE_LIBS )
    foreach(component IN LISTS components_to_build)
      add_library("mono-component-${component}-static" STATIC $<TARGET_OBJECTS:${component}-objects>)
      install(TARGETS "mono-component-${component}-static" LIBRARY)
    endforeach()
    foreach(component IN LISTS stubs_to_build)
      add_library("mono-component-${component}-stub-static" STATIC $<TARGET_OBJECTS:${component}-stub-objects>)
      install(TARGETS "mono-component-${component}-stub-static" LIBRARY)
    endforeach()
  endif()
endfunction()

# a generic component interface that all components implement
add_library(component_base INTERFACE)
target_sources(component_base INTERFACE
  ${MONO_COMPONENT_PATH}/component.h
)
target_link_libraries(component_base INTERFACE monoapi)

if(NOT AOT_COMPONENTS AND (DISABLE_COMPONENTS OR DISABLE_LIBS))
  set(DISABLE_COMPONENT_OBJECTS 1)
endif()

# define a component_name-objects and component_name-stub-objects object
# targets with the relative source file names
foreach(component IN LISTS components)
  if(NOT DISABLE_COMPONENT_OBJECTS)
    add_library("${component}-objects" OBJECT "${${component}-sources}")
    target_link_libraries("${component}-objects" PRIVATE component_base)
    target_link_libraries("${component}-objects" PUBLIC eglib_api)
    foreach(dependency IN LISTS "${component}-dependencies")
      add_dependencies("${component}-objects" "${dependency}")
    endforeach()
  endif()
  add_library("${component}-stub-objects" OBJECT "${${component}-stub-sources}")
  target_link_libraries("${component}-stub-objects" PRIVATE component_base)
  target_link_libraries("${component}-stub-objects" PUBLIC eglib_api)
endforeach()

if(NOT DISABLE_COMPONENTS AND NOT STATIC_COMPONENTS)
  # define a shared library for each component
  foreach(component IN LISTS components_to_build)
    # NOTE: keep library naming pattern in sync with RuntimeComponentManifest.targets
    if(HOST_WIN32)
      add_library("mono-component-${component}" SHARED "${${component}-sources}")
      target_compile_definitions("mono-component-${component}" PRIVATE -DCOMPILING_COMPONENT_DYNAMIC;-DMONO_DLL_IMPORT)
    else()
      add_library("mono-component-${component}" SHARED $<TARGET_OBJECTS:${component}-objects>)
      target_compile_definitions("${component}-objects" PRIVATE -DCOMPILING_COMPONENT_DYNAMIC;-DMONO_DLL_IMPORT)
    endif()
    foreach(dependency IN LISTS "${component}-dependencies")
      add_dependencies("mono-component-${component}" "${dependency}")
    endforeach()
    # each shared library component gets its own copy for eglib
    #
    # FIXME: this is bad for things like the g_log_set_default_handler/g_logv
    # which use global state - we should move those functions into
    # monosgen-shared and get them via dynamic linking.
    target_link_libraries("mono-component-${component}" PRIVATE eglib_objects)
    if(NOT DISABLE_SHARED_LIBS)
      # If we disable shared libs, but build dynamic components we would need
      # to enable allowing undefined symbols here (presumably to be resolved
      # from the mono-sgen executable. But that's not a configuration we
      # should need in dotnet/runtime.
      target_link_libraries("mono-component-${component}" PRIVATE monosgen-shared)
    endif()
    target_link_libraries("mono-component-${component}" PRIVATE ${OS_LIBS})
    install_with_stripped_symbols("mono-component-${component}" TARGETS lib)
  endforeach()

  #define a library for each component and component stub
  define_component_libs()

elseif(AOT_COMPONENTS OR (NOT DISABLE_COMPONENTS AND STATIC_COMPONENTS))

  #define a library for each component and component stub
  define_component_libs()

  # define a list of mono-components objects for mini if building a shared libmono with static-linked components
  set(mono-components-objects "")
  foreach(component IN LISTS components_to_build)
    list(APPEND mono-components-objects $<TARGET_OBJECTS:${component}-objects>)
  endforeach()

endif()

# define a list of mono-components-stubs objects that will be linked into
# the runtime to be used as fallbacks if the dynamic components are not
# available or when only static component stubs are requested.
set(mono-components-stub-objects "")
foreach(component IN LISTS components)
  list(APPEND mono-components-stub-objects $<TARGET_OBJECTS:${component}-stub-objects>)
endforeach()

if(NOT MONO_CROSS_COMPILE)
  set(TemplateMonoRuntimeComponentSharedLibExt "${CMAKE_SHARED_LIBRARY_SUFFIX}")
  set(TemplateMonoRuntimeComponentStaticLibExt "${CMAKE_STATIC_LIBRARY_SUFFIX}")
  set(TemplateRuntimeIdentifier "${MONO_COMPONENTS_RID}")
  if(DISABLE_COMPONENTS)
    set(TemplateMonoRuntimeComponentLinking "static")
    set(TemplateMonoRuntimeAvailableComponents "")
  else()
    list(TRANSFORM components REPLACE "^(.+)$" "{ \"identity\": \"\\1\", \"RuntimeIdentifier\": \"${TemplateRuntimeIdentifier}\" }," OUTPUT_VARIABLE TemplateMonoRuntimeAvailableComponentsList)
    list(JOIN TemplateMonoRuntimeAvailableComponentsList "\n" TemplateMonoRuntimeAvailableComponents)
    if(STATIC_COMPONENTS)
      set(TemplateMonoRuntimeComponentLinking "static")
    else()
      set(TemplateMonoRuntimeComponentLinking "dynamic")
    endif()
  endif()
  # Write a RuntimeComponentManifest.json file in the artifacts/obj/mono/<host>/build/ directory
  # without the ../.. the file would go in artifacts/obj/mono/<host>/mono/mini
  configure_file( "${MONO_COMPONENT_PATH}/RuntimeComponentManifest.json.in" "../../build/RuntimeComponentManifest.json")
endif()

# component tests
set(MONO_EVENTPIPE_TEST_SOURCE_PATH "${MONO_EVENTPIPE_SHIM_SOURCE_PATH}/test")
include(${MONO_EVENTPIPE_TEST_SOURCE_PATH}/CMakeLists.txt)
