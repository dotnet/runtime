

# a list of every component, add a new component here
set(components
  hot_reload
  #component_two, etc...
  )

# the sources for each individiable component define a new
# component_name-sources_base list for each component, and a
# component_name-stub-sources_base list for the component stub.
set(hot_reload-sources_base
  hot_reload.c
  hot_reload.h
  )
set(hot_reload-stub-sources_base
  hot_reload-stub.c
  )

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
    target_compile_definitions ("${component}-objects" PRIVATE -DCOMPILING_COMPONENT_DYNAMIC)
    add_library("mono-component-${component}" SHARED $<TARGET_OBJECTS:${component}-objects>)
    target_link_libraries("mono-component-${component}" monosgen-shared)
    install(TARGETS "mono-component-${component}" LIBRARY)
  endforeach()

  # define a single components_stubs_objects target that will be linked into
  # the runtime to be used as fallbacks if the dynamic components are not
  # available
  add_library (components_stubs_objects OBJECT)
  foreach (component IN LISTS components)
    target_sources (components_stubs_objects PRIVATE ${${component}-stub-sources})
  endforeach()
  target_link_libraries(components_stubs_objects component_base)

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

  # define a single components_static_objects target for mini if building a shared libmono with static-linked components
  add_library (components_static_objects OBJECT)
  foreach (component IN LISTS components)
    target_sources (components_static_objects PRIVATE ${${component}-sources})
  endforeach()
  target_link_libraries(components_static_objects component_base)

endif()
