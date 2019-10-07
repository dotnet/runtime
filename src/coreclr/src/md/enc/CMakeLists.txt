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
)

set(MDRUNTIMERW_HEADERS
  ../../inc/corhdr.h
  ../../inc/metadata.h
  ../../inc/pedecoder.h
  ../../inc/pedecoder.inl
  ../../inc/posterror.h
  ../../inc/sstring.h
  ../../inc/sstring.inl
  ../compiler/importhelper.h
  ../compiler/regmeta.h
  ../hotdata/hotdataformat.h
  ../inc/liteweightstgdb.h
  ../inc/mdfileformat.h
  ../inc/mdinternalrw.h
  ../inc/mdlog.h
  ../inc/metadatahash.h
  ../inc/metamodel.h
  ../inc/metamodelro.h
  ../inc/metamodelrw.h
  ../inc/rwutil.h
  ../inc/stgio.h
  ../inc/stgtiggerstorage.h
  ../inc/stgtiggerstream.h
  ../inc/streamutil.h
  ../runtime/mdinternalro.h
)

if (WIN32)
  list(APPEND MDRUNTIMERW_SOURCES ${MDRUNTIMERW_HEADERS})
endif(WIN32)

convert_to_absolute_path(MDRUNTIMERW_HEADERS ${MDRUNTIMERW_HEADERS})
convert_to_absolute_path(MDRUNTIMERW_SOURCES ${MDRUNTIMERW_SOURCES})

add_library_clr(mdruntimerw_dac ${MDRUNTIMERW_SOURCES})
set_target_properties(mdruntimerw_dac PROPERTIES DAC_COMPONENT TRUE)
target_precompile_header(TARGET mdruntimerw_dac HEADER stdafx.h)

add_library_clr(mdruntimerw_wks ${MDRUNTIMERW_SOURCES})
target_compile_definitions(mdruntimerw_wks PRIVATE FEATURE_METADATA_EMIT_ALL)
target_precompile_header(TARGET mdruntimerw_wks HEADER stdafx.h)

add_library_clr(mdruntimerw-dbi ${MDRUNTIMERW_SOURCES})
set_target_properties(mdruntimerw-dbi PROPERTIES DBI_COMPONENT TRUE)
target_precompile_header(TARGET mdruntimerw-dbi HEADER stdafx.h)

add_library_clr(mdruntimerw_crossgen ${MDRUNTIMERW_SOURCES})
set_target_properties(mdruntimerw_crossgen PROPERTIES CROSSGEN_COMPONENT TRUE)
target_precompile_header(TARGET mdruntimerw_crossgen HEADER stdafx.h)
