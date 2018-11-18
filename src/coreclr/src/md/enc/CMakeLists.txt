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

if(CLR_CMAKE_PLATFORM_UNIX)
    add_compile_options(-fPIC)
endif(CLR_CMAKE_PLATFORM_UNIX)

add_subdirectory(dac)
add_subdirectory(wks)
add_subdirectory(dbi)
add_subdirectory(crossgen)
