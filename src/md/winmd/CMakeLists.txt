set(MDWINMD_SOURCES
  adapter.cpp
  winmdimport.cpp
  winmdinternalimportro.cpp
)

set(MDWINMD_HEADERS
  ../../inc/cor.h
  ../../inc/corpriv.h
  ../../inc/metadataexports.h
  ../../inc/nsutilpriv.h
  ../../inc/sigbuilder.h
  ../../inc/sigparser.h
  ../../inc/utsem.h
  ../inc/winmdinterfaces.h
  inc/adapter.h
  inc/memotable.h
)

convert_to_absolute_path(MDWINMD_HEADERS ${MDWINMD_HEADERS})
convert_to_absolute_path(MDWINMD_SOURCES ${MDWINMD_SOURCES})

if (WIN32)
  list(APPEND MDWINMD_SOURCES ${MDWINMD_HEADERS})
endif (WIN32)


add_library_clr(mdwinmd_dac ${MDWINMD_SOURCES})
set_target_properties(mdwinmd_dac PROPERTIES DAC_COMPONENT TRUE)
target_precompile_header(TARGET mdwinmd_dac HEADER stdafx.h)

add_library_clr(mdwinmd_wks ${MDWINMD_SOURCES})
target_compile_definitions(mdwinmd_wks PRIVATE FEATURE_METADATA_EMIT_ALL)
target_precompile_header(TARGET mdwinmd_wks HEADER stdafx.h)

if(WIN32)
  add_library_clr(mdwinmd_dbi ${MDWINMD_SOURCES})
  set_target_properties(mdwinmd_dbi PROPERTIES DBI_COMPONENT TRUE)
  target_precompile_header(TARGET mdwinmd_dbi HEADER stdafx.h)

  add_library_clr(mdwinmd_crossgen ${MDWINMD_SOURCES})
  set_target_properties(mdwinmd_crossgen PROPERTIES CROSSGEN_COMPONENT TRUE)
  target_precompile_header(TARGET mdwinmd_crossgen HEADER stdafx.h)
endif(WIN32)
