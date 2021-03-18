
set(component_sources_base
  component.h
  )

set(hot_reload_sources_base
  ${component_source_base}
  hot_reload.c
  hot_reload.h
  )

addprefix(hot_reload_sources ../component "${hot_reload_sources_base}")

add_library(hot_reload-objects OBJECT "${hot_reload_sources}")

if(NOT STATIC_COMPONENTS)
  add_library(mono-component-hot_reload SHARED $<TARGET_OBJECTS:hot_reload-objects>)
  target_link_libraries(mono-component-hot_reload monosgen-shared)
  install(TARGETS mono-component-hot_reload LIBRARY)
endif()
