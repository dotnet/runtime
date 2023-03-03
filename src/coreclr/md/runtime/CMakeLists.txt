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
  strongnameinternal.cpp
)

set(MDRUNTIME_HEADERS
  ../../inc/caparser.h
  ../../inc/cor.h
  ../../inc/corhlpr.h
  ../../inc/corpriv.h
  ../../inc/mdcommon.h
  ../../inc/mdfileformat.h
  ../../inc/pedecoder.h
  ../../inc/posterror.h
  ../compiler/regmeta.h
  ../inc/assemblymdinternaldisp.h
  ../inc/liteweightstgdb.h
  ../inc/mdcolumndescriptors.h
  ../inc/metamodel.h
  ../inc/metamodelro.h
  ../inc/pdbheap.h
  ../inc/portablepdbmdds.h
  ../inc/portablepdbmdi.h
  ../inc/recordpool.h
  metamodelcolumndefs.h
  mdinternaldisp.h
  mdinternalro.h
)

convert_to_absolute_path(MDRUNTIME_HEADERS ${MDRUNTIME_HEADERS})
convert_to_absolute_path(MDRUNTIME_SOURCES ${MDRUNTIME_SOURCES})

if (CLR_CMAKE_TARGET_WIN32)
    list(APPEND MDRUNTIME_SOURCES ${MDRUNTIME_HEADERS})
endif (CLR_CMAKE_TARGET_WIN32)

add_library_clr(mdruntime_dac ${MDRUNTIME_SOURCES})
set_target_properties(mdruntime_dac PROPERTIES DAC_COMPONENT TRUE)
target_precompile_headers(mdruntime_dac PRIVATE stdafx.h)

add_library_clr(mdruntime_wks_obj OBJECT ${MDRUNTIME_SOURCES})
target_compile_definitions(mdruntime_wks_obj PRIVATE FEATURE_METADATA_EMIT_ALL)
target_precompile_headers(mdruntime_wks_obj PRIVATE stdafx.h)
add_library(mdruntime_wks INTERFACE)
target_sources(mdruntime_wks INTERFACE $<TARGET_OBJECTS:mdruntime_wks_obj>)

add_library_clr(mdruntime-dbi ${MDRUNTIME_SOURCES})
set_target_properties(mdruntime-dbi PROPERTIES DBI_COMPONENT TRUE)
target_precompile_headers(mdruntime-dbi PRIVATE stdafx.h)

add_library_clr(mdruntime_ppdb ${MDRUNTIME_SOURCES})
target_compile_definitions(mdruntime_ppdb PRIVATE FEATURE_METADATA_EMIT_ALL FEATURE_METADATA_EMIT_PORTABLE_PDB)
target_precompile_headers(mdruntime_ppdb PRIVATE stdafx.h)
