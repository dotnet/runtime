include(../../md_wks.cmake)

add_precompiled_header(stdafx.h ../stdafx.cpp MDWINMD_SOURCES)
add_library(mdwinmd_wks ${MDWINMD_SOURCES})