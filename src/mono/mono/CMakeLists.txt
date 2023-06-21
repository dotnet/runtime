project(mono)

find_package(dncp REQUIRED)
find_package(dnmd REQUIRED)

set(subdirs
  eglib
  utils
  sgen
  metadata
  mini
  profiler)

foreach(dir ${subdirs})
  add_subdirectory(${dir})
endforeach()
