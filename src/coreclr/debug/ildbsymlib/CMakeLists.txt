
set( ILDBSYMLIB_SOURCES
  symread.cpp
  symbinder.cpp
  ildbsymbols.cpp
  symwrite.cpp
)

add_library_clr(ildbsymlib_obj OBJECT ${ILDBSYMLIB_SOURCES})
add_library(ildbsymlib INTERFACE)
target_sources(ildbsymlib INTERFACE $<TARGET_OBJECTS:ildbsymlib_obj>)
