include(../../md_dbi.cmake)
add_precompiled_header(stdafx.h ../stdafx.cpp MDRUNTIME_SOURCES)
add_library(mdruntime-dbi ${MDRUNTIME_SOURCES})