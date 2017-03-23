include(../../md_wks.cmake)

add_definitions(-DFEATURE_METADATA_EMIT_ALL)

add_precompiled_header(stdafx.h ../stdafx.cpp MDCOMPILER_SOURCES)
add_library_clr(mdcompiler_wks ${MDCOMPILER_SOURCES})
