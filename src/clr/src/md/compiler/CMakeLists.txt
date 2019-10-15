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

if (WIN32)
    list(APPEND MDCOMPILER_SOURCES ${MDCOMPILER_HEADERS})
endif()

add_library_clr(mdcompiler_dac ${MDCOMPILER_SOURCES})
set_target_properties(mdcompiler_dac PROPERTIES DAC_COMPONENT TRUE)
target_precompile_header(TARGET mdcompiler_dac HEADER stdafx.h)

add_library_clr(mdcompiler_wks ${MDCOMPILER_SOURCES})
target_compile_definitions(mdcompiler_wks PRIVATE FEATURE_METADATA_EMIT_ALL)
target_precompile_header(TARGET mdcompiler_wks HEADER stdafx.h)

add_library_clr(mdcompiler-dbi ${MDCOMPILER_SOURCES})
set_target_properties(mdcompiler-dbi PROPERTIES DBI_COMPONENT TRUE)
target_precompile_header(TARGET mdcompiler-dbi HEADER stdafx.h)

add_library_clr(mdcompiler_crossgen ${MDCOMPILER_SOURCES})
set_target_properties(mdcompiler_crossgen PROPERTIES CROSSGEN_COMPONENT TRUE)
target_precompile_header(TARGET mdcompiler_crossgen HEADER stdafx.h)
