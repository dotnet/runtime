# a list of every component.
set(components "")

# the sources for each individiable component define a new
# component_name-sources_base list for each component, and a
# component_name-stub-sources_base list for the component stub.

# hot_reload
list(APPEND components
    hot_reload
)
set(hot_reload-sources_base
  hot_reload.c
  hot_reload.h
  )
set(hot_reload-stub-sources_base
  hot_reload-stub.c
  )

# diagnostics_tracing (event_pipe/diagnostics_server)
list(APPEND components
    diagnostics_tracing
)

set(SHARED_EVENTPIPE_INCLUDE_PATH "../../../native/")
set(SHARED_EVENTPIPE_SOURCE_PATH "../../../native/eventpipe/")
set(MONO_EVENTPIPE_SHIM_SOURCE_PATH "../eventpipe/")

include(${MONO_EVENTPIPE_SHIM_SOURCE_PATH}/CMakeLists.txt)

include_directories(
  ${SHARED_EVENTPIPE_INCLUDE_PATH}
  ${MONO_EVENTPIPE_SHIM_SOURCE_PATH}
)

set(diagnostics_tracing-sources_base
  ${eventpipe_sources}
  ${diagnostic_server_sources}
  event_pipe.c
  event_pipe.h
  diagnostics_server.c
  diagnostics_server.h
  )
set(diagnostics_tracing-stub-sources_base
  event_pipe-stub.c
  diagnostics_server-stub.c
  )

# from here down, all the components are treated in the same way

# a generic component interface that all components implement
add_library(component_base INTERFACE)
target_sources(component_base INTERFACE
  ../component/component.h
)

if(DISABLE_COMPONENTS OR (NOT STATIC_COMPONENTS AND HOST_WIN32))
  set(DISABLE_COMPONENT_OBJECTS 1)
endif()

# define a component_name-objects and component_name-stub-objects object
# targets with the relative source file names
foreach(component IN LISTS components)
  addprefix("${component}-sources" ../component "${${component}-sources_base}")
  if(NOT DISABLE_COMPONENT_OBJECTS)
    add_library("${component}-objects" OBJECT "${${component}-sources}")
    target_link_libraries("${component}-objects" component_base)
  endif()
  addprefix("${component}-stub-sources" ../component "${${component}-stub-sources_base}")
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
    endif()
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

elseif(NOT DISABLE_COMPONENTS AND STATIC_COMPONENTS)

  #define a static library for each component and component stub
  foreach(component IN LISTS components)
    add_library("mono-component-${component}-static" STATIC $<TARGET_OBJECTS:${component}-objects>)
    install(TARGETS "mono-component-${component}-static" LIBRARY)
  endforeach()
  foreach(component IN LISTS components)
    add_library("mono-component-${component}-stub-static" STATIC $<TARGET_OBJECTS:${component}-stub-objects>)
    install(TARGETS "mono-component-${component}-stub-static" LIBRARY)
  endforeach()

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
