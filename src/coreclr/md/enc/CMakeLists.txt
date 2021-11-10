set(MDRUNTIMERW_SOURCES
  liteweightstgdbrw.cpp
  metamodelenc.cpp
  metamodelrw.cpp
  peparse.cpp
  rwutil.cpp
  stgio.cpp
  stgtiggerstorage.cpp
  stgtiggerstream.cpp
  mdinternalrw.cpp
  pdbheap.cpp
)

set(MDRUNTIMERW_HEADERS
  ../../inc/corhdr.h
  ../../inc/metadata.h
  ../../inc/mdfileformat.h
  ../../inc/pedecoder.h
  ../../inc/pedecoder.inl
  ../../inc/posterror.h
  ../../inc/sstring.h
  ../../inc/sstring.inl
  ../compiler/importhelper.h
  ../compiler/regmeta.h
  ../inc/liteweightstgdb.h
  ../inc/mdinternalrw.h
  ../inc/mdlog.h
  ../inc/metadatahash.h
  ../inc/metamodel.h
  ../inc/metamodelro.h
  ../inc/metamodelrw.h
  ../inc/pdbheap.h
  ../inc/portablepdbmdds.h
  ../inc/portablepdbmdi.h
  ../inc/rwutil.h
  ../inc/stgio.h
  ../inc/stgtiggerstorage.h
  ../inc/stgtiggerstream.h
  ../inc/streamutil.h
  ../runtime/mdinternalro.h
)

if (CLR_CMAKE_TARGET_WIN32)
  list(APPEND MDRUNTIMERW_SOURCES ${MDRUNTIMERW_HEADERS})
endif(CLR_CMAKE_TARGET_WIN32)

convert_to_absolute_path(MDRUNTIMERW_HEADERS ${MDRUNTIMERW_HEADERS})
convert_to_absolute_path(MDRUNTIMERW_SOURCES ${MDRUNTIMERW_SOURCES})

add_library_clr(mdruntimerw_dac ${MDRUNTIMERW_SOURCES})
set_target_properties(mdruntimerw_dac PROPERTIES DAC_COMPONENT TRUE)
target_precompile_headers(mdruntimerw_dac PRIVATE stdafx.h)

add_library_clr(mdruntimerw_wks_obj OBJECT ${MDRUNTIMERW_SOURCES})
target_compile_definitions(mdruntimerw_wks_obj PRIVATE FEATURE_METADATA_EMIT_ALL)
target_precompile_headers(mdruntimerw_wks_obj PRIVATE stdafx.h)
add_library(mdruntimerw_wks INTERFACE)
target_sources(mdruntimerw_wks INTERFACE $<TARGET_OBJECTS:mdruntimerw_wks_obj>)

add_library_clr(mdruntimerw-dbi ${MDRUNTIMERW_SOURCES})
set_target_properties(mdruntimerw-dbi PROPERTIES DBI_COMPONENT TRUE)
target_precompile_headers(mdruntimerw-dbi PRIVATE stdafx.h)

add_library_clr(mdruntimerw_ppdb ${MDRUNTIMERW_SOURCES})
target_compile_definitions(mdruntimerw_ppdb PRIVATE FEATURE_METADATA_EMIT_ALL FEATURE_METADATA_EMIT_PORTABLE_PDB)
target_precompile_headers(mdruntimerw_ppdb PRIVATE stdafx.h)
