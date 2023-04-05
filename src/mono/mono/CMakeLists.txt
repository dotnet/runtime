project(mono)

set(subdirs
  eglib
  containers
  utils
  sgen
  metadata
  mini
  profiler)

foreach(dir ${subdirs})
  add_subdirectory(${dir})
endforeach()
