project(mono)

set(SHARED_CONTAINERS_SOURCE_PATH "${CLR_SRC_NATIVE_DIR}/containers/")

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
