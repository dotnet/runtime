
set(component_sources_base
  component.h
  )

set(hot_reload_sources_base
  ${component_source_base}
  hot-reload.c
  hot-reload.h
  )


if(NOT STATIC_COMPONENTS)
  add_library(mono-component-hot_reload SHARED "${hot_reload_sources}")
  target_link_libraries(mono-component-hot_reload monosgen-shared)
  install(TARGETS mono-component_hot_reload LIBRARY)
endif()
