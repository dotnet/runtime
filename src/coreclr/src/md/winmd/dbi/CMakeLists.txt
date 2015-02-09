include(../../md_dbi.cmake)

add_precompiled_header(stdafx.h ../stdafx.cpp MDWINMD_SOURCES)
add_library(mdwinmd_dbi ${MDWINMD_SOURCES})