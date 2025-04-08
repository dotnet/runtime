project(mono)

set(subdirs
  minipal
  eglib
  utils
  sgen
  metadata
  mini
  profiler)

foreach(dir ${subdirs})
  add_subdirectory(${dir})
endforeach()
