# a list of every component.
set(components "")

# the sources for each individiable component define a new
# component_name-sources_base list for each component, and a
# component_name-stub-sources_base list for the component stub.
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

if(ENABLE_PERFTRACING)

    list(APPEND components
        event_pipe
    )

    set (SHARED_EVENTPIPE_INCLUDE_PATH "../../../native/")
    set (SHARED_EVENTPIPE_SOURCE_PATH "../../../native/eventpipe/")
    set (MONO_EVENTPIPE_SHIM_SOURCE_PATH "../eventpipe/")

    include(${MONO_EVENTPIPE_SHIM_SOURCE_PATH}/CMakeLists.txt)

    include_directories(
      ${SHARED_EVENTPIPE_INCLUDE_PATH}
      ${MONO_EVENTPIPE_SHIM_SOURCE_PATH}
    )

    set(event_pipe-sources_base
      ${eventpipe_sources}
      ${diagnostic_server_sources}
      event_pipe.c
      event_pipe.h
      diagnostics_server.c
      diagnostics_server.h
      )
    set(event_pipe-stub-sources_base
      event_pipe-stub.c
      diagnostics_server-stub.c
      )

endif(ENABLE_PERFTRACING)

# from here down, all the components are treated in the same way

# a generic component interface that all components implement
add_library(component_base INTERFACE)
target_sources(component_base INTERFACE
  ../component/component.h
  )


# define a component_name-objects and component_name-stub-objects object
# targets with the relative source file names
foreach (component IN LISTS components)
  addprefix("${component}-sources" ../component "${${component}-sources_base}")
  add_library("${component}-objects" OBJECT "${${component}-sources}")
  target_link_libraries("${component}-objects" component_base)
  addprefix("${component}-stub-sources" ../component "${${component}-stub-sources_base}")
  add_library("${component}-stub-objects" OBJECT "${${component}-stub-sources}")
  target_link_libraries("${component}-stub-objects" component_base)
endforeach()

if (NOT STATIC_COMPONENTS)
  # define a shared library for each component
  foreach (component IN LISTS components)
    if(HOST_WIN32)
      add_library("mono-component-${component}" SHARED "${${component}-sources}")
    else()
      add_library("mono-component-${component}" SHARED $<TARGET_OBJECTS:${component}-objects>)
    endif()
    target_compile_definitions ("mono-component-${component}" PRIVATE -DCOMPILING_COMPONENT_DYNAMIC;-DMONO_DLL_IMPORT)
    if (NOT DISABLE_SHARED_LIBS)
      # each shared library component gets its own copy for eglib
      #
      # FIXME: this is bad for things like the g_log_set_default_handler/g_logv
      # which use global state - we should move those functions into
      # monosgen-shared and get them via dynamic linking.
      target_link_libraries("mono-component-${component}" eglib_objects)
      target_link_libraries("mono-component-${component}" monosgen-shared)
      # If we disable shared libs, but build dynamic components we would need
      # to enable allowing undefined symbols here (presumably to be resolved
      # from the mono-sgen executable.  But that's not a configuration we
      # should need in dotnet/runtime.
    endif()
    install(TARGETS "mono-component-${component}" LIBRARY)
  endforeach()

  # define a single static library mono-components-stubs-static that will be linked into
  # the runtime to be used as fallbacks if the dynamic components are not
  # available
  add_library ("mono-components-stubs-static" STATIC)
  foreach (component IN LISTS components)
    target_sources ("mono-components-stubs-static" PRIVATE $<TARGET_OBJECTS:${component}-stub-objects>)
  endforeach()
  target_link_libraries("mono-components-stubs-static" component_base)

else()

  #define a static library for each component and component stub
  foreach (component IN LISTS components)
    add_library("mono-component-${component}-static" STATIC $<TARGET_OBJECTS:${component}-objects>)
    install(TARGETS "mono-component-${component}-static" LIBRARY)
  endforeach()
  foreach (component IN LISTS components)
    add_library("mono-component-${component}-stub-static" STATIC $<TARGET_OBJECTS:${component}-stub-objects>)
    install(TARGETS "mono-component-${component}-stub-static" LIBRARY)
  endforeach()

  # define a single static library mono-components-static for mini if building a shared libmono with static-linked components
  add_library ("mono-components-static" STATIC)
  foreach (component IN LISTS components)
    target_sources ("mono-components-static" PRIVATE $<TARGET_OBJECTS:${component}-objects>)
  endforeach()
  target_link_libraries("mono-components-static" component_base)

endif()
