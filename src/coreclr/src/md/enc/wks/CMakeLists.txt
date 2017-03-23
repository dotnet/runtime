include(../../md_wks.cmake)

add_definitions(-DFEATURE_METADATA_EMIT_ALL)

add_precompiled_header(stdafx.h ../stdafx.cpp MDRUNTIMERW_SOURCES)
add_library_clr(mdruntimerw_wks ${MDRUNTIMERW_SOURCES})
