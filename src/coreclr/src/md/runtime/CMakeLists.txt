add_definitions(-DNO_COR)

set(MDRUNTIME_SOURCES
  mdcolumndescriptors.cpp
  liteweightstgdb.cpp
  mdfileformat.cpp
  metamodel.cpp
  metamodelro.cpp
  recordpool.cpp
  mdinternaldisp.cpp
  mdinternalro.cpp
)

set(MDRUNTIME_HEADERS
  ../../inc/caparser.h
  ../../inc/cor.h
  ../../inc/corhlpr.h
  ../../inc/corpriv.h
  ../../inc/mdcommon.h
  ../../inc/metadatatracker.h
  ../../inc/pedecoder.h
  ../../inc/posterror.h
  ../compiler/regmeta.h
  ../hotdata/export.h
  ../inc/assemblymdinternaldisp.h
  ../inc/liteweightstgdb.h
  ../inc/mdcolumndescriptors.h
  ../inc/mdfileformat.h
  ../inc/metamodel.h
  ../inc/metamodelro.h
  ../inc/recordpool.h
  ../inc/winmdinterfaces.h
  metamodelcolumndefs.h
  mdinternaldisp.h
  mdinternalro.h
  metamodel.cpp
  metamodelro.cpp
  recordpool.cpp
)

convert_to_absolute_path(MDRUNTIME_HEADERS ${MDRUNTIME_HEADERS})
convert_to_absolute_path(MDRUNTIME_SOURCES ${MDRUNTIME_SOURCES})

if (WIN32)
    list(APPEND MDRUNTIME_SOURCES ${MDRUNTIME_HEADERS})
endif (WIN32)

add_library_clr(mdruntime_dac ${MDRUNTIME_SOURCES})
set_target_properties(mdruntime_dac PROPERTIES DAC_COMPONENT TRUE)
target_precompile_header(TARGET mdruntime_dac HEADER stdafx.h)

add_library_clr(mdruntime_wks ${MDRUNTIME_SOURCES})
target_compile_definitions(mdruntime_wks PRIVATE FEATURE_METADATA_EMIT_ALL)
target_precompile_header(TARGET mdruntime_wks HEADER stdafx.h)

add_library_clr(mdruntime-dbi ${MDRUNTIME_SOURCES})
set_target_properties(mdruntime-dbi PROPERTIES DBI_COMPONENT TRUE)
target_precompile_header(TARGET mdruntime-dbi HEADER stdafx.h)

add_library_clr(mdruntime_crossgen ${MDRUNTIME_SOURCES})
set_target_properties(mdruntime_crossgen PROPERTIES CROSSGEN_COMPONENT TRUE)
target_precompile_header(TARGET mdruntime_crossgen HEADER stdafx.h)
