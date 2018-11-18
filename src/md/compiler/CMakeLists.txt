set(MDCOMPILER_SOURCES
  assemblymd.cpp
  assemblymd_emit.cpp
  classfactory.cpp
  custattr_import.cpp
  custattr_emit.cpp
  disp.cpp
  emit.cpp
  filtermanager.cpp
  helper.cpp
  import.cpp
  importhelper.cpp
  mdutil.cpp
  regmeta.cpp
  regmeta_compilersupport.cpp
  regmeta_emit.cpp
  regmeta_import.cpp
  regmeta_imetadatatables.cpp
  regmeta_vm.cpp
  verifylayouts.cpp
)

set(MDCOMPILER_HEADERS
  ../../inc/corhdr.h
  ../../inc/corpriv.h
  ../../inc/mdcommon.h
  ../../inc/metadata.h
  ../../inc/posterror.h
  ../../inc/strongname.h
  ../../inc/sstring.h
  ../../inc/switches.h
  ../inc/cahlprinternal.h
  ../inc/mdlog.h
  ../inc/metamodelrw.h
  ../inc/rwutil.h
  ../inc/stgio.h
  ../inc/verifylayouts.h
  ../inc/VerifyLayouts.h
  cacheload.h
  classfactory.h
  custattr.h
  disp.h
  filtermanager.h
  importhelper.h
  mdperf.h
  mdsighelper.h
  mdutil.h
  regmeta.h
)

convert_to_absolute_path(MDCOMPILER_SOURCES ${MDCOMPILER_SOURCES})
convert_to_absolute_path(MDCOMPILER_HEADERS ${MDCOMPILER_HEADERS})

if(CLR_CMAKE_PLATFORM_UNIX)
    add_compile_options(-fPIC)
endif(CLR_CMAKE_PLATFORM_UNIX)

add_subdirectory(dac)
add_subdirectory(wks)
add_subdirectory(dbi)
add_subdirectory(crossgen)
