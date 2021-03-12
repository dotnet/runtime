project(mono)

set(subdirs mini profiler)

if(ENABLE_PERFTRACING)
  list(APPEND subdirs
    eventpipe/test
  )
endif(ENABLE_PERFTRACING)

if(NOT STATIC_COMPONENTS)
  list(APPEND subdirs
    component
    )
endif()

foreach(dir ${subdirs})
  add_subdirectory(${dir})
endforeach()
