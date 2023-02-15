project(mono)

set(subdirs
  eglib
  utils
  sgen
  mini
  profiler)

foreach(dir ${subdirs})
  add_subdirectory(${dir})
endforeach()
