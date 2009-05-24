set(SUBDIRS utils io-layer cil metadata
      arch interpreter mini dis monograph tests benchmark profiler)

foreach(subdir ${SUBDIRS})
  add_subdirectory(${subdir})
endforeach()