project(mono)

set(subdirs eglib mini profiler)

foreach(dir ${subdirs})
  add_subdirectory(${dir})
endforeach()
