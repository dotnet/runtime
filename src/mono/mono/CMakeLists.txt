project(mono)

set(subdirs mini profiler)

if(ENABLE_PERFTRACING)
  list(APPEND subdirs
    eventpipe/test
  )
endif(ENABLE_PERFTRACING)

foreach(dir ${subdirs})
  add_subdirectory(${dir})
endforeach()
