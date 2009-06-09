set(SUBDIRS src test)

autoheader("config.h" null)

foreach(subdir ${SUBDIRS})
  add_subdirectory(${subdir})
endforeach()

