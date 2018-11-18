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

if(CLR_CMAKE_PLATFORM_UNIX)
    add_compile_options(-fPIC)
endif(CLR_CMAKE_PLATFORM_UNIX)

add_subdirectory(dac)
add_subdirectory(wks)
add_subdirectory(dbi)
add_subdirectory(crossgen)
