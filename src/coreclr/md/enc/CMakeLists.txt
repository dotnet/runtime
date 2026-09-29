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
  ../../inc/cordecoderhelpers.h
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
target_compile_definitions(mdruntimerw_dac PRIVATE FEATURE_METADATA_PUBLIC_INTERFACES)
target_precompile_headers(mdruntimerw_dac PRIVATE stdafx.h)

add_library_clr(mdruntimerw_wks OBJECT ${MDRUNTIMERW_SOURCES})
target_compile_definitions(mdruntimerw_wks PRIVATE FEATURE_METADATA_CORECLR FEATURE_METADATA_EMIT_ALL)
if (FEATURE_CORPROFILER)
    target_compile_definitions(mdruntimerw_wks PRIVATE FEATURE_METADATA_PUBLIC_INTERFACES)
endif()
target_precompile_headers(mdruntimerw_wks PRIVATE stdafx.h)

add_library_clr(mdruntimerw-dbi ${MDRUNTIMERW_SOURCES})
set_target_properties(mdruntimerw-dbi PROPERTIES DBI_COMPONENT TRUE)
target_compile_definitions(mdruntimerw-dbi PRIVATE FEATURE_METADATA_PUBLIC_INTERFACES)
target_precompile_headers(mdruntimerw-dbi PRIVATE stdafx.h)

add_library_clr(mdruntimerw_ppdb ${MDRUNTIMERW_SOURCES})
target_compile_definitions(mdruntimerw_ppdb PRIVATE FEATURE_METADATA_EMIT_ALL FEATURE_METADATA_EMIT_PORTABLE_PDB FEATURE_METADATA_PERSISTENCE FEATURE_METADATA_PUBLIC_INTERFACES SELF_NO_HOST)
target_precompile_headers(mdruntimerw_ppdb PRIVATE stdafx.h)
