include(../../md_wks.cmake)

add_definitions(-DFEATURE_METADATA_EMIT_ALL)

add_precompiled_header(stdafx.h ../stdafx.cpp MDRUNTIME_SOURCES)
add_library_clr(mdruntime_wks ${MDRUNTIME_SOURCES})

