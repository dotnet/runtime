include(../../md_dbi.cmake)

add_precompiled_header(stdafx.h ../stdafx.cpp MDCOMPILER_SOURCES)
add_library(mdcompiler-dbi ${MDCOMPILER_SOURCES})