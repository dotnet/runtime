project(mono)

set(subdirs mini profiler)

foreach(dir ${subdirs})
  add_subdirectory(${dir})
endforeach()
