include(../../md_dbi.cmake)

add_precompiled_header(stdafx.h ../stdafx.cpp MDRUNTIMERW_SOURCES)
add_library(mdruntimerw-dbi ${MDRUNTIMERW_SOURCES})