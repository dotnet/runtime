project(mono)

set(subdirs
  eglib
  utils
  mini
  profiler)

foreach(dir ${subdirs})
  add_subdirectory(${dir})
endforeach()
