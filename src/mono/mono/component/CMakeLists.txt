set(MONO_COMPONENT_PATH "${CMAKE_CURRENT_SOURCE_DIR}/../component")
set(SHARED_EVENTPIPE_INCLUDE_PATH "${CMAKE_CURRENT_SOURCE_DIR}/../../../native/")
set(SHARED_EVENTPIPE_SOURCE_PATH "${CMAKE_CURRENT_SOURCE_DIR}/../../../native/eventpipe/")
set(MONO_EVENTPIPE_SHIM_SOURCE_PATH "${CMAKE_CURRENT_SOURCE_DIR}/../eventpipe/")
set(MONO_EVENTPIPE_GEN_INCLUDE_PATH "${CMAKE_CURRENT_BINARY_DIR}/eventpipe")

set(MONO_HOT_RELOAD_COMPONENT_NAME "hot_reload")
set(MONO_DIAGNOSTICS_TRACING_COMPONENT_NAME "diagnostics_tracing")

# a list of every component.
set(components "")

# the sources for each individiable component define a new
# component_name-sources list for each component, and a
# component_name-stub-sources list for the component stub.

# hot_reload
list(APPEND components
  ${MONO_HOT_RELOAD_COMPONENT_NAME}
)
set(${MONO_HOT_RELOAD_COMPONENT_NAME}-sources
  ${MONO_COMPONENT_PATH}/hot_reload.c
  ${MONO_COMPONENT_PATH}/hot_reload.h
  )
set(${MONO_HOT_RELOAD_COMPONENT_NAME}-stub-sources
  ${MONO_COMPONENT_PATH}/hot_reload-stub.c
  )

# diagnostics_tracing (event_pipe/diagnostics_server)
list(APPEND components
  ${MONO_DIAGNOSTICS_TRACING_COMPONENT_NAME}
)

include(${MONO_EVENTPIPE_SHIM_SOURCE_PATH}/CMakeLists.txt)

include_directories(
  ${SHARED_EVENTPIPE_INCLUDE_PATH}
  ${MONO_EVENTPIPE_SHIM_SOURCE_PATH}
  ${MONO_EVENTPIPE_GEN_INCLUDE_PATH}
)

set(${MONO_DIAGNOSTICS_TRACING_COMPONENT_NAME}-sources
  ${eventpipe_sources}
  ${diagnostic_server_sources}
  ${MONO_COMPONENT_PATH}/event_pipe.c
  ${MONO_COMPONENT_PATH}/event_pipe.h
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

# from here down, all the components are treated in the same way

#define a library for each component and component stub
function(define_component_libs)
  if (NOT DISABLE_LIBS)
    foreach(component IN LISTS components)
      add_library("mono-component-${component}-static" STATIC $<TARGET_OBJECTS:${component}-objects>)
      install(TARGETS "mono-component-${component}-static" LIBRARY)
    endforeach()
    foreach(component IN LISTS components)
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

if(DISABLE_COMPONENTS OR DISABLE_LIBS)
  set(DISABLE_COMPONENT_OBJECTS 1)
endif()

# define a component_name-objects and component_name-stub-objects object
# targets with the relative source file names
foreach(component IN LISTS components)
  if(NOT DISABLE_COMPONENT_OBJECTS)
    add_library("${component}-objects" OBJECT "${${component}-sources}")
    target_link_libraries("${component}-objects" component_base)
    foreach(dependency IN LISTS "${component}-dependencies")
      add_dependencies("${component}-objects" "${dependency}")
    endforeach()
  endif()
  add_library("${component}-stub-objects" OBJECT "${${component}-stub-sources}")
  target_link_libraries("${component}-stub-objects" component_base)
endforeach()

if(NOT DISABLE_COMPONENTS AND NOT STATIC_COMPONENTS)
  # define a shared library for each component
  foreach(component IN LISTS components)
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
    target_sources("mono-component-${component}" PRIVATE $<TARGET_OBJECTS:eglib_objects>)
    if(NOT DISABLE_SHARED_LIBS)
      # If we disable shared libs, but build dynamic components we would need
      # to enable allowing undefined symbols here (presumably to be resolved
      # from the mono-sgen executable. But that's not a configuration we
      # should need in dotnet/runtime.
      target_link_libraries("mono-component-${component}" PRIVATE monosgen-shared)
    endif()
    target_link_libraries("mono-component-${component}" PRIVATE ${OS_LIBS})
    install(TARGETS "mono-component-${component}" LIBRARY)
  endforeach()

  #define a library for each component and component stub
  define_component_libs()

elseif(NOT DISABLE_COMPONENTS AND STATIC_COMPONENTS)

  #define a library for each component and component stub
  define_component_libs()

  # define a list of mono-components objects for mini if building a shared libmono with static-linked components
  set(mono-components-objects "")
  foreach(component IN LISTS components)
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


# component tests
set(MONO_EVENTPIPE_TEST_SOURCE_PATH "${MONO_EVENTPIPE_SHIM_SOURCE_PATH}/test")
include(${MONO_EVENTPIPE_TEST_SOURCE_PATH}/CMakeLists.txt)
