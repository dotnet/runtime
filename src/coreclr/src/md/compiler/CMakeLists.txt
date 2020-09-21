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
)

set(MDCOMPILER_HEADERS
  ../../inc/corhdr.h
  ../../inc/corpriv.h
  ../../inc/mdcommon.h
  ../../inc/metadata.h
  ../../inc/posterror.h
  ../../inc/sstring.h
  ../../inc/switches.h
  ../inc/cahlprinternal.h
  ../inc/mdlog.h
  ../inc/metamodelrw.h
  ../inc/rwutil.h
  ../inc/stgio.h
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

set(MDCOMPILER_WKS_SOURCES
  ${MDCOMPILER_SOURCES}
  verifylayouts.cpp
)

set(MDCOMPILER_WKS_HEADERS
  ${MDCOMPILER_HEADERS}
  ../inc/verifylayouts.h
)

convert_to_absolute_path(MDCOMPILER_SOURCES ${MDCOMPILER_SOURCES})
convert_to_absolute_path(MDCOMPILER_HEADERS ${MDCOMPILER_HEADERS})
convert_to_absolute_path(MDCOMPILER_WKS_SOURCES ${MDCOMPILER_WKS_SOURCES})
convert_to_absolute_path(MDCOMPILER_WKS_HEADERS ${MDCOMPILER_WKS_HEADERS})

if (CLR_CMAKE_TARGET_WIN32)
    list(APPEND MDCOMPILER_SOURCES ${MDCOMPILER_HEADERS})
    list(APPEND MDCOMPILER_WKS_SOURCES ${MDCOMPILER_WKS_HEADERS})
endif()

add_library_clr(mdcompiler_dac ${MDCOMPILER_SOURCES})
set_target_properties(mdcompiler_dac PROPERTIES DAC_COMPONENT TRUE)
target_precompile_header(TARGET mdcompiler_dac HEADER stdafx.h)

add_library_clr(mdcompiler_wks_obj OBJECT ${MDCOMPILER_WKS_SOURCES})
target_compile_definitions(mdcompiler_wks_obj PRIVATE FEATURE_METADATA_EMIT_ALL)
target_precompile_header(TARGET mdcompiler_wks_obj HEADER stdafx.h)
add_library(mdcompiler_wks INTERFACE)
target_sources(mdcompiler_wks INTERFACE $<TARGET_OBJECTS:mdcompiler_wks_obj>)

add_library_clr(mdcompiler-dbi ${MDCOMPILER_SOURCES})
set_target_properties(mdcompiler-dbi PROPERTIES DBI_COMPONENT TRUE)
target_precompile_header(TARGET mdcompiler-dbi HEADER stdafx.h)

add_library_clr(mdcompiler_crossgen ${MDCOMPILER_SOURCES})
set_target_properties(mdcompiler_crossgen PROPERTIES CROSSGEN_COMPONENT TRUE)
target_precompile_header(TARGET mdcompiler_crossgen HEADER stdafx.h)

add_library_clr(mdcompiler_ppdb ${MDCOMPILER_SOURCES})
target_compile_definitions(mdcompiler_ppdb PRIVATE FEATURE_METADATA_EMIT_ALL FEATURE_METADATA_EMIT_PORTABLE_PDB)
target_precompile_header(TARGET mdcompiler_ppdb HEADER stdafx.h)