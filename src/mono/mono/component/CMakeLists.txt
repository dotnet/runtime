

# a list of every component, add a new component here
set(components
  hot_reload
  #component_two, etc...
  )

# the generic components source files
set(component_sources_base
  component.h
  )

# the sources for each individiable component
# define a new component_name-sources_base list for each component

set(hot_reload-sources_base
  ${component_source_base}
  hot_reload.c
  hot_reload.h
  )

# from here down, all the components are treated in the same way

# define a component_name-objects oject target with the relative source file names
foreach (component IN LISTS components)
  addprefix("${component}-sources" ../component "${${component}-sources_base}")
  add_library("${component}-objects" OBJECT "${${component}-sources}")
endforeach()

if (NOT STATIC_COMPONENTS)
  foreach (component IN LISTS components)
    target_compile_definitions ("${component}-objects" PRIVATE -DCOMPILING_COMPONENT_DYNAMIC)
    add_library("mono-component-${component}" SHARED $<TARGET_OBJECTS:${component}-objects>)
    target_link_libraries("mono-component-${component}" monosgen-shared)
    install(TARGETS "mono-component-${component}" LIBRARY)
  endforeach()
endif()
